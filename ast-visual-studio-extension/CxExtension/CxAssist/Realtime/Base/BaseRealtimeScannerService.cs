using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Interfaces;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Threading.Tasks;

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
        private readonly Timer _debounceTimer;
        private const int DEBOUNCE_DELAY = 2000;
        private bool _isSubscribed = false;
        private bool _isInitialized = false;
        private string _lastDocumentContent = string.Empty;
        private TextEditorEvents _textEditorEvents;

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
        /// Performs the actual scan and returns the result count.
        /// Called after debounce timer fires and the temp file is written.
        /// Subclasses must implement the specific scanner invocation.
        /// Results are mapped to Vulnerability objects and passed to CxAssistDisplayCoordinator.
        /// </summary>
        protected abstract Task<int> ScanAndDisplayAsync(string tempFilePath, Document document);

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
                _textEditorEvents.LineChanged -= OnTextChanged;
                _textEditorEvents = null;
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
                _isSubscribed = true;

                var document = dte.ActiveDocument;
                if (document == null) return;
                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument == null) return;
                _lastDocumentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);

                Debug.WriteLine($"{ScannerName}: Successfully registered for text change events.");
            }
        }

        private void OnTextChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            if (!_isSubscribed) return;

            var document = _uiManager.GetActiveDocument();
            if (document == null) return;

            try
            {
                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument == null) return;

                var currentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                if (_lastDocumentContent == currentContent) return;

                _lastDocumentContent = currentContent;
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName} - Exception: {ex.Message}");
            }
        }

        private async void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _debounceTimer.Stop();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string tempFilePath = null;
            try
            {
                var dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
                var document = dte?.ActiveDocument;
                if (document == null) return;

                if (!ShouldScanFile(document.FullName)) return;

                var textDocument = (TextDocument)document.Object("TextDocument");
                if (textDocument?.StartPoint == null || textDocument?.EndPoint == null) return;

                var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                var originalFileName = Path.GetFileName(document.FullName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                var extension = Path.GetExtension(originalFileName);
                tempFilePath = Path.Combine(Path.GetTempPath(), $"{fileNameWithoutExt}_{timestamp}{extension}");

                File.WriteAllText(tempFilePath, content);
                Debug.WriteLine($"{ScannerName}: Starting scan on: {document.FullName}");

                await ScanAndDisplayAsync(tempFilePath, document);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ScannerName} - Failed to process document: {ex.Message}");
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Debug.WriteLine($"{ScannerName}: Temporary file deleted: {tempFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"{ScannerName}: Failed to delete temporary file: {ex.Message}");
                    }
                }
            }
        }
    }
}
