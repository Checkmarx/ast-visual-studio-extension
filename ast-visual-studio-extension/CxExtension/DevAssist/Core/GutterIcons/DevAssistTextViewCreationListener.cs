using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using ast_visual_studio_extension.CxExtension.DevAssist.Core;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.GutterIcons
{
    /// <summary>
    /// Listens for text view creation and automatically adds test gutter icons and colored markers
    /// This is a temporary POC to test gutter icon and marker functionality
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class DevAssistTextViewCreationListener : IWpfTextViewCreationListener
    {
        private static int _fallbackDocumentCounter;

        public void TextViewCreated(IWpfTextView textView)
        {
            System.Diagnostics.Debug.WriteLine("DevAssist: TextViewCreated - C# file opened");

            // Wait for MEF to create the taggers, then add test vulnerabilities
            // We need to wait because the taggers are created asynchronously by MEF
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
            {
                try
                {
                    Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        System.Diagnostics.Debug.WriteLine("DevAssist: Attempting to add test vulnerabilities to C# file");

                        var buffer = textView.TextBuffer;

                        // Try to get the glyph tagger - it should have been created by MEF by now
                        DevAssistGlyphTagger glyphTagger = null;
                        DevAssistErrorTagger errorTagger = null;

                        // Try multiple times with delays in case MEF is still loading
                        for (int i = 0; i < 8; i++)
                        {
                            glyphTagger = DevAssistGlyphTaggerProvider.GetTaggerForBuffer(buffer);
                            errorTagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(buffer);

                            if (glyphTagger != null && errorTagger != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"DevAssist: Both taggers found on attempt {i + 1}");
                                break;
                            }
                            System.Diagnostics.Debug.WriteLine($"DevAssist: Taggers not found, attempt {i + 1}/8, waiting...");
                            await System.Threading.Tasks.Task.Delay(200);
                        }

                        if (glyphTagger != null && errorTagger != null)
                        {
                            System.Diagnostics.Debug.WriteLine("DevAssist: Both taggers found, updating via coordinator (gutter, underline, problem window)");

                            // Single coordinator call: updates gutter, underline, and current findings for problem window (Option B)
                            var filePath = DevAssistDisplayCoordinator.GetFilePathForBuffer(buffer);
                            // When path is unknown (e.g. ITextDocument not available), use a unique key per buffer so multi-file doesn't overwrite with "Program.cs"
                            if (string.IsNullOrEmpty(filePath))
                            {
                                var fallback = Interlocked.Increment(ref _fallbackDocumentCounter);
                                filePath = $"Document {fallback}";
                                System.Diagnostics.Debug.WriteLine($"DevAssist: GetFilePathForBuffer returned null, using fallback: {filePath}");
                            }
                            var vulnerabilities = DevAssistMockData.GetCommonVulnerabilities(filePath);
                            DevAssistDisplayCoordinator.UpdateFindings(buffer, vulnerabilities, filePath);

                            System.Diagnostics.Debug.WriteLine("DevAssist: Coordinator updated gutter, underline, and findings successfully");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DevAssist: Taggers are NULL - MEF hasn't created them yet");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist: Error adding test vulnerabilities: {ex.Message}");
                }
            });
        }
    }
}

