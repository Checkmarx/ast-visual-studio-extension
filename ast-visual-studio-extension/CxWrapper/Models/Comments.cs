using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [ExcludeFromCodeCoverage]
    public class Comments
    {
        [JsonProperty("comments")]
        public string GetComments { get; set; }
    }
}
