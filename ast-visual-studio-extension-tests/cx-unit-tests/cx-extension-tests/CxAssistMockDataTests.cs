using System.Collections.Generic;
using System.Linq;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxAssistMockData (mock vulnerability lists for POC/demo).
    /// </summary>
    public class CxAssistMockDataTests
    {
        #region Constants

        [Fact]
        public void DefaultFilePath_IsProgramCs()
        {
            Assert.Equal("Program.cs", CxAssistMockData.DefaultFilePath);
        }

        [Fact]
        public void QuickInfoOnlyVulnerabilityId_IsPoc007()
        {
            Assert.Equal("POC-007", CxAssistMockData.QuickInfoOnlyVulnerabilityId);
        }

        #endregion

        #region GetCommonVulnerabilities

        [Fact]
        public void GetCommonVulnerabilities_NullPath_UsesDefaultFilePath()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities(null);
            Assert.NotNull(list);
            Assert.All(list, v => Assert.Equal(CxAssistMockData.DefaultFilePath, v.FilePath));
        }

        [Fact]
        public void GetCommonVulnerabilities_EmptyPath_UsesDefaultFilePath()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities("");
            Assert.NotNull(list);
            Assert.All(list, v => Assert.Equal(CxAssistMockData.DefaultFilePath, v.FilePath));
        }

        [Fact]
        public void GetCommonVulnerabilities_CustomPath_AllUseCustomPath()
        {
            var path = @"C:\custom\file.cs";
            var list = CxAssistMockData.GetCommonVulnerabilities(path);
            Assert.NotNull(list);
            Assert.NotEmpty(list);
            Assert.All(list, v => Assert.Equal(path, v.FilePath));
        }

        [Fact]
        public void GetCommonVulnerabilities_ContainsExpectedSeveritiesAndScanners()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            var severities = list.Select(v => v.Severity).Distinct().ToList();
            var scanners = list.Select(v => v.Scanner).Distinct().ToList();

            Assert.Contains(SeverityLevel.Malicious, severities);
            Assert.Contains(SeverityLevel.Critical, severities);
            Assert.Contains(SeverityLevel.High, severities);
            Assert.Contains(SeverityLevel.Medium, severities);
            Assert.Contains(SeverityLevel.Low, severities);
            Assert.Contains(ScannerType.OSS, scanners);
            Assert.Contains(ScannerType.ASCA, scanners);
        }

        [Fact]
        public void GetCommonVulnerabilities_ContainsQuickInfoOnlyId()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            var quickInfoOnly = list.Where(v => v.Id == CxAssistMockData.QuickInfoOnlyVulnerabilityId).ToList();
            Assert.NotEmpty(quickInfoOnly);
        }

        [Fact]
        public void GetCommonVulnerabilities_AllIdsNonEmpty()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            Assert.All(list, v => Assert.False(string.IsNullOrEmpty(v.Id)));
        }

        [Fact]
        public void GetCommonVulnerabilities_LineNumbersPositive()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            Assert.All(list, v => Assert.True(v.LineNumber >= 1));
        }

        #endregion

        #region GetPackageJsonMockVulnerabilities

        [Fact]
        public void GetPackageJsonMockVulnerabilities_NullPath_UsesPackageJson()
        {
            var list = CxAssistMockData.GetPackageJsonMockVulnerabilities(null);
            Assert.NotNull(list);
            Assert.All(list, v => Assert.Equal("package.json", v.FilePath));
        }

        [Fact]
        public void GetPackageJsonMockVulnerabilities_ContainsOssAndOkSeverity()
        {
            var list = CxAssistMockData.GetPackageJsonMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
            Assert.All(list, v => Assert.Equal(ScannerType.OSS, v.Scanner));
            Assert.Contains(list, v => v.Severity == SeverityLevel.Ok);
        }

        #endregion

        #region GetPomMockVulnerabilities

        [Fact]
        public void GetPomMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetPomMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetPomMockVulnerabilities_CustomPath_AllUseCustomPath()
        {
            var path = @"C:\project\pom.xml";
            var list = CxAssistMockData.GetPomMockVulnerabilities(path);
            Assert.All(list, v => Assert.Equal(path, v.FilePath));
        }

        #endregion

        #region GetSecretsPyMockVulnerabilities

        [Fact]
        public void GetSecretsPyMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetSecretsPyMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetSecretsPyMockVulnerabilities_AllSecretsScanner()
        {
            var list = CxAssistMockData.GetSecretsPyMockVulnerabilities();
            Assert.All(list, v => Assert.Equal(ScannerType.Secrets, v.Scanner));
        }

        #endregion

        #region GetRequirementsMockVulnerabilities

        [Fact]
        public void GetRequirementsMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetRequirementsMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        #endregion

        #region GetIacMockVulnerabilities

        [Fact]
        public void GetIacMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetIacMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetIacMockVulnerabilities_AllIacScanner()
        {
            var list = CxAssistMockData.GetIacMockVulnerabilities();
            Assert.All(list, v => Assert.Equal(ScannerType.IaC, v.Scanner));
        }

        #endregion

        #region GetContainerMockVulnerabilities / GetContainerImageMockVulnerabilities

        [Fact]
        public void GetContainerMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetContainerMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetContainerMockVulnerabilities_AllContainersScanner()
        {
            var list = CxAssistMockData.GetContainerMockVulnerabilities();
            Assert.All(list, v => Assert.Equal(ScannerType.Containers, v.Scanner));
        }

        [Fact]
        public void GetContainerImageMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetContainerImageMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        #endregion

        #region GetDockerComposeMockVulnerabilities

        [Fact]
        public void GetDockerComposeMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetDockerComposeMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        #endregion

        #region GetDirectoryPackagesPropsMockVulnerabilities, GetGoModMockVulnerabilities, GetCsprojMockVulnerabilities

        [Fact]
        public void GetDirectoryPackagesPropsMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetDirectoryPackagesPropsMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetGoModMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetGoModMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetCsprojMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetCsprojMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetPackagesConfigMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetPackagesConfigMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetBuildGradleMockVulnerabilities_ReturnsNonEmptyList()
        {
            var list = CxAssistMockData.GetBuildGradleMockVulnerabilities();
            Assert.NotNull(list);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetPomMockVulnerabilities_ContainsMvnPackageManager()
        {
            var list = CxAssistMockData.GetPomMockVulnerabilities();
            var mvn = list.FirstOrDefault(v => v.PackageManager == "mvn");
            Assert.NotNull(mvn);
        }

        [Fact]
        public void GetPackageJsonMockVulnerabilities_ContainsNpmPackageManager()
        {
            var list = CxAssistMockData.GetPackageJsonMockVulnerabilities();
            var npm = list.FirstOrDefault(v => v.PackageManager == "npm");
            Assert.NotNull(npm);
        }

        [Fact]
        public void GetCommonVulnerabilities_ContainsAtLeastOneWithLocationsOrLineNumber()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            Assert.All(list, v => Assert.True(v.LineNumber >= 0));
        }

        [Fact]
        public void GetCommonVulnerabilities_AllHaveScannerSet()
        {
            var list = CxAssistMockData.GetCommonVulnerabilities();
            Assert.All(list, v => Assert.True(v.Scanner == ScannerType.OSS || v.Scanner == ScannerType.ASCA));
        }

        [Fact]
        public void GetIacMockVulnerabilities_CustomPath_AllUsePath()
        {
            var path = @"C:\iac\main.tf";
            var list = CxAssistMockData.GetIacMockVulnerabilities(path);
            Assert.All(list, v => Assert.Equal(path, v.FilePath));
        }

        [Fact]
        public void GetContainerImageMockVulnerabilities_AllContainersScanner()
        {
            var list = CxAssistMockData.GetContainerImageMockVulnerabilities();
            Assert.All(list, v => Assert.Equal(ScannerType.Containers, v.Scanner));
        }

        #endregion
    }
}
