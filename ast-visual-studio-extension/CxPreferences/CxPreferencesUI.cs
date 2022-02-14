using ast_visual_studio_extension.CxCli;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxCLI.Models;
using System;
using System.Windows.Forms;
using static ast_visual_studio_extension.CxCLI.CxConfig;

namespace ast_visual_studio_extension.CxPreferences
{
    public partial class CxPreferencesUI : UserControl
    {
        internal CxPreferencesModule cxPreferencesModule;

        public CxPreferencesUI()
        {
            InitializeComponent();
        }

        public void Initialize(CxPreferencesModule preferencesModule)
        {
            cxPreferencesModule = preferencesModule;

            tbServerUrl.Text = cxPreferencesModule.ServerUrl;
            tbAuthUrl.Text = cxPreferencesModule.AuthUrl;
            tbTenantName.Text = cxPreferencesModule.TenantName;
            tbApiKey.Text = cxPreferencesModule.ApiKey;
            tbAdditionalParameters.Text = cxPreferencesModule.AdditionalParameters;
        }

        private void OnServerUrlChange(object sender, EventArgs e)
        {
            cxPreferencesModule.ServerUrl = tbServerUrl.Text;
        }

        private void OnAuthUrlChange(object sender, EventArgs e)
        {
            cxPreferencesModule.AuthUrl = tbAuthUrl.Text;
        }

        private void OnTenantNameChange(object sender, EventArgs e)
        {
            cxPreferencesModule.TenantName = tbTenantName.Text;
        }

        private void OnApiKeyChange(object sender, EventArgs e)
        {
            cxPreferencesModule.ApiKey = tbApiKey.Text;
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
                CxWrapper cxWrapper = new CxWrapper(GetCxConfig());
                lblValidationResult.Text = cxWrapper.AuthValidate();
            }
            //catch (InvalidCLIConfigException ex)
            catch (Exception ex)
            {
                lblValidationResult.Text = ex.Message;
            }
        }

        private void OnLoadSettings(object sender, EventArgs e)
        {
            lblValidationResult.Text = string.Empty; // TODO: check if it is needed
        }

        private CxConfig GetCxConfig()
        {
            CxConfig configuration = new CxConfig
            {
                BaseUri = tbServerUrl.Text,
                BaseAuthURI = tbAuthUrl.Text,
                Tenant = tbTenantName.Text,
                ApiKey = tbApiKey.Text,
                AdditionalParameters = tbAdditionalParameters.Text,
            };

            return configuration;
        }
    }
}
