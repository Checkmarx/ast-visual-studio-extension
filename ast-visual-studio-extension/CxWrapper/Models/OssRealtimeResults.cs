using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    /// <summary>
    /// Represents the results of an OSS realtime scan.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OssRealtimeResults
    {
        [JsonProperty("Packages")]
        public List<OssRealtimeScanPackage> Packages { get; }

        [JsonConstructor]
        public OssRealtimeResults(
            [JsonProperty("Packages")] List<OssRealtimeScanPackage> packages)
        {
            Packages = packages ?? new List<OssRealtimeScanPackage>();
        }
    }

    /// <summary>
    /// Represents a package found during OSS realtime scanning.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OssRealtimeScanPackage
    {
        [JsonProperty("PackageManager")]
        public string PackageManager { get; }

        [JsonProperty("PackageName")]
        public string PackageName { get; }

        [JsonProperty("PackageVersion")]
        public string PackageVersion { get; }

        [JsonProperty("FilePath")]
        public string FilePath { get; }

        [JsonProperty("Locations")]
        public List<RealtimeLocation> Locations { get; }

        [JsonProperty("Status")]
        public string Status { get; }

        [JsonProperty("Vulnerabilities")]
        public List<OssRealtimeVulnerability> Vulnerabilities { get; }

        [JsonConstructor]
        public OssRealtimeScanPackage(
            [JsonProperty("PackageManager")] string packageManager,
            [JsonProperty("PackageName")] string packageName,
            [JsonProperty("PackageVersion")] string packageVersion,
            [JsonProperty("FilePath")] string filePath,
            [JsonProperty("Locations")] List<RealtimeLocation> locations,
            [JsonProperty("Status")] string status,
            [JsonProperty("Vulnerabilities")] List<OssRealtimeVulnerability> vulnerabilities)
        {
            PackageManager = packageManager;
            PackageName = packageName;
            PackageVersion = packageVersion;
            FilePath = filePath;
            Locations = locations ?? new List<RealtimeLocation>();
            Status = status;
            Vulnerabilities = vulnerabilities ?? new List<OssRealtimeVulnerability>();
        }
    }

    /// <summary>
    /// Represents a vulnerability found in an OSS package during realtime scanning.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OssRealtimeVulnerability
    {
        [JsonProperty("CVE")]
        public string Cve { get; }

        [JsonProperty("Severity")]
        public string Severity { get; }

        [JsonProperty("Description")]
        public string Description { get; }

        [JsonProperty("FixVersion")]
        public string FixVersion { get; }

        [JsonConstructor]
        public OssRealtimeVulnerability(
            [JsonProperty("CVE")] string cve,
            [JsonProperty("Severity")] string severity,
            [JsonProperty("Description")] string description,
            [JsonProperty("FixVersion")] string fixVersion)
        {
            Cve = cve;
            Severity = severity;
            Description = description;
            FixVersion = fixVersion;
        }
    }
}

