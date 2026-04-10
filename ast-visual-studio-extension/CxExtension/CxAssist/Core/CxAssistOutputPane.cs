using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using ast_visual_studio_extension.CxExtension.Utils;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Writes CxAssist messages to the VS Output Window ("Checkmarx" pane).
    /// Same pattern as ASCAUIManager.WriteToOutputPane — main lifecycle messages only.
    /// </summary>
    internal static class CxAssistOutputPane
    {
        private static OutputWindowPane _outputPane;
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte != null)
                {
                    var outputWindow = dte.ToolWindows.OutputWindow;
                    _outputPane = OutputPaneUtils.InitializeOutputPane(outputWindow, CxConstants.EXTENSION_TITLE);
                }
                _initialized = true;
            }
            catch
            {
                // Output pane is best-effort; swallow initialization errors
            }
        }

        /// <summary>
        /// Writes a timestamped message to the Checkmarx Output Window pane.
        /// Safe to call from UI thread only (ThreadHelper.ThrowIfNotOnUIThread inside).
        /// </summary>
        public static void WriteToOutputPane(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                EnsureInitialized();
                _outputPane?.OutputString($"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Output pane write is best-effort
            }
        }
    }
}
