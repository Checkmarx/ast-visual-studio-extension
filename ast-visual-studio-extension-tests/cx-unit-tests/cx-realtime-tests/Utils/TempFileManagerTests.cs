using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using System;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class TempFileManagerTests : IDisposable
    {
        private readonly string _testTempDir = Path.Combine(Path.GetTempPath(), "CxTempFileManagerTests");

        public TempFileManagerTests()
        {
            if (Directory.Exists(_testTempDir))
                Directory.Delete(_testTempDir, true);
            Directory.CreateDirectory(_testTempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testTempDir))
                Directory.Delete(_testTempDir, true);
        }

        [Fact]
        public void CreateAscaTempFile_WithValidContent_CreatesFile()
        {
            var fileName = "test.cs";
            var content = "public class Test { }";

            var tempFilePath = TempFileManager.CreateAscaTempFile(fileName, content);

            Assert.True(File.Exists(tempFilePath));
            Assert.Contains("cx-asca-", tempFilePath);
            Assert.Contains("test.cs", tempFilePath);
        }

        [Fact]
        public void CreateAscaTempFile_WithValidContent_WritesCorrectContent()
        {
            var fileName = "test.cs";
            var content = "public class Test { }";

            var tempFilePath = TempFileManager.CreateAscaTempFile(fileName, content);

            try
            {
                var fileContent = File.ReadAllText(tempFilePath);
                Assert.Equal(content, fileContent);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void CreateAscaTempFile_WithSpecialCharacters_SanitizesFilename()
        {
            var fileName = "test<>:*.cs";
            var content = "code";

            var tempFilePath = TempFileManager.CreateAscaTempFile(fileName, content);

            Assert.True(File.Exists(tempFilePath));
            // Should not contain invalid characters
            var fileInfo = new FileInfo(tempFilePath);
            Assert.DoesNotContain("<", fileInfo.Name);
            Assert.DoesNotContain(">", fileInfo.Name);
            Assert.DoesNotContain(":", fileInfo.Name);
            Assert.DoesNotContain("*", fileInfo.Name);

            try
            {
                File.Delete(tempFilePath);
            }
            catch { }
        }

        [Fact]
        public void CreateAscaTempFile_WithLongFileName_TruncatesTo255Chars()
        {
            var longFileName = new string('a', 300) + ".cs";
            var content = "code";

            var tempFilePath = TempFileManager.CreateAscaTempFile(longFileName, content);

            try
            {
                var fileInfo = new FileInfo(tempFilePath);
                Assert.True(fileInfo.Name.Length <= 255);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void CreateSecretsTempDir_WithContent_CreatesDirectory()
        {
            var content = "CONFIG_VALUE=" + Guid.NewGuid().ToString();

            var tempDir = TempFileManager.CreateSecretsTempDir(content);

            Assert.True(Directory.Exists(tempDir));
            Assert.Contains("cx-secrets-", tempDir);
        }

        [Fact]
        public void CreateSecretsTempDir_WithDifferentContent_CreatesDifferentDirectories()
        {
            var content1 = "CONFIG_VALUE=" + Guid.NewGuid().ToString();
            var content2 = "CONFIG_VALUE=" + Guid.NewGuid().ToString();

            var tempDir1 = TempFileManager.CreateSecretsTempDir(content1);
            var tempDir2 = TempFileManager.CreateSecretsTempDir(content2);

            try
            {
                Assert.NotEqual(tempDir1, tempDir2);
            }
            finally
            {
                if (Directory.Exists(tempDir1))
                    Directory.Delete(tempDir1, true);
                if (Directory.Exists(tempDir2))
                    Directory.Delete(tempDir2, true);
            }
        }

        [Fact]
        public void CreateSecretsTempDir_WithSameContent_CreatesDifferentDirectories()
        {
            var content = "CONFIG_VALUE=" + Guid.NewGuid().ToString();

            var tempDir1 = TempFileManager.CreateSecretsTempDir(content);
            var tempDir2 = TempFileManager.CreateSecretsTempDir(content);

            try
            {
                // Different directories due to UUID and timestamp
                Assert.NotEqual(tempDir1, tempDir2);
                // But both contain hash
                Assert.Contains("cx-secrets-", tempDir1);
                Assert.Contains("cx-secrets-", tempDir2);
            }
            finally
            {
                if (Directory.Exists(tempDir1))
                    Directory.Delete(tempDir1, true);
                if (Directory.Exists(tempDir2))
                    Directory.Delete(tempDir2, true);
            }
        }

        [Fact]
        public void SanitizeFilename_WithPathSeparators_RemovesThem()
        {
            var fileName = "path\\to\\file.cs";
            var sanitized = TempFileManager.SanitizeFilename(fileName, 255);

            Assert.DoesNotContain("\\", sanitized);
            Assert.DoesNotContain("/", sanitized);
        }

        [Fact]
        public void SanitizeFilename_WithSpecialCharacters_RemovesThem()
        {
            var fileName = "test<>:|?.cs";
            var sanitized = TempFileManager.SanitizeFilename(fileName, 255);

            Assert.DoesNotContain("<", sanitized);
            Assert.DoesNotContain(">", sanitized);
            Assert.DoesNotContain(":", sanitized);
            Assert.DoesNotContain("|", sanitized);
            Assert.DoesNotContain("?", sanitized);
        }

        [Fact]
        public void SanitizeFilename_WithLongName_RespectMaxLength()
        {
            var longName = new string('a', 300);
            var sanitized = TempFileManager.SanitizeFilename(longName, 100);

            Assert.True(sanitized.Length <= 100);
        }

        [Fact]
        public void SanitizeFilename_WithValidName_KeepsIt()
        {
            var fileName = "valid_filename-123.cs";
            var sanitized = TempFileManager.SanitizeFilename(fileName, 255);

            Assert.Equal(fileName, sanitized);
        }

        [Fact]
        public void GetContentHash_WithSameContent_ReturnsSameHash()
        {
            var content = "same content";

            var hash1 = TempFileManager.GetContentHash(content);
            var hash2 = TempFileManager.GetContentHash(content);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetContentHash_WithDifferentContent_ReturnsDifferentHash()
        {
            var hash1 = TempFileManager.GetContentHash("content1");
            var hash2 = TempFileManager.GetContentHash("content2");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GetContentHash_WithNullContent_ReturnsValidHash()
        {
            var hash = TempFileManager.GetContentHash(null);

            Assert.NotNull(hash);
            Assert.True(hash.Length > 0);
        }

        [Fact]
        public void GetContentHash_ReturnsReasonableLength()
        {
            var hash = TempFileManager.GetContentHash("test content");

            // Should be reasonable length (not full SHA-256 hex)
            Assert.True(hash.Length > 0);
            Assert.True(hash.Length <= 100);
        }

        [Fact]
        public void GetContentHash_WithCustomLength_RespectsMaxLength()
        {
            var hash = TempFileManager.GetContentHash("test content", length: 8);

            Assert.True(hash.Length <= 8);
        }
    }
}
