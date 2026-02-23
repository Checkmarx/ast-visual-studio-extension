using ast_visual_studio_extension.CxCLI;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    /// <summary>
    /// Unit tests for telemetry-related constants.
    /// These tests verify that the constants are correctly defined without requiring CLI execution.
    /// </summary>
    public class TelemetryConstantsTests
    {
        [Fact]
        public void CLI_TELEMETRY_CMD_HasCorrectValue()
        {
            Assert.Equal("telemetry", CxConstants.CLI_TELEMETRY_CMD);
        }

        [Fact]
        public void CLI_TELEMETRY_AI_CMD_HasCorrectValue()
        {
            Assert.Equal("ai", CxConstants.CLI_TELEMETRY_AI_CMD);
        }

        [Fact]
        public void FLAG_AI_PROVIDER_HasCorrectValue()
        {
            Assert.Equal("--ai-provider", CxConstants.FLAG_AI_PROVIDER);
        }

        [Fact]
        public void FLAG_TYPE_HasCorrectValue()
        {
            Assert.Equal("--type", CxConstants.FLAG_TYPE);
        }

        [Fact]
        public void FLAG_SUB_TYPE_HasCorrectValue()
        {
            Assert.Equal("--sub-type", CxConstants.FLAG_SUB_TYPE);
        }

        [Fact]
        public void FLAG_PROBLEM_SEVERITY_HasCorrectValue()
        {
            Assert.Equal("--problem-severity", CxConstants.FLAG_PROBLEM_SEVERITY);
        }

        [Fact]
        public void FLAG_STATUS_HasCorrectValue()
        {
            Assert.Equal("--status", CxConstants.FLAG_STATUS);
        }

        [Fact]
        public void FLAG_TOTAL_COUNT_HasCorrectValue()
        {
            Assert.Equal("--total-count", CxConstants.FLAG_TOTAL_COUNT);
        }

        [Fact]
        public void LOG_RUNNING_TELEMETRY_AI_CMD_HasCorrectFormat()
        {
            string logMessage = CxConstants.LOG_RUNNING_TELEMETRY_AI_CMD;
            
            Assert.Contains("{0}", logMessage);
            Assert.Contains("{1}", logMessage);
            Assert.Contains("{2}", logMessage);
            Assert.Contains("telemetry", logMessage.ToLower());
        }

        [Fact]
        public void LOG_RUNNING_TELEMETRY_AI_CMD_CanBeFormatted()
        {
            string formatted = string.Format(
                CxConstants.LOG_RUNNING_TELEMETRY_AI_CMD,
                "Copilot",
                "click",
                "testSubType"
            );

            Assert.Contains("Copilot", formatted);
            Assert.Contains("click", formatted);
            Assert.Contains("testSubType", formatted);
        }

        [Fact]
        public void EXTENSION_AGENT_HasCorrectValue()
        {
            Assert.Equal("Visual Studio", CxConstants.EXTENSION_AGENT);
        }

        [Fact]
        public void FLAG_AGENT_HasCorrectValue()
        {
            Assert.Equal("--agent", CxConstants.FLAG_AGENT);
        }
    }
}

