using System;
using System.IO;
using System.Linq;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// Both MCP entry shapes launch the mcp-remote bridge through npx, so Node.js must be installed.
    /// Visual Studio starts MCP servers with its own environment, so the extension's PATH is representative.
    /// </summary>
    internal static class McpPrerequisites
    {
        internal const string NPX_MISSING_MESSAGE =
            "Node.js (npx) was not found on PATH. The Checkmarx MCP server runs through npx: install Node.js (LTS), restart Visual Studio and click Install MCP again.";

        private const string DEFAULT_PATHEXT = ".COM;.EXE;.BAT;.CMD";

        internal static bool IsNpxAvailable()
        {
            return IsOnPath("npx", Environment.GetEnvironmentVariable("PATH"), Environment.GetEnvironmentVariable("PATHEXT"));
        }

        internal static bool IsOnPath(string command, string path, string pathExt)
        {
            if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(path))
                return false;

            string[] extensions = (string.IsNullOrWhiteSpace(pathExt) ? DEFAULT_PATHEXT : pathExt)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .ToArray();

            foreach (string rawDir in path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                string dir = rawDir.Trim().Trim('"');
                if (dir.Length == 0 || dir.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    continue;

                if (extensions.Any(ext => File.Exists(Path.Combine(dir, command + ext))))
                    return true;
            }

            return false;
        }
    }
}
