using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ast_visual_studio_extension.CxExtension.Utils;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Automation;
using Process = System.Diagnostics.Process;
using System.Linq;

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
            public const int CopilotOpenDelayMs = 900;

            /// <summary>Delay after starting a new thread for UI to settle.</summary>
            public const int NewThreadDelayMs = 400;

            /// <summary>Delay before paste/submit to ensure input field has focus.</summary>
            public const int PasteDelayMs = 350;

            /// <summary>Brief pause between paste and Enter to let VS process clipboard.</summary>
            public const int PasteSettleMs = 100;
        }

        /// <summary>
        /// UI Automation properties for GitHub Copilot Chat integration.
        /// </summary>
        private static class AutomationProperties
        {
            public static readonly string[] ModePickerNames = {
                // VS 2026 name (primary)
                "Chat mode",
                // VS 2022 and fallback names
                "Chat Mode Picker",
                "Agent Mode Picker", "Agent mode", "Agent",
                "Mode", "Copilot mode", "Chat mode picker",
                "Mode picker", "Pick a mode"
            };
            public const string AgentOptionName = "Agent";
        }

        // ==================== Command ID Constants ====================

        /// <summary>DTE command IDs for opening the Copilot Chat window.</summary>
        private static readonly string[] OpenChatCommands =
        {
            "View.GitHub.Copilot.Chat",
            "Copilot.Open.Output.Window",
            "GitHub.Copilot.Chat.OpenThreads"
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
        /// Shows a non-modal main-window info bar (fallback: status bar). No blocking dialogs.
        /// </summary>
        /// <param name="useWarningSeverity">When true and not an error, uses the warning (yellow) info bar style.</param>
        public static void ShowAssistNotification(string message, bool isError = false, bool useWarningSeverity = false)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var pkg = ServiceProvider.GlobalProvider?.GetService(typeof(AsyncPackage)) as AsyncPackage;
                if (pkg != null)
                {
                    if (isError)
                        CxUtils.DisplayMessageInInfoBar(pkg, message, KnownMonikers.StatusError, autoDismiss: true);
                    else if (useWarningSeverity)
                        CxUtils.DisplayMessageInInfoBar(pkg, message, KnownMonikers.StatusWarning, autoDismiss: true);
                    else
                        CxUtils.DisplayMessageInInfoBar(pkg, message, KnownMonikers.StatusInformation, autoDismiss: true);
                    return;
                }

                var dte = GetDte();
                if (dte?.StatusBar != null)
                    dte.StatusBar.Text = message;
            }
            catch (Exception ex)
            {
                Log("ShowAssistNotification failed: " + ex.Message);
            }
        }

        /// <summary>True when GitHub Copilot chat commands are registered (extension present).</summary>
        public static bool CheckCopilotInstalled()
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

        /// <summary>Legacy name; use <see cref="CheckCopilotInstalled"/>.</summary>
        public static bool IsCopilotAvailable() => CheckCopilotInstalled();

        /// <summary>
        /// Starts a new Copilot chat thread via DTE (best-effort).
        /// </summary>
        public static bool OpenCopilotThread()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return TryExecuteDteCommands(NewThreadCommands);
        }

        /// <summary>
        /// Whether Copilot Chat appears to be in Agent mode (VS 2022 vs newer UIs differ; heuristics apply for major version 19+).
        /// VS 2026: Mode detection is unreliable via UI Automation, so we assume Agent mode is active.
        /// </summary>
        public static bool IsAgentMode()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var vsProcess = Process.GetCurrentProcess();
                AutomationElement vsWindow = AutomationElement.FromHandle(vsProcess.MainWindowHandle);

                if (vsWindow == null)
                {
                    Log("IsAgentMode: Could not get VS main window");
                    return false;
                }

                // Attempt UI Automation detection
                // NOTE: VS 2026 doesn't reliably expose current mode through standard UI Automation patterns.
                // In Ask mode, detection will return false (correct behavior).
                // In Agent mode, detection may or may not work depending on whether the UI exposes the mode state.
                bool detected = IsAgentModeAlreadyActive(vsWindow);
                return detected;
            }
            catch (Exception ex)
            {
                Log("IsAgentMode failed: " + ex.Message);
                return false;
            }
        }

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

                // Capture the code document window before Copilot steals focus (for editor info bar above the file).
                IVsWindowFrame assistDocumentFrame = TryCaptureAssistDocumentFrame();

                // Step 2: Pre-check if Copilot is available (aligned with JetBrains CopilotIntegration.isCopilotAvailable)
                if (!CheckCopilotInstalled())
                {
                    Log("Copilot not available (pre-check), prompt copied to clipboard");
                    ShowCopilotNotInstalledMessage(assistDocumentFrame);
                    return IntegrationResult.CopilotNotAvailable(
                        CxAssistConstants.CopilotNotInstalledInfoBarMessage);
                }

                // Step 3: Open Copilot Chat
                bool opened = OpenCopilotChat();
                if (!opened)
                {
                    Log("Copilot Chat failed to open - Copilot may not be installed");
                    ShowCopilotChatOpenFailedMessage(assistDocumentFrame);
                    return IntegrationResult.CopilotNotAvailable(
                        CxAssistConstants.CopilotChatOpenFailedInfoBarMessage);
                }

                Log("Copilot Chat opened, scheduling automation sequence");

                // Step 4: Schedule the automation sequence after UI renders
                ScheduleAutomatedPromptEntry(prompt, assistDocumentFrame);

                return IntegrationResult.PartialSuccess(
                    "Copilot Chat opened, automation in progress...");
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.SendPromptToCopilot");
                try
                {
                    CopyToClipboard(prompt);
                    ShowAssistNotification(
                        clipboardFallbackMessage ?? CxAssistConstants.CopilotGenericFallbackMessage);
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
        /// <para><b>Agent mode:</b> new thread → paste → Enter (submit).</para>
        /// <para><b>Non-agent:</b> new thread (awaited via timer chain) → paste only → info bar (no modal).</para>
        /// </summary>
        /// <summary>
        /// Resolves the active document <see cref="IVsWindowFrame"/> while the editor still has selection context.
        /// </summary>
        private static IVsWindowFrame TryCaptureAssistDocumentFrame()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var mon = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                if (mon != null
                    && ErrorHandler.Succeeded(mon.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out object frameObj))
                    && frameObj is IVsWindowFrame frame)
                {
                    return frame;
                }
            }
            catch (Exception ex)
            {
                Log("TryCaptureAssistDocumentFrame: " + ex.Message);
            }

            return null;
        }

        private static void ShowCopilotNotAgentModeUserMessage(IVsWindowFrame assistDocumentFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                CxAssistConstants.CopilotNotAgentModeInfoBarMessage,
                () => ShowAssistNotification(
                    CxAssistConstants.CopilotNotAgentModeInfoBarMessage,
                    isError: false,
                    useWarningSeverity: true));
        }

        /// <summary>
        /// Shows Copilot not installed warning in the info bar.
        /// </summary>
        private static void ShowCopilotNotInstalledMessage(IVsWindowFrame assistDocumentFrame)
        {
            if (assistDocumentFrame == null) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                CxAssistConstants.CopilotNotInstalledInfoBarMessage,
                () => ShowAssistNotification(
                    CxAssistConstants.CopilotNotInstalledInfoBarMessage,
                    isError: false,
                    useWarningSeverity: true));
        }

        /// <summary>
        /// Shows Copilot Chat failed to open warning in the info bar.
        /// </summary>
        private static void ShowCopilotChatOpenFailedMessage(IVsWindowFrame assistDocumentFrame)
        {
            if (assistDocumentFrame == null) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                CxAssistConstants.CopilotChatOpenFailedInfoBarMessage,
                () => ShowAssistNotification(
                    CxAssistConstants.CopilotChatOpenFailedInfoBarMessage,
                    isError: false,
                    useWarningSeverity: true));
        }

        /// <summary>
        /// Shows Copilot prompt preparation failed error in the info bar.
        /// </summary>
        private static void ShowCopilotPromptPrepareFailedMessage(IVsWindowFrame assistDocumentFrame)
        {
            if (assistDocumentFrame == null) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowError(
                assistDocumentFrame,
                CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage,
                () => ShowAssistNotification(
                    CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage,
                    isError: true));
        }

        /// <summary>
        /// Shows VS 2026 paste-only workflow message in the info bar.
        /// Used when mode detection is unavailable and prompt is pasted without auto-submit.
        /// </summary>
        private static void ShowCopilotPasteOnlyVs2026Message(IVsWindowFrame assistDocumentFrame)
        {
            if (assistDocumentFrame == null) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                CxAssistConstants.CopilotPasteOnlyVs2026InfoBarMessage,
                () => ShowAssistNotification(
                    CxAssistConstants.CopilotPasteOnlyVs2026InfoBarMessage,
                    isError: false,
                    useWarningSeverity: true));
        }

        private static void ScheduleAutomatedPromptEntry(string prompt, IVsWindowFrame assistDocumentFrame)
        {
            ScheduleOnIdle(Timing.CopilotOpenDelayMs, () =>
            {
                try
                {
                    int vsMajor = GetVisualStudioMajorVersion();

                    // VS 2026+: Mode detection is unreliable (Chat mode button doesn't expose selection state)
                    // Skip all automation and paste-only workflow with user message
                    if (vsMajor >= 18)
                    {
                        Log("VS 2026+ detected — using paste-only workflow (mode detection unavailable)");
                        ScheduleOnIdle(Timing.NewThreadDelayMs, () =>
                        {
                            bool threadOk = OpenCopilotThread();
                            if (!threadOk)
                                ShowCopilotChatOpenFailedMessage(assistDocumentFrame);
                            else
                            {
                                try
                                {
                                    var vsProc = Process.GetCurrentProcess();
                                    AutomationElement wnd = AutomationElement.FromHandle(vsProc.MainWindowHandle);
                                    if (wnd != null)
                                        FocusCopilotInput(wnd);
                                }
                                catch (Exception exFocus)
                                {
                                    Log("UI Automation: focus before paste: " + exFocus.Message);
                                }
                            }

                            int delayBeforePaste = threadOk ? Timing.NewThreadDelayMs : Timing.PasteDelayMs;
                            ScheduleOnIdle(delayBeforePaste, () =>
                            {
                                bool inserted = InsertPromptWithoutSubmitting();
                                if (!threadOk)
                                    return;
                                if (!inserted)
                                    ShowCopilotPromptPrepareFailedMessage(assistDocumentFrame);
                                else
                                    ShowCopilotPasteOnlyVs2026Message(assistDocumentFrame);
                            });
                        });
                        return;
                    }

                    // VS 2022 and earlier: Attempt to detect Agent mode
                    bool agentMode = IsAgentMode();

                    if (agentMode)
                    {
                        Log("Agent mode detected — auto-submitting prompt");
                        ScheduleOnIdle(Timing.NewThreadDelayMs, () =>
                        {
                            bool newThreadStarted = OpenCopilotThread();
                            Log(newThreadStarted
                                ? "New thread started via DTE command"
                                : "DTE new-thread commands not available, continuing with current thread");

                            if (newThreadStarted)
                            {
                                try
                                {
                                    var vsProc = Process.GetCurrentProcess();
                                    AutomationElement wnd = AutomationElement.FromHandle(vsProc.MainWindowHandle);
                                    if (wnd != null)
                                    {
                                        bool focused = FocusCopilotInput(wnd);
                                        Log("UI Automation: Focused Copilot input after new thread: " + focused);
                                    }
                                }
                                catch (Exception exFocus)
                                {
                                    Log("UI Automation: error focusing input after new thread: " + exFocus.Message);
                                }
                            }

                            int delayAfterThread = newThreadStarted ? Timing.NewThreadDelayMs : Timing.PasteDelayMs;
                            ScheduleOnIdle(delayAfterThread, PerformPasteAndSubmit);
                        });
                        return;
                    }

                    Log("Agent mode not detected — pasting prompt without auto-submit");
                    ScheduleOnIdle(Timing.NewThreadDelayMs, () =>
                    {
                        bool threadOk = OpenCopilotThread();
                        if (!threadOk)
                            ShowCopilotChatOpenFailedMessage(assistDocumentFrame);
                        else
                        {
                            try
                            {
                                var vsProc = Process.GetCurrentProcess();
                                AutomationElement wnd = AutomationElement.FromHandle(vsProc.MainWindowHandle);
                                if (wnd != null)
                                    FocusCopilotInput(wnd);
                            }
                            catch (Exception exFocus)
                            {
                                Log("UI Automation: focus before non-agent paste: " + exFocus.Message);
                            }
                        }

                        int delayBeforePaste = threadOk ? Timing.NewThreadDelayMs : Timing.PasteDelayMs;
                        ScheduleOnIdle(delayBeforePaste, () =>
                        {
                            bool inserted = InsertPromptWithoutSubmitting();
                            if (!threadOk)
                                return;
                            if (!inserted)
                                ShowCopilotPromptPrepareFailedMessage(assistDocumentFrame);
                            else
                                ShowCopilotNotAgentModeUserMessage(assistDocumentFrame);
                        });
                    });
                }
                catch (Exception ex)
                {
                    Log("ScheduleAutomatedPromptEntry error: " + ex.Message);
                }
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
                ShowAssistNotification(CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage, isError: true);
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
                    ShowAssistNotification(CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage, isError: true);
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Pastes the prompt from the clipboard into Copilot input without sending (no Enter).
        /// </summary>
        private static bool InsertPromptWithoutSubmitting()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                TryExecuteDteCommands(OpenChatCommands);
                System.Windows.Forms.SendKeys.SendWait("^v");
                return true;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.InsertPromptWithoutSubmitting");
                return false;
            }
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
        /// Opens the Copilot Chat tool window (not necessarily a new thread), using DTE commands or Ctrl+\, C.
        /// </summary>
        private static bool OpenCopilotChat()
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

        // ==================== Availability Check ====================
        // See <see cref="CheckCopilotInstalled"/> and <see cref="IsCopilotAvailable"/>.

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

        /// <summary>
        /// Returns the Visual Studio major version number (e.g. 17 for VS2022).
        /// Returns -1 if the version cannot be determined.
        /// </summary>
        private static int GetVisualStudioMajorVersion()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var dte = GetDte();
                if (dte?.Version != null)
                {
                    Log($"GetVisualStudioMajorVersion: DTE.Version = '{dte.Version}'");
                    var parts = dte.Version.Split('.');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int major))
                    {
                        Log($"GetVisualStudioMajorVersion: Parsed major version = {major}");
                        return major;
                    }
                }
                else
                {
                    Log($"GetVisualStudioMajorVersion: DTE or DTE.Version is null");
                }
            }
            catch (Exception ex)
            {
                Log("Failed to parse Visual Studio version: " + ex.Message);
            }
            return -1;
        }

        // ==================== Agent Mode Switching ====================

        /// <summary>
        /// Finds the Mode Picker button by searching the VS window for known names.
        /// </summary>
        private static AutomationElement FindModePickerButton(AutomationElement root)
        {
            foreach (string pickerName in AutomationProperties.ModePickerNames)
            {
                var picker = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.NameProperty, pickerName));

                if (picker != null)
                {
                    Log("UI Automation: Found Mode Picker button: '" + pickerName + "'");
                    return picker;
                }
            }
            return null;
        }

        /// <summary>
        /// Attempts to read the currently selected mode string from the Mode Picker.
        /// Tries Value/Text/Selection/SelectionItem patterns then falls back to
        /// local descendants, parent siblings, and a nearby spatial search.
        /// Returns null when no candidate is found.
        /// </summary>
        private static string GetSelectedMode(AutomationElement modePicker, AutomationElement root)
        {
            try
            {
                if (modePicker == null) return null;

                // 1) ValuePattern
                try
                {
                    if (modePicker.TryGetCurrentPattern(ValuePattern.Pattern, out object valObj))
                    {
                        var vp = (ValuePattern)valObj;
                        string v = vp.Current.Value?.Trim();
                        if (!string.IsNullOrEmpty(v)) return v;
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: ValuePattern failed: " + ex.Message);
                }

                // 2) TextPattern
                try
                {
                    if (modePicker.TryGetCurrentPattern(TextPattern.Pattern, out object textObj))
                    {
                        var tp = (TextPattern)textObj;
                        string t = tp.DocumentRange.GetText(-1)?.Trim();
                        if (!string.IsNullOrEmpty(t)) return t;
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: TextPattern failed: " + ex.Message);
                }

                // 3) SelectionPattern
                try
                {
                    if (modePicker.TryGetCurrentPattern(SelectionPattern.Pattern, out object selObj))
                    {
                        var sp = (SelectionPattern)selObj;
                        var sel = sp.Current.GetSelection();
                        if (sel != null && sel.Length > 0)
                        {
                            string nm = sel[0].Current.Name?.Trim();
                            if (!string.IsNullOrEmpty(nm)) return nm;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: SelectionPattern failed: " + ex.Message);
                }

                // 4) SelectionItem on descendants (some tree items report selection)
                try
                {
                    var all = modePicker.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                    for (int i = 0; i < all.Count; i++)
                    {
                        try
                        {
                            var el = all[i];
                            if (el.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object sipObj))
                            {
                                var sip = (SelectionItemPattern)sipObj;
                                if (sip.Current.IsSelected)
                                {
                                    string nm = el.Current.Name?.Trim();
                                    if (!string.IsNullOrEmpty(nm)) return nm;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("UI Automation: SelectionItem descendant iteration failed: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: SelectionItem descendants enumeration failed: " + ex.Message);
                }

                // 5) Fallback: first non-empty named descendant
                try
                {
                    var all = modePicker.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                    for (int i = 0; i < all.Count; i++)
                    {
                        try
                        {
                            var el = all[i];
                            string nm = el.Current.Name?.Trim();
                            if (!string.IsNullOrEmpty(nm)) return nm;
                        }
                        catch (Exception ex)
                        {
                            Log("UI Automation: Named descendant iteration failed: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: Named descendants enumeration failed: " + ex.Message);
                }

                // 6) Parent siblings
                try
                {
                    var parent = System.Windows.Automation.TreeWalker.ControlViewWalker.GetParent(modePicker);
                    if (parent != null)
                    {
                        var siblings = parent.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                        for (int i = 0; i < siblings.Count; i++)
                        {
                            try
                            {
                                var s = siblings[i];
                                string sn = s.Current.Name?.Trim();
                                if (!string.IsNullOrEmpty(sn)) return sn;
                            }
                            catch (Exception ex)
                            {
                                Log("UI Automation: Sibling iteration failed: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: Parent siblings enumeration failed: " + ex.Message);
                }

                // 7) Spatial fallback: nearby elements overlapping the picker's bounds
                try
                {
                    var pickerRect = modePicker.Current.BoundingRectangle;
                    if (!pickerRect.IsEmpty)
                    {
                        var all = root.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                        for (int i = 0; i < all.Count; i++)
                        {
                            try
                            {
                                var el = all[i];
                                var r = el.Current.BoundingRectangle;
                                if (r.IsEmpty) continue;
                                bool intersect = !(r.Right < pickerRect.Left || r.Left > pickerRect.Right || r.Bottom < pickerRect.Top || r.Top > pickerRect.Bottom);
                                if (intersect)
                                {
                                    string nm = el.Current.Name?.Trim();
                                    if (!string.IsNullOrEmpty(nm)) return nm;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("UI Automation: Spatial fallback iteration failed: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: Spatial fallback enumeration failed: " + ex.Message);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if Agent mode is already active by examining the Mode Picker
        /// button's current display name. If it contains "Agent", the mode is active.
        /// </summary>
        private static bool IsAgentModeAlreadyActive(AutomationElement root)
        {
            try
            {
                AutomationElement modePicker = FindModePickerButton(root);
                if (modePicker == null)
                    return false;

                // VS 2026: Search entire Copilot Chat pane for "Agent" text indicator
                // The current mode is displayed somewhere in the Copilot Chat window, not necessarily at the button
                try
                {
                    // Find the Copilot Chat pane (parent of the mode picker)
                    AutomationElement copilotPane = modePicker;
                    for (int i = 0; i < 10; i++) // Walk up the tree
                    {
                        var parent = TreeWalker.ControlViewWalker.GetParent(copilotPane);
                        if (parent == null) break;

                        string parentName = parent.Current.Name ?? "";
                        if (parentName.IndexOf("Copilot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            parentName.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            copilotPane = parent;
                            break;
                        }
                        copilotPane = parent;
                    }

                    // Search pane for "Agent" text
                    var allInPane = copilotPane.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                    for (int i = 0; i < allInPane.Count; i++)
                    {
                        try
                        {
                            string name = allInPane[i].Current.Name ?? "";
                            // Look for standalone "Agent" or "Agent mode" indicator
                            if (name.Equals("Agent", StringComparison.OrdinalIgnoreCase) ||
                                name.IndexOf("Agent mode", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Log("UI Automation: Found Agent mode indicator: '" + name + "'");
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: Copilot pane search failed: " + ex.Message);
                }

                // Fallback: Check button's direct children
                try
                {
                    var children = modePicker.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                    foreach (AutomationElement child in children)
                    {
                        try
                        {
                            string childName = child.Current.Name ?? "";
                            if (childName.IndexOf("agent", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Log("UI Automation: Found 'Agent' in button child: " + childName);
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log("UI Automation: Child search failed: " + ex.Message);
                }

                string modePickerName = modePicker.Current.Name ?? "";
                bool isAgentActive = modePickerName.IndexOf("agent", StringComparison.OrdinalIgnoreCase) >= 0;

                // Prefer a direct read of the selected value via common patterns
                string selected = GetSelectedMode(modePicker, root);
                if (!string.IsNullOrEmpty(selected))
                {
                    isAgentActive = selected.IndexOf("agent", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else
                {
                    // Fall back to checking the Mode Picker's own Name text
                    isAgentActive = modePickerName.IndexOf("agent", StringComparison.OrdinalIgnoreCase) >= 0;
                }

                // Additional heuristics for newer VS versions (e.g., VS2026):
                // 1) Spatial: sometimes the visible selected text is rendered in a
                // nearby descendant rather than as the Mode Picker's Name/value.
                // 2) VS-version-specific: for VS2026 UI changes, expand the spatial
                // search area and allow matches that are near the picker even if
                // their bounding rects don't strictly intersect.
                if (!isAgentActive)
                {
                    try
                    {
                        var pickerRect = modePicker.Current.BoundingRectangle;
                        var all = root.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);

                        int vsMajor = GetVisualStudioMajorVersion();
                        // VS 17 = VS2022; VS 18+ = newer shell (2025/2026) with different Copilot mode UI.
                        bool isNewVs = vsMajor >= 18;

                        for (int i = 0; i < all.Count; i++)
                        {
                            try
                            {
                                var el = all[i];
                                string name = el.Current.Name ?? "";
                                if (string.IsNullOrEmpty(name)) continue;

                                if (name.IndexOf("agent", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                if (name.IndexOf("search agents", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                                var r = el.Current.BoundingRectangle;
                                if (r.IsEmpty) continue;

                                bool intersect = false;
                                if (!pickerRect.IsEmpty)
                                {
                                    // For newer VS, expand the picker area by 40 pixels to be more forgiving
                                    var expanded = new System.Windows.Rect(
                                        pickerRect.X - (isNewVs ? 40 : 0),
                                        pickerRect.Y - (isNewVs ? 20 : 0),
                                        pickerRect.Width + (isNewVs ? 80 : 0),
                                        pickerRect.Height + (isNewVs ? 40 : 0));

                                    intersect = !(r.Right < expanded.Left || r.Left > expanded.Right || r.Bottom < expanded.Top || r.Top > expanded.Bottom);
                                }

                                if (intersect || !isNewVs)
                                {
                                    Log("UI Automation: Heuristic detected Agent text: '" + name + "'");
                                    if (!el.Current.IsOffscreen)
                                    {
                                        isAgentActive = true;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("UI Automation: IsAgentModeAlreadyActive heuristic iteration failed: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("UI Automation: IsAgentModeAlreadyActive heuristic enumeration failed: " + ex.Message);
                    }
                }

                // If VS2026 (or newer) couldn't be positively detected, log the VS major version
                int detectedVsMajor = GetVisualStudioMajorVersion();
                if (detectedVsMajor >= 0)
                {
                    Log("UI Automation: Detected Visual Studio major version: " + detectedVsMajor);
                }

                Log("UI Automation: Mode Picker current name: '" + modePickerName
                    + "' (Agent active: " + isAgentActive + ")");
                return isAgentActive;
            }
            catch (Exception ex)
            {
                Log("UI Automation error in IsAgentModeAlreadyActive: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Attempts to find the Copilot Chat text input area and set keyboard focus to it.
        /// Uses heuristics: editable controls, document controls, or any focusable
        /// element whose name looks like a chat prompt. Returns true when focus set.
        /// </summary>
        private static bool FocusCopilotInput(AutomationElement root)
        {
            try
            {
                if (root == null) return false;

                var all = root.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                for (int i = 0; i < all.Count; i++)
                {
                    try
                    {
                        var el = all[i];
                        if (!el.Current.IsEnabled) continue;

                        string ct = el.Current.ControlType?.ProgrammaticName ?? "";
                        string name = el.Current.Name ?? "";

                        bool likelyEdit = ct.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0
                            || ct.IndexOf("Document", StringComparison.OrdinalIgnoreCase) >= 0;

                        bool nameHint = !string.IsNullOrEmpty(name) && (
                            name.IndexOf("type", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("message", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("chat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("prompt", StringComparison.OrdinalIgnoreCase) >= 0);

                        if ((likelyEdit || nameHint) && el.Current.IsKeyboardFocusable)
                        {
                            try
                            {
                                el.SetFocus();
                                System.Threading.Thread.Sleep(120);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                Log("UI Automation: SetFocus for likely edit failed: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("UI Automation: FocusCopilotInput likely edit enumeration failed: " + ex.Message);
                    }
                }

                // Final fallback: any focusable element
                for (int i = 0; i < all.Count; i++)
                {
                    try
                    {
                        var el = all[i];
                        if (el.Current.IsKeyboardFocusable && el.Current.IsEnabled)
                        {
                            try
                            {
                                el.SetFocus();
                                System.Threading.Thread.Sleep(120);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                Log("UI Automation: SetFocus for fallback focusable element failed: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("UI Automation: FocusCopilotInput final fallback iteration failed: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("UI Automation: FocusCopilotInput error: " + ex.Message);
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
