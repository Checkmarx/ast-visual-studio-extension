using ast_visual_studio_extension.CxWrapper.Models;
using Newtonsoft.Json;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class SecretsRealtimeResultsTests
    {
        [Fact]
        public void Deserialize_ValidJson_ReturnsResults()
        {
            // Arrange
            string json = @"{
                ""Secrets"": [
                    {
                        ""Title"": ""AWS Access Key"",
                        ""Description"": ""Exposed AWS access key detected"",
                        ""SecretValue"": ""AKIA***********"",
                        ""FilePath"": ""config.env"",
                        ""Severity"": ""high"",
                        ""Locations"": [{""Line"": 5, ""StartIndex"": 0, ""EndIndex"": 40}]
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Secrets);

            var secret = result.Secrets.First();
            Assert.Equal("AWS Access Key", secret.Title);
            Assert.Equal("Exposed AWS access key detected", secret.Description);
            Assert.Equal("AKIA***********", secret.SecretValue);
            Assert.Equal("config.env", secret.FilePath);
            Assert.Equal("high", secret.Severity);

            Assert.Single(secret.Locations);
            Assert.Equal(5, secret.Locations.First().Line);
        }

        [Fact]
        public void Deserialize_NullSecrets_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Secrets"": null}";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Secrets);
            Assert.Empty(result.Secrets);
        }

        [Fact]
        public void Deserialize_EmptySecretsArray_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Secrets"": []}";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Secrets);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptySecrets()
        {
            // Arrange
            string json = @"{}";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Secrets);
        }

        [Fact]
        public void Deserialize_MultipleSecrets_AllParsed()
        {
            // Arrange
            string json = @"{
                ""Secrets"": [
                    {""Title"": ""AWS Access Key"", ""Severity"": ""high""},
                    {""Title"": ""GitHub Token"", ""Severity"": ""high""},
                    {""Title"": ""Private Key"", ""Severity"": ""critical""}
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Secrets.Count);
            Assert.Equal("AWS Access Key", result.Secrets[0].Title);
            Assert.Equal("GitHub Token", result.Secrets[1].Title);
            Assert.Equal("Private Key", result.Secrets[2].Title);
        }

        [Fact]
        public void Deserialize_SecretWithNullLocations_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{
                ""Secrets"": [
                    {
                        ""Title"": ""API Key"",
                        ""Locations"": null
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<SecretsRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Secrets);
            Assert.NotNull(result.Secrets.First().Locations);
            Assert.Empty(result.Secrets.First().Locations);
        }
    }
}

