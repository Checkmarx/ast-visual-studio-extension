using ast_visual_studio_extension.CxWrapper.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;
using MARKERTYPE = Microsoft.VisualStudio.TextManager.Interop.MARKERTYPE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using ast_visual_studio_extension.CxExtension.Utils;


namespace ast_visual_studio_extension.CxExtension.Services
{
    public class ASCAUIManager
    {
        private readonly DTE2 _dte;
        private OutputWindowPane _outputPane;
        private ErrorListProvider _errorListProvider;
        private List<IVsTextLineMarker> _activeMarkers = new List<IVsTextLineMarker>();

        public ASCAUIManager()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            InitializeOutputPane();
        }

        public DTE2 GetDTE()
        {
            return _dte;
        }

        private void InitializeOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outputWindow = _dte.ToolWindows.OutputWindow;
            _outputPane = OutputPaneUtils.InitializeOutputPane(outputWindow, CxConstants.EXTENSION_TITLE);
        }

        public Document GetActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _dte?.ActiveDocument;
        }

        public void WriteToOutputPane(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputPane?.OutputString($"{DateTime.Now}: {message}\n");
        }

        public async Task DisplayDiagnosticsAsync(List<CxAscaDetail> scanDetails, string filePath)
        {
            if (scanDetails == null || string.IsNullOrEmpty(filePath)) return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var document = _dte.Documents.Cast<Document>()
                .FirstOrDefault(doc => doc.FullName == filePath);

            if (document == null) return;

            if (_errorListProvider == null)
            {
                _errorListProvider = new ErrorListProvider(ServiceProvider.GlobalProvider);
            }

            _errorListProvider.Tasks.Clear();
            ClearAllMarkers();

            var textManager = ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager)) as IVsTextManager2;
            if (textManager == null) return;

            IVsTextView activeTextView = null;
            IVsTextLines buffer = null;

            try
            {
                // Get primary view (1), no buffer filter (null), reserved value (0)
                int hr = textManager.GetActiveView2(1, null, 0, out activeTextView);
                if (ErrorHandler.Failed(hr) || activeTextView == null) return;

                hr = activeTextView.GetBuffer(out buffer);
                if (ErrorHandler.Failed(hr) || buffer == null) return;

                WriteToOutputPane($"{scanDetails.Count} security best practice violations were found in {document.FullName}");

                foreach (var detail in scanDetails)
                {
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

                    try
                    {
                        string problemTextValue = detail.ProblematicLine;
                        int startIndex = problemTextValue.Length - problemTextValue.TrimStart().Length;
                        if (startIndex < 0)
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

        private void ClearAllMarkers()
        {
            foreach (var marker in _activeMarkers)
            {
                try
                {
                    marker.Invalidate();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to invalidate marker: {ex.Message}");
                }
            }

            _activeMarkers.Clear();
        }

        public async Task ClearAllMarkersAndTasksAsync()
        {
            if (_errorListProvider?.Tasks != null)
            {
                _errorListProvider.Tasks.Clear();
            }
            ClearAllMarkers();
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

        private class VsTextMarkerClient : IVsTextMarkerClient
        {
            private readonly string _ruleName;
            private readonly string _remediation;

            public VsTextMarkerClient(string ruleName, string remediation, string severity)
            {
                _ruleName = ruleName;
                _remediation = remediation;
            }

            /// <summary>
            /// Method called when the text marker becomes invalid. 
            /// This implementation is intentionally left empty as we don't need to perform any cleanup 
            /// </summary>
            public void MarkerInvalidated()
            {
            }

            public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
            {
                pbstrText[0] = $"{_ruleName} - {_remediation}\t(ASCA)";
                return VSConstants.S_OK;
            }

            /// <summary>
            /// Called when the buffer is saved. No action needed in our implementation.
            /// </summary>
            public void OnBufferSave(string pszFileName)
            {
            }

            /// <summary>
            /// Called before the buffer is closed. No action needed in our implementation.
            /// </summary>
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

            /// <summary>
            /// Called after the span is reloaded. No action needed in our implementation.
            /// </summary>
            public void OnAfterSpanReload()
            {
            }

            public int OnAfterMarkerChange(IVsTextMarker pMarker)
            {
                return VSConstants.S_OK;
            }
        }
    }
}