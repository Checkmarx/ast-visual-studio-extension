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
    internal class ScansCombobox
    {
        private readonly CxToolbar cxToolbar;
        private List<ComboBoxItem> _allScans;
        private string _previousText = string.Empty;
        private bool _isFiltering = false;

        public ScansCombobox(CxToolbar cxToolbar)
        {
            this.cxToolbar = cxToolbar;
            _allScans = new List<ComboBoxItem>();
        }

        /// <summary>
        /// Populate Scans combobox
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public async Task LoadScansAsync(string projectId, string branch)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
            if (cxWrapper == null)
            {
                cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
                return;
            }

            string errorMessage = string.Empty;

            List<Scan> scans = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.GetScans(projectId, branch);
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
            cxToolbar.ScansCombo.Items.Clear();
            _allScans.Clear();
            foreach (Scan scan in scans)
            {
                DateTime scanCreatedAt = DateTime.Parse(scan.CreatedAt, System.Globalization.CultureInfo.InvariantCulture);
                string createdAt = scanCreatedAt.ToString(CxConstants.DATE_OUTPUT_FORMAT);

                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = string.Format(CxConstants.SCAN_ID_DISPLAY_FORMAT, createdAt, scan.ID),
                    Tag = scan,
                };

                _allScans.Add(comboBoxItem);
                cxToolbar.ScansCombo.Items.Add(comboBoxItem);
            }
            cxToolbar.ScansCombo.IsEnabled = true;
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.BranchesCombo.IsEnabled = true;

            if (CxToolbar.reverseSearch)
            {
                cxToolbar.ScansCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(CxToolbar.currentScanId, cxToolbar.ScansCombo, Enums.ComboboxType.SCANS);
            }
            else
            {
                string scanId = SettingsUtils.GetToolbarValue(cxToolbar.Package, SettingsUtils.scanIdProperty);

                if (!string.IsNullOrEmpty(scanId))
                {
                    cxToolbar.ScansCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(scanId, cxToolbar.ScansCombo, Enums.ComboboxType.SCANS);
                }
            }

            CxToolbar.reverseSearch = false;
        }

        /// <summary>
        /// On change event for Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeScan(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ComboBox scansCombo) || scansCombo.SelectedItem == null || scansCombo.SelectedIndex == -1) return;

            ComboBoxItem selectedScan = (scansCombo.SelectedItem as ComboBoxItem);
            string selectedScanID = (selectedScan.Tag as Scan).ID;
           
            _previousText = selectedScan.Content.ToString();

            if (_isFiltering)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _isFiltering = false;
                UpdateScansComboBox(_allScans);
                cxToolbar.ScansCombo.SelectedItem = selectedScan;
                Mouse.OverrideCursor = null;
            }
            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, "scanId", selectedScanID);

            _ = cxToolbar.ResultsTreePanel.DrawAsync(selectedScanID, cxToolbar);

            CxToolbar.currentBranch = string.Empty;
            CxToolbar.currentScanId = string.Empty;
        }

        public async Task LoadScanByIdAsync(string scanId)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
            if (cxWrapper == null) return;

            string currentProjectName = cxToolbar.ProjectsCombo.SelectedItem is ComboBoxItem projectCombo ? (projectCombo.Tag as Project).Name : CxConstants.TOOLBAR_SELECT_PROJECT;
            string currentBranch = cxToolbar.BranchesCombo.SelectedItem is ComboBoxItem branchCombo ? branchCombo.Content.ToString() : CxConstants.TOOLBAR_SELECT_BRANCH;

            CxToolbar.reverseSearch = true;
            cxToolbar.ProjectsCombo.IsEnabled = false;
            cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_LOADING_PROJECTS;
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ResultsTreePanel.ClearAll();

            Scan scan = null;

            string scanShowSuccessfully = await Task.Run(() =>
            {
                try
                {
                    scan = cxWrapper.ScanShow(scanId);

                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            });

            if (string.IsNullOrEmpty(scanShowSuccessfully))
            {
                CxToolbar.currentBranch = scan.Branch;
                CxToolbar.currentScanId = scanId;

                cxToolbar.ProjectsCombo.SelectedIndex = -1; // used to trigger onChangeProjects when the provided scanId belongs to the current project
                cxToolbar.ProjectsCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(scan.ProjectId, cxToolbar.ProjectsCombo, Enums.ComboboxType.PROJECTS);
            }
            else
            {
                cxToolbar.ProjectsCombo.IsEnabled = true;
                cxToolbar.ProjectsCombo.Text = currentProjectName;
                cxToolbar.BranchesCombo.IsEnabled = cxToolbar.ProjectsCombo.SelectedIndex != -1;
                cxToolbar.BranchesCombo.Text = currentBranch;
                cxToolbar.ScansCombo.IsEnabled = true;

                AddMessageToTree(scanShowSuccessfully);
            }
        }

        /// <summary>
        /// Add a message to the results tree
        /// </summary>
        /// <param name="message"></param>
        private void AddMessageToTree(string message)
        {
            cxToolbar.ResultsTreePanel.ClearAll();
            cxToolbar.ResultsTree.Items.Clear();
            cxToolbar.ResultsTree.Items.Add(message);
        }
        public void OnScanTextChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox comboBox)) return;
            {
                string newText = comboBox.Text;
                if (newText == _previousText) return;
                {
                    int savedSelectionStart = 0;
                    var textBox = (TextBox)cxToolbar.ScansCombo.Template.FindName("PART_EditableTextBox", cxToolbar.ScansCombo);

                    if (textBox != null)
                    {
                        savedSelectionStart = textBox.SelectionStart;
                        _previousText = newText;
                        cxToolbar.ResultsTreePanel.ClearAll();
                        cxToolbar.ScansCombo.Items.Clear();
                        Mouse.OverrideCursor = Cursors.Wait;
                        if (string.IsNullOrEmpty(newText))
                        {
                            UpdateScansComboBox(_allScans);
                        }
                        else
                        {
                            var filteredItems = _allScans.Where(item => item.Content.ToString().IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                            UpdateScansComboBox(filteredItems);
                            _isFiltering = true;
                        }
                    }
                    Mouse.OverrideCursor = null;
                    cxToolbar.ScansCombo.IsDropDownOpen = true;
                    cxToolbar.ScansCombo.Text = newText;

                    textBox.SelectionStart = Math.Min(savedSelectionStart, newText.Length);
                    textBox.SelectionLength = 0;
                }
            }
        }

        private void UpdateScansComboBox(List<ComboBoxItem> items)
        {
            cxToolbar.ScansCombo.Items.Clear();
            foreach (var item in items)
            {
                cxToolbar.ScansCombo.Items.Add(item);
            }
        }
    }
}
