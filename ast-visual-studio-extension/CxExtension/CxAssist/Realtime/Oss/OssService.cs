using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss
{
    /// <summary>
    /// Open Source Software (OSS) and Malicious package realtime scanner service.
    /// Scans dependency manifest files (package.json, pom.xml, requirements.txt, etc.)
    /// for known vulnerabilities and malicious packages.
    /// </summary>
    public class OssService : BaseRealtimeScannerService
    {
        private static volatile OssService _instance;
        private static readonly object _lock = new object();

        private static readonly HashSet<string> ManifestFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "package.json", "pom.xml", "requirements.txt", "go.mod", "packages.config",
            "build.gradle", "Gemfile", "composer.json"
        };

        private static readonly HashSet<string> ManifestExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj", ".vbproj", ".fsproj"
        };

        protected override string ScannerName => "OSS";

        private OssService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// Unregisters the scanner and resets the singleton.
        /// Allows re-registration to create a fresh instance with proper event wiring.
        /// </summary>
        public override async Task UnregisterAsync()
        {
            await base.UnregisterAsync();
            lock (_lock)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// OSS scanner scans dependency manifest files and additional package manager files.
        /// Uses FileFilterStrategy for consistent, enhanced filtering rules including Pipfile, setup.py, etc.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return new Utils.OssFileFilterStrategy().ShouldScanFile(filePath);
        }

        /// <summary>
        /// OSS scanner uses a directory-based temp strategy with content hash.
        /// Creates: %TEMP%/Cx-oss-realtime-scanner/{contentHash}/{originalFileName}
        /// Also copies companion lock files into the same directory (e.g., package-lock.json, yarn.lock).
        /// </summary>
        protected override string CreateTempFilePath(string originalFileName, string content)
        {
            var hash = Utils.TempFileManager.GetContentHash(content);
            var tempDir = Utils.TempFileManager.CreateOssTempDir(hash);
            return Path.Combine(tempDir, originalFileName);
        }

        /// <summary>
        /// Invokes the OSS realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// Copies companion lock files (package-lock.json, yarn.lock) alongside the temp file.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            // Copy companion lock file (package-lock.json / yarn.lock) alongside temp file
            CopyCompanionLockFile(document.FullName, Path.GetDirectoryName(tempFilePath));

            var results = await _cxWrapper.OssRealtimeScanAsync(tempFilePath);
            if (results?.Packages == null || results.Packages.Count == 0) return 0;

            var mappedResults = VulnerabilityMapper.FromOss(results.Packages, document.FullName);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, document.FullName);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static OssService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new OssService(cxWrapper);
            }
            return _instance;
        }

        /// <summary>
        /// Copies companion lock files to temp directory using centralized manager.
        ///
        /// CRITICAL: Lock files are essential for accurate OSS scanning.
        /// Different package managers use different lock file formats:
        /// - NPM: package-lock.json, npm-shrinkwrap.json
        /// - Yarn: yarn.lock
        /// - Maven: pom.xml.lock
        /// - .NET: package.lock.json, packages.lock.json
        ///
        /// Lock files are optional but when present, they MUST be copied.
        /// Without them, OSS scanning provides incomplete results.
        /// </summary>
        private void CopyCompanionLockFile(string originalFilePath, string tempDir)
        {
            CompanionFileManager.CopyCompanionLockFiles(originalFilePath, tempDir);
        }
    }
}
