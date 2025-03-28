using ast_visual_studio_extension.CxWrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_wrapper_tests
{
    [Collection("Cx Collection")]
    public class TriageTest : BaseTest
    {
        [Fact]
        public void TestTriageShow()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Any());

            Scan scan = GetFirstScanWithResults(scanList).First().Key;
            Result result = GetFirstScanWithResults(scanList).First().Value.results.Where(r => r.Type.Equals("sast")).First();

            List<Predicate> predicates = cxWrapper.TriageShow(scan.ProjectId, result.SimilarityId, result.Type);

            Assert.NotNull(predicates);
        }

        [Fact]
        public void TestTriageUpdate()
        {
            List<Scan> scanList = cxWrapper.GetScans("statuses=Completed");
            Assert.True(scanList.Count > 0);

            Scan scan = GetFirstScanWithResults(scanList).First().Key;
            Result result = GetFirstScanWithResults(scanList).First().Value.results.Where(r => r.Type.Equals("sast")).First();

            try
            {
                cxWrapper.TriageUpdate(scan.ProjectId, result.SimilarityId, result.Type, "CONFIRMED", "Edited via Wrapper", "HIGH");
            }
            catch(Exception e)
            {
                throw new XunitException("Triage update failed. An exception shouldn't be thrown. Cause: " + e.Message);
            }
        }

        [Fact]
        public void TestTriageGetStates_ShouldReturnAtLeastFiveStates()
        {
            List<State> states = cxWrapper.TriageGetStates(false);

            Assert.NotNull(states);
            Assert.True(states.Count >= 5, $"Expected at least 5 states, but got {states.Count}");
        }
    }

}

