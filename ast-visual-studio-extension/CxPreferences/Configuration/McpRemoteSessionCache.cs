using System;
using System.IO;
using System.Linq;
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
        private const string TOKENS_FILE_PATTERN = "*_tokens.json";
        private const string CLIENT_INFO_FILE_PATTERN = "*_client_info.json";

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
        /// mcp-remote names its cache files after a hash of the server URL, so the entry is matched on content
        /// instead: the Keycloak realm in the token issuer is the tenant, which is the last segment of the
        /// tenant-scoped MCP URL.
        /// </summary>
        internal static bool TryGetTenant(string mcpUrl, out string tenant)
        {
            tenant = null;
            if (!Uri.TryCreate(mcpUrl, UriKind.Absolute, out Uri uri) || uri.Segments.Length == 0)
                return false;

            tenant = uri.Segments[uri.Segments.Length - 1].Trim('/');
            return tenant.Length > 0;
        }

        internal static bool BelongsToTenant(string tokensJson, string tenant)
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

            return string.Equals(TryGetJwtRealm(tokens.Value<string>("access_token")), tenant, StringComparison.Ordinal)
                || string.Equals(TryGetJwtRealm(tokens.Value<string>("refresh_token")), tenant, StringComparison.Ordinal);
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
            return RemoveTokenFiles(mcpUrl, tokensJson => IsSessionExpired(tokensJson, now));
        }

        /// <summary>
        /// Removes the cached DCR client registration (and the half-finished sign-in files stored with it) of the
        /// tenant. mcp-remote reuses that registration forever; when the server no longer knows the client the
        /// browser stops at "Client not found" and no sign-in can complete until a new client is registered.
        /// The files are named after a hash, so the entry is matched on the issuer stored in client_info.
        /// </summary>
        /// <returns>The number of cached registrations removed.</returns>
        public virtual int ClearClientRegistration(string mcpUrl)
        {
            if (!TryGetTenant(mcpUrl, out string tenant) || !Directory.Exists(_authRoot))
                return 0;

            int removed = 0;

            try
            {
                foreach (string versionDir in Directory.GetDirectories(_authRoot, "mcp-remote-*"))
                {
                    foreach (string clientInfoFile in Directory.GetFiles(versionDir, CLIENT_INFO_FILE_PATTERN))
                    {
                        if (!ClientInfoBelongsToTenant(File.ReadAllText(clientInfoFile), tenant))
                            continue;

                        string prefix = Path.GetFileName(clientInfoFile)
                            .Substring(0, Path.GetFileName(clientInfoFile).Length - "client_info.json".Length);
                        foreach (string stale in Directory.GetFiles(versionDir, prefix + "code_verifier_*.txt"))
                            File.Delete(stale);

                        File.Delete(clientInfoFile);
                        removed++;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Best effort, like the token cleanup.
                System.Diagnostics.Debug.WriteLine($"Failed to clear cached MCP OAuth client registration: {ex.Message}");
            }

            return removed;
        }

        internal static bool ClientInfoBelongsToTenant(string clientInfoJson, string tenant)
        {
            try
            {
                string issuer = JObject.Parse(clientInfoJson).Value<string>("issuer");
                if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
                    return false;

                string[] segments = issuerUri.Segments.Select(s => s.Trim('/')).ToArray();
                int realmsIndex = Array.IndexOf(segments, "realms");
                return realmsIndex >= 0 && realmsIndex + 1 < segments.Length
                    && string.Equals(segments[realmsIndex + 1], tenant, StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// True when a cached session for the tenant can still work (access or refresh token known not to be expired),
        /// so no browser sign-in is needed. When <paramref name="writtenAfterUtc"/> is given, only a token file
        /// written after that moment counts, which is how a just-completed sign-in is recognised.
        /// </summary>
        public virtual bool HasValidSession(string mcpUrl, DateTime? writtenAfterUtc = null)
        {
            if (!TryGetTenant(mcpUrl, out string tenant) || !Directory.Exists(_authRoot))
                return false;

            DateTimeOffset now = DateTimeOffset.UtcNow;

            try
            {
                foreach (string versionDir in Directory.GetDirectories(_authRoot, "mcp-remote-*"))
                {
                    foreach (string tokensFile in Directory.GetFiles(versionDir, TOKENS_FILE_PATTERN))
                    {
                        if (writtenAfterUtc != null && File.GetLastWriteTimeUtc(tokensFile) <= writtenAfterUtc.Value)
                            continue;

                        string tokensJson = File.ReadAllText(tokensFile);
                        if (BelongsToTenant(tokensJson, tenant) && IsSessionUsable(tokensJson, now))
                            return true;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // A token file mid-write by mcp-remote; the caller polls again.
                System.Diagnostics.Debug.WriteLine($"Failed to read cached MCP OAuth tokens: {ex.Message}");
            }

            return false;
        }

        internal static bool IsSessionExpired(string tokensJson, DateTimeOffset now)
            => GetSessionState(tokensJson, now) == SessionState.Expired;

        /// <summary>
        /// True only when a token is known to be unexpired. A session whose expiry cannot be read is not trusted
        /// here (the browser sign-in runs), although <see cref="IsSessionExpired"/> does not delete it either.
        /// </summary>
        internal static bool IsSessionUsable(string tokensJson, DateTimeOffset now)
            => GetSessionState(tokensJson, now) == SessionState.Usable;

        private enum SessionState { Usable, Expired, Unknown }

        private static SessionState GetSessionState(string tokensJson, DateTimeOffset now)
        {
            JObject tokens;
            try
            {
                tokens = JObject.Parse(tokensJson);
            }
            catch (JsonException)
            {
                return SessionState.Unknown;
            }

            DateTimeOffset? accessExpiry = TryGetJwtExpiry(tokens.Value<string>("access_token"));
            long? expiresAtMs = tokens.Value<long?>("expires_at");
            if (accessExpiry == null && expiresAtMs != null)
                accessExpiry = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs.Value);

            if (accessExpiry > now)
                return SessionState.Usable;

            string refreshToken = tokens.Value<string>("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
                return accessExpiry == null ? SessionState.Unknown : SessionState.Expired;

            DateTimeOffset? refreshExpiry = TryGetJwtExpiry(refreshToken);
            if (refreshExpiry == null)
                return SessionState.Unknown;

            return refreshExpiry > now ? SessionState.Usable : SessionState.Expired;
        }

        private static DateTimeOffset? TryGetJwtExpiry(string jwt)
        {
            long? exp = TryGetJwtClaims(jwt)?.Value<long?>("exp");
            return exp == null ? (DateTimeOffset?)null : DateTimeOffset.FromUnixTimeSeconds(exp.Value);
        }

        private static string TryGetJwtRealm(string jwt)
        {
            string issuer = TryGetJwtClaims(jwt)?.Value<string>("iss");
            if (!Uri.TryCreate(issuer, UriKind.Absolute, out Uri issuerUri))
                return null;

            string[] segments = issuerUri.Segments.Select(s => s.Trim('/')).ToArray();
            int realmsIndex = Array.IndexOf(segments, "realms");
            return realmsIndex >= 0 && realmsIndex + 1 < segments.Length ? segments[realmsIndex + 1] : null;
        }

        private static JObject TryGetJwtClaims(string jwt)
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
                return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException)
            {
                return null;
            }
        }

        private int RemoveTokenFiles(string mcpUrl, Func<string, bool> shouldRemove)
        {
            if (!TryGetTenant(mcpUrl, out string tenant) || !Directory.Exists(_authRoot))
                return 0;

            int removed = 0;

            try
            {
                // One folder per mcp-remote storage version (e.g. "mcp-remote-v1", older "mcp-remote-0.1.x").
                foreach (string versionDir in Directory.GetDirectories(_authRoot, "mcp-remote-*"))
                {
                    foreach (string tokensFile in Directory.GetFiles(versionDir, TOKENS_FILE_PATTERN))
                    {
                        string tokensJson = File.ReadAllText(tokensFile);
                        if (BelongsToTenant(tokensJson, tenant) && shouldRemove(tokensJson))
                        {
                            File.Delete(tokensFile);
                            removed++;
                        }
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
