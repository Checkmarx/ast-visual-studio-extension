using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca;
using ast_visual_studio_extension.CxWrapper.Models;
using Moq;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_integration_tests.Services
{
    /// <summary>
    /// Integration tests for ASCA realtime scanner service.
    /// Requires VS UI context (ThreadHelper, JoinableTaskFactory).
    /// Run separately from unit tests; skip in CI if VS test host unavailable.
    /// </summary>
    [Trait("Category", "Integration")]
    public class AscaServiceTests : IDisposable
    {
        private readonly CxWrapper _wrapperInstance;

        public AscaServiceTests()
        {
            // Create a real CxConfig for testing (doesn't make actual HTTP calls in unit tests)
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            // Create a real CxWrapper instance - it won't make actual calls unless explicitly invoked
            _wrapperInstance = new CxWrapper(config, typeof(AscaServiceTests));
        }

        public void Dispose()
        {
            // Cleanup: each test gets a fresh instance via xUnit test fixture
        }

        [Fact]
        public void AscaService_GetInstance_ReturnsServiceInstance()
        {
            // Get first instance
            var service1 = AscaService.GetInstance(_wrapperInstance);

            Assert.NotNull(service1);
        }

        [Fact]
        public void AscaService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = AscaService.GetInstance(_wrapperInstance);
            var service2 = AscaService.GetInstance(_wrapperInstance);

            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("test.cs")]
        [InlineData("test.java")]
        [InlineData("test.go")]
        [InlineData("test.py")]
        [InlineData("test.js")]
        [InlineData("test.jsx")]
        public void AscaService_ShouldScanFile_WithValidExtension_ReturnsTrue(string filePath)
        {
            var service = AscaService.GetInstance(_wrapperInstance);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.txt")]
        [InlineData("test.cpp")]
        [InlineData("test.xml")]
        [InlineData("test.yml")]
        public void AscaService_ShouldScanFile_WithInvalidExtension_ReturnsFalse(string filePath)
        {
            var service = AscaService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("C:\\node_modules\\app.js")]
        [InlineData("C:\\venv\\script.py")]
        [InlineData("C:\\dist\\bundle.js")]
        public void AscaService_ShouldScanFile_InExcludedPath_ReturnsFalse(string filePath)
        {
            var service = AscaService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void AscaService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = AscaService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public void AscaService_ShouldScanFile_WithEmpty_ReturnsFalse()
        {
            var service = AscaService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(string.Empty));
        }

        [Fact]
        public async System.Threading.Tasks.Task AscaService_UnregisterAsync_AllowsReinitializationAsync()
        {
            var service1 = AscaService.GetInstance(_wrapperInstance);
            await service1.UnregisterAsync();

            // After unregister, next GetInstance should create new instance
            var service2 = AscaService.GetInstance(_wrapperInstance);

            // Should be different instances after unregister
            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void AscaService_MultipleGetInstance_WithDifferentWrappers_StillReturnsSingleton()
        {
            var config1 = new CxConfig { ApiKey = "test1" };
            var config2 = new CxConfig { ApiKey = "test2" };
            var wrapper1 = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(config1, typeof(AscaServiceTests));
            var wrapper2 = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(config2, typeof(AscaServiceTests));

            var service1 = AscaService.GetInstance(wrapper1.Object);
            var service2 = AscaService.GetInstance(wrapper2.Object);

            // Singleton pattern returns same instance regardless of wrapper
            Assert.Same(service1, service2);
        }
    }
}
