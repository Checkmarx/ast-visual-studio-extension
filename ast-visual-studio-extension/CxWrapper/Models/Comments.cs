using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxCLI.Models
{
    internal class Comments
    {
        [JsonProperty("comments")]
        public string comments { get; set; }
    }
}
