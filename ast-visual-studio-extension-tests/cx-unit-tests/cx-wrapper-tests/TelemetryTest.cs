using ast_visual_studio_extension.CxCLI;
using System;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class TelemetryTest : BaseTest
    {
        /// <summary>
        /// Test that TelemetryAIEvent executes without throwing an exception.
        /// Similar to JavaWrapper's TelemetryTest pattern - fire-and-forget, response not validated.
        /// </summary>
        [Fact]
        public void TestTelemetryAIEvent_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.TelemetryAIEvent(
                    aiProvider: "Copilot",
                    eventType: "click",
                    subType: "test-event",
                    engine: "sast",
                    problemSeverity: "high",
                    scanType: "",
                    status: "",
                    totalCount: 0
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test that TelemetryAIEvent handles null parameters gracefully.
        /// </summary>
        [Fact]
        public void TestTelemetryAIEvent_WithNullParameters_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.TelemetryAIEvent(
                    aiProvider: null,
                    eventType: null,
                    subType: null,
                    engine: null,
                    problemSeverity: null,
                    scanType: null,
                    status: null,
                    totalCount: 0
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test LogUserEventTelemetry convenience method.
        /// </summary>
        [Fact]
        public void TestLogUserEventTelemetry_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.LogUserEventTelemetry(
                    eventType: "click",
                    subType: "fixWithAIChat",
                    engine: "Secrets",
                    problemSeverity: "High"
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test LogDetectionTelemetry convenience method with valid count.
        /// </summary>
        [Fact]
        public void TestLogDetectionTelemetry_WithValidCount_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.LogDetectionTelemetry(
                    scanType: "Oss",
                    status: "High",
                    totalCount: 5
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test LogDetectionTelemetry with zero count - should skip telemetry call.
        /// </summary>
        [Fact]
        public void TestLogDetectionTelemetry_WithZeroCount_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.LogDetectionTelemetry(
                    scanType: "Oss",
                    status: "High",
                    totalCount: 0
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test LogDetectionTelemetry with negative count - should skip telemetry call.
        /// </summary>
        [Fact]
        public void TestLogDetectionTelemetry_WithNegativeCount_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.LogDetectionTelemetry(
                    scanType: "Oss",
                    status: "High",
                    totalCount: -1
                );
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Test fire-and-forget telemetry method does not throw.
        /// </summary>
        [Fact]
        public void TestTelemetryAIEventFireAndForget_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                cxWrapper.TelemetryAIEventFireAndForget(
                    aiProvider: "Copilot",
                    eventType: "click",
                    subType: "test-event",
                    engine: "sast",
                    problemSeverity: "high",
                    scanType: "",
                    status: "",
                    totalCount: 0
                );
            });

            Assert.Null(exception);
        }
    }
}

