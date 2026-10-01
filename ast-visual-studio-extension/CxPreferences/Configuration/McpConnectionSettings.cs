namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// How the Checkmarx MCP server entry in .mcp.json authenticates. The OAuth server URL and tenant
    /// are only used in <see cref="McpAuthMode.OAuth"/> mode; blank values are derived from the API key.
    /// </summary>
    internal sealed class McpConnectionSettings
    {
        internal static readonly McpConnectionSettings Default = new McpConnectionSettings(McpAuthMode.ApiKey, null, null);

        public McpConnectionSettings(McpAuthMode authMode, string oauthServerUrl, string oauthTenant)
        {
            AuthMode = authMode;
            OAuthServerUrl = oauthServerUrl?.Trim() ?? string.Empty;
            OAuthTenant = oauthTenant?.Trim() ?? string.Empty;
        }

        public McpAuthMode AuthMode { get; }

        public string OAuthServerUrl { get; }

        public string OAuthTenant { get; }
    }
}
