using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_integration_tests.Services
{
    /// <summary>
    /// Integration tests for OSS realtime scanner service.
    /// Requires VS UI context (ThreadHelper, JoinableTaskFactory).
    /// Run separately from unit tests; skip in CI if VS test host unavailable.
    /// </summary>
    [Trait("Category", "Integration")]
    public class OssServiceTests : IDisposable
    {
        private readonly CxWrapper _wrapperInstance;

        public OssServiceTests()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            _wrapperInstance = new CxWrapper(config, typeof(OssServiceTests));
        }

        public void Dispose()
        {
            // Cleanup
        }

        [Fact]
        public void OssService_GetInstance_ReturnsServiceInstance()
        {
            var service = OssService.GetInstance(_wrapperInstance);

            Assert.NotNull(service);
        }

        [Fact]
        public void OssService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = OssService.GetInstance(_wrapperInstance);
            var service2 = OssService.GetInstance(_wrapperInstance);

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
            var service = OssService.GetInstance(_wrapperInstance);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("app.cs")]
        [InlineData("main.tf")]
        [InlineData("dockerfile")]
        public void OssService_ShouldScanFile_WithNonManifestFile_ReturnsFalse(string filePath)
        {
            var service = OssService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void OssService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = OssService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task OssService_UnregisterAsync_AllowsReinitializationAsync()
        {
            var service1 = OssService.GetInstance(_wrapperInstance);
            await service1.UnregisterAsync();

            var service2 = OssService.GetInstance(_wrapperInstance);

            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void OssService_ShouldScanFile_CaseInsensitive()
        {
            var service = OssService.GetInstance(_wrapperInstance);

            Assert.True(service.ShouldScanFile("PACKAGE.JSON"));
            Assert.True(service.ShouldScanFile("POM.XML"));
            Assert.True(service.ShouldScanFile("APP.CSPROJ"));
        }
    }
}
