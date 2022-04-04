using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class Comments
    {
        [JsonProperty("comments")]
        public string GetComments { get; set; }
    }
}
