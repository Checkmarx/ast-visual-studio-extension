using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class ScansCombobox
    {
        private readonly CxToolbar cxToolbar;

        public ScansCombobox(CxToolbar cxToolbar)
        {
            this.cxToolbar = cxToolbar;
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

            foreach(Scan scan in scans)
            {
                DateTime scanCreatedAt = DateTime.Parse(scan.CreatedAt, System.Globalization.CultureInfo.InvariantCulture);
                string createdAt = scanCreatedAt.ToString(CxConstants.DATE_OUTPUT_FORMAT);

                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = string.Format(CxConstants.SCAN_ID_DISPLAY_FORMAT, createdAt, scan.ID),
                    Tag = scan,
                };

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

            string selectedScan = ((scansCombo.SelectedItem as ComboBoxItem).Tag as Scan).ID;

            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, "scanId", selectedScan);

            _ = cxToolbar.ResultsTreePanel.DrawAsync(selectedScan, cxToolbar);

            CxToolbar.currentBranch = string.Empty;
            CxToolbar.currentScanId = string.Empty;
        }

        /// <summary>
        /// On press enter or tab in Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async Task OnTypeScanAsync(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Tab)
            {
                string scanId = ValidateScanId((e.OriginalSource as TextBox).Text);
                if (string.IsNullOrEmpty(scanId)) return;

                CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
                if (cxWrapper == null) return;

                CxToolbar.reverseSearch = true;
                cxToolbar.ProjectsCombo.IsEnabled = false;
                cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_LOADING_PROJECTS;
                cxToolbar.BranchesCombo.IsEnabled = false;
                cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
                cxToolbar.ScansCombo.IsEnabled = false;
                cxToolbar.ResultsTreePanel.ClearAll();

                Scan scan = null;

                bool scanShowSuccessfully = await Task.Run(() =>
                {
                    try
                    {
                        scan = cxWrapper.ScanShow(scanId);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        AddMessageToTree(ex.Message);

                        return false;
                    }
                });

                if (scanShowSuccessfully)
                {
                    CxToolbar.currentBranch = scan.Branch;
                    CxToolbar.currentScanId = scanId;

                    cxToolbar.ProjectsCombo.SelectedIndex = -1; // used to trigger onChangeProjects when the provided scanId belongs to the current project
                    cxToolbar.ProjectsCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(scan.ProjectId, cxToolbar.ProjectsCombo, Enums.ComboboxType.PROJECTS);
                }
            }
        }

        /// <summary>
        /// Validate if provided scan id is valid to get results
        /// </summary>
        /// <param name="scan"></param>
        /// <returns></returns>
        private string ValidateScanId(string scan)
        {
            string scanId = !string.IsNullOrEmpty(scan) ? scan.Trim() : string.Empty;

            bool isValidScanId = Guid.TryParse(scanId, out _) && !string.IsNullOrEmpty(scanId);

            if(isValidScanId) return scanId;

            AddMessageToTree(CxConstants.INVALID_SCAN_ID);

            return string.Empty;
            
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
    }
}
