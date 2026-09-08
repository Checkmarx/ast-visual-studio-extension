using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using System;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    /// <summary>
    /// Additional tests for TempFileManager covering the three completely untested factory methods
    /// (CreateIacTempDir, CreateContainersTempDir, CreateOssTempDir), DeleteTempDirectory,
    /// TryGetVerifiedRegularFileInfo, and SanitizeFilename edge cases.
    /// </summary>
    public class TempFileManagerAdditionalTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // CreateIacTempDir
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void CreateIacTempDir_ValidHash_CreatesDirectory()
        {
            var dir = TempFileManager.CreateIacTempDir("abc12345");
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(dir); }
        }

        [Fact]
        public void CreateIacTempDir_ValidHash_PathContainsHash()
        {
            var dir = TempFileManager.CreateIacTempDir("myhash");
            try
            {
                Assert.Contains("myhash", dir);
            }
            finally { TempFileManager.DeleteTempDirectory(dir); }
        }

        [Fact]
        public void CreateIacTempDir_ValidHash_PathContainsIacDirName()
        {
            var dir = TempFileManager.CreateIacTempDir("abc12345");
            try
            {
                Assert.Contains("Cx-iac-realtime-scanner", dir);
            }
            finally { TempFileManager.DeleteTempDirectory(dir); }
        }

        [Fact]
        public void CreateIacTempDir_NullHash_StillCreatesDirectory()
        {
            // Null hash → auto-generated GUID replaces it
            var dir = TempFileManager.CreateIacTempDir(null);
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(dir); }
        }

        [Fact]
        public void CreateIacTempDir_EmptyHash_StillCreatesDirectory()
        {
            var dir = TempFileManager.CreateIacTempDir("");
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(dir); }
        }

        [Fact]
        public void CreateIacTempDir_SameHash_ReturnsSamePath()
        {
            var dir1 = TempFileManager.CreateIacTempDir("fixed-hash");
            var dir2 = TempFileManager.CreateIacTempDir("fixed-hash");
            try
            {
                // Same hash → same sub-directory path (idempotent)
                Assert.Equal(dir1, dir2);
            }
            finally { TempFileManager.DeleteTempDirectory(dir1); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CreateContainersTempDir
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void CreateContainersTempDir_ValidHash_CreatesDirectory()
        {
            var dir = TempFileManager.CreateContainersTempDir("abc12345");
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateContainersTempDir_ValidHash_PathContainsContainersDirName()
        {
            var dir = TempFileManager.CreateContainersTempDir("abc12345");
            try
            {
                Assert.Contains("Cx-container-realtime-scanner", dir);
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateContainersTempDir_NotHelmFile_DoesNotContainHelmSegment()
        {
            var dir = TempFileManager.CreateContainersTempDir("abc12345", isHelmFile: false);
            try
            {
                Assert.DoesNotContain("helm", dir);
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateContainersTempDir_HelmFile_PathContainsHelmSubfolder()
        {
            var dir = TempFileManager.CreateContainersTempDir("abc12345", isHelmFile: true);
            try
            {
                Assert.True(Directory.Exists(dir));
                Assert.EndsWith("helm", dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            finally
            {
                // Parent is hash dir, grandparent is base dir
                var parent = Path.GetDirectoryName(dir);
                TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(parent));
            }
        }

        [Fact]
        public void CreateContainersTempDir_NullHash_StillCreatesDirectory()
        {
            var dir = TempFileManager.CreateContainersTempDir(null);
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CreateOssTempDir
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void CreateOssTempDir_ValidHash_CreatesDirectory()
        {
            var dir = TempFileManager.CreateOssTempDir("abc12345");
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateOssTempDir_ValidHash_PathContainsOssDirName()
        {
            var dir = TempFileManager.CreateOssTempDir("abc12345");
            try
            {
                Assert.Contains("Cx-oss-realtime-scanner", dir);
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateOssTempDir_NullHash_StillCreatesDirectory()
        {
            var dir = TempFileManager.CreateOssTempDir(null);
            try
            {
                Assert.True(Directory.Exists(dir));
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir)); }
        }

        [Fact]
        public void CreateOssTempDir_SameHash_ReturnsSamePath()
        {
            var dir1 = TempFileManager.CreateOssTempDir("manifest-hash");
            var dir2 = TempFileManager.CreateOssTempDir("manifest-hash");
            try
            {
                Assert.Equal(dir1, dir2);
            }
            finally { TempFileManager.DeleteTempDirectory(Path.GetDirectoryName(dir1)); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DeleteTempDirectory
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void DeleteTempDirectory_ExistingEmptyDir_DeletesIt()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            TempFileManager.DeleteTempDirectory(dir);

            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void DeleteTempDirectory_DirWithFiles_DeletesAllContents()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "a.txt"), "data");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "more");

            TempFileManager.DeleteTempDirectory(dir);

            Assert.False(Directory.Exists(dir));
        }

        [Fact]
        public void DeleteTempDirectory_DirWithSubdirs_DeletesRecursively()
        {
            var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var sub  = Directory.CreateDirectory(Path.Combine(root, "sub")).FullName;
            File.WriteAllText(Path.Combine(sub, "file.txt"), "x");

            TempFileManager.DeleteTempDirectory(root);

            Assert.False(Directory.Exists(root));
        }

        [Fact]
        public void DeleteTempDirectory_NullPath_DoesNotThrow()
        {
            TempFileManager.DeleteTempDirectory(null);
        }

        [Fact]
        public void DeleteTempDirectory_EmptyPath_DoesNotThrow()
        {
            TempFileManager.DeleteTempDirectory("");
        }

        [Fact]
        public void DeleteTempDirectory_NonExistentPath_DoesNotThrow()
        {
            TempFileManager.DeleteTempDirectory(@"C:\this_path_does_not_exist_xyz_abc");
        }

        [Fact]
        public void DeleteTempDirectory_CalledTwice_DoesNotThrow()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            TempFileManager.DeleteTempDirectory(dir);
            TempFileManager.DeleteTempDirectory(dir); // already gone — should not throw
        }

        // ══════════════════════════════════════════════════════════════════════
        // TryGetVerifiedRegularFileInfo
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void TryGetVerifiedRegularFileInfo_ValidFile_ReturnsTrueWithFileInfo()
        {
            var path = Path.GetTempFileName();
            try
            {
                bool result = TempFileManager.TryGetVerifiedRegularFileInfo(path, out var fi);
                Assert.True(result);
                Assert.NotNull(fi);
                Assert.Equal(Path.GetFullPath(path), fi.FullName);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void TryGetVerifiedRegularFileInfo_NonExistentFile_ReturnsFalse()
        {
            bool result = TempFileManager.TryGetVerifiedRegularFileInfo(
                @"C:\nonexistent_file_xyz.cs", out var fi);
            Assert.False(result);
            Assert.Null(fi);
        }

        [Fact]
        public void TryGetVerifiedRegularFileInfo_NullPath_ReturnsFalse()
        {
            bool result = TempFileManager.TryGetVerifiedRegularFileInfo(null, out var fi);
            Assert.False(result);
            Assert.Null(fi);
        }

        [Fact]
        public void TryGetVerifiedRegularFileInfo_EmptyPath_ReturnsFalse()
        {
            bool result = TempFileManager.TryGetVerifiedRegularFileInfo("", out var fi);
            Assert.False(result);
            Assert.Null(fi);
        }

        [Fact]
        public void TryGetVerifiedRegularFileInfo_WhitespacePath_ReturnsFalse()
        {
            bool result = TempFileManager.TryGetVerifiedRegularFileInfo("   ", out var fi);
            Assert.False(result);
            Assert.Null(fi);
        }

        [Fact]
        public void TryGetVerifiedRegularFileInfo_PathWithNullByte_ReturnsFalse()
        {
            // Null byte is a classic path traversal / injection vector
            bool result = TempFileManager.TryGetVerifiedRegularFileInfo(
                "C:\\file\0.txt", out var fi);
            Assert.False(result);
            Assert.Null(fi);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SanitizeFilename — edge cases not in existing tests
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void SanitizeFilename_NullInput_ReturnsSafeDefault()
        {
            var result = TempFileManager.SanitizeFilename(null, 255);
            Assert.False(string.IsNullOrEmpty(result));
            Assert.DoesNotContain("\0", result, StringComparison.Ordinal);
        }

        [Fact]
        public void SanitizeFilename_EmptyInput_ReturnsSafeDefault()
        {
            var result = TempFileManager.SanitizeFilename("", 255);
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void SanitizeFilename_DotOnlyName_PreservesExtension()
        {
            // ".editorconfig" — stem is empty, should produce "file.editorconfig"
            var result = TempFileManager.SanitizeFilename(".editorconfig", 255);
            Assert.EndsWith(".editorconfig", result);
        }

        [Fact]
        public void SanitizeFilename_DoubleDot_IsReplaced()
        {
            // ".." in stem is a directory traversal vector — must be replaced
            var result = TempFileManager.SanitizeFilename("..\\..\\evil.cs", 255);
            Assert.DoesNotContain("..", result);
        }

        [Fact]
        public void SanitizeFilename_NoExtension_AddsDatExtension()
        {
            // Files with no extension get .dat so ASCA CLI has a valid extension
            var result = TempFileManager.SanitizeFilename("Makefile", 255);
            Assert.EndsWith(".dat", result);
        }

        [Fact]
        public void SanitizeFilename_NormalCsFile_PreservesExtension()
        {
            var result = TempFileManager.SanitizeFilename("Program.cs", 255);
            Assert.EndsWith(".cs", result);
            Assert.Contains("Program", result);
        }

        [Fact]
        public void SanitizeFilename_MaxLengthOne_ReturnsOneCharPlusExtension()
        {
            // maxLength=5, extension=".cs"(3), stem truncated to 2
            var result = TempFileManager.SanitizeFilename("Program.cs", 5);
            Assert.True(result.Length <= 5);
        }

        [Fact]
        public void SanitizeFilename_ResultNeverExceedsMaxLength()
        {
            const int max = 20;
            var result = TempFileManager.SanitizeFilename(new string('A', 300) + ".cs", max);
            Assert.True(result.Length <= max);
        }

        [Fact]
        public void SanitizeFilename_PathWithDirectory_OnlyKeepsFileName()
        {
            // Full path passed in — should strip directory components
            var result = TempFileManager.SanitizeFilename(@"C:\Users\test\project\app.js", 255);
            Assert.DoesNotContain(":\\", result);
            Assert.EndsWith(".js", result);
        }
    }
}
