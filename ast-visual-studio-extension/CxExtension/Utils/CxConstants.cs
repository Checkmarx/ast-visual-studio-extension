using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class CxConstants
    {
        /************ GENERAL ************/
        public static string EXTENSION_TITLE => "Checkmarx";
        public static string TREE_PARENT_NODE => "Scan: {0}";
        public static string DESC_TAB_LBL_ACTUAL_VALUE => "Actual Value: ";
        public static string DESC_TAB_LBL_EXPECTED_VALUE => "Expected Value: ";

        /************ ICONS ************/
        public static string ICON_HIGH => "high.png";
        public static string ICON_HIGH_TITLE => "high_title.png";
        public static string ICON_MEDIUM => "medium.png";
        public static string ICON_MEDIUM_TITLE => "medium_title.png";
        public static string ICON_LOW => "low.png";
        public static string ICON_LOW_TITLE => "low_title.png";
        public static string ICON_INFO => "info.png";
        public static string ICON_INFO_TITLE => "info_title.png";

        /************ PROJECT FOLDERS ************/
        public static string FOLDER_RESOURCES => "Resources";
        public static string FOLDER_CX_EXTENSION => "CxExtension";

        /************ INFO MESSAGES ************/
        public static string INFO_GETTING_RESULTS => "Getting results...";
    }
}
