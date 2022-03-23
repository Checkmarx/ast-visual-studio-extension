namespace ast_visual_studio_extension.CxCLI
{
    internal static class CxConstants
    {
        /** GENERAL **/
        public static string LIMIT_FILTER => "limit=10000";
        public static string JSON_FORMAT_VALUE => "json";
        public static string FILTER_SCANS_FOR_BRANCH => "project-id={0},branch={1},limit=10000,statuses=Completed";

        /** CLI COMMANDS **/
        public static string CLI_AUTH_CMD => "auth";
        public static string CLI_VALIDATE_CMD => "validate";
        public static string CLI_RESULT_CMD => "result";
        public static string CLI_PROJECT_CMD => "project";
        public static string CLI_LIST_CMD => "list";
        public static string CLI_BRANCHES_CMD => "branches";
        public static string CLI_SCAN_CMD => "scan";
        public static string CLI_SHOW_CMD => "show";
        public static string CLI_TRIAGE_CMD => "triage";
        public static string CLI_UPDATE_CMD => "update";

        /** CLI FLAGS **/
        public static string FLAG_BASE_URI => "--base-uri";
        public static string FLAG_BASE_AUTH_URI => "--base-auth-uri";
        public static string FLAG_TENANT => "--tenant";
        public static string FLAG_API_KEY => "--apikey";
        public static string FLAG_SCAN_ID => "--scan-id";
        public static string FLAG_REPORT_FORMAT => "--report-format";
        public static string FLAG_OUTPUT_NAME => "--output-name";
        public static string FLAG_OUTPUT_PATH => "--output-path";
        public static string FLAG_FILTER => "--filter";
        public static string FLAG_FORMAT => "--format";
        public static string FLAG_PROJECT_ID => "--project-id";
        public static string FLAG_SIMILARITY_ID => "--similarity-id";
        public static string FLAG_SCAN_TYPE => "--scan-type";
        public static string FLAG_STATE => "--state";
        public static string FLAG_COMMENT => "--comment";
        public static string FLAG_SEVERITY => "--severity";
        public static string FLAG_DEBUG => "--debug";

        /** EXCEPTIONS **/
        public static string EXCEPTION_URI_NOT_SET => "Checkmarx server URL is not set";
        public static string EXCEPTION_CREDENTIALS_NOT_SET => "Credentials are not set";

        /** LOGGING **/
        public static string LOG_RUNNING_AUTH_VALIDATE_CMD => "Initialized authentication validation command";
        public static string LOG_RUNNING_GET_RESULTS_CMD => "Retrieving the scan result for scan id {0}...";
        public static string LOG_RUNNING_GET_PROJECTS_CMD => "Getting projects...";
        public static string LOG_RUNNING_GET_BRANCHES_CMD => "Getting branches for project id {0}...";
        public static string LOG_RUNNING_GET_SCANS_FOR_BRANCH_CMD => "Getting scans for branch {0}...";
        public static string LOG_RUNNING_GET_SCAN_DETAILS_CMD => "Retrieving details for scan id {0}...";
        public static string LOG_RUNNING_TRIAGE_UPDATE_CMD => "Executing 'triage update' command using the CLI...";
        public static string LOG_RUNNING_TRIAGE_UPDATE_INFO_CMD => "Updating the similarityId {0} with state {1} and severity {2}...";
        public static string LOG_RUNNING_TRIAGE_SHOW_CMD => "Executing 'triage show' command using the CLI...";
        public static string LOG_RUNNING_TRIAGE_SHOW_INFO_CMD => "Fetching the list of predicates for projectId {0} , similarityId {1} and scan-type {2}...";

        /** FILE EXTENSIONS **/
        public static string EXTENSION_JSON => ".json";
    }
}
