using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Shows Visual Studio status bar progress message during realtime scans.
    /// Displays: "Checkmarx is Scanning File : filename.ext" with single-run progress bar.
    /// Progress bar fills from 0-100% once per scan, then clears.
    /// </summary>
    internal static class RealtimeScanProgressIndicator
    {
        private static readonly object ProgressLock = new object();
        private static int _depth;
        private static uint _progressCookie;
        private static string _currentFileName = string.Empty;
        private static System.Timers.Timer _progressTimer;
        private static uint _currentProgress = 0;

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
                    // Show progress bar that fills from 0-100% once during the scan.
                    // Progress fills at ~200ms per 10%, completing in ~2 seconds.
                    string label = $"Checkmarx is Scanning File : {fileName}";
                    _currentProgress = 0;
                    StartProgressBar(label);
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

                try
                {
                    if (_depth == 0)
                    {
                        // Stop progress bar and clear status bar
                        StopProgressBar();

                        var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                        if (statusBar != null)
                        {
                            statusBar.Progress(ref _progressCookie, 0, string.Empty, 0, 0);
                        }
                        TrySetTextFallback(string.Empty);
                        ResetProgress();
                    }
                    else
                    {
                        // More scans pending - show current file
                        string label = $"Checkmarx is Scanning File : {_currentFileName}";
                        TrySetTextFallback(label);
                    }
                }
                catch
                {
                    if (_depth == 0)
                    {
                        StopProgressBar();
                        TrySetTextFallback(string.Empty);
                        ResetProgress();
                    }
                }
            }
        }

        /// <summary>
        /// Resets progress state when all scans complete.
        /// _progressCookie must be reset to 0 so VS allocates a fresh one on the next PushScan.
        /// Reusing a cookie that VS has already closed causes the bar to silently disappear.
        /// </summary>
        private static void ResetProgress()
        {
            _progressCookie = 0;
            _currentFileName = string.Empty;
        }

        /// <summary>
        /// Starts progress bar that fills 0-100% once during scan.
        /// Updates every 200ms with +10% increment = ~2 second fill time.
        /// </summary>
        private static void StartProgressBar(string label)
        {
            StopProgressBar();

            _currentProgress = 0;
            _progressTimer = new System.Timers.Timer(200);
            _progressTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    lock (ProgressLock)
                    {
                        _currentProgress += 10;
                        if (_currentProgress > 100)
                            _currentProgress = 100;

                        var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                        if (statusBar != null)
                        {
                            statusBar.Progress(ref _progressCookie, 1, label, _currentProgress, 100);
                        }

                        // Stop timer once progress reaches 100%
                        if (_currentProgress >= 100)
                        {
                            StopProgressBar();
                        }
                    }
                }
                catch
                {
                    // Ignore timer errors
                }
            };
            _progressTimer.AutoReset = true;
            _progressTimer.Start();
        }

        /// <summary>
        /// Stops the progress bar timer.
        /// </summary>
        private static void StopProgressBar()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer.Dispose();
                _progressTimer = null;
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
