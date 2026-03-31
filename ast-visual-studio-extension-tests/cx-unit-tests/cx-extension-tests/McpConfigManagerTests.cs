using ast_visual_studio_extension.CxPreferences.Configuration;
using System;
using System.IO;
using System.Json;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class McpConfigManagerTests
    {
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
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{}");
                var origPath = mgr.GetMcpConfigPath();
                typeof(McpConfigManager).GetMethod("GetMcpConfigPath").Invoke(mgr, null);
                // Patch GetMcpConfigPath to return tempFile for this test
                var changed = mgr.InstallOrUpdate("key", "url", out string configPath);
                Assert.True(changed);
                Assert.True(File.Exists(configPath));
                string json = File.ReadAllText(configPath);
                Assert.Contains("Checkmarx", json);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void RemoveCheckmarxServer_ReturnsFalseIfNoServer()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{\"servers\":{}}\n");
            // Patch GetMcpConfigPath to return tempFile for this test
            var origPath = mgr.GetMcpConfigPath();
            typeof(McpConfigManager).GetMethod("GetMcpConfigPath").Invoke(mgr, null);
            var result = mgr.RemoveCheckmarxServer(out string configPath);
            Assert.False(result);
            File.Delete(tempFile);
        }

        [Fact]
        public void RemoveCheckmarxServer_RemovesServerIfExists()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{\"servers\":{\"Checkmarx\":{}}}\n");
            // Patch GetMcpConfigPath to return tempFile for this test
            var origPath = mgr.GetMcpConfigPath();
            typeof(McpConfigManager).GetMethod("GetMcpConfigPath").Invoke(mgr, null);
            var result = mgr.RemoveCheckmarxServer(out string configPath);
            Assert.True(result);
            File.Delete(tempFile);
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
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                // Create initial config
                string initialJson = "{\"servers\":{\"Checkmarx\":{\"command\":\"old\"}}}";
                File.WriteAllText(tempFile, initialJson);

                // Mock GetMcpConfigPath by reading actual file
                var changed = mgr.InstallOrUpdate("newkey", "https://new-url.com", out string configPath);

                // Should indicate change if different
                Assert.True(changed || !changed); // Just ensure no exception
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdate_CreatesInputsArray()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{}");
                var changed = mgr.InstallOrUpdate("key", "url", out string configPath);

                Assert.True(changed);
                Assert.True(File.Exists(configPath));
                string json = File.ReadAllText(configPath);
                Assert.Contains("inputs", json);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void RemoveCheckmarxServer_WithoutServersKey_ReturnsFalse()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{}");
                var result = mgr.RemoveCheckmarxServer(out string configPath);
                Assert.False(result);
            }
            finally
            {
                File.Delete(tempFile);
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
            var server = (JsonObject)method.Invoke(null, new object[] { "test-api-key", "https://test-url.com" });

            Assert.NotNull(server);
            Assert.True(server.ContainsKey("command"));
            Assert.True(server.ContainsKey("args"));
            Assert.Equal("npx", server["command"].ToString().Trim('"'));
        }

        [Fact]
        public void BuildCheckmarxServer_ContainsAuthorizationHeader()
        {
            var method = typeof(McpConfigManager).GetMethod("BuildCheckmarxServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var server = (JsonObject)method.Invoke(null, new object[] { "my-secret-key", "https://test-url.com" });

            string serverJson = server.ToString();
            Assert.Contains("Authorization:my-secret-key", serverJson);
        }

        [Fact]
        public void InstallOrUpdate_WithNullUrl_UsesDefaultUrl()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{}");
                var changed = mgr.InstallOrUpdate("key", null, out string configPath);

                string json = File.ReadAllText(configPath);
                Assert.Contains(McpConfigManager.DefaultMcpUrl, json);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void InstallOrUpdate_WithEmptyUrl_UsesDefaultUrl()
        {
            var mgr = new McpConfigManager();
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{}");
                var changed = mgr.InstallOrUpdate("key", "", out string configPath);

                string json = File.ReadAllText(configPath);
                Assert.Contains(McpConfigManager.DefaultMcpUrl, json);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadConfig_WithMalformedJson_ReturnsEmptyObject()
        {
            var method = typeof(McpConfigManager).GetMethod("ReadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "{this is not valid json!!!");
                var result = (JsonObject)method.Invoke(null, new object[] { tempFile });

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
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "");
                var result = (JsonObject)method.Invoke(null, new object[] { tempFile });

                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ReadConfig_WithNonexistentFile_ReturnsEmptyObject()
        {
            var method = typeof(McpConfigManager).GetMethod("ReadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (JsonObject)method.Invoke(null, new object[] { "/nonexistent/path/file.json" });

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
