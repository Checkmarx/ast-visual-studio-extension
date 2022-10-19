using ast_visual_studio_extension.CxWrapper.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class ScanTest : BaseTest
    {
        [Fact]
        public void TestScanShow()
        {
            List<Scan> scanList = cxWrapper.GetScans();
            Assert.True(scanList.Any());

            Scan scan = cxWrapper.ScanShow(scanList.First().ID);
            Assert.Equal(scanList.First().ID, scan.ID);
        }

        [Fact]
        public void TestScanList()
        {
            List<Scan> scanList = cxWrapper.GetScans("limit=10");
            Assert.True(scanList.Count <= 10);
        }

    }
}
