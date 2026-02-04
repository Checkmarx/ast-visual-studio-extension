using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// MEF provider for DevAssist squiggly adornment layer
    /// Creates the custom colored squiggly underlines for vulnerabilities
    /// Based on JetBrains EffectType.WAVE_UNDERSCORE pattern
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class DevAssistSquigglyAdornmentProvider : IWpfTextViewCreationListener
    {
        /// <summary>
        /// Defines the adornment layer for squiggly underlines
        /// Order: After selection, before text (so squiggles appear under text)
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("DevAssistSquigglyAdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        public AdornmentLayerDefinition AdornmentLayer = null;

        /// <summary>
        /// Called when a text view is created
        /// Creates the squiggly adornment for the view
        /// </summary>
        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView == null)
                return;

            System.Diagnostics.Debug.WriteLine("DevAssist Adornment: TextViewCreated called");

            // Get the error tagger for this buffer
            var errorTagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(textView.TextBuffer);
            if (errorTagger == null)
            {
                System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Error tagger not found, will retry with longer delay");

                // The error tagger might not be created yet, so we'll wait longer and retry multiple times
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                {
                    Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        errorTagger = DevAssistErrorTaggerProvider.GetTaggerForBuffer(textView.TextBuffer);
                        if (errorTagger != null)
                        {
                            System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Error tagger found on retry, creating adornment");
                            textView.Properties.GetOrCreateSingletonProperty(() => new DevAssistSquigglyAdornment(textView, errorTagger));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Error tagger still not found after retry");
                        }
                    });
                }, System.Threading.Tasks.TaskScheduler.Default);

                return;
            }

            // Create the adornment and store it in the view's property bag
            textView.Properties.GetOrCreateSingletonProperty(() => new DevAssistSquigglyAdornment(textView, errorTagger));

            System.Diagnostics.Debug.WriteLine("DevAssist Adornment: Squiggly adornment created successfully");
        }
    }
}

