using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore
{
    /// <summary>
    /// Business logic on top of <see cref="IgnoreFileManager"/>. Builds <see cref="IgnoreEntry"/> from
    /// <see cref="Vulnerability"/> instances, matches new scan results against the persisted list, and
    /// applies "Ignore all of this type" expansion across files. Mirrors JetBrains <c>IgnoreManager</c>.
    /// </summary>
    public static class IgnoreManager
    {
        /// <summary>
        /// Snapshots of revived entries, keyed by their storage key. Populated by <see cref="ReviveSingle"/>
        /// and <see cref="ReviveMultiple"/> so that <see cref="Restore"/> can re-insert the exact same entry
        /// without needing the caller to supply the snapshot.
        /// </summary>
        private static readonly Dictionary<string, IgnoreEntry> _revivedSnapshots =
            new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);

        /// <summary>
        /// Persists "ignore this vulnerability" for <paramref name="vuln"/>. Returns the storage key
        /// for later revive.
        /// </summary>
        public static string AddIgnoredEntry(Vulnerability vuln)
        {
            if (vuln == null) return null;
            TelemetryService.LogIgnoreThis(vuln);
            var title = ResolveTitle(vuln);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Adding ignore entry for issue: {title}");
            var entry = BuildEntry(vuln);
            string key = BuildKey(vuln);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Ignoring {key}");
            IgnoreFileManager.UpsertEntry(key, entry);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Successfully added ignore entry for issue: {title}");
            TriggerRescanForVulnerability(vuln);
            return key;
        }

        /// <summary>
        /// "Ignore all of this type" — extends a single entry with every matching finding currently
        /// in the display coordinator (across all files). Only meaningful for OSS and Containers.
        /// </summary>
        public static string AddAllIgnoredEntry(Vulnerability vuln, IEnumerable<Vulnerability> allCurrentFindings)
        {
            if (vuln == null) return null;
            TelemetryService.LogIgnoreAll(vuln);
            var title = ResolveTitle(vuln);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Adding ignore entry for issue: {title}");
            var entry = BuildEntry(vuln);
            string key = BuildKey(vuln);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Ignoring all vulnerabilities for: {key}");

            if (allCurrentFindings != null)
            {
                foreach (var other in allCurrentFindings)
                {
                    if (other == null || other == vuln) continue;
                    if (!IsSameType(vuln, other)) continue;

                    var extraRef = BuildFileReference(other);
                    if (extraRef == null) continue;
                    if (entry.Files.Any(f =>
                            string.Equals(f.Path, extraRef.Path, StringComparison.Ordinal) &&
                            f.Line == extraRef.Line &&
                            string.Equals(f.ProblematicLine, extraRef.ProblematicLine, StringComparison.Ordinal)))
                        continue;
                    entry.Files.Add(extraRef);
                }
            }

            IgnoreFileManager.UpsertEntry(key, entry);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Successfully added ignore entry for issue: {title}");
            // Trigger rescan of all affected files so gutter icons, findings tree, and error list update.
            TriggerRescanForEntry(entry);
            return key;
        }

        /// <summary>
        /// Revives a single entry: removes from ignore file, triggers rescan of affected files,
        /// and shows a VS InfoBar notification with an Undo button.
        /// Mirrors JetBrains <c>reviveSingleEntry</c>.
        /// </summary>
        public static void ReviveSingle(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Reviving entry: {key}");

            // Snapshot before revive so Undo can restore it
            IgnoreFileManager.GetAllEntries().TryGetValue(key, out var snapshot);
            int fileCount = snapshot?.Files?.Count(f => f.Active) ?? 0;

            var revived = IgnoreFileManager.ReviveEntry(key);
            if (revived == null)
            {
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Failed to revive entry: {key}");
                return;
            }
            // Cache the snapshot so Restore(key) can re-insert the exact same entry.
            lock (_revivedSnapshots) { _revivedSnapshots[key] = revived; }
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Successfully revived entry: {key}");

            TriggerRescanForEntry(revived);
            ShowReviveUndoNotification(key, revived, fileCount);
        }

        /// <summary>
        /// Revives multiple entries in bulk: removes each from ignore file, triggers rescans,
        /// and shows a summary notification (no Undo for bulk).
        /// Mirrors JetBrains <c>reviveMultipleEntries</c>.
        /// </summary>
        public static void ReviveMultiple(IEnumerable<string> keys)
        {
            if (keys == null) return;
            int successCount = 0, totalFileCount = 0, failCount = 0;

            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                IgnoreFileManager.GetAllEntries().TryGetValue(key, out var snapshot);
                int fc = snapshot?.Files?.Count(f => f.Active) ?? 0;

                var revived = IgnoreFileManager.ReviveEntry(key);
                if (revived != null)
                {
                    successCount++;
                    totalFileCount += fc;
                    lock (_revivedSnapshots) { _revivedSnapshots[key] = revived; }
                    TriggerRescanForEntry(revived);
                    CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Successfully revived entry: {key}");
                }
                else
                {
                    failCount++;
                    CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Failed to revive entry: {key}");
                }
            }

            if (successCount > 0)
            {
                string msg = successCount == 1
                    ? $"Revived 1 vulnerability in {totalFileCount} file{(totalFileCount == 1 ? "" : "s")}"
                    : $"Revived {successCount} vulnerabilities in {totalFileCount} file{(totalFileCount == 1 ? "" : "s")}";
                if (failCount > 0) msg += $" ({failCount} failed)";
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ShowInfoBar(msg, KnownMonikers.StatusInformation);
                });
            }
        }

        // Keep for callers that pass a single key (e.g. existing RowRevive_Click path).
        public static void Revive(string key) => ReviveSingle(key);

        /// <summary>
        /// Restores a previously revived entry (undo revive). Mirrors the Undo path triggered by InfoBar.
        /// </summary>
        public static void Restore(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            // First check the revived-snapshot cache (populated by ReviveSingle/ReviveMultiple)
            // so the caller does not need to supply the snapshot after a Revive.
            IgnoreEntry snapshot = null;
            lock (_revivedSnapshots)
            {
                _revivedSnapshots.TryGetValue(key, out snapshot);
                if (snapshot != null) _revivedSnapshots.Remove(key);
            }
            // Fall back to current entries in case the entry was not revived via this manager.
            if (snapshot == null)
                IgnoreFileManager.GetAllEntries().TryGetValue(key, out snapshot);
            if (snapshot == null) return;
            IgnoreFileManager.RestoreEntry(key, snapshot);
            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Restored entry: {key}");
        }

        private static void TriggerRescanForEntry(IgnoreEntry entry)
        {
            if (entry?.Files == null) return;
            var root = IgnoreFileManager.SolutionRoot;
            foreach (var fileRef in entry.Files)
            {
                if (string.IsNullOrEmpty(fileRef.Path)) continue;
                string fullPath = string.IsNullOrEmpty(root)
                    ? fileRef.Path
                    : System.IO.Path.Combine(root, fileRef.Path.Replace('/', System.IO.Path.DirectorySeparatorChar));
                _ = RealtimeScannerHost.TriggerFileScanAsync(fullPath);
            }
        }

        /// <summary>
        /// Triggers a rescan of the file associated with <paramref name="vuln"/> so that the gutter
        /// icon, findings tree, and Error List refresh immediately after the ignore entry is saved.
        /// </summary>
        private static void TriggerRescanForVulnerability(Vulnerability vuln)
        {
            if (string.IsNullOrEmpty(vuln?.FilePath)) return;
            _ = RealtimeScannerHost.TriggerFileScanAsync(vuln.FilePath);
        }

        private static void ShowReviveUndoNotification(string key, IgnoreEntry revived, int fileCount)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var serviceProvider = ServiceProvider.GlobalProvider;
                    var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                    if (factory == null) return;

                    string title = revived.Title ?? revived.PackageName ?? key;
                    string detail = $"vulnerability has been revived in {fileCount} file{(fileCount == 1 ? "" : "s")}";

                    var spans = new InfoBarTextSpan[] { new InfoBarTextSpan($"{title} — {detail}") };
                    var actions = new InfoBarActionItem[] { new InfoBarHyperlink("Undo") };
                    var model = new InfoBarModel(spans, actions, KnownMonikers.StatusInformation, isCloseButtonVisible: true);
                    var element = factory.CreateInfoBar(model);

                    if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
                    {
                        shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var hostObj);
                        if (!(hostObj is IVsInfoBarHost host)) return;

                        var handler = new ReviveUndoInfoBarHandler(host, element, key, revived);
                        element.Advise(handler, out uint cookie);
                        handler.SetCookie(cookie);
                        host.AddInfoBar(element);
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreManager.ShowReviveUndoNotification");
                }
            });
        }

        private static void ShowInfoBar(string message, Microsoft.VisualStudio.Imaging.Interop.ImageMoniker icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var serviceProvider = ServiceProvider.GlobalProvider;
                var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                if (factory == null) return;
                var model = new InfoBarModel(new[] { new InfoBarTextSpan(message) }, icon, isCloseButtonVisible: true);
                var element = factory.CreateInfoBar(model);
                if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
                {
                    shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var hostObj);
                    if (hostObj is IVsInfoBarHost host)
                    {
                        var handler = new SimpleInfoBarHandler(element);
                        element.Advise(handler, out uint c2);
                        handler.SetCookie(c2);
                        host.AddInfoBar(element);
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreManager.ShowInfoBar");
            }
        }

        // Handles Undo click on single-revive notification. Auto-closes after 5 seconds.
        private sealed class ReviveUndoInfoBarHandler : IVsInfoBarUIEvents
        {
            private readonly IVsInfoBarHost _host;
            private readonly IVsInfoBarUIElement _element;
            private readonly string _key;
            private readonly IgnoreEntry _snapshot;
            private uint _cookie;
            private readonly System.Timers.Timer _timer;
            private bool _closed;

            internal ReviveUndoInfoBarHandler(IVsInfoBarHost host, IVsInfoBarUIElement element, string key, IgnoreEntry snapshot)
            {
                _host = host; _element = element; _key = key; _snapshot = snapshot;
                _timer = new System.Timers.Timer(5000) { AutoReset = false };
                _timer.Elapsed += (s, e) => CloseAsync();
            }

            internal void SetCookie(uint cookie)
            {
                _cookie = cookie;
                _timer.Start();
            }

            private void CloseAsync()
            {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (!_closed) _element.Close();
                });
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _timer.Stop();
                IgnoreFileManager.RestoreEntry(_key, _snapshot);
                TriggerRescanForEntry(_snapshot);
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Undo revive for entry: {_key}");
                infoBarUIElement.Close();
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _closed = true;
                _timer.Stop();
                _timer.Dispose();
                infoBarUIElement.Unadvise(_cookie);
            }
        }

        private sealed class SimpleInfoBarHandler : IVsInfoBarUIEvents
        {
            private readonly IVsInfoBarUIElement _element;
            private uint _cookie;
            private bool _closed;
            private readonly System.Timers.Timer _timer;

            internal SimpleInfoBarHandler(IVsInfoBarUIElement element)
            {
                _element = element;
                _timer = new System.Timers.Timer(3000) { AutoReset = false };
                _timer.Elapsed += (s, e) =>
                {
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        if (!_closed) _element.Close();
                    });
                };
            }

            internal void SetCookie(uint cookie)
            {
                _cookie = cookie;
                _timer.Start();
            }

            public void OnActionItemClicked(IVsInfoBarUIElement e, IVsInfoBarActionItem a) { ThreadHelper.ThrowIfNotOnUIThread(); _timer.Stop(); e.Close(); }

            public void OnClosed(IVsInfoBarUIElement e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _closed = true;
                _timer.Stop();
                _timer.Dispose();
                e.Unadvise(_cookie);
            }
        }

        /// <summary>
        /// Removes ASCA ignore entries for <paramref name="filePath"/> whose ProblematicLine is no longer
        /// found in <paramref name="freshAscaFindings"/> (JetBrains <c>removeIgnoreEntriesForFileIfEmpty</c> parity).
        /// </summary>
        public static void RemoveIgnoreEntriesForFileIfEmpty(string filePath, IEnumerable<Vulnerability> freshAscaFindings)
        {
            if (string.IsNullOrEmpty(filePath) || !IgnoreFileManager.IsInitialized) return;
            string relativePath = IgnoreFileManager.NormalizePath(filePath);
            var stillPresentLines = new HashSet<string>(StringComparer.Ordinal);
            foreach (var v in freshAscaFindings ?? Enumerable.Empty<Vulnerability>())
            {
                if (v.Scanner != ScannerType.ASCA) continue;
                // JetBrains parity: ProblematicLine is returned by the CLI scan result and is
                // always populated by VulnerabilityMapper.FromAsca. Fall back to disk read only if missing.
                string line = !string.IsNullOrEmpty(v.ProblematicLine)
                    ? v.ProblematicLine
                    : TryReadTrimmedLine(v.FilePath, v.LineNumber);
                if (!string.IsNullOrEmpty(line)) stillPresentLines.Add(line);
            }
            bool changed = IgnoreFileManager.PruneStaleFileReferences(relativePath, stillPresentLines);
            if (changed)
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Pruned stale ignore entries for file: {relativePath}");
        }

        /// <summary>
        /// Updates stored line numbers for non-ASCA ignored entries (OSS, Secrets, IaC, Containers) in
        /// <paramref name="filePath"/> by matching the stored key against fresh scan results.
        /// Removes entries whose key no longer appears in the scan results for this file.
        /// JetBrains parity: <c>updateLineNumbersForIgnoredEntries</c>.
        /// </summary>
        public static void UpdateLineNumbersForIgnoredEntries(string filePath, ScannerType scanner, IEnumerable<Vulnerability> freshFindings)
        {
            if (string.IsNullOrEmpty(filePath) || !IgnoreFileManager.IsInitialized) return;
            if (scanner == ScannerType.ASCA) return; // ASCA uses ProblematicLine matching instead

            string relativePath = IgnoreFileManager.NormalizePath(filePath);

            // Build lookup: key -> line number from fresh scan results for this file
            var scanKeyToLine = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var v in freshFindings ?? Enumerable.Empty<Vulnerability>())
            {
                if (v.Scanner != scanner) continue;
                string relative = IgnoreFileManager.NormalizePath(v.FilePath);
                if (!string.Equals(relative, relativePath, StringComparison.OrdinalIgnoreCase)) continue;
                string key = BuildKey(v);
                if (!scanKeyToLine.ContainsKey(key))
                    scanKeyToLine[key] = v.LineNumber;
            }

            bool hasChanges = false;
            var keysToRemove = new List<string>();

            foreach (var kv in IgnoreFileManager.GetAllEntries())
            {
                var entry = kv.Value;
                if (entry?.Type != scanner) continue;

                if (scanKeyToLine.TryGetValue(kv.Key, out int newLine))
                {
                    // Key still present in scan — update line number if it shifted
                    foreach (var fileRef in entry.Files ?? Enumerable.Empty<IgnoreEntry.FileReference>())
                    {
                        if (!fileRef.Active) continue;
                        if (!string.Equals(fileRef.Path, relativePath, StringComparison.OrdinalIgnoreCase)) continue;
                        if (fileRef.Line != newLine)
                        {
                            fileRef.Line = newLine;
                            hasChanges = true;
                            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Updated line number for {scanner} entry '{kv.Key}' in {relativePath}: {newLine}");
                        }
                    }
                }
                else
                {
                    // Key no longer in scan results for this file — mark for removal
                    bool hasRefForFile = entry.Files?.Any(f => f.Active &&
                        string.Equals(f.Path, relativePath, StringComparison.OrdinalIgnoreCase)) == true;
                    if (hasRefForFile)
                    {
                        keysToRemove.Add(kv.Key);
                        CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Marking stale {scanner} entry '{kv.Key}' for removal (not in scan results for {relativePath})");
                    }
                }
            }

            foreach (var key in keysToRemove)
                IgnoreFileManager.RemoveEntry(key);

            if (hasChanges || keysToRemove.Count > 0)
            {
                IgnoreFileManager.ForceSaveToDisk();
                IgnoreFileManager.NotifyChanged();
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Saved line number updates for {scanner} entries in {relativePath}");
            }
        }

        /// <summary>
        /// Updates stored line numbers for ASCA ignored entries in <paramref name="filePath"/> by
        /// matching <see cref="IgnoreEntry.FileReference.ProblematicLine"/> against fresh scan findings
        /// (JetBrains <c>updateLineNumbersForIgnoredEntriesByProblematicLine</c> parity).
        /// </summary>
        public static void UpdateLineNumbersForFile(string filePath, IEnumerable<Vulnerability> freshFindings)
        {
            if (string.IsNullOrEmpty(filePath) || !IgnoreFileManager.IsInitialized) return;
            string relativePath = IgnoreFileManager.NormalizePath(filePath);
            bool anyChanged = false;

            foreach (var entry in IgnoreFileManager.GetAllEntryList())
            {
                if (entry?.Type != ScannerType.ASCA) continue;
                foreach (var fileRef in entry.Files ?? Enumerable.Empty<IgnoreEntry.FileReference>())
                {
                    if (!fileRef.Active) continue;
                    if (!string.Equals(fileRef.Path, relativePath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrEmpty(fileRef.ProblematicLine)) continue;

                    // Find fresh finding whose trimmed source line matches the stored content.
                    // JetBrains parity: ProblematicLine is returned by the CLI scan result and is
                    // always populated by VulnerabilityMapper.FromAsca. Fall back to disk read only if missing.
                    var match = freshFindings?.FirstOrDefault(v =>
                        v.Scanner == ScannerType.ASCA &&
                        v.LineNumber > 0 &&
                        string.Equals(
                            !string.IsNullOrEmpty(v.ProblematicLine) ? v.ProblematicLine : TryReadTrimmedLine(v.FilePath, v.LineNumber),
                            fileRef.ProblematicLine,
                            StringComparison.Ordinal));

                    if (match != null && match.LineNumber != fileRef.Line)
                    {
                        fileRef.Line = match.LineNumber;
                        anyChanged = true;
                        CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Updated line number for ignore entry in {relativePath}: {fileRef.Line}");
                    }
                }
            }

            if (anyChanged)
            {
                IgnoreFileManager.ForceSaveToDisk();
                IgnoreFileManager.NotifyChanged();
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: Saved updated line numbers for file: {relativePath}");
            }
        }

        /// <summary>
        /// True if <paramref name="vuln"/> matches an active file reference of any stored entry.
        /// Used by the display coordinator to filter scan results before showing them.
        /// </summary>
        public static bool IsVulnerabilityIgnored(Vulnerability vuln)
        {
            if (vuln == null) return false;
            if (!IgnoreFileManager.IsInitialized) return false;

            string relativePath = IgnoreFileManager.NormalizePath(vuln.FilePath);
            foreach (var entry in IgnoreFileManager.GetAllEntryList())
            {
                if (entry == null || entry.Type != vuln.Scanner) continue;
                if (!HasActiveFileMatch(entry, vuln, relativePath)) continue;
                if (MatchesScannerFields(entry, vuln)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the entries that produced the filtered-out findings for <paramref name="filePath"/>.
        /// Useful when building the "Ignored Findings" tree view.
        /// </summary>
        public static List<IgnoreEntry> GetActiveEntries()
        {
            return IgnoreFileManager.GetAllEntryList()
                .Where(e => e?.Files != null && e.Files.Any(f => f.Active))
                .ToList();
        }

        /// <summary>Looks up the persisted key for the given entry, or null if not stored.</summary>
        public static string FindKeyForEntry(IgnoreEntry entry)
        {
            if (entry == null) return null;
            return IgnoreFileManager.GetAllEntries()
                .FirstOrDefault(kv => ReferenceEquals(kv.Value, entry)).Key
                ?? IgnoreEntry.BuildKey(
                    entry.Type, entry.Title, entry.RuleId, entry.SimilarityId,
                    entry.PackageManager, entry.PackageName, entry.PackageVersion,
                    entry.ImageName, entry.ImageTag, entry.SecretValue,
                    entry.Files?.FirstOrDefault()?.Path);
        }

        private static string BuildKey(Vulnerability vuln)
        {
            string relative = IgnoreFileManager.NormalizePath(vuln.FilePath);
            return IgnoreEntry.BuildKey(
                vuln.Scanner,
                ResolveTitle(vuln),
                TryParseRuleId(vuln),
                vuln.Id,                            // similarity id placeholder (IaC)
                vuln.PackageManager,
                vuln.PackageName,
                vuln.PackageVersion,
                vuln.PackageName,                   // image name (Containers map to PackageName)
                vuln.PackageVersion,                // image tag
                vuln.Description,                   // secret value placeholder
                relative);
        }

        private static IgnoreEntry BuildEntry(Vulnerability vuln)
        {
            var fileRef = BuildFileReference(vuln);
            var entry = new IgnoreEntry
            {
                Type = vuln.Scanner,
                Title = ResolveTitle(vuln),
                Severity = CxAssistConstants.GetRichSeverityName(vuln.Severity),
                Description = vuln.Description,
                DateAdded = DateTime.UtcNow.ToString("O"),
                Files = new List<IgnoreEntry.FileReference>()
            };
            if (fileRef != null) entry.Files.Add(fileRef);

            switch (vuln.Scanner)
            {
                case ScannerType.OSS:
                    entry.PackageManager = vuln.PackageManager;
                    entry.PackageName = vuln.PackageName;
                    entry.PackageVersion = vuln.PackageVersion;
                    break;
                case ScannerType.Containers:
                    entry.ImageName = vuln.PackageName;
                    entry.ImageTag = vuln.PackageVersion;
                    break;
                case ScannerType.ASCA:
                    entry.RuleId = TryParseRuleId(vuln);
                    break;
                case ScannerType.IaC:
                    entry.SimilarityId = vuln.Id;
                    break;
                case ScannerType.Secrets:
                    entry.SecretValue = vuln.Description;
                    break;
            }
            return entry;
        }

        private static IgnoreEntry.FileReference BuildFileReference(Vulnerability vuln)
        {
            if (string.IsNullOrEmpty(vuln.FilePath)) return null;
            var fileRef = new IgnoreEntry.FileReference
            {
                Path = IgnoreFileManager.NormalizePath(vuln.FilePath),
                Active = true,
                Line = vuln.LineNumber > 0 ? vuln.LineNumber : (int?)null
            };
            if (vuln.Scanner == ScannerType.ASCA)
            {
                // JetBrains parity: ProblematicLine is returned directly by the CLI in the scan result.
                // Prefer it over a disk read so that ignoring an unsaved (moved) vulnerability stores
                // the correct source-line content for future content-based matching.
                fileRef.ProblematicLine = !string.IsNullOrEmpty(vuln.ProblematicLine)
                    ? vuln.ProblematicLine
                    : TryReadTrimmedLine(vuln.FilePath, vuln.LineNumber);
            }
            return fileRef;
        }

        private static bool IsSameType(Vulnerability a, Vulnerability b)
        {
            if (a.Scanner != b.Scanner) return false;
            switch (a.Scanner)
            {
                case ScannerType.OSS:
                    return string.Equals(a.PackageManager, b.PackageManager, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.PackageName, b.PackageName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.PackageVersion, b.PackageVersion, StringComparison.OrdinalIgnoreCase);
                case ScannerType.Containers:
                    return string.Equals(a.PackageName, b.PackageName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.PackageVersion, b.PackageVersion, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static bool HasActiveFileMatch(IgnoreEntry entry, Vulnerability vuln, string relativePath)
        {
            if (entry.Files == null || entry.Files.Count == 0) return false;
            foreach (var fileRef in entry.Files)
            {
                if (!fileRef.Active) continue;

                // Containers don't bind to files (image+tag identifies the entry).
                if (entry.Type == ScannerType.Containers) return true;

                if (string.Equals(fileRef.Path, relativePath, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool MatchesScannerFields(IgnoreEntry entry, Vulnerability vuln)
        {
            switch (entry.Type)
            {
                case ScannerType.OSS:
                    return Eq(entry.PackageManager, vuln.PackageManager)
                        && Eq(entry.PackageName, vuln.PackageName)
                        && Eq(entry.PackageVersion, vuln.PackageVersion);

                case ScannerType.Containers:
                    return Eq(entry.ImageName, vuln.PackageName)
                        && Eq(entry.ImageTag, vuln.PackageVersion);

                case ScannerType.IaC:
                    return Eq(entry.Title, ResolveTitle(vuln))
                        && Eq(entry.SimilarityId, vuln.Id);

                case ScannerType.Secrets:
                    return Eq(entry.Title, ResolveTitle(vuln))
                        && Eq(entry.SecretValue, vuln.Description);

                case ScannerType.ASCA:
                    if (!Eq(entry.Title, ResolveTitle(vuln))) return false;
                    // JetBrains parity: prefer ProblematicLine from the CLI scan result (always populated
                    // by VulnerabilityMapper.FromAsca). Fall back to disk read only if missing.
                    string current = !string.IsNullOrEmpty(vuln.ProblematicLine)
                        ? vuln.ProblematicLine
                        : TryReadTrimmedLine(vuln.FilePath, vuln.LineNumber);
                    if (string.IsNullOrEmpty(current))
                    {
                        // Fall back to active file ref's stored line content (JetBrains parity).
                        string stored = entry.Files?.FirstOrDefault(f => f.Active)?.ProblematicLine;
                        return !string.IsNullOrEmpty(stored);
                    }
                    return entry.Files?.Any(f => f.Active && Eq(f.ProblematicLine, current)) == true
                        || entry.Files?.Any(f => f.Active && f.Line == vuln.LineNumber) == true;

                default:
                    return false;
            }
        }

        private static bool Eq(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        private static string ResolveTitle(Vulnerability vuln)
        {
            if (vuln == null) return null;
            if (vuln.Scanner == ScannerType.ASCA)
                return !string.IsNullOrEmpty(vuln.RuleName) ? vuln.RuleName : vuln.Title;
            return vuln.Title;
        }

        private static int? TryParseRuleId(Vulnerability vuln)
        {
            if (vuln == null) return null;
            if (vuln.Scanner != ScannerType.ASCA) return null;
            if (int.TryParse(vuln.RuleName, out int n)) return n;
            // ASCA detail.RuleId is numeric on CLI side but isn't carried through Vulnerability — use Id hash fallback.
            return null;
        }

        private static string TryReadTrimmedLine(string filePath, int line1Based)
        {
            if (string.IsNullOrEmpty(filePath) || line1Based < 1) return null;
            try
            {
                if (!File.Exists(filePath)) return null;
                int n = 0;
                foreach (var line in File.ReadLines(filePath))
                {
                    n++;
                    if (n == line1Based) return line?.Trim();
                }
            }
            catch
            {
                // file locked, transient — ignore
            }
            return null;
        }
    }
}
