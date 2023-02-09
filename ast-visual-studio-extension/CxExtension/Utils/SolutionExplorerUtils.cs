using ast_visual_studio_extension.CxExtension.Panels;
using ast_visual_studio_extension.CxWrapper.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        internal static async void OpenFileAsync(object sender, RoutedEventArgs e) {
            FileNode node = (((sender as Hyperlink).Parent as TextBlock).Parent as ListViewItem).Tag as FileNode;
            EnvDTE.DTE dte = GetDTE();

            string partialFileLocation = PrepareFileName(node.FileName);

            List<string> files = await SearchFilesBasedOnProjectDirectoryAsync(partialFileLocation, dte);
            if (files.Count == 0) files = await SearchAllFilesAsync(partialFileLocation, dte);

            if (files.Count > 0)
            {
                foreach (string filePath in files)
                {
                    OpenFile(filePath, node);
                }
            }
            else
            {
                CxUtils.DisplayMessageInInfoBar(AsyncPackage, string.Format(CxConstants.NOTIFY_FILE_NOT_FOUND, node.FileName), KnownMonikers.StatusWarning);
            }
        }
        internal static Task<List<string>> SearchAllFilesAsync(string partialFileLocation, EnvDTE.DTE dte)
        {
            return Task.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var projects = dte.Solution.Projects;

                List<string> allFiles = new List<string>();

                allFiles.AddRange(await
                    GetAllProjectFilesAsync(Path.GetDirectoryName(dte.Solution.FullName), partialFileLocation, new string[] { "bin", "obj", "packages", "node_modules", ".git", ".vs" }));

                if (allFiles.Count == 0) { 
                    foreach (EnvDTE.Project project in dte.Solution.Projects)
                    {
                        if (!await IsProjectLoadedAsync(project)) continue;

                        FileInfo projectFileInfo = new FileInfo(project.FullName);
                        string projectPath = Directory.GetParent(projectFileInfo.Directory.FullName).FullName;

                        string[] files = await GetAllProjectFilesAsync(projectPath, partialFileLocation, new string[] { "bin", "obj", "packages", "node_modules", ".git", ".vs" });

                        allFiles.AddRange(files);
                    }
                }

                return allFiles;
            });
        }


        internal static Task<List<string>> SearchFilesBasedOnProjectDirectoryAsync(string partialFileLocation, EnvDTE.DTE dte) {
            return Task.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                List<string> files = new List<string>();

                PopulateFileList(Path.GetDirectoryName(dte.Solution.FullName), partialFileLocation, files);

                if (files.Count == 0)
                {
                    foreach (EnvDTE.Project project in dte.Solution.Projects)
                    {
                        if (!await IsProjectLoadedAsync(project)) continue;

                        FileInfo projectFileInfo = new FileInfo(project.FullName);

                        PopulateFileList(projectFileInfo.Directory.FullName, partialFileLocation, files);
                    }
                }

                return files;
            });
        }

        static async Task<string[]> GetAllProjectFilesAsync(string path, string pathString, string[] excludedDirectories)
        {
            List<string> files = new List<string>();

            string[] topLevelFiles = await Task.Run(() => Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly));
            files.AddRange(topLevelFiles.Where(file => file.EndsWith(pathString)));

            foreach (string directory in Directory.GetDirectories(path))
            {
                if (!excludedDirectories.Contains(Path.GetFileName(directory)))
                {
                    string[] subdirectoryFiles = await GetAllProjectFilesAsync(directory, pathString, excludedDirectories);
                    files.AddRange(subdirectoryFiles);
                }
            }

            return files.ToArray();
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

        static async Task<bool> IsProjectLoadedAsync(EnvDTE.Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            bool projectIsUnloadedInSolution = string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, project.Kind, System.StringComparison.OrdinalIgnoreCase) == 0;
            return !(projectIsUnloadedInSolution || string.IsNullOrEmpty(project.FullName));
        }

        private static string PrepareFileName(string partialFileLocation)
        {
            if (partialFileLocation[0] == '/')
            {
                partialFileLocation = partialFileLocation.Substring(1);
            }
            return partialFileLocation.Replace('/', '\\');
        }

        private static void PopulateFileList(string fullName, string partialFileLocation, List<string> files)
        {
            string fullPath = GetFullPath(fullName, partialFileLocation);
            if (File.Exists(fullPath))
            {
                files.Add(fullPath);
            }
        }

        private static string GetFullPathParent(string fullName, string partialFileLocation)
        {
            FileInfo projectFileInfo = new FileInfo(fullName);
            string projectPath = Directory.GetParent(projectFileInfo.Directory.FullName).FullName;
            return Path.Combine(projectPath, partialFileLocation);
        }

        private static string GetFullPath(string fullName, string partialFileLocation)
        {
            return Path.Combine(fullName, partialFileLocation);
        }
    }
}
