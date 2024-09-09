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
    internal class ProjectsCombobox
    {
        private readonly CxToolbar cxToolbar;
        private readonly BranchesCombobox branchesCombobox;
        private bool initialized = false;
        private List<ComboBoxItem> _allProjects;
        private string _previousText = string.Empty;
        private bool _isFiltering = false;

        public ProjectsCombobox(CxToolbar cxToolbar, BranchesCombobox branchesCombobox)
        {
            this.cxToolbar = cxToolbar;
            this.branchesCombobox = branchesCombobox;
            _allProjects = new List<ComboBoxItem>();
            _ = LoadProjectsAsync();
        }

        /// <summary>
        /// Populate Projects combobox
        /// </summary>
        /// <returns></returns>
        public async Task LoadProjectsAsync()
        {
            await LoadProjectsComboboxAsync();

            string projectId = SettingsUtils.GetToolbarValue(cxToolbar.Package, SettingsUtils.projectIdProperty);

            if (!string.IsNullOrEmpty(projectId))
            {
                cxToolbar.ProjectsCombo.SelectedIndex = CxUtils.GetItemIndexInCombo(projectId, cxToolbar.ProjectsCombo, Enums.ComboboxType.PROJECTS);

                if (cxToolbar.ProjectsCombo.SelectedIndex == -1)
                {
                    cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
                    cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
                }
            }

            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.ScansCombo.IsEnabled = true;
            CxToolbar.redrawExtension = false;
            initialized = true;
        }

        /// <summary>
        /// Reset extension
        /// </summary>
        /// <returns></returns>
        public async Task ResetExtensionAsync()
        {
            cxToolbar.ProjectsCombo.Items.Clear();
            cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_LOADING_PROJECTS;
            cxToolbar.ProjectsCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.Items.Clear();
            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_LOADING_SCANS;
            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ResultsTreePanel.ClearAll();
            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.projectIdProperty, string.Empty);
            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.branchProperty, string.Empty);
            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.scanIdProperty, string.Empty);

            await LoadProjectsComboboxAsync();

            cxToolbar.ScansCombo.IsEnabled = true;
            cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
            CxToolbar.resetExtension = false;
        }

        /// <summary>
        /// Load projects combobox
        /// </summary>
        /// <returns></returns>
        private async Task LoadProjectsComboboxAsync()
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree, GetType());
            if (cxWrapper == null)
            {
                cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_SELECT_PROJECT;
                return;
            }

            string errorMessage = string.Empty;

            List<Project> projects = await Task.Run(() =>
            {
                try
                {
                    return cxWrapper.GetProjects();
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return new List<Project>();
                }
            });

            if (!string.IsNullOrEmpty(errorMessage))
            {
                cxToolbar.ResultsTree.Items.Clear();
                cxToolbar.ResultsTree.Items.Add(errorMessage);

                cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;
                cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_SELECT_PROJECT;
                cxToolbar.ProjectsCombo.IsEnabled = true;
                cxToolbar.ScansCombo.Text = CxConstants.TOOLBAR_SELECT_SCAN;
                cxToolbar.ScansCombo.IsEnabled = true;

                return;
            }

            cxToolbar.ProjectsCombo.Items.Clear();
            _allProjects.Clear();

            for (int i = 0; i < projects.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = projects[i].Name,
                    Tag = projects[i]
                };
                cxToolbar.ProjectsCombo.Items.Add(comboBoxItem);
                _allProjects.Add(comboBoxItem);
            }

            cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_SELECT_PROJECT;
        }

        /// <summary>
        /// On change event for Projects combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnChangeProject(object sender, SelectionChangedEventArgs e)
        {
            ComboBox projectsCombo = cxToolbar.ProjectsCombo;
            if (projectsCombo == null || projectsCombo.SelectedItem == null || projectsCombo.SelectedIndex == -1) return;

            ComboBoxItem selectedProject = projectsCombo.SelectedItem as ComboBoxItem;

            _previousText = selectedProject.Content.ToString();
            if (_isFiltering)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _isFiltering = false;
                UpdateProjectsComboBox(_allProjects);
                cxToolbar.ProjectsCombo.SelectedItem = selectedProject;
                Mouse.OverrideCursor = null;
            }

            cxToolbar.EnableCombos(false);

            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_SELECT_SCAN : CxToolbar.currentScanId;

            cxToolbar.ResultsTreePanel.ClearAll();


            SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.projectIdProperty, (selectedProject.Tag as Project).Id);
            if (initialized)
            {
                SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.branchProperty, string.Empty);
                SettingsUtils.StoreToolbarValue(cxToolbar.Package, SettingsUtils.toolbarCollection, SettingsUtils.scanIdProperty, string.Empty);
            }

            _ = branchesCombobox.LoadBranchesAsync((selectedProject.Tag as Project).Id);

            cxToolbar.ScanButtonByCombos();
        }

        public void OnProjectTextChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox comboBox)) return;
            {
                string newText = comboBox.Text;
                if (newText == _previousText) return;
                {
                    int savedSelectionStart = 0;
                    var textBox = (TextBox)cxToolbar.ProjectsCombo.Template.FindName("PART_EditableTextBox", cxToolbar.ProjectsCombo);
                    Mouse.OverrideCursor = Cursors.Wait;
                    if (textBox != null)
                    {
                        savedSelectionStart = textBox.SelectionStart;
                        _previousText = newText;
                        ResetCombosAndResults();
                        cxToolbar.ProjectsCombo.SelectedItem = null;

                        if (string.IsNullOrEmpty(newText))
                        {
                            UpdateProjectsComboBox(_allProjects);
                        }
                        else
                        {
                            var filteredItems = _allProjects.Where(item => item.Content.ToString().IndexOf(newText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                            UpdateProjectsComboBox(filteredItems);
                            _isFiltering = true;
                        }
                    }
                    Mouse.OverrideCursor = null;
                    cxToolbar.ProjectsCombo.IsDropDownOpen = true;
                    cxToolbar.ProjectsCombo.Text = newText;

                    textBox.SelectionStart = Math.Min(savedSelectionStart, newText.Length);
                    textBox.SelectionLength = 0;
                }
            }
        }
        private void UpdateProjectsComboBox(List<ComboBoxItem> items)
        {
            cxToolbar.ProjectsCombo.Items.Clear();
            foreach (var item in items)
            {
                cxToolbar.ProjectsCombo.Items.Add(item);
            }
        }
        private void ResetCombosAndResults()
        {
            cxToolbar.BranchesCombo.IsEnabled = false;
            cxToolbar.BranchesCombo.Items.Clear();
            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_SELECT_BRANCH;

            cxToolbar.ScansCombo.IsEnabled = false;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_SELECT_SCAN : CxToolbar.currentScanId;

            cxToolbar.ResultsTreePanel.ClearAll();
        }
    }
}
