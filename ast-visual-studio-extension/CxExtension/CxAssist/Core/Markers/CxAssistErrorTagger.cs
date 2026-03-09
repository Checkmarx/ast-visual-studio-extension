using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Tagger that provides IErrorTag for CxAssist vulnerabilities.
    /// Uses VS built-in ErrorTag only; no custom tag. VS draws squiggles and shows tooltip.
    /// </summary>
    internal class CxAssistErrorTagger : ITagger<IErrorTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Dictionary<int, List<Vulnerability>> _vulnerabilitiesByLine;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CxAssistErrorTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _vulnerabilitiesByLine = new Dictionary<int, List<Vulnerability>>();
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var result = new List<ITagSpan<IErrorTag>>();

            if (spans == null || spans.Count == 0 || _vulnerabilitiesByLine.Count == 0)
                return result;

            ITextSnapshot snapshot = null;
            try
            {
                snapshot = spans[0].Snapshot;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "ErrorTagger.GetTags (snapshot)");
            }

            if (snapshot == null) return result;
            foreach (var span in spans)
            {
                try
                {
                    var startLine = snapshot.GetLineNumberFromPosition(span.Start);
                    var endLine = snapshot.GetLineNumberFromPosition(span.End);

                    for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
                    {
                        if (_vulnerabilitiesByLine.TryGetValue(lineNumber, out var vulnerabilities))
                        {
                            foreach (var vulnerability in vulnerabilities)
                            {
                                if (!ShouldShowUnderline(vulnerability.Severity))
                                    continue;

                                var line = snapshot.GetLineFromLineNumber(lineNumber);
                                SnapshotSpan underlineSpan = GetUnderlineSpan(snapshot, line, vulnerability);

                                var tooltipText = BuildTooltipText(vulnerability);
                                IErrorTag tag = new ErrorTag("Error", tooltipText);
                                result.Add(new TagSpan<IErrorTag>(underlineSpan, tag));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "ErrorTagger.GetTags (span)");
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the snapshot span for the underline. When Locations is set, use the range for this line from the matching location.
        /// Otherwise: on the first line use StartIndex/EndIndex when set; on continuation lines use full line (JetBrains: one range highlighter per location line).
        /// </summary>
        private static SnapshotSpan GetUnderlineSpan(ITextSnapshot snapshot, ITextSnapshotLine line, Vulnerability v)
        {
            int line0Based = line.LineNumber;
            int line1Based = line0Based + 1;

            // Per-line locations (e.g. pom.xml): use StartIndex/EndIndex for this line when present.
            if (v.Locations != null && v.Locations.Count > 0)
            {
                foreach (var loc in v.Locations)
                {
                    if (loc.Line != line1Based) continue;
                    if (loc.EndIndex > loc.StartIndex && loc.StartIndex >= 0)
                    {
                        int startOffset = Math.Min(loc.StartIndex, line.Length);
                        int length = Math.Min(loc.EndIndex - loc.StartIndex, line.Length - startOffset);
                        if (length > 0)
                        {
                            int startPos = line.Start + startOffset;
                            return new SnapshotSpan(snapshot, startPos, length);
                        }
                    }
                    return new SnapshotSpan(snapshot, line.Start, line.Length);
                }
                return new SnapshotSpan(snapshot, line.Start, line.Length);
            }

            // Fallback: single LineNumber/EndLineNumber with one StartIndex/EndIndex on first line.
            int firstLine0Based = CxAssistConstants.To0BasedLineForEditor(v.Scanner, v.LineNumber);
            bool isFirstLine = (line0Based == firstLine0Based);
            if (isFirstLine && v.EndIndex > v.StartIndex && v.StartIndex >= 0)
            {
                int startOffset = Math.Min(v.StartIndex, line.Length);
                int length = Math.Min(v.EndIndex - v.StartIndex, line.Length - startOffset);
                if (length > 0)
                {
                    int startPos = line.Start + startOffset;
                    return new SnapshotSpan(snapshot, startPos, length);
                }
            }
            return new SnapshotSpan(snapshot, line.Start, line.Length);
        }

        /// <summary>
        /// Determines if a severity level should show an underline (squiggle).
        /// Aligned with JetBrains ScanIssueProcessor: underline only when isProblem(severity) is true
        /// (not Ok, not Unknown, not Ignored). Gutter icons are shown for all severities.
        /// </summary>
        private static bool ShouldShowUnderline(SeverityLevel severity)
        {
            return CxAssistConstants.IsProblem(severity);
        }

        /// <summary>
        /// Builds tooltip text for ErrorTag. Use minimal text so the rich Quick Info (async source) is the single place
        /// for full content; avoids duplicate "Checkmarx One Assist" block from ErrorTag tooltip in the same popup.
        /// </summary>
        private static string BuildTooltipText(Vulnerability vulnerability)
        {
            return string.Empty;
        }

        /// <summary>
        /// Updates the vulnerabilities and triggers a refresh of error tags
        /// Similar to reference MarkupModel.removeAllHighlighters() + addRangeHighlighter()
        /// </summary>
        public void UpdateVulnerabilities(List<Vulnerability> vulnerabilities)
        {
            CxAssistErrorHandler.TryRun(() => UpdateVulnerabilitiesCore(vulnerabilities), "ErrorTagger.UpdateVulnerabilities");
        }

        private void UpdateVulnerabilitiesCore(List<Vulnerability> vulnerabilities)
        {
            _vulnerabilitiesByLine.Clear();

            var snapshot = _buffer.CurrentSnapshot;
            if (vulnerabilities != null)
            {
                int lineCount = snapshot.LineCount;
                foreach (var vulnerability in vulnerabilities)
                {
                    // Per-line locations (e.g. pom.xml): add this vulnerability to each line in Locations; LineNumber = first location for gutter.
                    if (vulnerability.Locations != null && vulnerability.Locations.Count > 0)
                    {
                        foreach (var loc in vulnerability.Locations)
                        {
                            if (!CxAssistConstants.IsLineInRange(loc.Line, lineCount))
                                continue;
                            int lineNumber = CxAssistConstants.To0BasedLineForEditor(vulnerability.Scanner, loc.Line);
                            if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                                _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                            _vulnerabilitiesByLine[lineNumber].Add(vulnerability);
                        }
                        continue;
                    }
                    // Fallback: LineNumber..EndLineNumber range.
                    if (!CxAssistConstants.IsLineInRange(vulnerability.LineNumber, lineCount))
                        continue;
                    int lastUnderlineLine = GetUnderlineEndLine(vulnerability);
                    for (int line1Based = vulnerability.LineNumber; line1Based <= lastUnderlineLine; line1Based++)
                    {
                        if (!CxAssistConstants.IsLineInRange(line1Based, lineCount))
                            break;
                        int lineNumber = CxAssistConstants.To0BasedLineForEditor(vulnerability.Scanner, line1Based);
                        if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                            _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                        _vulnerabilitiesByLine[lineNumber].Add(vulnerability);
                    }
                }
            }

            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
        }

        /// <summary>Last 1-based line for underline (multi-line block). When EndLineNumber is set, use it; otherwise single line.</summary>
        private static int GetUnderlineEndLine(Vulnerability v)
        {
            if (v.EndLineNumber > 0 && v.EndLineNumber >= v.LineNumber)
                return v.EndLineNumber;
            return v.LineNumber;
        }

        /// <summary>
        /// Clears all vulnerabilities and error tags
        /// Similar to reference MarkupModel.removeAllHighlighters()
        /// </summary>
        public void ClearVulnerabilities()
        {
            UpdateVulnerabilities(null);
        }

        /// <summary>
        /// Gets vulnerabilities on the given line (0-based) for rich Quick Info hover.
        /// </summary>
        public IReadOnlyList<Vulnerability> GetVulnerabilitiesForLine(int zeroBasedLineNumber)
        {
            return CxAssistErrorHandler.TryGet(
                () => _vulnerabilitiesByLine.TryGetValue(zeroBasedLineNumber, out var list) ? list : (IReadOnlyList<Vulnerability>)Array.Empty<Vulnerability>(),
                "ErrorTagger.GetVulnerabilitiesForLine",
                Array.Empty<Vulnerability>());
        }
    }
}

