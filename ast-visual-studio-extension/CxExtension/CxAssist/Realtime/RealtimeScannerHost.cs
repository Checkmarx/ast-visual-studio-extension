using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime
{
    /// <summary>
    /// Single shared orchestrator for realtime scanners so registration works from the package/solution
    /// without requiring the Checkmarx tool window to be open.
    /// </summary>
    internal static class RealtimeScannerHost
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static RealtimeScannerOrchestrator _orchestrator;
        private static bool _initializationInProgress;

        internal static async Task RegisterAsync(AsyncPackage package, CxCLI.CxWrapper cxWrapper, Type ownerType)
        {
            if (package == null)
                return;

            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await RegisterAsyncInternal(package, cxWrapper, ownerType);
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Internal register without acquiring gate (for use by callers that already hold the gate).
        /// Prevents deadlock when called from ReinitializeAsync which already holds Gate.
        /// </summary>
        private static async Task RegisterAsyncInternal(AsyncPackage package, CxCLI.CxWrapper cxWrapper, Type ownerType)
        {
            if (package == null)
                return;

            // Skip if already initialized or initialization in progress
            if (_initializationInProgress || _orchestrator != null)
                return;

            _initializationInProgress = true;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (!CxPreferencesUI.IsAuthenticated())
                    return;

                var assistSettings = package.GetDialogPage(typeof(CxOneAssistSettingsModule)) as CxOneAssistSettingsModule;
                if (assistSettings == null)
                    return;
                if (!assistSettings.McpEnabled)
                    return;
                if (!assistSettings.DevAssistLicenseEnabled && !assistSettings.OneAssistLicenseEnabled)
                    return;
                if (cxWrapper == null)
                    return;

                _orchestrator = new RealtimeScannerOrchestrator();
                await _orchestrator.InitializeAsync(cxWrapper, assistSettings);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Realtime scanner initialization failed: {ex.Message}");
            }
            finally
            {
                _initializationInProgress = false;
            }
        }

        internal static Task RegisterFromPackageAsync(AsyncPackage package, Type ownerType)
        {
            if (package == null)
                return Task.CompletedTask;

            return RegisterFromPackageCoreAsync(package, ownerType);
        }

        private static async Task RegisterFromPackageCoreAsync(AsyncPackage package, Type ownerType)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cxWrapper = CxUtils.GetCxWrapper(package, ownerType);
            await RegisterAsync(package, cxWrapper, ownerType).ConfigureAwait(true);
        }

        /// <summary>
        /// Drops the current orchestrator and rebuilds it from persisted Assist settings, then rescans the active
        /// document and manifest files. Used after login (when checkboxes are updated after auth) and when Assist
        /// settings are applied — avoids a no-op <see cref="RegisterFromPackageAsync"/> when an orchestrator already exists.
        /// </summary>
        internal static async Task ResyncFromPersistedSettingsAsync(AsyncPackage package, Type ownerType)
        {
            if (package == null)
                return;

            await UnregisterAsync().ConfigureAwait(true);
            await RegisterFromPackageAsync(package, ownerType).ConfigureAwait(true);
            await TriggerFullRescanAsync().ConfigureAwait(true);
        }

        internal static async Task UnregisterAsync()
        {
            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_orchestrator != null)
                {
                    await _orchestrator.UnregisterAllAsync();
                    _orchestrator = null;
                }

                OssManifestSweepPolicy.ClearSession();
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Realtime scanners failed to stop: {ex.Message}");
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Triggers manifest file and current document scans after authentication.
        /// Called when user logs in or re-enables scanners in settings.
        /// </summary>
        internal static async Task TriggerFullRescanAsync()
        {
            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await TriggerFullRescanAsyncInternal();
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Internal trigger without acquiring gate (for use by callers that already hold the gate).
        /// Prevents deadlock when called from ReinitializeAsync which already holds Gate.
        /// </summary>
        private static async Task TriggerFullRescanAsyncInternal()
        {
            if (_orchestrator == null)
                return;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _orchestrator.TriggerCurrentDocumentScanAsync();
                // OSS manifest sweep is started from OssService.InitializeAsync (JetBrains scanAllManifestFilesInFolder).
                // Avoid a second full solution walk here for every engine.
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Realtime scanner rescan failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Re-initializes scanners when enable/disable settings change.
        /// Unregisters old scanner instances (canceling pending scans) and initializes new set.
        /// Uses internal helpers to prevent deadlock (gate must be held for entire operation).
        /// </summary>
        internal static async Task ReinitializeAsync()
        {
            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Unregister old scanners (cancels pending scans)
                if (_orchestrator != null)
                {
                    await _orchestrator.UnregisterAllAsync();
                    _orchestrator = null;
                }

                _initializationInProgress = false;

                // Re-initialize with current settings (use internal helper to avoid re-acquiring gate)
                var package = ServiceProvider.GlobalProvider?.GetService(typeof(AsyncPackage)) as AsyncPackage;
                if (package != null)
                {
                    var cxWrapper = CxUtils.GetCxWrapper(package, typeof(RealtimeScannerHost));
                    await RegisterAsyncInternal(package, cxWrapper, typeof(RealtimeScannerHost)).ConfigureAwait(true);

                    // Trigger full rescan with new scanner set (use internal helper to avoid re-acquiring gate)
                    await TriggerFullRescanAsyncInternal();
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Failed to reinitialize scanners: {ex.Message}");
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Enables a specific scanner (e.g., when user checks a scanner checkbox).
        /// Only initializes that scanner, doesn't reinit the entire set.
        /// </summary>
        internal static async Task EnableScannerAsync(string scannerName)
        {
            if (_orchestrator == null || string.IsNullOrEmpty(scannerName))
                return;

            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _orchestrator.EnableScannerAsync(scannerName);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Failed to enable {scannerName} scanner: {ex.Message}");
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Disables a specific scanner (e.g., when user unchecks a scanner checkbox).
        /// Only unregisters that scanner, doesn't reinit the entire set.
        /// </summary>
        internal static async Task DisableScannerAsync(string scannerName)
        {
            if (_orchestrator == null || string.IsNullOrEmpty(scannerName))
                return;

            await Gate.WaitAsync().ConfigureAwait(true);
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _orchestrator.DisableScannerAsync(scannerName);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Failed to disable {scannerName} scanner: {ex.Message}");
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
