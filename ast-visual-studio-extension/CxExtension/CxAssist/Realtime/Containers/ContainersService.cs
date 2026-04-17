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

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Containers
{
    /// <summary>
    /// Realtime Containers scanner service for detecting vulnerable container images.
    /// Scans Dockerfile and docker-compose files for base images with known vulnerabilities.
    /// Requires Docker or Podman to be installed.
    /// </summary>
    public class ContainersService : SingletonScannerBase<ContainersService>
    {
        private readonly string _containersTool;
        private static readonly IFileFilterStrategy _fileFilter = new ContainersFileFilterStrategy();

        protected override string ScannerName => "Containers";

        protected override ScannerType CoordinatorScannerType => ScannerType.Containers;

        private ContainersService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper, string containersTool = "docker") : base(cxWrapper)
        {
            _containersTool = containersTool ?? "docker";
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
        /// Containers scanner scans Dockerfile, docker-compose, and Helm chart YAML files.
        /// Uses cached FileFilterStrategy for consistent, enhanced filtering rules.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return _fileFilter.ShouldScanFile(filePath);
        }

        /// <summary>
        /// Containers scanner uses a directory-based temp strategy with content hash.
        /// Creates: %TEMP%/Cx-container-realtime-scanner/{contentHash}/{originalFileName}
        /// Helm chart YAML uses a <c>helm</c> subdirectory under temp (aligned with JetBrains).
        /// </summary>
        protected override string CreateTempFilePath(string originalFileName, string content, string fullSourcePath = null)
        {
            var hash = Utils.TempFileManager.GetContentHash(content);
            var isHelm = IsHelmChartPath(fullSourcePath);
            var tempDir = Utils.TempFileManager.CreateContainersTempDir(hash, isHelmFile: isHelm);
            return Path.Combine(tempDir, originalFileName);
        }

        private static bool IsHelmChartPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return false;
            return fullPath.IndexOf("\\helm\\", StringComparison.OrdinalIgnoreCase) >= 0
                || fullPath.IndexOf("/helm/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Invokes the Containers realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// Shows error if Docker/Podman is not available (aligned with JetBrains error handling).
        /// Catches and logs all errors to the output pane.
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            try
            {
                // Check if Docker/Podman is available first
                bool engineExists = await _cxWrapper.CheckEngineExistAsync(_containersTool);
                if (!engineExists)
                {
                    OutputPaneWriter.WriteError($"{ScannerName} scanner: {_containersTool} is not available. Please ensure Docker or Podman is installed and running.");
                    _logger.Warn($"{ScannerName} scanner: {_containersTool} engine not found on system");
                    return 0;
                }

                if (new System.IO.FileInfo(tempFilePath).Length == 0)
                {
                    OutputPaneWriter.WriteWarning($"{ScannerName} scanner: no content found in file - {Path.GetFileName(sourceFilePath)}");
                    return 0;
                }

                // CLI: cx scan containers-realtime has no --engine; _containersTool is only used for CheckEngineExistAsync above.
                var results = await _cxWrapper.ContainersRealtimeScanAsync(tempFilePath, ignoredFilePath: null);

            if (results == null)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: null results returned - {sourceFilePath}");
                return 0;
            }

            if (results.Images == null || results.Images.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no images found - {sourceFilePath}");
                return 0;
            }

                int imageCount = results.Images.Count;
                OutputPaneWriter.WriteLine($"{ScannerName} scanner: {imageCount} image(s) with vulnerabilities — {Path.GetFileName(sourceFilePath)}");

                var mappedResults = VulnerabilityMapper.FromContainers(results.Images, sourceFilePath);
                CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(sourceFilePath, CoordinatorScannerType, mappedResults);
                return mappedResults.Count;
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: failed to scan {Path.GetFileName(sourceFilePath)} - {ex.Message}");
                _logger.Warn($"{ScannerName} scanner: scan error on {Path.GetFileName(sourceFilePath)}: {ex.Message}", ex);
                ClearDisplayForFile(sourceFilePath);
                return 0;
            }
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// If tool parameter changes (e.g. docker → podman), the instance is reset
        /// to ensure the new tool is used on next GetOrCreate.
        /// </summary>
        public static ContainersService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper, string containersTool = "docker")
        {
            containersTool = string.IsNullOrEmpty(containersTool) ? "docker" : containersTool;
            return GetOrCreate(() => new ContainersService(cxWrapper, containersTool));
        }
    }
}
