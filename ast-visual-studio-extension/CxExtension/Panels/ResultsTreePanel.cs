using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using ast_visual_studio_extension.CxWrapper.Models;
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
    internal class ResultsTreePanel
    {
        private readonly ResultInfoPanel resultInfoPanel;
        public readonly ResultVulnerabilitiesPanel resultVulnerabilitiesPanel;

        private string currentScanId;
        public static Results currentResults;
        private readonly CxWindowControl cxWindowUI;
        private readonly AsyncPackage package;

        public ResultsTreePanel(AsyncPackage package, CxWindowControl cxWindow, ResultInfoPanel resultInfoPanel)
        {
            this.package = package;
            cxWindowUI = cxWindow;
            this.resultInfoPanel = resultInfoPanel;

            resultVulnerabilitiesPanel = new ResultVulnerabilitiesPanel(package, cxWindow);
            UIUtils.CxWindowUI = cxWindowUI;
        }

        public void Redraw(bool clearSecondAndThirdPanel)
        {
            if (currentScanId != null && currentResults != null)
            {
                var treeView = cxWindowUI.TreeViewResults;

                var expanded = CollectExpandedNodes(treeView.Items[0] as TreeViewItem);

                ClearPanels(clearSecondAndThirdPanel);

                TreeViewItem rootNode = BuildTree();

                treeView.Items.Add(rootNode);

                ExpandNodes(expanded, rootNode);
            }
        }

        // Draw results tree
        public async Task DrawAsync(string currentScanId, CxToolbar cxToolbar)
        {
            this.currentScanId = currentScanId;

            cxToolbar.ProjectsCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.ScansCombo.IsEnabled = false;

            TreeView resultsTree = cxWindowUI.TreeViewResults;

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
                cxWindowUI.TreeViewResults.Items.Clear();
                cxWindowUI.TreeViewResults.Items.Add(ex.Message);
            }

            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.BranchesCombo.IsEnabled = true;
            cxToolbar.ScansCombo.IsEnabled = true;
        }

        // Get AST results
        private async Task<Results> GetResultsAsync(Guid scanId)
        {
            CxPreferencesModule preferences = (CxPreferencesModule) package.GetDialogPage(typeof(CxPreferencesModule));
            CxConfig configuration = preferences.GetCxConfig();

            CxCLI.CxWrapper cxWrapper = new CxCLI.CxWrapper(configuration, GetType());

            var resultsAsync = Task.Run(() => cxWrapper.GetResults(scanId));

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
        /// Clear panels and state
        /// </summary>
        public void ClearAll()
        {
            ClearPanels(true);
            currentResults = null;
            currentScanId = null;
        }

        /// <summary>
        /// Clear panels
        /// </summary>
        /// <param name="clearSecondAndThirdPanel"></param>
        private void ClearPanels(bool clearSecondAndThirdPanel)
        {
            cxWindowUI.TreeViewResults.Items.Clear();

            if (clearSecondAndThirdPanel)
            {
                resultInfoPanel.Clear();
                resultVulnerabilitiesPanel.Clear();
            }
        }

        // Iterates the tree to collect all expanded nodes.
        // Using a stack to iterate as a recursive version was slow.
        private List<TreeViewItem> CollectExpandedNodes(TreeViewItem root)
        {
            var expanded = new List<TreeViewItem>();
            var toVisit = new Stack<TreeViewItem>();
            toVisit.Push(root);
            while (toVisit.Count > 0)
            {
                var current = toVisit.Pop();
                if (current.IsExpanded)
                {
                    expanded.Add(current);
                    if (current.ItemsSource != null)
                    {
                        foreach (var item in current.ItemsSource)
                        {
                            toVisit.Push(item as TreeViewItem);
                        }
                    }
                }
            }

            return expanded;
        }

        // Iterates the tree to expand previously expanded nodes.
        // Using a stack to iterate as a recursive version was slow.
        private void ExpandNodes(List<TreeViewItem> expandedNodes, TreeViewItem root)
        {
            var toVisit = new Stack<TreeViewItem>();
            toVisit.Push(root);

            while (toVisit.Count > 0)
            {
                var current = toVisit.Pop();
                foreach (var node in expandedNodes)
                {
                    if ((current.Header as TextBlock).Tag as string == (node.Header as TextBlock).Tag as string)
                    {
                        current.IsExpanded = true;
                        if (current.ItemsSource != null)
                        {
                            foreach (var item in current.ItemsSource)
                            {
                                toVisit.Push(item as TreeViewItem);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
