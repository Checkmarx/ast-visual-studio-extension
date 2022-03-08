using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI.Models;
using ast_visual_studio_extension.CxExtension.Utils;
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
            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
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

            for (int i = 0; i < scans.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = scans[i].ID
                };

                cxToolbar.ScansCombo.Items.Add(comboBoxItem);
            }

            cxToolbar.ScansCombo.IsEnabled = true;
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.BranchesCombo.IsEnabled = true;

            if (!string.IsNullOrEmpty(CxToolbar.currentScanId))
            {
                cxToolbar.ScansCombo.SelectedIndex = GetScanIndex(CxToolbar.currentScanId);
            }
        }

        /// <summary>
        /// Get index of current scan in Scans combobox
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        private int GetScanIndex(string scanId)
        {
            for (var i = 0; i < cxToolbar.ScansCombo.Items.Count; i++)
            {
                ComboBoxItem item = cxToolbar.ScansCombo.Items[i] as ComboBoxItem;

                string p = item.Content as string;

                if (p.Equals(scanId)) return i;
            }

            return -1;
        }

        /// <summary>
        /// On change event for Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeScan(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedItem == null) return;

            string selectedScan = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;

            _ = cxToolbar.ResultsTreePanel.DrawAsync(selectedScan, cxToolbar);

            CxToolbar.currentBranch = string.Empty;
            CxToolbar.currentScanId = string.Empty;
        }

        /// <summary>
        /// On press enter or tab in Scans combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnKeyDownScans(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Tab)
            {
                string scanId = (e.OriginalSource as TextBox).Text;

                CxWrapper wrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);

                Scan scan = wrapper.ScanShow(scanId);

                string projectId = scan.ProjectId;

                int projectIdx = GetProjectIndex(projectId);

                bool needsToTrigger = ScanExist(scanId);

                CxToolbar.currentBranch = scan.Branch;
                CxToolbar.currentScanId = scanId;

                cxToolbar.ProjectsCombo.SelectedIndex = projectIdx;

                if (needsToTrigger)
                {
                    cxToolbar.ProjectsCombobox.OnChangeProject(null, null);
                }
            }
        }

        /// <summary>
        /// Check if scan exist in the Scans combobox
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        private bool ScanExist(string scanId)
        {
            for (var i = 0; i < cxToolbar.ScansCombo.Items.Count; i++)
            {
                ComboBoxItem item = cxToolbar.ScansCombo.Items[i] as ComboBoxItem;

                string p = item.Content as string;

                if (p.Equals(scanId)) return true;
            }

            return false;
        }

        /// <summary>
        /// Get index of a given project in Projects combobox
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private int GetProjectIndex(string projectId)
        {
            for (var i = 0; i < cxToolbar.ProjectsCombo.Items.Count; i++)
            {
                ComboBoxItem item = cxToolbar.ProjectsCombo.Items[i] as ComboBoxItem;

                Project p = item.Tag as Project;

                if (p.Id == projectId) return i;
            }

            return -1;
        }
    }
}
