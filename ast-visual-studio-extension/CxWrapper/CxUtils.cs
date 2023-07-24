using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ast_visual_studio_extension.CxCLI
{
    public class CxUtils
    {
        public static List<string> ParseAdditionalParameters(String additionalParameters)
        {
            List<string> additionalParametersList = new List<string>();
            if (!string.IsNullOrEmpty(additionalParameters))
            {
                // regex to exclude spaces and search for "" and ''
                foreach (Match match in Regex.Matches(additionalParameters, "(?:[^\\s\"']+|\"[^\"]*\"|'[^\']*')+", RegexOptions.IgnoreCase))
                {
                    additionalParametersList.Add(match.Captures[0].Value.Replace("\'", "\""));
                }
            }
            return additionalParametersList;
        }
    }
}
