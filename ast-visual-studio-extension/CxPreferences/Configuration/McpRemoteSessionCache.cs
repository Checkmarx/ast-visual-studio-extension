using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// Clears the OAuth tokens that mcp-remote caches for the Checkmarx OAuth MCP entry.
    /// The MCP server answers a stale/expired bearer token with a 401 that carries no WWW-Authenticate
    /// header, so mcp-remote cannot rediscover the authorization server and sends the browser to a
    /// non-existent "/authorize" page (HTTP 404). Without cached tokens the first request receives the
    /// full challenge and the DCR sign-in works again.
    /// </summary>
    internal class McpRemoteSessionCache
    {
        // Must match the custom headers of McpConfigManager.BuildCheckmarxServerOAuth, serialized the way
        // mcp-remote does (JSON.stringify with sorted keys).
        private const string OAUTH_ENTRY_HEADERS_JSON = "{\"cx-origin\":\"VisualStudio\"}";
        private const string TOKENS_FILE_SUFFIX = "_tokens.json";

        private readonly string _authRoot;

        public McpRemoteSessionCache()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcp-auth"))
        {
        }

        internal McpRemoteSessionCache(string authRoot)
        {
            _authRoot = authRoot;
        }

        /// <summary>
        /// mcp-remote keys its cache by md5("&lt;server url&gt;|&lt;headers json&gt;"). MD5 is required for
        /// compatibility with that file naming; it is not used for any security purpose.
        /// </summary>
        internal static string ComputeCacheKey(string mcpUrl)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(mcpUrl + "|" + OAUTH_ENTRY_HEADERS_JSON));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <returns>The number of cached token files removed.</returns>
        public virtual int ClearTokens(string mcpUrl)
        {
            return RemoveTokenFiles(mcpUrl, _ => true);
        }

        /// <summary>
        /// Removes the cached session only when it can no longer work: the access token has expired and the
        /// refresh token (a Keycloak JWT bound to the SSO session, ~5-8 h) has expired too. mcp-remote would
        /// otherwise keep replaying the dead token and hit the header-less 401 described on this class.
        /// A session whose refresh token cannot be read is left alone.
        /// </summary>
        /// <returns>The number of cached token files removed.</returns>
        public virtual int ClearExpiredTokens(string mcpUrl)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return RemoveTokenFiles(mcpUrl, tokensFile => IsSessionExpired(File.ReadAllText(tokensFile), now));
        }

        internal static bool IsSessionExpired(string tokensJson, DateTimeOffset now)
        {
            JObject tokens;
            try
            {
                tokens = JObject.Parse(tokensJson);
            }
            catch (JsonException)
            {
                return false;
            }

            DateTimeOffset? accessExpiry = TryGetJwtExpiry(tokens.Value<string>("access_token"));
            long? expiresAtMs = tokens.Value<long?>("expires_at");
            if (accessExpiry == null && expiresAtMs != null)
                accessExpiry = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs.Value);

            if (accessExpiry == null || accessExpiry > now)
                return false;

            string refreshToken = tokens.Value<string>("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
                return true;

            DateTimeOffset? refreshExpiry = TryGetJwtExpiry(refreshToken);
            return refreshExpiry != null && refreshExpiry <= now;
        }

        private static DateTimeOffset? TryGetJwtExpiry(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
                return null;

            string[] parts = jwt.Split('.');
            if (parts.Length < 2)
                return null;

            try
            {
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                JObject claims = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                long? exp = claims.Value<long?>("exp");
                return exp == null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeSeconds(exp.Value);
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException || ex is ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private int RemoveTokenFiles(string mcpUrl, Func<string, bool> shouldRemove)
        {
            if (string.IsNullOrWhiteSpace(mcpUrl) || !Directory.Exists(_authRoot))
                return 0;

            string tokensFileName = ComputeCacheKey(mcpUrl) + TOKENS_FILE_SUFFIX;
            int removed = 0;

            try
            {
                // One folder per mcp-remote storage version (e.g. "mcp-remote-v1", older "mcp-remote-0.1.x").
                foreach (string versionDir in Directory.GetDirectories(_authRoot, "mcp-remote-*"))
                {
                    string tokensFile = Path.Combine(versionDir, tokensFileName);
                    if (File.Exists(tokensFile) && shouldRemove(tokensFile))
                    {
                        File.Delete(tokensFile);
                        removed++;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Best effort: a failure here must not fail the MCP install that already succeeded.
                System.Diagnostics.Debug.WriteLine($"Failed to clear cached MCP OAuth tokens: {ex.Message}");
            }

            return removed;
        }
    }
}
