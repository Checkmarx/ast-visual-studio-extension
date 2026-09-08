using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime
{
    /// <summary>
    /// Authoritative list of all realtime scanner registrations.
    /// Single source of truth for scanner configuration and creation.
    ///
    /// Design Pattern: Registry pattern for extensible scanner discovery.
    ///
    /// To add a new scanner: add one entry here. Do NOT edit RealtimeScannerOrchestrator.
    /// The orchestrator loops through this list, checking IsEnabled for each and creating
    /// instances via Factory. New scanners require zero orchestrator changes.
    /// </summary>
    internal static class ScannerRegistry
    {
        /// <summary>
        /// All realtime scanner registrations.
        /// Each entry pairs a scanner class with its enablement settings property
        /// and factory function.
        /// </summary>
        public static IReadOnlyList<ScannerRegistration> All { get; } = new List<ScannerRegistration>
        {
            new ScannerRegistration("ASCA",
                s => s.AscaCheckBox,
                (p, s) => AscaService.GetInstance(p)),

            new ScannerRegistration("Secrets",
                s => s.SecretDetectionRealtimeCheckBox,
                (p, s) => SecretsService.GetInstance(p)),

            new ScannerRegistration("IaC",
                s => s.IacRealtimeCheckBox,
                (p, s) => IacService.GetInstance(p)),

            new ScannerRegistration("Containers",
                s => s.ContainersRealtimeCheckBox,
                (p, s) => ContainersService.GetInstance(p, s.ContainersTool ?? "docker")),

            new ScannerRegistration("OSS",
                s => s.OssRealtimeCheckBox,
                (p, s) => OssService.GetInstance(p)),
        };
    }
}
