using ast_visual_studio_extension.CxPreferences;
using System.Threading.Tasks;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class CxPreferencesUITests_Auth
    {
        [Fact]
        public void SetAuthState_UpdatesIsAuthenticated()
        {
            // Arrange
            var ui = CxPreferencesUI.GetInstance();
            var isAuthenticated = typeof(CxPreferencesUI).GetField("_isAuthenticated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            isAuthenticated.SetValue(null, false);

            // Act
            var setAuthState = typeof(CxPreferencesUI).GetMethod("SetAuthState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            setAuthState.Invoke(null, new object[] { true });

            // Assert
            Assert.True(CxPreferencesUI.IsAuthenticated());
        }

        [Fact]
        public async Task TryRestoreAuthenticatedSessionAsync_WithNullPackage_DoesNotThrow()
        {
            await CxPreferencesUI.TryRestoreAuthenticatedSessionAsync(null);
        }
    }
}
