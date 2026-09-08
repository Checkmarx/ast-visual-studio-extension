using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using static System.Net.Mime.MediaTypeNames;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    internal class ResultVulnerabilitiesPanel
    {
        private readonly CxWindowControl cxWindowUI;
        private List<LearnMore> learnMore;
        public ResultVulnerabilitiesPanel(AsyncPackage package, CxWindowControl cxWindow)
        {
            cxWindowUI = cxWindow;

            SolutionExplorerUtils.AsyncPackage = package;
        }

        private Result result;
        private List<Node> nodes;
        private List<PackageData> packageDataList;

        /// <summary>
        /// Draw result vulnerabilities panel
        /// </summary>
        /// <param name="result"></param>
        public void Draw(Result result)
        {
            this.result = result;
            nodes = result.Data.Nodes ?? new List<Node>();
            packageDataList = result.Data.PackageData ?? new List<PackageData>();

            cxWindowUI.VulnerabilitiesList.Items.Clear();

            switch (EngineTypeExtensions.FromEngineString(result.Type))
            {
                case EngineType.SAST:
                    cxWindowUI.LearnMorePanelTitle.Children.Clear();
                    cxWindowUI.RemediationPanelTitle.Children.Clear();
                    BuildAttackVectorPanel();
                    cxWindowUI.SastVulnerabilitiesPanel.Visibility = Visibility.Visible;
                    cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Hidden;
                    learnMore = null;
                    cxWindowUI.VulnerabilitiesTabItem.Visibility = Visibility.Visible;
                    cxWindowUI.VulnerabilitiesTabItem.IsSelected = true;
                    break;
                case EngineType.SCA:
                    BuildPackageDataPanel();
                    cxWindowUI.SastVulnerabilitiesPanel.Visibility = Visibility.Hidden;
                    cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Visible;
                    break;
                case EngineType.KICS:
                    BuildVulnerabilityLocation();
                    cxWindowUI.SastVulnerabilitiesPanel.Visibility = Visibility.Hidden;
                    cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Visible;
                    break;
                case EngineType.SCS_SECRET_DETECTION:
                    cxWindowUI.LearnMorePanelTitle.Children.Clear();
                    cxWindowUI.RemediationPanelTitle.Children.Clear();
                    BuildAttackVectorPanel();
                    cxWindowUI.VulnerabilitiesTabItem.Visibility = Visibility.Collapsed;
                    cxWindowUI.TriageSeverityCombobox.Visibility= Visibility.Collapsed;
                    cxWindowUI.SastVulnerabilitiesPanel.Visibility = Visibility.Visible;
                    learnMore = null;
                    cxWindowUI.LearnMore.IsSelected = true;
                    cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Hidden;
                    SecretDetectionDetailsLearnMoreTab();
                    break;
            }
        }

        /// <summary>
        /// Build attack vector panel for sast results
        /// </summary>
        private void BuildAttackVectorPanel()
        {
            ListView vulnerabilitiesListSast = cxWindowUI.VulnerabilitiesListSast;
            vulnerabilitiesListSast.Items.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                string itemName = string.Format(CxConstants.LBL_ATTACK_VECTOR_ITEM, i+1, node.Name);

                ListViewItem item = new ListViewItem();

                TextBlock tb = new TextBlock();
                tb.Inlines.Add(itemName);
                Hyperlink link = new Hyperlink();
                link.Inlines.Add(CxUtils.CapToLen(node.FileName));
                link.ToolTip = node.FileName;
                link.Click += new RoutedEventHandler(SolutionExplorerUtils.OpenFileAsync);
                tb.Inlines.Add(link);
                item.Tag = FileNode.Builder().WithFileName(node.FileName).WithLine(node.Line).WithColumn(node.Column);
                item.Content = tb;

                vulnerabilitiesListSast.Items.Add(item);
            }
        }

        /// <summary>
        /// Build package data panel for sca results
        /// </summary>
        private void BuildPackageDataPanel()
        {
            cxWindowUI.VulnerabilitiesPanelTitle.Text = CxConstants.LBL_PACKAGE_DATA;

            ListView vulnerabilitiesList = cxWindowUI.VulnerabilitiesList;
           
            if (packageDataList.Count == 0)
            {
                TextBlock tb = new TextBlock();
                tb.Inlines.Add(CxConstants.NO_INFORMATION);
                ListViewItem item = new ListViewItem();
                item.Content = tb;
                vulnerabilitiesList.Items.Add(item);
                return;
            }

            for (int i = 0; i < packageDataList.Count; i++)
            {
                PackageData packageData = packageDataList[i];

                string itemName = string.Format(CxConstants.LBL_ATTACK_VECTOR_ITEM, i + 1, packageData.Type);

                ListViewItem item = new ListViewItem();

                TextBlock tb = new TextBlock();
                tb.Inlines.Add(itemName);
                Hyperlink link = new Hyperlink
                {
                    NavigateUri = new System.Uri(packageData.Url)
                };
                link.Inlines.Add(packageData.Url);
                link.RequestNavigate += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                    e.Handled = true;
                };

                tb.Inlines.Add(link);
                item.Content = tb;

                vulnerabilitiesList.Items.Add(item);

            }
        }

        /// <summary>
        /// Build location panel for kics results
        /// </summary>
        private void BuildVulnerabilityLocation()
        {
            cxWindowUI.VulnerabilitiesPanelTitle.Text = CxConstants.LBL_LOCATION;

            ListViewItem item = new ListViewItem();

            TextBlock tb = new TextBlock();
            tb.Inlines.Add(CxConstants.LBL_LOCATION_FILE);
            Hyperlink link = new Hyperlink();
            link.Inlines.Add(result.Data.FileName);
            link.Click += new RoutedEventHandler(SolutionExplorerUtils.OpenFileAsync);
            tb.Inlines.Add(link);
            item.Tag = FileNode.Builder().WithFileName(result.Data.FileName).WithLine(result.Data.Line).WithColumn(1);
            item.Content = tb;

            ListView vulnerabilitiesList = cxWindowUI.VulnerabilitiesList;
            vulnerabilitiesList.Items.Add(item);
        }


        /// <summary>
        /// Fill Learn More and Remediation Examples tab
        /// </summary>
        /// <param name="cxToolbar"></param>
        public async Task LearnMoreAndRemediationAsync(CxToolbar cxToolbar)
        {
            if (learnMore != null) return;

            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());

            ClearPanels();

            if (cxWrapper == null)
            {
                ShowNoInformation();
                return;
            }

            if (result.Type == EngineTypeExtensions.ToEngineString(EngineType.SCS_SECRET_DETECTION))
            {
                SecretDetectionDetailsLearnMoreTab(); 
                SecretDetectionDetailsRemediationTab();
                return;
            }

            ShowLoading();

            await Task.Run(() =>
            {
                try
                {
                    learnMore = cxWrapper.LearnMoreAndRemediation(result.Data.QueryId);
                }
                catch (Exception ex)
                {
                    CxUtils.DisplayMessageInInfoBar(cxToolbar.Package, string.Format(CxConstants.ERROR_GETTING_LEARNMORE, ex.Message), KnownMonikers.StatusError);
                }
            });

            ClearPanels();

            if (learnMore == null || learnMore.Count == 0)
            {
                ShowNoInformation();
                learnMore = new List<LearnMore>();
                return;
            }

            foreach (var learnInfo in learnMore)
            {
                AddSectionWithText(cxWindowUI.LearnMorePanelTitle, CxConstants.RISK, learnInfo.risk);
                AddSectionWithText(cxWindowUI.LearnMorePanelTitle, CxConstants.CAUSE, learnInfo.cause);
                AddSectionWithText(cxWindowUI.LearnMorePanelTitle, CxConstants.GENERAL_RECOMENDATIONS, learnInfo.generalRecommendations);

                if (learnInfo.samples == null || learnInfo.samples.Count == 0)
                {
                    AddTextWithTitle(cxWindowUI.RemediationPanelTitle, CxConstants.NO_INFORMATION);
                    continue;
                }

                foreach (var sample in learnInfo.samples)
                {
                    AddSectionTitle(cxWindowUI.RemediationPanelTitle,
                        string.Format(CxConstants.CODE_SAMPLE_TITLE, sample.title, sample.progLanguage));

                    TextBox codeTextBox = new TextBox
                    {
                        Text = sample.code.Trim(),
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.WrapWithOverflow,
                        Margin = new Thickness(10, 5, 0, 0)
                    };
                    cxWindowUI.RemediationPanelTitle.Children.Add(codeTextBox);
                    AddTextWithTitle(cxWindowUI.RemediationPanelTitle, string.Empty);
                }
            }
        }

        private void ClearPanels()
        {
            cxWindowUI.LearnMorePanelTitle.Children.Clear();
            cxWindowUI.RemediationPanelTitle.Children.Clear();
        }

        private void ShowNoInformation()
        {
            AddTextWithTitle(cxWindowUI.LearnMorePanelTitle, CxConstants.NO_INFORMATION);
            AddTextWithTitle(cxWindowUI.RemediationPanelTitle, CxConstants.NO_INFORMATION);
        }

        private void ShowLoading()
        {
            AddTextWithTitle(cxWindowUI.LearnMorePanelTitle, CxConstants.LOADING_INFORMATION);
            AddTextWithTitle(cxWindowUI.RemediationPanelTitle, CxConstants.LOADING_INFORMATION);
        }

        private void AddSectionWithText(StackPanel panel, string title, string content)
        {
            AddSectionTitle(panel, title);
            AddTextWithTitle(panel, content);
            AddTextWithTitle(panel, string.Empty);
        }

        /// <summary>
        /// Add section title
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="title"></param
        private static void AddSectionTitle(StackPanel panel, string title)
        {
            TextBlock sectionTitle = CreateParsedTextBlock(title);
            sectionTitle.FontWeight = FontWeights.Bold;
            panel.Children.Add(sectionTitle);
        }

        /// <summary>
        /// Add Text with title
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="text"></param
        private static void AddTextWithTitle(StackPanel panel, string text)
        {
            TextBlock textBlock = CreateParsedTextBlock(text);
            panel.Children.Add(textBlock);
        }

        /// <summary>
        /// Create a text block
        /// </summary>
        /// <param name="text"></param
        /// <returns></returns>
        private static TextBlock CreateParsedTextBlock(string text)
        {
            byte[] bytes = Encoding.Default.GetBytes(text);
            text = Encoding.UTF8.GetString(bytes);
            text = WebUtility.HtmlDecode(text);
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWithOverflow,
                Margin = new Thickness(10, 5, 0, 0)
            };
            return textBlock;
        }

        /// <summary>
        /// Clear result vulnerabilities panel
        /// </summary>
        public void Clear()
        {
            cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Hidden;
            cxWindowUI.SastVulnerabilitiesPanel.Visibility = Visibility.Hidden;
        }

        public void SecretDetectionDetailsRemediationTab()
        {
            var remediation = result?.Data?.Remediation;

            if (string.IsNullOrEmpty(remediation))
            {
                AddTextWithTitle(cxWindowUI.RemediationPanelTitle, CxConstants.NO_INFORMATION);
                return;
            }

            AddTextWithTitle(cxWindowUI.RemediationPanelTitle, remediation);
        }

        private void SecretDetectionDetailsLearnMoreTab()
        {
            var ruleDescription = result?.Data?.RuleDescription;

            if (string.IsNullOrEmpty(ruleDescription))
            {
                AddTextWithTitle(cxWindowUI.LearnMorePanelTitle, CxConstants.NO_INFORMATION);
                return;
            }

            AddTextWithTitle(cxWindowUI.LearnMorePanelTitle, ruleDescription);
        }


    }
}
