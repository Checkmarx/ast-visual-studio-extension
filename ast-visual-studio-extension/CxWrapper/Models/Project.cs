

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [ExcludeFromCodeCoverage]
    public class Project
    {
        [JsonProperty("ID")]
        public string Id { get; set; }

        [JsonProperty("CreatedAt")]
        public string CreatedAt { get; set; }

        [JsonProperty("UpdatedAt")]
        public string UpdatedAt { get; set; }

        [JsonProperty("Tags")]

        public Dictionary<string, string> Tags { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Groups")]
        public List<string> Groups { get; set; }
    }
}
