using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Services
{
    public class IacServiceTests : IDisposable
    {
        private readonly CxWrapper _wrapperInstance;

        public IacServiceTests()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            _wrapperInstance = new CxWrapper(config, typeof(IacServiceTests));
        }

        public void Dispose()
        {
            // Cleanup
        }

        [Fact]
        public void IacService_GetInstance_ReturnsServiceInstance()
        {
            var service = IacService.GetInstance(_wrapperInstance);

            Assert.NotNull(service);
        }

        [Fact]
        public void IacService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = IacService.GetInstance(_wrapperInstance);
            var service2 = IacService.GetInstance(_wrapperInstance);

            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("main.tf")]
        [InlineData("config.yaml")]
        [InlineData("data.json")]
        [InlineData("dockerfile")]
        public void IacService_ShouldScanFile_WithValidType_ReturnsTrue(string filePath)
        {
            var service = IacService.GetInstance(_wrapperInstance);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("test.py")]
        [InlineData("app.cs")]
        [InlineData("script.sh")]
        public void IacService_ShouldScanFile_WithInvalidType_ReturnsFalse(string filePath)
        {
            var service = IacService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void IacService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = IacService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task IacService_UnregisterAsync_AllowsReinitialization()
        {
            var service1 = IacService.GetInstance(_wrapperInstance);
            await service1.UnregisterAsync();

            var service2 = IacService.GetInstance(_wrapperInstance);

            Assert.NotSame(service1, service2);
        }
    }
}
