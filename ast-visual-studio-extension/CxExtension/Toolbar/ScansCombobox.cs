using ast_visual_studio_extension.CxExtension.Panels;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace ast_visual_studio_extension.CxExtension.Toolbar
{
    internal class ScansCombobox
    {
        private static string currentScanId = String.Empty;
        private readonly ResultsTreePanel resultsTreePanel;

        public ScansCombobox(AsyncPackage package)
        {
            this.resultsTreePanel = new ResultsTreePanel(package);
        }

        /// <summary>
        ///  Create command to load scans in the combobox
        /// </summary>
        /// <returns></returns>
        public OleMenuCommand GetOnLoadScansCommand()
        {
            CommandID getListCmd = new CommandID(PackageGuids.guidCxWindowPackageCmdSet, (int)PackageIds.ScansComboGetList);
            return new OleMenuCommand(new EventHandler(OnLoadScans), getListCmd);
        }

        /// <summary>
        /// Create command to handle the on change event in the scans combobox
        /// </summary>
        /// <returns></returns>
        public OleMenuCommand GetOnChangeScanCommand()
        {
            CommandID menuMyMRUComboCommandID = new CommandID(PackageGuids.guidCxWindowPackageCmdSet, (int)PackageIds.ScansCombo);
            return new OleMenuCommand(new EventHandler(OnChangeScan), menuMyMRUComboCommandID);
        }

        /// <summary>
        /// Populate Scans combobox with a list of scan ids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnLoadScans(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs args = e as OleMenuCmdEventArgs;
            if (args != null)
            {
                IntPtr outValue = args.OutValue;

                string[] scanIdList = { "76891fb9-a342-4bf3-9b0a-788c8cf759da", "296a43fa-ecf9-4068-9172-8a2e80e4de8a" };

                if (outValue != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(scanIdList, outValue);
                }
            }
        }
    
        /// <summary>
        /// On change event assigned to the scan combobox. Draws the results tree panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChangeScan(object sender, EventArgs e)
        {
            if (e is OleMenuCmdEventArgs eventArgs)
            {
                object input = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(currentScanId, vOut);
                }
                else if (input != null)
                {
                    string scanId = (e as OleMenuCmdEventArgs).InValue as string;
                    bool isValidGuid = Guid.TryParse(scanId, out _);

                    if (!isValidGuid)
                    {
                        return;
                    }

                    currentScanId = scanId;
                    _ = resultsTreePanel.DrawAsync(currentScanId);
                }
            }
        }
    }
}
