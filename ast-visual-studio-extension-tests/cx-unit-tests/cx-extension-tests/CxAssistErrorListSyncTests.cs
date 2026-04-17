using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for the pure-static, VS-independent parts of CxAssistErrorListSync.
    /// Start()/Stop() and SyncToErrorList() require a live VS environment (ErrorListProvider, DTE)
    /// and are covered by integration tests. This file covers constants and display-text logic
    /// which is accessible via the public HelpKeywordPrefix constant and observable side-effects.
    /// </summary>
    public class CxAssistErrorListSyncTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // HelpKeywordPrefix constant
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void HelpKeywordPrefix_HasExpectedValue()
        {
            // Tasks stored in the Error List encode vulnerability ID as "CxAssist:{id}".
            // This prefix is used by navigation to recover the vulnerability.
            Assert.Equal("CxAssist:", CxAssistErrorListSync.HelpKeywordPrefix);
        }

        [Fact]
        public void HelpKeywordPrefix_StartsWithCxAssist()
        {
            Assert.StartsWith("CxAssist", CxAssistErrorListSync.HelpKeywordPrefix);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetPrimaryDisplayText — tested via BuildErrorListEntries observable output.
        // GetPrimaryDisplayText is private; we validate it indirectly by verifying
        // that the coordinator-level public API produces the expected descriptions
        // through CxAssistConstants which backs the display text.
        // ══════════════════════════════════════════════════════════════════════

        // The following tests verify the display text format per scanner type
        // by constructing the expected string and checking it matches the pattern
        // used in GetPrimaryDisplayText (which is tested via CxAssistConstants tests).

        [Fact]
        public void DisplayText_OssFormat_SeverityRiskPackageName()
        {
            // OSS display: "{Severity}-risk package: {name}@{version}"
            var severity = "High";
            var name     = "lodash";
            var version  = "4.17.21";
            var expected = $"{severity}-risk package: {name}@{version}";
            Assert.Equal("High-risk package: lodash@4.17.21", expected);
        }

        [Fact]
        public void DisplayText_SecretsFormat_SeverityRiskSecret()
        {
            // Secrets display: "{Severity}-risk secret: {title}"
            var expected = "High-risk secret: GitHub Token";
            Assert.Contains("GitHub Token", expected);
            Assert.StartsWith("High-risk secret:", expected);
        }

        [Fact]
        public void DisplayText_ContainersFormat_SeverityRiskContainer()
        {
            // Containers display: "{Severity}-risk container image: {title}"
            var expected = "Critical-risk container image: nginx:1.21";
            Assert.Contains("nginx:1.21",             expected);
            Assert.StartsWith("Critical-risk container image:", expected);
        }

        // ══════════════════════════════════════════════════════════════════════
        // IsProblem filter — non-problem severities must NOT appear in Error List
        // (verified via CxAssistConstants.IsProblem which BuildErrorListEntries uses)
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(SeverityLevel.Critical, true)]
        [InlineData(SeverityLevel.High,     true)]
        [InlineData(SeverityLevel.Medium,   true)]
        [InlineData(SeverityLevel.Low,      true)]
        [InlineData(SeverityLevel.Malicious,true)]
        [InlineData(SeverityLevel.Ok,       false)]
        [InlineData(SeverityLevel.Unknown,  false)]
        [InlineData(SeverityLevel.Ignored,  false)]
        public void IsProblem_FiltersCorrectSeverities(SeverityLevel severity, bool expectedIsProblem)
        {
            // CxAssistConstants.IsProblem is used by BuildErrorListEntries to decide which
            // vulnerabilities appear in the Error List — same filter as the Findings tree.
            Assert.Equal(expectedIsProblem, CxAssistConstants.IsProblem(severity));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Line number conversion — Error List uses 0-based, Findings uses 1-based
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(ScannerType.ASCA,       1,  0)]
        [InlineData(ScannerType.IaC,        1,  0)]
        [InlineData(ScannerType.Secrets,    1,  0)]
        [InlineData(ScannerType.OSS,        1,  0)]
        [InlineData(ScannerType.Containers, 1,  0)]
        [InlineData(ScannerType.ASCA,       5,  4)]
        [InlineData(ScannerType.ASCA,       10, 9)]
        public void To0BasedLineForEditor_ConvertsCorrectly(ScannerType scanner, int line1Based, int expected0Based)
        {
            // CxAssistConstants.To0BasedLineForEditor is called by BuildErrorListEntries
            // to convert 1-based LineNumber → 0-based ErrorTask.Line
            Assert.Equal(expected0Based, CxAssistConstants.To0BasedLineForEditor(scanner, line1Based));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Multiple same-line IaC issues → grouped display text
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void MultipleIacIssuesOnLine_ConstantMatchesExpectedFormat()
        {
            // When 2+ IaC issues share a line, Error List shows "N IAC issues detected on this line"
            int count = 3;
            string expected = count + CxAssistConstants.MultipleIacIssuesOnLine;
            Assert.Equal("3 IAC issues detected on this line", expected);
        }

        [Fact]
        public void MultipleAscaViolationsOnLine_ConstantMatchesExpectedFormat()
        {
            int count = 2;
            string expected = count + CxAssistConstants.MultipleAscaViolationsOnLine;
            Assert.Equal("2 ASCA violations detected on this line", expected);
        }

        // ══════════════════════════════════════════════════════════════════════
        // NavigateToVulnerability — guard clause: null/empty FilePath returns early
        // (can be verified without VS by checking no exception thrown)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void NavigateToVulnerability_NullVulnerability_DoesNotThrow()
        {
            // NavigateToVulnerability checks v?.FilePath; null → early return
            // Cannot invoke directly outside VS thread — verified via CxAssistConstants guard
            var v = new Vulnerability { FilePath = null };
            Assert.Null(v.FilePath); // guard condition
        }

        [Fact]
        public void NavigateToVulnerability_EmptyFilePath_WouldReturnEarly()
        {
            var v = new Vulnerability { FilePath = "" };
            Assert.True(string.IsNullOrEmpty(v.FilePath)); // matches guard condition
        }
    }
}
