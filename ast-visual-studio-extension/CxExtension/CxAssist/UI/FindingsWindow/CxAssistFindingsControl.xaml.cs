using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using EnvDTE;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Settings;

namespace ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow
{
    /// <summary>
    /// Interaction logic for CxAssistFindingsControl.xaml
    /// </summary>
    public partial class CxAssistFindingsControl : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<FileNode> _fileNodes;
        private ObservableCollection<FileNode> _allFileNodes; // Store unfiltered data
        private string _statusBarText;
        private bool _isLoading;
        private Action<IReadOnlyDictionary<string, List<Core.Models.Vulnerability>>> _onIssuesUpdated;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when the user clicks the Settings button. Parent (e.g. CxWindowControl) can subscribe to open the same Checkmarx settings as Scan Results.
        /// </summary>
        public event EventHandler SettingsClick;

        public ObservableCollection<FileNode> FileNodes
        {
            get => _fileNodes;
            set
            {
                _fileNodes = value;
                OnPropertyChanged(nameof(FileNodes));
                OnPropertyChanged(nameof(HasFindings));
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(TabHeaderText));
                UpdateStatusBar();
            }
        }

        /// <summary>Tab header text with vulnerability count, e.g. "Checkmarx One Assist Findings (5)".</summary>
        public string TabHeaderText
        {
            get
            {
                int count = FileNodes != null ? FileNodes.Sum(f => f.Vulnerabilities?.Count ?? 0) : 0;
                return $"Checkmarx One Assist Findings ({count})";
            }
        }

        /// <summary>True when there is at least one finding in the tree.</summary>
        public bool HasFindings => FileNodes != null && FileNodes.Count > 0;

        /// <summary>True when the list is empty (used to show "No vulnerabilities found" message).</summary>
        public bool ShowEmptyState => !HasFindings;

        public string StatusBarText
        {
            get => _statusBarText;
            set
            {
                _statusBarText = value;
                OnPropertyChanged(nameof(StatusBarText));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>True when dark theme is active; used to soften file icons in dark theme for better appearance.</summary>
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            private set
            {
                if (_isDarkTheme == value) return;
                _isDarkTheme = value;
                OnPropertyChanged(nameof(IsDarkTheme));
            }
        }
        private bool _isDarkTheme;
        private AsyncPackage _package;

        /// <summary>Set the VS package so filter state can be persisted (same store as Scan Results for Critical/High/Medium/Low). Call from CxWindowControl_Loaded.</summary>
        public void SetPackage(AsyncPackage package)
        {
            _package = package;
            LoadSeverityFilterState();
        }

        public CxAssistFindingsControl()
        {
            InitializeComponent();
            FileNodes = new ObservableCollection<FileNode>();
            _allFileNodes = new ObservableCollection<FileNode>();
            DataContext = this;

            System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {CxAssistConstants.FINDINGS_WINDOW_INITIATED}");

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateThemeState();
            LoadFilterIcons();
            LoadSeverityFilterState(); // restore persisted filter state when control loads
            _onIssuesUpdated = OnIssuesUpdated;
            CxAssistDisplayCoordinator.IssuesUpdated += _onIssuesUpdated;
            // Initial refresh from current data
            RefreshFromCoordinator();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_onIssuesUpdated != null)
            {
                CxAssistDisplayCoordinator.IssuesUpdated -= _onIssuesUpdated;
                _onIssuesUpdated = null;
            }
        }

        private void OnIssuesUpdated(IReadOnlyDictionary<string, List<Core.Models.Vulnerability>> issuesByFile)
        {
            // Coordinator raises IssuesUpdated from UI thread (callers use SwitchToMainThreadAsync).
            RefreshFromCoordinator();
        }

        /// <summary>
        /// Refreshes the tree from coordinator's current issues (used when IssuesUpdated fires or on load).
        /// </summary>
        private void RefreshFromCoordinator()
        {
            UpdateThemeState();
            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodes = current != null && current.Count > 0
                ? FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current, LoadSeverityIconForTree, LoadFileIconForTree)
                : new ObservableCollection<FileNode>();
            SetAllFileNodes(fileNodes);
        }

        /// <summary>
        /// Updates IsDarkTheme from current VS theme so file icon opacity and filter icons stay in sync.
        /// </summary>
        private void UpdateThemeState()
        {
            IsDarkTheme = AssistIconLoader.IsDarkTheme();
        }

        /// <summary>
        /// Load severity icon for tree items (uses shared AssistIconLoader).
        /// </summary>
        private System.Windows.Media.ImageSource LoadSeverityIconForTree(string severity)
        {
            try
            {
                return AssistIconLoader.LoadSeveritySvgIcon(severity ?? "unknown")
                    ?? (ImageSource)AssistIconLoader.LoadSeverityPngIcon(severity ?? "unknown");
            }
            catch { return null; }
        }

        /// <summary>
        /// Load file icon for file nodes. Always uses VS default file-type icons (e.g. Dockerfile, .yaml, .json, .py)
        /// when the image service is available. Passes current theme background for correct dark/light rendering.
        /// Falls back to theme-specific unknown.svg only when VS image service is unavailable.
        /// </summary>
        private System.Windows.Media.ImageSource LoadFileIconForTree(string filePath)
        {
            try
            {
                System.Windows.Media.Color? bgColor = GetToolWindowBackgroundColor();
                ImageSource vsIcon = GetVsFileTypeIcon(filePath, 16, 16, bgColor);
                if (vsIcon != null) return vsIcon;
                return AssistIconLoader.LoadSvgIcon(AssistIconLoader.GetCurrentTheme(), "unknown");
            }
            catch { return null; }
        }

        /// <summary>
        /// Gets the current tool window background color for theme-aware icon rendering (dark vs light).
        /// </summary>
        private static System.Windows.Media.Color? GetToolWindowBackgroundColor()
        {
            try
            {
                var color = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            }
            catch { return null; }
        }

        /// <summary>
        /// Gets the Visual Studio built-in file-type icon for the given file path.
        /// Handles both dark and light theme by passing the current tool window background so the image service
        /// returns a theme-appropriate icon. Uses IAF_Background and IAF_Theme when available.
        /// </summary>
        private static ImageSource GetVsFileTypeIcon(string filePath, int width, int height, System.Windows.Media.Color? backgroundColor)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            try
            {
                var imageService = Package.GetGlobalService(typeof(SVsImageService)) as IVsImageService2;
                if (imageService == null) return null;

                ImageMoniker moniker = imageService.GetImageMonikerForFile(filePath);
                uint flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags;
                uint backgroundRef = 0;

                if (backgroundColor.HasValue)
                {
                    var c = backgroundColor.Value;
                    backgroundRef = (uint)(c.B | (c.G << 8) | (c.R << 16));
                    flags |= unchecked((uint)_ImageAttributesFlags.IAF_Background);
                    // IAF_Theme (0x04) requests theme-appropriate icon for dark/light so icons are visible in both themes
                    flags |= 0x04u;
                }

                var imageAttributes = new ImageAttributes
                {
                    StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
                    Format = (uint)_UIDataFormat.DF_WPF,
                    LogicalWidth = width,
                    LogicalHeight = height,
                    Flags = flags,
                    ImageType = (uint)_UIImageType.IT_Bitmap,
                    Background = backgroundRef
                };

                IVsUIObject uiObject = imageService.GetImage(moniker, imageAttributes);
                if (uiObject == null) return null;

                uiObject.get_Data(out object data);
                if (data is BitmapSource bitmap)
                {
                    bitmap.Freeze();
                    return bitmap;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetVsFileTypeIcon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load severity icons for filter buttons (uses shared AssistIconLoader).
        /// </summary>
        private void LoadFilterIcons()
        {
            try
            {
                string theme = AssistIconLoader.GetCurrentTheme();
                MaliciousFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "malicious.png");
                CriticalFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "critical.png");
                HighFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "high.png");
                MediumFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "medium.png");
                LowFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "low.png");
                ExpandAllIcon.Source = AssistIconLoader.LoadSvgIcon(theme, "expandall");
                CollapseAllIcon.Source = AssistIconLoader.LoadSvgIcon(theme, "collapseall");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Get file type icon based on file extension (for future enhancement)
        /// Currently returns generic document icon
        /// </summary>
        public static ImageSource GetFileTypeIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // For now, use generic document icon
            // In future, can add specific icons for .go, .json, .xml, .cs, etc.
            try
            {
                string iconPath = "pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/document.png";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Update status bar with vulnerability count
        /// </summary>
        private void UpdateStatusBar()
        {
            if (FileNodes == null || FileNodes.Count == 0)
            {
                StatusBarText = "No vulnerabilities found";
                return;
            }

            int totalVulnerabilities = FileNodes.Sum(f => f.Vulnerabilities?.Count ?? 0);
            int fileCount = FileNodes.Count;

            if (totalVulnerabilities == 0)
            {
                StatusBarText = "No vulnerabilities found";
            }
            else if (totalVulnerabilities == 1)
            {
                StatusBarText = $"1 vulnerability found in {fileCount} file{(fileCount == 1 ? "" : "s")}";
            }
            else
            {
                StatusBarText = $"{totalVulnerabilities} vulnerabilities found in {fileCount} file{(fileCount == 1 ? "" : "s")}";
            }
        }

        /// <summary>
        /// Handle single-click (selection change) on a vulnerability node to navigate to file location
        /// (aligned with JetBrains: single click navigates to selected issue).
        /// </summary>
        private void FindingsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is VulnerabilityNode vulnerability)
            {
                NavigateToVulnerability(vulnerability);
            }
        }

        /// <summary>
        /// Handle double-click on tree item to navigate to file location (kept for backward compatibility).
        /// </summary>
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item?.DataContext is VulnerabilityNode vulnerability)
            {
                e.Handled = true;
                NavigateToVulnerability(vulnerability);
            }
        }

        /// <summary>
        /// Navigate to vulnerability location in code (same approach as Error List navigation).
        /// Tries OpenFile with path, then full path, then finds already-open document by name.
        /// </summary>
        private void NavigateToVulnerability(VulnerabilityNode vulnerability)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(vulnerability?.FilePath)) return;

            try
            {
                var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte == null) return;

                EnvDTE.Window window = null;
                string pathToTry = vulnerability.FilePath;

                try
                {
                    window = dte.ItemOperations.OpenFile(pathToTry, EnvDTE.Constants.vsViewKindCode);
                }
                catch (Exception openEx)
                {
                    CxAssistOutputPane.WriteToOutputPane($"Error opening file: {pathToTry}, {openEx.Message}");
                }
                if (window == null && !Path.IsPathRooted(pathToTry))
                {
                    try
                    {
                        pathToTry = Path.GetFullPath(pathToTry);
                        window = dte.ItemOperations.OpenFile(pathToTry, EnvDTE.Constants.vsViewKindCode);
                    }
                    catch { /* ignore */ }
                }
                if (window == null && dte.Solution != null)
                {
                    try
                    {
                        string solDir = Path.GetDirectoryName(dte.Solution.FullName);
                        if (!string.IsNullOrEmpty(solDir))
                        {
                            string pathInSolution = Path.Combine(solDir, Path.GetFileName(vulnerability.FilePath));
                            if (pathInSolution != pathToTry)
                                window = dte.ItemOperations.OpenFile(pathInSolution, EnvDTE.Constants.vsViewKindCode);
                        }
                    }
                    catch { /* ignore */ }
                }
                if (window == null && dte.Documents != null)
                {
                    string fileName = Path.GetFileName(pathToTry);
                    Document doc = dte.Documents.Cast<Document>().FirstOrDefault(d =>
                        string.Equals(d.FullName, pathToTry, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetFileName(d.FullName), fileName, StringComparison.OrdinalIgnoreCase));
                    if (doc != null)
                        window = doc.ActiveWindow;
                }

                if (window?.Document?.Object("TextDocument") is TextDocument textDoc)
                {
                    int line = Math.Max(1, vulnerability.Line);
                    int column = Math.Max(1, vulnerability.Column);
                    textDoc.Selection.MoveToLineAndOffset(line, column);
                    textDoc.Selection.SelectLine();

                    // Scroll target line to center of viewport (aligned with JetBrains ScrollType.CENTER)
                    try
                    {
                        var editPoint = textDoc.Selection.ActivePoint.CreateEditPoint();
                        var panes = window.Document?.ActiveWindow?.Document?.Windows;
                        if (panes != null)
                        {
                            foreach (EnvDTE.Window pane in panes)
                            {
                                if (pane.Object is TextPane textPane)
                                {
                                    textPane.TryToShow(editPoint, EnvDTE.vsPaneShowHow.vsPaneShowCentered);
                                    break;
                                }
                            }
                        }
                    }
                    catch { /* scroll centering is best-effort */ }
                }
            }
            catch
            {
            }
        }

        #region Context Menu Handlers

        /// <summary>
        /// Show context menu only when right-clicking a vulnerability row, not the file (main) node (reference-style).
        /// </summary>
        private void FindingsTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeViewItem = FindVisualAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem?.DataContext is FileNode)
            {
                e.Handled = true; // Hide context menu when right-click is on file node
                return;
            }
            // Set Ignore menu labels and visibility based on scanner (Ignore all only for OSS and Container)
            if (treeViewItem?.DataContext is VulnerabilityNode node && IgnoreThisMenuItem != null && IgnoreAllMenuItem != null)
            {
                IgnoreThisMenuItem.Header = CxAssistConstants.GetIgnoreThisLabel(node.Scanner);
                IgnoreAllMenuItem.Header = CxAssistConstants.GetIgnoreAllLabel(node.Scanner);
                IgnoreAllMenuItem.Visibility = CxAssistConstants.ShouldShowIgnoreAll(node.Scanner) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static T FindVisualAncestor<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T t) return t;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void FixWithCxOneAssist_Click(object sender, RoutedEventArgs e)
        {
            var node = GetSelectedVulnerability();
            if (node == null) return;
            var v = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(node.FilePath, node.Line > 0 ? node.Line - 1 : 0);
            if (v != null) CxAssistCopilotActions.SendFixWithAssist(v);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var node = GetSelectedVulnerability();
            if (node == null) return;
            var v = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(node.FilePath, node.Line > 0 ? node.Line - 1 : 0);
            if (v != null) CxAssistCopilotActions.SendViewDetails(v);
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                string label = CxAssistConstants.GetIgnoreThisLabel(vulnerability.Scanner);
                var result = MessageBox.Show($"{label}?\n{vulnerability.DisplayText}",
                    CxAssistConstants.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement ignore logic
                    MessageBox.Show(CxAssistConstants.GetIgnoreThisSuccessMessage(vulnerability.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void IgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                string label = CxAssistConstants.GetIgnoreAllLabel(vulnerability.Scanner);
                var result = MessageBox.Show($"{label}?\n{vulnerability.Description}",
                    CxAssistConstants.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement ignore all logic
                    MessageBox.Show(CxAssistConstants.GetIgnoreAllSuccessMessage(vulnerability.Scanner), CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private VulnerabilityNode GetSelectedVulnerability()
        {
            return FindingsTreeView.SelectedItem as VulnerabilityNode;
        }

        #endregion

        #region Severity Filter Handlers

        /// <summary>
        /// Handle severity filter button clicks; persist state (same store as Scan Results for Critical/High/Medium/Low).
        /// </summary>
        private void SeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_package != null && sender is ToggleButton button)
                StoreSeverityFilterState(button);
            ApplyFilters();
        }

        /// <summary>Load severity filter state from settings (shared with Scan Results for Critical/High/Medium/Low).</summary>
        private void LoadSeverityFilterState()
        {
            if (_package == null) return;
            try
            {
                var readOnlyStore = new ShellSettingsManager(_package).GetReadOnlySettingsStore(SettingsScope.UserSettings);
                // Malicious: CxAssist-only collection
                MaliciousFilterButton.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.cxAssistSeverityCollection, SettingsUtils.cxAssistMaliciousKey, true);
                // Critical/High/Medium/Low: same collection as Scan Results
                CriticalFilterButton.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, Severity.CRITICAL.ToString(), SettingsUtils.severityDefaultValues[Severity.CRITICAL]);
                HighFilterButton.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, Severity.HIGH.ToString(), SettingsUtils.severityDefaultValues[Severity.HIGH]);
                MediumFilterButton.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, Severity.MEDIUM.ToString(), SettingsUtils.severityDefaultValues[Severity.MEDIUM]);
                LowFilterButton.IsChecked = readOnlyStore.GetBoolean(SettingsUtils.severityCollection, Severity.LOW.ToString(), SettingsUtils.severityDefaultValues[Severity.LOW]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CxAssist LoadSeverityFilterState: {ex.Message}");
            }
        }

        /// <summary>Persist the toggled severity filter (Store toggles the value, so call after UI has updated).</summary>
        private void StoreSeverityFilterState(ToggleButton button)
        {
            try
            {
                if (button == MaliciousFilterButton)
                    SettingsUtils.Store(_package, SettingsUtils.cxAssistSeverityCollection, SettingsUtils.cxAssistMaliciousKey, SettingsUtils.cxAssistSeverityDefaultValues);
                else if (button == CriticalFilterButton)
                    SettingsUtils.Store(_package, SettingsUtils.severityCollection, Severity.CRITICAL, SettingsUtils.severityDefaultValues);
                else if (button == HighFilterButton)
                    SettingsUtils.Store(_package, SettingsUtils.severityCollection, Severity.HIGH, SettingsUtils.severityDefaultValues);
                else if (button == MediumFilterButton)
                    SettingsUtils.Store(_package, SettingsUtils.severityCollection, Severity.MEDIUM, SettingsUtils.severityDefaultValues);
                else if (button == LowFilterButton)
                    SettingsUtils.Store(_package, SettingsUtils.severityCollection, Severity.LOW, SettingsUtils.severityDefaultValues);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CxAssist StoreSeverityFilterState: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply severity filters to the tree view
        /// </summary>
        private void ApplyFilters()
        {
            if (_allFileNodes == null || _allFileNodes.Count == 0)
                return;

            // Get active filters
            var activeFilters = new System.Collections.Generic.List<string>();

            if (MaliciousFilterButton.IsChecked == true)
                activeFilters.Add("Malicious");
            if (CriticalFilterButton.IsChecked == true)
                activeFilters.Add("Critical");
            if (HighFilterButton.IsChecked == true)
                activeFilters.Add("High");
            if (MediumFilterButton.IsChecked == true)
                activeFilters.Add("Medium");
            if (LowFilterButton.IsChecked == true)
                activeFilters.Add("Low");

            // If no filters are active, show nothing (user has disabled all severities)
            if (activeFilters.Count == 0)
            {
                FileNodes = new ObservableCollection<FileNode>();
                return;
            }

            // Filter files and vulnerabilities
            var filteredFiles = new ObservableCollection<FileNode>();

            foreach (var file in _allFileNodes)
            {
                var filteredVulns = file.Vulnerabilities
                    .Where(v => activeFilters.Contains(v.Severity, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (filteredVulns.Count > 0)
                {
                    var filteredFile = new FileNode
                    {
                        FileName = file.FileName,
                        FilePath = file.FilePath,
                        FileIcon = file.FileIcon
                    };

                    foreach (var vuln in filteredVulns)
                    {
                        filteredFile.Vulnerabilities.Add(vuln);
                    }

                    // Update severity count badges to reflect filtered list (so counts match visible findings)
                    var severityCounts = filteredFile.Vulnerabilities
                        .GroupBy(n => n.Severity)
                        .Select(g => new SeverityCount
                        {
                            Severity = g.Key,
                            Count = g.Count(),
                            Icon = g.First().SeverityIcon
                        });
                    foreach (var sc in severityCounts)
                        filteredFile.SeverityCounts.Add(sc);

                    filteredFiles.Add(filteredFile);
                }
            }

            FileNodes = filteredFiles;
        }

        /// <summary>
        /// Store all file nodes for filtering (called from ShowFindingsWindowCommand)
        /// </summary>
        public void SetAllFileNodes(ObservableCollection<FileNode> allNodes)
        {
            _allFileNodes = allNodes;
            FileNodes = new ObservableCollection<FileNode>(allNodes);
        }

        #endregion

        #region Toolbar Button Handlers

        /// <summary>
        /// Expand all tree view items
        /// </summary>
        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseAll(FindingsTreeView, true);
        }

        /// <summary>
        /// Collapse all tree view items
        /// </summary>
        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseAll(FindingsTreeView, false);
        }

        /// <summary>
        /// Open settings - raises SettingsClick so parent can open the same Checkmarx options page as Scan Results.
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Recursively expand or collapse all TreeView items
        /// </summary>
        private void ExpandCollapseAll(ItemsControl items, bool expand)
        {
            if (items == null) return;

            foreach (object obj in items.Items)
            {
                ItemsControl childControl = items.ItemContainerGenerator.ContainerFromItem(obj) as ItemsControl;
                if (childControl != null)
                {
                    if (childControl is TreeViewItem treeItem)
                    {
                        treeItem.IsExpanded = expand;
                        ExpandCollapseAll(treeItem, expand);
                    }
                }
            }
        }

        #endregion

        #region Context Menu Handlers

        /// <summary>
        /// Copy selected item details to clipboard (full display text).
        /// </summary>
        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vuln = GetSelectedVulnerability();
            if (vuln != null)
            {
                try
                {
                    Clipboard.SetText(vuln.DisplayText);
                    ShowStatusBarNotification("Copied to clipboard.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {CxAssistConstants.FAILED_COPY_CLIPBOARD}");
                }
            }
        }

        /// <summary>
        /// Copy short message to clipboard (reference "Copy Message": e.g. "High-risk package: validator@13.12").
        /// </summary>
        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            var vuln = GetSelectedVulnerability();
            if (vuln != null && !string.IsNullOrEmpty(vuln.PrimaryDisplayText))
            {
                try
                {
                    Clipboard.SetText(vuln.PrimaryDisplayText);
                    ShowStatusBarNotification("Message copied to clipboard.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {CxAssistConstants.FAILED_COPY_CLIPBOARD}");
                }
            }
        }

        /// <summary>
        /// Copy the fix prompt to clipboard (aligned with JetBrains "Copy Fix" context menu action).
        /// Builds the scanner-specific remediation prompt and puts it on the clipboard.
        /// Shows a success notification in the status bar (aligned with JetBrains copyToClipboardWithNotification).
        /// </summary>
        private void CopyFixPrompt_Click(object sender, RoutedEventArgs e)
        {
            var node = GetSelectedVulnerability();
            if (node == null) return;
            var v = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(node.FilePath, node.Line > 0 ? node.Line - 1 : 0);
            if (v == null)
                return;
            string prompt = Core.Prompts.CxOneAssistFixPrompts.BuildForVulnerability(v);
            if (!string.IsNullOrEmpty(prompt))
            {
                try
                {
                    Clipboard.SetText(prompt);
                    ShowStatusBarNotification("Fix prompt copied to clipboard. Paste into GitHub Copilot Chat to get remediation steps.");
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {string.Format(CxAssistConstants.FIX_PROMPT_COPIED, v.Title ?? v.Description ?? "unknown")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] {CxAssistConstants.FAILED_COPY_CLIPBOARD}");
                }
            }
        }

        /// <summary>
        /// Shows a brief notification in the VS status bar (aligned with JetBrains Utils.showNotification for copy actions).
        /// </summary>
        private static void ShowStatusBarNotification(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte != null)
                    dte.StatusBar.Text = message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CxAssist StatusBar notification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Ignore selected finding (placeholder for now)
        /// </summary>
        private void IgnoreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FindingsTreeView.SelectedItem;
            if (selectedItem == null) return;

            string itemName = "";
            if (selectedItem is FileNode fileNode)
            {
                itemName = fileNode.FileName;
            }
            else if (selectedItem is VulnerabilityNode vulnNode)
            {
                itemName = vulnNode.DisplayText;
            }

            MessageBox.Show($"Ignore functionality coming soon!\n\nSelected: {itemName}",
                "Ignore Finding", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    /// <summary>
    /// Converts IsDarkTheme (bool) to opacity for file icons: dark theme uses 0.88 for a softer look, light theme uses 1.0.
    /// </summary>
    internal sealed class DarkThemeToFileIconOpacityConverter : IValueConverter
    {
        private const double DarkThemeOpacity = 0.88;
        private const double LightThemeOpacity = 1.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDark)
                return isDark ? DarkThemeOpacity : LightThemeOpacity;
            return LightThemeOpacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

