using ast_visual_studio_extension.CxWrapper.Models;
using Newtonsoft.Json;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class IacRealtimeResultsTests
    {
        [Fact]
        public void Deserialize_ValidJson_ReturnsResults()
        {
            // Arrange
            string json = @"{
                ""Results"": [
                    {
                        ""Title"": ""S3 Bucket Public Access"",
                        ""Description"": ""S3 bucket allows public access"",
                        ""SimilarityID"": ""abc123"",
                        ""FilePath"": ""main.tf"",
                        ""Severity"": ""high"",
                        ""ExpectedValue"": ""acl = private"",
                        ""ActualValue"": ""acl = public-read"",
                        ""Locations"": [{""Line"": 15, ""StartIndex"": 2, ""EndIndex"": 30}]
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);

            var issue = result.Results.First();
            Assert.Equal("S3 Bucket Public Access", issue.Title);
            Assert.Equal("S3 bucket allows public access", issue.Description);
            Assert.Equal("abc123", issue.SimilarityId);
            Assert.Equal("main.tf", issue.FilePath);
            Assert.Equal("high", issue.Severity);
            Assert.Equal("acl = private", issue.ExpectedValue);
            Assert.Equal("acl = public-read", issue.ActualValue);

            Assert.Single(issue.Locations);
            Assert.Equal(15, issue.Locations.First().Line);
        }

        [Fact]
        public void Deserialize_NullResults_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Results"": null}";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public void Deserialize_EmptyResultsArray_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Results"": []}";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptyResults()
        {
            // Arrange
            string json = @"{}";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public void Deserialize_MultipleIssues_AllParsed()
        {
            // Arrange
            string json = @"{
                ""Results"": [
                    {""Title"": ""S3 Public Access"", ""Severity"": ""high""},
                    {""Title"": ""Unencrypted EBS"", ""Severity"": ""medium""},
                    {""Title"": ""Open Security Group"", ""Severity"": ""critical""}
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal("S3 Public Access", result.Results[0].Title);
            Assert.Equal("Unencrypted EBS", result.Results[1].Title);
            Assert.Equal("Open Security Group", result.Results[2].Title);
        }

        [Fact]
        public void Deserialize_IssueWithNullLocations_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{
                ""Results"": [
                    {
                        ""Title"": ""IAC Issue"",
                        ""Locations"": null
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<IacRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results.First().Locations);
            Assert.Empty(result.Results.First().Locations);
        }
    }
}

