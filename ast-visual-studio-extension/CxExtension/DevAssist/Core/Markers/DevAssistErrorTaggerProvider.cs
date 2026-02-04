using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// MEF provider for DevAssist error tagger
    /// Based on JetBrains EditorFactoryListener pattern adapted for Visual Studio
    /// Creates and manages error tagger instances per buffer (not per view)
    /// Provides severity-based colored squiggly underlines similar to JetBrains WAVE_UNDERSCORE
    /// IMPORTANT: Uses ITaggerProvider (not IViewTaggerProvider) for error tags
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [ContentType("text")]
    [TagType(typeof(DevAssistErrorTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class DevAssistErrorTaggerProvider : ITaggerProvider
    {
        // Static instance for external access
        private static DevAssistErrorTaggerProvider _instance;

        // Cache taggers per buffer to ensure single instance per buffer
        private readonly Dictionary<ITextBuffer, DevAssistErrorTagger> _taggers =
            new Dictionary<ITextBuffer, DevAssistErrorTagger>();

        public DevAssistErrorTaggerProvider()
        {
            System.Diagnostics.Debug.WriteLine("DevAssist Markers: DevAssistErrorTaggerProvider constructor called - MEF is loading error tagger provider");
            _instance = this;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist Markers: CreateTagger called - buffer: {buffer != null}");

            if (buffer == null)
                return null;

            // Return cached tagger if it exists, otherwise create new one
            lock (_taggers)
            {
                if (_taggers.TryGetValue(buffer, out var existingTagger))
                {
                    System.Diagnostics.Debug.WriteLine("DevAssist Markers: Returning existing error tagger from cache");
                    return existingTagger as ITagger<T>;
                }

                System.Diagnostics.Debug.WriteLine("DevAssist Markers: Creating new error tagger");
                var tagger = new DevAssistErrorTagger(buffer);
                _taggers[buffer] = tagger;

                // Clean up when buffer is disposed
                buffer.Properties.GetOrCreateSingletonProperty(() =>
                {
                    buffer.Changed += (sender, args) =>
                    {
                        // Could add buffer change handling here if needed
                    };
                    return tagger;
                });

                return tagger as ITagger<T>;
            }
        }

        /// <summary>
        /// Gets the error tagger for a specific buffer
        /// Used by external components to update vulnerability markers
        /// Similar to JetBrains MarkupModel access pattern
        /// </summary>
        public static DevAssistErrorTagger GetTaggerForBuffer(ITextBuffer buffer)
        {
            if (_instance == null || buffer == null)
                return null;

            lock (_instance._taggers)
            {
                _instance._taggers.TryGetValue(buffer, out var tagger);
                return tagger;
            }
        }

        /// <summary>
        /// Gets all active error taggers
        /// Useful for debugging and diagnostics
        /// </summary>
        public static IEnumerable<DevAssistErrorTagger> GetAllTaggers()
        {
            if (_instance == null)
                return new List<DevAssistErrorTagger>();

            lock (_instance._taggers)
            {
                return new List<DevAssistErrorTagger>(_instance._taggers.Values);
            }
        }
    }
}

