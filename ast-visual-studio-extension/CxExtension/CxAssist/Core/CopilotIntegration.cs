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
    /// Reusable: sends a prompt to GitHub Copilot Chat (open via Ctrl+\ then C, new chat, paste, submit).
    /// Used by <see cref="CxAssistCopilotActions"/> for Fix and View details. Message text comes from <see cref="CxAssistConstants"/>.
    /// </summary>
    internal static class CopilotIntegration
    {
        private const int PasteAndSubmitDelayMs = 1000;

        /// <summary>Sends the prompt to Copilot: clipboard, open chat, new chat, paste and submit. Returns true if clipboard was set.</summary>
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
                            MessageBox.Show(CxAssistConstants.CopilotPromptSentMessage, CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.PasteAndSubmitPrompt");
                            MessageBox.Show(CxAssistConstants.CopilotPasteFailedMessage, CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    };
                    timer.Start();
                }
                else
                {
                    MessageBox.Show(CxAssistConstants.CopilotOpenInstructionsMessage, CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }
            catch (Exception ex)
            {
                CxAssistErrorHandler.LogAndSwallow(ex, "CopilotIntegration.SendPromptToCopilot");
                try
                {
                    Clipboard.SetText(prompt);
                    MessageBox.Show(clipboardFallbackMessage ?? CxAssistConstants.CopilotGenericFallbackMessage, CxAssistConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void StartNewChatThenPasteAndSubmit()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Start new conversation (Copilot chat slash command)
            System.Windows.Forms.SendKeys.SendWait("/new{ENTER}");
            System.Threading.Thread.Sleep(400);
            // Paste prompt and submit
            System.Windows.Forms.SendKeys.SendWait("^v{ENTER}");
        }

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
