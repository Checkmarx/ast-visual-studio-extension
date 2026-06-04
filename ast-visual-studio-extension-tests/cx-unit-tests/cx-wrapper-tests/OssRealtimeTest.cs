using ast_visual_studio_extension.CxWrapper.Models;
using System.IO;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class OssRealtimeTest : BaseTest
    {
        private readonly string TEST_DATA_PATH = "test-data";

        [Fact]
        public void TestOssRealtimeScan_BasicScan()
        {
            // Arrange
            string sourcePath = Path.Combine(TEST_DATA_PATH, "package.json");

            // Act
            OssRealtimeResults result = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Packages);
            Assert.True(result.Packages.Count > 0, "Should detect npm dependencies in package.json");
        }

        [Fact]
        public void TestOssRealtimeScan_PackageFieldsPopulated()
        {
            // Arrange
            string sourcePath = Path.Combine(TEST_DATA_PATH, "package.json");

            // Act
            OssRealtimeResults result = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Packages.Count > 0, "Should have packages to validate mapping");

            var samplePackage = result.Packages.First();
            Assert.NotNull(samplePackage.PackageName);
            Assert.NotNull(samplePackage.PackageVersion);
            Assert.NotNull(samplePackage.Status);
            Assert.NotNull(samplePackage.Locations);
            Assert.NotNull(samplePackage.Vulnerabilities);
        }

        [Fact]
        public void TestOssRealtimeScan_WithIgnoreFile()
        {
            // Arrange
            string sourcePath = Path.Combine(TEST_DATA_PATH, "package.json");
            string ignoredFilePath = Path.Combine(TEST_DATA_PATH, "ignored-packages.json");

            // Act
            OssRealtimeResults baseline = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );
            OssRealtimeResults filtered = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath,
                ignoredFilePath: ignoredFilePath
            );

            // Assert
            Assert.NotNull(baseline);
            Assert.NotNull(filtered);
            Assert.NotNull(baseline.Packages);
            Assert.NotNull(filtered.Packages);
            Assert.True(filtered.Packages.Count <= baseline.Packages.Count,
                "Filtered scan should have same or fewer packages than baseline");
        }

        [Fact]
        public void TestOssRealtimeScan_ConsistentResults()
        {
            // Arrange
            string sourcePath = Path.Combine(TEST_DATA_PATH, "package.json");

            // Act
            OssRealtimeResults firstScan = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );
            OssRealtimeResults secondScan = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );

            // Assert
            Assert.NotNull(firstScan);
            Assert.NotNull(secondScan);
            Assert.Equal(firstScan.Packages.Count, secondScan.Packages.Count);
        }

        [Fact]
        public void TestOssRealtimeScan_VulnerablePackagesHaveDetails()
        {
            // Arrange
            string sourcePath = Path.Combine(TEST_DATA_PATH, "package.json");

            // Act
            OssRealtimeResults result = cxWrapper.OssRealtimeScan(
                sourcePath: sourcePath
            );

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Packages);

            var vulnerablePackages = result.Packages
                .Where(p => p.Vulnerabilities != null && p.Vulnerabilities.Count > 0)
                .ToList();

            Assert.True(vulnerablePackages.Count > 0,
                "Should find at least one package with vulnerabilities (lodash 4.17.0 / express 4.16.0 have known CVEs)");

            foreach (var pkg in vulnerablePackages)
            {
                foreach (var vuln in pkg.Vulnerabilities)
                {
                    Assert.False(string.IsNullOrEmpty(vuln.Cve), "Vulnerability CVE should not be empty");
                    Assert.False(string.IsNullOrEmpty(vuln.Severity), "Vulnerability Severity should not be empty");
                }
            }
        }
    }
}
