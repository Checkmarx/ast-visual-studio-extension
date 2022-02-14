using ast_visual_studio_extension.CxCLI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class ResultsFilteringAndGrouping
    {
        public static List<TreeViewItem> FilterAndGroupResults(List<TreeViewItem> results)
        {
            List<TreeViewItem> filteredResults = FilterResults(results);

            return GroupResults(filteredResults);
        }

        private static List<TreeViewItem> FilterResults(List<TreeViewItem> results)
        {
            return results;
        }

        private static List<TreeViewItem> GroupResults(List<TreeViewItem> results)
        {
            return GroupByEngine(results);
        }

        public static List<TreeViewItem> GroupByEngine(List<TreeViewItem> results)
        {
            List<List<TreeViewItem>> groupedByEngine = results
                                        .GroupBy(u => new { (u.Tag as Result).Type })
                                        .Select(grp => grp.ToList())
                                        .ToList();

            List<TreeViewItem> enginesByType = new List<TreeViewItem>();

            // Create root folder for each engine type
            groupedByEngine.ForEach(engineChild => enginesByType.Add(UIUtils.CreateTreeViewItemWithItemsSource((engineChild[0].Tag as Result).Type, engineChild)));

            return enginesByType;
        }
    }
}
