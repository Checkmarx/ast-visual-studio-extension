using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Shows Visual Studio status bar progress (marquee) while a realtime CLI scan runs — similar in spirit to JetBrains scan progress.
    /// </summary>
    internal static class RealtimeScanProgressIndicator
    {
        private static readonly object ProgressLock = new object();
        private static int _depth;
        private static uint _progressCookie;

        internal static async Task PushScanAsync(string scannerName, string sourceFilePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string fileName = string.IsNullOrEmpty(sourceFilePath) ? "?" : Path.GetFileName(sourceFilePath);

            lock (ProgressLock)
            {
                _depth++;
                string label = _depth > 1
                    ? $"Checkmarx realtime: {_depth} scans…"
                    : $"Checkmarx {scannerName}: {fileName}…";

                var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                if (statusBar == null)
                {
                    TrySetTextFallback(label);
                    return;
                }

                try
                {
                    statusBar.Progress(ref _progressCookie, 1, label, 0, 0);
                }
                catch
                {
                    TrySetTextFallback(label);
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
                        TrySetTextFallback(string.Empty);
                    else
                        TrySetTextFallback($"Checkmarx realtime: {_depth} scans…");
                    return;
                }

                try
                {
                    if (_depth == 0)
                    {
                        statusBar.Progress(ref _progressCookie, 0, string.Empty, 0, 0);
                    }
                    else
                    {
                        string label = $"Checkmarx realtime: {_depth} scans…";
                        statusBar.Progress(ref _progressCookie, 1, label, 0, 0);
                    }
                }
                catch
                {
                    if (_depth == 0)
                        TrySetTextFallback(string.Empty);
                }
            }
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
