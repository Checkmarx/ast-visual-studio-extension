using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Classification type names used in Quick Info content. Must match the names used in
    /// [ClassificationType(ClassificationTypeNames = ...)] on the format definitions.
    /// </summary>
    internal static class DevAssistQuickInfoClassificationNames
    {
        public const string Header = "DevAssist.QuickInfo.Header";
        public const string Link = "DevAssist.QuickInfo.Link";
    }

    /// <summary>
    /// Format for "Checkmarx One Assist" and severity (e.g. "High") in Quick Info: bold + colored.
    /// Applied when ClassifiedTextRun uses DevAssistQuickInfoClassificationNames.Header.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DevAssistQuickInfoClassificationNames.Header)]
    [Name("DevAssist Quick Info Header")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class DevAssistQuickInfoHeaderFormat : ClassificationFormatDefinition
    {
        public DevAssistQuickInfoHeaderFormat()
        {
            DisplayName = "DevAssist Quick Info Header";
            IsBold = true;
            ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6); // light blue, readable on dark theme
        }
    }

    /// <summary>
    /// Format for action links in Quick Info: link-like color + underline.
    /// Applied when ClassifiedTextRun uses DevAssistQuickInfoClassificationNames.Link.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = DevAssistQuickInfoClassificationNames.Link)]
    [Name("DevAssist Quick Info Link")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class DevAssistQuickInfoLinkFormat : ClassificationFormatDefinition
    {
        public DevAssistQuickInfoLinkFormat()
        {
            DisplayName = "DevAssist Quick Info Link";
            IsBold = false;
            ForegroundColor = Color.FromRgb(0x37, 0x94, 0xFF); // link blue (underline comes from ClassifiedTextRunStyle.Underline)
        }
    }
}
