using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime
{
    /// <summary>
    /// Orchestrator for all five realtime scanners (ASCA, Secrets, IaC, Containers, OSS).
    /// Manages the lifecycle of enabled scanners based on settings and handles registration/unregistration.
    /// Serves as a facade for CxWindowControl — single entry point for realtime scanning.
    /// </summary>
    public class RealtimeScannerOrchestrator
    {
        private readonly List<IRealtimeScannerService> _scanners = new List<IRealtimeScannerService>();
        private ManifestFileWatcher _manifestWatcher;

        /// <summary>
        /// Initializes all enabled realtime scanners based on the current settings.
        /// Reads the settings module to determine which scanners are enabled,
        /// creates instances, and calls InitializeAsync on each.
        /// </summary>
        public async Task InitializeAsync(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper, CxOneAssistSettingsModule settings)
        {
            if (cxWrapper == null || settings == null) return;

            if (!ShouldInitializeRealtimeScanners(settings))
                return;

            try
            {
                // Initialize enabled scanners from registry
                // This loop is extensible: adding a new scanner requires only adding one entry to ScannerRegistry.All
                foreach (var registration in ScannerRegistry.All)
                {
                    if (!registration.IsEnabled(settings))
                        continue;

                    var scanner = registration.Factory(cxWrapper, settings);
                    await scanner.InitializeAsync();
                    _scanners.Add(scanner);
                }

                var solutionRoot = GetSolutionDirectory();

                // Start manifest file watcher to detect dependency/config changes
                StartManifestFileWatcher(solutionRoot);

                // OSS full manifest sweep runs from OssService.InitializeAsync (JetBrains: OssScannerCommand.scanAllManifestFilesInFolder).

                try
                {
                    cxWrapper.LogUserEventTelemetryFireAndForget(
                        "AssistRealtime",
                        "OrchestratorInitialized",
                        _scanners.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        "Info");
                }
                catch
                {
                    // Telemetry must not break initialization
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Error initializing realtime scanners: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Starts monitoring manifest files in the solution directory.
        /// </summary>
        private void StartManifestFileWatcher(string solutionRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(solutionRoot))
                {
                    OutputPaneWriter.WriteWarning("RealtimeScannerOrchestrator: Could not determine solution directory");
                    return;
                }

                _manifestWatcher = new ManifestFileWatcher(solutionRoot);
                _manifestWatcher.ManifestFileChanged += OnManifestFileChanged;
                _manifestWatcher.Start();
                OutputPaneWriter.WriteLine("RealtimeScannerOrchestrator: Manifest file watcher started");
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"RealtimeScannerOrchestrator: Failed to start manifest file watcher: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the solution directory from the VS environment.
        /// </summary>
        private string GetSolutionDirectory()
        {
            try
            {
                var dir = RealtimeSolutionScanner.TryGetSolutionDirectory();
                if (string.IsNullOrEmpty(dir))
                {
                    OutputPaneWriter.WriteDebug(
                        "RealtimeScannerOrchestrator: No solution file path yet (save the solution or open a .sln)");
                }
                return dir;
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"RealtimeScannerOrchestrator: Error getting solution directory: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Called when a manifest file is detected as changed.
        /// Triggers re-scans for affected scanners.
        /// </summary>
        private void OnManifestFileChanged(string filePath, System.IO.WatcherChangeTypes changeType)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                OutputPaneWriter.WriteLine($"RealtimeScannerOrchestrator: Manifest file changed: {fileName} ({changeType})");

                foreach (var scanner in _scanners)
                {
                    try
                    {
                        if (scanner.ShouldScanFile(filePath))
                            _ = scanner.ScanExternalFileAsync(filePath);
                    }
                    catch (Exception ex)
                    {
                        OutputPaneWriter.WriteDebug($"RealtimeScannerOrchestrator: rescan dispatch for {fileName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"RealtimeScannerOrchestrator: Error handling manifest file change: {ex.Message}");
            }
        }

        /// <summary>
        /// JetBrains parity: <c>DevAssistUtils.isScannerActive</c> / <c>GlobalScannerController.isScannerGloballyEnabled</c>
        /// (authenticated, MCP enabled, at least one Assist license flag, then per-engine toggles).
        /// </summary>
        private bool ShouldInitializeRealtimeScanners(CxOneAssistSettingsModule settings)
        {
            if (settings == null)
            {
                OutputPaneWriter.WriteDebug("RealtimeScannerOrchestrator: No Assist settings module, skipping scanner initialization");
                return false;
            }

            if (!CxPreferencesUI.IsAuthenticated())
            {
                OutputPaneWriter.WriteDebug("RealtimeScannerOrchestrator: User not authenticated, skipping scanner initialization");
                return false;
            }

            if (!settings.McpEnabled)
            {
                OutputPaneWriter.WriteDebug("RealtimeScannerOrchestrator: MCP disabled for tenant, skipping realtime scanners");
                return false;
            }

            if (!settings.DevAssistLicenseEnabled && !settings.OneAssistLicenseEnabled)
            {
                OutputPaneWriter.WriteDebug("RealtimeScannerOrchestrator: No Assist license entitlement, skipping realtime scanners");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Unregisters all active scanners, clearing markers, error list entries, and event subscriptions.
        /// Called when settings change or the extension is shutting down.
        /// </summary>
        public async Task UnregisterAllAsync()
        {
            try
            {
                foreach (var scanner in _scanners)
                {
                    await scanner.UnregisterAsync();
                }
                _scanners.Clear();

                // Stop manifest file watcher
                StopManifestFileWatcher();
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Error unregistering realtime scanners: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops monitoring manifest files and disposes the watcher.
        /// </summary>
        private void StopManifestFileWatcher()
        {
            try
            {
                if (_manifestWatcher != null)
                {
                    _manifestWatcher.ManifestFileChanged -= OnManifestFileChanged;
                    _manifestWatcher.Stop();
                    _manifestWatcher.Dispose();
                    _manifestWatcher = null;
                    OutputPaneWriter.WriteLine("RealtimeScannerOrchestrator: Manifest file watcher stopped");
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"RealtimeScannerOrchestrator: Error stopping manifest file watcher: {ex.Message}");
            }
        }
    }
}
