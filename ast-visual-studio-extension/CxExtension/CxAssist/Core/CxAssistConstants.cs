using System;
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
        /// <summary>IaC/KICS uses 1-based line numbers; other scanners use 0-based. Convert to 0-based for editor/taggers.</summary>
        public static int To0BasedLineForEditor(ScannerType scanner, int lineNumber)
        {
            return scanner == ScannerType.IaC ? Math.Max(0, lineNumber - 1) : lineNumber;
        }

        /// <summary>Convert to 1-based line for DTE MoveToLineAndOffset (IaC already 1-based; others add 1).</summary>
        public static int To1BasedLineForDte(ScannerType scanner, int lineNumber)
        {
            return scanner == ScannerType.IaC ? Math.Max(1, lineNumber) : Math.Max(1, lineNumber + 1);
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
        public const string SecretFindingLabel = "Secret finding";
        public const string SastVulnerabilityLabel = "SAST vulnerability";
        public const string IacVulnerabilityLabel = "IaC vulnerability";
        /// <summary>OSS Quick Info header suffix (reference: "validator@13.12.0 - High Severity Package").</summary>
        public const string SeverityPackageLabel = "Severity Package";

        /// <summary>Container image Quick Info header suffix (reference: "nginx:latest - Critical Severity Image").</summary>
        public const string SeverityImageLabel = "Severity Image";

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

        /// <summary>Context menu / Error List: "Ignore all [type]" label based on scanner (only shown for OSS and Containers).</summary>
        public static string GetIgnoreAllLabel(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.Secrets: return "Ignore all secrets";
                case ScannerType.Containers: return "Ignore all container issues";
                case ScannerType.IaC: return "Ignore all IaC findings";
                case ScannerType.ASCA: return "Ignore all ASCA violations";
                case ScannerType.OSS:
                default: return "Ignore all OSS issues";
            }
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
