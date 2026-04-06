using ast_visual_studio_extension.CxExtension.Utils;
using System;
using System.Diagnostics;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Logs scan metrics for telemetry and performance monitoring.
    /// Tracks scan duration, issue count, and success/failure rates.
    /// All methods are compiled out in Release builds using [Conditional("DEBUG")].
    /// </summary>
    public static class ScanMetricsLogger
    {
        private const string LOG_PREFIX = "[CxRealtimeScan]";

        /// <summary>
        /// Logs scan start event.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogScanStart(string scannerName, string filePath)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan started: {GetFileName(filePath)}");
        }

        /// <summary>
        /// Logs scan completion with metrics.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogScanComplete(string scannerName, string filePath, int issueCount, long elapsedMilliseconds)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            var duration = elapsedMilliseconds > 0 ? $"{elapsedMilliseconds}ms" : "unknown";
            var message = $"{scannerName} scan completed: {GetFileName(filePath)} | Issues: {issueCount} | Duration: {duration}";
            Debug.WriteLine($"{LOG_PREFIX} {message}");

            // Write to output pane if there are issues found
            if (issueCount > 0)
            {
                OutputPaneWriter.WriteTrace($"{scannerName}: {issueCount} issue(s) found in {GetFileName(filePath)}");
            }
        }

        /// <summary>
        /// Logs scan skipped event (e.g., file type not applicable to scanner).
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogScanSkipped(string scannerName, string filePath, string reason)
        {
            if (string.IsNullOrEmpty(scannerName) || string.IsNullOrEmpty(filePath))
                return;

            Debug.WriteLine($"{LOG_PREFIX} {scannerName} scan skipped: {GetFileName(filePath)} | Reason: {reason}");
        }

        /// <summary>
        /// Logs scan error event.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogScanError(string scannerName, string filePath, Exception ex)
        {
            if (string.IsNullOrEmpty(scannerName))
                return;

            var fileName = !string.IsNullOrEmpty(filePath) ? GetFileName(filePath) : "unknown";
            var message = $"{scannerName} scan error: {fileName} | Exception: {ex.Message}";
            Debug.WriteLine($"{LOG_PREFIX} {message}");

            // Write errors to output pane (not conditional on DEBUG)
            OutputPaneWriter.WriteError($"{scannerName}: {message}");
        }

        /// <summary>
        /// Logs file filtering decision.
        /// </summary>
        [Conditional("DEBUG")]
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
        [Conditional("DEBUG")]
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
        [Conditional("DEBUG")]
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
        [Conditional("DEBUG")]
        public static void LogResultMapping(string scannerName, int sourceCount, int mappedCount, long elapsedMilliseconds)
        {
            Debug.WriteLine($"{LOG_PREFIX} {scannerName} mapping: {sourceCount} source items → {mappedCount} Result objects | {elapsedMilliseconds}ms");
        }

        /// <summary>
        /// Logs orchestrator event.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogOrchestratorEvent(string eventName, string details = null)
        {
            var message = !string.IsNullOrEmpty(details) ? $" | {details}" : "";
            Debug.WriteLine($"{LOG_PREFIX} Orchestrator: {eventName}{message}");

            // Write important orchestrator events to output pane
            if (eventName == "ManifestFileChanged" || eventName == "InitializeAsync")
            {
                OutputPaneWriter.WriteTrace($"Orchestrator: {eventName}{message}");
            }
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
