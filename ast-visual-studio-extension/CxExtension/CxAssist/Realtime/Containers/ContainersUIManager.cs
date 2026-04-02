using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Task;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers
{
    /// <summary>
    /// UI manager for Containers scanner results.
    /// Displays container image vulnerabilities as wave underlines and error list entries.
    /// </summary>
    public class ContainersUIManager : BaseRealtimeScannerUIManager
    {
        /// <summary>
        /// Displays container image vulnerabilities as markers and error list entries.
        /// </summary>
        public async Task DisplayDiagnosticsAsync(List<ContainersRealtimeImage> images, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await ClearAllMarkersAndTasksAsync();

                if (images == null || images.Count == 0) return;

                var buffer = GetActiveBuffer();
                if (buffer == null) return;

                foreach (var image in images)
                {
                    if (image.Vulnerabilities == null || image.Vulnerabilities.Count == 0) continue;

                    var highestSeverity = GetHighestSeverity(image.Vulnerabilities);

                    if (image.Locations == null) continue;

                    foreach (var location in image.Locations)
                    {
                        // Add marker for the image (one marker per image, not per CVE)
                        AddMarker(buffer, location.Line - 1, location.StartIndex, location.EndIndex,
                                  new ContainersMarkerClient(image), highestSeverity);

                        // Add to error list
                        var cveList = string.Join(", ", image.Vulnerabilities.Take(3).Select(v => v.Cve));
                        var task = new ErrorTask
                        {
                            Text = $"{image.ImageName}:{image.ImageTag} - {image.Vulnerabilities.Count} vulnerabilities: {cveList}",
                            Line = location.Line - 1,
                            Column = location.StartIndex,
                            Category = GetErrorCategory(highestSeverity),
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
                Debug.WriteLine($"Error displaying Containers diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the highest severity from a list of vulnerabilities.
        /// </summary>
        private string GetHighestSeverity(List<ContainersRealtimeVulnerability> vulnerabilities)
        {
            var severityMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "critical", 4 },
                { "high", 3 },
                { "medium", 2 },
                { "low", 1 }
            };

            var highest = vulnerabilities
                .Select(v => v.Severity?.ToLowerInvariant() ?? "low")
                .OrderByDescending(s => severityMap.ContainsKey(s) ? severityMap[s] : 0)
                .FirstOrDefault();

            return highest ?? "low";
        }

        /// <summary>
        /// Marker client for container images with tooltip.
        /// </summary>
        private class ContainersMarkerClient : IVsTextMarkerClient
        {
            private readonly ContainersRealtimeImage _image;

            public ContainersMarkerClient(ContainersRealtimeImage image)
            {
                _image = image;
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                var firstCve = _image.Vulnerabilities?.FirstOrDefault()?.Cve ?? "UNKNOWN";
                pbstrText[0] = $"{_image.ImageName}:{_image.ImageTag} - {firstCve}\t(Containers)";
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
