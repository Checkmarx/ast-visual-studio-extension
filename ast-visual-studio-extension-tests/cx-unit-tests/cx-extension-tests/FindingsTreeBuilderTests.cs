using System.Collections.Generic;
using System.Linq;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for FindingsTreeBuilder (tree model construction for Findings window).
    /// </summary>
    public class FindingsTreeBuilderTests
    {
        #region Null/Empty Input

        [Fact]
        public void BuildFileNodes_NullList_ReturnsEmpty()
        {
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(null);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildFileNodes_EmptyList_ReturnsEmpty()
        {
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(new List<Vulnerability>());
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Non-Problem Severities Filtered Out

        [Theory]
        [InlineData(SeverityLevel.Ok)]
        [InlineData(SeverityLevel.Unknown)]
        [InlineData(SeverityLevel.Ignored)]
        public void BuildFileNodes_NonProblemSeverity_ReturnsEmpty(SeverityLevel severity)
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Test", "Desc", severity, ScannerType.OSS, 1, 1, @"C:\src\pom.xml")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Empty(result);
        }

        #endregion

        #region Single Vulnerability

        [Fact]
        public void BuildFileNodes_SingleOssVulnerability_CreatesOneFileNodeOneVulnNode()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "CVE-2024-1234", "Test vuln", SeverityLevel.High, ScannerType.OSS, 10, 1, @"C:\src\package.json")
                {
                    PackageName = "lodash",
                    PackageVersion = "4.17.19"
                }
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            var fileNode = result[0];
            Assert.Equal("package.json", fileNode.FileName);
            Assert.Equal(@"C:\src\package.json", fileNode.FilePath);
            Assert.Single(fileNode.Vulnerabilities);
            Assert.Equal("lodash", fileNode.Vulnerabilities[0].PackageName);
            Assert.Equal("4.17.19", fileNode.Vulnerabilities[0].PackageVersion);
        }

        [Fact]
        public void BuildFileNodes_SingleIacVulnerability_CreatesCorrectNode()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Healthcheck Not Set", "Missing healthcheck", SeverityLevel.Medium, ScannerType.IaC, 5, 1, @"C:\src\dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal(ScannerType.IaC, result[0].Vulnerabilities[0].Scanner);
            Assert.Equal("Healthcheck Not Set", result[0].Vulnerabilities[0].Description);
        }

        #endregion

        #region IaC Grouping By Line

        [Fact]
        public void BuildFileNodes_MultipleIacSameLine_GroupsIntoSingleRow()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.Medium, ScannerType.IaC, 5, 1, @"C:\src\dockerfile"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.High, ScannerType.IaC, 5, 1, @"C:\src\dockerfile"),
                new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Low, ScannerType.IaC, 5, 1, @"C:\src\dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Contains("3 IAC issues detected on this line", result[0].Vulnerabilities[0].Description);
        }

        [Fact]
        public void BuildFileNodes_IacDifferentLines_CreatesSeparateRows()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.Medium, ScannerType.IaC, 5, 1, @"C:\src\dockerfile"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.High, ScannerType.IaC, 10, 1, @"C:\src\dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(2, result[0].Vulnerabilities.Count);
        }

        #endregion

        #region Multi-File Grouping

        [Fact]
        public void BuildFileNodes_VulnerabilitiesInDifferentFiles_CreatesMultipleFileNodes()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.IaC, 5, 1, @"C:\src\dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Severity Counts

        [Fact]
        public void BuildFileNodes_MultipleSeverities_CreatesSeverityCounts()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.High, ScannerType.OSS, 2, 1, @"C:\src\package.json"),
                new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Medium, ScannerType.OSS, 3, 1, @"C:\src\package.json")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            var counts = result[0].SeverityCounts;
            Assert.True(counts.Count >= 1);
            Assert.Contains(counts, c => c.Severity == "High");
        }

        #endregion

        #region Default File Path

        [Fact]
        public void BuildFileNodes_NullFilePath_UsesDefaultFilePath()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 1, 1, null)
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(FindingsTreeBuilder.DefaultFilePath, result[0].FilePath);
        }

        [Fact]
        public void BuildFileNodes_EmptyFilePath_UsesDefaultFilePath()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 1, 1, "")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(FindingsTreeBuilder.DefaultFilePath, result[0].FilePath);
        }

        [Fact]
        public void BuildFileNodes_CustomDefaultFilePath_IsUsed()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 1, 1, null)
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, defaultFilePath: "custom.cs");

            Assert.Single(result);
            Assert.Equal("custom.cs", result[0].FilePath);
        }

        #endregion

        #region Ordering

        [Fact]
        public void BuildFileNodes_VulnerabilitiesOrderedByLineThenColumn()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 20, 1, @"C:\src\app.cs"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 5, 1, @"C:\src\app.cs"),
                new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Low, ScannerType.ASCA, 10, 1, @"C:\src\app.cs")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            var lines = result[0].Vulnerabilities.Select(v => v.Line).ToList();
            Assert.Equal(new[] { 5, 10, 20 }, lines);
        }

        #endregion

        #region All Scanner Types

        [Fact]
        public void BuildFileNodes_SecretsScanner_CreatesCorrectNode()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "generic-api-key", "Detected API key", SeverityLevel.Critical, ScannerType.Secrets, 15, 1, @"C:\src\config.py")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(ScannerType.Secrets, result[0].Vulnerabilities[0].Scanner);
        }

        [Fact]
        public void BuildFileNodes_ContainersScanner_CreatesCorrectNode()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "nginx:latest", "Vulnerable image", SeverityLevel.Critical, ScannerType.Containers, 1, 1, @"C:\src\values.yaml")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(ScannerType.Containers, result[0].Vulnerabilities[0].Scanner);
        }

        [Fact]
        public void BuildFileNodes_MixedScanners_GroupsByFile()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\file.cs"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 5, 1, @"C:\src\file.cs"),
                new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Low, ScannerType.Secrets, 10, 1, @"C:\src\file.cs")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Equal(3, result[0].Vulnerabilities.Count);
        }

        #endregion

        #region ASCA Grouping By Line

        [Fact]
        public void BuildFileNodes_MultipleAscaSameLine_ShowsHighestSeverity()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Rule1", "Low issue", SeverityLevel.Low, ScannerType.ASCA, 10, 1, @"C:\src\app.cs"),
                new Vulnerability("V2", "Rule2", "High issue", SeverityLevel.High, ScannerType.ASCA, 10, 1, @"C:\src\app.cs")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            // Multiple ASCA on same line -> only one node shown (highest severity)
            Assert.Single(result[0].Vulnerabilities);
        }

        #endregion

        #region OSS Grouping By Line

        [Fact]
        public void BuildFileNodes_MultipleOssSameLine_ShowsHighestSeverity()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "CVE-1", "Desc1", SeverityLevel.Medium, ScannerType.OSS, 5, 1, @"C:\src\package.json")
                { PackageName = "lodash", PackageVersion = "4.17.19" },
                new Vulnerability("V2", "CVE-2", "Desc2", SeverityLevel.Critical, ScannerType.OSS, 5, 1, @"C:\src\package.json")
                { PackageName = "lodash", PackageVersion = "4.17.19" }
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
        }

        #endregion

        #region Severity Icon Callback

        [Fact]
        public void BuildFileNodes_SeverityIconCallback_IsInvoked()
        {
            var invoked = new List<string>();
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json")
            };

            FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, loadSeverityIcon: (sev) =>
            {
                invoked.Add(sev);
                return null;
            });

            Assert.Contains("High", invoked);
        }

        [Fact]
        public void BuildFileNodes_NullCallbacks_DoesNotThrow()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, null, null);
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public void BuildFileNodes_FileIconCallback_IsInvokedPerFile()
        {
            var invokedPaths = new List<string>();
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.OSS, 1, 1, @"C:\src\package.json"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.IaC, 1, 1, @"C:\src\dockerfile")
            };

            FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, null, (path) =>
            {
                invokedPaths.Add(path);
                return null;
            });

            Assert.Equal(2, invokedPaths.Count);
            Assert.Contains(@"C:\src\package.json", invokedPaths);
            Assert.Contains(@"C:\src\dockerfile", invokedPaths);
        }

        [Fact]
        public void BuildFileNodes_MultipleSecretsSameLine_ShowsHighestSeverityOnly()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "api-key", "Desc1", SeverityLevel.Low, ScannerType.Secrets, 7, 1, @"C:\src\secrets.py"),
                new Vulnerability("V2", "api-key", "Desc2", SeverityLevel.Critical, ScannerType.Secrets, 7, 1, @"C:\src\secrets.py")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal("Critical", result[0].Vulnerabilities[0].Severity);
        }

        [Fact]
        public void BuildFileNodes_MultipleContainersSameLine_ShowsHighestSeverityOnly()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "nginx", "Desc1", SeverityLevel.Medium, ScannerType.Containers, 1, 1, @"C:\src\Dockerfile"),
                new Vulnerability("V2", "nginx", "Desc2", SeverityLevel.High, ScannerType.Containers, 1, 1, @"C:\src\Dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal("High", result[0].Vulnerabilities[0].Severity);
        }

        [Fact]
        public void BuildFileNodes_OrderingByLineThenColumn_RespectsColumn()
        {
            // ASCA groups by line (same line → one node). Use different lines to get 3 nodes and assert order by line then column.
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 10, 20, @"C:\src\app.cs"),
                new Vulnerability("V2", "Issue2", "Desc2", SeverityLevel.Medium, ScannerType.ASCA, 11, 5, @"C:\src\app.cs"),
                new Vulnerability("V3", "Issue3", "Desc3", SeverityLevel.Low, ScannerType.ASCA, 12, 15, @"C:\src\app.cs")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result);
            var nodes = result[0].Vulnerabilities;
            Assert.Equal(3, nodes.Count);
            Assert.Equal(10, nodes[0].Line);
            Assert.Equal(11, nodes[1].Line);
            Assert.Equal(12, nodes[2].Line);
            Assert.Equal(20, nodes[0].Column);
            Assert.Equal(5, nodes[1].Column);
            Assert.Equal(15, nodes[2].Column);
        }

        [Fact]
        public void BuildFileNodes_EmptyDefaultFilePath_UsesDefaultFilePathConstant()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue1", "Desc1", SeverityLevel.High, ScannerType.ASCA, 1, 1, null)
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns, defaultFilePath: "");

            Assert.Single(result);
            Assert.Equal(FindingsTreeBuilder.DefaultFilePath, result[0].FilePath);
        }

        [Fact]
        public void BuildFileNodes_IacSingleIssueOnLine_ShowsTitleNotCountMessage()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Healthcheck Not Set", "Missing healthcheck", SeverityLevel.Medium, ScannerType.IaC, 5, 1, @"C:\src\dockerfile")
            };

            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);

            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal("Healthcheck Not Set", result[0].Vulnerabilities[0].Description);
            Assert.DoesNotContain("issues detected on this line", result[0].Vulnerabilities[0].Description);
        }

        [Fact]
        public void BuildFileNodes_FileNodeWithoutExtension_FileNameUsesPath()
        {
            var path = @"C:\src\Dockerfile";
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Issue", "Desc", SeverityLevel.High, ScannerType.Containers, 1, 1, path)
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Single(result);
            Assert.Equal("Dockerfile", result[0].FileName);
            Assert.Equal(path, result[0].FilePath);
        }

        [Fact]
        public void BuildFileNodes_AllProblemSeverities_IncludedInTree()
        {
            var path = @"C:\src\file.cs";
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "T1", "D1", SeverityLevel.Critical, ScannerType.ASCA, 1, 1, path),
                new Vulnerability("V2", "T2", "D2", SeverityLevel.Info, ScannerType.ASCA, 2, 1, path),
                new Vulnerability("V3", "T3", "D3", SeverityLevel.Malicious, ScannerType.OSS, 3, 1, path)
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Single(result);
            Assert.Equal(3, result[0].Vulnerabilities.Count);
        }

        [Fact]
        public void DefaultFilePath_Constant_IsProgramCs()
        {
            Assert.Equal("Program.cs", FindingsTreeBuilder.DefaultFilePath);
        }

        [Fact]
        public void BuildFileNodes_MixedOkAndHigh_InTreeOnlyHigh()
        {
            var path = @"C:\src\package.json";
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "OK pkg", "No vuln", SeverityLevel.Ok, ScannerType.OSS, 1, 1, path),
                new Vulnerability("V2", "High pkg", "Vuln", SeverityLevel.High, ScannerType.OSS, 2, 1, path)
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Single(result);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal("High", result[0].Vulnerabilities[0].Severity);
        }

        [Fact]
        public void BuildFileNodes_AllOkSeverity_ReturnsEmpty()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "OK", "No vuln", SeverityLevel.Ok, ScannerType.OSS, 1, 1, @"C:\src\p.json")
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildFileNodes_AllUnknownSeverity_ReturnsEmpty()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Unknown", "Unknown", SeverityLevel.Unknown, ScannerType.OSS, 1, 1, @"C:\src\p.json")
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildFileNodes_AllIgnoredSeverity_ReturnsEmpty()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Ignored", "Ignored", SeverityLevel.Ignored, ScannerType.ASCA, 1, 1, @"C:\src\app.cs")
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildFileNodes_IacUsesTitleWhenDescriptionNull()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability("V1", "Rule Title", null, SeverityLevel.Medium, ScannerType.IaC, 1, 1, @"C:\src\dockerfile")
            };
            var result = FindingsTreeBuilder.BuildFileNodesFromVulnerabilities(vulns);
            Assert.Single(result[0].Vulnerabilities);
            Assert.Equal("Rule Title", result[0].Vulnerabilities[0].Description);
        }

        #endregion
    }
}
