using Newtonsoft.Json;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    /// <summary>
    /// Result of an IAC (Infrastructure as Code) realtime scan.
    /// Maps to the CLI JSON response from 'cx scan iac-realtime' command.
    /// </summary>
    public class IacRealtimeResults
    {
        [JsonProperty("Results")]
        public List<IacIssue> Results { get; }

        [JsonConstructor]
        public IacRealtimeResults(
            [JsonProperty("Results")] List<IacIssue> results)
        {
            Results = results ?? new List<IacIssue>();
        }
    }

    /// <summary>
    /// Represents an IAC security issue detected in infrastructure configuration files.
    /// </summary>
    public class IacIssue
    {
        /// <summary>
        /// Title of the IAC issue
        /// </summary>
        [JsonProperty("Title")]
        public string Title { get; }

        /// <summary>
        /// Description of the security issue
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; }

        /// <summary>
        /// Unique identifier for similar issues
        /// </summary>
        [JsonProperty("SimilarityID")]
        public string SimilarityId { get; }

        /// <summary>
        /// Path to the file containing the issue
        /// </summary>
        [JsonProperty("FilePath")]
        public string FilePath { get; }

        /// <summary>
        /// Severity level (e.g., "high", "medium", "low")
        /// </summary>
        [JsonProperty("Severity")]
        public string Severity { get; }

        /// <summary>
        /// The expected/secure value for the configuration
        /// </summary>
        [JsonProperty("ExpectedValue")]
        public string ExpectedValue { get; }

        /// <summary>
        /// The actual/insecure value found in the configuration
        /// </summary>
        [JsonProperty("ActualValue")]
        public string ActualValue { get; }

        /// <summary>
        /// Locations in the file where the issue was found
        /// </summary>
        [JsonProperty("Locations")]
        public List<RealtimeLocation> Locations { get; }

        [JsonConstructor]
        public IacIssue(
            [JsonProperty("Title")] string title,
            [JsonProperty("Description")] string description,
            [JsonProperty("SimilarityID")] string similarityId,
            [JsonProperty("FilePath")] string filePath,
            [JsonProperty("Severity")] string severity,
            [JsonProperty("ExpectedValue")] string expectedValue,
            [JsonProperty("ActualValue")] string actualValue,
            [JsonProperty("Locations")] List<RealtimeLocation> locations)
        {
            Title = title;
            Description = description;
            SimilarityId = similarityId;
            FilePath = filePath;
            Severity = severity;
            ExpectedValue = expectedValue;
            ActualValue = actualValue;
            Locations = locations ?? new List<RealtimeLocation>();
        }
    }
}

