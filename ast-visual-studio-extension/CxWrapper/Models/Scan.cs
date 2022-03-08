using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxCLI.Models
{
    internal class Scan
    {
        [JsonProperty("ID")]
        public string ID { get; set; }

        [JsonProperty("ProjectID")]
        public string ProjectId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Initiator")]
        public string Initiator { get; set; }

        [JsonProperty("Origin")]
        public string Origin { get; set; }

        [JsonProperty("Branch")]
        public string Branch { get; set; }
    }
}
