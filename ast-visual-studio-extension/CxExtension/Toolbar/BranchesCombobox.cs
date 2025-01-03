﻿using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using ast_visual_studio_extension.CxWrapper.Models;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class BranchesCombobox : ComboboxBase
    {
        private readonly ScansCombobox scansCombobox;

        private bool initialized = false;

        public BranchesCombobox(CxToolbar cxToolbar, ScansCombobox scansCombobox)
            : base(cxToolbar, cxToolbar.BranchesCombo)
        {
            this.scansCombobox = scansCombobox;
        }

        /// <summary>
        /// Populate Branches combobox
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task LoadBranchesAsync(string projectId)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
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

            cxToolbar.BranchesCombo.Items.Clear();
            allItems.Clear();
            for (int i = 0; i < branches.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = branches[i]
                };

                cxToolbar.BranchesCombo.Items.Add(comboBoxItem);
                allItems.Add(comboBoxItem);
            }

            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
            cxToolbar.EnableCombos(true);

            if (CxToolbar.reverseSearch)
            {
                cxToolbar.BranchesCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(CxToolbar.currentBranch, cxToolbar.BranchesCombo, Enums.ComboboxType.BRANCHES);
            }
            else
            {
                string branch = SettingsUtils.GetToolbarValue(cxToolbar.Package, SettingsUtils.branchProperty);

                if (!string.IsNullOrEmpty(branch))
                {
                    cxToolbar.BranchesCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(branch, cxToolbar.BranchesCombo, Enums.ComboboxType.BRANCHES);
                }
            }

            initialized = true;
        }

        /// <summary>
        /// On change event for Branches combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeBranch(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox branchesCombo) || branchesCombo.SelectedItem == null || branchesCombo.SelectedIndex == -1)
            {
                cxToolbar.CheckScanButtonStateByCombos();
                return;
            }

            ResetFilteringState(branchesCombo.SelectedItem as ComboBoxItem);

            string selectedBranch = (branchesCombo.SelectedItem as ComboBoxItem).Content as string;

            cxToolbar.EnableCombos(false);
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_LOADING_SCANS : CxToolbar.currentScanId;
            cxToolbar.ResultsTreePanel.ClearAll();

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as Project).Id;

            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.branchProperty, selectedBranch);

            if (initialized)
            {
                SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.scanIdProperty, string.Empty);
            }

            _ = scansCombobox.LoadScansAsync(projectId, selectedBranch);

            cxToolbar.CheckScanButtonStateByCombos();
        }
        protected override void ResetOthersComboBoxesAndResults()
        {
            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_SELECT_SCAN : CxToolbar.currentScanId;

            cxToolbar.ResultsTreePanel.ClearAll();
        }
    }
}
