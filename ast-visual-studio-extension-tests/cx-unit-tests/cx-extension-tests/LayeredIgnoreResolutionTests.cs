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
    /// Integration tests for layered ignore resolution combining all three priority levels:
    /// 1. File system (.vs/.checkmarxIgnored)
    /// 2. Repository root (.vs/.checkmarxIgnored)
    /// 3. Windows Registry (user-level)
    /// </summary>
    public class LayeredIgnoreResolutionTests : IDisposable
    {
        private readonly string _root;

        public LayeredIgnoreResolutionTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "cx-layered-ignore-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            IgnoreFileManager.Shutdown();
            IgnoreRegistryManager.ClearAllUserIgnores();
            IgnoreFileManager.Initialize(_root);
        }

        public void Dispose()
        {
            IgnoreFileManager.Shutdown();
            IgnoreRegistryManager.ClearAllUserIgnores();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
        }

        [Fact]
        public void GetAllEntries_IncludesFileAndRegistryLayers()
        {
            // Add file-level entry
            var fileEntry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0"
            };
            IgnoreFileManager.UpsertEntry("npm:lodash:1.0.0", fileEntry);

            // Add user-level entry
            var userEntry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "express",
                PackageVersion = "4.0.0"
            };
            IgnoreRegistryManager.UpsertUserIgnore("npm:express:4.0.0", userEntry);

            var allEntries = IgnoreFileManager.GetAllEntries();

            Assert.Equal(2, allEntries.Count);
            Assert.True(allEntries.ContainsKey("npm:lodash:1.0.0"));
            Assert.True(allEntries.ContainsKey("npm:express:4.0.0"));
        }

        [Fact]
        public void GetAllEntries_RegistryTakesPrecedenceForSameKey()
        {
            // Add file-level entry
            var fileEntry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0",
                Description = "File level"
            };
            IgnoreFileManager.UpsertEntry("npm:lodash", fileEntry);

            // Override with user-level entry
            var userEntry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0",
                Description = "User level (takes precedence)"
            };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash", userEntry);

            var allEntries = IgnoreFileManager.GetAllEntries();

            Assert.Single(allEntries);
            var lodashEntry = allEntries["npm:lodash"];
            Assert.Equal("User level (takes precedence)", lodashEntry.Description);
        }

        [Fact]
        public void GetAllEntryList_ReturnsAllLayersInOrder()
        {
            // Add 2 file-level entries
            IgnoreFileManager.UpsertEntry("file:1", new IgnoreEntry { Type = ScannerType.ASCA, RuleId = 1 });
            IgnoreFileManager.UpsertEntry("file:2", new IgnoreEntry { Type = ScannerType.ASCA, RuleId = 2 });

            // Add 2 user-level entries
            IgnoreRegistryManager.UpsertUserIgnore("user:1", new IgnoreEntry { Type = ScannerType.Secrets, Title = "User1" });
            IgnoreRegistryManager.UpsertUserIgnore("user:2", new IgnoreEntry { Type = ScannerType.Secrets, Title = "User2" });

            var allList = IgnoreFileManager.GetAllEntryList();

            Assert.Equal(4, allList.Count);
        }

        [Fact]
        public void DeleteAll_ClearsFileAndRegistryLayers()
        {
            // Add entries at both levels
            IgnoreFileManager.UpsertEntry("file:1", new IgnoreEntry { Type = ScannerType.ASCA });
            IgnoreRegistryManager.UpsertUserIgnore("user:1", new IgnoreEntry { Type = ScannerType.Secrets });

            Assert.True(IgnoreFileManager.IsInitialized);
            Assert.True(IgnoreRegistryManager.HasUserIgnores());

            IgnoreFileManager.DeleteAll();

            // Should clear both layers
            var allEntries = IgnoreFileManager.GetAllEntries();
            Assert.Empty(allEntries);
            Assert.False(IgnoreRegistryManager.HasUserIgnores());
        }

        [Fact]
        public void UpsertUserIgnore_ThroughFileManagerDelegates()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "webpack",
                PackageVersion = "5.0.0"
            };

            IgnoreFileManager.UpsertUserIgnore("npm:webpack:5.0.0", entry);

            var allEntries = IgnoreFileManager.GetAllEntries();
            Assert.True(allEntries.ContainsKey("npm:webpack:5.0.0"));
            Assert.True(IgnoreRegistryManager.HasUserIgnores());
        }

        [Fact]
        public void RemoveUserIgnore_ThroughFileManagerDelegates()
        {
            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "webpack" };
            IgnoreFileManager.UpsertUserIgnore("npm:webpack", entry);
            Assert.True(IgnoreFileManager.HasUserIgnores());

            bool removed = IgnoreFileManager.RemoveUserIgnore("npm:webpack");
            Assert.True(removed);
            Assert.False(IgnoreFileManager.HasUserIgnores());
        }

        [Fact]
        public void LayeredResolution_ComplexScenario()
        {
            // Scenario: Mono-repo with shared and per-project ignores

            // File-level: Project-specific ignores
            IgnoreFileManager.UpsertEntry("sast:ProjectSpecific", new IgnoreEntry
            {
                Type = ScannerType.ASCA,
                RuleId = 1001,
                Description = "Project-specific SAST rule"
            });

            // User-level: Personal preferences (e.g., secrets not to be scanned)
            IgnoreRegistryManager.UpsertUserIgnore("secret:APIKey", new IgnoreEntry
            {
                Type = ScannerType.Secrets,
                Title = "API Key Pattern",
                SecretValue = "test-*-key"
            });

            // User-level: Common library ignore (no time to fix now)
            IgnoreRegistryManager.UpsertUserIgnore("npm:vulnerable-lib", new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "vulnerable-lib",
                PackageVersion = "1.0.0"
            });

            var allEntries = IgnoreFileManager.GetAllEntries();

            Assert.Equal(3, allEntries.Count);
            Assert.NotNull(allEntries["sast:ProjectSpecific"]);
            Assert.NotNull(allEntries["secret:APIKey"]);
            Assert.NotNull(allEntries["npm:vulnerable-lib"]);
        }

        [Fact]
        public void HasUserIgnores_ReturnsCorrectState()
        {
            Assert.False(IgnoreFileManager.HasUserIgnores());

            IgnoreFileManager.UpsertUserIgnore("npm:test", new IgnoreEntry { Type = ScannerType.OSS });

            Assert.True(IgnoreFileManager.HasUserIgnores());

            IgnoreFileManager.RemoveUserIgnore("npm:test");

            Assert.False(IgnoreFileManager.HasUserIgnores());
        }

        [Fact]
        public void LayeredResolution_IgnoreDataChangedEventFiredForBothLayers()
        {
            int fileChangeCount = 0;
            int registryChangeCount = 0;

            IgnoreFileManager.IgnoreDataChanged += () => fileChangeCount++;
            IgnoreRegistryManager.UserLevelIgnoreDataChanged += () => registryChangeCount++;

            // File-level change
            IgnoreFileManager.UpsertEntry("file:1", new IgnoreEntry { Type = ScannerType.ASCA });
            Assert.True(fileChangeCount > 0, "File-level change should fire event");

            // Registry-level change (through FileManager delegator)
            IgnoreFileManager.UpsertUserIgnore("user:1", new IgnoreEntry { Type = ScannerType.Secrets });
            Assert.True(registryChangeCount > 0, "Registry change should fire event");
        }
    }
}
