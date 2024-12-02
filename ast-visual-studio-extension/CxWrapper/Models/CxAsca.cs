using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CxAsca
    {
        [JsonProperty("request_id")]
        public string RequestId { get; }

        [JsonProperty("status")]
        public bool Status { get; }

        [JsonProperty("message")]
        public string Message { get; }

        [JsonProperty("scan_details")]
        public List<CxAscaDetail> ScanDetails { get; }

        [JsonProperty("error")]
        public CxAscaError Error { get; }

        [JsonConstructor]
        public CxAsca(
            [JsonProperty("request_id")] string requestId,
            [JsonProperty("status")] bool status,
            [JsonProperty("message")] string message,
            [JsonProperty("scan_details")] List<CxAscaDetail> scanDetails,
            [JsonProperty("error")] CxAscaError error)
        {
            RequestId = requestId;
            Status = status;
            Message = message;
            ScanDetails = scanDetails ?? new List<CxAscaDetail>();
            Error = error;
        }

        public static CxAsca FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CxAsca>(json);
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CxAscaDetail
    {
        [JsonProperty("rule_id")]
        public int RuleId { get; }

        [JsonProperty("language")]
        public string Language { get; }

        [JsonProperty("rule_name")]
        public string RuleName { get; }

        [JsonProperty("severity")]
        public string Severity { get; }

        [JsonProperty("file_name")]
        public string FileName { get; }

        [JsonProperty("line")]
        public int Line { get; }

        [JsonProperty("problematicLine")]
        public string ProblematicLine { get; }

        [JsonProperty("length")]
        public int Length { get; }

        [JsonProperty("remediationAdvise")]
        public string RemediationAdvise { get; }

        [JsonProperty("description")]
        public string Description { get; }

        [JsonConstructor]
        public CxAscaDetail(
            [JsonProperty("rule_id")] int ruleId,
            [JsonProperty("language")] string language,
            [JsonProperty("rule_name")] string ruleName,
            [JsonProperty("severity")] string severity,
            [JsonProperty("file_name")] string fileName,
            [JsonProperty("line")] int line,
            [JsonProperty("problematicLine")] string problematicLine,
            [JsonProperty("length")] int length,
            [JsonProperty("remediationAdvise")] string remediationAdvise,
            [JsonProperty("description")] string description)
        {
            RuleId = ruleId;
            Language = language;
            RuleName = ruleName;
            Severity = severity;
            FileName = fileName;
            Line = line;
            ProblematicLine = problematicLine;
            Length = length;
            RemediationAdvise = remediationAdvise;
            Description = description;
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CxAscaError
    {
        [JsonProperty("code")]
        public int Code { get; }

        [JsonProperty("description")]
        public string Description { get; }

        [JsonConstructor]
        public CxAscaError(
            [JsonProperty("code")] int code,
            [JsonProperty("description")] string description)
        {
            Code = code;
            Description = description;
        }
    }

}
