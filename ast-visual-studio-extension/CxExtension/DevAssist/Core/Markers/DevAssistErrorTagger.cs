using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Tagger that provides error tags for DevAssist vulnerabilities
    /// Based on JetBrains MarkupModel.addRangeHighlighter pattern
    /// Creates severity-based colored squiggly underlines in the text editor
    /// Similar to JetBrains EffectType.WAVE_UNDERSCORE implementation
    /// </summary>
    internal class DevAssistErrorTagger : ITagger<DevAssistErrorTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Dictionary<int, List<Vulnerability>> _vulnerabilitiesByLine;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public DevAssistErrorTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _vulnerabilitiesByLine = new Dictionary<int, List<Vulnerability>>();
        }

        public IEnumerable<ITagSpan<DevAssistErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: GetTags called - spans count: {spans.Count}, vulnerabilities count: {_vulnerabilitiesByLine.Count}");

            if (spans.Count == 0 || _vulnerabilitiesByLine.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Markers: GetTags returning early - no spans or vulnerabilities");
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            int tagCount = 0;

            foreach (var span in spans)
            {
                var startLine = snapshot.GetLineNumberFromPosition(span.Start);
                var endLine = snapshot.GetLineNumberFromPosition(span.End);

                for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
                {
                    if (_vulnerabilitiesByLine.TryGetValue(lineNumber, out var vulnerabilities))
                    {
                        // Create error tags ONLY for actual severity vulnerabilities (not Unknown, Ok, Ignored)
                        // Similar to JetBrains plugin which shows underlines only for real issues
                        // Each vulnerability gets its own squiggly underline
                        foreach (var vulnerability in vulnerabilities)
                        {
                            // Filter: Show underlines ONLY for Malicious, Critical, High, Medium, Low
                            // Do NOT show underlines for Unknown, Ok, Ignored (they only get gutter icons)
                            if (!ShouldShowUnderline(vulnerability.Severity))
                            {
                                System.Diagnostics.Debug.WriteLine($"DevAssist Markers: Skipping underline for {vulnerability.Severity} on line {lineNumber}");
                                continue;
                            }

                            var line = snapshot.GetLineFromLineNumber(lineNumber);

                            // Create span for the entire line (similar to JetBrains TextRange)
                            // In the future, this could be refined to highlight specific code ranges
                            var lineSpan = new SnapshotSpan(snapshot, line.Start, line.Length);

                            var tooltipText = BuildTooltipText(vulnerability);
                            var tag = new DevAssistErrorTag(
                                vulnerability.Severity.ToString(),
                                tooltipText,
                                vulnerability.Id
                            );

                            tagCount++;
                            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: Creating error tag #{tagCount} for line {lineNumber}, severity: {vulnerability.Severity}");
                            yield return new TagSpan<DevAssistErrorTag>(lineSpan, tag);
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: GetTags completed - returned {tagCount} error tags");
        }

        /// <summary>
        /// Determines if a severity level should show an underline
        /// Similar to JetBrains plugin: only show underlines for actual issues (Malicious, Critical, High, Medium, Low)
        /// Do NOT show underlines for Unknown, Ok, Ignored (they only get gutter icons)
        /// </summary>
        private bool ShouldShowUnderline(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious:
                case SeverityLevel.Critical:
                case SeverityLevel.High:
                case SeverityLevel.Medium:
                case SeverityLevel.Low:
                case SeverityLevel.Info: // Info maps to Low
                    return true;

                case SeverityLevel.Unknown:
                case SeverityLevel.Ok:
                case SeverityLevel.Ignored:
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Builds tooltip text for a vulnerability
        /// Similar to JetBrains GutterIconRenderer.getTooltipText()
        /// </summary>
        private string BuildTooltipText(Vulnerability vulnerability)
        {
            return $"[{vulnerability.Severity}] {vulnerability.Description}\nID: {vulnerability.Id}";
        }

        /// <summary>
        /// Updates the vulnerabilities and triggers a refresh of error tags
        /// Similar to JetBrains MarkupModel.removeAllHighlighters() + addRangeHighlighter()
        /// </summary>
        public void UpdateVulnerabilities(List<Vulnerability> vulnerabilities)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: UpdateVulnerabilities called with {vulnerabilities?.Count ?? 0} vulnerabilities");

            _vulnerabilitiesByLine.Clear();

            if (vulnerabilities != null)
            {
                foreach (var vulnerability in vulnerabilities)
                {
                    // Convert 1-based line number to 0-based for Visual Studio
                    int lineNumber = vulnerability.LineNumber - 1;

                    if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                    {
                        _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                    }

                    _vulnerabilitiesByLine[lineNumber].Add(vulnerability);
                    System.Diagnostics.Debug.WriteLine($"DevAssist Markers: Added vulnerability {vulnerability.Id} to line {lineNumber}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: Vulnerabilities stored in {_vulnerabilitiesByLine.Count} lines");

            // Notify that tags have changed
            var snapshot = _buffer.CurrentSnapshot;
            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: Raising TagsChanged event for span: {entireSpan.Start} to {entireSpan.End}");
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: TagsChanged event raised");
        }

        /// <summary>
        /// Clears all vulnerabilities and error tags
        /// Similar to JetBrains MarkupModel.removeAllHighlighters()
        /// </summary>
        public void ClearVulnerabilities()
        {
            System.Diagnostics.Debug.WriteLine("DevAssist Markers: ClearVulnerabilities called");
            UpdateVulnerabilities(null);
        }
    }
}

