using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.GutterIcons
{
    /// <summary>
    /// Factory for creating custom gutter glyphs for DevAssist vulnerabilities
    /// Based on JetBrains GutterIconRenderer pattern adapted for Visual Studio MEF
    /// Uses IGlyphFactory to display custom severity icons in the gutter margin
    /// </summary>
    internal class DevAssistGlyphFactory : IGlyphFactory
    {
        private const double GlyphSize = 16.0;

        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist: GenerateGlyph called - tag type: {tag?.GetType().Name}");

            if (tag == null || !(tag is DevAssistGlyphTag))
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: Tag is null or not DevAssistGlyphTag");
                return null;
            }

            var glyphTag = (DevAssistGlyphTag)tag;
            System.Diagnostics.Debug.WriteLine($"DevAssist: Generating glyph for severity: {glyphTag.Severity}");

            try
            {
                // Create image element for the glyph
                var iconSource = GetIconForSeverity(glyphTag.Severity);
                if (iconSource == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist: Icon source is null for severity: {glyphTag.Severity}");
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

                System.Diagnostics.Debug.WriteLine($"DevAssist: Successfully created glyph image for severity: {glyphTag.Severity}");
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: Icon loading failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the appropriate icon based on severity level
        /// Maps to JetBrains CxIcons.Small pattern (16x16 icons)
        /// Uses SVG icons from ast-jetbrains-plugin organized by theme
        /// Supports: MALICIOUS, CRITICAL, HIGH, MEDIUM, LOW, OK, IGNORED, UNKNOWN
        /// </summary>
        private ImageSource GetIconForSeverity(string severity)
        {
            string iconFileName;

            switch (severity?.ToLower())
            {
                case "malicious":
                    iconFileName = "malicious";
                    break;
                case "critical":
                    iconFileName = "critical";
                    break;
                case "high":
                    iconFileName = "high";
                    break;
                case "medium":
                    iconFileName = "medium";
                    break;
                case "low":
                    iconFileName = "low";
                    break;
                case "info":
                    // Info severity - could use a separate info icon if available
                    // For now, using low severity icon as fallback
                    iconFileName = "low";
                    break;
                case "ok":
                    iconFileName = "ok";
                    break;
                case "ignored":
                    iconFileName = "ignored";
                    break;
                default:
                    iconFileName = "unknown"; // Default fallback
                    break;
            }

            // Try to load themed icon, fallback to PNG if loading fails
            try
            {
                return LoadThemedIcon(iconFileName);
            }
            catch
            {
                // Fallback to existing PNG if themed icon loading fails
                var pngFileName = iconFileName + ".png";
                var iconUri = new Uri($"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/{pngFileName}");
                return new BitmapImage(iconUri);
            }
        }

        /// <summary>
        /// Loads a themed icon from the organized folder structure
        /// Detects Visual Studio theme (Light/Dark) and loads appropriate icon
        /// Path: CxExtension/Resources/DevAssist/Icons/{Light|Dark}/{iconName}.svg
        /// Uses SharpVectors to render SVG files in WPF
        /// </summary>
        private ImageSource LoadThemedIcon(string iconName)
        {
            // Detect Visual Studio theme
            bool isDarkTheme = IsVsDarkTheme();
            string themeFolder = isDarkTheme ? "Dark" : "Light";

            // Build path to themed SVG icon
            var iconUri = new Uri($"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/DevAssist/Icons/{themeFolder}/{iconName}.svg");

            try
            {
                // Load SVG from Pack URI using StreamResourceInfo
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist: Failed to load SVG icon {iconName} - resource not found");
                    return null;
                }

                // Use SharpVectors to load and render SVG from stream
                var settings = new WpfDrawingSettings
                {
                    IncludeRuntime = true,
                    TextAsGeometry = false,
                    OptimizePath = true
                };

                using (var stream = streamInfo.Stream)
                {
                    var converter = new FileSvgReader(settings);
                    var drawing = converter.Read(stream);

                    if (drawing != null)
                    {
                        var drawingImage = new DrawingImage(drawing);
                        drawingImage.Freeze(); // Freeze for better performance
                        return drawingImage;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"DevAssist: Failed to load SVG icon {iconName} - drawing is null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: Failed to load SVG icon {iconName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Detects if Visual Studio is using a dark theme
        /// Uses VSColorTheme to detect current theme
        /// </summary>
        private bool IsVsDarkTheme()
        {
            try
            {
                // Get the current VS theme background color
                var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

                // Calculate brightness (simple luminance formula)
                double brightness = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255.0;

                // If brightness is less than 0.5, it's a dark theme
                return brightness < 0.5;
            }
            catch
            {
                // Default to light theme if detection fails
                return false;
            }
        }
    }

    /// <summary>
    /// MEF export for DevAssist glyph factory provider
    /// Registers the factory for the "DevAssist" glyph tag type
    /// </summary>
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("DevAssistGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [ContentType("text")]
    [TagType(typeof(DevAssistGlyphTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class DevAssistGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public DevAssistGlyphFactoryProvider()
        {
            System.Diagnostics.Debug.WriteLine("DevAssist: DevAssistGlyphFactoryProvider constructor called - MEF is loading glyph factory provider");
        }

        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist: GetGlyphFactory called for margin: {margin?.GetType().Name}");
            return new DevAssistGlyphFactory();
        }
    }

    /// <summary>
    /// Custom glyph tag for DevAssist vulnerabilities
    /// Based on JetBrains GutterIconRenderer pattern
    /// </summary>
    internal class DevAssistGlyphTag : IGlyphTag
    {
        public string Severity { get; }
        public string TooltipText { get; }
        public string VulnerabilityId { get; }

        public DevAssistGlyphTag(string severity, string tooltipText, string vulnerabilityId)
        {
            Severity = severity;
            TooltipText = tooltipText;
            VulnerabilityId = vulnerabilityId;
        }
    }
}

