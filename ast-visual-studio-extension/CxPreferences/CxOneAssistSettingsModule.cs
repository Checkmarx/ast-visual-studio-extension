using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Settings;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxPreferences.Configuration;
using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell.Settings;

namespace ast_visual_studio_extension.CxPreferences
{
    /// <summary>
    /// Checkmarx One Assist Settings Page (child page)
    /// </summary>
    [Guid("e2527bed-dc52-4188-9e62-c8037a3fc797")]
    public class CxOneAssistSettingsModule : DialogPage
    {
        private static readonly string UserPreferencesCollection = "Checkmarx/CxAssist/UserPreferences";
        private static readonly string McpConnectionCollection = "Checkmarx/CxAssist/McpConnection";

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
        /// User's chosen authentication mode for the MCP server entry in .mcp.json.
        /// Independent of CLI authentication, which always uses the API key.
        /// MCP connection settings are persisted to their own registry collection (not DialogPage
        /// serialization) so "Install MCP" can commit them without applying the rest of the page.
        /// </summary>
        internal McpAuthMode McpAuthMode { get; set; } = McpAuthMode.ApiKey;

        /// <summary>Checkmarx One server URL for the OAuth MCP entry; blank = derived from the API key.</summary>
        internal string McpOAuthServerUrl { get; set; } = string.Empty;

        /// <summary>Tenant for the OAuth MCP entry; blank = derived from the API key.</summary>
        internal string McpOAuthTenant { get; set; } = string.Empty;

        internal McpConnectionSettings GetMcpConnectionSettings()
            => new McpConnectionSettings(McpAuthMode, McpOAuthServerUrl, McpOAuthTenant);

        // What the registry holds for the MCP connection; null until loaded or saved once.
        private string _persistedMcpConnectionSignature;

        /// <summary>
        /// Product entitlement flags (cached during authentication). JetBrains: GlobalSettingsState DevAssist/OneAssist license.
        /// Default true until auth flow sets them from the tenant.
        /// </summary>
        public bool DevAssistLicenseEnabled { get; set; } = true;

        public bool OneAssistLicenseEnabled { get; set; } = true;

        // Preserve user scanner preferences across MCP enable/disable transitions (persisted to registry, not DialogPage)
        internal bool UserPreferencesSet { get; set; } = false;
        internal bool UserPrefAscaRealtime { get; set; } = true;
        internal bool UserPrefOssRealtime { get; set; } = true;
        internal bool UserPrefSecretDetectionRealtime { get; set; } = true;
        internal bool UserPrefContainersRealtime { get; set; } = true;
        internal bool UserPrefIacRealtime { get; set; } = true;

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
        /// Also load user preferences from registry.
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            LoadUserPreferencesFromRegistry();
            LoadMcpConnectionSettingsFromRegistry();
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
            CxOneAssistSettingsUI settingsUI = CxOneAssistSettingsUI.GetInstance();

            // An invalid OAuth URL/tenant would otherwise be persisted and break every silent MCP reinstall.
            if (!settingsUI.TryValidateMcpConnection(out string mcpConnectionError))
            {
                settingsUI.ShowMcpValidationError(mcpConnectionError);
                e.ApplyBehavior = ApplyKind.Cancel;
                return;
            }

            string persistedMcpConnection = _persistedMcpConnectionSignature;

            // Flush UI → properties so serialization matches the Tools → Options surface (also marks page dirty via property updates when handlers ran).
            settingsUI.ApplyUiToModule(this);
            base.OnApply(e);
            // Treat OK/Apply on Assist page as the baseline for logout / next login (per-engine toggles).
            SaveCurrentSettingsAsUserPreferences();
            PersistSettings();

            if (persistedMcpConnection != null && persistedMcpConnection != McpConnectionSignature)
                ReinstallMcpInBackground();
        }

        private string McpConnectionSignature => McpAuthMode + "|" + McpOAuthServerUrl + "|" + McpOAuthTenant;

        /// <summary>
        /// Rewrites the .mcp.json entry after the MCP connection settings changed on OK/Apply, so the change does not
        /// wait for the next login. Silent: no browser sign-in and no session reset (Install MCP does those).
        /// </summary>
        private void ReinstallMcpInBackground()
        {
            if (!McpEnabled || !CxPreferencesUI.IsAuthenticated())
                return;

            CxConfig config = CxPreferencesUI.GetCxConfigFromPackage(GetOwnerPackage());
            if (string.IsNullOrWhiteSpace(config?.ApiKey))
                return;

            McpConnectionSettings connectionSettings = GetMcpConnectionSettings();
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await new McpInstallService().InstallSilentlyAsync(config, connectionSettings, typeof(CxOneAssistSettingsModule));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MCP reinstall after settings change failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Explicitly persist all module settings to the registry and notify listeners to resync realtime scanners.
        /// Also syncs scanner enable/disable state so ClearFindingsFromDisabledScanners() has correct state.
        /// </summary>
        public void PersistSettings()
        {
            SaveSettingsToStorage();
            SaveUserPreferencesToRegistry();
            SaveMcpConnectionSettingsToRegistry();

            // Sync scanner enabled/disabled state BEFORE firing the event so ClearFindingsFromDisabledScanners() has current state
            CxAssistConstants.SetScannerEnabled(ScannerType.ASCA, AscaCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.OSS, OssRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.Secrets, SecretDetectionRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.Containers, ContainersRealtimeCheckBox);
            CxAssistConstants.SetScannerEnabled(ScannerType.IaC, IacRealtimeCheckBox);

            RealtimeAssistSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveCurrentSettingsAsUserPreferences()
        {
            UserPrefAscaRealtime = AscaCheckBox;
            UserPrefOssRealtime = OssRealtimeCheckBox;
            UserPrefSecretDetectionRealtime = SecretDetectionRealtimeCheckBox;
            UserPrefContainersRealtime = ContainersRealtimeCheckBox;
            UserPrefIacRealtime = IacRealtimeCheckBox;
            UserPreferencesSet = true;
            SaveUserPreferencesToRegistry();
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

        private void SaveUserPreferencesToRegistry()
        {
            try
            {
                var package = GetOwnerPackage() as AsyncPackage;
                if (package == null)
                    return;

                var settingsManager = new ShellSettingsManager(package);
                var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                if (!userSettingsStore.CollectionExists(UserPreferencesCollection))
                    userSettingsStore.CreateCollection(UserPreferencesCollection);

                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPreferencesSet", UserPreferencesSet);
                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPrefAscaRealtime", UserPrefAscaRealtime);
                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPrefOssRealtime", UserPrefOssRealtime);
                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPrefSecretDetectionRealtime", UserPrefSecretDetectionRealtime);
                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPrefContainersRealtime", UserPrefContainersRealtime);
                userSettingsStore.SetBoolean(UserPreferencesCollection, "UserPrefIacRealtime", UserPrefIacRealtime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save user preferences to registry: {ex.Message}");
            }
        }

        private void LoadUserPreferencesFromRegistry()
        {
            try
            {
                var package = GetOwnerPackage() as AsyncPackage;
                if (package == null)
                    return;

                var settingsManager = new ShellSettingsManager(package);
                var readOnlyStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

                if (!readOnlyStore.CollectionExists(UserPreferencesCollection))
                    return;

                UserPreferencesSet = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPreferencesSet", false);
                UserPrefAscaRealtime = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPrefAscaRealtime", true);
                UserPrefOssRealtime = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPrefOssRealtime", true);
                UserPrefSecretDetectionRealtime = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPrefSecretDetectionRealtime", true);
                UserPrefContainersRealtime = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPrefContainersRealtime", true);
                UserPrefIacRealtime = readOnlyStore.GetBoolean(UserPreferencesCollection, "UserPrefIacRealtime", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load user preferences from registry: {ex.Message}");
            }
        }

        internal void SaveMcpConnectionSettingsToRegistry()
        {
            try
            {
                var package = GetOwnerPackage() as AsyncPackage;
                if (package == null)
                    return;

                var settingsManager = new ShellSettingsManager(package);
                var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                if (!userSettingsStore.CollectionExists(McpConnectionCollection))
                    userSettingsStore.CreateCollection(McpConnectionCollection);

                userSettingsStore.SetString(McpConnectionCollection, "McpAuthMode", McpAuthMode.ToString());
                userSettingsStore.SetString(McpConnectionCollection, "McpOAuthServerUrl", McpOAuthServerUrl ?? string.Empty);
                userSettingsStore.SetString(McpConnectionCollection, "McpOAuthTenant", McpOAuthTenant ?? string.Empty);
                _persistedMcpConnectionSignature = McpConnectionSignature;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save MCP connection settings to registry: {ex.Message}");
            }
        }

        private void LoadMcpConnectionSettingsFromRegistry()
        {
            try
            {
                var package = GetOwnerPackage() as AsyncPackage;
                if (package == null)
                    return;

                var settingsManager = new ShellSettingsManager(package);
                var readOnlyStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

                if (readOnlyStore.CollectionExists(McpConnectionCollection))
                {
                    string authMode = readOnlyStore.GetString(McpConnectionCollection, "McpAuthMode", nameof(McpAuthMode.ApiKey));
                    McpAuthMode = Enum.TryParse(authMode, out McpAuthMode parsed) ? parsed : McpAuthMode.ApiKey;
                    McpOAuthServerUrl = readOnlyStore.GetString(McpConnectionCollection, "McpOAuthServerUrl", string.Empty);
                    McpOAuthTenant = readOnlyStore.GetString(McpConnectionCollection, "McpOAuthTenant", string.Empty);
                }

                _persistedMcpConnectionSignature = McpConnectionSignature;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load MCP connection settings from registry: {ex.Message}");
            }
        }
    }
}
