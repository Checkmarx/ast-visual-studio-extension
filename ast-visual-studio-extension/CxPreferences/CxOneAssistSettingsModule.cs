using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    /// <summary>
    /// Checkmarx One Assist Settings Page (child page)
    /// </summary>
    [Guid("e2527bed-dc52-4188-9e62-c8037a3fc797")]
    public class CxOneAssistSettingsModule : DialogPage
    {
        public bool AscaCheckBox { get; set; } = true;
        public bool OssRealtimeCheckBox { get; set; } = true;
        public bool SecretDetectionRealtimeCheckBox { get; set; } = true;
        public bool ContainersRealtimeCheckBox { get; set; } = true;
        public bool IacRealtimeCheckBox { get; set; } = true;
        public string ContainersTool { get; set; } = "docker";

        protected override IWin32Window Window
        {
            get
            {
                CxOneAssistSettingsUI settingsUI = CxOneAssistSettingsUI.GetInstance();
                settingsUI.Initialize(this);
                return settingsUI;
            }
        }

        /// <summary>
        /// On apply settings
        /// </summary>
        /// <param name="e"></param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            CxOneAssistSettingsUI.GetInstance().ThrowEventOnApply();
        }

        /// <summary>
        /// Checks if ASCA scanning is enabled in preferences
        /// </summary>
        /// <returns>True if ASCA is enabled, false otherwise</returns>
        public bool IsAscaEnabled()
        {
            return AscaCheckBox;
        }
    }
}
