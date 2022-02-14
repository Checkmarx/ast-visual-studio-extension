using ast_visual_studio_extension.Cx;
using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxCLI.Models;
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

        public ResultsTreePanel(AsyncPackage package) : base(package)
        {
            resultInfoPanel = new ResultInfoPanel(package);
        }

        // Draw results tree
        public async Task DrawAsync(string currentScanId)
        {
            TreeView resultsTree = GetCxWindowControl().TreeViewResults;

            try
            {
                resultInfoPanel.Clear();
                resultsTree.Items.Clear();

                resultsTree.Items.Add(CxConstants.INFO_GETTING_RESULTS);

                Results results = await GetResultsAsync(Guid.Parse(currentScanId));

                List<TreeViewItem> treeViewResults = ConvertResultsToTreeViewItem(results);
                List<TreeViewItem> treeResults = ResultsFilteringAndGrouping.FilterAndGroupResults(treeViewResults);

                TreeViewItem rootNode = new TreeViewItem
                {
                    Header = UIUtils.CreateTreeViewItemHeader(String.Empty, String.Format(CxConstants.TREE_PARENT_NODE, currentScanId)),
                    ItemsSource = treeResults
                };

                resultsTree.Items.Clear();
                resultsTree.Items.Add(rootNode);
            }
            catch (Exception ex)
            {
                resultsTree.Items.Clear();
                resultsTree.Items.Add(ex.Message);
            }
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

                item.MouseLeftButtonUp += OnClickResult;

                transformedResults.Add(item);
            }

            return transformedResults;
        }

        // Handle click event when clicking on a tree view item
        private void OnClickResult(object sender, RoutedEventArgs e)
        {
            resultInfoPanel.Draw((sender as TreeViewItem).Tag as Result);
        }
    }
}
