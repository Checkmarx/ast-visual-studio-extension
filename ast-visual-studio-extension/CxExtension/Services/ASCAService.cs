using ast_visual_studio_extension.CxWrapper.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.IO;



using Microsoft.VisualStudio.TextManager.Interop;
using MARKERTYPE = Microsoft.VisualStudio.TextManager.Interop.MARKERTYPE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace ast_visual_studio_extension.CxExtension.Services
{
    public class ASCAService
    {
        private readonly System.Timers.Timer _debounceTimer;

        private readonly CxCLI.CxWrapper cxWrapper;
        private const int DEBOUNCE_DELAY = 2000; // Delay of 2 seconds
        private bool _isSubscribed = false;
        private bool _isInitialized = false;

        private readonly DTE2 _dte;
        private string _lastDocumentContent = string.Empty;
        private OutputWindowPane _outputPane;
        private static ASCAService _instance;
        private static readonly object _lock = new object();
        private TextEditorEvents _textEditorEvents;
        private ErrorListProvider _errorListProvider;
        private List<IVsTextLineMarker> _activeMarkers = new List<IVsTextLineMarker>();



        private ASCAService(CxCLI.CxWrapper cxWrapper)
        {
            this.cxWrapper = cxWrapper;
            _debounceTimer = new System.Timers.Timer(DEBOUNCE_DELAY);
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false;

            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            InitializeOutputPane();
        }

        public static ASCAService GetInstance(CxCLI.CxWrapper cxWrapper)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ASCAService(cxWrapper);
                    }
                }
            }
            return _instance;
        }

        private void InitializeOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outputWindow = _dte.ToolWindows.OutputWindow;
            _outputPane = outputWindow.OutputWindowPanes
                .Cast<OutputWindowPane>()
                .FirstOrDefault(p => p.Name == "Checkmarx") ?? outputWindow.OutputWindowPanes.Add("Checkmarx");
            _outputPane.Activate();
        }

        private void WriteToOutputPane(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputPane?.OutputString($"{DateTime.Now}: {message}\n");
        }

        private async void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
        {

            _debounceTimer.Stop();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var document = GetActiveDocument();
                if (document != null)
                {

                    // Retrieve document content
                    var textDocument = (TextDocument)document.Object("TextDocument");
                    var content = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);

                    // Use the same file name but save it in the Temp directory
                    var originalFileName = Path.GetFileName(document.FullName);
                    var tempFilePath = Path.Combine(Path.GetTempPath(), originalFileName);
                    File.WriteAllText(tempFilePath, content);
                    Debug.WriteLine($"Temporary file created: {tempFilePath}");

                    try
                    {
                        WriteToOutputPane($"Start ASCA scan On File: {document.FullName}");

                        // Perform ASCA scan
                        CxAsca scanResult = await cxWrapper.ScanAscaAsync(
                            fileSource: tempFilePath,
                            ascaLatestVersion: false,
                            agent: "Visual Studio"
                        );

                        if (scanResult.Error != null)
                        {
                            string errorMessage = $"ASCA Warning: {scanResult.Error.Description ?? scanResult.Error.ToString()}";
                            WriteToOutputPane(errorMessage);
                            return;
                        }

                        Debug.WriteLine("ASCA scan completed successfully.");
                        await DisplayDiagnosticsAsync(scanResult.ScanDetails, document.FullName);
                    }
                    finally
                    {
                        // Delete the temporary file
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                            Debug.WriteLine($"Temporary file deleted: {tempFilePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to process document: {ex.Message}");
            }
        }

        private void ClearAllMarkers()
        {
            foreach (var marker in _activeMarkers)
            {
                try
                {
                    marker.Invalidate(); // Removes the marker
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to invalidate marker: {ex.Message}");
                }
            }

            _activeMarkers.Clear(); // Clear the list to avoid references to deleted markers
        }


        private async Task DisplayDiagnosticsAsync(List<CxAscaDetail> scanDetails, string filePath)
        {
            if (scanDetails == null || string.IsNullOrEmpty(filePath)) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var document = _dte.Documents.Cast<Document>()
                .FirstOrDefault(doc => doc.FullName == filePath);

            if (document == null) return;

            // Create if not exists
            if (_errorListProvider == null)
            {
                _errorListProvider = new ErrorListProvider(ServiceProvider.GlobalProvider);
            }

            // Clear existing tasks in Error List and remove existing markers
            _errorListProvider.Tasks.Clear();
            ClearAllMarkers();

            var textManager = ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager)) as IVsTextManager2;
            if (textManager == null) return;

            IVsTextView viewEx = null;
            IVsTextLines buffer = null;

            try
            {
                int hr = textManager.GetActiveView2(1, null, 0, out viewEx);
                if (ErrorHandler.Failed(hr) || viewEx == null) return;

                hr = viewEx.GetBuffer(out buffer);
                if (ErrorHandler.Failed(hr) || buffer == null) return;

                WriteToOutputPane($"{scanDetails.Count} security best practice violations were found in {document.FullName}");


                foreach (var detail in scanDetails)
                {
                    // Add to Error List
                    var task = new ErrorTask
                    {
                        Category = TaskCategory.CodeSense,
                        ErrorCategory = GetErrorCategory(detail.Severity),
                        Text = $"{detail.RuleName} - {detail.RemediationAdvise} (ASCA)",
                        Document = document.FullName,
                        Line = detail.Line - 1,
                        Column = 0,
                        HierarchyItem = GetHierarchyItem(document)
                    };

                    task.Navigate += (s, e) =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        var textDocument = (TextDocument)document.Object("TextDocument");
                        var selection = textDocument.Selection;
                        selection.MoveToLineAndOffset(detail.Line, 1);
                        selection.SelectLine();
                    };

                    _errorListProvider.Tasks.Add(task);

                    // Create visual marker
                    try
                    {

                        // Find the problematic text position
                        string problemTextValue = detail.ProblematicLine;
                        int startIndex = problemTextValue.Length - problemTextValue.TrimStart().Length;
                        if (startIndex == -1)
                        {
                            startIndex = 0;
                        }
                        int endIndex = startIndex + problemTextValue.Length;

                        IVsTextLineMarker[] markers = new IVsTextLineMarker[1];
                        var errorSpan = new TextSpan
                        {
                            iStartLine = detail.Line - 1,
                            iStartIndex = startIndex,
                            iEndLine = detail.Line - 1,
                            iEndIndex = endIndex
                        };

                        var markerClient = new VsTextMarkerClient(detail.RuleName, detail.RemediationAdvise, detail.Severity);
                        hr = buffer.CreateLineMarker(
                            (int)GetMarkerType(detail.Severity),
                            errorSpan.iStartLine,
                            errorSpan.iStartIndex,
                            errorSpan.iEndLine,
                            errorSpan.iEndIndex,
                            markerClient,
                            markers
                        );

                        if (ErrorHandler.Succeeded(hr) && markers[0] != null)
                        {
                            _activeMarkers.Add(markers[0]);
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to create marker on line {detail.Line}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create marker on line {detail.Line}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to setup text view: {ex.Message}");
            }

        }

        private class VsTextMarkerClient : IVsTextMarkerClient
        {
            private readonly string _ruleName;
            private readonly string _remediation;

            public VsTextMarkerClient(string ruleName, string remediation, string severity)
            {
                _ruleName = ruleName;
                _remediation = remediation;
            }

            public void MarkerInvalidated()
            {
            }


            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                pbstrText[0] = $"{_ruleName} - {_remediation}\t(ASCA)";
                return VSConstants.S_OK;
            }


            public void OnBufferSave(string pszFileName)
            {
            }

            public void OnBeforeBufferClose()
            {
            }

            public int GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf)
            {
                if (pbstrText != null && pbstrText.Length > 0)
                    pbstrText[0] = string.Empty;
                if (pcmdf != null && pcmdf.Length > 0)
                    pcmdf[0] = 0;
                return VSConstants.E_NOTIMPL;
            }

            public int ExecMarkerCommand(IVsTextMarker pMarker, int iItem)
            {
                return VSConstants.E_NOTIMPL;
            }

            public void OnAfterSpanReload()
            {
            }

            public int OnAfterMarkerChange(IVsTextMarker pMarker)
            {
                return VSConstants.S_OK;
            }
        }

        private IVsHierarchy GetHierarchyItem(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (document?.ProjectItem?.ContainingProject == null)
                return null;

            var serviceProvider = ServiceProvider.GlobalProvider;
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            if (solution == null)
                return null;

            IVsHierarchy hierarchy;
            solution.GetProjectOfUniqueName(document.ProjectItem.ContainingProject.UniqueName, out hierarchy);

            return hierarchy;
        }

        private MARKERTYPE GetMarkerType(string severity)
        {
            switch (severity.ToLower())
            {
                case "critical":
                case "high":
                    return MARKERTYPE.MARKER_CODESENSE_ERROR;
                case "medium":
                    return MARKERTYPE.MARKER_COMPILE_ERROR;
                case "low":
                case "info":
                    return MARKERTYPE.MARKER_OTHER_ERROR;
                default:
                    return MARKERTYPE.MARKER_OTHER_ERROR;
            }
        }


        private TaskErrorCategory GetErrorCategory(string severity)
        {
            switch (severity.ToLower())
            {
                case "critical":
                case "high":
                    return TaskErrorCategory.Error;
                case "medium":
                    return TaskErrorCategory.Warning;
                case "low":
                case "info":
                    return TaskErrorCategory.Message;
                default:
                    return TaskErrorCategory.Message;
            }
        }

        private Document GetActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _dte?.ActiveDocument;
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

                WriteToOutputPane("AI Secure Coding Assistant Engine started");
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

            if (_dte != null && _textEditorEvents == null)
            {
                _textEditorEvents = _dte.Events.TextEditorEvents;
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

            var document = GetActiveDocument();
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
                if (_errorListProvider?.Tasks != null)
                {
                    _errorListProvider.Tasks.Clear();
                }
                ClearAllMarkers();

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
            await cxWrapper.ScanAscaAsync(
                 fileSource: "",
                 ascaLatestVersion: true,
                 agent: "Visual Studio"
             );
        }

    }
}