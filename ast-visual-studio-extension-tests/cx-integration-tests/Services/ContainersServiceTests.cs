using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_integration_tests.Services
{
    /// <summary>
    /// Integration tests for Containers realtime scanner service.
    /// Requires VS UI context (ThreadHelper, JoinableTaskFactory).
    /// Run separately from unit tests; skip in CI if VS test host unavailable.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ContainersServiceTests
    {
        [Fact]
        public void ContainersService_GetInstance_ReturnsServiceInstance()
        {
            var service = ContainersService.GetInstance(null, "docker");

            Assert.NotNull(service);
        }

        [Fact]
        public void ContainersService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = ContainersService.GetInstance(null, "docker");
            var service2 = ContainersService.GetInstance(null, "podman");

            // Singleton pattern returns same instance regardless of container engine
            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("docker-compose.yml")]
        [InlineData("C:\\helm\\values.yaml")]
        public void ContainersService_ShouldScanFile_WithValidType_ReturnsTrue(string filePath)
        {
            var service = ContainersService.GetInstance(null, "docker");

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("main.tf")]
        [InlineData("package.json")]
        public void ContainersService_ShouldScanFile_WithInvalidType_ReturnsFalse(string filePath)
        {
            var service = ContainersService.GetInstance(null, "docker");

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void ContainersService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = ContainersService.GetInstance(null, "docker");

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task ContainersService_UnregisterAsync_AllowsReinitializationAsync()
        {
            var service1 = ContainersService.GetInstance(null, "docker");
            await service1.UnregisterAsync();

            var service2 = ContainersService.GetInstance(null, "docker");

            Assert.NotSame(service1, service2);
        }

        [Theory]
        [InlineData("docker")]
        [InlineData("podman")]
        public void ContainersService_GetInstance_AcceptsContainerEngine(string engine)
        {
            var service = ContainersService.GetInstance(null, engine);

            Assert.NotNull(service);
        }
    }
}
