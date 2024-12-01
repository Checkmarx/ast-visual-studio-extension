using ast_visual_studio_extension.CxWrapper.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using ast_visual_studio_extension.CxCLI;

namespace ast_visual_studio_extension.CxExtension.Services
{
    public class ASCAService
    {
        private readonly CxCLI.CxWrapper _cxWrapper;
        private readonly ASCAUIManager _uiManager;
        private readonly System.Timers.Timer _debounceTimer;
        private const int DEBOUNCE_DELAY = 2000;
        private bool _isSubscribed = false;
        private bool _isInitialized = false;
        private string _lastDocumentContent = string.Empty;
        private TextEditorEvents _textEditorEvents;
        private static volatile ASCAService _instance;
        private static readonly object _lock = new object();

        private ASCAService(CxCLI.CxWrapper cxWrapper)
        {
            _cxWrapper = cxWrapper;
            _uiManager = new ASCAUIManager();
            _debounceTimer = new System.Timers.Timer(DEBOUNCE_DELAY);
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false;
        }

        public static ASCAService GetInstance(CxCLI.CxWrapper cxWrapper)
        {
            if (_instance != null) return _instance;
    
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new ASCAService(cxWrapper);
                }
            }
            return _instance;
        }
        private async void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _debounceTimer.Stop();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string tempFilePath = null;

            try
            {
                var document = _uiManager.GetActiveDocument();
                if (document != null)
                {
                    var textDocument = (TextDocument)document.Object("TextDocument");
                    var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                    if (textDocument?.StartPoint == null || textDocument?.EndPoint == null)
                    {
                        return; 
                    }
                    var originalFileName = Path.GetFileName(document.FullName);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    tempFilePath = Path.Combine(Path.GetTempPath(), $"{originalFileName}_{timestamp}.cs");

                    File.WriteAllText(tempFilePath, content);
                    Debug.WriteLine($"Temporary file created: {tempFilePath}");

                    _uiManager.WriteToOutputPane($"Start ASCA scan On File: {document.FullName}");

                    CxAsca scanResult = await _cxWrapper.ScanAscaAsync(
                        fileSource: tempFilePath,
                        ascaLatestVersion: false,
                        agent: CxConstants.EXTENSION_AGENT
                    );

                    if (scanResult.Error != null)
                    {
                        string errorMessage = $"ASCA Warning: {scanResult.Error.Description ?? scanResult.Error.ToString()}";
                        _uiManager.WriteToOutputPane(errorMessage);
                        return;
                    }

                    Debug.WriteLine("ASCA scan completed successfully.");
                    await _uiManager.DisplayDiagnosticsAsync(scanResult.ScanDetails, document.FullName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to process document: {ex.Message}");
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        Debug.WriteLine($"Temporary file deleted: {tempFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to delete temporary file: {ex.Message}");
                    }
                }
            }
        }

        public async Task InitializeASCAAsync()
        {
            if (_isInitialized)
            {
                Debug.WriteLine("ASCA Service is already initialized.");
                return;
            }

            try
            {
                _isInitialized = true;
                await InstallAscaAsync();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RegisterTextChangeEvents();

                _uiManager.WriteToOutputPane(CxConstants.ASCA_ENGINE_STARTED_MESSAGE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize ASCA: {ex.Message}");
                _isInitialized = false;
                throw;
            }
        }

        private void RegisterTextChangeEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_isSubscribed) return;

            var dte = _uiManager.GetDTE();
            if (dte != null && _textEditorEvents == null)
            {
                _textEditorEvents = dte.Events.TextEditorEvents;
                _textEditorEvents.LineChanged += OnTextChanged;
                _isSubscribed = true;
                Debug.WriteLine("Successfully registered for text change events.");
            }
            else
            {
                Debug.WriteLine("Failed to register text change events: DTE or Events not available.");
            }
        }
        private void OnTextChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            if (!_isSubscribed) return;

            var document = _uiManager.GetActiveDocument();
            if (document != null)
            {
                try
                {
                    var textDocument = (TextDocument)document.Object("TextDocument");
                    if (textDocument != null)
                    {
                        var currentContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                        if (_lastDocumentContent != currentContent)
                        {
                            _lastDocumentContent = currentContent;
                            _debounceTimer.Stop();
                            _debounceTimer.Start();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"COMException: {ex.Message}");
                }
            }
        }

        public async Task UnregisterTextChangeEventsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _uiManager.ClearAllMarkersAndTasksAsync();

                if (!_isSubscribed || _textEditorEvents == null)
                {
                    Debug.WriteLine("No active text change events subscription to unregister.");
                    return;
                }

                _textEditorEvents.LineChanged -= OnTextChanged;
                _textEditorEvents = null;
                _isSubscribed = false;
                _isInitialized = false;

                Debug.WriteLine("Successfully unregistered text change events.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unregistering events: {ex.Message}");
                throw;
            }
        }

        private async Task InstallAscaAsync()
        {
            await _cxWrapper.ScanAscaAsync(
                fileSource: "",
                ascaLatestVersion: true,
                agent: CxConstants.EXTENSION_AGENT
            );
        }
    }
}