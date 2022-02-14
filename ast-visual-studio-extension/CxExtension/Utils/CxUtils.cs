using ast_visual_studio_extension.CxExtension.Enums;
using System;
using System.IO;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class CxUtils
    {
        /// <summary>
        /// Get the Icon path from a given severity
        /// </summary>
        /// <param name="severity"></param>
        /// <returns>Icon path related to the given severity</returns>
        public static string GetIconPathFromSeverity(string severity, Boolean iconForTitle)
        {
            switch (GetSeverityFromString(severity))
            {
                case Severity.HIGH:
                    return Path.Combine(Environment.CurrentDirectory, CxConstants.FOLDER_CX_EXTENSION, CxConstants.FOLDER_RESOURCES, iconForTitle ? CxConstants.ICON_HIGH_TITLE : CxConstants.ICON_HIGH);
                case Severity.MEDIUM:
                    return Path.Combine(Environment.CurrentDirectory, CxConstants.FOLDER_CX_EXTENSION, CxConstants.FOLDER_RESOURCES, iconForTitle ? CxConstants.ICON_MEDIUM_TITLE : CxConstants.ICON_MEDIUM);
                case Severity.LOW:
                    return Path.Combine(Environment.CurrentDirectory, CxConstants.FOLDER_CX_EXTENSION, CxConstants.FOLDER_RESOURCES, iconForTitle ? CxConstants.ICON_LOW_TITLE : CxConstants.ICON_LOW);
                case Severity.INFO:
                    return Path.Combine(Environment.CurrentDirectory, CxConstants.FOLDER_CX_EXTENSION, CxConstants.FOLDER_RESOURCES, iconForTitle ? CxConstants.ICON_INFO_TITLE : CxConstants.ICON_INFO);
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts a severity into its corresponding enum
        /// </summary>
        /// <param name="severity"></param>
        /// <returns></returns>
        public static Severity GetSeverityFromString(string severity)
        {
            Enum.TryParse(severity, out Severity resultSeverity);

            return resultSeverity;
        }
    }
}
