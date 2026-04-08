using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers;
using Moq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Services
{
    public class ContainersServiceTests
    {
        private readonly Mock<ast_visual_studio_extension.CxCLI.CxWrapper> _mockWrapper;

        public ContainersServiceTests()
        {
            _mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>();
        }

        [Fact]
        public void ContainersService_GetInstance_ReturnsServiceInstance()
        {
            var service = ContainersService.GetInstance(_mockWrapper.Object, "docker");

            Assert.NotNull(service);
        }

        [Fact]
        public void ContainersService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = ContainersService.GetInstance(_mockWrapper.Object, "docker");
            var service2 = ContainersService.GetInstance(_mockWrapper.Object, "podman");

            // Singleton pattern returns same instance regardless of container engine
            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("dockerfile")]
        [InlineData("docker-compose.yml")]
        [InlineData("C:\\helm\\values.yaml")]
        public void ContainersService_ShouldScanFile_WithValidType_ReturnsTrue(string filePath)
        {
            var service = ContainersService.GetInstance(_mockWrapper.Object, "docker");

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("main.tf")]
        [InlineData("package.json")]
        public void ContainersService_ShouldScanFile_WithInvalidType_ReturnsFalse(string filePath)
        {
            var service = ContainersService.GetInstance(_mockWrapper.Object, "docker");

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void ContainersService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = ContainersService.GetInstance(_mockWrapper.Object, "docker");

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task ContainersService_UnregisterAsync_AllowsReinitialization()
        {
            var service1 = ContainersService.GetInstance(_mockWrapper.Object, "docker");
            await service1.UnregisterAsync();

            var service2 = ContainersService.GetInstance(_mockWrapper.Object, "docker");

            Assert.NotSame(service1, service2);
        }

        [Theory]
        [InlineData("docker")]
        [InlineData("podman")]
        public void ContainersService_GetInstance_AcceptsContainerEngine(string engine)
        {
            var service = ContainersService.GetInstance(_mockWrapper.Object, engine);

            Assert.NotNull(service);
        }
    }
}
