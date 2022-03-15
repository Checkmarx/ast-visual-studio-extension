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
    internal class ProjectsCombobox
    {
        private readonly CxToolbar cxToolbar;
        private readonly BranchesCombobox branchesCombobox;

        public ProjectsCombobox(CxToolbar cxToolbar, BranchesCombobox branchesCombobox)
        {
            this.cxToolbar = cxToolbar;
            this.branchesCombobox = branchesCombobox;

            _ = LoadProjectsAsync();
        }

        /// <summary>
        /// Populate Projects combobox
        /// </summary>
        /// <returns></returns>
        private async Task LoadProjectsAsync()
        {
            CxWrapper cxWrapper = CxUtils.GetCxWrapper(cxToolbar.Package, cxToolbar.ResultsTree);
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
                    return null;
                }
            });

            if (!string.IsNullOrEmpty(errorMessage))
            {
                cxToolbar.ResultsTree.Items.Clear();
                cxToolbar.ResultsTree.Items.Add(errorMessage);

                cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_SELECT_PROJECT;
                cxToolbar.ProjectsCombo.IsEnabled = true;
                cxToolbar.ScansCombo.IsEnabled = true;

                return;
            }

            for (int i = 0; i < projects.Count; i++)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem
                {
                    Content = projects[i].Name,
                    Tag = projects[i]
                };
                cxToolbar.ProjectsCombo.Items.Add(comboBoxItem);
            }

            cxToolbar.ProjectsCombo.Text = CxConstants.TOOLBAR_SELECT_PROJECT;
            cxToolbar.ProjectsCombo.IsEnabled = true;
            cxToolbar.ScansCombo.IsEnabled = true;
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

            cxToolbar.EnableCombos(false);

            cxToolbar.BranchesCombo.Text = CxConstants.TOOLBAR_LOADING_BRANCHES;
            cxToolbar.ScansCombo.Items.Clear();
            cxToolbar.ScansCombo.Text = string.IsNullOrEmpty(CxToolbar.currentScanId) ? CxConstants.TOOLBAR_SELECT_SCAN : CxToolbar.currentScanId;

            cxToolbar.ResultsTreePanel.ClearAllPanels();
            
            Project selectedProject = (projectsCombo.SelectedItem as ComboBoxItem).Tag as Project;

            _ = branchesCombobox.LoadBranchesAsync(selectedProject.Id);
        }
    }
}
