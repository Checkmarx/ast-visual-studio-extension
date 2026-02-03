using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.GutterIcons
{
    /// <summary>
    /// MEF provider for DevAssist glyph tagger
    /// Based on JetBrains EditorFactoryListener pattern adapted for Visual Studio
    /// Creates and manages tagger instances per buffer (not per view)
    /// IMPORTANT: Uses ITaggerProvider (not IViewTaggerProvider) for glyph tags
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [ContentType("text")]
    [TagType(typeof(DevAssistGlyphTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class DevAssistGlyphTaggerProvider : ITaggerProvider
    {
        // Static instance for external access
        private static DevAssistGlyphTaggerProvider _instance;

        // Cache taggers per buffer to ensure single instance per buffer
        private readonly Dictionary<ITextBuffer, DevAssistGlyphTagger> _taggers =
            new Dictionary<ITextBuffer, DevAssistGlyphTagger>();

        public DevAssistGlyphTaggerProvider()
        {
            System.Diagnostics.Debug.WriteLine("DevAssist: DevAssistGlyphTaggerProvider constructor called - MEF is loading this provider");
            _instance = this;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            System.Diagnostics.Debug.WriteLine($"DevAssist: CreateTagger called - buffer: {buffer != null}");

            if (buffer == null)
                return null;

            // Return existing tagger or create new one
            lock (_taggers)
            {
                if (!_taggers.TryGetValue(buffer, out var tagger))
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist: CreateTagger - creating NEW tagger for buffer");
                    tagger = new DevAssistGlyphTagger(buffer);
                    _taggers[buffer] = tagger;

                    // Store tagger in buffer properties for external access
                    try
                    {
                        buffer.Properties.AddProperty(typeof(DevAssistGlyphTagger), tagger);
                        System.Diagnostics.Debug.WriteLine($"DevAssist: CreateTagger - tagger stored in buffer properties");
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"DevAssist: CreateTagger - tagger already in buffer properties");
                    }

                    // Clean up when buffer is closed
                    buffer.Properties.GetOrCreateSingletonProperty(() => new BufferClosedListener(buffer, () =>
                    {
                        lock (_taggers)
                        {
                            _taggers.Remove(buffer);
                            buffer.Properties.RemoveProperty(typeof(DevAssistGlyphTagger));
                        }
                    }));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DevAssist: CreateTagger - returning EXISTING tagger");
                }

                return tagger as ITagger<T>;
            }
        }

        /// <summary>
        /// Gets the tagger for a specific buffer (for external access)
        /// Allows DevAssistPOC or other components to update vulnerabilities
        /// IMPORTANT: Only returns taggers created by MEF through CreateTagger()
        /// This ensures Visual Studio is properly subscribed to TagsChanged events
        /// </summary>
        public static DevAssistGlyphTagger GetTaggerForBuffer(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                System.Diagnostics.Debug.WriteLine("DevAssist: GetTaggerForBuffer - buffer is null");
                return null;
            }

            // ONLY get tagger from buffer properties - do NOT create it directly
            // The tagger MUST be created by MEF through CreateTagger() so that
            // Visual Studio subscribes to the TagsChanged event
            if (buffer.Properties.TryGetProperty(typeof(DevAssistGlyphTagger), out DevAssistGlyphTagger tagger))
            {
                System.Diagnostics.Debug.WriteLine("DevAssist: GetTaggerForBuffer - found tagger in buffer properties");
                return tagger;
            }

            // Also check instance cache
            if (_instance != null)
            {
                lock (_instance._taggers)
                {
                    if (_instance._taggers.TryGetValue(buffer, out tagger))
                    {
                        System.Diagnostics.Debug.WriteLine("DevAssist: GetTaggerForBuffer - found tagger in instance cache");
                        return tagger;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("DevAssist: GetTaggerForBuffer - tagger NOT found (MEF hasn't created it yet)");
            return null;
        }

        /// <summary>
        /// Helper class to clean up taggers when buffer is closed
        /// </summary>
        private class BufferClosedListener
        {
            private readonly ITextBuffer _buffer;
            private readonly Action _onClosed;

            public BufferClosedListener(ITextBuffer buffer, Action onClosed)
            {
                _buffer = buffer;
                _onClosed = onClosed;
                _buffer.Changed += OnBufferChanged;
            }

            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                // Check if buffer is being disposed
                if (_buffer.Properties.ContainsProperty("BufferClosed"))
                {
                    _buffer.Changed -= OnBufferChanged;
                    _onClosed?.Invoke();
                }
            }
        }
    }
}

