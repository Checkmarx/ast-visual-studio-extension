using ast_visual_studio_extension.CxWrapper.Models;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public static class ResultUtils
    {
        /// <summary>
        /// Formats a filename with its line number and associated rule name.
        /// </summary>
        public static string FormatFilenameLine(string filename, int? line, string ruleName)
        {
            if (!string.IsNullOrEmpty(ruleName) && !string.IsNullOrEmpty(filename) && line.HasValue)
            {
                string file = Path.GetFileName(filename); // Safer and more portable
                return $"{ruleName} (/{file}:{line.Value})";
            }
            return null;
        }

        /// <summary>
        /// Appends file and line information to a display name based on the result data.
        /// </summary>
        public static string HandleFileNameAndLine(Result result, string displayName)
        {
            if (result.Data?.Nodes != null && result.Data.Nodes.Count > 0)
            {
                var node = result.Data.Nodes[0];
                string filename = node.FileName;
                string shortFilename = !string.IsNullOrEmpty(filename) && filename.Contains("/")
                    ? filename.Substring(filename.LastIndexOf("/"))
                    : filename;

                string lineInfo = node.Line > 0 ? $":{node.Line}" : "";
                displayName += $"\n ({shortFilename}{lineInfo})";
            }
            return displayName;
        }
    }

}
