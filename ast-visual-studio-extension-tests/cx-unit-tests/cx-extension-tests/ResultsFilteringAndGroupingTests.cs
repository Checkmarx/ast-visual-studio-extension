using ast_visual_studio_extension.CxExtension.Enums;
using ast_visual_studio_extension.CxWrapper.Models;
using ast_visual_studio_extension.CxExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using Xunit;
using ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_extansion_test
{
    public class ResultsFilteringAndGroupingTests
    {
        //[Fact]
        //public void FilterAndGroupResults_ShouldReturnFilteredAndGroupedResults()
        //    {
        //        StaThreadHelper.RunInStaThread(() =>
        //        {
        //            var packageMock = new Mock<AsyncPackage>();
        //            var results = new List<TreeViewItem>
        //{
        //    new TreeViewItem { Tag = new Result { State = "Open", Severity = "High", Type = "Engine1" } },
        //    new TreeViewItem { Tag = new Result { State = "Closed", Severity = "Low", Type = "Engine2" } }
        //};

        //            var filteredResults = ResultsFilteringAndGrouping.FilterAndGroupResults(packageMock.Object, results);

        //            Assert.Single(filteredResults);
        //            Assert.Equal("Engine1", (filteredResults[0].Header as TextBlock).Tag as string);

        //        });
        //    }

        [Fact]
        public void GetInsertLocation_ShouldReturnCorrectInsertLocation()
        {
            StaThreadHelper.RunInStaThread(() =>
            {
                var enabledGroupBys = new List<GroupBy> { GroupBy.ENGINE };
                var treeResults = new List<TreeViewItem>();
                var result = new Result { Type = "SAST" };

                var insertLocation = ResultsFilteringAndGrouping.GetInsertLocation(enabledGroupBys, treeResults, result);

                Assert.NotNull(insertLocation);
                Assert.Single(treeResults);
                Assert.Equal("SAST", (treeResults[0].Header as TextBlock).Tag as string);
            });
        }

        [Theory]
        [InlineData(GroupBy.ENGINE, "SAST")]
        [InlineData(GroupBy.FILE, "file1.cs")]
        [InlineData(GroupBy.SEVERITY, "HIGH")]
        [InlineData(GroupBy.STATE, "CONFIRMED")]
        [InlineData(GroupBy.QUERY_NAME, "Query1")]
        public void GetGroupByTitleGenerator_ShouldReturnCorrectTitle(GroupBy groupBy, string expectedTitle)
        {
            var result = new Result
            {
                Type = "SAST",
                Data = new Data { FileName = "file1.cs", QueryName = "Query1" },
                Severity = Severity.HIGH.ToString(),
                State = State.CONFIRMED.ToString()
            };

            var generator = ResultsFilteringAndGrouping.GetGroupByTitleGenerator(groupBy);
            var title = generator.Invoke(result);

            Assert.Equal(expectedTitle, title);
        }
    }
}
