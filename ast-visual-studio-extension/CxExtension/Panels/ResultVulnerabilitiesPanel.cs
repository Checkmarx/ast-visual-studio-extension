using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    internal class ResultVulnerabilitiesPanel : BasePanel
    {
        public ResultVulnerabilitiesPanel(AsyncPackage package) : base(package) { }

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

            CxWindowControl cxWindowUI = GetCxWindowControl();
            cxWindowUI.VulnerabilitiesList.Items.Clear();

            if (nodes.Count > 0)
            {
                BuildAttackVectorPanel();
            }
            else if (packageDataList.Count > 0)
            {
                BuildPackageDataPanel();
            }
            else if (!string.IsNullOrEmpty(result.Data.FileName))
            {
                BuildVulnerabilityLocation();
            }

            cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Build attack vector panel for sast results
        /// </summary>
        private void BuildAttackVectorPanel()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();
            cxWindowUI.VulnerabilitiesPanelTitle.Text = CxConstants.LBL_ATTACK_VECTOR;

            ListView vulnerabilitiesList = cxWindowUI.VulnerabilitiesList;

            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                string itemName = string.Format(CxConstants.LBL_ATTACK_VECTOR_ITEM, i+1, node.Name);

                ListViewItem item = new ListViewItem();

                TextBlock tb = new TextBlock();
                tb.Inlines.Add(itemName);
                Hyperlink link = new Hyperlink();
                link.Inlines.Add(node.FileName);
                link.Click += new RoutedEventHandler(SolutionExplorerUtils.OpenFile);
                tb.Inlines.Add(link);
                item.Tag = FileNode.Builder().WithFileName(node.FileName).WithLine(node.Line).WithColumn(node.Column);
                item.Content = tb;

                vulnerabilitiesList.Items.Add(item);
            }
        }

        /// <summary>
        /// Build package data panel for sca results
        /// </summary>
        private void BuildPackageDataPanel()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();
            cxWindowUI.VulnerabilitiesPanelTitle.Text = CxConstants.LBL_PACKAGE_DATA;

            ListView vulnerabilitiesList = cxWindowUI.VulnerabilitiesList;

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
                link.Inlines.Add(packageData.Type);
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
            CxWindowControl cxWindowUI = GetCxWindowControl();
            cxWindowUI.VulnerabilitiesPanelTitle.Text = CxConstants.LBL_LOCATION;

            ListViewItem item = new ListViewItem();

            TextBlock tb = new TextBlock();
            tb.Inlines.Add(CxConstants.LBL_LOCATION_FILE);
            Hyperlink link = new Hyperlink();
            link.Inlines.Add(result.Data.FileName);
            link.Click += new RoutedEventHandler(SolutionExplorerUtils.OpenFile);
            tb.Inlines.Add(link);
            item.Tag = FileNode.Builder().WithFileName(result.Data.FileName).WithLine(result.Data.Line).WithColumn(1);
            item.Content = tb;

            ListView vulnerabilitiesList = cxWindowUI.VulnerabilitiesList;
            vulnerabilitiesList.Items.Add(item);
        }

        /// <summary>
        /// Clear result vulnerabilities panel
        /// </summary>
        public void Clear()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();

            if (cxWindowUI != null)
            {
                cxWindowUI.VulnerabilitiesPanel.Visibility = Visibility.Hidden;
            }
        }
    }
}
