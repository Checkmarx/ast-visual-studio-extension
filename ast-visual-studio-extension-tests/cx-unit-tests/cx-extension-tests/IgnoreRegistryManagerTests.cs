using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for <see cref="IgnoreRegistryManager"/> — Registry-based ignore persistence
    /// for user-level (Priority 3) ignore entries. Tests Registry I/O and event firing.
    /// </summary>
    public class IgnoreRegistryManagerTests : IDisposable
    {
        public IgnoreRegistryManagerTests()
        {
            IgnoreRegistryManager.ClearAllUserIgnores();
        }

        public void Dispose()
        {
            IgnoreRegistryManager.ClearAllUserIgnores();
        }

        [Fact]
        public void UpsertUserIgnore_StoresInRegistry()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0",
                Title = "Test Package"
            };

            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash:1.0.0", entry);

            var stored = IgnoreRegistryManager.GetUserLevelIgnores();
            Assert.NotEmpty(stored);
            Assert.True(stored.ContainsKey("npm:lodash:1.0.0"));
            Assert.Equal("lodash", stored["npm:lodash:1.0.0"].PackageName);
        }

        [Fact]
        public void GetUserLevelIgnores_ReturnsEmptyWhenNone()
        {
            var ignores = IgnoreRegistryManager.GetUserLevelIgnores();
            Assert.NotNull(ignores);
            Assert.Empty(ignores);
        }

        [Fact]
        public void RemoveUserIgnore_DeletesEntry()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0"
            };

            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash:1.0.0", entry);
            Assert.True(IgnoreRegistryManager.HasUserIgnores());

            bool removed = IgnoreRegistryManager.RemoveUserIgnore("npm:lodash:1.0.0");
            Assert.True(removed);
            Assert.False(IgnoreRegistryManager.HasUserIgnores());
        }

        [Fact]
        public void RemoveUserIgnore_ReturnsFalseForNonExistent()
        {
            bool removed = IgnoreRegistryManager.RemoveUserIgnore("non-existent-key");
            Assert.False(removed);
        }

        [Fact]
        public void HasUserIgnores_ReturnsTrueWhenEntries()
        {
            Assert.False(IgnoreRegistryManager.HasUserIgnores());

            var entry = new IgnoreEntry
            {
                Type = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "1.0.0"
            };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash:1.0.0", entry);

            Assert.True(IgnoreRegistryManager.HasUserIgnores());
        }

        [Fact]
        public void HasUserIgnores_ReturnsFalseAfterClear()
        {
            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash" };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash", entry);
            Assert.True(IgnoreRegistryManager.HasUserIgnores());

            IgnoreRegistryManager.ClearAllUserIgnores();
            Assert.False(IgnoreRegistryManager.HasUserIgnores());
        }

        [Fact]
        public void UpsertUserIgnore_MergesFileReferences()
        {
            var entry1 = new IgnoreEntry
            {
                Type = ScannerType.ASCA,
                RuleId = 123,
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "file1.cs", Line = 10, Active = true }
                }
            };

            var entry2 = new IgnoreEntry
            {
                Type = ScannerType.ASCA,
                RuleId = 123,
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference { Path = "file2.cs", Line = 20, Active = true }
                }
            };

            IgnoreRegistryManager.UpsertUserIgnore("rule:123", entry1);
            IgnoreRegistryManager.UpsertUserIgnore("rule:123", entry2);

            var stored = IgnoreRegistryManager.GetUserLevelIgnores();
            Assert.Single(stored);
            var rule = stored["rule:123"];
            Assert.NotNull(rule.Files);
            Assert.Equal(2, rule.Files.Count);
        }

        [Fact]
        public void UserLevelIgnoreDataChanged_RaisedOnUpsert()
        {
            int eventCount = 0;
            IgnoreRegistryManager.UserLevelIgnoreDataChanged += () => eventCount++;

            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash" };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash", entry);

            Assert.True(eventCount > 0, "UserLevelIgnoreDataChanged event should have been raised");
        }

        [Fact]
        public void UserLevelIgnoreDataChanged_RaisedOnRemove()
        {
            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash" };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash", entry);

            int eventCount = 0;
            IgnoreRegistryManager.UserLevelIgnoreDataChanged += () => eventCount++;

            IgnoreRegistryManager.RemoveUserIgnore("npm:lodash");

            Assert.True(eventCount > 0, "UserLevelIgnoreDataChanged event should have been raised on remove");
        }

        [Fact]
        public void UpsertUserIgnore_HandlesComplexEntry()
        {
            var entry = new IgnoreEntry
            {
                Type = ScannerType.IaC,
                Title = "Complex Security Issue",
                Description = "Multi-line description",
                Severity = "High",
                DateAdded = DateTime.UtcNow.ToString("O"),
                SimilarityId = "abc-123",
                Files = new List<IgnoreEntry.FileReference>
                {
                    new IgnoreEntry.FileReference
                    {
                        Path = "terraform/main.tf",
                        Line = 42,
                        ProblematicLine = "resource \"aws_s3_bucket\" \"insecure\" {",
                        Active = true
                    }
                }
            };

            IgnoreRegistryManager.UpsertUserIgnore("iac:S3UnencryptedBucket", entry);

            var stored = IgnoreRegistryManager.GetUserLevelIgnores();
            Assert.Single(stored);
            var iacEntry = stored["iac:S3UnencryptedBucket"];
            Assert.Equal("Complex Security Issue", iacEntry.Title);
            Assert.Single(iacEntry.Files);
            Assert.Equal("terraform/main.tf", iacEntry.Files[0].Path);
        }

        [Fact]
        public void GetUserLevelIgnores_FilesInitializedIfNull()
        {
            var entry = new IgnoreEntry { Type = ScannerType.OSS, PackageName = "lodash" };
            IgnoreRegistryManager.UpsertUserIgnore("npm:lodash", entry);

            var stored = IgnoreRegistryManager.GetUserLevelIgnores();
            var lodashEntry = stored["npm:lodash"];
            Assert.NotNull(lodashEntry.Files);
        }
    }
}
