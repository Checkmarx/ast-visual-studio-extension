using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace ast_visual_studio_extension.CxExtension
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    [Guid("9cc11b43-869b-4804-a3c3-cd202b8ec977")]
    public class CxWindow : ToolWindowPane
    {

        public CxWindow(object context) : base(null)
        {
            Caption = CxConstants.EXTENSION_TITLE;

            AsyncPackage package = context as AsyncPackage;

            if (CxUtils.AreCxCredentialsDefined(package))
            {
                Content = new CxWindowControl(package);
            }
            else
            {
                Content = new CxInitialPanel(package);
            }
        }
    }
}
