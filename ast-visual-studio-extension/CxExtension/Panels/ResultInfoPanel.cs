using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    /// <summary>
    /// This class is used to draw the result information panel
    /// </summary>
    internal class ResultInfoPanel : BasePanel
    {
        private Result result;

        public ResultInfoPanel(AsyncPackage package) : base(package) { }

        // Draw result information content
        public void Draw(Result result)
        {
            this.result = result;

            CxWindowControl cxWindowUI = GetCxWindowControl();

            // Disable all triage stuff if selected result is sca
            bool isNotScaEngine = !(this.result.Data.PackageData != null || (this.result.Data.Nodes == null && string.IsNullOrEmpty(this.result.Data.FileName)));

            cxWindowUI.TriageSeverityCombobox.IsEnabled = isNotScaEngine;
            cxWindowUI.TriageStateCombobox.IsEnabled = isNotScaEngine;
            cxWindowUI.TriageComment.Visibility = isNotScaEngine ? Visibility.Visible : Visibility.Hidden;
            cxWindowUI.TriageUpdateBtn.Visibility = isNotScaEngine ? Visibility.Visible : Visibility.Hidden;
            cxWindowUI.ResultTabControl.Margin = isNotScaEngine ? new Thickness(0, 10, 0, 0) : new Thickness(0, -45, 0, 0);

            // Set description tab as selected when drawing result info panel
            cxWindowUI.DescriptionTabItem.IsSelected = true;
            cxWindowUI.TriageComment.Text = CxConstants.TRIAGE_COMMENT_PLACEHOLDER;
            cxWindowUI.TriageComment.Foreground = new SolidColorBrush(Colors.Gray);

            DrawTitle();
            DrawDesrciptionTab();
        }

        // Draw title
        private void DrawTitle()
        {
            Image severityIcon = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri(CxUtils.GetIconPathFromSeverity(result.Severity, true)));
            severityIcon.Source = bitmapImage;

            CxWindowControl cxWindowUI = GetCxWindowControl();

            cxWindowUI.ResultSeverityIcon.Source = bitmapImage;
            cxWindowUI.ResultTitle.Text = result.Data.QueryName ?? result.Id;
        }

        // Draw description tab
        private void DrawDesrciptionTab()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();

            cxWindowUI.ResultInfoStackPanel.Children.Clear();

            if (result.Description != null)
            {
                TextBlock descriptionTextBlock = new TextBlock
                {
                    Text = result.Description,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Margin = new Thickness(10, 5, 0, 0)
                };

                cxWindowUI.ResultInfoStackPanel.Children.Add(descriptionTextBlock);
            }

            if (result.Data.Value != null)
            {
                Run labelActualValue = new Run(CxConstants.DESC_TAB_LBL_ACTUAL_VALUE)
                {
                    FontWeight = FontWeights.Bold
                };

                TextBlock actualValueTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Margin = new Thickness(10, result.Description != null ? 30 : 5, 0, 0)
                };

                actualValueTextBlock.Inlines.Add(labelActualValue);
                actualValueTextBlock.Inlines.Add(new Run(result.Data.Value));

                cxWindowUI.ResultInfoStackPanel.Children.Add(actualValueTextBlock);

                Run labelExpectedValue = new Run(CxConstants.DESC_TAB_LBL_EXPECTED_VALUE)
                {
                    FontWeight = FontWeights.Bold
                };

                TextBlock expectedValueTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Margin = new Thickness(10, 15, 0, 0)
                };

                expectedValueTextBlock.Inlines.Add(labelExpectedValue);
                expectedValueTextBlock.Inlines.Add(new Run(result.Data.ExpectedValue));

                cxWindowUI.ResultInfoStackPanel.Children.Add(expectedValueTextBlock);
            }

            cxWindowUI.TriageSeverityCombobox.SelectedIndex = CxUtils.GetItemIndexInCombo(result.Severity, cxWindowUI.TriageSeverityCombobox, Enums.ComboboxType.SEVERITY);
            cxWindowUI.TriageStateCombobox.SelectedIndex = CxUtils.GetItemIndexInCombo(result.State.Trim(), cxWindowUI.TriageStateCombobox, Enums.ComboboxType.STATE);

            cxWindowUI.ResultInfoPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Update result
        /// </summary>
        /// <param name="triageUpdateBtn"></param>
        /// <param name="treeViewResults"></param>
        /// <param name="cxToolbar"></param>
        /// <param name="severityCombobox"></param>
        /// <param name="stateCombobox"></param>
        /// <param name="selectedTabItem"></param>
        /// <param name="triageChangesTab"></param>
        /// <param name="triageComment"></param>
        /// <returns></returns>
        public async Task TriageUpdateAsync(Button triageUpdateBtn, TreeView treeViewResults, CxToolbar cxToolbar, ComboBox severityCombobox, ComboBox stateCombobox, string selectedTabItem, StackPanel triageChangesTab, TextBox triageComment)
        {
            triageUpdateBtn.IsEnabled = false;
            severityCombobox.IsEnabled = false;
            stateCombobox.IsEnabled = false;
            triageComment.IsEnabled = false;
            triageComment.Foreground = new SolidColorBrush(Colors.Gray);

            Result result = (treeViewResults.SelectedItem as TreeViewItem).Tag as Result;

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;
            string similarityId = result.SimilarityId;
            string engineType = result.Type;
            string state = (stateCombobox.SelectedValue as ComboBoxItem).Content as string;
            string severity = (severityCombobox.SelectedValue as ComboBoxItem).Content as string;
            string comment = triageComment.Text.Equals(CxConstants.TRIAGE_COMMENT_PLACEHOLDER) ? string.Empty : triageComment.Text;

            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null)
            {
                triageUpdateBtn.IsEnabled = true;
                severityCombobox.IsEnabled = true;
                stateCombobox.IsEnabled = true;
                triageComment.IsEnabled = true;

                return;
            }

            bool triageUpdatedSuccessfully = await Task.Run(() =>
            {
                try
                {
                    cxWrapper.TriageUpdate(projectId, similarityId, engineType, state, comment, severity);
                    return true;
                }
                catch (Exception ex)
                {
                    CxUtils.DisplayNotification(CxConstants.TRIAGE_UPDATE_FAILED, ex.Message);

                    return false;
                }
            });

            if (!triageUpdatedSuccessfully)
            {
                UpdateTriageElementsState(triageUpdateBtn, triageComment, severityCombobox, stateCombobox);

                return;
            }

            result.State = state;
            result.Severity = severity;

            string displayName = result.Data.QueryName ?? result.Id;

            (treeViewResults.SelectedItem as TreeViewItem).Header = UIUtils.CreateTreeViewItemHeader(result.Severity, displayName);
            (treeViewResults.SelectedItem as TreeViewItem).Tag = result;

            if (selectedTabItem.Equals("ChangesTabItem"))
            {
                await TriageShowAsync(treeViewResults, cxToolbar, triageChangesTab);
            }

            UpdateTriageElementsState(triageUpdateBtn, triageComment, severityCombobox, stateCombobox);
        }

        private void UpdateTriageElementsState(Button triageUpdateBtn, TextBox triageComment, ComboBox severityCombobox, ComboBox stateCombobox)
        {
            triageUpdateBtn.IsEnabled = true;
            triageComment.Text = CxConstants.TRIAGE_COMMENT_PLACEHOLDER;
            triageComment.Foreground = new SolidColorBrush(Colors.Gray);
            severityCombobox.IsEnabled = true;
            stateCombobox.IsEnabled = true;
            triageComment.IsEnabled = true;
        }

        /// <summary>
        /// List triage changes
        /// </summary>
        /// <param name="treeViewResults"></param>
        /// <param name="cxToolbar"></param>
        /// <param name="triageChangesTab"></param>
        /// <returns></returns>
        public async Task TriageShowAsync(TreeView treeViewResults, CxToolbar cxToolbar, StackPanel triageChangesTab)
        {
            Result result = ((treeViewResults.SelectedItem as TreeViewItem).Tag as Result);

            triageChangesTab.Children.Clear();

            bool isSca = result.Data.PackageData != null || (result.Data.Nodes == null && string.IsNullOrEmpty(result.Data.FileName));

            if (result != null && isSca)
            {
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock(CxConstants.TRIAGE_SCA_NOT_AVAILABLE));

                return;
            }

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;
            string similarityId = result.SimilarityId;
            string engineType = result.Type;

            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null) return;

            triageChangesTab.Children.Clear();

            triageChangesTab.Children.Add(UIUtils.CreateTextBlock(CxConstants.TRIAGE_LOADING_CHANGES));

            string errorMessage = string.Empty;

            List<Predicate> predicates = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.TriageShow(projectId, similarityId, engineType);
                }
                catch (Exception ex)
                {
                    CxUtils.DisplayNotification(CxConstants.TRIAGE_SHOW_FAILED, ex.Message);
                    errorMessage = ex.Message;

                    return null;
                }
            });

            triageChangesTab.Children.Clear();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock(CxConstants.TRIAGE_SHOW_FAILED + ". " + errorMessage));

                return;
            }

            if (predicates.Count == 0)
            {
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock(CxConstants.TRIAGE_NO_CHANGES));

                return;
            }

            foreach(Predicate predicate in predicates)
            {
                DateTime predicateCreatedAt = DateTime.Parse(predicate.CreatedAt, System.Globalization.CultureInfo.InvariantCulture);
                string createdAt = predicateCreatedAt.ToString(CxConstants.DATE_OUTPUT_FORMAT);

                TextBlock tb = new TextBlock();
                tb.Inlines.Add(new Bold(new Run(predicate.CreatedBy)));
                tb.Inlines.Add(new Run(" | " + createdAt));

                triageChangesTab.Children.Add(tb);
                triageChangesTab.Children.Add(UIUtils.CreateSeverityLabelWithIcon(predicate.Severity));
                triageChangesTab.Children.Add(UIUtils.CreateLabelWithImage(predicate.State, CxConstants.ICON_FLAG));

                if (!string.IsNullOrEmpty(predicate.Comment))
                {
                    triageChangesTab.Children.Add(UIUtils.CreateLabelWithImage(predicate.Comment, CxConstants.ICON_COMMENT));
                }

                triageChangesTab.Children.Add(new Separator());
            }
        }

        /// <summary>
        /// Clear Result Info panel
        /// </summary>
        public void Clear()
        {
            CxWindowControl cxWindowUI = GetCxWindowControl();

            if (cxWindowUI != null)
            {
                cxWindowUI.ResultSeverityIcon.Source = null;
                cxWindowUI.ResultTitle.Text = string.Empty;
                cxWindowUI.ResultInfoStackPanel.Children.Clear();
                cxWindowUI.ResultInfoPanel.Visibility = Visibility.Hidden;
            }
        }
    }
}
