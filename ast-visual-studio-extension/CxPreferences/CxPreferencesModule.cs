using ast_visual_studio_extension.CxCLI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    [Guid("2576da3e-59a6-4462-b3d9-9b4a87b55635")]
    public class CxPreferencesModule : DialogPage
    {
        public string ServerUrl { get; set; }
        public string AuthUrl { get; set; }
        public string TenantName { get; set; }
        public string ApiKey { get; set; }
        public string AdditionalParameters { get; set; }

        public CxPreferencesModule() { }

        private CxPreferencesUI preferencesUI;

        protected override IWin32Window Window
        {
            get
            {
                preferencesUI = new CxPreferencesUI();
                preferencesUI.Initialize(this);

                return preferencesUI;
            }
        }

        /// <summary>
        /// On apply settings
        /// </summary>
        /// <param name="e"></param>
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
        }

        public CxConfig GetCxConfig
        {
            get
            {
                CxConfig configuration = new CxConfig
                {
                    BaseUri = ServerUrl,
                    BaseAuthURI = AuthUrl,
                    Tenant = TenantName,
                    ApiKey = ApiKey,
                    AdditionalParameters = AdditionalParameters,
                };

                return configuration;
            }
        }
    }
}