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
    /// Only scans files with supported source code extensions.
    /// </summary>
    public class AscaFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".java", ".cs", ".go", ".py", ".js", ".jsx", ".ts", ".tsx",
            ".cpp", ".c", ".h", ".cc", ".cxx", ".hh", ".hpp"
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
            return "ASCA: C#, Java, Go, Python, JavaScript/TypeScript, C/C++";
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
    /// Scans Infrastructure-as-Code files (Terraform, YAML, JSON, Dockerfile, etc.)
    /// </summary>
    public class IacFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> IacExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".tf", ".yaml", ".yml", ".json", ".hcl", ".bicep", ".arm", ".tmpl"
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

            // Check filename prefix (Dockerfile, Dockerfile-dev, etc.)
            if (fileName.StartsWith("dockerfile", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check terraform variable files (*.auto.tfvars, *.terraform.tfvars)
            if (fileName.EndsWith(".tfvars", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public string GetFilterDescription()
        {
            return "IaC: Terraform, YAML, JSON, Dockerfile, CloudFormation, Bicep";
        }
    }

    /// <summary>
    /// Containers Scanner: Pattern-based filtering with Helm support
    /// Scans Dockerfile, docker-compose, and Helm chart files.
    /// </summary>
    public class ContainersFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> ContainerFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "dockerfile", "docker-compose.yml", "docker-compose.yaml"
        };

        public bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);

            // Container files
            if (ContainerFileNames.Contains(fileName))
                return true;

            // Dockerfile variants: Dockerfile, Dockerfile.dev, Dockerfile-prod, etc.
            if (fileName.StartsWith("dockerfile", StringComparison.OrdinalIgnoreCase))
                return true;

            // Helm charts: YAML files in /helm/ directory (exclude chart.yml, chart.yaml)
            if (filePath.Contains("/helm/") || filePath.Contains("\\helm\\"))
            {
                var helmFileName = Path.GetFileName(filePath);
                var isChartConfig = helmFileName.Equals("chart.yml", StringComparison.OrdinalIgnoreCase) ||
                                    helmFileName.Equals("chart.yaml", StringComparison.OrdinalIgnoreCase);

                if (!isChartConfig && (filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                        filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        public string GetFilterDescription()
        {
            return "Containers: Dockerfile, docker-compose, Helm charts";
        }
    }

    /// <summary>
    /// OSS Scanner: Manifest-based filtering
    /// Only scans dependency manifest files (package.json, pom.xml, requirements.txt, etc.)
    /// </summary>
    public class OssFileFilterStrategy : IFileFilterStrategy
    {
        private static readonly HashSet<string> ManifestFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "package.json", "pom.xml", "requirements.txt", "go.mod", "packages.config",
            "build.gradle", "Gemfile", "composer.json", "Pipfile",
            "setup.py", "pubspec.yaml", "Cargo.toml", "mix.exs"
        };

        private static readonly HashSet<string> ManifestExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj", ".vbproj", ".fsproj"
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
            return "OSS: Dependency manifests (package.json, pom.xml, go.mod, *.csproj, etc.)";
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
