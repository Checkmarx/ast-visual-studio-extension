using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Utility class for integrating with GitHub Copilot Chat in Visual Studio.
    ///
    /// <para>
    /// Since GitHub Copilot does not expose a public API for VS extensions, this
    /// implementation uses DTE commands with SendKeys as a non-blocking async chain:
    /// </para>
    ///
    /// <list type="number">
    ///   <item>Copy prompt to clipboard (safety fallback)</item>
    ///   <item>Open Copilot Chat via DTE command or keyboard shortcut</item>
    ///   <item>Start a new chat thread via DTE command (best effort)</item>
    ///   <item>Re-focus Copilot Chat, paste prompt from clipboard, submit via Enter</item>
    /// </list>
    ///
    /// <para>
    /// Each step is scheduled via <see cref="DispatcherTimer"/> at ApplicationIdle
    /// priority so the UI thread is never blocked (no Thread.Sleep). This ensures
    /// Copilot Chat can fully render between operations.
    /// </para>
    ///
    /// <para><b>Fallback Behavior:</b></para>
    /// <para>
    /// If automation fails at any stage, the prompt remains in the clipboard and
    /// the user is notified to paste manually.
    /// </para>
    /// </summary>
    internal static class CopilotIntegration
    {
        // ==================== Configuration Constants ====================

        /// <summary>
        /// Timing delays for UI automation. Tuned for typical VS response times.
        /// </summary>
        private static class Timing
        {
            /// <summary>Delay after opening Copilot to allow UI to fully render.</summary>
            public const int CopilotOpenDelayMs = 1200;

            /// <summary>Delay after starting a new thread for UI to settle.</summary>
            public const int NewThreadDelayMs = 500;

            /// <summary>Delay before paste/submit to ensure input field has focus.</summary>
            public const int PasteDelayMs = 400;

            /// <summary>Brief pause between paste and Enter to let VS process clipboard.</summary>
            public const int PasteSettleMs = 100;
        }

        // ==================== Command ID Constants ====================

        /// <summary>DTE command IDs for opening the Copilot Chat window.</summary>
        private static readonly string[] OpenChatCommands =
        {
            "GitHub.Copilot.Chat.Show",
            "View.CopilotChat",
            "View.GitHubCopilotChat",
            "Edit.Copilot.Open"
        };

        /// <summary>DTE command IDs for starting a new chat thread.</summary>
        private static readonly string[] NewThreadCommands =
        {
            "GitHub.Copilot.Chat.NewThread",
            "GitHub.Copilot.Chat.New",
            "GitHub.Copilot.Chat.ClearHistory"
        };

        // ==================== Result Types ====================

        /// <summary>
        /// Result of a Copilot integration operation.
        /// </summary>
        public enum OperationResult
        {
            /// <summary>Full automation succeeded — prompt was sent to Copilot.</summary>
            FullSuccess,

            /// <summary>Partial success — Copilot opened but automation may have issues.</summary>
            PartialSuccess,

            /// <summary>Copilot not available — prompt copied to clipboard only.</summary>
            CopilotNotAvailable,

            /// <summary>Operation failed completely.</summary>
            Failed
        }

        /// <summary>
        /// Detailed result with message for user feedback.
        /// </summary>
        public class IntegrationResult
        {
            public OperationResult Result { get; }
            public string Message { get; }
            public Exception Exception { get; }

            private IntegrationResult(OperationResult result, string message, Exception exception = null)
            {
                Result = result;
                Message = message;
                Exception = exception;
            }

            public bool IsSuccess =>
                Result == OperationResult.FullSuccess || Result == OperationResult.PartialSuccess;

            public static IntegrationResult FullSuccess(string msg) =>
                new IntegrationResult(OperationResult.FullSuccess, msg);

            public static IntegrationResult PartialSuccess(string msg) =>
                new IntegrationResult(OperationResult.PartialSuccess, msg);

            public static IntegrationResult CopilotNotAvailable(string msg) =>
                new IntegrationResult(OperationResult.CopilotNotAvailable, msg);

            public static IntegrationResult Fail(string msg, Exception ex = null) =>
                new IntegrationResult(OperationResult.Failed, msg, ex);
        }

        // ==================== Public API ====================

        /// <summary>
        /// Opens Copilot Chat, starts a new thread, pastes the prompt, and sends it.
        /// Returns true if the clipboard was set (even if full automation failed).
        /// Maintains backward compatibility with existing callers.
        /// </summary>
        /// <param name="prompt">The prompt to send to Copilot.</param>
        /// <param name="clipboardFallbackMessage">Message shown if only clipboard copy succeeded.</param>
        public static bool SendPromptToCopilot(string prompt, string clipboardFallbackMessage)
        {
            IntegrationResult result = SendPromptToCopilotDetailed(prompt, clipboardFallbackMessage);
            return result != null && result.Result != OperationResult.Failed;
        }

        /// <summary>
        /// Opens Copilot Chat with prompt and returns detailed result.
        /// </summary>
        /// <param name="prompt">The prompt to send to Copilot.</param>
        /// <param name="clipboardFallbackMessage">Message shown if only clipboard copy succeeded.</param>
        public static IntegrationResult SendPromptToCopilotDetailed(string prompt, string clipboardFallbackMessage)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return IntegrationResult.Fail("Prompt is empty");

            Log("Starting Copilot integration workflow");

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Step 1: Always copy to clipboard first (guaranteed fallback)
                if (!CopyToClipboard(prompt))
                {
                    Log("Failed to copy prompt to clipboard");
                    return IntegrationResult.Fail("Failed to copy prompt to clipboard");
                }
                Log("Prompt copied to clipboard");

                // Step 2: Pre-check if Copilot is available (aligned with JetBrains CopilotIntegration.isCopilotAvailable)
                if (!IsCopilotAvailable())
                {
                    Log("Copilot not available (pre-check), prompt copied to clipboard");
                    MessageBox.Show(
                        CxAssistConstants.CopilotOpenInstructionsMessage,
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return IntegrationResult.CopilotNotAvailable(
                        CxAssistConstants.CopilotOpenInstructionsMessage);
                }

                // Step 3: Open Copilot Chat
                bool opened = TryOpenCopilotChat();
                if (!opened)
                {
                    Log("Copilot Chat failed to open");
                    MessageBox.Show(
                        CxAssistConstants.CopilotOpenInstructionsMessage,
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return IntegrationResult.CopilotNotAvailable(
                        CxAssistConstants.CopilotOpenInstructionsMessage);
                }

                Log("Copilot Chat opened, scheduling automation sequence");

                // Step 3: Schedule the automation sequence after UI renders
                ScheduleAutomatedPromptEntry(prompt);

                return IntegrationResult.PartialSuccess(
                    "Copilot Chat opened, automation in progress...");
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.SendPromptToCopilot");
                try
                {
                    CopyToClipboard(prompt);
                    MessageBox.Show(
                        clipboardFallbackMessage ?? CxAssistConstants.CopilotGenericFallbackMessage,
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return IntegrationResult.PartialSuccess(clipboardFallbackMessage);
                }
                catch
                {
                    return IntegrationResult.Fail("Failed to send prompt", ex);
                }
            }
        }

        // ==================== Automation Scheduler ====================

        /// <summary>
        /// Schedules the automated prompt entry as a chain of non-blocking
        /// DispatcherTimer steps. Each step yields to the UI thread so that
        /// Copilot Chat can render and process events between operations.
        ///
        /// <para><b>Step 1</b> (after CopilotOpenDelayMs): Start new thread via DTE.</para>
        /// <para><b>Step 2</b> (after NewThreadDelayMs): Re-focus Copilot Chat, paste prompt, submit.</para>
        /// </summary>
        private static void ScheduleAutomatedPromptEntry(string prompt)
        {
            // Step 1: Wait for Copilot Chat to render, then start new thread
            ScheduleOnIdle(Timing.CopilotOpenDelayMs, () =>
            {
                bool newThreadStarted = TryStartNewThread();
                Log(newThreadStarted
                    ? "New thread started via DTE command"
                    : "DTE new-thread commands not available, continuing with current thread");

                int settleDelay = newThreadStarted ? Timing.NewThreadDelayMs : Timing.PasteDelayMs;

                // Step 2: Wait for new thread UI to settle, then paste + submit
                ScheduleOnIdle(settleDelay, () =>
                {
                    PerformPasteAndSubmit();
                });
            });
        }

        /// <summary>
        /// Re-focuses Copilot Chat and pastes the prompt from clipboard.
        /// Uses only DTE commands and SendKeys — no Thread.Sleep, no blocking
        /// UI Automation tree scans.
        /// </summary>
        private static void PerformPasteAndSubmit()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Re-focus the Copilot Chat window so SendKeys goes to the right place
                TryExecuteDteCommands(OpenChatCommands);
                Log("Re-focused Copilot Chat before paste");

                PasteAndSubmitViaSendKeys();

                Log("Paste + submit completed");
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.PerformPasteAndSubmit");
                MessageBox.Show(
                    CxAssistConstants.CopilotPasteFailedMessage,
                    CxAssistConstants.DisplayName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Schedules an action on the UI thread after a delay, without
        /// blocking (no Thread.Sleep). Uses DispatcherTimer at ApplicationIdle
        /// so VS remains responsive.
        /// </summary>
        private static void ScheduleOnIdle(int delayMs, Action action)
        {
            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(delayMs)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.ScheduleOnIdle");
                    MessageBox.Show(
                        CxAssistConstants.CopilotPasteFailedMessage,
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            };
            timer.Start();
        }

        // ==================== SendKeys ====================

        /// <summary>
        /// Pastes the prompt from clipboard and submits via SendKeys.
        /// Brief pause between paste and Enter lets VS process the clipboard content.
        /// </summary>
        private static void PasteAndSubmitViaSendKeys()
        {
            System.Windows.Forms.SendKeys.SendWait("^v");
            System.Threading.Thread.Sleep(Timing.PasteSettleMs);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        // ==================== Opening Copilot Chat ====================

        /// <summary>
        /// Attempts to open GitHub Copilot Chat using multiple strategies:
        /// <list type="number">
        ///   <item>DTE ExecuteCommand with known command IDs</item>
        ///   <item>Keyboard shortcut simulation (Ctrl+\ then C)</item>
        /// </list>
        /// </summary>
        private static bool TryOpenCopilotChat()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Strategy 1: DTE commands (most reliable)
            if (TryExecuteDteCommands(OpenChatCommands))
            {
                Log("Opened Copilot Chat via DTE command");
                return true;
            }

            // Strategy 2: Keyboard shortcut (Ctrl+\ then C)
            try
            {
                System.Windows.Forms.SendKeys.SendWait("^\\c");
                Log("Opened Copilot Chat via keyboard shortcut Ctrl+\\, C");
                return true;
            }
            catch (Exception ex)
            {
                Log("Keyboard shortcut failed: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Attempts to start a new chat thread via DTE commands.
        /// </summary>
        private static bool TryStartNewThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return TryExecuteDteCommands(NewThreadCommands);
        }

        // ==================== Availability Check ====================

        /// <summary>
        /// Checks if GitHub Copilot is available before attempting to open it.
        /// Aligned with JetBrains CopilotIntegration.isCopilotAvailable: checks known
        /// command IDs via DTE.Commands to see if any are registered.
        /// </summary>
        public static bool IsCopilotAvailable()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var dte = GetDte();
                if (dte?.Commands == null) return false;

                foreach (string cmdId in OpenChatCommands)
                {
                    try
                    {
                        var cmd = dte.Commands.Item(cmdId);
                        if (cmd != null) return true;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        // ==================== DTE Helpers ====================

        private static DTE2 GetDte()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        /// <summary>
        /// Tries each command ID in order. Returns true on the first success.
        /// </summary>
        private static bool TryExecuteDteCommands(string[] commandIds)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = GetDte();
            if (dte == null) return false;

            foreach (string cmd in commandIds)
            {
                try
                {
                    dte.ExecuteCommand(cmd);
                    Log("DTE command succeeded: " + cmd);
                    return true;
                }
                catch
                {
                    Log("DTE command not available: " + cmd);
                }
            }
            return false;
        }

        // ==================== Clipboard ====================

        private static bool CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (Exception ex)
            {
                Log("Clipboard failed: " + ex.Message);
                return false;
            }
        }

        // ==================== Logging ====================

        private static void Log(string message)
        {
            Debug.WriteLine("[" + CxAssistConstants.LogCategory + "] CopilotIntegration: " + message);
        }
    }
}
