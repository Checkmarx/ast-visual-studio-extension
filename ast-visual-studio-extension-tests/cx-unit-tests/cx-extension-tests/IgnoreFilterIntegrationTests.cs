using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore;
using ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Integration tests that drive the ignore pipeline end-to-end through <see cref="CxAssistDisplayCoordinator"/>:
    /// scanner posts findings → coordinator filters via <see cref="IgnoreManager"/> → revive resurfaces filtered findings.
    /// </summary>
    public class IgnoreFilterIntegrationTests : IDisposable
    {
        private readonly string _root;

        public IgnoreFilterIntegrationTests()
        {
            VsThreadingTestHelper.EnsureInitialized();
            _root = Path.Combine(Path.GetTempPath(), "cx-ignflt-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            IgnoreFileManager.Shutdown();
            IgnoreFileManager.Initialize(_root);
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());
        }

        public void Dispose()
        {
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());
            IgnoreFileManager.Shutdown();
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
        }

        private Vulnerability OssVuln(string name, string version, string fileRelative)
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
                FilePath = Path.Combine(_root, fileRelative),
                LineNumber = 5
            };
        }

        [Fact]
        public void MergeUpdate_FilteredFindings_ExcludeIgnoredOnes()
        {
            string file = Path.Combine(_root, "package.json");
            var v1 = OssVuln("lodash", "1.0.0", "package.json");
            var v2 = OssVuln("express", "1.0.0", "package.json");

            // Ignore lodash.
            IgnoreManager.AddIgnoredEntry(v1);

            // Feed both findings into the coordinator (mimics scanner result).
            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(file, ScannerType.OSS, new List<Vulnerability> { v1, v2 });

            var stored = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            var key = stored.Keys.First();
            var visible = stored[key];

            Assert.Single(visible);
            Assert.Equal("express", visible[0].PackageName);
        }

        [Fact]
        public void ReviveAfterIgnore_ReSurfacesFindings()
        {
            string file = Path.Combine(_root, "package.json");
            var v1 = OssVuln("lodash", "1.0.0", "package.json");

            string key = IgnoreManager.AddIgnoredEntry(v1);
            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(file, ScannerType.OSS, new List<Vulnerability> { v1 });

            // Sanity: filtered out.
            Assert.Empty(CxAssistDisplayCoordinator.GetAllIssuesByFile());

            // Revive: IgnoreFileManager fires IgnoreDataChanged → coordinator re-derives from raw cache.
            IgnoreManager.Revive(key);

            var stored = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.Single(stored);
            Assert.Single(stored.Values.First());
            Assert.Equal("lodash", stored.Values.First()[0].PackageName);
        }

        [Fact]
        public void IgnoreAfterFindingsStored_FiresChangeAndHidesFinding()
        {
            string file = Path.Combine(_root, "package.json");
            var v1 = OssVuln("lodash", "1.0.0", "package.json");

            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(file, ScannerType.OSS, new List<Vulnerability> { v1 });
            Assert.Single(CxAssistDisplayCoordinator.GetAllIssuesByFile());

            // After-the-fact ignore should remove from visible set on the next IgnoreDataChanged.
            IgnoreManager.AddIgnoredEntry(v1);

            Assert.Empty(CxAssistDisplayCoordinator.GetAllIssuesByFile());
        }
    }
}
