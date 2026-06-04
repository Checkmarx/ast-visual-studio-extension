using System.Collections.Generic;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for ViewDetailsPrompts (explanation prompt generation for all scanner types).
    /// </summary>
    public class ViewDetailsPromptsTests
    {
        #region BuildForVulnerability - Dispatch

        [Fact]
        public void BuildForVulnerability_Null_ReturnsNull()
        {
            Assert.Null(ViewDetailsPrompts.BuildForVulnerability(null));
        }

        [Fact]
        public void BuildForVulnerability_OssScanner_ReturnsSCAExplanation()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "4.17.19",
                Severity = SeverityLevel.High,
                CveName = "CVE-2021-23337",
                Description = "Prototype pollution"
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("lodash", result);
            Assert.Contains("4.17.19", result);
            Assert.Contains("High", result);
        }

        [Fact]
        public void BuildForVulnerability_SecretsScanner_ReturnsSecretsExplanation()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Secrets,
                Title = "generic-api-key",
                Description = "API key found in code",
                Severity = SeverityLevel.Critical
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("generic-api-key", result);
            Assert.Contains("Critical", result);
            Assert.Contains("Do not change any code", result);
        }

        [Fact]
        public void BuildForVulnerability_ContainersScanner_ReturnsContainersExplanation()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Containers,
                Title = "nginx",
                PackageVersion = "latest",
                Severity = SeverityLevel.Critical,
                FilePath = @"C:\src\values.yaml"
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("nginx", result);
            Assert.Contains("container", result.ToLower());
        }

        [Fact]
        public void BuildForVulnerability_IacScanner_ReturnsIACExplanation()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.IaC,
                Title = "Healthcheck Not Set",
                Description = "Missing healthcheck",
                Severity = SeverityLevel.Medium,
                FilePath = @"C:\src\dockerfile",
                ExpectedValue = "defined",
                ActualValue = "undefined"
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("Healthcheck Not Set", result);
            Assert.Contains("Medium", result);
        }

        [Fact]
        public void BuildForVulnerability_AscaScanner_ReturnsASCAExplanation()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.ASCA,
                RuleName = "sql-injection",
                Description = "SQL Injection found",
                Severity = SeverityLevel.High
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("sql-injection", result);
            Assert.Contains("High", result);
        }

        #endregion

        #region SCA Explanation Prompt

        [Fact]
        public void BuildSCAExplanationPrompt_ContainsAgentName()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability { CveName = "CVE-2021-23337", Severity = SeverityLevel.High, Description = "Prototype pollution" }
            };
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("lodash", "4.17.19", "High", vulns);
            Assert.Contains("Checkmarx One Assist", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_ContainsDoNotChangeCode()
        {
            var vulns = new List<Vulnerability>();
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "Medium", vulns);
            Assert.Contains("Do not change anything in the code", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_MaliciousStatus_ShowsMaliciousWarning()
        {
            var vulns = new List<Vulnerability>();
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("evil-pkg", "1.0", "Malicious", vulns);
            Assert.Contains("Malicious Package Detected", result);
            Assert.Contains("Never install or use this package", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_WithCVEs_ListsThem()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability { CveName = "CVE-2021-001", Severity = SeverityLevel.High, Description = "Issue 1" },
                new Vulnerability { CveName = "CVE-2021-002", Severity = SeverityLevel.Medium, Description = "Issue 2" }
            };
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "High", vulns);
            Assert.Contains("CVE-2021-001", result);
            Assert.Contains("CVE-2021-002", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_EmptyVulns_ShowsNoCVEMessage()
        {
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "Medium", new List<Vulnerability>());
            Assert.Contains("No CVEs were provided", result);
        }

        #endregion

        #region Secrets Explanation Prompt

        [Fact]
        public void BuildSecretsExplanationPrompt_ContainsRiskBySeverity()
        {
            var result = ViewDetailsPrompts.BuildSecretsExplanationPrompt("api-key", "Found key", "Critical");
            Assert.Contains("Risk Understanding Based on Severity", result);
            Assert.Contains("Critical", result);
        }

        [Fact]
        public void BuildSecretsExplanationPrompt_NullDescription_DoesNotThrow()
        {
            var result = ViewDetailsPrompts.BuildSecretsExplanationPrompt("api-key", null, "High");
            Assert.NotNull(result);
        }

        #endregion

        #region Containers Explanation Prompt

        [Fact]
        public void BuildContainersExplanationPrompt_ContainsImageInfo()
        {
            var result = ViewDetailsPrompts.BuildContainersExplanationPrompt("dockerfile", "nginx", "1.24", "Critical");
            Assert.Contains("nginx:1.24", result);
            Assert.Contains("dockerfile", result);
            Assert.Contains("Critical", result);
        }

        #endregion

        #region IAC Explanation Prompt

        [Fact]
        public void BuildIACExplanationPrompt_ContainsAllFields()
        {
            var result = ViewDetailsPrompts.BuildIACExplanationPrompt(
                "Healthcheck Not Set", "Missing healthcheck", "Medium", "dockerfile", "defined", "undefined");

            Assert.Contains("Healthcheck Not Set", result);
            Assert.Contains("Medium", result);
            Assert.Contains("defined", result);
            Assert.Contains("undefined", result);
        }

        #endregion

        #region ASCA Explanation Prompt

        [Fact]
        public void BuildASCAExplanationPrompt_ContainsRuleAndSeverity()
        {
            var result = ViewDetailsPrompts.BuildASCAExplanationPrompt("sql-injection", "SQL injection found", "High");
            Assert.Contains("sql-injection", result);
            Assert.Contains("High", result);
        }

        #endregion

        #region Null Field Fallbacks

        [Fact]
        public void BuildForVulnerability_OssNullPackageName_FallsBackToTitle()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                Title = "vulnerable-pkg",
                PackageName = null,
                PackageVersion = "1.0",
                Severity = SeverityLevel.High
            };
            var result = ViewDetailsPrompts.BuildForVulnerability(v);
            Assert.Contains("vulnerable-pkg", result);
        }

        [Fact]
        public void BuildForVulnerability_ContainersNullFilePath_UsesUnknownFileType()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Containers,
                Title = "nginx",
                FilePath = null,
                Severity = SeverityLevel.High
            };
            var result = ViewDetailsPrompts.BuildForVulnerability(v);
            Assert.Contains("Unknown", result);
        }

        [Fact]
        public void BuildForVulnerability_IacNullFields_UsesEmptyStrings()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.IaC,
                Title = null,
                RuleName = null,
                Description = null,
                Severity = SeverityLevel.Low,
                FilePath = null,
                ExpectedValue = null,
                ActualValue = null
            };
            var result = ViewDetailsPrompts.BuildForVulnerability(v);
            Assert.NotNull(result);
        }

        #endregion

        #region SameLineVulns Parameter

        [Fact]
        public void BuildForVulnerability_OssWithSameLineVulns_PassedToPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "4.17.19",
                Severity = SeverityLevel.High
            };
            var sameLineVulns = new List<Vulnerability>
            {
                v,
                new Vulnerability { CveName = "CVE-2024-5678", Severity = SeverityLevel.Medium, Description = "Another issue" }
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v, sameLineVulns);
            Assert.Contains("CVE-2024-5678", result);
        }

        [Fact]
        public void BuildForVulnerability_OssSameLineVulnsNull_UsesSingleVulnerability()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "4.17.19",
                Severity = SeverityLevel.High,
                CveName = "CVE-2021-001",
                Description = "Prototype pollution"
            };

            var result = ViewDetailsPrompts.BuildForVulnerability(v, null);

            Assert.NotNull(result);
            Assert.Contains("lodash", result);
            Assert.Contains("CVE-2021-001", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_CveNameNull_UsesId()
        {
            var vulns = new List<Vulnerability>
            {
                new Vulnerability { Id = "POC-001", CveName = null, Severity = SeverityLevel.High, Description = "Issue" }
            };
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "High", vulns);
            Assert.Contains("POC-001", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_MoreThan20CVEs_ListsFirst20()
        {
            var vulns = new List<Vulnerability>();
            for (int i = 0; i < 25; i++)
                vulns.Add(new Vulnerability { CveName = $"CVE-2021-{i:D3}", Severity = SeverityLevel.High, Description = $"Issue {i}" });

            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "High", vulns);

            Assert.Contains("CVE-2021-000", result);
            Assert.Contains("CVE-2021-019", result);
        }

        [Fact]
        public void BuildASCAExplanationPrompt_ContainsComprehensiveExplanation()
        {
            var result = ViewDetailsPrompts.BuildASCAExplanationPrompt("xss", "XSS vulnerability", "High");
            Assert.Contains("xss", result);
            Assert.Contains("XSS vulnerability", result);
            Assert.Contains("Output Format Guidelines", result);
        }

        [Fact]
        public void BuildIACExplanationPrompt_NullExpectedActual_StillBuilds()
        {
            var result = ViewDetailsPrompts.BuildIACExplanationPrompt(
                "Issue", "Desc", "Medium", "yaml", "", "");
            Assert.Contains("Issue", result);
            Assert.Contains("yaml", result);
        }

        [Fact]
        public void BuildForVulnerability_SecretsNullTitle_FallsBackToDescription()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Secrets,
                Title = null,
                Description = "Detected secret in code",
                Severity = SeverityLevel.Critical
            };
            var result = ViewDetailsPrompts.BuildForVulnerability(v);
            Assert.NotNull(result);
            Assert.Contains("Detected secret in code", result);
        }

        [Fact]
        public void BuildSCAExplanationPrompt_NullVulnerabilities_DoesNotThrow()
        {
            var result = ViewDetailsPrompts.BuildSCAExplanationPrompt("pkg", "1.0", "High", null);
            Assert.NotNull(result);
            Assert.Contains("pkg", result);
        }

        [Fact]
        public void BuildSecretsExplanationPrompt_ContainsDoNotChangeCode()
        {
            var result = ViewDetailsPrompts.BuildSecretsExplanationPrompt("api-key", "Found key", "High");
            Assert.Contains("Do not change any code", result);
        }

        [Fact]
        public void BuildContainersExplanationPrompt_ContainsDoNotChangeCode()
        {
            var result = ViewDetailsPrompts.BuildContainersExplanationPrompt("dockerfile", "nginx", "latest", "Critical");
            Assert.Contains("Do not change anything", result);
        }

        [Fact]
        public void BuildForVulnerability_AscaNullRuleName_FallsBackToTitle()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.ASCA,
                RuleName = null,
                Title = "Fallback Title",
                Description = "Desc",
                Severity = SeverityLevel.High
            };
            var result = ViewDetailsPrompts.BuildForVulnerability(v);
            Assert.NotNull(result);
            Assert.Contains("Fallback Title", result);
        }

        [Fact]
        public void BuildIACExplanationPrompt_EmptyStrings_StillBuilds()
        {
            var result = ViewDetailsPrompts.BuildIACExplanationPrompt("", "", "Medium", "tf", "", "");
            Assert.NotNull(result);
            Assert.Contains("Medium", result);
        }

        #endregion
    }
}
