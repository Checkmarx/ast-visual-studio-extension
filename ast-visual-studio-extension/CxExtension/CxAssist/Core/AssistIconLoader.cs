using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Reusable icon and theme utilities for DevAssist (CxAssist).
    /// Single place for VS theme detection and loading CxAssist icons (PNG/SVG) so that
    /// Quick Info, Gutter, Findings window, and other UI stay consistent and DRY.
    /// </summary>
    internal static class AssistIconLoader
    {
        /// <summary>Base resource path for CxAssist icons (theme subfolder appended).</summary>
        public const string IconsBasePath = "CxExtension/Resources/CxAssist/Icons";

        private static bool _popupIconsLogged;
        private static string _lastKnownTheme;

        /// <summary>Returns "Dark" or "Light" based on current VS theme.</summary>
        public static string GetCurrentTheme()
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

        /// <summary>True when current VS theme is dark.</summary>
        public static bool IsDarkTheme()
        {
            return string.Equals(GetCurrentTheme(), CxAssistConstants.ThemeDark, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>File name for severity (e.g. SeverityLevel.Critical -> "critical.png").</summary>
        public static string GetSeverityIconFileName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious: return "malicious.png";
                case SeverityLevel.Critical: return "critical.png";
                case SeverityLevel.High: return "high.png";
                case SeverityLevel.Medium: return "medium.png";
                case SeverityLevel.Low:
                case SeverityLevel.Info: return "low.png";
                case SeverityLevel.Ok: return "ok.png";
                case SeverityLevel.Unknown: return "unknown.png";
                case SeverityLevel.Ignored: return "ignored.png";
                default: return "unknown.png";
            }
        }

        /// <summary>Base name for severity (for SVG: "critical", "malicious", etc.).</summary>
        public static string GetSeverityIconBaseName(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return "unknown";
            switch (severity.ToLowerInvariant())
            {
                case "malicious": return "malicious";
                case "critical": return "critical";
                case "high": return "high";
                case "medium": return "medium";
                case "low":
                case "info": return "low";
                case "ok": return "ok";
                case "unknown": return "unknown";
                case "ignored": return "ignored";
                default: return "unknown";
            }
        }

        /// <summary>Loads a PNG icon from CxAssist Icons/{theme}/{fileName}. Returns null on failure.</summary>
        public static BitmapImage LoadPngIcon(string theme, string fileName)
        {
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/{IconsBasePath}/{theme}/{fileName}";
            try
            {
                var uri = new Uri(packPath, UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(uri);
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
                CxAssistErrorHandler.LogAndSwallow(ex, $"AssistIconLoader.LoadPngIcon (pack): {fileName}");
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
                CxAssistErrorHandler.LogAndSwallow(ex, $"AssistIconLoader.LoadPngIcon (manifest): {fileName}");
            }

            return null;
        }

        /// <summary>Loads a PNG icon for the given severity using current theme. Tries Light if Dark fails.</summary>
        public static BitmapImage LoadSeverityPngIcon(SeverityLevel severity)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconFileName(severity);
            var img = LoadPngIcon(theme, fileName);
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadPngIcon(CxAssistConstants.ThemeDark, fileName);
            return img;
        }

        /// <summary>Loads a PNG icon for the given severity string (e.g. "critical"). Use when you have string severity from tags.</summary>
        public static BitmapImage LoadSeverityPngIcon(string severity)
        {
            string theme = GetCurrentTheme();
            string fileName = GetSeverityIconBaseName(severity) + ".png";
            var img = LoadPngIcon(theme, fileName);
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadPngIcon(CxAssistConstants.ThemeDark, fileName);
            return img;
        }

        /// <summary>Loads an SVG icon from CxAssist Icons/{theme}/{iconName}.svg. iconName without extension.</summary>
        public static ImageSource LoadSvgIcon(string theme, string iconNameWithoutExtension)
        {
            string fileName = iconNameWithoutExtension.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                ? iconNameWithoutExtension
                : iconNameWithoutExtension + ".svg";
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/{IconsBasePath}/{theme}/{fileName}";
            try
            {
                var iconUri = new Uri(packPath, UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo?.Stream == null) return null;

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
                        drawingImage.Freeze();
                        return drawingImage;
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, $"AssistIconLoader.LoadSvgIcon: {fileName}");
            }
            return null;
        }

        /// <summary>Loads SVG for severity (e.g. "critical") using current theme.</summary>
        public static ImageSource LoadSeveritySvgIcon(string severity)
        {
            string theme = GetCurrentTheme();
            string baseName = GetSeverityIconBaseName(severity);
            var img = LoadSvgIcon(theme, baseName);
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadSvgIcon(CxAssistConstants.ThemeDark, baseName);
            return img;
        }

        /// <summary>Loads severity icon (JetBrains-style; prefers SVG, fallback PNG). Use for Quick Info and any UI that can show either.</summary>
        public static ImageSource LoadSeverityIcon(SeverityLevel severity)
        {
            string currentTheme = GetCurrentTheme();

            // Log theme change (aligned with JetBrains ProblemDescription.reloadIcons: "RTS: Icons reloading completed.")
            if (_lastKnownTheme != null && !string.Equals(_lastKnownTheme, currentTheme, StringComparison.OrdinalIgnoreCase))
            {
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.ICONS_RELOADING_FOR_THEME, _lastKnownTheme, currentTheme));
            }
            _lastKnownTheme = currentTheme;

            var img = LoadSeveritySvgIcon(severity.ToString());
            if (img != null)
            {
                if (!_popupIconsLogged)
                {
                    _popupIconsLogged = true;
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {string.Format(CxAssistConstants.ICONS_LOADED_FOR_THEME, currentTheme)}");
                }
                return img;
            }
            var png = LoadSeverityPngIcon(severity);
            return png;
        }

        /// <summary>Loads badge/logo PNG (e.g. CxAssistConstants.BadgeIconFileName).</summary>
        public static BitmapImage LoadBadgeIcon()
        {
            string theme = GetCurrentTheme();
            var img = LoadPngIcon(theme, CxAssistConstants.BadgeIconFileName);
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadPngIcon(CxAssistConstants.ThemeDark, CxAssistConstants.BadgeIconFileName);
            return img;
        }

        /// <summary>Loads the package/cube icon for OSS title row (JetBrains-style; prefers SVG, fallback PNG).</summary>
        public static ImageSource LoadPackageIcon()
        {
            string theme = GetCurrentTheme();
            var img = LoadSvgIcon(theme, "package");
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadSvgIcon(CxAssistConstants.ThemeDark, "package");
            if (img == null)
            {
                var png = LoadPngIcon(theme, "package.png");
                if (png == null && theme != CxAssistConstants.ThemeDark)
                    png = LoadPngIcon(CxAssistConstants.ThemeDark, "package.png");
                return png;
            }
            return img;
        }

        /// <summary>Loads the container/image icon for container scan title row (JetBrains card-containers graphic).</summary>
        public static ImageSource LoadContainerIcon()
        {
            string theme = GetCurrentTheme();
            var img = LoadSvgIcon(theme, "container");
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadSvgIcon(CxAssistConstants.ThemeDark, "container");
            return img;
        }

        /// <summary>Loads the star-action icon (JetBrains-style; used for fix/view/ignore actions).</summary>
        public static ImageSource LoadStarActionIcon()
        {
            string theme = GetCurrentTheme();
            var img = LoadSvgIcon(theme, "star-action");
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = LoadSvgIcon(CxAssistConstants.ThemeDark, "star-action");
            return img;
        }
    }
}
