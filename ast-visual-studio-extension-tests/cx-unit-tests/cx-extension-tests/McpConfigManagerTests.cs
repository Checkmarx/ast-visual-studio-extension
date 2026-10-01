using ast_visual_studio_extension.CxPreferences.Configuration;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class McpConfigManagerTests
    {
        /// <summary>
        /// Test-only subclass that redirects GetMcpConfigPath to a caller-supplied path,
        /// so tests operate on a temp file instead of the real ~/.mcp.json.
        /// No Moq or InternalsVisibleTo("DynamicProxyGenAssembly2") needed.
        /// </summary>
        private class TestableConfigManager : McpConfigManager
        {
            private readonly string _configPath;
            public TestableConfigManager(string configPath) { _configPath = configPath; }
            public override string GetMcpConfigPath() => _configPath;
        }

        [Fact]
        public void InstallOrUpdate_ThrowsIfApiKeyMissing()
        {
            var mgr = new McpConfigManager();
            Assert.Throws<ArgumentException>(() => mgr.InstallOrUpdate(null, "url", out _));
            Assert.Throws<ArgumentException>(() => mgr.InstallOrUpdate("", "url", out _));
        }

        [Fact]
        public void InstallOrUpdate_WritesConfigAndReturnsChanged()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{}");
                var mgr = new TestableConfigManager(tempFile);

                var changed = mgr.InstallOrUpdate("key", "url", out string configPath);
                Assert.True(changed);
                Assert.True(File.Exists(configPath));
                string json = File.ReadAllText(configPath);
                Assert.Contains("Checkmarx", json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void RemoveCheckmarxServer_ReturnsFalseIfNoServer()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{\"servers\":{}}\n");
                var mgr = new TestableConfigManager(tempFile);

                var result = mgr.RemoveCheckmarxServer(out string configPath);
                Assert.False(result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void RemoveCheckmarxServer_RemovesServerIfExists()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{\"servers\":{\"Checkmarx\":{}}}\n");
                var mgr = new TestableConfigManager(tempFile);

                var result = mgr.RemoveCheckmarxServer(out string configPath);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void StripJsonComments_RemovesComments()
        {
            string jsonWithComments = "{\n  // line comment\n  \"a\": 1, /* block comment */ \"b\": 2\n}";
            var method = typeof(McpConfigManager).GetMethod("StripJsonComments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string result = (string)method.Invoke(null, new object[] { jsonWithComments });
            Assert.DoesNotContain("//", result);
            Assert.DoesNotContain("/*", result);
            Assert.Contains("a", result);
            Assert.Contains("b", result);
        }

        [Fact]
        public void StripJsonComments_IgnoresCommentsInStrings()
        {
            string jsonWithCommentsInString = "{\"url\": \"https://example.com// not a comment\", \"comment\": \"/* also not */\"}";
            var method = typeof(McpConfigManager).GetMethod("StripJsonComments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string result = (string)method.Invoke(null, new object[] { jsonWithCommentsInString });
            Assert.Contains("https://example.com// not a comment", result);
            Assert.Contains("/* also not */", result);
        }

        [Fact]
        public void StripJsonComments_HandlesEscapedQuotes()
        {
            string jsonWithEscapedQuotes = "{\"key\": \"value\\\" with quote\", /* comment */ \"other\": \"value\"}";
            var method = typeof(McpConfigManager).GetMethod("StripJsonComments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string result = (string)method.Invoke(null, new object[] { jsonWithEscapedQuotes });
            Assert.DoesNotContain("/*", result);
            Assert.Contains("other", result);
        }

        [Fact]
        public void InstallOrUpdate_WithExistingConfig_UpdatesIfChanged()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                string initialJson = "{\"servers\":{\"Checkmarx\":{\"command\":\"old\"}}}";
                File.WriteAllText(Path.GetFullPath(tempFile),initialJson);
                var mgr = new TestableConfigManager(tempFile);

                var changed = mgr.InstallOrUpdate("newkey", "https://new-url.com", out string configPath);
                Assert.True(changed); // new key+url differs from old config
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdate_CreatesInputsArray()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{}");
                var mgr = new TestableConfigManager(tempFile);

                var changed = mgr.InstallOrUpdate("key", "url", out string configPath);
                Assert.True(changed);
                Assert.True(File.Exists(configPath));
                string json = File.ReadAllText(configPath);
                Assert.Contains("inputs", json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void RemoveCheckmarxServer_WithoutServersKey_ReturnsFalse()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{}");
                var mgr = new TestableConfigManager(tempFile);

                var result = mgr.RemoveCheckmarxServer(out string configPath);
                Assert.False(result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetMcpConfigPath_ReturnsValidPath()
        {
            var mgr = new McpConfigManager();
            var path = mgr.GetMcpConfigPath();

            Assert.NotNull(path);
            Assert.Contains(".mcp.json", path);
            Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path);
        }

        [Fact]
        public void BuildCheckmarxServer_ContainsRequiredFields()
        {
            var method = typeof(McpConfigManager).GetMethod("BuildCheckmarxServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var server = (JObject)method.Invoke(null, new object[] { "test-api-key", "https://test-url.com" });

            Assert.NotNull(server);
            Assert.True(server.ContainsKey("command"));
            Assert.True(server.ContainsKey("args"));
            Assert.Equal("npx", server["command"].ToString().Trim('"'));
        }

        [Fact]
        public void BuildCheckmarxServer_ContainsAuthorizationHeader()
        {
            var method = typeof(McpConfigManager).GetMethod("BuildCheckmarxServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var server = (JObject)method.Invoke(null, new object[] { "my-secret-key", "https://test-url.com" });

            string serverJson = server.ToString();
            Assert.Contains("Authorization:my-secret-key", serverJson);
        }

        [Fact]
        public void InstallOrUpdate_WithNullUrl_UsesDefaultUrl()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{}");
                var mgr = new TestableConfigManager(tempFile);

                mgr.InstallOrUpdate("key", null, out string configPath);
                string json = File.ReadAllText(configPath);
                Assert.Contains(McpConfigManager.DefaultMcpUrl, json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdate_WithEmptyUrl_UsesDefaultUrl()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{}");
                var mgr = new TestableConfigManager(tempFile);

                mgr.InstallOrUpdate("key", "", out string configPath);
                string json = File.ReadAllText(configPath);
                Assert.Contains(McpConfigManager.DefaultMcpUrl, json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadConfig_WithMalformedJson_ReturnsEmptyObject()
        {
            var method = typeof(McpConfigManager).GetMethod("ReadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"{this is not valid json!!!");
                var result = (JObject)method.Invoke(null, new object[] { tempFile });

                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadConfig_WithEmptyFile_ReturnsEmptyObject()
        {
            var method = typeof(McpConfigManager).GetMethod("ReadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),"");
                var result = (JObject)method.Invoke(null, new object[] { tempFile });

                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void BuildCheckmarxServerOAuth_ContainsNpxBridgeWithoutAuthorization()
        {
            var method = typeof(McpConfigManager).GetMethod("BuildCheckmarxServerOAuth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var server = (JObject)method.Invoke(null, new object[] { "https://test-url.com" });

            Assert.NotNull(server);
            Assert.Equal("npx", server["command"].ToString().Trim('"'));
            var args = (JArray)server["args"];
            Assert.Equal("-y", args[0].ToString());
            Assert.Equal(McpConfigManager.MCP_REMOTE_PACKAGE, args[1].ToString());
            Assert.Contains(args, t => t.ToString().Trim('"') == "https://test-url.com");
            Assert.Contains(args, t => t.ToString().Trim('"') == "cx-origin:VisualStudio");
            Assert.False(server.ContainsKey("type"));
            Assert.False(server.ContainsKey("url"));
            Assert.False(server.ContainsKey("headers"));
            Assert.DoesNotContain("Authorization", server.ToString());
        }

        [Fact]
        public void InstallOrUpdateOAuth_WritesConfigAndReturnsChanged()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile), "{}");
                var mgr = new TestableConfigManager(tempFile);

                var changed = mgr.InstallOrUpdateOAuth("https://oauth-url.com", out string configPath);
                Assert.True(changed);
                Assert.True(File.Exists(configPath));
                string json = File.ReadAllText(configPath);
                Assert.Contains("Checkmarx", json);
                Assert.Contains("\"command\": \"npx\"", json);
                Assert.Contains("mcp-remote", json);
                Assert.Contains("https://oauth-url.com", json);
                Assert.DoesNotContain("Authorization", json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdateOAuth_WritesExactlyWorkingBridgeShape()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile), "{}");
                var mgr = new TestableConfigManager(tempFile);

                const string workingUrl = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher";
                mgr.InstallOrUpdateOAuth(workingUrl, out string configPath);

                var root = JObject.Parse(File.ReadAllText(configPath));
                var server = root["servers"]["Checkmarx"] as JObject;

                var expected = new JObject
                {
                    ["command"] = "npx",
                    ["args"] = new JArray
                    {
                        "-y",
                        "mcp-remote@0.14.3",
                        workingUrl,
                        "--transport",
                        "http-first",
                        "--header",
                        "cx-origin:VisualStudio",
                        "--verbose"
                    }
                };

                Assert.True(JToken.DeepEquals(expected, server), "OAuth MCP entry must match the working bridge config exactly.");
                Assert.False(server.ToString().Contains("Authorization"));
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdate_ApiKeyEntryUsesPinnedBridge()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile), "{}");
                var mgr = new TestableConfigManager(tempFile);

                mgr.InstallOrUpdate("the-key", "https://ast.example.com/api/security-mcp/mcp", out string configPath);

                var args = (JArray)JObject.Parse(File.ReadAllText(configPath))["servers"]["Checkmarx"]["args"];
                Assert.Equal("-y", args[0].ToString());
                Assert.Equal(McpConfigManager.MCP_REMOTE_PACKAGE, args[1].ToString());
                Assert.Equal("https://ast.example.com/api/security-mcp/mcp", args[2].ToString());
                Assert.Contains(args, t => t.ToString() == "Authorization:the-key");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetInstalledOAuthServerUrl_ReturnsUrlOnlyForOAuthEntry()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile), "{}");
                var mgr = new TestableConfigManager(tempFile);
                const string oauthUrl = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher";

                Assert.Null(mgr.GetInstalledOAuthServerUrl());

                mgr.InstallOrUpdateOAuth(oauthUrl, out _);
                Assert.Equal(oauthUrl, mgr.GetInstalledOAuthServerUrl());

                mgr.InstallOrUpdate("the-key", oauthUrl, out _);
                Assert.Null(mgr.GetInstalledOAuthServerUrl());
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetInstalledOAuthServerUrl_RecognizesLegacyUnpinnedEntry()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile),
                    "{\"servers\":{\"Checkmarx\":{\"command\":\"npx\",\"args\":[\"mcp-remote\",\"https://h.example.com/api/security-mcp/mcp/t\",\"--header\",\"cx-origin:VisualStudio\"]}}}");
                var mgr = new TestableConfigManager(tempFile);

                Assert.Equal("https://h.example.com/api/security-mcp/mcp/t", mgr.GetInstalledOAuthServerUrl());
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdateOAuth_WithNullUrl_UsesDefaultUrl()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(Path.GetFullPath(tempFile), "{}");
                var mgr = new TestableConfigManager(tempFile);

                mgr.InstallOrUpdateOAuth(null, out string configPath);
                string json = File.ReadAllText(configPath);
                Assert.Contains(McpConfigManager.DefaultMcpUrl, json);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadConfig_WithNonexistentFile_ReturnsEmptyObject()
        {
            var method = typeof(McpConfigManager).GetMethod("ReadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (JObject)method.Invoke(null, new object[] { "/nonexistent/path/file.json" });

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
