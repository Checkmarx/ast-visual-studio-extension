namespace ast_visual_studio_extension.CxCLI
{
    public static class CxConstants
    {
        /** GENERAL **/
        public static string EXTENSION_AGENT => "Visual Studio";
        public static string LIMIT_FILTER => "limit=10000";
        public static string JSON_FORMAT_VALUE => "json";
        public static string FILTER_SCANS_FOR_BRANCH => "project-id={0},branch={1},limit=10000,statuses=Completed";
        public static string IDE_SCANS_KEY = "scan.config.plugins.ideScans";
        public static string ASCA_ENGINE_STARTED_MESSAGE = "AI Secure Coding Assistant Engine started";

        /** CLI COMMANDS **/
        public static string CLI_AUTH_CMD => "auth";
        public static string CLI_VALIDATE_CMD => "validate";
        public static string CLI_RESULTS_CMD => "results";
        public static string CLI_PROJECT_CMD => "project";
        public static string CLI_LIST_CMD => "list";
        public static string CLI_BRANCHES_CMD => "branches";
        public static string CLI_SCAN_CMD => "scan";
        public static string CLI_ASCA_CMD => "asca";
        public static string CLI_SHOW_CMD => "show";
        public static string CLI_GET_STATES_CMD => "get-states";
        public static string CLI_TRIAGE_CMD => "triage";
        public static string CLI_UPDATE_CMD => "update";
        public static string CLI_CODEBASHING_CMD => "codebashing";
        public static string CLI_CREATE_CMD => "create";
        public static string CLI_CANCEL_CMD => "cancel";
        public static string CLI_UTILS_CMD = "utils";
        public static string CLI_TENANT_CMD = "tenant";
        public static string CLI_LEARN_MORE_CMD => "learn-more";


        /** CLI FLAGS **/
        public static string FLAG_API_KEY => "--apikey";
        public static string FLAG_SCAN_ID => "--scan-id";
        public static string FLAG_FILE_SOURCE => "--file-source";
        public static string FLAG_ASCA_LATEST_VERSION => "--asca-latest-version";
        public static string FLAG_REPORT_FORMAT => "--report-format";
        public static string FLAG_OUTPUT_NAME => "--output-name";
        public static string FLAG_OUTPUT_PATH => "--output-path";
        public static string FLAG_FILTER => "--filter";
        public static string FLAG_FORMAT => "--format";
        public static string FLAG_PROJECT_ID => "--project-id";
        public static string FLAG_PROJECT_NAME => "--project-name";
        public static string FLAG_SIMILARITY_ID => "--similarity-id";
        public static string FLAG_SCAN_TYPE => "--scan-type";
        public static string FLAG_STATE => "--state";
        public static string FLAG_ALL => "--all";
        public static string FLAG_COMMENT => "--comment";
        public static string FLAG_SEVERITY => "--severity";
        public static string FLAG_DEBUG => "--debug";
        public static string FLAG_LANGUAGE => "--language";
        public static string FLAG_VULNERABILITY_TYPE => "--vulnerability-type";
        public static string FLAG_CWE_ID => "--cwe-id";
        public static string FLAG_SCAN_INFO_FORMAT => "--scan-info-format";
        public static string FLAG_BRANCH => "--branch";
        public static string FLAG_FILE_FILTER => "--file-filter";
        public static string FLAG_SAST_PRESET_NAME => "--sast-preset-name";
        public static string FLAG_AGENT => "--agent";
        public static string FLAG_SOURCE => "-s";
        public static string FLAG_ASYNC => "--async";
        public static string FLAG_INCREMENTAL => "--sast-incremental";
        public static string FLAG_RESUBMIT => "--resubmit";
        public static string FLAG_QUERY_ID => "--query-id";


        /** EXCEPTIONS **/
        public static string EXCEPTION_CREDENTIALS_NOT_SET => "Credentials are not set";

        /** FILE EXTENSIONS **/
        public static string EXTENSION_JSON => ".json";

        /** LOGGING MESSAGES **/
        public static string LOG_CLI_COMMAND_EXECUTING => "Executing CLI command: {0}";
        public static string LOG_CLI_COMMAND_COMPLETED => "CLI command completed with result length: {0}";
        public static string LOG_CLI_COMMAND_ERROR => "CLI command failed: {0}";
        public static string LOG_INITIALIZATION => "Initializing CxWrapper with configuration";
        public static string LOG_RUNNING_ASCA_SCAN_CMD => "Running ASCA scan command for file: {0}";
        public static string LOG_RUNNING_AUTH_VALIDATE_CMD => "Running auth validate command";
        public static string LOG_RUNNING_GET_RESULTS_CMD => "Getting results for scan ID: {0}";
        public static string LOG_RUNNING_GET_PROJECTS_CMD => "Getting projects list";
        public static string LOG_RUNNING_PROJECT_SHOW_CMD => "Getting project details for ID: {0}";
        public static string LOG_RUNNING_GET_BRANCHES_CMD => "Getting branches for project ID: {0}";
        public static string LOG_RUNNING_GET_SCANS_FOR_BRANCH_CMD => "Getting scans for branch: {0}";
        public static string LOG_RUNNING_GET_SCANS_CMD => "Getting all scans";
        public static string LOG_RUNNING_GET_SCAN_DETAILS_CMD => "Getting scan details for ID: {0}";
        public static string LOG_RUNNING_TRIAGE_UPDATE_CMD => "Running triage update command";
        public static string LOG_RUNNING_TRIAGE_UPDATE_INFO_CMD => "Updating triage for similarity ID: {0}, state: {1}, severity: {2}";
        public static string LOG_RUNNING_TRIAGE_SHOW_CMD => "Running triage show command";
        public static string LOG_RUNNING_TRIAGE_SHOW_INFO_CMD => "Getting triage info for project ID: {0}, similarity ID: {1}, scan type: {2}";
        public static string LOG_RUNNING_TRIAGE_GET_STATES_CMD => "Getting triage states";
        public static string LOG_RUNNING_CODEBASHING_CMD => "Getting Codebashing links";
        public static string LOG_RUNNING_TENANT_SETTINGS_CMD => "Getting tenant settings";
        public static string LOG_RUNNING_SCAN_CREATE_CMD => "Creating new scan";
        public static string LOG_RUNNING_SCAN_CANCEL_CMD => "Canceling scan";

        /** SCAN STATUS **/
        public static string SCAN_RUNNING => "running";
        public static string SCAN_PARTIAL => "partial";
        public static string SCAN_COMPLETED => "completed";

    }
}
