using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core
{
    /// <summary>
    /// Syncs DevAssist findings to the built-in Error List so issues appear in both
    /// the custom DevAssist findings window and the VS Error List.
    /// </summary>
    internal sealed class DevAssistErrorListSync
    {
        private ErrorListProvider _errorListProvider;
        private bool _subscribed;

        public void Start()
        {
            if (_subscribed) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureErrorListProvider();
            DevAssistDisplayCoordinator.IssuesUpdated += OnIssuesUpdated;
            _subscribed = true;

            // Initial sync from current state
            var snapshot = DevAssistDisplayCoordinator.GetAllIssuesByFile();
            if (snapshot != null && snapshot.Count > 0)
                SyncToErrorList(snapshot);
        }

        public void Stop()
        {
            if (!_subscribed) return;

            DevAssistDisplayCoordinator.IssuesUpdated -= OnIssuesUpdated;
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
                ProviderName = "DevAssist"
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

                foreach (var v in list)
                {
                    string severityLabel = v.Severity.ToString();
                    var task = new ErrorTask
                    {
                        Category = TaskCategory.CodeSense,
                        ErrorCategory = GetErrorCategory(v.Severity),
                        Text = $"[DevAssist] [{severityLabel}] {v.Title}",
                        Document = filePath,
                        Line = Math.Max(0, v.LineNumber - 1),
                        Column = Math.Max(0, v.ColumnNumber),
                        HierarchyItem = document != null ? GetHierarchyItem(document) : null
                    };

                    task.Navigate += (s, e) => NavigateToVulnerability(v);
                    _errorListProvider.Tasks.Add(task);
                }
            }
        }

        private static TaskErrorCategory GetErrorCategory(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious:
                case SeverityLevel.Critical:
                case SeverityLevel.High:
                    return TaskErrorCategory.Error;
                case SeverityLevel.Medium:
                    return TaskErrorCategory.Warning;
                case SeverityLevel.Low:
                case SeverityLevel.Info:
                case SeverityLevel.Unknown:
                case SeverityLevel.Ok:
                case SeverityLevel.Ignored:
                default:
                    return TaskErrorCategory.Message;
            }
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

        private static void NavigateToVulnerability(Vulnerability v)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(v?.FilePath)) return;

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte == null) return;

            try
            {
                var window = dte.ItemOperations.OpenFile(v.FilePath, EnvDTE.Constants.vsViewKindCode);
                Document doc = window.Document;
                if (doc?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    int line = Math.Max(1, v.LineNumber);
                    selection.MoveToLineAndOffset(line, 1);
                    selection.SelectLine();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssistErrorListSync: Navigate failed: {ex.Message}");
            }
        }
    }
}
