using ast_visual_studio_extension.CxPreferences;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxPreferencesModuleTests
    {
        [Fact]
        public void RestoreAuthenticatedSession_DefaultsToFalse()
        {
            var module = new CxPreferencesModule();
            Assert.False(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void RestoreAuthenticatedSession_CanBeSetAndPersisted()
        {
            var module = new CxPreferencesModule();
            module.RestoreAuthenticatedSession = true;
            Assert.True(module.RestoreAuthenticatedSession);
        }
    }
}
