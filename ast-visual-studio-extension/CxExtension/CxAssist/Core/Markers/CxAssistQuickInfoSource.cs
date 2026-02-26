using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Quick Info source using official content types (ContainerElement, ClassifiedTextElement, ClassifiedTextRun)
    /// so the default Quick Info presenter shows description, links, and optional image.
    /// </summary>
    internal class CxAssistQuickInfoSource : IQuickInfoSource
    {
        internal const bool UseRichHover = true;

        private readonly CxAssistQuickInfoSourceProvider _provider;
        private readonly ITextBuffer _buffer;
        private bool _disposed;

        public CxAssistQuickInfoSource(CxAssistQuickInfoSourceProvider provider, ITextBuffer buffer)
        {
            _provider = provider;
            _buffer = buffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!UseRichHover)
                return;

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!triggerPoint.HasValue && session.TextView != null)
            {
                var viewSnapshot = session.TextView.TextSnapshot;
                var viewTrigger = session.GetTriggerPoint(viewSnapshot);
                if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer != _buffer)
                {
                    var mapped = session.TextView.BufferGraph.MapDownToFirstMatch(
                        viewTrigger.Value,
                        Microsoft.VisualStudio.Text.PointTrackingMode.Positive,
                        sb => sb == _buffer,
                        Microsoft.VisualStudio.Text.PositionAffinity.Predecessor);
                    if (mapped.HasValue)
                        triggerPoint = mapped.Value;
                }
                else if (viewTrigger.HasValue && viewTrigger.Value.Snapshot.TextBuffer == _buffer)
                    triggerPoint = viewTrigger;
            }

            if (!triggerPoint.HasValue)
                return;

            var snapshot = triggerPoint.Value.Snapshot;
            int lineNumber = snapshot.GetLineNumberFromPosition(triggerPoint.Value.Position);

            var tagger = CxAssistErrorTaggerProvider.GetTaggerForBuffer(_buffer);
            if (tagger == null)
                return;

            var vulnerabilities = tagger.GetVulnerabilitiesForLine(lineNumber);
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return;

            // Success (Ok) and Unknown: gutter icon only; do not show in popup
            var issuesOnly = vulnerabilities
                .Where(v => v.Severity != SeverityLevel.Ok && v.Severity != SeverityLevel.Unknown)
                .ToList();
            if (issuesOnly.Count == 0)
                return;

            object content = BuildQuickInfoContentForLine(issuesOnly);
            if (content == null)
                return;

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            applicableToSpan = snapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);
            qiContent.Insert(0, content);
        }

        /// <summary>
        /// Builds Quick Info content for all vulnerabilities on the line (e.g. line 13 with 2 findings shows both).
        /// Shared for use by CxAssistAsyncQuickInfoSource (IAsyncQuickInfoSource).
        /// </summary>
        internal static object BuildQuickInfoContentForLine(IReadOnlyList<Vulnerability> vulnerabilities)
        {
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return null;
            if (vulnerabilities.Count == 1)
                return BuildQuickInfoContent(vulnerabilities[0]);

            var elements = new List<object>();

            // Single header at top
            var headerRow = CreateHeaderRow();
            if (headerRow != null)
                elements.Add(headerRow);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.DisplayName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                ));

            for (int i = 0; i < vulnerabilities.Count; i++)
            {
                var v = vulnerabilities[i];
                var title = !string.IsNullOrEmpty(v.Title) ? v.Title : (!string.IsNullOrEmpty(v.RuleName) ? v.RuleName : v.Description);
                var description = !string.IsNullOrEmpty(v.Description) ? v.Description : "Vulnerability detected by " + v.Scanner + ".";
                var severityName = GetRichSeverityName(v.Severity);

                if (i > 0)
                {
                    var betweenSeparator = CreateHorizontalSeparator();
                    if (betweenSeparator != null)
                        elements.Add(betweenSeparator);
                    // Header (Checkmarx One Assist) for each additional finding
                    var headerForFinding = CreateHeaderRow();
                    if (headerForFinding != null)
                        elements.Add(headerForFinding);
                    else
                        elements.Add(new ClassifiedTextElement(
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.DisplayName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                        ));
                }

                var severityTitleRow = CreateSeverityTitleRow(v.Severity, title ?? "", severityName);
                if (severityTitleRow != null)
                    elements.Add(severityTitleRow);
                else
                {
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, severityName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, title ?? "", ClassifiedTextRunStyle.UseClassificationFont)
                    ));
                }

                var descriptionBlock = CreateDescriptionBlock(description);
                if (descriptionBlock != null)
                    elements.Add(descriptionBlock);
                else
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, description, ClassifiedTextRunStyle.UseClassificationFont)
                    ));

                var linksRow = CreateActionLinksRow(v);
                if (linksRow != null)
                    elements.Add(linksRow);
                else
                {
                    const string urlClassification = "url";
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(urlClassification, "Fix with Checkmarx Assist", () => RunFixWithAssist(v), "Fix with Checkmarx Assist", ClassifiedTextRunStyle.Underline),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(urlClassification, "View Details", () => RunViewDetails(v), "View Details", ClassifiedTextRunStyle.Underline),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                        new ClassifiedTextRun(urlClassification, "Ignore vulnerability", () => RunIgnoreVulnerability(v), "Ignore vulnerability", ClassifiedTextRunStyle.Underline)
                    ));
                }
            }

            var separator = CreateHorizontalSeparator();
            if (separator != null)
                elements.Add(separator);

            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        /// <summary>
        /// Builds content for a single vulnerability (header + severity+title + description + links + separator).
        /// </summary>
        internal static object BuildQuickInfoContent(Vulnerability v)
        {
            if (v == null) return null;

            var title = !string.IsNullOrEmpty(v.Title) ? v.Title : (!string.IsNullOrEmpty(v.RuleName) ? v.RuleName : v.Description);
            var description = !string.IsNullOrEmpty(v.Description) ? v.Description : "Vulnerability detected by " + v.Scanner + ".";
            var severityName = GetRichSeverityName(v.Severity);

            var elements = new List<object>();

            // Row 1 – Header (badge + "Checkmarx One Assist") – custom-popup style
            var headerRow = CreateHeaderRow();
            if (headerRow != null)
                elements.Add(headerRow);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, CxAssistConstants.DisplayName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                ));

            // Row 2 – Severity icon + title on one line – custom-popup style
            var severityTitleRow = CreateSeverityTitleRow(v.Severity, title ?? "", severityName);
            if (severityTitleRow != null)
                elements.Add(severityTitleRow);
            else
            {
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, severityName, ClassifiedTextRunStyle.UseClassificationStyle | ClassifiedTextRunStyle.UseClassificationFont)
                ));
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, title ?? "", ClassifiedTextRunStyle.UseClassificationFont)
                ));
            }

            // Description with extra line spacing between lines
            var descriptionBlock = CreateDescriptionBlock(description);
            if (descriptionBlock != null)
                elements.Add(descriptionBlock);
            else
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, description, ClassifiedTextRunStyle.UseClassificationFont)
                ));

            // Action links: underline only on mouse hover
            var linksRow = CreateActionLinksRow(v);
            if (linksRow != null)
                elements.Add(linksRow);
            else
            {
                const string urlClassification = "url";
                elements.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(urlClassification, "Fix with Checkmarx Assist", () => RunFixWithAssist(v), "Fix with Checkmarx Assist", ClassifiedTextRunStyle.Underline),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(urlClassification, "View Details", () => RunViewDetails(v), "View Details", ClassifiedTextRunStyle.Underline),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "  ", ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(urlClassification, "Ignore vulnerability", () => RunIgnoreVulnerability(v), "Ignore vulnerability", ClassifiedTextRunStyle.Underline)
                ));
            }

            // Horizontal separator line after our details
            var separator = CreateHorizontalSeparator();
            if (separator != null)
                elements.Add(separator);

            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        internal static void RunFixWithAssist(Vulnerability v)
        {
            RunOnUiThread(() => MessageBox.Show($"Fix with {CxAssistConstants.DisplayName}:\n{v?.Title ?? v?.Description ?? "—"}", CxAssistConstants.DisplayName));
        }

        internal static void RunViewDetails(Vulnerability v)
        {
            var url = !string.IsNullOrEmpty(v?.LearnMoreUrl) ? v.LearnMoreUrl : v?.FixLink;
            if (!string.IsNullOrEmpty(url))
            {
                RunOnUiThread(() =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CxAssist QuickInfo View Details: {ex.Message}");
                        MessageBox.Show($"Could not open link: {url}", CxAssistConstants.DisplayName);
                    }
                });
            }
            else
            {
                RunOnUiThread(() => MessageBox.Show($"View Details:\n{v?.Title ?? ""}\n{v?.Description ?? ""}\nSeverity: {v?.Severity}", CxAssistConstants.DisplayName));
            }
        }

        internal static void RunIgnoreVulnerability(Vulnerability v)
        {
            RunOnUiThread(() => MessageBox.Show($"Ignore vulnerability:\n{v?.Title ?? v?.Description ?? "—"}", CxAssistConstants.DisplayName));
        }

        internal static void RunOnUiThread(Action action)
        {
            if (action == null) return;
            try
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(action, DispatcherPriority.Send);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.RunOnUiThread");
            }
        }

        /// <summary>
        /// Description text with extra line spacing between lines.
        /// </summary>
        private static System.Windows.UIElement CreateDescriptionBlock(string description)
        {
            if (string.IsNullOrEmpty(description)) return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return new TextBlock
                    {
                        Text = description,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 20,
                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateDescriptionBlock");
                return null;
            }
        }

        /// <summary>
        /// Action links row: underline only on mouse hover (not by default).
        /// </summary>
        private static System.Windows.UIElement CreateActionLinksRow(Vulnerability v)
        {
            if (v == null) return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var linkBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };

                    void AddLink(string text, Action clickAction)
                    {
                        var block = new TextBlock
                        {
                            Text = text,
                            Foreground = linkBrush,
                            Cursor = System.Windows.Input.Cursors.Hand,
                            Margin = new Thickness(0, 0, 12, 0)
                        };
                        block.MouseEnter += (s, _) => { block.TextDecorations = TextDecorations.Underline; };
                        block.MouseLeave += (s, _) => { block.TextDecorations = null; };
                        block.MouseLeftButtonDown += (s, _) => { RunOnUiThread(clickAction); };
                        panel.Children.Add(block);
                    }

                    AddLink("Fix with Checkmarx Assist", () => RunFixWithAssist(v));
                    AddLink("View Details", () => RunViewDetails(v));
                    AddLink("Ignore vulnerability", () => RunIgnoreVulnerability(v));

                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateActionLinksRow");
                return null;
            }
        }

        /// <summary>
        /// Creates a thin horizontal line (separator) to show after our Quick Info details.
        /// </summary>
        private static System.Windows.UIElement CreateHorizontalSeparator()
        {
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return new Border
                    {
                        Height = 1,
                        Margin = new Thickness(0, 6, 0, 0),
                        Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
                    };
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateHorizontalSeparator");
                return null;
            }
        }

        /// <summary>
        /// Returns current VS theme folder name for icon paths: "Dark" or "Light".
        /// Uses VSColorTheme so badge and severity icons follow IDE theme dynamically.
        /// </summary>
        private static string GetCurrentTheme()
        {
            try
            {
                var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                double brightness = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255.0;
                return brightness < 0.5 ? CxAssistConstants.ThemeDark : CxAssistConstants.ThemeLight;
            }
            catch
            {
                return CxAssistConstants.ThemeDark;
            }
        }

        /// <summary>
        /// Header row: badge + "Checkmarx One Assist" text (custom-popup style, no custom popup).
        /// </summary>
        private static System.Windows.UIElement CreateHeaderRow()
        {
            string theme = GetCurrentTheme();
            var source = LoadIconFromAssembly(theme, CxAssistConstants.BadgeIconFileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, CxAssistConstants.BadgeIconFileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 150,
                        Height = 32,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    panel.Children.Add(image);
                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateHeaderRow");
                return null;
            }
        }

        /// <summary>
        /// Severity + title row: icon + finding title on one line (custom-popup style).
        /// </summary>
        private static System.Windows.UIElement CreateSeverityTitleRow(SeverityLevel severity, string title, string severityName)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            ImageSource severitySource = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                severitySource = LoadIconFromAssembly(theme, fileName);
                if (severitySource == null && theme != CxAssistConstants.ThemeDark)
                    severitySource = LoadIconFromAssembly(CxAssistConstants.ThemeDark, fileName);
            }
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                    if (severitySource != null)
                    {
                        var image = new Image
                        {
                            Source = severitySource,
                            Width = 16,
                            Height = 16,
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 6, 0)
                        };
                        panel.Children.Add(image);
                    }
                    var text = new TextBlock
                    {
                        Text = string.IsNullOrEmpty(title) ? severityName : title,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    };
                    panel.Children.Add(text);
                    return (System.Windows.UIElement)panel;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSeverityTitleRow");
                return null;
            }
        }

        /// <summary>
        /// Creates a WPF Image for the Checkmarx One Assist badge only (theme-based). Used when not using header row.
        /// </summary>
        private static System.Windows.UIElement CreateCxAssistBadgeImage()
        {
            string theme = GetCurrentTheme();
            var source = LoadIconFromAssembly(theme, CxAssistConstants.BadgeIconFileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, CxAssistConstants.BadgeIconFileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 150,
                        Height = 32,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    return (System.Windows.UIElement)image;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateCxAssistBadgeImage");
                return null;
            }
        }

        /// <summary>
        /// Creates a WPF Image for the severity icon (theme-based, dynamic by SeverityLevel).
        /// </summary>
        private static System.Windows.UIElement CreateSeverityImage(SeverityLevel severity)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            if (string.IsNullOrEmpty(fileName))
                return null;
            var source = LoadIconFromAssembly(theme, fileName);
            if (source == null && theme != CxAssistConstants.ThemeDark)
                source = LoadIconFromAssembly(CxAssistConstants.ThemeDark, fileName);
            if (source == null)
                return null;
            try
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var image = new Image
                    {
                        Source = source,
                        Width = 16,
                        Height = 16,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    return (System.Windows.UIElement)image;
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "QuickInfo.CreateSeverityImage");
                return null;
            }
        }

        private static string GetSeverityIconFileName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious: return "malicious.png";
                case SeverityLevel.Critical: return "critical.png";
                case SeverityLevel.High: return "high.png";
                case SeverityLevel.Medium: return "medium.png";
                case SeverityLevel.Low:
                case SeverityLevel.Info: return "low.png";
                default: return null;
            }
        }

        /// <summary>
        /// Loads a PNG from CxAssist Icons by theme (Dark/Light). Used for badge and severity icons.
        /// </summary>
        private static BitmapImage LoadIconFromAssembly(string theme, string fileName)
        {
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/{theme}/{fileName}";
            try
            {
                var streamInfo = System.Windows.Application.GetResourceStream(new Uri(packPath, UriKind.Absolute));
                if (streamInfo?.Stream != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        streamInfo.Stream.CopyTo(ms);
                        ms.Position = 0;
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = ms;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        img.Freeze();
                        return img;
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, $"QuickInfo.LoadIconFromAssembly (pack): {fileName}");
            }

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Replace('\\', '/').EndsWith($"CxAssist/Icons/{theme}/{fileName}", StringComparison.OrdinalIgnoreCase)
                                      || n.Replace('\\', '.').EndsWith($"CxAssist.Icons.{theme}.{fileName}", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    using (var stream = asm.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.StreamSource = stream;
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();
                            img.Freeze();
                            return img;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, $"QuickInfo.LoadIconFromAssembly (manifest): {fileName}");
            }

            return null;
        }

        internal static string GetRichSeverityName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Critical: return "Critical";
                case SeverityLevel.High: return "High";
                case SeverityLevel.Medium: return "Medium";
                case SeverityLevel.Low: return "Low";
                case SeverityLevel.Info: return "Info";
                case SeverityLevel.Malicious: return "Malicious";
                case SeverityLevel.Unknown: return "Unknown";
                case SeverityLevel.Ok: return "Ok";
                case SeverityLevel.Ignored: return "Ignored";
                default: return severity.ToString();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }
}
