using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Rich hover popup for DevAssist vulnerabilities
    /// Similar to JetBrains hover popup with custom formatting, icons, and links
    /// </summary>
    public partial class DevAssistHoverPopup : UserControl
    {
        private readonly Vulnerability _vulnerability;

        public DevAssistHoverPopup(Vulnerability vulnerability)
        {
            InitializeComponent();
            _vulnerability = vulnerability;
            PopulateContent();
        }

        private void PopulateContent()
        {
            // Set severity icon
            SetSeverityIcon();

            // Set title
            TitleText.Text = !string.IsNullOrEmpty(_vulnerability.Title)
                ? _vulnerability.Title
                : !string.IsNullOrEmpty(_vulnerability.RuleName)
                    ? _vulnerability.RuleName
                    : _vulnerability.Description;

            // Set scanner badge
            SetScannerBadge();

            // Set description
            DescriptionText.Text = _vulnerability.Description;

            // Set scanner-specific content
            SetScannerSpecificContent();

            // Set location
            var fileName = Path.GetFileName(_vulnerability.FilePath);
            LocationText.Text = $"{fileName}:{_vulnerability.LineNumber}:{_vulnerability.ColumnNumber}";

            // Wire up event handlers for links
            ViewDetailsLink.Click += ViewDetailsLink_Click;
            NavigateToCodeLink.Click += NavigateToCodeLink_Click;
            LearnMoreLink.Click += LearnMoreLink_Click;
            ApplyFixLink.Click += ApplyFixLink_Click;
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
                try
                {
                    // Determine theme (Dark/Light)
                    var theme = GetCurrentTheme();
                    var iconPath = $"pack://application:,,,/ast-visual-studio-extension;component/Resources/Icons/{theme}/{iconFileName}";
                    
                    SeverityIcon.Source = new BitmapImage(new Uri(iconPath));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load severity icon: {ex.Message}");
                }
            }
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

        private void ViewDetailsLink_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open DevAssist Findings Window and select this vulnerability
            System.Diagnostics.Debug.WriteLine($"View Details clicked for vulnerability: {_vulnerability.Id}");
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
                // This would update package.json/requirements.txt/etc with recommended version
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: Apply Fix for {_vulnerability.PackageName} -> {_vulnerability.RecommendedVersion}");

                // For now, just show a message
                // In the future, this would:
                // 1. Detect package manager (npm, pip, maven, etc.)
                // 2. Update the package file with recommended version
                // 3. Optionally run package manager update command
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist Hover: Error applying fix: {ex.Message}");
            }
        }
    }
}

