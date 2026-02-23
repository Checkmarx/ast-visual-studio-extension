using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Async Quick Info source so the modern presenter can wire navigation callbacks
    /// (legacy IQuickInfoSource presenter ignores per-ClassifiedTextRun actions).
    /// Only one content block is shown per session even when multiple subject buffers exist.
    /// </summary>
    internal class DevAssistAsyncQuickInfoSource : IAsyncQuickInfoSource
    {
        private static readonly HashSet<IAsyncQuickInfoSession> _sessionsWithDevAssistContent = new HashSet<IAsyncQuickInfoSession>();
        private static readonly object _sessionLock = new object();

        private readonly ITextBuffer _buffer;
        private bool _disposed;

        public DevAssistAsyncQuickInfoSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (!DevAssistQuickInfoSource.UseRichHover)
                return null;

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!triggerPoint.HasValue && session.TextView != null)
            {
                var viewSnapshot = session.TextView.TextSnapshot;
                var viewTrigger = session.GetTriggerPoint(viewSnapshot);
                if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer != _buffer)
                {
                    var mapped = session.TextView.BufferGraph.MapDownToFirstMatch(
                        viewTrigger.Value,
                        PointTrackingMode.Positive,
                        sb => sb == _buffer,
                        PositionAffinity.Predecessor);
                    if (mapped.HasValue)
                        triggerPoint = mapped.Value;
                }
                else if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer == _buffer)
                {
                    triggerPoint = viewTrigger;
                }
            }

            if (!triggerPoint.HasValue)
                return null;

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = triggerPoint.Value.Snapshot;
            int lineNumber = snapshot.GetLineNumberFromPosition(triggerPoint.Value.Position);

            var tagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(_buffer);
            if (tagger == null)
                return null;

            var vulnerabilities = tagger.GetVulnerabilitiesForLine(lineNumber);
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return null;

            // Only one of our sources (per session) should contribute; avoid duplicate blocks when multiple subject buffers exist.
            lock (_sessionLock)
            {
                if (_sessionsWithDevAssistContent.Contains(session))
                    return null;
                _sessionsWithDevAssistContent.Add(session);
            }

            void OnSessionStateChanged(object sender, QuickInfoSessionStateChangedEventArgs e)
            {
                if (e.NewState == QuickInfoSessionState.Dismissed)
                {
                    lock (_sessionLock)
                    {
                        _sessionsWithDevAssistContent.Remove(session);
                    }
                    session.StateChanged -= OnSessionStateChanged;
                }
            }

            session.StateChanged += OnSessionStateChanged;

            object content = DevAssistQuickInfoSource.BuildQuickInfoContentForLine(vulnerabilities);
            if (content == null)
            {
                lock (_sessionLock)
                {
                    _sessionsWithDevAssistContent.Remove(session);
                }
                session.StateChanged -= OnSessionStateChanged;
                return null;
            }

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            var applicableToSpan = snapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);
            return new QuickInfoItem(applicableToSpan, content);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
        }
    }
}
