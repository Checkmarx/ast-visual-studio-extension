using ast_visual_studio_extension.CxWrapper.Models;
using Newtonsoft.Json;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class ContainersRealtimeResultsTests
    {
        [Fact]
        public void Deserialize_ValidJson_ReturnsResults()
        {
            // Arrange
            string json = @"{
                ""Images"": [
                    {
                        ""ImageName"": ""nginx"",
                        ""ImageTag"": ""1.19.0"",
                        ""FilePath"": ""Dockerfile"",
                        ""Status"": ""vulnerable"",
                        ""Locations"": [{""Line"": 1, ""StartIndex"": 5, ""EndIndex"": 20}],
                        ""Vulnerabilities"": [
                            {""CVE"": ""CVE-2021-23017"", ""Severity"": ""high""},
                            {""CVE"": ""CVE-2021-3618"", ""Severity"": ""medium""}
                        ]
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Images);

            var image = result.Images.First();
            Assert.Equal("nginx", image.ImageName);
            Assert.Equal("1.19.0", image.ImageTag);
            Assert.Equal("Dockerfile", image.FilePath);
            Assert.Equal("vulnerable", image.Status);

            Assert.Single(image.Locations);
            Assert.Equal(1, image.Locations.First().Line);

            Assert.Equal(2, image.Vulnerabilities.Count);
            Assert.Equal("CVE-2021-23017", image.Vulnerabilities[0].Cve);
            Assert.Equal("high", image.Vulnerabilities[0].Severity);
        }

        [Fact]
        public void Deserialize_NullImages_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Images"": null}";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Images);
            Assert.Empty(result.Images);
        }

        [Fact]
        public void Deserialize_EmptyImagesArray_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Images"": []}";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Images);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptyImages()
        {
            // Arrange
            string json = @"{}";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Images);
        }

        [Fact]
        public void Deserialize_MultipleImages_AllParsed()
        {
            // Arrange
            string json = @"{
                ""Images"": [
                    {""ImageName"": ""nginx"", ""ImageTag"": ""latest""},
                    {""ImageName"": ""node"", ""ImageTag"": ""18-alpine""},
                    {""ImageName"": ""postgres"", ""ImageTag"": ""14""}
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Images.Count);
            Assert.Equal("nginx", result.Images[0].ImageName);
            Assert.Equal("node", result.Images[1].ImageName);
            Assert.Equal("postgres", result.Images[2].ImageName);
        }

        [Fact]
        public void Deserialize_ImageWithNullVulnerabilities_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{
                ""Images"": [
                    {
                        ""ImageName"": ""alpine"",
                        ""Vulnerabilities"": null
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Images);
            Assert.NotNull(result.Images.First().Vulnerabilities);
            Assert.Empty(result.Images.First().Vulnerabilities);
        }

        [Fact]
        public void Deserialize_ImageWithNullLocations_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{
                ""Images"": [
                    {
                        ""ImageName"": ""alpine"",
                        ""Locations"": null
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<ContainersRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Images);
            Assert.NotNull(result.Images.First().Locations);
            Assert.Empty(result.Images.First().Locations);
        }
    }
}

