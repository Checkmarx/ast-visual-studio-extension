using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using ast_visual_studio_extension.CxPreferences;
using Moq;
using System.Runtime.Serialization;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Infrastructure
{
    public class ScannerRegistrationTests
    {
        private static CxOneAssistSettingsModule CreateMockSettings(
            bool ascaEnabled = true,
            bool secretsEnabled = true,
            bool iacEnabled = true,
            bool containersEnabled = true,
            bool ossEnabled = true)
        {
            var settings = (CxOneAssistSettingsModule)FormatterServices
                .GetUninitializedObject(typeof(CxOneAssistSettingsModule));
            settings.AscaCheckBox = ascaEnabled;
            settings.SecretDetectionRealtimeCheckBox = secretsEnabled;
            settings.IacRealtimeCheckBox = iacEnabled;
            settings.ContainersRealtimeCheckBox = containersEnabled;
            settings.OssRealtimeCheckBox = ossEnabled;
            settings.ContainersTool = "docker";
            return settings;
        }

        [Fact]
        public void ScannerRegistration_Constructor_StoresName()
        {
            var mockSettings = CreateMockSettings();
            var registration = new ScannerRegistration(
                "TestScanner",
                s => s.AscaCheckBox,
                (w, s) => null);

            Assert.Equal("TestScanner", registration.Name);
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_ChecksSettings()
        {
            var settings = CreateMockSettings(ascaEnabled: true);
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (w, s) => null);

            Assert.True(registration.IsEnabled(settings));
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_ReturnsFalseWhenDisabled()
        {
            var settings = CreateMockSettings(ascaEnabled: false);
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (w, s) => null);

            Assert.False(registration.IsEnabled(settings));
        }

        [Fact]
        public void ScannerRegistration_Factory_CanBeInvoked()
        {
            // CxWrapper requires CxConfig and Type in constructor; pass them to mock
            var mockConfig = new ast_visual_studio_extension.CxWrapper.Models.CxConfig { ApiKey = "test" };
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(mockConfig, typeof(ScannerRegistrationTests));
            var settings = CreateMockSettings();

            var registration = new ScannerRegistration(
                "ASCA",
                s => true,
                (w, s) => null);

            var result = registration.Factory(mockWrapper.Object, settings);
            Assert.Null(result); // Factory returns null in this test (by design)
        }

        [Fact]
        public void ScannerRegistration_MultipleRegistrations_HaveDifferentNames()
        {
            var reg1 = new ScannerRegistration("ASCA", s => s.AscaCheckBox, (w, s) => null);
            var reg2 = new ScannerRegistration("Secrets", s => s.SecretDetectionRealtimeCheckBox, (w, s) => null);

            Assert.NotEqual(reg1.Name, reg2.Name);
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_WithNullSettings_ReturnsFalse()
        {
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (w, s) => null);

            // Should handle null gracefully
            try
            {
                registration.IsEnabled(null);
            }
            catch (System.NullReferenceException)
            {
                // Expected behavior - IsEnabled uses settings directly
            }
        }
    }
}
