using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class TenantSettingTest : BaseTest
    {
        [Fact]
        public void TestTenantSetting()
        {
            List<TenantSetting> tenantSettings = cxWrapper.TenantSettings();
            Assert.True(tenantSettings.Any());
            try
            {
                var _ = cxWrapper.IdeScansEnabled();
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
    }
}
