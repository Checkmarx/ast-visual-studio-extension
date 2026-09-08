using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for AssistIconLoader (severity icon file names and base names; no VS/theme required).
    /// </summary>
    public class AssistIconLoaderTests
    {
        #region IconsBasePath

        [Fact]
        public void IconsBasePath_IsExpected()
        {
            Assert.Equal("CxExtension/Resources/CxAssist/Icons", AssistIconLoader.IconsBasePath);
        }

        #endregion

        #region GetSeverityIconFileName

        [Theory]
        [InlineData(SeverityLevel.Malicious, "malicious.png")]
        [InlineData(SeverityLevel.Critical, "critical.png")]
        [InlineData(SeverityLevel.High, "high.png")]
        [InlineData(SeverityLevel.Medium, "medium.png")]
        [InlineData(SeverityLevel.Low, "low.png")]
        [InlineData(SeverityLevel.Info, "low.png")]
        [InlineData(SeverityLevel.Ok, "ok.png")]
        [InlineData(SeverityLevel.Unknown, "unknown.png")]
        [InlineData(SeverityLevel.Ignored, "ignored.png")]
        public void GetSeverityIconFileName_ReturnsExpectedFileName(SeverityLevel severity, string expected)
        {
            Assert.Equal(expected, AssistIconLoader.GetSeverityIconFileName(severity));
        }

        [Fact]
        public void GetSeverityIconFileName_EndsWithPng()
        {
            Assert.EndsWith(".png", AssistIconLoader.GetSeverityIconFileName(SeverityLevel.Critical));
        }

        #endregion

        #region GetSeverityIconBaseName

        [Theory]
        [InlineData("Malicious", "malicious")]
        [InlineData("malicious", "malicious")]
        [InlineData("MALICIOUS", "malicious")]
        [InlineData("Critical", "critical")]
        [InlineData("High", "high")]
        [InlineData("Medium", "medium")]
        [InlineData("Low", "low")]
        [InlineData("Info", "low")]
        [InlineData("Ok", "ok")]
        [InlineData("Unknown", "unknown")]
        [InlineData("Ignored", "ignored")]
        public void GetSeverityIconBaseName_ReturnsExpectedBaseName(string severity, string expected)
        {
            Assert.Equal(expected, AssistIconLoader.GetSeverityIconBaseName(severity));
        }

        [Fact]
        public void GetSeverityIconBaseName_Null_ReturnsUnknown()
        {
            Assert.Equal("unknown", AssistIconLoader.GetSeverityIconBaseName(null));
        }

        [Fact]
        public void GetSeverityIconBaseName_Empty_ReturnsUnknown()
        {
            Assert.Equal("unknown", AssistIconLoader.GetSeverityIconBaseName(""));
        }

        [Fact]
        public void GetSeverityIconBaseName_UnknownSeverity_ReturnsUnknown()
        {
            Assert.Equal("unknown", AssistIconLoader.GetSeverityIconBaseName("CustomSeverity"));
        }

        [Fact]
        public void GetSeverityIconFileName_AllSeverityLevels_ReturnNonEmptyPng()
        {
            foreach (SeverityLevel sev in System.Enum.GetValues(typeof(SeverityLevel)))
            {
                var name = AssistIconLoader.GetSeverityIconFileName(sev);
                Assert.False(string.IsNullOrEmpty(name));
                Assert.EndsWith(".png", name);
            }
        }

        [Fact]
        public void GetSeverityIconBaseName_WhitespaceOnly_ReturnsUnknown()
        {
            Assert.Equal("unknown", AssistIconLoader.GetSeverityIconBaseName("   "));
        }

        #endregion
    }
}
