using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore
{
    /// <summary>
    /// Manages user-specific ignore entries stored in Windows Registry (Priority 3 fallback).
    /// Entries are per-machine, not shared across solutions or repositories.
    /// Registry location: HKEY_CURRENT_USER\Software\Checkmarx\AST\IgnoredFindings
    /// </summary>
    public static class IgnoreRegistryManager
    {
        private const string RegistryHive = @"Software\Checkmarx\AST";
        private const string IgnoredFindingsSubkey = "IgnoredFindings";
        private const string IgnoreLevelValueName = "Level";
        private const string UserLevelValue = "User";
        private const int MaxRegistryValueSize = 16000; // Registry value size limit

        private static readonly object _lock = new object();

        /// <summary>Raised after registry ignore data changes.</summary>
        public static event Action UserLevelIgnoreDataChanged;

        /// <summary>Gets all user-level ignore entries from Registry.</summary>
        public static IReadOnlyDictionary<string, IgnoreEntry> GetUserLevelIgnores()
        {
            lock (_lock)
            {
                return LoadFromRegistry();
            }
        }

        /// <summary>Adds or updates a user-level ignore entry in Registry.</summary>
        public static void UpsertUserIgnore(string key, IgnoreEntry entry)
        {
            if (string.IsNullOrEmpty(key) || entry == null)
            {
                CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: IgnoreRegistryManager.UpsertUserIgnore: skipped (key empty? {string.IsNullOrEmpty(key)}, entry null? {entry == null})");
                return;
            }

            lock (_lock)
            {
                try
                {
                    // Merge file references if an entry already exists for this key (mirrors IgnoreFileManager.UpsertEntry)
                    var existing = LoadFromRegistry();
                    if (existing.TryGetValue(key, out var prior) && prior != null)
                    {
                        MergeFileReferencesInto(prior, entry);
                        foreach (var f in prior.Files) f.Active = true;
                        entry = prior;
                    }

                    string json = JsonConvert.SerializeObject(entry);
                    if (json.Length > MaxRegistryValueSize)
                    {
                        CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: IgnoreRegistryManager.UpsertUserIgnore: key '{key}' too large for Registry ({json.Length} > {MaxRegistryValueSize} bytes), skipping user-level storage.");
                        return;
                    }

                    using (var key_hkcu = Registry.CurrentUser.OpenSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}", writable: true) ??
                                         Registry.CurrentUser.CreateSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}"))
                    {
                        key_hkcu.SetValue(key, json, RegistryValueKind.String);
                        key_hkcu.SetValue(IgnoreLevelValueName, UserLevelValue, RegistryValueKind.String);
                    }

                    CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: IgnoreRegistryManager.UpsertUserIgnore: key='{key}' stored in user-level Registry.");
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.UpsertUserIgnore");
                }
            }

            RaiseChanged();
        }

        /// <summary>Removes a user-level ignore entry from Registry.</summary>
        public static bool RemoveUserIgnore(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            lock (_lock)
            {
                try
                {
                    using (var subkey = Registry.CurrentUser.OpenSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}", writable: true))
                    {
                        if (subkey != null && subkey.GetValue(key) != null)
                        {
                            subkey.DeleteValue(key, false);
                            CxAssistOutputPane.WriteToOutputPane($"RTS-Ignore: IgnoreRegistryManager.RemoveUserIgnore: key='{key}' removed from user-level Registry.");
                            RaiseChanged();
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.RemoveUserIgnore");
                }
            }

            return false;
        }

        /// <summary>Clears all user-level ignore entries from Registry.</summary>
        public static void ClearAllUserIgnores()
        {
            lock (_lock)
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}", false);
                    CxAssistOutputPane.WriteToOutputPane("RTS-Ignore: IgnoreRegistryManager.ClearAllUserIgnores: all user-level ignores cleared from Registry.");
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.ClearAllUserIgnores");
                }
            }

            RaiseChanged();
        }

        /// <summary>Checks if any user-level ignores exist in Registry.</summary>
        public static bool HasUserIgnores()
        {
            lock (_lock)
            {
                try
                {
                    using (var subkey = Registry.CurrentUser.OpenSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}"))
                    {
                        if (subkey != null)
                        {
                            var values = subkey.GetValueNames();
                            // Exclude the Level marker value
                            return values.Any(v => !v.Equals(IgnoreLevelValueName, StringComparison.Ordinal));
                        }
                    }
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.HasUserIgnores");
                }
            }

            return false;
        }

        private static Dictionary<string, IgnoreEntry> LoadFromRegistry()
        {
            var result = new Dictionary<string, IgnoreEntry>(StringComparer.Ordinal);

            try
            {
                using (var subkey = Registry.CurrentUser.OpenSubKey($"{RegistryHive}\\{IgnoredFindingsSubkey}"))
                {
                    if (subkey == null) return result;

                    foreach (var valueName in subkey.GetValueNames())
                    {
                        // Skip metadata values
                        if (valueName.Equals(IgnoreLevelValueName, StringComparison.Ordinal))
                            continue;

                        try
                        {
                            var json = subkey.GetValue(valueName) as string;
                            if (string.IsNullOrWhiteSpace(json)) continue;

                            var entry = JsonConvert.DeserializeObject<IgnoreEntry>(json);
                            if (entry != null && entry.Files == null)
                                entry.Files = new List<IgnoreEntry.FileReference>();

                            if (entry != null)
                                result[valueName] = entry;
                        }
                        catch (Exception ex)
                        {
                            CxAssistErrorHandler.LogAndSwallow(ex, $"IgnoreRegistryManager.LoadFromRegistry: failed to deserialize '{valueName}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.LoadFromRegistry");
            }

            return result;
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
            try { UserLevelIgnoreDataChanged?.Invoke(); }
            catch (Exception ex) { CxAssistErrorHandler.LogAndSwallow(ex, "IgnoreRegistryManager.RaiseChanged"); }
        }
    }
}
