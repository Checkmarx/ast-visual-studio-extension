using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Oss
{
    /// <summary>
    /// Open Source Software (OSS) and Malicious package realtime scanner service.
    /// Scans dependency manifest files (package.json, pom.xml, requirements.txt, etc.)
    /// for known vulnerabilities and malicious packages.
    /// </summary>
    public class OssService : SingletonScannerBase<OssService>
    {
        private static readonly IFileFilterStrategy _fileFilter = new OssFileFilterStrategy();

        private CancellationTokenSource _manifestSweepCts;

        protected override string ScannerName => "OSS";

        protected override ScannerType CoordinatorScannerType => ScannerType.OSS;

        private OssService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper) : base(cxWrapper)
        {
        }

        /// <summary>
        /// JetBrains parity: <c>OssScannerCommand.initializeScanner</c> → <c>scanAllManifestFilesInFolder</c> when OSS starts.
        /// Registers editor events via base, then may queue one background sweep per solution per session — not on every
        /// orchestrator re-init when only other scanners (e.g. ASCA) change.
        /// </summary>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            string solutionRoot = RealtimeSolutionScanner.TryGetSolutionDirectory();
            if (string.IsNullOrEmpty(solutionRoot))
            {
                OutputPaneWriter.WriteLine("OSS scanner: manifest sweep skipped — no solution directory. Save the solution or open a .sln file.");
                return;
            }

            if (!OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(solutionRoot))
            {
                _logger.Debug($"OSS scanner: manifest sweep skipped — already completed for this solution in this session ({solutionRoot})");
                return;
            }

            _manifestSweepCts?.Cancel();
            _manifestSweepCts?.Dispose();
            _manifestSweepCts = new CancellationTokenSource();
            CancellationTokenSource sweepCts = _manifestSweepCts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await ScanAllManifestsInSolutionAsync(solutionRoot, sweepCts.Token).ConfigureAwait(false);
                    if (!sweepCts.Token.IsCancellationRequested)
                    {
                        OssManifestSweepPolicy.MarkSweepCompleted(solutionRoot);
                        OutputPaneWriter.WriteLine("OSS scanner: startup manifest sweep completed");
                    }
                }
                catch (OperationCanceledException)
                {
                    OutputPaneWriter.WriteLine("OSS scanner: manifest folder sweep stopped (scanner disabled or session ended).");
                }
                catch (Exception ex)
                {
                    OutputPaneWriter.WriteWarning($"OSS scanner: manifest folder sweep failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Unregisters the scanner and resets the singleton.
        /// Allows re-registration to create a fresh instance with proper event wiring.
        /// </summary>
        public override void CancelPendingScans()
        {
            try
            {
                _manifestSweepCts?.Cancel();
            }
            catch
            {
                // ignore
            }

            base.CancelPendingScans();
        }

        public override async Task UnregisterAsync()
        {
            try
            {
                _manifestSweepCts?.Cancel();
                _manifestSweepCts?.Dispose();
            }
            catch
            {
                // ignore
            }
            finally
            {
                _manifestSweepCts = null;
            }

            await base.UnregisterAsync();
            ResetInstance();
        }

        /// <summary>
        /// OSS scanner scans dependency manifest files and additional package manager files.
        /// Uses cached FileFilterStrategy for consistent, enhanced filtering rules including Pipfile, setup.py, etc.
        /// </summary>
        public override bool ShouldScanFile(string filePath)
        {
            return _fileFilter.ShouldScanFile(filePath);
        }

        /// <summary>
        /// OSS scanner uses a directory-based temp strategy with content hash.
        /// Creates: %TEMP%/Cx-oss-realtime-scanner/{contentHash}/{originalFileName}
        /// Also copies companion lock files into the same directory (e.g., package-lock.json, yarn.lock).
        /// </summary>
        protected override string CreateTempFilePath(string originalFileName, string content, string fullSourcePath = null)
        {
            var hash = Utils.TempFileManager.GetContentHash(content);
            var tempDir = Utils.TempFileManager.CreateOssTempDir(hash);
            return Path.Combine(tempDir, originalFileName);
        }

        /// <summary>
        /// Invokes the OSS realtime scan CLI command.
        /// Maps results to Result objects for display in the findings panel.
        /// Copies companion lock files (package-lock.json, yarn.lock) alongside the temp file.
        /// Catches and logs all errors to the output pane (aligned with JetBrains error handling).
        /// </summary>
        protected override async Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath)
        {
            try
            {
                if (new System.IO.FileInfo(tempFilePath).Length == 0)
                {
                    OutputPaneWriter.WriteWarning($"{ScannerName} scanner: no content found in file - {Path.GetFileName(sourceFilePath)}");
                    return 0;
                }

                // Copy companion lock file (package-lock.json / yarn.lock) alongside temp file
                CopyCompanionLockFile(sourceFilePath, Path.GetDirectoryName(tempFilePath));

                var results = await _cxWrapper.OssRealtimeScanAsync(tempFilePath);

                if (results?.Packages == null || results.Packages.Count == 0)
                {
                    ClearDisplayForFile(sourceFilePath);
                    return 0;
                }

                int packageCount = results.Packages.Count;
                OutputPaneWriter.WriteLine($"{ScannerName} scanner: {packageCount} vulnerable package(s) found — {Path.GetFileName(sourceFilePath)}");

                var mappedResults = VulnerabilityMapper.FromOss(results.Packages, sourceFilePath);
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
        /// Scans every dependency manifest under the solution directory.
        /// Invoked from <see cref="InitializeAsync"/> (JetBrains: <c>scanAllManifestFilesInFolder</c> on scanner start).
        /// Runs with limited parallelism so the IDE stays responsive.
        /// </summary>
        public async Task ScanAllManifestsInSolutionAsync(string solutionRoot, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(solutionRoot) || !Directory.Exists(solutionRoot))
                return;

            var paths = RealtimeSolutionScanner.EnumerateFiles(solutionRoot).Where(ShouldScanFile).ToList();
            OutputPaneWriter.WriteLine($"OSS scanner: startup manifest sweep — {paths.Count} file(s)");

            var semaphore = new SemaphoreSlim(2);
            try
            {
                foreach (var path in paths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await ScanExternalFileAsync(path).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }
            finally
            {
                semaphore.Dispose();
            }
        }

        /// <summary>
        /// JetBrains <c>scanAllManifestFilesInFolder</c>: rescans every dependency manifest under the solution
        /// (login resync, explicit manifest trigger, or OSS re-enabled after init skipped sweep due to policy).
        /// </summary>
        public override async Task RescanManifestFilesAsync(string solutionRoot)
        {
            if (string.IsNullOrEmpty(solutionRoot) || !Directory.Exists(solutionRoot))
                return;

            await ScanAllManifestsInSolutionAsync(solutionRoot, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static OssService GetInstance(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
            => GetOrCreate(() => new OssService(cxWrapper));

        /// <summary>
        /// Copies companion lock files to temp directory using centralized manager.
        ///
        /// CRITICAL: Lock files are essential for accurate OSS scanning.
        /// Different package managers use different lock file formats:
        /// - NPM: package-lock.json, npm-shrinkwrap.json
        /// - Yarn: yarn.lock
        /// - Maven: pom.xml.lock
        /// - .NET: package.lock.json, packages.lock.json
        ///
        /// Lock files are optional but when present, they MUST be copied.
        /// Without them, OSS scanning provides incomplete results.
        /// </summary>
        private void CopyCompanionLockFile(string originalFilePath, string tempDir)
        {
            CompanionFileManager.CopyCompanionLockFiles(originalFilePath, tempDir);
        }
    }
}
