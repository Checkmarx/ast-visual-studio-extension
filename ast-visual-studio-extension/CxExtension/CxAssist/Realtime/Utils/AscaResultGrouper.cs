using System;
using System.Collections.Generic;
using System.Linq;
using ast_visual_studio_extension.CxCLI;
using ast_visual_studio_extension.CxWrapper.Models;
using ast_visual_studio_extension.CxExtension.Utils;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Groups and sorts ASCA scan results by line number and severity.
    ///
    /// Design Pattern: Strategy pattern for ASCA-specific result aggregation
    ///
    /// Grouping algorithm:
    /// 1. Group all scan details by line number
    /// 2. Within each line, sort by severity precedence (Critical > High > Medium > Low)
    /// 3. Order groups by line number (ascending)
    ///
    /// Result:
    /// Each ScanIssue represents issues on ONE LINE with primary severity.
    /// Multiple issues on same line are grouped together.
    ///
    /// Example output:
    ///   Line 42: [Critical SQL Injection, High XSS] → ScanIssue with 2 vulnerabilities
    ///   Line 50: [Medium Hardcoded Password] → ScanIssue with 1 vulnerability
    /// </summary>
    public static class AscaResultGrouper
    {
        /// <summary>
        /// Represents a group of ASCA issues on the same line.
        /// Maintains grouping and severity ordering for UI display.
        /// </summary>
        public class AscaIssueGroup
        {
            /// <summary>
            /// Line number where these issues occur.
            /// </summary>
            public int Line { get; set; }

            /// <summary>
            /// Scan details for this line, sorted by severity precedence.
            /// Index 0 = highest severity (most important to display first).
            /// </summary>
            public List<CxAscaDetail> Details { get; set; } = new();

            /// <summary>
            /// Gets primary (highest severity) issue in this group.
            /// </summary>
            public CxAscaDetail PrimaryIssue => Details?.FirstOrDefault();

            /// <summary>
            /// Indicates if this group has multiple issues.
            /// </summary>
            public bool HasMultipleIssues => Details?.Count > 1;

            /// <summary>
            /// Gets highest severity in this group.
            /// </summary>
            public string HighestSeverity =>
                Details?.Count > 0
                    ? SeverityMapper.GetHighestSeverity(
                        Details.Select(d => d.Severity).ToArray())
                    : "Medium";
        }

        /// <summary>
        /// Groups ASCA details by line number and sorts each group by severity.
        ///
        /// Algorithm:
        /// 1. Filter out null items
        /// 2. Group by line number
        /// 3. Sort each group by severity precedence (ascending)
        /// 4. Order groups by line number
        ///
        /// </summary>
        /// <param name="scanDetails">List of ASCA scan details from CLI</param>
        /// <returns>Ordered list of grouped issues</returns>
        public static List<AscaIssueGroup> GroupByLineAndSortBySeverity(List<CxAscaDetail> scanDetails)
        {
            if (scanDetails == null || scanDetails.Count == 0)
                return new List<AscaIssueGroup>();

            try
            {
                var grouped = scanDetails
                    .Where(d => d != null)                                  // Filter null items
                    .GroupBy(d => d.Line)                                  // Group by line
                    .Select(g => new AscaIssueGroup
                    {
                        Line = g.Key,
                        Details = g
                            .OrderBy(d => SeverityMapper.GetPrecedence(d.Severity))  // Sort by severity
                            .ToList()
                    })
                    .OrderBy(g => g.Line)                                  // Order groups by line
                    .ToList();

                OutputPaneWriter.WriteDebug(
                    $"AscaResultGrouper: Grouped {scanDetails.Count} details into {grouped.Count} groups");

                return grouped;
            }
            catch (Exception ex)
            {
                OutputPaneWriter.WriteError($"AscaResultGrouper: Error grouping results: {ex.Message}");
                return new List<AscaIssueGroup>();
            }
        }

        /// <summary>
        /// Gets primary (highest severity) issue from group.
        /// </summary>
        /// <param name="group">Issue group</param>
        /// <returns>Primary issue or null</returns>
        public static CxAscaDetail GetPrimaryIssue(AscaIssueGroup group)
        {
            return group?.Details?.FirstOrDefault();
        }

        /// <summary>
        /// Merges multiple groups into single logical issue for display.
        /// Used when displaying multiple issues on same line.
        /// </summary>
        /// <param name="groups">Groups to merge</param>
        /// <returns>Merged issue details for display</returns>
        public static (int totalCount, string highestSeverity, List<CxAscaDetail> allDetails)
            MergeGroups(params AscaIssueGroup[] groups)
        {
            if (groups == null || groups.Length == 0)
                return (0, "Medium", new List<CxAscaDetail>());

            var allDetails = groups
                .Where(g => g != null)
                .SelectMany(g => g.Details)
                .ToList();

            var highestSeverity = SeverityMapper.GetHighestSeverity(
                groups
                    .Where(g => g != null)
                    .Select(g => g.HighestSeverity)
                    .ToArray());

            return (allDetails.Count, highestSeverity, allDetails);
        }

        /// <summary>
        /// Validates group structure and content.
        /// Returns true if group is valid for UI display.
        /// </summary>
        /// <param name="group">Group to validate</param>
        /// <returns>True if valid (has line and details)</returns>
        public static bool IsValidGroup(AscaIssueGroup group)
        {
            return group != null
                && group.Line > 0
                && group.Details != null
                && group.Details.Count > 0
                && group.Details.All(d => d != null);
        }

        /// <summary>
        /// Counts total issues across all groups.
        /// </summary>
        /// <param name="groups">Groups to count</param>
        /// <returns>Total number of individual issues</returns>
        public static int CountTotalIssues(IEnumerable<AscaIssueGroup> groups)
        {
            return groups?
                .Where(g => g != null)
                .Sum(g => g.Details?.Count ?? 0) ?? 0;
        }
    }
}
