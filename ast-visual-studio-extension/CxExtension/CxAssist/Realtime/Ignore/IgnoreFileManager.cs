using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore
{
    /// <summary>
    /// Persists ignore entries using layered resolution:
    /// 1. Solution folder: &lt;solution&gt;/.vs/.checkmarxIgnored (file layer)
    /// 2. Repository root: &lt;repo-root&gt;/.vs/.checkmarxIgnored (fallback file layer for workspace development)
    /// 3. Windows Registry: HKEY_CURRENT_USER\Software\Checkmarx\AST\IgnoredFindings (user-level, machine-specific)
    ///
    /// Resolution order: File-based entries merged with Registry entries (Registry takes precedence for same key).
    /// Watches the closest file for external changes (git pull, manual edits).
    /// Mirrors JetBrains <c>IgnoreFileManager</c>: in-memory <see cref="Dictionary{TKey, TValue}"/>
    /// kept in sync with disk and Registry, change events fired on every write or external reload.
    /// </summary>
    public static class IgnoreFileManager
    {
        private const string IgnoredFileName = ".checkmarxIgnored";
        private const string TempListFileName = ".checkmarxIgnoredTempList";
        private const string VsDirName = ".vs";

        private static readonly object _lock = new object();
        private static Dictionary<string, IgnoreEntry> _ignoreData = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);
        private static Dictionary<string, IgnoreEntry> _previousIgnoreData = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);
        private static string _solutionRoot;
        private static FileSystemWatcher _watcher;
        private static DateTime _lastSelfWriteUtc = DateTime.MinValue;
        private static System.Threading.Timer _debounceTimer;
        private const int DebounceMs = 1000;

        /// <summary>Raised after the in-memory ignore data changes (own write or external reload).</summary>
        public static event Action IgnoreDataChanged;

        /// <summary>
        /// Sets the solution root and loads the ignore file (creating an empty one if missing).
        /// Safe to call multiple times — re-binds the watcher when the root changes.
        /// </summary>
        public static void Initialize(string solutionRoot)
        {
            if (string.IsNullOrWhiteSpace(solutionRoot))
            {
                CxAssistOutputPane.WriteToOutputPane("IgnoreFileManager.Initialize: empty solutionRoot — Shutdown invoked. Ignore feature will not persist.");
                Shutdown();
                return;
            }

            lock (_lock)
            {
                if (string.Equals(_solutionRoot, solutionRoot, StringComparison.OrdinalIgnoreCase) && _watcher != null)
                    return;

                DisposeWatcherLocked();
                _solutionRoot = solutionRoot;
                _ignoreData = LoadFromDisk();
                _previousIgnoreData = new Dictionary<string, IgnoreEntry>(_ignoreData, StringComparer.Ordinal);
                StartWatcherLocked();
            }

            CxAssistOutputPane.WriteToOutputPane($"IgnoreFileManager.Initialize: root='{solutionRoot}', loaded {_ignoreData.Count} entries from disk.");
            RaiseChanged();
        }

        /// <summary>Releases the watcher and clears in-memory data. Called on solution close / logout.</summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                DisposeWatcherLocked();
                _solutionRoot = null;
                _ignoreData = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);
            }
            RaiseChanged();
        }

        /// <summary>Whether <see cref="Initialize"/> has been called with a valid solution root.</summary>
        public static bool IsInitialized
        {
            get { lock (_lock) { return !string.IsNullOrEmpty(_solutionRoot); } }
        }

        /// <summary>Absolute path to the solution root (null when not initialized).</summary>
        public static string SolutionRoot
        {
            get { lock (_lock) { return _solutionRoot; } }
        }

        /// <summary>
        /// Returns a snapshot of all stored entries (file + registry layers).
        /// Merges entries from:
        /// 1. File system (.vs/.checkmarxIgnored)
        /// 2. Windows Registry (user-level ignores)
        /// Registry entries take precedence over file-level for same key.
        /// </summary>
        public static IReadOnlyDictionary<string, IgnoreEntry> GetAllEntries()
        {
            lock (_lock)
            {
                var merged = new Dictionary<string, IgnoreEntry>(_ignoreData, StringComparer.Ordinal);
                var userLevelIgnores = IgnoreRegistryManager.GetUserLevelIgnores();
                foreach (var kv in userLevelIgnores)
                {
                    merged[kv.Key] = kv.Value;
                }
                return merged;
            }
        }

        /// <summary>
        /// Returns all entries in stable order (file + registry layers).
        /// Registry entries take precedence over file-level for same key.
        /// </summary>
        public static List<IgnoreEntry> GetAllEntryList()
        {
            lock (_lock)
            {
                return GetAllEntries().Values.ToList();
            }
        }

        /// <summary>
        /// Adds or replaces the entry under <paramref name="key"/> and writes the file.
        /// If an entry under the same key already exists, file references are merged so a second
        /// "ignore" of the same package in a different file extends the existing entry (JetBrains parity).
        /// </summary>
        public static void UpsertEntry(string key, IgnoreEntry entry)
        {
            if (string.IsNullOrEmpty(key) || entry == null)
            {
                CxAssistOutputPane.WriteToOutputPane($"IgnoreFileManager.UpsertEntry: skipped (key empty? {string.IsNullOrEmpty(key)}, entry null? {entry == null})");
                return;
            }

            int countAfter;
            lock (_lock)
            {
                if (_ignoreData.TryGetValue(key, out var existing))
                {
                    MergeFileReferencesInto(existing, entry);
                    foreach (var f in existing.Files) f.Active = true;
                    _ignoreData[key] = existing;
                }
                else
                {
                    _ignoreData[key] = entry;
                }
                SaveToDiskLocked();
                countAfter = _ignoreData.Count;
            }
            CxAssistOutputPane.WriteToOutputPane($"IgnoreFileManager.UpsertEntry: key='{key}', solutionRoot='{_solutionRoot ?? "<null>"}', totalEntries={countAfter}.");
            RaiseChanged();
        }

        /// <summary>
        /// Revives an entry: sets all file references to inactive and removes the entry from the file
        /// (JetBrains parity — entries with no active files are deleted). Returns the revived entry
        /// snapshot so the caller can offer an Undo action.
        /// </summary>
        public static IgnoreEntry ReviveEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            IgnoreEntry revived = null;
            lock (_lock)
            {
                if (!_ignoreData.TryGetValue(key, out var entry) || entry == null) return null;
                if (!entry.Files.Any(f => f.Active)) return null;
                // Snapshot the entry before removing (needed for undo)
                revived = DeepCloneEntry(entry);
                _ignoreData.Remove(key);
                SaveToDiskLocked();
            }
            // Also remove from Registry layer if it exists there
            IgnoreRegistryManager.RemoveUserIgnore(key);
            RaiseChanged();
            return revived;
        }

        /// <summary>
        /// Re-inserts a previously revived entry under <paramref name="key"/> (Undo support).
        /// All file references are set to active.
        /// </summary>
        public static bool RestoreEntry(string key, IgnoreEntry snapshot)
        {
            if (string.IsNullOrEmpty(key) || snapshot == null) return false;
            lock (_lock)
            {
                foreach (var f in snapshot.Files) f.Active = true;
                _ignoreData[key] = snapshot;
                SaveToDiskLocked();
            }
            RaiseChanged();
            return true;
        }

        private static IgnoreEntry DeepCloneEntry(IgnoreEntry src)
        {
            var clone = new IgnoreEntry
            {
                Type = src.Type, Title = src.Title, Severity = src.Severity,
                Description = src.Description, DateAdded = src.DateAdded,
                SimilarityId = src.SimilarityId, PackageManager = src.PackageManager,
                PackageName = src.PackageName, PackageVersion = src.PackageVersion,
                RuleId = src.RuleId, ImageName = src.ImageName, ImageTag = src.ImageTag,
                SecretValue = src.SecretValue,
                Files = new List<IgnoreEntry.FileReference>()
            };
            foreach (var f in src.Files)
                clone.Files.Add(new IgnoreEntry.FileReference { Path = f.Path, Active = f.Active, Line = f.Line, ProblematicLine = f.ProblematicLine });
            return clone;
        }

        /// <summary>Deletes both the ignore file, temp list, and user-level Registry entries. Used on logout.</summary>
        public static void DeleteAll()
        {
            lock (_lock)
            {
                _ignoreData.Clear();
                TryDeleteFile(GetIgnoreFilePath());
                TryDeleteFile(GetTempListFilePath());
            }
            IgnoreRegistryManager.ClearAllUserIgnores();
            RaiseChanged();
        }

        /// <summary>Adds or updates a user-level ignore entry in Windows Registry (Priority 3).</summary>
        public static void UpsertUserIgnore(string key, IgnoreEntry entry)
        {
            IgnoreRegistryManager.UpsertUserIgnore(key, entry);
            RaiseChanged();
        }

        /// <summary>Removes a user-level ignore entry from Windows Registry (Priority 3).</summary>
        public static bool RemoveUserIgnore(string key)
        {
            bool removed = IgnoreRegistryManager.RemoveUserIgnore(key);
            if (removed)
                RaiseChanged();
            return removed;
        }

        /// <summary>Returns whether any user-level ignores exist in Windows Registry.</summary>
        public static bool HasUserIgnores()
        {
            return IgnoreRegistryManager.HasUserIgnores();
        }

        /// <summary>
        /// For ASCA entries: deactivates file references for <paramref name="relativePath"/> whose
        /// ProblematicLine is NOT in <paramref name="stillPresentLines"/>, then removes entries
        /// that have no remaining active references. Saves to disk and raises IgnoreDataChanged.
        /// (JetBrains <c>removeIgnoreEntriesForFileIfEmpty</c> parity.)
        /// </summary>
        public static bool PruneStaleFileReferences(string relativePath, HashSet<string> stillPresentLines)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            bool anyChanged = false;
            lock (_lock)
            {
                var keysToRemove = new List<string>();
                foreach (var kv in _ignoreData)
                {
                    var entry = kv.Value;
                    if (entry?.Type != ScannerType.ASCA || entry.Files == null) continue;
                    foreach (var fileRef in entry.Files)
                    {
                        if (!fileRef.Active) continue;
                        if (!string.Equals(fileRef.Path, relativePath, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.IsNullOrEmpty(fileRef.ProblematicLine) &&
                            !(stillPresentLines != null && stillPresentLines.Contains(fileRef.ProblematicLine)))
                        {
                            fileRef.Active = false;
                            anyChanged = true;
                        }
                    }
                    if (entry.Files.All(f => !f.Active))
                        keysToRemove.Add(kv.Key);
                }
                foreach (var key in keysToRemove)
                    _ignoreData.Remove(key);
                if (anyChanged)
                    SaveToDiskLocked();
            }
            if (anyChanged)
                RaiseChanged();
            return anyChanged;
        }

        /// <summary>
        /// Removes the entry under <paramref name="key"/> from the in-memory store and fires IgnoreDataChanged.
        /// Used by line-number tracking to drop stale entries that no longer appear in scan results.
        /// Does not snapshot for undo — use <see cref="ReviveEntry"/> when undo support is needed.
        /// </summary>
        public static void RemoveEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            bool removed;
            lock (_lock)
            {
                removed = _ignoreData.Remove(key);
            }
            if (removed) RaiseChanged();
        }

        /// <summary>Saves current in-memory data to disk without raising IgnoreDataChanged (used for silent line-number updates).</summary>
        public static void ForceSaveToDisk()
        {
            lock (_lock) { SaveToDiskLocked(); }
        }

        /// <summary>Normalizes <paramref name="filePath"/> to a forward-slash, solution-relative path.</summary>
        public static string NormalizePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return filePath;
            string root;
            lock (_lock) { root = _solutionRoot; }
            try
            {
                string full = Path.GetFullPath(filePath);
                if (!string.IsNullOrEmpty(root) && full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    string rel = full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return rel.Replace('\\', '/');
                }
                return full.Replace('\\', '/');
            }
            catch
            {
                return filePath.Replace('\\', '/');
            }
        }

        /// <summary>
        /// Resolves the ignore file path using layered resolution:
        /// 1. Solution folder (.vs/.checkmarxIgnored)
        /// 2. Repository root (.vs/.checkmarxIgnored) for workspace-based development
        /// Returns the closest existing file, or solution-level path if none exist (for writes).
        /// </summary>
        internal static string GetIgnoreFilePath()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_solutionRoot)) return null;

                // Priority 1: Solution folder
                string solutionLevelPath = Path.Combine(_solutionRoot, VsDirName, IgnoredFileName);
                if (File.Exists(solutionLevelPath))
                    return solutionLevelPath;

                // Priority 2: Repository root (walk up to find .git)
                string repoRoot = FindRepositoryRoot(_solutionRoot);
                if (!string.IsNullOrEmpty(repoRoot) && !string.Equals(repoRoot, _solutionRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string repoLevelPath = Path.Combine(repoRoot, VsDirName, IgnoredFileName);
                    if (File.Exists(repoLevelPath))
                        return repoLevelPath;
                }

                // Default: Use solution-level path (for writes)
                return solutionLevelPath;
            }
        }

        internal static string GetTempListFilePath()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_solutionRoot)) return null;
                // Temp list always goes to solution folder (not shared, per-scan)
                return Path.Combine(_solutionRoot, VsDirName, TempListFileName);
            }
        }

        /// <summary>Walks up from <paramref name="startPath"/> to find repository root (contains .git).</summary>
        private static string FindRepositoryRoot(string startPath)
        {
            if (string.IsNullOrEmpty(startPath)) return null;
            try
            {
                var dir = new DirectoryInfo(startPath);
                while (dir != null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch { /* ignore errors */ }
            return null;
        }

        private static Dictionary<string, IgnoreEntry> LoadFromDisk()
        {
            var result = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);
            string path = GetIgnoreFilePath();
            if (string.IsNullOrEmpty(path)) return result;

            try
            {
                if (!File.Exists(path))
                {
                    EnsureVsDirectory();
                    File.WriteAllText(path, "{}" + Environment.NewLine, Encoding.UTF8);
                    return result;
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return result;

                var loaded = JsonConvert.DeserializeObject<Dictionary<string, IgnoreEntry>>(json);
                if (loaded != null)
                {
                    foreach (var kv in loaded)
                    {
                        if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                        if (kv.Value.Files == null) kv.Value.Files = new List<IgnoreEntry.FileReference>();
                        result[kv.Key] = kv.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.LoadFromDisk");
            }
            return result;
        }

        private static void SaveToDiskLocked()
        {
            string path = GetIgnoreFilePath();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                EnsureVsDirectory();
                string json = JsonConvert.SerializeObject(_ignoreData, Formatting.Indented);
                _lastSelfWriteUtc = DateTime.UtcNow;
                File.WriteAllText(path, json, Encoding.UTF8);
                SaveTempListLocked();
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.SaveToDisk");
            }
        }

        /// <summary>
        /// Writes the simplified temp list used by CLI scanners to <c>.vscode/.checkmarxIgnoredTempList</c>.
        /// Format mirrors JetBrains <c>updateIgnoreTempList()</c>: a JSON array of <see cref="TempItem"/> objects,
        /// one per active file reference for per-file scanners (ASCA, IaC, Secrets) and one per entry for
        /// package-level scanners (OSS, Containers).
        /// </summary>
        private static void SaveTempListLocked()
        {
            string path = GetTempListFilePath();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var items = new List<TempItem>();
                foreach (var entry in _ignoreData.Values)
                {
                    if (entry == null) continue;
                    var activeFiles = entry.Files?.Where(f => f.Active).ToList()
                        ?? new List<IgnoreEntry.FileReference>();

                    switch (entry.Type)
                    {
                        case ScannerType.OSS:
                            if (!string.IsNullOrEmpty(entry.PackageName))
                                items.Add(new TempItem
                                {
                                    PackageManager = entry.PackageManager,
                                    PackageName = entry.PackageName,
                                    PackageVersion = entry.PackageVersion
                                });
                            break;

                        case ScannerType.Containers:
                            if (!string.IsNullOrEmpty(entry.ImageName))
                                items.Add(new TempItem
                                {
                                    ImageName = entry.ImageName,
                                    ImageTag = entry.ImageTag
                                });
                            break;

                        case ScannerType.Secrets:
                            foreach (var f in activeFiles)
                                items.Add(new TempItem
                                {
                                    Title = entry.Title,
                                    SecretValue = entry.SecretValue
                                });
                            break;

                        case ScannerType.IaC:
                            foreach (var f in activeFiles)
                                items.Add(new TempItem
                                {
                                    Title = entry.Title,
                                    SimilarityID = entry.SimilarityId
                                });
                            break;

                        case ScannerType.ASCA:
                            foreach (var f in activeFiles)
                                items.Add(new TempItem
                                {
                                    FileName = f.Path,
                                    Line = f.Line,
                                    RuleID = entry.RuleId
                                });
                            break;
                    }
                }
                File.WriteAllText(path, JsonConvert.SerializeObject(items, Formatting.Indented), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.SaveTempList");
            }
        }

        /// <summary>
        /// Simplified DTO written to the temp list consumed by CLI scanners.
        /// Mirrors JetBrains <c>TempItem</c>: only the fields relevant to each scanner type are populated.
        /// </summary>
        private sealed class TempItem
        {
            [JsonProperty("Title", NullValueHandling = NullValueHandling.Ignore)]
            public string Title { get; set; }

            [JsonProperty("SecretValue", NullValueHandling = NullValueHandling.Ignore)]
            public string SecretValue { get; set; }

            [JsonProperty("SimilarityID", NullValueHandling = NullValueHandling.Ignore)]
            public string SimilarityID { get; set; }

            [JsonProperty("FileName", NullValueHandling = NullValueHandling.Ignore)]
            public string FileName { get; set; }

            [JsonProperty("Line", NullValueHandling = NullValueHandling.Ignore)]
            public int? Line { get; set; }

            [JsonProperty("RuleID", NullValueHandling = NullValueHandling.Ignore)]
            public int? RuleID { get; set; }

            [JsonProperty("PackageManager", NullValueHandling = NullValueHandling.Ignore)]
            public string PackageManager { get; set; }

            [JsonProperty("PackageName", NullValueHandling = NullValueHandling.Ignore)]
            public string PackageName { get; set; }

            [JsonProperty("PackageVersion", NullValueHandling = NullValueHandling.Ignore)]
            public string PackageVersion { get; set; }

            [JsonProperty("ImageName", NullValueHandling = NullValueHandling.Ignore)]
            public string ImageName { get; set; }

            [JsonProperty("ImageTag", NullValueHandling = NullValueHandling.Ignore)]
            public string ImageTag { get; set; }
        }

        private static void EnsureVsDirectory()
        {
            string root;
            lock (_lock) { root = _solutionRoot; }
            if (string.IsNullOrEmpty(root)) return;
            string dir = Path.Combine(root, VsDirName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void StartWatcherLocked()
        {
            try
            {
                if (string.IsNullOrEmpty(_solutionRoot)) return;
                string dir = Path.Combine(_solutionRoot, VsDirName);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                _watcher = new FileSystemWatcher(dir, IgnoredFileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                _watcher.Changed += OnFileChangedExternally;
                _watcher.Created += OnFileChangedExternally;
                _watcher.Renamed += OnFileChangedExternally;
                _watcher.Deleted += OnFileDeletedExternally;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.StartWatcher");
            }
        }

        private static void OnFileChangedExternally(object sender, FileSystemEventArgs e)
        {
            // Ignore our own writes (FileSystemWatcher fires Changed for our SaveToDisk too).
            if ((DateTime.UtcNow - _lastSelfWriteUtc).TotalMilliseconds < 500)
                return;

            // Debounce: reset the timer on every rapid-fire event, fire once after quiet period.
            lock (_lock)
            {
                _debounceTimer?.Change(DebounceMs, System.Threading.Timeout.Infinite);
                if (_debounceTimer == null)
                    _debounceTimer = new System.Threading.Timer(_ => ProcessExternalFileChange(), null, DebounceMs, System.Threading.Timeout.Infinite);
            }
        }

        private static void ProcessExternalFileChange()
        {
            Dictionary<string, IgnoreEntry> previous;
            Dictionary<string, IgnoreEntry> current;
            lock (_lock)
            {
                previous = new Dictionary<string, IgnoreEntry>(_previousIgnoreData, StringComparer.Ordinal);
                _ignoreData = LoadFromDisk();
                current = _ignoreData;
            }
            DetectAndHandleActiveChanges(previous, current);
            lock (_lock) { _previousIgnoreData = new Dictionary<string, IgnoreEntry>(current, StringComparer.Ordinal); }
            RaiseChanged();
        }

        /// <summary>
        /// Mirrors JetBrains <c>detectAndHandleActiveChanges()</c>: diffs previous vs current in-memory
        /// entries after an external file reload. Removes entries that were active before but are now gone
        /// or fully inactive, then rebuilds the temp list. This handles the case where another IDE instance
        /// or the CLI revives an entry externally.
        /// </summary>
        private static void DetectAndHandleActiveChanges(Dictionary<string, IgnoreEntry> previous, Dictionary<string, IgnoreEntry> current)
        {
            if (previous == null || previous.Count == 0) return;

            var keysToRemove = new List<string>();
            foreach (var kv in previous)
            {
                string key = kv.Key;
                var wasActive = kv.Value?.Files?.Any(f => f.Active) == true;
                if (!wasActive) continue;

                bool stillActive = current.TryGetValue(key, out var nowEntry) &&
                                   nowEntry?.Files?.Any(f => f.Active) == true;

                if (!stillActive)
                    keysToRemove.Add(key);
            }

            if (keysToRemove.Count == 0) return;

            lock (_lock)
            {
                foreach (var key in keysToRemove)
                    _ignoreData.Remove(key);

                // Rebuild temp list after cleanup (mirrors JetBrains updateIgnoreTempList).
                SaveToDiskLocked();
            }

            CxAssistOutputPane.WriteToOutputPane(
                $"IgnoreFileManager: external change removed {keysToRemove.Count} deactivated entr{(keysToRemove.Count == 1 ? "y" : "ies")}.");
        }

        private static void OnFileDeletedExternally(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _ignoreData = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);
            }
            CxAssistOutputPane.WriteToOutputPane("IgnoreFileManager: .checkmarxIgnored deleted externally — ignore list cleared.");
            RaiseChanged();
        }

        private static void DisposeWatcherLocked()
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;

            if (_watcher == null) return;
            try
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFileChangedExternally;
                _watcher.Created -= OnFileChangedExternally;
                _watcher.Renamed -= OnFileChangedExternally;
                _watcher.Deleted -= OnFileDeletedExternally;
                _watcher.Dispose();
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.DisposeWatcher");
            }
            _watcher = null;
        }

        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.TryDeleteFile"); }
        }

        private static void MergeFileReferencesInto(IgnoreEntry existing, IgnoreEntry incoming)
        {
            if (existing.Files == null) existing.Files = new List<IgnoreEntry.FileReference>();
            if (incoming?.Files == null) return;

            foreach (var incomingRef in incoming.Files)
            {
                bool isDuplicate = existing.Files.Any(f =>
                    string.Equals(f.Path, incomingRef.Path, StringComparison.Ordinal) &&
                    f.Line == incomingRef.Line &&
                    string.Equals(f.ProblematicLine, incomingRef.ProblematicLine, StringComparison.Ordinal));
                if (!isDuplicate)
                    existing.Files.Add(incomingRef);
            }
        }

        private static void RaiseChanged()
        {
            try { IgnoreDataChanged?.Invoke(); }
            catch (Exception ex) { CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreFileManager.RaiseChanged"); }
        }
    }
}
