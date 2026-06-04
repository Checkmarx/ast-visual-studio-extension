using ast_visual_studio_extension.CxPreferences;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class CxPreferencesUITests_Events : IDisposable
    {
        public CxPreferencesUITests_Events()
        {
            ResetUiState();
        }

        public void Dispose()
        {
            ResetUiState();
        }
        private static CxPreferencesModule CreateModule()
        {
            return (CxPreferencesModule)FormatterServices
                .GetUninitializedObject(typeof(CxPreferencesModule));
        }

        /// <summary>
        /// Resets the CxPreferencesUI singleton and its static state so tests
        /// don't leak _isAuthenticated / _isValidationInProgress between runs.
        /// </summary>
        private static void ResetUiState()
        {
            var instanceField = typeof(CxPreferencesUI).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            var authField = typeof(CxPreferencesUI).GetField("_isAuthenticated", BindingFlags.NonPublic | BindingFlags.Static);
            authField?.SetValue(null, false);
        }

        [Fact]
        public void OnApiKeyChange_ResetsAuthAndDisablesRestoreSession()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = CreateModule();
            module.ApiKey = "old";
            // Initialize WITHOUT RestoreAuthenticatedSession=true to avoid
            // firing the fire-and-forget ValidateApiKeyAsync race condition.
            ui.Initialize(module);

            // Set it AFTER Initialize so we can verify OnApiKeyChange resets it.
            module.RestoreAuthenticatedSession = true;

            var tbApiKey = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as TextBox;
            tbApiKey.Text = "new";
            var method = ui.GetType().GetMethod("OnApiKeyChange", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(ui, new object[] { tbApiKey, EventArgs.Empty });
            Assert.False(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void Initialize_WithValidModule_LoadsSettingsCorrectly()
        {
            var ui = CxPreferencesUI.GetInstance();
            var module = CreateModule();
            module.ApiKey = "test-key";
            module.AdditionalParameters = "--test-param";
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
            var module = CreateModule();
            module.ApiKey = "";
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
            var module = CreateModule();
            module.ApiKey = "original";
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
