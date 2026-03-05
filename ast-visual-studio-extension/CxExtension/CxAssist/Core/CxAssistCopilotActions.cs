using System.Collections.Generic;
using System.Windows;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Reusable DevAssist actions: Fix with Checkmarx One Assist and View details.
    /// Builds the appropriate prompt and sends it to GitHub Copilot Chat. Use from Quick Info, Error List, Findings window, and Quick Fix.
    /// </summary>
    internal static class CxAssistCopilotActions
    {
        /// <summary>
        /// Sends a "Fix with Checkmarx One Assist" prompt to Copilot for the given vulnerability.
        /// Builds a remediation prompt by scanner type and opens Copilot with a new chat. No-op if no prompt is available.
        /// </summary>
        public static void SendFixWithAssist(Vulnerability v)
        {
            if (v == null) return;
            string prompt = CxOneAssistFixPrompts.BuildForVulnerability(v);
            if (string.IsNullOrEmpty(prompt))
            {
                ShowNoPromptMessage(v?.Title ?? v?.Description ?? "—", isFix: true);
                return;
            }
            CopilotIntegration.SendPromptToCopilot(prompt, CxAssistConstants.CopilotFixFallbackMessage);
        }

        /// <summary>
        /// Sends a "View details" prompt to Copilot for the given vulnerability.
        /// Builds an explanation prompt by scanner type and opens Copilot with a new chat. No-op if no prompt is available.
        /// </summary>
        /// <param name="v">The vulnerability to explain.</param>
        /// <param name="sameLineVulns">Optional. For OSS, pass other vulnerabilities on the same line to include CVE list in the prompt.</param>
        public static void SendViewDetails(Vulnerability v, IReadOnlyList<Vulnerability> sameLineVulns = null)
        {
            if (v == null) return;
            string prompt = ViewDetailsPrompts.BuildForVulnerability(v, sameLineVulns);
            if (string.IsNullOrEmpty(prompt))
            {
                ShowNoPromptMessage($"{v?.Title ?? ""}\n{v?.Description ?? ""}\nSeverity: {v?.Severity}", isFix: false);
                return;
            }
            CopilotIntegration.SendPromptToCopilot(prompt, CxAssistConstants.CopilotViewDetailsFallbackMessage);
        }

        private static void ShowNoPromptMessage(string detail, bool isFix)
        {
            string message = isFix
                ? "No fix prompt available for this finding.\n" + detail
                : "View Details:\n" + detail;
            MessageBox.Show(message, CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
