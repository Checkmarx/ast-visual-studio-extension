using Newtonsoft.Json;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class ResultsSummary
    {
        [JsonProperty("TotalIssues")]
        public int TotalIssues { get; set; }

        [JsonProperty("HighIssues")]
        public int HighIssues { get; set; }

        [JsonProperty("MediumIssues")]
        public int MediumIssues { get; set; }

        [JsonProperty("LowIssues")]
        public int LowIssues { get; set; }

        [JsonProperty("SastIssues")]
        public int SastIssues { get; set; }

        [JsonProperty("ScaIssues")]
        public int ScaIssues { get; set; }

        [JsonProperty("KicsIssues")]
        public int KicsIssues { get; set; }

        [JsonProperty("RiskStyle")]
        public string RiskStyle { get; set; }

        [JsonProperty("RiskMsg")]
        public string RiskMsg { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("ScanID")]
        public string ScanID { get; set; }

        [JsonProperty("ScanDate")]
        public string ScanDate { get; set; }

        [JsonProperty("ScanTime")]
        public string ScanTime { get; set; }

        [JsonProperty("CreatedAt")]
        public string CreatedAt { get; set; }

        [JsonProperty("ProjectID")]
        public string ProjectID { get; set; }

        [JsonProperty("BaseURI")]
        public string BaseURI { get; set; }
    }
}
