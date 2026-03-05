using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;
using ast_visual_studio_extension.CxExtension.CxAssist.UI.FindingsWindow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Common mock data used to demonstrate all four POC features:
    /// underline (squiggle), gutter icon, problem window, and popup hover.
    /// One source of truth so editor and findings window show the same data.
    /// </summary>
    public static class CxAssistMockData
    {
        /// <summary>Default file path used for mock vulnerabilities (editor and findings window).</summary>
        public const string DefaultFilePath = "Program.cs";

        /// <summary>Vulnerability Id that uses standard Quick Info popup only (no custom hover popup).</summary>
        public const string QuickInfoOnlyVulnerabilityId = "POC-007";

        /// <summary>
        /// Returns the common list of mock vulnerabilities used for:
        /// - Gutter icons (severity-specific icons on lines 1, 3, 5, 7, 9)
        /// - Underline (squiggles on the same lines)
        /// - Popup hover (hover over those lines to see rich popup with OSS/ASCA content)
        /// - Problem window (when converted to FileNodes via BuildFileNodesFromVulnerabilities)
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses DefaultFilePath.</param>
        public static List<Vulnerability> GetCommonVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? DefaultFilePath : filePath;

            return new List<Vulnerability>
            {
                // Line 1 – Malicious (OSS) – shows in gutter, underline, hover, problem window
                new Vulnerability
                {
                    Id = "POC-001",
                    Title = "Malicious Package",
                    Description = "Test Malicious vulnerability – known malicious package in dependencies.",
                    Severity = SeverityLevel.Malicious,
                    Scanner = ScannerType.OSS,
                    LineNumber = 1,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "node-ipc",
                    PackageVersion = "10.1.1",
                    RecommendedVersion = "10.2.0",
                    CveName = "CVE-Malicious-Example",
                    CvssScore = 9.8,
                    LearnMoreUrl = "https://example.com/cve"
                },
                // Line 3 – Critical (ASCA)
                new Vulnerability
                {
                    Id = "POC-002",
                    Title = "SQL Injection",
                    Description = "Test Critical vulnerability – user input concatenated into SQL without sanitization.",
                    Severity = SeverityLevel.Critical,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 3,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "SQL_INJECTION",
                    RemediationAdvice = "Use parameterized queries or prepared statements."
                },
                // Line 5 – High (OSS) – first of two on same line (severity count in popup)
                new Vulnerability
                {
                    Id = "POC-003",
                    Title = "High-Risk Package",
                    Description = "Test High vulnerability – vulnerable version of package.",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.OSS,
                    LineNumber = 5,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "lodash",
                    PackageVersion = "4.17.15",
                    RecommendedVersion = "4.17.21",
                    CveName = "CVE-2020-8203",
                    CvssScore = 7.4
                },
                // Line 5 – Medium (second on same line)
                new Vulnerability
                {
                    Id = "POC-004",
                    Title = "Medium Severity Finding",
                    Description = "Test Medium vulnerability on same line as High.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 5,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "WEAK_CRYPTO",
                    RemediationAdvice = "Use a stronger algorithm."
                },
                // Line 7 – Medium (OSS)
                new Vulnerability
                {
                    Id = "POC-005",
                    Title = "Outdated Dependency",
                    Description = "Test Medium vulnerability – dependency has a known issue.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 7,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "axios",
                    PackageVersion = "0.21.0",
                    RecommendedVersion = "0.27.0"
                },
                // Line 9 – Low
                new Vulnerability
                {
                    Id = "POC-006",
                    Title = "Low Severity",
                    Description = "Test Low vulnerability – minor finding.",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.OSS,
                    LineNumber = 9,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "debug",
                    PackageVersion = "2.6.9"
                },
                // Line 11 – Quick Info only (no custom hover popup): shows standard VS Quick Info with rich text and links
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "High Severity Finding",
                    Description = "This finding uses the standard Quick Info popup: Checkmarx One Assist badge, rich severity name, description, and action links (Fix with Checkmarx Assist, View Details, Ignore vulnerability).",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 11,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "QUICK_INFO_DEMO",
                    RemediationAdvice = "Use the Quick Info links to fix, view details, or ignore."
                },
                // Line 13 – Quick Info only: 2 vulnerabilities on same line (no custom popup; hover shows Quick Info for first)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "First finding on line (Critical)",
                    Description = "First of two Quick-Info-only findings on this line. Critical severity – sensitive data exposure risk.",
                    Severity = SeverityLevel.Critical,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 13,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "SENSITIVE_DATA",
                    RemediationAdvice = "Avoid logging or exposing sensitive data."
                },
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Second finding on line (Medium)",
                    Description = "Second of two Quick-Info-only findings on this line. Medium severity – weak cryptographic usage.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 13,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "WEAK_CRYPTO",
                    RemediationAdvice = "Use a stronger algorithm."
                },
                // Line 15 – Quick Info only (single finding)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Quick Info – Outdated dependency",
                    Description = "Quick-Info-only demo: outdated package with known CVE. Use standard Quick Info links to fix or view details.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 15,
                    ColumnNumber = 0,
                    FilePath = path,
                    PackageName = "minimist",
                    PackageVersion = "1.2.0",
                    RecommendedVersion = "1.2.6",
                    CveName = "CVE-2022-21803"
                },
                // Line 17 – Quick Info only (single finding)
                new Vulnerability
                {
                    Id = QuickInfoOnlyVulnerabilityId,
                    Title = "Quick Info – Low severity",
                    Description = "Quick-Info-only demo: low-severity finding. Only the standard Quick Info popup is shown here.",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.ASCA,
                    LineNumber = 17,
                    ColumnNumber = 0,
                    FilePath = path,
                    RuleName = "LOW_SEVERITY_DEMO",
                    RemediationAdvice = "Consider addressing in next sprint."
                }
            };
        }

        /// <summary>
        /// Returns mock OSS-style vulnerabilities for package.json (gutter, underline, problem window, Error List, popup).
        /// Line numbers and StartIndex/EndIndex match AST-CLI OSS realtime scan output (Locations per package).
        /// Includes Status=OK (success gutter icon) and Status=Unknown (unknown icon) per reference behavior.
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses "package.json".</param>
        public static List<Vulnerability> GetPackageJsonMockVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? "package.json" : filePath;

            return new List<Vulnerability>
            {
                // OSS no vul (Status OK) – success gutter icon; Locations from scan
                new Vulnerability
                {
                    Id = "OSS-ok-nyc-config-typescript",
                    Title = "@istanbuljs/nyc-config-typescript (OK)",
                    Description = "No known vulnerabilities.",
                    Severity = SeverityLevel.Ok,
                    Scanner = ScannerType.OSS,
                    LineNumber = 9,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 49,
                    FilePath = path,
                    PackageName = "@istanbuljs/nyc-config-typescript",
                    PackageVersion = "1.0.2",
                    PackageManager = "npm"
                },
                new Vulnerability
                {
                    Id = "OSS-ok-webpack-cli",
                    Title = "webpack-cli (OK)",
                    Description = "No known vulnerabilities.",
                    Severity = SeverityLevel.Ok,
                    Scanner = ScannerType.OSS,
                    LineNumber = 11,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 27,
                    FilePath = path,
                    PackageName = "webpack-cli",
                    PackageVersion = "5.1.4",
                    PackageManager = "npm"
                },
                new Vulnerability
                {
                    Id = "OSS-ok-popperjs",
                    Title = "@popperjs/core (OK)",
                    Description = "No known vulnerabilities.",
                    Severity = SeverityLevel.Ok,
                    Scanner = ScannerType.OSS,
                    LineNumber = 15,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 32,
                    FilePath = path,
                    PackageName = "@popperjs/core",
                    PackageVersion = "2.11.8",
                    PackageManager = "npm"
                },
                new Vulnerability
                {
                    Id = "OSS-ok-minimist",
                    Title = "minimist (OK)",
                    Description = "No known vulnerabilities.",
                    Severity = SeverityLevel.Ok,
                    Scanner = ScannerType.OSS,
                    LineNumber = 17,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 24,
                    FilePath = path,
                    PackageName = "minimist",
                    PackageVersion = "1.2.6",
                    PackageManager = "npm"
                },
                // OSS unknown status – unknown gutter icon
                new Vulnerability
                {
                    Id = "OSS-unknown-ast-cli-wrapper",
                    Title = "@checkmarxdev/ast-cli-javascript-wrapper (Unknown)",
                    Description = "Unknown status.",
                    Severity = SeverityLevel.Unknown,
                    Scanner = ScannerType.OSS,
                    LineNumber = 10,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 74,
                    FilePath = path,
                    PackageName = "@checkmarxdev/ast-cli-javascript-wrapper",
                    PackageVersion = "0.0.131",
                    PackageManager = "npm"
                },
                // Line 13 – validator (2 CVEs); Locations: StartIndex 4, EndIndex 27
                new Vulnerability
                {
                    Id = "CVE-2025-12758",
                    Title = "validator (CVE-2025-12758)",
                    Description = "Incomplete Filtering in isLength() function.",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.OSS,
                    LineNumber = 14,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 27,
                    FilePath = path,
                    PackageName = "validator",
                    PackageVersion = "13.12.0",
                    PackageManager = "npm",
                    CveName = "CVE-2025-12758",
                    RecommendedVersion = "13.15.22"
                },
                new Vulnerability
                {
                    Id = "CVE-2025-56200",
                    Title = "validator (CVE-2025-56200)",
                    Description = "A URL validation bypass vulnerability exists in validator.js.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 14,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 27,
                    FilePath = path,
                    PackageName = "validator",
                    PackageVersion = "13.12.0",
                    PackageManager = "npm",
                    CveName = "CVE-2025-56200",
                    RecommendedVersion = "13.15.16"
                },
                // Line 15 – lodash; Locations: StartIndex 4, EndIndex 24
                new Vulnerability
                {
                    Id = "CVE-2025-13465",
                    Title = "lodash (CVE-2025-13465)",
                    Description = "Prototype Pollution in _.unset and _.omit.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 16,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 24,
                    FilePath = path,
                    PackageName = "lodash",
                    PackageVersion = "4.17.21",
                    PackageManager = "npm",
                    CveName = "CVE-2025-13465",
                    RecommendedVersion = "4.17.23"
                },
                // Line 17 – moment (2 CVEs); Locations: StartIndex 4, EndIndex 23
                new Vulnerability
                {
                    Id = "CVE-2022-24785",
                    Title = "moment (CVE-2022-24785)",
                    Description = "Path traversal vulnerability in Moment.js.",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.OSS,
                    LineNumber = 18,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 23,
                    FilePath = path,
                    PackageName = "moment",
                    PackageVersion = "2.18.0",
                    PackageManager = "npm",
                    CveName = "CVE-2022-24785",
                    RecommendedVersion = "2.29.2"
                },
                new Vulnerability
                {
                    Id = "CVE-2022-31129",
                    Title = "moment (CVE-2022-31129)",
                    Description = "ReDoS via inefficient parsing algorithm.",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.OSS,
                    LineNumber = 18,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 23,
                    FilePath = path,
                    PackageName = "moment",
                    PackageVersion = "2.18.0",
                    PackageManager = "npm",
                    CveName = "CVE-2022-31129",
                    RecommendedVersion = "2.29.4"
                },
                // Line 18 – request; Locations: StartIndex 4, EndIndex 24
                new Vulnerability
                {
                    Id = "CVE-2023-28155",
                    Title = "request (CVE-2023-28155)",
                    Description = "SSRF mitigations bypass via cross-protocol redirect.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.OSS,
                    LineNumber = 19,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 24,
                    FilePath = path,
                    PackageName = "request",
                    PackageVersion = "2.88.2",
                    PackageManager = "npm",
                    CveName = "CVE-2023-28155"
                },
                // Line 19 – node-ipc (Malicious); Locations: StartIndex 4, EndIndex 23
                new Vulnerability
                {
                    Id = "OSS-node-ipc-10.1.1-Malicious",
                    Title = "node-ipc (Malicious)",
                    Description = "Malicious package: node-ipc@10.1.1.",
                    Severity = SeverityLevel.Malicious,
                    Scanner = ScannerType.OSS,
                    LineNumber = 20,
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 23,
                    FilePath = path,
                    PackageName = "node-ipc",
                    PackageVersion = "10.1.1",
                    PackageManager = "npm"
                }
            };
        }

        /// <summary>
        /// Returns mock Secrets + ASCA vulnerabilities for secrets.py (gutter, underline, problem window, Error List, popup).
        /// Matches Secrets realtime scan shape: generic-api-key (line 5), github-pat (line 7), private-key (lines 17–19), plus ASCA findings.
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses "secrets.py".</param>
        public static List<Vulnerability> GetSecretsPyMockVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? "secrets.py" : filePath;
            var list = new List<Vulnerability>();

            // --- Secrets (from realtime scan JSON; Locations with StartIndex/EndIndex) ---
            list.Add(new Vulnerability
            {
                Id = "generic-api-key",
                Title = "generic-api-key",
                Description = "Detected a Generic API Key, potentially exposing access to various services and sensitive operations.",
                Severity = SeverityLevel.High,
                Scanner = ScannerType.Secrets,
                LineNumber = 6,
                ColumnNumber = 2,
                StartIndex = 1,
                EndIndex = 43,
                FilePath = path,
                SecretType = "Generic API Key"
            });
            list.Add(new Vulnerability
            {
                Id = "github-pat",
                Title = "github-pat",
                Description = "Uncovered a GitHub Personal Access Token, potentially leading to unauthorized repository access and sensitive content exposure.",
                Severity = SeverityLevel.Medium,
                Scanner = ScannerType.Secrets,
                LineNumber = 9,
                ColumnNumber = 18,
                StartIndex = 17,
                EndIndex = 56,
                FilePath = path,
                SecretType = "GitHub PAT"
            });
            list.Add(new Vulnerability
            {
                Id = "private-key-17",
                Title = "private-key",
                Description = "Identified a Private Key, which may compromise cryptographic security and sensitive data encryption.",
                Severity = SeverityLevel.High,
                Scanner = ScannerType.Secrets,
                LineNumber = 18,
                ColumnNumber = 2,
                StartIndex = 1,
                EndIndex = 29,
                FilePath = path,
                SecretType = "Private Key"
            });

            // --- ASCA (SAST-style for same file) ---
            list.Add(new Vulnerability
            {
                Id = "ASCA-SECRETS-001",
                Title = "Hardcoded credential",
                Description = "Hardcoded credential detected; use a secrets manager or environment variables.",
                Severity = SeverityLevel.High,
                Scanner = ScannerType.ASCA,
                LineNumber = 11,
                ColumnNumber = 1,
                FilePath = path,
                RuleName = "HARDCODED_CREDENTIAL",
                RemediationAdvice = "Store secrets in a secure vault or environment variables."
            });
            list.Add(new Vulnerability
            {
                Id = "ASCA-SECRETS-002",
                Title = "Insecure deserialization",
                Description = "User input passed to deserialization may lead to code execution.",
                Severity = SeverityLevel.Critical,
                Scanner = ScannerType.ASCA,
                LineNumber = 14,
                ColumnNumber = 1,
                FilePath = path,
                RuleName = "INSECURE_DESERIALIZATION",
                RemediationAdvice = "Avoid deserializing untrusted data; use allowlists or safe formats."
            });

            return list;
        }

        /// <summary>
        /// Returns mock IaC (KICS) vulnerabilities for Docker compose / IaC files (e.g. negative1.yaml).
        /// Matches IaC realtime scan shape: ExpectedValue, ActualValue, SimilarityID, Locations.
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses "negative1.yaml".</param>
        public static List<Vulnerability> GetIacMockVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? "negative1.yaml" : filePath;

            return new List<Vulnerability>
            {
                new Vulnerability
                {
                    Id = "c24d49e3710af1b9fa880e09c3a46afb7455000cec909ff34660f83fb56e3883",
                    Title = "Container Traffic Not Bound To Host Interface",
                    Description = "Incoming container traffic should be bound to a specific host interface",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.IaC,
                    LineNumber = 10, // 1-based (IaC/KICS): line 10 in file (ports)
                    ColumnNumber = 5,
                    StartIndex = 4,
                    EndIndex = 10,
                    FilePath = path,
                    ExpectedValue = "Docker compose file to have 'ports' attribute bound to a specific host interface.",
                    ActualValue = "Docker compose file doesn't have 'ports' attribute bound to a specific host interface"
                },
                new Vulnerability
                {
                    Id = "c3d88e010e72fa55d0e40eee12ad066741421c4036e1cc9f409204b38de23abd",
                    Title = "Healthcheck Not Set",
                    Description = "Check containers periodically to see if they are running properly.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.IaC,
                    LineNumber = 4, // 1-based (IaC/KICS): line 3 in file (services:)
                    ColumnNumber = 3,
                    StartIndex = 2,
                    EndIndex = 9,
                    FilePath = path,
                    ExpectedValue = "Healthcheck should be defined.",
                    ActualValue = "Healthcheck is not defined."
                },
                new Vulnerability
                {
                    Id = "4022c1441ba03ca00c1ad057f5e3cfb25ed165cb6b94988276bacad0485d3b74",
                    Title = "Memory Not Limited",
                    Description = "Memory limits should be defined for each container. This prevents potential resource exhaustion by ensuring that containers consume not more than the designated amount of memory",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.IaC,
                    LineNumber = 4, // 1-based (IaC/KICS): line 3 in file
                    ColumnNumber = 3,
                    StartIndex = 2,
                    EndIndex = 9,
                    FilePath = path,
                    ExpectedValue = "'deploy.resources.limits.memory' should be defined",
                    ActualValue = "'deploy' is not defined"
                },
                new Vulnerability
                {
                    Id = "f39f133cdd646d7f46b746af74b20062a89e3a9b6c28706ca81b40527d247656",
                    Title = "Security Opt Not Set",
                    Description = "Attribute 'security_opt' should be defined.",
                    Severity = SeverityLevel.Medium,
                    Scanner = ScannerType.IaC,
                    LineNumber = 4, // 1-based (IaC/KICS): line 3 in file
                    ColumnNumber = 3,
                    StartIndex = 2,
                    EndIndex = 9,
                    FilePath = path,
                    ExpectedValue = "Docker compose file to have 'security_opt' attribute",
                    ActualValue = "Docker compose file does not have 'security_opt' attribute"
                },
                new Vulnerability
                {
                    Id = "b8b8fedf4bcebf05b64d29bc81378df312516e9211063c16fca4cbc5c3a3beac",
                    Title = "Cpus Not Limited",
                    Description = "CPU limits should be set because if the system has CPU time free, a container is guaranteed to be allocated as much CPU as it requests",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.IaC,
                    LineNumber = 4, // 1-based (IaC/KICS): line 3 in file
                    ColumnNumber = 3,
                    StartIndex = 2,
                    EndIndex = 9,
                    FilePath = path,
                    ExpectedValue = "'deploy.resources.limits.cpus' should be defined",
                    ActualValue = "'deploy' is not defined"
                }
            };
        }

        /// <summary>
        /// Returns mock Container image scan vulnerabilities for values.yaml (e.g. Helm chart referencing nginx:latest).
        /// Matches AST-CLI container scan result shape: ImageName, ImageTag, FilePath, Locations (Line, StartIndex, EndIndex), Status, Vulnerabilities (CVE, Severity).
        /// All CVEs share the same location (line 1, StartIndex 7, EndIndex 19) so they group in gutter/findings/Error List.
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses "values.yaml".</param>
        public static List<Vulnerability> GetContainerImageMockVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? "values.yaml" : filePath;
            const int lineNumber = 1;  // 1-based; scan had Line 0
            const int startIndex = 7;
            const int endIndex = 19;
            const string imageTitle = "nginx:latest";

            var cves = new[]
            {
                ("CVE-2011-3374", SeverityLevel.Low),
                ("TEMP-0841856-B18BAF", SeverityLevel.Unknown),
                ("CVE-2022-0563", SeverityLevel.Medium),
                ("CVE-2025-14104", SeverityLevel.Medium),
                ("CVE-2017-18018", SeverityLevel.High),
                ("CVE-2025-5278", SeverityLevel.Medium),
                ("CVE-2025-10966", SeverityLevel.Medium),
                ("CVE-2025-15224", SeverityLevel.Low),
                ("CVE-2025-15079", SeverityLevel.Medium),
                ("CVE-2025-14819", SeverityLevel.Medium),
                ("CVE-2025-14524", SeverityLevel.Medium),
                ("CVE-2025-14017", SeverityLevel.Medium),
                ("CVE-2025-13034", SeverityLevel.Medium),
                ("CVE-2018-20796", SeverityLevel.High),
                ("CVE-2019-1010025", SeverityLevel.Medium),
                ("CVE-2010-4756", SeverityLevel.Medium),
                ("CVE-2019-9192", SeverityLevel.High),
                ("CVE-2019-1010024", SeverityLevel.Medium),
                ("CVE-2019-1010023", SeverityLevel.Medium),
                ("CVE-2019-1010022", SeverityLevel.Critical),
                ("CVE-2026-0861", SeverityLevel.High),
                ("CVE-2026-0915", SeverityLevel.Unknown),
                ("CVE-2025-15281", SeverityLevel.High),
                ("CVE-2024-38950", SeverityLevel.Medium),
                ("CVE-2024-38949", SeverityLevel.Medium),
                ("CVE-2025-59375", SeverityLevel.High),
                ("CVE-2025-66382", SeverityLevel.Medium),
                ("CVE-2026-25210", SeverityLevel.Medium),
                ("CVE-2026-24515", SeverityLevel.Low),
                ("CVE-2018-6829", SeverityLevel.High),
                ("CVE-2024-2236", SeverityLevel.Medium),
                ("CVE-2025-14831", SeverityLevel.Medium),
                ("CVE-2011-3389", SeverityLevel.Medium),
                ("CVE-2018-5709", SeverityLevel.High),
                ("CVE-2024-26458", SeverityLevel.Medium),
                ("CVE-2024-26461", SeverityLevel.High),
                ("CVE-2025-68431", SeverityLevel.High),
                ("CVE-2017-17740", SeverityLevel.High),
                ("CVE-2015-3276", SeverityLevel.High),
                ("CVE-2017-14159", SeverityLevel.Medium),
                ("CVE-2020-15719", SeverityLevel.Medium),
                ("CVE-2026-22185", SeverityLevel.Medium),
                ("CVE-2021-4214", SeverityLevel.Medium),
                ("CVE-2025-64720", SeverityLevel.High),
                ("CVE-2025-64505", SeverityLevel.Medium),
                ("CVE-2025-66293", SeverityLevel.High),
                ("CVE-2025-65018", SeverityLevel.High),
                ("CVE-2025-64506", SeverityLevel.Medium),
                ("CVE-2021-45346", SeverityLevel.Medium),
                ("CVE-2025-7709", SeverityLevel.Medium),
                ("CVE-2013-4392", SeverityLevel.Medium),
                ("CVE-2023-31437", SeverityLevel.Medium),
                ("CVE-2023-31439", SeverityLevel.Medium),
                ("CVE-2023-31438", SeverityLevel.Medium),
                ("CVE-2025-13151", SeverityLevel.High),
                ("CVE-2025-6141", SeverityLevel.Medium),
                ("CVE-2025-8732", SeverityLevel.Medium),
                ("CVE-2026-1757", SeverityLevel.Medium),
                ("CVE-2026-0992", SeverityLevel.Low),
                ("CVE-2026-0990", SeverityLevel.Medium),
                ("CVE-2026-0989", SeverityLevel.Low),
                ("CVE-2024-56433", SeverityLevel.Low),
                ("TEMP-0628843-DBAD28", SeverityLevel.Unknown),
                ("CVE-2007-5686", SeverityLevel.Medium),
                ("CVE-2026-1642", SeverityLevel.High),
                ("CVE-2009-4487", SeverityLevel.Medium),
                ("CVE-2013-0337", SeverityLevel.High),
                ("CVE-2011-4116", SeverityLevel.Low),
                ("TEMP-0517018-A83CE6", SeverityLevel.Unknown),
                ("TEMP-0290435-0B57B5", SeverityLevel.Unknown),
                ("CVE-2005-2541", SeverityLevel.Critical),
                ("CVE-2026-3184", SeverityLevel.Medium)
            };

            var list = new List<Vulnerability>(cves.Length);
            foreach (var (cve, severity) in cves)
            {
                list.Add(new Vulnerability
                {
                    Id = cve,
                    Title = imageTitle,
                    Description = $"Container image vulnerability: {cve}.",
                    Severity = severity,
                    Scanner = ScannerType.Containers,
                    LineNumber = lineNumber,
                    ColumnNumber = 1,
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    FilePath = path,
                    CveName = cve
                });
            }
            return list;
        }

        /// <summary>
        /// Returns mock Container (Dockerfile) vulnerabilities.
        /// Matches Container realtime scan shape: ExpectedValue, ActualValue, SimilarityID, Locations.
        /// Includes Status=OK (success gutter icon) and Status=Unknown (unknown icon) per reference ContainerScanResultAdaptor getStatus().
        /// </summary>
        /// <param name="filePath">Optional file path; if null or empty, uses "Dockerfile".</param>
        public static List<Vulnerability> GetContainerMockVulnerabilities(string filePath = null)
        {
            var path = string.IsNullOrEmpty(filePath) ? "Dockerfile" : filePath;

            return new List<Vulnerability>
            {
                // Container status Unknown (gutter icon only; no underline, problem window, Error List, popup)
                new Vulnerability
                {
                    Id = "container-unknown-base",
                    Title = "Base image (Unknown)",
                    Description = "Container image status unknown.",
                    Severity = SeverityLevel.Unknown,
                    Scanner = ScannerType.Containers,
                    LineNumber = 3,
                    ColumnNumber = 1,
                    FilePath = path
                },
                // Container status OK (gutter icon only)
                new Vulnerability
                {
                    Id = "container-ok-stage",
                    Title = "Build stage (No vulnerabilities)",
                    Description = "No vulnerabilities found in this stage.",
                    Severity = SeverityLevel.Unknown,
                    Scanner = ScannerType.Containers,
                    LineNumber = 7,
                    ColumnNumber = 1,
                    FilePath = path
                },
                new Vulnerability
                {
                    Id = "6f55673a2f4c0138b0a85c9aa5b175823a01b84aba6db7f368cfd5e4f24c563c",
                    Title = "Missing User Instruction",
                    Description = "A user should be specified in the dockerfile, otherwise the image will run as root",
                    Severity = SeverityLevel.High,
                    Scanner = ScannerType.Containers,
                    LineNumber = 9,
                    ColumnNumber = 1,
                    StartIndex = 0,
                    EndIndex = 19,
                    FilePath = path,
                    ExpectedValue = "The 'Dockerfile' should contain the 'USER' instruction",
                    ActualValue = "The 'Dockerfile' does not contain any 'USER' instruction"
                },
                new Vulnerability
                {
                    Id = "873ed998215f2ded3e3edadb334b918b72a6ac129df8ef95a3ce20913ed04898",
                    Title = "Healthcheck Instruction Missing",
                    Description = "Ensure that HEALTHCHECK is being used. The HEALTHCHECK instruction tells Docker how to test a container to check that it is still working",
                    Severity = SeverityLevel.Low,
                    Scanner = ScannerType.Containers,
                    LineNumber = 9,
                    ColumnNumber = 1,
                    StartIndex = 0,
                    EndIndex = 19,
                    FilePath = path,
                    ExpectedValue = "Dockerfile should contain instruction 'HEALTHCHECK'",
                    ActualValue = "Dockerfile doesn't contain instruction 'HEALTHCHECK'"
                }
            };
        }

    }
}
