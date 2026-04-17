using ast_visual_studio_extension.CxExtension.Utils;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Writes CxAssist messages to the VS Output Window (shared "Checkmarx" pane via <see cref="OutputPaneWriter"/>).
    /// </summary>
    internal static class CxAssistOutputPane
    {
        /// <summary>
        /// Writes a timestamped message to the Checkmarx output pane. Thread-safe (marshals to UI when needed).
        /// </summary>
        public static void WriteToOutputPane(string message)
        {
            try
            {
                OutputPaneWriter.WriteAssistLifecycle(message ?? string.Empty);
            }
            catch
            {
                // Output pane write is best-effort
            }
        }
    }
}
