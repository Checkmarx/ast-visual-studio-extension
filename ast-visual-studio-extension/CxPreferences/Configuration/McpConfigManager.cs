using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    internal class McpConfigManager
    {
        internal const string ServerName = "Checkmarx";
        internal const string DefaultMcpUrl = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp";

        public virtual string GetMcpConfigPath()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".mcp.json");
        }

        public virtual bool InstallOrUpdate(string apiKey, string mcpUrl, out string configPath)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key is required.", nameof(apiKey));

            if (string.IsNullOrWhiteSpace(mcpUrl))
                mcpUrl = DefaultMcpUrl;

            configPath = GetMcpConfigPath();

            JObject root = ReadConfig(configPath);

            // Ensure 'inputs' key exists and is a JArray
            if (!root.ContainsKey("inputs") || !(root["inputs"] is JArray))
                root["inputs"] = new JArray();

            // Ensure 'servers' key exists and is a JObject
            JObject servers = null;
            if (root.ContainsKey("servers") && root["servers"] is JObject)
            {
                servers = root["servers"] as JObject;
            }
            else
            {
                servers = new JObject();
                root["servers"] = servers;
            }

            JObject desiredServer = BuildCheckmarxServer(apiKey, mcpUrl);
            JToken existingServer = servers[ServerName];

            bool changed = existingServer == null || existingServer.ToString() != desiredServer.ToString();
            if (changed)
            {
                servers[ServerName] = desiredServer;
                WriteConfig(configPath, root);
            }

            return changed;
        }

        internal bool RemoveCheckmarxServer(out string configPath)
        {
            configPath = GetMcpConfigPath();
            JObject root = ReadConfig(configPath);

            JObject servers = root.ContainsKey("servers") ? root["servers"] as JObject : null;
            if (servers == null || !servers.ContainsKey(ServerName) || servers[ServerName] == null)
                return false;

            servers.Remove(ServerName);
            WriteConfig(configPath, root);
            return true;
        }

        private static JObject BuildCheckmarxServer(string apiKey, string mcpUrl)
        {
            return new JObject
            {
                ["command"] = "npx",
                ["args"] = new JArray
                {
                    "mcp-remote",
                    mcpUrl,
                    "--transport",
                    "http-first",
                    "--header",
                    "Authorization:" + apiKey,
                    "--header",
                    "cx-origin:VisualStudio",
                    "--verbose"
                }
            };
        }

        private static JObject ReadConfig(string configPath)
        {
            if (!File.Exists(configPath))
                return new JObject();

            string json = File.ReadAllText(configPath);
            if (string.IsNullOrWhiteSpace(json))
                return new JObject();

            try
            {
                JToken parsed = JToken.Parse(StripJsonComments(json));
                JObject root = parsed as JObject;
                return root ?? new JObject();
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Keep install resilient when file is malformed: start from a fresh root object.
                return new JObject();
            }
        }

        private static void WriteConfig(string configPath, JObject root)
        {
            string directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Format JSON with proper indentation
            string formattedJson = root.ToString(Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configPath, formattedJson);
        }

        private static string StripJsonComments(string json)
        {
            var sb = new StringBuilder(json.Length);
            bool inString = false;
            bool escaping = false;
            bool lineComment = false;
            bool blockComment = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                char next = i + 1 < json.Length ? json[i + 1] : '\0';

                if (lineComment)
                {
                    if (c == '\n')
                    {
                        lineComment = false;
                        sb.Append(c);
                    }
                    continue;
                }

                if (blockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        blockComment = false;
                        i++;
                    }
                    continue;
                }

                if (!inString && c == '/' && next == '/')
                {
                    lineComment = true;
                    i++;
                    continue;
                }

                if (!inString && c == '/' && next == '*')
                {
                    blockComment = true;
                    i++;
                    continue;
                }

                sb.Append(c);

                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (c == '\\')
                    {
                        escaping = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else if (c == '"')
                {
                    inString = true;
                }
            }

            return sb.ToString();
        }
    }
}
