using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Shows the JetBrains-like rich hover via a custom WPF Popup on mouse hover
    /// (Quick Info often does not display our WPF content; this guarantees the badge/links appear).
    /// </summary>
    internal class DevAssistQuickInfoController : IIntellisenseController
    {
        private readonly ITextView _textView;
        private readonly IList<ITextBuffer> _subjectBuffers;
        private readonly DevAssistQuickInfoControllerProvider _provider;
        private Popup _hoverPopup;

        internal DevAssistQuickInfoController(
            ITextView textView,
            IList<ITextBuffer> subjectBuffers,
            DevAssistQuickInfoControllerProvider provider)
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

                var tagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(buffer);
                if (tagger == null)
                    return;

                var vulnerabilities = tagger.GetVulnerabilitiesForLine(lineNumber);
                if (vulnerabilities == null || vulnerabilities.Count == 0)
                {
                    CloseHoverPopup();
                    return;
                }

                if (!_provider.QuickInfoBroker.IsQuickInfoActive(_textView))
                {
                    var triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);
                    _provider.QuickInfoBroker.TriggerQuickInfo(_textView, triggerPoint, trackMouse: true);
                }

                var wpfView = _textView as IWpfTextView;
                if (wpfView?.VisualElement != null)
                {
                    var element = wpfView.VisualElement;
                    var vulnList = vulnerabilities;
                    Dispatcher.CurrentDispatcher.Invoke(() => ShowHoverPopup(element, vulnList));
                }
            }
            catch (Exception ex)
            {
                DevAssistErrorHandler.LogAndSwallow(ex, "QuickInfoController.OnTextViewMouseHover");
                CloseHoverPopup();
            }
        }

        private void ShowHoverPopup(FrameworkElement placementTarget, IReadOnlyList<DevAssist.Core.Models.Vulnerability> vulnerabilities)
        {
            try
            {
                CloseHoverPopup();
                if (vulnerabilities == null || vulnerabilities.Count == 0) return;

                var first = vulnerabilities[0];
                var content = new DevAssistHoverPopup(first, vulnerabilities);

                _hoverPopup = new Popup
                {
                    Child = content,
                    PlacementTarget = placementTarget,
                    Placement = PlacementMode.MousePoint,
                    StaysOpen = false,
                    AllowsTransparency = false
                };

                _hoverPopup.Closed += (s, args) => _hoverPopup = null;
                _hoverPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                DevAssistErrorHandler.LogAndSwallow(ex, "QuickInfoController.ShowHoverPopup");
            }
        }

        private void CloseHoverPopup()
        {
            if (_hoverPopup != null)
            {
                _hoverPopup.IsOpen = false;
                _hoverPopup = null;
            }
        }

        public void Detach(ITextView textView)
        {
            if (_textView == textView)
            {
                _textView.MouseHover -= OnTextViewMouseHover;
                CloseHoverPopup();
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }
    }
}
