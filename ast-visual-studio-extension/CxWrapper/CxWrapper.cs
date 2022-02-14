using ast_visual_studio_extension.Cx;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxCLI.Models;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ast_visual_studio_extension.CxCli
{
    internal class CxWrapper
    {
        private readonly CxConfig cxConfig;
        private readonly ILog logger;

        public CxWrapper(CxConfig cxConfiguration)
        {
            cxConfiguration.Validate();
            cxConfig = cxConfiguration;
            logger = LogManager.GetLogger(this.GetType());
        }

        public string AuthValidate()
        {
            logger.Info(CxConstants.LogRunningAuthValidateCommand);
            

            List<string> authValidateArguments = new List<string>
            {
                CxConstants.CLIAuthCmd,
                CxConstants.CLIValidateCmd
            };

            return Execution.ExecuteCommand(WithConfigArguments(authValidateArguments));
        }

        public Results GetResults(Guid scanId, ReportFormat reportFormat)
        {
            logger.Info(string.Format(CxConstants.LogRunningGetResultsCommand, scanId));

            string tempDir = Path.GetTempPath();
            string fileName = System.Guid.NewGuid().ToString();

            List<string> resultsArguments = new List<string>
            {
                CxConstants.CLIResultCmd,
                CxConstants.FlagScanId,
                scanId.ToString(),
                CxConstants.FlagReportFormat,
                reportFormat.ToString(),
                CxConstants.FlagOutputName,
                fileName,
                CxConstants.FlagOutputPath,
                tempDir
            };

            string results = Execution.ExecuteCommand(WithConfigArguments(resultsArguments), tempDir, fileName + ".json");

            return JsonConvert.DeserializeObject<Results>(results);
        }

        private List<string> WithConfigArguments(List<string> baseArguments)
        {
            List<string> arguments = new List<string>();
            arguments.AddRange(baseArguments);
            arguments.AddRange(cxConfig.ToArguments());

            return arguments;
        }
    }
}
