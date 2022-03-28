using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxCLI.Models
{
    internal class CodeBashing
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
