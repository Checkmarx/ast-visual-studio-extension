using System.Collections.Generic;
using Xunit;
using ast_visual_studio_extension.CxCLI;

namespace ast_visual_studio_extension.Tests
{
    public class CxUtilsTests
    {
        [Theory]
        [InlineData("", new string[] { })]
        [InlineData("param1 param2", new string[] { "param1", "param2" })]
        [InlineData("\"param with spaces\" 'another param'", new string[] { "\"param with spaces\"", "\"another param\"" })]
        [InlineData("param1 \"param with spaces\" param2", new string[] { "param1", "\"param with spaces\"", "param2" })]
        [InlineData("param1 'param with spaces' param2", new string[] { "param1", "\"param with spaces\"", "param2" })]
        public void ParseAdditionalParameters_ValidInput_ReturnsExpectedList(string input, string[] expected)
        {
            List<string> result = CxUtils.ParseAdditionalParameters(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseAdditionalParameters_NullInput_ReturnsEmptyList()
        {
            List<string> result = CxUtils.ParseAdditionalParameters(null);

            Assert.Empty(result);
        }
    }
}
