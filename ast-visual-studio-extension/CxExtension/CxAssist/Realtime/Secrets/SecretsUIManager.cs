using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets
{
    /// <summary>
    /// UI manager for Secrets scanner results.
    /// Displays secrets as wave underlines and error list entries.
    /// </summary>
    public class SecretsUIManager : BaseRealtimeScannerUIManager
    {
        /// <summary>
        /// Displays discovered secrets as markers and error list entries.
        /// </summary>
        public async Task DisplayDiagnosticsAsync(List<Secret> secrets, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await ClearAllMarkersAndTasksAsync();

                if (secrets == null || secrets.Count == 0) return;

                var buffer = GetActiveBuffer();
                if (buffer == null) return;

                foreach (var secret in secrets)
                {
                    if (secret.Locations == null) continue;

                    foreach (var location in secret.Locations)
                    {
                        // Add marker for the secret location
                        AddMarker(buffer, location.Line - 1, location.StartIndex, location.EndIndex,
                                  new SecretsMarkerClient(secret), secret.Severity);

                        // Add to error list
                        var task = new ErrorTask
                        {
                            Text = $"SECRET: {secret.Title} - {secret.Description}",
                            Line = location.Line - 1,
                            Column = location.StartIndex,
                            Category = GetErrorCategory(secret.Severity),
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
                Debug.WriteLine($"Error displaying Secrets diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Marker client for Secrets with tooltip.
        /// </summary>
        private class SecretsMarkerClient : IVsTextMarkerClient
        {
            private readonly Secret _secret;

            public SecretsMarkerClient(Secret secret)
            {
                _secret = secret;
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                pbstrText[0] = $"SECRET: {_secret.Title} - {_secret.Description}\t(Secrets)";
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
