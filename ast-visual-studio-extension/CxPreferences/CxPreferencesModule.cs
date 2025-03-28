﻿using ast_visual_studio_extension.CxWrapper.Models;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    [Guid("2576da3e-59a6-4462-b3d9-9b4a87b55635")]
    public class CxPreferencesModule : DialogPage
    {
        public string ApiKey { get; set; }
        public string AdditionalParameters { get; set; }
        public bool AscaCheckBox { get; set; } = true;

        protected override IWin32Window Window
        {
            get
            {
                CxPreferencesUI preferencesUI = CxPreferencesUI.GetInstance();
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
            CxPreferencesUI.GetInstance().ThrowEventOnApply();
        }

        /// <summary>
        /// Get Checkmarx configuration
        /// </summary>
        /// <returns></returns>
        public CxConfig GetCxConfig()
        {
            CxConfig configuration = new CxConfig
            {
                ApiKey = ApiKey,
                AdditionalParameters = AdditionalParameters,
                AscaEnabled = AscaCheckBox // Add the ASCA setting to configuration
            };
            return configuration;
        }

        /// <summary>
        /// Checks if ASCA scanning is enabled in preferences
        /// </summary>
        /// <returns>True if ASCA is enabled, false otherwise</returns>
        public bool IsAscaEnabled()
        {
            return AscaCheckBox;
        }
    }
}