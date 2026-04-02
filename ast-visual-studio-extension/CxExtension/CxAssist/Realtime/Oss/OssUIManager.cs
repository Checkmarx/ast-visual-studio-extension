using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Task;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss
{
    /// <summary>
    /// UI manager for OSS/SCA scanner results.
    /// Displays vulnerable packages as wave underlines and error list entries.
    /// Groups all CVEs for a package under a single entry.
    /// </summary>
    public class OssUIManager : BaseRealtimeScannerUIManager
    {
        /// <summary>
        /// Displays vulnerable packages as markers and error list entries.
        /// One marker per package (not per CVE).
        /// </summary>
        public async Task DisplayDiagnosticsAsync(List<OssRealtimeScanPackage> packages, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await ClearAllMarkersAndTasksAsync();

                if (packages == null || packages.Count == 0) return;

                var buffer = GetActiveBuffer();
                if (buffer == null) return;

                foreach (var package in packages)
                {
                    if (package.Vulnerabilities == null || package.Vulnerabilities.Count == 0) continue;

                    var highestSeverity = GetHighestSeverity(package.Vulnerabilities);

                    if (package.Locations == null) continue;

                    foreach (var location in package.Locations)
                    {
                        // Add marker for the package (one marker per package, not per CVE)
                        AddMarker(buffer, location.Line - 1, location.StartIndex, location.EndIndex,
                                  new OssMarkerClient(package), highestSeverity);

                        // Add to error list
                        var firstCve = package.Vulnerabilities.FirstOrDefault()?.Cve ?? "UNKNOWN";
                        var task = new ErrorTask
                        {
                            Text = $"{package.PackageName}@{package.PackageVersion} - {package.Vulnerabilities.Count} vulnerabilities: {firstCve}",
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
                Debug.WriteLine($"Error displaying OSS diagnostics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the highest severity from a list of vulnerabilities.
        /// Malicious packages are treated as Critical.
        /// </summary>
        private string GetHighestSeverity(List<OssRealtimeVulnerability> vulnerabilities)
        {
            var severityMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "malicious", 5 },
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
        /// Marker client for vulnerable packages with tooltip.
        /// </summary>
        private class OssMarkerClient : IVsTextMarkerClient
        {
            private readonly OssRealtimeScanPackage _package;

            public OssMarkerClient(OssRealtimeScanPackage package)
            {
                _package = package;
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                var firstCve = _package.Vulnerabilities?.FirstOrDefault()?.Cve ?? "UNKNOWN";
                var severity = _package.Vulnerabilities?.FirstOrDefault()?.Severity ?? "unknown";
                pbstrText[0] = $"{_package.PackageName}@{_package.PackageVersion} - {firstCve} [{severity}]\t(OSS/SCA)";
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
