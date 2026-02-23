using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// Gets compiler / VS Error List messages for a given file and line so the custom popup can show
    /// "Also on this line (Compiler / VS):" combined with CxAssist findings.
    /// </summary>
    internal static class CxAssistCompilerErrorsForLine
    {
        /// <summary>
        /// Returns Error List (compiler/VS) messages for the given file and line.
        /// Line is 1-based (Error List uses 1-based line numbers).
        /// Returns empty list if DTE/Error List is unavailable or on error.
        /// </summary>
        public static IReadOnlyList<string> GetErrorsForLine(string filePath, int line1Based)
        {
            if (string.IsNullOrEmpty(filePath) || line1Based < 1)
                return Array.Empty<string>();

            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte == null) return Array.Empty<string>();

                var dte2 = dte as DTE2;
                if (dte2 == null) return Array.Empty<string>();

                ErrorList errorList = dte2.ToolWindows?.ErrorList;
                if (errorList == null) return Array.Empty<string>();

                ErrorItems errorItems = errorList.ErrorItems;
                if (errorItems == null) return Array.Empty<string>();

                int count = errorItems.Count;
                if (count <= 0) return Array.Empty<string>();

                string normalizedPath = NormalizePath(filePath);
                string normalizedFileName = Path.GetFileName(normalizedPath);
                var list = new List<string>();

                // ErrorItems.Item is 1-based
                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        ErrorItem item = errorItems.Item(i);
                        if (item == null) continue;

                        string fileName = item.FileName ?? "";
                        if (string.IsNullOrEmpty(fileName)) continue;
                        if (!PathsMatch(fileName, normalizedPath, normalizedFileName))
                            continue;

                        int itemLine = item.Line;
                        if (itemLine != line1Based) continue;

                        string description = item.Description ?? "";
                        if (string.IsNullOrEmpty(description)) description = "Compiler / VS error";
                        list.Add(description);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CxAssist: GetErrorsForLine item {i}: {ex.Message}");
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CxAssist: GetErrorsForLine failed: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            try { return Path.GetFullPath(path.Trim()); }
            catch { return path.Trim(); }
        }

        /// <summary>Match document path with Error List item path (full path or filename only).</summary>
        private static bool PathsMatch(string errorItemFileName, string normalizedDocPath, string normalizedDocFileName)
        {
            if (string.IsNullOrEmpty(errorItemFileName)) return false;
            string normalizedError = NormalizePath(errorItemFileName);
            if (string.Compare(normalizedError, normalizedDocPath, StringComparison.OrdinalIgnoreCase) == 0)
                return true;
            if (string.Compare(Path.GetFileName(normalizedError), normalizedDocFileName, StringComparison.OrdinalIgnoreCase) == 0)
                return true;
            if (normalizedDocPath.EndsWith(errorItemFileName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            if (normalizedError.EndsWith(normalizedDocFileName, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}
