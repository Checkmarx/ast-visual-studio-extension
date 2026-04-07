using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// Secrets scanner scans all files EXCEPT manifest files and lock files.
        /// Uses FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return new Utils.SecretsFileFilterStrategy().ShouldScanFile(filePath);
        }

        /// <summary>
        /// Secrets scanner uses a directory-based temp strategy with content hash + UUID.
        /// Creates: %TEMP%/Cx-secrets-realtime-scanner/{hash}_{uuid}_{timestamp}/{originalFileName}
        /// </summary>
        protected override string CreateTempFilePath(string originalFileName, string content, string fullSourcePath = null)
        {
            var tempDir = Utils.TempFileManager.CreateSecretsTempDir(content);
            return Path.Combine(tempDir, originalFileName);
        }

        /// <summary>
        /// Invokes the Secrets realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// Validates that file content is not empty before scanning.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            // Validate file is not empty (prevent scanning blank files)
            try
            {
                var fileContent = System.IO.File.ReadAllText(tempFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no content found - {sourceFilePath}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: scan error - {ex.Message}");
                return 0;
            }

            var results = await _cxWrapper.SecretsRealtimeScanAsync(tempFilePath);

            // Log raw JSON response
            if (results != null)
            {
                var jsonResponse = JsonConvert.SerializeObject(results, Formatting.Indented);
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: raw JSON response - {jsonResponse}");
            }

            if (results?.Secrets == null || results.Secrets.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results returned - {sourceFilePath}");
                return 0;
            }

            int secretCount = results.Secrets.Count;
            OutputPaneWriter.WriteLine($"{ScannerName} scanner: scan completed - {sourceFilePath} ({secretCount} secrets found)");

            // Log individual secrets like JetBrains does
            for (int i = 0; i < secretCount; i++)
            {
                var secret = results.Secrets[i];
                var severity = secret.Severity ?? "UNKNOWN";
                var title = secret.Title ?? "Unknown Secret";
                OutputPaneWriter.WriteLine($"Secret {i + 1}: {title} [{severity}]");
            }

            var mappedResults = VulnerabilityMapper.FromSecrets(results.Secrets, sourceFilePath);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, sourceFilePath);
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
