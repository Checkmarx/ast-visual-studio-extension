
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

        /************ PROJECT FOLDERS ************/
        public static string FOLDER_RESOURCES => "Resources";
        public static string FOLDER_CX_EXTENSION => "CxExtension";

        /************ INFO MESSAGES ************/
        public static string INFO_GETTING_RESULTS => "Getting results...";

        /************ NOTIFICATIONS ************/
        public static string NOTIFY_FILE_NOT_FOUND_TITLE => "File {0} not found in the solution";
        public static string NOTIFY_FILE_NOT_FOUND_DESCRIPTION => "Please ensure you are in the correct scan";

        /************ TOOLBAR ************/
        public static string TOOLBAR_SELECT_PROJECT => "Select a project";
        public static string TOOLBAR_SELECT_BRANCH => "Select a branch";
        public static string TOOLBAR_SELECT_SCAN => "Select a scan";
        public static string TOOLBAR_LOADING_PROJECTS => "Loading projects...";
        public static string TOOLBAR_LOADING_BRANCHES => "Loading branches...";
        public static string TOOLBAR_LOADING_SCANS => "Loading scans...";
    }
}
