using Newtonsoft.Json;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    // LearnMore myDeserializedClass = JsonConvert.DeserializeObject<List<LearnMore>>(myJsonResponse);
    public class LearnMore {

        [JsonProperty("queryId", NullValueHandling = NullValueHandling.Ignore)]
        public string queryId;

        [JsonProperty("queryName", NullValueHandling = NullValueHandling.Ignore)]
        public string queryName;

        [JsonProperty("queryDescriptionId", NullValueHandling = NullValueHandling.Ignore)]
        public string queryDescriptionId;

        [JsonProperty("resultDescription", NullValueHandling = NullValueHandling.Ignore)]
        public string resultDescription;

        [JsonProperty("risk", NullValueHandling = NullValueHandling.Ignore)]
        public string risk;

        [JsonProperty("cause", NullValueHandling = NullValueHandling.Ignore)]
        public string cause;

        [JsonProperty("generalRecommendations", NullValueHandling = NullValueHandling.Ignore)]
        public string generalRecommendations;

        [JsonProperty("samples", NullValueHandling = NullValueHandling.Ignore)]
        public List<Sample> samples;
    }

    public class Sample
    {
        [JsonProperty("progLanguage", NullValueHandling = NullValueHandling.Ignore)]
        public string progLanguage;

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string code;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string title;
    }
}
