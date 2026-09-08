using System;
using System.Diagnostics;
#if !NO_VS_EXTENSION_MANAGER
using Microsoft.VisualStudio.ExtensionManager;
#endif

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
#if !NO_VS_EXTENSION_MANAGER
    /// <summary>
    /// Handles MCP cleanup when the Checkmarx extension is uninstalled.
    /// Mirrors McpUninstallHandler (DynamicPluginListener) in the JetBrains plugin.
    ///
    /// Subscribes to IVsExtensionManager.InstallCompleted and removes the
    /// Checkmarx MCP server entry from mcp.json when extension uninstall completes.
    /// </summary>
    internal sealed class McpUninstallHandler
    {
        // Extension identity from source.extension.vsixmanifest
        private const string ExtensionId = "ast_visual_studio_extension.ee50149d-e5ee-4be4-9b84-fecf58ef2360";

        private readonly IVsExtensionManager _extensionManager;

        internal McpUninstallHandler(IVsExtensionManager extensionManager)
        {
            _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
            _extensionManager.InstallCompleted += OnInstallCompleted;
        }

        private void OnInstallCompleted(object sender, InstallCompletedEventArgs e)
        {
            // Only act when our extension uninstall has completed.
            if (e.State != InstallState.Uninstalled)
                return;

            if (!string.Equals(e.Extension?.Header?.Identifier, ExtensionId, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var service = new McpInstallService();
                service.Uninstall(out string message);
                Debug.WriteLine($"[Checkmarx] MCP cleanup on extension uninstall: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Checkmarx] MCP cleanup on extension uninstall failed: {ex.Message}");
            }
        }
    }
#endif
}
