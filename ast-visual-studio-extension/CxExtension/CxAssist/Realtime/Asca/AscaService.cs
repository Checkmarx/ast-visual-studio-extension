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

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca
{
    /// <summary>
    /// ASCA (AI Secure Coding Assistant) realtime scanner service.
    /// Scans source code files for security best practice violations.
    /// Supports: .java, .cs, .go, .py, .js, .jsx
    /// </summary>
    public class AscaService : SingletonScannerBase<AscaService>
    {
        private static readonly IFileFilterStrategy _fileFilter = new AscaFileFilterStrategy();

        protected override string ScannerName => "ASCA";

        protected override ScannerType CoordinatorScannerType => ScannerType.ASCA;

        private AscaService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
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
        /// ASCA scanner only scans supported source code file types.
        /// Uses cached FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return _fileFilter.ShouldScanFile(filePath);
        }

        /// <summary>
        /// Invokes the ASCA realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            var results = await _cxWrapper.ScanAscaAsync(tempFilePath, ascaLatestVersion: false);

            if (results?.ScanDetails == null || results.ScanDetails.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results - {Path.GetFileName(sourceFilePath)}");
                ClearDisplayForFile(sourceFilePath);
                return 0;
            }

            int issueCount = results.ScanDetails.Count;
            OutputPaneWriter.WriteLine($"{ScannerName} scanner: {issueCount} issue(s) found — {Path.GetFileName(sourceFilePath)}");

            for (int i = 0; i < issueCount; i++)
            {
                var issue = results.ScanDetails[i];
                OutputPaneWriter.WriteDebug($"{ScannerName} issue {i + 1}: {issue.RuleName ?? "Unknown"} [{issue.Severity ?? "UNKNOWN"}]");
            }

            var mappedResults = VulnerabilityMapper.FromAsca(results.ScanDetails, sourceFilePath);
            CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(sourceFilePath, CoordinatorScannerType, mappedResults);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static AscaService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
            => GetOrCreate(() => new AscaService(cxWrapper));
    }
}
