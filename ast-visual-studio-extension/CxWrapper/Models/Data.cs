using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxCLI.Models
{
    internal class Data
    {
        [JsonProperty("queryId")]
        public string QueryId { get; set; }

        [JsonProperty("queryName")]
        public string QueryName { get; set; }

        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("resultHash")]
        public string ResultHash { get; set; }

        [JsonProperty("languageName")]
        public string LanguageName { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("issueType")]
        public string IssueType { get; set; }

        [JsonProperty("expectedValue")]
        public string ExpectedValue { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("nodes")]
        public List<Node> Nodes { get; set; }

        [JsonProperty("packageData")]
        public List<PackageData> PackageData { get; set; }
    }
}
