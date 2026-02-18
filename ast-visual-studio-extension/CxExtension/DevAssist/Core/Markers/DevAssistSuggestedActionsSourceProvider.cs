using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Provides Quick Fix (light bulb) suggested actions for DevAssist findings:
    /// "Fix with Checkmarx One Assist" and "View details" when the caret is on a line with a vulnerability.
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("DevAssist Quick Fix")]
    [ContentType("code")]
    [ContentType("text")]
    internal class DevAssistSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
                return null;
            return new DevAssistSuggestedActionsSource(textView, textBuffer);
        }
    }
}
