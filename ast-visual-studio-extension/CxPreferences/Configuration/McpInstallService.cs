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
        private const string MCP_PATH = "/api/security-mcp/mcp";

        private static readonly Regex TenantPattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$");
        private static readonly Regex SafeAuthorityPattern = new Regex(@"^[A-Za-z0-9.-]+(:[0-9]{1,5})?$");
        private static readonly Regex SafePathPattern = new Regex(@"^(/[A-Za-z0-9._~-]+)*$");
        private static readonly Regex SafeOAuthMcpUrlPattern = new Regex(@"^https://[A-Za-z0-9.-]+(:[0-9]{1,5})?(/[A-Za-z0-9._~-]+)+$");
        private static readonly Regex IamLabelPattern = new Regex(@"(?<prefix>^|\.)iam(?=[.-])", RegexOptions.IgnoreCase);

        private readonly McpConfigManager _configManager;
        private readonly McpRemoteSessionCache _sessionCache;
        private readonly Func<bool> _isNpxAvailable;

        public McpInstallService() : this(new McpConfigManager())
        {
        }

        internal McpInstallService(McpConfigManager configManager)
            : this(configManager, new McpRemoteSessionCache())
        {
        }

        internal McpInstallService(McpConfigManager configManager, McpRemoteSessionCache sessionCache)
            : this(configManager, sessionCache, McpPrerequisites.IsNpxAvailable)
        {
        }

        internal McpInstallService(McpConfigManager configManager, McpRemoteSessionCache sessionCache, Func<bool> isNpxAvailable)
        {
            _configManager = configManager;
            _sessionCache = sessionCache;
            _isNpxAvailable = isNpxAvailable;
        }

        public Task<McpInstallResult> InstallAsync(CxConfig config, Type ownerType)
        {
            return InstallAsync(config, McpConnectionSettings.Default, ownerType);
        }

        public Task<McpInstallResult> InstallAsync(CxConfig config, McpConnectionSettings settings, Type ownerType)
        {
            return Task.Run(() => Install(config, settings, ownerType));
        }

        public Task<bool> IsTenantMcpEnabledAsync(CxConfig config, Type ownerType)
        {
            return Task.Run(() => IsTenantMcpEnabled(config, ownerType));
        }

        public Task<bool> InstallSilentlyAsync(CxConfig config, Type ownerType)
        {
            return InstallSilentlyAsync(config, McpConnectionSettings.Default, ownerType);
        }

        public Task<bool> InstallSilentlyAsync(CxConfig config, McpConnectionSettings settings, Type ownerType)
        {
            return Task.Run(() =>
            {
                var result = Install(config, settings, ownerType, silentMode: true);
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
            return Install(config, McpConnectionSettings.Default, ownerType, silentMode: false);
        }

        private McpInstallResult Install(CxConfig config, McpConnectionSettings settings, Type ownerType)
        {
            return Install(config, settings, ownerType, silentMode: false);
        }

        private McpInstallResult Install(CxConfig config, McpConnectionSettings settings, Type ownerType, bool silentMode)
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

            // Both entry shapes run through npx. Silent installs keep writing the entry (as before) so it works
            // once Node.js is installed; an explicit install explains why VS would fail to start it.
            if (!silentMode && !_isNpxAvailable())
            {
                return new McpInstallResult
                {
                    Success = false,
                    Message = McpPrerequisites.NPX_MISSING_MESSAGE
                };
            }

            bool isOAuth = (settings ?? McpConnectionSettings.Default).AuthMode == McpAuthMode.OAuth;
            string mcpUrl;
            if (isOAuth)
            {
                // Validate the configured OAuth server URL / tenant before spending CLI round-trips.
                if (!TryResolveMcpUrlForOAuth(config.ApiKey, settings.OAuthServerUrl, settings.OAuthTenant, out mcpUrl, out string urlError))
                {
                    return new McpInstallResult
                    {
                        Success = false,
                        Message = urlError
                    };
                }
            }
            else
            {
                mcpUrl = ResolveMcpUrl(config.ApiKey);
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

                string previousOAuthUrl = _configManager.GetInstalledOAuthServerUrl();

                bool changed;
                string configPath;
                if (isOAuth)
                    changed = _configManager.InstallOrUpdateOAuth(mcpUrl, out configPath);
                else
                    changed = _configManager.InstallOrUpdate(config.ApiKey, mcpUrl, out configPath);

                // A replaced OAuth entry (mode switched or URL changed) must not leave its sign-in behind.
                if (previousOAuthUrl != null && !(isOAuth && string.Equals(previousOAuthUrl, mcpUrl, StringComparison.OrdinalIgnoreCase)))
                    _sessionCache.ClearTokens(previousOAuthUrl);

                if (isOAuth)
                {
                    // The server answers a dead cached token with a 401 lacking WWW-Authenticate, which sends the
                    // browser to a 404 page. Silent installs (VS start, login) drop only sessions that can no longer
                    // work; an explicit install is a user-initiated reconnect and starts a fresh sign-in.
                    if (silentMode)
                        _sessionCache.ClearExpiredTokens(mcpUrl);
                    else
                        _sessionCache.ClearTokens(mcpUrl);
                }

                return new McpInstallResult
                {
                    Success = true,
                    Changed = changed,
                    ConfigPath = configPath,
                    McpUrl = mcpUrl,
                    Message = BuildInstallMessage(isOAuth, changed, mcpUrl)
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

        private static string BuildInstallMessage(bool isOAuth, bool changed, string mcpUrl)
        {
            if (!isOAuth)
                return changed ? "MCP configuration installed successfully." : "MCP configuration is already up to date.";

            return changed
                ? "MCP configuration installed for " + mcpUrl + "."
                : "MCP configuration for " + mcpUrl + " is already up to date.";
        }

        public bool Uninstall(out string message)
        {
            try
            {
                string oauthUrl = _configManager.GetInstalledOAuthServerUrl();
                bool changed = _configManager.RemoveCheckmarxServer(out string configPath);

                // Logging out must also end the OAuth sign-in, which mcp-remote keeps outside .mcp.json.
                if (oauthUrl != null)
                    _sessionCache.ClearTokens(oauthUrl);

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

                // The IAM host does not serve MCP (404); map it the same way as OAuth mode. The API-key entry
                // keeps the base endpoint, which accepts API keys.
                return issuerUri.Scheme + "://" + ResolveMcpAuthority(issuerUri.Authority) + MCP_PATH;
            }
            catch
            {
                return McpConfigManager.DefaultMcpUrl;
            }
        }

        /// <summary>
        /// Validates only the user-entered OAuth Server URL / Tenant; blank values are valid (derived later).
        /// </summary>
        internal static bool TryValidateOAuthOverrides(string serverUrl, string tenant, out string error)
        {
            return TryParseOAuthOverrides(serverUrl, tenant, out _, out _, out error);
        }

        /// <summary>
        /// Builds the tenant-scoped OAuth MCP URL "&lt;server&gt;/api/security-mcp/mcp/&lt;tenant&gt;".
        /// A configured server URL / tenant wins; a blank one is derived from the API-key JWT issuer.
        /// There is deliberately no fallback URL: the base endpoint cannot complete OAuth (its protected-resource
        /// metadata lists no authorization server) and a guessed host would point users at the wrong environment.
        /// </summary>
        /// <returns>false (with a user-facing <paramref name="error"/>) when a value is invalid or cannot be determined.</returns>
        internal static bool TryResolveMcpUrlForOAuth(string apiKey, string serverUrl, string tenant, out string mcpUrl, out string error)
        {
            mcpUrl = null;
            if (!TryParseOAuthOverrides(serverUrl, tenant, out string serverBase, out string resolvedTenant, out error))
                return false;

            if (serverBase == null || resolvedTenant == null)
            {
                Uri issuerUri = TryGetIssuerUri(apiKey);
                if (issuerUri != null)
                {
                    serverBase ??= issuerUri.Scheme + "://" + ResolveMcpAuthority(issuerUri.Authority);
                    resolvedTenant ??= TryGetRealm(issuerUri);
                }
            }

            if (serverBase == null || resolvedTenant == null)
            {
                error = "Could not determine the MCP " + (serverBase == null ? "server URL" : "tenant")
                    + " from your API key. Enter the Server URL and Tenant for OAuth.";
                return false;
            }

            string candidate = serverBase + MCP_PATH + "/" + resolvedTenant;
            if (!IsSafeOAuthMcpUrl(candidate))
            {
                error = "Could not determine a valid https MCP URL from your API key. Enter the Server URL and Tenant for OAuth.";
                return false;
            }

            mcpUrl = candidate;
            return true;
        }

        /// <summary>
        /// Shape of every OAuth MCP URL this extension writes or launches. It is placed on a cmd.exe command line
        /// (npx is a .cmd shim), so only characters that are inert for cmd are allowed.
        /// </summary>
        internal static bool IsSafeOAuthMcpUrl(string mcpUrl)
        {
            return !string.IsNullOrWhiteSpace(mcpUrl) && SafeOAuthMcpUrlPattern.IsMatch(mcpUrl);
        }

        private static bool TryParseOAuthOverrides(string serverUrl, string tenant, out string serverBase, out string resolvedTenant, out string error)
        {
            serverBase = null;
            resolvedTenant = null;
            error = null;

            string tenantFromUrl = null;
            if (!string.IsNullOrWhiteSpace(serverUrl) && !TryNormalizeServerUrl(serverUrl, out serverBase, out tenantFromUrl))
            {
                error = "Invalid MCP server URL. Enter your Checkmarx One https:// URL, e.g. https://eu.ast.checkmarx.net.";
                return false;
            }

            resolvedTenant = string.IsNullOrWhiteSpace(tenant) ? tenantFromUrl : tenant.Trim();
            if (resolvedTenant != null && !TenantPattern.IsMatch(resolvedTenant))
            {
                error = "Invalid tenant name. Use only letters, digits, '.', '_' or '-'.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Accepts either the Checkmarx One base URL or a pasted MCP endpoint URL
        /// (".../api/security-mcp/mcp[/&lt;tenant&gt;]") and returns the base without a trailing slash.
        /// </summary>
        private static bool TryNormalizeServerUrl(string serverUrl, out string serverBase, out string tenantFromUrl)
        {
            serverBase = null;
            tenantFromUrl = null;

            string candidate = serverUrl.Trim();
            if (!candidate.Contains("://"))
                candidate = "https://" + candidate;

            // mcp-remote refuses plain http for non-localhost servers, so only https is usable here.
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment)
                || !SafeAuthorityPattern.IsMatch(uri.Authority))
                return false;

            string path = uri.AbsolutePath.TrimEnd('/');
            int mcpPathIndex = path.IndexOf(MCP_PATH, StringComparison.OrdinalIgnoreCase);
            int mcpPathEnd = mcpPathIndex + MCP_PATH.Length;
            if (mcpPathIndex >= 0 && (path.Length == mcpPathEnd || path[mcpPathEnd] == '/'))
            {
                string rest = path.Substring(mcpPathEnd).Trim('/');
                tenantFromUrl = rest.Length == 0 ? null : rest;
                path = path.Substring(0, mcpPathIndex);
            }

            // The URL ends up on a command line (npx), so only plain path characters are accepted.
            if (!SafePathPattern.IsMatch(path))
                return false;

            serverBase = uri.Scheme + "://" + uri.Authority + path;
            return true;
        }

        private static Uri TryGetIssuerUri(string apiKey)
        {
            try
            {
                string issuer = TryGetIssuer(apiKey);
                return !string.IsNullOrWhiteSpace(issuer) && Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri)
                    ? issuerUri
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Maps the IAM host to the host that serves the security MCP endpoint (the IAM host itself returns 404).
        /// On Checkmarx cloud environments (&lt;env&gt;.cxast.net) the MCP endpoint is served from the
        /// "ast-master-components" host in place of the IAM host (e.g.
        /// iam-dev.dev.cxast.net -&gt; ast-master-components.dev.cxast.net). A naive "iam" -&gt; "ast"
        /// label swap yields ast-dev.dev.cxast.net, which does not resolve in DNS. Other environments swap
        /// the "iam" DNS label wherever it sits (iam.checkmarx.net -&gt; ast.checkmarx.net,
        /// eu.iam.checkmarx.net -&gt; eu.ast.checkmarx.net).
        /// </summary>
        private static string ResolveMcpAuthority(string authority)
        {
            Match cxastMatch = Regex.Match(authority, @"^iam(?:-[^.]+)?\.(?<envAndDomain>[^.]+\.cxast\.net)$", RegexOptions.IgnoreCase);
            if (cxastMatch.Success)
                return "ast-master-components." + cxastMatch.Groups["envAndDomain"].Value;

            return IamLabelPattern.Replace(authority, "${prefix}ast", 1);
        }

        /// <summary>
        /// Extracts the Keycloak realm from the issuer URI (e.g. "/auth/realms/master-sypher").
        /// The tenant-scoped MCP endpoint ("/api/security-mcp/mcp/&lt;realm&gt;") is the only one whose
        /// Protected Resource Metadata advertises an authorization server, which is required for
        /// Visual Studio / GitHub Copilot OAuth DCR discovery.
        /// </summary>
        private static string TryGetRealm(Uri issuerUri)
        {
            if (issuerUri == null || string.IsNullOrWhiteSpace(issuerUri.AbsolutePath))
                return null;

            string[] segments = issuerUri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int realmsIndex = Array.FindIndex(segments, s => string.Equals(s, "realms", StringComparison.OrdinalIgnoreCase));
            if (realmsIndex >= 0 && realmsIndex + 1 < segments.Length)
            {
                string realm = segments[realmsIndex + 1];
                return string.IsNullOrWhiteSpace(realm) ? null : realm;
            }

            return null;
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
