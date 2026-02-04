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
    /// Direct test command that manually creates tagger without MEF
    /// This bypasses MEF to test if the glyph rendering works at all
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

                // Create glyph tagger DIRECTLY without MEF
                System.Diagnostics.Debug.WriteLine("DevAssist: Creating glyph tagger DIRECTLY (bypassing MEF)");
                var glyphTagger = new DevAssistGlyphTagger(buffer);

                // Store it in buffer properties so the glyph factory can find it
                try
                {
                    buffer.Properties.AddProperty(typeof(DevAssistGlyphTagger), glyphTagger);
                    System.Diagnostics.Debug.WriteLine("DevAssist: Glyph tagger stored in buffer properties");
                }
                catch
                {
                    buffer.Properties.RemoveProperty(typeof(DevAssistGlyphTagger));
                    buffer.Properties.AddProperty(typeof(DevAssistGlyphTagger), glyphTagger);
                    System.Diagnostics.Debug.WriteLine("DevAssist: Glyph tagger replaced in buffer properties");
                }

                // Create error tagger DIRECTLY without MEF (for colored squiggly underlines)
                System.Diagnostics.Debug.WriteLine("DevAssist: Creating error tagger DIRECTLY (bypassing MEF)");
                var errorTagger = new DevAssistErrorTagger(buffer);

                // Store it in buffer properties
                try
                {
                    buffer.Properties.AddProperty(typeof(DevAssistErrorTagger), errorTagger);
                    System.Diagnostics.Debug.WriteLine("DevAssist: Error tagger stored in buffer properties");
                }
                catch
                {
                    buffer.Properties.RemoveProperty(typeof(DevAssistErrorTagger));
                    buffer.Properties.AddProperty(typeof(DevAssistErrorTagger), errorTagger);
                    System.Diagnostics.Debug.WriteLine("DevAssist: Error tagger replaced in buffer properties");
                }

                // Create test vulnerabilities - including Ok, Unknown, and Ignored severity levels
                var vulnerabilities = new List<Vulnerability>
                {
                    new Vulnerability { Id = "TEST-001", Severity = SeverityLevel.Malicious, LineNumber = 1, Description = "Test Malicious vulnerability" },
                    new Vulnerability { Id = "TEST-002", Severity = SeverityLevel.Critical, LineNumber = 3, Description = "Test Critical vulnerability" },
                    new Vulnerability { Id = "TEST-003", Severity = SeverityLevel.High, LineNumber = 5, Description = "Test High vulnerability" },
                    new Vulnerability { Id = "TEST-004", Severity = SeverityLevel.Medium, LineNumber = 7, Description = "Test Medium vulnerability" },
                    new Vulnerability { Id = "TEST-005", Severity = SeverityLevel.Low, LineNumber = 9, Description = "Test Low vulnerability" },
                    new Vulnerability { Id = "TEST-006", Severity = SeverityLevel.Unknown, LineNumber = 11, Description = "Test Unknown vulnerability" },
                    new Vulnerability { Id = "TEST-007", Severity = SeverityLevel.Ok, LineNumber = 13, Description = "Test Ok vulnerability" },
                    new Vulnerability { Id = "TEST-008", Severity = SeverityLevel.Ignored, LineNumber = 15, Description = "Test Ignored vulnerability" }
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
                    $"✅ Direct test completed!\n\n" +
                    $"Added {vulnerabilities.Count} test vulnerabilities with:\n" +
                    $"• Gutter icons (left margin) - ALL 8 severities\n" +
                    $"• Custom colored squiggly underlines - ONLY 5 main severities\n\n" +
                    $"CUSTOM COLORED UNDERLINES:\n" +
                    $"🔴 Line 1: Malicious (CRIMSON squiggly)\n" +
                    $"🔴 Line 3: Critical (RED squiggly)\n" +
                    $"🟠 Line 5: High (ORANGE squiggly)\n" +
                    $"🟡 Line 7: Medium (GOLD squiggly)\n" +
                    $"🟢 Line 9: Low (GREEN squiggly)\n\n" +
                    $"GUTTER ICONS ONLY (No underlines):\n" +
                    $"⚪ Line 11: Unknown (icon only)\n" +
                    $"✅ Line 13: Ok (icon only)\n" +
                    $"🚫 Line 15: Ignored (icon only)\n\n" +
                    $"Features:\n" +
                    $"✅ Custom colored squiggly underlines (adornment layer)\n" +
                    $"✅ Tooltips on hover\n" +
                    $"✅ Gutter icons for all severities\n" +
                    $"✅ Similar to JetBrains plugin",
                    "Test DevAssist Gutter Icons & Custom Colored Markers",
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

