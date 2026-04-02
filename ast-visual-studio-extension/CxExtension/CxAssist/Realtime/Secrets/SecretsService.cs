using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CxWrapper = ast_visual_studio_extension.CxCLI.CxWrapper;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Secrets
{
    /// <summary>
    /// Realtime Secrets scanner service.
    /// Scans all files EXCEPT manifest files for hardcoded secrets (API keys, tokens, passwords, etc.).
    /// </summary>
    public class SecretsService : BaseRealtimeScannerService
    {
        private static volatile SecretsService _instance;
        private static readonly object _lock = new object();

        private static readonly HashSet<string> ExcludedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "package.json", "pom.xml", "requirements.txt", "go.mod", "packages.config",
            "build.gradle", "Gemfile", "composer.json"
        };

        protected override string ScannerName => "Secrets";

        private SecretsService(CxWrapper cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// Secrets scanner scans all files EXCEPT manifest files.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var fileName = System.IO.Path.GetFileName(filePath);
            return !ExcludedFileNames.Contains(fileName);
        }

        /// <summary>
        /// Invokes the Secrets realtime scan CLI command.
        /// Results will be mapped to Vulnerability objects and passed to CxAssistDisplayCoordinator.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            var results = await _cxWrapper.SecretsRealtimeScanAsync(tempFilePath);
            if (results?.Secrets == null || results.Secrets.Count == 0) return 0;

            // TODO: Map results to Vulnerability and call CxAssistDisplayCoordinator.UpdateFindings
            return results.Secrets.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static SecretsService GetInstance(CxWrapper cxWrapper)
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new SecretsService(cxWrapper);
            }
            return _instance;
        }
    }
}
