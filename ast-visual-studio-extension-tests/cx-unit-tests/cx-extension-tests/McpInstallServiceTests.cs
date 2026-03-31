using ast_visual_studio_extension.CxPreferences.Configuration;
using ast_visual_studio_extension.CxWrapper.Models;
using Moq;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class McpInstallServiceTests
    {
        [Fact]
        public void Install_WithNullConfig_ReturnsFailure()
        {
            var service = new McpInstallService();
            var result = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, new object[] { null, typeof(McpInstallService) });
            Assert.False(((McpInstallResult)result).Success);
            Assert.Contains("Missing configuration", ((McpInstallResult)result).Message);
        }

        [Fact]
        public void Install_WithEmptyApiKey_ReturnsFailure()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };
            var result = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, new object[] { config, typeof(McpInstallService) });
            Assert.False(((McpInstallResult)result).Success);
            Assert.Contains("Please authenticate", ((McpInstallResult)result).Message);
        }

        [Fact]
        public void Uninstall_WhenNoConfig_ReturnsNoConfigMessage()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            string dummyPath = "dummy.json";
            mockConfigManager.Setup(m => m.RemoveCheckmarxServer(out dummyPath)).Returns(false);
            var service = new McpInstallService(mockConfigManager.Object);
            var result = service.Uninstall(out string message);
            Assert.False(result);
            Assert.Contains("No Checkmarx MCP configuration found", message);
        }

        [Fact]
        public void Uninstall_WhenConfigExists_ReturnsRemovedMessage()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            string dummyPath = "dummy.json";
            mockConfigManager.Setup(m => m.RemoveCheckmarxServer(out dummyPath)).Returns(true);
            var service = new McpInstallService(mockConfigManager.Object);
            var result = service.Uninstall(out string message);
            Assert.True(result);
            Assert.Contains("Removed Checkmarx MCP configuration", message);
        }

        [Fact]
        public void ResolveMcpUrl_WithInvalidApiKey_ReturnsDefault()
        {
            var url = McpInstallService.ResolveMcpUrl("");
            Assert.Equal(McpConfigManager.DefaultMcpUrl, url);
        }
    }
}
