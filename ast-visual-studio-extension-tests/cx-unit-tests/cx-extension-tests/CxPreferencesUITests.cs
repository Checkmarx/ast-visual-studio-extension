using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class CxPreferencesUITests
    {
        private static CxPreferencesModule CreateModule()
        {
            return (CxPreferencesModule)FormatterServices
                .GetUninitializedObject(typeof(CxPreferencesModule));
        }

        [Fact]
        public void AuthMessages_ShouldMatchCentralizedConstants()
        {
            Assert.Equal("Validating...", CxConstants.AUTH_VALIDATE_IN_PROGRESS);
            Assert.Equal("You are connected to Checkmarx One", CxConstants.AUTH_VALIDATE_SUCCESS);
            Assert.Equal("Failed authentication: {0}", CxConstants.AUTH_VALIDATE_FAIL_TEMPLATE);
            Assert.Equal("Error in authentication", CxConstants.AUTH_VALIDATE_ERROR);
            Assert.Equal("You have successfully logged out", CxConstants.AUTH_LOGOUT_SUCCESS);
        }

        [Fact]
        public void Initialize_WithEmptyApiKey_ShouldDisableConnectAndLogout()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                ResetPreferencesUiState();

                var ui = CxPreferencesUI.GetInstance();
                var module = CreateModule();
                module.ApiKey = string.Empty;
                module.AdditionalParameters = string.Empty;

                ui.Initialize(module);

                var connectButton = GetPrivateField<Button>(ui, "button1");
                var logoutButton = GetPrivateField<Button>(ui, "btnLogout");

                Assert.False(connectButton.Enabled);
                Assert.False(logoutButton.Enabled);
            });
        }

        [Fact]
        public void Initialize_WithApiKey_ShouldEnableConnect()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                ResetPreferencesUiState();

                var ui = CxPreferencesUI.GetInstance();
                var module = CreateModule();
                module.ApiKey = "abc123";
                module.AdditionalParameters = string.Empty;

                ui.Initialize(module);

                var connectButton = GetPrivateField<Button>(ui, "button1");
                var logoutButton = GetPrivateField<Button>(ui, "btnLogout");

                // Connect enabled when API key present and not yet authenticated
                Assert.True(connectButton.Enabled);
                // Logout disabled because user is not authenticated in test environment
                Assert.False(logoutButton.Enabled);
            });
        }

        [Fact]
        public void OnApiKeyChange_ShouldTrimAndPersistApiKey_AndEnableButtons()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                ResetPreferencesUiState();

                var ui = CxPreferencesUI.GetInstance();
                var module = CreateModule();
                module.ApiKey = string.Empty;
                module.AdditionalParameters = string.Empty;

                ui.Initialize(module);

                var apiKeyTextBox = GetPrivateField<TextBox>(ui, "tbApiKey");
                apiKeyTextBox.Text = "  test-key  ";

                InvokePrivateMethod(ui, "OnApiKeyChange", ui, EventArgs.Empty);

                var connectButton = GetPrivateField<Button>(ui, "button1");

                Assert.Equal("test-key", module.ApiKey);
                Assert.True(connectButton.Enabled);
            });
        }

        /// <summary>
        /// Resets the CxPreferencesUI singleton and all static state so tests
        /// don't leak _isAuthenticated / _isValidationInProgress between runs.
        /// </summary>
        private static void ResetPreferencesUiState()
        {
            var instanceField = typeof(CxPreferencesUI).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            var authField = typeof(CxPreferencesUI).GetField("_isAuthenticated", BindingFlags.NonPublic | BindingFlags.Static);
            authField?.SetValue(null, false);
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            var value = field!.GetValue(target) as T;
            Assert.NotNull(value);
            return value!;
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(target, args);
        }
    }
}
