using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension
{
    public partial class CxWindowControl : UserControl
    {
        private readonly CxToolbar cxToolbar;
        private readonly ResultInfoPanel resultInfoPanel;
        private readonly AsyncPackage package;
        
        public CxWindowControl(AsyncPackage package)
        {
            InitializeComponent();

            this.package = package;

            ResultsTreePanel resultsTreePanel = new ResultsTreePanel(package, this);

            resultInfoPanel = Panels.ResultInfoPanel.GetInstance(this);

            // Subscribe OnApply event in checkmarx settings window
            CxPreferencesUI.GetInstance().OnApplySettingsEvent += CheckToolWindowPanel;

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
        /// Check if panel should be redraw after applying new checkmarx settings
        /// </summary>
        private void CheckToolWindowPanel()
        {
            if (!CxUtils.AreCxCredentialsDefined(package))
            {
                CxPreferencesUI.GetInstance().OnApplySettingsEvent -= CheckToolWindowPanel;
                Content = new CxInitialPanel(package);

                return;
            }

            cxToolbar.ProjectsCombo.Items.Clear();
            cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_LOADING_PROJECTS;
            cxToolbar.ProjectsCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.Items.Clear();
            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_LOADING_SCANS;
            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ResultsTreePanel.ClearAll();

            CxToolbar.redrawExtension = true;
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
        private void OnTypeScan(object sender, KeyEventArgs e)
        {
            _ = cxToolbar.ScansCombobox.OnTypeScanAsync(sender, e);
        }

        /// <summary>
        /// On press triage update button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickTriageUpdate(object sender, RoutedEventArgs e)
        {
            _ = resultInfoPanel.TriageUpdateAsync(TriageUpdateBtn, cxToolbar, TriageSeverityCombobox, TriageStateCombobox, (ResultTabControl.SelectedItem as TabItem).Name, TriageChangesTab, TriageComment);
        }

        /// <summary>
        /// On click triage changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickTriageChanges(object sender, MouseButtonEventArgs e)
        {
            _ = resultInfoPanel.TriageShowAsync(cxToolbar, TriageChangesTab);
        }

        /// <summary>
        /// On Focus comment field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFocusComment(object sender, RoutedEventArgs e)
        {
            if (TriageComment.Text.Equals(CxConstants.TRIAGE_COMMENT_PLACEHOLDER))
            {
                TriageComment.Text = string.Empty;
                TriageComment.ClearValue(TextBox.ForegroundProperty);
            }
        }

        /// <summary>
        /// On Lost Focus comment field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLostFocusComment(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TriageComment.Text))
            {
                TriageComment.Text = CxConstants.TRIAGE_COMMENT_PLACEHOLDER;
                TriageComment.Foreground = new SolidColorBrush(Colors.Gray);
            }
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

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshBtn.IsChecked = false;
            CxToolbar.currentProjectId = string.Empty;
            CxToolbar.currentBranch = string.Empty;
            CxToolbar.currentScanId = string.Empty;
            CxToolbar.resetExtension = true;
            cxToolbar.Init();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsBtn.IsChecked = false;
            package.ShowOptionPage(typeof(CxPreferencesModule));
        }
    }
}
