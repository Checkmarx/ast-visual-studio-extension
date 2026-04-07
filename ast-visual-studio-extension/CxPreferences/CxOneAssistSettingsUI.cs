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
        private bool _isMcpInstallInProgress;
        private CancellationTokenSource _mcpStatusDismissCts;
        private bool _isAuthEventSubscribed;

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

            ApplyAuthenticationState(CxPreferencesUI.IsAuthenticated());
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

                SetMcpStatus("Please authenticate first before using Checkmarx One Assist settings.", isSuccess: false, autoDismiss: false);
                return;
            }

            bool hasApiKey = !string.IsNullOrWhiteSpace(CxPreferencesUI.GetConfigSnapshot()?.ApiKey);
            lnkInstallMcp.Enabled = hasApiKey && !_isMcpInstallInProgress && mcpEnabled;
            lnkEditMcp.Enabled = true;

            if (!hasApiKey)
                SetMcpStatus("Please authenticate first before installing MCP.", isSuccess: false, autoDismiss: false);
            else if (!mcpEnabled)
                SetMcpStatus("MCP is disabled by your tenant settings.", isSuccess: false, autoDismiss: false);
            else
                SetMcpStatus(string.Empty, isSuccess: true, autoDismiss: false);
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
        }

        private void AscaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            try
            {
                bool isChecked = ascaCheckBox.Checked;
                cxOneAssistSettingsModule.AscaCheckBox = isChecked;
                cxOneAssistSettingsModule.PersistSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ASCA checkbox change failed: {ex.Message}");
            }
        }

        private void OssCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            cxOneAssistSettingsModule.OssRealtimeCheckBox = ossCheckBox.Checked;
            cxOneAssistSettingsModule.PersistSettings();
        }

        private void SecretsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            cxOneAssistSettingsModule.SecretDetectionRealtimeCheckBox = secretsCheckBox.Checked;
            cxOneAssistSettingsModule.PersistSettings();
        }

        private void ContainersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            cxOneAssistSettingsModule.ContainersRealtimeCheckBox = containersCheckBox.Checked;
            cxOneAssistSettingsModule.PersistSettings();
        }

        private void IacCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            cxOneAssistSettingsModule.IacRealtimeCheckBox = iacCheckBox.Checked;
            cxOneAssistSettingsModule.PersistSettings();
        }

        private void CmbContainersTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!CxPreferencesUI.IsAuthenticated())
                return;

            cxOneAssistSettingsModule.ContainersTool = cmbContainersTool.SelectedItem?.ToString() ?? "docker";
            cxOneAssistSettingsModule.PersistSettings();
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

            var config = CxPreferencesUI.GetConfigSnapshot();

            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                SetMcpStatus("Please authenticate first before installing MCP.", isSuccess: false, autoDismiss: false);
                return;
            }

            _isMcpInstallInProgress = true;
            lnkInstallMcp.Enabled = false;
            SetMcpStatus("Installing MCP configuration...", isSuccess: true, autoDismiss: false);

            try
            {
                var installService = new McpInstallService();
                McpInstallResult result = await installService.InstallAsync(config, GetType());

                SetMcpStatus(result.Message, isSuccess: result.Success, autoDismiss: result.Success);
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
