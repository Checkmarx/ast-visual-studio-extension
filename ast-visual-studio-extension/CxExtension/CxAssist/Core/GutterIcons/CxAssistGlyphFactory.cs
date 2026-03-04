using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.GutterIcons
{
    /// <summary>
    /// Factory for creating custom gutter glyphs for CxAssist vulnerabilities
    /// Based on reference GutterIconRenderer pattern adapted for Visual Studio MEF
    /// Uses IGlyphFactory to display custom severity icons in the gutter margin
    /// </summary>
    internal class CxAssistGlyphFactory : IGlyphFactory
    {
        private const double GlyphSize = 14.0;

        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            System.Diagnostics.Debug.WriteLine($"CxAssist: GenerateGlyph called - tag type: {tag?.GetType().Name}");

            if (tag == null || !(tag is CxAssistGlyphTag))
            {
                System.Diagnostics.Debug.WriteLine($"CxAssist: Tag is null or not CxAssistGlyphTag");
                return null;
            }

            var glyphTag = (CxAssistGlyphTag)tag;
            System.Diagnostics.Debug.WriteLine($"CxAssist: Generating glyph for severity: {glyphTag.Severity}");

            try
            {
                // Create image element for the glyph (SVG preferred, fallback to PNG)
                var iconSource = AssistIconLoader.LoadSeveritySvgIcon(glyphTag.Severity)
                    ?? (ImageSource)AssistIconLoader.LoadSeverityPngIcon(glyphTag.Severity);
                if (iconSource == null)
                {
                    System.Diagnostics.Debug.WriteLine($"CxAssist: Icon source is null for severity: {glyphTag.Severity}");
                    return null;
                }

                var image = new Image
                {
                    Width = GlyphSize,
                    Height = GlyphSize,
                    Source = iconSource
                };

                // Set tooltip
                if (!string.IsNullOrEmpty(glyphTag.TooltipText))
                {
                    image.ToolTip = glyphTag.TooltipText;
                }

                System.Diagnostics.Debug.WriteLine($"CxAssist: Successfully created glyph image for severity: {glyphTag.Severity}");
                return image;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "GlyphFactory.GenerateGlyph");
                return null;
            }
        }

    }

    /// <summary>
    /// MEF export for CxAssist glyph factory provider
    /// Registers the factory for the "CxAssist" glyph tag type
    /// </summary>
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("CxAssistGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [ContentType("text")]
    [TagType(typeof(CxAssistGlyphTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class CxAssistGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public CxAssistGlyphFactoryProvider()
        {
            System.Diagnostics.Debug.WriteLine("CxAssist: CxAssistGlyphFactoryProvider constructor called - MEF is loading glyph factory provider");
        }

        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            System.Diagnostics.Debug.WriteLine($"CxAssist: GetGlyphFactory called for margin: {margin?.GetType().Name}");
            return new CxAssistGlyphFactory();
        }
    }

    /// <summary>
    /// Custom glyph tag for CxAssist vulnerabilities
    /// Based on reference GutterIconRenderer pattern
    /// </summary>
    internal class CxAssistGlyphTag : IGlyphTag
    {
        public string Severity { get; }
        public string TooltipText { get; }
        public string VulnerabilityId { get; }

        public CxAssistGlyphTag(string severity, string tooltipText, string vulnerabilityId)
        {
            Severity = severity;
            TooltipText = tooltipText;
            VulnerabilityId = vulnerabilityId;
        }
    }
}

