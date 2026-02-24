using ast_visual_studio_extension.CxExtension.Utils;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Services
{
    public class CopilotIntegration
    {
        /// <summary>
        /// Optional package set by the hosting VS package (e.g. CxWindowPackage) so that
        /// OpenCopilotChatAsync can run extension-manager checks when called without an explicit package (e.g. from ASCA marker).
        /// </summary>
        public static AsyncPackage Package { get; set; }

        private DTE2 _dte;
        private OutputWindowPane _outputPane;

        public CopilotIntegration()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
        }

        private void Log(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_dte == null)
                {
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [Copilot] {message}");
                    return;
                }
                if (_outputPane == null)
                    _outputPane = OutputPaneUtils.InitializeOutputPane(_dte.ToolWindows.OutputWindow, CxConstants.EXTENSION_TITLE);
                _outputPane?.OutputString($"{DateTime.Now:HH:mm:ss.fff} [Copilot] {message}\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CopilotIntegration] Log failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Detect if Copilot Chat extension is installed (GitHub.CopilotChat VSIX).
        /// Returns true if the Copilot Chat extension is in the list of installed extensions.
        /// </summary>
        public async Task<bool> IsCopilotChatEnabledAsync(AsyncPackage package)
        {
            if (package == null)
            {
                Log("[IsCopilotChatEnabled] package is null -> false");
                return false;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var extensionManager = await package.GetServiceAsync(typeof(SVsExtensionManager)) as IVsExtensionManager;
            if (extensionManager == null)
            {
                Log("[IsCopilotChatEnabled] IVsExtensionManager is null -> false");
                return false;
            }

            const string CopilotChatVsixId = "Component.VisualStudio.GitHub.Copilot";

            bool found = extensionManager.GetInstalledExtensions()
                .Any(e => e.Header.Identifier.Equals(CopilotChatVsixId, StringComparison.OrdinalIgnoreCase));

            Log(found ? "[IsCopilotChatEnabled] Copilot Chat extension installed -> true" : "[IsCopilotChatEnabled] Copilot Chat extension not found -> false");
            return found;
        }

        /// <summary>
        /// Check if GitHub Copilot for Visual Studio is installed and Chat is available (legacy/alias).

        public async Task<bool> IsCopilotInstalledAsync(AsyncPackage package)
        {
            return await IsCopilotChatEnabledAsync(package);
        }

        ///  Open GitHub Copilot Chat with optional prompt.
        public async Task<bool> OpenCopilotChatAsync(string prompt = null, AsyncPackage package = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AsyncPackage packageToUse = package ?? Package;
            if (packageToUse == null)
                Log("[OpenCopilotChatAsync] WARNING: package is null and CopilotIntegration.Package was not set (e.g. CxWindowPackage should set it)");

            try
            {
                bool success = await TryOpenCopilotChatAsync(packageToUse);

                if (success && !string.IsNullOrEmpty(prompt))
                {
                    await Task.Delay(1000);
                    await SendPromptToCopilotAsync(prompt);
                }
                else
                {
                    if (string.IsNullOrEmpty(prompt)) Log("[OpenCopilotChatAsync] Skipping prompt: no prompt");
                }
                return success;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to open GitHub Copilot: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> TryOpenCopilotChatAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // default keyboard shortcut (Ctrl+Backslash then C) to open GitHub Copilot Chat
            try
            {
                System.Windows.Forms.SendKeys.SendWait("^\\c");
                Log("[TryOpenCopilotChatAsync] Default shortcut (Ctrl+\\ then C) sent");
                return true;
            }
            catch (Exception ex)
            {
                Log($"[TryOpenCopilotChatAsync] Shortcut fallback failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Enumerates DTE commands whose name contains "Copilot" or "GitHub" and logs them to Output.
        /// Use this to find the exact command name that opens Copilot Chat on your machine.
        /// </summary>

        private async Task SendPromptToCopilotAsync(string prompt)
        {
            Log("[SendPromptToCopilotAsync] Enter");
            Log($"[SendPromptToCopilotAsync] prompt length={prompt?.Length ?? 0}");
            try
            {
                Log("[SendPromptToCopilotAsync] Method 1: clipboard + paste");
                Clipboard.SetText(prompt);
                Log("[SendPromptToCopilotAsync] Clipboard.SetText done, waiting 100ms");
                await Task.Delay(100);
                System.Windows.Forms.SendKeys.SendWait("^v{ENTER}");
                Log("[SendPromptToCopilotAsync] SendKeys ^v{ENTER} sent");
            }
            catch (Exception ex)
            {
                Log($"[SendPromptToCopilotAsync] Method 1 failed: {ex.Message}");
                Log("[SendPromptToCopilotAsync] Method 2: direct SendKeys (fallback)");
                try
                {
                    System.Windows.Forms.SendKeys.SendWait(prompt.Replace("{", "{{").Replace("}", "}}"));
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    Log("[SendPromptToCopilotAsync] Method 2 completed");
                }
                catch (Exception ex2)
                {
                    Log($"[SendPromptToCopilotAsync] Method 2 failed: {ex2.Message}");
                }
            }
        }
        private void ShowError(string message)
        {
            Log($"[ShowError] {message}");
            MessageBox.Show(message, "Copilot Integration Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
