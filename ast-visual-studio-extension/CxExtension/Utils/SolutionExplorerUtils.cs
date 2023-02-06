using ast_visual_studio_extension.CxExtension.Panels;
using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    internal class SolutionExplorerUtils
    {
        public static AsyncPackage AsyncPackage { private get; set; }
        /// <summary>
        /// Get current EnvDTE
        /// </summary>
        /// <returns></returns>
        private static DTE GetDTE()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(SDTE)) as DTE;
        }

        /// <summary>
        /// Open a file when it exists in the solution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void OpenFile(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            FileNode node = (((sender as Hyperlink).Parent as TextBlock).Parent as ListViewItem).Tag as FileNode;

            string partialFileLocation = PrepareFileName(node.FileName);
            EnvDTE.DTE dte = GetDTE();

            string solutionPath = Path.GetDirectoryName(dte.Solution.FullName);

            string fullPath = Path.Combine(solutionPath, partialFileLocation);
            if (File.Exists(fullPath))
            {
                OpenFile(fullPath, node);
                return;
            }


            if (dte.Solution.Projects.Count > 0)
            {
                foreach (EnvDTE.Project project in dte.Solution.Projects)
                {
                    fullPath = GetFullPath(project, partialFileLocation);
                    if (fullPath != null && File.Exists(fullPath))
                    {
                        OpenFile(fullPath, node);
                        dte.ItemOperations.OpenFile(fullPath);
                        return;
                    }
                }
            }

            CxUtils.DisplayMessageInInfoBar(AsyncPackage, string.Format(CxConstants.NOTIFY_FILE_NOT_FOUND, partialFileLocation), KnownMonikers.StatusWarning);
        }

        private static void OpenFile(string filePath, FileNode node)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Open the file itself
            _ = GetDTE().ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);

            try
            {
                // move the cursor for the specific line and column
                EnvDTE.TextSelection textSelection = GetDTE().ActiveDocument.Selection as EnvDTE.TextSelection;
                textSelection.MoveToLineAndOffset(node.Line, node.Column);
            }
            catch (Exception)
            {
            }
        }

        private static string PrepareFileName(string partialFileLocation)
        {
            if (partialFileLocation[0] == '/')
            {
                partialFileLocation = partialFileLocation.Substring(1);
            }
            return partialFileLocation.Replace('/', '\\');
        }

        private static string GetFullPath(EnvDTE.Project project, string partialFileLocation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string fullPath = null;
            try
            {
                FileInfo projectFileInfo = new FileInfo(project.FullName);
                string projectPath = Directory.GetParent(projectFileInfo.Directory.FullName).FullName;
                fullPath = Path.Combine(projectPath, partialFileLocation);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch (Exception)
            {

            }

            return fullPath;
        }
    }
}
