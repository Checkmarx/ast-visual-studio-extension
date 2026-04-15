using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private ast_visual_studio_extension.CxCLI.CxWrapper _cxWrapper;
        private CxOneAssistSettingsModule _settings;

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

            // Store for later use in enable/disable operations
            _cxWrapper = cxWrapper;
            _settings = settings;

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

                if (_scanners.Count > 0)
                {
                    var names = string.Join(", ", _scanners.Select(s => GetScannerName(s)));
                    OutputPaneWriter.WriteLine($"Realtime scanners started: {names}");
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
                OutputPaneWriter.WriteDebug($"Manifest file changed: {fileName} ({changeType})");

                // JetBrains parity: dependency manifest rescans are OSS-only; other engines follow the active document.
                foreach (var scanner in _scanners)
                {
                    if (!(scanner is OssService oss))
                        continue;
                    try
                    {
                        if (oss.ShouldScanFile(filePath))
                            _ = oss.ScanExternalFileAsync(filePath);
                    }
                    catch (Exception ex)
                    {
                        OutputPaneWriter.WriteDebug($"RealtimeScannerOrchestrator: OSS manifest rescan for {fileName}: {ex.Message}");
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
                // Cancel all pending scans first
                CancelAllPendingScans();

                foreach (var scanner in _scanners)
                {
                    await scanner.UnregisterAsync();
                }

                if (_scanners.Count > 0)
                    OutputPaneWriter.WriteLine("Realtime scanners stopped");

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
        /// Cancels all pending debounced scans across all active scanners.
        /// </summary>
        private void CancelAllPendingScans()
        {
            try
            {
                foreach (var scanner in _scanners)
                {
                    scanner.CancelPendingScans();
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"RealtimeScannerOrchestrator: Error canceling pending scans: {ex.Message}");
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
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"RealtimeScannerOrchestrator: Error stopping manifest file watcher: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables a specific scanner by name and initializes it.
        /// Called when user enables a scanner checkbox.
        /// </summary>
        public async Task EnableScannerAsync(string scannerName)
        {
            if (string.IsNullOrEmpty(scannerName) || _cxWrapper == null || _settings == null)
                return;

            try
            {
                // Find the registration for this scanner
                var registration = ScannerRegistry.All.FirstOrDefault(r => r.Name == scannerName);
                if (registration == null || registration.IsEnabled(_settings))
                    return; // Already enabled or not found

                // Create and initialize the scanner
                var scanner = registration.Factory(_cxWrapper, _settings);
                await scanner.InitializeAsync();
                _scanners.Add(scanner);

                OutputPaneWriter.WriteLine($"{scannerName} scanner enabled");

                var solutionDir = GetSolutionDirectory();
                await scanner.TriggerCurrentDocumentScanAsync();

                // OSS: full manifest sweep runs from Initialize when policy allows. If policy already marked this
                // solution (e.g. OSS toggled off/on), run manifest resync here so manifests are still scanned.
                if (scanner is OssService && !string.IsNullOrEmpty(solutionDir)
                    && !OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(solutionDir))
                    await scanner.RescanManifestFilesAsync(solutionDir);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Failed to enable {scannerName} scanner: {ex.Message}");
            }
        }

        /// <summary>
        /// Disables a specific scanner by name and unregisters it.
        /// Called when user disables a scanner checkbox.
        /// </summary>
        public async Task DisableScannerAsync(string scannerName)
        {
            if (string.IsNullOrEmpty(scannerName))
                return;

            try
            {
                var scannerToRemove = _scanners.FirstOrDefault(s => GetScannerName(s) == scannerName);
                if (scannerToRemove == null)
                    return;

                // Cancel pending scans first
                scannerToRemove.CancelPendingScans();

                // Unregister
                await scannerToRemove.UnregisterAsync();
                _scanners.Remove(scannerToRemove);

                OutputPaneWriter.WriteLine($"{scannerName} scanner disabled");
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"Failed to disable {scannerName} scanner: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the scanner type name from a scanner instance.
        /// </summary>
        private string GetScannerName(IRealtimeScannerService scanner)
        {
            if (scanner is Asca.AscaService) return "ASCA";
            if (scanner is Secrets.SecretsService) return "Secrets";
            if (scanner is Iac.IacService) return "IaC";
            if (scanner is Containers.ContainersService) return "Containers";
            if (scanner is Oss.OssService) return "OSS";
            return scanner.GetType().Name;
        }

        /// <summary>
        /// Triggers an instant scan of the current active document.
        /// Called when user logs in, after re-authentication, or when scanners are re-enabled.
        /// </summary>
        public async Task TriggerCurrentDocumentScanAsync()
        {
            try
            {
                var dte = ServiceProvider.GlobalProvider?.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.ActiveDocument == null)
                    return;

                var filePath = dte.ActiveDocument.FullName;
                if (string.IsNullOrEmpty(filePath))
                    return;

                foreach (var scanner in _scanners)
                {
                    await scanner.InstantScanAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"TriggerCurrentDocumentScanAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// OSS-only solution manifest sweep. Other engines use <see cref="TriggerCurrentDocumentScanAsync"/> only.
        /// </summary>
        public async Task TriggerManifestRescanAsync()
        {
            try
            {
                var solutionRoot = GetSolutionDirectory();
                if (string.IsNullOrEmpty(solutionRoot))
                    return;

                var oss = _scanners.OfType<OssService>().FirstOrDefault();
                if (oss != null)
                    await oss.RescanManifestFilesAsync(solutionRoot);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"TriggerManifestRescanAsync failed: {ex.Message}");
            }
        }
    }
}
