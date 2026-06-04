using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxAssistScannerConstants (file pattern matching for OSS, Containers, IaC, Secrets, Helm).
    /// </summary>
    public class CxAssistScannerConstantsTests
    {
        #region NormalizePathForMatching

        [Fact]
        public void NormalizePathForMatching_BackslashesConverted()
        {
            Assert.Equal("C:/src/file.cs", CxAssistScannerConstants.NormalizePathForMatching(@"C:\src\file.cs"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NormalizePathForMatching_NullOrEmpty_ReturnsAsIs(string input)
        {
            Assert.Equal(input, CxAssistScannerConstants.NormalizePathForMatching(input));
        }

        [Fact]
        public void NormalizePathForMatching_ForwardSlashes_Unchanged()
        {
            Assert.Equal("C:/src/file.cs", CxAssistScannerConstants.NormalizePathForMatching("C:/src/file.cs"));
        }

        #endregion

        #region PassesBaseScanCheck

        [Fact]
        public void PassesBaseScanCheck_NormalPath_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.PassesBaseScanCheck(@"C:\project\src\file.cs"));
        }

        [Fact]
        public void PassesBaseScanCheck_NodeModulesForwardSlash_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.PassesBaseScanCheck("C:/project/node_modules/pkg/index.js"));
        }

        [Fact]
        public void PassesBaseScanCheck_NodeModulesBackslash_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.PassesBaseScanCheck(@"C:\project\node_modules\pkg\index.js"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void PassesBaseScanCheck_NullOrEmpty_ReturnsTrue(string path)
        {
            Assert.True(CxAssistScannerConstants.PassesBaseScanCheck(path));
        }

        #endregion

        #region IsManifestFile - OSS patterns

        [Theory]
        [InlineData("Directory.Packages.props")]
        [InlineData("packages.config")]
        [InlineData("pom.xml")]
        [InlineData("package.json")]
        [InlineData("requirements.txt")]
        [InlineData("go.mod")]
        public void IsManifestFile_KnownManifestFiles_ReturnsTrue(string fileName)
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile($@"C:\project\{fileName}"));
        }

        [Theory]
        [InlineData(@"C:\project\MyApp.csproj")]
        [InlineData(@"C:\project\src\Lib.csproj")]
        public void IsManifestFile_CsprojFiles_ReturnsTrue(string path)
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(path));
        }

        [Theory]
        [InlineData(@"C:\project\Program.cs")]
        [InlineData(@"C:\project\dockerfile")]
        [InlineData(@"C:\project\main.tf")]
        [InlineData(@"C:\project\app.py")]
        public void IsManifestFile_NonManifestFiles_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsManifestFile(path));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsManifestFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsManifestFile(path));
        }

        [Fact]
        public void IsManifestFile_CaseInsensitive_PomXml()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\project\POM.XML"));
        }

        [Fact]
        public void IsManifestFile_CaseInsensitive_PackageJson()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\project\PACKAGE.JSON"));
        }

        #endregion

        #region IsContainersFile

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("Dockerfile")]
        [InlineData("dockerfile-prod")]
        [InlineData("dockerfile.dev")]
        public void IsContainersFile_DockerfileVariants_ReturnsTrue(string fileName)
        {
            Assert.True(CxAssistScannerConstants.IsContainersFile($@"C:\project\{fileName}"));
        }

        [Theory]
        [InlineData("docker-compose.yml")]
        [InlineData("docker-compose.yaml")]
        [InlineData("docker-compose-prod.yml")]
        [InlineData("docker-compose-dev.yaml")]
        public void IsContainersFile_DockerComposeVariants_ReturnsTrue(string fileName)
        {
            Assert.True(CxAssistScannerConstants.IsContainersFile($@"C:\project\{fileName}"));
        }

        [Theory]
        [InlineData(@"C:\project\main.tf")]
        [InlineData(@"C:\project\package.json")]
        [InlineData(@"C:\project\app.py")]
        public void IsContainersFile_NonContainerFiles_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsContainersFile(path));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsContainersFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsContainersFile(path));
        }

        #endregion

        #region IsDockerFile

        [Theory]
        [InlineData("dockerfile", true)]
        [InlineData("Dockerfile", true)]
        [InlineData("dockerfile-prod", true)]
        [InlineData("docker-compose.yml", false)]
        [InlineData("main.tf", false)]
        public void IsDockerFile_VariousInputs_ReturnsExpected(string fileName, bool expected)
        {
            Assert.Equal(expected, CxAssistScannerConstants.IsDockerFile($@"C:\project\{fileName}"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsDockerFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsDockerFile(path));
        }

        #endregion

        #region IsDockerComposeFile

        [Theory]
        [InlineData("docker-compose.yml", true)]
        [InlineData("docker-compose.yaml", true)]
        [InlineData("docker-compose-prod.yml", true)]
        [InlineData("dockerfile", false)]
        [InlineData("package.json", false)]
        public void IsDockerComposeFile_VariousInputs_ReturnsExpected(string fileName, bool expected)
        {
            Assert.Equal(expected, CxAssistScannerConstants.IsDockerComposeFile($@"C:\project\{fileName}"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsDockerComposeFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsDockerComposeFile(path));
        }

        #endregion

        #region IsIacFile

        [Theory]
        [InlineData("main.tf")]
        [InlineData("vars.auto.tfvars")]
        [InlineData("prod.terraform.tfvars")]
        [InlineData("config.yaml")]
        [InlineData("config.yml")]
        [InlineData("template.json")]
        [InlineData("service.proto")]
        public void IsIacFile_IacExtensions_ReturnsTrue(string fileName)
        {
            Assert.True(CxAssistScannerConstants.IsIacFile($@"C:\project\{fileName}"));
        }

        [Fact]
        public void IsIacFile_Dockerfile_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\dockerfile"));
        }

        [Theory]
        [InlineData(@"C:\project\app.cs")]
        [InlineData(@"C:\project\main.py")]
        [InlineData(@"C:\project\index.js")]
        public void IsIacFile_NonIacFiles_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsIacFile(path));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsIacFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsIacFile(path));
        }

        #endregion

        #region IsHelmFile

        [Fact]
        public void IsHelmFile_YamlUnderHelm_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsHelmFile("C:/project/helm/templates/deployment.yaml"));
        }

        [Fact]
        public void IsHelmFile_YmlUnderHelm_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsHelmFile("C:/project/charts/helm/values.yml"));
        }

        [Fact]
        public void IsHelmFile_ChartYaml_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.IsHelmFile("C:/project/helm/chart.yaml"));
        }

        [Fact]
        public void IsHelmFile_ChartYml_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.IsHelmFile("C:/project/helm/chart.yml"));
        }

        [Fact]
        public void IsHelmFile_YamlNotUnderHelm_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.IsHelmFile("C:/project/config/deployment.yaml"));
        }

        [Fact]
        public void IsHelmFile_NonYamlUnderHelm_ReturnsFalse()
        {
            Assert.False(CxAssistScannerConstants.IsHelmFile("C:/project/helm/readme.md"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsHelmFile_NullOrEmpty_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsHelmFile(path));
        }

        #endregion

        #region IsExcludedForSecrets

        [Theory]
        [InlineData(@"C:\project\pom.xml")]
        [InlineData(@"C:\project\package.json")]
        [InlineData(@"C:\project\requirements.txt")]
        [InlineData(@"C:\project\MyApp.csproj")]
        public void IsExcludedForSecrets_ManifestFiles_ReturnsTrue(string path)
        {
            Assert.True(CxAssistScannerConstants.IsExcludedForSecrets(path));
        }

        [Fact]
        public void IsExcludedForSecrets_CheckmarxIgnoredForwardSlash_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsExcludedForSecrets("C:/project/.vscode/.checkmarxIgnored"));
        }

        [Fact]
        public void IsExcludedForSecrets_CheckmarxIgnoredTempList_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsExcludedForSecrets(@"C:\project\.vscode\.checkmarxIgnoredTempList"));
        }

        [Theory]
        [InlineData(@"C:\project\app.cs")]
        [InlineData(@"C:\project\config.yaml")]
        [InlineData(@"C:\project\main.py")]
        public void IsExcludedForSecrets_RegularFiles_ReturnsFalse(string path)
        {
            Assert.False(CxAssistScannerConstants.IsExcludedForSecrets(path));
        }

        #endregion

        #region ManifestFilePatterns and Constants

        [Fact]
        public void ManifestFilePatterns_ContainsExpectedEntries()
        {
            var patterns = CxAssistScannerConstants.ManifestFilePatterns;
            Assert.Contains("Directory.Packages.props", patterns);
            Assert.Contains("packages.config", patterns);
            Assert.Contains("pom.xml", patterns);
            Assert.Contains("package.json", patterns);
            Assert.Contains("requirements.txt", patterns);
            Assert.Contains("go.mod", patterns);
            Assert.Equal(6, patterns.Count);
        }

        [Fact]
        public void ManifestCsprojSuffix_IsCsproj()
        {
            Assert.Equal(".csproj", CxAssistScannerConstants.ManifestCsprojSuffix);
        }

        [Fact]
        public void IacFileExtensions_ContainsExpectedExtensions()
        {
            var exts = CxAssistScannerConstants.IacFileExtensions;
            Assert.Contains("tf", exts);
            Assert.Contains("yaml", exts);
            Assert.Contains("yml", exts);
            Assert.Contains("json", exts);
            Assert.Contains("proto", exts);
            Assert.Contains("dockerfile", exts);
        }

        [Fact]
        public void IsManifestFile_GoMod_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\project\go.mod"));
        }

        [Fact]
        public void IsManifestFile_DirectoryPackagesProps_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\src\Directory.Packages.props"));
        }

        [Fact]
        public void IsIacFile_TfExtension_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\main.tf"));
        }

        [Fact]
        public void IsIacFile_ProtoExtension_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\service.proto"));
        }

        [Fact]
        public void IsIacFile_JsonExtension_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\template.json"));
        }

        [Fact]
        public void IsContainersFile_DockerComposeDevYaml_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsContainersFile(@"C:\project\docker-compose-dev.yaml"));
        }

        [Fact]
        public void DockerfileLiteral_IsDockerfile()
        {
            Assert.Equal("dockerfile", CxAssistScannerConstants.DockerfileLiteral);
        }

        [Fact]
        public void DockerComposeLiteral_IsDockerCompose()
        {
            Assert.Equal("docker-compose", CxAssistScannerConstants.DockerComposeLiteral);
        }

        [Fact]
        public void IacAutoTfvarsSuffix_IsExpected()
        {
            Assert.Equal(".auto.tfvars", CxAssistScannerConstants.IacAutoTfvarsSuffix);
        }

        [Fact]
        public void IacTerraformTfvarsSuffix_IsExpected()
        {
            Assert.Equal(".terraform.tfvars", CxAssistScannerConstants.IacTerraformTfvarsSuffix);
        }

        [Fact]
        public void HelmPathSegment_IsExpected()
        {
            Assert.Equal("/helm/", CxAssistScannerConstants.HelmPathSegment);
        }

        [Fact]
        public void ContainerHelmExtensions_ContainsYmlAndYaml()
        {
            var exts = CxAssistScannerConstants.ContainerHelmExtensions;
            Assert.Contains("yml", exts);
            Assert.Contains("yaml", exts);
        }

        [Fact]
        public void ContainerHelmExcludedFiles_ContainsChartYamlAndYml()
        {
            var excluded = CxAssistScannerConstants.ContainerHelmExcludedFiles;
            Assert.Contains("chart.yml", excluded);
            Assert.Contains("chart.yaml", excluded);
        }

        [Fact]
        public void NodeModulesPathSegment_IsExpected()
        {
            Assert.Equal("/node_modules/", CxAssistScannerConstants.NodeModulesPathSegment);
        }

        [Fact]
        public void IsExcludedForSecrets_BackslashCheckmarxIgnored_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsExcludedForSecrets(@"C:\project\.vscode\.checkmarxIgnored"));
        }

        [Fact]
        public void IsIacFile_AutoTfvars_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\vars.auto.tfvars"));
        }

        [Fact]
        public void IsIacFile_TerraformTfvars_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsIacFile(@"C:\project\prod.terraform.tfvars"));
        }

        [Fact]
        public void IsManifestFile_RequirementsTxt_ReturnsTrue()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\project\requirements.txt"));
        }

        [Fact]
        public void IsManifestFile_DirectoryPackagesProps_CaseInsensitive()
        {
            Assert.True(CxAssistScannerConstants.IsManifestFile(@"C:\project\directory.packages.props"));
        }

        #endregion
    }
}
