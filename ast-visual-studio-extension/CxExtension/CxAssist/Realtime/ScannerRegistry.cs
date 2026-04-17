using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets;
using ast_visual_studio_extension.CxPreferences;
using System.Collections.Generic;
using CxWrapperClass = ast_visual_studio_extension.CxCLI.CxWrapper;

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
                (w, s) => AscaService.GetInstance(w)),

            new ScannerRegistration("Secrets",
                s => s.SecretDetectionRealtimeCheckBox,
                (w, s) => SecretsService.GetInstance(w)),

            new ScannerRegistration("IaC",
                s => s.IacRealtimeCheckBox,
                (w, s) => IacService.GetInstance(w)),

            new ScannerRegistration("Containers",
                s => s.ContainersRealtimeCheckBox,
                (w, s) => ContainersService.GetInstance(w, s.ContainersTool ?? "docker")),

            new ScannerRegistration("OSS",
                s => s.OssRealtimeCheckBox,
                (w, s) => OssService.GetInstance(w)),
        };
    }
}
