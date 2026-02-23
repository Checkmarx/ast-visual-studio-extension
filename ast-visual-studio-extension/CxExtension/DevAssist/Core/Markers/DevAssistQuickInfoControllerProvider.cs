using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("DevAssist QuickInfo Controller")]
    [ContentType("code")]
    [ContentType("text")]
    internal class DevAssistQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker AsyncQuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new DevAssistQuickInfoController(textView, subjectBuffers, this);
        }
    }
}
