using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts
{
    /// <summary>
    /// Builds explanation prompts for "View details" (aligned with JetBrains ViewDetailsPrompts).
    /// Used to generate a prompt sent to GitHub Copilot to explain the finding without changing code.
    /// </summary>
    internal static class ViewDetailsPrompts
    {
        private const string AgentName = "Checkmarx One Assist";

        /// <summary>Builds View Details prompt for the given vulnerability. For OSS, pass all vulns on same line for CVE list if available.</summary>
        public static string BuildForVulnerability(Vulnerability v, IReadOnlyList<Vulnerability> sameLineVulns = null)
        {
            if (v == null) return null;
            switch (v.Scanner)
            {
                case ScannerType.OSS:
                    return BuildSCAExplanationPrompt(
                        v.PackageName ?? v.Title ?? "",
                        v.PackageVersion ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        sameLineVulns ?? new[] { v });
                case ScannerType.Secrets:
                    return BuildSecretsExplanationPrompt(
                        v.Title ?? v.Description ?? "",
                        v.Description,
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                case ScannerType.Containers:
                    return BuildContainersExplanationPrompt(
                        GetFileType(v.FilePath),
                        v.Title ?? v.PackageName ?? "image",
                        v.PackageVersion ?? "latest",
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                case ScannerType.IaC:
                    return BuildIACExplanationPrompt(
                        v.Title ?? v.RuleName ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        GetFileType(v.FilePath),
                        v.ExpectedValue ?? "",
                        v.ActualValue ?? "");
                case ScannerType.ASCA:
                    return BuildASCAExplanationPrompt(
                        v.RuleName ?? v.Title ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                default:
                    return null;
            }
        }

        private static string GetFileType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return "Unknown";
            var ext = System.IO.Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            return string.IsNullOrEmpty(ext) ? "Unknown" : ext;
        }

        public static string BuildSCAExplanationPrompt(string packageName, string version, string status, IReadOnlyList<Vulnerability> vulnerabilities)
        {
            var sb = new StringBuilder();
            sb.Append("You are the `").Append(AgentName).Append("`.\n\n")
                .Append("Your task is to **analyze and explain** the security issue affecting the package `").Append(packageName).Append("@").Append(version).Append("` with status: `").Append(status).Append("`.\n\n");
            sb.Append("### Important Instructions:\n- **Do not change anything in the code. Just explain the risks and remediation steps.**\n- **Never include references from Checkmarx competitors.**\n\n");
            sb.Append("### Package Overview\n\n- **Package:** `").Append(packageName).Append("`\n- **Version:** `").Append(version).Append("`\n- **Status:** `").Append(status).Append("`\n\n");
            if (string.Equals(status, "Malicious", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("### Malicious Package Detected\n\nThis package has been flagged as **malicious**. **Never install or use this package.** Explain what malicious packages typically do and recommend immediate removal and replacement.\n\n");
            }
            else
            {
                sb.Append("### Known Vulnerabilities\n\n");
                if (vulnerabilities != null && vulnerabilities.Count > 0)
                {
                    foreach (var vuln in vulnerabilities.Take(20))
                        sb.Append("- ").Append(vuln.CveName ?? vuln.Id ?? "CVE").Append(" — ").Append(CxAssistConstants.GetRichSeverityName(vuln.Severity)).Append(": ").Append(vuln.Description ?? "").Append("\n");
                }
                else
                {
                    sb.Append("No CVEs were provided. Verify if this is expected for status `").Append(status).Append("`.\n\n");
                }
            }
            sb.Append("### Remediation Guidance\n\nOffer actionable advice: remove, upgrade, or replace the package; recommend safer alternatives; suggest SCA in CI/CD and version pinning.\n\n");
            sb.Append("### Summary\n\nConclude with overall risk, immediate remediation steps, and output in Markdown (developer-friendly, concise).\n");
            return sb.ToString();
        }

        public static string BuildSecretsExplanationPrompt(string title, string description, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("You are the `").Append(AgentName).Append("`.\n\n")
                .Append("A potential secret has been detected: **\"").Append(title).Append("\"**  \nSeverity: **").Append(severity).Append("**\n\n");
            sb.Append("### Important Instruction:\n**Do not change any code. Just explain the risk, validation level, and recommended actions.**\n\n");
            sb.Append("### Secret Overview\n\n- **Secret Name:** `").Append(title).Append("`\n- **Severity:** `").Append(severity).Append("`\n- **Details:** ").Append(description ?? "").Append("\n\n");
            sb.Append("### Risk by Severity\n- **Critical**: Validated as active; immediate remediation.\n- **High**: Unknown validation; treat as potentially live.\n- **Medium**: Likely invalid/mock; still remove.\n\n");
            sb.Append("### Why This Matters\nHardcoded secrets risk leakage, unauthorized access, exploitation. Recommend: rotate if live, move to env/vault, audit history, add secret scanning in CI/CD.\n\n");
            sb.Append("### Output\nUse Markdown. Be factual and helpful. Do not edit code.\n");
            return sb.ToString();
        }

        public static string BuildContainersExplanationPrompt(string fileType, string imageName, string imageTag, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("You are the `").Append(AgentName).Append("`.\n\n")
                .Append("Your task is to **analyze and explain** the container security issue affecting `").Append(fileType).Append("` with image `").Append(imageName).Append(":").Append(imageTag).Append("` and severity: `").Append(severity).Append("`.\n\n");
            sb.Append("### Important Instructions:\n**Do not change anything in the code. Just explain the risks and remediation steps.**\n\n");
            sb.Append("### Container Overview\n- **File Type:** `").Append(fileType).Append("`\n- **Image:** `").Append(imageName).Append(":").Append(imageTag).Append("`\n- **Severity:** `").Append(severity).Append("`\n\n");
            sb.Append("Explain container security issues (outdated base images, CVEs, root user, missing patches). Offer remediation guidance and summary in Markdown.\n");
            return sb.ToString();
        }

        public static string BuildIACExplanationPrompt(string title, string description, string severity, string fileType, string expectedValue, string actualValue)
        {
            var sb = new StringBuilder();
            sb.Append("You are the `").Append(AgentName).Append("`.\n\n")
                .Append("Your task is to **analyze and explain** the IaC security issue: **").Append(title).Append("** with severity: `").Append(severity).Append("`.\n\n");
            sb.Append("### Important Instructions:\n**Do not change configuration. Just explain risks and remediation.**\n\n");
            sb.Append("### IaC Overview\n- **Issue:** `").Append(title).Append("`\n- **File Type:** `").Append(fileType).Append("`\n- **Severity:** `").Append(severity).Append("`\n- **Description:** ").Append(description).Append("\n- **Expected:** `").Append(expectedValue).Append("`\n- **Actual:** `").Append(actualValue).Append("`\n\n");
            sb.Append("Explain security risks (overly permissive access, exposed credentials, insecure config). Offer remediation and preventative measures. Output in Markdown.\n");
            return sb.ToString();
        }

        public static string BuildASCAExplanationPrompt(string ruleName, string description, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("You are the ").Append(AgentName).Append(" providing detailed security explanations.\n\n")
                .Append("**Rule:** `").Append(ruleName).Append("`  \n**Severity:** `").Append(severity).Append("`  \n**Description:** ").Append(description).Append("\n\n");
            sb.Append("Provide a comprehensive explanation: security issue overview, why it matters (attacks, impact), best practices, secure alternatives, and additional resources. Use clear Markdown.\n");
            return sb.ToString();
        }
    }
}
