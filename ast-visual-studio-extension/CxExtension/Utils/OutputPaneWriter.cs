using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    /// <summary>
    /// Provides a unified interface for writing log messages to the VS Output Window "Checkmarx" pane.
    /// Complements Debug.WriteLine() with visible output in the IDE.
    ///
    /// Usage:
    /// OutputPaneWriter.WriteLine("message");
    /// OutputPaneWriter.WriteError("error details");
    /// OutputPaneWriter.WriteWarning("warning message");
    /// </summary>
    public static class OutputPaneWriter
    {
        private static OutputWindowPane _checkmarxPane;
        private static readonly object _lockObject = new object();
        private const string PANE_NAME = "Checkmarx";
        private const string PANE_PREFIX = "[Checkmarx]";

        /// <summary>
        /// Writes an informational message to the Checkmarx output pane.
        /// Thread-safe and handles missing DTE gracefully.
        /// </summary>
        public static void WriteLine(string message)
        {
            WriteToPane($"{PANE_PREFIX} {message}");
        }

        /// <summary>
        /// Writes an error message to the Checkmarx output pane with ERROR prefix.
        /// </summary>
        public static void WriteError(string message)
        {
            WriteToPane($"{PANE_PREFIX} [ERROR] {message}");
        }

        /// <summary>
        /// Writes a warning message to the Checkmarx output pane with WARNING prefix.
        /// </summary>
        public static void WriteWarning(string message)
        {
            WriteToPane($"{PANE_PREFIX} [WARNING] {message}");
        }

        /// <summary>
        /// Writes a debug message to the Checkmarx output pane with DEBUG prefix.
        /// </summary>
        public static void WriteDebug(string message)
        {
            WriteToPane($"{PANE_PREFIX} [DEBUG] {message}");
        }

        /// <summary>
        /// Writes a message with timestamp for tracing scanner operations.
        /// </summary>
        public static void WriteTrace(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            WriteToPane($"{PANE_PREFIX} [{timestamp}] {message}");
        }

        /// <summary>
        /// Core method that writes to the output pane.
        /// Handles initialization and thread safety.
        /// </summary>
        private static void WriteToPane(string message)
        {
            try
            {
                // Also write to Debug output for consistency
                Debug.WriteLine(message);

                // Only try to write to output pane if we have UI thread access
                if (!ThreadHelper.CheckAccess())
                {
                    // Queue the write for the UI thread
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        WriteDirectlyToPane(message);
                    });
                    return;
                }

                WriteDirectlyToPane(message);
            }
            catch (Exception ex)
            {
                // Fail silently - don't let logging errors break the application
                Debug.WriteLine($"[OutputPaneWriter] Error writing to output pane: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes directly to the pane (requires UI thread).
        /// Must be called from UI thread via ThreadHelper.
        /// </summary>
        private static void WriteDirectlyToPane(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            lock (_lockObject)
            {
                try
                {
                    // Initialize pane if needed
                    if (_checkmarxPane == null)
                    {
                        var dte = (DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
                        if (dte?.ToolWindows?.OutputWindow == null)
                        {
                            Debug.WriteLine("[OutputPaneWriter] DTE or OutputWindow not available");
                            return;
                        }

                        _checkmarxPane = OutputPaneUtils.InitializeOutputPane(
                            dte.ToolWindows.OutputWindow,
                            PANE_NAME);

                        if (_checkmarxPane == null)
                        {
                            Debug.WriteLine("[OutputPaneWriter] Failed to initialize Checkmarx pane");
                            return;
                        }
                    }

                    // Write message with newline
                    _checkmarxPane.OutputString(message + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OutputPaneWriter] Exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears all content from the Checkmarx output pane.
        /// Called when extension shutting down or user requests clear.
        /// </summary>
        public static void Clear()
        {
            try
            {
                if (!ThreadHelper.CheckAccess())
                {
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ClearDirectly();
                    });
                    return;
                }

                ClearDirectly();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OutputPaneWriter] Error clearing pane: {ex.Message}");
            }
        }

        private static void ClearDirectly()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            lock (_lockObject)
            {
                try
                {
                    if (_checkmarxPane != null)
                    {
                        _checkmarxPane.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OutputPaneWriter] Exception clearing pane: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Activates (makes visible) the Checkmarx output pane.
        /// Called after writing important messages to bring pane to user's attention.
        /// </summary>
        public static void Activate()
        {
            try
            {
                if (!ThreadHelper.CheckAccess())
                {
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ActivateDirectly();
                    });
                    return;
                }

                ActivateDirectly();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OutputPaneWriter] Error activating pane: {ex.Message}");
            }
        }

        private static void ActivateDirectly()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            lock (_lockObject)
            {
                try
                {
                    if (_checkmarxPane != null)
                    {
                        _checkmarxPane.Activate();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OutputPaneWriter] Exception activating pane: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resets the pane reference (used when extension reinitializes).
        /// </summary>
        public static void Reset()
        {
            lock (_lockObject)
            {
                _checkmarxPane = null;
            }
        }
    }
}
