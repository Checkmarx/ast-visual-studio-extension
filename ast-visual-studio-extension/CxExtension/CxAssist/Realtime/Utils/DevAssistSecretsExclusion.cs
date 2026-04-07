using System;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Mirrors JetBrains <c>SecretsScannerService.isExcludedFileForSecretsScanning</c> and
    /// <c>DevAssistConstants.MANIFEST_FILE_PATTERNS</c> — dependency manifests are excluded; IaC/container
    /// files (Dockerfile, compose, .tf, etc.) are <em>not</em> excluded here so Secrets can run alongside
    /// IaC/Containers engines, matching <c>ScannerFactory.getAllSupportedScanners</c> behaviour.
    /// </summary>
    internal static class DevAssistSecretsExclusion
    {
        /// <summary>
        /// JetBrains MANIFEST_FILE_PATTERNS (glob → filename/extension checks).
        /// </summary>
        public static bool MatchesManifestDependencyPattern(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath);

            if (string.Equals(fileName, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "packages.config", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "pom.xml", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "requirements.txt", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "go.mod", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(ext, ".csproj", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// JetBrains: paths under .vscode for realtime ignore temp files.
        /// </summary>
        public static bool IsCheckmarxIgnoreSidecarPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var p = filePath.Replace('/', Path.DirectorySeparatorChar);
            return p.IndexOf($"{Path.DirectorySeparatorChar}.vscode{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0
                   && (p.IndexOf(".checkmarxIgnored", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
