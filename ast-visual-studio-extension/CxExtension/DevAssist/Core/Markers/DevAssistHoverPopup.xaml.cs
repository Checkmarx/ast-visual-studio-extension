using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Rich hover popup for DevAssist vulnerabilities
    /// Similar to JetBrains hover popup: DevAssist logo, severity icon, badge, description, severity counts, links
    /// </summary>
    public partial class DevAssistHoverPopup : UserControl
    {
        private readonly Vulnerability _vulnerability;
        private readonly IReadOnlyList<Vulnerability> _allForLine;
        private readonly IReadOnlyList<string> _compilerErrorsOnLine;

        public DevAssistHoverPopup(Vulnerability vulnerability)
            : this(vulnerability, new[] { vulnerability }, null)
        {
        }

        public DevAssistHoverPopup(Vulnerability first, IReadOnlyList<Vulnerability> allForLine)
            : this(first, allForLine, null)
        {
        }

        public DevAssistHoverPopup(Vulnerability first, IReadOnlyList<Vulnerability> allForLine, IReadOnlyList<string> compilerErrorsOnLine)
        {
            InitializeComponent();
            _vulnerability = first ?? throw new ArgumentNullException(nameof(first));
            _allForLine = allForLine ?? new[] { first };
            _compilerErrorsOnLine = compilerErrorsOnLine ?? Array.Empty<string>();
            PopulateContent();
        }

        private void PopulateContent()
        {
            if (TitleText == null)
                return; // XAML failed to load from embedded resource

            // JetBrains-style: when multiple vulnerabilities on same line, show "N issues detected" + one card per vulnerability
            if (_allForLine != null && _allForLine.Count > 1 && MultipleIssuesPanel != null && MultipleIssuesCards != null)
            {
                BuildMultipleIssuesContent();
                return;
            }

            if (SingleIssuePanel != null)
                SingleIssuePanel.Visibility = Visibility.Visible;
            if (MultipleIssuesPanel != null)
                MultipleIssuesPanel.Visibility = Visibility.Collapsed;

            // DevAssist logo (JetBrains-style at top of tooltip)
            SetDevAssistIcon();

            // Set severity icon
            SetSeverityIcon();

            // Severity count row when multiple findings on same line (JetBrains buildVulnerabilitySection)
            BuildSeverityCountRow();

            // Set title: JetBrains-style "Package@Version - Title" when package present (e.g. node-ipc@10.1.1 - Malicious Package)
            string titlePart = !string.IsNullOrEmpty(_vulnerability.Title)
                ? _vulnerability.Title
                : !string.IsNullOrEmpty(_vulnerability.RuleName)
                    ? _vulnerability.RuleName
                    : _vulnerability.Description;
            if (!string.IsNullOrEmpty(_vulnerability.PackageName))
            {
                var version = !string.IsNullOrEmpty(_vulnerability.PackageVersion) ? _vulnerability.PackageVersion : "";
                TitleText.Text = $"{_vulnerability.PackageName}@{version} - {titlePart}";
            }
            else
                TitleText.Text = titlePart ?? "";

            // Set scanner badge (show as pill below title when used)
            SetScannerBadge();
            if (!string.IsNullOrEmpty(ScannerText.Text))
                ScannerBadge.Visibility = Visibility.Visible;

            // Set description (static fallback when empty)
            DescriptionText.Text = !string.IsNullOrEmpty(_vulnerability.Description)
                ? _vulnerability.Description
                : "Vulnerability detected by " + _vulnerability.Scanner + ".";

            // Set scanner-specific content
            SetScannerSpecificContent();

            // Set location
            var fileName = !string.IsNullOrEmpty(_vulnerability.FilePath) ? Path.GetFileName(_vulnerability.FilePath) : "file";
            LocationText.Text = $"{fileName}:{_vulnerability.LineNumber}:{_vulnerability.ColumnNumber}";

            // JetBrains-style action links (static handlers for now)
            FixWithCxOneAssistLink.Click += FixWithCxOneAssistLink_Click;
            ViewDetailsLink.Click += ViewDetailsLink_Click;
            IgnoreThisLink.Click += IgnoreThisLink_Click;
            IgnoreAllOfThisTypeLink.Click += IgnoreAllOfThisTypeLink_Click;
            LearnMoreLink.Click += LearnMoreLink_Click;
            ApplyFixLink.Click += ApplyFixLink_Click;

            // Mitigation 4: PreviewMouseDown fires before tooltip dismisses on mouse move, so links work reliably.
            FixWithCxOneAssistLink.PreviewMouseDown += (s, e) => FixWithCxOneAssistLink_Click(s, e);
            ViewDetailsLink.PreviewMouseDown += (s, e) => ViewDetailsLink_Click(s, e);
            IgnoreThisLink.PreviewMouseDown += (s, e) => IgnoreThisLink_Click(s, e);
            IgnoreAllOfThisTypeLink.PreviewMouseDown += (s, e) => IgnoreAllOfThisTypeLink_Click(s, e);
            LearnMoreLink.PreviewMouseDown += (s, e) => LearnMoreLink_Click(s, e);
            ApplyFixLink.PreviewMouseDown += (s, e) => ApplyFixLink_Click(s, e);

            // Show "Ignore all of this type" for OSS and Containers (like JetBrains)
            if (_vulnerability.Scanner == ScannerType.OSS || _vulnerability.Scanner == ScannerType.Containers)
                IgnoreAllOfThisTypeBlock.Visibility = Visibility.Visible;

            if (MoreOptionsButton != null)
                MoreOptionsButton.Click += MoreOptionsButton_Click;

            // Combined: show compiler/VS errors on this line in the same popup
            BuildCompilerErrorsSection();

            // Apply VS theme so popup respects light/dark (was always dark due to hardcoded XAML colors)
            ApplyVsTheme();
        }

        /// <summary>Applies Visual Studio theme (light/dark) so popup is not always black in light theme.</summary>
        private void ApplyVsTheme()
        {
            try
            {
                var root = Content as Border;
                if (root != null)
                {
                    root.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.ToolTipBrushKey);
                    root.SetResourceReference(Border.BorderBrushProperty, EnvironmentColors.ToolTipBorderBrushKey);
                }
                SetResourceRef(TitleText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(DescriptionText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(LocationTextBlock, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(LocationText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(PackageNameText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(CveText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(CvssScoreText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(RecommendedVersionText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(RemediationText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(ExpectedValueText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(ActualValueText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                SetResourceRef(FixWithCxOneAssistLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(ViewDetailsLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(IgnoreThisLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(IgnoreAllOfThisTypeLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(NavigateToCodeLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(LearnMoreLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                SetResourceRef(ApplyFixLink, TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                if (MultipleIssuesHeader != null) SetResourceRef(MultipleIssuesHeader, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                if (ScannerText != null) SetResourceRef(ScannerText, TextElement.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                if (SeparatorLine1 != null) SeparatorLine1.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.ToolTipBorderBrushKey);
                if (SeparatorLine2 != null) SeparatorLine2.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.ToolTipBorderBrushKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: ApplyVsTheme failed: {ex.Message}");
            }
        }

        private static void SetResourceRef(DependencyObject element, DependencyProperty property, object resourceKey)
        {
            if (element == null) return;
            if (element is FrameworkElement fe)
                fe.SetResourceReference(property, resourceKey);
            else if (element is FrameworkContentElement fce)
                fce.SetResourceReference(property, resourceKey);
        }

        private void SetScannerSpecificContent()
        {
            switch (_vulnerability.Scanner)
            {
                case ScannerType.OSS:
                    SetOssContent();
                    break;
                case ScannerType.ASCA:
                    SetAscaContent();
                    break;
                case ScannerType.IaC:
                    SetIacContent();
                    break;
                case ScannerType.Secrets:
                    SetSecretsContent();
                    break;
                case ScannerType.Containers:
                    SetContainersContent();
                    break;
            }
        }

        private void SetOssContent()
        {
            // Show package information for OSS/SCA vulnerabilities
            if (!string.IsNullOrEmpty(_vulnerability.PackageName))
            {
                PackageInfoPanel.Visibility = Visibility.Visible;
                PackageNameText.Text = $"{_vulnerability.PackageName}@{_vulnerability.PackageVersion}";

                // Show CVE if available
                if (!string.IsNullOrEmpty(_vulnerability.CveName))
                {
                    CveText.Text = $"CVE: {_vulnerability.CveName}";
                    CveText.Visibility = Visibility.Visible;
                }

                // Show CVSS score if available
                if (_vulnerability.CvssScore.HasValue)
                {
                    CvssScoreText.Text = $"CVSS Score: {_vulnerability.CvssScore.Value:F1}";
                    CvssScoreText.Visibility = Visibility.Visible;
                }

                // Show recommended version if available
                if (!string.IsNullOrEmpty(_vulnerability.RecommendedVersion))
                {
                    RecommendedVersionPanel.Visibility = Visibility.Visible;
                    RecommendedVersionText.Text = _vulnerability.RecommendedVersion;

                    // Show "Apply Fix" link
                    ApplyFixLinkBlock.Visibility = Visibility.Visible;
                }
            }

            // Show "Learn More" link if available
            if (!string.IsNullOrEmpty(_vulnerability.LearnMoreUrl) || !string.IsNullOrEmpty(_vulnerability.FixLink))
            {
                LearnMoreLinkBlock.Visibility = Visibility.Visible;
            }
        }

        private void SetAscaContent()
        {
            // Show remediation advice if available for ASCA/SAST vulnerabilities
            if (!string.IsNullOrEmpty(_vulnerability.RemediationAdvice))
            {
                RemediationPanel.Visibility = Visibility.Visible;
                RemediationText.Text = _vulnerability.RemediationAdvice;
            }

            // Show "Learn More" link if available
            if (!string.IsNullOrEmpty(_vulnerability.LearnMoreUrl))
            {
                LearnMoreLinkBlock.Visibility = Visibility.Visible;
            }
        }

        private void SetIacContent()
        {
            // Show expected vs actual values for IaC/KICS vulnerabilities
            if (!string.IsNullOrEmpty(_vulnerability.ExpectedValue) || !string.IsNullOrEmpty(_vulnerability.ActualValue))
            {
                IacValuesPanel.Visibility = Visibility.Visible;
                ExpectedValueText.Text = _vulnerability.ExpectedValue ?? "N/A";
                ActualValueText.Text = _vulnerability.ActualValue ?? "N/A";
            }

            // Show "Learn More" link if available
            if (!string.IsNullOrEmpty(_vulnerability.LearnMoreUrl))
            {
                LearnMoreLinkBlock.Visibility = Visibility.Visible;
            }
        }

        private void SetSecretsContent()
        {
            // For secrets, we might want to show the secret type
            if (!string.IsNullOrEmpty(_vulnerability.SecretType))
            {
                // Could add a SecretTypePanel in XAML if needed
            }

            // Show "Learn More" link if available
            if (!string.IsNullOrEmpty(_vulnerability.LearnMoreUrl))
            {
                LearnMoreLinkBlock.Visibility = Visibility.Visible;
            }
        }

        private void SetContainersContent()
        {
            // For containers, similar to OSS - show package/image information
            if (!string.IsNullOrEmpty(_vulnerability.PackageName))
            {
                PackageInfoPanel.Visibility = Visibility.Visible;
                PackageNameText.Text = $"{_vulnerability.PackageName}@{_vulnerability.PackageVersion}";

                // Show CVE if available
                if (!string.IsNullOrEmpty(_vulnerability.CveName))
                {
                    CveText.Text = $"CVE: {_vulnerability.CveName}";
                    CveText.Visibility = Visibility.Visible;
                }

                // Show CVSS score if available
                if (_vulnerability.CvssScore.HasValue)
                {
                    CvssScoreText.Text = $"CVSS Score: {_vulnerability.CvssScore.Value:F1}";
                    CvssScoreText.Visibility = Visibility.Visible;
                }
            }

            // Show "Learn More" link if available
            if (!string.IsNullOrEmpty(_vulnerability.LearnMoreUrl))
            {
                LearnMoreLinkBlock.Visibility = Visibility.Visible;
            }
        }

        private void SetSeverityIcon()
        {
            string iconFileName = GetSeverityIconFileName(_vulnerability.Severity);
            if (!string.IsNullOrEmpty(iconFileName))
            {
                var theme = GetCurrentTheme();
                var source = LoadIconFromAssembly(theme, iconFileName);
                if (source != null)
                    SeverityIcon.Source = source;
            }
        }

        /// <summary>Sets the header to the Checkmarx One Assist logo image (not text/label).</summary>
        private void SetDevAssistIcon()
        {
            if (HeaderLogoImage == null) return;
            var theme = GetCurrentTheme();
            var source = LoadIconFromAssembly(theme, "cxone_assist.png");
            if (source != null)
                HeaderLogoImage.Source = source;
        }

        /// <summary>
        /// Loads a PNG from DevAssist Icons so badge/severity icons show when hosted in VS
        /// (pack URI alone can fail; fallback to manifest resource stream).
        /// </summary>
        private static BitmapImage LoadIconFromAssembly(string theme, string fileName)
        {
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/DevAssist/Icons/{theme}/{fileName}";
            try
            {
                var streamInfo = System.Windows.Application.GetResourceStream(new Uri(packPath, UriKind.Absolute));
                if (streamInfo != null && streamInfo.Stream != null)
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
                System.Diagnostics.Debug.WriteLine($"DevAssist hover: pack URI load failed for {fileName}: {ex.Message}");
            }

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Replace('\\', '/').EndsWith($"DevAssist/Icons/{theme}/{fileName}", StringComparison.OrdinalIgnoreCase)
                                      || n.Replace('\\', '.').EndsWith($"DevAssist.Icons.{theme}.{fileName}", StringComparison.OrdinalIgnoreCase));
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
                System.Diagnostics.Debug.WriteLine($"DevAssist hover: manifest load failed for {fileName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Same as LoadIconFromAssembly but for severity_count subfolder (e.g. severity_count/critical.png).
        /// </summary>
        private static BitmapImage LoadSeverityCountIcon(string theme, string fileName)
        {
            var packPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/DevAssist/Icons/{theme}/severity_count/{fileName}";
            try
            {
                var streamInfo = System.Windows.Application.GetResourceStream(new Uri(packPath, UriKind.Absolute));
                if (streamInfo != null && streamInfo.Stream != null)
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
                System.Diagnostics.Debug.WriteLine($"DevAssist hover: pack URI load failed for severity_count/{fileName}: {ex.Message}");
            }

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var subPath = $"severity_count/{fileName}";
                var resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.Replace('\\', '/').EndsWith($"DevAssist/Icons/{theme}/{subPath}", StringComparison.OrdinalIgnoreCase)
                                      || n.Replace('\\', '.').EndsWith($"DevAssist.Icons.{theme}.severity_count.{fileName}", StringComparison.OrdinalIgnoreCase));
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
                System.Diagnostics.Debug.WriteLine($"DevAssist hover: manifest load failed for severity_count/{fileName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Builds severity count row (icon + number per severity) when multiple findings on same line, like JetBrains buildVulnerabilitySection.
        /// Uses severity_count icons from JetBrains (critical, high, medium, low).
        /// </summary>
        private void BuildSeverityCountRow()
        {
            if (_allForLine == null || _allForLine.Count <= 1)
                return;

            var counts = _allForLine
                .GroupBy(v => v.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            var theme = GetCurrentTheme();

            // Order: Malicious, Critical, High, Medium, Low (JetBrains order; severity_count has critical/high/medium/low)
            var severitiesToShow = new[] { SeverityLevel.Malicious, SeverityLevel.Critical, SeverityLevel.High, SeverityLevel.Medium, SeverityLevel.Low };
            foreach (var severity in severitiesToShow)
            {
                if (!counts.TryGetValue(severity, out int count) || count <= 0)
                    continue;

                string fileName = GetSeverityCountIconFileName(severity);
                if (string.IsNullOrEmpty(fileName))
                    continue;

                var iconSource = LoadSeverityCountIcon(theme, fileName);
                if (iconSource == null)
                    continue;

                try
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 0) };
                    var img = new Image
                    {
                        Source = iconSource,
                        Width = 16,
                        Height = 16,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 2, 0)
                    };
                    var tb = new TextBlock
                    {
                        Text = count.ToString(),
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    try
                    {
                        tb.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
                    }
                    catch
                    {
                        tb.Foreground = Brushes.Gray;
                    }
                    panel.Children.Add(img);
                    panel.Children.Add(tb);
                    SeverityCountPanel.Children.Add(panel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add severity count for {severity}: {ex.Message}");
                }
            }

            if (SeverityCountPanel.Children.Count > 0)
                SeverityCountPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// JetBrains-style: "N issues detected on this line Checkmarx One Assist" plus one card per vulnerability
        /// (each card: severity icon, title, description, Fix / View details / Ignore this).
        /// </summary>
        private void BuildMultipleIssuesContent()
        {
            SetDevAssistIcon();
            SingleIssuePanel.Visibility = Visibility.Collapsed;
            MultipleIssuesPanel.Visibility = Visibility.Visible;

            int n = _allForLine.Count;
            MultipleIssuesHeader.Text = $"{n} issue{(n == 1 ? "" : "s")} detected on this line Checkmarx One Assist";
            try
            {
                MultipleIssuesHeader.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey);
            }
            catch { /* theme not available */ }

            MultipleIssuesCards.Children.Clear();
            foreach (var v in _allForLine)
                AddVulnerabilityCard(v);

            if (MoreOptionsButton != null)
                MoreOptionsButton.Click += MoreOptionsButton_Click;
            BuildCompilerErrorsSection();
            ApplyVsTheme();
        }

        /// <summary>
        /// Shows "Also on this line (Compiler / VS):" with Error List messages when there are compiler errors on the same line.
        /// </summary>
        private void BuildCompilerErrorsSection()
        {
            try
            {
                if (_compilerErrorsOnLine == null || _compilerErrorsOnLine.Count == 0 ||
                    CompilerErrorsPanel == null || CompilerErrorsList == null)
                    return;

                CompilerErrorsList.Children.Clear();
                foreach (string message in _compilerErrorsOnLine)
                {
                    var tb = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    try { tb.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey); }
                    catch { tb.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)); }
                    CompilerErrorsList.Children.Add(tb);
                }
                if (CompilerErrorsTitle != null)
                {
                    try { CompilerErrorsTitle.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey); }
                    catch { }
                }
                CompilerErrorsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: BuildCompilerErrorsSection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds one vulnerability card (severity icon, title, description, action links) to MultipleIssuesCards.
        /// </summary>
        private void AddVulnerabilityCard(Vulnerability v)
        {
            // Separator between cards (skip before first card)
            if (MultipleIssuesCards.Children.Count > 0)
            {
                var sep = new Border { Height = 1, Margin = new Thickness(0, 0, 0, 8) };
                try { sep.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.ToolTipBorderBrushKey); }
                catch { sep.Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)); }
                MultipleIssuesCards.Children.Add(sep);
            }

            var theme = GetCurrentTheme();
            string iconFileName = GetSeverityIconFileName(v.Severity);
            var iconSource = !string.IsNullOrEmpty(iconFileName) ? LoadIconFromAssembly(theme, iconFileName) : null;

            var card = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            // Row: severity icon + title (bold)
            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (iconSource != null)
            {
                var img = new Image
                {
                    Source = iconSource,
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(img, 0);
                titleRow.Children.Add(img);
            }
            string titlePart = !string.IsNullOrEmpty(v.Title) ? v.Title : (!string.IsNullOrEmpty(v.RuleName) ? v.RuleName : v.Description);
            var titleBlock = new TextBlock
            {
                Text = titlePart ?? "",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            try { titleBlock.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey); }
            catch { titleBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)); }
            Grid.SetColumn(titleBlock, 1);
            titleRow.Children.Add(titleBlock);
            card.Children.Add(titleRow);

            // Description
            var descText = !string.IsNullOrEmpty(v.Description) ? v.Description : "Vulnerability detected by " + v.Scanner + ".";
            if (v.Scanner == ScannerType.IaC)
                descText += " IaC vulnerability";
            else if (v.Scanner == ScannerType.ASCA)
                descText += " SAST vulnerability";
            var descBlock = new TextBlock
            {
                Text = descText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 6)
            };
            try { descBlock.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.ToolTipTextBrushKey); }
            catch { descBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)); }
            card.Children.Add(descBlock);

            // Action links: Fix with Checkmarx One Assist | View details | Ignore this vulnerability
            var linksBlock = new TextBlock { Margin = new Thickness(0, 0, 0, 0) };
            var fixLink = new System.Windows.Documents.Hyperlink(new Run("Fix with Checkmarx One Assist")) { Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x94, 0xFF)) };
            fixLink.Click += (s, e) => DoFixWithCxOneAssist(v);
            fixLink.PreviewMouseDown += (s, e) => DoFixWithCxOneAssist(v);
            var viewLink = new System.Windows.Documents.Hyperlink(new Run("View details")) { Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x94, 0xFF)) };
            viewLink.Click += (s, e) => DoViewDetails(v);
            viewLink.PreviewMouseDown += (s, e) => DoViewDetails(v);
            var ignoreLink = new System.Windows.Documents.Hyperlink(new Run("Ignore this vulnerability")) { Foreground = new SolidColorBrush(Color.FromRgb(0x37, 0x94, 0xFF)) };
            ignoreLink.Click += (s, e) => DoIgnoreThis(v);
            ignoreLink.PreviewMouseDown += (s, e) => DoIgnoreThis(v);
            try
            {
                fixLink.SetResourceReference(TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                viewLink.SetResourceReference(TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
                ignoreLink.SetResourceReference(TextElement.ForegroundProperty, EnvironmentColors.ControlLinkTextBrushKey);
            }
            catch { }
            linksBlock.Inlines.Add(fixLink);
            linksBlock.Inlines.Add(new Run("  "));
            linksBlock.Inlines.Add(viewLink);
            linksBlock.Inlines.Add(new Run("  "));
            linksBlock.Inlines.Add(ignoreLink);
            card.Children.Add(linksBlock);

            MultipleIssuesCards.Children.Add(card);
        }

        private void SetScannerBadge()
        {
            // Set scanner text
            ScannerText.Text = _vulnerability.Scanner.ToString().ToUpper();

            // Set scanner badge color based on scanner type
            var badgeColor = GetScannerBadgeColor(_vulnerability.Scanner);
            ScannerBadge.Background = new SolidColorBrush(badgeColor);
        }

        private string GetSeverityIconFileName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious:
                    return "malicious.png";
                case SeverityLevel.Critical:
                    return "critical.png";
                case SeverityLevel.High:
                    return "high.png";
                case SeverityLevel.Medium:
                    return "medium.png";
                case SeverityLevel.Low:
                case SeverityLevel.Info:
                    return "low.png";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns severity_count icon filename for the given severity (C# 7.3 compatible).
        /// </summary>
        private static string GetSeverityCountIconFileName(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious:
                    return "critical.png"; // use critical icon for malicious (no separate severity_count in JB)
                case SeverityLevel.Critical:
                    return "critical.png";
                case SeverityLevel.High:
                    return "high.png";
                case SeverityLevel.Medium:
                    return "medium.png";
                case SeverityLevel.Low:
                    return "low.png";
                default:
                    return null;
            }
        }

        private Color GetScannerBadgeColor(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.ASCA:
                    return Color.FromRgb(0, 122, 204); // Blue for ASCA/SAST
                case ScannerType.OSS:
                    return Color.FromRgb(16, 124, 16); // Green for OSS/SCA
                case ScannerType.IaC:
                    return Color.FromRgb(156, 39, 176); // Purple for IaC/KICS
                case ScannerType.Secrets:
                    return Color.FromRgb(211, 47, 47); // Red for Secrets
                case ScannerType.Containers:
                    return Color.FromRgb(255, 140, 0); // Orange for Containers
                default:
                    return Color.FromRgb(117, 117, 117); // Gray
            }
        }

        private string GetCurrentTheme()
        {
            // Simple theme detection based on background color
            // In a real implementation, you'd use VS theme service
            return "Dark"; // Default to Dark for now
        }

        private void DoFixWithCxOneAssist(Vulnerability v)
        {
            if (v == null) return;
            System.Diagnostics.Debug.WriteLine($"Fix with Checkmarx One Assist for: {v.Id}");
            MessageBox.Show(
                $"Fix with Checkmarx One Assist\nVulnerability: {v.Title}\nID: {v.Id}",
                "DevAssist",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DoViewDetails(Vulnerability v)
        {
            if (v == null) return;
            System.Diagnostics.Debug.WriteLine($"View details clicked for vulnerability: {v.Id}");
            MessageBox.Show(
                $"{v.Title}\n\n{v.Description}\n\nScanner: {v.Scanner} | Severity: {v.Severity}",
                "View details",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DoIgnoreThis(Vulnerability v)
        {
            if (v == null) return;
            System.Diagnostics.Debug.WriteLine($"Ignore this vulnerability: {v.Id}");
            MessageBox.Show(
                $"Ignore this vulnerability: {v.Title}\n(Static demo – ignore not persisted yet)",
                "DevAssist",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void FixWithCxOneAssistLink_Click(object sender, RoutedEventArgs e)
        {
            DoFixWithCxOneAssist(_vulnerability);
        }

        private void ViewDetailsLink_Click(object sender, RoutedEventArgs e)
        {
            DoViewDetails(_vulnerability);
        }

        private void IgnoreThisLink_Click(object sender, RoutedEventArgs e)
        {
            DoIgnoreThis(_vulnerability);
        }

        private void IgnoreAllOfThisTypeLink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Ignore all of this type: {_vulnerability.Id}");
            MessageBox.Show(
                $"Ignore all of this type: {_vulnerability.Title}\n(Static demo – ignore not persisted yet)",
                "DevAssist",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void NavigateToCodeLink_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to the vulnerability location in code
            System.Diagnostics.Debug.WriteLine($"Navigate to Code clicked for vulnerability: {_vulnerability.Id}");
        }

        private void LearnMoreLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open external URL in default browser
                var url = !string.IsNullOrEmpty(_vulnerability.LearnMoreUrl)
                    ? _vulnerability.LearnMoreUrl
                    : _vulnerability.FixLink;

                if (!string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: Error opening Learn More link: {ex.Message}");
            }
        }

        private void ApplyFixLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Implement auto-remediation for SCA packages
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: Apply Fix for {_vulnerability.PackageName} -> {_vulnerability.RecommendedVersion}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: Error applying fix: {ex.Message}");
            }
        }

        /// <summary>JetBrains-style header ellipsis: more options for the popup.</summary>
        private void MoreOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"DevAssist Hover: More options for {_vulnerability.Id}");
            MessageBox.Show("More actions: View details, Ignore, Navigate to Code, Learn More, Apply Fix.", "Checkmarx One Assist", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}

