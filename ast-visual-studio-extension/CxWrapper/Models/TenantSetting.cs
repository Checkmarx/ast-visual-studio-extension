using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class TenantSetting
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
