using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    // Legacy sync provider disabled: built-in presenter ignores per-ClassifiedTextRun navigation callbacks.
    // Quick Info is now provided by DevAssistAsyncQuickInfoSourceProvider (IAsyncQuickInfoSource).
    // [Export(typeof(IQuickInfoSourceProvider))]
    [Name("DevAssist QuickInfo Source (legacy, disabled)")]
    [ContentType("code")]
    [ContentType("text")]
    internal class DevAssistQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new DevAssistQuickInfoSource(this, textBuffer);
        }
    }
}
