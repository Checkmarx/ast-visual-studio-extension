using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Task;

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

        private OssService(CxWrapper cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// OSS scanner only scans dependency manifest files.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var name = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath);
            return ManifestFileNames.Contains(name) || ManifestExtensions.Contains(ext);
        }

        /// <summary>
        /// Invokes the OSS realtime scan CLI command.
        /// Results will be mapped to Vulnerability objects and passed to CxAssistDisplayCoordinator.
        /// Copies companion lock files (package-lock.json, yarn.lock) alongside the temp file.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            // Copy companion lock file (package-lock.json / yarn.lock) alongside temp file
            CopyCompanionLockFile(document.FullName, Path.GetDirectoryName(tempFilePath));

            var results = await _cxWrapper.OssRealtimeScanAsync(tempFilePath);
            if (results?.Packages == null || results.Packages.Count == 0) return 0;

            // TODO: Map results to Vulnerability and call CxAssistDisplayCoordinator.UpdateFindings
            return results.Packages.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static OssService GetInstance(CxWrapper cxWrapper)
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
        /// Copies companion lock file (package-lock.json, yarn.lock, pom.xml.lock) to temp directory.
        /// </summary>
        private void CopyCompanionLockFile(string originalFilePath, string tempDir)
        {
            var dir = Path.GetDirectoryName(originalFilePath);
            var fileName = Path.GetFileName(originalFilePath);

            if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var lockName in new[] { "package-lock.json", "yarn.lock" })
                {
                    var lockPath = Path.Combine(dir, lockName);
                    if (File.Exists(lockPath))
                    {
                        try
                        {
                            File.Copy(lockPath, Path.Combine(tempDir, lockName), overwrite: true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to copy lock file: {ex.Message}");
                        }
                        break;
                    }
                }
            }
            else if (fileName.Equals("pom.xml", StringComparison.OrdinalIgnoreCase))
            {
                var lockPath = Path.Combine(dir, "pom.xml.lock");
                if (File.Exists(lockPath))
                {
                    try
                    {
                        File.Copy(lockPath, Path.Combine(tempDir, "pom.xml.lock"), overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to copy lock file: {ex.Message}");
                    }
                }
            }
        }
    }
}
