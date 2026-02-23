using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.DevAssist.UI.FindingsWindow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core
{
    /// <summary>
    /// Common mock data used to demonstrate all four POC features:
    /// underline (squiggle), gutter icon, problem window, and popup hover.
    /// One source of truth so editor and findings window show the same data.
    /// </summary>
    public static class DevAssistMockData
    {
        /// <summary>Default file path used for mock vulnerabilities (editor and findings window).</summary>
        public const string DefaultFilePath = "Program.cs"

        /// <summary>Vulnerability Id that uses standard Quick Info popup only (no custom hover popup).</summary>
        public const string QuickInfoOnlyVulnerabilityId = "POC-007";

        /// <summary>
        /// Returns the common list of mock vulnerabilities used for:
        /// - Gutter icons (severity-specific icons on lines 1, 3, 5, 7, 9)
        /// - Underline (squiggles on the same lines)
        /// - Popup hover (hover over those lines to see rich popup with OSS/ASCA content)
        /// - Problem window (when converted to FileNodes via BuildFileNodesFromVulnerabilities)
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses DefaultFilePath.</param>
        public static List<Vulnerability> GetCommonVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? DefaultFilePath : filePath;

            return new List<Vulnerability>
            {
                // Line 1 – Malicious (OSS) – shows in gutter, underline, hover, problem window
                new Vulnerability
                {
                    Id = "POC-001",
                    Title = "Malicious Package",
                    Description = "Test Malicious vulnerability – known malicious package in dependencies.",
                    Severity = SeverityLevel.Malicious,
                    Scanner = ScannerType.OSS,
                    LineNumber = 1,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "node-ipc",
                    PackageVersion = "10.1.1",
                    RecommendedVersion = "10.2.0",
                    CveName = "CVE-Malicious-Example",
                    CvssScore = 9.8,
                    LearnMoreUrl = "https://example.com/cve"
                },
                // Line 3 – Critical (ASCA)
                new Vulnerability
                {
                    Id = "POC-002",
                    Title = "SQL Injection",
                    Description = "Test Critical vulnerability – user input concatenated into SQL without sanitization.",
                    Severity = SeverityLevel.Critical,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 3,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "SQL_INJECTION",
                    RemediationAdvice = "Use parameterized queries or prepared statements."
                },
                // Line 5 – High (OSS) – first of two on same line (severity count in popup)
                new Vulnerability
                {
                    Id = "POC-003",
                    Title = "High-Risk Package",
                    Description = "Test High vulnerability – vulnerable version of package.",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.OSS,
                    LineNumber = 5,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "lodash",
                    PackageVersion = "4.17.15",
                    RecommendedVersion = "4.17.21",
                    CveName = "CVE-2020-8203",
                    CvssScore = 7.4
                },
                // Line 5 – Medium (second on same line)
                new Vulnerability
                {
                    Id = "POC-004",
                    Title = "Medium Severity Finding",
                    Description = "Test Medium vulnerability on same line as High.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 5,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "WEAK_CRYPTO",
                    RemediationAdvice = "Use a stronger algorithm."
                },
                // Line 7 – Medium (OSS)
                new Vulnerability
                {
                    Id = "POC-005",
                    Title = "Outdated Dependency",
                    Description = "Test Medium vulnerability – dependency has a known issue.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 7,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "axios",
                    PackageVersion = "0.21.0",
                    RecommendedVersion = "0.27.0"
                },
                // Line 9 – Low
                new Vulnerability
                {
                    Id = "POC-006",
                    Title = "Low Severity",
                    Description = "Test Low vulnerability – minor finding.",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.OSS,
                    LineNumber = 9,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "debug",
                    PackageVersion = "2.6.9"
                },
                // Line 11 – Quick Info only (no custom hover popup): shows standard VS Quick Info with rich text and links
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "High Severity Finding",
                    Description = "This finding uses the standard Quick Info popup: Checkmarx One Assist badge, rich severity name, description, and action links (Fix with Checkmarx Assist, View Details, Ignore vulnerability).",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 11,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "QUICK_INFO_DEMO",
                    RemediationAdvice = "Use the Quick Info links to fix, view details, or ignore."
                },
                // Line 13 – Quick Info only: 2 vulnerabilities on same line (no custom popup; hover shows Quick Info for first)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "First finding on line (Critical)",
                    Description = "First of two Quick-Info-only findings on this line. Critical severity – sensitive data exposure risk.",
                    Severity = SeverityLevel.Critical,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 13,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "SENSITIVE_DATA",
                    RemediationAdvice = "Avoid logging or exposing sensitive data."
                },
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Second finding on line (Medium)",
                    Description = "Second of two Quick-Info-only findings on this line. Medium severity – weak cryptographic usage.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 13,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "WEAK_CRYPTO",
                    RemediationAdvice = "Use a stronger algorithm."
                },
                // Line 15 – Quick Info only (single finding)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Quick Info – Outdated dependency",
                    Description = "Quick-Info-only demo: outdated package with known CVE. Use standard Quick Info links to fix or view details.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 15,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "minimist",
                    PackageVersion = "1.2.0",
                    RecommendedVersion = "1.2.6",
                    CveName = "CVE-2022-21803"
                },
                // Line 17 – Quick Info only (single finding)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Quick Info – Low severity",
                    Description = "Quick-Info-only demo: low-severity finding. Only the standard Quick Info popup is shown here.",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 17,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "LOW_SEVERITY_DEMO",
                    RemediationAdvice = "Consider addressing in next sprint."
                }
            };
        }

        /// <summary>
        /// Builds the Findings window tree (FileNode with VulnerabilityNodes) from the common vulnerability list.
        /// Use the same list from GetCommonVulnerabilities so problem window shows the same data as gutter/underline/hover.
        /// </summary>
        /// <param name="vulnerabilities">Typically from GetCommonVulnerabilities().</param>
        /// <param name="loadSeverityIcon">Callback to get severity icon (e.g. from ShowFindingsWindowCommand). Can be null; then SeverityIcon is not set.</param>
        /// <param name="loadFileIcon">Callback to get file icon. Can be null.</param>
        public static ObservableCollection<FileNode> BuildFileNodesFromVulnerabilities(
            List<Vulnerability> vulnerabilities,
            Func<string, ImageSource> loadSeverityIcon = null,
            Func<ImageSource> loadFileIcon = null)
        {
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return new ObservableCollection<FileNode>();

            var fileIcon = loadFileIcon?.Invoke();
            var grouped = vulnerabilities
                .GroupBy(v => string.IsNullOrEmpty(v.FilePath) ? DefaultFilePath : v.FilePath)
                .OrderBy(g => g.Key);

            var fileNodes = new ObservableCollection<FileNode>();

            foreach (var group in grouped)
            {
                var filePath = group.Key;
                var fileName = System.IO.Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(fileName)) fileName = filePath;

                var fileNode = new FileNode
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileIcon = fileIcon
                };

                foreach (var v in group)
                {
                    var vulnNode = new VulnerabilityNode
                    {
                        Severity = v.Severity.ToString(),
                        SeverityIcon = loadSeverityIcon?.Invoke(v.Severity.ToString()),
                        Description = v.Title ?? v.Description,
                        PackageName = v.PackageName,
                        PackageVersion = v.PackageVersion,
                        Line = v.LineNumber,
                        Column = v.ColumnNumber,
                        FilePath = v.FilePath
                    };
                    fileNode.Vulnerabilities.Add(vulnNode);
                }

                // Severity counts for badges
                var severityCounts = fileNode.Vulnerabilities
                    .GroupBy(n => n.Severity)
                    .Select(g => new SeverityCount
                    {
                        Severity = g.Key,
                        Count = g.Count(),
                        Icon = loadSeverityIcon?.Invoke(g.Key)
                    });
                foreach (var sc in severityCounts)
                    fileNode.SeverityCounts.Add(sc);

                fileNodes.Add(fileNode);
            }

            return fileNodes;
        }
    }
}
