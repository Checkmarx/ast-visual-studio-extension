using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.GutterIcons
{
    /// <summary>
    /// When package.json, secrets.py, IaC (.yaml/.yml), or Dockerfile is opened, loads the corresponding
    /// mock data and updates gutter, underline, problem window, Error List, and popup.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class CxAssistMockDataViewCreationListener : IWpfTextViewCreationListener
    {
        private static bool IsCSharpFile(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        private static List<Vulnerability> GetMockVulnerabilitiesForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName)) return null;

            if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
                return CxAssistMockData.GetPackageJsonMockVulnerabilities(filePath);

            if (fileName.Equals("secrets.py", StringComparison.OrdinalIgnoreCase))
                return CxAssistMockData.GetSecretsPyMockVulnerabilities(filePath);

            if (fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase))
                return CxAssistMockData.GetContainerMockVulnerabilities(filePath);

            if (filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                return CxAssistMockData.GetIacMockVulnerabilities(filePath);

            return null;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            string filePath = null;
            try
            {
                filePath = CxAssistDisplayCoordinator.GetFilePathForBuffer(textView.TextBuffer);
                if (string.IsNullOrEmpty(filePath))
                {
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                    filePath = dte?.ActiveDocument?.FullName;
                }
            }
            catch { }

            if (IsCSharpFile(filePath))
                return;

            List<Vulnerability> vulnerabilities = GetMockVulnerabilitiesForFile(filePath);
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return;

            System.Diagnostics.Debug.WriteLine($"CxAssist: Mock data file opened ({filePath}), will apply {vulnerabilities.Count} findings");

            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
            {
                try
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var buffer = textView.TextBuffer;
                        filePath = CxAssistDisplayCoordinator.GetFilePathForBuffer(buffer);
                        if (string.IsNullOrEmpty(filePath))
                        {
                            try
                            {
                                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                                filePath = dte?.ActiveDocument?.FullName ?? "file";
                            }
                            catch { filePath = "file"; }
                        }

                        CxAssistGlyphTagger glyphTagger = null;
                        CxAssistErrorTagger errorTagger = null;
                        for (int i = 0; i < 8; i++)
                        {
                            glyphTagger = CxAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer);
                            errorTagger = CxAssistErrorTaggerProvider.GetTaggerForBuffer(buffer);
                            if (glyphTagger != null && errorTagger != null) break;
                            await System.Threading.Tasks.Task.Delay(200);
                        }

                        if (glyphTagger == null || errorTagger == null)
                        {
                            System.Diagnostics.Debug.WriteLine("CxAssist: Mock data listener – taggers not found");
                            return;
                        }

                        vulnerabilities = GetMockVulnerabilitiesForFile(filePath);
                        if (vulnerabilities == null || vulnerabilities.Count == 0)
                            return;

                        CxAssistDisplayCoordinator.UpdateFindings(buffer, vulnerabilities, filePath);
                        System.Diagnostics.Debug.WriteLine($"CxAssist: Updated gutter, underline, findings for {filePath} ({vulnerabilities.Count} items)");
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CxAssist: Mock data listener error: {ex.Message}");
                }
            });
        }
    }
}
