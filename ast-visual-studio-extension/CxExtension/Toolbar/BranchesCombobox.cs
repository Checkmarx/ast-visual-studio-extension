using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class BranchesCombobox
    {
        private readonly ScansCombobox scansCombobox;

        private readonly CxToolbar cxToolbar;

        public BranchesCombobox(CxToolbar cxToolbar, ScansCombobox scansCombobox)
        {
            this.cxToolbar = cxToolbar;
            this.scansCombobox = scansCombobox;
        }

        /// <summary>
        /// Populate Branches combobox
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task LoadBranchesAsync(string projectId)
        {
            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
            if (cxWrapper == null)
            {
                cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
                return;
            }

            string errorMessage = string.Empty;

            List<string> branches = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.GetBranches(projectId);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return null;
                }
            });

            if (!string.IsNullOrEmpty(errorMessage))
            {
                cxToolbar.ResultsTree.Items.Clear();
                cxToolbar.ResultsTree.Items.Add(errorMessage);

                return;
            }

            for (int i = 0; i < branches.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = branches[i]
                };

                cxToolbar.BranchesCombo.Items.Add(comboBoxItem);
            }

            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
            cxToolbar.EnableCombos(true);

            if(!string.IsNullOrEmpty(CxToolbar.currentBranch))
            {
                cxToolbar.BranchesCombo.SelectedIndex = GetBranchIndex(CxToolbar.currentBranch);
            }
        }

        /// <summary>
        /// Get branch index in the Branches combobox
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        private int GetBranchIndex(string branch)
        {
            for (var i = 0; i < cxToolbar.BranchesCombo.Items.Count; i++)
            {
                ComboBoxItem item = cxToolbar.BranchesCombo.Items[i] as ComboBoxItem;

                string p = item.Content as string;

                if (p.Equals(branch)) return i;
            }

            return -1;
        }

        /// <summary>
        /// On change event for Branches combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeBranch(object sender, SelectionChangedEventArgs e)
        {
            cxToolbar.EnableCombos(false);
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
            cxToolbar.ResultsTreePanel.ClearAllPanels();

            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem == null) return;

            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_LOADING_SCANS;

            string selectedBranch = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;

            _ = scansCombobox.LoadScansAsync(projectId, selectedBranch);
        }
    }
}
