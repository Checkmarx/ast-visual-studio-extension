using ast_visual_studio_extension.CxPreferences.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class McpRemoteSessionCacheTests
    {
        private const string DevTenantMcpUrl = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher";

        [Fact]
        public void ComputeCacheKey_MatchesMcpRemoteFileNaming()
        {
            // Value taken from the ~/.mcp-auth file name mcp-remote 0.14.x wrote for this OAuth entry.
            Assert.Equal("27c51094d69ae2c602119526aeb7d525", McpRemoteSessionCache.ComputeCacheKey(DevTenantMcpUrl));
        }

        [Fact]
        public void ClearTokens_RemovesOnlyTokensOfTheGivenEntryAcrossStorageVersions()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                string key = McpRemoteSessionCache.ComputeCacheKey(DevTenantMcpUrl);
                string v1 = Directory.CreateDirectory(Path.Combine(root, "mcp-remote-v1")).FullName;
                string legacy = Directory.CreateDirectory(Path.Combine(root, "mcp-remote-0.1.38")).FullName;
                File.WriteAllText(Path.Combine(v1, key + "_tokens.json"), "{}");
                File.WriteAllText(Path.Combine(legacy, key + "_tokens.json"), "{}");
                File.WriteAllText(Path.Combine(v1, key + "_client_info.json"), "{}");
                File.WriteAllText(Path.Combine(v1, "otherserver_tokens.json"), "{}");

                int removed = new McpRemoteSessionCache(root).ClearTokens(DevTenantMcpUrl);

                Assert.Equal(2, removed);
                Assert.False(File.Exists(Path.Combine(v1, key + "_tokens.json")));
                Assert.False(File.Exists(Path.Combine(legacy, key + "_tokens.json")));
                // Client registration is kept so a reset does not create a new DCR client every time.
                Assert.True(File.Exists(Path.Combine(v1, key + "_client_info.json")));
                Assert.True(File.Exists(Path.Combine(v1, "otherserver_tokens.json")));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void ClearTokens_WithMissingCacheRoot_ReturnsZero()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Assert.Equal(0, new McpRemoteSessionCache(root).ClearTokens(DevTenantMcpUrl));
        }

        [Fact]
        public void ClearTokens_WithEmptyUrl_ReturnsZero()
        {
            Assert.Equal(0, new McpRemoteSessionCache(Path.GetTempPath()).ClearTokens(""));
        }

        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

        private static string Jwt(DateTimeOffset exp)
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"exp\":" + exp.ToUnixTimeSeconds() + "}"));
            return "eyJhbGciOiJub25lIn0." + payload.TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".sig";
        }

        private static string TokensJson(string accessToken, string refreshToken)
        {
            var tokens = new JObject { ["access_token"] = accessToken, ["token_type"] = "Bearer" };
            if (refreshToken != null)
                tokens["refresh_token"] = refreshToken;
            return tokens.ToString();
        }

        [Fact]
        public void IsSessionExpired_WhenAccessAndRefreshTokensExpired_ReturnsTrue()
        {
            // The state that produced the 404 sign-in page: access expired, Keycloak refresh token (SSO-bound) expired.
            Assert.True(McpRemoteSessionCache.IsSessionExpired(TokensJson(Jwt(Now.AddDays(-7)), Jwt(Now.AddDays(-6))), Now));
        }

        [Fact]
        public void IsSessionExpired_WhenRefreshTokenStillValid_ReturnsFalse()
        {
            // mcp-remote refreshes this on its own; deleting it would force an unnecessary sign-in.
            Assert.False(McpRemoteSessionCache.IsSessionExpired(TokensJson(Jwt(Now.AddMinutes(-5)), Jwt(Now.AddHours(4))), Now));
        }

        [Fact]
        public void IsSessionExpired_WhenAccessTokenStillValid_ReturnsFalse()
        {
            Assert.False(McpRemoteSessionCache.IsSessionExpired(TokensJson(Jwt(Now.AddMinutes(20)), Jwt(Now.AddMinutes(-1))), Now));
        }

        [Fact]
        public void IsSessionExpired_WhenAccessExpiredAndNoRefreshToken_ReturnsTrue()
        {
            Assert.True(McpRemoteSessionCache.IsSessionExpired(TokensJson(Jwt(Now.AddMinutes(-1)), null), Now));
        }

        [Fact]
        public void IsSessionExpired_UsesExpiresAtWhenAccessTokenIsOpaque()
        {
            var tokens = new JObject
            {
                ["access_token"] = "opaque",
                ["refresh_token"] = Jwt(Now.AddDays(-1)),
                ["expires_at"] = Now.AddHours(-1).ToUnixTimeMilliseconds()
            };
            Assert.True(McpRemoteSessionCache.IsSessionExpired(tokens.ToString(), Now));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{}")]
        public void IsSessionExpired_WhenUnreadable_KeepsTheSession(string json)
        {
            Assert.False(McpRemoteSessionCache.IsSessionExpired(json, Now));
        }

        [Fact]
        public void IsSessionExpired_WhenRefreshTokenIsOpaque_KeepsTheSession()
        {
            Assert.False(McpRemoteSessionCache.IsSessionExpired(TokensJson(Jwt(Now.AddDays(-1)), "opaque-refresh"), Now));
        }

        [Fact]
        public void ClearExpiredTokens_RemovesOnlyDeadSessions()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                const string liveUrl = "https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg";
                string dir = Directory.CreateDirectory(Path.Combine(root, "mcp-remote-v1")).FullName;
                string deadFile = Path.Combine(dir, McpRemoteSessionCache.ComputeCacheKey(DevTenantMcpUrl) + "_tokens.json");
                string liveFile = Path.Combine(dir, McpRemoteSessionCache.ComputeCacheKey(liveUrl) + "_tokens.json");
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                File.WriteAllText(deadFile, TokensJson(Jwt(utcNow.AddDays(-2)), Jwt(utcNow.AddDays(-1))));
                File.WriteAllText(liveFile, TokensJson(Jwt(utcNow.AddDays(-2)), Jwt(utcNow.AddHours(3))));

                var cache = new McpRemoteSessionCache(root);

                Assert.Equal(1, cache.ClearExpiredTokens(DevTenantMcpUrl));
                Assert.Equal(0, cache.ClearExpiredTokens(liveUrl));
                Assert.False(File.Exists(deadFile));
                Assert.True(File.Exists(liveFile));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }
    }
}
