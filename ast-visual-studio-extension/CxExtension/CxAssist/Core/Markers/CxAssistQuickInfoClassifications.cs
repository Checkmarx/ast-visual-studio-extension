using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Classification type names used in Quick Info content. Must match the names used in
    /// [ClassificationType(ClassificationTypeNames = ...)] on the format definitions.
    /// </summary>
    internal static class CxAssistQuickInfoClassificationNames
    {
        public const string Header = "CxAssist.QuickInfo.Header";
        public const string Link = "CxAssist.QuickInfo.Link";
    }

    /// <summary>
    /// Format for "Checkmarx One Assist" and severity (e.g. "High") in Quick Info: bold + colored.
    /// Applied when ClassifiedTextRun uses CxAssistQuickInfoClassificationNames.Header.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CxAssistQuickInfoClassificationNames.Header)]
    [Name("CxAssist Quick Info Header")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class CxAssistQuickInfoHeaderFormat : ClassificationFormatDefinition
    {
        public CxAssistQuickInfoHeaderFormat()
        {
            DisplayName = "CxAssist Quick Info Header";
            IsBold = true;
            ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6); // light blue, readable on dark theme
        }
    }

    /// <summary>
    /// Format for action links in Quick Info: link-like color + underline.
    /// Applied when ClassifiedTextRun uses CxAssistQuickInfoClassificationNames.Link.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = CxAssistQuickInfoClassificationNames.Link)]
    [Name("CxAssist Quick Info Link")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class CxAssistQuickInfoLinkFormat : ClassificationFormatDefinition
    {
        public CxAssistQuickInfoLinkFormat()
        {
            DisplayName = "CxAssist Quick Info Link";
            IsBold = false;
            ForegroundColor = Color.FromRgb(0x37, 0x94, 0xFF); // link blue (underline comes from ClassifiedTextRunStyle.Underline)
        }
    }
}
