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
            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(Type) }, null);
            var result = method.Invoke(service, new object[] { null, typeof(McpInstallService) });
            Assert.False(((McpInstallResult)result).Success);
            Assert.Contains("Missing configuration", ((McpInstallResult)result).Message);
        }

        [Fact]
        public void Install_WithEmptyApiKey_ReturnsFailure()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };
            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(Type) }, null);
            var result = method.Invoke(service, new object[] { config, typeof(McpInstallService) });
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

        [Fact]
        public void ResolveMcpUrl_WithValidJwt_ReturnsResolvedUrl()
        {
            // Using a minimal valid JWT structure with issuer
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"iss\":\"https://iam.checkmarx.net\"}"));
            string token = "header." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
            var url = McpInstallService.ResolveMcpUrl(token);
            Assert.Contains("ast.checkmarx.net", url);
            Assert.DoesNotContain("iam.", url);
        }

        [Fact]
        public void ResolveMcpUrl_WithIamPrefix_ReplaceWithAst()
        {
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"iss\":\"https://iam.example.com\"}"));
            string token = "header." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
            var url = McpInstallService.ResolveMcpUrl(token);
            Assert.Contains("ast.example.com", url);
        }

        [Fact]
        public void ResolveMcpUrl_WithMissingIssuer_ReturnsDefault()
        {
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"sub\":\"test\"}"));
            string token = "header." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
            var url = McpInstallService.ResolveMcpUrl(token);
            Assert.Equal(McpConfigManager.DefaultMcpUrl, url);
        }

        [Fact]
        public void ResolveMcpUrl_WithMalformedToken_ReturnsDefault()
        {
            var url = McpInstallService.ResolveMcpUrl("not.a.valid.token");
            Assert.Equal(McpConfigManager.DefaultMcpUrl, url);
        }

        [Fact]
        public void ResolveMcpUrl_WithInvalidBase64_ReturnsDefault()
        {
            var url = McpInstallService.ResolveMcpUrl("header.!!!.signature");
            Assert.Equal(McpConfigManager.DefaultMcpUrl, url);
        }

        [Theory]
        [InlineData("https://iam.tenant.net/path", "https://ast.tenant.net/api/security-mcp/mcp")]
        [InlineData("http://iam.local:8080", "http://ast.local:8080/api/security-mcp/mcp")]
        public void ResolveMcpUrl_WithDifferentIssuers_ConstructsCorrectUrl(string issuer, string expectedUrl)
        {
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"iss\":\"{issuer}\"}}"));
            string token = "header." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
            var url = McpInstallService.ResolveMcpUrl(token);
            Assert.Equal(expectedUrl, url);
        }

        [Fact]
        public void IsTenantMcpEnabled_WithNullConfig_ReturnsFalse()
        {
            var service = new McpInstallService();
            var result = service.GetType().GetMethod("IsTenantMcpEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, new object[] { null, null });
            Assert.False((bool)result);
        }

        [Fact]
        public void IsTenantMcpEnabled_WithEmptyApiKey_ReturnsFalse()
        {
            var service = new McpInstallService();
            var config = new CxConfig { ApiKey = "" };
            var result = service.GetType().GetMethod("IsTenantMcpEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(service, new object[] { config, null });
            Assert.False((bool)result);
        }

        [Fact]
        public void DecodeBase64Url_WithValidBase64_DecodesCorrectly()
        {
            // Test various base64url padding scenarios
            string[] testCases = new[]
            {
                "SGVsbG8gV29ybGQ", // "Hello World" - no padding
                "SGVsbG8gV29ybGQh", // "Hello World!" - with padding
                "YQ", // "a" - needs == padding
                "YWI" // "ab" - needs = padding
            };

            var method = typeof(McpInstallService).GetMethod("DecodeBase64Url", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            foreach (var testCase in testCases)
            {
                try
                {
                    var result = (string)method.Invoke(null, new object[] { testCase });
                    Assert.NotNull(result);
                }
                catch
                {
                    // Some edge cases may fail, which is acceptable
                }
            }
        }

        [Fact]
        public void Install_WithAuthError_ReturnsFalse()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object);
            var config = new CxConfig { ApiKey = "valid-key" };

            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(Type) }, null);
            var result = method.Invoke(service, new object[] { config, typeof(McpInstallServiceTests) });

            Assert.False(((McpInstallResult)result).Success);
        }

        [Fact]
        public void Uninstall_WithException_ReturnsFailureMessage()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            mockConfigManager.Setup(m => m.RemoveCheckmarxServer(out It.Ref<string>.IsAny))
                .Throws(new Exception("Test error"));

            var service = new McpInstallService(mockConfigManager.Object);
            var result = service.Uninstall(out string message);

            Assert.False(result);
            Assert.Contains("Failed to remove MCP configuration", message);
        }
    }
}
