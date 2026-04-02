using System;
using System.Security.Cryptography;
using System.Text;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Generates deterministic, unique IDs for scan issues.
    ///
    /// Used for:
    /// - Issue deduplication across multiple scans
    /// - Ignore file tracking and persistence
    /// - UI state persistence (expanded/collapsed, selected)
    /// - Change detection (moved issues, line number changes)
    ///
    /// Design Pattern: Utility/Factory pattern with immutable operations
    /// Thread-safe: All methods are thread-safe with no shared mutable state
    /// </summary>
    public static class UniqueIdGenerator
    {
        private const int HASH_LENGTH = 16; // Use first 16 chars of SHA-256 hex for readability

        /// <summary>
        /// Generates unique ID from line number, issue identifier, and file name.
        /// ID is deterministic - same input always produces same ID.
        ///
        /// Use case: Most common - ASCA, Secrets, OSS scanners
        /// </summary>
        /// <param name="line">Line number where issue occurs</param>
        /// <param name="issueIdentifier">Unique issue identifier (rule ID, title, etc.)</param>
        /// <param name="fileName">Name of the file (not full path for consistency)</param>
        /// <returns>Deterministic unique ID string</returns>
        public static string GenerateId(int line, string issueIdentifier, string fileName)
        {
            if (string.IsNullOrEmpty(issueIdentifier))
                issueIdentifier = "unknown";
            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            var combined = $"{line}:{issueIdentifier}:{fileName}";
            return HashString(combined);
        }

        /// <summary>
        /// Generates ID from multiple properties for complex grouping scenarios.
        ///
        /// Use case: When additional context is needed for deduplication
        /// </summary>
        /// <param name="line">Line number</param>
        /// <param name="rule">Rule/check identifier</param>
        /// <param name="description">Issue description</param>
        /// <param name="fileName">File name</param>
        /// <returns>Deterministic unique ID string</returns>
        public static string GenerateId(int line, string rule, string description, string fileName)
        {
            if (string.IsNullOrEmpty(rule))
                rule = "unknown";
            if (string.IsNullOrEmpty(description))
                description = "unknown";
            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            var combined = $"{line}:{rule}:{description}:{fileName}";
            return HashString(combined);
        }

        /// <summary>
        /// Generates ID for severity + line combination.
        /// Used for ASCA grouping where severity determines primary issue.
        ///
        /// Use case: ASCA scanner - when multiple issues on same line
        /// </summary>
        /// <param name="line">Line number</param>
        /// <param name="severity">Issue severity</param>
        /// <param name="ruleId">Rule identifier</param>
        /// <param name="fileName">File name</param>
        /// <returns>Deterministic unique ID string</returns>
        public static string GenerateIdWithSeverity(int line, string severity, string ruleId, string fileName)
        {
            if (string.IsNullOrEmpty(severity))
                severity = "unknown";
            if (string.IsNullOrEmpty(ruleId))
                ruleId = "unknown";
            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            var combined = $"{line}:{severity}:{ruleId}:{fileName}";
            return HashString(combined);
        }

        /// <summary>
        /// Generates ID for location-based grouping (used by IAC and Containers).
        /// Includes character range for precise location tracking.
        ///
        /// Use case: IaC scanner - multiple locations in same file
        /// </summary>
        /// <param name="line">Line number</param>
        /// <param name="startColumn">Start column (0-based)</param>
        /// <param name="endColumn">End column (0-based)</param>
        /// <param name="fileName">File name</param>
        /// <returns>Deterministic unique ID string</returns>
        public static string GenerateLocationBasedId(int line, int startColumn, int endColumn, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            var combined = $"{line}:{startColumn}-{endColumn}:{fileName}";
            return HashString(combined);
        }

        /// <summary>
        /// Generates ID for package-based grouping (used by OSS scanner).
        /// Includes package version for vulnerability tracking.
        ///
        /// Use case: OSS scanner - track vulnerabilities per package version
        /// </summary>
        /// <param name="packageName">Name of the package</param>
        /// <param name="packageVersion">Version of the package</param>
        /// <param name="fileName">Manifest file name (package.json, pom.xml, etc.)</param>
        /// <returns>Deterministic unique ID string</returns>
        public static string GeneratePackageId(string packageName, string packageVersion, string fileName)
        {
            if (string.IsNullOrEmpty(packageName))
                packageName = "unknown";
            if (string.IsNullOrEmpty(packageVersion))
                packageVersion = "0.0.0";
            if (string.IsNullOrEmpty(fileName))
                fileName = "unknown";

            var combined = $"pkg:{packageName}@{packageVersion}:{fileName}";
            return HashString(combined);
        }

        /// <summary>
        /// Hashes input string to create deterministic, fixed-length ID.
        /// Uses SHA-256 for strong collision resistance.
        /// Falls back to hashCode if SHA-256 unavailable.
        /// </summary>
        private static string HashString(string input)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                    var hexString = Convert.ToHexString(hashedBytes);
                    // Return first 16 chars for readability (still 64-bit collision resistance)
                    return hexString.Length > HASH_LENGTH ? hexString.Substring(0, HASH_LENGTH) : hexString;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SHA-256 hashing failed: {ex.Message}. Using fallback.");
                // Fallback to simple hash code (less collision resistant but always available)
                return input.GetHashCode().ToString("x8");
            }
        }
    }
}
