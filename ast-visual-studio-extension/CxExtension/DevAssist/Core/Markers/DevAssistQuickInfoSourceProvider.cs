using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("DevAssist QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
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
