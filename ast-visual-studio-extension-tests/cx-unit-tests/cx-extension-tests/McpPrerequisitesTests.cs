using ast_visual_studio_extension.CxPreferences.Configuration;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class McpPrerequisitesTests
    {
        [Fact]
        public void IsOnPath_FindsCommandWithPathExtension()
        {
            string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                File.WriteAllText(Path.Combine(dir, "npx.cmd"), "@echo off");
                string path = @"C:\does-not-exist" + Path.PathSeparator + "\"" + dir + "\"";

                Assert.True(McpPrerequisites.IsOnPath("npx", path, ".EXE;.CMD"));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void IsOnPath_WhenMissing_ReturnsFalse()
        {
            string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                Assert.False(McpPrerequisites.IsOnPath("npx", dir, ".EXE;.CMD"));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsOnPath_WithEmptyPath_ReturnsFalse(string path)
        {
            Assert.False(McpPrerequisites.IsOnPath("npx", path, null));
        }

        [Fact]
        public void IsOnPath_IgnoresInvalidPathEntries()
        {
            Assert.False(McpPrerequisites.IsOnPath("npx", "C:\\bad|dir" + Path.PathSeparator + "C:\\also<bad", null));
        }
    }
}
