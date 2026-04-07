using log4net;
using System;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Structured log lines for realtime scan duration and outcome (log4net file + diagnostics).
    /// </summary>
    internal static class ScanMetricsLogger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScanMetricsLogger));

        internal static void LogRealtimeScanCompleted(string scannerName, string sourceFilePath, long elapsedMs, int issueCount)
        {
            try
            {
                string name = string.IsNullOrEmpty(sourceFilePath) ? "?" : Path.GetFileName(sourceFilePath);
                Log.Info($"RealtimeScan scanner={scannerName} file={name} ms={elapsedMs} issues={issueCount}");
            }
            catch
            {
                // Never break scanning
            }
        }
    }
}
