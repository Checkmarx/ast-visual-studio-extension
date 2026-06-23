using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for <see cref="IgnoreManager"/> — per-scanner add + isIgnored matching, addAll expansion,
    /// revive flow. Each test owns a temp solution root.
    /// </summary>
    public class IgnoreManagerTests : IDisposable
    {
        private readonly string _root;

        public IgnoreManagerTests()
        {
            VsThreadingTestHelper.EnsureInitialized();
            _root = Path.Combine(Path.GetTempPath(), "cx-ignmgr-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            IgnoreFileManager.Shutdown();
            IgnoreFileManager.Initialize(_root);
        }

        public void Dispose()
        {
            IgnoreFileManager.Shutdown();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
        }

        private Vulnerability OssVuln(string name, string version, string file)
        {
            return new Vulnerability
            {
                Id = "id-" + name,
                Title = name + "@" + version,
                Severity = SeverityLevel.High,
                Scanner = ScannerType.OSS,
                PackageManager = "npm",
                PackageName = name,
                PackageVersion = version,
                FilePath = Path.Combine(_root, file),
                LineNumber = 5
            };
        }

        [Fact]
        public void AddIgnoredEntry_Oss_PersistsEntryAndMatchesByPackageTriple()
        {
            var vuln = OssVuln("lodash", "4.17.21", "package.json");
            IgnoreManager.AddIgnoredEntry(vuln);

            Assert.True(IgnoreManager.IsVulnerabilityIgnored(vuln));

            // Different version of same package should NOT match.
            var other = OssVuln("lodash", "5.0.0", "package.json");
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(other));

            // Different package should NOT match.
            var other2 = OssVuln("express", "4.17.21", "package.json");
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(other2));
        }

        [Fact]
        public void IgnoreNotInitialized_ReturnsFalse()
        {
            IgnoreFileManager.Shutdown();
            var vuln = OssVuln("lodash", "1.0.0", "package.json");
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(vuln));
        }

        [Fact]
        public void AddIgnoredEntry_Iac_MatchesByTitleAndSimilarityId()
        {
            var file = Path.Combine(_root, "infra", "s3.tf");
            var vuln = new Vulnerability
            {
                Id = "sim-123",
                Title = "S3-Public",
                Scanner = ScannerType.IaC,
                Severity = SeverityLevel.High,
                FilePath = file,
                LineNumber = 10
            };
            IgnoreManager.AddIgnoredEntry(vuln);
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(vuln));

            // Different similarity id on same file: not ignored.
            var other = new Vulnerability
            {
                Id = "sim-OTHER",
                Title = "S3-Public",
                Scanner = ScannerType.IaC,
                Severity = SeverityLevel.High,
                FilePath = file,
                LineNumber = 10
            };
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(other));
        }

        [Fact]
        public void AddIgnoredEntry_Containers_MatchesByImageAndTag_FileAgnostic()
        {
            var v1 = new Vulnerability
            {
                Id = "cve-1",
                Title = "nginx:latest",
                Scanner = ScannerType.Containers,
                Severity = SeverityLevel.High,
                PackageName = "nginx",
                PackageVersion = "latest",
                FilePath = Path.Combine(_root, "Dockerfile"),
                LineNumber = 1
            };
            IgnoreManager.AddIgnoredEntry(v1);
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(v1));

            // Same image:tag appearing in a different file is still ignored — Containers ignore is global.
            var v2 = new Vulnerability
            {
                Id = "cve-2",
                Title = "nginx:latest",
                Scanner = ScannerType.Containers,
                Severity = SeverityLevel.High,
                PackageName = "nginx",
                PackageVersion = "latest",
                FilePath = Path.Combine(_root, "docker-compose.yml"),
                LineNumber = 7
            };
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(v2));
        }

        [Fact]
        public void AddIgnoredEntry_Secrets_MatchesByTitleSecretValueAndFile()
        {
            var file = Path.Combine(_root, "src", "config.ts");
            var vuln = new Vulnerability
            {
                Id = "secret-1",
                Title = "aws-access-key",
                Description = "AKIAIOSFODNN7EXAMPLE",
                Scanner = ScannerType.Secrets,
                Severity = SeverityLevel.High,
                FilePath = file,
                LineNumber = 22
            };
            IgnoreManager.AddIgnoredEntry(vuln);
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(vuln));

            // Different secret value should not match.
            var other = new Vulnerability
            {
                Id = "secret-2",
                Title = "aws-access-key",
                Description = "DIFFERENT",
                Scanner = ScannerType.Secrets,
                Severity = SeverityLevel.High,
                FilePath = file,
                LineNumber = 22
            };
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(other));
        }

        [Fact]
        public void Revive_RestoresVisibility()
        {
            var vuln = OssVuln("lodash", "1.0.0", "package.json");
            string key = IgnoreManager.AddIgnoredEntry(vuln);
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(vuln));

            IgnoreManager.Revive(key);
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(vuln));
        }

        [Fact]
        public void Restore_UndoesARevive()
        {
            var vuln = OssVuln("lodash", "1.0.0", "package.json");
            string key = IgnoreManager.AddIgnoredEntry(vuln);
            IgnoreManager.Revive(key);
            Assert.False(IgnoreManager.IsVulnerabilityIgnored(vuln));

            IgnoreManager.Restore(key);
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(vuln));
        }

        [Fact]
        public void AddAllIgnoredEntry_Oss_ExpandsAcrossFiles()
        {
            var primary = OssVuln("lodash", "1.0.0", "package.json");
            var sibling = OssVuln("lodash", "1.0.0", "package-lock.json");
            string key = IgnoreManager.AddAllIgnoredEntry(primary, new List<Vulnerability> { primary, sibling });

            // Both occurrences should now be ignored.
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(primary));
            Assert.True(IgnoreManager.IsVulnerabilityIgnored(sibling));

            // The single stored entry should have file references for both files.
            var entries = IgnoreFileManager.GetAllEntries();
            Assert.True(entries.ContainsKey(key));
            Assert.Equal(2, entries[key].Files.Count);
        }
    }
}
