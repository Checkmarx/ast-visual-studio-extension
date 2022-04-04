using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class Node
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("nodeID")]
        public int NodeID { get; set; }

        [JsonProperty("domType")]
        public string DomType { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("typeName")]
        public string TypeName { get; set; }

        [JsonProperty("methodLine")]
        public string MethodLine { get; set; }

        [JsonProperty("definitions")]
        public string Definitions { get; set; }
    }
}
