using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TaskStatusCenter;
using System.Threading;
using ast_visual_studio_extension.CxCLI;
using Microsoft.VisualStudio.Imaging;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class CxToolbar
    {

        public static string currentProjectId = string.Empty;
        public static string currentBranch = string.Empty;
        public static string currentScanId = string.Empty;
        public static bool resetExtension = false;
        public static bool redrawExtension = false;
        public static bool reverseSearch = false;
        public static CxToolbar instance = null;

        public AsyncPackage Package { get; set; }
        public ResultsTreePanel ResultsTreePanel { get; set; }
        public ComboBox ProjectsCombo { get; set; }
        public ComboBox BranchesCombo { get; set; }
        public ComboBox ScansCombo { get; set; }
        public TreeView ResultsTree { get; set; }
        public ProjectsCombobox ProjectsCombobox { get; set; }
        public BranchesCombobox BranchesCombobox { get; set; }
        public ScansCombobox ScansCombobox { get; set; }
        public Dictionary<ToggleButton, Severity> SeverityFilters { get; set; }
        public Dictionary<Severity, Image> SeverityFilterImages { get; set; }
        public Dictionary<MenuItem, State> StateFilters { get; set; }
        public Dictionary<MenuItem, GroupBy> GroupByOptions { get; set; }
        public StackPanel ScanningSeparator { get; set; }
        public ToggleButton ScanStartButton { get; set; }

        private static bool initPolling = false;

        public static CxToolbar Builder()
        {
            instance = new CxToolbar();
            return instance;
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

        public CxToolbar WithSeverityFilters(Dictionary<ToggleButton, Severity> severityFilters, Dictionary<Severity, Image> severityFilterImages)
        {
            SeverityFilters = severityFilters;
            SeverityFilterImages = severityFilterImages;
            return this;
        }

        public CxToolbar WithStateFilters(Dictionary<MenuItem, State> stateFilters)
        {
            StateFilters = stateFilters;
            return this;
        }

        public CxToolbar WithGroupByOptions(Dictionary<MenuItem, GroupBy> groupByOptions)
        {
            GroupByOptions = groupByOptions;
            return this;
        }

        public CxToolbar WithScanButtons(StackPanel scanningSeparator, ToggleButton scanStartButton)
        {
            ScanStartButton = scanStartButton;
            ScanningSeparator = scanningSeparator;
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

            if (resetExtension)
            {
                _ = ProjectsCombobox.ResetExtensionAsync();
            }

            if (redrawExtension)
            {
                _ = ProjectsCombobox.LoadProjectsAsync();
            }

            var readOnlyStore = new ShellSettingsManager(Package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (KeyValuePair<ToggleButton, Severity> pair in SeverityFilters)
            {
                var control = pair.Key;
                var severity = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, severity.ToString(), SettingsUtils.severityDefaultValues[severity]);
            }
            foreach (KeyValuePair<Severity, Image> pair in SeverityFilterImages)
            {
                var severity = pair.Key;
                var control = pair.Value;
                control.Source = new BitmapImage(new Uri(CxUtils.GetIconPathFromSeverity(severity.ToString(), true), UriKind.RelativeOrAbsolute));
            }
            foreach (KeyValuePair<MenuItem, State> pair in StateFilters)
            {
                var control = pair.Key;
                var state = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.stateCollection, state.ToString(), SettingsUtils.stateDefaultValues[state]);
            }
            foreach (KeyValuePair<MenuItem, GroupBy> pair in GroupByOptions)
            {
                var control = pair.Key;
                var groupBy = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.groupByCollection, groupBy.ToString(), SettingsUtils.groupByDefaultValues[groupBy]);
            }

            if (!initPolling)
            {
                initPolling = true;
                _ = PollScanStartedAsync();
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

        public void SeverityFilterClick(ToggleButton severityControl)
        {
            SettingsUtils.Store(Package, SettingsUtils.severityCollection, SeverityFilters[severityControl], SettingsUtils.severityDefaultValues);
            ResultsTreePanel.Redraw(true);
        }

        public void StateFilterClick(MenuItem stateControl)
        {
            SettingsUtils.Store(Package, SettingsUtils.stateCollection, StateFilters[stateControl], SettingsUtils.stateDefaultValues);
            ResultsTreePanel.Redraw(true);
        }

        public void GroupByClick(MenuItem groupByControl)
        {
            SettingsUtils.Store(Package, SettingsUtils.groupByCollection, GroupByOptions[groupByControl], SettingsUtils.groupByDefaultValues);
            ResultsTreePanel.Redraw(true);
        }

        public void ScanStart_Click()
        {
            SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, string.Empty);
            _ = ScanStartedAsync();
        }

        public async Task ScanStartedAsync()
        {
            var tsc = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            var options = default(TaskHandlerOptions);
            options.Title = "Checkmarx: Creating a scan";
            options.ActionsAfterCompletion = CompletionActions.None;
            TaskProgressData data = default;
            ITaskHandler handler = tsc.PreRegister(options, data);
            var t = StartScanAsync();
            handler.RegisterTask(t);
            await t;

            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (string.IsNullOrEmpty(scanId))
            {
                return;
            }

            await PollScanStartedAsync();
        }

        private async Task StartScanAsync()
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (cxWrapper == null || !string.IsNullOrWhiteSpace(scanId)) return;

            string errorMessage = string.Empty;

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { CxCLI.CxConstants.FLAG_SOURCE, "." },
                { CxCLI.CxConstants.FLAG_PROJECT_NAME, ProjectsCombo.Text },
                { CxCLI.CxConstants.FLAG_BRANCH, BranchesCombo.Text },
                { CxCLI.CxConstants.FLAG_AGENT, "Visual Studio" }
            };

            UpdateStatusBar("Checkmarx: Creating scan");
            Scan scan = await cxWrapper.ScanCreateAsync(parameters, "--async --sast-incremental --resubmit");

            if (scan != null)
            {
                SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, scan.ID);
                UpdateStatusBar("Checkmarx: Scan created with ID " + scan.ID);
            }
            else
            {
                UpdateStatusBar("Checkmarx: Failed to create scan: " + errorMessage);
            }
        }

        private async Task PollScanStartedAsync()
        {
            var tsc = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (string.IsNullOrWhiteSpace(scanId)) return;
            var options = default(TaskHandlerOptions);
            options.Title = "Checkmarx: Scan running with ID " + scanId;
            options.ActionsAfterCompletion = CompletionActions.None;
            TaskProgressData data = default;
            data.CanBeCanceled = true;
            ITaskHandler handler = tsc.PreRegister(options, data);
            var token = handler.UserCancellation;
            var t = PollScanAsync(token);
            handler.RegisterTask(t);
            try
            {
                await t;
            } catch (OperationCanceledException)
            {
                UpdateStatusBar("Checkmarx: Cancelling scan " + scanId);
                CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
                _ = cxWrapper.ScanCancelAsync(scanId);
            } finally
            {
                SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, string.Empty);
            }
        }

        private async Task PollScanAsync(CancellationToken token)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (cxWrapper == null || string.IsNullOrWhiteSpace(scanId)) return;
            Scan scan = null;
            while (scan == null || scan.Status == "Running")
            {
                await Task.Delay(1000 * 15, token);
                scan = await cxWrapper.ScanShowAsync(scanId);
                if (scan == null)
                {
                    UpdateStatusBar("Checkmarx: Failed polling scan " + scanId);
                    return;
                }
                UpdateStatusBar("Checkmarx: Scan ID " + scanId + " status " + scan.Status);
                token.ThrowIfCancellationRequested();

            }
            UpdateStatusBar("Checkmarx: Scan " + scanId + " finished with status " + scan.Status);
            if (scan.Status.ToLower() == "completed" || scan.Status.ToLower() == "partial")
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, "Load scan results?", KnownMonikers.StatusInformation, "Yes", scanId);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously", Justification = "Readbility")]
        private static void UpdateStatusBar(string message)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
                var statusBar = dte.StatusBar;

                statusBar.Text = message;
            });
        }
    }
}
