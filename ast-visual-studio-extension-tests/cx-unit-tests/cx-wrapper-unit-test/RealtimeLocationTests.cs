using ast_visual_studio_extension.CxWrapper.Models;
using Newtonsoft.Json;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class RealtimeLocationTests
    {
        [Fact]
        public void Deserialize_ValidJson_ReturnsLocation()
        {
            // Arrange
            string json = @"{""Line"": 10, ""StartIndex"": 5, ""EndIndex"": 25}";

            // Act
            var result = JsonConvert.DeserializeObject<RealtimeLocation>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Line);
            Assert.Equal(5, result.StartIndex);
            Assert.Equal(25, result.EndIndex);
        }

        [Fact]
        public void Deserialize_PartialJson_MissingPropertiesDefaultToZero()
        {
            // Arrange
            string json = @"{""Line"": 15}";

            // Act
            var result = JsonConvert.DeserializeObject<RealtimeLocation>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.Line);
            Assert.Equal(0, result.StartIndex);
            Assert.Equal(0, result.EndIndex);
        }

        [Fact]
        public void Deserialize_EmptyJson_AllPropertiesDefaultToZero()
        {
            // Arrange
            string json = @"{}";

            // Act
            var result = JsonConvert.DeserializeObject<RealtimeLocation>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Line);
            Assert.Equal(0, result.StartIndex);
            Assert.Equal(0, result.EndIndex);
        }

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Act
            var location = new RealtimeLocation(line: 42, startIndex: 10, endIndex: 50);

            // Assert
            Assert.Equal(42, location.Line);
            Assert.Equal(10, location.StartIndex);
            Assert.Equal(50, location.EndIndex);
        }
    }
}

