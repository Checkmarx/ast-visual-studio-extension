using ast_visual_studio_extension.CxPreferences.Configuration;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extension_test
{
    public class McpOAuthSignInTests
    {
        [Fact]
        public void TryBuildArguments_RunsPinnedClientWithTheEntryArguments()
        {
            // Same URL + headers as the .mcp.json entry, so mcp-remote-client writes the cache the VS bridge reads.
            const string url = "https://ast-master-components.dev.cxast.net/api/security-mcp/mcp/master-sypher";

            Assert.True(McpOAuthSignIn.TryBuildArguments(url, out string arguments));
            Assert.Equal(
                "/d /s /c \"npx -y -p mcp-remote@0.14.3 mcp-remote-client " + url + " --transport http-first --header cx-origin:VisualStudio\"",
                arguments);
        }

        [Theory]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg & calc")]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg\"&calc")]
        [InlineData("https://eu.ast.checkmarx.net/api/security-mcp/mcp/cx|seg")]
        [InlineData("http://eu.ast.checkmarx.net/api/security-mcp/mcp/cx_seg")]
        [InlineData("")]
        [InlineData(null)]
        public void TryBuildArguments_RejectsUnsafeUrls(string url)
        {
            Assert.False(McpOAuthSignIn.TryBuildArguments(url, out string arguments));
            Assert.Null(arguments);
        }
    }
}
