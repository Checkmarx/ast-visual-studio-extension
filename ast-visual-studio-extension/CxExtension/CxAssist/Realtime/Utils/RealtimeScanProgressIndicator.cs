using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Shows Visual Studio status bar progress with animated progress bar.
    /// Displays: "Checkmarx is Scanning File : filename.ext" with animated bar underneath
    /// </summary>
    internal static class RealtimeScanProgressIndicator
    {
        private static readonly object ProgressLock = new object();
        private static int _depth;
        private static uint _progressCookie;
        private static string _currentFileName = string.Empty;

        internal static async Task PushScanAsync(string scannerName, string sourceFilePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string fileName = string.IsNullOrEmpty(sourceFilePath) ? "?" : Path.GetFileName(sourceFilePath);

            lock (ProgressLock)
            {
                _depth++;
                _currentFileName = fileName;

                var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                if (statusBar == null)
                {
                    TrySetTextFallback($"Checkmarx is Scanning File : {fileName}");
                    return;
                }

                try
                {
                    // Show animated progress bar with label in same call
                    // This prevents the gap between text and bar
                    string label = $"Checkmarx is Scanning File : {fileName}";
                    statusBar.Progress(ref _progressCookie, 1, label, 1, 1);
                }
                catch
                {
                    TrySetTextFallback($"Checkmarx is Scanning File : {fileName}");
                }
            }
        }

        internal static async Task PopScanAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            lock (ProgressLock)
            {
                if (_depth > 0)
                    _depth--;

                var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                if (statusBar == null)
                {
                    if (_depth == 0)
                    {
                        TrySetTextFallback(string.Empty);
                        ResetProgress();
                    }
                    else
                    {
                        TrySetTextFallback($"Checkmarx is Scanning File : {_currentFileName}");
                    }
                    return;
                }

                try
                {
                    if (_depth == 0)
                    {
                        // Clear the progress bar when all scans complete
                        statusBar.Progress(ref _progressCookie, 0, string.Empty, 0, 0);
                        ResetProgress();
                    }
                    else
                    {
                        // Show progress for next scan
                        string label = $"Checkmarx is Scanning File : {_currentFileName}";
                        statusBar.Progress(ref _progressCookie, 1, label, 1, 1);
                    }
                }
                catch
                {
                    if (_depth == 0)
                    {
                        TrySetTextFallback(string.Empty);
                        ResetProgress();
                    }
                }
            }
        }

        /// <summary>
        /// Resets progress state when all scans complete.
        /// </summary>
        private static void ResetProgress()
        {
            _currentFileName = string.Empty;
        }

        private static void TrySetTextFallback(string message)
        {
            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.StatusBar == null)
                    return;
                dte.StatusBar.Text = message ?? string.Empty;
            }
            catch
            {
                // ignore
            }
        }
    }
}
