using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class DevAssistSecretsExclusionTests
    {
        [Theory]
        [InlineData("package.json")]
        [InlineData("package-lock.json")]
        [InlineData("yarn.lock")]
        [InlineData("pom.xml")]
        [InlineData("go.mod")]
        [InlineData("go.sum")]
        public void MatchesManifestDependencyPattern_WithDependencyFile_ReturnsTrue(string fileName)
        {
            var result = DevAssistSecretsExclusion.MatchesManifestDependencyPattern(
                $"C:\\project\\{fileName}");

            Assert.True(result);
        }

        [Theory]
        [InlineData("config.txt")]
        [InlineData("main.js")]
        [InlineData("readme.md")]
        public void MatchesManifestDependencyPattern_WithNonDependencyFile_ReturnsFalse(string fileName)
        {
            var result = DevAssistSecretsExclusion.MatchesManifestDependencyPattern(
                $"C:\\project\\{fileName}");

            Assert.False(result);
        }

        [Fact]
        public void MatchesManifestDependencyPattern_WithNull_ReturnsFalse()
        {
            var result = DevAssistSecretsExclusion.MatchesManifestDependencyPattern(null);

            Assert.False(result);
        }

        [Theory]
        [InlineData(@"C:\repo\.vscode\foo.checkmarxIgnored")]
        [InlineData(@"C:\repo\.vscode\sub\temp.checkmarxIgnored.txt")]
        public void IsCheckmarxIgnoreSidecarPath_WithVscodeAndCheckmarxIgnored_ReturnsTrue(string path)
        {
            var result = DevAssistSecretsExclusion.IsCheckmarxIgnoreSidecarPath(path);

            Assert.True(result);
        }

        [Theory]
        [InlineData(@"C:\project\config.txt")]
        [InlineData(@"C:\repo\.vscode\readme.md")]
        [InlineData(@"C:\project\.cxignore")]
        public void IsCheckmarxIgnoreSidecarPath_WithoutRequiredPattern_ReturnsFalse(string path)
        {
            var result = DevAssistSecretsExclusion.IsCheckmarxIgnoreSidecarPath(path);

            Assert.False(result);
        }

        [Fact]
        public void IsCheckmarxIgnoreSidecarPath_WithNull_ReturnsFalse()
        {
            var result = DevAssistSecretsExclusion.IsCheckmarxIgnoreSidecarPath(null);

            Assert.False(result);
        }
    }
}
