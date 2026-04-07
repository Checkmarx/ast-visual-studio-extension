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

        internal static async Task RegisterAsync(AsyncPackage package, CxCLI.CxWrapper cxWrapper, Type ownerType)
        {
            if (package == null)
                return;

            await Gate.WaitAsync().ConfigureAwait(true);
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

                if (_orchestrator != null)
                    await _orchestrator.UnregisterAllAsync();

                _orchestrator = new RealtimeScannerOrchestrator();
                await _orchestrator.InitializeAsync(cxWrapper, assistSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RealtimeScannerHost: {ex.Message}");
                OutputPaneWriter.WriteError($"Realtime scanner initialization failed: {ex.Message}");
            }
            finally
            {
                Gate.Release();
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RealtimeScannerHost: unregister failed: {ex.Message}");
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
