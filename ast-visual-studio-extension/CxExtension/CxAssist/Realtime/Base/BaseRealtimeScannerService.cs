using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using ast_visual_studio_extension.CxExtension.Utils;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using static System.Diagnostics.Stopwatch;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base
{
    /// <summary>
    /// Abstract base class for all realtime scanner services (ASCA, Secrets, IaC, Containers, OSS).
    /// Provides shared debounce logic, DTE event wiring, and temp file lifecycle management.
    /// Subclasses implement the specific file filter (ShouldScanFile) and scan logic (ScanAndDisplayAsync).
    /// </summary>
    public abstract class BaseRealtimeScannerService : IRealtimeScannerService
    {
        protected readonly ast_visual_studio_extension.CxCLI.CxWrapper _cxWrapper;
        private readonly System.Timers.Timer _debounceTimer;
        private const int DEBOUNCE_DELAY = 2000;
        private const int SCAN_TIMEOUT_MS = 60000; // 60 second timeout for CLI scans
        private const long MAX_FILE_SIZE_BYTES = 100 * 1024 * 1024; // 100MB max file size
        private bool _isSubscribed = false;
        private bool _isInitialized = false;
        private string _lastDocumentContent = string.Empty;
        private int _currentDocumentVersion = 0; // Track document changes for result freshness
        private DateTime _lastResultTimestamp = DateTime.MinValue; // Track result display timestamps
        private TextEditorEvents _textEditorEvents;

        // AI assistant temp files that should be skipped (Copilot, GitHub Copilot, etc.)
        private static readonly string[] AiAgentFilePaths = { "Dummy.txt", "AIAssistantInput" };

        /// <summary>
        /// Gets the human-readable name of this scanner (e.g., "ASCA", "Secrets", "IaC").
        /// Used for logging and output pane messages.
        /// </summary>
        protected abstract string ScannerName { get; }

        /// <summary>
        /// Determines whether this scanner should process the given file path.
        /// Subclasses override to implement scanner-specific file filtering.
        /// </summary>
        public abstract bool ShouldScanFile(string filePath);

        /// <summary>
        /// Creates the temporary file/directory path for scanning.
        /// Can be overridden by subclasses to use scanner-specific strategies (e.g., TempFileManager).
        /// Default: creates a flat temp file in system temp directory.
        /// </summary>
        protected virtual string CreateTempFilePath(string originalFileName, string content)
        {
            // Default strategy: flat file in system temp directory with timestamp
            var sanitizedFileName = Utils.TempFileManager.SanitizeFilename(originalFileName, 255);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);
            var extension = Path.GetExtension(sanitizedFileName);
            return Path.Combine(Path.GetTempPath(), $"{fileNameWithoutExt}_{timestamp}{extension}");
        }

        /// <summary>
        /// Performs the actual scan and returns the result count.
        /// Called after debounce timer fires and the temp file is written.
        /// Subclasses must implement the specific scanner invocation.
        /// Results are mapped to Vulnerability objects and passed to CxAssistDisplayCoordinator.
        /// </summary>
        protected abstract Task<int> ScanAndDisplayAsync(string tempFilePath, Document document);

        /// <summary>
        /// Wraps an async scan operation with timeout and file size validation.
        /// Prevents UI freezes from long-running scans and handles large files.
        /// </summary>
        protected async Task<T> ExecuteScanWithTimeoutAsync<T>(
            Func<CancellationToken, Task<T>> scanOperation,
            string filePath) where T : class
        {
            try
            {
                // Validate file size before scanning
                if (!ValidateFileSize(filePath))
                {
                    Utils.ScanMetricsLogger.LogScanSkipped(ScannerName, filePath, "file size exceeds 100MB limit");
                    return null;
                }

                // Create cancellation token with 60 second timeout
                using (var cts = new CancellationTokenSource(SCAN_TIMEOUT_MS))
                {
                    return await scanOperation(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Utils.ScanMetricsLogger.LogScanError(ScannerName, filePath,
                    new Exception($"Scan timeout after {SCAN_TIMEOUT_MS / 1000} seconds"));
                Utils.OutputPaneWriter.WriteError($"{ScannerName}: Scan timeout after {SCAN_TIMEOUT_MS / 1000}s");
                return null;
            }
            catch (Exception ex)
            {
                Utils.ScanMetricsLogger.LogScanError(ScannerName, filePath, ex);
                throw;
            }
        }

        /// <summary>
        /// Validates that a file does not exceed maximum size limit.
        /// Prevents CLI timeouts and out-of-memory issues on large files.
        /// </summary>
        private bool ValidateFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                {
                    Debug.WriteLine($"{ScannerName}: File {filePath} exceeds max size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB ({fileInfo.Length / (1024 * 1024)}MB)");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName}: Error validating file size: {ex.Message}");
                return true; // Allow scan if we can't determine size
            }
        }

        protected BaseRealtimeScannerService(ast_visual_studio_extension.CxCLI.CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
            _debounceTimer = new Timer(DEBOUNCE_DELAY);
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false;
        }

        /// <summary>
        /// Initializes the scanner: registers text-change events and writes startup message.
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            if (_isInitialized) return;
            try
            {
                _isInitialized = true;
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RegisterTextChangeEvents();
                Debug.WriteLine($"{ScannerName} scanner initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize {ScannerName} scanner: {ex.Message}");
                _isInitialized = false;
                throw;
            }
        }

        /// <summary>
        /// Tears down the scanner: unregisters event listeners and clears UI.
        /// </summary>
        public virtual async Task UnregisterAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (!_isSubscribed || _textEditorEvents == null) return;

                // Unsubscribe from text changes
                _textEditorEvents.LineChanged -= OnTextChanged;
                _textEditorEvents = null;

                // Unsubscribe from document lifecycle
                var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
                if (dte != null)
                {
                    var documentEvents = dte.Events.DocumentEvents;
                    documentEvents.DocumentOpened -= OnDocumentOpened;
                    documentEvents.DocumentClosing -= OnDocumentClosing;
                }

                _isSubscribed = false;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unregistering {ScannerName} events: {ex.Message}");
                throw;
            }
        }

        private void RegisterTextChangeEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_isSubscribed) return;

            var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte != null && _textEditorEvents == null)
            {
                _textEditorEvents = dte.Events.TextEditorEvents;
                _textEditorEvents.LineChanged += OnTextChanged;

                // Register document open/close handlers for instant scan on file open
                var documentEvents = dte.Events.DocumentEvents;
                documentEvents.DocumentOpened += OnDocumentOpened;
                documentEvents.DocumentClosing += OnDocumentClosing;

                _isSubscribed = true;

                var document = dte.ActiveDocument;
                if (document == null) return;
                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument == null) return;
                _lastDocumentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);

                Debug.WriteLine($"{ScannerName}: Successfully registered for text change and document lifecycle events.");
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
                if (_lastDocumentContent == currentContent) return;

                _lastDocumentContent = currentContent;
                _currentDocumentVersion++; // Increment version on each edit (for result freshness tracking)
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName} - Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that a result is fresh (not stale from a previous scan).
        /// Checks if result timestamp is newer than last displayed result,
        /// and if document version matches (no edits since scan started).
        /// </summary>
        protected bool IsResultFresh(DateTime resultTimestamp, int scanDocumentVersion)
        {
            // Check 1: Result timestamp should be newer than last displayed result
            if (resultTimestamp <= _lastResultTimestamp)
            {
                Debug.WriteLine($"{ScannerName}: Discarding stale result (timestamp {resultTimestamp} <= {_lastResultTimestamp})");
                return false;
            }

            // Check 2: Document version should match (no edits since scan started)
            if (scanDocumentVersion != _currentDocumentVersion)
            {
                Debug.WriteLine($"{ScannerName}: Discarding result (document edited: version {scanDocumentVersion} != {_currentDocumentVersion})");
                return false;
            }

            // Result is fresh - update last timestamp
            _lastResultTimestamp = resultTimestamp;
            return true;
        }

        /// <summary>
        /// Called when a document is opened in the editor.
        /// Triggers an instant scan (no debounce) if the file matches this scanner.
        /// </summary>
        private void OnDocumentOpened(Document document)
        {
            if (!_isSubscribed || document == null) return;

            try
            {
                // Check if this file should be scanned
                if (!ShouldScanFile(document.FullName))
                    return;

                Debug.WriteLine($"{ScannerName}: File opened: {document.Name}, triggering instant scan");
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () => await InstantScanAsync(document));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName}: Error in OnDocumentOpened: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when a document is about to be closed.
        /// Cancels any pending debounce timers and cleanup.
        /// </summary>
        private void OnDocumentClosing(Document document)
        {
            if (!_isSubscribed || document == null) return;

            try
            {
                Debug.WriteLine($"{ScannerName}: File closing: {document.Name}");
                // Cancel pending debounce for this file
                _debounceTimer.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName}: Error in OnDocumentClosing: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs an instant scan without debounce delay.
        /// Called when a document is opened.
        /// </summary>
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

                var originalFileName = Path.GetFileName(document.FullName);
                var tempFilePath = CreateTempFilePath(originalFileName, content);

                var tempDir = Path.GetDirectoryName(tempFilePath);
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                File.WriteAllText(tempFilePath, content);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int issueCount = await ScanAndDisplayAsync(tempFilePath, document);
                stopwatch.Stop();

                Utils.ScanMetricsLogger.LogScanComplete(ScannerName, document.FullName, issueCount, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Utils.ScanMetricsLogger.LogScanError(ScannerName, document.FullName, ex);
            }
        }

        private async void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _debounceTimer.Stop();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string tempFilePath = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
                var document = dte?.ActiveDocument;
                if (document == null) return;

                // Skip AI assistant temp files (Copilot, etc.)
                if (AiAgentFilePaths.Any(p => document.FullName.Contains(p)))
                {
                    Utils.ScanMetricsLogger.LogScanSkipped(ScannerName, document.FullName, "AI assistant temp file");
                    return;
                }

                // Check for /node_modules/ exclusion (applies to all scanners)
                if (document.FullName.Contains("\\node_modules\\") || document.FullName.Contains("/node_modules/"))
                {
                    Utils.ScanMetricsLogger.LogScanSkipped(ScannerName, document.FullName, "in node_modules directory");
                    return;
                }

                if (!ShouldScanFile(document.FullName))
                {
                    Utils.ScanMetricsLogger.LogScanSkipped(ScannerName, document.FullName, "file type not applicable");
                    return;
                }

                Utils.ScanMetricsLogger.LogScanStart(ScannerName, document.FullName);

                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument?.StartPoint == null || textDocument?.EndPoint == null) return;

                var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);

                // Skip empty files (no need to scan blank content)
                if (string.IsNullOrWhiteSpace(content))
                {
                    Utils.ScanMetricsLogger.LogScanSkipped(ScannerName, document.FullName, "file content is empty");
                    return;
                }

                var originalFileName = Path.GetFileName(document.FullName);

                // Create temp file using scanner-specific strategy (can be overridden by subclasses)
                tempFilePath = CreateTempFilePath(originalFileName, content);

                // Ensure directory exists if CreateTempFilePath created a directory-based path
                var tempDir = Path.GetDirectoryName(tempFilePath);
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                File.WriteAllText(tempFilePath, content);
                var scanResult = await ScanAndDisplayAsync(tempFilePath, document);
                stopwatch.Stop();
                Utils.ScanMetricsLogger.LogScanComplete(ScannerName, document.FullName, scanResult, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Utils.ScanMetricsLogger.LogScanError(ScannerName, tempFilePath, ex);
            }
            finally
            {
                if (tempFilePath != null)
                {
                    try
                    {
                        var tempDir = Path.GetDirectoryName(tempFilePath);
                        // Check if the temp file is in a scanner-specific subdirectory (not directly in %TEMP%)
                        var isSubDir = !string.Equals(
                            tempDir?.TrimEnd(Path.DirectorySeparatorChar),
                            Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
                            StringComparison.OrdinalIgnoreCase);

                        if (isSubDir && Directory.Exists(tempDir))
                        {
                            // Directory-based scanners (Secrets, IaC, Containers, OSS): delete the whole temp directory
                            Utils.TempFileManager.DeleteTempDirectory(tempDir);
                            Utils.ScanMetricsLogger.LogTempFileOperation("delete-dir", tempDir, true);
                        }
                        else if (File.Exists(tempFilePath))
                        {
                            // Flat-file scanners (ASCA): delete just the file
                            File.Delete(tempFilePath);
                            Utils.ScanMetricsLogger.LogTempFileOperation("delete", tempFilePath, true);
                        }
                    }
                    catch (Exception)
                    {
                        Utils.ScanMetricsLogger.LogTempFileOperation("delete", tempFilePath, false);
                    }
                }
            }
        }
    }
}
