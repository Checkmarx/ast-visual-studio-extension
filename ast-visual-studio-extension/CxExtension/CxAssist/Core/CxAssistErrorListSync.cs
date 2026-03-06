using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Syncs CxAssist findings to the built-in Error List so issues appear in both
    /// the custom CxAssist findings window and the VS Error List.
    /// </summary>
    internal sealed class CxAssistErrorListSync
    {
        /// <summary>Prefix stored in ErrorTask.HelpKeyword so we can identify CxAssist tasks and recover vulnerability Id.</summary>
        public const string HelpKeywordPrefix = "CxAssist:";

        private ErrorListProvider _errorListProvider;
        private bool _subscribed;

        public void Start()
        {
            if (_subscribed) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureErrorListProvider();
            CxAssistDisplayCoordinator.IssuesUpdated += OnIssuesUpdated;
            _subscribed = true;

            // Initial sync from current state
            var snapshot = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            if (snapshot != null && snapshot.Count > 0)
                SyncToErrorList(snapshot);
        }

        public void Stop()
        {
            if (!_subscribed) return;

            CxAssistDisplayCoordinator.IssuesUpdated -= OnIssuesUpdated;
            _subscribed = false;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _errorListProvider?.Tasks.Clear();
            });
        }

        private void OnIssuesUpdated(IReadOnlyDictionary<string, List<Vulnerability>> snapshot)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                SyncToErrorList(snapshot);
            });
        }

        private void EnsureErrorListProvider()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_errorListProvider != null) return;

            _errorListProvider = new ErrorListProvider(ServiceProvider.GlobalProvider)
            {
                ProviderName = CxAssistConstants.DisplayName
            };
        }

        private void SyncToErrorList(IReadOnlyDictionary<string, List<Vulnerability>> issuesByFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureErrorListProvider();

            _errorListProvider.Tasks.Clear();

            if (issuesByFile == null || issuesByFile.Count == 0)
                return;

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte?.Documents == null) return;

            foreach (var kv in issuesByFile)
            {
                string filePath = kv.Key;
                var list = kv.Value;
                if (list == null) continue;

                Document document = null;
                try
                {
                    document = dte.Documents.Cast<Document>().FirstOrDefault(doc =>
                        string.Equals(doc.FullName, filePath, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    // Document may not be open
                }

                // Build entries like the Findings tree: same-line grouping for IaC and ASCA (one row per line when 2+ issues)
                var entries = BuildErrorListEntries(list);
                string docPath = GetDocumentPath(list.Count > 0 ? list[0].FilePath : null, filePath);

                foreach (var entry in entries)
                {
                    var v = entry.Vulnerability;
                    // Same description format as Findings tab: PrimaryDisplayText + " Checkmarx One Assist [Ln X, Col Y]"
                    int displayLine = entry.Line + 1; // 1-based for description text to match Findings
                    string fullDescription = $"{entry.DisplayText} {CxAssistConstants.DisplayName} [Ln {displayLine}, Col {entry.Column}]";
                    var task = new ErrorTask
                    {
                        Category = TaskCategory.BuildCompile,
                        ErrorCategory = GetErrorCategory(v.Severity),
                        Text = fullDescription,
                        Document = docPath,
                        Line = entry.Line,
                        Column = Math.Max(1, entry.Column),
                        HierarchyItem = document != null ? GetHierarchyItem(document) : null,
                        HelpKeyword = HelpKeywordPrefix + v.Id
                    };

                    task.Navigate += (s, e) => NavigateToVulnerability(v);
                    _errorListProvider.Tasks.Add(task);
                }
            }
        }

        /// <summary>
        /// Builds Error List entries with same-line grouping as the Findings tree: all scanners
        /// show one entry per line when multiple issues share a line (e.g. "N OSS issues detected on this line").
        /// Vulnerability.LineNumber is 1-based. We convert to 0-based for Error List (ErrorTask.Line);
        /// VS displays that as 1-based in the UI, so the column matches "[Ln X, Col Y]" in the Findings tab.
        /// </summary>
        private static List<(string DisplayText, int Line, int Column, Vulnerability Vulnerability)> BuildErrorListEntries(List<Vulnerability> list)
        {
            var result = new List<(string, int, int, Vulnerability)>();
            var issuesOnly = list.Where(v => v.Severity != SeverityLevel.Ok && v.Severity != SeverityLevel.Unknown).ToList();

            // Error List expects 0-based line (VS shows 1-based in UI). Convert 1-based LineNumber to 0-based.
            int LineForErrorList(ScannerType scanner, int line1Based) => CxAssistConstants.To0BasedLineForEditor(scanner, line1Based);
            int ColForErrorList(int c) => Math.Max(1, c);

            // IaC: group by line (same as Findings tree).
            foreach (var lineGroup in issuesOnly.Where(v => v.Scanner == ScannerType.IaC).GroupBy(v => v.LineNumber))
            {
                var lineList = lineGroup.ToList();
                var first = lineList[0];
                int line0Based = LineForErrorList(ScannerType.IaC, first.LineNumber);
                if (lineList.Count > 1)
                    result.Add((lineList.Count + CxAssistConstants.MultipleIacIssuesOnLine, line0Based, ColForErrorList(first.ColumnNumber), first));
                else
                    result.Add((GetPrimaryDisplayText(first.Severity, first.Scanner, first.Title ?? first.Description, first.PackageName, first.PackageVersion), line0Based, ColForErrorList(first.ColumnNumber), first));
            }

            // ASCA: group by line; multiple on same line → show highest-severity detail only (same as Findings)
            foreach (var lineGroup in issuesOnly.Where(v => v.Scanner == ScannerType.ASCA).GroupBy(v => v.LineNumber))
            {
                var lineList = lineGroup.ToList();
                var v = lineList.Count > 1 ? lineList.OrderBy(x => x.Severity).First() : lineList[0];
                result.Add((GetPrimaryDisplayText(v.Severity, v.Scanner, v.Title ?? v.Description, v.PackageName, v.PackageVersion), LineForErrorList(v.Scanner, v.LineNumber), ColForErrorList(v.ColumnNumber), v));
            }

            // OSS: group by line; multiple on same line → show highest-severity detail only (same as Findings)
            foreach (var lineGroup in issuesOnly.Where(v => v.Scanner == ScannerType.OSS).GroupBy(v => v.LineNumber))
            {
                var lineList = lineGroup.ToList();
                var v = lineList.Count > 1 ? lineList.OrderBy(x => x.Severity).First() : lineList[0];
                result.Add((GetPrimaryDisplayText(v.Severity, v.Scanner, v.Title ?? v.Description, v.PackageName, v.PackageVersion), LineForErrorList(v.Scanner, v.LineNumber), ColForErrorList(v.ColumnNumber), v));
            }

            // Secrets: group by line; multiple on same line → show highest-severity detail only
            foreach (var lineGroup in issuesOnly.Where(v => v.Scanner == ScannerType.Secrets).GroupBy(v => v.LineNumber))
            {
                var lineList = lineGroup.ToList();
                var v = lineList.Count > 1 ? lineList.OrderBy(x => x.Severity).First() : lineList[0];
                result.Add((GetPrimaryDisplayText(v.Severity, v.Scanner, v.Title ?? v.Description, v.PackageName, v.PackageVersion), LineForErrorList(v.Scanner, v.LineNumber), ColForErrorList(v.ColumnNumber), v));
            }

            // Containers: group by line; multiple on same line → show highest-severity detail only
            foreach (var lineGroup in issuesOnly.Where(v => v.Scanner == ScannerType.Containers).GroupBy(v => v.LineNumber))
            {
                var lineList = lineGroup.ToList();
                var v = lineList.Count > 1 ? lineList.OrderBy(x => x.Severity).First() : lineList[0];
                result.Add((GetPrimaryDisplayText(v.Severity, v.Scanner, v.Title ?? v.Description, v.PackageName, v.PackageVersion), LineForErrorList(v.Scanner, v.LineNumber), ColForErrorList(v.ColumnNumber), v));
            }

            return result;
        }

        /// <summary>
        /// Builds the same primary description text as the Findings tab (VulnerabilityNode.PrimaryDisplayText)
        /// so the Error List description column matches the Findings tree.
        /// </summary>
        private static string GetPrimaryDisplayText(SeverityLevel severity, ScannerType scanner, string titleOrDescription, string packageName, string packageVersion)
        {
            string title = titleOrDescription ?? "";
            if (title.Contains(" detected on this line") || title.Contains(" violations detected on this line"))
                return title.TrimEnd();
            string severityStr = severity.ToString();
            switch (scanner)
            {
                case ScannerType.OSS:
                    string name = !string.IsNullOrEmpty(title) ? title : (packageName ?? "");
                    name = CxAssistConstants.StripCveFromDisplayName(name);
                    string version = !string.IsNullOrEmpty(packageVersion) ? "@" + packageVersion : "";
                    return $"{severityStr}-risk package: {name}{version}";
                case ScannerType.Secrets:
                    return $"{severityStr}-risk secret: {title}";
                case ScannerType.Containers:
                    return $"{severityStr}-risk container image: {title}";
                case ScannerType.ASCA:
                case ScannerType.IaC:
                default:
                    return title + (string.IsNullOrEmpty(title) ? "" : " ");
            }
        }

        /// <summary>
        /// Returns a normalized full path for the Error List so VS shows the actual file name instead of "Document 1".
        /// </summary>
        private static string GetDocumentPath(string vulnerabilityFilePath, string fallbackFilePath)
        {
            string path = !string.IsNullOrEmpty(vulnerabilityFilePath) ? vulnerabilityFilePath : fallbackFilePath;
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        /// Use Error for all findings so the Error List draws only red underlines. Otherwise
        /// Warning (green) and Message (blue) on the same line can override red and make severity unclear.
        /// Severity is still shown in the task Text (e.g. [High], [Medium]).
        /// </summary>
        private static TaskErrorCategory GetErrorCategory(SeverityLevel severity)
        {
            return TaskErrorCategory.Error;
        }

        private static IVsHierarchy GetHierarchyItem(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (document?.ProjectItem?.ContainingProject == null) return null;

            var serviceProvider = ServiceProvider.GlobalProvider;
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution == null) return null;

            solution.GetProjectOfUniqueName(document.ProjectItem.ContainingProject.UniqueName, out IVsHierarchy hierarchy);
            return hierarchy;
        }

        /// <summary>Called when user navigates from Error List task or from Error List context menu.</summary>
        internal static void NavigateToVulnerability(Vulnerability v)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(v?.FilePath)) return;

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte == null) return;

            try
            {
                Window window = null;
                string pathToTry = v.FilePath;

                window = dte.ItemOperations.OpenFile(pathToTry, EnvDTE.Constants.vsViewKindCode);
                if (window == null && !Path.IsPathRooted(pathToTry))
                {
                    try
                    {
                        pathToTry = Path.GetFullPath(pathToTry);
                        window = dte.ItemOperations.OpenFile(pathToTry, EnvDTE.Constants.vsViewKindCode);
                    }
                    catch { /* ignore */ }
                }
                if (window == null && dte.Solution != null)
                {
                    try
                    {
                        string solDir = Path.GetDirectoryName(dte.Solution.FullName);
                        if (!string.IsNullOrEmpty(solDir))
                        {
                            string pathInSolution = Path.Combine(solDir, Path.GetFileName(v.FilePath));
                            if (pathInSolution != pathToTry)
                                window = dte.ItemOperations.OpenFile(pathInSolution, EnvDTE.Constants.vsViewKindCode);
                        }
                    }
                    catch { /* ignore */ }
                }
                if (window == null && dte.Documents != null)
                {
                    string fileName = Path.GetFileName(pathToTry);
                    Document doc = dte.Documents.Cast<Document>().FirstOrDefault(d =>
                        string.Equals(d.FullName, pathToTry, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetFileName(d.FullName), fileName, StringComparison.OrdinalIgnoreCase));
                    if (doc != null)
                        window = doc.ActiveWindow;
                }

                if (window?.Document?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    int line = CxAssistConstants.To1BasedLineForDte(v.Scanner, v.LineNumber);
                    selection.MoveToLineAndOffset(line, Math.Max(1, v.ColumnNumber));
                    selection.SelectLine();
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "ErrorListSync.NavigateToVulnerability");
            }
        }
    }
}
