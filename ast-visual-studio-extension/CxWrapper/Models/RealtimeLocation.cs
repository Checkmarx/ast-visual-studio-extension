using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    /// <summary>
    /// Represents a location in a file for realtime scanner results.
    /// Used by IAC, Secrets, and OSS realtime scanners.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class RealtimeLocation
    {
        [JsonProperty("Line")]
        public int Line { get; }

        [JsonProperty("StartIndex")]
        public int StartIndex { get; }

        [JsonProperty("EndIndex")]
        public int EndIndex { get; }

        [JsonConstructor]
        public RealtimeLocation(
            [JsonProperty("Line")] int line,
            [JsonProperty("StartIndex")] int startIndex,
            [JsonProperty("EndIndex")] int endIndex)
        {
            Line = line;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}

