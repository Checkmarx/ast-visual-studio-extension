using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxPreferences;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Windows.Controls;

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
                case Severity.CRITICAL:
                    return Path.Combine(CxConstants.RESOURCES_BASE_DIR, iconForTitle ? CxConstants.ICON_CRITICAL_TITLE   : CxConstants.ICON_CRITICAL);
                case Severity.HIGH:
                    return Path.Combine(CxConstants.RESOURCES_BASE_DIR, iconForTitle ? CxConstants.ICON_HIGH_TITLE : CxConstants.ICON_HIGH);
                case Severity.MEDIUM:
                    return Path.Combine(CxConstants.RESOURCES_BASE_DIR, iconForTitle ? CxConstants.ICON_MEDIUM_TITLE : CxConstants.ICON_MEDIUM);
                case Severity.LOW:
                    return Path.Combine(CxConstants.RESOURCES_BASE_DIR, iconForTitle ? CxConstants.ICON_LOW_TITLE : CxConstants.ICON_LOW);
                case Severity.INFO:
                    return Path.Combine(CxConstants.RESOURCES_BASE_DIR, iconForTitle ? CxConstants.ICON_INFO_TITLE : CxConstants.ICON_INFO);
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

        /// <summary>
        /// Create a wrapper to call CLI
        /// </summary>
        /// <param name="package"></param>
        /// <param name="resultsTree"></param>
        /// <returns></returns>
        public static CxCLI.CxWrapper GetCxWrapper(AsyncPackage package, TreeView resultsTree, Type type)
        {
            try
            {
                CxPreferencesModule preferences = (CxPreferencesModule) package.GetDialogPage(typeof(CxPreferencesModule));
                CxConfig configuration = preferences.GetCxConfig();

                return new CxCLI.CxWrapper(configuration, type);
            }
            catch (Exception e)
            {
                resultsTree.Items.Clear();
                resultsTree.Items.Add(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Get item's index in combobox
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cxToolbar"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetItemIndexInCombo(string value, ComboBox combobox, ComboboxType type)
        {
            for (var i = 0; i < combobox.Items.Count; i++)
            {
                ComboBoxItem item = combobox.Items[i] as ComboBoxItem;

                string valueToCheck = GetValueToCompareWith(type, item);

                if (valueToCheck.Equals(value)) return i;
            }

            return -1;
        }

        /// <summary>
        /// Get value to compare with to retrieve combobox item index
        /// </summary>
        /// <param name="type"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static string GetValueToCompareWith(ComboboxType type, ComboBoxItem item)
        {
            switch (type)
            {
                case ComboboxType.PROJECTS: return (item.Tag as Project).Id;
                case ComboboxType.BRANCHES: return item.Content as string;
                case ComboboxType.SCANS: return (item.Tag as Scan).ID;
                case ComboboxType.SEVERITY: return item.Content as string;
                case ComboboxType.STATE: return item.Tag as string;
            }

            return null;
        }

        /// <summary>
        /// Display a warning notification in the info bar
        /// </summary>
        /// <param name="package"></param>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <returns></returns>
        public static void DisplayMessageInInfoBar(AsyncPackage package, string message, ImageMoniker messageSeverity)
        {
            _ = InfobarService.Initialize(package).ShowInfoBarAsync(message, messageSeverity, true);
        }

        /// <summary>
        /// Display a warning notification in the info bar
        /// </summary>
        /// <param name="package"></param>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <param name="autoDismiss"></param>
        /// <returns></returns>
        public static void DisplayMessageInInfoBar(AsyncPackage package, string message, ImageMoniker messageSeverity, bool autoDismiss)
        {
            _ = InfobarService.Initialize(package).ShowInfoBarAsync(message, messageSeverity, autoDismiss);
        }

        /// <summary>
        /// Display a warning notification in the info bar with http link
        /// </summary>
        /// <param name="package"></param>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <param name="linkDisplayName"></param>
        /// <param name="linkId"></param>
        public static void DisplayMessageInInfoWithLinkBar(AsyncPackage package, string message, ImageMoniker messageSeverity, string linkDisplayName, string linkId)
        {
            _ = InfobarService.Initialize(package).ShowInfoBarWithLinkAsync(message, messageSeverity, linkDisplayName, linkId, true);
        }

        /// <summary>
        /// Display a warning notification in the info bar with http link
        /// </summary>
        /// <param name="package"></param>
        /// <param name="message"></param>
        /// <param name="messageSeverity"></param>
        /// <param name="linkDisplayName"></param>
        /// <param name="linkId"></param>
        /// <param name="autoDismiss"></param>
        public static void DisplayMessageInInfoWithLinkBar(AsyncPackage package, string message, ImageMoniker messageSeverity, string linkDisplayName, string linkId, bool autoDismiss)
        {
            _ = InfobarService.Initialize(package).ShowInfoBarWithLinkAsync(message, messageSeverity, linkDisplayName, linkId, autoDismiss);
        }

        /// <summary>
        /// CHeck if checkmarx settings are defined
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static bool AreCxCredentialsDefined(AsyncPackage package)
        {
            CxPreferencesModule preferences = (CxPreferencesModule) package.GetDialogPage(typeof(CxPreferencesModule));
            CxConfig configuration = preferences.GetCxConfig();
            
            if (configuration == null || string.IsNullOrEmpty(configuration.ApiKey)) return false;

            return true;
        }

        /// <summary>
        /// Trim file name when it is greater than 45
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string CapToLen(string fileName)
        {
            return fileName.Length > CxConstants.FILE_PATH_MAX_LEN ? CxConstants.COLLAPSE_CRUMB + fileName.Substring(fileName.Length - CxConstants.FILE_PATH_MAX_LEN + CxConstants.COLLAPSE_CRUMB.Length) : fileName;
        }
    }
}
