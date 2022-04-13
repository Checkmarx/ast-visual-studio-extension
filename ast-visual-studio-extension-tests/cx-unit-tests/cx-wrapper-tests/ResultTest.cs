using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    public class ResultTest : BaseTest
    {
        [Fact]
        public void TestResultsHTML()
        {
            List<Scan> scanList = cxWrapper.GetScans();
            Assert.True(scanList.Any());

            string scanId = scanList[0].ID;
            string results = cxWrapper.GetResults(scanId, ReportFormat.summaryHTML);

            Assert.True(!string.IsNullOrEmpty(results));
        }

        [Fact]
        public void TestResultsJSON()
        {
            List<Scan> scanList = cxWrapper.GetScans();
            Assert.True(scanList.Any());

            string scanId = scanList[0].ID;
            string results = cxWrapper.GetResults(scanId, ReportFormat.json);

            Assert.True(!string.IsNullOrEmpty(results));
        }

        [Fact]
        public void TestResultsSummaryJSON()
        {
            List<Scan> scanList = cxWrapper.GetScans();
            Assert.True(scanList.Any());

            string scanId = scanList[0].ID;
            ResultsSummary results = cxWrapper.GetResultsSummary(scanId);

            Assert.True(!string.IsNullOrEmpty(results.ScanID));
        }

        [Fact]
        public void TestResultsStructure()
        {
            List<Scan> scanList = cxWrapper.GetScans();
            Assert.True(scanList.Any());

            Results results = GetFirstScanWithResults(scanList).First().Value;

            Assert.Equal(results.totalCount, results.results.Count);
        }
    }
}
