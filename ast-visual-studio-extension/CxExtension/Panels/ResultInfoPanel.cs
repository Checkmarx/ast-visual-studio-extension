using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

            // Set description tab as selected when drawing result info panel
            cxWindowUI.DescriptionTabItem.IsSelected = true;

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

            cxWindowUI.TriageSeverityCombobox.SelectedIndex = GetSeverityIndex(result.Severity, cxWindowUI);
            cxWindowUI.TriageStateCombobox.SelectedIndex = GetStateIndex(result.State.Trim(), cxWindowUI);

            cxWindowUI.ResultInfoPanel.Visibility = Visibility.Visible;
        }

        private int GetSeverityIndex(string severity, CxWindowControl cxWindowUI)
        {
            for (var i = 0; i < cxWindowUI.TriageSeverityCombobox.Items.Count; i++)
            {
                ComboBoxItem item = cxWindowUI.TriageSeverityCombobox.Items[i] as ComboBoxItem;

                string p = item.Content as string;

                if (p.Equals(severity)) return i;
            }

            return -1;
        }

        private int GetStateIndex(string state, CxWindowControl cxWindowUI)
        {
            for (var i = 0; i < cxWindowUI.TriageStateCombobox.Items.Count; i++)
            {
                ComboBoxItem item = cxWindowUI.TriageStateCombobox.Items[i] as ComboBoxItem;

                string p = item.Content as string;

                if (p.Equals(state)) return i;
            }

            return -1;
        }

        // TODO static?
        public async Task TriageUpdateAsync(Button triageUpdateBtn, TreeView treeViewResults, CxToolbar cxToolbar, ComboBox severityCombobox, ComboBox stateCombobox, string selectedTabItem, StackPanel triageChangesTab)
        {
            triageUpdateBtn.IsEnabled = false;

            Result result = (treeViewResults.SelectedItem as TreeViewItem).Tag as Result;

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;
            string similarityId = result.SimilarityId;
            string engineType = result.Type;
            string state = (stateCombobox.SelectedValue as ComboBoxItem).Content as string;
            string severity = (severityCombobox.SelectedValue as ComboBoxItem).Content as string;
            string comment = "";

            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null)
            {
                triageUpdateBtn.IsEnabled = true;

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
                    new ToastContentBuilder()
                                .AddText("Triage Update failed")
                                .AddText(ex.Message)
                                .Show();

                    triageUpdateBtn.IsEnabled = true;

                    return false;
                }
            });

            if (!triageUpdatedSuccessfully) return;

            result.State = state;
            result.Severity = severity;

            string displayName = result.Data.QueryName ?? result.Id;

            (treeViewResults.SelectedItem as TreeViewItem).Header = UIUtils.CreateTreeViewItemHeader(result.Severity, displayName);
            (treeViewResults.SelectedItem as TreeViewItem).Tag = result;

            if (selectedTabItem.Equals("ChangesTabItem"))
            {
                _ = TriageShowAsync(treeViewResults, cxToolbar, triageChangesTab);
            }

            triageUpdateBtn.IsEnabled = true;
        }

        public async Task TriageShowAsync(TreeView treeViewResults, CxToolbar cxToolbar, StackPanel triageChangesTab)
        {
            Result result = ((treeViewResults.SelectedItem as TreeViewItem).Tag as Result);

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;
            string similarityId = result.SimilarityId;
            string engineType = result.Type;

            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null) return;

            triageChangesTab.Children.Clear();

            triageChangesTab.Children.Add(UIUtils.CreateTextBlock("Loading changes..."));

            List<Predicate> predicates = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.TriageShow(projectId, similarityId, engineType);
                }
                catch (Exception ex)
                {
                    new ToastContentBuilder()
                                    .AddText("Triage Show failed")
                                    .AddText(ex.Message)
                                    .Show();

                    return null;
                }
            });

            if (predicates == null) return;

            triageChangesTab.Children.Clear();

            if (predicates.Count == 0)
            {
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock("No changes."));

                return;
            }

            for (int i = 0; i < predicates.Count; i++)
            {
                Predicate pred = predicates[i];

                DateTime myDate = DateTime.ParseExact(pred.CreatedAt, "yyyy-MM-dd'T'HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
                string createdAt = myDate.ToString("dd/MM/yyyy HH:mm:ss");
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock(pred.CreatedBy + " | " + createdAt));
                triageChangesTab.Children.Add(UIUtils.CreateSeverityLabelWithIcon(pred.Severity));
                triageChangesTab.Children.Add(UIUtils.CreateLabelWithImage(pred.State));

                triageChangesTab.Children.Add(new Separator());
            }            
        }

        // Clear panel
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
