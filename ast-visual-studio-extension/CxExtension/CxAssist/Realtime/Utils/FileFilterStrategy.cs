using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Base interface for scanner-specific file filtering strategies.
    ///
    /// Design Pattern: Strategy pattern for pluggable filtering logic
    ///
    /// Each scanner has unique file filtering requirements:
    /// - ASCA: Inclusion-based (only specific extensions)
    /// - Secrets: Exclusion-based (JetBrains MANIFEST_FILE_PATTERNS + ignore sidecars; not suppressed for IaC/container paths)
    /// - IaC: Pattern-based (glob patterns)
    /// - Containers: Pattern-based with special Helm handling
    /// - OSS: Manifest-based (exact manifest files only)
    /// </summary>
    public interface IFileFilterStrategy
    {
        /// <summary>
        /// Determines whether the file should be scanned by this scanner.
        /// </summary>
        /// <param name="filePath">Absolute or relative file path</param>
        /// <returns>True if file should be scanned; false otherwise</returns>
        bool ShouldScanFile(string filePath);

        /// <summary>
        /// Gets human-readable description of what files this scanner accepts.
        /// Used for logging and debugging.
        /// </summary>
        string GetFilterDescription();
    }

    /// <summary>
    /// ASCA Scanner: Inclusion-based filtering
    /// Only scans files with supported source code extensions (aligned with JetBrains DevAssistConstants).
    /// Supported: Java, C#, Go, Python, JavaScript, JSX
    /// </summary>
    public class AscaFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".java", ".cs", ".go", ".py", ".js", ".jsx"
        };

        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/node_modules/",
            "\\node_modules\\",
            "/venv/",
            "\\venv\\",
            "/.venv/",
            "\\.venv\\",
            "/dist/",
            "\\dist\\",
            "/build/",
            "\\build\\"
        };

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Exclude common dependency/build directories
            if (ExcludedPaths.Any(excluded => filePath.Contains(excluded)))
                return false;

            var ext = Path.GetExtension(filePath);
            return SupportedExtensions.Contains(ext);
        }

        public string GetFilterDescription()
        {
            return "ASCA: Java, C#, Go, Python, JavaScript, JSX";
        }
    }

    /// <summary>
    /// Secrets Scanner: JetBrains-aligned exclusion filtering
    /// (<c>SecretsScannerService.isExcludedFileForSecretsScanning</c> + <c>MANIFEST_FILE_PATTERNS</c>).
    /// Scans text files except listed dependency manifests and Checkmarx ignore sidecars; does not exclude
    /// Dockerfile / compose / Terraform / Helm — those may run Secrets together with IaC and Containers.
    /// </summary>
    public class SecretsFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/node_modules/",
            "\\node_modules\\",
            "/.git/",
            "\\.git\\"
        };

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (ExcludedPaths.Any(excluded => filePath.Contains(excluded)))
                return false;

            if (DevAssistSecretsExclusion.IsCheckmarxIgnoreSidecarPath(filePath))
                return false;

            if (DevAssistSecretsExclusion.MatchesManifestDependencyPattern(filePath))
                return false;

            return true;
        }

        public string GetFilterDescription()
        {
            return "Secrets: all files except JetBrains MANIFEST_FILE_PATTERNS + .vscode ignore sidecars (node_modules/.git out)";
        }
    }

    /// <summary>
    /// IaC Scanner: Pattern-based filtering
    /// Scans Infrastructure-as-Code files aligned with JetBrains DevAssistConstants.IAC_FILE_EXTENSIONS.
    /// Supported: Terraform, YAML, JSON, Protobuf, Dockerfile (and variants)
    /// </summary>
    public class IacFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> IacExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".tf", ".yaml", ".yml", ".json", ".proto"
        };

        private static readonly HashSet<string> IacFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "dockerfile", "docker-compose.yml", "docker-compose.yaml",
            "buildspec.yml", "buildspec.yaml"
        };

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath);

            // Check extension
            if (IacExtensions.Contains(ext))
                return true;

            // Check exact filename
            if (IacFileNames.Contains(fileName))
                return true;

            // Check filename contains "dockerfile" (Dockerfile, Dockerfile-dev, nginx-alpine-slim.dockerfile, etc.)
            if (fileName.IndexOf("dockerfile", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check terraform variable files (*.auto.tfvars, *.terraform.tfvars)
            if (fileName.EndsWith(".tfvars", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public string GetFilterDescription()
        {
            return "IaC: Terraform, YAML, JSON, Protobuf, Dockerfile variants";
        }
    }

    /// <summary>
    /// Containers Scanner: Pattern-based filtering with Helm support
    /// Aligned with JetBrains DevAssistConstants.CONTAINERS_FILE_PATTERNS.
    /// Scans: Dockerfile variants, docker-compose variants, and Helm chart files.
    /// </summary>
    public class ContainersFileFilterStrategy : IFileFilterStrategy
    {
        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            var lowerFileName = fileName.ToLowerInvariant();

            // Dockerfile variants: Dockerfile, dockerfile, dockerfile-*, dockerfile.prod, etc. (but not dockerfile.md)
            // Match: Dockerfile, dockerfile-prod, dockerfile.prod (with extensions like .windows, .linux, .prod)
            if (lowerFileName == "dockerfile" ||
                lowerFileName.StartsWith("dockerfile-", StringComparison.OrdinalIgnoreCase) ||
                (lowerFileName.StartsWith("dockerfile.", StringComparison.OrdinalIgnoreCase) &&
                 !lowerFileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                 !lowerFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Docker Compose variants: docker-compose.yml, docker-compose.yaml, docker-compose-*.yml, docker-compose-*.yaml
            if (fileName.Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase))
                return true;

            // Docker Compose with suffix variants: docker-compose-prod.yml, docker-compose-staging.yaml, etc.
            if (fileName.StartsWith("docker-compose-", StringComparison.OrdinalIgnoreCase) &&
                (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                 fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Helm charts: YAML files in /helm/ directory (exclude chart.yml, chart.yaml)
            if (filePath.Contains("/helm/") || filePath.Contains("\\helm\\"))
            {
                var isChartConfig = fileName.Equals("chart.yml", StringComparison.OrdinalIgnoreCase) ||
                                    fileName.Equals("chart.yaml", StringComparison.OrdinalIgnoreCase);

                if (!isChartConfig && (filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                        filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        public string GetFilterDescription()
        {
            return "Containers: Dockerfile variants, docker-compose variants, Helm charts";
        }
    }

    /// <summary>
    /// OSS Scanner: Manifest-based filtering
    /// Aligned with JetBrains DevAssistConstants.MANIFEST_FILE_PATTERNS.
    /// Only scans dependency manifest files: Directory.Packages.props, packages.config, pom.xml,
    /// package.json, requirements.txt, go.mod, *.csproj
    /// </summary>
    public class OssFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> ManifestFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "directory.packages.props", "packages.config", "pom.xml", "package.json", "requirements.txt", "go.mod"
        };

        private static readonly HashSet<string> ManifestExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj"
        };

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath);

            return ManifestFileNames.Contains(fileName) || ManifestExtensions.Contains(ext);
        }

        public string GetFilterDescription()
        {
            return "OSS: Dependency manifests (Directory.Packages.props, packages.config, pom.xml, package.json, requirements.txt, go.mod, *.csproj)";
        }
    }

    /// <summary>
    /// Factory for creating appropriate filter strategy for a given scanner.
    /// </summary>
    public static class FileFilterStrategyFactory
    {
        /// <summary>
        /// Creates filter strategy for given scanner type.
        /// </summary>
        /// <param name="scannerName">Scanner name (ASCA, Secrets, IaC, Containers, OSS)</param>
        /// <returns>Appropriate file filter strategy</returns>
        public static IFileFilterStrategy CreateStrategy(string scannerName)
        {
            return scannerName?.ToUpperInvariant() switch
            {
                "ASCA" => new AscaFileFilterStrategy(),
                "SECRETS" => new SecretsFileFilterStrategy(),
                "IAC" => new IacFileFilterStrategy(),
                "CONTAINERS" => new ContainersFileFilterStrategy(),
                "OSS" => new OssFileFilterStrategy(),
                _ => throw new ArgumentException($"Unknown scanner: {scannerName}")
            };
        }
    }
}
