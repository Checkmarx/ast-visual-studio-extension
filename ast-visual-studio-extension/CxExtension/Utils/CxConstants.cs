namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class CxConstants
    {
        /************ GENERAL ************/
        public static string EXTENSION_TITLE => "Checkmarx";
        public static string TREE_PARENT_NODE => "Scan: {0}";
        public static string TREE_PARENT_NODE_NO_RESULTS => "No results for scan {0}";
        public static string DESC_TAB_LBL_ACTUAL_VALUE => "Actual Value: ";
        public static string DESC_TAB_LBL_EXPECTED_VALUE => "Expected Value: ";
        public static string LBL_ATTACK_VECTOR => "Attack Vector";
        public static string LBL_PACKAGE_DATA => "Package Data";
        public static string LBL_LOCATION => "Location";
        public static string LBL_ATTACK_VECTOR_ITEM => "{0}. {1} ";
        public static string LBL_LOCATION_FILE => "File ";
        public static string INVALID_SCAN_ID => "Invalid scan id ";
        public static string SCAN_ID_DISPLAY_FORMAT => "{0}    {1}";
        public static string RESOURCES_BASE_DIR => "pack://application:,,,/ast-visual-studio-extension;component/CxExtension/Resources/";
        public static int FILE_PATH_MAX_LEN => 45;
        public static string COLLAPSE_CRUMB => "...";

        /************ ICONS ************/
        public static string ICON_HIGH => "high.png";
        public static string ICON_HIGH_TITLE => "high_18x22.png";
        public static string ICON_MEDIUM => "medium.png";
        public static string ICON_MEDIUM_TITLE => "medium_18x22.png";
        public static string ICON_LOW => "low.png";
        public static string ICON_LOW_TITLE => "low_18x22.png";
        public static string ICON_INFO => "info.png";
        public static string ICON_INFO_TITLE => "info_18x22.png";
        public static string ICON_FLAG => "Flag.png";
        public static string ICON_COMMENT => "Comment.png";
        public static string ICON_CX_LOGO_INITIAL_PANEL => "checkmarx-80.png";

        /************ PROJECT FOLDERS ************/
        public static string FOLDER_RESOURCES => "Resources";
        public static string FOLDER_CX_EXTENSION => "CxExtension";

        /************ INFO MESSAGES ************/
        public static string INFO_GETTING_RESULTS => "Getting results...";

        /************ ERROR MESSAGES ************/
        public static string ERROR_GETTING_CODEBASHING_LINK => "An error occurred while getting codebashing link: {0}";

        /************ NOTIFICATIONS ************/
        public static string NOTIFY_FILE_NOT_FOUND => "File {0} not found in the solution. Please ensure you are in the correct scan.";

        public static string NOTIFY_SOLUTION_NOT_FOUND => "Unable to open file. Please ensure you have an open project or loaded solution.";

        /************ TOOLBAR ************/
        public static string TOOLBAR_SELECT_PROJECT => "Select a project";
        public static string TOOLBAR_SELECT_BRANCH => "Select a branch";
        public static string TOOLBAR_SELECT_SCAN => "Select a scan";
        public static string TOOLBAR_LOADING_PROJECTS => "Loading projects...";
        public static string TOOLBAR_LOADING_BRANCHES => "Loading branches...";
        public static string TOOLBAR_LOADING_SCANS => "Loading scans...";

        /************ TRIAGE ************/
        public static string TRIAGE_COMMENT_PLACEHOLDER => "Comment (Optional)";
        public static string TRIAGE_UPDATE_FAILED => "Triage Update failed. {0}";
        public static string TRIAGE_SHOW_FAILED => "Triage Show failed. {0}";
        public static string TRIAGE_LOADING_CHANGES => "Loading changes...";
        public static string TRIAGE_NO_CHANGES => "No changes.";
        public static string TRIAGE_SCA_NOT_AVAILABLE => "Changes not available for sca engine.";

        /************ DATE FORMATS ************/
        public static string DATE_OUTPUT_FORMAT => "dd/MM/yyyy HH:mm:ss";

        /************ EXIT CODES ************/
        public static int LICENSE_NOT_FOUND_EXIT_CODE => 3;
        public static int LESSON_NOT_FOUND_EXIT_CODE => 4;

        /************ CODEBASHING ************/
        public static string CODEBASHING_NO_LESSON => "Currently, this vulnerability has no lesson.";
        public static string CODEBASHING_NO_LICENSE => "You don't have a license for Codebashing. Please Contact your Admin for the full version implementation. Meanwhile, you can use:";
        public static string CODEBASHING_LINK => "https://free.codebashing.com";
        public static string CODEBASHING_OPEN_HTTP_LINK_ID => "OpenHTTPLink";
        public static string CODEBASHING_LINK_TOOLTIP => "Learn more about {0} using Checkmarx's eLearning platform";

        /************ SCANNING ************/
        public static string STATUS_CREATING_SCAN => "Checkmarx: Creating scan";
        public static string STATUS_CREATING_SCAN_FAILED => "Checkmarx: Failed creating scan";
        public static string STATUS_FORMAT_CREATED_SCAN => "Checkmarx: Scan created with ID {0}";
        public static string STATUS_FORMAT_POLLING_SCAN => "Checkmarx: Scan {0} is {1}";
        public static string STATUS_FORMAT_POLLING_SCAN_FAILED => "Checkmarx: Failed polling scan {0}";
        public static string STATUS_FORMAT_CANCELLING_SCAN => "Checkmarx: Cancelling scan {0}";
        public static string STATUS_FORMAT_FINISHED_SCAN => "Checkmarx: Scan {0} finished with status {1}";
        public static string INFOBAR_SCAN_COMPLETED => "Checkmarx scan completed successfully.";
        public static string INFOBAR_RESULTS_LINK => "Load scan results";
        public static string BRANCH_DOES_NOT_MATCH => "The Git branch open in your workspace isn't the same as the branch that was previously scanned in this Checkmarx project. Do you want to scan anyway?";
        public static string PROJECT_DOES_NOT_MATCH => "The files open in your workspace don't match the files previously scanned in this Checkmarx project. Do you want to scan anyway?";
        public static string PROJECT_AND_BRANCH_DO_NOT_MATCH => "The Git branch and files open in your workspace don't match the branch and project that were previously scanned in this Checkmarx project. Do you want to scan anyway?";
        public static string RUN_SCAN => "Run scan";
        public static string RUN_SCAN_ACTION => "RUN_SCAN_ACTION";

        /** LEARN MORE AND REMEDIATION **/
        public static string CODE_SAMPLE_TITLE => "{0} using {1}";
        public static string NO_INFORMATION => "No information";
        public static string LOADING_INFORMATION => "Loading Information...";

        public static string RISK => "Risk";
        public static string CAUSE => "Cause";
        public static string GENERAL_RECOMENDATIONS => "General Recommendations";

        public static string ERROR_GETTING_LEARNMORE => "ERROR GETTING LEARN MORE AND REMEDIATIONS";

    }
}
