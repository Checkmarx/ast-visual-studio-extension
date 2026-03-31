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
    }
}
