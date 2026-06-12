using System;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Represents standardized severity levels with precedence ordering.
    /// Used for consistent severity handling across all realtime scanners.
    /// Lower precedence value = higher severity.
    /// </summary>
    public enum SeverityLevel
    {
        Critical = 0,   // Highest severity
        High = 1,
        Medium = 2,     // Default for unknown/null
        Low = 3,
        Unknown = 4     // Distinct value (not aliased to Medium)
    }

    /// <summary>
    /// Maps various severity formats to standardized severity levels.
    /// Handles null values, "info" mapping, and invalid inputs.
    ///
    /// Thread-safe and uses immutable mapping dictionary.
    /// </summary>
    public static class SeverityMapper
    {
        /// <summary>
        /// Immutable mapping of raw severity strings to standardized levels.
        /// Case-insensitive for robust handling of various input formats.
        /// </summary>
        private static readonly Dictionary<string, SeverityLevel> SeverityMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "critical", SeverityLevel.Critical },
            { "high", SeverityLevel.High },
            { "medium", SeverityLevel.Medium },
            { "low", SeverityLevel.Low },
            { "info", SeverityLevel.Low },              // Map info to Low for consistency
            { "informational", SeverityLevel.Low },
            { "warning", SeverityLevel.Medium },
            { "error", SeverityLevel.High }
        };

        /// <summary>
        /// Maps raw severity string to standardized SeverityLevel enum.
        /// </summary>
        /// <param name="rawSeverity">Raw severity string (can be null or whitespace)</param>
        /// <returns>Standardized SeverityLevel; defaults to Medium if input is null/empty</returns>
        public static SeverityLevel MapToLevel(string rawSeverity)
        {
            if (string.IsNullOrWhiteSpace(rawSeverity))
                return SeverityLevel.Medium;

            return SeverityMap.TryGetValue(rawSeverity.Trim(), out var level)
                ? level
                : SeverityLevel.Unknown;
        }

        /// <summary>
        /// Maps raw severity to display string.
        /// Null/empty severities default to "Medium". Unrecognized severities display as "Unknown"
        /// to distinguish data quality issues from legitimate lack of severity info.
        /// </summary>
        /// <param name="rawSeverity">Raw severity string</param>
        /// <returns>Capitalized display string (Critical, High, Medium, Low, Unknown)</returns>
        public static string MapToString(string rawSeverity)
        {
            var level = MapToLevel(rawSeverity);
            return level switch
            {
                SeverityLevel.Critical => "Critical",
                SeverityLevel.High => "High",
                SeverityLevel.Medium => "Medium",
                SeverityLevel.Low => "Low",
                SeverityLevel.Unknown => "Unknown",
                _ => "Medium"
            };
        }

        /// <summary>
        /// Gets precedence value for sorting (lower = higher priority).
        /// Used for grouping and selecting highest severity issues.
        /// </summary>
        /// <param name="rawSeverity">Raw severity string</param>
        /// <returns>Precedence value (0-3); lower is higher priority</returns>
        public static int GetPrecedence(string rawSeverity)
        {
            return (int)MapToLevel(rawSeverity);
        }

        /// <summary>
        /// Returns highest severity from multiple values.
        /// Used when grouping multiple issues on same line.
        /// </summary>
        /// <param name="severities">Variable length array of severity strings</param>
        /// <returns>Display string of highest severity (Critical > High > Medium > Low)</returns>
        public static string GetHighestSeverity(params string[] severities)
        {
            if (severities == null || severities.Length == 0)
                return MapToString(null);

            var minPrecedence = int.MaxValue;
            string highest = "Medium";

            foreach (var severity in severities)
            {
                var precedence = GetPrecedence(severity);
                if (precedence < minPrecedence)
                {
                    minPrecedence = precedence;
                    highest = MapToString(severity);
                }
            }

            return highest;
        }

        /// <summary>
        /// Compares two severity strings by precedence.
        /// Returns: < 0 if s1 is higher priority
        ///         = 0 if equal priority
        ///         > 0 if s2 is higher priority
        /// </summary>
        public static int CompareSeverities(string severity1, string severity2)
        {
            var prec1 = GetPrecedence(severity1);
            var prec2 = GetPrecedence(severity2);
            return prec1.CompareTo(prec2);
        }
    }
}
