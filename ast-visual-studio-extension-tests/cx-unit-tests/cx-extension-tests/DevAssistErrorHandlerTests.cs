using System;
using Xunit;
using ast_visual_studio_extension.CxExtension.DevAssist.Core;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for DevAssist error-handling scenarios.
    /// Verifies that TryRun, TryGet, and LogAndSwallow never rethrow and behave correctly.
    /// </summary>
    public class DevAssistErrorHandlerTests
    {
        [Fact]
        public void TryRun_ReturnsTrue_WhenActionSucceeds()
        {
            bool executed = false;
            bool result = DevAssistErrorHandler.TryRun(() => { executed = true; }, "Test");

            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void TryRun_ReturnsFalse_WhenActionThrows()
        {
            bool result = DevAssistErrorHandler.TryRun(() => throw new InvalidOperationException("Test exception"), "Test");

            Assert.False(result);
        }

        [Fact]
        public void TryRun_DoesNotRethrow_WhenActionThrows()
        {
            var ex = Record.Exception(() =>
                DevAssistErrorHandler.TryRun(() => throw new InvalidOperationException("Test"), "Test"));

            Assert.Null(ex);
        }

        [Fact]
        public void TryRun_HandlesNullAction_WithoutThrowing()
        {
            bool result = DevAssistErrorHandler.TryRun(null, "Test");

            Assert.True(result);
        }

        [Fact]
        public void TryGet_ReturnsValue_WhenFunctionSucceeds()
        {
            int value = DevAssistErrorHandler.TryGet(() => 42, "Test", 0);

            Assert.Equal(42, value);
        }

        [Fact]
        public void TryGet_ReturnsDefault_WhenFunctionThrows()
        {
            int value = DevAssistErrorHandler.TryGet<int>(() => throw new InvalidOperationException("Test"), "Test", 99);

            Assert.Equal(99, value);
        }

        [Fact]
        public void TryGet_DoesNotRethrow_WhenFunctionThrows()
        {
            var ex = Record.Exception(() =>
                DevAssistErrorHandler.TryGet<int>(() => throw new InvalidOperationException("Test"), "Test", 0));

            Assert.Null(ex);
        }

        [Fact]
        public void TryGet_ReturnsDefaultT_WhenFunctionIsNull()
        {
            int value = DevAssistErrorHandler.TryGet<int>(null, "Test", 7);

            Assert.Equal(7, value);
        }

        [Fact]
        public void LogAndSwallow_DoesNotThrow_WhenGivenException()
        {
            var ex = Record.Exception(() =>
                DevAssistErrorHandler.LogAndSwallow(new InvalidOperationException("Test"), "TestContext"));

            Assert.Null(ex);
        }

        [Fact]
        public void LogAndSwallow_DoesNotThrow_WhenGivenNull()
        {
            var ex = Record.Exception(() =>
                DevAssistErrorHandler.LogAndSwallow(null, "TestContext"));

            Assert.Null(ex);
        }
    }
}
