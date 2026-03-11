using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Suggested actions source for CxAssist: shows Quick Fix (light bulb) when the caret is on a line that has at least one vulnerability.
    /// </summary>
    internal class CxAssistSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;

        public CxAssistSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
            _textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    int lineNumber = range.Snapshot.GetLineNumberFromPosition(range.Start);
                    var tagger = CxAssistErrorTaggerProvider.GetTaggerForBuffer(_textBuffer);
                    if (tagger == null) return false;
                    var list = tagger.GetVulnerabilitiesForLine(lineNumber);
                    return list != null && list.Count > 0;
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistSuggestedActionsSource.HasSuggestedActionsAsync");
                    return false;
                }
            }, cancellationToken);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            try
            {
                int lineNumber = range.Snapshot.GetLineNumberFromPosition(range.Start);
                var tagger = CxAssistErrorTaggerProvider.GetTaggerForBuffer(_textBuffer);
                if (tagger == null) return Enumerable.Empty<SuggestedActionSet>();
                var list = tagger.GetVulnerabilitiesForLine(lineNumber);
                if (list == null || list.Count == 0) return Enumerable.Empty<SuggestedActionSet>();

                var vulnerability = list[0];
                var actions = new List<ISuggestedAction>
                {
                    new FixWithCxOneAssistSuggestedAction(vulnerability),
                    new ViewDetailsSuggestedAction(vulnerability),
                    new IgnoreThisVulnerabilitySuggestedAction(vulnerability)
                };
                if (CxAssistConstants.ShouldShowIgnoreAll(vulnerability.Scanner))
                    actions.Add(new IgnoreAllOfThisTypeSuggestedAction(vulnerability));
                return new[] { new SuggestedActionSet(actions) };
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistSuggestedActionsSource.GetSuggestedActions");
                return Enumerable.Empty<SuggestedActionSet>();
            }
        }
    }
}
