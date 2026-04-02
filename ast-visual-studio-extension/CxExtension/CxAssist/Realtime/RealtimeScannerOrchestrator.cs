using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets;
using ast_visual_studio_extension.CxPreferences;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Initializes all enabled realtime scanners based on the current settings.
        /// Reads the settings module to determine which scanners are enabled,
        /// creates instances, and calls InitializeAsync on each.
        /// </summary>
        public async Task InitializeAsync(CxWrapper cxWrapper, CxOneAssistSettingsModule settings)
        {
            if (cxWrapper == null || settings == null) return;

            try
            {
                // Initialize ASCA scanner if enabled
                if (settings.AscaCheckBox)
                {
                    var ascaService = AscaService.GetInstance(cxWrapper);
                    await ascaService.InitializeAsync();
                    _scanners.Add(ascaService);
                }

                // Initialize Secrets scanner if enabled
                if (settings.SecretDetectionRealtimeCheckBox)
                {
                    var secretsService = SecretsService.GetInstance(cxWrapper);
                    await secretsService.InitializeAsync();
                    _scanners.Add(secretsService);
                }

                // Initialize IaC scanner if enabled
                if (settings.IacRealtimeCheckBox)
                {
                    var iacService = IacService.GetInstance(cxWrapper);
                    await iacService.InitializeAsync();
                    _scanners.Add(iacService);
                }

                // Initialize Containers scanner if enabled
                if (settings.ContainersRealtimeCheckBox)
                {
                    var containersTool = settings.ContainersTool ?? "docker";
                    var containersService = ContainersService.GetInstance(cxWrapper, containersTool);
                    await containersService.InitializeAsync();
                    _scanners.Add(containersService);
                }

                // Initialize OSS scanner if enabled
                if (settings.OssRealtimeCheckBox)
                {
                    var ossService = OssService.GetInstance(cxWrapper);
                    await ossService.InitializeAsync();
                    _scanners.Add(ossService);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing realtime scanners: {ex.Message}");
                throw;
            }
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unregistering realtime scanners: {ex.Message}");
                throw;
            }
        }
    }
}
