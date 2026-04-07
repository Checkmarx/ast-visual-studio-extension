using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Asca
{
    /// <summary>
    /// ASCA (AI Secure Coding Assistant) realtime scanner service.
    /// Scans source code files for security best practice violations.
    /// Supports: .java, .cs, .go, .py, .js, .jsx, .ts, .tsx, .cpp, .c, .h
    /// </summary>
    public class AscaService : BaseRealtimeScannerService
    {
        private static volatile AscaService _instance;
        private static readonly object _lock = new object();

        private static readonly HashSet<string> AscaExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".java", ".cs", ".go", ".py", ".js", ".jsx", ".ts", ".tsx",
                ".cpp", ".c", ".h", ".cc", ".cxx", ".hh", ".hpp"
            };

        protected override string ScannerName => "ASCA";

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
            lock (_lock)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// ASCA scanner only scans supported source code file types.
        /// Uses FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return new Utils.AscaFileFilterStrategy().ShouldScanFile(filePath);
        }

        /// <summary>
        /// Invokes the ASCA realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            var results = await _cxWrapper.ScanAscaAsync(tempFilePath, ascaLatestVersion: false);

            // Log raw JSON response
            if (results != null)
            {
                var jsonResponse = JsonConvert.SerializeObject(results, Formatting.Indented);
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: raw JSON response - {jsonResponse}");
            }

            if (results?.ScanDetails == null || results.ScanDetails.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results returned - {sourceFilePath}");
                return 0;
            }

            int issueCount = results.ScanDetails.Count;
            OutputPaneWriter.WriteLine($"{ScannerName} scanner: scan completed - {sourceFilePath} ({issueCount} issues found)");

            // Log individual issues like JetBrains does
            for (int i = 0; i < issueCount; i++)
            {
                var issue = results.ScanDetails[i];
                var severity = issue.Severity ?? "UNKNOWN";
                var title = issue.RuleName ?? "Unknown Issue";
                OutputPaneWriter.WriteLine($"Issue {i + 1}: {title} [{severity}]");
            }

            var mappedResults = VulnerabilityMapper.FromAsca(results.ScanDetails, sourceFilePath);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, sourceFilePath);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static AscaService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new AscaService(cxWrapper);
            }
            return _instance;
        }
    }
}
