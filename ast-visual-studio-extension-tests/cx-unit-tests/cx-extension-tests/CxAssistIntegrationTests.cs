using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Integration tests for CxAssist: multi-component flows (MockData → TreeBuilder → Coordinator, Prompts).
    /// No VS or WPF required; tests use in-memory APIs only.
    /// </summary>
    public class CxAssistIntegrationTests
    {
        private static void ClearCoordinator()
        {
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());
        }

        #region MockData → TreeBuilder

        [Fact]
        public void Integration_CommonMockData_ToTreeBuilder_ProducesValidFileNodes()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities(@"C:\src\Program.cs");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            Assert.NotEmpty(fileNodes);
            Assert.Single(fileNodes);
            var fileNode = fileNodes[0];
            Assert.Equal("Program.cs", fileNode.FileName);
            Assert.Equal(@"C:\src\Program.cs", fileNode.FilePath);
            Assert.NotNull(fileNode.Vulnerabilities);
            Assert.NotEmpty(fileNode.Vulnerabilities);
            Assert.NotNull(fileNode.SeverityCounts);
            Assert.All(fileNode.Vulnerabilities, v => Assert.NotNull(v.Severity));
            Assert.All(fileNode.Vulnerabilities, v => Assert.NotNull(v.Description));
        }

        [Fact]
        public void Integration_CommonMockData_ToTreeBuilder_OnlyProblemSeveritiesInTree()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            var allSeverities = fileNodes.SelectMany(f => f.Vulnerabilities.Select(v => v.Severity)).ToList();
            Assert.DoesNotContain("Ok", allSeverities);
            Assert.DoesNotContain("Unknown", allSeverities);
            Assert.DoesNotContain("Ignored", allSeverities);
        }

        [Fact]
        public void Integration_PackageJsonMockData_ToTreeBuilder_OkAndUnknownFilteredOut()
        {
            var vulnerabilities = CxAssistMockData.GetPackageJsonMockVulnerabilities();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            var problemCount = vulnerabilities.Count(v => CxAssistConstants.IsProblem(v.Severity));
            var treeVulnCount = fileNodes.Sum(f => f.Vulnerabilities.Count);
            Assert.True(treeVulnCount <= problemCount, "Tree groups same-line findings so count can be less.");
            Assert.True(problemCount == 0 || treeVulnCount > 0, "All problem findings should appear in tree (possibly grouped).");
        }

        [Fact]
        public void Integration_PomMockData_ToTreeBuilder_ProducesFileNodeWithOssFindings()
        {
            var path = @"C:\project\pom.xml";
            var vulnerabilities = CxAssistMockData.GetPomMockVulnerabilities(path);
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            if (fileNodes.Count > 0)
            {
                Assert.Equal("pom.xml", fileNodes[0].FileName);
                Assert.Equal(path, fileNodes[0].FilePath);
            }
        }

        [Fact]
        public void Integration_SecretsMockData_ToTreeBuilder_ContainsSecretsAndAsca()
        {
            var vulnerabilities = CxAssistMockData.GetSecretsPyMockVulnerabilities(@"C:\src\secrets.py");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            Assert.NotEmpty(fileNodes);
            var allScanners = fileNodes.SelectMany(f => f.Vulnerabilities).Select(v => v.Scanner).Distinct().ToList();
            Assert.Contains(ScannerType.Secrets, allScanners);
            Assert.Contains(ScannerType.ASCA, allScanners);
        }

        [Fact]
        public void Integration_IacMockData_ToTreeBuilder_AllIacScanner()
        {
            var vulnerabilities = CxAssistMockData.GetIacMockVulnerabilities(@"C:\src\main.tf");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            Assert.NotEmpty(fileNodes);
            Assert.All(fileNodes.SelectMany(f => f.Vulnerabilities), v => Assert.Equal(ScannerType.IaC, v.Scanner));
        }

        [Fact]
        public void Integration_ContainerMockData_ToTreeBuilder_AllContainersScanner()
        {
            var vulnerabilities = CxAssistMockData.GetContainerMockVulnerabilities(@"C:\src\Dockerfile");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            Assert.NotNull(fileNodes);
            Assert.NotEmpty(fileNodes);
            Assert.All(fileNodes.SelectMany(f => f.Vulnerabilities), v => Assert.Equal(ScannerType.Containers, v.Scanner));
        }

        #endregion

        #region Coordinator + TreeBuilder (SetFindingsByFile → GetCurrentFindings → BuildFileNodes)

        [Fact]
        public void Integration_CoordinatorSetFindings_GetCurrentFindings_MatchesInput()
        {
            ClearCoordinator();
            var path = @"C:\src\app.cs";
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities(path);
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, vulnerabilities } };

            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var current = CxAssistDisplayCoordinator.GetCurrentFindings();

            Assert.NotNull(current);
            Assert.Equal(vulnerabilities.Count, current.Count);
        }

        [Fact]
        public void Integration_CoordinatorSetFindings_BuildFileNodesFromCurrent_ProducesConsistentTree()
        {
            ClearCoordinator();
            var path = @"C:\src\Program.cs";
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities(path);
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, vulnerabilities } };

            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current);

            Assert.NotNull(fileNodes);
            Assert.NotEmpty(fileNodes);
            var problemCount = vulnerabilities.Count(v => CxAssistConstants.IsProblem(v.Severity));
            var treeCount = fileNodes[0].Vulnerabilities.Count;
            Assert.True(treeCount <= problemCount, "Same-line grouping can reduce tree node count.");
            Assert.True(treeCount > 0, "Tree should have at least one vulnerability node.");
        }

        [Fact]
        public void Integration_CoordinatorFindVulnerabilityById_FromMockData_ReturnsCorrectVulnerability()
        {
            ClearCoordinator();
            var path = @"C:\src\Program.cs";
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities(path);
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, vulnerabilities } };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            var first = vulnerabilities.First(v => CxAssistConstants.IsProblem(v.Severity));
            var found = CxAssistDisplayCoordinator.FindVulnerabilityById(first.Id);

            Assert.NotNull(found);
            Assert.Equal(first.Id, found.Id);
            Assert.Equal(first.Title, found.Title);
            Assert.Equal(first.Severity, found.Severity);
        }

        [Fact]
        public void Integration_CoordinatorFindVulnerabilityByLocation_FromMockData_ReturnsVulnerabilityOnLine()
        {
            ClearCoordinator();
            var path = @"C:\src\Program.cs";
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities(path);
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, vulnerabilities } };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            int line1Based = 1;
            int zeroBased = CxAssistConstants.To0BasedLineForEditor(ScannerType.OSS, line1Based);
            var found = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, zeroBased);

            Assert.NotNull(found);
            Assert.Equal(line1Based, found.LineNumber);
        }

        [Fact]
        public void Integration_MultiFileMockData_CoordinatorGetAllIssuesByFile_ThenTreeBuilder_ProducesMultipleFileNodes()
        {
            ClearCoordinator();
            var packageJson = CxAssistMockData.GetPackageJsonMockVulnerabilities(@"C:\project\package.json");
            var pom = CxAssistMockData.GetPomMockVulnerabilities(@"C:\project\pom.xml");
            var byFile = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\project\package.json", packageJson },
                { @"C:\project\pom.xml", pom }
            };

            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.Equal(2, all.Count);

            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current);

            Assert.True(fileNodes.Count >= 1);
            var fileNames = fileNodes.Select(f => f.FileName).ToList();
            Assert.Contains("package.json", fileNames);
            Assert.Contains("pom.xml", fileNames);
        }

        #endregion

        #region MockData / Vulnerability → Prompts (Fix + ViewDetails)

        [Fact]
        public void Integration_CommonMockVulnerability_FixPrompt_And_ViewDetailsPrompt_BothNonNull()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var firstProblem = vulnerabilities.First(v => CxAssistConstants.IsProblem(v.Severity));

            var fixPrompt = CxOneAssistFixPrompts.BuildForVulnerability(firstProblem);
            var viewPrompt = ViewDetailsPrompts.BuildForVulnerability(firstProblem);

            Assert.NotNull(fixPrompt);
            Assert.NotNull(viewPrompt);
            Assert.Contains("Checkmarx One Assist", fixPrompt);
            Assert.Contains("Checkmarx One Assist", viewPrompt);
        }

        [Fact]
        public void Integration_OssVulnerability_FixPrompt_ContainsPackageAndRemediation()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var oss = vulnerabilities.First(v => v.Scanner == ScannerType.OSS && CxAssistConstants.IsProblem(v.Severity));

            var fixPrompt = CxOneAssistFixPrompts.BuildForVulnerability(oss);

            Assert.NotNull(fixPrompt);
            Assert.Contains(oss.PackageName ?? oss.Title, fixPrompt);
            Assert.True(fixPrompt.Contains("PackageRemediation") || fixPrompt.Contains("remediat"));
        }

        [Fact]
        public void Integration_AscaVulnerability_ViewDetailsPrompt_ContainsRuleAndDescription()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var asca = vulnerabilities.First(v => v.Scanner == ScannerType.ASCA);

            var viewPrompt = ViewDetailsPrompts.BuildForVulnerability(asca);

            Assert.NotNull(viewPrompt);
            Assert.Contains(asca.RuleName ?? asca.Title, viewPrompt);
        }

        [Fact]
        public void Integration_SecretsMockVulnerability_FixAndViewDetails_BothBuild()
        {
            var vulnerabilities = CxAssistMockData.GetSecretsPyMockVulnerabilities();
            var first = vulnerabilities.First(v => CxAssistConstants.IsProblem(v.Severity));

            var fixPrompt = CxOneAssistFixPrompts.BuildForVulnerability(first);
            var viewPrompt = ViewDetailsPrompts.BuildForVulnerability(first);

            Assert.NotNull(fixPrompt);
            Assert.NotNull(viewPrompt);
            Assert.Contains("secret", fixPrompt.ToLower());
            Assert.Contains("secret", viewPrompt.ToLower());
        }

        [Fact]
        public void Integration_IacMockVulnerability_FixAndViewDetails_BothBuild()
        {
            var vulnerabilities = CxAssistMockData.GetIacMockVulnerabilities(@"C:\src\main.tf");
            var first = vulnerabilities.First(v => CxAssistConstants.IsProblem(v.Severity));

            var fixPrompt = CxOneAssistFixPrompts.BuildForVulnerability(first);
            var viewPrompt = ViewDetailsPrompts.BuildForVulnerability(first);

            Assert.NotNull(fixPrompt);
            Assert.NotNull(viewPrompt);
            Assert.Contains("IaC", fixPrompt);
        }

        [Fact]
        public void Integration_ContainerMockVulnerability_FixAndViewDetails_BothBuild()
        {
            var vulnerabilities = CxAssistMockData.GetContainerMockVulnerabilities();
            var first = vulnerabilities.First(v => CxAssistConstants.IsProblem(v.Severity));

            var fixPrompt = CxOneAssistFixPrompts.BuildForVulnerability(first);
            var viewPrompt = ViewDetailsPrompts.BuildForVulnerability(first);

            Assert.NotNull(fixPrompt);
            Assert.NotNull(viewPrompt);
        }

        #endregion

        #region IssuesUpdated event + snapshot

        [Fact]
        public void Integration_SetFindingsByFile_RaisesIssuesUpdated_WithSnapshotMatchingGetAllIssuesByFile()
        {
            ClearCoordinator();
            var path = @"C:\src\file.cs";
            var list = new List<Vulnerability>
            {
                new Vulnerability("V1", "Title", "Desc", SeverityLevel.High, ScannerType.OSS, 1, 1, path)
            };
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, list } };
            IReadOnlyDictionary<string, List<Vulnerability>> eventSnapshot = null;
            void Handler(System.Collections.Generic.IReadOnlyDictionary<string, List<Vulnerability>> snapshot) => eventSnapshot = snapshot;

            CxAssistDisplayCoordinator.IssuesUpdated += Handler;
            try
            {
                CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
                Assert.NotNull(eventSnapshot);
                Assert.Single(eventSnapshot);
                Assert.True(eventSnapshot.ContainsKey(path));
                Assert.Single(eventSnapshot[path]);
                Assert.Equal("V1", eventSnapshot[path][0].Id);

                var getAll = CxAssistDisplayCoordinator.GetAllIssuesByFile();
                Assert.Equal(eventSnapshot.Count, getAll.Count);
            }
            finally
            {
                CxAssistDisplayCoordinator.IssuesUpdated -= Handler;
            }
        }

        #endregion

        #region TreeBuilder + VulnerabilityNode display text

        [Fact]
        public void Integration_CommonMockData_TreeBuilder_VulnerabilityNodeDisplayText_ContainsLineAndColumn()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            var firstNode = fileNodes[0].Vulnerabilities[0];
            Assert.NotNull(firstNode.DisplayText);
            Assert.Contains("Ln", firstNode.DisplayText);
            Assert.Contains("Col", firstNode.DisplayText);
            Assert.Contains(CxAssistConstants.DisplayName, firstNode.DisplayText);
        }

        [Fact]
        public void Integration_CommonMockData_TreeBuilder_FileNodesOrderedByFilePath()
        {
            var path1 = @"C:\src\a.cs";
            var path2 = @"C:\src\b.cs";
            var v1 = new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, path1) };
            var v2 = new List<Vulnerability> { new Vulnerability("V2", "T", "D", SeverityLevel.Medium, ScannerType.OSS, 1, 1, path2) };
            var combined = v1.Concat(v2).ToList();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(combined);

            Assert.Equal(2, fileNodes.Count);
            Assert.True(fileNodes[0].FilePath.CompareTo(fileNodes[1].FilePath) <= 0);
        }

        [Fact]
        public void Integration_CoordinatorClear_ThenSetAgain_ReflectsNewData()
        {
            ClearCoordinator();
            var path = @"C:\src\file.cs";
            var first = new Dictionary<string, List<Vulnerability>>
            {
                { path, new List<Vulnerability> { new Vulnerability("V1", "T1", "D1", SeverityLevel.High, ScannerType.OSS, 1, 1, path) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(first);
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V1"));

            ClearCoordinator();
            Assert.Null(CxAssistDisplayCoordinator.GetCurrentFindings());
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("V1"));

            var second = new Dictionary<string, List<Vulnerability>>
            {
                { path, new List<Vulnerability> { new Vulnerability("V2", "T2", "D2", SeverityLevel.Medium, ScannerType.ASCA, 2, 1, path) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(second);
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("V1"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V2"));
        }

        [Fact]
        public void Integration_RequirementsMock_ToTreeBuilder_OnlyProblemSeveritiesInTree()
        {
            var vulnerabilities = CxAssistMockData.GetRequirementsMockVulnerabilities(@"C:\src\requirements.txt");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            var problemCount = vulnerabilities.Count(v => CxAssistConstants.IsProblem(v.Severity));
            var treeCount = fileNodes.Sum(f => f.Vulnerabilities.Count);
            Assert.Equal(problemCount, treeCount);
        }

        [Fact]
        public void Integration_EveryMockDataSource_BuildsValidTreeOrEmpty()
        {
            var path = @"C:\test\file";
            var sources = new List<List<Vulnerability>>
            {
                CxAssistMockData.GetCommonVulnerabilities(path),
                CxAssistMockData.GetPackageJsonMockVulnerabilities(path + ".json"),
                CxAssistMockData.GetPomMockVulnerabilities(path + ".xml"),
                CxAssistMockData.GetSecretsPyMockVulnerabilities(path + ".py"),
                CxAssistMockData.GetRequirementsMockVulnerabilities(path + ".txt"),
                CxAssistMockData.GetIacMockVulnerabilities(path + ".tf"),
                CxAssistMockData.GetContainerMockVulnerabilities(path),
                CxAssistMockData.GetDockerComposeMockVulnerabilities(path + ".yml"),
                CxAssistMockData.GetGoModMockVulnerabilities(path + ".mod"),
                CxAssistMockData.GetCsprojMockVulnerabilities(path + ".csproj")
            };

            foreach (var list in sources)
            {
                var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(list);
                Assert.NotNull(fileNodes);
                var problemCount = list.Count(v => CxAssistConstants.IsProblem(v.Severity));
                var treeCount = fileNodes.Sum(f => f.Vulnerabilities.Count);
                Assert.True(treeCount <= problemCount, "Same-line grouping can reduce tree node count.");
                Assert.True(problemCount == 0 || treeCount > 0, "At least one tree node when there are problem findings.");
            }
        }

        #endregion

        #region File-based integration (test-data layout)

        /// <summary>
        /// Resolves test-data path when running from test output (test-data is CopyToOutputDirectory).
        /// </summary>
        private static string GetTestDataPath(string relativePath)
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location);
            if (string.IsNullOrEmpty(baseDir)) return null;
            return Path.Combine(baseDir, "test-data", relativePath);
        }

        [Fact]
        public void Integration_TestDataManifestFiles_RecognizedByScannerConstants()
        {
            var packageJsonPath = GetTestDataPath("package.json");
            var pomPath = GetTestDataPath("pom.xml");
            if (!string.IsNullOrEmpty(packageJsonPath))
                Assert.True(CxAssistScannerConstants.IsManifestFile(packageJsonPath));
            if (!string.IsNullOrEmpty(pomPath))
                Assert.True(CxAssistScannerConstants.IsManifestFile(pomPath));
        }

        [Fact]
        public void Integration_TestDataContainerAndIacFiles_RecognizedByScannerConstants()
        {
            var dockerfilePath = GetTestDataPath("Dockerfile");
            var valuesYamlPath = GetTestDataPath("values.yaml");
            if (!string.IsNullOrEmpty(dockerfilePath))
            {
                Assert.True(CxAssistScannerConstants.IsContainersFile(dockerfilePath));
                Assert.True(CxAssistScannerConstants.IsDockerFile(dockerfilePath));
                Assert.True(CxAssistScannerConstants.IsIacFile(dockerfilePath));
            }
            if (!string.IsNullOrEmpty(valuesYamlPath))
                Assert.True(CxAssistScannerConstants.IsIacFile(valuesYamlPath));
        }

        [Fact]
        public void Integration_TestDataSecretsFile_NotExcludedForSecrets()
        {
            var secretsPath = GetTestDataPath("secrets.py");
            if (string.IsNullOrEmpty(secretsPath)) return;
            Assert.False(CxAssistScannerConstants.IsManifestFile(secretsPath));
            Assert.False(CxAssistScannerConstants.IsExcludedForSecrets(secretsPath));
        }

        [Fact]
        public void Integration_TestDataPackageJson_PassesBaseScanCheckAndIsManifest()
        {
            var path = GetTestDataPath("package.json");
            if (string.IsNullOrEmpty(path)) return;
            Assert.True(CxAssistScannerConstants.PassesBaseScanCheck(path));
            Assert.True(CxAssistScannerConstants.IsManifestFile(path));
        }

        [Fact]
        public void Integration_TestDataYamlFiles_RecognizedAsIac()
        {
            var valuesPath = GetTestDataPath("values.yaml");
            var negativePath = GetTestDataPath("negative1.yaml");
            if (!string.IsNullOrEmpty(valuesPath))
                Assert.True(CxAssistScannerConstants.IsIacFile(valuesPath));
            if (!string.IsNullOrEmpty(negativePath))
                Assert.True(CxAssistScannerConstants.IsIacFile(negativePath));
        }

        #endregion

        #region Coordinator + FindVulnerabilityByLocation (multiple lines)

        [Fact]
        public void Integration_Coordinator_FindVulnerabilityByLocation_EachLineReturnsCorrectVulnerability()
        {
            ClearCoordinator();
            var path = @"C:\src\app.cs";
            var list = new List<Vulnerability>
            {
                new Vulnerability("V1", "T1", "D1", SeverityLevel.High, ScannerType.ASCA, 5, 1, path),
                new Vulnerability("V2", "T2", "D2", SeverityLevel.Medium, ScannerType.ASCA, 10, 1, path),
                new Vulnerability("V3", "T3", "D3", SeverityLevel.Low, ScannerType.ASCA, 15, 1, path)
            };
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, list } };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            var atLine5 = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 4);
            var atLine10 = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 9);
            var atLine15 = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(path, 14);

            Assert.NotNull(atLine5);
            Assert.Equal("V1", atLine5.Id);
            Assert.NotNull(atLine10);
            Assert.Equal("V2", atLine10.Id);
            Assert.NotNull(atLine15);
            Assert.Equal("V3", atLine15.Id);
        }

        [Fact]
        public void Integration_Coordinator_TwoFiles_FindVulnerabilityByLocation_RespectsDocumentPath()
        {
            ClearCoordinator();
            var pathA = @"C:\src\a.cs";
            var pathB = @"C:\src\b.cs";
            var byFile = new Dictionary<string, List<Vulnerability>>
            {
                { pathA, new List<Vulnerability> { new Vulnerability("VA", "TA", "DA", SeverityLevel.High, ScannerType.OSS, 1, 1, pathA) } },
                { pathB, new List<Vulnerability> { new Vulnerability("VB", "TB", "DB", SeverityLevel.Medium, ScannerType.OSS, 1, 1, pathB) } }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            var foundA = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(pathA, 0);
            var foundB = CxAssistDisplayCoordinator.FindVulnerabilityByLocation(pathB, 0);

            Assert.NotNull(foundA);
            Assert.Equal("VA", foundA.Id);
            Assert.NotNull(foundB);
            Assert.Equal("VB", foundB.Id);
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityByLocation(pathA, 99));
        }

        #endregion

        #region TreeBuilder + SeverityCounts and display

        [Fact]
        public void Integration_CommonMockData_TreeBuilder_SeverityCountsReflectVulnerabilities()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            var problemSeverities = vulnerabilities
                .Where(v => CxAssistConstants.IsProblem(v.Severity))
                .Select(v => v.Severity.ToString())
                .Distinct()
                .ToList();
            var countSeverities = fileNodes[0].SeverityCounts.Select(c => c.Severity).ToList();

            foreach (var sev in problemSeverities)
                Assert.Contains(sev, countSeverities);
        }

        [Fact]
        public void Integration_CommonMockData_TreeBuilder_PrimaryDisplayText_FormattedPerScanner()
        {
            var vulnerabilities = CxAssistMockData.GetCommonVulnerabilities();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulnerabilities);

            foreach (var node in fileNodes[0].Vulnerabilities)
            {
                Assert.False(string.IsNullOrEmpty(node.PrimaryDisplayText));
                Assert.True(
                    node.PrimaryDisplayText.Contains("package") ||
                    node.PrimaryDisplayText.Contains("secret") ||
                    node.PrimaryDisplayText.Contains("container") ||
                    node.PrimaryDisplayText.Contains("detected on this line") ||
                    node.Description != null);
            }
        }

        [Fact]
        public void Integration_TreeBuilder_NullFilePath_UsesDefaultFilePath()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.ASCA, 1, 1, null)
            };
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(fileNodes);
            Assert.Equal(FindingsTreeBuilder.DefaultFilePath, fileNodes[0].FilePath);
            Assert.Equal(FindingsTreeBuilder.DefaultFilePath, fileNodes[0].FileName);
        }

        [Fact]
        public void Integration_TreeBuilder_CustomDefaultFilePath_UsedForNullPath()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, null)
            };
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, defaultFilePath: "custom.cs");

            Assert.Single(fileNodes);
            Assert.Equal("custom.cs", fileNodes[0].FilePath);
        }

        #endregion

        #region Multi-scanner in one file

        [Fact]
        public void Integration_OneFile_MixedScanners_CoordinatorAndTreeBuilder_AllPresent()
        {
            ClearCoordinator();
            var path = @"C:\src\mixed.cs";
            var list = new List<Vulnerability>
            {
                new Vulnerability("V1", "OSS", "D1", SeverityLevel.High, ScannerType.OSS, 1, 1, path) { PackageName = "pkg", PackageVersion = "1.0" },
                new Vulnerability("V2", "ASCA", "D2", SeverityLevel.Medium, ScannerType.ASCA, 5, 1, path) { RuleName = "R1" },
                new Vulnerability("V3", "Secret", "D3", SeverityLevel.Critical, ScannerType.Secrets, 10, 1, path)
            };
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, list } };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            Assert.NotNull(current);
            Assert.Equal(3, current.Count);

            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current);
            Assert.Single(fileNodes);
            Assert.Equal(3, fileNodes[0].Vulnerabilities.Count);

            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V1"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V2"));
            Assert.NotNull(CxAssistDisplayCoordinator.FindVulnerabilityById("V3"));
        }

        [Fact]
        public void Integration_OneFile_SameLineMultipleScanners_TreeOrderedByLineThenColumn()
        {
            var path = @"C:\src\same.cs";
            var list = new List<Vulnerability>
            {
                new Vulnerability("V1", "First", "D1", SeverityLevel.High, ScannerType.ASCA, 7, 10, path),
                new Vulnerability("V2", "Second", "D2", SeverityLevel.Medium, ScannerType.ASCA, 7, 5, path)
            };
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(list);

            Assert.Single(fileNodes);
            // ASCA groups by line: multiple findings on same line → one node (highest severity shown).
            Assert.Equal(1, fileNodes[0].Vulnerabilities.Count);
            Assert.Equal(7, fileNodes[0].Vulnerabilities[0].Line);
        }

        #endregion

        #region Prompts + same-line OSS

        [Fact]
        public void Integration_OssVulnerability_ViewDetailsWithSameLineVulns_ContainsCveList()
        {
            var v = new Vulnerability
            {
                Id = "V1",
                Scanner = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "4.17.19",
                Severity = SeverityLevel.High,
                CveName = "CVE-2021-001",
                Description = "Issue 1"
            };
            var sameLine = new List<Vulnerability>
            {
                v,
                new Vulnerability { CveName = "CVE-2021-002", Severity = SeverityLevel.Medium, Description = "Issue 2" }
            };

            var prompt = ViewDetailsPrompts.BuildForVulnerability(v, sameLine);

            Assert.NotNull(prompt);
            Assert.Contains("CVE-2021-001", prompt);
            Assert.Contains("CVE-2021-002", prompt);
        }

        [Fact]
        public void Integration_EveryScannerType_FromMockData_FixAndViewDetailsBothProducePrompt()
        {
            var path = @"C:\test\file";
            var oss = CxAssistMockData.GetCommonVulnerabilities(path).First(v => v.Scanner == ScannerType.OSS && CxAssistConstants.IsProblem(v.Severity));
            var asca = CxAssistMockData.GetCommonVulnerabilities(path).First(v => v.Scanner == ScannerType.ASCA);
            var secrets = CxAssistMockData.GetSecretsPyMockVulnerabilities(path + ".py").First(v => CxAssistConstants.IsProblem(v.Severity));
            var iac = CxAssistMockData.GetIacMockVulnerabilities(path + ".tf").First(v => CxAssistConstants.IsProblem(v.Severity));
            var containers = CxAssistMockData.GetContainerMockVulnerabilities(path).First(v => CxAssistConstants.IsProblem(v.Severity));

            Assert.NotNull(CxOneAssistFixPrompts.BuildForVulnerability(oss));
            Assert.NotNull(ViewDetailsPrompts.BuildForVulnerability(oss));
            Assert.NotNull(CxOneAssistFixPrompts.BuildForVulnerability(asca));
            Assert.NotNull(ViewDetailsPrompts.BuildForVulnerability(asca));
            Assert.NotNull(CxOneAssistFixPrompts.BuildForVulnerability(secrets));
            Assert.NotNull(ViewDetailsPrompts.BuildForVulnerability(secrets));
            Assert.NotNull(CxOneAssistFixPrompts.BuildForVulnerability(iac));
            Assert.NotNull(ViewDetailsPrompts.BuildForVulnerability(iac));
            Assert.NotNull(CxOneAssistFixPrompts.BuildForVulnerability(containers));
            Assert.NotNull(ViewDetailsPrompts.BuildForVulnerability(containers));
        }

        #endregion

        #region Coordinator edge cases

        [Fact]
        public void Integration_Coordinator_EmptyFindings_GetCurrentFindingsNull_FindByIdNull()
        {
            ClearCoordinator();
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>());

            Assert.Null(CxAssistDisplayCoordinator.GetCurrentFindings());
            Assert.Null(CxAssistDisplayCoordinator.FindVulnerabilityById("any"));
            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            Assert.NotNull(all);
            Assert.Empty(all);
        }

        [Fact]
        public void Integration_Coordinator_SetFindingsTwice_IssuesUpdatedSnapshotReflectsSecondSet()
        {
            ClearCoordinator();
            var path = @"C:\src\file.cs";
            IReadOnlyDictionary<string, List<Vulnerability>> lastSnapshot = null;
            void Handler(System.Collections.Generic.IReadOnlyDictionary<string, List<Vulnerability>> s) => lastSnapshot = s;

            CxAssistDisplayCoordinator.IssuesUpdated += Handler;
            try
            {
                CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
                {
                    { path, new List<Vulnerability> { new Vulnerability("V1", "T1", "D1", SeverityLevel.High, ScannerType.OSS, 1, 1, path) } }
                });
                Assert.NotNull(lastSnapshot);
                Assert.Single(lastSnapshot[path]);
                Assert.Equal("V1", lastSnapshot[path][0].Id);

                CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>>
                {
                    { path, new List<Vulnerability> { new Vulnerability("V2", "T2", "D2", SeverityLevel.Medium, ScannerType.ASCA, 2, 1, path) } }
                });
                Assert.Single(lastSnapshot[path]);
                Assert.Equal("V2", lastSnapshot[path][0].Id);
            }
            finally
            {
                CxAssistDisplayCoordinator.IssuesUpdated -= Handler;
            }
        }

        [Fact]
        public void Integration_Coordinator_GetAllIssuesByFile_ReturnsIndependentSnapshot()
        {
            ClearCoordinator();
            var path = @"C:\src\file.cs";
            var list = new List<Vulnerability> { new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, path) };
            CxAssistDisplayCoordinator.SetFindingsByFile(new Dictionary<string, List<Vulnerability>> { { path, list } });

            var snap1 = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            var snap2 = CxAssistDisplayCoordinator.GetAllIssuesByFile();

            Assert.NotSame(snap1, snap2);
            Assert.Equal(snap1.Count, snap2.Count);
        }

        #endregion

        #region TreeBuilder + callbacks

        [Fact]
        public void Integration_TreeBuilder_WithSeverityIconCallback_InvokedForEachVulnerabilityNode()
        {
            var vulns = CxAssistMockData.GetCommonVulnerabilities();
            var invokedSeverities = new List<string>();

            FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, loadSeverityIcon: sev =>
            {
                invokedSeverities.Add(sev);
                return null;
            });

            var problemCount = vulns.Count(v => CxAssistConstants.IsProblem(v.Severity));
            Assert.True(invokedSeverities.Count >= problemCount);
            Assert.Contains("High", invokedSeverities);
            Assert.Contains("Critical", invokedSeverities);
        }

        [Fact]
        public void Integration_TreeBuilder_WithFileIconCallback_InvokedPerFile()
        {
            var path1 = @"C:\src\a.cs";
            var path2 = @"C:\src\b.cs";
            var combined = new List<Vulnerability>
            {
                new Vulnerability("V1", "T", "D", SeverityLevel.High, ScannerType.OSS, 1, 1, path1),
                new Vulnerability("V2", "T", "D", SeverityLevel.Medium, ScannerType.OSS, 1, 1, path2)
            };
            var invokedPaths = new List<string>();

            FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(combined, loadFileIcon: p =>
            {
                invokedPaths.Add(p);
                return null;
            });

            Assert.Equal(2, invokedPaths.Count);
            Assert.Contains(path1, invokedPaths);
            Assert.Contains(path2, invokedPaths);
        }

        #endregion

        #region DockerCompose + ContainerImage mock flows

        [Fact]
        public void Integration_DockerComposeMock_ToTreeBuilder_ProducesContainersNodes()
        {
            var vulns = CxAssistMockData.GetDockerComposeMockVulnerabilities(@"C:\src\docker-compose.yml");
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.NotNull(fileNodes);
            if (fileNodes.Count > 0)
                Assert.All(fileNodes.SelectMany(f => f.Vulnerabilities), v => Assert.Equal(ScannerType.Containers, v.Scanner));
        }

        [Fact]
        public void Integration_ContainerImageMock_ToCoordinator_ThenTreeBuilder_Consistent()
        {
            ClearCoordinator();
            var path = @"C:\src\values.yaml";
            var vulns = CxAssistMockData.GetContainerImageMockVulnerabilities(path);
            var byFile = new Dictionary<string, List<Vulnerability>> { { path, vulns } };

            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);
            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodes = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current ?? new List<Vulnerability>());

            var problemCount = vulns.Count(v => CxAssistConstants.IsProblem(v.Severity));
            var treeCount = fileNodes.Sum(f => f.Vulnerabilities.Count);
            Assert.True(treeCount <= problemCount, "Container image mock has all findings on same line → one tree node.");
            Assert.True(treeCount >= 1, "At least one tree node for problem findings.");
        }

        #endregion

        #region BuildFileNodes from GetAllIssuesByFile

        [Fact]
        public void Integration_GetAllIssuesByFile_FlattenToCurrent_BuildFileNodes_SameAsFromCurrent()
        {
            ClearCoordinator();
            var packageJson = CxAssistMockData.GetPackageJsonMockVulnerabilities(@"C:\p\package.json");
            var pom = CxAssistMockData.GetPomMockVulnerabilities(@"C:\p\pom.xml");
            var byFile = new Dictionary<string, List<Vulnerability>>
            {
                { @"C:\p\package.json", packageJson },
                { @"C:\p\pom.xml", pom }
            };
            CxAssistDisplayCoordinator.SetFindingsByFile(byFile);

            var all = CxAssistDisplayCoordinator.GetAllIssuesByFile();
            var flattened = all.Values.SelectMany(list => list).ToList();
            var fileNodesFromAll = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(flattened);

            var current = CxAssistDisplayCoordinator.GetCurrentFindings();
            var fileNodesFromCurrent = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(current);

            Assert.Equal(fileNodesFromCurrent.Count, fileNodesFromAll.Count);
            Assert.Equal(
                fileNodesFromCurrent.Sum(f => f.Vulnerabilities.Count),
                fileNodesFromAll.Sum(f => f.Vulnerabilities.Count));
        }

        #endregion
    }
}
