using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxOneAssistFixPrompts (remediation prompt generation for all scanner types).
    /// </summary>
    public class CxOneAssistFixPromptsTests
    {
        #region BuildForVulnerability - Dispatch

        [Fact]
        public void BuildForVulnerability_Null_ReturnsNull()
        {
            Assert.Null(CxOneAssistFixPrompts.BuildForVulnerability(null));
        }

        [Fact]
        public void BuildForVulnerability_OssScanner_ReturnsSCAPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                PackageName = "lodash",
                PackageVersion = "4.17.19",
                PackageManager = "npm",
                Severity = SeverityLevel.High
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("lodash", result);
            Assert.Contains("4.17.19", result);
            Assert.Contains("npm", result);
            Assert.Contains("High", result);
        }

        [Fact]
        public void BuildForVulnerability_SecretsScanner_ReturnsSecretPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Secrets,
                Title = "generic-api-key",
                Description = "API key detected",
                Severity = SeverityLevel.Critical
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("generic-api-key", result);
            Assert.Contains("secret", result.ToLower());
        }

        [Fact]
        public void BuildForVulnerability_ContainersScanner_ReturnsContainerPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Containers,
                Title = "nginx",
                PackageVersion = "latest",
                Severity = SeverityLevel.Critical,
                FilePath = @"C:\src\dockerfile"
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("nginx", result);
            Assert.Contains("container", result.ToLower());
        }

        [Fact]
        public void BuildForVulnerability_IacScanner_ReturnsIACPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.IaC,
                Title = "Healthcheck Not Set",
                Description = "Missing healthcheck",
                Severity = SeverityLevel.Medium,
                FilePath = @"C:\src\main.tf",
                LineNumber = 10,
                ExpectedValue = "true",
                ActualValue = "false"
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("Healthcheck Not Set", result);
            Assert.Contains("IaC", result);
        }

        [Fact]
        public void BuildForVulnerability_AscaScanner_ReturnsASCAPrompt()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.ASCA,
                RuleName = "sql-injection",
                Description = "SQL Injection found",
                Severity = SeverityLevel.High,
                RemediationAdvice = "Use parameterized queries",
                LineNumber = 42
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);

            Assert.NotNull(result);
            Assert.Contains("sql-injection", result);
            Assert.Contains("parameterized queries", result);
        }

        #endregion

        #region SCA Prompt

        [Fact]
        public void BuildSCARemediationPrompt_ContainsAgentName()
        {
            var result = CxOneAssistFixPrompts.BuildSCARemediationPrompt("lodash", "4.17.19", "npm", "High");
            Assert.Contains("Checkmarx One Assist", result);
        }

        [Fact]
        public void BuildSCARemediationPrompt_ContainsPackageDetails()
        {
            var result = CxOneAssistFixPrompts.BuildSCARemediationPrompt("express", "4.18.0", "npm", "Critical");
            Assert.Contains("express@4.18.0", result);
            Assert.Contains("npm", result);
            Assert.Contains("Critical", result);
        }

        [Fact]
        public void BuildSCARemediationPrompt_ContainsRemediationSteps()
        {
            var result = CxOneAssistFixPrompts.BuildSCARemediationPrompt("lodash", "4.17.19", "npm", "High");
            Assert.Contains("Step 1", result);
            Assert.Contains("Step 2", result);
            Assert.Contains("PackageRemediation", result);
        }

        #endregion

        #region Secret Prompt

        [Fact]
        public void BuildSecretRemediationPrompt_ContainsSecretTitle()
        {
            var result = CxOneAssistFixPrompts.BuildSecretRemediationPrompt("aws-access-key", "Found AWS key", "Critical");
            Assert.Contains("aws-access-key", result);
            Assert.Contains("Critical", result);
        }

        [Fact]
        public void BuildSecretRemediationPrompt_NullDescription_DoesNotThrow()
        {
            var result = CxOneAssistFixPrompts.BuildSecretRemediationPrompt("api-key", null, "High");
            Assert.NotNull(result);
        }

        #endregion

        #region Containers Prompt

        [Fact]
        public void BuildContainersRemediationPrompt_ContainsImageDetails()
        {
            var result = CxOneAssistFixPrompts.BuildContainersRemediationPrompt("dockerfile", "nginx", "latest", "Critical");
            Assert.Contains("nginx:latest", result);
            Assert.Contains("dockerfile", result);
            Assert.Contains("imageRemediation", result);
        }

        #endregion

        #region IAC Prompt

        [Fact]
        public void BuildIACRemediationPrompt_ContainsAllFields()
        {
            var result = CxOneAssistFixPrompts.BuildIACRemediationPrompt(
                "Healthcheck Not Set", "Missing healthcheck", "Medium", "dockerfile", "true", "false", 9);

            Assert.Contains("Healthcheck Not Set", result);
            Assert.Contains("Medium", result);
            Assert.Contains("dockerfile", result);
            Assert.Contains("true", result);
            Assert.Contains("false", result);
            Assert.Contains("9", result);
        }

        [Fact]
        public void BuildIACRemediationPrompt_NullLineNumber_ShowsUnknown()
        {
            var result = CxOneAssistFixPrompts.BuildIACRemediationPrompt(
                "Issue", "Desc", "High", "tf", "expected", "actual", null);

            Assert.Contains("[unknown]", result);
        }

        #endregion

        #region ASCA Prompt

        [Fact]
        public void BuildASCARemediationPrompt_ContainsRuleAndAdvice()
        {
            var result = CxOneAssistFixPrompts.BuildASCARemediationPrompt(
                "sql-injection", "SQL injection detected", "High", "Use parameterized queries", 41);

            Assert.Contains("sql-injection", result);
            Assert.Contains("High", result);
            Assert.Contains("42", result);
            Assert.Contains("codeRemediation", result);
        }

        [Fact]
        public void BuildASCARemediationPrompt_NullLineNumber_ShowsUnknown()
        {
            var result = CxOneAssistFixPrompts.BuildASCARemediationPrompt(
                "rule", "desc", "Medium", "advice", null);

            Assert.Contains("[unknown]", result);
        }

        #endregion

        #region OSS Null Fields Fallback

        [Fact]
        public void BuildForVulnerability_OssNullPackageManager_DefaultsToNpm()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                PackageName = "pkg",
                PackageVersion = "1.0",
                PackageManager = null,
                Severity = SeverityLevel.High
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("npm", result);
        }

        [Fact]
        public void BuildForVulnerability_OssNullPackageName_FallsBackToTitle()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.OSS,
                Title = "vulnerable-pkg",
                PackageName = null,
                PackageVersion = "1.0",
                Severity = SeverityLevel.Medium
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("vulnerable-pkg", result);
        }

        [Fact]
        public void BuildForVulnerability_ContainersNullTitle_FallsBackToPackageNameOrImage()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Containers,
                Title = null,
                PackageName = "nginx",
                PackageVersion = "alpine",
                Severity = SeverityLevel.High,
                FilePath = @"C:\src\Dockerfile"
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("nginx", result);
            Assert.Contains("alpine", result);
        }

        [Fact]
        public void BuildForVulnerability_ContainersNullFilePath_UsesUnknownFileType()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Containers,
                Title = "base",
                FilePath = null,
                Severity = SeverityLevel.Medium
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("Unknown", result);
        }

        [Fact]
        public void BuildForVulnerability_IacLineNumberZero_PassesNullLine()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.IaC,
                Title = "Issue",
                Description = "Desc",
                Severity = SeverityLevel.Low,
                FilePath = @"C:\src\main.tf",
                LineNumber = 0,
                ExpectedValue = "x",
                ActualValue = "y"
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("[unknown]", result);
        }

        [Fact]
        public void BuildForVulnerability_AscaLineNumberZero_PassesNullLine()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.ASCA,
                RuleName = "rule",
                Description = "Desc",
                Severity = SeverityLevel.High,
                LineNumber = 0
            };

            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.Contains("[unknown]", result);
        }

        [Fact]
        public void BuildSCARemediationPrompt_ContainsIssueTypeInstructions()
        {
            var result = CxOneAssistFixPrompts.BuildSCARemediationPrompt("pkg", "1.0", "npm", "High");
            Assert.Contains("issueType", result);
            Assert.Contains("CVE", result);
            Assert.Contains("malicious", result);
        }

        [Fact]
        public void BuildSecretRemediationPrompt_ContainsCodeRemediationStep()
        {
            var result = CxOneAssistFixPrompts.BuildSecretRemediationPrompt("api-key", "Description", "Critical");
            Assert.Contains("codeRemediation", result);
            Assert.Contains("secret", result.ToLower());
        }

        [Fact]
        public void BuildForVulnerability_SecretsNullTitle_FallsBackToDescription()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.Secrets,
                Title = null,
                Description = "Hardcoded API key",
                Severity = SeverityLevel.High
            };
            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.NotNull(result);
            Assert.Contains("Hardcoded API key", result);
        }

        [Fact]
        public void BuildForVulnerability_IacNullTitle_FallsBackToRuleName()
        {
            var v = new Vulnerability
            {
                Scanner = ScannerType.IaC,
                Title = null,
                RuleName = "KICS_RULE_1",
                Description = "Desc",
                Severity = SeverityLevel.Medium,
                FilePath = @"C:\src\main.tf",
                LineNumber = 5
            };
            var result = CxOneAssistFixPrompts.BuildForVulnerability(v);
            Assert.NotNull(result);
            Assert.Contains("KICS_RULE_1", result);
        }

        [Fact]
        public void BuildContainersRemediationPrompt_ContainsStep3Output()
        {
            var result = CxOneAssistFixPrompts.BuildContainersRemediationPrompt("yaml", "nginx", "latest", "High");
            Assert.Contains("Step 3", result);
            Assert.Contains("OUTPUT", result);
        }

        [Fact]
        public void BuildSCARemediationPrompt_EmptyPackageName_StillBuilds()
        {
            var result = CxOneAssistFixPrompts.BuildSCARemediationPrompt("", "1.0", "npm", "High");
            Assert.NotNull(result);
            Assert.Contains("npm", result);
        }

        [Fact]
        public void BuildIACRemediationPrompt_ZeroLineNumber_ShowsUnknown()
        {
            var result = CxOneAssistFixPrompts.BuildIACRemediationPrompt("Issue", "Desc", "Low", "yaml", "exp", "act", 0);
            Assert.Contains("0", result);
        }

        [Fact]
        public void BuildASCARemediationPrompt_NullRemediationAdvice_StillBuilds()
        {
            var result = CxOneAssistFixPrompts.BuildASCARemediationPrompt("rule", "desc", "High", null, 1);
            Assert.NotNull(result);
            Assert.Contains("rule", result);
        }

        #endregion
    }
}
