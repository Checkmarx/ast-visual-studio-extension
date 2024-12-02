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
        public bool AscaEnabled { get; set; } = false;


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
                arguments.AddRange(CxUtils.ParseAdditionalParameters(AdditionalParameters));
            }

            return arguments;
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
