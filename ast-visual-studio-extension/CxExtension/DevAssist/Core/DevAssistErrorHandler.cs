using System;
using System.Diagnostics;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core
{
    /// <summary>
    /// Central error handling for DevAssist so third-party plugins or VS errors
    /// do not crash gutter, underline, problem window, or hover.
    /// We log and swallow exceptions at VS callback boundaries (GetTags, GenerateGlyph, etc.).
    /// </summary>
    internal static class DevAssistErrorHandler
    {
        private const string Category = "DevAssist";

        /// <summary>
        /// Logs the exception and returns without rethrowing.
        /// Use at VS/extension callback boundaries (GetTags, GenerateGlyph, event handlers)
        /// so our code never throws into VS or other extensions.
        /// </summary>
        /// <param name="ex">The exception (can be from our code, third-party, or VS).</param>
        /// <param name="context">Short description of where it happened (e.g. "GlyphTagger.GetTags").</param>
        public static void LogAndSwallow(Exception ex, string context)
        {
            if (ex == null) return;
            try
            {
                Debug.WriteLine($"[{Category}] {context}: {ex.Message}");
                Debug.WriteLine($"[{Category}] {ex.StackTrace}");
            }
            catch
            {
                // Do not throw from error handler
            }
        }

        /// <summary>
        /// Wraps an action in try-catch; on exception logs and swallows (does not rethrow).
        /// Returns true if the action ran without exception, false otherwise.
        /// </summary>
        public static bool TryRun(Action action, string context)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogAndSwallow(ex, context);
                return false;
            }
        }

        /// <summary>
        /// Tries to run a function; on exception logs, swallows, and returns default(T).
        /// </summary>
        public static T TryGet<T>(Func<T> func, string context, T defaultValue = default)
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception ex)
            {
                LogAndSwallow(ex, context);
                return defaultValue;
            }
        }
    }
}
