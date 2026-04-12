using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
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
                var buffer = GutterIcons.CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile(kv.Key);
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
        /// Tries to get the file path for the buffer from <see cref="ITextDocument"/> (when the buffer is backed by a file).
        /// Uses <see cref="typeof(ITextDocument)"/> as the property-bag key (same as the editor). Do not use
        /// <see cref="Type.GetType(string)"/> with a simple assembly name — that returns null unless that assembly is already loaded.
        /// </summary>
        private static string TryGetFilePathFromBuffer(ITextBuffer buffer)
        {
            if (buffer?.Properties == null) return null;
            try
            {
                if (!buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) || document == null)
                    return null;
                return document.FilePath;
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
        /// Nudges the tagging system so <see cref="CxAssistGlyphTaggerProvider"/> / <see cref="CxAssistErrorTaggerProvider"/> materialize for buffers
        /// that were resolved via RDT before a view existed.
        /// </summary>
        private static void TryEnsureTaggersMaterialized(ITextBuffer buffer)
        {
            if (buffer == null)
                return;

            try
            {
                var mef = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
                var factory = mef?.GetService<IBufferTagAggregatorFactoryService>();
                if (factory == null)
                    return;

                var snapshot = buffer.CurrentSnapshot;
                var span = snapshot.Length > 0
                    ? new SnapshotSpan(snapshot, 0, snapshot.Length)
                    : new SnapshotSpan(snapshot, 0, 0);
                var spans = new NormalizedSnapshotSpanCollection(span);

                using (ITagAggregator<CxAssistGlyphTag> glyphAgg = factory.CreateTagAggregator<CxAssistGlyphTag>(buffer))
                using (ITagAggregator<IErrorTag> errorAgg = factory.CreateTagAggregator<IErrorTag>(buffer))
                {
                    if (glyphAgg != null && spans.Count > 0)
                        _ = glyphAgg.GetTags(spans);
                    if (errorAgg != null && spans.Count > 0)
                        _ = errorAgg.GetTags(spans);
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.TryEnsureTaggersMaterialized");
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

            ApplyGutterAndErrorTaggersToBuffer(buffer, list);

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
        /// Applies gutter icons and error tag underlines for the given buffer (must run on the UI thread).
        /// </summary>
        private static async Task ApplyGutterAndErrorTaggersToBufferCoreAsync(ITextBuffer buffer, List<Vulnerability> list)
        {
            if (buffer == null)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            TryEnsureTaggersMaterialized(buffer);

            var deadline = Environment.TickCount + 500;
            while (Environment.TickCount < deadline)
            {
                var glyphTagger = CxAssistErrorHandler.TryGet(() => CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetGlyphTagger", null);
                var errorTagger = CxAssistErrorHandler.TryGet(() => CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetErrorTagger", null);
                if (glyphTagger != null && errorTagger != null)
                    break;
                await Task.Delay(15);
            }

            var glyphTaggerFinal = CxAssistErrorHandler.TryGet(() => CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetGlyphTagger", null);
            if (glyphTaggerFinal != null)
                CxAssistErrorHandler.TryRun(() => glyphTaggerFinal.UpdateVulnerabilities(list), "Coordinator.GlyphTagger.UpdateVulnerabilities");

            var errorTaggerFinal = CxAssistErrorHandler.TryGet(() => CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer), "Coordinator.GetErrorTagger", null);
            if (errorTaggerFinal != null)
                CxAssistErrorHandler.TryRun(() => errorTaggerFinal.UpdateVulnerabilities(list), "Coordinator.ErrorTagger.UpdateVulnerabilities");
        }

        /// <summary>
        /// Applies gutter icons and error tag underlines for the given buffer (marshals to UI thread).
        /// </summary>
        private static void ApplyGutterAndErrorTaggersToBuffer(ITextBuffer buffer, List<Vulnerability> list)
        {
            if (buffer == null)
                return;

            try
            {
                ThreadHelper.JoinableTaskFactory.Run(() => ApplyGutterAndErrorTaggersToBufferCoreAsync(buffer, list));
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.ApplyGutterAndErrorTaggersToBuffer.UI");
            }
        }

        /// <summary>
        /// OSS / manifest scans often finish before the JSON buffer is registered in the RDT or before taggers exist.
        /// Retries on the UI thread so gutter and squiggles apply once <see cref="CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile"/> succeeds.
        /// </summary>
        private static void ScheduleApplyGutterWhenBufferAvailable(string filePath, List<Vulnerability> findings)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var payload = findings != null ? new List<Vulnerability>(findings) : new List<Vulnerability>();

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var buffer = CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile(filePath);
                        if (buffer != null)
                        {
                            await ApplyGutterAndErrorTaggersToBufferCoreAsync(buffer, payload);
                            return;
                        }

                        await Task.Delay(60);
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.ScheduleApplyGutterWhenBufferAvailable");
                }
            });
        }

        /// <summary>
        /// Replaces stored and displayed findings for one scanner on a file, preserving findings from other scanners.
        /// Use for realtime scan results so an engine returning 0 issues does not clear sibling engines' issues on the same file.
        /// </summary>
        public static void MergeUpdateFindingsForScanner(string filePath, ScannerType scanner, List<Vulnerability> vulnerabilitiesFromScanner)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var incoming = vulnerabilitiesFromScanner != null
                ? vulnerabilitiesFromScanner.FindAll(v => v.Scanner == scanner && CxAssistConstants.IsScannerEnabled(v.Scanner))
                : new List<Vulnerability>();

            string key = NormalizePath(filePath);
            if (string.IsNullOrEmpty(key))
                return;

            List<Vulnerability> mergedForUi;
            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            lock (_lock)
            {
                _fileToIssues.TryGetValue(key, out var existing);
                var merged = existing != null
                    ? existing.Where(v => v.Scanner != scanner).ToList()
                    : new List<Vulnerability>();
                merged.AddRange(incoming);

                mergedForUi = new List<Vulnerability>(merged);
                if (merged.Count == 0)
                    _fileToIssues.Remove(key);
                else
                    _fileToIssues[key] = merged;

                var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _fileToIssues)
                    copy[kv.Key] = new List<Vulnerability>(kv.Value);
                snapshot = copy;
            }

            CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.DECORATING_UI_FOR_FILE, mergedForUi.Count, filePath));

            var buffer = CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile(filePath);
            if (buffer != null)
                ApplyGutterAndErrorTaggersToBuffer(buffer, mergedForUi);
            else
                ScheduleApplyGutterWhenBufferAvailable(filePath, mergedForUi);

            IssuesUpdated?.Invoke(snapshot);
        }

        /// <summary>
        /// Replaces all findings for a single file in storage and notifies subscribers.
        /// Does not update gutter/underlines; does not merge with other scanners — prefer <see cref="MergeUpdateFindingsForScanner"/> for per-engine updates.
        /// </summary>
        /// <param name="filePath">File path to update findings for.</param>
        /// <param name="vulnerabilities">Findings for this file; null or empty removes this file from stored issues.</param>
        public static void UpdateFindingsForFile(string filePath, List<Vulnerability> vulnerabilities)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // Filter out findings from disabled scanners (aligned with UpdateFindings)
            var list = vulnerabilities != null
                ? vulnerabilities.FindAll(v => CxAssistConstants.IsScannerEnabled(v.Scanner))
                : new List<Vulnerability>();

            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            lock (_lock)
            {
                string key = NormalizePath(filePath);
                if (string.IsNullOrEmpty(key)) return;

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
        }

        /// <summary>
        /// Sets the stored findings by file and raises IssuesUpdated without updating gutter/underline.
        /// CLEARS ALL previous findings from all files before setting new ones.
        /// Use when displaying fallback data (e.g. package.json mock) in the Findings window so the Error List shows the same data.
        /// For realtime scanner updates on a file shared by multiple engines, use <see cref="MergeUpdateFindingsForScanner"/> instead.
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
        /// Clears all findings from all files and notifies subscribers.
        /// Also clears gutter icons and underlines in all open files.
        /// Called only on logout to completely remove all findings.
        /// </summary>
        public static void ClearAllFindings()
        {
            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            var filesToClear = new List<string>();

            lock (_lock)
            {
                filesToClear.AddRange(_fileToIssues.Keys);
                _fileToIssues.Clear();
                snapshot = new Dictionary<string, List<Vulnerability>>(StringComparer.OrdinalIgnoreCase);
            }

            // Clear gutter icons and underlines for all files that had findings (UI thread)
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    foreach (var filePath in filesToClear)
                    {
                        var buffer = GutterIcons.CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile(filePath);
                        if (buffer != null)
                        {
                            // Update taggers with empty list to clear all icons and underlines
                            var glyphTagger = CxAssistErrorHandler.TryGet(() => GutterIcons.CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer), "", null);
                            if (glyphTagger != null)
                                CxAssistErrorHandler.TryRun(() => glyphTagger.UpdateVulnerabilities(new List<Vulnerability>()), "");

                            var errorTagger = CxAssistErrorHandler.TryGet(() => Markers.CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer), "", null);
                            if (errorTagger != null)
                                CxAssistErrorHandler.TryRun(() => errorTagger.UpdateVulnerabilities(new List<Vulnerability>()), "");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.ClearAllFindings.UI");
            }

            // Notify subscribers (clears Findings window and Error List)
            IssuesUpdated?.Invoke(snapshot);
        }

        /// <summary>
        /// Clears findings only from disabled scanners, preserving findings from enabled scanners.
        /// Also updates gutter icons and underlines in all open files.
        /// Called when user toggles a scanner on/off in settings without logging out.
        /// </summary>
        public static void ClearFindingsFromDisabledScanners()
        {
            IReadOnlyDictionary<string, List<Vulnerability>> snapshot;
            var filesToRefresh = new List<string>();

            lock (_lock)
            {
                // Iterate all files and remove vulnerabilities from disabled scanners
                foreach (var filePath in _fileToIssues.Keys.ToList())
                {
                    var list = _fileToIssues[filePath];
                    if (list == null) continue;

                    // Keep only findings from enabled scanners
                    var filtered = list.Where(v => CxAssistConstants.IsScannerEnabled(v.Scanner)).ToList();

                    if (filtered.Count == 0)
                    {
                        // Remove file entry if no findings left
                        _fileToIssues.Remove(filePath);
                        filesToRefresh.Add(filePath);
                    }
                    else if (filtered.Count < list.Count)
                    {
                        // Update file entry with filtered findings
                        _fileToIssues[filePath] = filtered;
                        filesToRefresh.Add(filePath);
                    }
                }

                // Create snapshot for notification
                var copy = new Dictionary<string, List<Vulnerability>>(_fileToIssues.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in _fileToIssues)
                    copy[kv.Key] = new List<Vulnerability>(kv.Value);
                snapshot = copy;
            }

            // Update gutter icons and underlines for all affected files (UI thread)
            try
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    foreach (var filePath in filesToRefresh)
                    {
                        var buffer = GutterIcons.CxAssistGlyphTaggerProvider.ResolveBufferForOpenFile(filePath);
                        if (buffer != null)
                        {
                            // Get remaining findings for this file (after filtering)
                            var remainingVulns = snapshot.ContainsKey(NormalizePath(filePath))
                                ? snapshot[NormalizePath(filePath)]
                                : new List<Vulnerability>();

                            // Update both gutter and underline taggers
                            var glyphTagger = CxAssistErrorHandler.TryGet(() => GutterIcons.CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer), "", null);
                            if (glyphTagger != null)
                                CxAssistErrorHandler.TryRun(() => glyphTagger.UpdateVulnerabilities(remainingVulns), "");

                            var errorTagger = CxAssistErrorHandler.TryGet(() => Markers.CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer), "", null);
                            if (errorTagger != null)
                                CxAssistErrorHandler.TryRun(() => errorTagger.UpdateVulnerabilities(remainingVulns), "");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "DisplayCoordinator.ClearFindingsFromDisabledScanners.UI");
            }

            // Notify subscribers (updates Findings window and Error List)
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
    }
}
