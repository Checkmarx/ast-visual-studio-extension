using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_integration_tests.Services
{
    /// <summary>
    /// Integration tests for Containers realtime scanner service.
    /// Requires VS UI context (ThreadHelper, JoinableTaskFactory).
    /// Run separately from unit tests; skip in CI if VS test host unavailable.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ContainersServiceTests : IDisposable
    {
        private readonly CxWrapper _wrapperInstance;

        public ContainersServiceTests()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            _wrapperInstance = new CxWrapper(config, typeof(ContainersServiceTests));
        }

        public void Dispose()
        {
            // Cleanup
        }

        [Fact]
        public void ContainersService_GetInstance_ReturnsServiceInstance()
        {
            var service = ContainersService.GetInstance(_wrapperInstance, "docker");

            Assert.NotNull(service);
        }

        [Fact]
        public void ContainersService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = ContainersService.GetInstance(_wrapperInstance, "docker");
            var service2 = ContainersService.GetInstance(_wrapperInstance, "podman");

            // Singleton pattern returns same instance regardless of container engine
            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("docker-compose.yml")]
        [InlineData("C:\\helm\\values.yaml")]
        public void ContainersService_ShouldScanFile_WithValidType_ReturnsTrue(string filePath)
        {
            var service = ContainersService.GetInstance(_wrapperInstance, "docker");

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("main.tf")]
        [InlineData("package.json")]
        public void ContainersService_ShouldScanFile_WithInvalidType_ReturnsFalse(string filePath)
        {
            var service = ContainersService.GetInstance(_wrapperInstance, "docker");

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void ContainersService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = ContainersService.GetInstance(_wrapperInstance, "docker");

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task ContainersService_UnregisterAsync_AllowsReinitialization()
        {
            var service1 = ContainersService.GetInstance(_wrapperInstance, "docker");
            await service1.UnregisterAsync();

            var service2 = ContainersService.GetInstance(_wrapperInstance, "docker");

            Assert.NotSame(service1, service2);
        }

        [Theory]
        [InlineData("docker")]
        [InlineData("podman")]
        public void ContainersService_GetInstance_AcceptsContainerEngine(string engine)
        {
            var service = ContainersService.GetInstance(_wrapperInstance, engine);

            Assert.NotNull(service);
        }
    }
}
