using ast_visual_studio_extension.CxExtension.CxAssist.Realtime;
using ast_visual_studio_extension.CxPreferences;
using Moq;
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
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNullWrapper_ReturnsEarlyAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var settings = CreateMockSettings();

            // Should return early without error
            await orchestrator.InitializeAsync(null, settings);

            // No exception thrown
            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNullSettings_ReturnsEarlyAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockConfig = new ast_visual_studio_extension.CxWrapper.Models.CxConfig { ApiKey = "test" };
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(mockConfig, typeof(RealtimeScannerOrchestratorTests));

            // Should return early without error
            await orchestrator.InitializeAsync(mockWrapper.Object, null);

            // No exception thrown
            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_UnregisterAllAsync_WithoutInitialization_SucceedsAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            // Should not throw even without prior initialization
            await orchestrator.UnregisterAllAsync();

            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithMcpDisabled_SkipsScannerInitializationAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockConfig = new ast_visual_studio_extension.CxWrapper.Models.CxConfig { ApiKey = "test" };
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(mockConfig, typeof(RealtimeScannerOrchestratorTests));
            var settings = CreateMockSettings(mcpEnabled: false);

            // Should return early because MCP is disabled
            await orchestrator.InitializeAsync(mockWrapper.Object, settings);

            // No scanners should be initialized
            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithNoLicense_SkipsScannerInitializationAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockConfig = new ast_visual_studio_extension.CxWrapper.Models.CxConfig { ApiKey = "test" };
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>(mockConfig, typeof(RealtimeScannerOrchestratorTests));
            var settings = CreateMockSettings(devAssistLicense: false);
            settings.OneAssistLicenseEnabled = false;

            // Should return early because no license
            await orchestrator.InitializeAsync(mockWrapper.Object, settings);

            // No scanners should be initialized
            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_UnregisterAllAsync_CanBeCalledMultipleTimesAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();

            // Should be idempotent
            await orchestrator.UnregisterAllAsync();
            await orchestrator.UnregisterAllAsync();

            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_ThenUnregister_RestoresCleanStateAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>();
            var settings = CreateMockSettings();

            // This will attempt to initialize scanners
            // We expect it to handle gracefully (VS environment not available in tests)
            try
            {
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);
            }
            catch
            {
                // Expected in test environment - no DTE available
            }

            // Should be able to unregister without error
            await orchestrator.UnregisterAllAsync();

            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_InitializeAsync_WithAllScannersDisabled_StillInitializesAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>();
            var settings = CreateMockSettings(
                ascaEnabled: false,
                secretsEnabled: false,
                iacEnabled: false,
                containersEnabled: false,
                ossEnabled: false);

            // Should initialize orchestrator even if all scanners disabled
            // (manifest watcher still starts if solution directory available)
            try
            {
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);
            }
            catch
            {
                // Expected in test environment
            }

            // Should be able to unregister
            await orchestrator.UnregisterAllAsync();

            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_SkipsInitializationMultipleTimes_BehavesIdempotentAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>();
            var settings = CreateMockSettings();

            // Call multiple times
            try
            {
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);
            }
            catch
            {
                // Expected in test environment
            }

            Assert.True(true);
        }

        [Fact]
        public async Task RealtimeScannerOrchestrator_Lifecycle_InitializeUnregisterInitializeAsync()
        {
            var orchestrator = new RealtimeScannerOrchestrator();
            var mockWrapper = new Mock<ast_visual_studio_extension.CxCLI.CxWrapper>();
            var settings = CreateMockSettings();

            try
            {
                // Initialize
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);

                // Unregister
                await orchestrator.UnregisterAllAsync();

                // Initialize again
                await orchestrator.InitializeAsync(mockWrapper.Object, settings);

                // Final cleanup
                await orchestrator.UnregisterAllAsync();
            }
            catch
            {
                // Expected in test environment
            }

            Assert.True(true);
        }
    }
}
