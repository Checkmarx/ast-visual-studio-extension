using ast_visual_studio_extension.CxPreferences;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class CxPreferencesUITests_Async
    {
        [Fact]
        public async Task ValidateApiKeyAsync_WithEmptyApiKey_DoesNotAuthenticate()
        {
            var ui = CxPreferencesUI.GetInstance();

            // Safely get the TextBox control
            var tbApiKeyField = ui.GetType().GetField("tbApiKey", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(tbApiKeyField);
            var tbApiKey = tbApiKeyField.GetValue(ui) as TextBox;
            Assert.NotNull(tbApiKey);

            tbApiKey.Text = string.Empty;

            // Safely get and invoke the validation method
            var method = ui.GetType().GetMethod("ValidateApiKeyAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            await (Task)method.Invoke(ui, new object[] { true });

            // Verify that authentication was not set
            Assert.False(CxPreferencesUI.IsAuthenticated());
        }

        [Fact]
        public async Task TryRestoreAuthenticatedSessionAsync_WithNullPackage_DoesNotThrow()
        {
            // TryRestoreAuthenticatedSessionAsync is internal and should handle null gracefully.
            // Early return on null input (line 310 of CxPreferencesUI.cs).
            await CxPreferencesUI.TryRestoreAuthenticatedSessionAsync(null);

            // If we reach here, the method completed without throwing.
            // That's the expected behavior for null input.
        }
    }
}
