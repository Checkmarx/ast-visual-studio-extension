using ast_visual_studio_extension.CxWrapper.Models;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class ASCATest : BaseTest
    {
        private readonly string TEST_DATA_PATH = "test-data";


        [Fact]
        public void TestInstallAsca()
        {
            CxAsca result = cxWrapper.ScanAsca(
                fileSource: "",
                ascaLatestVersion: true,
                agent: "Visual Studio Test"
            );
            Assert.NotNull(result);
        }

        [Fact]
        public void TestScanAsca_CSharpVulnerable()
        {
            // Arrange
            string filePath = Path.Combine(TEST_DATA_PATH, "python-vul-file.py");

            // Act
            CxAsca result = cxWrapper.ScanAsca(
                fileSource: filePath,
                ascaLatestVersion: false,
                agent: "Visual Studio Test"
            );

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ScanDetails);
            Assert.True(result.ScanDetails.Count > 0, "Should find at least one vulnerability");
        }

        [Fact]
        public void TestScanAsca_NoExtension()
        {
            // Arrange
            string filePath = Path.Combine(TEST_DATA_PATH, "no-extension");

            // Act
            CxAsca result = cxWrapper.ScanAsca(
                fileSource: filePath,
                ascaLatestVersion: false,
                agent: "Visual Studio Test"
            );

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("The file name must have an extension.", result.Error.Description);
        }

        [Fact]
        public void TestScanAsca_CSharpClean()
        {
            // Arrange
            string filePath = Path.Combine(TEST_DATA_PATH, "csharp-clean.cs");

            // Act
            CxAsca result = cxWrapper.ScanAsca(
                fileSource: filePath,
                ascaLatestVersion: false,
                agent: "Visual Studio Test"
            );

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);  
            Assert.Empty(result.ScanDetails);  
        }
    }
}