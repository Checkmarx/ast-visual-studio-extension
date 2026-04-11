using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac
{
    /// <summary>
    /// Infrastructure as Code (IaC) realtime scanner service.
    /// Scans Terraform, YAML, JSON, Dockerfile, and related IaC configuration files.
    /// </summary>
    public class IacService : SingletonScannerBase<IacService>
    {
        private static readonly IFileFilterStrategy _fileFilter = new IacFileFilterStrategy();

        protected override string ScannerName => "IaC";

        protected override ScannerType CoordinatorScannerType => ScannerType.IaC;

        private IacService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
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
        /// IaC scanner scans IaC-related file types including Terraform variable files.
        /// Uses cached FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return _fileFilter.ShouldScanFile(filePath);
        }

        /// <summary>
        /// IaC scanner uses a directory-based temp strategy with content hash.
        /// Creates: %TEMP%/Cx-iac-realtime-scanner/{contentHash}/{originalFileName}
        /// </summary>
        protected override string CreateTempFilePath(string originalFileName, string content, string fullSourcePath = null)
        {
            var hash = Utils.TempFileManager.GetContentHash(content);
            var tempDir = Utils.TempFileManager.CreateIacTempDir(hash);
            return Path.Combine(tempDir, originalFileName);
        }

        /// <summary>
        /// Invokes the IaC realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            var results = await _cxWrapper.IacRealtimeScanAsync(tempFilePath);

            // Log raw JSON response
            if (results != null)
            {
                var jsonResponse = JsonConvert.SerializeObject(results, Formatting.Indented);
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: raw JSON response - {jsonResponse}");
            }

            if (results?.Results == null || results.Results.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results returned - {sourceFilePath}");
                ClearDisplayForFile(sourceFilePath);
                return 0;
            }

            int issueCount = results.Results.Count;
            OutputPaneWriter.WriteLine($"{ScannerName} scanner: scan completed - {sourceFilePath} ({issueCount} issues found)");

            // Log individual issues like JetBrains does
            for (int i = 0; i < issueCount; i++)
            {
                var issue = results.Results[i];
                var severity = issue.Severity ?? "UNKNOWN";
                var title = issue.Title ?? "Unknown Issue";
                OutputPaneWriter.WriteLine($"Issue {i + 1}: {title} [{severity}]");
            }

            var mappedResults = VulnerabilityMapper.FromIac(results.Results, sourceFilePath);
            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(sourceFilePath, CoordinatorScannerType, mappedResults);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static IacService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
            => GetOrCreate(() => new IacService(cxWrapper));
    }
}
