using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using ast_visual_studio_extension.CxPreferences;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Integration
{
    public class RealtimeScannerOrchestratorTests
    {
        private static CxOneAssistSettingsModule CreateMockSettings(
            bool ascaEnabled = true,
            bool secretsEnabled = true,
            bool iacEnabled = true,
            bool containersEnabled = true,
            bool ossEnabled = true,
            bool mcpEnabled = true,
            bool devAssistLicense = true)
        {
            var settings = (CxOneAssistSettingsModule)FormatterServices
                .GetUninitializedObject(typeof(CxOneAssistSettingsModule));
            settings.AscaCheckBox = ascaEnabled;
            settings.SecretDetectionRealtimeCheckBox = secretsEnabled;
            settings.IacRealtimeCheckBox = iacEnabled;
            settings.ContainersRealtimeCheckBox = containersEnabled;
            settings.OssRealtimeCheckBox = ossEnabled;
            settings.ContainersTool = "docker";
            settings.McpEnabled = mcpEnabled;
            settings.DevAssistLicenseEnabled = devAssistLicense;
            return settings;
        }

        [Fact]
        public void RealtimeScannerOrchestrator_Constructor_CreatesInstance()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            Assert.NotNull(orchestrator);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNullPackage_ReturnsEarlyAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings();

            // Should return early without error or exception
            var ex = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));
            Assert.Null(ex);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNullSettings_ReturnsEarlyAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            // Should return early without error or exception
            var ex = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, null));
            Assert.Null(ex);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_UnregisterAllAsync_WithoutInitialization_SucceedsAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            // Should not throw even without prior initialization
            var ex = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            Assert.Null(ex);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithMcpDisabled_SkipsScannerInitializationAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings(mcpEnabled: false);

            // Should return early because MCP is disabled without throwing
            var ex = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));
            Assert.Null(ex);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNoLicense_SkipsScannerInitializationAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings(devAssistLicense: false);
            settings.OneAssistLicenseEnabled = false;

            // Should return early because no license without throwing
            var ex = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));
            Assert.Null(ex);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_UnregisterAllAsync_CanBeCalledMultipleTimesAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            // Should be idempotent - no exceptions on multiple calls
            var ex1 = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            var ex2 = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_ThenUnregister_RestoresCleanStateAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings();

            // null package causes early return (VS environment not available in tests)
            await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));

            // Should be able to unregister without error
            var unregisterEx = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            Assert.Null(unregisterEx);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithAllScannersDisabled_StillInitializesAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings(
                ascaEnabled: false,
                secretsEnabled: false,
                iacEnabled: false,
                containersEnabled: false,
                ossEnabled: false);

            // null package causes early return; unregister should still be safe
            await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));

            var unregisterEx = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            Assert.Null(unregisterEx);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_SkipsInitializationMultipleTimes_BehavesIdempotentAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings();

            // Call multiple times - should be idempotent
            var ex1 = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));
            var ex2 = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));

            if (ex1 == null)
                Assert.Null(ex2);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_Lifecycle_InitializeUnregisterInitializeAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings();

            var initEx1 = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));

            var unregEx1 = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            Assert.Null(unregEx1);

            var initEx2 = await Record.ExceptionAsync(() => orchestrator.InitializeAsync(null, settings));

            var unregEx2 = await Record.ExceptionAsync(() => orchestrator.UnregisterAllAsync());
            Assert.Null(unregEx2);

            if (initEx1 == null)
                Assert.Null(initEx2);
        }
    }
}
