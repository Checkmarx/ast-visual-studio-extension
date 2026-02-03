using ast_visual_studio_extension.CxWrapper.Models;
using Newtonsoft.Json;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class OssRealtimeResultsTests
    {
        [Fact]
        public void Deserialize_ValidJson_ReturnsResults()
        {
            // Arrange
            string json = @"{
                ""Packages"": [
                    {
                        ""PackageManager"": ""npm"",
                        ""PackageName"": ""lodash"",
                        ""PackageVersion"": ""4.17.0"",
                        ""FilePath"": ""package.json"",
                        ""Status"": ""vulnerable"",
                        ""Locations"": [{""Line"": 10, ""StartIndex"": 4, ""EndIndex"": 20}],
                        ""Vulnerabilities"": [
                            {
                                ""CVE"": ""CVE-2021-23337"",
                                ""Severity"": ""high"",
                                ""Description"": ""Prototype pollution"",
                                ""FixVersion"": ""4.17.21""
                            }
                        ]
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Packages);

            var package = result.Packages.First();
            Assert.Equal("npm", package.PackageManager);
            Assert.Equal("lodash", package.PackageName);
            Assert.Equal("4.17.0", package.PackageVersion);
            Assert.Equal("package.json", package.FilePath);
            Assert.Equal("vulnerable", package.Status);

            Assert.Single(package.Locations);
            Assert.Equal(10, package.Locations.First().Line);

            Assert.Single(package.Vulnerabilities);
            Assert.Equal("CVE-2021-23337", package.Vulnerabilities.First().Cve);
            Assert.Equal("high", package.Vulnerabilities.First().Severity);
            Assert.Equal("4.17.21", package.Vulnerabilities.First().FixVersion);
        }

        [Fact]
        public void Deserialize_NullPackages_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Packages"": null}";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Packages);
            Assert.Empty(result.Packages);
        }

        [Fact]
        public void Deserialize_EmptyPackagesArray_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{""Packages"": []}";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Packages);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptyPackages()
        {
            // Arrange
            string json = @"{}";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Packages);
        }

        [Fact]
        public void Deserialize_MultiplePackages_AllParsed()
        {
            // Arrange
            string json = @"{
                ""Packages"": [
                    {""PackageManager"": ""npm"", ""PackageName"": ""lodash"", ""PackageVersion"": ""4.17.0""},
                    {""PackageManager"": ""npm"", ""PackageName"": ""express"", ""PackageVersion"": ""4.16.0""},
                    {""PackageManager"": ""maven"", ""PackageName"": ""log4j"", ""PackageVersion"": ""2.14.0""}
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Packages.Count);
            Assert.Equal("lodash", result.Packages[0].PackageName);
            Assert.Equal("express", result.Packages[1].PackageName);
            Assert.Equal("log4j", result.Packages[2].PackageName);
        }

        [Fact]
        public void Deserialize_PackageWithNullVulnerabilities_ReturnsEmptyList()
        {
            // Arrange
            string json = @"{
                ""Packages"": [
                    {
                        ""PackageName"": ""safe-package"",
                        ""Vulnerabilities"": null
                    }
                ]
            }";

            // Act
            var result = JsonConvert.DeserializeObject<OssRealtimeResults>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Packages);
            Assert.NotNull(result.Packages.First().Vulnerabilities);
            Assert.Empty(result.Packages.First().Vulnerabilities);
        }
    }
}

