using System.Collections.Generic;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxAssistDisplayCoordinator (per-file issue storage, lookup, events).
    /// Only tests pure logic (storage, lookup, events); buffer-dependent methods are tested via integration tests.
    /// </summary>
    public class CxAssistDisplayCoordinatorTests
    {
        /// <summary>
        /// Helper to clear coordinator state before each test to avoid cross-test contamination.
        /// </summary>
        private void ClearCoordinator()
        {
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());
        }

        #region SetFindingsByFile

        [Fact]
        public void SetFindingsByFile_NullInput_DoesNotThrow()
        {
            CxAssistDisplayCoordinator.SetFindingsByFile(null);
        }

        [Fact]
        public void SetFindingsByFile_EmptyDictionary_ClearsFindings()
        {
            ClearCoordinator();

            var result = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void SetFindingsByFile_WithData_StoresFindings()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\package.json", new List<Vulnerability>
                    {
                        new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json")
                    }
                }
            };

            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            var result = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void SetFindingsByFile_SkipsNullKeyOrValue()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                { "", new List<Vulnerability> { new Vulnerability("V1", "Issue", "Desc", SeverityLevel.High, ScannerType.OSS, 1, 1, null) } },
            };

            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            var result = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            // Empty key should be skipped
            Assert.Empty(result);
        }

        #endregion

        #region GetCurrentFindings

        [Fact]
        public void GetCurrentFindings_NoData_ReturnsNull()
        {
            ClearCoordinator();
            var result = CxAssistDisplayCoordinator.GetCurrentFindings();
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentFindings_WithData_ReturnsFlatList()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\file1.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 1, 1, @"C:\src\file1.cs"),
                        new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 2, 1, @"C:\src\file1.cs")
                    }
                },
                {
                    @"C:\src\file2.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Low, ScannerType.Secrets, 5, 1, @"C:\src\file2.cs")
                    }
                }
            };

            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            var result = CxAssistDisplayCoordinator.GetCurrentFindings();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        #endregion

        #region FindVulnerabilityById

        [Fact]
        public void FindVulnerabilityById_Null_ReturnsNull()
        {
            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById(null));
        }

        [Fact]
        public void FindVulnerabilityById_Empty_ReturnsNull()
        {
            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById(""));
        }

        [Fact]
        public void FindVulnerabilityById_ExistingId_ReturnsVulnerability()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\file.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V-001", "SQL Injection", "Desc", SeverityLevel.High, ScannerType.ASCA, 10, 1, @"C:\src\file.cs"),
                        new Vulnerability("V-002", "XSS", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 20, 1, @"C:\src\file.cs")
                    }
                }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            var found = CxAssistDisplayCoordinator.FindVulnerabilityById("V-002");
            Assert.NotNull(found);
            Assert.Equal("XSS", found.Title);
        }

        [Fact]
        public void FindVulnerabilityById_NonExistingId_ReturnsNull()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\file.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V-001", "Issue", "Desc", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs")
                    }
                }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("V-999"));
        }

        [Fact]
        public void FindVulnerabilityById_CaseInsensitive()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\file.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V-001", "Issue", "Desc", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs")
                    }
                }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            var found = CxAssistDisplayCoordinator.FindVulnerabilityById("v-001");
            Assert.NotNull(found);
        }

        #endregion

        #region IssuesUpdated Event

        [Fact]
        public void SetFindingsByFile_RaisesIssuesUpdated()
        {
            ClearCoordinator();
            bool eventFired = false;
            void handler(System.Collections.Generic.IReadOnlyDictionary<string, List<Vulnerability>> _) { eventFired = true; }

            CxAssistDisplayCoordinator.IssuesUpdated += handler;
            try
            {
                CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
                {
                    { @"C:\src\file.cs", new List<Vulnerability>
                        { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs") } }
                });

                Assert.True(eventFired);
            }
            finally
            {
                CxAssistDisplayCoordinator.IssuesUpdated -= handler;
            }
        }

        [Fact]
        public void SetFindingsByFile_EventContainsSnapshot()
        {
            ClearCoordinator();
            IReadOnlyDictionary<string, List<Vulnerability>> snapshot = null;
            void handler(IReadOnlyDictionary<string, List<Vulnerability>> data) { snapshot = data; }

            CxAssistDisplayCoordinator.IssuesUpdated += handler;
            try
            {
                CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
                {
                    { @"C:\src\file.cs", new List<Vulnerability>
                        { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs") } }
                });

                Assert.NotNull(snapshot);
                Assert.NotEmpty(snapshot);
            }
            finally
            {
                CxAssistDisplayCoordinator.IssuesUpdated -= handler;
            }
        }

        #endregion

        #region GetAllIssuesByFile - Snapshot Isolation

        [Fact]
        public void GetAllIssuesByFile_ReturnsCopy_NotReference()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    @"C:\src\file.cs", new List<Vulnerability>
                    {
                        new Vulnerability("V1", "Issue", "Desc", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs")
                    }
                }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            var snapshot1 = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            var snapshot2 = CxAssistDisplayCoordinator.GetAllIssuesByFile();

            // Different references
            Assert.NotSame(snapshot1, snapshot2);
        }

        [Fact]
        public void SetFindingsByFile_EmptyDictionary_GetCurrentFindingsReturnsNull()
        {
            ClearCoordinator();
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());

            var result = CxAssistDisplayCoordinator.GetCurrentFindings();
            Assert.Null(result);
        }

        [Fact]
        public void SetFindingsByFile_ReplacesPreviousFindings()
        {
            ClearCoordinator();
            var first = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\src\a.cs", new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\a.cs") } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(first);
            var second = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\src\b.cs", new List<Vulnerability> { new Vulnerability("V2", "T2", "D2", SeverityLevel.Medium, ScannerType.ASCA, 5, 1, @"C:\src\b.cs") } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(second);

            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.Single(all);
            Assert.Contains(@"C:\src\b.cs", all.Keys);
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("V1"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V2"));
        }

        [Fact]
        public void FindVulnerabilityById_MultipleFiles_SearchesAll()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\src\file1.cs", new List<Vulnerability> { new Vulnerability("ID-1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file1.cs") } },
                { @"C:\src\file2.cs", new List<Vulnerability> { new Vulnerability("ID-2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 1, 1, @"C:\src\file2.cs") } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);

            var found1 = CxAssistDisplayCoordinator.FindVulnerabilityById("ID-1");
            var found2 = CxAssistDisplayCoordinator.FindVulnerabilityById("ID-2");

            Assert.NotNull(found1);
            Assert.Equal("Issue1", found1.Title);
            Assert.NotNull(found2);
            Assert.Equal("Issue2", found2.Title);
        }

        [Fact]
        public void SetFindingsByFile_NoEventSubscriber_DoesNotThrow()
        {
            ClearCoordinator();
            var ex = Record.Exception(() =>
                CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
                {
                    { @"C:\src\file.cs", new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs") } }
                }));

            Assert.Null(ex);
        }

        #endregion

        #region FindVulnerabilityByLocation

        [Fact]
        public void FindVulnerabilityByLocation_NullDocumentPath_ReturnsNull()
        {
            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(null, 0));
        }

        [Fact]
        public void FindVulnerabilityByLocation_EmptyDocumentPath_ReturnsNull()
        {
            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation("", 0));
        }

        [Fact]
        public void FindVulnerabilityByLocation_NoData_ReturnsNull()
        {
            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(@"C:\src\file.cs", 0));
        }

        [Fact]
        public void FindVulnerabilityByLocation_ZeroBasedLine_MatchesFirstVulnerabilityOnLine()
        {
            ClearCoordinator();
            var path = @"C:\src\app.cs";
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                {
                    path, new List<Vulnerability>
                    {
                        new Vulnerability("V1", "First", "Desc1", SeverityLevel.High, ScannerType.ASCA, 11, 1, path),
                        new Vulnerability("V2", "Second", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 11, 1, path)
                    }
                }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            // Line 11 is 1-based -> 0-based line 10
            var found = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 10);
            Assert.NotNull(found);
            Assert.True(found.LineNumber == 11);
        }

        [Fact]
        public void FindVulnerabilityByLocation_ZeroBasedLineZero_MatchesLineOne()
        {
            ClearCoordinator();
            var path = @"C:\project\file.cs";
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                { path, new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, path) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            var found = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 0);
            Assert.NotNull(found);
            Assert.Equal(1, found.LineNumber);
        }

        [Fact]
        public void FindVulnerabilityByLocation_NonMatchingLine_ReturnsNull()
        {
            ClearCoordinator();
            var path = @"C:\src\file.cs";
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                { path, new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 5, 1, path) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 0));
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 99));
        }

        [Fact]
        public void FindVulnerabilityByLocation_FileNotInFindings_ReturnsNull()
        {
            ClearCoordinator();
            var issues = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\src\other.cs", new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\other.cs") } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(issues);
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(@"C:\src\file.cs", 0));
        }

        [Fact]
        public void GetAllIssuesByFile_AfterSetFindingsByFile_KeysMatchInput()
        {
            ClearCoordinator();
            var path1 = @"C:\src\a.cs";
            var path2 = @"C:\src\b.cs";
            var byFile = new Dictionary<string, List<Vulnerability>>
            {
                { path1, new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, path1) } },
                { path2, new List<Vulnerability> { new Vulnerability("V2", "T", "D", SeverityLevel.Medium, ScannerType.ASCA, 1, 1, path2) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.Equal(2, all.Count);
            Assert.True(all.ContainsKey(path1) || all.Keys.Any(k => string.Equals(k, path1, System.StringComparison.OrdinalIgnoreCase)));
            Assert.True(all.ContainsKey(path2) || all.Keys.Any(k => string.Equals(k, path2, System.StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void SetFindingsByFile_NullValueInDictionary_KeySkipped()
        {
            ClearCoordinator();
            var byFile = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\src\valid.cs", new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\valid.cs") } },
                { @"C:\src\nullvalue", null }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.Single(all);
        }

        #endregion
    }
}
