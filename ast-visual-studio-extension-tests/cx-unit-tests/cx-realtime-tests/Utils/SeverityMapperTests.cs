using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class SeverityMapperTests
    {
        [Fact]
        public void MapToLevel_WithCritical_ReturnsCritical()
        {
            var result = SeverityMapper.MapToLevel("critical");
            Assert.Equal(SeverityLevel.Critical, result);
        }

        [Fact]
        public void MapToLevel_WithHigh_ReturnsHigh()
        {
            var result = SeverityMapper.MapToLevel("high");
            Assert.Equal(SeverityLevel.High, result);
        }

        [Fact]
        public void MapToLevel_WithMedium_ReturnsMedium()
        {
            var result = SeverityMapper.MapToLevel("medium");
            Assert.Equal(SeverityLevel.Medium, result);
        }

        [Fact]
        public void MapToLevel_WithLow_ReturnsLow()
        {
            var result = SeverityMapper.MapToLevel("low");
            Assert.Equal(SeverityLevel.Low, result);
        }

        [Fact]
        public void MapToLevel_WithInfo_ReturnsLow()
        {
            var result = SeverityMapper.MapToLevel("info");
            Assert.Equal(SeverityLevel.Low, result);
        }

        [Fact]
        public void MapToLevel_WithInformational_ReturnsLow()
        {
            var result = SeverityMapper.MapToLevel("informational");
            Assert.Equal(SeverityLevel.Low, result);
        }

        [Fact]
        public void MapToLevel_WithWarning_ReturnsMedium()
        {
            var result = SeverityMapper.MapToLevel("warning");
            Assert.Equal(SeverityLevel.Medium, result);
        }

        [Fact]
        public void MapToLevel_WithError_ReturnsHigh()
        {
            var result = SeverityMapper.MapToLevel("error");
            Assert.Equal(SeverityLevel.High, result);
        }

        [Fact]
        public void MapToLevel_WithUnknownSeverity_ReturnsUnknown()
        {
            var result = SeverityMapper.MapToLevel("unknown-type");
            Assert.Equal(SeverityLevel.Unknown, result);
        }

        [Fact]
        public void MapToLevel_WithNull_ReturnsMedium()
        {
            var result = SeverityMapper.MapToLevel(null);
            Assert.Equal(SeverityLevel.Medium, result);
        }

        [Fact]
        public void MapToLevel_WithEmptyString_ReturnsMedium()
        {
            var result = SeverityMapper.MapToLevel(string.Empty);
            Assert.Equal(SeverityLevel.Medium, result);
        }

        [Fact]
        public void MapToLevel_WithWhitespace_ReturnsMedium()
        {
            var result = SeverityMapper.MapToLevel("   ");
            Assert.Equal(SeverityLevel.Medium, result);
        }

        [Fact]
        public void MapToLevel_CaseInsensitive_WithMixedCase_MapsCorrected()
        {
            Assert.Equal(SeverityLevel.Critical, SeverityMapper.MapToLevel("CRITICAL"));
            Assert.Equal(SeverityLevel.Critical, SeverityMapper.MapToLevel("Critical"));
            Assert.Equal(SeverityLevel.High, SeverityMapper.MapToLevel("HIGH"));
            Assert.Equal(SeverityLevel.Medium, SeverityMapper.MapToLevel("MeDiUm"));
        }

        [Fact]
        public void MapToString_WithCritical_ReturnsCritical()
        {
            var result = SeverityMapper.MapToString("critical");
            Assert.Equal("Critical", result);
        }

        [Fact]
        public void MapToString_WithHigh_ReturnsHigh()
        {
            var result = SeverityMapper.MapToString("high");
            Assert.Equal("High", result);
        }

        [Fact]
        public void MapToString_WithMedium_ReturnsMedium()
        {
            var result = SeverityMapper.MapToString("medium");
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void MapToString_WithLow_ReturnsLow()
        {
            var result = SeverityMapper.MapToString("low");
            Assert.Equal("Low", result);
        }

        [Fact]
        public void MapToString_WithUnknown_ReturnsMedium()
        {
            var result = SeverityMapper.MapToString("unknown-type");
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void MapToString_WithNull_ReturnsMedium()
        {
            var result = SeverityMapper.MapToString(null);
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void GetPrecedence_Critical_Returns0()
        {
            var result = SeverityMapper.GetPrecedence("critical");
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetPrecedence_High_Returns1()
        {
            var result = SeverityMapper.GetPrecedence("high");
            Assert.Equal(1, result);
        }

        [Fact]
        public void GetPrecedence_Medium_Returns2()
        {
            var result = SeverityMapper.GetPrecedence("medium");
            Assert.Equal(2, result);
        }

        [Fact]
        public void GetPrecedence_Low_Returns3()
        {
            var result = SeverityMapper.GetPrecedence("low");
            Assert.Equal(3, result);
        }

        [Fact]
        public void GetPrecedence_Unknown_Returns4()
        {
            var result = SeverityMapper.GetPrecedence("unknown-type");
            Assert.Equal(4, result);
        }

        [Fact]
        public void GetHighestSeverity_WithMultipleSeverities_ReturnsCritical()
        {
            var result = SeverityMapper.GetHighestSeverity("low", "medium", "critical", "high");
            Assert.Equal("Critical", result);
        }

        [Fact]
        public void GetHighestSeverity_WithoutCritical_ReturnsHigh()
        {
            var result = SeverityMapper.GetHighestSeverity("low", "medium", "high");
            Assert.Equal("High", result);
        }

        [Fact]
        public void GetHighestSeverity_WithSingleSeverity_ReturnsThatSeverity()
        {
            var result = SeverityMapper.GetHighestSeverity("medium");
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void GetHighestSeverity_WithNull_ReturnsMedium()
        {
            var result = SeverityMapper.GetHighestSeverity(null);
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void GetHighestSeverity_WithEmptyArray_ReturnsMedium()
        {
            var result = SeverityMapper.GetHighestSeverity();
            Assert.Equal("Medium", result);
        }

        [Fact]
        public void CompareSeverities_CriticalVsHigh_ReturnNegative()
        {
            var result = SeverityMapper.CompareSeverities("critical", "high");
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareSeverities_HighVsCritical_ReturnPositive()
        {
            var result = SeverityMapper.CompareSeverities("high", "critical");
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareSeverities_SameSeverity_ReturnZero()
        {
            var result = SeverityMapper.CompareSeverities("high", "high");
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareSeverities_MediumVsLow_ReturnNegative()
        {
            var result = SeverityMapper.CompareSeverities("medium", "low");
            Assert.True(result < 0);
        }
    }
}
