using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    // Legacy sync provider disabled: built-in presenter ignores per-ClassifiedTextRun navigation callbacks.
    // Quick Info is now provided by CxAssistAsyncQuickInfoSourceProvider (IAsyncQuickInfoSource).
    // [Export(typeof(IQuickInfoSourceProvider))]
    [Name("CxAssist QuickInfo Source (legacy, disabled)")]
    [ContentType("code")]
    [ContentType("text")]
    internal class CxAssistQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new CxAssistQuickInfoSource(this, textBuffer);
        }
    }
}
