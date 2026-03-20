using ast_visual_studio_extension.CxExtension.Services;
using System;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    public partial class CxOneAssistSettingsUI : UserControl
    {
        internal CxOneAssistSettingsModule cxOneAssistSettingsModule;

        public delegate void EventHandler();
        public event EventHandler OnApplySettingsEvent = delegate { };
        private static CxOneAssistSettingsUI Instance;
        private static ASCAService _ascaService;

        private CxOneAssistSettingsUI()
        {
            InitializeComponent();
        }

        public static CxOneAssistSettingsUI GetInstance()
        {
            if (Instance == null)
            {
                Instance = new CxOneAssistSettingsUI();
            }

            return Instance;
        }

        public void ThrowEventOnApply()
        {
            OnApplySettingsEvent();
        }

        public void Initialize(CxOneAssistSettingsModule settingsModule)
        {
            cxOneAssistSettingsModule = settingsModule;
            ascaCheckBox.Checked = cxOneAssistSettingsModule.AscaCheckBox;
            ossCheckBox.Checked = cxOneAssistSettingsModule.OssRealtimeCheckBox;
            secretsCheckBox.Checked = cxOneAssistSettingsModule.SecretDetectionRealtimeCheckBox;
            containersCheckBox.Checked = cxOneAssistSettingsModule.ContainersRealtimeCheckBox;
            iacCheckBox.Checked = cxOneAssistSettingsModule.IacRealtimeCheckBox;
            cmbContainersTool.SelectedItem = cxOneAssistSettingsModule.ContainersTool ?? "docker";
        }

        private async void AscaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                bool isChecked = ascaCheckBox.Checked;
                cxOneAssistSettingsModule.AscaCheckBox = isChecked;

                if (isChecked)
                {
                    var parentModule = new CxPreferencesModule();
                    CxCLI.CxWrapper cxWrapper = new CxCLI.CxWrapper(parentModule.GetCxConfig(), GetType());
                    _ascaService = ASCAService.GetInstance(cxWrapper);
                    await _ascaService.InitializeASCAAsync();
                }
                else
                {
                    // If ASCA is disabled, dispose the service if it exists
                    if (_ascaService != null)
                    {
                        await _ascaService.UnregisterTextChangeEventsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ASCA checkbox change failed: {ex.Message}");
            }
        }

        private void OssCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            cxOneAssistSettingsModule.OssRealtimeCheckBox = ossCheckBox.Checked;
        }

        private void SecretsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            cxOneAssistSettingsModule.SecretDetectionRealtimeCheckBox = secretsCheckBox.Checked;
        }

        private void ContainersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            cxOneAssistSettingsModule.ContainersRealtimeCheckBox = containersCheckBox.Checked;
        }

        private void IacCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            cxOneAssistSettingsModule.IacRealtimeCheckBox = iacCheckBox.Checked;
        }

        private void CmbContainersTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            cxOneAssistSettingsModule.ContainersTool = cmbContainersTool.SelectedItem?.ToString() ?? "docker";
        }

        private void LnkInstallMcp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://checkmarx.com/resource/documents/en/34965-install-mcp");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open MCP documentation: {ex.Message}");
            }
        }

        private void LnkEditMcp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // Open mcp.json file from user profile
                string homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                string mcpJsonPath = System.IO.Path.Combine(homeDir, ".codeium", "mcp.json");

                if (System.IO.File.Exists(mcpJsonPath))
                {
                    System.Diagnostics.Process.Start(mcpJsonPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"mcp.json not found at: {mcpJsonPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open mcp.json: {ex.Message}");
            }
        }
    }
}
