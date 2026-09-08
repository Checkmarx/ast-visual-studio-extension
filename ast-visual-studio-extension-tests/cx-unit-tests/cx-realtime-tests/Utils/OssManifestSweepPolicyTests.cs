using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class OssManifestSweepPolicyTests
    {
        // Each test calls ClearSession() first so tests are isolated from each other.

        [Fact]
        public void ShouldScheduleFullManifestSweep_NewSolution_ReturnsTrue()
        {
            OssManifestSweepPolicy.ClearSession();
            Assert.True(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_AfterMarkCompleted_ReturnsFalse()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\MyProject");
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_DifferentSolution_ReturnsTrue()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\ProjectA");
            // ProjectB was never swept — should still return true
            Assert.True(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\ProjectB"));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_NullPath_ReturnsFalse()
        {
            OssManifestSweepPolicy.ClearSession();
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(null));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_EmptyPath_ReturnsFalse()
        {
            OssManifestSweepPolicy.ClearSession();
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(""));
        }

        [Fact]
        public void MarkSweepCompleted_NullPath_DoesNotThrow()
        {
            OssManifestSweepPolicy.ClearSession();
            // Should silently do nothing
            OssManifestSweepPolicy.MarkSweepCompleted(null);
        }

        [Fact]
        public void MarkSweepCompleted_EmptyPath_DoesNotThrow()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted("");
        }

        [Fact]
        public void MarkSweepCompleted_CalledTwiceSamePath_DoesNotThrow()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\MyProject");
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\MyProject"); // idempotent
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }

        [Fact]
        public void ClearSession_AfterMarkCompleted_ResetsState()
        {
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\MyProject");
            OssManifestSweepPolicy.ClearSession();
            // After clear, sweep should be re-scheduled
            Assert.True(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }

        [Fact]
        public void ClearSession_MultipleSolutions_ResetsAll()
        {
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\ProjectA");
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\ProjectB");
            OssManifestSweepPolicy.ClearSession();
            Assert.True(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\ProjectA"));
            Assert.True(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\ProjectB"));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_PathNormalization_TrailingSlashIgnored()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\MyProject\");
            // Without trailing slash should be treated as same path
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }

        [Fact]
        public void ShouldScheduleFullManifestSweep_CaseInsensitive()
        {
            OssManifestSweepPolicy.ClearSession();
            OssManifestSweepPolicy.MarkSweepCompleted(@"C:\myproject");
            Assert.False(OssManifestSweepPolicy.ShouldScheduleFullManifestSweep(@"C:\MyProject"));
        }
    }
}
