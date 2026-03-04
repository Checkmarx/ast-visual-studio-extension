using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Builds the Findings window tree (FileNode → VulnerabilityNode) from any list of vulnerabilities.
    /// Used for both mock data and real-time scanner results. Applies reference-style grouping
    /// (IaC/ASCA by line, OSS/Secrets/Containers one per finding) and severity badges.
    /// </summary>
    public static class FindingsTreeBuilder
    {
        /// <summary>Fallback file path when a vulnerability has no FilePath (e.g. unsaved document).</summary>
        public const string DefaultFilePath = "Program.cs";

        /// <summary>
        /// Converts a list of vulnerabilities into the tree model for the Findings tab.
        /// </summary>
        /// <param name="vulnerabilities">Findings from mock or real-time (e.g. GetCommonVulnerabilities or coordinator).</param>
        /// <param name="loadSeverityIcon">Callback to get severity icon. Can be null.</param>
        /// <param name="loadFileIcon">Callback to get file-type icon by file path (e.g. for VS built-in icons). Can be null.</param>
        /// <param name="defaultFilePath">Used when vulnerability.FilePath is null/empty. Defaults to DefaultFilePath.</param>
        public static ObservableCollection<FileNode> BuildFileNodesFromVulnerabilities(
            List<Vulnerability> vulnerabilities,
            Func<string, ImageSource> loadSeverityIcon = null,
            Func<string, ImageSource> loadFileIcon = null,
            string defaultFilePath = null)
        {
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return new ObservableCollection<FileNode>();

            var fallbackPath = string.IsNullOrEmpty(defaultFilePath) ? DefaultFilePath : defaultFilePath;

            // Success (Ok) and Unknown: gutter icon only; do not show in problem window
            var issuesOnly = vulnerabilities
                .Where(v => v.Severity != SeverityLevel.Ok && v.Severity != SeverityLevel.Unknown)
                .ToList();
            if (issuesOnly.Count == 0)
                return new ObservableCollection<FileNode>();

            var grouped = issuesOnly
                .GroupBy(v => string.IsNullOrEmpty(v.FilePath) ? fallbackPath : v.FilePath)
                .OrderBy(g => g.Key);

            var fileNodes = new ObservableCollection<FileNode>();

            foreach (var group in grouped)
            {
                var filePath = group.Key;
                var fileName = Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(fileName)) fileName = filePath;

                var fileIcon = loadFileIcon?.Invoke(filePath);
                var fileNode = new FileNode
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileIcon = fileIcon
                };

                var fileVulns = group.ToList();
                var iacVulns = fileVulns.Where(v => v.Scanner == ScannerType.IaC).ToList();
                var ascaVulns = fileVulns.Where(v => v.Scanner == ScannerType.ASCA).ToList();
                var ossVulns = fileVulns.Where(v => v.Scanner == ScannerType.OSS).ToList();
                var secretsVulns = fileVulns.Where(v => v.Scanner == ScannerType.Secrets).ToList();
                var containersVulns = fileVulns.Where(v => v.Scanner == ScannerType.Containers).ToList();

                var nodesToAdd = new List<VulnerabilityNode>();

                // IaC: group by line; multiple issues on same line → one row "N IAC issues detected on this line" (reference-style).
                // IaC/KICS uses 1-based line numbers; use as-is for display and navigation.
                foreach (var lineGroup in iacVulns.GroupBy(v => v.LineNumber))
                {
                    var list = lineGroup.ToList();
                    var first = list[0];
                    int line1Based = CxAssistConstants.To1BasedLineForDte(ScannerType.IaC, first.LineNumber);
                    if (list.Count > 1)
                    {
                        nodesToAdd.Add(new VulnerabilityNode
                        {
                            Severity = first.Severity.ToString(),
                            SeverityIcon = loadSeverityIcon?.Invoke(first.Severity.ToString()),
                            Description = list.Count + CxAssistConstants.MultipleIacIssuesOnLine,
                            Line = line1Based,
                            Column = first.ColumnNumber,
                            FilePath = first.FilePath,
                            Scanner = ScannerType.IaC
                        });
                    }
                    else
                    {
                        nodesToAdd.Add(new VulnerabilityNode
                        {
                            Severity = first.Severity.ToString(),
                            SeverityIcon = loadSeverityIcon?.Invoke(first.Severity.ToString()),
                            Description = first.Title ?? first.Description,
                            Line = line1Based,
                            Column = first.ColumnNumber,
                            FilePath = first.FilePath,
                            Scanner = ScannerType.IaC
                        });
                    }
                }

                // ASCA: group by line; multiple on same line → show highest-severity detail only (not "N ASCA violations...")
                foreach (var lineGroup in ascaVulns.GroupBy(v => v.LineNumber))
                {
                    var list = lineGroup.ToList();
                    var v = list.Count > 1 ? list.OrderBy(x => x.Severity).First() : list[0];
                    nodesToAdd.Add(new VulnerabilityNode
                    {
                        Severity = v.Severity.ToString(),
                        SeverityIcon = loadSeverityIcon?.Invoke(v.Severity.ToString()),
                        Description = v.Title ?? v.Description,
                        Line = v.LineNumber + 1,
                        Column = v.ColumnNumber,
                        FilePath = v.FilePath,
                        Scanner = ScannerType.ASCA
                    });
                }

                // OSS: group by line; multiple on same line → show highest-severity detail only (not "N OSS issues...")
                foreach (var lineGroup in ossVulns.GroupBy(v => v.LineNumber))
                {
                    var list = lineGroup.ToList();
                    var v = list.Count > 1 ? list.OrderBy(x => x.Severity).First() : list[0];
                    nodesToAdd.Add(new VulnerabilityNode
                    {
                        Severity = v.Severity.ToString(),
                        SeverityIcon = loadSeverityIcon?.Invoke(v.Severity.ToString()),
                        Description = v.Title ?? v.Description,
                        PackageName = v.PackageName,
                        PackageVersion = v.PackageVersion,
                        Line = v.LineNumber + 1,
                        Column = v.ColumnNumber,
                        FilePath = v.FilePath,
                        Scanner = ScannerType.OSS
                    });
                }

                // Secrets: group by line; multiple on same line → show highest-severity detail only
                foreach (var lineGroup in secretsVulns.GroupBy(v => v.LineNumber))
                {
                    var list = lineGroup.ToList();
                    var v = list.Count > 1 ? list.OrderBy(x => x.Severity).First() : list[0];
                    nodesToAdd.Add(new VulnerabilityNode
                    {
                        Severity = v.Severity.ToString(),
                        SeverityIcon = loadSeverityIcon?.Invoke(v.Severity.ToString()),
                        Description = v.Title ?? v.Description,
                        Line = v.LineNumber + 1,
                        Column = v.ColumnNumber,
                        FilePath = v.FilePath,
                        Scanner = ScannerType.Secrets
                    });
                }

                // Containers: group by line; multiple on same line → show highest-severity detail only
                foreach (var lineGroup in containersVulns.GroupBy(v => v.LineNumber))
                {
                    var list = lineGroup.ToList();
                    var v = list.Count > 1 ? list.OrderBy(x => x.Severity).First() : list[0];
                    nodesToAdd.Add(new VulnerabilityNode
                    {
                        Severity = v.Severity.ToString(),
                        SeverityIcon = loadSeverityIcon?.Invoke(v.Severity.ToString()),
                        Description = v.Title ?? v.Description,
                        Line = v.LineNumber + 1,
                        Column = v.ColumnNumber,
                        FilePath = v.FilePath,
                        Scanner = ScannerType.Containers
                    });
                }

                // Sort by line then column (reference order)
                foreach (var n in nodesToAdd.OrderBy(n => n.Line).ThenBy(n => n.Column))
                    fileNode.Vulnerabilities.Add(n);

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
