using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.GutterIcons
{
    /// <summary>
    /// Tagger that provides glyph tags for DevAssist vulnerabilities
    /// Based on JetBrains MarkupModel.addRangeHighlighter pattern
    /// Manages the lifecycle of gutter icons in the text view
    /// </summary>
    internal class DevAssistGlyphTagger : ITagger<DevAssistGlyphTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Dictionary<int, List<Vulnerability>> _vulnerabilitiesByLine;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public DevAssistGlyphTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _vulnerabilitiesByLine = new Dictionary<int, List<Vulnerability>>();
        }

        public IEnumerable<ITagSpan<DevAssistGlyphTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist: GetTags called - spans count: {spans.Count}, vulnerabilities count: {_vulnerabilitiesByLine.Count}");

            if (spans.Count == 0 || _vulnerabilitiesByLine.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: GetTags returning early - no spans or vulnerabilities");
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
                        // Get the most severe vulnerability for this line (for gutter icon)
                        var mostSevere = GetMostSevereVulnerability(vulnerabilities);
                        if (mostSevere != null)
                        {
                            var line = snapshot.GetLineFromLineNumber(lineNumber);
                            var lineSpan = new SnapshotSpan(snapshot, line.Start, line.Length);

                            var tooltipText = BuildTooltipText(vulnerabilities);
                            var tag = new DevAssistGlyphTag(
                                mostSevere.Severity.ToString(),
                                tooltipText,
                                mostSevere.Id
                            );

                            tagCount++;
                            System.Diagnostics.Debug.WriteLine($"DevAssist: Creating tag #{tagCount} for line {lineNumber}, severity: {mostSevere.Severity}");
                            yield return new TagSpan<DevAssistGlyphTag>(lineSpan, tag);
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"DevAssist: GetTags completed - returned {tagCount} tags");
        }

        /// <summary>
        /// Updates vulnerabilities for the buffer
        /// Based on JetBrains ProblemDecorator.decorateUI pattern
        /// </summary>
        public void UpdateVulnerabilities(List<Vulnerability> vulnerabilities)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist: UpdateVulnerabilities called with {vulnerabilities?.Count ?? 0} vulnerabilities");

            _vulnerabilitiesByLine.Clear();

            if (vulnerabilities != null)
            {
                foreach (var vuln in vulnerabilities)
                {
                    int lineNumber = vuln.LineNumber - 1; // Convert to 0-based

                    if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                    {
                        _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                    }

                    _vulnerabilitiesByLine[lineNumber].Add(vuln);
                }
            }

            System.Diagnostics.Debug.WriteLine($"DevAssist: Vulnerabilities stored in {_vulnerabilitiesByLine.Count} lines");
            System.Diagnostics.Debug.WriteLine($"DevAssist: TagsChanged event has {(TagsChanged != null ? TagsChanged.GetInvocationList().Length : 0)} subscribers");

            // Notify that tags have changed
            var snapshot = _buffer.CurrentSnapshot;
            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            System.Diagnostics.Debug.WriteLine($"DevAssist: Raising TagsChanged event for span: {entireSpan.Start} to {entireSpan.End}");
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
            System.Diagnostics.Debug.WriteLine($"DevAssist: TagsChanged event raised");
        }

        /// <summary>
        /// Clears all vulnerabilities
        /// Based on JetBrains ProblemDecorator.removeAllHighlighters pattern
        /// </summary>
        public void ClearVulnerabilities()
        {
            _vulnerabilitiesByLine.Clear();

            var snapshot = _buffer.CurrentSnapshot;
            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
        }

        /// <summary>
        /// Gets the most severe vulnerability from a list
        /// Based on JetBrains ProblemDecorator.getMostSeverity pattern
        /// </summary>
        private Vulnerability GetMostSevereVulnerability(List<Vulnerability> vulnerabilities)
        {
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return null;

            // Order by severity: Critical > High > Medium > Low > Info
            return vulnerabilities
                .OrderByDescending(v => GetSeverityPriority(v.Severity))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets severity priority for ordering (higher number = more severe)
        /// Based on JetBrains SeverityLevel precedence (inverted for descending order)
        /// </summary>
        private int GetSeverityPriority(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious: return 8;  // Highest priority
                case SeverityLevel.Critical: return 7;
                case SeverityLevel.High: return 6;
                case SeverityLevel.Medium: return 5;
                case SeverityLevel.Low: return 4;
                case SeverityLevel.Unknown: return 3;
                case SeverityLevel.Ok: return 2;
                case SeverityLevel.Ignored: return 1;
                case SeverityLevel.Info: return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// Builds tooltip text for multiple vulnerabilities on the same line
        /// Based on JetBrains GutterIconRenderer.getTooltipText pattern
        /// </summary>
        private string BuildTooltipText(List<Vulnerability> vulnerabilities)
        {
            if (vulnerabilities.Count == 1)
            {
                var vuln = vulnerabilities[0];
                return $"{vuln.Severity} - {vuln.Title}\n{vuln.Description}\n(DevAssist - {vuln.Scanner})";
            }
            else
            {
                return $"{vulnerabilities.Count} vulnerabilities on this line\n" +
                       string.Join("\n", vulnerabilities.Select(v => $"• {v.Severity}: {v.Title}"));
            }
        }
    }
}

