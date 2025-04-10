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
using Microsoft.VisualStudio.Imaging;
using ast_visual_studio_extension.CxWrapper.Exceptions;
using System.Linq;
using EnvDTE;
using EnvDTE80;

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
        public Func<List<State>, Dictionary<MenuItem, State>> CreateStateMenuItems { get; set; }

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

        public CxToolbar WithStateFilters(Dictionary<MenuItem, State> statesMenuItems)
        {
            StateFilters = statesMenuItems;
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

        public CxToolbar WithCreateStateMenuItemsFunc(Func<List<State>, Dictionary<MenuItem, State>> createStateMenuItemsFunc)
        {
            CreateStateMenuItems = createStateMenuItemsFunc;
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
            foreach (KeyValuePair<MenuItem, GroupBy> pair in GroupByOptions)
            {
                var control = pair.Key;
                var groupBy = pair.Value;
                control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.groupByCollection, groupBy.ToString(), SettingsUtils.groupByDefaultValues[groupBy]);
            }

            CheckScanButtonStateByCombos();

            _ = IdeScansEnabledAsync();

            if (!initPolling)
            {
                initPolling = true;
                _ = PollScanStartedAsync();
            }
        }

        private void RetrieveEnabledStates()
        {
            var readOnlyStore = new ShellSettingsManager(Package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (KeyValuePair<MenuItem, State> pair in StateFilters)
            {
                var control = pair.Key;
                var state = pair.Value;

                if (Enum.TryParse(state.name, out SystemState stateEnum))
                {
                    if (SettingsUtils.stateDefaultValues.TryGetValue(stateEnum, out bool value))
                    {
                        control.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.stateCollection, state.name, SettingsUtils.stateDefaultValues[stateEnum]);
                    }
                }
                else if (Enum.TryParse<DependencyFilter>(state.name, out var dependencyFilter))
                {
                    control.IsChecked = readOnlyStore.GetBoolean(
                        SettingsUtils.dependencyFiltersCollection,
                        state.name,
                        SettingsUtils.dependencyFilterDefaultValues[dependencyFilter]
                    );
                }
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

        public void RefreshStates()
        {
            StateManager stateManager = StateManagerProvider.GetStateManager();
            List<State> states = stateManager.GetAllStates();
            var statesMenuItems = CreateStateMenuItems(states);
            WithStateFilters(statesMenuItems);
            RetrieveEnabledStates();
        }

        public void StateFilterClick(MenuItem stateControl)
        {
            string selectedStateName = StateFilters[stateControl].name;
            if (Enum.TryParse(selectedStateName, out SystemState stateEnum))
            {
                if (SettingsUtils.stateDefaultValues.TryGetValue(stateEnum, out bool value))
                {
                    SettingsUtils.Store(Package, SettingsUtils.stateCollection, stateEnum, SettingsUtils.stateDefaultValues);
                }
            }
            else
            {
                StateManager stateManager = StateManagerProvider.GetStateManager();

                if (!stateManager.enabledCustemStates.Remove(selectedStateName))
                {
                    stateManager.enabledCustemStates.Add(selectedStateName);
                }
                if (Enum.TryParse<DependencyFilter>(selectedStateName, out var dependencyFilter))
                {
                    SettingsUtils.Store(Package, SettingsUtils.dependencyFiltersCollection, dependencyFilter, SettingsUtils.dependencyFilterDefaultValues);
                }
            }
            ResultsTreePanel.Redraw(true);
        }

        public void GroupByClick(MenuItem groupByControl)
        {
            SettingsUtils.Store(Package, SettingsUtils.groupByCollection, GroupByOptions[groupByControl], SettingsUtils.groupByDefaultValues);
            ResultsTreePanel.Redraw(true);
        }

        public async Task ScanStart_ClickAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ScanStartButton.IsEnabled = false;

            EnvDTE.DTE dte = SolutionExplorerUtils.GetDTE();

            if (string.IsNullOrEmpty(dte.Solution.FullName))
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, CxConstants.PROJECT_AND_BRANCH_DO_NOT_MATCH, KnownMonikers.StatusWarning, CxConstants.RUN_SCAN, CxConstants.RUN_SCAN_ACTION, false);
                ScanStartButton.IsEnabled = true;
                return;
            }

            var currentGitBranch = await GetCurrentGitBranchAsync(dte);
            var checkmarxBranch = SettingsUtils.GetToolbarValue(Package, SettingsUtils.branchProperty);
            var matchProject = await ASTProjectMatchesWorkspaceProjectAsync(dte);
            var matchBranch = string.IsNullOrEmpty(currentGitBranch) || currentGitBranch.Equals(checkmarxBranch);

            if (!matchProject && !matchBranch)
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, CxConstants.PROJECT_AND_BRANCH_DO_NOT_MATCH, KnownMonikers.StatusWarning, CxConstants.RUN_SCAN, CxConstants.RUN_SCAN_ACTION, false);
                ScanStartButton.IsEnabled = true;
                return;
            }

            if (!matchBranch)
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, CxConstants.BRANCH_DOES_NOT_MATCH, KnownMonikers.StatusWarning, CxConstants.RUN_SCAN, CxConstants.RUN_SCAN_ACTION, false);
                ScanStartButton.IsEnabled = true;
                return;
            }

            if (!matchProject)
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, CxConstants.PROJECT_DOES_NOT_MATCH, KnownMonikers.StatusWarning, CxConstants.RUN_SCAN, CxConstants.RUN_SCAN_ACTION, false);
                ScanStartButton.IsEnabled = true;
                return;
            }

            SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, string.Empty);
            _ = ScanStartedAsync();
        }

        private static async Task<string> GetCurrentGitBranchAsync(EnvDTE.DTE dte)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                string workingDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);
                RepositoryInformation repository = RepositoryInformation.GetRepositoryInformation(workingDir);

                if (repository == null)
                {
                    return string.Empty;
                }

                return repository.CurrentBranch;
            }
            catch (Exception ex)
            {
                UpdateStatusBar("Checkmarx: Error getting git branch: " + ex.Message);
            }

            return string.Empty;
        }

        private static async Task<bool> ASTProjectMatchesWorkspaceProjectAsync(EnvDTE.DTE dte)
        {
            if (ResultsTreePanel.currentResults == null || !ResultsTreePanel.currentResults.results.Any())
            {
                return true;
            }

            List<Result> astResults = ResultsTreePanel.currentResults.results;
            HashSet<string> resultsFileNames = new HashSet<string>();

            foreach (Result result in astResults)
            {
                if (result.Data.Nodes != null && result.Data.Nodes.Any())
                {
                    resultsFileNames.Add(result.Data.Nodes[0].FileName);
                }
                else if (!string.IsNullOrEmpty(result.Data.FileName))
                {
                    resultsFileNames.Add(result.Data.FileName);
                }
            }

            foreach (string fileName in resultsFileNames)
            {
                string partialFileLocation = SolutionExplorerUtils.PrepareFileName(fileName);

                List<string> files = await SolutionExplorerUtils.SearchFilesBasedOnProjectDirectoryAsync(partialFileLocation, dte);
                if (files.Count == 0)
                {
                    files = await SolutionExplorerUtils.SearchAllFilesAsync(partialFileLocation, dte);
                }

                if (files.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void CheckScanButtonStateByCombos()
        {
            var isProject = ProjectsCombo?.SelectedItem != null && ProjectsCombo.SelectedIndex != -1;
            var isBranch = BranchesCombo?.SelectedItem != null && BranchesCombo.SelectedIndex != -1;
            ScanStartButton.IsEnabled = isProject && isBranch;
        }

        private async Task IdeScansEnabledAsync()
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
            var ideScansEnabled = false;
            try
            {
                ideScansEnabled = await cxWrapper.IdeScansEnabledAsync();
            }
            catch (CxException ex)
            {
                UpdateStatusBar("Checkmarx: " + ex.Message);
            }

            ScanStartButton.Visibility = ScanningSeparator.Visibility = ideScansEnabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public async Task ScanStartedAsync()
        {
            ScanStartButton.IsEnabled = false;
            var tsc = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            var options = default(TaskHandlerOptions);
            options.Title = CxConstants.STATUS_CREATING_SCAN;
            options.ActionsAfterCompletion = CompletionActions.None;
            TaskProgressData data = default;
            ITaskHandler handler = tsc.PreRegister(options, data);
            var t = StartScanAsync();
            handler.RegisterTask(t);
            await t;

            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (string.IsNullOrEmpty(scanId))
            {
                ScanStartButton.IsEnabled = true;
                return;
            }

            await PollScanStartedAsync();
        }

        private async Task StartScanAsync()
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (cxWrapper == null || !string.IsNullOrWhiteSpace(scanId)) return;

            string currentPath = await GetCurrentWorkingDirAsync();

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { CxCLI.CxConstants.FLAG_SOURCE, currentPath },
                { CxCLI.CxConstants.FLAG_PROJECT_NAME, ProjectsCombo.Text },
                { CxCLI.CxConstants.FLAG_BRANCH, BranchesCombo.Text },
                { CxCLI.CxConstants.FLAG_AGENT, CxCLI.CxConstants.EXTENSION_AGENT }
            };
            const string additionalParamaters = "{0} {1} {2}";

            UpdateStatusBar(CxConstants.STATUS_CREATING_SCAN);
            Scan scan = await cxWrapper.ScanCreateAsync(parameters, string.Format(additionalParamaters, CxCLI.CxConstants.FLAG_ASYNC, CxCLI.CxConstants.FLAG_INCREMENTAL, CxCLI.CxConstants.FLAG_RESUBMIT));

            if (scan != null)
            {
                SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, scan.ID);
                UpdateStatusBar(string.Format(CxConstants.STATUS_FORMAT_CREATED_SCAN, scan.ID));
            }
            else
            {
                UpdateStatusBar(CxConstants.STATUS_CREATING_SCAN_FAILED);
            }
        }

        private static async Task<string> GetCurrentWorkingDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            if ((solutionExplorer.DTE.ActiveSolutionProjects as Array)?.Length > 0)
            {
                return System.IO.Path.GetDirectoryName(dte.Solution.FullName);
            }
            return ".";
        }

        private async Task PollScanStartedAsync()
        {
            ScanStartButton.IsEnabled = false;
            var tsc = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsTaskStatusCenterService)) as IVsTaskStatusCenterService;
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (string.IsNullOrWhiteSpace(scanId))
            {
                CheckScanButtonStateByCombos();
                return;
            };
            var options = default(TaskHandlerOptions);
            options.Title = string.Format(CxConstants.STATUS_FORMAT_POLLING_SCAN, scanId, CxCLI.CxConstants.SCAN_RUNNING);
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
            }
            catch (OperationCanceledException)
            {
                UpdateStatusBar(string.Format(CxConstants.STATUS_FORMAT_CANCELLING_SCAN, scanId));
                CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
                _ = cxWrapper.ScanCancelAsync(scanId);
            }
            finally
            {
                SettingsUtils.StoreToolbarValue(Package, SettingsUtils.toolbarCollection, SettingsUtils.createdScanIdProperty, string.Empty);
                ScanStartButton.IsEnabled = true;
            }
        }

        private async Task PollScanAsync(CancellationToken token)
        {
            CxCLI.CxWrapper cxWrapper = CxUtils.GetCxWrapper(Package, ResultsTree, GetType());
            var scanId = SettingsUtils.GetToolbarValue(Package, SettingsUtils.createdScanIdProperty);
            if (cxWrapper == null || string.IsNullOrWhiteSpace(scanId)) return;
            Scan scan = null;
            while (scan == null || scan.Status.ToLower() == CxCLI.CxConstants.SCAN_RUNNING)
            {
                await Task.Delay(1000 * 15, token);
                scan = await cxWrapper.ScanShowAsync(scanId);
                if (scan == null)
                {
                    UpdateStatusBar(string.Format(CxConstants.STATUS_FORMAT_POLLING_SCAN_FAILED, scanId));
                    return;
                }
                UpdateStatusBar(string.Format(CxConstants.STATUS_FORMAT_POLLING_SCAN, scanId, scan.Status.ToLower()));
                token.ThrowIfCancellationRequested();

            }
            UpdateStatusBar(string.Format(CxConstants.STATUS_FORMAT_FINISHED_SCAN, scanId, scan.Status.ToLower()));
            if (scan.Status.ToLower() == CxCLI.CxConstants.SCAN_COMPLETED || scan.Status.ToLower() == CxCLI.CxConstants.SCAN_PARTIAL)
            {
                CxUtils.DisplayMessageInInfoWithLinkBar(Package, CxConstants.INFOBAR_SCAN_COMPLETED, KnownMonikers.StatusInformation, CxConstants.INFOBAR_RESULTS_LINK, scanId, false);
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
