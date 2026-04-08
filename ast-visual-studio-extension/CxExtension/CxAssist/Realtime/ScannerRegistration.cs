using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxPreferences;
using System;
using CxWrapperClass = ast_visual_studio_extension.CxCLI.CxWrapper;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime
{
    /// <summary>
    /// Describes how to create and check enablement for a single realtime scanner.
    ///
    /// Design Pattern: Descriptor/Builder for extensible scanner registration.
    /// Each scanner is defined by:
    /// - A human-readable name (for logging)
    /// - An enablement predicate (checks if scanner should be active given current settings)
    /// - A factory function (creates the scanner instance)
    ///
    /// The orchestrator consumes a list of these; adding a new scanner requires only
    /// adding a new descriptor — no orchestrator edits (Open/Closed Principle).
    /// </summary>
    internal sealed class ScannerRegistration
    {
        /// <summary>
        /// Human-readable name for logging and debugging.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns true if this scanner should be activated given current settings.
        /// </summary>
        public Func<CxOneAssistSettingsModule, bool> IsEnabled { get; }

        /// <summary>
        /// Creates the scanner instance given a wrapper and settings.
        /// </summary>
        public Func<CxWrapperClass, CxOneAssistSettingsModule, IRealtimeScannerService> Factory { get; }

        public ScannerRegistration(
            string name,
            Func<CxOneAssistSettingsModule, bool> isEnabled,
            Func<CxWrapperClass, CxOneAssistSettingsModule, IRealtimeScannerService> factory)
        {
            Name = name;
            IsEnabled = isEnabled;
            Factory = factory;
        }
    }
}
