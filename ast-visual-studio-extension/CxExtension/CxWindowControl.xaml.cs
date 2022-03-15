using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension
{
    public partial class CxWindowControl : UserControl
    {
        private readonly CxToolbar cxToolbar;
        private readonly ResultInfoPanel resultInfoPanel;

        public CxWindowControl(AsyncPackage package)
        {
            ResultsTreePanel resultsTreePanel = new ResultsTreePanel(package);

            resultInfoPanel = new ResultInfoPanel(package);

            InitializeComponent();

            // Build CxToolbar
            cxToolbar = CxToolbar.Builder()
                .WithPackage(package)
                .WithResultsTreePanel(resultsTreePanel)
                .WithProjectsCombo(ProjectsCombobox)
                .WithBranchesCombo(BranchesCombobox)
                .WithScansCombo(ScansCombobox)
                .WithResultsTree(TreeViewResults);

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
            _ = resultInfoPanel.TriageUpdateAsync(TriageUpdateBtn, TreeViewResults, cxToolbar, TriageSeverityCombobox, TriageStateCombobox, (ResultTabControl.SelectedItem as TabItem).Name, TriageChangesTab, TriageComment);
        }

        /// <summary>
        /// On click triage changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickTriageChanges(object sender, MouseButtonEventArgs e)
        {
            _ = resultInfoPanel.TriageShowAsync(TreeViewResults, cxToolbar, TriageChangesTab);
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
                TriageComment.Foreground = new SolidColorBrush(Colors.White);
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
    }
}