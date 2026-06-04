using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using System.IO;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class CompanionFileManagerTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // HasCompanionFiles
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("package.json")]
        [InlineData("pom.xml")]
        [InlineData(".csproj")]
        [InlineData("go.mod")]
        [InlineData("requirements.txt")]
        [InlineData("Gemfile")]
        [InlineData("composer.json")]
        public void HasCompanionFiles_KnownManifest_ReturnsTrue(string manifest)
        {
            Assert.True(CompanionFileManager.HasCompanionFiles(manifest));
        }

        [Theory]
        [InlineData("build.gradle")]
        [InlineData("webpack.config.js")]
        [InlineData("Dockerfile")]
        [InlineData("unknown.txt")]
        [InlineData("")]
        public void HasCompanionFiles_UnknownManifest_ReturnsFalse(string manifest)
        {
            Assert.False(CompanionFileManager.HasCompanionFiles(manifest));
        }

        [Fact]
        public void HasCompanionFiles_Null_ReturnsFalse()
        {
            Assert.False(CompanionFileManager.HasCompanionFiles(null));
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetCompanionFileNames
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void GetCompanionFileNames_PackageJson_ReturnsNpmLockFiles()
        {
            var files = CompanionFileManager.GetCompanionFileNames("package.json");
            Assert.Contains("package-lock.json",     files);
            Assert.Contains("yarn.lock",             files);
            Assert.Contains("npm-shrinkwrap.json",   files);
        }

        [Fact]
        public void GetCompanionFileNames_PomXml_ReturnsMavenLock()
        {
            var files = CompanionFileManager.GetCompanionFileNames("pom.xml");
            Assert.Contains("pom.xml.lock", files);
        }

        [Fact]
        public void GetCompanionFileNames_CsProj_ReturnsDotNetLock()
        {
            var files = CompanionFileManager.GetCompanionFileNames(".csproj");
            Assert.Contains("packages.lock.json", files);
        }

        [Fact]
        public void GetCompanionFileNames_GoMod_ReturnsGoSum()
        {
            var files = CompanionFileManager.GetCompanionFileNames("go.mod");
            Assert.Contains("go.sum", files);
        }

        [Fact]
        public void GetCompanionFileNames_RequirementsTxt_ReturnsPythonLocks()
        {
            var files = CompanionFileManager.GetCompanionFileNames("requirements.txt");
            Assert.Contains("requirements.lock", files);
            Assert.Contains("Pipfile.lock",       files);
        }

        [Fact]
        public void GetCompanionFileNames_Gemfile_ReturnsGemfileLock()
        {
            var files = CompanionFileManager.GetCompanionFileNames("Gemfile");
            Assert.Contains("Gemfile.lock", files);
        }

        [Fact]
        public void GetCompanionFileNames_ComposerJson_ReturnsComposerLock()
        {
            var files = CompanionFileManager.GetCompanionFileNames("composer.json");
            Assert.Contains("composer.lock", files);
        }

        [Fact]
        public void GetCompanionFileNames_UnknownManifest_ReturnsEmptyArray()
        {
            var files = CompanionFileManager.GetCompanionFileNames("Dockerfile");
            Assert.Empty(files);
        }

        [Fact]
        public void GetCompanionFileNames_Null_ReturnsEmptyArray()
        {
            var files = CompanionFileManager.GetCompanionFileNames(null);
            Assert.Empty(files);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CopyCompanionLockFiles — guard clauses
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void CopyCompanionLockFiles_NullManifestPath_DoesNotThrow()
        {
            var tempDir = Path.GetTempPath();
            CompanionFileManager.CopyCompanionLockFiles(null, tempDir); // should silently return
        }

        [Fact]
        public void CopyCompanionLockFiles_NullTempDir_DoesNotThrow()
        {
            CompanionFileManager.CopyCompanionLockFiles(@"C:\project\package.json", null);
        }

        [Fact]
        public void CopyCompanionLockFiles_EmptyManifestPath_DoesNotThrow()
        {
            CompanionFileManager.CopyCompanionLockFiles("", Path.GetTempPath());
        }

        [Fact]
        public void CopyCompanionLockFiles_EmptyTempDir_DoesNotThrow()
        {
            CompanionFileManager.CopyCompanionLockFiles(@"C:\project\package.json", "");
        }

        // ══════════════════════════════════════════════════════════════════════
        // CopyCompanionLockFiles — real file operations
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void CopyCompanionLockFiles_PackageJsonWithLockFile_CopiesPackageLockJson()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                // Create package.json and package-lock.json in source
                File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");
                File.WriteAllText(Path.Combine(sourceDir, "package-lock.json"), "{}");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "package.json"), targetDir);

                Assert.True(File.Exists(Path.Combine(targetDir, "package-lock.json")));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_PackageJsonWithYarnLock_CopiesYarnLock()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");
                File.WriteAllText(Path.Combine(sourceDir, "yarn.lock"), "# yarn");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "package.json"), targetDir);

                Assert.True(File.Exists(Path.Combine(targetDir, "yarn.lock")));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_PackageJsonNoLockFiles_DoesNotCreateFiles()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                // Only package.json exists, no lock files
                File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "package.json"), targetDir);

                // No lock files should appear in target
                Assert.Empty(Directory.GetFiles(targetDir));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_GoMod_CopiesGoSum()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(sourceDir, "go.mod"), "module test");
                File.WriteAllText(Path.Combine(sourceDir, "go.sum"), "hash data");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "go.mod"), targetDir);

                Assert.True(File.Exists(Path.Combine(targetDir, "go.sum")));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_CsProj_UseExtensionMatching()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                // The manifest is "MyApp.csproj" — must match by .csproj extension
                File.WriteAllText(Path.Combine(sourceDir, "MyApp.csproj"), "<Project/>");
                File.WriteAllText(Path.Combine(sourceDir, "packages.lock.json"), "{}");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "MyApp.csproj"), targetDir);

                Assert.True(File.Exists(Path.Combine(targetDir, "packages.lock.json")));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_UnknownManifest_DoesNotCopyAnything()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(sourceDir, "Dockerfile"), "FROM ubuntu");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "Dockerfile"), targetDir);

                Assert.Empty(Directory.GetFiles(targetDir));
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
        }

        [Fact]
        public void CopyCompanionLockFiles_OverwritesExistingLockFile()
        {
            var sourceDir = CreateTempDir();
            var targetDir = CreateTempDir();
            try
            {
                File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");
                File.WriteAllText(Path.Combine(sourceDir, "package-lock.json"), "new content");
                File.WriteAllText(Path.Combine(targetDir,  "package-lock.json"), "old content");

                CompanionFileManager.CopyCompanionLockFiles(
                    Path.Combine(sourceDir, "package.json"), targetDir);

                var content = File.ReadAllText(Path.Combine(targetDir, "package-lock.json"));
                Assert.Equal("new content", content);
            }
            finally { CleanupDir(sourceDir); CleanupDir(targetDir); }
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
