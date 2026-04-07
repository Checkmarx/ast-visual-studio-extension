using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    /// <summary>
    /// Maps Checkmarx tenant settings to <see cref="CxOneAssistSettingsModule"/> license flags after authentication.
    /// </summary>
    internal static class AssistEntitlementSync
    {
        internal static void ApplyFromTenant(CxCLI.CxWrapper wrapper, CxOneAssistSettingsModule module)
        {
            if (wrapper == null || module == null)
                return;

            try
            {
                ApplyFromSettings(wrapper.TenantSettings(), module);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssistEntitlementSync: {ex.Message}");
            }
        }

        internal static void ApplyFromSettings(List<TenantSetting> settings, CxOneAssistSettingsModule module)
        {
            if (settings == null || module == null)
                return;

            module.DevAssistLicenseEnabled = IsEntitled(settings, CxCLI.CxConstants.DEV_ASSIST_LICENSE_KEY);
            module.OneAssistLicenseEnabled = IsEntitled(settings, CxCLI.CxConstants.ONE_ASSIST_LICENSE_KEY);
            module.SaveSettingsToStorage();
        }

        /// <summary>Key missing or empty → true. Explicit "true"/"false" parsed; invalid values → true (fail-open).</summary>
        private static bool IsEntitled(List<TenantSetting> settings, string key)
        {
            string value = settings.Find(s => s.Key.Equals(key))?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return true;
            return bool.TryParse(value, out bool result) && result;
        }
    }
}
