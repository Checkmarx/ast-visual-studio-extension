using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
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
    public class SecretsService : SingletonScannerBase<SecretsService>
    {
        private static readonly IFileFilterStrategy _fileFilter = new SecretsFileFilterStrategy();

        protected override string ScannerName => "Secrets";

        protected override ScannerType CoordinatorScannerType => ScannerType.Secrets;

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
            ResetInstance();
        }

        /// <summary>
        /// Secrets scanner scans all files EXCEPT manifest files and lock files.
        /// Uses cached FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return _fileFilter.ShouldScanFile(filePath);
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

            if (results?.Secrets == null || results.Secrets.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results - {Path.GetFileName(sourceFilePath)}");
                ClearDisplayForFile(sourceFilePath);
                return 0;
            }

            int secretCount = results.Secrets.Count;
            OutputPaneWriter.WriteLine($"{ScannerName} scanner: {secretCount} secret(s) found — {Path.GetFileName(sourceFilePath)}");

            for (int i = 0; i < secretCount; i++)
            {
                var secret = results.Secrets[i];
                OutputPaneWriter.WriteDebug($"{ScannerName} secret {i + 1}: {secret.Title ?? "Unknown"} [{secret.Severity ?? "UNKNOWN"}]");
            }

            var mappedResults = VulnerabilityMapper.FromSecrets(results.Secrets, sourceFilePath);
            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(sourceFilePath, CoordinatorScannerType, mappedResults);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static SecretsService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
            => GetOrCreate(() => new SecretsService(cxWrapper));
    }
}
