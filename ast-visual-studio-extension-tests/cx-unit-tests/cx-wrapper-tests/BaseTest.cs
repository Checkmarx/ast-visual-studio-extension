using System;
using System.Collections.Generic;
using System.Linq;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxWrapper.Models;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    public abstract class BaseTest : IDisposable
    {
        private static readonly string? CX_APIKEY = GetEnvOrNull("CX_APIKEY");
        private static readonly string? CX_ADDITIONAL_PARAMETERS = GetEnvOrNull("CX_ADDITIONAL_PARAMETERS");
        
        protected  CxWrapper cxWrapper;

        public BaseTest()
        {
            cxWrapper = new CxWrapper(GetCxConfig(), GetType());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public void Dispose()
        {
           
        }

        private static string? GetEnvOrNull(string param)
        {
            return string.IsNullOrEmpty(param) ? string.Empty : Environment.GetEnvironmentVariable(param);
        }

        protected static CxConfig GetCxConfig()
        {
            CxConfig configuration = new()
            {
                ApiKey = CX_APIKEY,
                AdditionalParameters = CX_ADDITIONAL_PARAMETERS,
            };

            return configuration;
        }

        protected static Dictionary<string, string> GetCommonParams()
        {
            return new Dictionary<string, string>
            {
                { CxConstants.FLAG_PROJECT_NAME, "CLI-Visual-Studio-Wrapper-Tests" },
                { CxConstants.FLAG_SOURCE, "." },
                { CxConstants.FLAG_FILE_FILTER, "!test" },
                { CxConstants.FLAG_BRANCH, "main" },
                { CxConstants.FLAG_SAST_PRESET_NAME, "Checkmarx Default" },
                { CxConstants.FLAG_AGENT, "CLI-Java-Wrapper" },
            };
        }

        protected Dictionary<Scan, Results> GetFirstScanWithResults(List<Scan> scanList)
        {
            Dictionary<Scan, Results> result = new();

            for (int i = 0; i < scanList.Count; i++)
            {
                Scan scan = scanList[i];

                Results results = cxWrapper.GetResults(new Guid(scan.ID));

                if (results != null && results.results.Any() && results.results.Where(r => r.Type.Equals("sast")).Any())
                {
                    result.Add(scan, results);
                    break;
                }
            }

            return result;
        }
    }
}
