using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// Completes the OAuth (DCR) browser sign-in for the Checkmarx OAuth MCP entry outside Visual Studio by
    /// running mcp-remote's own client with the same arguments as the entry, so the tokens land in the same
    /// ~/.mcp-auth cache the VS-launched bridge reads. Signing in here avoids VS's ~60 s MCP initialization
    /// timeout, which a first-time browser sign-in inside VS can exceed.
    /// </summary>
    internal class McpOAuthSignIn
    {
        // Interactive browser sign-in (MFA included) cannot fit the 30 s CLI timeout; it is still bounded.
        internal static readonly TimeSpan SIGN_IN_TIMEOUT = TimeSpan.FromMinutes(3);
        private const int POLL_INTERVAL_MS = 1000;

        internal static bool TryBuildArguments(string mcpUrl, out string arguments)
        {
            arguments = null;
            // Checked again here (not only when the URL was resolved) because it goes on a cmd.exe command line.
            if (!McpInstallService.IsSafeOAuthMcpUrl(mcpUrl))
                return false;

            arguments = "/d /s /c \"npx -y -p " + McpConfigManager.MCP_REMOTE_PACKAGE + " mcp-remote-client " + mcpUrl
                + " --transport http-first --header " + McpConfigManager.ORIGIN_HEADER + "\"";
            return true;
        }

        private readonly McpRemoteSessionCache _sessionCache;

        public McpOAuthSignIn() : this(new McpRemoteSessionCache())
        {
        }

        internal McpOAuthSignIn(McpRemoteSessionCache sessionCache)
        {
            _sessionCache = sessionCache;
        }

        public virtual Task<McpInstallResult> SignInAsync(string mcpUrl)
        {
            return Task.Run(() => SignIn(mcpUrl));
        }

        private McpInstallResult SignIn(string mcpUrl)
        {
            if (!TryBuildArguments(mcpUrl, out string arguments))
                return Failure("Cannot start the MCP sign-in for this server URL.");

            // A cached session (refreshed by mcp-remote on its own) means the user is already signed in.
            if (_sessionCache.HasValidSession(mcpUrl))
                return Success(mcpUrl, "You are already signed in to the Checkmarx MCP server. The MCP tools are ready in Copilot Chat.");

            DateTime startedUtc = DateTime.UtcNow.AddSeconds(-1);
            var startInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        return Failure("Could not start the MCP sign-in.");

                    // Drain both pipes so the child never blocks; the output (auth URLs, tool list) is not kept.
                    process.OutputDataReceived += (_, __) => { };
                    process.ErrorDataReceived += (_, __) => { };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // The client may stay running after the browser step, so success is read from the token cache
                    // mcp-remote writes once the sign-in completes, not only from the process exit.
                    DateTime deadlineUtc = DateTime.UtcNow + SIGN_IN_TIMEOUT;
                    while (DateTime.UtcNow < deadlineUtc)
                    {
                        bool exited = process.WaitForExit(POLL_INTERVAL_MS);

                        if (_sessionCache.HasValidSession(mcpUrl, startedUtc))
                        {
                            if (!exited)
                                KillProcessTree(process.Id);
                            return Success(mcpUrl, "Login successful. The Checkmarx MCP tools are ready in Copilot Chat.");
                        }

                        if (exited)
                        {
                            return process.ExitCode == 0
                                ? Success(mcpUrl, "Login successful. The Checkmarx MCP tools are ready in Copilot Chat.")
                                : Failure("The MCP sign-in did not complete (exit code " + process.ExitCode + "). Click Install MCP to try again.");
                        }
                    }

                    KillProcessTree(process.Id);
                    return Failure("The browser sign-in was not completed within "
                        + SIGN_IN_TIMEOUT.TotalMinutes + " minutes. Click Install MCP to try again.");
                }
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                Debug.WriteLine($"MCP sign-in could not start: {ex.Message}");
                return Failure("Could not start the MCP sign-in: " + ex.Message);
            }
        }

        // Killing cmd.exe alone would leave node (and its OAuth callback port) running.
        private static void KillProcessTree(int processId)
        {
            var startInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "taskkill.exe"), "/T /F /PID " + processId)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process killer = Process.Start(startInfo))
                    killer?.WaitForExit(10000);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                Debug.WriteLine($"Failed to stop the MCP sign-in process: {ex.Message}");
            }
        }

        private static McpInstallResult Success(string mcpUrl, string message)
        {
            return new McpInstallResult { Success = true, McpUrl = mcpUrl, Message = message };
        }

        private static McpInstallResult Failure(string message)
        {
            return new McpInstallResult { Success = false, Message = message };
        }
    }
}
