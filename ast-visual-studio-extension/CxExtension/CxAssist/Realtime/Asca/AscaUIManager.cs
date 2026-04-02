using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca
{
    /// <summary>
    /// UI manager for ASCA scanner results.
    /// Displays violations as wave underlines and error list entries.
    /// </summary>
    public class AscaUIManager : BaseRealtimeScannerUIManager
    {
        /// <summary>
        /// Displays ASCA violations as markers and error list entries.
        /// </summary>
        public async Task DisplayDiagnosticsAsync(List<CxAscaDetail> violations, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await ClearAllMarkersAndTasksAsync();

                if (violations == null || violations.Count == 0) return;

                var buffer = GetActiveBuffer();
                if (buffer == null) return;

                foreach (var violation in violations)
                {
                    // Add marker for the violation
                    var startIndex = ComputeStartIndex(violation.ProblematicLine);
                    AddMarker(buffer, violation.Line - 1, startIndex, startIndex + 10,
                              new AscaMarkerClient(violation), violation.Severity);

                    // Add to error list
                    var task = new ErrorTask
                    {
                        Text = $"{violation.RuleName}: {violation.RemediationAdvise}",
                        Line = violation.Line - 1,
                        Column = 0,
                        Category = GetErrorCategory(violation.Severity),
                        ErrorCategory = TaskErrorCategory.CodeSense,
                        Document = filePath,
                        Priority = TaskPriority.Normal
                    };
                    _errorListProvider.Tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying ASCA diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Computes the start index for the marker from the problematic line.
        /// </summary>
        private int ComputeStartIndex(string problematicLine)
        {
            if (string.IsNullOrEmpty(problematicLine)) return 0;
            var trimmed = problematicLine.TrimStart();
            return problematicLine.Length - trimmed.Length;
        }

        /// <summary>
        /// Marker client for ASCA violations with tooltip.
        /// </summary>
        private class AscaMarkerClient : IVsTextMarkerClient
        {
            private readonly CxAscaDetail _violation;

            public AscaMarkerClient(CxAscaDetail violation)
            {
                _violation = violation;
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                pbstrText[0] = $"{_violation.RuleName} - {_violation.RemediationAdvise}\t(ASCA)";
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
