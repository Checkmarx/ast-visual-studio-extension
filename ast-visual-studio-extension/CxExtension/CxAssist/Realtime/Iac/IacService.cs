using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Iac
{
    /// <summary>
    /// Infrastructure as Code (IaC) realtime scanner service.
    /// Scans Terraform, YAML, JSON, Dockerfile, and related IaC configuration files.
    /// </summary>
    public class IacService : BaseRealtimeScannerService
    {
        private static volatile IacService _instance;
        private static readonly object _lock = new object();

        private static readonly HashSet<string> IacExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tf", ".yaml", ".yml", ".json", ".hcl", ".bicep", ".arm", ".tmpl"
        };

        private static readonly HashSet<string> IacFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dockerfile", "docker-compose.yml", "docker-compose.yaml", "buildspec.yml", "buildspec.yaml"
        };

        protected override string ScannerName => "IaC";

        private IacService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// IaC scanner scans IaC-related file types.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var fileName = System.IO.Path.GetFileName(filePath);
            var ext = System.IO.Path.GetExtension(filePath);

            return IacExtensions.Contains(ext) || IacFileNames.Contains(fileName);
        }

        /// <summary>
        /// Invokes the IaC realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            var results = await _cxWrapper.IacRealtimeScanAsync(tempFilePath);
            if (results?.Results == null || results.Results.Count == 0) return 0;

            var mappedResults = VulnerabilityMapper.FromIac(results.Results, document.FullName);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, document.FullName);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static IacService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new IacService(cxWrapper);
            }
            return _instance;
        }
    }
}
