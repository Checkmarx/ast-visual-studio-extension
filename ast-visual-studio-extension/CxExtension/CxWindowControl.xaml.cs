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
                .WithSeverityFilters(new Dictionary<Severity, ToggleButton>
                {
                    { Severity.HIGH, HighSeverityFilter },
                    { Severity.MEDIUM, MediumSeverityFilter },
                    { Severity.LOW, LowSeverityFilter },
                    { Severity.INFO, InfoSeverityFilter },
                }, new Dictionary<Severity, Image>
                {
                    { Severity.HIGH, HighSeverityFilterImage },
                    { Severity.MEDIUM, MediumSeverityFilterImage },
                    { Severity.LOW, LowSeverityFilterImage },
                    { Severity.INFO, InfoSeverityFilterImage },
                })
                .WithStateFilters(new Dictionary<State, MenuItem>
                {
                    { State.NOT_IGNORED, NotIgnoredStateFilter },
                    { State.IGNORED, IgnoredStateFilter },
                    { State.TO_VERIFY, ToVerifyStateFilter },
                    { State.CONFIRMED, ConfirmedStateFilter },
                    { State.PROPOSED_NOT_EXPLOITABLE, ProposedNotExploitableStateFilter },
                    { State.NOT_EXPLOITABLE, NotExploitableStateFilter },
                    { State.URGENT, UrgentStateFilter },
                })
                .WithGroupByOptions(new Dictionary<GroupBy, MenuItem>
                {
                    { GroupBy.FILE, FileGroupBy },
                    { GroupBy.SEVERITY, SeverityGroupBy },
                    { GroupBy.STATE, StateGroupBy },
                    { GroupBy.QUERY_NAME, QueryNameGroupBy },
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

        private void HighSeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            cxToolbar.SeverityFilterClick(Severity.HIGH);
        }

        private void MediumSeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            cxToolbar.SeverityFilterClick(Severity.MEDIUM);
        }

        private void LowSeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            cxToolbar.SeverityFilterClick(Severity.LOW);
        }

        private void InfoSeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            cxToolbar.SeverityFilterClick(Severity.INFO);
        }

        private void ConfirmedStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.CONFIRMED);
        }

        private void ToVerifyStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.TO_VERIFY);
        }

        private void UrgentStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.URGENT);
        }

        private void NotExploitableStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.NOT_EXPLOITABLE);
        }

        private void ProposedNotExploitableStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.PROPOSED_NOT_EXPLOITABLE);
        }

        private void IgnoredStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.IGNORED);
        }

        private void NotIgnoredStateFilter_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.StateFilterClick(State.NOT_IGNORED);
        }

        private void FileGroupBy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.GroupByClick(GroupBy.FILE);
        }

        private void SeverityGroupBy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.GroupByClick(GroupBy.SEVERITY);
        }

        private void StateGroupBy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.GroupByClick(GroupBy.STATE);
        }

        private void QueryNameGroupBy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(sender);
            cxToolbar.GroupByClick(GroupBy.QUERY_NAME);
        }

        private void MenuItem_Click(object sender)
        {
            MenuItem menuItem = (sender as MenuItem);
            menuItem.IsChecked = !menuItem.IsChecked;
        }
    }
}