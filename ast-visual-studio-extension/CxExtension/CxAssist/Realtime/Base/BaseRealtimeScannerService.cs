using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxExtension.Utils;
using EnvDTE;
using EnvDTE80;
using log4net;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base
{
    /// <summary>
    /// Abstract base class for all realtime scanner services (ASCA, Secrets, IaC, Containers, OSS).
    /// Provides per-file debounced scheduling, optional content fingerprint skip, DTE event wiring,
    /// and temp file lifecycle management.
    /// </summary>
    public abstract class BaseRealtimeScannerService : IRealtimeScannerService
    {
        protected readonly ast_visual_studio_extension.CxCLI.CxWrapper _cxWrapper;
        protected readonly ILog _logger;
        private readonly RealtimeFileScanScheduler _debounceScheduler;
        private const int SCAN_TIMEOUT_MS = 60000;
        private const long MAX_FILE_SIZE_BYTES = 100 * 1024 * 1024;
        private bool _isSubscribed = false;
        private bool _isInitialized = false;
        private string _lastDocumentContent = string.Empty;
        /// <summary>
        /// Document path that <see cref="_lastDocumentContent"/> refers to. When the user switches tabs or opens a file,
        /// <see cref="OnTextChanged"/> must not treat the new buffer as an "edit" vs the previous file's snapshot.
        /// </summary>
        private string _lineChangeBaselinePath;
        private int _currentDocumentVersion = 0;
        private DateTime _lastResultTimestamp = DateTime.MinValue;
        private TextEditorEvents _textEditorEvents;
        /// <summary>
        /// Must be retained for the lifetime of the subscription. If this reference is dropped, COM will not
        /// deliver <see cref="DocumentEvents.DocumentOpened"/> / <see cref="DocumentEvents.DocumentClosing"/> (VS extensibility requirement).
        /// </summary>
        private DocumentEvents _documentEvents;

        /// <summary>
        /// Last successful scan content fingerprint per normalized path (skip redundant rescans when unchanged).
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _lastScannedContentFingerprint =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] AiAgentFilePaths = { "Dummy.txt", "AIAssistantInput" };

        protected abstract string ScannerName { get; }

        /// <summary>
        /// Scanner type used when merging into the display coordinator so clearing one engine does not remove others.
        /// </summary>
        protected abstract ScannerType CoordinatorScannerType { get; }

        public abstract bool ShouldScanFile(string filePath);

        /// <summary>
        /// Creates the temporary file/directory path for scanning.
        /// </summary>
        /// <param name="fullSourcePath">Full path of the source file (used by Containers for Helm layout).</param>
        protected virtual string CreateTempFilePath(string originalFileName, string content, string fullSourcePath = null)
        {
            var sanitizedFileName = Utils.TempFileManager.SanitizeFilename(originalFileName, 255);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);
            var extension = Path.GetExtension(sanitizedFileName);
            return Path.Combine(Path.GetTempPath(), $"{fileNameWithoutExt}_{timestamp}{extension}");
        }

        /// <summary>
        /// Performs the scan CLI call and maps results. <paramref name="sourceFilePath"/> is the original file path.
        /// </summary>
        protected abstract Task<int> ScanAndDisplayAsync(string tempFilePath, string sourceFilePath);

        protected async Task<T> ExecuteScanWithTimeoutAsync<T>(
            Func<CancellationToken, Task<T>> scanOperation,
            string filePath) where T : class
        {
            try
            {
                if (!ValidateFileSize(filePath))
                    return null;

                using (var cts = new CancellationTokenSource(SCAN_TIMEOUT_MS))
                {
                    return await scanOperation(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Error($"{ScannerName} scanner: Scan timeout after {SCAN_TIMEOUT_MS / 1000} seconds on {Path.GetFileName(filePath)}");
                OutputPaneWriter.WriteError($"{ScannerName}: Scan timeout after {SCAN_TIMEOUT_MS / 1000}s");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"{ScannerName} scanner: Scan error on {Path.GetFileName(filePath)}: {ex.Message}", ex);
                throw;
            }
        }

        private bool ValidateFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                {
                    OutputPaneWriter.WriteWarning($"{ScannerName}: Skipping {Path.GetFileName(filePath)} — file exceeds {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB limit");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"{ScannerName}: Error validating file size: {ex.Message}");
                return true;
            }
        }

        protected BaseRealtimeScannerService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
            _logger = LogManager.GetLogger(GetType());
            try
            {
                _debounceScheduler = new RealtimeFileScanScheduler(ThreadHelper.JoinableTaskFactory);
            }
            catch (Exception)
            {
                // In unit test context without VS UI (NullReferenceException or InvalidOperationException),
                // create scheduler with null factory (scheduler will not be functional, but service can be instantiated for testing)
                _debounceScheduler = new RealtimeFileScanScheduler(null);
            }
        }

        public virtual async Task InitializeAsync()
        {
            if (_isInitialized) return;
            try
            {
                _isInitialized = true;
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RegisterDteRealtimeEvents();
                OutputPaneWriter.WriteLine($"{ScannerName} scanner: initialized for real-time scanning");
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: failed to initialize - {ex.Message}");
                _isInitialized = false;
                throw;
            }
        }

        public virtual async Task UnregisterAsync()
        {
            try
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                }
                catch (Exception)
                {
                    // In unit test context without VS UI, skip the main thread switch
                }

                _debounceScheduler?.Dispose();

                if (!_isSubscribed) return;

                if (_textEditorEvents != null)
                {
                    _textEditorEvents.LineChanged -= OnTextChanged;
                    _textEditorEvents = null;
                }

                if (_documentEvents != null)
                {
                    _documentEvents.DocumentOpened -= OnDocumentOpened;
                    _documentEvents.DocumentClosing -= OnDocumentClosing;
                    _documentEvents = null;
                }

                _isSubscribed = false;
                _isInitialized = false;
                OutputPaneWriter.WriteLine($"{ScannerName} scanner: disabled");
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: failed to disable - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Scans a file on disk (startup sweep, manifest watcher). Does not require an open editor.
        /// Validates file path to prevent path traversal attacks.
        /// </summary>
        public virtual async Task ScanExternalFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                if (!TempFileManager.TryReadVerifiedExistingFileContent(filePath, MAX_FILE_SIZE_BYTES, out var content, out var safePath))
                {
                    if (TempFileManager.TryGetVerifiedRegularFileInfo(filePath, out var fiDiag) && fiDiag.Length > MAX_FILE_SIZE_BYTES)
                        OutputPaneWriter.WriteWarning($"{ScannerName} scanner: skipping {fiDiag.Name} — file exceeds {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB limit");
                    else
                        _logger.Debug($"{ScannerName} scanner: skipping unsafe or missing file: {Path.GetFileName(filePath)}");
                    return;
                }

                // Background / batch scans run in parallel; status bar Push/Pop is not LIFO-safe and misleads when most paths skip.
                await RunScanCoreAsync(safePath, content, bypassContentFingerprint: false, showStatusBarProgress: false)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: failed to scan {Path.GetFileName(filePath)} - {ex.Message}");
                _logger.Warn($"{ScannerName} scanner: ScanExternalFileAsync failed for {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
        }

        protected bool IsResultFresh(DateTime resultTimestamp, int scanDocumentVersion)
        {
            if (resultTimestamp <= _lastResultTimestamp)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName}: Discarding stale result (timestamp {resultTimestamp} <= {_lastResultTimestamp})");
                return false;
            }

            if (scanDocumentVersion != _currentDocumentVersion)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName}: Discarding result (document edited: version {scanDocumentVersion} != {_currentDocumentVersion})");
                return false;
            }

            _lastResultTimestamp = resultTimestamp;
            return true;
        }

        /// <summary>
        /// Wires DTE subscriptions for realtime scans: active editor line changes and solution-wide document open/close.
        /// </summary>
        private void RegisterDteRealtimeEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_isSubscribed) return;

            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte == null) return;

            RegisterTextEditorEvents(dte);
            RegisterDocumentEvents(dte);

            _isSubscribed = true;

            var document = dte.ActiveDocument;
            if (document != null)
                TrySyncLineChangeBaseline(document);

            OutputPaneWriter.WriteLine($"{ScannerName} scanner: monitoring enabled");

            ScheduleActiveDocumentOpenScanAfterSubscribe();
        }

        /// <summary>
        /// <see cref="DocumentEvents.DocumentOpened"/> does not fire for documents already open when we subscribe (e.g. package.json at startup).
        /// Mirrors <see cref="OnDocumentOpened"/> for the active document, with short retries when DTE text is not yet hydrated.
        /// </summary>
        private void ScheduleActiveDocumentOpenScanAfterSubscribe()
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    for (int attempt = 0; attempt < 6; attempt++)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
                        var document = dte?.ActiveDocument;
                        if (document == null)
                            return;

                        if (!ShouldScanFile(document.FullName))
                            return;

                        var textDocument = (TextDocument)document.Object("TextDocument");
                        if (textDocument?.StartPoint == null || textDocument?.EndPoint == null)
                            return;

                        var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            _debounceScheduler?.CancelPending(document.FullName);
                            TrySyncLineChangeBaseline(document);
                            await InstantScanAsync(document);
                            return;
                        }

                        await Task.Delay(75);
                    }
                }
                catch (Exception ex)
                {
                    OutputPaneWriter.WriteError($"{ScannerName}: Active document scan after subscribe failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Debounced scan when the user edits the <em>active</em> document (<see cref="TextEditorEvents.LineChanged"/>).
        /// </summary>
        private void RegisterTextEditorEvents(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_textEditorEvents != null) return;

            _textEditorEvents = dte.Events.TextEditorEvents;
            _textEditorEvents.LineChanged += OnTextChanged;
        }

        /// <summary>
        /// Instant scan on open and cancel pending work on close (<see cref="DocumentEvents"/> for all documents).
        /// The <see cref="_documentEvents"/> field must be held for the lifetime of the subscription (COM).
        /// </summary>
        private void RegisterDocumentEvents(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_documentEvents != null) return;

            // Events2 + null document = all documents.
            _documentEvents = ((Events2)dte.Events).get_DocumentEvents(null);
            _documentEvents.DocumentOpened += OnDocumentOpened;
            _documentEvents.DocumentClosing += OnDocumentClosing;
        }

        /// <summary>
        /// Aligns line-change detection with the given document so we do not treat a tab switch as an edit.
        /// </summary>
        private void TrySyncLineChangeBaseline(Document document)
        {
            try
            {
                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument?.StartPoint == null || textDocument?.EndPoint == null)
                    return;
                _lineChangeBaselinePath = document.FullName;
                _lastDocumentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            }
            catch (Exception ex)
            {
                _logger.Debug($"{ScannerName}: TrySyncLineChangeBaseline failed: {ex.Message}");
            }
        }

        private void OnTextChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            if (!_isSubscribed) return;

            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
            var document = dte?.ActiveDocument;
            if (document == null) return;

            try
            {
                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument == null) return;

                var currentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                var path = document.FullName;

                // Tab switch / newly opened file: rebaseline without scheduling a scan (DocumentOpened handles open).
                if (_lineChangeBaselinePath == null || !PathsEqual(path, _lineChangeBaselinePath))
                {
                    _lineChangeBaselinePath = path;
                    _lastDocumentContent = currentContent;
                    return;
                }

                if (_lastDocumentContent == currentContent) return;

                _lastDocumentContent = currentContent;
                _currentDocumentVersion++;

                _debounceScheduler.Schedule(path, async ct => await ExecuteDebouncedScanAsync(path, ct));
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} - Exception: {ex.Message}");
            }
        }

        private async Task ExecuteDebouncedScanAsync(string expectedPath, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
            if (ct.IsCancellationRequested) return;

            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
            var document = dte?.ActiveDocument;
            if (document == null) return;

            if (!PathsEqual(document.FullName, expectedPath))
                return;

            if (AiAgentFilePaths.Any(p => document.FullName.Contains(p)))
                return;

            if (document.FullName.Contains("\\node_modules\\") || document.FullName.Contains("/node_modules/"))
            {
                _logger.Debug($"{ScannerName} scanner: file not eligible (base filter) - {document.FullName}");
                return;
            }

            if (!ShouldScanFile(document.FullName))
            {
                _logger.Debug($"{ScannerName} scanner: unsupported file - {document.FullName}");
                return;
            }

            var textDocument = (TextDocument)document.Object("TextDocument");
            if (textDocument?.StartPoint == null || textDocument?.EndPoint == null) return;

            var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            if (string.IsNullOrWhiteSpace(content))
                return;

            await RunScanCoreAsync(document.FullName, content, bypassContentFingerprint: false);
        }

        private void OnDocumentOpened(Document document)
        {
            if (!_isSubscribed || document == null) return;

            try
            {
                if (!ShouldScanFile(document.FullName))
                    return;

                // Drop debounced work queued from a spurious LineChanged during tab switch; avoid scanning twice with InstantScan.
                _debounceScheduler?.CancelPending(document.FullName);
                TrySyncLineChangeBaseline(document);

                _logger.Debug($"{ScannerName}: File opened: {document.Name}");
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () => await InstantScanAsync(document));
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName}: Error in OnDocumentOpened: {ex.Message}");
            }
        }

        private void OnDocumentClosing(Document document)
        {
            if (!_isSubscribed || document == null) return;

            try
            {
                _debounceScheduler?.CancelPending(document.FullName);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName}: Error in OnDocumentClosing: {ex.Message}");
            }
        }

        private async Task InstantScanAsync(Document document)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument?.StartPoint == null || textDocument?.EndPoint == null)
                    return;

                var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                if (string.IsNullOrWhiteSpace(content))
                    return;

                await RunScanCoreAsync(document.FullName, content, bypassContentFingerprint: true);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ScannerName} scanner: Scan error on {Path.GetFileName(document.FullName)}: {ex.Message}", ex);
            }
        }

        private async Task<int> RunScanCoreAsync(
            string sourceFilePath,
            string content,
            bool bypassContentFingerprint,
            bool showStatusBarProgress = true)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            var key = NormalizePathKey(sourceFilePath);
            if (!bypassContentFingerprint)
            {
                var fp = Utils.TempFileManager.GetContentHash(content);
                if (_lastScannedContentFingerprint.TryGetValue(key, out var prev) && prev == fp)
                {
                    // Batch rescans hit this thousands of times — do not write to Checkmarx Output (use Debug only).
                    Debug.WriteLine($"{ScannerName}: skip scan (unchanged content) - {sourceFilePath}");
                    return 0;
                }
            }

            string tempFilePath = null;
            try
            {
                _logger.Debug($"{ScannerName} scanner: starting scan - {sourceFilePath}");

                var originalFileName = Path.GetFileName(sourceFilePath);
                tempFilePath = CreateTempFilePath(originalFileName, content, sourceFilePath);

                var tempDir = Path.GetDirectoryName(tempFilePath);
                if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                File.WriteAllText(tempFilePath, content);

                bool progressShown = false;
                if (showStatusBarProgress)
                {
                    await RealtimeScanProgressIndicator.PushScanAsync(ScannerName, sourceFilePath);
                    progressShown = true;
                }

                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var count = await ScanAndDisplayAsync(tempFilePath, sourceFilePath);
                    sw.Stop();
                    ScanMetricsLogger.LogRealtimeScanCompleted(ScannerName, sourceFilePath, sw.ElapsedMilliseconds, count);
                    LogRealtimeDetectionTelemetry(count);

                    var fpDone = Utils.TempFileManager.GetContentHash(content);
                    _lastScannedContentFingerprint[key] = fpDone;

                    return count;
                }
                finally
                {
                    if (progressShown)
                        await RealtimeScanProgressIndicator.PopScanAsync();
                }
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"{ScannerName} scanner: failed to scan {Path.GetFileName(sourceFilePath)} - {ex.Message}");
                _logger.Error($"{ScannerName} scanner: scan error - {ex.Message}", ex);
                return 0;
            }
            finally
            {
                CleanupTempArtifacts(tempFilePath);
            }
        }

        private void CleanupTempArtifacts(string tempFilePath)
        {
            if (string.IsNullOrEmpty(tempFilePath)) return;

            try
            {
                var tempDir = Path.GetDirectoryName(tempFilePath);
                var isSubDir = !string.Equals(
                    tempDir?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);

                if (isSubDir && Directory.Exists(tempDir))
                    Utils.TempFileManager.DeleteTempDirectory(tempDir);
                else if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"{ScannerName} scanner: error cleaning up temp: {ex.Message}");
            }
        }

        /// <summary>
        /// Generic logging helper for ScanAndDisplayAsync implementations.
        /// Logs raw JSON, handles null/empty results, and formats individual items.
        /// Reduces boilerplate across all five scanner types.
        /// </summary>
        protected void LogScanResults<TItem>(
            object rawResult,
            IList<TItem> items,
            string itemLabel,
            string sourceFilePath,
            Func<TItem, string> describeItem)
        {
            if (rawResult != null)
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: raw JSON - "
                    + JsonConvert.SerializeObject(rawResult, Formatting.Indented));

            if (items == null || items.Count == 0)
            {
                OutputPaneWriter.WriteDebug($"{ScannerName} scanner: no results - {Path.GetFileName(sourceFilePath)}");
                return;
            }

            for (int i = 0; i < items.Count; i++)
                OutputPaneWriter.WriteDebug($"{ScannerName} {itemLabel} {i + 1}: {describeItem(items[i])}");
        }

        /// <summary>
        /// Clears markers and stored findings for this scanner only on the given file; other engines' findings stay.
        /// Call when a scan returns 0 results so stale markers for this engine are removed after a fix.
        /// </summary>
        protected void ClearDisplayForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                Core.CxAssistDisplayCoordinator.MergeUpdateFindingsForScanner(filePath, CoordinatorScannerType, new List<Vulnerability>());
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteWarning($"ClearDisplayForFile failed for {filePath}: {ex.Message}");
            }
        }

        protected void LogRealtimeDetectionTelemetry(int issueCount)
        {
            if (issueCount <= 0) return;
            try
            {
                _cxWrapper.LogDetectionTelemetryFireAndForget($"Realtime{ScannerName}", "IssuesFound", issueCount);
            }
            catch
            {
                // Telemetry must never break scanning.
            }
        }

        private static bool PathsEqual(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string NormalizePathKey(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path ?? string.Empty;
            }
        }

        /// <summary>
        /// Instantly scans the given file (e.g., active document).
        /// Called when user logs in or scanners are re-enabled.
        /// </summary>
        public virtual async Task InstantScanAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !ShouldScanFile(filePath))
                return;

            try
            {
                if (!TempFileManager.TryReadVerifiedExistingFileContent(filePath, MAX_FILE_SIZE_BYTES, out var content, out var safePath))
                {
                    if (TempFileManager.TryGetVerifiedRegularFileInfo(filePath, out var fiDiag) && fiDiag.Length > MAX_FILE_SIZE_BYTES)
                        OutputPaneWriter.WriteWarning($"{ScannerName}: Skipping {fiDiag.Name} — file exceeds {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB limit");
                    return;
                }

                await RunScanCoreAsync(safePath, content, bypassContentFingerprint: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warn($"{ScannerName} scanner: InstantScanAsync failed for {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Triggers an instant scan of the currently active document.
        /// </summary>
        public virtual async Task TriggerCurrentDocumentScanAsync()
        {
            try
            {
                var dte = ServiceProvider.GlobalProvider?.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.ActiveDocument == null)
                    return;

                var filePath = dte.ActiveDocument.FullName;
                if (string.IsNullOrEmpty(filePath) || !ShouldScanFile(filePath))
                    return;

                await InstantScanAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.Warn($"{ScannerName} scanner: TriggerCurrentDocumentScanAsync failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JetBrains parity: ASCA, Secrets, IaC, and Containers scan only the active editor document
        /// (open/save/line-change). Solution-wide dependency manifest enumeration is OSS-only; see
        /// <see cref="Oss.OssService.RescanManifestFilesAsync"/>.
        /// </summary>
        public virtual Task RescanManifestFilesAsync(string solutionRoot) => Task.CompletedTask;

        /// <summary>
        /// Cancels all pending debounced scans.
        /// Called when scanner is disabled in settings to stop in-flight work.
        /// </summary>
        public virtual void CancelPendingScans()
        {
            try
            {
                _debounceScheduler?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Warn($"{ScannerName} scanner: CancelPendingScans failed: {ex.Message}", ex);
            }
        }
    }
}
