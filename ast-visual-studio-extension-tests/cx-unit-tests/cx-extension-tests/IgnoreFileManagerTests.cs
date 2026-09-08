using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for <see cref="IgnoreFileManager"/> — file lifecycle, normalize path, upsert merge,
    /// revive flag flip. Each test gets a fresh temp directory so the FileSystemWatcher binding doesn't leak.
    /// </summary>
    public class IgnoreFileManagerTests : IDisposable
    {
        private readonly string _root;

        public IgnoreFileManagerTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "cx-ignore-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            IgnoreFileManager.Shutdown();
            IgnoreFileManager.Initialize(_root);
        }

        public void Dispose()
        {
            IgnoreFileManager.Shutdown();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
        }

        [Fact]
        public void Initialize_CreatesEmptyIgnoreFile()
        {
            string ignored = Path.Combine(_root, ".vs", ".checkmarxIgnored");
            Assert.True(File.Exists(ignored), "Initialize should create the .checkmarxIgnored file");
            string content = File.ReadAllText(ignored).Trim();
            Assert.StartsWith("{", content);
        }

        [Fact]
        public void UpsertEntry_PersistsToDisk()
        {
            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash", PackageVersion = "1.0.0" };
            IgnoreFileManager.UpsertEntry("npm:lodash:1.0.0", entry);

            string json = File.ReadAllText(Path.Combine(_root, ".vs", ".checkmarxIgnored"));
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, IgnoreEntry>>(json);
            Assert.NotNull(loaded);
            Assert.True(loaded.ContainsKey("npm:lodash:1.0.0"));
            Assert.Equal("lodash", loaded["npm:lodash:1.0.0"].PackageName);
        }

        [Fact]
        public void UpsertEntry_SameKey_MergesNewFileReferences()
        {
            var first = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "package.json", Active = true, Line = 1 }
                }
            };
            var second = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "package-lock.json", Active = true, Line = 5 }
                }
            };

            IgnoreFileManager.UpsertEntry("k", first);
            IgnoreFileManager.UpsertEntry("k", second);

            var all = IgnoreFileManager.GetAllEntries();
            var merged = all["k"];
            Assert.Equal(2, merged.Files.Count);
            Assert.Contains(merged.Files, f => f.Path == "package.json");
            Assert.Contains(merged.Files, f => f.Path == "package-lock.json");
        }

        [Fact]
        public void ReviveEntry_SetsAllFileReferencesInactive()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "package.json", Active = true },
                    new IgnoreEntry.FileReference { Path = "package-lock.json", Active = true }
                }
            };
            IgnoreFileManager.UpsertEntry("k", entry);

            var snapshot = IgnoreFileManager.ReviveEntry("k");

            // ReviveEntry removes the entry and returns a snapshot for undo support
            Assert.NotNull(snapshot);
            Assert.Equal(2, snapshot.Files.Count);
            Assert.False(IgnoreFileManager.GetAllEntries().ContainsKey("k"));
        }

        [Fact]
        public void ReviveEntry_AlreadyRevived_ReturnsFalse()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "package.json", Active = false }
                }
            };
            IgnoreFileManager.UpsertEntry("k", entry);
            Assert.Null(IgnoreFileManager.ReviveEntry("k"));
        }

        [Fact]
        public void NormalizePath_ConvertsToForwardSlashRelative()
        {
            string absolute = Path.Combine(_root, "src", "queries.cs");
            string normalized = IgnoreFileManager.NormalizePath(absolute);
            Assert.Equal("src/queries.cs", normalized);
        }

        [Fact]
        public void NormalizePath_PathOutsideSolution_ReturnsFullPath()
        {
            // Use a path that is *not* under _root; we should still get forward slashes back.
            string absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "x.cs");
            string normalized = IgnoreFileManager.NormalizePath(absolute);
            Assert.DoesNotContain("\\", normalized);
        }

        [Fact]
        public void Shutdown_ClearsState_NoEntriesReturned()
        {
            IgnoreFileManager.UpsertEntry("k", new IgnoreEntry { Type = ScannerType.OSS });
            IgnoreFileManager.Shutdown();
            Assert.False(IgnoreFileManager.IsInitialized);
            Assert.Empty(IgnoreFileManager.GetAllEntries());
        }

        [Fact]
        public void DeleteAll_RemovesIgnoreFile()
        {
            IgnoreFileManager.UpsertEntry("k", new IgnoreEntry { Type = ScannerType.OSS });
            IgnoreFileManager.DeleteAll();
            string ignored = Path.Combine(_root, ".vs", ".checkmarxIgnored");
            Assert.False(File.Exists(ignored));
            Assert.Empty(IgnoreFileManager.GetAllEntries());
        }

        [Fact]
        public void IgnoreDataChanged_FiresOnUpsert()
        {
            int count = 0;
            Action handler = () => count++;
            IgnoreFileManager.IgnoreDataChanged += handler;
            try
            {
                IgnoreFileManager.UpsertEntry("k", new IgnoreEntry { Type = ScannerType.OSS });
                IgnoreFileManager.UpsertEntry("k2", new IgnoreEntry { Type = ScannerType.IaC });
            }
            finally
            {
                IgnoreFileManager.IgnoreDataChanged -= handler;
            }
            Assert.Equal(2, count);
        }

        [Fact]
        public void Initialize_LoadsExistingFile()
        {
            // Pre-populate the file before Initialize runs (Shutdown first to avoid the test's own watcher).
            IgnoreFileManager.Shutdown();
            string vsDir = Path.Combine(_root, ".vs");
            Directory.CreateDirectory(vsDir);
            var seed = new Dictionary<string, IgnoreEntry>
            {
                ["k"] = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash" }
            };
            File.WriteAllText(Path.Combine(vsDir, ".checkmarxIgnored"), JsonConvert.SerializeObject(seed));

            IgnoreFileManager.Initialize(_root);
            Assert.True(IgnoreFileManager.GetAllEntries().ContainsKey("k"));
            Assert.Equal("lodash", IgnoreFileManager.GetAllEntries()["k"].PackageName);
        }
    }
}
