using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using ast_visual_studio_extension.CxPreferences;
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
                (p, s) => null);

            Assert.Equal("TestScanner", registration.Name);
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_ChecksSettings()
        {
            var settings = CreateMockSettings(ascaEnabled: true);
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (p, s) => null);

            Assert.True(registration.IsEnabled(settings));
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_ReturnsFalseWhenDisabled()
        {
            var settings = CreateMockSettings(ascaEnabled: false);
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (p, s) => null);

            Assert.False(registration.IsEnabled(settings));
        }

        [Fact]
        public void ScannerRegistration_Factory_CanBeInvoked()
        {
            var settings = CreateMockSettings();

            var registration = new ScannerRegistration(
                "ASCA",
                s => true,
                (p, s) => null);

            // null package is acceptable here — factory returns null by design in this test
            var result = registration.Factory(null, settings);
            Assert.Null(result);
        }

        [Fact]
        public void ScannerRegistration_MultipleRegistrations_HaveDifferentNames()
        {
            var reg1 = new ScannerRegistration("ASCA", s => s.AscaCheckBox, (p, s) => null);
            var reg2 = new ScannerRegistration("Secrets", s => s.SecretDetectionRealtimeCheckBox, (p, s) => null);

            Assert.NotEqual(reg1.Name, reg2.Name);
        }

        [Fact]
        public void ScannerRegistration_IsEnabled_WithNullSettings_ThrowsNullReferenceException()
        {
            var registration = new ScannerRegistration(
                "ASCA",
                s => s.AscaCheckBox,
                (p, s) => null);

            // IsEnabled does not null-check settings, so null input will throw
            Assert.Throws<System.NullReferenceException>(() => registration.IsEnabled(null));
        }
    }
}
