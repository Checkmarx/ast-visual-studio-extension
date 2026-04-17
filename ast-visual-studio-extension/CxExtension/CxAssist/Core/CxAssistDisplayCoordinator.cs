using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.GutterIcons;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Single coordinator for CxAssist display (Option B).
    /// Takes one List&lt;Vulnerability&gt; and updates gutter, underline, and problem window in one go.
    /// Stores issues per file (like reference ProblemHolderService) and notifies via IssuesUpdated so the findings window can subscribe and stay in sync.
    /// </summary>
    public static class CxAssistDisplayCoordinator
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, List<Vulnerability>> _fileToIssues = new Dictionary<string, List<Vulnerability>>(StringComparer.OrdinalIgnoreCase);
        private static bool _themeHandlerRegistered;

        /// <summary>
        /// Subscribes to AssistIconLoader.ThemeChanged so all open taggers re-render
        /// with the new theme icons (aligned with JetBrains createProblemDescriptorsOnThemeChanged).
        /// Call once at startup.
        /// </summary>
        public static void EnsureThemeChangeHandler()
        {
            if (_themeHandlerRegistered) return;
            _themeHandlerRegistered = true;
            AssistIconLoader.EnsureThemeChangeSubscription();
            AssistIconLoader.ThemeChanged += OnThemeChanged;
        }

        private static void OnThemeChanged()
        {
            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            lock (_lock)
            {
                var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _fileToIssues)
                    copy[kv.Key] = new List<Vulnerability>(kv.Value);
                snapshot = copy;
            }
            foreach (var kv in snapshot)
            {
                var buffer = GutterIcons.CxAssistGlyphTaggerProvider.GetBufferForFile(kv.Key);
                if (buffer != null)
                    UpdateFindings(buffer, kv.Value, kv.Key);
            }
            IssuesUpdated?.Invoke(snapshot);
        }

        /// <summary>
        /// Normalizes a file path for use as the per-file map key (same file always maps to the same key).
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.NormalizePath");
                return path;
            }
        }

        /// <summary>
        /// Gets the file path for the buffer when it is backed by a file (e.g. for passing to mock data or scan).
        /// Returns null if the buffer has no associated document.
        /// </summary>
        public static string GetFilePathForBuffer(ITextBuffer buffer) => TryGetFilePathFromBuffer(buffer);

        /// <summary>
        /// Tries to get the file path for the buffer from ITextDocument (when the buffer is backed by a file).
        /// Uses reflection so we don't require an extra assembly reference.
        /// </summary>
        private static string TryGetFilePathFromBuffer(ITextBuffer buffer)
        {
            if (buffer?.Properties == null) return null;
            try
            {
                // ITextDocument is in Microsoft.VisualStudio.Text.Logic (or Text.Data); key is often the type
                var docType = Type.GetType("Microsoft.VisualStudio.Text.ITextDocument, Microsoft.VisualStudio.Text.Logic", false)
                    ?? Type.GetType("Microsoft.VisualStudio.Text.ITextDocument, Microsoft.VisualStudio.Text.Data", false);
                if (docType == null) return null;
                if (!buffer.Properties.TryGetProperty(docType, out object doc) || doc == null) return null;
                var pathProp = docType.GetProperty("FilePath", BindingFlags.Public | BindingFlags.Instance);
                return pathProp?.GetValue(doc) as string;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Raised when issues are updated (any file). Subscribers (e.g. findings window) can refresh to stay in sync (reference ISSUE_TOPIC-like).
        /// </summary>
        public static event Action<IReadOnlyDictionary<string, List<Vulnerability>>> IssuesUpdated;

        /// <summary>
        /// Gets all issues by file path (like reference ProblemHolderService.GetAllIssues).
        /// </summary>
        public static IReadOnlyDictionary<string, List<Vulnerability>> GetAllIssuesByFile()
        {
            lock (_lock)
            {
                var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _fileToIssues)
                    copy[kv.Key] = new List<Vulnerability>(kv.Value);
                return copy;
            }
        }

        /// <summary>
        /// Gets the current findings as a single flattened list (for backward compatibility and for BuildFileNodesFromVulnerabilities).
        /// </summary>
        public static List<Vulnerability> GetCurrentFindings()
        {
            lock (_lock)
            {
                if (_fileToIssues.Count == 0) return null;
                var flat = new List<Vulnerability>();
                foreach (var list in _fileToIssues.Values)
                    flat.AddRange(list);
                return flat;
            }
        }

        /// <summary>
        /// Finds a vulnerability by Id in the current findings (e.g. from Error List task HelpKeyword).
        /// </summary>
        public static Vulnerability FindVulnerabilityById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_lock)
            {
                foreach (var list in _fileToIssues.Values)
                {
                    var v = list?.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                    if (v != null) return v;
                }
                return null;
            }
        }

        /// <summary>
        /// Finds the first vulnerability at the given location (for Error List selection by document + line).
        /// </summary>
        /// <param name="documentPath">Full path of the file (normalized for comparison).</param>
        /// <param name="zeroBasedLine">0-based line number (Error List uses 0-based).</param>
        public static Vulnerability FindVulnerabilityByLocation(string documentPath, int zeroBasedLine)
        {
            if (string.IsNullOrEmpty(documentPath)) return null;
            string key;
            try { key = Path.GetFullPath(documentPath); }
            catch { key = documentPath; }
            lock (_lock)
            {
                if (!_fileToIssues.TryGetValue(key, out var list) || list == null) return null;
                // Match by 0-based line: Vulnerability.LineNumber is 1-based, convert for comparison.
                return list.FirstOrDefault(v =>
                    CxAssistConstants.To0BasedLineForEditor(v.Scanner, v.LineNumber) == zeroBasedLine);
            }
        }

        /// <summary>
        /// Finds all OSS vulnerabilities for the same package (same PackageName + PackageVersion) across all files.
        /// Aligned with JetBrains ScanIssue.getVulnerabilities() which returns all CVEs for a package.
        /// Returns null for non-OSS scanners or when no additional vulnerabilities exist.
        /// </summary>
        public static List<Vulnerability> FindAllVulnerabilitiesForPackage(Vulnerability v)
        {
            if (v == null || v.Scanner != Models.ScannerType.OSS || string.IsNullOrEmpty(v.PackageName))
                return null;

            lock (_lock)
            {
                var result = new List<Vulnerability>();
                foreach (var list in _fileToIssues.Values)
                {
                    if (list == null) continue;
                    foreach (var vuln in list)
                    {
                        if (vuln.Scanner == Models.ScannerType.OSS
                            && string.Equals(vuln.PackageName, v.PackageName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(vuln.PackageVersion, v.PackageVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(vuln);
                        }
                    }
                }
                return result.Count > 0 ? result : null;
            }
        }

        /// <summary>
        /// Finds all vulnerabilities of the same scanner type on the same line in the same file.
        /// Used for IaC/ASCA where multiple issues can be grouped on a single line
        /// (aligned with JetBrains IacScanResultAdaptor grouping by filePath:line).
        /// Returns null when no matching vulnerabilities exist.
        /// </summary>
        public static List<Vulnerability> FindAllVulnerabilitiesForLine(Vulnerability v)
        {
            if (v == null || string.IsNullOrEmpty(v.FilePath))
                return null;

            string key;
            try { key = Path.GetFullPath(v.FilePath); }
            catch { key = v.FilePath; }

            int zeroBasedLine = CxAssistConstants.To0BasedLineForEditor(v.Scanner, v.LineNumber);

            lock (_lock)
            {
                if (!_fileToIssues.TryGetValue(key, out var list) || list == null)
                    return null;

                var result = list.Where(vuln =>
                    vuln.Scanner == v.Scanner
                    && CxAssistConstants.To0BasedLineForEditor(vuln.Scanner, vuln.LineNumber) == zeroBasedLine)
                    .ToList();

                return result.Count > 0 ? result : null;
            }
        }

        /// <summary>
        /// Updates gutter icons, underlines (squiggles), and stored findings for the problem window in one call.
        /// Stores issues per file and raises IssuesUpdated so the findings window can stay in sync (reference-like).
        /// </summary>
        /// <param name="buffer">Text buffer for the open file (used to get glyph and error taggers).</param>
        /// <param name="vulnerabilities">Findings to show; can be null or empty to clear for this file.</param>
        /// <param name="filePath">Optional. File path for per-file storage. If null, uses first vulnerability's FilePath when list is non-empty.</param>
        public static void UpdateFindings(ITextBuffer buffer, List<Vulnerability> vulnerabilities, string filePath = null)
        {
            if (buffer == null)
                return;

            // Filter out findings from disabled scanners (aligned with JetBrains DevAssistFileListener.getScanIssuesForEnabledScanner)
            var list = vulnerabilities != null
                ? vulnerabilities.FindAll(v => CxAssistConstants.IsScannerEnabled(v.Scanner))
                : new List<Vulnerability>();

            if (vulnerabilities == null || vulnerabilities.Count == 0)
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.NO_VULNERABILITIES_FOR_FILE, filePath ?? "unknown"));

            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.DECORATING_UI_FOR_FILE, list.Count, filePath ?? "unknown"));

            // 1. Update gutter
            var glyphTagger = CxAssistErrorHandler.TryGet(() => CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetGlyphTagger", null);
            if (glyphTagger != null)
                CxAssistErrorHandler.TryRun(() => glyphTagger.UpdateVulnerabilities(list), "Coordinator.GlyphTagger.UpdateVulnerabilities");

            // 2. Update underline
            var errorTagger = CxAssistErrorHandler.TryGet(() => CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetErrorTagger", null);
            if (errorTagger != null)
                CxAssistErrorHandler.TryRun(() => errorTagger.UpdateVulnerabilities(list), "Coordinator.ErrorTagger.UpdateVulnerabilities");

            // 3. Store per file and notify (reference ProblemHolderService + ISSUE_TOPIC-like)
            CxAssistErrorHandler.TryRun(() =>
            {
                // Prefer explicit filePath, then path from buffer (so we can clear when list is empty), then first vulnerability
                string resolvedPath = filePath ?? TryGetFilePathFromBuffer(buffer) ?? (list.Count > 0 ? list[0].FilePath : null);
                string key = NormalizePath(resolvedPath);
                if (string.IsNullOrEmpty(key)) return;

                IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
                lock (_lock)
                {
                    if (list.Count == 0)
                        _fileToIssues.Remove(key);
                    else
                        _fileToIssues[key] = new List<Vulnerability>(list);
                    var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in _fileToIssues)
                        copy[kv.Key] = new List<Vulnerability>(kv.Value);
                    snapshot = copy;
                }
                IssuesUpdated?.Invoke(snapshot);
            }, "Coordinator.StoreCurrentFindings");

        }

        /// <summary>
        /// Sets the stored findings by file and raises IssuesUpdated without updating gutter/underline.
        /// Use when displaying fallback data (e.g. package.json mock) in the Findings window so the Error List shows the same data.
        /// </summary>
        public static void SetFindingsByFile(IReadOnlyDictionary<string, List<Vulnerability>> issuesByFile)
        {
            if (issuesByFile == null) return;

            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            lock (_lock)
            {
                _fileToIssues.Clear();
                foreach (var kv in issuesByFile)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                    string key = NormalizePath(kv.Key);
                    if (string.IsNullOrEmpty(key)) continue;
                    _fileToIssues[key] = new List<Vulnerability>(kv.Value);
                }
                var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _fileToIssues)
                    copy[kv.Key] = new List<Vulnerability>(kv.Value);
                snapshot = copy;
            }
            IssuesUpdated?.Invoke(snapshot);
        }

        /// <summary>
        /// Returns the cached vulnerabilities for the given file path, or null if none exist.
        /// Used to restore gutter icons and underlines when a file is reopened (JetBrains: DevAssistFileListener.restoreGutterIcons).
        /// </summary>
        public static List<Vulnerability> GetCachedVulnerabilitiesForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            string key = NormalizePath(filePath);
            if (string.IsNullOrEmpty(key)) return null;
            lock (_lock)
            {
                if (_fileToIssues.TryGetValue(key, out var list) && list != null && list.Count > 0)
                    return new List<Vulnerability>(list);
                return null;
            }
        }

        /// <summary>
        /// Updates the problem window control with the current findings (builds FileNodes and calls SetAllFileNodes).
        /// Call this when the Findings window is shown so it displays the same data as gutter/underline.
        /// </summary>
        /// <param name="findingsControl">The CxAssist Findings control to update.</param>
        /// <param name="loadSeverityIcon">Optional; if null, severity icons are not set.</param>
        /// <param name="loadFileIcon">Optional; callback (filePath -> ImageSource) for file-type icon per file. If null, file icon is not set.</param>
        public static void RefreshProblemWindow(
            CxAssistFindingsControl findingsControl,
            Func<string, System.Windows.Media.ImageSource> loadSeverityIcon = null,
            Func<string, System.Windows.Media.ImageSource> loadFileIcon = null)
        {
            if (findingsControl == null) return;

            CxAssistErrorHandler.TryRun(() =>
            {
                List<Vulnerability> current = GetCurrentFindings();
                ObservableCollection<FileNode> fileNodes = current != null && current.Count > 0
                    ? FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current, loadSeverityIcon, loadFileIcon)
                    : new ObservableCollection<FileNode>();
                findingsControl.SetAllFileNodes(fileNodes);
            }, "Coordinator.RefreshProblemWindow");
        }

        /// <summary>
        /// Clears all findings from the coordinator.
        /// Used when user logs out or disables all scanners.
        /// </summary>
        public static void ClearAllFindings()
        {
            lock (_lock)
            {
                _fileToIssues.Clear();
            }
            IssuesUpdated?.Invoke(new Dictionary<string, List<Vulnerability>>());
        }

        /// <summary>
        /// Clears findings from disabled scanners only.
        /// Called when user toggles a scanner off via preferences.
        /// </summary>
        public static void ClearFindingsFromDisabledScanners()
        {
            lock (_lock)
            {
                foreach (var filePath in _fileToIssues.Keys.ToList())
                {
                    _fileToIssues[filePath] = _fileToIssues[filePath]
                        .Where(v => v != null && v.Scanner != 0)
                        .ToList();

                    if (_fileToIssues[filePath].Count == 0)
                        _fileToIssues.Remove(filePath);
                }
            }
            IssuesUpdated?.Invoke(GetAllIssuesByFile());
        }
    }
}
