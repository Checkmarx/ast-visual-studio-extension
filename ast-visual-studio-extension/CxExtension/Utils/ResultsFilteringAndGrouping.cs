using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class ResultsFilteringAndGrouping
    {
        public static List<TreeViewItem> FilterAndGroupResults(AsyncPackage package, List<TreeViewItem> results)
        {

            HashSet<SystemState> enabledSystemStates = SettingsUtils.EnabledStates(package);
            HashSet<Severity> enabledSeverities = SettingsUtils.EnabledSeverities(package);
            List<GroupBy> enabledGroupBys = SettingsUtils.EnabledGroupByOptions(package);
            StateManager stateManager = StateManagerProvider.GetStateManager();

            var treeResults = new List<TreeViewItem>();

            var isDevOrTestSelected = stateManager.enabledCustemStates.Contains("SCA Dev & Test Dependencies");
            enabledGroupBys.Insert(0, GroupBy.ENGINE);

            // Dictionary to store group headers and counts
            var groupNodeCounts = new Dictionary<TreeViewItem, int>();

            foreach (TreeViewItem item in results)
            {
                var result = item.Tag as Result;
                bool isSca = result.Type?.ToLower() == "sca";
                bool isDev = result.Data?.ScaPackageData?.IsDevelopmentDependency == true;
                bool isTest = result.Data?.ScaPackageData?.IsTestDependency == true;
                bool skipFurtherFiltering = false;
                if (isSca)
                {
                    if (!isDevOrTestSelected && (isDev || isTest))
                    {
                        continue;
                    }

                    if (isDevOrTestSelected && (isDev || isTest))
                    {
                        skipFurtherFiltering = true;
                    }
                }


                bool isSystemState = Enum.TryParse(result.State, out SystemState itemSystemState);
                Enum.TryParse(result.Severity, out Severity itemSeverity);

                if ( !skipFurtherFiltering && (isSystemState && !enabledSystemStates.Contains(itemSystemState)) || (!isSystemState && !stateManager.enabledCustemStates.Contains(result.State)) || !enabledSeverities.Contains(itemSeverity))
                {
                    continue;
                }

                List<TreeViewItem> children = GetInsertLocation(enabledGroupBys, treeResults, result, groupNodeCounts);

                children.Add(item);
            }

            // Update group headers with counts
            foreach (var kvp in groupNodeCounts)
            {
                var headerBlock = kvp.Key.Header as TextBlock;
                if (headerBlock != null)
                {
                    string baseLabel = (headerBlock.Tag as string).Replace("_", " ");
                    if(baseLabel == EngineTypeExtensions.ToEngineString(EngineType.SCS_SECRET_DETECTION))
                        baseLabel = EngineTypeExtensions.ToEngineString(EngineType.SECRET_DETECTION);
                    else if(baseLabel == EngineTypeExtensions.ToEngineString(EngineType.KICS))
                        baseLabel = EngineTypeExtensions.ToEngineString(EngineType.IAC_SECURITY);

                    string labelWithCount = $"{baseLabel} ({kvp.Value})";

                    // Replace the existing header with a new TextBlock
                    kvp.Key.Header = UIUtils.CreateTreeViewItemHeader(string.Empty, labelWithCount);
                    kvp.Key.ToolTip = $"{baseLabel}";
                }
            }


            return treeResults;
        }

        private static List<TreeViewItem> GetInsertLocation(List<GroupBy> enabledGroupBys, List<TreeViewItem> treeResults, Result result, Dictionary<TreeViewItem, int> groupNodeCounts)
        {
            var children = treeResults;
            foreach (GroupBy groupBy in enabledGroupBys)
            {
                var generator = GetGroupByTitleGenerator(groupBy);
                if (generator == null) continue;

                var childNodeName = GetGroupByTitleGenerator(groupBy).Invoke(result);
                if (childNodeName == null) continue;

                // single underscore is used as mnemonic
                childNodeName = childNodeName.Replace("_", " ");

                TreeViewItem child = null;

                foreach (var childNode in children)
                {
                    if (childNodeName == (childNode.Header as TextBlock).Tag as string)
                    {
                        child = childNode;
                    }
                }
                if (child == null)
                {
                    child = UIUtils.CreateTreeViewItemWithItemsSource(childNodeName, new List<TreeViewItem> { new TreeViewItem() });
                    (child.ItemsSource as List<TreeViewItem>).Clear();
                    children.Add(child);
                    groupNodeCounts[child] = 0; // initialize count
                }
                // Increment count for this group node
                if (groupNodeCounts.ContainsKey(child))
                {
                    groupNodeCounts[child]++;
                }
                children = child.ItemsSource as List<TreeViewItem>;
            }

            return children;
        }

        private static Func<Result, string> GetGroupByTitleGenerator(GroupBy groupBy)
        {
            switch (groupBy)
            {
                case GroupBy.ENGINE:
                    return (result) => result.Type;
                case GroupBy.SEVERITY:
                    return (result) => result.Severity;
                case GroupBy.VULNERABILITY_TYPE:
                    return (result) => result.Data?.QueryName ?? result.Data?.PackageIdentifier ?? result.Id;
                case GroupBy.STATE:
                    return (result) => result.State;
                case GroupBy.STATUS:
                    return (result) => result.Status;
                case GroupBy.Language:
                    return (result) => result.Data.LanguageName;
                case GroupBy.FILE:
                    return (result) =>
                    {
                        if (result.Data.FileName != null)
                            return result.Data.FileName;
                        if (result.Data.Nodes != null && result.Data.Nodes.Count > 0)
                            return result.Data.Nodes[0].FileName;
                        return null;
                    };
                case GroupBy.DIRECT_DEPENDENCY:
                    return (result) => result.Type == "sca" ? result.Data.ScaPackageData.TypeOfDependency : null;
                default:
                    return null;
            }
        }
    }
}
