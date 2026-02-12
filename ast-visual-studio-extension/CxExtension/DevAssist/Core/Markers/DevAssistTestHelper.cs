using ast_visual_studio_extension.CxExtension.DevAssist.Core.Models;
using System.Collections.Generic;

namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Markers
{
    /// <summary>
    /// Helper class for testing DevAssist hover popups with mock data
    /// Use this to generate sample vulnerabilities for each scanner type
    /// </summary>
    public static class DevAssistTestHelper
    {
        /// <summary>
        /// Creates a sample OSS vulnerability with package information
        /// </summary>
        public static Vulnerability CreateOssVulnerability()
        {
            return new Vulnerability
            {
                Id = "OSS-001",
                Title = "Vulnerable Package Detected",
                Description = "The package 'lodash' has a known security vulnerability that allows prototype pollution attacks.",
                Severity = SeverityLevel.High,
                Scanner = ScannerType.OSS,
                LineNumber = 5,
                ColumnNumber = 10,
                FilePath = "package.json",
                
                // OSS-specific fields
                PackageName = "lodash",
                PackageVersion = "4.17.15",
                PackageManager = "npm",
                RecommendedVersion = "4.17.21",
                CveName = "CVE-2020-8203",
                CvssScore = 7.4,
                LearnMoreUrl = "https://nvd.nist.gov/vuln/detail/CVE-2020-8203",
                FixLink = "https://github.com/lodash/lodash/security/advisories"
            };
        }

        /// <summary>
        /// Creates a sample ASCA vulnerability with remediation advice
        /// </summary>
        public static Vulnerability CreateAscaVulnerability()
        {
            return new Vulnerability
            {
                Id = "ASCA-001",
                Title = "SQL Injection Vulnerability",
                Description = "User input is directly concatenated into SQL query without sanitization.",
                Severity = SeverityLevel.Critical,
                Scanner = ScannerType.ASCA,
                LineNumber = 42,
                ColumnNumber = 15,
                FilePath = "UserController.cs",
                
                // ASCA-specific fields
                RuleName = "SQL_INJECTION",
                RemediationAdvice = "Use parameterized queries or prepared statements instead of string concatenation. Replace the current query with SqlCommand.Parameters.AddWithValue() to safely handle user input.",
                LearnMoreUrl = "https://owasp.org/www-community/attacks/SQL_Injection"
            };
        }

        /// <summary>
        /// Creates a sample IaC vulnerability with expected vs actual values
        /// </summary>
        public static Vulnerability CreateIacVulnerability()
        {
            return new Vulnerability
            {
                Id = "IAC-001",
                Title = "S3 Bucket Publicly Accessible",
                Description = "S3 bucket is configured to allow public access, which may expose sensitive data.",
                Severity = SeverityLevel.High,
                Scanner = ScannerType.IaC,
                LineNumber = 28,
                ColumnNumber = 5,
                FilePath = "terraform/s3.tf",
                
                // IaC-specific fields
                ExpectedValue = "acl = 'private'",
                ActualValue = "acl = 'public-read'",
                LearnMoreUrl = "https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html"
            };
        }

        /// <summary>
        /// Creates a sample Secrets vulnerability
        /// </summary>
        public static Vulnerability CreateSecretsVulnerability()
        {
            return new Vulnerability
            {
                Id = "SECRETS-001",
                Title = "Hardcoded API Key Detected",
                Description = "An API key is hardcoded in the source code. This is a security risk as the key may be exposed in version control.",
                Severity = SeverityLevel.Critical,
                Scanner = ScannerType.Secrets,
                LineNumber = 12,
                ColumnNumber = 20,
                FilePath = "config.js",
                
                // Secrets-specific fields
                SecretType = "API Key",
                LearnMoreUrl = "https://owasp.org/www-community/vulnerabilities/Use_of_hard-coded_password"
            };
        }

        /// <summary>
        /// Creates a sample Containers vulnerability
        /// </summary>
        public static Vulnerability CreateContainersVulnerability()
        {
            return new Vulnerability
            {
                Id = "CONTAINERS-001",
                Title = "Vulnerable Base Image",
                Description = "The Docker base image contains known vulnerabilities in system packages.",
                Severity = SeverityLevel.Medium,
                Scanner = ScannerType.Containers,
                LineNumber = 1,
                ColumnNumber = 1,
                FilePath = "Dockerfile",
                
                // Containers-specific fields (similar to OSS)
                PackageName = "openssl",
                PackageVersion = "1.1.1d",
                CveName = "CVE-2021-3711",
                CvssScore = 9.8,
                LearnMoreUrl = "https://nvd.nist.gov/vuln/detail/CVE-2021-3711"
            };
        }

        /// <summary>
        /// Creates a sample Low severity vulnerability for testing colored markers (AST-133227).
        /// Use with Critical, High, Medium to verify visual distinction between all four severities.
        /// </summary>
        public static Vulnerability CreateLowSeverityVulnerability()
        {
            return new Vulnerability
            {
                Id = "LOW-001",
                Title = "Low Severity Finding",
                Description = "Minor code quality or low-risk security finding for testing Low severity underline color (green).",
                Severity = SeverityLevel.Low,
                Scanner = ScannerType.ASCA,
                LineNumber = 7,
                ColumnNumber = 1,
                FilePath = "Sample.cs",
                RuleName = "LOW_RISK",
                RemediationAdvice = "Consider addressing when convenient."
            };
        }

        /// <summary>
        /// Creates a collection of all sample vulnerabilities for testing
        /// </summary>
        public static List<Vulnerability> CreateAllSampleVulnerabilities()
        {
            return new List<Vulnerability>
            {
                CreateOssVulnerability(),
                CreateAscaVulnerability(),
                CreateIacVulnerability(),
                CreateSecretsVulnerability(),
                CreateContainersVulnerability(),
                CreateLowSeverityVulnerability()
            };
        }
    }
}

