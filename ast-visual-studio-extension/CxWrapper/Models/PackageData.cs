using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class PackageData
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
