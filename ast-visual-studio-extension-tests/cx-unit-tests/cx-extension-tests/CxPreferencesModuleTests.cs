using ast_visual_studio_extension.CxPreferences;
using System.Runtime.Serialization;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class CxPreferencesModuleTests
    {
        /// <summary>
        /// Creates a CxPreferencesModule without invoking the DialogPage
        /// base constructor, which requires a running VS Shell environment.
        /// All CxPreferencesModule properties default to CLR defaults (null/false),
        /// which matches the field-initializer defaults in the production class.
        /// </summary>
        private static CxPreferencesModule CreateModule()
        {
            return (CxPreferencesModule)FormatterServices
                .GetUninitializedObject(typeof(CxPreferencesModule));
        }

        [Fact]
        public void RestoreAuthenticatedSession_DefaultsToFalse()
        {
            var module = CreateModule();
            Assert.False(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void RestoreAuthenticatedSession_CanBeSetAndPersisted()
        {
            var module = CreateModule();
            module.RestoreAuthenticatedSession = true;
            Assert.True(module.RestoreAuthenticatedSession);
        }

        [Fact]
        public void ApiKey_DefaultsToNull()
        {
            var module = CreateModule();
            Assert.Null(module.ApiKey);
        }

        [Fact]
        public void ApiKey_CanBeSet()
        {
            var module = CreateModule();
            string testKey = "test-api-key-12345";
            module.ApiKey = testKey;
            Assert.Equal(testKey, module.ApiKey);
        }

        [Fact]
        public void AdditionalParameters_DefaultsToNull()
        {
            var module = CreateModule();
            Assert.Null(module.AdditionalParameters);
        }

        [Fact]
        public void AdditionalParameters_CanBeSet()
        {
            var module = CreateModule();
            string testParams = "--param1 value1 --param2 value2";
            module.AdditionalParameters = testParams;
            Assert.Equal(testParams, module.AdditionalParameters);
        }

        [Fact]
        public void GetCxConfig_ReturnsConfigWithApiKeyAndParameters()
        {
            var module = CreateModule();
            string testKey = "test-key";
            string testParams = "--test";
            module.ApiKey = testKey;
            module.AdditionalParameters = testParams;

            var config = module.GetCxConfig();

            Assert.NotNull(config);
            Assert.Equal(testKey, config.ApiKey);
            Assert.Equal(testParams, config.AdditionalParameters);
        }

        [Fact]
        public void GetCxConfig_WithNullValues_ReturnsConfigWithNullValues()
        {
            var module = CreateModule();
            module.ApiKey = null;
            module.AdditionalParameters = null;

            var config = module.GetCxConfig();

            Assert.NotNull(config);
            Assert.Null(config.ApiKey);
            Assert.Null(config.AdditionalParameters);
        }

        [Fact]
        public void GetCxConfig_WithEmptyStrings_ReturnsConfigWithEmptyStrings()
        {
            var module = CreateModule();
            module.ApiKey = "";
            module.AdditionalParameters = "";

            var config = module.GetCxConfig();

            Assert.NotNull(config);
            Assert.Equal("", config.ApiKey);
            Assert.Equal("", config.AdditionalParameters);
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("very-long-api-key-with-special-chars!@#$%")]
        [InlineData("")]
        [InlineData(null)]
        public void ApiKey_SupportsDifferentFormats(string apiKey)
        {
            var module = CreateModule();
            module.ApiKey = apiKey;
            Assert.Equal(apiKey, module.ApiKey);
        }

        [Theory]
        [InlineData("--scan-type sast")]
        [InlineData("")]
        [InlineData(null)]
        public void AdditionalParameters_SupportsDifferentFormats(string parameters)
        {
            var module = CreateModule();
            module.AdditionalParameters = parameters;
            Assert.Equal(parameters, module.AdditionalParameters);
        }

        [Fact]
        public void MultiplePropertyChanges_ArePersisted()
        {
            var module = CreateModule();

            module.ApiKey = "key1";
            module.AdditionalParameters = "params1";
            module.RestoreAuthenticatedSession = true;

            Assert.Equal("key1", module.ApiKey);
            Assert.Equal("params1", module.AdditionalParameters);
            Assert.True(module.RestoreAuthenticatedSession);

            // Change values
            module.ApiKey = "key2";
            module.AdditionalParameters = "params2";
            module.RestoreAuthenticatedSession = false;

            Assert.Equal("key2", module.ApiKey);
            Assert.Equal("params2", module.AdditionalParameters);
            Assert.False(module.RestoreAuthenticatedSession);
        }
    }
}
