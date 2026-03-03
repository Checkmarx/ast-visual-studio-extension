using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class UIUtils
    {
        /// <summary>
        /// Returns theme-aware severity icon (CxAssist Dark/Light). Info uses the original icon; all others use CxAssist Icons.
        /// </summary>
        public static ImageSource GetSeverityIconSource(string severity, bool iconForTitle)
        {
            if (string.IsNullOrEmpty(severity)) return null;
            string s = severity.ToUpperInvariant();
            if (s == "INFO")
            {
                try
                {
                    string path = CxUtils.GetIconPathFromSeverity("INFO", iconForTitle);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch { return null; }
            }
            string themeFolder = IsDarkTheme() ? "Dark" : "Light";
            string iconName;
            switch (s)
            {
                case "CRITICAL": iconName = "critical.png"; break;
                case "HIGH": iconName = "high.png"; break;
                case "MEDIUM": iconName = "medium.png"; break;
                case "LOW": iconName = "low.png"; break;
                default: iconName = "low.png"; break;
            }
            try
            {
                string iconPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/{themeFolder}/{iconName}";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private static bool IsDarkTheme()
        {
            try
            {
                var color = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                int brightness = (int)Math.Sqrt(
                    color.R * color.R * 0.299 +
                    color.G * color.G * 0.587 +
                    color.B * color.B * 0.114);
                return brightness < 128;
            }
            catch { return true; }
        }

        public static string FormatStateName(string stateName)
        {
            string formatted = stateName.Replace("_", " ").ToLower();
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(formatted);
        }


        public static CxWindowControl CxWindowUI { get; set; }

        /// <summary>
        ///  Create header for tree view item
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static TextBlock CreateTreeViewItemHeader(string severity, string displayName)
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Add highlight color on hovering each item
            stackPanel.MouseEnter += OnMouseOverResult;
            stackPanel.MouseLeave += OnMouseLeaveResult;

            if (!string.IsNullOrEmpty(severity))
            {
                Image severityIcon = new Image
                {
                    Source = GetSeverityIconSource(severity, false),
                    Width = 14,
                    Height = 14,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center
                };

                stackPanel.Children.Add(severityIcon);
            }

            Label resultDisplayName = new Label
            {
                Content = displayName.Replace("_", " ")
            };
            stackPanel.Children.Add(resultDisplayName);

            InlineUIContainer uiContainer = new InlineUIContainer(stackPanel);

            TextBlock resultUIElement = new TextBlock();
            resultUIElement.Inlines.Add(uiContainer);
            resultUIElement.Tag = displayName;
            resultUIElement.TextWrapping = TextWrapping.WrapWithOverflow;
            if (!string.IsNullOrEmpty(severity)) resultUIElement.ToolTip = displayName.Replace("_" ," ");
            return resultUIElement;
        }

        /// <summary>
        ///  Create a tree view item using items source
        /// </summary>
        /// <param name="headerText"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TreeViewItem CreateTreeViewItemWithItemsSource(string headerText, List<TreeViewItem> source)
        {
            return new TreeViewItem
            {
                Header = CreateTreeViewItemHeader(string.Empty, headerText),
                ItemsSource = source,
                Tag = headerText
            };
        }

        private static void OnMouseOverResult(object sender, RoutedEventArgs e)
        {
            (sender as StackPanel).Background = CxWindowUI.hiddenLblHoverColor.Foreground;
        }

        private static void OnMouseLeaveResult(object sender, RoutedEventArgs e)
        {
            (sender as StackPanel).Background = new SolidColorBrush(Colors.Transparent);
        }

        public static TextBlock CreateTextBlock(string message)
        {
            return new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWithOverflow
            };
        }

        public static StackPanel CreateSeverityLabelWithIcon(string severity)
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            Image severityIcon = new Image
            {
                Source = GetSeverityIconSource(severity, false),
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(severityIcon);

            Label severityLabel = new Label
            {
                Content = severity
            };

            stackPanel.Children.Add(severityLabel);

            return stackPanel;
        }

        public static StackPanel CreateLabelWithImage(string message, string icon)
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            Image severityIcon = new Image();
            
            BitmapImage severityBitmap = new BitmapImage(new Uri(CxConstants.RESOURCES_BASE_DIR + icon, UriKind.RelativeOrAbsolute));
            severityIcon.Source = severityBitmap;

            stackPanel.Children.Add(severityIcon);

            Label severityLabel = new Label
            {
                Content = message
            };

            stackPanel.Children.Add(severityLabel);

            return stackPanel;
        }
    }
}
