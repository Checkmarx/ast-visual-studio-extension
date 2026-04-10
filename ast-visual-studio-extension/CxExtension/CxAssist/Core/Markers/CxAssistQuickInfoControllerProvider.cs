using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("CxAssist QuickInfo Controller")]
    [ContentType("code")]
    [ContentType("text")]
    internal class CxAssistQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker AsyncQuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new CxAssistQuickInfoController(textView, subjectBuffers, this);
        }
    }
}
