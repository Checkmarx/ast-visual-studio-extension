using ast_visual_studio_extension.CxExtension.Services;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    public partial class CxPreferencesUI : UserControl
    {
        internal CxPreferencesModule cxPreferencesModule;

        public delegate void EventHandler();
        public event EventHandler OnApplySettingsEvent = delegate { };
        private static CxPreferencesUI Instance;
        private static ASCAService _ascaService; 

        private CxPreferencesUI()
        {
            InitializeComponent();
        }

        public static CxPreferencesUI GetInstance()
        {
            if (Instance == null)
            {
                Instance = new CxPreferencesUI();
            }

            return Instance;
        }

        public void ThrowEventOnApply()
        {
            OnApplySettingsEvent();
        }

        public void Initialize(CxPreferencesModule preferencesModule)
        {
            cxPreferencesModule = preferencesModule;
            tbApiKey.Text = cxPreferencesModule.ApiKey;
            tbAdditionalParameters.Text = cxPreferencesModule.AdditionalParameters;
            ascaCheckBox.Checked = cxPreferencesModule.AscaCheckBox;
            UpdateAscaUiState();
        }
        private void UpdateAscaUiState()
        {
            label1.Visible = ascaCheckBox.Checked; // Show or hide label based on ASCA state
        }


        private void OnApiKeyChange(object sender, EventArgs e)
        {
            cxPreferencesModule.ApiKey = tbApiKey.Text.Trim();
        }

        private void OnAdditionalParametersChange(object sender, EventArgs e)
        {
            cxPreferencesModule.AdditionalParameters = tbAdditionalParameters.Text;
        }

        private void OnValidateConnection(object sender, EventArgs e)
        {
            lblValidationResult.Text = "Validating...";

            try
            {
                CxCLI.CxWrapper cxWrapper = new CxCLI.CxWrapper(GetCxConfig(), GetType());
                lblValidationResult.Text = cxWrapper.AuthValidate();
            }
            catch (Exception ex)
            {
                lblValidationResult.Text = ex.Message;
            }
        }

        private CxConfig GetCxConfig()
        {
            CxConfig configuration = new CxConfig
            {
                ApiKey = tbApiKey.Text,
                AdditionalParameters = tbAdditionalParameters.Text,
            };

            return configuration;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblValidationResult.Text = string.Empty;
        }

        private void HelpPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://checkmarx.com/resource/documents/en/34965-68738-checkmarx-one-visual-studio-extension--plugin-.html");
        }

        private void AdditionalParametersHelPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://checkmarx.com/resource/documents/en/34965-68626-global-flags.html");
        }



        private async void AscaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                bool isChecked = ascaCheckBox.Checked;  
                cxPreferencesModule.AscaCheckBox = isChecked;
                UpdateAscaUiState();
                if (isChecked)
                {
                    CxCLI.CxWrapper cxWrapper = new CxCLI.CxWrapper(GetCxConfig(), GetType());
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
                lblValidationResult.Text = ex.Message;
            }
        }


    }
}
