using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Shared constants for CxAssist (display names, log categories, theme and resource names).
    /// Use these instead of magic strings to maintain consistency and simplify changes.
    /// </summary>
    internal static class CxAssistConstants
    {
        #region Scanner Enable/Disable (aligned with JetBrains GlobalScannerController)

        private static readonly HashSet<ScannerType> _disabledScanners = new HashSet<ScannerType>();
        private static readonly object _scannerLock = new object();

        /// <summary>
        /// Whether the given scanner type is enabled (aligned with JetBrains GlobalScannerController.isScannerGloballyEnabled).
        /// All scanners are enabled by default.
        /// </summary>
        public static bool IsScannerEnabled(ScannerType scanner)
        {
            lock (_scannerLock)
            {
                return !_disabledScanners.Contains(scanner);
            }
        }

        /// <summary>Enables or disables a scanner globally. Disabled scanner findings are excluded from decoration.</summary>
        public static void SetScannerEnabled(ScannerType scanner, bool enabled)
        {
            lock (_scannerLock)
            {
                if (enabled)
                    _disabledScanners.Remove(scanner);
                else
                    _disabledScanners.Add(scanner);
            }
            CxAssistOutputPane.WriteToOutputPane(string.Format(SCANNER_CONFIG_CHANGED, scanner, enabled ? "enabled" : "disabled"));
        }

        /// <summary>Whether any scanner is currently enabled.</summary>
        public static bool IsAnyScannerEnabled()
        {
            lock (_scannerLock)
            {
                return _disabledScanners.Count < Enum.GetValues(typeof(ScannerType)).Length;
            }
        }

        #endregion

        #region AI Agent File Skip (aligned with JetBrains DevAssistInspection.isAgentEvent)

        private static readonly string[] AiAgentFileNames = { "Dummy.txt", "AIAssistantInput" };
        private static readonly string[] AiAgentFilePrefixes = { "/AIAssistantInput" };

        /// <summary>
        /// Whether the file path belongs to a Copilot/AI assistant temporary file that should be skipped.
        /// JetBrains skips Dummy.txt and AIAssistantInput-* files generated during remediation or chat prompts.
        /// </summary>
        public static bool IsAIAgentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string fileName = System.IO.Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName)) return false;

            foreach (var agentName in AiAgentFileNames)
            {
                if (fileName.Equals(agentName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            foreach (var prefix in AiAgentFilePrefixes)
            {
                string prefixName = prefix.TrimStart('/');
                if (fileName.StartsWith(prefixName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        #endregion

        /// <summary>Vulnerability.LineNumber is 1-based in the model. Convert to 0-based for editor/taggers (ITextSnapshot).</summary>
        public static int To0BasedLineForEditor(ScannerType scanner, int lineNumber)
        {
            return Math.Max(0, lineNumber - 1);
        }

        /// <summary>Convert to 1-based line for DTE MoveToLineAndOffset. Vulnerability.LineNumber is already 1-based.</summary>
        public static int To1BasedLineForDte(ScannerType scanner, int lineNumber)
        {
            return Math.Max(1, lineNumber);
        }

        /// <summary>
        /// Whether the severity should be shown as a problem (underline + Error List / Problems view).
        /// Aligned with JetBrains DevAssistUtils.isProblem: not OK, not UNKNOWN, not IGNORED.
        /// Gutter icons are shown for all severities; underline and problem list only for "problem" severities.
        /// </summary>
        public static bool IsProblem(SeverityLevel severity)
        {
            return severity != SeverityLevel.Ok
                && severity != SeverityLevel.Unknown
                && severity != SeverityLevel.Ignored;
        }

        /// <summary>
        /// Whether the 1-based line number is within the document range.
        /// Aligned with JetBrains DevAssistUtils.isLineOutOfRange (inverted): valid when lineNumber in [1, lineCount].
        /// </summary>
        public static bool IsLineInRange(int lineNumber1Based, int documentLineCount)
        {
            return lineNumber1Based >= 1 && lineNumber1Based <= documentLineCount;
        }

        /// <summary>Removes "(CVE-...)" and "(Malicious)" from package/title text for display (e.g. "node-ipc (Malicious)@10.1.1" → "node-ipc").</summary>
        public static string StripCveFromDisplayName(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = Regex.Replace(text.Trim(), @"\s*\(CVE-[^)]+\)", "").Trim();
            text = Regex.Replace(text, @"\s*\(Malicious\)", "", RegexOptions.IgnoreCase).Trim();
            return text;
        }

        /// <summary>Formats secret title for display: kebab-case to Title-Case (e.g. "generic-api-key" → "Generic-Api-Key"). Reference formatTitle.</summary>
        public static string FormatSecretTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;
            var parts = title.Split(new[] { '-' }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
            }
            return string.Join("-", parts);
        }
        /// <summary>Product name shown in UI (Quick Info header, messages, Error List).</summary>
        public const string DisplayName = "Checkmarx One Assist";

        /// <summary>Suffix for grouped IaC findings on same line (reference-style). Use as: count + MultipleIacIssuesOnLine.</summary>
        public const string MultipleIacIssuesOnLine = " IAC issues detected on this line";

        /// <summary>Suffix for grouped ASCA findings on same line (reference-style). Use as: count + MultipleAscaViolationsOnLine.</summary>
        public const string MultipleAscaViolationsOnLine = " ASCA violations detected on this line";

        /// <summary>Suffix for grouped OSS findings on same line. Use as: count + MultipleOssIssuesOnLine.</summary>
        public const string MultipleOssIssuesOnLine = " OSS issues detected on this line";

        /// <summary>Suffix for grouped Secrets findings on same line. Use as: count + MultipleSecretsIssuesOnLine.</summary>
        public const string MultipleSecretsIssuesOnLine = " Secrets issues detected on this line";

        /// <summary>Suffix for grouped Containers findings on same line. Use as: count + MultipleContainersIssuesOnLine.</summary>
        public const string MultipleContainersIssuesOnLine = " Container issues detected on this line";

        /// <summary>Human-readable severity name for UI (Quick Info, tooltips, etc.).</summary>
        public static string GetRichSeverityName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Critical: return "Critical";
                case SeverityLevel.High: return "High";
                case SeverityLevel.Medium: return "Medium";
                case SeverityLevel.Low: return "Low";
                case SeverityLevel.Info: return "Info";
                case SeverityLevel.Malicious: return "Malicious";
                case SeverityLevel.Unknown: return "Unknown";
                case SeverityLevel.Ok: return "Ok";
                case SeverityLevel.Ignored: return "Ignored";
                default: return severity.ToString();
            }
        }

        /// <summary>Log category for debug/trace output (e.g. Debug.WriteLine).</summary>
        public const string LogCategory = "CxAssist";

        #region Output Pane Messages (main lifecycle messages written to VS Output Window)

        public const string UI_DECORATED_SUCCESSFULLY = "UI decorated successfully on file open for file: {0} ({1} findings)";
        public const string NO_SCANNER_ENABLED_SKIPPING = "No scanner is enabled, skipping restoring gutter icons for file: {0}";
        public const string AI_AGENT_FILE_SKIPPING = "Received copilot/AI agent event for file: {0}. Skipping file.";
        public const string SCANNER_CONFIG_CHANGED = "Scanner config changed: {0} is now {1}";
        public const string DECORATING_UI_FOR_FILE = "Decorating UI using {0} results for file: {1}";
        public const string NO_VULNERABILITIES_FOR_FILE = "No vulnerabilities found in scan result for file: {0}";
        public const string FINDINGS_WINDOW_INITIATED = "Checkmarx One Assist Findings window initiated";
        public const string ICONS_LOADED_FOR_THEME = "Loaded icons for theme: {0}";
        public const string ICONS_RELOADING_FOR_THEME = "Icons reloading for theme change ({0} -> {1})";
        public const string REMEDIATION_CALLED = "Remediation called: {0} for issue: {1}";
        public const string REMEDIATION_STARTED = "{0} remediation started for issue: {1}, for file: {2}";
        public const string REMEDIATION_SENT_COPILOT = "{0} remediation sent to Copilot for issue: {1}, for file: {2}";
        public const string REMEDIATION_COMPLETED_CLIPBOARD = "{0} remediation completed (clipboard) for issue: {1}, for file: {2}";
        public const string VIEW_DETAILS_STARTED = "{0} explanation started for issue: {1}, for file: {2}";
        public const string VIEW_DETAILS_SENT_COPILOT = "{0} explanation sent to Copilot for issue: {1}, for file: {2}";
        public const string VIEW_DETAILS_COMPLETED_CLIPBOARD = "{0} explanation completed (clipboard) for issue: {1}, for file: {2}";
        public const string ERROR_LIST_SYNCED = "Error List synced: {0} tasks for {1} files";
        public const string FIX_PROMPT_COPIED = "Fix prompt copied to clipboard for issue: {0}";
        public const string FAILED_COPY_CLIPBOARD = "Failed to copy text to clipboard";

        #endregion

        /// <summary>Theme folder name for dark theme icons.</summary>
        public const string ThemeDark = "Dark";

        /// <summary>Theme folder name for light theme icons.</summary>
        public const string ThemeLight = "Light";

        /// <summary>Badge image file name (header in Quick Info).</summary>
        public const string BadgeIconFileName = "cxone_assist.png";

        /// <summary>
        /// When true, CxAssist findings are also added to the built-in Error List.
        /// When false, the hover popup shows only one block (our Quick Info).
        /// </summary>
        public const bool SyncFindingsToBuiltInErrorList = true;

        /// <summary>
        /// When true and SyncFindingsToBuiltInErrorList is true: Error List task Text is set empty so the hover
        /// popup does not show a second duplicate block (VS still shows the task in the list with File/Line/Column;
        /// full details are in our Quick Info on hover). When false: full description is shown in the Error List
        /// but the same text appears again in the hover (duplicate).
        /// </summary>
        /// <summary>Menu label (reference-aligned).</summary>
        public const string FixWithCxOneAssist = "Fix with Checkmarx One Assist";
        public const string ViewDetails = "View details";
        public const string IgnoreThis = "Ignore this vulnerability";
        public const string IgnoreAllOfThisType = "Ignore all of this type";
        public const string CopyMessage = "Copy Message";
        public const string IgnoreFeatureInProgressMessage = "This feature is currently in progress and will be available in a future release.";
        public const string SecretFindingLabel = "Secret finding";
        public const string SastVulnerabilityLabel = "SAST vulnerability";
        public const string IacVulnerabilityLabel = "IaC vulnerability";
        /// <summary>OSS Quick Info header suffix (reference: "validator@13.12.0 - High Severity Package").</summary>
        public const string SeverityPackageLabel = "Severity Package";

        /// <summary>Container image Quick Info header suffix (reference: "nginx:latest - Critical Severity Image").</summary>
        public const string SeverityImageLabel = "Severity Image";

        // --- Copilot / DevAssist (reusable messages for Fix & View details) ---
        public const string CopilotFixFallbackMessage = "Fix prompt copied. Paste into GitHub Copilot Chat to get remediation steps.";
        public const string CopilotViewDetailsFallbackMessage = "View details prompt copied. Paste into GitHub Copilot Chat to get an explanation.";
        public const string CopilotPromptSentMessage = "Prompt was sent to GitHub Copilot Chat. Check the chat for the response.";
        public const string CopilotPasteFailedMessage = "Prompt copied to clipboard! Paste it into GitHub Copilot Chat (Agent Mode).";
        public const string CopilotOpenInstructionsMessage = "Prompt copied to clipboard! Paste it into GitHub Copilot Chat (Agent Mode).";
        public const string CopilotGenericFallbackMessage = "Prompt copied to clipboard. Paste into GitHub Copilot Chat.";

        /// <summary>Context menu / Error List / Quick Info / Quick Fix: "Ignore this [finding type]" label based on scanner.</summary>
        public static string GetIgnoreThisLabel(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.Secrets: return "Ignore this secret in file";
                case ScannerType.Containers:
                case ScannerType.ASCA:
                case ScannerType.IaC:
                case ScannerType.OSS:
                default: return "Ignore this vulnerability";
            }
        }

        /// <summary>True only for OSS and Containers; Secret, ASCA, IaC show only "Ignore this" (no "Ignore all").</summary>
        public static bool ShouldShowIgnoreAll(ScannerType scanner)
        {
            return scanner == ScannerType.OSS || scanner == ScannerType.Containers;
        }

        /// <summary>Context menu / Error List: "Ignore all of this type" for OSS and Containers (only shown for those scanners).</summary>
        public static string GetIgnoreAllLabel(ScannerType scanner)
        {
            return "Ignore all of this type";
        }

        /// <summary>Success message after "Ignore this" (e.g. "Vulnerability ignored.").</summary>
        public static string GetIgnoreThisSuccessMessage(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.Secrets: return "Secret ignored.";
                case ScannerType.Containers: return "Container image ignored.";
                case ScannerType.IaC: return "IaC finding ignored.";
                case ScannerType.ASCA: return "ASCA violation ignored.";
                case ScannerType.OSS:
                default: return "Vulnerability ignored.";
            }
        }

        /// <summary>Success message after "Ignore all" (e.g. "All vulnerabilities of this type ignored.").</summary>
        public static string GetIgnoreAllSuccessMessage(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.Secrets: return "All secrets ignored.";
                case ScannerType.Containers: return "All container issues ignored.";
                case ScannerType.IaC: return "All IaC findings ignored.";
                case ScannerType.ASCA: return "All ASCA violations ignored.";
                case ScannerType.OSS:
                default: return "All OSS issues ignored.";
            }
        }
    }
}
