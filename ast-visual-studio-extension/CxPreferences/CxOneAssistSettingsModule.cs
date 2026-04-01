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

        // MCP and welcome-page state flags
        public bool McpEnabled { get; set; } = false;
        public bool McpStatusChecked { get; set; } = false;
        public bool WelcomeShown { get; set; } = false;

        // Preserve user scanner preferences across MCP enable/disable transitions
        public bool UserPreferencesSet { get; set; } = false;
        public bool UserPrefAscaRealtime { get; set; } = true;
        public bool UserPrefOssRealtime { get; set; } = true;
        public bool UserPrefSecretDetectionRealtime { get; set; } = true;
        public bool UserPrefContainersRealtime { get; set; } = true;
        public bool UserPrefIacRealtime { get; set; } = true;

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
            SaveSettingsToStorage();
        }

        /// <summary>
        /// Explicitly persist all module settings to the registry
        /// </summary>
        public void PersistSettings()
        {
            SaveSettingsToStorage();
        }

        /// <summary>
        /// Checks if ASCA scanning is enabled in preferences
        /// </summary>
        /// <returns>True if ASCA is enabled, false otherwise</returns>
        public bool IsAscaEnabled()
        {
            return AscaCheckBox;
        }

        public bool AreAllRealtimeScannersEnabled()
        {
            return AscaCheckBox
                && OssRealtimeCheckBox
                && SecretDetectionRealtimeCheckBox
                && ContainersRealtimeCheckBox
                && IacRealtimeCheckBox;
        }

        public void SaveCurrentSettingsAsUserPreferences()
        {
            UserPrefAscaRealtime = AscaCheckBox;
            UserPrefOssRealtime = OssRealtimeCheckBox;
            UserPrefSecretDetectionRealtime = SecretDetectionRealtimeCheckBox;
            UserPrefContainersRealtime = ContainersRealtimeCheckBox;
            UserPrefIacRealtime = IacRealtimeCheckBox;
            UserPreferencesSet = true;
        }

        public void ApplyUserPreferencesToRealtimeSettings()
        {
            AscaCheckBox = UserPrefAscaRealtime;
            OssRealtimeCheckBox = UserPrefOssRealtime;
            SecretDetectionRealtimeCheckBox = UserPrefSecretDetectionRealtime;
            ContainersRealtimeCheckBox = UserPrefContainersRealtime;
            IacRealtimeCheckBox = UserPrefIacRealtime;
        }

        public void EnableAllRealtimeScanners()
        {
            AscaCheckBox = true;
            OssRealtimeCheckBox = true;
            SecretDetectionRealtimeCheckBox = true;
            ContainersRealtimeCheckBox = true;
            IacRealtimeCheckBox = true;
        }

        public void DisableAllRealtimeScanners()
        {
            AscaCheckBox = false;
            OssRealtimeCheckBox = false;
            SecretDetectionRealtimeCheckBox = false;
            ContainersRealtimeCheckBox = false;
            IacRealtimeCheckBox = false;
        }

        public void AutoEnableRealtimeScanners()
        {
            if (UserPreferencesSet)
                ApplyUserPreferencesToRealtimeSettings();
            else
            {
                EnableAllRealtimeScanners();
                SaveCurrentSettingsAsUserPreferences();
            }
        }

        public void DisableRealtimeScannersPreservingPreferences()
        {
            SaveCurrentSettingsAsUserPreferences();
            DisableAllRealtimeScanners();
        }
    }
}
