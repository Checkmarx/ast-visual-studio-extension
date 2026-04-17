using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxPreferences
{
    /// <summary>
    /// Checkmarx One Assist Settings Page (child page)
    /// </summary>
    [Guid("e2527bed-dc52-4188-9e62-c8037a3fc797")]
    public class CxOneAssistSettingsModule : DialogPage
    {
        /// <summary>
        /// Fired after Assist realtime-related settings are persisted (checkboxes, Apply, welcome dialog).
        /// JetBrains parity: GlobalScannerController settingsApplied / syncAll.
        /// </summary>
        public static event EventHandler RealtimeAssistSettingsChanged;

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

        /// <summary>
        /// Product entitlement flags (cached during authentication). JetBrains: GlobalSettingsState DevAssist/OneAssist license.
        /// Default true until auth flow sets them from the tenant.
        /// </summary>
        public bool DevAssistLicenseEnabled { get; set; } = true;

        public bool OneAssistLicenseEnabled { get; set; } = true;

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
        /// After registry reload (e.g. Options Cancel), sync the custom Assist UI from this page's properties.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            try
            {
                CxOneAssistSettingsUI.GetInstance()?.RefreshCheckboxesFromModule();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CxOneAssistSettingsModule.LoadSettingsFromStorage UI refresh: {ex.Message}");
            }
        }
        internal Microsoft.VisualStudio.Shell.Package GetOwnerPackage()
            => GetService(typeof(Microsoft.VisualStudio.Shell.Package)) as Microsoft.VisualStudio.Shell.Package;

        /// <summary>
        /// On apply settings
        /// </summary>
        /// <param name="e"></param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            // Flush UI → properties so serialization matches the Tools → Options surface (also marks page dirty via property updates when handlers ran).
            CxOneAssistSettingsUI.GetInstance().ApplyUiToModule(this);
            base.OnApply(e);
            // Treat OK/Apply on Assist page as the baseline for logout / next login (per-engine toggles).
            SaveCurrentSettingsAsUserPreferences();
            PersistSettings();
        }

        /// <summary>
        /// Explicitly persist all module settings to the registry and notify listeners to resync realtime scanners.
        /// Also syncs scanner enable/disable state so ClearFindingsFromDisabledScanners() has correct state.
        /// </summary>
        public void PersistSettings()
        {
            SaveSettingsToStorage();

            // Sync scanner enabled/disabled state BEFORE firing the event so ClearFindingsFromDisabledScanners() has current state
            CxAssistConstants.SetScannerEnabled(ScannerType.ASCA, AscaCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.OSS, OssRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.Secrets, SecretDetectionRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.Containers, ContainersRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.IaC, IacRealtimeCheckBox);

            RealtimeAssistSettingsChanged?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// JetBrains <c>GlobalSettingsComponent.disableAllRealtimeScanners</c>: when MCP becomes unavailable,
        /// turns off all engines but only snapshots preferences if the user never had a saved baseline yet.
        /// </summary>
        public void DisableAllRealtimeScannersWhenMcpUnavailable()
        {
            if (!UserPreferencesSet)
                SaveCurrentSettingsAsUserPreferences();
            DisableAllRealtimeScanners();
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
