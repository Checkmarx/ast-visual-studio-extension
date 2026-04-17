using ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils;
using ast_visual_studio_extension.CxWrapper.Models;
using System.Collections.Generic;
using Xunit;

namespace ast_visual_studio_extension_tests.cx_unit_tests.cx_realtime_tests.Utils
{
    public class AscaResultGrouperTests
    {
        private static CxAscaDetail CreateDetail(int line, string severity, string ruleName)
        {
            return new CxAscaDetail(
                ruleId: line,
                language: "csharp",
                ruleName: ruleName,
                severity: severity,
                fileName: "test.cs",
                line: line,
                problematicLine: "code",
                length: 5,
                remediationAdvise: "fix it",
                description: "test issue");
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_WithEmptyList_ReturnsEmptyList()
        {
            var details = new List<CxAscaDetail>();

            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(details);

            Assert.Empty(result);
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_WithNullList_ReturnsEmptyList()
        {
            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(null);

            Assert.Empty(result);
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_WithSingleIssue_ReturnsSingleGroup()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "High", "SQL Injection")
            };

            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(details);

            Assert.Single(result);
            Assert.Equal(10, result[0].Line);
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_WithMultipleIssuesSameLine_GroupsTogether()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "Medium", "Issue A"),
                CreateDetail(10, "High", "Issue B"),
                CreateDetail(10, "Low", "Issue C")
            };

            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(details);

            Assert.Single(result);
            Assert.Equal(3, result[0].Details.Count);
            Assert.True(result[0].HasMultipleIssues);
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_SortsBySeverityPrecedence()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "Low", "Issue A"),
                CreateDetail(10, "Critical", "Issue B"),
                CreateDetail(10, "Medium", "Issue C")
            };

            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(details);

            Assert.Single(result);
            Assert.Equal("Critical", result[0].Details[0].Severity);
            Assert.Equal("Medium", result[0].Details[1].Severity);
            Assert.Equal("Low", result[0].Details[2].Severity);
        }

        [Fact]
        public void GroupByLineAndSortBySeverity_WithMultipleLines_GroupsByLine()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(20, "High", "Issue 1"),
                CreateDetail(10, "Medium", "Issue 2"),
                CreateDetail(15, "Low", "Issue 3")
            };

            var result = AscaResultGrouper.GroupByLineAndSortBySeverity(details);

            Assert.Equal(3, result.Count);
            Assert.Equal(10, result[0].Line);
            Assert.Equal(15, result[1].Line);
            Assert.Equal(20, result[2].Line);
        }

        [Fact]
        public void AscaIssueGroup_PrimaryIssue_ReturnsMostSevereIssue()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "Low", "Issue A"),
                CreateDetail(10, "Critical", "Issue B"),
                CreateDetail(10, "Medium", "Issue C")
            };

            // After sorting by severity
            var sorted = AscaResultGrouper.GroupByLineAndSortBySeverity(details);
            Assert.Equal("Issue B", sorted[0].PrimaryIssue.RuleName);
        }

        [Fact]
        public void AscaIssueGroup_HighestSeverity_ReturnsHighestSeverityInGroup()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "Medium", "Issue A"),
                CreateDetail(10, "Low", "Issue B")
            };

            var group = new AscaResultGrouper.AscaIssueGroup
            {
                Line = 10,
                Details = details
            };

            Assert.Equal("Medium", group.HighestSeverity);
        }

        [Fact]
        public void AscaIssueGroup_HasMultipleIssues_ReturnsTrueWhenMultiple()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "High", "Issue A"),
                CreateDetail(10, "Medium", "Issue B")
            };

            var group = new AscaResultGrouper.AscaIssueGroup
            {
                Line = 10,
                Details = details
            };

            Assert.True(group.HasMultipleIssues);
        }

        [Fact]
        public void AscaIssueGroup_HasMultipleIssues_ReturnsFalseWhenSingle()
        {
            var details = new List<CxAscaDetail>
            {
                CreateDetail(10, "High", "Issue A")
            };

            var group = new AscaResultGrouper.AscaIssueGroup
            {
                Line = 10,
                Details = details
            };

            Assert.False(group.HasMultipleIssues);
        }

        [Fact]
        public void AscaIssueGroup_EmptyDetails_HighestSeverityReturnsMedium()
        {
            var group = new AscaResultGrouper.AscaIssueGroup
            {
                Line = 10,
                Details = new List<CxAscaDetail>()
            };

            Assert.Equal("Medium", group.HighestSeverity);
        }
    }
}
