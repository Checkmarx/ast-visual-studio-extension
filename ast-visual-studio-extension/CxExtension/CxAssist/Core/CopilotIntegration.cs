using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Integrates with GitHub Copilot Chat: opens chat via default VS shortcut (Ctrl+\ then C), pastes the prompt, and submits.
    /// Aligned with PR #304 (DevAssist/Services/CopilotIntegration.cs): same keyboard shortcut and clipboard + SendKeys flow.
    /// </summary>
    internal static class CopilotIntegration
    {
        /// <summary>Delay in ms after opening Copilot before pasting and submitting (PR #304 uses 1000).</summary>
        private const int PasteAndSubmitDelayMs = 1000;

        /// <summary>
        /// Opens Copilot Chat, pastes the prompt into the chat input, and submits it (new chat in agent mode when supported).
        /// Prompt is always copied to clipboard as fallback. If opening or submit fails, user is told to paste manually.
        /// </summary>
        /// <param name="prompt">The prompt text to send (fix or view-details).</param>
        /// <param name="clipboardFallbackMessage">Message when only clipboard is used (e.g. "Prompt copied. Paste into GitHub Copilot Chat.").</param>
        /// <returns>True if clipboard was set; Copilot may or may not have opened and submitted.</returns>
        public static bool SendPromptToCopilot(string prompt, string clipboardFallbackMessage)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Clipboard.SetText(prompt);

                bool opened = TryOpenCopilotChat();
                if (opened)
                {
                    // Schedule: ensure new chat, then paste + submit after the chat UI is ready.
                    var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
                    {
                        Interval = TimeSpan.FromMilliseconds(PasteAndSubmitDelayMs)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        try
                        {
                            StartNewChatThenPasteAndSubmit();
                            MessageBox.Show(
                                "Prompt was sent to GitHub Copilot Chat. Check the chat for the response.",
                                CxAssistConstants.DisplayName,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.PasteAndSubmitPrompt");
                            MessageBox.Show(
                                "Copilot Chat was opened but the prompt could not be sent automatically. The prompt is on your clipboard—click in the chat box, paste (Ctrl+V), then press Enter.",
                                CxAssistConstants.DisplayName,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    };
                    timer.Start();
                }
                else
                {
                    MessageBox.Show(
                        "Prompt copied to clipboard. Open GitHub Copilot Chat (View → GitHub Copilot Chat, or press Alt+Ctrl+Enter), then paste (Ctrl+V) and press Enter to get assistance.",
                        CxAssistConstants.DisplayName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return true;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.SendPromptToCopilot");
                try
                {
                    Clipboard.SetText(prompt);
                    MessageBox.Show(clipboardFallbackMessage ?? "Prompt copied to clipboard. Paste into GitHub Copilot Chat.", CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Starts a new Copilot chat (slash command /new), then pastes from clipboard and submits.
        /// Ensures each Fix/View details action gets a fresh chat.
        /// </summary>
        private static void StartNewChatThenPasteAndSubmit()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Start new conversation (Copilot chat slash command)
            System.Windows.Forms.SendKeys.SendWait("/new{ENTER}");
            System.Threading.Thread.Sleep(400);
            // Paste prompt and submit
            System.Windows.Forms.SendKeys.SendWait("^v{ENTER}");
        }

        /// <summary>
        /// Opens GitHub Copilot Chat using the default Visual Studio shortcut (Ctrl+\ then C), per PR #304.
        /// Fallback: DTE.ExecuteCommand with known command names.
        /// </summary>
        private static bool TryOpenCopilotChat()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // PR #304: default keyboard shortcut (Ctrl+\ then C) to open GitHub Copilot Chat
                try
                {
                    System.Windows.Forms.SendKeys.SendWait("^\\c");
                    return true;
                }
                catch
                {
                    // SendKeys failed, try DTE commands
                }

                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte == null) return false;

                foreach (var commandName in new[] { "Edit.Copilot.Open", "View.CopilotChat", "GitHub.Copilot.Chat.Show" })
                {
                    try
                    {
                        dte.ExecuteCommand(commandName);
                        return true;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.TryOpenCopilotChat");
            }

            return false;
        }
    }
}
