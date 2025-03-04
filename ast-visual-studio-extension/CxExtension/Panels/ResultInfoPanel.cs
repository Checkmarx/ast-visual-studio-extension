using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Toolbar;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Exceptions;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.UI.WebControls;
using MenuItem = System.Web.UI.WebControls.MenuItem;
using Image = System.Windows.Controls.Image;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using System.Diagnostics;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    /// <summary>
    /// This class is used to draw the result information panel
    /// </summary>
    internal class ResultInfoPanel
    {
        private Result result;
        private readonly CxWindowControl cxWindowUI;
        

        public ResultInfoPanel(CxWindowControl cxWindow)
        {
            cxWindowUI = cxWindow;
            UIUtils.CxWindowUI = cxWindowUI;

        }

        // Draw result information content
        public void Draw(Result result)
        {

            this.result = result;

            // Disable all triage stuff if selected result is sca
            bool isNotScaEngine = !(this.result.Data.PackageData != null || (this.result.Data.Nodes == null && string.IsNullOrEmpty(this.result.Data.FileName)));
            StateManager stateManager = StateManagerProvider.GetStateManager();
            List<State> states = stateManager.GetAllStates();

            cxWindowUI.TriageSeverityCombobox.IsEnabled = isNotScaEngine;
            cxWindowUI.TriageStateCombobox.IsEnabled = isNotScaEngine;
            cxWindowUI.TriageComment.Visibility = isNotScaEngine ? Visibility.Visible : Visibility.Hidden;
            cxWindowUI.TriageUpdateBtn.Visibility = isNotScaEngine ? Visibility.Visible : Visibility.Hidden;
            cxWindowUI.ResultTabControl.Margin = isNotScaEngine ? new Thickness(0, 10, 0, 0) : new Thickness(0, -45, 0, 0);

            // Set description tab as selected when drawing result info panel
            cxWindowUI.DescriptionTabItem.IsSelected = true;
            cxWindowUI.TriageComment.Text = CxConstants.TRIAGE_COMMENT_PLACEHOLDER;
            cxWindowUI.TriageComment.Foreground = new SolidColorBrush(Colors.Gray);

            cxWindowUI.TriageStateCombobox.Items.Clear();

            if (this.result.Type == "sast")
                {
                foreach (State state in states)
                {
                    string formattedState = UIUtils.FormatStateName(state.name);

                    ComboBoxItem item = new ComboBoxItem { Content = formattedState, Tag = state.name };

                    cxWindowUI.TriageStateCombobox.Items.Add(item);
                }
            }

        
            else
            {

            

            foreach (SystemState state in Enum.GetValues(typeof(SystemState)))
            {
                if (isNotScaEngine && (state == SystemState.IGNORED || state == SystemState.NOT_IGNORED)) continue;

                cxWindowUI.TriageStateCombobox.Items.Add(new ComboBoxItem { Content = UIUtils.FormatStateName(state.ToString()), Tag = state.ToString() });
            }
            }



            DrawTitle();
            DrawDesrciptionTab();
        }

        // Draw title
        private void DrawTitle()
        {
            Image severityIcon = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri(CxUtils.GetIconPathFromSeverity(result.Severity, true), UriKind.RelativeOrAbsolute));
            severityIcon.Source = bitmapImage;

            cxWindowUI.ResultSeverityIcon.Source = bitmapImage;
            cxWindowUI.ResultTitle.Text = result.Data.QueryName ?? result.Id;
            cxWindowUI.CodebashingTextBlock.Visibility = result.Type.Equals("sast") ? Visibility.Visible : Visibility.Hidden;

            if(cxWindowUI.CodebashingTextBlock.Visibility == Visibility.Visible)
            {
                cxWindowUI.CodebashingTextBlock.ToolTip = string.Format(CxConstants.CODEBASHING_LINK_TOOLTIP, result.Data.QueryName);
            }
        }

        // Draw description tab
        private void DrawDesrciptionTab()
        {
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
        public async Task TriageUpdateAsync(Button triageUpdateBtn, CxToolbar cxToolbar, ComboBox severityCombobox, ComboBox stateCombobox, string selectedTabItem, StackPanel triageChangesTab, TextBox triageComment)
        {
            triageUpdateBtn.IsEnabled = false;
            severityCombobox.IsEnabled = false;
            stateCombobox.IsEnabled = false;
            triageComment.IsEnabled = false;
            triageComment.Foreground = new SolidColorBrush(Colors.Gray);

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;
            string similarityId = result.SimilarityId;
            string engineType = result.Type;
            string state = (stateCombobox.SelectedValue as ComboBoxItem).Tag as string;
            string severity = (severityCombobox.SelectedValue as ComboBoxItem).Content as string;
            string comment = triageComment.Text.Equals(CxConstants.TRIAGE_COMMENT_PLACEHOLDER) ? string.Empty : triageComment.Text;

            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
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
                    CxUtils.DisplayMessageInInfoBar(cxToolbar.Package, string.Format(CxConstants.TRIAGE_UPDATE_FAILED, ex.Message), KnownMonikers.StatusWarning);
                    
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

            cxToolbar.ResultsTreePanel.Redraw(false);

            if (selectedTabItem.Equals("ChangesTabItem"))
            {
                await TriageShowAsync(cxToolbar, triageChangesTab);
            }

            DrawTitle();

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
        public async Task TriageShowAsync(CxToolbar cxToolbar, StackPanel triageChangesTab)
        {
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

            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
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
                    CxUtils.DisplayMessageInInfoBar(cxToolbar.Package, string.Format(CxConstants.TRIAGE_SHOW_FAILED, ex.Message), KnownMonikers.StatusWarning);
                    errorMessage = ex.Message;

                    return null;
                }
            });

            triageChangesTab.Children.Clear();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                triageChangesTab.Children.Add(UIUtils.CreateTextBlock(string.Format(CxConstants.TRIAGE_SHOW_FAILED, errorMessage)));

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

                predicate.State = predicate.State.Replace("_", "__");

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
        /// Get codebashing link
        /// </summary>
        /// <param name="cxToolbar"></param>
        /// <returns></returns>
        public async Task CodeBashingListAsync(CxToolbar cxToolbar)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
            if (cxWrapper == null) return;

            await Task.Run(() =>
            {
                try
                {
                    CodeBashing codeBashing = cxWrapper.CodeBashingList(result.VulnerabilityDetails.CweId, result.Data.LanguageName, result.Data.QueryName)[0];

                    System.Diagnostics.Process.Start(codeBashing.Path);
                }
                catch (CxException ex)
                {
                    if (ex.ExitCode == CxConstants.LICENSE_NOT_FOUND_EXIT_CODE)
                    {
                        CxUtils.DisplayMessageInInfoWithLinkBar(cxToolbar.Package, CxConstants.CODEBASHING_NO_LICENSE, KnownMonikers.StatusWarning, CxConstants.CODEBASHING_LINK, CxConstants.CODEBASHING_OPEN_HTTP_LINK_ID);
                    }
                    else if (ex.ExitCode == CxConstants.LESSON_NOT_FOUND_EXIT_CODE)
                    {
                        CxUtils.DisplayMessageInInfoBar(cxToolbar.Package, CxConstants.CODEBASHING_NO_LESSON, KnownMonikers.StatusWarning);
                    }
                }
                catch(Exception ex)
                {
                    //TODO: send error message to log
                    CxUtils.DisplayMessageInInfoBar(cxToolbar.Package, string.Format(CxConstants.ERROR_GETTING_CODEBASHING_LINK, ex.Message), KnownMonikers.StatusError);
                }
            });
        }

        /// <summary>
        /// Clear Result Info panel
        /// </summary>
        public void Clear()
        {
            cxWindowUI.ResultSeverityIcon.Source = null;
            cxWindowUI.ResultTitle.Text = string.Empty;
            cxWindowUI.ResultInfoStackPanel.Children.Clear();
            cxWindowUI.ResultInfoPanel.Visibility = Visibility.Hidden;
        }
    }
}
