using ast_visual_studio_extension.CxExtension.Utils;
using log4net;
using System;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Structured log lines for realtime scan duration and outcome (log4net file + diagnostics).
    /// CWE-117: All logged values are sanitized to prevent log forging via newline injection.
    /// </summary>
    internal static class ScanMetricsLogger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScanMetricsLogger));

        internal static void LogRealtimeScanCompleted(string scannerName, string sourceFilePath, long elapsedMs, int issueCount)
        {
            try
            {
                string name = string.IsNullOrEmpty(sourceFilePath) ? "?" : Path.GetFileName(sourceFilePath);

                // CWE-117: Sanitize all user-controlled values to prevent log forging
                string sanitizedScannerName = LogForgingSanitizer.StripLineTermination(scannerName) ?? "Unknown";
                string sanitizedFileName = LogForgingSanitizer.StripLineTermination(name) ?? "?";

                Log.Info($"RealtimeScan scanner={sanitizedScannerName} file={sanitizedFileName} ms={elapsedMs} issues={issueCount}");
            }
            catch (ArgumentException)
            {
                // sourceFilePath contains invalid path characters (should not happen, but be defensive)
            }
            catch (Exception ex)
            {
                // Log unexpected errors without breaking scanning (e.g., log4net connection issues)
                System.Diagnostics.Debug.WriteLine($"ScanMetricsLogger: Failed to log metrics: {ex.Message}");
            }
        }
    }
}
