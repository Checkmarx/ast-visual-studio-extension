using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension.CxExtension.Panels
{
    internal abstract class BasePanel
    {
        public AsyncPackage package;
        private CxWindowControl cxWindowUI;

        public BasePanel(AsyncPackage package) 
        { 
            this.package = package;
        }

        public CxWindowControl GetCxWindowControl()
        {
            if(cxWindowUI == null)
            {
                ToolWindowPane window = package.FindToolWindow(typeof(CxWindow), 0, true);

                cxWindowUI = window.Content as CxWindowControl;
            }
            
            return cxWindowUI;
        }
    }
}
