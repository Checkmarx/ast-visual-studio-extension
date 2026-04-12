using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
        /// Returns the workspace root for realtime OSS sweep and manifest watching: directory of the loaded .sln,
        /// or the folder when using Open Folder / directory-based workspace, or <see cref="IVsSolution.GetSolutionInfo"/>
        /// when DTE <c>Solution.FullName</c> is not set yet.
        /// </summary>
        public static string TryGetSolutionDirectory()
        {
            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var fullName = dte?.Solution?.FullName;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    try
                    {
                        // Open Folder (or similar): FullName is the opened directory.
                        if (Directory.Exists(fullName))
                            return NormalizeExistingDirectory(fullName);

                        if (File.Exists(fullName))
                        {
                            var dir = Path.GetDirectoryName(fullName);
                            return NormalizeExistingDirectory(dir);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // fall through to IVsSolution
                    }
                }

                return TryGetSolutionDirectoryFromVsSolution();
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetSolutionDirectoryFromVsSolution()
        {
            try
            {
                var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
                if (solution == null)
                    return null;

                // Works for many "no .sln path in DTE yet" cases, including Open Folder where pbstrSolutionFile may be empty.
                int hr = solution.GetSolutionInfo(out string solutionDirectory, out _, out _);
                if (ErrorHandler.Failed(hr))
                    return null;

                return NormalizeExistingDirectory(solutionDirectory);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeExistingDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            try
            {
                var full = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return Directory.Exists(full) ? full : null;
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
