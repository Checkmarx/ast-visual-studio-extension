using ast_visual_studio_extension.CxPreferences.Configuration;
using ast_visual_studio_extension.CxWrapper.Models;
using Moq;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
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
        public void ResolveMcpUrl_WithDevIamIssuer_MapsToAstMasterComponents()
        {
            // The IAM host returns 404 for /api/security-mcp; API keys work on the AST host's base endpoint.
            string token = BuildToken("https://iam-dev.dev.cxast.net/auth/realms/master-sypher");
            var url = McpInstallService.ResolveMcpUrl(token);
            Assert.Equal("https://ast-master-components.dev.cxast.net/api/security-mcp/mcp", url);
        }

        [Fact]
        public void ResolveMcpUrl_WithRegionalIamIssuer_MapsToRegionalAstHost()
        {
            var url = McpInstallService.ResolveMcpUrl(BuildToken("https://eu.iam.checkmarx.net/auth/realms/cx_seg"));
            Assert.Equal("https://eu.ast.checkmarx.net/api/security-mcp/mcp", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithDevTenantRealmPath_ResolvesTenantScopedMcpEndpoint()
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth(
                BuildToken("https://iam-dev.dev.cxast.net/auth/realms/master-sypher"), null, null, out string url, out _);
            Assert.True(ok);
            Assert.Equal("https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithPlainIamIssuer_ReplacesWithAst()
        {
            McpInstallService.TryResolveMcpUrlForOAuth(
                BuildToken("https://iam.checkmarx.net/auth/realms/tenant1"), null, null, out string url, out _);
            Assert.Equal("https://ast.checkmarx.net/api/security-mcp/mcp/tenant1", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithoutRealmInIssuer_AsksForTenantInsteadOfUsingBaseEndpoint()
        {
            // The base endpoint's protected-resource metadata has no authorization server, so OAuth cannot work there.
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth(
                BuildToken("https://iam-dev.dev.cxast.net"), null, null, out string url, out string error);
            Assert.False(ok);
            Assert.Null(url);
            Assert.Contains("tenant", error);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithUnreadableApiKeyAndNoOverrides_DoesNotFallBackToDevUrl()
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth("not-a-jwt", null, null, out string url, out string error);
            Assert.False(ok);
            Assert.Null(url);
            Assert.Contains("server URL", error);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithUnreadableApiKeyButBothOverrides_Succeeds()
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth("not-a-jwt", "https://eu.ast.checkmarx.net", "cx_seg", out string url, out _);
            Assert.True(ok);
            Assert.Equal("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", url);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("https://eu.ast.checkmarx.net", "")]
        [InlineData("", "cx_seg")]
        public void TryValidateOAuthOverrides_AcceptsBlankOrValidValues(string serverUrl, string tenant)
        {
            Assert.True(McpInstallService.TryValidateOAuthOverrides(serverUrl, tenant, out string error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("https://eu.ast.checkmarx.net/a&calc", "cx_seg")]
        [InlineData("https://eu.ast.checkmarx.net/a%20b", "cx_seg")]
        [InlineData("https://user:pw@eu.ast.checkmarx.net", "cx_seg")]
        [InlineData("https://eu.ast.checkmarx.net", "t&x")]
        public void TryValidateOAuthOverrides_RejectsCommandLineUnsafeInput(string serverUrl, string tenant)
        {
            Assert.False(McpInstallService.TryValidateOAuthOverrides(serverUrl, tenant, out string error));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher", true)]
        [InlineData("https://eu.ast.checkmarx.net:8443/api/security-mcp/mcp/cx_seg", true)]
        [InlineData("http://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", false)]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg&whoami", false)]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg\" & calc", false)]
        [InlineData("https://eu.ast.checkmarx.net", false)]
        [InlineData("", false)]
        public void IsSafeOAuthMcpUrl_AllowsOnlyPlainHttpsUrls(string url, bool expected)
        {
            Assert.Equal(expected, McpInstallService.IsSafeOAuthMcpUrl(url));
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
                var result = (string)method.Invoke(null, new object[] { testCase });
                Assert.NotNull(result);
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
        public void Install_WithOAuthModeAndAuthError_ReturnsFalse()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object);
            var config = new CxConfig { ApiKey = "valid-key" };

            var settings = new McpConnectionSettings(McpAuthMode.OAuth, null, null);

            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(McpConnectionSettings), typeof(Type) }, null);
            var result = method.Invoke(service, new object[] { config, settings, typeof(McpInstallServiceTests) });

            // Auth against the real CxWrapper will fail in a unit test environment regardless of auth mode.
            Assert.False(((McpInstallResult)result).Success);
        }

        [Fact]
        public void Install_ExplicitWithoutNpx_FailsWithNodeMessageBeforeTouchingConfig()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object, new Mock<McpRemoteSessionCache>().Object, () => false);
            var config = new CxConfig { ApiKey = "valid-key" };
            var settings = new McpConnectionSettings(McpAuthMode.OAuth, "https://eu.ast.checkmarx.net", "cx_seg");

            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(McpConnectionSettings), typeof(Type) }, null);
            var result = (McpInstallResult)method.Invoke(service, new object[] { config, settings, typeof(McpInstallServiceTests) });

            Assert.False(result.Success);
            Assert.Equal(McpPrerequisites.NPX_MISSING_MESSAGE, result.Message);
            string ignored;
            mockConfigManager.Verify(m => m.InstallOrUpdateOAuth(It.IsAny<string>(), out ignored), Times.Never);
        }

        [Fact]
        public void Uninstall_ClearsTheOAuthSessionOfTheInstalledEntry()
        {
            const string oauthUrl = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher";
            var mockConfigManager = new Mock<McpConfigManager>();
            string dummyPath = "dummy.json";
            mockConfigManager.Setup(m => m.GetInstalledOAuthServerUrl()).Returns(oauthUrl);
            mockConfigManager.Setup(m => m.RemoveCheckmarxServer(out dummyPath)).Returns(true);
            var mockSessionCache = new Mock<McpRemoteSessionCache>();
            var service = new McpInstallService(mockConfigManager.Object, mockSessionCache.Object, () => true);

            Assert.True(service.Uninstall(out _));

            mockSessionCache.Verify(c => c.ClearTokens(oauthUrl), Times.Once);
        }

        [Fact]
        public void Uninstall_WithApiKeyEntry_DoesNotTouchOAuthSessions()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            string dummyPath = "dummy.json";
            mockConfigManager.Setup(m => m.RemoveCheckmarxServer(out dummyPath)).Returns(true);
            var mockSessionCache = new Mock<McpRemoteSessionCache>();
            var service = new McpInstallService(mockConfigManager.Object, mockSessionCache.Object, () => true);

            service.Uninstall(out _);

            mockSessionCache.Verify(c => c.ClearTokens(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Install_WithOAuthModeAndInvalidServerUrl_FailsBeforeTouchingConfig()
        {
            var mockConfigManager = new Mock<McpConfigManager>();
            var service = new McpInstallService(mockConfigManager.Object, new Mock<McpRemoteSessionCache>().Object, () => true);
            var config = new CxConfig { ApiKey = "valid-key" };
            var settings = new McpConnectionSettings(McpAuthMode.OAuth, "http://not-https.example.com", "tenant");

            var method = service.GetType().GetMethod("Install", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(CxConfig), typeof(McpConnectionSettings), typeof(Type) }, null);
            var result = (McpInstallResult)method.Invoke(service, new object[] { config, settings, typeof(McpInstallServiceTests) });

            Assert.False(result.Success);
            Assert.Contains("Invalid MCP server URL", result.Message);
            string ignored;
            mockConfigManager.Verify(m => m.InstallOrUpdateOAuth(It.IsAny<string>(), out ignored), Times.Never);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithConfiguredServerUrlAndTenant_BuildsTenantScopedUrl()
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth(null, "https://ast-master-components.dev.cxast.net/", "master-sypher", out string url, out string error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal("https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_ConfiguredValuesOverrideApiKeyIssuer()
        {
            string token = BuildToken("https://iam-dev.dev.cxast.net/auth/realms/master-sypher");

            McpInstallService.TryResolveMcpUrlForOAuth(token, "https://eu.ast.checkmarx.net", "cx_seg", out string url, out _);

            Assert.Equal("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithOnlyServerUrl_TakesTenantFromApiKey()
        {
            string token = BuildToken("https://iam-dev.dev.cxast.net/auth/realms/master-sypher");

            McpInstallService.TryResolveMcpUrlForOAuth(token, "https://custom.example.com", "", out string url, out _);

            Assert.Equal("https://custom.example.com/api/security-mcp/mcp/master-sypher", url);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithOnlyTenant_TakesServerFromApiKey()
        {
            string token = BuildToken("https://iam-dev.dev.cxast.net/auth/realms/master-sypher");

            McpInstallService.TryResolveMcpUrlForOAuth(token, "  ", "other-tenant", out string url, out _);

            Assert.Equal("https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/other-tenant", url);
        }

        [Theory]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", "")]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/", "cx_seg")]
        [InlineData("eu.ast.checkmarx.net", "cx_seg")]
        [InlineData(" https://EU.AST.checkmarx.net/ ", " cx_seg ")]
        public void TryResolveMcpUrlForOAuth_NormalizesServerUrlInput(string serverUrl, string tenant)
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth(null, serverUrl, tenant, out string url, out _);

            Assert.True(ok);
            Assert.Equal("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", url);
        }

        [Theory]
        [InlineData("http://eu.ast.checkmarx.net", "cx_seg", "Invalid MCP server URL")]
        [InlineData("https://eu.ast.checkmarx.net?x=1", "cx_seg", "Invalid MCP server URL")]
        [InlineData("https://", "cx_seg", "Invalid MCP server URL")]
        [InlineData("https://eu.ast.checkmarx.net", "cx seg", "Invalid tenant name")]
        [InlineData("https://eu.ast.checkmarx.net", "a/b", "Invalid tenant name")]
        public void TryResolveMcpUrlForOAuth_WithInvalidInput_ReturnsError(string serverUrl, string tenant, string expectedError)
        {
            bool ok = McpInstallService.TryResolveMcpUrlForOAuth(null, serverUrl, tenant, out string url, out string error);

            Assert.False(ok);
            Assert.Null(url);
            Assert.Contains(expectedError, error);
        }

        [Fact]
        public void TryResolveMcpUrlForOAuth_WithRegionalIamIssuer_MapsToAstHost()
        {
            // eu.iam.checkmarx.net does not serve the MCP endpoint (404); eu.ast.checkmarx.net does.
            string token = BuildToken("https://eu.iam.checkmarx.net/auth/realms/cx_seg");

            McpInstallService.TryResolveMcpUrlForOAuth(token, null, null, out string url, out _);

            Assert.Equal("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg", url);
        }

        [Fact]
        public void McpConnectionSettings_TrimsAndDefaultsNullValues()
        {
            var settings = new McpConnectionSettings(McpAuthMode.OAuth, "  https://eu.ast.checkmarx.net ", null);

            Assert.Equal(McpAuthMode.OAuth, settings.AuthMode);
            Assert.Equal("https://eu.ast.checkmarx.net", settings.OAuthServerUrl);
            Assert.Equal(string.Empty, settings.OAuthTenant);
            Assert.Equal(McpAuthMode.ApiKey, McpConnectionSettings.Default.AuthMode);
        }

        private static string BuildToken(string issuer)
        {
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"iss\":\"{issuer}\"}}"));
            return "header." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
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
