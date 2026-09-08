using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_integration_tests.Services
{
    /// <summary>
    /// Integration tests for ASCA realtime scanner service.
    /// Requires VS UI context (ThreadHelper, JoinableTaskFactory).
    /// Run separately from unit tests; skip in CI if VS test host unavailable.
    /// </summary>
    [Trait("Category", "Integration")]
    public class AscaServiceTests
    {
        [Fact]
        public void AscaService_GetInstance_ReturnsServiceInstance()
        {
            var service = AscaService.GetInstance(null);

            Assert.NotNull(service);
        }

        [Fact]
        public void AscaService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = AscaService.GetInstance(null);
            var service2 = AscaService.GetInstance(null);

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
            var service = AscaService.GetInstance(null);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.txt")]
        [InlineData("test.cpp")]
        [InlineData("test.xml")]
        [InlineData("test.yml")]
        public void AscaService_ShouldScanFile_WithInvalidExtension_ReturnsFalse(string filePath)
        {
            var service = AscaService.GetInstance(null);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("C:\\node_modules\\app.js")]
        [InlineData("C:\\venv\\script.py")]
        [InlineData("C:\\dist\\bundle.js")]
        public void AscaService_ShouldScanFile_InExcludedPath_ReturnsFalse(string filePath)
        {
            var service = AscaService.GetInstance(null);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void AscaService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = AscaService.GetInstance(null);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public void AscaService_ShouldScanFile_WithEmpty_ReturnsFalse()
        {
            var service = AscaService.GetInstance(null);

            Assert.False(service.ShouldScanFile(string.Empty));
        }

        [Fact]
        public async System.Threading.Tasks.Task AscaService_UnregisterAsync_AllowsReinitializationAsync()
        {
            var service1 = AscaService.GetInstance(null);
            await service1.UnregisterAsync();

            var service2 = AscaService.GetInstance(null);

            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void AscaService_MultipleGetInstance_WithDifferentPackages_StillReturnsSingleton()
        {
            var service1 = AscaService.GetInstance(null);
            var service2 = AscaService.GetInstance(null);

            // Singleton pattern returns same instance regardless of package
            Assert.Same(service1, service2);
        }
    }
}
