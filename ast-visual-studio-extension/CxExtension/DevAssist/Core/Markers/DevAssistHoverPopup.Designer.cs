using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Designer partial: declares XAML-named elements and loads the XAML from embedded resource
    /// so that the project compiles without relying on MSBuild-generated .g.cs.
    /// </summary>
    public partial class DevAssistHoverPopup
    {
        // Header: Checkmarx One Assist logo image (not text/label)
        internal Image HeaderLogoImage;
        internal Button MoreOptionsButton;
        // Main line
        internal Image SeverityIcon;
        internal TextBlock TitleText;
        internal Border ScannerBadge;
        internal TextBlock ScannerText;
        internal StackPanel SeverityCountPanel;
        // Description and panels
        internal TextBlock DescriptionText;
        internal StackPanel PackageInfoPanel;
        internal TextBlock PackageNameText;
        internal TextBlock CveText;
        internal TextBlock CvssScoreText;
        internal StackPanel RecommendedVersionPanel;
        internal TextBlock RecommendedVersionText;
        internal StackPanel RemediationPanel;
        internal TextBlock RemediationText;
        internal StackPanel IacValuesPanel;
        internal TextBlock ExpectedValueText;
        internal TextBlock ActualValueText;
        // Location and links
        internal TextBlock LocationTextBlock;
        internal Run LocationText;
        internal Border SeparatorLine1;
        internal Border SeparatorLine2;
        internal System.Windows.Documents.Hyperlink FixWithCxOneAssistLink;
        internal System.Windows.Documents.Hyperlink ViewDetailsLink;
        internal System.Windows.Documents.Hyperlink IgnoreThisLink;
        internal TextBlock IgnoreAllOfThisTypeBlock;
        internal System.Windows.Documents.Hyperlink IgnoreAllOfThisTypeLink;
        internal StackPanel SecondaryLinksPanel;
        internal System.Windows.Documents.Hyperlink NavigateToCodeLink;
        internal TextBlock LearnMoreLinkBlock;
        internal System.Windows.Documents.Hyperlink LearnMoreLink;
        internal TextBlock ApplyFixLinkBlock;
        internal System.Windows.Documents.Hyperlink ApplyFixLink;

        private void InitializeComponent()
        {
            const string resourceName = "ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers.DevAssistHoverPopup.xaml";
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssistHoverPopup: Embedded resource not found: {resourceName}");
                    return;
                }
                var root = (FrameworkElement)XamlReader.Load(stream);
                Content = root;
                HeaderLogoImage = (Image)root.FindName("HeaderLogoImage");
                MoreOptionsButton = (Button)root.FindName("MoreOptionsButton");
                SeverityIcon = (Image)root.FindName("SeverityIcon");
                TitleText = (TextBlock)root.FindName("TitleText");
                ScannerBadge = (Border)root.FindName("ScannerBadge");
                ScannerText = (TextBlock)root.FindName("ScannerText");
                SeverityCountPanel = (StackPanel)root.FindName("SeverityCountPanel");
                DescriptionText = (TextBlock)root.FindName("DescriptionText");
                PackageInfoPanel = (StackPanel)root.FindName("PackageInfoPanel");
                PackageNameText = (TextBlock)root.FindName("PackageNameText");
                CveText = (TextBlock)root.FindName("CveText");
                CvssScoreText = (TextBlock)root.FindName("CvssScoreText");
                RecommendedVersionPanel = (StackPanel)root.FindName("RecommendedVersionPanel");
                RecommendedVersionText = (TextBlock)root.FindName("RecommendedVersionText");
                RemediationPanel = (StackPanel)root.FindName("RemediationPanel");
                RemediationText = (TextBlock)root.FindName("RemediationText");
                IacValuesPanel = (StackPanel)root.FindName("IacValuesPanel");
                ExpectedValueText = (TextBlock)root.FindName("ExpectedValueText");
                ActualValueText = (TextBlock)root.FindName("ActualValueText");
                LocationTextBlock = (TextBlock)root.FindName("LocationTextBlock");
                LocationText = (Run)root.FindName("LocationText");
                SeparatorLine1 = (Border)root.FindName("SeparatorLine1");
                SeparatorLine2 = (Border)root.FindName("SeparatorLine2");
                FixWithCxOneAssistLink = (System.Windows.Documents.Hyperlink)root.FindName("FixWithCxOneAssistLink");
                ViewDetailsLink = (System.Windows.Documents.Hyperlink)root.FindName("ViewDetailsLink");
                IgnoreThisLink = (System.Windows.Documents.Hyperlink)root.FindName("IgnoreThisLink");
                IgnoreAllOfThisTypeBlock = (TextBlock)root.FindName("IgnoreAllOfThisTypeBlock");
                IgnoreAllOfThisTypeLink = (System.Windows.Documents.Hyperlink)root.FindName("IgnoreAllOfThisTypeLink");
                SecondaryLinksPanel = (StackPanel)root.FindName("SecondaryLinksPanel");
                NavigateToCodeLink = (System.Windows.Documents.Hyperlink)root.FindName("NavigateToCodeLink");
                LearnMoreLinkBlock = (TextBlock)root.FindName("LearnMoreLinkBlock");
                LearnMoreLink = (System.Windows.Documents.Hyperlink)root.FindName("LearnMoreLink");
                ApplyFixLinkBlock = (TextBlock)root.FindName("ApplyFixLinkBlock");
                ApplyFixLink = (System.Windows.Documents.Hyperlink)root.FindName("ApplyFixLink");
            }
        }
    }
}
