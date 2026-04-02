using System;
using System.IO;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Manages companion lock files for dependency manifests.
    ///
    /// Design Pattern: Strategy pattern for different manifest types
    ///
    /// CRITICAL FEATURE: OSS scanning accuracy depends on lock files.
    /// Without lock files, OSS scanner provides incomplete/inaccurate results.
    ///
    /// Supported manifest + lock file combinations:
    /// - package.json → package-lock.json, yarn.lock
    /// - pom.xml → pom.xml.lock (Maven)
    /// - .csproj → package.lock.json (.NET)
    /// - go.mod → go.sum (Go)
    /// - requirements.txt → requirements.lock (Python)
    ///
    /// Lock files are OPTIONAL but CRITICAL when present.
    /// </summary>
    public static class CompanionFileManager
    {
        /// <summary>
        /// Lock file strategy definitions per manifest type.
        /// Each manifest can have multiple associated lock files.
        /// </summary>
        private static readonly Dictionary<string, string[]> LockFilesByManifest =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // NPM/Yarn
                {
                    "package.json",
                    new[] { "package-lock.json", "yarn.lock", "npm-shrinkwrap.json" }
                },
                // Maven
                {
                    "pom.xml",
                    new[] { "pom.xml.lock" }
                },
                // .NET
                {
                    ".csproj",
                    new[] { "package.lock.json", "packages.lock.json" }
                },
                // Go
                {
                    "go.mod",
                    new[] { "go.sum" }
                },
                // Python
                {
                    "requirements.txt",
                    new[] { "requirements.lock", "Pipfile.lock" }
                },
                // Ruby
                {
                    "Gemfile",
                    new[] { "Gemfile.lock" }
                },
                // PHP
                {
                    "composer.json",
                    new[] { "composer.lock" }
                }
            };

        /// <summary>
        /// Copies all applicable companion lock files from source directory to temp directory.
        ///
        /// Process:
        /// 1. Determine manifest type from filename
        /// 2. Get list of potential lock files for that manifest type
        /// 3. Copy each lock file if it exists in source directory
        /// 4. Log success/failure for each attempt
        ///
        /// Error handling: Non-fatal. Logs failures but continues processing.
        /// </summary>
        /// <param name="manifestPath">Full path to manifest file</param>
        /// <param name="tempDir">Target temp directory to copy lock files to</param>
        public static void CopyCompanionLockFiles(string manifestPath, string tempDir)
        {
            if (string.IsNullOrEmpty(manifestPath) || string.IsNullOrEmpty(tempDir))
            {
                System.Diagnostics.Debug.WriteLine("CompanionFileManager: Invalid parameters");
                return;
            }

            var fileName = Path.GetFileName(manifestPath);
            var originalDir = Path.GetDirectoryName(manifestPath);

            if (string.IsNullOrEmpty(originalDir))
            {
                System.Diagnostics.Debug.WriteLine($"CompanionFileManager: Invalid manifest path: {manifestPath}");
                return;
            }

            // Get lock files for this manifest type
            if (!LockFilesByManifest.TryGetValue(fileName, out var lockFiles))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: No lock files defined for {fileName}");
                return;
            }

            // Copy each lock file if it exists
            foreach (var lockFileName in lockFiles)
            {
                CopyLockFileIfExists(originalDir, tempDir, lockFileName);
            }
        }

        /// <summary>
        /// Copies single lock file from source to target if source exists.
        ///
        /// Non-fatal on error - logs warning but doesn't throw.
        /// </summary>
        /// <param name="sourceDir">Source directory</param>
        /// <param name="targetDir">Target directory</param>
        /// <param name="lockFileName">Lock file name to copy</param>
        private static void CopyLockFileIfExists(string sourceDir, string targetDir, string lockFileName)
        {
            var sourcePath = Path.Combine(sourceDir, lockFileName);

            if (!File.Exists(sourcePath))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: Lock file not found: {sourcePath}");
                return;
            }

            try
            {
                var targetPath = Path.Combine(targetDir, lockFileName);
                File.Copy(sourcePath, targetPath, overwrite: true);

                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: Copied lock file: {lockFileName}");
            }
            catch (IOException ioEx)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: IO error copying {lockFileName}: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException authEx)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: Permission denied copying {lockFileName}: {authEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"CompanionFileManager: Error copying {lockFileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if given manifest type has companion lock files defined.
        /// </summary>
        /// <param name="manifestFileName">Manifest file name (e.g., "package.json")</param>
        /// <returns>True if lock files are defined for this manifest type</returns>
        public static bool HasCompanionFiles(string manifestFileName)
        {
            return LockFilesByManifest.ContainsKey(manifestFileName);
        }

        /// <summary>
        /// Gets list of companion lock file names for given manifest.
        /// </summary>
        /// <param name="manifestFileName">Manifest file name</param>
        /// <returns>Array of lock file names, or empty array if none defined</returns>
        public static string[] GetCompanionFileNames(string manifestFileName)
        {
            return LockFilesByManifest.TryGetValue(manifestFileName, out var lockFiles)
                ? lockFiles
                : Array.Empty<string>();
        }
    }
}
