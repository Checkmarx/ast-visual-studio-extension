using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    /// <summary>
    /// This class is used to draw the result information panel
    /// </summary>
    internal class ResultInfoPanel : BasePanel
    {
        private Result result;

        public ResultInfoPanel(AsyncPackage package) : base(package) {}

        // Draw result information content
        public void Draw(Result result)
        {
            this.result = result;

            DrawTitle();
            DrawDesrciptionTab();
        }

        // Draw title
        private void DrawTitle()
        {
            Image severityIcon = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri(CxUtils.GetIconPathFromSeverity(result.Severity, true)));
            severityIcon.Source = bitmapImage;

            CxWindowControl cxWindowUI = GetCxWindowControl();

            cxWindowUI.ResultSeverityIcon.Source = bitmapImage;
            cxWindowUI.ResultTitle.Text = result.Data.QueryName ?? result.Id;
        }

        // Draw description tab
        private void DrawDesrciptionTab()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();

            cxWindowUI.ResultInfoStackPanel.Children.Clear();

            if (result.Description != null)
            {
                TextBlock descriptionTextBlock = new TextBlock
                {
                    Text = result.Description,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    FontSize = 13,
                    Margin = new Thickness(10, 5, 0, 0)
                };

                cxWindowUI.ResultInfoStackPanel.Children.Add(descriptionTextBlock);
            }

            if (result.Data.Value != null)
            {
                Run labelActualValue = new Run(CxConstants.DESC_TAB_LBL_ACTUAL_VALUE)
                {
                    FontWeight = FontWeights.Bold
                };

                TextBlock actualValueTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    FontSize = 13,
                    Margin = new Thickness(10, result.Description != null ? 30 : 5, 0, 0)
                };

                actualValueTextBlock.Inlines.Add(labelActualValue);
                actualValueTextBlock.Inlines.Add(new Run(result.Data.Value));
                
                cxWindowUI.ResultInfoStackPanel.Children.Add(actualValueTextBlock);

                Run labelExpectedValue = new Run(CxConstants.DESC_TAB_LBL_EXPECTED_VALUE)
                {
                    FontWeight = FontWeights.Bold
                };

                TextBlock expectedValueTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    FontSize = 13,
                    Margin = new Thickness(10, 15, 0, 0)
                };

                expectedValueTextBlock.Inlines.Add(labelExpectedValue);
                expectedValueTextBlock.Inlines.Add(new Run(result.Data.ExpectedValue));

                cxWindowUI.ResultInfoStackPanel.Children.Add(expectedValueTextBlock);
            }

            cxWindowUI.ResultTabControl.Visibility = Visibility.Visible;
            cxWindowUI.Separator.Visibility = Visibility.Visible;
        }

        // Clear panel
        public void Clear()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();

            if (cxWindowUI != null)
            {
                cxWindowUI.ResultSeverityIcon.Source = null;
                cxWindowUI.ResultTitle.Text = String.Empty;
                cxWindowUI.ResultInfoStackPanel.Children.Clear();
                cxWindowUI.Separator.Visibility = Visibility.Hidden;
                cxWindowUI.ResultTabControl.Visibility = Visibility.Hidden;
            }
        }
    }
}
