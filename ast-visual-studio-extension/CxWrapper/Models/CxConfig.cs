using ast_visual_studio_extension.CxCLI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ast_visual_studio_extension.CxWrapper.Models
{
    public class CxConfig
    {
        public string ApiKey { get; set; }
        public string AdditionalParameters { get; set; }

        public List<string> ToArguments()
        {
            List<string> arguments = new List<string>();    

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
