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
        /// Gets the snapshot span for the underline: from StartIndex to EndIndex (0-based within line) when set;
        /// otherwise the full line. Clamps to line bounds.
        /// </summary>
        private static SnapshotSpan GetUnderlineSpan(ITextSnapshot snapshot, ITextSnapshotLine line, Vulnerability v)
        {
            if (v.EndIndex > v.StartIndex && v.StartIndex >= 0)
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
        /// Determines if a severity level should show an underline
        /// Similar to reference plugin: only show underlines for actual issues (Malicious, Critical, High, Medium, Low)
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

            if (vulnerabilities != null)
            {
                foreach (var vulnerability in vulnerabilities)
                {
                    int lineNumber = CxAssistConstants.To0BasedLineForEditor(vulnerability.Scanner, vulnerability.LineNumber);
                    if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                        _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                    _vulnerabilitiesByLine[lineNumber].Add(vulnerability);
                }
            }

            var snapshot = _buffer.CurrentSnapshot;
            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
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

