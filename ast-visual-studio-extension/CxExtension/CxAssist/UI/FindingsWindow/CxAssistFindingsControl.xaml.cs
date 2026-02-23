using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;

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

        public ObservableCollection<FileNode> FileNodes
        {
            get => _fileNodes;
            set
            {
                _fileNodes = value;
                OnPropertyChanged(nameof(FileNodes));
                OnPropertyChanged(nameof(HasFindings));
                OnPropertyChanged(nameof(ShowEmptyState));
                UpdateStatusBar();
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

        public CxAssistFindingsControl()
        {
            InitializeComponent();
            FileNodes = new ObservableCollection<FileNode>();
            _allFileNodes = new ObservableCollection<FileNode>();
            DataContext = this;

            // Load filter icons and subscribe to coordinator (JetBrains ISSUE_TOPIC-like: window stays in sync when issues change)
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadFilterIcons();
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
            Dispatcher.BeginInvoke(new Action(() => RefreshFromCoordinator()));
        }

        /// <summary>
        /// Refreshes the tree from coordinator's current issues (used when IssuesUpdated fires or on load).
        /// </summary>
        private void RefreshFromCoordinator()
        {
            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodes = current != null && current.Count > 0
                ? CxAssistMockData.BuildFileNodesFromVulnerabilities(current, LoadSeverityIconForTree, null)
                : new ObservableCollection<FileNode>();
            SetAllFileNodes(fileNodes);
        }

        /// <summary>
        /// Load severity icon for tree items (same theme logic as filter icons).
        /// </summary>
        private System.Windows.Media.ImageSource LoadSeverityIconForTree(string severity)
        {
            try
            {
                string themeFolder = IsDarkTheme() ? "Dark" : "Light";
                string iconName = (severity ?? "unknown").ToLower() + ".png";
                return LoadIcon(themeFolder, iconName);
            }
            catch { return null; }
        }

        /// <summary>
        /// Load severity icons for filter buttons
        /// </summary>
        private void LoadFilterIcons()
        {
            try
            {
                string themeFolder = IsDarkTheme() ? "Dark" : "Light";

                // Set icons directly to Image controls
                CriticalFilterIcon.Source = LoadIcon(themeFolder, "critical.png");
                HighFilterIcon.Source = LoadIcon(themeFolder, "high.png");
                MediumFilterIcon.Source = LoadIcon(themeFolder, "medium.png");
                LowFilterIcon.Source = LoadIcon(themeFolder, "low.png");
                MaliciousFilterIcon.Source = LoadIcon(themeFolder, "malicious.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading filter icons: {ex.Message}");
            }
        }

        /// <summary>
        /// Load icon from resources
        /// </summary>
        private ImageSource LoadIcon(string themeFolder, string iconName)
        {
            try
            {
                string iconPath = $"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/{themeFolder}/{iconName}";
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon {iconName}: {ex.Message}");
                return null;
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

        /// <summary>
        /// Detect if dark theme is active
        /// </summary>
        private bool IsDarkTheme()
        {
            try
            {
                var color = Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(
                    Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey);
                int brightness = (int)Math.Sqrt(
                    color.R * color.R * 0.299 +
                    color.G * color.G * 0.587 +
                    color.B * color.B * 0.114);
                return brightness < 128;
            }
            catch
            {
                return true;
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
        /// Handle double-click on tree item to navigate to file location
        /// </summary>
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item?.DataContext is VulnerabilityNode vulnerability)
            {
                e.Handled = true;
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    NavigateToVulnerability(vulnerability);
                });
            }
        }

        /// <summary>
        /// Navigate to vulnerability location in code (same approach as Error List navigation).
        /// </summary>
        private void NavigateToVulnerability(VulnerabilityNode vulnerability)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (string.IsNullOrEmpty(vulnerability?.FilePath)) return;

            try
            {
                var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte == null) return;

                var window = dte.ItemOperations.OpenFile(vulnerability.FilePath, EnvDTE.Constants.vsViewKindCode);
                Document doc = window?.Document;
                if (doc?.Object("TextDocument") is TextDocument textDoc)
                {
                    int line = Math.Max(1, vulnerability.Line);
                    int column = Math.Max(1, vulnerability.Column);
                    textDoc.Selection.MoveToLineAndOffset(line, column);
                    textDoc.Selection.SelectLine();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to vulnerability: {ex.Message}");
            }
        }

        #region Context Menu Handlers

        private void FixWithCxOneAssist_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                MessageBox.Show($"Fix with CxOne Assist:\n{vulnerability.DisplayText}", 
                    "Checkmarx CxAssist", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Implement actual fix logic
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                MessageBox.Show($"View Details:\n{vulnerability.DisplayText}\n\nSeverity: {vulnerability.Severity}\nFile: {vulnerability.FilePath}\nLine: {vulnerability.Line}, Column: {vulnerability.Column}", 
                    "Checkmarx CxAssist", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Implement actual details view
            }
        }

        private void Ignore_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                var result = MessageBox.Show($"Ignore this vulnerability?\n{vulnerability.DisplayText}", 
                    "Checkmarx CxAssist", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement ignore logic
                    MessageBox.Show("Vulnerability ignored.", "Checkmarx CxAssist", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void IgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            var vulnerability = GetSelectedVulnerability();
            if (vulnerability != null)
            {
                var result = MessageBox.Show($"Ignore all vulnerabilities of this type?\n{vulnerability.Description}", 
                    "Checkmarx CxAssist", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement ignore all logic
                    MessageBox.Show("All vulnerabilities of this type ignored.", "Checkmarx CxAssist", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// Handle severity filter button clicks
        /// </summary>
        private void SeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
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

            if (CriticalFilterButton.IsChecked == true)
                activeFilters.Add("Critical");
            if (HighFilterButton.IsChecked == true)
                activeFilters.Add("High");
            if (MediumFilterButton.IsChecked == true)
                activeFilters.Add("Medium");
            if (LowFilterButton.IsChecked == true)
                activeFilters.Add("Low");
            if (MaliciousFilterButton.IsChecked == true)
                activeFilters.Add("Malicious");

            // If no filters are active, show all
            if (activeFilters.Count == 0)
            {
                FileNodes = new ObservableCollection<FileNode>(_allFileNodes);
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
        /// Open settings (placeholder for now)
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings functionality coming soon!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// Copy selected item details to clipboard
        /// </summary>
        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FindingsTreeView.SelectedItem;
            if (selectedItem == null) return;

            string textToCopy = "";
            if (selectedItem is FileNode fileNode)
            {
                textToCopy = $"{fileNode.FileName} - {fileNode.FilePath}";
            }
            else if (selectedItem is VulnerabilityNode vulnNode)
            {
                textToCopy = vulnNode.DisplayText;
            }

            if (!string.IsNullOrEmpty(textToCopy))
            {
                Clipboard.SetText(textToCopy);
            }
        }

        /// <summary>
        /// Navigate to code location
        /// </summary>
        private void NavigateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FindingsTreeView.SelectedItem;
            if (selectedItem is VulnerabilityNode vulnNode)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    NavigateToVulnerability(vulnNode);
                });
            }
            else if (selectedItem is FileNode fileNode && !string.IsNullOrEmpty(fileNode.FilePath))
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                        if (dte != null)
                            dte.ItemOperations.OpenFile(fileNode.FilePath, EnvDTE.Constants.vsViewKindCode);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error navigating to file: {ex.Message}");
                    }
                });
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
}

