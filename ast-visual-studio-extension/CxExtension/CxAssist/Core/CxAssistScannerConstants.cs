using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Scanner and manifest file patterns aligned with JetBrains DevAssistConstants.
    /// Used to decide which scanner applies to a file (OSS, Containers, Secrets, IAC, ASCA).
    /// </summary>
    internal static class CxAssistScannerConstants
    {
        // --- OSS: Manifest file patterns (aligned with ast-vscode-extension feature/release-manifest-parser) ---
        // .NET: Directory.Packages.props, packages.config, *.csproj
        // Maven: pom.xml
        // npm: package.json
        // Bower: bower.json
        // Python: requirements*.txt, constraints.txt, constraints-*.txt, pyproject.toml, setup.cfg, setup.py
        // Go: go.mod
        // Gradle: *.gradle, *.gradle.kts, libs.versions.toml
        // SBT: *.sbt
        // iOS CocoaPods: Podfile, *.podspec, *.podspec.json
        // iOS Carthage: Cartfile, Cartfile.private
        // Swift Package Manager: Package.swift, Package@swift-*.swift
        // Dart/Flutter: pubspec.yaml
        // Ruby: Gemfile
        // PHP Composer: composer.json
        public static readonly IReadOnlyList<string> ManifestFilePatterns = new[]
        {
            // .NET
            "Directory.Packages.props",
            "packages.config",
            // Maven
            "pom.xml",
            // npm
            "package.json",
            // Bower
            "bower.json",
            // Python
            "requirements.txt",
            "constraints.txt",
            "pyproject.toml",
            "setup.cfg",
            "setup.py",
            // Go
            "go.mod",
            // Gradle
            // (handled separately with ManifestGradleSuffix and ManifestLibsVersionsToml)
            // SBT
            // (handled separately with ManifestSbtSuffix)
            // iOS CocoaPods
            "Podfile",
            // (*.podspec handled with extension matching)
            // iOS Carthage
            "Cartfile",
            "Cartfile.private",
            // Swift Package Manager
            "Package.swift",
            // (Package@swift-*.swift handled separately)
            // Dart/Flutter
            "pubspec.yaml",
            // Ruby
            "Gemfile",
            // PHP Composer
            "composer.json"
        };

        public static readonly string ManifestCsprojSuffix = ".csproj";
        public static readonly string ManifestPodspecSuffix = ".podspec";
        public static readonly string ManifestGradleSuffix = ".gradle";
        public static readonly string ManifestGradleKtsSuffix = ".gradle.kts";
        public static readonly string ManifestSbtSuffix = ".sbt";
        public static readonly string ManifestLibsVersionsToml = "libs.versions.toml";
        public static readonly string ManifestPackageSwiftGlob = "Package@swift-*.swift";

        // --- Containers (JetBrains CONTAINERS_FILE_PATTERNS) ---
        // **/dockerfile, **/dockerfile-*, **/dockerfile.*, **/docker-compose.yml, **/docker-compose.yaml,
        // **/docker-compose-*.yml, **/docker-compose-*.yaml
        public static readonly string DockerfileLiteral = "dockerfile";
        public static readonly string DockerComposeLiteral = "docker-compose";

        // --- IAC (JetBrains IAC_SUPPORTED_PATTERNS + IAC_FILE_EXTENSIONS) ---
        // Patterns: **/dockerfile, **/*.auto.tfvars, **/*.terraform.tfvars
        // Extensions: tf, yaml, yml, json, proto, dockerfile
        public static readonly IReadOnlyList<string> IacFileExtensions = new[]
        {
            "tf", "yaml", "yml", "json", "proto", "dockerfile"
        };

        public static readonly string IacAutoTfvarsSuffix = ".auto.tfvars";
        public static readonly string IacTerraformTfvarsSuffix = ".terraform.tfvars";

        // --- Helm (Containers): path contains /helm/, extension yml/yaml, exclude chart.yml, chart.yaml ---
        public static readonly IReadOnlyList<string> ContainerHelmExtensions = new[] { "yml", "yaml" };
        public static readonly IReadOnlyList<string> ContainerHelmExcludedFiles = new[] { "chart.yml", "chart.yaml" };
        public static readonly string HelmPathSegment = "/helm/";

        // --- Secrets: excluded paths = MANIFEST_FILE_PATTERNS + .vscode ignore files (JetBrains isExcludedFileForSecretsScanning) ---
        public static readonly string CheckmarxIgnoredPathSegment1 = "/.vscode/.checkmarxIgnored";
        public static readonly string CheckmarxIgnoredPathSegment2 = "/.vscode/.checkmarxIgnoredTempList";
        public static readonly string CheckmarxIgnoredPathSegment3 = "\\.vscode\\.checkmarxIgnored";
        public static readonly string CheckmarxIgnoredPathSegment4 = "\\.vscode\\.checkmarxIgnoredTempList";

        // --- Base (JetBrains BaseScannerService): skip node_modules ---
        public static readonly string NodeModulesPathSegment = "/node_modules/";
        public static readonly string NodeModulesPathSegmentBackslash = "\\node_modules\\";

        /// <summary>Normalizes path for pattern matching (forward slashes, lowercase where needed).</summary>
        public static string NormalizePathForMatching(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return filePath;
            return filePath.Replace('\\', '/');
        }

        /// <summary>Base check: file should not be under node_modules (JetBrains BaseScannerService.shouldScanFile).</summary>
        public static bool PassesBaseScanCheck(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return true;
            var normalized = NormalizePathForMatching(filePath);
            return !normalized.Contains(NodeModulesPathSegment) && !filePath.Contains(NodeModulesPathSegmentBackslash);
        }

        /// <summary>True if path matches OSS manifest patterns (aligned with ast-vscode-extension).</summary>
        public static bool IsManifestFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var normalized = NormalizePathForMatching(filePath);
            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(fileName)) return false;
            var fileNameLower = fileName.ToLowerInvariant();

            // Exact filename matches
            foreach (var pattern in ManifestFilePatterns)
            {
                if (fileNameLower.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Extension-based matches
            if (fileNameLower.EndsWith(ManifestCsprojSuffix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (fileNameLower.EndsWith(ManifestPodspecSuffix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (fileNameLower.EndsWith(ManifestGradleSuffix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (fileNameLower.EndsWith(ManifestGradleKtsSuffix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (fileNameLower.EndsWith(ManifestSbtSuffix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (fileNameLower.Equals(ManifestLibsVersionsToml, StringComparison.OrdinalIgnoreCase))
                return true;

            // Pattern-based matches (requirements*.txt, constraints*.txt, Package@swift-*.swift)
            if (MatchesRequirementsPattern(fileNameLower))
                return true;
            if (MatchesConstraintsPattern(fileNameLower))
                return true;
            if (MatchesPackageSwiftPattern(fileNameLower))
                return true;

            return false;
        }

        /// <summary>Matches requirement*.txt pattern (requirements.txt, requirements-dev.txt, etc.)</summary>
        public static bool MatchesRequirementsPattern(string fileName)
        {
            var lowerName = fileName.ToLowerInvariant();
            return (lowerName.StartsWith("requirements", StringComparison.OrdinalIgnoreCase) &&
                    lowerName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) ||
                   lowerName == "requirements.txt";
        }

        /// <summary>Matches constraints*.txt pattern (constraints.txt, constraints-dev.txt, etc.)</summary>
        public static bool MatchesConstraintsPattern(string fileName)
        {
            var lowerName = fileName.ToLowerInvariant();
            return (lowerName.StartsWith("constraints", StringComparison.OrdinalIgnoreCase) &&
                    lowerName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Matches Package@swift-*.swift glob pattern</summary>
        public static bool MatchesPackageSwiftPattern(string fileName)
        {
            var lowerName = fileName.ToLowerInvariant();
            return lowerName.StartsWith("package@swift-", StringComparison.OrdinalIgnoreCase) &&
                   lowerName.EndsWith(".swift", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Matches manifest file name glob pattern (used for wildcard manifest checks)</summary>
        public static bool MatchesManifestFileNameGlob(string fileName, string glob)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(glob)) return false;
            var lowerName = fileName.ToLowerInvariant();
            var lowerGlob = glob.ToLowerInvariant();

            // Simple glob matching for Package@swift-*.swift pattern
            if (lowerGlob == "package@swift-*.swift")
                return MatchesPackageSwiftPattern(lowerName);

            return false;
        }

        /// <summary>True if path matches container file patterns: dockerfile*, docker-compose*.yml/yaml (JetBrains isContainersFilePatternMatching).</summary>
        public static bool IsContainersFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var normalized = NormalizePathForMatching(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(fileName)) return false;
            if (fileName.Contains(DockerfileLiteral)) return true;
            if (fileName.Contains(DockerComposeLiteral) && (fileName.EndsWith(".yml") || fileName.EndsWith(".yaml")))
                return true;
            return false;
        }

        /// <summary>True if file is Dockerfile (filename contains "dockerfile").</summary>
        public static bool IsDockerFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            return fileName.Contains(DockerfileLiteral);
        }

        /// <summary>True if file is docker-compose (filename contains "docker-compose").</summary>
        public static bool IsDockerComposeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            return fileName.Contains(DockerComposeLiteral);
        }

        /// <summary>True if path matches IAC: dockerfile, *.auto.tfvars, *.terraform.tfvars, or extension in tf/yaml/yml/json/proto (JetBrains isIacFilePatternMatching).</summary>
        public static bool IsIacFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var normalized = NormalizePathForMatching(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(normalized);
            if (fileName.Contains(DockerfileLiteral)) return true;
            if (fileName.EndsWith(IacAutoTfvarsSuffix) || fileName.EndsWith(IacTerraformTfvarsSuffix))
                return true;
            var ext = Path.GetExtension(normalized);
            if (string.IsNullOrEmpty(ext)) return false;
            ext = ext.TrimStart('.').ToLowerInvariant();
            return IacFileExtensions.Contains(ext);
        }

        /// <summary>True if file is Helm chart (yaml/yml under path containing /helm/, excluding chart.yml, chart.yaml).</summary>
        public static bool IsHelmFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var normalized = NormalizePathForMatching(filePath);
            if (!normalized.Contains(HelmPathSegment)) return false;
            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(fileName)) return false;
            var lower = fileName.ToLowerInvariant();
            if (ContainerHelmExcludedFiles.Contains(lower)) return false;
            var ext = Path.GetExtension(normalized);
            if (string.IsNullOrEmpty(ext)) return false;
            ext = ext.TrimStart('.').ToLowerInvariant();
            return ContainerHelmExtensions.Contains(ext);
        }

        /// <summary>True if file is excluded from Secrets scan (manifest patterns or .vscode ignore files).</summary>
        public static bool IsExcludedForSecrets(string filePath)
        {
            if (IsManifestFile(filePath)) return true;
            var normalized = NormalizePathForMatching(filePath);
            return normalized.Contains(CheckmarxIgnoredPathSegment1) ||
                   normalized.Contains(CheckmarxIgnoredPathSegment2) ||
                   normalized.Contains(CheckmarxIgnoredPathSegment3) ||
                   normalized.Contains(CheckmarxIgnoredPathSegment4);
        }
    }
}
