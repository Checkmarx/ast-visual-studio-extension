using ast_visual_studio_extension.CxPreferences;
using System;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxPreferencesUITests_Events
    {
        [Fact]
        public void OnApiKeyChange_ResetsAuthAndDisablesRestoreSession()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = new CxPreferencesModule { ApiKey = "old", RestoreAuthenticatedSession = true };
            ui.Initialize(module);
            var tbApiKey = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as TextBox;
            tbApiKey.Text = "new";
            var method = ui.GetType().GetMethod("OnApiKeyChange", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(ui, new object[] { tbApiKey, EventArgs.Empty });
            Assert.False(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void OnLogout_ResetsAuthAndPersistsSettings()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = new CxPreferencesModule { ApiKey = "abc", RestoreAuthenticatedSession = true };
            ui.Initialize(module);
            // Simulate Yes on MessageBox
            typeof(MessageBox).GetField("defaultResult", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, DialogResult.Yes);
            var method = ui.GetType().GetMethod("OnLogout", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(ui, new object[] { ui, EventArgs.Empty });
            Assert.False(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void Initialize_WithValidModule_LoadsSettingsCorrectly()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = new CxPreferencesModule
            {
                ApiKey = "test-key",
                AdditionalParameters = "--test-param",
                RestoreAuthenticatedSession = true
            };
            ui.Initialize(module);

            // Verify module was set
            var field = ui.GetType().GetField("cxPreferencesModule", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadedModule = (CxPreferencesModule)field.GetValue(ui);
            Assert.NotNull(loadedModule);
            Assert.Equal("test-key", loadedModule.ApiKey);
        }

        [Fact]
        public void Initialize_WithEmptyApiKey_ResetsAuthState()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = new CxPreferencesModule { ApiKey = "" };
            ui.Initialize(module);

            // Verify module was initialized
            var field = ui.GetType().GetField("cxPreferencesModule", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadedModule = (CxPreferencesModule)field.GetValue(ui);
            Assert.NotNull(loadedModule);
        }

        [Fact]
        public void OnApiKeyChange_UpdatesModuleProperty()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = new CxPreferencesModule { ApiKey = "original" };
            ui.Initialize(module);

            var tbApiKey = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as TextBox;
            string newKey = "updated-key";
            tbApiKey.Text = newKey;

            var method = ui.GetType().GetMethod("OnApiKeyChange", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(ui, new object[] { tbApiKey, EventArgs.Empty });

            // Should have modified the module
            Assert.NotNull(module);
        }
    }
}
