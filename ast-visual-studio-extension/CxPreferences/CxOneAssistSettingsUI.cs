using ast_visual_studio_extension.CxPreferences.Configuration;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace ast_visual_studio_extension.CxPreferences
{
    public partial class CxOneAssistSettingsUI : UserControl
    {
        internal CxOneAssistSettingsModule cxOneAssistSettingsModule;

        private static CxOneAssistSettingsUI Instance;
        public delegate void EventHandler();
        public event EventHandler OnApplySettingsEvent = delegate { };
        private bool _isMcpInstallInProgress;
        private CancellationTokenSource _mcpStatusDismissCts;
        private bool _isAuthEventSubscribed;
        private System.Threading.Timer _scannerCheckboxDebounceTimer;
        private const int DebounceDelayMs = 300;  // Batch checkbox changes within 300ms

        private static readonly Color McpSuccessColor = Color.FromArgb(0, 120, 50);
        private static readonly Color McpErrorColor = Color.FromArgb(160, 0, 0);

        private CxOneAssistSettingsUI()
        {
            InitializeComponent();
            EnsureAuthSubscription();
        }

        public static CxOneAssistSettingsUI GetInstance()
        {
            if (Instance == null)
            {
                Instance = new CxOneAssistSettingsUI();
            }

            return Instance;
        }

        public void Initialize(CxOneAssistSettingsModule settingsModule)
        {
            cxOneAssistSettingsModule = settingsModule;
            EnsureAuthSubscription();

            // Only set checked state and selected item; enabled state is handled by ApplyAuthenticationState
            ascaCheckBox.Checked = cxOneAssistSettingsModule.AscaCheckBox;
            ossCheckBox.Checked = cxOneAssistSettingsModule.OssRealtimeCheckBox;
            secretsCheckBox.Checked = cxOneAssistSettingsModule.SecretDetectionRealtimeCheckBox;
            containersCheckBox.Checked = cxOneAssistSettingsModule.ContainersRealtimeCheckBox;
            iacCheckBox.Checked = cxOneAssistSettingsModule.IacRealtimeCheckBox;
            cmbContainersTool.SelectedItem = cxOneAssistSettingsModule.ContainersTool ?? "docker";
            SetMcpConnectionControls(cxOneAssistSettingsModule);

            ApplyAuthenticationState(CxPreferencesUI.IsAuthenticated());
        }

        private void SetMcpConnectionControls(CxOneAssistSettingsModule module)
        {
            rbMcpAuthOAuth.Checked = module.McpAuthMode == McpAuthMode.OAuth;
            rbMcpAuthApiKey.Checked = module.McpAuthMode != McpAuthMode.OAuth;
            txtMcpOAuthServerUrl.Text = module.McpOAuthServerUrl ?? string.Empty;
            txtMcpOAuthTenant.Text = module.McpOAuthTenant ?? string.Empty;
            UpdateMcpOAuthFieldsEnabled();
        }

        private void ApplyMcpConnectionUiToModule(CxOneAssistSettingsModule module)
        {
            module.McpAuthMode = rbMcpAuthOAuth.Checked ? McpAuthMode.OAuth : McpAuthMode.ApiKey;
            module.McpOAuthServerUrl = txtMcpOAuthServerUrl.Text.Trim();
            module.McpOAuthTenant = txtMcpOAuthTenant.Text.Trim();
        }

        private void UpdateMcpOAuthFieldsEnabled()
        {
            bool enabled = rbMcpAuthOAuth.Enabled && rbMcpAuthOAuth.Checked;
            txtMcpOAuthServerUrl.Enabled = enabled;
            txtMcpOAuthTenant.Enabled = enabled;
        }

        /// <summary>
        /// Validates the OAuth server URL / tenant fields. Only relevant while OAuth mode is selected.
        /// </summary>
        internal bool TryValidateMcpConnection(out string error)
        {
            error = null;
            if (!CxPreferencesUI.IsAuthenticated() || !rbMcpAuthOAuth.Checked)
                return true;

            return McpInstallService.TryValidateOAuthOverrides(txtMcpOAuthServerUrl.Text, txtMcpOAuthTenant.Text, out error);
        }

        internal void ShowMcpValidationError(string error)
        {
            SetMcpStatus(error, isSuccess: false, autoDismiss: false);
        }

        private void EnsureAuthSubscription()
        {
            if (_isAuthEventSubscribed)
                return;

            CxPreferencesUI.AuthStateChanged += OnAuthStateChanged;
            _isAuthEventSubscribed = true;
        }

        private void OnAuthStateChanged(bool isAuthenticated)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ApplyAuthenticationState(isAuthenticated)));
                return;
            }

            ApplyAuthenticationState(isAuthenticated);
        }

        private void ApplyAuthenticationState(bool isAuthenticated)
        {
            bool mcpEnabled = cxOneAssistSettingsModule?.McpEnabled == true;
            bool enableScanners = isAuthenticated && mcpEnabled;
            SetInteractiveControlsEnabled(enableScanners);

            if (!isAuthenticated)
            {
                // Only update UI controls, never touch module/registry here.
                // Module state is managed by explicit logout or fresh login flows.
                ascaCheckBox.Checked = false;
                ossCheckBox.Checked = false;
                secretsCheckBox.Checked = false;
                containersCheckBox.Checked = false;
                iacCheckBox.Checked = false;
                cmbContainersTool.SelectedItem = "docker";
                // MCP auth mode / OAuth URL / tenant are not reset here: they are user preferences that must
                // survive logout, and a stale "API Key" radio would be written back to the module on next Apply.

                SetMcpStatus("Please authenticate first before using Checkmarx One Assist settings.", isSuccess: false, autoDismiss: false);
                return;
            }

            bool hasApiKey = !string.IsNullOrWhiteSpace(
                CxPreferencesUI.GetCxConfigFromPackage(cxOneAssistSettingsModule?.GetOwnerPackage() as Package)?.ApiKey);
            lnkInstallMcp.Enabled = hasApiKey && !_isMcpInstallInProgress && mcpEnabled;
            lnkEditMcp.Enabled = true;
            rbMcpAuthApiKey.Enabled = hasApiKey && mcpEnabled;
            rbMcpAuthOAuth.Enabled = hasApiKey && mcpEnabled;
            UpdateMcpOAuthFieldsEnabled();

            if (!hasApiKey)
                SetMcpStatus("Please authenticate first before installing MCP.", isSuccess: false, autoDismiss: false);
            else if (!mcpEnabled)
                SetMcpStatus("MCP is disabled by your tenant settings.", isSuccess: false, autoDismiss: false);
            else
                SetMcpStatus("MCP is enabled for your tenant.", isSuccess: true, autoDismiss: false);
        }

        public void RefreshCheckboxesFromModule()
        {
            if (cxOneAssistSettingsModule == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshCheckboxesFromModule);
                return;
            }

            ascaCheckBox.Checked = cxOneAssistSettingsModule.AscaCheckBox;
            ossCheckBox.Checked = cxOneAssistSettingsModule.OssRealtimeCheckBox;
            secretsCheckBox.Checked = cxOneAssistSettingsModule.SecretDetectionRealtimeCheckBox;
            containersCheckBox.Checked = cxOneAssistSettingsModule.ContainersRealtimeCheckBox;
            iacCheckBox.Checked = cxOneAssistSettingsModule.IacRealtimeCheckBox;
            cmbContainersTool.SelectedItem = cxOneAssistSettingsModule.ContainersTool ?? "docker";
            SetMcpConnectionControls(cxOneAssistSettingsModule);

            // AuthStateChanged can run before tenant MCP flags are written; sync Install MCP / status from module.
            ApplyAuthenticationState(CxPreferencesUI.IsAuthenticated());
        }

        /// <summary>
        /// Copies current control values into the settings module. Call from <see cref="CxOneAssistSettingsModule.OnApply"/> only
        /// so registry and realtime scanners update on OK/Apply, not on every checkbox click.
        /// </summary>
        internal void ApplyUiToModule(CxOneAssistSettingsModule module)
        {
            if (module == null)
                return;

            if (!CxPreferencesUI.IsAuthenticated())
                return;

            // MCP connection settings are independent of the realtime-scanner enablement below.
            ApplyMcpConnectionUiToModule(module);

            // Only allow scanner changes when MCP is enabled
            if (!module.McpEnabled)
                return;

            module.AscaCheckBox = ascaCheckBox.Checked;
            module.OssRealtimeCheckBox = ossCheckBox.Checked;
            module.SecretDetectionRealtimeCheckBox = secretsCheckBox.Checked;
            module.ContainersRealtimeCheckBox = containersCheckBox.Checked;
            module.IacRealtimeCheckBox = iacCheckBox.Checked;
            module.ContainersTool = cmbContainersTool.SelectedItem?.ToString() ?? "docker";
        }

        private void SetInteractiveControlsEnabled(bool enabled)
        {
            // Keep labels/group captions visible; disable only interactive controls.
            ascaCheckBox.Enabled = enabled;
            ossCheckBox.Enabled = enabled;
            secretsCheckBox.Enabled = enabled;
            containersCheckBox.Enabled = enabled;
            iacCheckBox.Enabled = enabled;
            cmbContainersTool.Enabled = enabled;
            lnkInstallMcp.Enabled = enabled;
            lnkEditMcp.Enabled = enabled;
            rbMcpAuthApiKey.Enabled = enabled;
            rbMcpAuthOAuth.Enabled = enabled;
            UpdateMcpOAuthFieldsEnabled();
        }

        /// <summary>
        /// Keeps <see cref="CxOneAssistSettingsModule"/> in sync with the form for Options dirty-state / serialization,
        /// without writing registry or resyncing realtime scanners until <see cref="CxOneAssistSettingsModule.OnApply"/>.
        /// </summary>
        private void SyncAssistUiToModuleProperties()
        {
            ApplyUiToModule(cxOneAssistSettingsModule);
        }

        private void AscaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void OssCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void SecretsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void ContainersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void IacCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        /// <summary>
        /// Debounces scanner checkbox changes so multiple checkbox clicks batch into a single sync operation.
        /// This prevents enabling/disabling scanners one-by-one which causes delays.
        /// </summary>
        private void DebounceSyncAssistUi()
        {
            // Dispose existing timer
            if (_scannerCheckboxDebounceTimer != null)
            {
                _scannerCheckboxDebounceTimer.Dispose();
            }

            // Start new timer to sync after user stops clicking
            _scannerCheckboxDebounceTimer = new System.Threading.Timer(
                callback: (_) =>
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke((Action)SyncAssistUiToModuleProperties);
                    }
                    else
                    {
                        SyncAssistUiToModuleProperties();
                    }
                    _scannerCheckboxDebounceTimer?.Dispose();
                },
                state: null,
                dueTime: DebounceDelayMs,
                period: Timeout.Infinite);
        }

        private void CmbContainersTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            SyncAssistUiToModuleProperties();
        }

        private void RbMcpAuthApiKey_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMcpOAuthFieldsEnabled();
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void RbMcpAuthOAuth_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMcpOAuthFieldsEnabled();
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private void TxtMcpOAuthSetting_TextChanged(object sender, EventArgs e)
        {
            if (cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated())
                return;
            DebounceSyncAssistUi();
        }

        private async void LnkInstallMcp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
            {
                SetMcpStatus("Please authenticate first before installing MCP.", isSuccess: false, autoDismiss: false);
                return;
            }

            if (_isMcpInstallInProgress)
                return;

            var config = CxPreferencesUI.GetCxConfigFromPackage(cxOneAssistSettingsModule?.GetOwnerPackage() as Package);

            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                SetMcpStatus("Please authenticate first before installing MCP.", isSuccess: false, autoDismiss: false);
                return;
            }

            if (!TryValidateMcpConnection(out string validationError))
            {
                SetMcpStatus(validationError, isSuccess: false, autoDismiss: false);
                return;
            }

            // Read the form directly (the module sync is debounced) and commit the MCP connection settings,
            // so the silent reinstall on next login/restore writes the same entry even if Options is cancelled.
            ApplyMcpConnectionUiToModule(cxOneAssistSettingsModule);
            cxOneAssistSettingsModule.SaveMcpConnectionSettingsToRegistry();
            McpConnectionSettings connectionSettings = cxOneAssistSettingsModule.GetMcpConnectionSettings();

            _isMcpInstallInProgress = true;
            lnkInstallMcp.Enabled = false;
            SetMcpStatus("Installing MCP configuration...", isSuccess: true, autoDismiss: false);

            try
            {
                var installService = new McpInstallService();
                McpInstallResult result = await installService.InstallAsync(config, connectionSettings, GetType());

                bool isOAuth = connectionSettings.AuthMode == McpAuthMode.OAuth;
                if (result.Success && isOAuth)
                {
                    // Sign in now rather than when Copilot first starts the server: VS gives an MCP server ~60 s to
                    // initialize, which a first browser sign-in can exceed.
                    SetMcpStatus(result.Message + " Complete the sign-in in your browser...", isSuccess: true, autoDismiss: false);
                    result = await new McpOAuthSignIn().SignInAsync(result.McpUrl);
                }

                // Keep the OAuth message (it contains the endpoint URL) visible so it can be checked.
                SetMcpStatus(result.Message, isSuccess: result.Success, autoDismiss: result.Success && !isOAuth);
            }
            finally
            {
                _isMcpInstallInProgress = false;
                lnkInstallMcp.Enabled = true;
            }
        }

        private void LnkEditMcp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
            {
                SetMcpStatus("Please authenticate first before editing MCP configuration.", isSuccess: false, autoDismiss: false);
                return;
            }

            try
            {
                string mcpJsonPath = new McpConfigManager().GetMcpConfigPath();

                if (File.Exists(mcpJsonPath))
                {
                    CloseHostingWindow();

                    VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, mcpJsonPath);
                }
                else
                {
                    SetMcpStatus($".mcp.json not found at: {mcpJsonPath}", isSuccess: false, autoDismiss: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open mcp.json: {ex.Message}");
                SetMcpStatus("Failed to open .mcp.json.", isSuccess: false, autoDismiss: false);
            }
        }

        private void CloseHostingWindow()
        {
            try
            {
                // Use Windows API to find and close the Options window.
                IntPtr optionsWindow = FindWindow(null, "Options");
                if (optionsWindow != IntPtr.Zero)
                {
                    PostMessage(optionsWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                // Fallback: traverse up to topmost parent Form and close it.
                Control current = this;
                while (current.Parent != null)
                {
                    current = current.Parent;
                }

                if (current is Form topForm)
                {
                    // Use BeginInvoke to ensure close is called on the UI thread.
                    topForm.BeginInvoke(new Action(() => topForm.Close()));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to close host window: {ex.Message}");
            }
        }

        // Windows API declarations
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void SetMcpStatus(string message, bool isSuccess, bool autoDismiss)
        {
            lblMcpStatus.Text = message;
            lblMcpStatus.ForeColor = isSuccess ? McpSuccessColor : McpErrorColor;

            if (autoDismiss)
                _ = AutoDismissMcpStatusAsync();
        }

        private async Task AutoDismissMcpStatusAsync()
        {
            _mcpStatusDismissCts?.Cancel();
            _mcpStatusDismissCts = new CancellationTokenSource();
            CancellationToken token = _mcpStatusDismissCts.Token;

            try
            {
                await Task.Delay(5000, token);
                if (!token.IsCancellationRequested)
                    lblMcpStatus.Text = string.Empty;
            }
            catch (TaskCanceledException)
            {
                // Replaced by a newer status message.
            }
        }

    }
}
