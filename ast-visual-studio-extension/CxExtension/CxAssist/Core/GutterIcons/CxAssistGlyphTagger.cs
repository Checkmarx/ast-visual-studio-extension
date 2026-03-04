using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.GutterIcons
{
    /// <summary>
    /// Tagger that provides glyph tags for CxAssist vulnerabilities
    /// Based on reference MarkupModel.addRangeHighlighter pattern
    /// Manages the lifecycle of gutter icons in the text view
    /// </summary>
    internal class CxAssistGlyphTagger : ITagger<CxAssistGlyphTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Dictionary<int, List<Vulnerability>> _vulnerabilitiesByLine;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CxAssistGlyphTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _vulnerabilitiesByLine = new Dictionary<int, List<Vulnerability>>();
        }

        public IEnumerable<ITagSpan<CxAssistGlyphTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var result = new List<ITagSpan<CxAssistGlyphTag>>();

            if (spans == null || spans.Count == 0 || _vulnerabilitiesByLine.Count == 0)
                return result;

            ITextSnapshot snapshot = null;
            try
            {
                snapshot = spans[0].Snapshot;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "GlyphTagger.GetTags (snapshot)");
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
                            var mostSevere = GetMostSevereVulnerability(vulnerabilities);
                            if (mostSevere != null)
                            {
                                var line = snapshot.GetLineFromLineNumber(lineNumber);
                                var lineSpan = new SnapshotSpan(snapshot, line.Start, line.Length);

                                // Tooltip shows only the severity that matches the icon (precedence / most severe on line)
                                var tag = new CxAssistGlyphTag(
                                    mostSevere.Severity.ToString(),
                                    mostSevere.Severity.ToString(),
                                    mostSevere.Id
                                );
                                result.Add(new TagSpan<CxAssistGlyphTag>(lineSpan, tag));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "GlyphTagger.GetTags (span)");
                }
            }

            return result;
        }

        /// <summary>
        /// Updates vulnerabilities for the buffer
        /// Based on reference ProblemDecorator.decorateUI pattern
        /// </summary>
        public void UpdateVulnerabilities(List<Vulnerability> vulnerabilities)
        {
            CxAssistErrorHandler.TryRun(() => UpdateVulnerabilitiesCore(vulnerabilities), "GlyphTagger.UpdateVulnerabilities");
        }

        private void UpdateVulnerabilitiesCore(List<Vulnerability> vulnerabilities)
        {
            _vulnerabilitiesByLine.Clear();

            if (vulnerabilities != null)
            {
                foreach (var vuln in vulnerabilities)
                {
                    int lineNumber = CxAssistConstants.To0BasedLineForEditor(vuln.Scanner, vuln.LineNumber);
                    if (!_vulnerabilitiesByLine.ContainsKey(lineNumber))
                        _vulnerabilitiesByLine[lineNumber] = new List<Vulnerability>();
                    _vulnerabilitiesByLine[lineNumber].Add(vuln);
                }
            }

            var snapshot = _buffer.CurrentSnapshot;
            var entireSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(entireSpan));
        }

        /// <summary>
        /// Clears all vulnerabilities
        /// Based on reference ProblemDecorator.removeAllHighlighters pattern
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
        /// Based on reference ProblemDecorator.getMostSeverity pattern
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
        /// Based on reference SeverityLevel precedence (inverted for descending order)
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

    }
}

