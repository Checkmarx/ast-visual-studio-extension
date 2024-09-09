using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class BranchesCombobox
    {
        private readonly ScansCombobox scansCombobox;

        private readonly CxToolbar cxToolbar;
        private bool initialized = false;
        private List<ComboBoxItem> _allBranches;
        private string _previousText = string.Empty;
        private bool _isFiltering = false;

        public BranchesCombobox(CxToolbar cxToolbar, ScansCombobox scansCombobox)
        {
            this.cxToolbar = cxToolbar;
            this.scansCombobox = scansCombobox;
            _allBranches = new List<ComboBoxItem>();
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
            _allBranches.Clear();
            for (int i = 0; i < branches.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = branches[i]
                };

                cxToolbar.BranchesCombo.Items.Add(comboBoxItem);
                _allBranches.Add(comboBoxItem);
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
            if (!(sender is ComboBox branchesCombo) || branchesCombo.SelectedItem == null || branchesCombo.SelectedIndex == -1) return;
            ComboBoxItem selectedBranch = branchesCombo.SelectedItem as ComboBoxItem;
            string selectedBranchContent = selectedBranch.Content as string;
            _previousText = selectedBranchContent;
            if (_isFiltering)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _isFiltering = false;
                UpdateBranchesComboBox(_allBranches);
                cxToolbar.BranchesCombo.SelectedItem = selectedBranch;
                Mouse.OverrideCursor = null;
            }

            cxToolbar.EnableCombos(false);
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_LOADING_SCANS : CxToolbar.currentScanId;
            cxToolbar.ResultsTreePanel.ClearAll();

            string projectId = ((cxToolbar.ProjectsCombo.SelectedItem as ComboBoxItem).Tag as ast_visual_studio_extension.CxWrapper.Models.Project).Id;

            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.branchProperty, selectedBranchContent);

            if (initialized)
            {
                SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.scanIdProperty, string.Empty);
            }

            _ = scansCombobox.LoadScansAsync(projectId, selectedBranchContent);

            cxToolbar.ScanButtonByCombos();
        }
        public void OnBranchTextChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox comboBox)) return;
            {
                string newText = comboBox.Text;
                if (newText == _previousText) return;
                {
                    int savedSelectionStart = 0;
                    var textBox = (TextBox)cxToolbar.BranchesCombo.Template.FindName("PART_EditableTextBox", cxToolbar.BranchesCombo);

                    if (textBox != null)
                    {
                        savedSelectionStart = textBox.SelectionStart;
                        _previousText = newText;
                        ResetCombosAndResults();
                        cxToolbar.BranchesCombo.SelectedItem = null;
                        Mouse.OverrideCursor = Cursors.Wait;

                        if (string.IsNullOrEmpty(newText))
                        {
                            UpdateBranchesComboBox(_allBranches);
                        }
                        else
                        {
                            var filteredItems = _allBranches.Where(item => item.Content.ToString().IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                            UpdateBranchesComboBox(filteredItems);
                            _isFiltering = true;
                        }
                    }
                    Mouse.OverrideCursor = null;
                    cxToolbar.BranchesCombo.IsDropDownOpen = true;
                    cxToolbar.BranchesCombo.Text = newText;

                    textBox.SelectionStart = Math.Min(savedSelectionStart, newText.Length);
                    textBox.SelectionLength = 0;
                }
            }
        }
        private void UpdateBranchesComboBox(List<ComboBoxItem> items)
        {
            cxToolbar.BranchesCombo.Items.Clear();
            foreach (var item in items)
            {
                cxToolbar.BranchesCombo.Items.Add(item);
            }
        }
        private void ResetCombosAndResults()
        {
            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_SELECT_SCAN : CxToolbar.currentScanId;

            cxToolbar.ResultsTreePanel.ClearAll();
        }
    }
}
