using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.ComponentModelHost;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.GutterIcons;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// Test command that injects sample vulnerabilities into the provider-managed taggers
    /// so that squiggles, gutter icons, and the rich Quick Info hover (badge, links, etc.) all see the same data.
    /// </summary>
    internal sealed class TestGutterIconsDirectCommand
    {
        public const int CommandId = 0x0105;
        public static readonly Guid CommandSet = new Guid("a6e70b7d-e3e1-4a3b-9b3e-3e3e3e3e3e3e");

        private readonly AsyncPackage package;

        private TestGutterIconsDirectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static TestGutterIconsDirectCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TestGutterIconsDirectCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                System.Diagnostics.Debug.WriteLine("DevAssist: TestGutterIconsDirectCommand - Starting DIRECT test (no MEF)");

                var textView = GetActiveTextView();
                if (textView == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No active text editor found. Please open a code file first.",
                        "Test Gutter Icons - Direct",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var buffer = textView.TextBuffer;

                // Use the same taggers the editor and Quick Info use (from MEF providers).
                // Creating new taggers and storing in buffer.Properties would not be used by
                // the error layer or Quick Info source, so the rich hover would never see data.
                var glyphTagger = DevAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer);
                var errorTagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(buffer);

                if (glyphTagger == null || errorTagger == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "DevAssist taggers not ready for this buffer. Ensure the code file is open and focused, then run this command again.",
                        "Test DevAssist Hover Popup",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("DevAssist: Using provider taggers for glyph and error (same as editor and Quick Info)");

                // Create test vulnerabilities: Critical, High, Medium, Low for colored marker verification (AST-133227)
                var vulnerabilities = new List<Vulnerability>
                {
                    // Scanner-specific vulnerabilities (cover Critical, High, Medium)
                    DevAssistTestHelper.CreateOssVulnerability(),         // Line 5:  High (red)
                    DevAssistTestHelper.CreateLowSeverityVulnerability(), // Line 7:  Low (green) - visual distinction
                    DevAssistTestHelper.CreateAscaVulnerability(),       // Line 42: Critical (dark red)
                    DevAssistTestHelper.CreateIacVulnerability(),         // Line 28: High (red)
                    DevAssistTestHelper.CreateSecretsVulnerability(),     // Line 12: Critical (dark red)
                    DevAssistTestHelper.CreateContainersVulnerability(),  // Line 1:  Medium (orange)

                    // Line 5: second finding on same line -> popup shows severity count row (Critical + High)
                    new Vulnerability { Id = "TEST-005B", Title = "Second finding on line 5", Severity = SeverityLevel.Critical, LineNumber = 5, Scanner = ScannerType.ASCA, Description = "Multiple findings on one line test." },

                    // Additional test vulnerabilities for other severity levels (gutter only, no underline)
                    new Vulnerability { Id = "TEST-006", Severity = SeverityLevel.Unknown, LineNumber = 11, Description = "Test Unknown vulnerability", Scanner = ScannerType.ASCA },
                    new Vulnerability { Id = "TEST-007", Severity = SeverityLevel.Ok, LineNumber = 13, Description = "Test Ok vulnerability", Scanner = ScannerType.OSS },
                    new Vulnerability { Id = "TEST-008", Severity = SeverityLevel.Ignored, LineNumber = 15, Description = "Test Ignored vulnerability", Scanner = ScannerType.IaC }
                };

                System.Diagnostics.Debug.WriteLine($"DevAssist: Adding {vulnerabilities.Count} test vulnerabilities to both taggers");

                // Update both taggers with the same vulnerabilities
                glyphTagger.UpdateVulnerabilities(vulnerabilities);
                errorTagger.UpdateVulnerabilities(vulnerabilities);

                // Force the text view to refresh
                System.Diagnostics.Debug.WriteLine("DevAssist: Forcing text view refresh");
                textView.VisualElement.InvalidateVisual();

                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"✅ DevAssist Hover Popup Test - Scanner-Specific Data!\n\n" +
                    $"Added {vulnerabilities.Count} test vulnerabilities with rich scanner-specific data:\n\n" +
                    $"SCANNER-SPECIFIC VULNERABILITIES:\n" +
                    $"🟢 Line 5: OSS - Package vulnerability\n" +
                    $"   • Package: lodash@4.17.15\n" +
                    $"   • CVE: CVE-2020-8203, CVSS: 7.4\n" +
                    $"   • Recommended: 4.17.21\n\n" +
                    $"🔴 Line 42: ASCA - SQL Injection\n" +
                    $"   • Remediation advice included\n\n" +
                    $"🟣 Line 28: IaC - S3 Bucket Public\n" +
                    $"   • Expected vs Actual values\n\n" +
                    $"🔴 Line 12: Secrets - Hardcoded API Key\n" +
                    $"   • Secret type: API Key\n\n" +
                    $"🟠 Line 1: Containers - Vulnerable Image\n" +
                    $"   • CVE: CVE-2021-3711, CVSS: 9.8\n\n" +
                    $"HOVER OVER ANY LINE TO SEE:\n" +
                    $"✅ Rich hover popup with scanner badge\n" +
                    $"✅ Scanner-specific content (CVE, CVSS, remediation, etc.)\n" +
                    $"✅ Action links (View Details, Navigate, Learn More, Apply Fix)\n" +
                    $"✅ Theme-aware styling\n" +
                    $"✅ Similar to JetBrains implementation",
                    "Test DevAssist Hover Popup - Scanner-Specific Content",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DevAssist: Error in direct test: {ex.Message}\n{ex.StackTrace}");
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error: {ex.Message}",
                    "Test Gutter Icons - Direct",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private IWpfTextView GetActiveTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = Package.GetGlobalService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager))
                as Microsoft.VisualStudio.TextManager.Interop.IVsTextManager2;

            if (textManager == null)
                return null;

            Microsoft.VisualStudio.TextManager.Interop.IVsTextView textViewCurrent;
            int mustHaveFocus = 1;
            textManager.GetActiveView2(mustHaveFocus, null,
                (uint)Microsoft.VisualStudio.TextManager.Interop._VIEWFRAMETYPE.vftCodeWindow,
                out textViewCurrent);

            if (textViewCurrent == null)
                return null;

            var userData = textViewCurrent as Microsoft.VisualStudio.TextManager.Interop.IVsUserData;
            if (userData == null)
                return null;

            Guid guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            object holder;
            userData.GetData(ref guidViewHost, out holder);

            var viewHost = holder as IWpfTextViewHost;
            return viewHost?.TextView;
        }
    }
}

