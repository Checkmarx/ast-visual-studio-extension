using System;
using System.Diagnostics;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Logs scan metrics for telemetry and performance monitoring.
    /// Tracks scan duration, issue count, and success/failure rates.
    /// </summary>
    public static class ScanMetricsLogger
    {
        private const string LOG_PREFIX = "[CxRealtimeScan]";

        /// <summary>
        /// Logs scan start event.
        /// </summary>
        public static void LogScanStart(string scannerName, string filePath)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan started: {GetFileName(filePath)}");
        }

        /// <summary>
        /// Logs scan completion with metrics.
        /// </summary>
        public static void LogScanComplete(string scannerName, string filePath, int issueCount, long elapsedMilliseconds)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            var duration = elapsedMilliseconds > 0 ? $"{elapsedMilliseconds}ms" : "unknown";
            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan completed: {GetFileName(filePath)} | Issues: {issueCount} | Duration: {duration}");
        }

        /// <summary>
        /// Logs scan skipped event (e.g., file type not applicable to scanner).
        /// </summary>
        public static void LogScanSkipped(string scannerName, string filePath, string reason)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan skipped: {GetFileName(filePath)} | Reason: {reason}");
        }

        /// <summary>
        /// Logs scan error event.
        /// </summary>
        public static void LogScanError(string scannerName, string filePath, Exception ex)
        {
            if (string.IsNullOrEmpty(scannerName))
                return;

            var fileName = !string.IsNullOrEmpty(filePath) ? GetFileName(filePath) : "unknown";
            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan error: {fileName} | Exception: {ex.Message}");
        }

        /// <summary>
        /// Logs file filtering decision.
        /// </summary>
        public static void LogFileFilterDecision(string scannerName, string filePath, bool shouldScan, string reason)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            var decision = shouldScan ? "included" : "excluded";
            Debug.WriteLine($"{LOG_PREFIX} {scannerName} filter: {GetFileName(filePath)} | {decision} | {reason}");
        }

        /// <summary>
        /// Logs companion file operation (e.g., lock file copy).
        /// </summary>
        public static void LogCompanionFileOperation(string operation, string fileName, bool success, string details = null)
        {
            if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(fileName))
                return;

            var result = success ? "success" : "failed";
            var message = !string.IsNullOrEmpty(details) ? $" | {details}" : "";
            Debug.WriteLine($"{LOG_PREFIX} Companion file {operation}: {fileName} | {result}{message}");
        }

        /// <summary>
        /// Logs temp file operation (create/delete).
        /// </summary>
        public static void LogTempFileOperation(string operation, string path, bool success)
        {
            if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(path))
                return;

            var result = success ? "success" : "failed";
            Debug.WriteLine($"{LOG_PREFIX} Temp file {operation}: {GetFileName(path)} | {result}");
        }

        /// <summary>
        /// Logs result mapping summary.
        /// </summary>
        public static void LogResultMapping(string scannerName, int sourceCount, int mappedCount, long elapsedMilliseconds)
        {
            Debug.WriteLine($"{LOG_PREFIX} {scannerName} mapping: {sourceCount} source items → {mappedCount} Result objects | {elapsedMilliseconds}ms");
        }

        /// <summary>
        /// Logs orchestrator event.
        /// </summary>
        public static void LogOrchestratorEvent(string eventName, string details = null)
        {
            var message = !string.IsNullOrEmpty(details) ? $" | {details}" : "";
            Debug.WriteLine($"{LOG_PREFIX} Orchestrator: {eventName}{message}");
        }

        /// <summary>
        /// Extracts filename from full path for readability.
        /// </summary>
        private static string GetFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "unknown";

            try
            {
                return System.IO.Path.GetFileName(filePath);
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
