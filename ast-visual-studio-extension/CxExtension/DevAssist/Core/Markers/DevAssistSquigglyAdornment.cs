using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Custom adornment layer that draws colored squiggly underlines for DevAssist vulnerabilities
    /// Based on JetBrains EffectType.WAVE_UNDERSCORE pattern
    /// Provides full control over squiggle colors for each severity level
    /// </summary>
    internal class DevAssistSquigglyAdornment
    {
        private readonly IAdornmentLayer _adornmentLayer;
        private readonly IWpfTextView _textView;
        private readonly ITextBuffer _textBuffer;
        private readonly DevAssistErrorTagger _errorTagger;

        // Severity color mapping - based on Checkmarx UI colors
        // Note: JetBrains and VSCode plugins use single red color for all severities
        // This Visual Studio extension provides enhanced UX with severity-specific colors
        private static readonly Dictionary<string, Color> SeverityColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Malicious", Color.FromRgb(139, 0, 0) },      // Dark Red (darker than Critical)
            { "Critical", Color.FromRgb(217, 75, 72) },     // Red (#D94B48 from Checkmarx UI)
            { "High", Color.FromRgb(217, 75, 72) },         // Red (#D94B48 from Checkmarx UI - same as Critical)
            { "Medium", Color.FromRgb(249, 174, 77) },      // Orange/Gold (#F9AE4D from Checkmarx UI)
            { "Low", Color.FromRgb(2, 147, 2) },            // Green (#029302 from Checkmarx UI)
            { "Info", Color.FromRgb(2, 147, 2) },           // Green (same as Low)
            { "Unknown", Color.FromRgb(135, 190, 209) },    // Blue (#87bed1 from Checkmarx UI)
            { "Ok", Color.FromRgb(160, 160, 160) },         // Light Gray
            { "Ignored", Color.FromRgb(128, 128, 128) }     // Dark Gray
        };

        public DevAssistSquigglyAdornment(IWpfTextView textView, DevAssistErrorTagger errorTagger)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
            _errorTagger = errorTagger ?? throw new ArgumentNullException(nameof(errorTagger));
            _textBuffer = textView.TextBuffer;

            // Get or create the adornment layer
            _adornmentLayer = textView.GetAdornmentLayer("DevAssistSquigglyAdornment");

            // Subscribe to events
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.Closed += OnViewClosed;
            _errorTagger.TagsChanged += OnTagsChanged;

            System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Squiggly adornment layer initialized");
        }

        /// <summary>
        /// Handle layout changes (scrolling, text changes, zoom, etc.)
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // Only redraw if the view has changed significantly
            if (e.NewOrReformattedLines.Count > 0 || e.VerticalTranslation || e.NewViewState.ViewportWidth != e.OldViewState.ViewportWidth)
            {
                RedrawAdornments();
            }
        }

        /// <summary>
        /// Handle tags changed event from the error tagger
        /// </summary>
        private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            RedrawAdornments();
        }

        /// <summary>
        /// Clean up when view is closed
        /// </summary>
        private void OnViewClosed(object sender, EventArgs e)
        {
            _textView.LayoutChanged -= OnLayoutChanged;
            _textView.Closed -= OnViewClosed;
            _errorTagger.TagsChanged -= OnTagsChanged;
        }

        /// <summary>
        /// Redraw all squiggly adornments for visible lines
        /// Uses virtualization - only draws squiggles for visible text
        /// </summary>
        private void RedrawAdornments()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DevAssist Adornment: RedrawAdornments called");

                // Clear all existing adornments
                _adornmentLayer.RemoveAllAdornments();
                System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Removed all existing adornments");

                // Get the visible span
                var visibleSpan = new SnapshotSpan(_textView.TextSnapshot, _textView.TextViewLines.FormattedSpan.Start, _textView.TextViewLines.FormattedSpan.Length);
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Visible span: {visibleSpan.Start.Position} to {visibleSpan.End.Position}");

                // Get all error tags in the visible span
                var tags = _errorTagger.GetTags(new NormalizedSnapshotSpanCollection(visibleSpan));
                var tagsList = tags.ToList();
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Found {tagsList.Count} error tags");

                int adornmentCount = 0;
                foreach (var tagSpan in tagsList)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: About to draw squiggly #{adornmentCount + 1} for severity: {tagSpan.Tag.Severity}");
                    DrawSquiggly(tagSpan.Span, tagSpan.Tag.Severity);
                    adornmentCount++;
                    System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Successfully drew squiggly #{adornmentCount}");
                }

                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Drew {adornmentCount} squiggly adornments");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Error redrawing adornments: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Draw a squiggly underline for a specific span with severity-based color
        /// </summary>
        private void DrawSquiggly(SnapshotSpan span, string severity)
        {
            try
            {
                // Get the geometry for the span
                var geometry = _textView.TextViewLines.GetMarkerGeometry(span);
                if (geometry == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: No geometry for span at {span.Start.Position}");
                    return;
                }

                // Get color for severity
                if (!SeverityColors.TryGetValue(severity, out var color))
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Unknown severity '{severity}', using default");
                    color = SeverityColors["Unknown"];
                }

                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Drawing {severity} squiggly with color R={color.R} G={color.G} B={color.B}");

                // Create the squiggly path
                var squigglyPath = CreateSquigglyPath(geometry.Bounds, color);
                if (squigglyPath == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Failed to create squiggly path");
                    return;
                }

                // Add to adornment layer
                Canvas.SetLeft(squigglyPath, geometry.Bounds.Left);
                Canvas.SetTop(squigglyPath, geometry.Bounds.Bottom - 2);

                _adornmentLayer.AddAdornment(
                    AdornmentPositioningBehavior.TextRelative,
                    span,
                    null,
                    squigglyPath,
                    null);

                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Successfully added squiggly adornment");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Error drawing squiggly: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a smooth wavy/squiggly path for the underline using Bezier curves
        /// Similar to Visual Studio's error squiggles but with custom colors
        /// </summary>
        private Path CreateSquigglyPath(Rect bounds, Color color)
        {
            try
            {
                const double waveHeight = 2.5;  // Height of the wave (amplitude)
                const double waveLength = 5.0;  // Length of one complete wave cycle

                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return null;

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure { StartPoint = new Point(0, 0) };

                // Create smooth wave pattern using Bezier curves
                double x = 0;
                bool goingDown = true;  // Start by going down

                while (x < bounds.Width)
                {
                    double halfWave = waveLength / 2.0;
                    double nextX = Math.Min(x + halfWave, bounds.Width);

                    // Control points for smooth Bezier curve
                    double controlX1 = x + halfWave / 3.0;
                    double controlX2 = x + 2.0 * halfWave / 3.0;
                    double peakY = goingDown ? waveHeight : -waveHeight;

                    // Create a smooth curve using quadratic Bezier
                    pathFigure.Segments.Add(new QuadraticBezierSegment(
                        new Point(x + halfWave / 2.0, peakY),  // Control point at peak
                        new Point(nextX, 0),                    // End point back at baseline
                        true));

                    x = nextX;
                    goingDown = !goingDown;
                }

                pathGeometry.Figures.Add(pathFigure);

                var path = new Path
                {
                    Data = pathGeometry,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1.5,  // Bolder/darker stroke for better visibility
                    Width = bounds.Width,
                    Height = waveHeight * 2,
                    Opacity = 1.0  // Full opacity for maximum visibility
                };

                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Adornment: Error creating squiggly path: {ex.Message}");
                return null;
            }
        }
    }
}

