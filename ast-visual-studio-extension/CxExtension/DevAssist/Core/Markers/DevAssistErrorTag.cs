using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Custom error tag for DevAssist vulnerabilities
    /// Based on JetBrains TextAttributes with EffectType.WAVE_UNDERSCORE pattern
    /// Provides tooltip support and metadata for vulnerabilities
    /// NOTE: The actual colored squiggly underlines are drawn by DevAssistSquigglyAdornment
    /// This tag provides the infrastructure (tooltips, accessibility) without visible squiggles
    /// We use an empty/null error type to completely disable the default Visual Studio squiggle
    /// </summary>
    internal class DevAssistErrorTag : IErrorTag
    {
        /// <summary>
        /// Gets the error type - using Suggestion which has minimal visual impact
        /// The actual colored squiggles are drawn by the adornment layer
        /// Suggestion type shows as a subtle dotted underline that our custom squiggles will override
        /// </summary>
        public string ErrorType => PredefinedErrorTypeNames.Suggestion;

        /// <summary>
        /// Gets the tooltip text shown when hovering over the vulnerability
        /// </summary>
        public object ToolTipContent { get; }

        /// <summary>
        /// Gets the severity level of the vulnerability
        /// Used by the adornment layer to determine squiggle color
        /// </summary>
        public string Severity { get; }

        /// <summary>
        /// Gets the vulnerability ID
        /// </summary>
        public string VulnerabilityId { get; }

        public DevAssistErrorTag(string severity, string tooltipText, string vulnerabilityId)
        {
            Severity = severity;
            VulnerabilityId = vulnerabilityId;
            ToolTipContent = tooltipText;
        }
    }
}

