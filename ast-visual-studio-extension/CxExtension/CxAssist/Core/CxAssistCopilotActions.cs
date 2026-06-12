using System.Collections.Generic;
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
        /// For IaC/ASCA, automatically resolves all issues on the same line (aligned with JetBrains IacScanResultAdaptor grouping).
        /// </summary>
        public static void SendFixWithAssist(Vulnerability v, IReadOnlyList<Vulnerability> sameLineVulns = null)
        {
            if (v == null) return;
            string issueDesc = v.Title ?? v.Description ?? "unknown";
            string filePath = v.FilePath ?? "unknown";
            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.REMEDIATION_CALLED, "Fix", issueDesc));

            if (sameLineVulns == null && (v.Scanner == Models.ScannerType.IaC || v.Scanner == Models.ScannerType.ASCA))
            {
                sameLineVulns = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(v);
            }

            string prompt = CxOneAssistFixPrompts.BuildForVulnerability(v, sameLineVulns);
            if (string.IsNullOrEmpty(prompt))
            {
                ShowNoPromptMessage(v?.Title ?? v?.Description ?? "—", isFix: true);
                return;
            }

            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.REMEDIATION_STARTED, v.Scanner, issueDesc, filePath));
            bool sent = CopilotIntegration.SendPromptToCopilot(prompt, CxAssistConstants.CopilotFixFallbackMessage);
            if (sent)
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.REMEDIATION_SENT_COPILOT, v.Scanner, issueDesc, filePath));
            else
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.REMEDIATION_COMPLETED_CLIPBOARD, v.Scanner, issueDesc, filePath));
        }

        /// <summary>
        /// Sends a "View details" prompt to Copilot for the given vulnerability.
        /// Builds an explanation prompt by scanner type and opens Copilot with a new chat. No-op if no prompt is available.
        /// For OSS findings, automatically resolves all CVEs for the same package (aligned with JetBrains ScanIssue.getVulnerabilities()).
        /// For IaC/ASCA, automatically resolves all issues on the same line (aligned with JetBrains IacScanResultAdaptor grouping).
        /// </summary>
        /// <param name="v">The vulnerability to explain.</param>
        /// <param name="relatedVulns">Optional. Related vulnerabilities (same package for OSS, same line for IaC/ASCA). If null, auto-resolved from coordinator.</param>
        public static void SendViewDetails(Vulnerability v, IReadOnlyList<Vulnerability> relatedVulns = null)
        {
            if (v == null) return;
            string issueDesc = v.Title ?? v.Description ?? "unknown";
            string filePath = v.FilePath ?? "unknown";
            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.REMEDIATION_CALLED, "View details", issueDesc));

            if (relatedVulns == null)
            {
                if (v.Scanner == Models.ScannerType.OSS)
                    relatedVulns = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(v);
                else if (v.Scanner == Models.ScannerType.IaC || v.Scanner == Models.ScannerType.ASCA)
                    relatedVulns = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(v);
            }

            string prompt = ViewDetailsPrompts.BuildForVulnerability(v, relatedVulns);
            if (string.IsNullOrEmpty(prompt))
            {
                ShowNoPromptMessage($"{v?.Title ?? ""}\n{v?.Description ?? ""}\nSeverity: {v?.Severity}", isFix: false);
                return;
            }

            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.VIEW_DETAILS_STARTED, v.Scanner, issueDesc, filePath));
            bool sent = CopilotIntegration.SendPromptToCopilot(prompt, CxAssistConstants.CopilotViewDetailsFallbackMessage);
            if (sent)
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.VIEW_DETAILS_SENT_COPILOT, v.Scanner, issueDesc, filePath));
            else
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.VIEW_DETAILS_COMPLETED_CLIPBOARD, v.Scanner, issueDesc, filePath));
        }

        private static void ShowNoPromptMessage(string detail, bool isFix)
        {
            string message = isFix
                ? "No fix prompt available for this finding. " + detail
                : "View Details: " + detail;
            CopilotIntegration.ShowAssistNotification(message, isError: false);
        }
    }
}
