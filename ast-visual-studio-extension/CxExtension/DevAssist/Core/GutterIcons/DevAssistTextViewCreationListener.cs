using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
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
                            System.Diagnostics.Debug.WriteLine("DevAssist: Both taggers found, adding test vulnerabilities");

                            // Create test vulnerabilities
                            var vulnerabilities = new List<Vulnerability>
                            {
                                new Vulnerability
                                {
                                    Id = "TEST-001",
                                    Severity = SeverityLevel.Malicious,
                                    LineNumber = 1,
                                    Description = "Test Malicious vulnerability"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-002",
                                    Severity = SeverityLevel.Critical,
                                    LineNumber = 3,
                                    Description = "Test Critical vulnerability"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-003",
                                    Severity = SeverityLevel.High,
                                    LineNumber = 5,
                                    Description = "Test High vulnerability"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-004",
                                    Severity = SeverityLevel.Medium,
                                    LineNumber = 7,
                                    Description = "Test Medium vulnerability"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-005",
                                    Severity = SeverityLevel.Low,
                                    LineNumber = 9,
                                    Description = "Test Low vulnerability"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-006",
                                    Severity = SeverityLevel.Ok,
                                    LineNumber = 11,
                                    Description = "Test OK"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-007",
                                    Severity = SeverityLevel.Unknown,
                                    LineNumber = 13,
                                    Description = "Test UnKnown"
                                },
                                new Vulnerability
                                {
                                    Id = "TEST-008",
                                    Severity = SeverityLevel.Ignored,
                                    LineNumber = 15,
                                    Description = "Test Ignored"
                                }
                            };

                            // Update both taggers with the same vulnerabilities
                            glyphTagger.UpdateVulnerabilities(vulnerabilities);
                            errorTagger.UpdateVulnerabilities(vulnerabilities);
                            System.Diagnostics.Debug.WriteLine("DevAssist: Test vulnerabilities added to both taggers successfully");
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

