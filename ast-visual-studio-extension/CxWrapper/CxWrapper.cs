using ast_visual_studio_extension.CxWrapper.Exceptions;
using ast_visual_studio_extension.CxWrapper.Models;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ast_visual_studio_extension.CxCLI
{
    public class CxWrapper
    {
        private readonly CxConfig cxConfig;
        private readonly ILog logger;

        public CxWrapper(CxConfig cxConfiguration, Type type)
        {
            cxConfiguration.Validate();
            cxConfig = cxConfiguration;


            logger = LogManager.GetLogger(type);
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

            return Execution.ExecuteCommand(WithConfigArguments(authValidateArguments), line => line);
        }

        /// <summary>
        /// Get Results command
        /// </summary>
        /// <param name="scanId"></param>
        /// <param name="reportFormat"></param>
        /// <returns></returns>
        public Results GetResults(Guid scanId)
        {
            string results = GetResults(scanId.ToString(), ReportFormat.json);

            return JsonConvert.DeserializeObject<Results>(results);
        }

        /// <summary>
        /// Get Results Summary command
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        public ResultsSummary GetResultsSummary(string scanId)
        {
            string results = GetResults(scanId, ReportFormat.summaryJSON);

            return JsonConvert.DeserializeObject<ResultsSummary>(results);
        }

        /// <summary>
        /// Get Results with provided report format
        /// </summary>
        /// <param name="scanId"></param>
        /// <param name="reportFormat"></param>
        /// <returns></returns>
        public string GetResults(string scanId, ReportFormat reportFormat)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_GET_RESULTS_CMD, scanId));

            string tempDir = Path.GetTempPath();
            // Remove backslashes at the end of path, due to paths with spaces
            // \"C:\\My temp\\\"" -> "C:\My temp\" -> the last double quotes gets escaped
            // \"C:\\My temp\" -> "C:\My temp"
            if (tempDir.EndsWith("\\"))
            {
                tempDir = tempDir.Substring(0, tempDir.Length - 1);
            }

            string fileName = Guid.NewGuid().ToString();

            List<string> resultsArguments = new List<string>
            {
                CxConstants.CLI_RESULTS_CMD,
                CxConstants.CLI_SHOW_CMD,
                CxConstants.FLAG_SCAN_ID, scanId.ToString(),
                CxConstants.FLAG_REPORT_FORMAT, reportFormat.ToString(),
                CxConstants.FLAG_OUTPUT_NAME, fileName,
                CxConstants.FLAG_OUTPUT_PATH, tempDir,
                CxConstants.FLAG_AGENT, CxCLI.CxConstants.EXTENSION_AGENT,
            };

            string extension = string.Empty;

            switch (reportFormat)
            {
                case ReportFormat.json:
                    extension = ".json";
                    break;
                case ReportFormat.summaryJSON:
                    extension = ".json";
                    break;
                case ReportFormat.summaryHTML:
                    extension = ".html";
                    break;
            }

            return Execution.ExecuteCommand(WithConfigArguments(resultsArguments), tempDir, fileName + extension);
        }

        /// <summary>
        /// Get Projects command with default filter
        /// </summary>
        /// <returns></returns>
        public List<Project> GetProjects()
        {
            return GetProjects(CxConstants.LIMIT_FILTER);
        }

        /// <summary>
        /// Get Projects command with provided filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<Project> GetProjects(string filter)
        {
            logger.Info(CxConstants.LOG_RUNNING_GET_PROJECTS_CMD);

            List<string> resultsArguments = new List<string>
            {
                CxConstants.CLI_PROJECT_CMD,
                CxConstants.CLI_LIST_CMD,
                CxConstants.FLAG_FILTER,
                filter,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string projects = Execution.ExecuteCommand(WithConfigArguments(resultsArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<List<Project>>(projects);
        }

        /// <summary>
        /// Show project command
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public Project ProjectShow(string projectId)
        {
            logger.Info(string.Format(CxConstants.LOG_RUNNING_PROJECT_SHOW_CMD, projectId));

            List<string> projectShowArguments = new List<string>
            {
                CxConstants.CLI_PROJECT_CMD,
                CxConstants.CLI_SHOW_CMD,
                CxConstants.FLAG_PROJECT_ID,
                projectId,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string project = Execution.ExecuteCommand(WithConfigArguments(projectShowArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<Project>(project);
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

            string branches = Execution.ExecuteCommand(WithConfigArguments(branchesArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<List<string>>(branches);
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

            return GetScans(filter);
        }

        /// <summary>
        /// Get scans command with no filter
        /// </summary>
        /// <returns></returns>
        public List<Scan> GetScans()
        {
            logger.Info(CxConstants.LOG_RUNNING_GET_SCANS_CMD);

            return GetScans(string.Empty);
        }

        /// <summary>
        /// Get scans command with provided filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<Scan> GetScans(string filter)
        {
            List<string> scansArguments = new List<string>
            {
                CxConstants.CLI_SCAN_CMD,
                CxConstants.CLI_LIST_CMD,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            if (!string.IsNullOrEmpty(filter))
            {
                scansArguments.Add(CxConstants.FLAG_FILTER);
                scansArguments.Add(filter);
            }

            string scans = Execution.ExecuteCommand(WithConfigArguments(scansArguments), Execution.CheckValidJSONString);

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

            string scan = Execution.ExecuteCommand(WithConfigArguments(scanArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<Scan>(scan);
        }

        /// <summary>
        /// Scan show command
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        public async Task<Scan> ScanShowAsync(string scanId)
        {
            return await Task.Run(() => ScanShow(scanId));
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

            Execution.ExecuteCommand(WithConfigArguments(triageArguments), line => null);
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

            string predicates = Execution.ExecuteCommand(WithConfigArguments(triageArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<List<Predicate>>(predicates);
        }

        /// <summary>
        /// Codebashing link command
        /// </summary>
        /// <param name="cweId"></param>
        /// <param name="language"></param>
        /// <param name="queryName"></param>
        /// <returns></returns>
        public List<CodeBashing> CodeBashingList(string cweId, string language, string queryName)
        {
            logger.Info(CxConstants.LOG_RUNNING_CODEBASHING_CMD);

            List<string> codebashingArguments = new List<string>
            {
                CxConstants.CLI_RESULTS_CMD,
                CxConstants.CLI_CODEBASHING_CMD,
                CxConstants.FLAG_LANGUAGE,
                language,
                CxConstants.FLAG_VULNERABILITY_TYPE,
                queryName,
                CxConstants.FLAG_CWE_ID,
                cweId,
            };

            string codebashingLink = Execution.ExecuteCommand(WithConfigArguments(codebashingArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<List<CodeBashing>>(codebashingLink);
        }

        /// <summary>
        /// Learn More and Code Samples
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        public List<LearnMore> LearnMoreAndRemediation(string queryId)
        {
            List<string> learnMoreRemediation = new List<string>
            {
                CxConstants.CLI_UTILS_CMD,
                CxConstants.CLI_LEARN_MORE_CMD,
                CxConstants.FLAG_QUERY_ID,
                queryId,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE,
            };

            string learnMoreRemediationSamples = Execution.ExecuteCommand(WithConfigArguments(learnMoreRemediation), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<List<LearnMore>>(learnMoreRemediationSamples);
        }

        /// <summary>
        /// Tenant settings command
        /// </summary>
        /// <returns></returns>
        public List<TenantSetting> TenantSettings()
        {
            logger.Info(CxConstants.LOG_RUNNING_TENANT_SETTINGS_CMD);

            List<string> arguments = new List<string>
            {
                CxConstants.CLI_UTILS_CMD,
                CxConstants.CLI_TENANT_CMD,
                CxConstants.FLAG_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            string jsonStr = Execution.ExecuteCommand(WithConfigArguments(arguments), Execution.CheckValidJSONString);

            var tenantSettings = JsonConvert.DeserializeObject<List<TenantSetting>>(jsonStr);

            return tenantSettings ?? throw new CxException(1, "Unable to get tenant settings");
        }


        /// <summary>
        /// Check tenant settings for IDE scans enabled
        /// </summary>
        /// <returns></returns>
        public bool IdeScansEnabled()
        {
            List<TenantSetting> tenantSettings = TenantSettings();

            return bool.Parse(tenantSettings.Find(s => s.Key.Equals(CxConstants.IDE_SCANS_KEY)).Value);
        }


        /// <summary>
        /// Check tenant settings for IDE scans enabled
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IdeScansEnabledAsync()
        {
            return await Task.Run(() => IdeScansEnabled());
        }

        /// <summary>
        /// Scan create command
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="additionalParameters"></param>
        /// <returns></returns>
        public Scan ScanCreate(Dictionary<string, string> parameters, string additionalParameters)
        {
            logger.Info(CxConstants.LOG_RUNNING_SCAN_CREATE_CMD);

            List<string> scanCreateArguments = new List<string>
            {
                CxConstants.CLI_SCAN_CMD,
                CxConstants.CLI_CREATE_CMD,
                CxConstants.FLAG_SCAN_INFO_FORMAT,
                CxConstants.JSON_FORMAT_VALUE
            };

            foreach (KeyValuePair<string, string> entry in parameters)
            {
                scanCreateArguments.Add(entry.Key);
                scanCreateArguments.Add(entry.Value);
            }

            scanCreateArguments.AddRange(CxUtils.ParseAdditionalParameters(additionalParameters));

            string scan = Execution.ExecuteCommand(WithConfigArguments(scanCreateArguments), Execution.CheckValidJSONString);

            return JsonConvert.DeserializeObject<Scan>(scan);
        }

        /// <summary>
        /// Scan create command async
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="additionalParameters"></param>
        /// <returns></returns>
        public async Task<Scan> ScanCreateAsync(Dictionary<string, string> parameters, string additionalParameters)
        {
            return await Task.Run(() => ScanCreate(parameters, additionalParameters));
        }

        /// <summary>
        /// Scan cancel command
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        public void ScanCancel(string scanId)
        {
            logger.Info(CxConstants.LOG_RUNNING_SCAN_CANCEL_CMD);

            List<string> scanCancelArguments = new List<string>
            {
                CxConstants.CLI_SCAN_CMD,
                CxConstants.CLI_CANCEL_CMD,
                CxConstants.FLAG_SCAN_ID,
                scanId
            };

            Execution.ExecuteCommand(WithConfigArguments(scanCancelArguments), line => null);
        }

        /// <summary>
        /// Scan cancel command
        /// </summary>
        /// <param name="scanId"></param>
        /// <returns></returns>
        public async Task ScanCancelAsync(string scanId)
        {

            await Task.Run(() => ScanCancel(scanId));
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
