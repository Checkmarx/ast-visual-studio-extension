using EnvDTE;
using System.Linq;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    public static class OutputPaneUtils
    {
        public static OutputWindowPane InitializeOutputPane(OutputWindow outputWindow, string paneName)
        {
            return outputWindow.OutputWindowPanes
                .Cast<OutputWindowPane>()
                .FirstOrDefault(p => p.Name == paneName)
                ?? outputWindow.OutputWindowPanes.Add(paneName);
        }
    }
}