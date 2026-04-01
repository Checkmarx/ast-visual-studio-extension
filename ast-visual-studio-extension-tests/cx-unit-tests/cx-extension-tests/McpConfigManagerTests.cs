using ast_visual_studio_extension.CxPreferences.Configuration;
using System;
using System.IO;
using System.Json;
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
                var result = (JsonObject)method.Invoke(null, new object[] { tempFile });

                Assert.NotNull(result);
                Assert.Equal(0, result.Count);
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
                var result = (JsonObject)method.Invoke(null, new object[] { tempFile });

                Assert.NotNull(result);
                Assert.Equal(0, result.Count);
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
            Assert.Equal(0, result.Count);
        }
    }
}
