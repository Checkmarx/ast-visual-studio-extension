using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class AscaFileFilterStrategyTests
    {
        private readonly AscaFileFilterStrategy _filter = new();

        [Theory]
        [InlineData("test.java")]
        [InlineData("test.cs")]
        [InlineData("test.go")]
        [InlineData("test.py")]
        [InlineData("test.js")]
        [InlineData("test.jsx")]
        public void ShouldScanFile_WithValidExtension_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("test.cpp")]
        [InlineData("test.java.bak")]
        [InlineData("test.txt")]
        [InlineData("test")]
        public void ShouldScanFile_WithInvalidExtension_ReturnsFalse(string fileName)
        {
            Assert.False(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("C:\\node_modules\\test.js")]
        [InlineData("C:\\project\\node_modules\\app.cs")]
        [InlineData("C:\\venv\\script.py")]
        [InlineData("C:\\project\\.venv\\main.go")]
        [InlineData("C:\\dist\\bundle.js")]
        [InlineData("C:\\build\\app.java")]
        public void ShouldScanFile_InExcludedPaths_ReturnsFalse(string filePath)
        {
            Assert.False(_filter.ShouldScanFile(filePath));
        }

        [Fact]
        public void ShouldScanFile_WithNull_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(null));
        }

        [Fact]
        public void ShouldScanFile_WithEmpty_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(string.Empty));
        }

        [Fact]
        public void GetFilterDescription_ReturnsCorrectDescription()
        {
            var description = _filter.GetFilterDescription();
            Assert.Contains("ASCA", description);
            Assert.Contains("Java", description);
            Assert.Contains("C#", description);
        }

        [Fact]
        public void ShouldScanFile_CaseInsensitive_WithUppercase_ReturnsTrue()
        {
            Assert.True(_filter.ShouldScanFile("test.CS"));
            Assert.True(_filter.ShouldScanFile("test.PY"));
            Assert.True(_filter.ShouldScanFile("test.GO"));
        }
    }

    public class SecretsFileFilterStrategyTests
    {
        private readonly SecretsFileFilterStrategy _filter = new();

        [Theory]
        [InlineData("config.txt")]
        [InlineData("password.txt")]
        [InlineData("app.js")]
        [InlineData("readme.md")]
        public void ShouldScanFile_WithTextFile_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("C:\\node_modules\\test.js")]
        [InlineData("C:\\project\\node_modules\\app.txt")]
        [InlineData("C:\\project\\.git\\config")]
        public void ShouldScanFile_InExcludedPaths_ReturnsFalse(string filePath)
        {
            Assert.False(_filter.ShouldScanFile(filePath));
        }

        [Fact]
        public void ShouldScanFile_WithNull_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(null));
        }

        [Fact]
        public void GetFilterDescription_ReturnsCorrectDescription()
        {
            var description = _filter.GetFilterDescription();
            Assert.Contains("Secrets", description);
            Assert.Contains("MANIFEST", description);
        }
    }

    public class IacFileFilterStrategyTests
    {
        private readonly IacFileFilterStrategy _filter = new();

        [Theory]
        [InlineData("main.tf")]
        [InlineData("variables.tf")]
        [InlineData("config.yaml")]
        [InlineData("config.yml")]
        [InlineData("data.json")]
        [InlineData("schema.proto")]
        public void ShouldScanFile_WithValidExtension_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("Dockerfile")]
        [InlineData("dockerfile-prod")]
        [InlineData("dockerfile.dev")]
        [InlineData("docker-compose.yml")]
        [InlineData("docker-compose.yaml")]
        [InlineData("buildspec.yml")]
        [InlineData("buildspec.yaml")]
        public void ShouldScanFile_WithDockerOrCompose_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("variables.tfvars")]
        [InlineData("prod.auto.tfvars")]
        [InlineData("test.terraform.tfvars")]
        public void ShouldScanFile_WithTfvarsFile_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("test.cpp")]
        [InlineData("test.java")]
        [InlineData("docker.txt")]
        public void ShouldScanFile_WithInvalidType_ReturnsFalse(string fileName)
        {
            Assert.False(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Fact]
        public void ShouldScanFile_WithNull_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(null));
        }

        [Fact]
        public void GetFilterDescription_ReturnsCorrectDescription()
        {
            var description = _filter.GetFilterDescription();
            Assert.Contains("IaC", description);
            Assert.Contains("Terraform", description);
            Assert.Contains("Docker", description);
        }
    }

    public class ContainersFileFilterStrategyTests
    {
        private readonly ContainersFileFilterStrategy _filter = new();

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("Dockerfile")]
        [InlineData("dockerfile-prod")]
        [InlineData("dockerfile.dev")]
        public void ShouldScanFile_WithDockerfile_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("docker-compose.yml")]
        [InlineData("docker-compose.yaml")]
        [InlineData("docker-compose-prod.yml")]
        [InlineData("docker-compose-staging.yaml")]
        public void ShouldScanFile_WithDockerCompose_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("C:\\project\\helm\\values.yaml")]
        [InlineData("C:\\project\\helm\\templates\\service.yaml")]
        [InlineData("C:\\project\\helm\\templates\\deployment.yml")]
        public void ShouldScanFile_WithHelmChart_ReturnsTrue(string filePath)
        {
            Assert.True(_filter.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("C:\\project\\helm\\chart.yaml")]
        [InlineData("C:\\project\\helm\\chart.yml")]
        public void ShouldScanFile_WithHelmChartConfig_ReturnsFalse(string filePath)
        {
            Assert.False(_filter.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("docker.txt")]
        [InlineData("compose.yaml")]
        [InlineData("dockerfile.md")]
        public void ShouldScanFile_WithInvalidType_ReturnsFalse(string fileName)
        {
            Assert.False(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Fact]
        public void ShouldScanFile_WithNull_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(null));
        }

        [Fact]
        public void GetFilterDescription_ReturnsCorrectDescription()
        {
            var description = _filter.GetFilterDescription();
            Assert.Contains("Containers", description);
            Assert.Contains("Docker", description);
            Assert.Contains("Helm", description);
        }
    }

    public class OssFileFilterStrategyTests
    {
        private readonly OssFileFilterStrategy _filter = new();

        [Theory]
        [InlineData("directory.packages.props")]
        [InlineData("packages.config")]
        [InlineData("pom.xml")]
        [InlineData("package.json")]
        [InlineData("requirements.txt")]
        [InlineData("go.mod")]
        public void ShouldScanFile_WithManifestFile_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("app.csproj")]
        [InlineData("library.csproj")]
        public void ShouldScanFile_WithCsprojFile_ReturnsTrue(string fileName)
        {
            Assert.True(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Theory]
        [InlineData("package.txt")]
        [InlineData("pom.json")]
        [InlineData("go.mod.bak")]
        [InlineData("requirements.py")]
        public void ShouldScanFile_WithInvalidFile_ReturnsFalse(string fileName)
        {
            Assert.False(_filter.ShouldScanFile($"C:\\project\\{fileName}"));
        }

        [Fact]
        public void ShouldScanFile_WithNull_ReturnsFalse()
        {
            Assert.False(_filter.ShouldScanFile(null));
        }

        [Fact]
        public void GetFilterDescription_ReturnsCorrectDescription()
        {
            var description = _filter.GetFilterDescription();
            Assert.Contains("OSS", description);
            Assert.Contains("Dependency", description);
            Assert.Contains("package.json", description);
        }

        [Fact]
        public void ShouldScanFile_CaseInsensitive_WithUppercase_ReturnsTrue()
        {
            Assert.True(_filter.ShouldScanFile("C:\\project\\DIRECTORY.PACKAGES.PROPS"));
            Assert.True(_filter.ShouldScanFile("C:\\project\\PACKAGES.CONFIG"));
        }
    }

    public class FileFilterStrategyFactoryTests
    {
        [Fact]
        public void CreateStrategy_WithASCA_ReturnsAscaStrategy()
        {
            var strategy = FileFilterStrategyFactory.CreateStrategy("ASCA");
            Assert.IsType<AscaFileFilterStrategy>(strategy);
        }

        [Fact]
        public void CreateStrategy_WithSecrets_ReturnsSecretsStrategy()
        {
            var strategy = FileFilterStrategyFactory.CreateStrategy("Secrets");
            Assert.IsType<SecretsFileFilterStrategy>(strategy);
        }

        [Fact]
        public void CreateStrategy_WithIAC_ReturnsIacStrategy()
        {
            var strategy = FileFilterStrategyFactory.CreateStrategy("IAC");
            Assert.IsType<IacFileFilterStrategy>(strategy);
        }

        [Fact]
        public void CreateStrategy_WithContainers_ReturnsContainersStrategy()
        {
            var strategy = FileFilterStrategyFactory.CreateStrategy("Containers");
            Assert.IsType<ContainersFileFilterStrategy>(strategy);
        }

        [Fact]
        public void CreateStrategy_WithOSS_ReturnsOssStrategy()
        {
            var strategy = FileFilterStrategyFactory.CreateStrategy("OSS");
            Assert.IsType<OssFileFilterStrategy>(strategy);
        }

        [Fact]
        public void CreateStrategy_WithUnknownType_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
                FileFilterStrategyFactory.CreateStrategy("Unknown"));
        }

        [Fact]
        public void CreateStrategy_WithNull_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
                FileFilterStrategyFactory.CreateStrategy(null));
        }
    }
}
