using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Task;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac
{
    /// <summary>
    /// UI manager for IaC scanner results.
    /// Displays configuration issues as wave underlines and error list entries.
    /// </summary>
    public class IacUIManager : BaseRealtimeScannerUIManager
    {
        /// <summary>
        /// Displays IaC issues as markers and error list entries.
        /// </summary>
        public async Task DisplayDiagnosticsAsync(List<IacIssue> issues, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await ClearAllMarkersAndTasksAsync();

                if (issues == null || issues.Count == 0) return;

                var buffer = GetActiveBuffer();
                if (buffer == null) return;

                foreach (var issue in issues)
                {
                    if (issue.Locations == null) continue;

                    foreach (var location in issue.Locations)
                    {
                        // Add marker for the IaC issue
                        AddMarker(buffer, location.Line - 1, location.StartIndex, location.EndIndex,
                                  new IacMarkerClient(issue), issue.Severity);

                        // Add to error list
                        var task = new ErrorTask
                        {
                            Text = $"{issue.Title} - Expected: {issue.ExpectedValue}, Actual: {issue.ActualValue}",
                            Line = location.Line - 1,
                            Column = location.StartIndex,
                            Category = GetErrorCategory(issue.Severity),
                            ErrorCategory = TaskErrorCategory.CodeSense,
                            Document = filePath,
                            Priority = TaskPriority.Normal
                        };
                        _errorListProvider.Tasks.Add(task);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying IaC diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Marker client for IaC issues with tooltip.
        /// </summary>
        private class IacMarkerClient : IVsTextMarkerClient
        {
            private readonly IacIssue _issue;

            public IacMarkerClient(IacIssue issue)
            {
                _issue = issue;
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                pbstrText[0] = $"{_issue.Title} - Expected: {_issue.ExpectedValue}, Actual: {_issue.ActualValue}\t(IaC)";
                return VSConstants.S_OK;
            }

            public int ExecMarkerCommand(IVsTextMarker pMarker, MSGLCID langID, uint iItem, string pszCommand)
            {
                return VSConstants.S_OK;
            }

            public int QueryMarkerCommand(IVsTextMarker pMarker, uint iItem, out OLECMD pCmd)
            {
                pCmd = new OLECMD();
                return VSConstants.S_OK;
            }

            public int OnBeforeSpanResize(IVsTextMarker pMarker, TextSpan[] ptsNewSpan)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterSpanResize(IVsTextMarker pMarker)
            {
                return VSConstants.S_OK;
            }

            public int OnMarkerInvalidated()
            {
                return VSConstants.S_OK;
            }

            public int NotifyMarkerChanged(IVsTextMarker pMarker)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
