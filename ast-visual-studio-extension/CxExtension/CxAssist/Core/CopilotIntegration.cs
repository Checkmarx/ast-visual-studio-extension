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
        };

        // ==================== Result Types ====================

        /// <summary>
        /// Which Copilot Chat mode a prompt is expected to be submitted in.
        /// "Fix with Checkmarx One Assist" requires Agent mode (it asks Copilot to edit files).
        /// "View details" requires Ask mode (it only asks Copilot to explain, not edit).
        /// </summary>
        public enum RequiredCopilotMode
        {
            Agent,
            Ask
        }

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
        /// If the Copilot Chat input currently has unsubmitted draft text, clears it and submits
        /// before requesting a new thread. GitHub.Copilot.Chat.NewThread/New silently keep the
        /// current thread when the input has unsubmitted typing — DTE.ExecuteCommand still reports
        /// success, so the caller can't tell the difference — which otherwise causes the next paste
        /// to land in the same, still-drafting chat instead of a fresh one.
        ///
        /// Tries <see cref="ValuePattern"/>.<c>SetValue("")</c> first — zero risk of typing into the
        /// wrong control, since it never sends keystrokes. Copilot Chat's real input is a modern
        /// rich-text editor that typically does NOT expose ValuePattern, so this alone leaves the
        /// draft untouched in practice; when ValuePattern is unavailable or reports no value, falls
        /// back to focusing the SAME already-located input (never a broader search) and sending
        /// Ctrl+A + Delete. Scoping the keystrokes to the element FindCopilotInputElement already
        /// matched by control type and name hint keeps this safe from the historical bug where a
        /// broad fallback selected the code editor instead.
        /// </summary>
        private static void ClearAndSubmitPendingCopilotDraft()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var vsProcess = Process.GetCurrentProcess();
                AutomationElement vsWindow = AutomationElement.FromHandle(vsProcess.MainWindowHandle);
                if (vsWindow == null) return;

                AutomationElement input = FindCopilotInputElement(vsWindow);
                if (input == null) return;

                bool hasDraft = false;
                bool clearedViaValuePattern = false;
                try
                {
                    if (input.TryGetCurrentPattern(ValuePattern.Pattern, out object vpObj))
                    {
                        var vp = (ValuePattern)vpObj;
                        hasDraft = !string.IsNullOrEmpty(vp.Current.Value);
                        if (hasDraft && !vp.Current.IsReadOnly)
                        {
                            vp.SetValue(string.Empty);
                            clearedViaValuePattern = true;
                            Log("ClearAndSubmitPendingCopilotDraft: cleared leftover draft via ValuePattern");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("ClearAndSubmitPendingCopilotDraft: ValuePattern clear failed: " + ex.Message);
                }

                if (!clearedViaValuePattern)
                {
                    // ValuePattern wasn't available/didn't apply — fall back to keystrokes, but only
                    // ever on this same, already-located input element.
                    try
                    {
                        input.SetFocus();
                        System.Threading.Thread.Sleep(120);
                        System.Windows.Forms.SendKeys.SendWait("^a");
                        System.Threading.Thread.Sleep(50);
                        System.Windows.Forms.SendKeys.SendWait("{DELETE}");
                        System.Threading.Thread.Sleep(50);
                        hasDraft = true;
                        Log("ClearAndSubmitPendingCopilotDraft: cleared leftover draft via SendKeys fallback");
                    }
                    catch (Exception ex)
                    {
                        Log("ClearAndSubmitPendingCopilotDraft: SendKeys fallback failed: " + ex.Message);
                    }
                }

                if (!hasDraft)
                {
                    Log("ClearAndSubmitPendingCopilotDraft: no unsubmitted draft found");
                    return;
                }

                input.SetFocus();
                System.Threading.Thread.Sleep(120);
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                Log("ClearAndSubmitPendingCopilotDraft: submitted cleared draft so NewThread won't no-op");
            }
            catch (Exception ex)
            {
                Log("ClearAndSubmitPendingCopilotDraft error: " + ex.Message);
            }
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
        /// <param name="requiredMode">The Copilot Chat mode this prompt expects to be submitted in (Agent for Fix, Ask for View details).</param>
        public static bool SendPromptToCopilot(string prompt, string clipboardFallbackMessage, RequiredCopilotMode requiredMode = RequiredCopilotMode.Agent)
        {
            IntegrationResult result = SendPromptToCopilotDetailed(prompt, clipboardFallbackMessage, requiredMode);
            return result != null && result.Result != OperationResult.Failed;
        }

        /// <summary>
        /// Opens Copilot Chat with prompt and returns detailed result.
        /// </summary>
        /// <param name="prompt">The prompt to send to Copilot.</param>
        /// <param name="clipboardFallbackMessage">Message shown if only clipboard copy succeeded.</param>
        /// <param name="requiredMode">The Copilot Chat mode this prompt expects to be submitted in (Agent for Fix, Ask for View details).</param>
        public static IntegrationResult SendPromptToCopilotDetailed(string prompt, string clipboardFallbackMessage, RequiredCopilotMode requiredMode = RequiredCopilotMode.Agent)
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

                // Pin immediately so the pane can't auto-hide before the scheduled automation
                // steps below run against it (see PinCopilotChatWindow for why this matters).
                PinCopilotChatWindow();

                // Step 4: Schedule the automation sequence after UI renders
                ScheduleAutomatedPromptEntry(prompt, assistDocumentFrame, requiredMode);

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

        /// <summary>
        /// Shows a non-modal warning that the prompt was pasted but not submitted because Copilot
        /// Chat is not in the mode this action requires (Agent for Fix, Ask for View details).
        /// </summary>
        private static void ShowCopilotWrongModeUserMessage(IVsWindowFrame assistDocumentFrame, RequiredCopilotMode requiredMode)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = requiredMode == RequiredCopilotMode.Agent
                ? CxAssistConstants.CopilotNotAgentModeInfoBarMessage
                : CxAssistConstants.CopilotNotAskModeInfoBarMessage;
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                message,
                () => ShowAssistNotification(
                    message,
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
        /// Shows Copilot prompt preparation failed error in the info bar (as warning with error fallback).
        /// </summary>
        private static void ShowCopilotPromptPrepareFailedMessage(IVsWindowFrame assistDocumentFrame)
        {
            if (assistDocumentFrame == null)
            {
                ShowAssistNotification(CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage, isError: true);
                return;
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            AssistDocumentInfoBar.TryShowWarning(
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
        private static void ShowCopilotPasteOnlyVs2026Message(IVsWindowFrame assistDocumentFrame, RequiredCopilotMode requiredMode)
        {
            if (assistDocumentFrame == null) return;
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = requiredMode == RequiredCopilotMode.Agent
                ? CxAssistConstants.CopilotPasteOnlyVs2026InfoBarMessage
                : CxAssistConstants.CopilotPasteOnlyAskModeVs2026InfoBarMessage;
            AssistDocumentInfoBar.TryShowWarning(
                assistDocumentFrame,
                message,
                () => ShowAssistNotification(
                    message,
                    isError: false,
                    useWarningSeverity: true));
        }

        private static void ScheduleAutomatedPromptEntry(string prompt, IVsWindowFrame assistDocumentFrame, RequiredCopilotMode requiredMode)
        {
            ScheduleOnIdle(Timing.CopilotOpenDelayMs, () =>
            {
                try
                {
                    int vsMajor = GetVisualStudioMajorVersion();

                    // Re-assert the pin: the window may not have been registered in dte.Windows
                    // yet when PinCopilotChatWindow() first ran right after OpenCopilotChat(), and
                    // if the user clicked back into the editor during this delay an unpinned pane
                    // would already have auto-hidden by now, collapsing the input the steps below
                    // (focus, paste) all depend on.
                    PinCopilotChatWindow();

                    // NewThread only actually creates a new thread when the input has no
                    // unsubmitted draft — if the user left text typed but unsent in the current
                    // chat, NewThread silently keeps that thread instead (DTE.ExecuteCommand still
                    // reports success either way, so the no-op is otherwise undetectable). Clear
                    // and submit any pending draft first so NewThread actually switches.
                    ClearAndSubmitPendingCopilotDraft();

                    // Always start a new chat thread FIRST, before any mode detection or paste.
                    // Each remediate click must land in a fresh session so that on repeat clicks
                    // the prior prompt isn't sitting in the input box and the chat mode is
                    // sampled against the freshly opened thread rather than the prior chat state.
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

                    // VS 2026+: Mode detection is unreliable (Chat mode button doesn't expose
                    // selection state), so paste-only after the new thread is open.
                    if (vsMajor >= 18)
                    {
                        Log("VS 2026+ detected — paste-only workflow after new thread (mode detection unavailable)");
                        ScheduleOnIdle(delayAfterThread, () =>
                        {
                            bool inserted = InsertPromptWithoutSubmitting();
                            if (!newThreadStarted)
                            {
                                ShowCopilotChatOpenFailedMessage(assistDocumentFrame);
                                return;
                            }
                            if (!inserted)
                                ShowCopilotPromptPrepareFailedMessage(assistDocumentFrame);
                            else
                                ShowCopilotPasteOnlyVs2026Message(assistDocumentFrame, requiredMode);
                        });
                        return;
                    }

                    // VS 2022 and earlier: detect chat mode AFTER the new thread is open, then
                    // paste-and-submit if the detected mode matches what this action requires
                    // (Agent for Fix, Ask for View details), or paste-only with a mode-specific
                    // warning otherwise.
                    ScheduleOnIdle(delayAfterThread, () =>
                    {
                        bool agentMode = IsAgentMode();
                        bool modeMatches = requiredMode == RequiredCopilotMode.Agent ? agentMode : !agentMode;
                        if (modeMatches)
                        {
                            Log(requiredMode + " mode detected — auto-submitting prompt");
                            if (!PerformPasteAndSubmit())
                                ShowCopilotPromptPrepareFailedMessage(assistDocumentFrame);
                            return;
                        }

                        Log(requiredMode + " mode not detected — pasting prompt without auto-submit");
                        bool inserted = InsertPromptWithoutSubmitting();
                        if (!newThreadStarted)
                        {
                            ShowCopilotChatOpenFailedMessage(assistDocumentFrame);
                            return;
                        }
                        if (!inserted)
                            ShowCopilotPromptPrepareFailedMessage(assistDocumentFrame);
                        else
                            ShowCopilotWrongModeUserMessage(assistDocumentFrame, requiredMode);
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
        /// <returns>True if the paste was sent to the Copilot input; false if aborted because focus could not be confirmed.</returns>
        private static bool PerformPasteAndSubmit()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Re-focus the Copilot Chat window so SendKeys goes to the right place
                TryExecuteDteCommands(OpenChatCommands);
                Log("Re-focused Copilot Chat before paste");

                if (!TryEnsureCopilotInputFocused())
                {
                    Log("Aborting paste+submit: could not confirm Copilot input has keyboard focus");
                    return false;
                }

                PasteAndSubmitViaSendKeys();

                Log("Paste + submit completed");
                return true;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.PerformPasteAndSubmit");
                ShowAssistNotification(CxAssistConstants.CopilotPromptPrepareFailedInfoBarMessage, isError: true);
                return false;
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
        /// Aborts (returns false) rather than pasting if keyboard focus cannot be confirmed to be
        /// on the Copilot input — e.g. the user clicked into a code editor or another window while
        /// the automation delay was pending. SendKeys is focus-relative, not window-targeted, so a
        /// blind paste in that case would land in whatever the user is now focused on.
        /// </summary>
        private static bool InsertPromptWithoutSubmitting()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                TryExecuteDteCommands(OpenChatCommands);

                if (!TryEnsureCopilotInputFocused())
                {
                    Log("Aborting paste: could not confirm Copilot input has keyboard focus");
                    return false;
                }

                System.Windows.Forms.SendKeys.SendWait("^v");
                return true;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.InsertPromptWithoutSubmitting");
                return false;
            }
        }

        /// <summary>
        /// Returns true when the element that currently has real OS keyboard focus looks like the
        /// Copilot Chat input (same heuristic as <see cref="FocusCopilotInput"/>). If it doesn't,
        /// makes one attempt to re-focus the Copilot input and re-checks. This guards against the
        /// user having clicked into a different window/document during the automation delay, which
        /// would otherwise cause the subsequent SendKeys paste to land wherever the user's focus is
        /// now — since SendKeys targets focus, not a specific window.
        /// </summary>
        private static bool TryEnsureCopilotInputFocused()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (IsFocusedElementLikelyCopilotInput())
                    return true;

                Log("Focused element does not look like Copilot input, attempting re-focus");

                var vsProcess = Process.GetCurrentProcess();
                AutomationElement vsWindow = AutomationElement.FromHandle(vsProcess.MainWindowHandle);
                if (vsWindow != null)
                    FocusCopilotInput(vsWindow);

                return IsFocusedElementLikelyCopilotInput();
            }
            catch (Exception ex)
            {
                Log("TryEnsureCopilotInputFocused failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks whether <see cref="AutomationElement.FocusedElement"/> matches the same
        /// edit/name heuristics used to locate the Copilot Chat input in <see cref="FocusCopilotInput"/>.
        /// </summary>
        private static bool IsFocusedElementLikelyCopilotInput()
        {
            try
            {
                AutomationElement focused = AutomationElement.FocusedElement;
                if (focused == null) return false;

                string ct = focused.Current.ControlType?.ProgrammaticName ?? "";
                string name = focused.Current.Name ?? "";

                bool likelyEdit = ct.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0
                    || ct.IndexOf("Document", StringComparison.OrdinalIgnoreCase) >= 0;

                bool nameHint = !string.IsNullOrEmpty(name) && (
                    name.IndexOf("type", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("message", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("chat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("prompt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("copilot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ask", StringComparison.OrdinalIgnoreCase) >= 0);

                bool result = likelyEdit && nameHint;
                Log("IsFocusedElementLikelyCopilotInput: ct='" + ct + "' name='" + name + "' -> " + result);
                return result;
            }
            catch (Exception ex)
            {
                Log("IsFocusedElementLikelyCopilotInput failed: " + ex.Message);
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

        /// <summary>
        /// Pins the Copilot Chat tool window (disables auto-hide) so it cannot collapse when the
        /// user clicks back into the editor while the automated prompt entry is still pending.
        /// Without this, an unpinned Copilot pane auto-hides on focus loss, which both hides the
        /// UI Automation input the later paste/submit steps depend on and can leave the DTE
        /// NewThread command operating on a collapsed window. Best-effort: failures are logged and
        /// swallowed since pinning is a convenience, not a requirement for the clipboard fallback.
        /// </summary>
        private static void PinCopilotChatWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var dte = GetDte();
                if (dte?.Windows == null) return;

                foreach (EnvDTE.Window window in dte.Windows)
                {
                    try
                    {
                        string caption = window.Caption ?? "";
                        if (caption.IndexOf("Copilot", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        if (window.AutoHides)
                        {
                            window.AutoHides = false;
                            Log("Pinned Copilot Chat window (disabled auto-hide): '" + caption + "'");
                        }
                        return;
                    }
                    catch (Exception exWindow)
                    {
                        Log("PinCopilotChatWindow: window inspection failed: " + exWindow.Message);
                    }
                }

                Log("PinCopilotChatWindow: Copilot window not found among dte.Windows");
            }
            catch (Exception ex)
            {
                Log("PinCopilotChatWindow failed: " + ex.Message);
            }
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

                // VS 2026: Search the Copilot Chat pane for an "Agent" text indicator.
                // Scope strictly to a Copilot/Chat-named ancestor — if one isn't found within
                // 10 levels, do NOT fall back to a higher ancestor (i.e. the whole VS window),
                // otherwise the editor's "switch to Agent mode" info bar from a prior click
                // gets caught by the descendant search and produces a false positive.
                try
                {
                    AutomationElement copilotPane = null;
                    AutomationElement cursor = modePicker;
                    for (int i = 0; i < 10; i++)
                    {
                        var parent = TreeWalker.ControlViewWalker.GetParent(cursor);
                        if (parent == null) break;

                        string parentName = parent.Current.Name ?? "";
                        if (parentName.IndexOf("Copilot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            parentName.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            copilotPane = parent;
                            break;
                        }
                        cursor = parent;
                    }

                    if (copilotPane != null)
                    {
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
                    else
                    {
                        Log("UI Automation: Copilot/Chat-named ancestor not found, skipping pane descendant search");
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

                                // Require spatial overlap with the picker for both VS 2022 and
                                // newer shells. Without this, any element whose Name contains
                                // "agent" anywhere in the VS window matches — including the
                                // editor's "switch to Agent mode" document info bar, which
                                // false-positives a second remediate click as Agent mode.
                                if (intersect)
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
        /// Only sets focus on an element whose ControlType is Edit/Document or whose Name
        /// looks like a chat prompt — there is intentionally NO broad "any focusable element"
        /// fallback, because subsequent callers send keystrokes (e.g. Ctrl+A, Delete, Ctrl+V)
        /// and a wrong focus target such as the code editor would destroy user content.
        /// Returns true when focus was set on a plausible chat input.
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
            }
            catch (Exception ex)
            {
                Log("UI Automation: FocusCopilotInput error: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Returns true if name looks like an open source file (e.g. "CopilotIntegration.cs") rather
        /// than a chat control, since a file's title can itself contain "chat"/"copilot"/"prompt"/etc.
        /// </summary>
        private static bool LooksLikeSourceFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            int dot = name.LastIndexOf('.');
            if (dot <= 0 || dot == name.Length - 1) return false;
            int end = dot + 1;
            while (end < name.Length && char.IsLetterOrDigit(name[end])) end++;
            int extLength = end - (dot + 1);
            return extLength >= 1 && extLength <= 6;
        }

        /// <summary>
        /// Locates the Copilot Chat text input area, purely read-only — never calls SetFocus or
        /// sends keystrokes. Used to inspect/clear the input (e.g. via ValuePattern or a scoped
        /// SendKeys fallback) without risking keyboard input landing on the wrong control.
        ///
        /// Unlike <see cref="FocusCopilotInput"/> (which OR's control-type and name-hint, safe there
        /// because callers only paste after independently confirming focus via
        /// <see cref="IsFocusedElementLikelyCopilotInput"/>), this requires BOTH an Edit/Document
        /// control type AND a chat-like name, and excludes file-named controls — because callers of
        /// this method may run Ctrl+A + Delete on whatever is returned even when it's not currently
        /// focused, so a broader match here could select the code editor.
        /// </summary>
        private static AutomationElement FindCopilotInputElement(AutomationElement root)
        {
            try
            {
                if (root == null) return null;

                var all = root.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                for (int i = 0; i < all.Count; i++)
                {
                    try
                    {
                        var el = all[i];
                        if (!el.Current.IsEnabled) continue;
                        if (!el.Current.IsKeyboardFocusable) continue;

                        string ct = el.Current.ControlType?.ProgrammaticName ?? "";
                        bool likelyEdit = ct.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0
                            || ct.IndexOf("Document", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!likelyEdit) continue;

                        string name = el.Current.Name ?? "";
                        if (LooksLikeSourceFileName(name)) continue;

                        bool nameHint = !string.IsNullOrEmpty(name) && (
                            name.IndexOf("type", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("message", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("chat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("prompt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("copilot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("ask", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (!nameHint) continue;

                        return el;
                    }
                    catch (Exception ex)
                    {
                        Log("UI Automation: FindCopilotInputElement enumeration failed: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("UI Automation: FindCopilotInputElement error: " + ex.Message);
            }
            return null;
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
