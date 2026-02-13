using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Quick Info source using official content types (ContainerElement, ClassifiedTextElement, ClassifiedTextRun)
    /// so the default Quick Info presenter shows description, links, and optional image.
    /// </summary>
    internal class DevAssistQuickInfoSource : IQuickInfoSource
    {
        internal const bool UseRichHover = true;

        private readonly DevAssistQuickInfoSourceProvider _provider;
        private readonly ITextBuffer _buffer;
        private bool _disposed;

        public DevAssistQuickInfoSource(DevAssistQuickInfoSourceProvider provider, ITextBuffer buffer)
        {
            _provider = provider;
            _buffer = buffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!UseRichHover)
                return;

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!triggerPoint.HasValue && session.TextView != null)
            {
                var viewSnapshot = session.TextView.TextSnapshot;
                var viewTrigger = session.GetTriggerPoint(viewSnapshot);
                if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer != _buffer)
                {
                    var mapped = session.TextView.BufferGraph.MapDownToFirstMatch(
                        viewTrigger.Value,
                        Microsoft.VisualStudio.Text.PointTrackingMode.Positive,
                        sb => sb == _buffer,
                        Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
                    if (mapped.HasValue)
                        triggerPoint = mapped.Value;
                }
                else if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer == _buffer)
                    triggerPoint = viewTrigger;
            }

            if (!triggerPoint.HasValue)
                return;

            var snapshot = triggerPoint.Value.Snapshot;
            int lineNumber = snapshot.GetLineNumberFromPosition(triggerPoint.Value.Position);

            var tagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(_buffer);
            if (tagger == null)
                return;

            var vulnerabilities = tagger.GetVulnerabilitiesForLine(lineNumber);
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return;

            var first = vulnerabilities[0];

            object content = BuildQuickInfoContent(first);
            if (content == null)
                return;

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            applicableToSpan = snapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);
            qiContent.Insert(0, content);
        }

        /// <summary>
        /// Builds content using official Quick Info types: ContainerElement, ClassifiedTextElement, ClassifiedTextRun (with navigation).
        /// Default presenter resolves these via IViewElementFactoryService for description, links, and theming.
        /// </summary>
        private static object BuildQuickInfoContent(Vulnerability v)
        {
            if (v == null) return null;

            var title = !string.IsNullOrEmpty(v.Title) ? v.Title : (!string.IsNullOrEmpty(v.RuleName) ? v.RuleName : v.Description);
            var description = !string.IsNullOrEmpty(v.Description) ? v.Description : "Vulnerability detected by " + v.Scanner + ".";

            var elements = new List<object>();

            elements.Add(new ClassifiedTextElement(
                new ClassifiedTextRun("keyword", "DevAssist", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", " • ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("keyword", v.Scanner.ToString(), ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", " • ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("keyword", v.Severity.ToString(), ClassifiedTextRunStyle.UseClassificationFont)
            ));

            elements.Add(new ClassifiedTextElement(
                new ClassifiedTextRun("plain text", title ?? "", ClassifiedTextRunStyle.UseClassificationFont)
            ));

            elements.Add(new ClassifiedTextElement(
                new ClassifiedTextRun("plain text", description, ClassifiedTextRunStyle.UseClassificationFont)
            ));

            elements.Add(new ClassifiedTextElement(
                new ClassifiedTextRun("plain text", "Fix with Checkmarx One Assist", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", " | ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", "View details", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", " | ", ClassifiedTextRunStyle.UseClassificationFont),
                new ClassifiedTextRun("plain text", "Ignore this vulnerability", ClassifiedTextRunStyle.UseClassificationFont)
            ));

            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }
}
