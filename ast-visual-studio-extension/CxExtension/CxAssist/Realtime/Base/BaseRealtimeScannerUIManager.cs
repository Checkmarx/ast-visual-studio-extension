using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Base
{
    /// <summary>
    /// Abstract base class for all realtime scanner UI managers.
    /// Provides shared logic for VS UI integration: markers (IVsTextLineMarker), error list, output pane.
    /// </summary>
    public abstract class BaseRealtimeScannerUIManager
    {
        protected readonly DTE2 _dte;
        protected OutputWindowPane _outputPane;
        protected ErrorListProvider _errorListProvider;
        protected List<IVsTextLineMarker> _activeMarkers = new List<IVsTextLineMarker>();

        protected BaseRealtimeScannerUIManager()
        {
            _dte = (DTE2)Package.GetGlobalService(typeof(SDTE));
            _errorListProvider = new ErrorListProvider(new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte));
        }

        /// <summary>
        /// Gets the active DTE instance for VS automation.
        /// </summary>
        public DTE2 GetDTE() => _dte;

        /// <summary>
        /// Gets the currently active document in VS editor.
        /// </summary>
        public Document GetActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _dte?.ActiveDocument;
        }

        /// <summary>
        /// Gets the active text buffer for the current document.
        /// </summary>
        public IVsTextLines GetActiveBuffer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            if (textManager == null) return null;

            textManager.GetActiveView(1, null, out IVsTextView textView);
            if (textView == null) return null;

            textView.GetBuffer(out IVsTextLines buffer);
            return buffer;
        }

        /// <summary>
        /// Writes a message to the Checkmarx output pane.
        /// </summary>
        public void WriteToOutputPane(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_outputPane == null)
                {
                    var outputWindow = (OutputWindow)_dte.Windows.Item(EnvDTE.Constants.vsWindowTypeOutput).Object;
                    _outputPane = outputWindow.OutputWindowPanes.Item("Checkmarx");
                }
                _outputPane.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to output pane: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a line marker to the document at the specified line/range.
        /// </summary>
        protected void AddMarker(IVsTextLines buffer, int line, int startIndex, int endIndex,
                                IVsTextMarkerClient markerClient, string severity)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (buffer == null) return;

            try
            {
                var markerType = GetMarkerType(severity);
                var textSpan = new TextSpan { iStartLine = line, iStartIndex = startIndex, iEndLine = line, iEndIndex = endIndex };

                buffer.CreateLineMarker(
                    (int)markerType,
                    textSpan.iStartLine,
                    textSpan.iStartIndex,
                    textSpan.iEndLine,
                    textSpan.iEndIndex,
                    markerClient,
                    out IVsTextLineMarker marker
                );

                if (marker != null)
                {
                    _activeMarkers.Add(marker);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding marker: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps severity to VS marker type for visual styling.
        /// </summary>
        protected MARKERTYPE GetMarkerType(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return MARKERTYPE.MARKER_OTHER_ERROR;

            var sev = severity.ToLowerInvariant();
            return sev switch
            {
                "critical" or "high" => MARKERTYPE.MARKER_CODESENSE_ERROR,
                "medium" => MARKERTYPE.MARKER_COMPILE_ERROR,
                _ => MARKERTYPE.MARKER_OTHER_ERROR
            };
        }

        /// <summary>
        /// Maps severity to VS error list category.
        /// </summary>
        protected TaskErrorCategory GetErrorCategory(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return TaskErrorCategory.Message;

            var sev = severity.ToLowerInvariant();
            return sev switch
            {
                "critical" or "high" => TaskErrorCategory.Error,
                "medium" => TaskErrorCategory.Warning,
                _ => TaskErrorCategory.Message
            };
        }

        /// <summary>
        /// Clears all active markers from the document.
        /// </summary>
        public void ClearAllMarkers()
        {
            foreach (var marker in _activeMarkers)
            {
                try
                {
                    marker?.Invalidate();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error invalidating marker: {ex.Message}");
                }
            }
            _activeMarkers.Clear();
        }

        /// <summary>
        /// Clears all markers and error list tasks asynchronously.
        /// </summary>
        public async Task ClearAllMarkersAndTasksAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ClearAllMarkers();
            try
            {
                _errorListProvider.Tasks.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing error list: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the IVsHierarchy for the given document (for error list association).
        /// </summary>
        protected IVsHierarchy GetHierarchyItem(Document document)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                if (solution == null) return null;

                solution.GetProjectOfUniqueName(document.ProjectItem?.ContainingProject?.UniqueName ?? "", out IVsHierarchy hierarchy);
                return hierarchy;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting hierarchy: {ex.Message}");
                return null;
            }
        }
    }
}
