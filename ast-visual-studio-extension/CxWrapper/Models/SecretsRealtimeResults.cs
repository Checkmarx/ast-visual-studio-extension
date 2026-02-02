using Newtonsoft.Json;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    /// <summary>
    /// Result of a Secrets realtime scan containing detected secrets.
    /// Maps to the CLI JSON response from 'cx scan secrets-realtime' command.
    /// </summary>
    public class SecretsRealtimeResults
    {
        [JsonProperty("Secrets")]
        public List<Secret> Secrets { get; }

        [JsonConstructor]
        public SecretsRealtimeResults(
            [JsonProperty("Secrets")] List<Secret> secrets)
        {
            Secrets = secrets ?? new List<Secret>();
        }
    }

    /// <summary>
    /// Represents a detected secret in the source code.
    /// </summary>
    public class Secret
    {
        /// <summary>
        /// Title/type of the secret (e.g., "AWS Access Key", "GitHub Token")
        /// </summary>
        [JsonProperty("Title")]
        public string Title { get; }

        /// <summary>
        /// Description of why this is considered a secret and potential risks
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; }

        /// <summary>
        /// The actual secret value detected (may be partially masked)
        /// </summary>
        [JsonProperty("SecretValue")]
        public string SecretValue { get; }

        /// <summary>
        /// Path to the file containing the secret
        /// </summary>
        [JsonProperty("FilePath")]
        public string FilePath { get; }

        /// <summary>
        /// Severity level of the detected secret (e.g., "high", "medium", "low")
        /// </summary>
        [JsonProperty("Severity")]
        public string Severity { get; }

        /// <summary>
        /// Locations in the file where the secret was found
        /// </summary>
        [JsonProperty("Locations")]
        public List<RealtimeLocation> Locations { get; }

        [JsonConstructor]
        public Secret(
            [JsonProperty("Title")] string title,
            [JsonProperty("Description")] string description,
            [JsonProperty("SecretValue")] string secretValue,
            [JsonProperty("FilePath")] string filePath,
            [JsonProperty("Severity")] string severity,
            [JsonProperty("Locations")] List<RealtimeLocation> locations)
        {
            Title = title;
            Description = description;
            SecretValue = secretValue;
            FilePath = filePath;
            Severity = severity;
            Locations = locations ?? new List<RealtimeLocation>();
        }
    }
}

