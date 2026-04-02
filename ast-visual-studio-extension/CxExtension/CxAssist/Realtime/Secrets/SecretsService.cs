using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        private SecretsService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
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
        /// Maps results to Result objects for display in the findings panel.
        /// Validates that file content is not empty before scanning.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            // Validate file is not empty (prevent scanning blank files)
            try
            {
                var fileContent = System.IO.File.ReadAllText(tempFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    ScanMetricsLogger.LogScanSkipped(ScannerName, document.FullName, "file content is empty");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                ScanMetricsLogger.LogScanError(ScannerName, document.FullName, ex);
                return 0;
            }

            var results = await _cxWrapper.SecretsRealtimeScanAsync(tempFilePath);
            if (results?.Secrets == null || results.Secrets.Count == 0) return 0;

            var mappedResults = VulnerabilityMapper.FromSecrets(results.Secrets, document.FullName);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, document.FullName);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static SecretsService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
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
