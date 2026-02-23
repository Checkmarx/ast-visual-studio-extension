using System;
using Xunit;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_tests
{
    /// <summary>
    /// Unit tests for CxAssist error-handling scenarios.
    /// Verifies that TryRun, TryGet, and LogAndSwallow never rethrow and behave correctly.
    /// </summary>
    public class CxAssistErrorHandlerTests
    {
        [Fact]
        public void TryRun_ReturnsTrue_WhenActionSucceeds()
        {
            bool executed = false;
            bool result = CxAssistErrorHandler.TryRun(() => { executed = true; }, "Test");

            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void TryRun_ReturnsFalse_WhenActionThrows()
        {
            bool result = CxAssistErrorHandler.TryRun(() => throw new InvalidOperationException("Test exception"), "Test");

            Assert.False(result);
        }

        [Fact]
        public void TryRun_DoesNotRethrow_WhenActionThrows()
        {
            var ex = Record.Exception(() =>
                CxAssistErrorHandler.TryRun(() => throw new InvalidOperationException("Test"), "Test"));

            Assert.Null(ex);
        }

        [Fact]
        public void TryRun_HandlesNullAction_WithoutThrowing()
        {
            bool result = CxAssistErrorHandler.TryRun(null, "Test");

            Assert.True(result);
        }

        [Fact]
        public void TryGet_ReturnsValue_WhenFunctionSucceeds()
        {
            int value = CxAssistErrorHandler.TryGet(() => 42, "Test", 0);

            Assert.Equal(42, value);
        }

        [Fact]
        public void TryGet_ReturnsDefault_WhenFunctionThrows()
        {
            int value = CxAssistErrorHandler.TryGet<int>(() => throw new InvalidOperationException("Test"), "Test", 99);

            Assert.Equal(99, value);
        }

        [Fact]
        public void TryGet_DoesNotRethrow_WhenFunctionThrows()
        {
            var ex = Record.Exception(() =>
                CxAssistErrorHandler.TryGet<int>(() => throw new InvalidOperationException("Test"), "Test", 0));

            Assert.Null(ex);
        }

        [Fact]
        public void TryGet_ReturnsDefaultT_WhenFunctionIsNull()
        {
            int value = CxAssistErrorHandler.TryGet<int>(null, "Test", 7);

            Assert.Equal(7, value);
        }

        [Fact]
        public void LogAndSwallow_DoesNotThrow_WhenGivenException()
        {
            var ex = Record.Exception(() =>
                CxAssistErrorHandler.LogAndSwallow(new InvalidOperationException("Test"), "TestContext"));

            Assert.Null(ex);
        }

        [Fact]
        public void LogAndSwallow_DoesNotThrow_WhenGivenNull()
        {
            var ex = Record.Exception(() =>
                CxAssistErrorHandler.LogAndSwallow(null, "TestContext"));

            Assert.Null(ex);
        }
    }
}
