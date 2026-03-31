using ast_visual_studio_extension.CxPreferences;
using System.Runtime.Serialization;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxOneAssistSettingsModuleTests
    {
        /// <summary>
        /// Creates a CxOneAssistSettingsModule without invoking the DialogPage
        /// base constructor, which requires a running VS Shell environment.
        /// Field-initializer defaults are applied manually.
        /// </summary>
        private static CxOneAssistSettingsModule CreateModule()
        {
            var module = (CxOneAssistSettingsModule)FormatterServices
                .GetUninitializedObject(typeof(CxOneAssistSettingsModule));
            module.AscaCheckBox = true;
            module.OssRealtimeCheckBox = true;
            module.SecretDetectionRealtimeCheckBox = true;
            module.ContainersRealtimeCheckBox = true;
            module.IacRealtimeCheckBox = true;
            module.ContainersTool = "docker";
            module.UserPrefAscaRealtime = true;
            module.UserPrefOssRealtime = true;
            module.UserPrefSecretDetectionRealtime = true;
            module.UserPrefContainersRealtime = true;
            module.UserPrefIacRealtime = true;
            return module;
        }

        [Fact]
        public void EnableAllRealtimeScanners_ShouldEnableAll()
        {
            var module = CreateModule();
            module.EnableAllRealtimeScanners();
            Assert.True(module.AscaCheckBox);
            Assert.True(module.OssRealtimeCheckBox);
            Assert.True(module.SecretDetectionRealtimeCheckBox);
            Assert.True(module.ContainersRealtimeCheckBox);
            Assert.True(module.IacRealtimeCheckBox);
        }

        [Fact]
        public void DisableAllRealtimeScanners_ShouldDisableAll()
        {
            var module = CreateModule();
            module.DisableAllRealtimeScanners();
            Assert.False(module.AscaCheckBox);
            Assert.False(module.OssRealtimeCheckBox);
            Assert.False(module.SecretDetectionRealtimeCheckBox);
            Assert.False(module.ContainersRealtimeCheckBox);
            Assert.False(module.IacRealtimeCheckBox);
        }

        [Fact]
        public void SaveAndApplyUserPreferences_ShouldRestoreSettings()
        {
            var module = CreateModule();
            module.AscaCheckBox = false;
            module.OssRealtimeCheckBox = true;
            module.SecretDetectionRealtimeCheckBox = false;
            module.ContainersRealtimeCheckBox = true;
            module.IacRealtimeCheckBox = false;
            module.SaveCurrentSettingsAsUserPreferences();

            // Change values
            module.EnableAllRealtimeScanners();
            module.ApplyUserPreferencesToRealtimeSettings();

            Assert.False(module.AscaCheckBox);
            Assert.True(module.OssRealtimeCheckBox);
            Assert.False(module.SecretDetectionRealtimeCheckBox);
            Assert.True(module.ContainersRealtimeCheckBox);
            Assert.False(module.IacRealtimeCheckBox);
        }

        [Fact]
        public void AutoEnableRealtimeScanners_ShouldRespectUserPreferences()
        {
            var module = CreateModule();
            module.AscaCheckBox = false;
            module.OssRealtimeCheckBox = false;
            module.SecretDetectionRealtimeCheckBox = false;
            module.ContainersRealtimeCheckBox = false;
            module.IacRealtimeCheckBox = false;
            module.SaveCurrentSettingsAsUserPreferences();

            // Should restore user preferences
            module.AutoEnableRealtimeScanners();
            Assert.False(module.AscaCheckBox);
            Assert.False(module.OssRealtimeCheckBox);
            Assert.False(module.SecretDetectionRealtimeCheckBox);
            Assert.False(module.ContainersRealtimeCheckBox);
            Assert.False(module.IacRealtimeCheckBox);
        }

        [Fact]
        public void DisableRealtimeScannersPreservingPreferences_ShouldDisableAllAndPreserve()
        {
            var module = CreateModule();
            module.AscaCheckBox = true;
            module.OssRealtimeCheckBox = false;
            module.SecretDetectionRealtimeCheckBox = true;
            module.ContainersRealtimeCheckBox = false;
            module.IacRealtimeCheckBox = true;
            module.DisableRealtimeScannersPreservingPreferences();
            Assert.False(module.AscaCheckBox);
            Assert.False(module.OssRealtimeCheckBox);
            Assert.False(module.SecretDetectionRealtimeCheckBox);
            Assert.False(module.ContainersRealtimeCheckBox);
            Assert.False(module.IacRealtimeCheckBox);
            // Restore
            module.ApplyUserPreferencesToRealtimeSettings();
            Assert.True(module.AscaCheckBox);
            Assert.False(module.OssRealtimeCheckBox);
            Assert.True(module.SecretDetectionRealtimeCheckBox);
            Assert.False(module.ContainersRealtimeCheckBox);
            Assert.True(module.IacRealtimeCheckBox);
        }
    }
}
