using System;
using ast_visual_studio_extension.CxExtension.Enums;
using Xunit;

namespace ast_visual_studio_extension.Tests.CxExtension.Enums
{
    public class EngineTypeTests
    {
        [Theory]
        [InlineData(EngineType.SAST, "sast")]
        [InlineData(EngineType.SCA, "sca")]
        [InlineData(EngineType.KICS, "kics")]
        [InlineData(EngineType.SECRET_DETECTION, "secret detection")]
        [InlineData(EngineType.SCS_SECRET_DETECTION, "sscs-secret-detection")]
        [InlineData(EngineType.IAC_SECURITY, "IaC Security")]
        public void ToEngineString_ReturnsCorrectString(EngineType engineType, string expected)
        {
            var result = engineType.ToEngineString();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("sast", EngineType.SAST)]
        [InlineData("sca", EngineType.SCA)]
        [InlineData("kics", EngineType.KICS)]
        [InlineData("secret detection", EngineType.SECRET_DETECTION)]
        [InlineData("sscs-secret-detection", EngineType.SCS_SECRET_DETECTION)]
        [InlineData("IaC Security", EngineType.IAC_SECURITY)]
        public void FromEngineString_ValidStrings_ReturnsCorrectEnum(string input, EngineType expected)
        {
            var result = EngineTypeExtensions.FromEngineString(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("SAST", EngineType.SAST)]
        [InlineData("Secret Detection", EngineType.SECRET_DETECTION)]
        [InlineData("SSCS-SECRET-DETECTION", EngineType.SCS_SECRET_DETECTION)]
        public void FromEngineString_MixedCaseStrings_ReturnsCorrectEnum(string input, EngineType expected)
        {
            var result = EngineTypeExtensions.FromEngineString(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FromEngineString_InvalidString_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => EngineTypeExtensions.FromEngineString("invalid-engine"));
            Assert.Contains("Unknown engine type", ex.Message);
        }
    }
}
