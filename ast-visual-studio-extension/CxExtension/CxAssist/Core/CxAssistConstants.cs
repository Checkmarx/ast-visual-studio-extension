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
        /// <summary>Product name shown in UI (Quick Info header, messages, Error List).</summary>
        public const string DisplayName = "Checkmarx One Assist";

        /// <summary>Suffix for grouped IaC findings on same line (JetBrains-style). Use as: count + MultipleIacIssuesOnLine.</summary>
        public const string MultipleIacIssuesOnLine = " IAC issues detected on this line";

        /// <summary>Suffix for grouped ASCA findings on same line (JetBrains-style). Use as: count + MultipleAscaViolationsOnLine.</summary>
        public const string MultipleAscaViolationsOnLine = " ASCA violations detected on this line";

        /// <summary>Suffix for grouped OSS findings on same line. Use as: count + MultipleOssIssuesOnLine.</summary>
        public const string MultipleOssIssuesOnLine = " OSS issues detected on this line";

        /// <summary>Suffix for grouped Secrets findings on same line. Use as: count + MultipleSecretsIssuesOnLine.</summary>
        public const string MultipleSecretsIssuesOnLine = " Secrets issues detected on this line";

        /// <summary>Suffix for grouped Containers findings on same line. Use as: count + MultipleContainersIssuesOnLine.</summary>
        public const string MultipleContainersIssuesOnLine = " Container issues detected on this line";

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
        /// </summary
    }
}
