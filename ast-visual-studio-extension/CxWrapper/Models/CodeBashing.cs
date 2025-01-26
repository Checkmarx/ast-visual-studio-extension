using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [ExcludeFromCodeCoverage]
    public class CodeBashing
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("cwe_id")]
        public string CweId { get; set; }

        [JsonProperty("lang")]
        public string Language { get; set; }

        [JsonProperty("cxQueryName")]
        public string QueryName { get; set; }
    }
}
