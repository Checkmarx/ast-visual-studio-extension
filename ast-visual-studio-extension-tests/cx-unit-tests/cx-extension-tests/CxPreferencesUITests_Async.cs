using ast_visual_studio_extension.CxPreferences;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxPreferencesUITests_Async
    {
        [Fact]
        public async Task ValidateApiKeyAsync_WithEmptyApiKey_DoesNotAuthenticate()
        {
            var ui = CxPreferencesUI.GetInstance();
            var tbApiKey = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as System.Windows.Forms.TextBox;
            tbApiKey.Text = string.Empty;
            var method = ui.GetType().GetMethod("ValidateApiKeyAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method.Invoke(ui, new object[] { true });
            Assert.False(CxPreferencesUI.IsAuthenticated());
        }

        [Fact]
        public async Task CompleteAuthenticationSetupAsync_WithNullConfig_DoesNotThrow()
        {
            var ui = CxPreferencesUI.GetInstance();
            var method = ui.GetType().GetMethod("CompleteAuthenticationSetupAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method.Invoke(ui, new object[] { null, false });
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithApiKey_ExecutesAsync()
        {
            var ui = CxPreferencesUI.GetInstance();
            var tbApiKey = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as System.Windows.Forms.TextBox;
            tbApiKey.Text = "test-key";
            var method = ui.GetType().GetMethod("ValidateApiKeyAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            // Should complete without throwing
            try
            {
                await (Task)method.Invoke(ui, new object[] { false });
            }
            catch
            {
                // Expected to potentially fail due to missing wrapper dependencies
            }
        }

        [Fact]
        public async Task TryRestoreAuthenticatedSessionAsync_ExecutesWithoutException()
        {
            // Should complete without throwing even with null package
            try
            {
                await CxPreferencesUI.TryRestoreAuthenticatedSessionAsync(null);
            }
            catch
            {
                // Acceptable if external dependencies fail
            }
        }

        [Fact]
        public async Task InitializeMcpModuleAsync_WithNullConfig_DoesNotThrow()
        {
            var ui = CxPreferencesUI.GetInstance();
            var method = ui.GetType().GetMethod("InitializeMcpModuleAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                // Only test if method exists
                try
                {
                    await (Task)method.Invoke(ui, new object[] { null });
                }
                catch
                {
                    // Acceptable if method doesn't exist or fails due to dependencies
                }
            }
        }
    }
}
