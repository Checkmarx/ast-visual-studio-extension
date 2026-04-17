using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [ExcludeFromCodeCoverage]
    public class Result
    {
        /// <summary>
        /// Timestamp when this result was generated (for freshness validation).
        /// Prevents out-of-order scan completions from displaying stale results.
        /// </summary>
        [JsonIgnore]
        public DateTime ResultTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Version/generation counter of the document when scan was initiated.
        /// Used to detect if document was edited between scan start and completion.
        /// </summary>
        [JsonIgnore]
        public int DocumentVersion { get; set; } = 0;
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("similarityId")]
        public string SimilarityId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("firstFoundAt")]
        public string FirstFoundAt { get; set; }

        [JsonProperty("foundAt")]
        public string FoundAt { get; set; }

        [JsonProperty("firstScan")]
        public string FirstScan { get; set; }

        [JsonProperty("firstScanId")]
        public string FirstScanId { get; set; }

        [JsonProperty("publishedAt")]
        public string PublishedAt { get; set; }

        [JsonProperty("recommendations")]
        public string Recommendations { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("comments")]
        public Comments GetComments { get; set; }

        [JsonProperty("vulnerabilityDetails")]
        public VulnerabilityDetails VulnerabilityDetails { get; set; }
    }
}
