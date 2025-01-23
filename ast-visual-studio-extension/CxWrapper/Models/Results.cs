using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [ExcludeFromCodeCoverage]
    public class Results
    {
        public int totalCount;
        public List<Result> results;

        [JsonConstructor]
        public Results(int totalCount, List<Result> results)
        {
            this.totalCount = totalCount;
            this.results = results;
        }

    }
}
