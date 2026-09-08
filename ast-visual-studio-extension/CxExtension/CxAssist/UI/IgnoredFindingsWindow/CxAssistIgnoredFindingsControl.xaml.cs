using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using IServiceProvider = System.IServiceProvider;

namespace ast_visual_studio_extension.CxExtension.CxAssist.UI.IgnoredFindingsWindow
{
    public partial class CxAssistIgnoredFindingsControl : UserControl
    {
        private readonly ObservableCollection<IgnoredFindingViewModel> _items = new ObservableCollection<IgnoredFindingViewModel>();
        private readonly List<IgnoredFindingViewModel> _subscribedVms = new List<IgnoredFindingViewModel>();
        private bool _dataLoaded;

        private static ImageSource _cachedFileIcon;
        private static ImageSource GetCachedFileIcon() => _cachedFileIcon ?? (_cachedFileIcon = LoadIgnoredIcon("file-icon"));
        private static ImageSource _cachedReviveIcon;
        private static ImageSource GetCachedReviveIcon() => _cachedReviveIcon ?? (_cachedReviveIcon = LoadIgnoredIcon("revive"));
        private IgnoredFindingsSortMode _sortMode = IgnoredFindingsSortMode.SeverityHighToLow;

        // Scanner type filter states
        private bool _filterAsca = true;
        private bool _filterOss = true;
        private bool _filterSecrets = true;
        private bool _filterIac = true;
        private bool _filterContainers = true;


        public CxAssistIgnoredFindingsControl()
        {
            InitializeComponent();
            IgnoredList.ItemsSource = _items;
            LoadToolbarIcons();
            // Subscribe immediately — IgnoreDataChanged fires even before the tab is first shown
            // (WPF TabControl defers visual-tree attachment until first selection).
            IgnoreFileManager.IgnoreDataChanged += OnIgnoreDataChanged;
            AssistIconLoader.EnsureThemeChangeSubscription();
            AssistIconLoader.ThemeChanged += OnThemeChanged;
            Loaded += OnLoadedAttach;
            Unloaded += OnUnloadedDetach;
        }

        private void OnLoadedAttach(object sender, RoutedEventArgs e)
        {
            CxAssistOutputPane.WriteToOutputPane($"IgnoredFindings tab attached. IsInitialized={IgnoreFileManager.IsInitialized}, entries={IgnoreFileManager.GetAllEntries().Count}");
            // Only load data on first visual-tree attach — tab switches re-fire Loaded but data/selections must persist.
            if (!_dataLoaded)
            {
                LoadFilterState();
                Refresh();
                _dataLoaded = true;
            }
        }

        private void OnUnloadedDetach(object sender, RoutedEventArgs e)
        {
            UnsubscribeVmEvents();
        }

        private void OnThemeChanged()
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(OnThemeChanged)); return; }
            _cachedFileIcon = null;
            _cachedReviveIcon = null;
            LoadToolbarIcons();
            Refresh();
        }

        private void LoadToolbarIcons()
        {
            string theme = AssistIconLoader.GetCurrentTheme();
            if (MaliciousFilterIcon != null) MaliciousFilterIcon.Source = AssistIconLoader.LoadPngIcon(theme, "malicious.png");
            if (CriticalFilterIcon  != null) CriticalFilterIcon.Source  = AssistIconLoader.LoadPngIcon(theme, "critical.png");
            if (HighFilterIcon      != null) HighFilterIcon.Source      = AssistIconLoader.LoadPngIcon(theme, "high.png");
            if (MediumFilterIcon    != null) MediumFilterIcon.Source    = AssistIconLoader.LoadPngIcon(theme, "medium.png");
            if (LowFilterIcon       != null) LowFilterIcon.Source       = AssistIconLoader.LoadPngIcon(theme, "low.png");
            if (VulnTypeFilterIcon  != null) VulnTypeFilterIcon.Source  = LoadIgnoredIcon("filter_icon");
            if (SortIconImage       != null) SortIconImage.Source       = LoadIgnoredIcon("sort_icon");
        }

        public void Refresh()
        {
            try
            {
                var entries = IgnoreFileManager.GetAllEntries();
                var rows = new List<IgnoredFindingViewModel>();
                foreach (var kv in entries)
                {
                    var entry = kv.Value;
                    if (entry == null) continue;
                    if (entry.Files == null || !entry.Files.Any(f => f.Active)) continue;
                    if (!PassesSeverityFilter(entry)) continue;
                    if (!PassesScannerFilter(entry)) continue;

                    var vm = new IgnoredFindingViewModel(entry, kv.Key);
                    vm.SeverityIcon    = LoadSeverityIcon(vm.SeverityText);
                    vm.ScannerChipIcon = LoadScannerChipIcon(entry.Type);
                    vm.CardIcon        = LoadCardIcon(entry.Type, entry.Severity);
                    vm.ReviveIcon      = GetCachedReviveIcon();
                    vm.FileIcon        = GetCachedFileIcon();
                    rows.Add(vm);
                }

                IEnumerable<IgnoredFindingViewModel> sorted;
                switch (_sortMode)
                {
                    case IgnoredFindingsSortMode.SeverityLowToHigh:
                        sorted = rows.OrderBy(r => SeverityRank(r.SeverityText)); break;
                    case IgnoredFindingsSortMode.DateAddedNewestFirst:
                        sorted = rows.OrderByDescending(r => r.SortableDateAdded); break;
                    case IgnoredFindingsSortMode.DateAddedOldestFirst:
                        sorted = rows.OrderBy(r => r.SortableDateAdded); break;
                    default:
                        sorted = rows.OrderByDescending(r => SeverityRank(r.SeverityText)); break;
                }

                UnsubscribeVmEvents();
                _items.Clear();
                foreach (var r in sorted) { _items.Add(r); r.PropertyChanged += OnVmIsSelectedChanged; _subscribedVms.Add(r); }
                bool empty = _items.Count == 0;
                EmptyStateText.Visibility  = empty ? Visibility.Visible   : Visibility.Collapsed;
                ColumnHeaderRow.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
                UpdateSelectionBar();
                RaiseCountChanged();
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistIgnoredFindingsControl.Refresh");
            }
        }

        // ── Count changed event (consumed by CxWindowControl for tab badge) ──────
        public static event Action<int> CountChanged;
        private void RaiseCountChanged() => CountChanged?.Invoke(_items.Count);

        private void OnIgnoreDataChanged()
        {
            try
            {
                if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(OnIgnoreDataChanged)); return; }
                _dataLoaded = true; // mark loaded so OnLoadedAttach skips its initial Refresh
                Refresh();
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistIgnoredFindingsControl.OnIgnoreDataChanged");
            }
        }

        // ── Filters ───────────────────────────────────────────────────────────────
        private bool PassesSeverityFilter(IgnoreEntry entry)
        {
            string sev = entry.Severity ?? string.Empty;
            if (sev.Equals("Malicious", StringComparison.OrdinalIgnoreCase)) return SeverityMaliciousButton.IsChecked == true;
            if (sev.Equals("Critical",  StringComparison.OrdinalIgnoreCase)) return SeverityCriticalButton.IsChecked == true;
            if (sev.Equals("High",      StringComparison.OrdinalIgnoreCase)) return SeverityHighButton.IsChecked == true;
            if (sev.Equals("Medium",    StringComparison.OrdinalIgnoreCase)) return SeverityMediumButton.IsChecked == true;
            if (sev.Equals("Low",       StringComparison.OrdinalIgnoreCase)) return SeverityLowButton.IsChecked == true;
            return true;
        }

        private bool PassesScannerFilter(IgnoreEntry entry)
        {
            switch (entry.Type)
            {
                case ScannerType.ASCA:       return _filterAsca;
                case ScannerType.OSS:        return _filterOss;
                case ScannerType.Secrets:    return _filterSecrets;
                case ScannerType.IaC:        return _filterIac;
                case ScannerType.Containers: return _filterContainers;
                default: return true;
            }
        }

        private static int SeverityRank(string s)
        {
            switch ((s ?? string.Empty).ToLowerInvariant())
            {
                case "malicious": return 6;
                case "critical":  return 5;
                case "high":      return 4;
                case "medium":    return 3;
                case "low":       return 2;
                case "info":      return 1;
                default:          return 0;
            }
        }

        // ── Toolbar event handlers ────────────────────────────────────────────────
        private void Filter_Click(object sender, RoutedEventArgs e) { Refresh(); SaveFilterState(); }

        private void VulnType_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mi && mi.Tag is string tag)) return;
            switch (tag)
            {
                case "ASCA":       _filterAsca       = mi.IsChecked; break;
                case "OSS":        _filterOss        = mi.IsChecked; break;
                case "Secrets":    _filterSecrets    = mi.IsChecked; break;
                case "IaC":        _filterIac        = mi.IsChecked; break;
                case "Containers": _filterContainers = mi.IsChecked; break;
            }
            Refresh();
            SaveFilterState();
        }

        private void SortBy_MenuClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mi && mi.Tag is string tagStr && int.TryParse(tagStr, out int sortIndex))) return;
            _sortMode = (IgnoredFindingsSortMode)sortIndex;
            mi.IsChecked = true;
            if (mi.Parent is MenuItem parent)
                foreach (var item in parent.Items.OfType<MenuItem>())
                    if (item != mi) item.IsChecked = false;
            Refresh();
            SaveFilterState();
        }

        // ── Row actions ───────────────────────────────────────────────────────────
        private void RowRevive_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.Tag is IgnoredFindingViewModel vm)) return;
            IgnoreManager.ReviveSingle(vm.Key);
            foreach (var item in _items) item.IsSelected = false;
            UpdateSelectionBar();
        }

        // ── File navigation ───────────────────────────────────────────────────────
        private void FileRef_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.Tag is FileReferenceViewModel fr)) return;
            if (string.IsNullOrEmpty(fr.AbsolutePath)) return;
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    OpenFileAtLine(fr.AbsolutePath, fr.Line ?? 1);
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistIgnoredFindingsControl.FileRef_Click");
            }
        }

        private static void OpenFileAtLine(string absolutePath, int line1Based)
        {
            try
            {
                if (!File.Exists(absolutePath)) return;
                var serviceProvider = ServiceProvider.GlobalProvider;
                var openDoc = serviceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShellOpenDocument))
                    as Microsoft.VisualStudio.Shell.Interop.IVsUIShellOpenDocument;
                if (openDoc == null) return;

                var logicalView = Microsoft.VisualStudio.VSConstants.LOGVIEWID.Code_guid;
                openDoc.OpenDocumentViaProject(absolutePath, ref logicalView, out _, out _, out _, out var windowFrame);
                if (windowFrame == null) return;
                windowFrame.Show();

                // Get the text view directly from the window frame — avoids relying on
                // GetActiveView() which may still point at the tool window that was focused before.
                var textView = VsShellUtilities.GetTextView(windowFrame);
                if (textView == null) return;

                int line0 = Math.Max(0, line1Based - 1);
                textView.SetCaretPos(line0, 0);
                textView.EnsureSpanVisible(new Microsoft.VisualStudio.TextManager.Interop.TextSpan
                {
                    iStartLine = line0,
                    iEndLine   = line0
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CxAssistIgnoredFindingsControl.OpenFileAtLine");
            }
        }

        private void ExpandFiles_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is IgnoredFindingViewModel vm)
                vm.IsExpanded = true;
        }

        private void CollapseFiles_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is IgnoredFindingViewModel vm)
                vm.IsExpanded = false;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox cb)) return;
            bool check = cb.IsChecked == true;
            foreach (var vm in _items) vm.IsSelected = check;
            UpdateSelectionBar();
        }

        private void ReviveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0) return;
            if (selected.Count == 1)
                IgnoreManager.ReviveSingle(selected[0].Key);
            else
                IgnoreManager.ReviveMultiple(selected.Select(vm => vm.Key));
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _items) vm.IsSelected = false;
            IgnoredList.SelectedItems.Clear();
            UpdateSelectionBar();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

        // ── Selection bar ─────────────────────────────────────────────────────────
        private void UnsubscribeVmEvents()
        {
            foreach (var vm in _subscribedVms)
                try { vm.PropertyChanged -= OnVmIsSelectedChanged; } catch { }
            _subscribedVms.Clear();
        }

        private void OnVmIsSelectedChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IgnoredFindingViewModel.IsSelected)) return;
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(UpdateSelectionBar)); return; }
            UpdateSelectionBar();
        }

        private void UpdateSelectionBar()
        {
            int count = _items.Count(vm => vm.IsSelected);
            if (SelectionBarText != null)
                SelectionBarText.Text = count == 1 ? "1 Risk selected" : $"{count} Risks selected";
            if (SelectionBar != null)
                SelectionBar.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (SelectAllCheckBox != null)
                SelectAllCheckBox.IsChecked = _items.Count > 0 && count == _items.Count;
        }

        // ── Icon helpers ──────────────────────────────────────────────────────────
        private static ImageSource LoadScannerChipIcon(ScannerType scanner)
        {
            string name;
            switch (scanner)
            {
                case ScannerType.OSS:        name = "engine-chip-sca";        break;
                case ScannerType.ASCA:       name = "engine-chip-sast";       break;
                case ScannerType.Secrets:    name = "engine-chip-secrets";    break;
                case ScannerType.IaC:        name = "engine-chip-iac";        break;
                case ScannerType.Containers: name = "engine-chip-containers"; break;
                default: return null;
            }
            return LoadIgnoredIcon(name);
        }

        private static ImageSource LoadCardIcon(ScannerType scanner, string severity)
        {
            // Map scanner type to card prefix matching JetBrains ignored_card/ folder naming
            string typePrefix;
            switch (scanner)
            {
                case ScannerType.OSS:        typePrefix = "card-package";       break;
                case ScannerType.Secrets:    typePrefix = "card-secret";        break;
                case ScannerType.Containers: typePrefix = "card-containers";    break;
                default:                     typePrefix = "card-vulnerability"; break;
            }

            string sev = (severity ?? "medium").ToLowerInvariant();
            switch (sev)
            {
                case "malicious": case "critical": case "high": case "low": break;
                default: sev = "medium"; break;
            }

            string name = $"Card/{typePrefix}-{sev}";
            return LoadIgnoredIcon(name);
        }

        private static ImageSource LoadIgnoredIcon(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return null;
            string theme = AssistIconLoader.GetCurrentTheme();
            var img = AssistIconLoader.LoadSvgIcon(theme, "Ignored/" + baseName);
            if (img == null && theme != CxAssistConstants.ThemeDark)
                img = AssistIconLoader.LoadSvgIcon(CxAssistConstants.ThemeDark, "Ignored/" + baseName);
            return img;
        }

        private static ImageSource LoadSeverityIcon(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return null;
            try
            {
                string themeFolder = IsDarkTheme() ? CxAssistConstants.ThemeDark : CxAssistConstants.ThemeLight;
                var uri = new Uri($"pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/CxAssist/Icons/{themeFolder}/{severity.ToLowerInvariant()}.png", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        // ── Filter/sort state persistence ─────────────────────────────────────────
        private const string SettingsCollection = "CxAssistIgnoredFindings";

        private void SaveFilterState()
        {
            try
            {
                var store = new ShellSettingsManager(ServiceProvider.GlobalProvider)
                    .GetWritableSettingsStore(SettingsScope.UserSettings);
                store.CreateCollection(SettingsCollection);
                store.SetBoolean(SettingsCollection, "FilterMalicious",  SeverityMaliciousButton.IsChecked == true);
                store.SetBoolean(SettingsCollection, "FilterCritical",   SeverityCriticalButton.IsChecked  == true);
                store.SetBoolean(SettingsCollection, "FilterHigh",       SeverityHighButton.IsChecked      == true);
                store.SetBoolean(SettingsCollection, "FilterMedium",     SeverityMediumButton.IsChecked    == true);
                store.SetBoolean(SettingsCollection, "FilterLow",        SeverityLowButton.IsChecked       == true);
                store.SetBoolean(SettingsCollection, "FilterAsca",       _filterAsca);
                store.SetBoolean(SettingsCollection, "FilterOss",        _filterOss);
                store.SetBoolean(SettingsCollection, "FilterSecrets",    _filterSecrets);
                store.SetBoolean(SettingsCollection, "FilterIac",        _filterIac);
                store.SetBoolean(SettingsCollection, "FilterContainers", _filterContainers);
                store.SetInt32(SettingsCollection,   "SortMode",         (int)_sortMode);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoredFindings.SaveFilterState");
            }
        }

        private void LoadFilterState()
        {
            try
            {
                var store = new ShellSettingsManager(ServiceProvider.GlobalProvider)
                    .GetReadOnlySettingsStore(SettingsScope.UserSettings);
                if (!store.CollectionExists(SettingsCollection)) return;

                // Load severity filter states
                if (SeverityMaliciousButton != null) SeverityMaliciousButton.IsChecked = store.GetBoolean(SettingsCollection, "FilterMalicious",  true);
                if (SeverityCriticalButton  != null) SeverityCriticalButton.IsChecked  = store.GetBoolean(SettingsCollection, "FilterCritical",   true);
                if (SeverityHighButton      != null) SeverityHighButton.IsChecked      = store.GetBoolean(SettingsCollection, "FilterHigh",       true);
                if (SeverityMediumButton    != null) SeverityMediumButton.IsChecked    = store.GetBoolean(SettingsCollection, "FilterMedium",     true);
                if (SeverityLowButton       != null) SeverityLowButton.IsChecked       = store.GetBoolean(SettingsCollection, "FilterLow",        true);

                // Load scanner filter states
                _filterAsca       = store.GetBoolean(SettingsCollection, "FilterAsca",       true);
                _filterOss        = store.GetBoolean(SettingsCollection, "FilterOss",        true);
                _filterSecrets    = store.GetBoolean(SettingsCollection, "FilterSecrets",    true);
                _filterIac        = store.GetBoolean(SettingsCollection, "FilterIac",        true);
                _filterContainers = store.GetBoolean(SettingsCollection, "FilterContainers", true);

                // Load sort mode
                _sortMode         = (IgnoredFindingsSortMode)store.GetInt32(SettingsCollection, "SortMode", 0);

                // Sync VulnType menu checkmarks
                if (VulnSAST       != null) VulnSAST.IsChecked       = _filterAsca;
                if (VulnSCA        != null) VulnSCA.IsChecked         = _filterOss;
                if (VulnSecrets    != null) VulnSecrets.IsChecked     = _filterSecrets;
                if (VulnIaC        != null) VulnIaC.IsChecked         = _filterIac;
                if (VulnContainers != null) VulnContainers.IsChecked  = _filterContainers;

                // Sync sort menu checkmarks
                if (SortSevHighLow != null) SortSevHighLow.IsChecked = _sortMode == IgnoredFindingsSortMode.SeverityHighToLow;
                if (SortSevLowHigh != null) SortSevLowHigh.IsChecked = _sortMode == IgnoredFindingsSortMode.SeverityLowToHigh;
                if (SortDateNew    != null) SortDateNew.IsChecked     = _sortMode == IgnoredFindingsSortMode.DateAddedNewestFirst;
                if (SortDateOld    != null) SortDateOld.IsChecked     = _sortMode == IgnoredFindingsSortMode.DateAddedOldestFirst;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoredFindings.LoadFilterState");
            }
        }

        private static bool IsDarkTheme()
        {
            try
            {
                var color = Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(
                    Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey);
                int brightness = (int)Math.Sqrt(color.R * color.R * 0.299 + color.G * color.G * 0.587 + color.B * color.B * 0.114);
                return brightness < 128;
            }
            catch { return true; }
        }
    }
}
