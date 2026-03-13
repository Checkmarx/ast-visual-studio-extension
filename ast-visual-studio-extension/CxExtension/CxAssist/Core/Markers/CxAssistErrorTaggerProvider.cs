using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Markers
{
    /// <summary>
    /// MEF provider for CxAssist error tagger
    /// Based on reference EditorFactoryListener pattern adapted for Visual Studio
    /// Creates and manages error tagger instances per buffer (not per view).
    /// Exports IErrorTag so VS built-in error layer draws squiggles using IErrorType (CompilerError / syntax error colour).
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [ContentType("text")]
    [TagType(typeof(IErrorTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class CxAssistErrorTaggerProvider : ITaggerProvider
    {
        // Static instance for external access
        private static CxAssistErrorTaggerProvider _instance;

        // Cache taggers per buffer to ensure single instance per buffer
        private readonly Dictionary<ITextBuffer, CxAssistErrorTagger> _taggers =
            new Dictionary<ITextBuffer, CxAssistErrorTagger>();

        public CxAssistErrorTaggerProvider()
        {
            System.Diagnostics.Debug.WriteLine("CxAssist Markers: CxAssistErrorTaggerProvider constructor called - MEF is loading error tagger provider");
            _instance = this;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            System.Diagnostics.Debug.WriteLine($"CxAssist Markers: CreateTagger called - buffer: {buffer != null}");

            if (buffer == null)
                return null;

            // Return cached tagger if it exists, otherwise create new one
            lock (_taggers)
            {
                if (_taggers.TryGetValue(buffer, out var existingTagger))
                {
                    System.Diagnostics.Debug.WriteLine("CxAssist Markers: Returning existing error tagger from cache");
                    return existingTagger as ITagger<T>;
                }

                System.Diagnostics.Debug.WriteLine("CxAssist Markers: Creating new error tagger");
                var tagger = new CxAssistErrorTagger(buffer);
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
        /// Similar to reference MarkupModel access pattern
        /// </summary>
        public static CxAssistErrorTagger GetTaggerForBuffer(ITextBuffer buffer)
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
        public static IEnumerable<CxAssistErrorTagger> GetAllTaggers()
        {
            if (_instance == null)
                return new List<CxAssistErrorTagger>();

            lock (_instance._taggers)
            {
                return new List<CxAssistErrorTagger>(_instance._taggers.Values);
            }
        }
    }
}

