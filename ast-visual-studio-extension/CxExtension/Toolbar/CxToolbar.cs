using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class CxToolbar
    {

        public static string currentProjectId = string.Empty;
        public static string currentBranch = string.Empty;
        public static string currentScanId = string.Empty;

        public AsyncPackage Package { get; set; }
        public ResultsTreePanel ResultsTreePanel { get; set; }
        public ComboBox ProjectsCombo { get; set; }
        public ComboBox BranchesCombo { get; set; }
        public ComboBox ScansCombo { get; set; }
        public TreeView ResultsTree { get; set; }
        public ProjectsCombobox ProjectsCombobox { get; set; }
        public BranchesCombobox BranchesCombobox { get; set; }
        public ScansCombobox ScansCombobox { get; set; }
        public Dictionary<Severity, ToggleButton> SeverityFilters { get; set; }
        public Dictionary<Severity, Image> SeverityFilterImages { get; set; }
        public Dictionary<State, MenuItem> StateFilters { get; set; }
        public Dictionary<GroupBy, MenuItem> GroupByOptions { get; set; }

        public static CxToolbar Builder()
        {
            return new CxToolbar();
        }

        public CxToolbar WithPackage(AsyncPackage package)
        {
            Package = package;
            return this;
        }

        public CxToolbar WithResultsTreePanel(ResultsTreePanel resultsTreePanel)
        {
            ResultsTreePanel = resultsTreePanel;
            return this;
        }

        public CxToolbar WithProjectsCombo(ComboBox projectsCombo)
        {
            ProjectsCombo = projectsCombo;
            return this;
        }

        public CxToolbar WithBranchesCombo(ComboBox branchesCombo)
        {
            BranchesCombo = branchesCombo;
            return this;
        }

        public CxToolbar WithScansCombo(ComboBox scansCombo)
        {
            ScansCombo = scansCombo;
            return this;
        }

        public CxToolbar WithResultsTree(TreeView resultsTree)
        {
            ResultsTree = resultsTree;
            return this;
        }

        public CxToolbar WithSeverityFilters(Dictionary<Severity, ToggleButton> severityFilters, Dictionary<Severity, Image> severityFilterImages)
        {
            SeverityFilters = severityFilters;
            SeverityFilterImages = severityFilterImages;
            return this;
        }

        public CxToolbar WithStateFilters(Dictionary<State, MenuItem> stateFilters)
        {
            StateFilters = stateFilters;
            return this;
        }

        public CxToolbar WithGroupByOptions(Dictionary<GroupBy, MenuItem> groupByOptions)
        {
            GroupByOptions = groupByOptions;
            return this;
        }

        /// <summary>
        /// Initialize toolbar elements
        /// </summary>
        public void Init()
        {
            ScansCombobox = new ScansCombobox(this);
            BranchesCombobox = new BranchesCombobox(this, ScansCombobox);
            ProjectsCombobox = new ProjectsCombobox(this, BranchesCombobox);

            var readOnlyStore = new ShellSettingsManager(Package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (KeyValuePair<Severity, ToggleButton> pair in SeverityFilters)
            {
                var severity = pair.Key;
                var control = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, severity.ToString(), SettingsUtils.severityDefaultValues[severity]);
            }
            foreach (KeyValuePair<Severity, Image> pair in SeverityFilterImages)
            {
                var severity = pair.Key;
                var control = pair.Value;
                control.Source = new BitmapImage(new Uri(CxUtils.GetIconPathFromSeverity(severity.ToString(), true)));
            }
            foreach (KeyValuePair<State, MenuItem> pair in StateFilters)
            {
                var state = pair.Key;
                var control = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.stateCollection, state.ToString(), SettingsUtils.stateDefaultValues[state]);
            }
            foreach (KeyValuePair<GroupBy, MenuItem> pair in GroupByOptions)
            {
                var groupBy = pair.Key;
                var control = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.groupByCollection, groupBy.ToString(), SettingsUtils.groupByDefaultValues[groupBy]);
            }
        }

        /// <summary>
        /// Set enable property for comboboxes
        /// </summary>
        /// <param name="enable"></param>
        public void EnableCombos(bool enable)
        {
            ProjectsCombo.IsEnabled = enable;
            BranchesCombo.IsEnabled = enable;
            ScansCombo.IsEnabled = enable;
        }

        public void SeverityFilterClick(Severity severity)
        {
            SettingsUtils.Store(Package, SettingsUtils.severityCollection, severity, SettingsUtils.severityDefaultValues);
            ResultsTreePanel.Redraw();
        }

        public void StateFilterClick(State state)
        {
            SettingsUtils.Store(Package, SettingsUtils.stateCollection, state, SettingsUtils.stateDefaultValues);
            ResultsTreePanel.Redraw();
        }

        public void GroupByClick(GroupBy groupBy)
        {
            SettingsUtils.Store(Package, SettingsUtils.groupByCollection, groupBy, SettingsUtils.groupByDefaultValues);
            ResultsTreePanel.Redraw();
        }
    }
}
