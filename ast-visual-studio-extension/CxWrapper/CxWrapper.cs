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

        public List<Project> GetProjects()
        {
            logger.Info("Getting projects");
            List<string> resultsArguments = new List<string>
            {
                "project",
                "list",
                "--format",
                "json"
            };

            string projects = Execution.ExecuteCommand(WithConfigArguments(resultsArguments));

            return JsonConvert.DeserializeObject<List<Project>>(projects);
        }

        public List<string> GetBranches(string projectId)
        {
            logger.Info("Getting branches for project id...");

            List<string> branchesArguments = new List<string>
            {
                "project",
                "branches",
                "--project-id",
                projectId
            };

            string branches = Execution.ExecuteCommand(WithConfigArguments(branchesArguments));

            return JsonConvert.DeserializeObject<List<string>>(branches); ;
        }

        public List<Scan> GetScans(string projectId, string branch)
        {
            logger.Info("Getting scans for branch...");

            string filter = string.Format("project-id={0},branch={1},limit=10000,statuses=Completed", projectId, branch);

            List<string> scansArguments = new List<string>
            {
                "scan",
                "list",
                "--format",
                "json",
                "--filter",
                filter
            };

            string scans = Execution.ExecuteCommand(WithConfigArguments(scansArguments));

            return JsonConvert.DeserializeObject<List<Scan>>(scans);
        }

        public Scan ScanShow(string scanId)
        {
            logger.Info("Retrieving the details for scan id: " + scanId);

            List<string> scanArguments = new List<string>
            {
                "scan",
                "show",
                "--scan-id",
                scanId,
                "--format",
                "json"
            };

            string scan = Execution.ExecuteCommand(WithConfigArguments(scanArguments));

            return JsonConvert.DeserializeObject<Scan>(scan);
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
