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
            logger = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Auth Validate command
        /// </summary>
        /// <returns></returns>
        public string AuthValidate()
        {
            logger.Info(CxConstants.LOG_RUNNING_AUTH_VALIDATE_CMD);
            
            List<string> authValidateArguments = new List<string>
            {
                CxConstants.CLI_AUTH_CMD,
                CxConstants.CLI_VALIDATE_CMD
            };

            return Execution.ExecuteCommand(WithConfigArguments(authValidateArguments));
        }

        /// <summary>
        /// Get Results command
        /// </summary>
        /// <param name="scanId"></param>
        /// <param name="reportFormat"></param>
        /// <returns></returns>
        public Results GetResults(Guid scanId, ReportFormat reportFormat)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_GET_RESULTS_CMD, scanId));

            string tempDir = Path.GetTempPath();
            string fileName = System.Guid.NewGuid().ToString();

            List<string> resultsArguments = new List<string>
            {
                CxConstants.CLI_RESULTS_CMD,
                CxConstants.CLI_SHOW_CMD,
                CxConstants.FLAG_SCAN_ID,
                scanId.ToString(),
                CxConstants.FLAG_REPORT_FORMAT,
                reportFormat.ToString(),
                CxConstants.FLAG_OUTPUT_NAME,
                fileName,
                CxConstants.FLAG_OUTPUT_PATH,
                tempDir
            };

            string results = Execution.ExecuteCommand(WithConfigArguments(resultsArguments), tempDir, fileName + CxConstants.EXTENSION_JSON);

            return JsonConvert.DeserializeObject<Results>(results);
        }

        /// <summary>
        /// Get Projects command
        /// </summary>
        /// <returns></returns>
        public List<Project> GetProjects()
        {
            logger.Info(CxConstants.LOG_RUNNING_GET_PROJECTS_CMD);

            List<string> resultsArguments = new List<string>
            {
                CxConstants.CLI_PROJECT_CMD,
                CxConstants.CLI_LIST_CMD,
                CxConstants.FLAG_FILTER,
                CxConstants.LIMIT_FILTER,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string projects = Execution.ExecuteCommand(WithConfigArguments(resultsArguments));

            return JsonConvert.DeserializeObject<List<Project>>(projects);
        }

        /// <summary>
        /// Get Branches command
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public List<string> GetBranches(string projectId)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_GET_BRANCHES_CMD, projectId));

            List<string> branchesArguments = new List<string>
            {
                CxConstants.CLI_PROJECT_CMD,
                CxConstants.CLI_BRANCHES_CMD,
                CxConstants.FLAG_PROJECT_ID,
                projectId
            };

            string branches = Execution.ExecuteCommand(WithConfigArguments(branchesArguments));

            return JsonConvert.DeserializeObject<List<string>>(branches); ;
        }

        /// <summary>
        /// Get scans command
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public List<Scan> GetScans(string projectId, string branch)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_GET_SCANS_FOR_BRANCH_CMD, branch));

            string filter = string.Format(CxConstants.FILTER_SCANS_FOR_BRANCH, projectId, branch);

            List<string> scansArguments = new List<string>
            {
                CxConstants.CLI_SCAN_CMD,
                CxConstants.CLI_LIST_CMD,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE,
                CxConstants.FLAG_FILTER,
                filter
            };

            string scans = Execution.ExecuteCommand(WithConfigArguments(scansArguments));

            return JsonConvert.DeserializeObject<List<Scan>>(scans);
        }

        /// <summary>
        /// Scan show command
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        public Scan ScanShow(string scanId)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_GET_SCAN_DETAILS_CMD, scanId));

            List<string> scanArguments = new List<string>
            {
                CxConstants.CLI_SCAN_CMD,
                CxConstants.CLI_SHOW_CMD,
                CxConstants.FLAG_SCAN_ID,
                scanId,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string scan = Execution.ExecuteCommand(WithConfigArguments(scanArguments));

            return JsonConvert.DeserializeObject<Scan>(scan);
        }

        /// <summary>
        /// Triage Update command
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="similarityId"></param>
        /// <param name="scanType"></param>
        /// <param name="state"></param>
        /// <param name="comment"></param>
        /// <param name="severity"></param>
        public void TriageUpdate(string projectId, string similarityId, string scanType, string state, string comment, string severity)
        {
            logger.Info(CxConstants.LOG_RUNNING_TRIAGE_UPDATE_CMD);
            logger.Info(string.Format(CxConstants.LOG_RUNNING_TRIAGE_UPDATE_INFO_CMD, similarityId, state, severity));

            List<string> triageArguments = new List<string>
            {
                CxConstants.CLI_TRIAGE_CMD,
                CxConstants.CLI_UPDATE_CMD,
                CxConstants.FLAG_PROJECT_ID,
                projectId,
                CxConstants.FLAG_SIMILARITY_ID,
                similarityId,
                CxConstants.FLAG_SCAN_TYPE,
                scanType,
                CxConstants.FLAG_STATE,
                state
            };

            if (!string.IsNullOrEmpty(comment))
            {
                triageArguments.Add(CxConstants.FLAG_COMMENT);
                triageArguments.Add(comment);
            }

            triageArguments.Add(CxConstants.FLAG_SEVERITY);
            triageArguments.Add(severity);

            Execution.ExecuteCommand(WithConfigArguments(triageArguments));
        }

        /// <summary>
        /// Triage Show command
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="similarityId"></param>
        /// <param name="scanType"></param>
        /// <returns></returns>
        public List<Predicate> TriageShow(string projectId, string similarityId, string scanType)
        {
            logger.Info(CxConstants.LOG_RUNNING_TRIAGE_SHOW_CMD);
            logger.Info(string.Format(CxConstants.LOG_RUNNING_TRIAGE_SHOW_INFO_CMD, projectId, similarityId, scanType));

            List<string> triageArguments = new List<string>
            {
                CxConstants.CLI_TRIAGE_CMD,
                CxConstants.CLI_SHOW_CMD,
                CxConstants.FLAG_PROJECT_ID,
                projectId,
                CxConstants.FLAG_SIMILARITY_ID,
                similarityId,
                CxConstants.FLAG_SCAN_TYPE,
                scanType,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string predicates = Execution.ExecuteCommand(WithConfigArguments(triageArguments));

            return JsonConvert.DeserializeObject<List<Predicate>>(predicates);
        }

        /// <summary>
        /// Add base arguments to command
        /// </summary>
        /// <param name="baseArguments"></param>
        /// <returns></returns>
        private List<string> WithConfigArguments(List<string> baseArguments)
        {
            List<string> arguments = new List<string>();
            arguments.AddRange(baseArguments);
            arguments.AddRange(cxConfig.ToArguments());

            return arguments;
        }
    }
}
