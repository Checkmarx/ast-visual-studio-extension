namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// How the Checkmarx MCP server entry in .mcp.json authenticates. In <see cref="McpAuthMode.OAuth"/> mode the
    /// server URL and tenant are always derived from the API key used to log in to the plugin.
    /// </summary>
    internal sealed class McpConnectionSettings
    {
        internal static readonly McpConnectionSettings Default = new McpConnectionSettings(McpAuthMode.OAuth);

        public McpConnectionSettings(McpAuthMode authMode)
        {
            AuthMode = authMode;
        }

        public McpAuthMode AuthMode { get; }
    }
}
