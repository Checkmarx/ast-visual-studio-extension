using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxCLI.Models
{
    internal class Results
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
