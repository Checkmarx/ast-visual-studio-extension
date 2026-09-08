using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class ScaPackageData
    {
        [JsonProperty("isTestDependency")]
        public bool IsTestDependency { get; set; }

        [JsonProperty("isDevelopmentDependency")]
        public bool IsDevelopmentDependency { get; set; }

        [JsonProperty("typeOfDependency")]
        public string TypeOfDependency { get; set; }
    }
}
