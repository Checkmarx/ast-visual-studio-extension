using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers
{
    /// <summary>
    /// Realtime Containers scanner service for detecting vulnerable container images.
    /// Scans Dockerfile and docker-compose files for base images with known vulnerabilities.
    /// Requires Docker or Podman to be installed.
    /// </summary>
    public class ContainersService : BaseRealtimeScannerService
    {
        private static volatile ContainersService _instance;
        private static readonly object _lock = new object();
        private readonly string _containersTool;

        private static readonly HashSet<string> ContainerFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dockerfile", "docker-compose.yml", "docker-compose.yaml"
        };

        protected override string ScannerName => "Containers";

        private ContainersService(CxWrapper cxWrapper, string containersTool = "docker") : base(cxWrapper)
        {
            _containersTool = containersTool ?? "docker";
        }

        /// <summary>
        /// Containers scanner only scans Dockerfile and docker-compose files.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var fileName = System.IO.Path.GetFileName(filePath);
            return ContainerFileNames.Contains(fileName) ||
                   fileName.StartsWith("dockerfile", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Invokes the Containers realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// Silently skips if Docker/Podman is not available.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, Document document)
        {
            // Check if Docker/Podman is available first
            bool engineExists = await _cxWrapper.CheckEngineExistAsync(_containersTool);
            if (!engineExists)
            {
                // Silently skip if container tool is not available
                return 0;
            }

            var results = await _cxWrapper.ContainersRealtimeScanAsync(tempFilePath,
                ignoredFilePath: null, engine: _containersTool);
            if (results?.Images == null || results.Images.Count == 0) return 0;

            var mappedResults = VulnerabilityMapper.FromContainers(results.Images, document.FullName);
            // TODO: Integrate with findings display (after CxAssistDisplayCoordinator PR merges)
            // CxAssistDisplayCoordinator.UpdateFindings(buffer, mappedResults, document.FullName);
            return mappedResults.Count;
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static ContainersService GetInstance(CxWrapper cxWrapper, string containersTool = "docker")
        {
            if (_instance != null) return _instance;
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new ContainersService(cxWrapper, containersTool);
            }
            return _instance;
        }
    }
}
