using ast_visual_studio_extension.CxCLI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class CxConfig
    {
        public string BaseUri { get; set; }
        public string BaseAuthURI { get; set; }
        public string Tenant { get; set; }
        public string ApiKey { get; set; }
        public string AdditionalParameters { get; set; }

        public List<string> ToArguments()
        {
            List<string> arguments = new List<string>();    

            if (!string.IsNullOrEmpty(BaseUri))
            {
                arguments.Add(CxConstants.FLAG_BASE_URI);
                arguments.Add(BaseUri.Trim());
            }

            if (!string.IsNullOrEmpty(BaseAuthURI))
            {
                arguments.Add(CxConstants.FLAG_BASE_AUTH_URI);
                arguments.Add(BaseAuthURI.Trim());
            }

            if (!string.IsNullOrEmpty(Tenant))
            {
                arguments.Add(CxConstants.FLAG_TENANT);
                arguments.Add(Tenant.Trim());
            }

            if (!string.IsNullOrEmpty(ApiKey))
            {
                arguments.Add(CxConstants.FLAG_API_KEY);
                arguments.Add(ApiKey.Trim());
            }

            if (!string.IsNullOrEmpty(AdditionalParameters))
            {
                arguments.AddRange(ParseAdditionalParameters());
            }

            return arguments;
        }

        private List<string> ParseAdditionalParameters()
        {
            List<string> additionalParameters = new List<string>();

            string pattern = "([^\"]\\S*|\".+?\")\\s*";
            Regex rg = new Regex(pattern);

            // TODO: check this validation. It's allowing all strings. It's giving error when validating. In Jetbrains ignores additional parameters
            MatchCollection parameters = rg.Matches(AdditionalParameters);

            foreach (Match parameter in parameters)
            {
                additionalParameters.Add(parameter.Value);
            }

            return additionalParameters;
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(BaseUri))
            {
                throw new InvalidCLIConfigException(CxConstants.EXCEPTION_URI_NOT_SET);
            }

            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new InvalidCLIConfigException(CxConstants.EXCEPTION_CREDENTIALS_NOT_SET);
            }
        }

        public sealed class InvalidCLIConfigException : Exception
        {
            public InvalidCLIConfigException(string message) : base(message) { }
        }
    }
}
