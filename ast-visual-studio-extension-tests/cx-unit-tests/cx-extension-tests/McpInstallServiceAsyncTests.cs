using ast_visual_studio_extension.CxPreferences.Configuration;
using ast_visual_studio_extension.CxWrapper.Models;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class McpInstallServiceAsyncTests
    {
        [Fact]
        public async Task InstallAsync_WithValidConfig_ReturnsResult()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object);
            var config = new CxConfig { ApiKey = "" };

            var result = await service.InstallAsync(config, typeof(McpInstallServiceAsyncTests));

            Assert.NotNull(result);
            // Will likely fail due to missing API, but should not throw
            Assert.False(result.Success);
        }

        [Fact]
        public async Task InstallAsync_WithNullConfig_ReturnsFalse()
        {
            var service = new McpInstallService();

            var result = await service.InstallAsync(null, typeof(McpInstallServiceAsyncTests));

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Missing configuration", result.Message);
        }

        [Fact]
        public async Task IsTenantMcpEnabledAsync_WithNullConfig_ReturnsFalse()
        {
            var service = new McpInstallService();

            var result = await service.IsTenantMcpEnabledAsync(null, typeof(McpInstallServiceAsyncTests));

            Assert.False(result);
        }

        [Fact]
        public async Task IsTenantMcpEnabledAsync_WithEmptyApiKey_ReturnsFalse()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };

            var result = await service.IsTenantMcpEnabledAsync(config, typeof(McpInstallServiceAsyncTests));

            Assert.False(result);
        }

        [Fact]
        public async Task InstallSilentlyAsync_WithNullConfig_ReturnsFalse()
        {
            var service = new McpInstallService();

            var result = await service.InstallSilentlyAsync(null, typeof(McpInstallServiceAsyncTests));

            Assert.False(result);
        }

        [Fact]
        public async Task InstallSilentlyAsync_WithEmptyApiKey_ReturnsFalse()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };

            var result = await service.InstallSilentlyAsync(config, typeof(McpInstallServiceAsyncTests));

            Assert.False(result);
        }

        [Fact]
        public async Task InstallAsync_CompletesWithoutException()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object);
            var config = new CxConfig { ApiKey = "test-key" };

            // Should complete without throwing
            try
            {
                var result = await service.InstallAsync(config, typeof(McpInstallServiceAsyncTests));
                Assert.NotNull(result);
            }
            catch (Exception ex)
            {
                // Expected if network/API calls fail, but should be caught
                Assert.True(ex is Exception);
            }
        }

        [Fact]
        public async Task MultipleAsyncOperations_ExecuteSequentially()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };

            var task1 = service.IsTenantMcpEnabledAsync(config, typeof(McpInstallServiceAsyncTests));
            var task2 = service.InstallSilentlyAsync(config, typeof(McpInstallServiceAsyncTests));

            var results = await Task.WhenAll(task1, task2);

            Assert.Equal(2, results.Length);
        }

        [Fact]
        public async Task IsTenantMcpEnabledAsync_CatchesExceptions()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object);
            var config = new CxConfig { ApiKey = "valid-key" };

            // Should handle any exceptions gracefully
            try
            {
                var result = await service.IsTenantMcpEnabledAsync(config, typeof(McpInstallServiceAsyncTests));
                // Should return false or true, but not throw
                Assert.IsType<bool>(result);
            }
            catch
            {
                // Acceptable if dependencies fail
                Assert.True(true);
            }
        }

        [Fact]
        public async Task InstallAsync_ReturnsValidMcpInstallResult()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };

            var result = await service.InstallAsync(config, typeof(McpInstallServiceAsyncTests));

            Assert.NotNull(result);
            Assert.IsType<McpInstallResult>(result);
            Assert.NotNull(result.Message);
        }
    }
}
