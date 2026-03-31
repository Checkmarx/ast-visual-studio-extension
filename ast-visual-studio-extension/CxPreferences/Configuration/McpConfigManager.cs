using System;
using System.IO;
using System.Json;
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

            JsonObject root = ReadConfig(configPath);

            // Ensure 'inputs' key exists and is a JsonArray
            if (!root.ContainsKey("inputs") || !(root["inputs"] is JsonArray))
                root["inputs"] = new JsonArray();

            // Ensure 'servers' key exists and is a JsonObject
            JsonObject servers = null;
            if (root.ContainsKey("servers") && root["servers"] is JsonObject)
            {
                servers = root["servers"] as JsonObject;
            }
            else
            {
                servers = new JsonObject();
                root["servers"] = servers;
            }

            JsonObject desiredServer = BuildCheckmarxServer(apiKey, mcpUrl);
            servers.TryGetValue(ServerName, out JsonValue existingServer);

            bool changed = existingServer == null || existingServer.ToString() != desiredServer.ToString();
            if (changed)
            {
                servers[ServerName] = desiredServer;
                WriteConfig(configPath, root);
            }

            return changed;
        }

        public virtual bool RemoveCheckmarxServer(out string configPath)
        {
            configPath = GetMcpConfigPath();
            JsonObject root = ReadConfig(configPath);

            JsonObject servers = root["servers"] as JsonObject;
            if (servers == null || servers[ServerName] == null)
                return false;

            servers.Remove(ServerName);
            WriteConfig(configPath, root);
            return true;
        }

        private static JsonObject BuildCheckmarxServer(string apiKey, string mcpUrl)
        {
            return new JsonObject
            {
                ["command"] = "npx",
                ["args"] = new JsonArray
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

        private static JsonObject ReadConfig(string configPath)
        {
            if (!File.Exists(configPath))
                return new JsonObject();

            string json = File.ReadAllText(configPath);
            if (string.IsNullOrWhiteSpace(json))
                return new JsonObject();

            try
            {
                JsonValue parsed = JsonValue.Parse(StripJsonComments(json));
                JsonObject root = parsed as JsonObject;
                return root ?? new JsonObject();
            }
            catch
            {
                // Keep install resilient when file is malformed: start from a fresh root object.
                return new JsonObject();
            }
        }

        private static void WriteConfig(string configPath, JsonObject root)
        {
            string directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Format JSON with proper indentation
            string jsonString = root.ToString();
            JToken parsed = JToken.Parse(jsonString);
            string formattedJson = parsed.ToString(Newtonsoft.Json.Formatting.Indented);

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
