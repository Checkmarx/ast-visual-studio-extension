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

        private CxPreferencesUI()
        {
            InitializeComponent();
        }

        public static CxPreferencesUI GetInstance()
        {
            if(Instance == null)
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

            tbServerUrl.Text = cxPreferencesModule.ServerUrl;
            tbAuthUrl.Text = cxPreferencesModule.AuthUrl;
            tbTenantName.Text = cxPreferencesModule.TenantName;
            tbApiKey.Text = cxPreferencesModule.ApiKey;
            tbAdditionalParameters.Text = cxPreferencesModule.AdditionalParameters;
        }

        private void OnServerUrlChange(object sender, EventArgs e)
        {
            cxPreferencesModule.ServerUrl = tbServerUrl.Text.Trim();
        }

        private void OnAuthUrlChange(object sender, EventArgs e)
        {
            cxPreferencesModule.AuthUrl = tbAuthUrl.Text.Trim();
        }

        private void OnTenantNameChange(object sender, EventArgs e)
        {
            cxPreferencesModule.TenantName = tbTenantName.Text.Trim();
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
                BaseUri = tbServerUrl.Text,
                BaseAuthURI = tbAuthUrl.Text,
                Tenant = tbTenantName.Text,
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
            System.Diagnostics.Process.Start("https://checkmarx.atlassian.net/wiki/spaces/AST/pages/6336643630/CxAST+Visual+Studio+Plugin");
        }

        private void AdditionalParametersHelPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://checkmarx.atlassian.net/wiki/spaces/AST/pages/3044541180/Global+Flags");
        }
    }
}
