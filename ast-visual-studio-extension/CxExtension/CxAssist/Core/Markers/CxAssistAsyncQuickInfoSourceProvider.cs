using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("CxAssist Async QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("code")]
    [ContentType("text")]
    [ContentType("JSON")]
    [ContentType("JSONC")]
    internal class CxAssistAsyncQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new CxAssistAsyncQuickInfoSource(textBuffer);
        }
    }
}
