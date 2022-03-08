using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ast_visual_studio_extension.CxCLI
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
                arguments.Add(CxConstants.FlagBaseURI);
                arguments.Add(BaseUri);
            }

            if (!string.IsNullOrEmpty(BaseAuthURI))
            {
                arguments.Add(CxConstants.FlagBaseAuthURI);
                arguments.Add(BaseAuthURI);
            }

            if (!string.IsNullOrEmpty(Tenant))
            {
                arguments.Add(CxConstants.FlagTenant);
                arguments.Add(Tenant);
            }

            if (!string.IsNullOrEmpty(ApiKey))
            {
                arguments.Add(CxConstants.FlagAPIKey);
                arguments.Add(ApiKey);
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
                throw new InvalidCLIConfigException(CxConstants.ExceptionURINotSet);
            }

            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new InvalidCLIConfigException(CxConstants.ExceptionCredentialsNotSet);
            }
        }

        public sealed class InvalidCLIConfigException : Exception
        {
            public InvalidCLIConfigException(string message) : base(message) { }
        }
    }
}
