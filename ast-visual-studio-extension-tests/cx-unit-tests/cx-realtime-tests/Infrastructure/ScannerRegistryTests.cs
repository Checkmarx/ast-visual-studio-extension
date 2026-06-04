using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Infrastructure
{
    public class ScannerRegistryTests
    {
        [Fact]
        public void ScannerRegistry_All_ContainsFiveScanners()
        {
            var registrations = ScannerRegistry.All;

            Assert.Equal(5, registrations.Count);
        }

        [Fact]
        public void ScannerRegistry_All_ContainsAllScannerNames()
        {
            var registrations = ScannerRegistry.All;
            var names = registrations.Select(r => r.Name).ToList();

            Assert.Contains("ASCA", names);
            Assert.Contains("Secrets", names);
            Assert.Contains("IaC", names);
            Assert.Contains("Containers", names);
            Assert.Contains("OSS", names);
        }

        [Fact]
        public void ScannerRegistry_All_IsReadOnly()
        {
            var registrations = ScannerRegistry.All;

            Assert.True(registrations is System.Collections.Generic.IReadOnlyList<ScannerRegistration>);
        }

        [Fact]
        public void ScannerRegistry_EachRegistration_HasValidName()
        {
            var registrations = ScannerRegistry.All;

            foreach (var registration in registrations)
            {
                Assert.NotNull(registration.Name);
                Assert.NotEmpty(registration.Name);
            }
        }

        [Fact]
        public void ScannerRegistry_EachRegistration_HasValidFactory()
        {
            var registrations = ScannerRegistry.All;

            foreach (var registration in registrations)
            {
                Assert.NotNull(registration.Factory);
            }
        }

        [Fact]
        public void ScannerRegistry_EachRegistration_HasEnabledCheck()
        {
            var registrations = ScannerRegistry.All;

            foreach (var registration in registrations)
            {
                Assert.NotNull(registration.IsEnabled);
            }
        }
    }
}
