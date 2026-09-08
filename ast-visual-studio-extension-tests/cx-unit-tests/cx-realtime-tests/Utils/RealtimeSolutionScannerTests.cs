using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class RealtimeSolutionScannerTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // EnumerateFiles — null / empty / non-existent directory
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void EnumerateFiles_NullDirectory_ReturnsEmpty()
        {
            var files = RealtimeSolutionScanner.EnumerateFiles(null).ToList();
            Assert.Empty(files);
        }

        [Fact]
        public void EnumerateFiles_EmptyDirectory_ReturnsEmpty()
        {
            var files = RealtimeSolutionScanner.EnumerateFiles("").ToList();
            Assert.Empty(files);
        }

        [Fact]
        public void EnumerateFiles_NonExistentDirectory_ReturnsEmpty()
        {
            var files = RealtimeSolutionScanner.EnumerateFiles(@"C:\this_path_does_not_exist_xyz").ToList();
            Assert.Empty(files);
        }

        // ══════════════════════════════════════════════════════════════════════
        // EnumerateFiles — real directory operations
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void EnumerateFiles_EmptyFolder_ReturnsEmpty()
        {
            var root = CreateTempDir();
            try
            {
                Assert.Empty(RealtimeSolutionScanner.EnumerateFiles(root));
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_SingleFile_ReturnsFile()
        {
            var root = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(root, "app.cs"), "code");
                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Single(files);
                Assert.Contains("app.cs", files[0]);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_FilesInSubfolder_ReturnsAll()
        {
            var root = CreateTempDir();
            try
            {
                var sub = Directory.CreateDirectory(Path.Combine(root, "src"));
                File.WriteAllText(Path.Combine(root,    "Program.cs"),      "root file");
                File.WriteAllText(Path.Combine(sub.FullName, "Helper.cs"),  "sub file");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Equal(2, files.Count);
            }
            finally { CleanupDir(root); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // EnumerateFiles — skipped directories
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("node_modules")]
        [InlineData("bin")]
        [InlineData("obj")]
        [InlineData(".git")]
        [InlineData(".vs")]
        [InlineData("packages")]
        [InlineData("dist")]
        [InlineData("build")]
        [InlineData("out")]
        [InlineData("target")]
        [InlineData("__pycache__")]
        [InlineData(".pytest_cache")]
        [InlineData("TestResults")]
        [InlineData("coverage")]
        public void EnumerateFiles_FilesInsideSkippedDirectory_AreExcluded(string skippedDir)
        {
            var root = CreateTempDir();
            try
            {
                var skipped = Directory.CreateDirectory(Path.Combine(root, skippedDir));
                File.WriteAllText(Path.Combine(skipped.FullName, "secret.js"), "data");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Empty(files);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_FileAtRootLevel_NotSkipped()
        {
            var root = CreateTempDir();
            try
            {
                // File directly in root should NOT be excluded even if root path contains "bin"
                File.WriteAllText(Path.Combine(root, "package.json"), "{}");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Single(files);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_SkippedAndNonSkippedDirs_OnlyReturnsNonSkipped()
        {
            var root = CreateTempDir();
            try
            {
                // src/app.cs  → should be returned
                var src = Directory.CreateDirectory(Path.Combine(root, "src"));
                File.WriteAllText(Path.Combine(src.FullName, "app.cs"), "code");

                // node_modules/lodash.js → should be excluded
                var nm = Directory.CreateDirectory(Path.Combine(root, "node_modules"));
                File.WriteAllText(Path.Combine(nm.FullName, "lodash.js"), "lib");

                // bin/debug.dll → should be excluded
                var bin = Directory.CreateDirectory(Path.Combine(root, "bin"));
                File.WriteAllText(Path.Combine(bin.FullName, "debug.dll"), "binary");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Single(files);
                Assert.Contains("app.cs", files[0]);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_NestedSkippedDirectory_IsFullyExcluded()
        {
            var root = CreateTempDir();
            try
            {
                // src/node_modules/dep.js → node_modules is nested but should still be skipped
                var src = Directory.CreateDirectory(Path.Combine(root, "src"));
                var nm  = Directory.CreateDirectory(Path.Combine(src.FullName, "node_modules"));
                File.WriteAllText(Path.Combine(nm.FullName, "dep.js"), "lib");

                // src/main.js → should be returned
                File.WriteAllText(Path.Combine(src.FullName, "main.js"), "app");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Single(files);
                Assert.Contains("main.js", files[0]);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_CaseSensitivity_SkippedDirCaseInsensitive()
        {
            var root = CreateTempDir();
            try
            {
                // "NODE_MODULES" (uppercase) — should also be excluded
                var upper = Directory.CreateDirectory(Path.Combine(root, "NODE_MODULES"));
                File.WriteAllText(Path.Combine(upper.FullName, "lib.js"), "data");

                var files = RealtimeSolutionScanner.EnumerateFiles(root).ToList();
                Assert.Empty(files);
            }
            finally { CleanupDir(root); }
        }

        [Fact]
        public void EnumerateFiles_TrailingSlashOnRoot_StillWorks()
        {
            var root = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(root, "app.js"), "code");

                // Add trailing directory separator
                var files = RealtimeSolutionScanner.EnumerateFiles(root + Path.DirectorySeparatorChar).ToList();
                Assert.Single(files);
            }
            finally { CleanupDir(root); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TryGetSolutionDirectory — cannot be unit-tested without VS context,
        // but we verify it doesn't throw when called outside of VS.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void TryGetSolutionDirectory_OutsideVsContext_DoesNotThrow()
        {
            // Outside VS, DTE service is null — should return null without throwing
            var dir = RealtimeSolutionScanner.TryGetSolutionDirectory();
            // Result is null outside VS — acceptable
            Assert.True(dir == null || Directory.Exists(dir));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════

        private static string CreateTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void CleanupDir(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch { /* best-effort */ }
        }
    }
}
