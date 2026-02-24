using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using ast_visual_studio_extension.CxExtension.DevAssist.Services;
using ast_visual_studio_extension.CxWrapper.Models;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Commands
{
    public class AIChatCommand
    {
        public const int CommandId = 0x0300;
        public static readonly Guid CommandSet = new Guid("e46cd6d8-268d-4e77-9074-071e72a25f39");

        private readonly AsyncPackage _package;
        private readonly CopilotIntegration _copilotService;
        private readonly PromptBuilderService _promptBuilder;

        private AIChatCommand(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _copilotService = new CopilotIntegration();
            _promptBuilder = new PromptBuilderService();
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var command = new AIChatCommand(package);
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(command.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Execute AI Chat command with vulnerability data
        /// </summary>
        private async void Execute(object sender, EventArgs e)
        {
            try
            {
                // Get vulnerability data from command parameter or context
                var vulnerabilityData = GetVulnerabilityDataFromContext(e);

                if (vulnerabilityData == null)
                {
                    System.Windows.MessageBox.Show("No vulnerability data available for AI assistance.",
                        "AI Chat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Build appropriate prompt based on vulnerability type
                string prompt = BuildPromptForVulnerability(vulnerabilityData);

                // Open Copilot with the prompt
                bool success = await _copilotService.OpenCopilotChatAsync(prompt, _package);

                if (!success)
                {
                    System.Windows.MessageBox.Show("Failed to open GitHub Copilot. Please ensure it's installed and try again.",
                        "AI Chat Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening AI Chat: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Public method to execute AI chat with specific vulnerability data
        /// </summary>
        public async Task ExecuteWithDataAsync(CxAscaDetail ascaData)
        {
            try
            {
                string prompt = _promptBuilder.BuildASCAPrompt(
                    ascaData.RuleName,
                    ascaData.RemediationAdvise,
                    ascaData.Severity,
                    ascaData.FileName,
                    ascaData.Line
                );

                await _copilotService.OpenCopilotChatAsync(prompt, _package);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening AI Chat: {ex.Message}",
                    "AI Chat Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Public method to execute AI chat with SCA vulnerability data
        /// </summary>
        public async Task ExecuteWithSCADataAsync(string packageName, string version, string severity, string packageManager = null)
        {
            try
            {
                string prompt = _promptBuilder.BuildSCAPrompt(packageName, version, severity, packageManager);
                await _copilotService.OpenCopilotChatAsync(prompt, _package);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening AI Chat: {ex.Message}",
                    "AI Chat Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private object GetVulnerabilityDataFromContext(EventArgs e)
        {
            // This would extract vulnerability data from the command context
            // Implementation depends on how data is passed to the command
            // For now, return null - will be implemented based on UI integration
            return null;
        }

        private string BuildPromptForVulnerability(object vulnerabilityData)
        {
            // Build prompt based on vulnerability data type
            if (vulnerabilityData is CxAscaDetail ascaData)
            {
                return _promptBuilder.BuildASCAPrompt(
                    ascaData.RuleName,
                    ascaData.RemediationAdvise,
                    ascaData.Severity,
                    ascaData.FileName,
                    ascaData.Line
                );
            }

            // Add other vulnerability types as needed
            return _promptBuilder.BuildGenericPrompt("Unknown vulnerability", "Unknown");
        }
    }
}
