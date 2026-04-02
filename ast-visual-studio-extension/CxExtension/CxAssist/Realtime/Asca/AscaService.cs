using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using EnvDTE;
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
        /// ASCA scanner only scans supported source code file types.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var ext = System.IO.Path.GetExtension(filePath);
            return AscaExtensions.Contains(ext);
        }

        /// <summary>
        /// Invokes the ASCA realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            var results = await _cxWrapper.ScanAscaAsync(tempFilePath, ascaLatestVersion: false);
            if (results?.ScanDetails == null || results.ScanDetails.Count == 0) return 0;

            var mappedResults = VulnerabilityMapper.FromAsca(results.ScanDetails, document.FullName);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, document.FullName);
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
