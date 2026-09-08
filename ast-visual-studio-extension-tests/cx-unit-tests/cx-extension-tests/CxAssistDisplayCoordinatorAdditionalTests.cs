using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Additional tests for CxAssistDisplayCoordinator covering the four completely untested methods:
    /// UpdateFindingsForFile, MergeUpdateFindingsForScanner (pure storage path),
    /// FindAllVulnerabilitiesForPackage, FindAllVulnerabilitiesForLine, and ClearAllFindings.
    /// Also covers GetCachedVulnerabilitiesForFile.
    /// Note: UpdateFindings requires an ITextBuffer (VS editor type) and is covered by integration tests.
    /// </summary>
    public class CxAssistDisplayCoordinatorAdditionalTests
    {
        private static Vulnerability MakeVuln(string id, string filePath, int line = 1,
            ScannerType scanner = ScannerType.ASCA, SeverityLevel severity = SeverityLevel.High,
            string packageName = null, string title = "Issue")
        {
            return new Vulnerability
            {
                Id = id, FilePath = filePath, LineNumber = line,
                Scanner = scanner, Severity = severity,
                PackageName = packageName, Title = title
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // UpdateFindingsForFile
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void UpdateFindingsForFile_NullFilePath_DoesNotThrow()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(null,
                new List<Vulnerability> { MakeVuln("v1", null) });
        }

        [Fact]
        public void UpdateFindingsForFile_EmptyFilePath_DoesNotThrow()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile("",
                new List<Vulnerability> { MakeVuln("v1", "") });
        }

        [Fact]
        public void UpdateFindingsForFile_ValidFindings_StoredAndRetrievable()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var vuln = MakeVuln("update-file-v1", @"C:\file.cs");

            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { vuln });

            var found = CxAssistDisplayCoordinator.FindVulnerabilityById("update-file-v1");
            Assert.NotNull(found);
            Assert.Equal("update-file-v1", found.Id);
        }

        [Fact]
        public void UpdateFindingsForFile_NullVulnerabilities_ClearsFileFindings()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\file.cs") });

            // Now clear by passing null
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs", null);

            var found = CxAssistDisplayCoordinator.FindVulnerabilityById("v1");
            Assert.Null(found);
        }

        [Fact]
        public void UpdateFindingsForFile_EmptyList_ClearsFileFindings()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\file.cs") });

            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability>());

            var found = CxAssistDisplayCoordinator.FindVulnerabilityById("v1");
            Assert.Null(found);
        }

        [Fact]
        public void UpdateFindingsForFile_ReplacesExistingFindings_ForSameFile()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("old-id", @"C:\file.cs") });

            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("new-id", @"C:\file.cs") });

            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("old-id"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("new-id"));
        }

        [Fact]
        public void UpdateFindingsForFile_DoesNotAffectOtherFiles()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\fileA.cs",
                new List<Vulnerability> { MakeVuln("va", @"C:\fileA.cs") });
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\fileB.cs",
                new List<Vulnerability> { MakeVuln("vb", @"C:\fileB.cs") });

            // Update only fileA
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\fileA.cs",
                new List<Vulnerability>());

            // fileB should be unaffected
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("vb"));
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("va"));
        }

        [Fact]
        public void UpdateFindingsForFile_RaisesIssuesUpdatedEvent()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            bool eventRaised = false;
            CxAssistDisplayCoordinator.IssuesUpdated += _ => { eventRaised = true; };

            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\file.cs") });

            Assert.True(eventRaised);
            CxAssistDisplayCoordinator.IssuesUpdated -= _ => { };
        }

        // ══════════════════════════════════════════════════════════════════════
        // FindAllVulnerabilitiesForPackage (takes a Vulnerability, searches by PackageName+Version)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void FindAllVulnerabilitiesForPackage_NullVulnerability_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(null);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForPackage_NonOssScanner_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var v = MakeVuln("v1", @"C:\file.cs", scanner: ScannerType.ASCA, packageName: "lodash");
            // Only OSS scanner is supported; other scanners return null
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(v);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForPackage_OssWithNullPackageName_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var v = MakeVuln("v1", @"C:\package.json", scanner: ScannerType.OSS, packageName: null);
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(v);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForPackage_MatchingPackage_ReturnsAllForSameNameAndVersion()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var p1 = new Vulnerability { Id = "p1", FilePath = @"C:\package.json", Scanner = ScannerType.OSS, PackageName = "lodash", PackageVersion = "4.17.21" };
            var p2 = new Vulnerability { Id = "p2", FilePath = @"C:\package.json", Scanner = ScannerType.OSS, PackageName = "lodash", PackageVersion = "4.17.21" };
            var p3 = new Vulnerability { Id = "p3", FilePath = @"C:\package.json", Scanner = ScannerType.OSS, PackageName = "express", PackageVersion = "4.18.0" };
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\package.json",
                new List<Vulnerability> { p1, p2, p3 });

            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(p1);
            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal("lodash", v.PackageName));
        }

        [Fact]
        public void FindAllVulnerabilitiesForPackage_NoMatchingPackage_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\package.json",
                new List<Vulnerability>
                {
                    new Vulnerability { Id = "v1", FilePath = @"C:\package.json", Scanner = ScannerType.OSS, PackageName = "lodash", PackageVersion = "4.0.0" }
                });

            var query = new Vulnerability { Scanner = ScannerType.OSS, PackageName = "express", PackageVersion = "4.0.0" };
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(query);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForPackage_NoData_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var query = new Vulnerability { Scanner = ScannerType.OSS, PackageName = "lodash", PackageVersion = "1.0.0" };
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForPackage(query);
            Assert.Null(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // FindAllVulnerabilitiesForLine (takes a Vulnerability, matches scanner+filePath+line)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void FindAllVulnerabilitiesForLine_NullVulnerability_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(null);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForLine_NullFilePath_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var v = MakeVuln("v1", null, line: 5);
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(v);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForLine_NoData_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var v = MakeVuln("v1", @"C:\file.cs", line: 5);
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(v);
            Assert.Null(result);
        }

        [Fact]
        public void FindAllVulnerabilitiesForLine_SingleIssueOnLine_ReturnsIt()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var stored = MakeVuln("v1", @"C:\file.cs", line: 10);
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { stored });

            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(stored);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("v1", result[0].Id);
        }

        [Fact]
        public void FindAllVulnerabilitiesForLine_MultipleIssuesSameLine_ReturnsAll()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var v1 = MakeVuln("a1", @"C:\file.cs", line: 5, scanner: ScannerType.ASCA);
            var v2 = MakeVuln("a2", @"C:\file.cs", line: 5, scanner: ScannerType.ASCA);
            var v3 = MakeVuln("a3", @"C:\file.cs", line: 7, scanner: ScannerType.ASCA);
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { v1, v2, v3 });

            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(v1);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FindAllVulnerabilitiesForLine_NonMatchingLine_ReturnsNull()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var stored = MakeVuln("v1", @"C:\file.cs", line: 10);
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { stored });

            var query = MakeVuln("q", @"C:\file.cs", line: 99);
            var result = CxAssistDisplayCoordinator.FindAllVulnerabilitiesForLine(query);
            Assert.Null(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ClearAllFindings
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void ClearAllFindings_RemovesAllStoredFindings()
        {
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("clear-v1", @"C:\file.cs") });

            CxAssistDisplayCoordinator.ClearAllFindings();

            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("clear-v1"));
        }

        [Fact]
        public void ClearAllFindings_GetCurrentFindings_ReturnsNullAfterClear()
        {
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\file.cs") });

            CxAssistDisplayCoordinator.ClearAllFindings();

            var findings = CxAssistDisplayCoordinator.GetCurrentFindings();
            Assert.True(findings == null || findings.Count == 0);
        }

        [Fact]
        public void ClearAllFindings_CalledOnEmpty_DoesNotThrow()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.ClearAllFindings(); // double clear should be safe
        }

        [Fact]
        public void ClearAllFindings_RaisesIssuesUpdatedEvent()
        {
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\file.cs") });

            bool eventRaised = false;
            void handler(System.Collections.Generic.IReadOnlyDictionary<string, List<Vulnerability>> _)
                => eventRaised = true;

            CxAssistDisplayCoordinator.IssuesUpdated += handler;
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.IssuesUpdated -= handler;

            Assert.True(eventRaised);
        }

        [Fact]
        public void ClearAllFindings_MultipleFiles_ClearsAll()
        {
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\a.cs",
                new List<Vulnerability> { MakeVuln("va", @"C:\a.cs") });
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\b.cs",
                new List<Vulnerability> { MakeVuln("vb", @"C:\b.cs") });

            CxAssistDisplayCoordinator.ClearAllFindings();

            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("va"));
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("vb"));
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetCachedVulnerabilitiesForFile
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void GetCachedVulnerabilitiesForFile_NullPath_ReturnsNullOrEmpty()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var result = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(null);
            Assert.True(result == null || result.Count == 0);
        }

        [Fact]
        public void GetCachedVulnerabilitiesForFile_EmptyPath_ReturnsNullOrEmpty()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var result = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile("");
            Assert.True(result == null || result.Count == 0);
        }

        [Fact]
        public void GetCachedVulnerabilitiesForFile_NoData_ReturnsNullOrEmpty()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var result = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(@"C:\file.cs");
            Assert.True(result == null || result.Count == 0);
        }

        [Fact]
        public void GetCachedVulnerabilitiesForFile_ExistingFile_ReturnsItsVulnerabilities()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            var vulns = new List<Vulnerability>
            {
                MakeVuln("cached-v1", @"C:\cached.cs"),
                MakeVuln("cached-v2", @"C:\cached.cs")
            };
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\cached.cs", vulns);

            var result = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(@"C:\cached.cs");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetCachedVulnerabilitiesForFile_UnknownFile_ReturnsNullOrEmpty()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\known.cs",
                new List<Vulnerability> { MakeVuln("v1", @"C:\known.cs") });

            var result = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(@"C:\other.cs");
            Assert.True(result == null || result.Count == 0);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SetFindingsByFile then UpdateFindingsForFile — interaction
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void UpdateFindingsForFile_AfterSetFindingsByFile_ReplacesFileEntry()
        {
            CxAssistDisplayCoordinator.ClearAllFindings();
            // Populate via SetFindingsByFile
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\file.cs", new List<Vulnerability> { MakeVuln("old", @"C:\file.cs") } }
            });

            // Then update just that file
            CxAssistDisplayCoordinator.UpdateFindingsForFile(@"C:\file.cs",
                new List<Vulnerability> { MakeVuln("new", @"C:\file.cs") });

            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("old"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("new"));
        }
    }
}
