namespace ast_visual_studio_extension.CxCLI
{
    internal static class CxConstants
    {
        /** CLI COMMANDS **/
        public static string CLIAuthCmd => "auth";
        public static string CLIValidateCmd => "validate";
        public static string CLIResultCmd => "result";

        /** CLI FLAGS **/
        public static string FlagBaseURI => "--base-uri";
        public static string FlagBaseAuthURI => "--base-auth-uri";
        public static string FlagTenant => "--tenant";
        public static string FlagAPIKey => "--apikey";
        public static string FlagScanId => "--scan-id";
        public static string FlagReportFormat => "--report-format";
        public static string FlagOutputName => "--output-name";
        public static string FlagOutputPath => "--output-path";

        /** EXCEPTIONS **/
        public static string ExceptionURINotSet => "Checkmarx server URL is not set";
        public static string ExceptionCredentialsNotSet => "Credentials are not set";

        /** LOGGING **/
        public static string LogRunningAuthValidateCommand => "Initialized authentication validation command";
        public static string LogRunningGetResultsCommand => "Retrieving the scan result for scan id {0}";
    }
}
