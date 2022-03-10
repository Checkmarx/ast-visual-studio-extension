using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Toolbar;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ast_visual_studio_extension.CxExtension
{
    public partial class CxWindowControl : UserControl
    {
        private readonly CxToolbar cxToolbar;

        public CxWindowControl(AsyncPackage package)
        {
            ResultsTreePanel resultsTreePanel = new ResultsTreePanel(package);

            InitializeComponent();

            // Build CxToolbar
            cxToolbar = CxToolbar.Builder()
                .WithPackage(package)
                .WithResultsTreePanel(resultsTreePanel)
                .WithProjectsCombo(ProjectsCombobox)
                .WithBranchesCombo(BranchesCombobox)
                .WithScansCombo(ScansCombobox)
                .WithResultsTree(TreeViewResults)
                .WithSeverityFilters(new Dictionary<ToggleButton, Severity>
                {
                    { HighSeverityFilter, Severity.HIGH },
                    { MediumSeverityFilter , Severity.MEDIUM},
                    { LowSeverityFilter, Severity.LOW },
                    { InfoSeverityFilter, Severity.INFO },
                }, new Dictionary<Severity, Image>
                {
                    { Severity.HIGH, HighSeverityFilterImage },
                    { Severity.MEDIUM, MediumSeverityFilterImage },
                    { Severity.LOW, LowSeverityFilterImage },
                    { Severity.INFO, InfoSeverityFilterImage },
                })
                .WithStateFilters(new Dictionary<MenuItem, State>
                {
                    { NotIgnoredStateFilter, State.NOT_IGNORED },
                    { IgnoredStateFilter, State.IGNORED },
                    { ToVerifyStateFilter, State.TO_VERIFY },
                    { ConfirmedStateFilter, State.CONFIRMED },
                    { ProposedNotExploitableStateFilter, State.PROPOSED_NOT_EXPLOITABLE },
                    { NotExploitableStateFilter, State.NOT_EXPLOITABLE },
                    { UrgentStateFilter, State.URGENT },
                })
                .WithGroupByOptions(new Dictionary<MenuItem, GroupBy>
                {
                    { FileGroupBy, GroupBy.FILE },
                    { SeverityGroupBy, GroupBy.SEVERITY },
                    { StateGroupBy, GroupBy.STATE },
                    { QueryNameGroupBy, GroupBy.QUERY_NAME },
                });

            // Init toolbar elements
            cxToolbar.Init();
        }

        /// <summary>
        /// Handle mouse wheel in the vulnerabilities panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VulnerabilitiesPanelPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleScrollViewer((ScrollViewer)sender, e);
        }

        /// <summary>
        /// Handle mouse wheel in the results tree panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultsTreePanelPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleScrollViewer((ScrollViewer)sender, e);
        }

        /// <summary>
        /// Used to allow the user scroll the panel with the mouse wheel
        /// </summary>
        /// <param name="scrollViewer"></param>
        /// <param name="args"></param>
        private void HandleScrollViewer(ScrollViewer scrollViewer, MouseWheelEventArgs args)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - args.Delta);
            args.Handled = true;
        }

        /// <summary>
        /// On change event for Projects combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangeProject(object sender, SelectionChangedEventArgs e)
        {
            cxToolbar.ProjectsCombobox.OnChangeProject(sender, e);
        }

        /// <summary>
        /// On change event for Branches combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangeBranch(object sender, SelectionChangedEventArgs e)
        {
            cxToolbar.BranchesCombobox.OnChangeBranch(sender, e);
        }

        /// <summary>
        /// On change event for Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangeScan(object sender, SelectionChangedEventArgs e)
        {
            cxToolbar.ScansCombobox.OnChangeScan(sender, e);
        }

        /// <summary>
        /// On press enter or tab in Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDownScans(object sender, KeyEventArgs e)
        {
            cxToolbar.ScansCombobox.OnKeyDownScans(sender, e);
        }
        private void SeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            cxToolbar.SeverityFilterClick(sender as ToggleButton);
        }

        private void StateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(sender as MenuItem);
        }

        private void GroupBy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.GroupByClick(sender as MenuItem);
        }

        private void MenuItem_Click(object sender)
        {
            MenuItem menuItem = (sender as MenuItem);
            menuItem.IsChecked = !menuItem.IsChecked;
        }
    }
}
