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
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.GutterIcons
{
    /// <summary>
    /// When a file matching scanner manifest/container/IAC/Secrets patterns is opened, loads the corresponding
    /// mock data and updates gutter, underline, problem window, Error List, and popup.
    /// Logic aligned with JetBrains: MANIFEST_FILE_PATTERNS (OSS), CONTAINERS_FILE_PATTERNS + Helm (Containers),
    /// IAC_SUPPORTED_PATTERNS + IAC_FILE_EXTENSIONS (IAC), and Secrets exclusions.
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

        /// <summary>
        /// Returns mock vulnerabilities for the file based on JetBrains-aligned scanner logic:
        /// Base: skip node_modules. OSS: manifest files only. Containers: dockerfile*, docker-compose* + Helm.
        /// IAC: dockerfile, *.tfvars, or extension tf/yaml/yml/json/proto. Secrets: non-manifest (e.g. secrets.py).
        /// </summary>
        private static List<Vulnerability> GetMockVulnerabilitiesForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            // Base check (JetBrains BaseScannerService.shouldScanFile)
            if (!CxAssistScannerConstants.PassesBaseScanCheck(filePath))
                return null;

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName)) return null;

            var pathNormalized = CxAssistScannerConstants.NormalizePathForMatching(filePath);
            var fileNameLower = fileName.ToLowerInvariant();

            // --- OSS: only manifest files (JetBrains OssScannerService.isManifestFilePatternMatching) ---
            if (CxAssistScannerConstants.IsManifestFile(filePath))
            {
                if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetPackageJsonMockVulnerabilities(filePath);
                if (fileName.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase) || fileNameLower.EndsWith(".pom", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetPomMockVulnerabilities(filePath);
                if (fileName.Equals("build.gradle", StringComparison.OrdinalIgnoreCase) || fileName.Equals("build.gradle.kts", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetBuildGradleMockVulnerabilities(filePath);
                if (fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("Pipfile", StringComparison.OrdinalIgnoreCase)
                    || fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetRequirementsMockVulnerabilities(filePath);
                if (fileName.Equals("packages.config", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetPackagesConfigMockVulnerabilities(filePath);
                if (fileName.Equals("package-lock.json", StringComparison.OrdinalIgnoreCase) || fileName.Equals("yarn.lock", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetPackageJsonMockVulnerabilities(filePath);
                if (fileName.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetDirectoryPackagesPropsMockVulnerabilities(filePath);
                if (fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetGoModMockVulnerabilities(filePath);
                if (fileNameLower.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetCsprojMockVulnerabilities(filePath);
                return null;
            }

            // --- Containers: dockerfile*, docker-compose* (JetBrains ContainerScannerService) or Helm / values.yaml ---
            if (CxAssistScannerConstants.IsHelmFile(filePath) ||
                fileName.Equals("values.yaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("values.yml", StringComparison.OrdinalIgnoreCase))
            {
                var iac = CxAssistMockData.GetIacMockVulnerabilities(filePath);
                var containerImage = CxAssistMockData.GetContainerImageMockVulnerabilities(filePath);
                var merged = new List<Vulnerability>(iac.Count + containerImage.Count);
                merged.AddRange(iac);
                merged.AddRange(containerImage);
                return merged;
            }
            if (CxAssistScannerConstants.IsContainersFile(filePath))
            {
                if (CxAssistScannerConstants.IsDockerFile(filePath))
                    return CxAssistMockData.GetContainerMockVulnerabilities(filePath);
                if (CxAssistScannerConstants.IsDockerComposeFile(filePath))
                    return CxAssistMockData.GetDockerComposeMockVulnerabilities(filePath);
                return CxAssistMockData.GetContainerMockVulnerabilities(filePath);
            }

            // --- IAC: tf, yaml, yml, json, proto, dockerfile, *.auto.tfvars, *.terraform.tfvars (JetBrains IacScannerService) ---
            if (CxAssistScannerConstants.IsIacFile(filePath))
                return CxAssistMockData.GetIacMockVulnerabilities(filePath);

            // --- Secrets: scan non-manifest files; we only have mock for a specific secrets file (JetBrains: exclude manifest + .vscode) ---
            if (!CxAssistScannerConstants.IsExcludedForSecrets(filePath))
            {
                if (fileName.Equals("secrets.py", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetSecretsPyMockVulnerabilities(filePath);

                if (fileName.StartsWith("multi_findings_one_line", StringComparison.OrdinalIgnoreCase) &&
                    fileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                    return CxAssistMockData.GetMultiFindingsOneLineMockVulnerabilities(filePath);
            }

            return null;
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            CxAssistDisplayCoordinator.EnsureThemeChangeHandler();

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

            // Skip Copilot/AI assistant temporary files (aligned with JetBrains DevAssistInspection.isAgentEvent)
            if (CxAssistConstants.IsAIAgentFile(filePath))
            {
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.AI_AGENT_FILE_SKIPPING, filePath));
                return;
            }

            // Skip when no scanner is enabled (aligned with JetBrains DevAssistFileListener.restoreGutterIcons)
            if (!CxAssistConstants.IsAnyScannerEnabled())
            {
                CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.NO_SCANNER_ENABLED_SKIPPING, filePath));
                return;
            }

            // Restore cached findings if this file was previously scanned (JetBrains: DevAssistFileListener.restoreGutterIcons)
            List<Vulnerability> cachedVulnerabilities = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(filePath);

            // Fall back to mock data when no cached findings exist
            List<Vulnerability> vulnerabilities = cachedVulnerabilities ?? GetMockVulnerabilitiesForFile(filePath);
            if (vulnerabilities == null || vulnerabilities.Count == 0)
                return;

            var vulnsToApply = vulnerabilities;
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
                            return;

                        // Re-check cached findings (may have been updated while waiting for taggers)
                        var latestCached = CxAssistDisplayCoordinator.GetCachedVulnerabilitiesForFile(filePath);
                        var finalVulns = latestCached ?? GetMockVulnerabilitiesForFile(filePath);
                        if (finalVulns == null || finalVulns.Count == 0)
                            return;

                        CxAssistDisplayCoordinator.UpdateFindings(buffer, finalVulns, filePath);
                        CxAssistOutputPane.WriteToOutputPane(string.Format(CxAssistConstants.UI_DECORATED_SUCCESSFULLY, filePath, finalVulns.Count));
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{CxAssistConstants.LogCategory}] Exception restoring gutter icons for: {filePath}, {ex.Message}");
                }
            });
        }
    }
}
