using ast_visual_studio_extension.CxWrapper.Models;
using ast_visual_studio_extension.CxCLI;
using System;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_unit_test
{
    public class CxConfigTests
    {
        [Fact]
        public void ToArguments_WithValidApiKey_ReturnsArgumentsList()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key",
                AdditionalParameters = null
            };

            var arguments = config.ToArguments();

            Assert.Contains(CxConstants.FLAG_API_KEY, arguments);
            Assert.Contains("test-api-key", arguments);
        }

        [Fact]
        public void ToArguments_WithAdditionalParameters_ReturnsArgumentsList()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key",
                AdditionalParameters = "--param1 value1 --param2 value2"
            };

            var arguments = config.ToArguments();

            Assert.Contains(CxConstants.FLAG_API_KEY, arguments);
            Assert.Contains("test-api-key", arguments);
            Assert.Contains("--param1", arguments);
            Assert.Contains("value1", arguments);
            Assert.Contains("--param2", arguments);
            Assert.Contains("value2", arguments);
        }

        [Fact]
        public void ToArguments_WithNullApiKey_ReturnsEmptyList()
        {
            var config = new CxConfig
            {
                ApiKey = null,
                AdditionalParameters = null
            };

            var arguments = config.ToArguments();

            Assert.Empty(arguments);
        }

        [Fact]
        public void Validate_WithNullApiKey_ThrowsInvalidCLIConfigException()
        {
            var config = new CxConfig
            {
                ApiKey = null
            };

            var exception = Assert.Throws<CxConfig.InvalidCLIConfigException>(() => config.Validate());
            Assert.Equal(CxConstants.EXCEPTION_CREDENTIALS_NOT_SET, exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyApiKey_ThrowsInvalidCLIConfigException()
        {
            var config = new CxConfig
            {
                ApiKey = string.Empty
            };

            var exception = Assert.Throws<CxConfig.InvalidCLIConfigException>(() => config.Validate());
            Assert.Equal(CxConstants.EXCEPTION_CREDENTIALS_NOT_SET, exception.Message);
        }

        [Fact]
        public void Validate_WithValidApiKey_DoesNotThrowException()
        {
            var config = new CxConfig
            {
                ApiKey = "test-api-key"
            };

            config.Validate();
        }
    }
}
