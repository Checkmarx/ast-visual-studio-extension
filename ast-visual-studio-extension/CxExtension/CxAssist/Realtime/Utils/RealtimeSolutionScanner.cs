using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Enumerates files under a solution directory while skipping common build/dependency folders.
    /// Used for OSS startup sweep and similar project-wide passes.
    /// </summary>
    public static class RealtimeSolutionScanner
    {
        /// <summary>
        /// Returns the directory containing the loaded solution file, or null if unavailable (unsaved solution, etc.).
        /// Shared by the realtime orchestrator and OSS startup sweep.
        /// </summary>
        public static string TryGetSolutionDirectory()
        {
            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var fullName = dte?.Solution?.FullName;
                if (string.IsNullOrWhiteSpace(fullName))
                    return null;
                try
                {
                    return Path.GetDirectoryName(fullName);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static readonly HashSet<string> SkippedDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "bin", "obj", ".git", ".vs", "packages", "dist", "build", "out", "target",
            "__pycache__", ".pytest_cache", "TestResults", "coverage"
        };

        /// <summary>
        /// Returns all files under <paramref name="rootDirectory"/> that are not under skipped folders.
        /// </summary>
        public static IEnumerable<string> EnumerateFiles(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
                yield break;

            string rootFull;
            try
            {
                rootFull = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                yield break;
            }

            foreach (var path in Directory.EnumerateFiles(rootFull, "*", SearchOption.AllDirectories))
            {
                if (IsUnderSkippedDirectory(rootFull, path))
                    continue;
                yield return path;
            }
        }

        private static bool IsUnderSkippedDirectory(string rootFull, string filePath)
        {
            try
            {
                var relative = filePath.Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    if (SkippedDirectoryNames.Contains(seg))
                        return true;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }
    }
}
