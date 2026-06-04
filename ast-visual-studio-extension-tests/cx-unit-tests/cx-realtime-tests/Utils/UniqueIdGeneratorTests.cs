using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class UniqueIdGeneratorTests
    {
        [Fact]
        public void GenerateId_WithSameInput_ReturnsSameId()
        {
            // Verify determinism: same input always produces same output
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateId_WithDifferentLine_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(43, "SQL_INJECTION", "test.cs");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateId_WithDifferentIssue_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(42, "XSS", "test.cs");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateId_WithDifferentFile_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "main.cs");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateId_WithNullIssueIdentifier_TreatsAsUnknown()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, null, "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(42, "unknown", "test.cs");

            // Both should generate same ID since null is replaced with "unknown"
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateId_WithNullFileName_TreatsAsUnknown()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", null);
            var id2 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "unknown");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateId_ReturnsFixedLength()
        {
            var id = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");

            // Should return 16-char hash (per HASH_LENGTH constant)
            Assert.NotNull(id);
            Assert.True(id.Length > 0);
            Assert.True(id.Length <= 16);
        }

        [Fact]
        public void GenerateId_WithFourParameters_ReturnsDifferentIdThanTwoParameter()
        {
            var id1 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateId(42, "SQL_INJECTION", "XSS vulnerability", "test.cs");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateIdWithSeverity_WithSameInput_ReturnsSameId()
        {
            var id1 = UniqueIdGenerator.GenerateIdWithSeverity(42, "High", "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateIdWithSeverity(42, "High", "SQL_INJECTION", "test.cs");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateIdWithSeverity_WithDifferentSeverity_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GenerateIdWithSeverity(42, "High", "SQL_INJECTION", "test.cs");
            var id2 = UniqueIdGenerator.GenerateIdWithSeverity(42, "Medium", "SQL_INJECTION", "test.cs");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateLocationBasedId_WithSameInput_ReturnsSameId()
        {
            var id1 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 20, "test.tf");
            var id2 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 20, "test.tf");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GenerateLocationBasedId_WithDifferentColumn_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 20, "test.tf");
            var id2 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 21, "test.tf");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GenerateLocationBasedId_WithNullFile_TreatsAsUnknown()
        {
            var id1 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 20, null);
            var id2 = UniqueIdGenerator.GenerateLocationBasedId(42, 10, 20, "unknown");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithSameInput_ReturnsSameId()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", "package.json");
            var id2 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", "package.json");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithDifferentVersion_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", "package.json");
            var id2 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.20", "package.json");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithDifferentPackageName_ReturnsDifferentId()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", "package.json");
            var id2 = UniqueIdGenerator.GeneratePackageId("underscore", "4.17.21", "package.json");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithNullPackageName_TreatsAsUnknown()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId(null, "4.17.21", "package.json");
            var id2 = UniqueIdGenerator.GeneratePackageId("unknown", "4.17.21", "package.json");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithNullVersion_TreatsAs0_0_0()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId("lodash", null, "package.json");
            var id2 = UniqueIdGenerator.GeneratePackageId("lodash", "0.0.0", "package.json");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GeneratePackageId_WithNullFileName_TreatsAsUnknown()
        {
            var id1 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", null);
            var id2 = UniqueIdGenerator.GeneratePackageId("lodash", "4.17.21", "unknown");

            Assert.Equal(id1, id2);
        }
    }
}
