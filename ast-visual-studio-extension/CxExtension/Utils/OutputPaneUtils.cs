using EnvDTE;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public static class OutputPaneUtils
    {
        /// <summary>
        /// Returns an existing output pane with the given name, or creates it with <see cref="OutputWindowPanes.Add"/>.
        /// Prefer this over always calling <c>Add</c>, which would create duplicate panes when the name already exists.
        /// </summary>
        public static OutputWindowPane InitializeOutputPane(OutputWindow outputWindow, string paneName)
        {
            return outputWindow.OutputWindowPanes
                .Cast<OutputWindowPane>()
                .FirstOrDefault(p => p.Name == paneName)
                ?? outputWindow.OutputWindowPanes.Add(paneName);
        }
    }
}