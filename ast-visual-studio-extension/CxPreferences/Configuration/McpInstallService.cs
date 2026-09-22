using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Json;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    internal class McpInstallService
    {
        private readonly McpConfigManager _configManager;

        public McpInstallService() : this(new McpConfigManager())
        {
        }

        internal McpInstallService(McpConfigManager configManager)
        {
            _configManager = configManager;
        }

        public Task<McpInstallResult> InstallAsync(CxConfig config, Type ownerType)
        {
            return InstallAsync(config, McpAuthMode.ApiKey, ownerType);
        }

        public Task<McpInstallResult> InstallAsync(CxConfig config, McpAuthMode authMode, Type ownerType)
        {
            return Task.Run(() => Install(config, authMode, ownerType));
        }

        public Task<bool> IsTenantMcpEnabledAsync(CxConfig config, Type ownerType)
        {
            return Task.Run(() => IsTenantMcpEnabled(config, ownerType));
        }

        public Task<bool> InstallSilentlyAsync(CxConfig config, Type ownerType)
        {
            return InstallSilentlyAsync(config, McpAuthMode.ApiKey, ownerType);
        }

        public Task<bool> InstallSilentlyAsync(CxConfig config, McpAuthMode authMode, Type ownerType)
        {
            return Task.Run(() =>
            {
                var result = Install(config, authMode, ownerType, silentMode: true);
                return result.Success;
            });
        }

        private bool IsTenantMcpEnabled(CxConfig config, Type ownerType)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
                return false;

            try
            {
                var wrapper = new CxCLI.CxWrapper(config, ownerType ?? GetType());
                wrapper.AuthValidate();
                return IsTenantMcpEnabled(wrapper, out _);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return true; // fail-open, but avoid catching critical system exceptions
            }
        }

        private McpInstallResult Install(CxConfig config, Type ownerType)
        {
            return Install(config, McpAuthMode.ApiKey, ownerType, silentMode: false);
        }

        private McpInstallResult Install(CxConfig config, McpAuthMode authMode, Type ownerType)
        {
            return Install(config, authMode, ownerType, silentMode: false);
        }

        private McpInstallResult Install(CxConfig config, McpAuthMode authMode, Type ownerType, bool silentMode)
        {
            if (config == null)
            {
                return new McpInstallResult
                {
                    Success = false,
                    Message = "Missing configuration."
                };
            }

            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return new McpInstallResult
                {
                    Success = false,
                    Message = "Please authenticate first before installing MCP."
                };
            }

            try
            {
                var wrapper = new CxCLI.CxWrapper(config, ownerType ?? GetType());
                wrapper.AuthValidate();

                if (!IsTenantMcpEnabled(wrapper, out bool checkFailed))
                {
                    return new McpInstallResult
                    {
                        Success = false,
                        Skipped = true,
                        Message = checkFailed && silentMode
                            ? ""
                            : "MCP is disabled by your tenant settings."
                    };
                }

                string mcpUrl = authMode == McpAuthMode.OAuth
                    ? ResolveMcpUrlForOAuth(config.ApiKey)
                    : ResolveMcpUrl(config.ApiKey);
                bool changed;
                string configPath;
                if (authMode == McpAuthMode.OAuth)
                    changed = _configManager.InstallOrUpdateOAuth(mcpUrl, out configPath);
                else
                    changed = _configManager.InstallOrUpdate(config.ApiKey, mcpUrl, out configPath);

                return new McpInstallResult
                {
                    Success = true,
                    Changed = changed,
                    ConfigPath = configPath,
                    Message = changed
                        ? (authMode == McpAuthMode.OAuth
                            ? "MCP configuration installed. Visual Studio will prompt you to sign in via your browser the first time the Checkmarx MCP server is used."
                            : "MCP configuration installed successfully.")
                        : "MCP configuration is already up to date."
                };
            }
            catch (Exception ex)
            {
                return new McpInstallResult
                {
                    Success = false,
                    Message = "Failed to install MCP: " + ex.Message
                };
            }
        }

        public bool Uninstall(out string message)
        {
            try
            {
                bool changed = _configManager.RemoveCheckmarxServer(out string configPath);
                message = changed
                    ? "Removed Checkmarx MCP configuration from " + configPath
                    : "No Checkmarx MCP configuration found.";
                return changed;
            }
            catch (Exception ex)
            {
                message = "Failed to remove MCP configuration: " + ex.Message;
                return false;
            }
        }

        internal static string ResolveMcpUrl(string apiKey)
        {
            try
            {
                string issuer = TryGetIssuer(apiKey);
                if (string.IsNullOrWhiteSpace(issuer))
                    return McpConfigManager.DefaultMcpUrl;

                if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
                    return McpConfigManager.DefaultMcpUrl;

                string authority = issuerUri.Authority.Replace("iam.", "ast.");
                return issuerUri.Scheme + "://" + authority + "/api/security-mcp/mcp";
            }
            catch
            {
                return McpConfigManager.DefaultMcpUrl;
            }
        }

        /// <summary>
        /// OAuth-mode variant of <see cref="ResolveMcpUrl"/>. The MCP endpoint is served from a
        /// different host than the API-key JWT issuer (the IAM host), so the issuer host must be
        /// mapped. Kept separate so the existing API-key path's URL resolution is unchanged.
        /// </summary>
        internal static string ResolveMcpUrlForOAuth(string apiKey)
        {
            try
            {
                string issuer = TryGetIssuer(apiKey);
                if (string.IsNullOrWhiteSpace(issuer))
                    return McpConfigManager.DefaultMcpUrl;

                if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
                    return McpConfigManager.DefaultMcpUrl;

                string authority = ResolveMcpAuthority(issuerUri.Authority);
                return issuerUri.Scheme + "://" + authority + "/api/security-mcp/mcp";
            }
            catch
            {
                return McpConfigManager.DefaultMcpUrl;
            }
        }

        /// <summary>
        /// Maps the IAM host to the host that serves the security MCP endpoint.
        /// On Checkmarx cloud environments (&lt;env&gt;.cxast.net) the MCP endpoint is served from the
        /// "ast-master-components" host in place of the IAM host (e.g.
        /// iam-dev.dev.cxast.net -&gt; ast-master-components.dev.cxast.net). A naive "iam" -&gt; "ast"
        /// label swap yields ast-dev.dev.cxast.net, which does not resolve in DNS. Other (non-cxast)
        /// environments keep the legacy "iam" -&gt; "ast" mapping.
        /// </summary>
        private static string ResolveMcpAuthority(string authority)
        {
            Match cxastMatch = Regex.Match(authority, @"^iam(?:-[^.]+)?\.(?<envAndDomain>[^.]+\.cxast\.net)$", RegexOptions.IgnoreCase);
            if (cxastMatch.Success)
                return "ast-master-components." + cxastMatch.Groups["envAndDomain"].Value;

            return Regex.Replace(authority, "^iam(?=[.-])", "ast");
        }

        private static string TryGetIssuer(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string[] parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            string payload = DecodeBase64Url(parts[1]);
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            JsonValue parsed = JsonValue.Parse(payload);
            JsonObject payloadObj = parsed as JsonObject;
            if (payloadObj == null)
                return null;

            JsonValue issuerValue = payloadObj["iss"];
            return issuerValue?.ToString().Trim('"');
        }

        /// <summary>
        /// Delegates to <see cref="CxCLI.CxWrapper.AiMcpServerEnabled"/>, which mirrors the
        /// VSCode JS wrapper's aiMcpServerEnabled() — a dedicated lookup of key
        /// "scan.config.plugins.aiMcpServer" in tenant settings.
        /// </summary>
        private static bool IsTenantMcpEnabled(CxCLI.CxWrapper wrapper, out bool checkFailed)
        {
            checkFailed = false;
            try
            {
                return wrapper.AiMcpServerEnabled();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                checkFailed = true;
                return true; // fail-open: don't block install on API errors
            }
        }

        private static string DecodeBase64Url(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            byte[] bytes = Convert.FromBase64String(padded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
