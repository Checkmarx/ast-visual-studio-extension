using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class ResultTest : BaseTest
    {
        public static string SCAN_COMPLETED => "completed";

        [Fact]
        public void TestResultsHTML()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Any());

            string scanId = scanList[0].ID;
            string results = cxWrapper.GetResults(scanId, ReportFormat.summaryHTML);

            Assert.True(!string.IsNullOrEmpty(results));
        }

        [Fact]
        public void TestResultsJSON()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Any());
            Scan scan = scanList.FirstOrDefault(scan => scan.Status.ToLower() == SCAN_COMPLETED);

            string scanId = scan.ID;
            string results = cxWrapper.GetResults(scanId, ReportFormat.json);

            Assert.True(!string.IsNullOrEmpty(results));
        }

        [Fact]
        public void TestResultsSummaryJSON()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Any());

            string scanId = scanList[0].ID;
            ResultsSummary results = cxWrapper.GetResultsSummary(scanId);

            Assert.True(!string.IsNullOrEmpty(results.ScanID));
        }

        [Fact]
        public void TestResultsStructure()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Any());
            List<Scan> completedScans = scanList.Where(scan => scan.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)).ToList();

            Results results = GetFirstScanWithResults(completedScans).First().Value;

            Assert.Equal(results.totalCount, results.results.Count);
        }
    }
}
