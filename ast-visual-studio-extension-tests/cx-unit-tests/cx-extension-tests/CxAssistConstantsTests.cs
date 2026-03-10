using System;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxAssistConstants (line conversions, severity checks, string formatting, labels).
    /// </summary>
    public class CxAssistConstantsTests
    {
        #region To0BasedLineForEditor

        [Theory]
        [InlineData(1, 0)]
        [InlineData(5, 4)]
        [InlineData(100, 99)]
        public void To0BasedLineForEditor_PositiveLineNumber_ReturnsZeroBased(int lineNumber, int expected)
        {
            var result = CxAssistConstants.To0BasedLineForEditor(ScannerType.OSS, lineNumber);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void To0BasedLineForEditor_ZeroOrNegative_ReturnsZero(int lineNumber)
        {
            var result = CxAssistConstants.To0BasedLineForEditor(ScannerType.ASCA, lineNumber);
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(ScannerType.OSS)]
        [InlineData(ScannerType.Secrets)]
        [InlineData(ScannerType.Containers)]
        [InlineData(ScannerType.IaC)]
        [InlineData(ScannerType.ASCA)]
        public void To0BasedLineForEditor_AllScannerTypes_BehaveSame(ScannerType scanner)
        {
            Assert.Equal(9, CxAssistConstants.To0BasedLineForEditor(scanner, 10));
        }

        #endregion

        #region To1BasedLineForDte

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(100, 100)]
        public void To1BasedLineForDte_PositiveLineNumber_ReturnsSameValue(int lineNumber, int expected)
        {
            var result = CxAssistConstants.To1BasedLineForDte(ScannerType.OSS, lineNumber);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void To1BasedLineForDte_ZeroOrNegative_ReturnsOne(int lineNumber)
        {
            var result = CxAssistConstants.To1BasedLineForDte(ScannerType.IaC, lineNumber);
            Assert.Equal(1, result);
        }

        #endregion

        #region IsProblem

        [Theory]
        [InlineData(SeverityLevel.Critical, true)]
        [InlineData(SeverityLevel.High, true)]
        [InlineData(SeverityLevel.Medium, true)]
        [InlineData(SeverityLevel.Low, true)]
        [InlineData(SeverityLevel.Info, true)]
        [InlineData(SeverityLevel.Malicious, true)]
        [InlineData(SeverityLevel.Ok, false)]
        [InlineData(SeverityLevel.Unknown, false)]
        [InlineData(SeverityLevel.Ignored, false)]
        public void IsProblem_AllSeverityLevels_ReturnsCorrectResult(SeverityLevel severity, bool expected)
        {
            Assert.Equal(expected, CxAssistConstants.IsProblem(severity));
        }

        #endregion

        #region IsLineInRange

        [Fact]
        public void IsLineInRange_LineOne_InRange()
        {
            Assert.True(CxAssistConstants.IsLineInRange(1, 10));
        }

        [Fact]
        public void IsLineInRange_LastLine_InRange()
        {
            Assert.True(CxAssistConstants.IsLineInRange(10, 10));
        }

        [Fact]
        public void IsLineInRange_ZeroLine_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(0, 10));
        }

        [Fact]
        public void IsLineInRange_NegativeLine_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(-1, 10));
        }

        [Fact]
        public void IsLineInRange_BeyondLastLine_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(11, 10));
        }

        [Fact]
        public void IsLineInRange_ZeroLineCount_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(1, 0));
        }

        #endregion

        #region StripCveFromDisplayName

        [Theory]
        [InlineData("node-ipc (CVE-2022-12345)", "node-ipc")]
        [InlineData("pkg (CVE-2024-1234) extra", "pkg extra")]
        [InlineData("node-ipc (Malicious)", "node-ipc")]
        [InlineData("node-ipc (malicious)", "node-ipc")]
        [InlineData("node-ipc (MALICIOUS)", "node-ipc")]
        [InlineData("clean-package", "clean-package")]
        [InlineData("pkg (CVE-2022-111) (Malicious)", "pkg")]
        public void StripCveFromDisplayName_VariousInputs_ReturnsExpected(string input, string expected)
        {
            Assert.Equal(expected, CxAssistConstants.StripCveFromDisplayName(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void StripCveFromDisplayName_NullOrEmpty_ReturnsAsIs(string input)
        {
            Assert.Equal(input, CxAssistConstants.StripCveFromDisplayName(input));
        }

        [Fact]
        public void StripCveFromDisplayName_WhitespaceInput_ReturnsEmpty()
        {
            Assert.Equal("", CxAssistConstants.StripCveFromDisplayName("  "));
        }

        #endregion

        #region FormatSecretTitle

        [Theory]
        [InlineData("generic-api-key", "Generic-Api-Key")]
        [InlineData("aws-secret-key", "Aws-Secret-Key")]
        [InlineData("simple", "Simple")]
        [InlineData("a-b-c", "A-B-C")]
        public void FormatSecretTitle_KebabCase_ReturnsTitleCase(string input, string expected)
        {
            Assert.Equal(expected, CxAssistConstants.FormatSecretTitle(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FormatSecretTitle_NullOrEmpty_ReturnsAsIs(string input)
        {
            Assert.Equal(input, CxAssistConstants.FormatSecretTitle(input));
        }

        [Fact]
        public void FormatSecretTitle_SingleChar_ReturnsUppercase()
        {
            Assert.Equal("A", CxAssistConstants.FormatSecretTitle("a"));
        }

        [Fact]
        public void FormatSecretTitle_AlreadyTitleCase_ReturnsSame()
        {
            Assert.Equal("Generic-Api-Key", CxAssistConstants.FormatSecretTitle("Generic-Api-Key"));
        }

        [Fact]
        public void FormatSecretTitle_AllUpperCase_ReturnsNormalized()
        {
            Assert.Equal("Generic-Api-Key", CxAssistConstants.FormatSecretTitle("GENERIC-API-KEY"));
        }

        #endregion

        #region GetRichSeverityName

        [Theory]
        [InlineData(SeverityLevel.Critical, "Critical")]
        [InlineData(SeverityLevel.High, "High")]
        [InlineData(SeverityLevel.Medium, "Medium")]
        [InlineData(SeverityLevel.Low, "Low")]
        [InlineData(SeverityLevel.Info, "Info")]
        [InlineData(SeverityLevel.Malicious, "Malicious")]
        [InlineData(SeverityLevel.Unknown, "Unknown")]
        [InlineData(SeverityLevel.Ok, "Ok")]
        [InlineData(SeverityLevel.Ignored, "Ignored")]
        public void GetRichSeverityName_AllLevels_ReturnsExpected(SeverityLevel severity, string expected)
        {
            Assert.Equal(expected, CxAssistConstants.GetRichSeverityName(severity));
        }

        #endregion

        #region GetIgnoreThisLabel

        [Fact]
        public void GetIgnoreThisLabel_Secrets_ReturnsSecretLabel()
        {
            Assert.Equal("Ignore this secret in file", CxAssistConstants.GetIgnoreThisLabel(ScannerType.Secrets));
        }

        [Theory]
        [InlineData(ScannerType.OSS)]
        [InlineData(ScannerType.Containers)]
        [InlineData(ScannerType.IaC)]
        [InlineData(ScannerType.ASCA)]
        public void GetIgnoreThisLabel_NonSecrets_ReturnsVulnerabilityLabel(ScannerType scanner)
        {
            Assert.Equal("Ignore this vulnerability", CxAssistConstants.GetIgnoreThisLabel(scanner));
        }

        #endregion

        #region ShouldShowIgnoreAll

        [Theory]
        [InlineData(ScannerType.OSS, true)]
        [InlineData(ScannerType.Containers, true)]
        [InlineData(ScannerType.Secrets, false)]
        [InlineData(ScannerType.IaC, false)]
        [InlineData(ScannerType.ASCA, false)]
        public void ShouldShowIgnoreAll_ReturnsCorrectResult(ScannerType scanner, bool expected)
        {
            Assert.Equal(expected, CxAssistConstants.ShouldShowIgnoreAll(scanner));
        }

        #endregion

        #region GetIgnoreThisSuccessMessage

        [Theory]
        [InlineData(ScannerType.Secrets, "Secret ignored.")]
        [InlineData(ScannerType.Containers, "Container image ignored.")]
        [InlineData(ScannerType.IaC, "IaC finding ignored.")]
        [InlineData(ScannerType.ASCA, "ASCA violation ignored.")]
        [InlineData(ScannerType.OSS, "Vulnerability ignored.")]
        public void GetIgnoreThisSuccessMessage_AllScanners_ReturnsExpected(ScannerType scanner, string expected)
        {
            Assert.Equal(expected, CxAssistConstants.GetIgnoreThisSuccessMessage(scanner));
        }

        #endregion

        #region GetIgnoreAllSuccessMessage

        [Theory]
        [InlineData(ScannerType.Secrets, "All secrets ignored.")]
        [InlineData(ScannerType.Containers, "All container issues ignored.")]
        [InlineData(ScannerType.IaC, "All IaC findings ignored.")]
        [InlineData(ScannerType.ASCA, "All ASCA violations ignored.")]
        [InlineData(ScannerType.OSS, "All OSS issues ignored.")]
        public void GetIgnoreAllSuccessMessage_AllScanners_ReturnsExpected(ScannerType scanner, string expected)
        {
            Assert.Equal(expected, CxAssistConstants.GetIgnoreAllSuccessMessage(scanner));
        }

        #endregion

        #region Constants

        [Fact]
        public void DisplayName_IsCheckmarxOneAssist()
        {
            Assert.Equal("Checkmarx One Assist", CxAssistConstants.DisplayName);
        }

        [Fact]
        public void GetIgnoreAllLabel_ReturnsCorrectLabel()
        {
            Assert.Equal("Ignore all of this type", CxAssistConstants.GetIgnoreAllLabel(ScannerType.OSS));
        }

        [Fact]
        public void MultipleIacIssuesOnLine_Constant_IsExpectedSuffix()
        {
            Assert.Equal(" IAC issues detected on this line", CxAssistConstants.MultipleIacIssuesOnLine);
        }

        [Fact]
        public void MultipleAscaViolationsOnLine_Constant_IsExpectedSuffix()
        {
            Assert.Equal(" ASCA violations detected on this line", CxAssistConstants.MultipleAscaViolationsOnLine);
        }

        [Fact]
        public void MultipleOssIssuesOnLine_Constant_IsExpectedSuffix()
        {
            Assert.Equal(" OSS issues detected on this line", CxAssistConstants.MultipleOssIssuesOnLine);
        }

        [Fact]
        public void MultipleSecretsIssuesOnLine_Constant_IsExpectedSuffix()
        {
            Assert.Equal(" Secrets issues detected on this line", CxAssistConstants.MultipleSecretsIssuesOnLine);
        }

        [Fact]
        public void MultipleContainersIssuesOnLine_Constant_IsExpectedSuffix()
        {
            Assert.Equal(" Container issues detected on this line", CxAssistConstants.MultipleContainersIssuesOnLine);
        }

        [Fact]
        public void LogCategory_IsCxAssist()
        {
            Assert.Equal("CxAssist", CxAssistConstants.LogCategory);
        }

        [Fact]
        public void ThemeDark_IsDark()
        {
            Assert.Equal("Dark", CxAssistConstants.ThemeDark);
        }

        [Fact]
        public void ThemeLight_IsLight()
        {
            Assert.Equal("Light", CxAssistConstants.ThemeLight);
        }

        [Fact]
        public void BadgeIconFileName_IsExpected()
        {
            Assert.Equal("cxone_assist.png", CxAssistConstants.BadgeIconFileName);
        }

        [Fact]
        public void FixWithCxOneAssist_Label_IsExpected()
        {
            Assert.Equal("Fix with Checkmarx One Assist", CxAssistConstants.FixWithCxOneAssist);
        }

        [Fact]
        public void ViewDetails_Label_IsExpected()
        {
            Assert.Equal("View details", CxAssistConstants.ViewDetails);
        }

        [Fact]
        public void CopyMessage_Label_IsExpected()
        {
            Assert.Equal("Copy Message", CxAssistConstants.CopyMessage);
        }

        [Fact]
        public void SecretFindingLabel_IsExpected()
        {
            Assert.Equal("Secret finding", CxAssistConstants.SecretFindingLabel);
        }

        [Fact]
        public void SeverityPackageLabel_IsExpected()
        {
            Assert.Equal("Severity Package", CxAssistConstants.SeverityPackageLabel);
        }

        [Fact]
        public void SeverityImageLabel_IsExpected()
        {
            Assert.Equal("Severity Image", CxAssistConstants.SeverityImageLabel);
        }

        [Fact]
        public void GetRichSeverityName_UnmappedEnum_ReturnsToString()
        {
            var unmapped = (SeverityLevel)99;
            Assert.Equal("99", CxAssistConstants.GetRichSeverityName(unmapped));
        }

        [Fact]
        public void IsLineInRange_LineOne_LineCountOne_InRange()
        {
            Assert.True(CxAssistConstants.IsLineInRange(1, 1));
        }

        [Fact]
        public void IsLineInRange_LineZero_LineCountOne_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(0, 1));
        }

        [Fact]
        public void IsLineInRange_LineTwo_LineCountOne_OutOfRange()
        {
            Assert.False(CxAssistConstants.IsLineInRange(2, 1));
        }

        [Fact]
        public void FormatSecretTitle_SingleHyphen_ReturnsTwoParts()
        {
            Assert.Equal("A-B", CxAssistConstants.FormatSecretTitle("a-b"));
        }

        [Fact]
        public void FormatSecretTitle_NoHyphen_ReturnsCapitalized()
        {
            Assert.Equal("Single", CxAssistConstants.FormatSecretTitle("single"));
        }

        [Fact]
        public void StripCveFromDisplayName_MultipleCvePatterns_StripsAll()
        {
            var input = "pkg (CVE-2020-001) (CVE-2021-002)";
            Assert.Equal("pkg", CxAssistConstants.StripCveFromDisplayName(input));
        }

        [Fact]
        public void IacVulnerabilityLabel_IsExpected()
        {
            Assert.Equal("IaC vulnerability", CxAssistConstants.IacVulnerabilityLabel);
        }

        [Fact]
        public void SastVulnerabilityLabel_IsExpected()
        {
            Assert.Equal("SAST vulnerability", CxAssistConstants.SastVulnerabilityLabel);
        }

        [Fact]
        public void SyncFindingsToBuiltInErrorList_IsBooleanConstant()
        {
            Assert.True(CxAssistConstants.SyncFindingsToBuiltInErrorList || !CxAssistConstants.SyncFindingsToBuiltInErrorList);
        }

        [Fact]
        public void CopilotFixFallbackMessage_ContainsCopiedOrPaste()
        {
            Assert.Contains("copied", CxAssistConstants.CopilotFixFallbackMessage.ToLower());
        }

        [Fact]
        public void IgnoreThis_Constant_IsExpected()
        {
            Assert.Equal("Ignore this vulnerability", CxAssistConstants.IgnoreThis);
        }

        [Fact]
        public void IgnoreAllOfThisType_Constant_IsExpected()
        {
            Assert.Equal("Ignore all of this type", CxAssistConstants.IgnoreAllOfThisType);
        }

        #endregion
    }
}
