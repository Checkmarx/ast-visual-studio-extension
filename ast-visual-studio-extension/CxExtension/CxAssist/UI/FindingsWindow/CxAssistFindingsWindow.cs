using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("8F3E8B6A-1234-4567-89AB-CDEF01234567")]
    public class CxAssistFindingsWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CxAssistFindingsWindow"/> class.
        /// </summary>
        public CxAssistFindingsWindow() : base(null)
        {
            this.Caption = "Checkmarx Findings";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new CxAssistFindingsControl();
        }

        /// <summary>
        /// Get the control hosted in this tool window
        /// </summary>
        public CxAssistFindingsControl FindingsControl => this.Content as CxAssistFindingsControl;
    }
}

