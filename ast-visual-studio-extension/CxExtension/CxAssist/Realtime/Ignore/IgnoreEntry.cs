using System.Collections.Generic;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Ignore
{
    /// <summary>
    /// Persisted ignore entry, aligned with JetBrains <c>IgnoreEntry</c>.
    /// Stored in <c>&lt;solution&gt;/.vscode/.checkmarxIgnored</c> as a JSON object keyed by
    /// scanner-specific composite key (see <see cref="IgnoreEntry.BuildKey"/>).
    /// </summary>
    public sealed class IgnoreEntry
    {
        [JsonProperty("files")]
        public List<FileReference> Files { get; set; } = new List<FileReference>();

        [JsonProperty("type")]
        public ScannerType Type { get; set; }

        [JsonProperty("similarityId", NullValueHandling = NullValueHandling.Ignore)]
        public string SimilarityId { get; set; }

        [JsonProperty("packageManager", NullValueHandling = NullValueHandling.Ignore)]
        public string PackageManager { get; set; }

        [JsonProperty("packageName", NullValueHandling = NullValueHandling.Ignore)]
        public string PackageName { get; set; }

        [JsonProperty("packageVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string PackageVersion { get; set; }

        [JsonProperty("ruleId", NullValueHandling = NullValueHandling.Ignore)]
        public int? RuleId { get; set; }

        [JsonProperty("imageName", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageName { get; set; }

        [JsonProperty("imageTag", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageTag { get; set; }

        [JsonProperty("severity", NullValueHandling = NullValueHandling.Ignore)]
        public string Severity { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("dateAdded", NullValueHandling = NullValueHandling.Ignore)]
        public string DateAdded { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("secretValue", NullValueHandling = NullValueHandling.Ignore)]
        public string SecretValue { get; set; }

        /// <summary>
        /// One file/line reference for this entry. JetBrains stores multiple references per entry
        /// (e.g. lodash appearing in both package.json and package-lock.json) and toggles
        /// <see cref="Active"/> rather than removing items, so a revive can be undone.
        /// </summary>
        public sealed class FileReference
        {
            /// <summary>Solution-relative path with forward slashes.</summary>
            [JsonProperty("path")]
            public string Path { get; set; }

            /// <summary><c>true</c> = still ignored. <c>false</c> = revived (kept for undo support).</summary>
            [JsonProperty("active")]
            public bool Active { get; set; } = true;

            [JsonProperty("line", NullValueHandling = NullValueHandling.Ignore)]
            public int? Line { get; set; }

            /// <summary>Trimmed source-line text for ASCA content matching (survives line shifts).</summary>
            [JsonProperty("problematicLine", NullValueHandling = NullValueHandling.Ignore)]
            public string ProblematicLine { get; set; }
        }

        /// <summary>
        /// Composite key under which the entry is stored in the JSON dictionary.
        /// Mirrors JetBrains keying so plugins can share the same file across IDEs.
        /// </summary>
        /// <param name="scanner">Scanner type.</param>
        /// <param name="title">Vulnerability title / rule name.</param>
        /// <param name="ruleId">ASCA rule id.</param>
        /// <param name="similarityId">IaC similarity id.</param>
        /// <param name="packageManager">OSS package manager.</param>
        /// <param name="packageName">OSS package name.</param>
        /// <param name="packageVersion">OSS package version.</param>
        /// <param name="imageName">Container image name.</param>
        /// <param name="imageTag">Container image tag.</param>
        /// <param name="secretValue">Secret literal value.</param>
        /// <param name="relativeFilePath">File path (solution-relative, forward slashes).</param>
        public static string BuildKey(
            ScannerType scanner,
            string title,
            int? ruleId,
            string similarityId,
            string packageManager,
            string packageName,
            string packageVersion,
            string imageName,
            string imageTag,
            string secretValue,
            string relativeFilePath)
        {
            switch (scanner)
            {
                case ScannerType.OSS:
                    return $"{packageManager ?? ""}:{packageName ?? ""}:{packageVersion ?? ""}";
                case ScannerType.Secrets:
                    return $"{title ?? ""}:{secretValue ?? ""}:{relativeFilePath ?? ""}";
                case ScannerType.IaC:
                    return $"{title ?? ""}:{similarityId ?? ""}:{relativeFilePath ?? ""}";
                case ScannerType.ASCA:
                    return $"{title ?? ""}:{(ruleId.HasValue ? ruleId.Value.ToString() : "")}:{relativeFilePath ?? ""}";
                case ScannerType.Containers:
                    return $"{imageName ?? ""}:{imageTag ?? ""}";
                default:
                    return $"{scanner}:{title ?? ""}:{relativeFilePath ?? ""}";
            }
        }
    }
}
