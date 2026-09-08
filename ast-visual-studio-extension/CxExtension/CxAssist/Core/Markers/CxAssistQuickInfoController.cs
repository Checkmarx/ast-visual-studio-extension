using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Triggers the default Quick Info popup on mouse hover over lines with CxAssist vulnerabilities.
    /// Content is provided by CxAssistAsyncQuickInfoSource (no custom popup).
    /// </summary>
    internal class CxAssistQuickInfoController : IIntellisenseController
    {
        private readonly ITextView _textView;
        private readonly IList<ITextBuffer> _subjectBuffers;
        private readonly CxAssistQuickInfoControllerProvider _provider;

        internal CxAssistQuickInfoController(
            ITextView textView,
            IList<ITextBuffer> subjectBuffers,
            CxAssistQuickInfoControllerProvider provider)
        {
            _textView = textView;
            _subjectBuffers = subjectBuffers;
            _provider = provider;
            _textView.MouseHover += OnTextViewMouseHover;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            try
            {
                var point = _textView.BufferGraph.MapDownToFirstMatch(
                    new SnapshotPoint(_textView.TextSnapshot, e.Position),
                    PointTrackingMode.Positive,
                    snapshot => _subjectBuffers.Contains(snapshot.TextBuffer),
                    PositionAffinity.Predecessor);

                if (!point.HasValue)
                    return;

                var buffer = point.Value.Snapshot.TextBuffer;
                int lineNumber = point.Value.Snapshot.GetLineNumberFromPosition(point.Value.Position);

                var tagger = CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer);
                if (tagger == null)
                    return;

                var vulnerabilities = tagger.GetVulnerabilitiesForLine(lineNumber);
                if (vulnerabilities == null || vulnerabilities.Count == 0)
                    return;

                if (!_provider.AsyncQuickInfoBroker.IsQuickInfoActive(_textView))
                {
                    var triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);
                    _ = _provider.AsyncQuickInfoBroker.TriggerQuickInfoAsync(_textView, triggerPoint, QuickInfoSessionOptions.None, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfoController.OnTextViewMouseHover");
            }
        }

        public void Detach(ITextView textView)
        {
            if (_textView == textView)
                _textView.MouseHover -= OnTextViewMouseHover;
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }
    }
}
