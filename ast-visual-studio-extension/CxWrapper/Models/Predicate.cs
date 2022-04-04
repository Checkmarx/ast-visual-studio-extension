using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class Predicate
    {

        [JsonProperty("ID")]
        public string ID { get; set; }

        [JsonProperty("SimilarityID")]
        public string SimilarityID { get; set; }

        [JsonProperty("ProjectID")]
        public string ProjectID { get; set; }

        [JsonProperty("State")]
        public string State { get; set; }

        [JsonProperty("Severity")]
        public string Severity { get; set; }

        [JsonProperty("Comment")]
        public string Comment { get; set; }

        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("CreatedAt")]
        public string CreatedAt { get; set; }

        [JsonProperty("UpdatedAt")]
        public string UpdatedAt { get; set; }
    }
}
