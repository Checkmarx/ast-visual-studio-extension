using Newtonsoft.Json;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    /// <summary>
    /// Result of a Containers realtime scan.
    /// Maps to the CLI JSON response from 'cx scan containers-realtime' command.
    /// </summary>
    public class ContainersRealtimeResults
    {
        [JsonProperty("Images")]
        public List<ContainersRealtimeImage> Images { get; }

        [JsonConstructor]
        public ContainersRealtimeResults(
            [JsonProperty("Images")] List<ContainersRealtimeImage> images)
        {
            Images = images ?? new List<ContainersRealtimeImage>();
        }
    }

    /// <summary>
    /// Represents a container image scanned for vulnerabilities.
    /// </summary>
    public class ContainersRealtimeImage
    {
        /// <summary>
        /// Name of the container image (e.g., "nginx", "node")
        /// </summary>
        [JsonProperty("ImageName")]
        public string ImageName { get; }

        /// <summary>
        /// Tag/version of the container image (e.g., "latest", "18-alpine")
        /// </summary>
        [JsonProperty("ImageTag")]
        public string ImageTag { get; }

        /// <summary>
        /// Path to the Dockerfile or docker-compose file
        /// </summary>
        [JsonProperty("FilePath")]
        public string FilePath { get; }

        /// <summary>
        /// Locations in the file where the image is referenced
        /// </summary>
        [JsonProperty("Locations")]
        public List<RealtimeLocation> Locations { get; }

        /// <summary>
        /// Status of the image scan (e.g., "vulnerable", "ok")
        /// </summary>
        [JsonProperty("Status")]
        public string Status { get; }

        /// <summary>
        /// List of vulnerabilities found in the container image
        /// </summary>
        [JsonProperty("Vulnerabilities")]
        public List<ContainersRealtimeVulnerability> Vulnerabilities { get; }

        [JsonConstructor]
        public ContainersRealtimeImage(
            [JsonProperty("ImageName")] string imageName,
            [JsonProperty("ImageTag")] string imageTag,
            [JsonProperty("FilePath")] string filePath,
            [JsonProperty("Locations")] List<RealtimeLocation> locations,
            [JsonProperty("Status")] string status,
            [JsonProperty("Vulnerabilities")] List<ContainersRealtimeVulnerability> vulnerabilities)
        {
            ImageName = imageName;
            ImageTag = imageTag;
            FilePath = filePath;
            Locations = locations ?? new List<RealtimeLocation>();
            Status = status;
            Vulnerabilities = vulnerabilities ?? new List<ContainersRealtimeVulnerability>();
        }
    }

    /// <summary>
    /// Represents a vulnerability found in a container image.
    /// </summary>
    public class ContainersRealtimeVulnerability
    {
        /// <summary>
        /// CVE identifier for the vulnerability
        /// </summary>
        [JsonProperty("CVE")]
        public string Cve { get; }

        /// <summary>
        /// Severity level (e.g., "critical", "high", "medium", "low")
        /// </summary>
        [JsonProperty("Severity")]
        public string Severity { get; }

        [JsonConstructor]
        public ContainersRealtimeVulnerability(
            [JsonProperty("CVE")] string cve,
            [JsonProperty("Severity")] string severity)
        {
            Cve = cve;
            Severity = severity;
        }
    }
}

