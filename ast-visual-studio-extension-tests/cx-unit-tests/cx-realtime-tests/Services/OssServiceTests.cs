using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxWrapper.Models;
using Moq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Services
{
    public class OssServiceTests
    {
        private readonly Mock<CxWrapper> _mockWrapper;

        public OssServiceTests()
        {
            var mockConfig = new Mock<CxConfig>();
            _mockWrapper = new Mock<CxWrapper>(
                MockBehavior.Loose,
                mockConfig.Object,
                typeof(OssServiceTests));
        }

        [Fact]
        public void OssService_GetInstance_ReturnsServiceInstance()
        {
            var service = OssService.GetInstance(_mockWrapper.Object);

            Assert.NotNull(service);
        }

        [Fact]
        public void OssService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = OssService.GetInstance(_mockWrapper.Object);
            var service2 = OssService.GetInstance(_mockWrapper.Object);

            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("package.json")]
        [InlineData("pom.xml")]
        [InlineData("go.mod")]
        [InlineData("requirements.txt")]
        [InlineData("directory.packages.props")]
        [InlineData("app.csproj")]
        public void OssService_ShouldScanFile_WithManifestFile_ReturnsTrue(string filePath)
        {
            var service = OssService.GetInstance(_mockWrapper.Object);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("app.cs")]
        [InlineData("main.tf")]
        [InlineData("dockerfile")]
        public void OssService_ShouldScanFile_WithNonManifestFile_ReturnsFalse(string filePath)
        {
            var service = OssService.GetInstance(_mockWrapper.Object);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void OssService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = OssService.GetInstance(_mockWrapper.Object);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task OssService_UnregisterAsync_AllowsReinitialization()
        {
            var service1 = OssService.GetInstance(_mockWrapper.Object);
            await service1.UnregisterAsync();

            var service2 = OssService.GetInstance(_mockWrapper.Object);

            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void OssService_ShouldScanFile_CaseInsensitive()
        {
            var service = OssService.GetInstance(_mockWrapper.Object);

            Assert.True(service.ShouldScanFile("PACKAGE.JSON"));
            Assert.True(service.ShouldScanFile("POM.XML"));
            Assert.True(service.ShouldScanFile("APP.CSPROJ"));
        }
    }
}
