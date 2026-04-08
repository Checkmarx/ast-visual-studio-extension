using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Services
{
    public class SecretsServiceTests : IDisposable
    {
        private readonly CxWrapper _wrapperInstance;

        public SecretsServiceTests()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            _wrapperInstance = new CxWrapper(config, typeof(SecretsServiceTests));
        }

        public void Dispose()
        {
            // Cleanup
        }

        [Fact]
        public void SecretsService_GetInstance_ReturnsServiceInstance()
        {
            var service = SecretsService.GetInstance(_wrapperInstance);

            Assert.NotNull(service);
        }

        [Fact]
        public void SecretsService_GetInstance_ReturnsSingletonInstance()
        {
            var service1 = SecretsService.GetInstance(_wrapperInstance);
            var service2 = SecretsService.GetInstance(_wrapperInstance);

            Assert.Same(service1, service2);
        }

        [Theory]
        [InlineData("config.txt")]
        [InlineData("app.js")]
        [InlineData("settings.py")]
        [InlineData("readme.md")]
        public void SecretsService_ShouldScanFile_WithTextFile_ReturnsTrue(string filePath)
        {
            var service = SecretsService.GetInstance(_wrapperInstance);

            Assert.True(service.ShouldScanFile(filePath));
        }

        [Theory]
        [InlineData("C:\\node_modules\\app.js")]
        [InlineData("C:\\project\\.git\\config")]
        public void SecretsService_ShouldScanFile_InExcludedPath_ReturnsFalse(string filePath)
        {
            var service = SecretsService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(filePath));
        }

        [Fact]
        public void SecretsService_ShouldScanFile_WithNull_ReturnsFalse()
        {
            var service = SecretsService.GetInstance(_wrapperInstance);

            Assert.False(service.ShouldScanFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task SecretsService_UnregisterAsync_AllowsReinitialization()
        {
            var service1 = SecretsService.GetInstance(_wrapperInstance);
            await service1.UnregisterAsync();

            var service2 = SecretsService.GetInstance(_wrapperInstance);

            // Should be different instances after unregister
            Assert.NotSame(service1, service2);
        }
    }
}
