using ast_visual_studio_extension.Cx;
using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CxConstants = ast_visual_studio_extension.CxExtension.Utils.CxConstants;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    /// <summary>
    /// This class is used to draw the result tree panel
    /// </summary>
    internal class ResultsTreePanel : BasePanel
    {
        private readonly ResultInfoPanel resultInfoPanel;
        private readonly ResultVulnerabilitiesPanel resultVulnerabilitiesPanel;

        private string currentScanId;
        private Results currentResults;

        public ResultsTreePanel(AsyncPackage package) : base(package)
        {
            resultInfoPanel = new ResultInfoPanel(package);
            resultVulnerabilitiesPanel = new ResultVulnerabilitiesPanel(package);
        }

        public void Draw(CxToolbar cxToolbar)
        {
            if (this.currentScanId != null && this.currentResults != null)
            {
                ClearAllPanels();

                TreeViewItem rootNode = BuildTree();

                GetCxWindowControl().TreeViewResults.Items.Add(rootNode);
            }
        }

        // Draw results tree
        public async Task DrawAsync(string currentScanId, CxToolbar cxToolbar)
        {
            this.currentScanId = currentScanId;

            cxToolbar.ProjectsCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.ScansCombo.IsEnabled = false;

            TreeView resultsTree = GetCxWindowControl().TreeViewResults;

            try
            {
                resultInfoPanel.Clear();
                resultVulnerabilitiesPanel.Clear();
                resultsTree.Items.Clear();

                resultsTree.Items.Add(CxConstants.INFO_GETTING_RESULTS);

                currentResults = await GetResultsAsync(Guid.Parse(currentScanId));
                
                TreeViewItem rootNode = BuildTree();

                resultsTree.Items.Clear();
                resultsTree.Items.Add(rootNode);
            }
            catch (Exception ex)
            {
                GetCxWindowControl().TreeViewResults.Items.Clear();
                GetCxWindowControl().TreeViewResults.Items.Add(ex.Message);
            }

            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.BranchesCombo.IsEnabled = true;
            cxToolbar.ScansCombo.IsEnabled = true;
        }

        // Get AST results
        private async Task<Results> GetResultsAsync(Guid scanId)
        {
            CxPreferencesModule preferences = (CxPreferencesModule) package.GetDialogPage(typeof(CxPreferencesModule));
            CxConfig configuration = preferences.GetCxConfig;

            CxWrapper cxWrapper = new CxWrapper(configuration);

            var resultsAsync = Task.Run(() => cxWrapper.GetResults(scanId, ReportFormat.json));

            Results results = await resultsAsync;

            return results;
        }

        private TreeViewItem BuildTree()
        {
            List<TreeViewItem> treeViewResults = ConvertResultsToTreeViewItem(currentResults);
            List<TreeViewItem> treeResults = ResultsFilteringAndGrouping.FilterAndGroupResults(package, treeViewResults);

            TreeViewItem rootNode = new TreeViewItem
            {
                Header = UIUtils.CreateTreeViewItemHeader(string.Empty, string.Format(treeResults.Count > 0 ? CxConstants.TREE_PARENT_NODE : CxConstants.TREE_PARENT_NODE_NO_RESULTS, currentScanId)),
                ItemsSource = treeResults
            };
            return rootNode;
        }

        // Convert AST results to tree view item
        private List<TreeViewItem> ConvertResultsToTreeViewItem(Results results)
        {
            List<Result> allResults = results.results;
            List<TreeViewItem> transformedResults = new List<TreeViewItem>(allResults.Count);

            foreach (Result result in allResults)
            {
                string displayName = result.Data.QueryName ?? result.Id;

                TreeViewItem item = new TreeViewItem
                {
                    Header = UIUtils.CreateTreeViewItemHeader(result.Severity, displayName),
                    Tag = result
                };

                item.GotFocus += OnClickResult;

                transformedResults.Add(item);
            }

            return transformedResults;
        }

        // Handle click event when clicking on a tree view item
        private void OnClickResult(object sender, RoutedEventArgs e)
        {
            resultInfoPanel.Draw((sender as TreeViewItem).Tag as Result);
            resultVulnerabilitiesPanel.Draw((sender as TreeViewItem).Tag as Result);
        }

        /// <summary>
        /// Clear all panels
        /// </summary>
        public void ClearAllPanels()
        {
            GetCxWindowControl().TreeViewResults.Items.Clear();
            resultInfoPanel.Clear();
            resultVulnerabilitiesPanel.Clear();
        }
    }
}
