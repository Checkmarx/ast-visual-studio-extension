using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Telemetry service for AI Security Companion button-click events.
    /// Mirrors JetBrains TelemetryService — fire-and-forget, never throws.
    /// </summary>
    internal static class TelemetryService
    {
        private const string EventTypeClick = "click";
        private const string SubTypeFixWithAiChat = "fixWithAIChat";
        private const string SubTypeViewDetails = "viewDetails";
        private const string SubTypeIgnorePackage = "ignorePackage";
        private const string SubTypeIgnoreAll = "ignoreAll";

        internal static void LogFixWithCxOneAssist(Vulnerability v) =>
            LogClickEvent(SubTypeFixWithAiChat, v);

        internal static void LogViewDetails(Vulnerability v) =>
            LogClickEvent(SubTypeViewDetails, v);

        internal static void LogIgnoreThis(Vulnerability v) =>
            LogClickEvent(SubTypeIgnorePackage, v);

        internal static void LogIgnoreAll(Vulnerability v) =>
            LogClickEvent(SubTypeIgnoreAll, v);

        private static void LogClickEvent(string subType, Vulnerability v)
        {
            if (v == null) return;
            string engine = MapEngine(v.Scanner);
            string severity = NormalizeSeverity(v.Severity);
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var package = RealtimeScannerHost.Package;
                    if (package == null) return;
                    var wrapper = CxUtils.GetCxWrapper(package, typeof(TelemetryService));
                    if (wrapper == null) return;
                    await System.Threading.Tasks.Task.Run(() =>
                        wrapper.LogUserEventTelemetry(EventTypeClick, subType, engine, severity));
                }
                catch (Exception ex)
                {
                    CxAssistOutputPane.WriteToOutputPane($"TelemetryService: failed to log {subType} — {ex.Message}");
                }
            });
        }

        private static string MapEngine(ScannerType scanner)
        {
            switch (scanner)
            {
                case ScannerType.OSS: return "Oss";
                case ScannerType.Secrets: return "Secrets";
                case ScannerType.IaC: return "IaC";
                case ScannerType.ASCA: return "Asca";
                case ScannerType.Containers: return "Containers";
                default: return "Oss";
            }
        }

        private static string NormalizeSeverity(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Malicious: return "Malicious";
                case SeverityLevel.Critical: return "Critical";
                case SeverityLevel.High: return "High";
                case SeverityLevel.Medium: return "Medium";
                case SeverityLevel.Low: return "Low";
                default: return "Unknown";
            }
        }
    }
}
