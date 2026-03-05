using System;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts
{
    /// <summary>
    /// Builds remediation prompts for "Fix with Checkmarx One Assist" (aligned with JetBrains CxOneAssistFixPrompts).
    /// Used to generate a prompt that is sent to GitHub Copilot for automated remediation.
    /// </summary>
    internal static class CxOneAssistFixPrompts
    {
        private const string AgentName = "Checkmarx One Assist";

        public static string BuildForVulnerability(Vulnerability v)
        {
            if (v == null) return null;
            switch (v.Scanner)
            {
                case ScannerType.OSS:
                    return BuildSCARemediationPrompt(
                        v.PackageName ?? v.Title ?? "",
                        v.PackageVersion ?? "",
                        v.PackageManager ?? "npm",
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                case ScannerType.Secrets:
                    return BuildSecretRemediationPrompt(
                        v.Title ?? v.Description ?? "",
                        v.Description,
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                case ScannerType.Containers:
                    return BuildContainersRemediationPrompt(
                        GetFileType(v.FilePath),
                        v.Title ?? v.PackageName ?? "image",
                        v.PackageVersion ?? "latest",
                        CxAssistConstants.GetRichSeverityName(v.Severity));
                case ScannerType.IaC:
                    return BuildIACRemediationPrompt(
                        v.Title ?? v.RuleName ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        GetFileType(v.FilePath),
                        v.ExpectedValue ?? "",
                        v.ActualValue ?? "",
                        v.LineNumber > 0 ? v.LineNumber - 1 : (int?)null);
                case ScannerType.ASCA:
                    return BuildASCARemediationPrompt(
                        v.RuleName ?? v.Title ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        v.RemediationAdvice ?? "",
                        v.LineNumber > 0 ? v.LineNumber - 1 : (int?)null);
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

        public static string BuildSCARemediationPrompt(string packageName, string packageVersion, string packageManager, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("You are the ").Append(AgentName).Append(".\n\n")
                .Append("A security issue has been detected in `").Append(packageName).Append("@").Append(packageVersion).Append("` (package manager: `").Append(packageManager).Append("`).\n")
                .Append("**Severity:** `").Append(severity).Append("`\n")
                .Append("Your task is to remediate the issue **completely and autonomously** using the internal PackageRemediation tool in Checkmarx MCP. Follow the exact instructions in `fix_instructions` - no assumptions or manual interaction allowed.\n\n");
            sb.Append("Step 1. ANALYSIS (AUTOMATED):\n\n")
                .Append("Determine the issue type:\n")
                .Append("- If `status` is one of: `Critical`, `High`, `Medium`, `Low`, `Info`, set: `issueType = \"CVE\"`\n")
                .Append("- If `status = \"Malicious\"`, set: `issueType = \"malicious\"`\n\n")
                .Append("Call the internal PackageRemediation tool with:\n\n")
                .Append("```json\n{\n  \"packageName\": \"").Append(packageName).Append("\",\n  \"packageVersion\": \"").Append(packageVersion).Append("\",\n  \"packageManager\": \"").Append(packageManager).Append("\",\n  \"issueType\": \"{determined issueType}\"\n}\n```\n\n");
            sb.Append("Parse the response and extract the `fix_instructions` field. Then execute each line in order. Track modified files, note changes, and run verification (build/test).\n\n");
            sb.Append("Step 2. OUTPUT: Prefix all output with: `").Append(AgentName).Append(" -`\n\n");
            sb.Append("✓ **Remediation Summary**\n\nIf all tasks succeeded: \"Remediation completed for ").Append(packageName).Append("@").Append(packageVersion).Append("\". If failed: \"Remediation failed for ").Append(packageName).Append("@").Append(packageVersion).Append("\".\n\n");
            sb.Append("CONSTRAINTS: Do not prompt the user. Only execute what's in `fix_instructions`. Insert TODO comments for unresolved issues.\n");
            return sb.ToString();
        }

        public static string BuildSecretRemediationPrompt(string title, string description, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("A secret has been detected: \"").Append(title).Append("\"  \n").Append(description ?? "").Append("\n\n");
            sb.Append("You are the `").Append(AgentName).Append("`.\n\n")
                .Append("Your mission is to identify and remediate this secret using secure coding standards. Follow industry best practices, automate safely, and clearly document all actions taken.\n\n");
            sb.Append("Step 1. SEVERITY: `").Append(severity ?? "").Append("` — Critical: valid secret, immediate remediation. High: treat as sensitive. Medium: likely invalid, still remove.\n\n");
            sb.Append("Step 2. Call the internal `codeRemediation` Checkmarx MCP tool with type \"secret\", sub_type \"").Append(title).Append("\". Apply remediation_steps. Replace secret with environment variable or vault reference.\n\n");
            sb.Append("Step 3. OUTPUT: Prefix with `").Append(AgentName).Append(" -`. Provide Secret Remediation Summary, Files Modified, Remediation Actions Taken, Next Steps, Best Practices. CONSTRAINTS: Do NOT expose real secrets. Follow MCP response.\n");
            return sb.ToString();
        }

        public static string BuildContainersRemediationPrompt(string fileType, string imageName, string imageTag, string severity)
        {
            var sb = new StringBuilder();
            sb.Append("You are the ").Append(AgentName).Append(".\n\n")
                .Append("A container security issue has been detected in `").Append(fileType).Append("` with image `").Append(imageName).Append(":").Append(imageTag).Append("`.\n")
                .Append("**Severity:** `").Append(severity).Append("`\n")
                .Append("Your task is to remediate using the internal imageRemediation tool. Follow `fix_instructions` exactly.\n\n");
            sb.Append("Step 1. Call imageRemediation with fileType, imageName, imageTag, severity. Parse `fix_instructions`.\n\n");
            sb.Append("Step 2. Execute each line. Track modified files (Dockerfile, docker-compose, values.yaml, etc.).\n\n");
            sb.Append("Step 3. OUTPUT: Prefix with `").Append(AgentName).Append(" -`. Remediation Summary. If succeeded: \"Remediation completed for ").Append(imageName).Append(":").Append(imageTag).Append("\". CONSTRAINTS: Do not prompt user. Follow fix_instructions only.\n");
            return sb.ToString();
        }

        public static string BuildIACRemediationPrompt(string title, string description, string severity, string fileType, string expectedValue, string actualValue, int? problematicLineNumber)
        {
            var lineNum = problematicLineNumber.HasValue ? (problematicLineNumber.Value).ToString() : "[unknown]";
            var sb = new StringBuilder();
            sb.Append("You are the ").Append(AgentName).Append(".\n\n");
            sb.Append("An IaC security issue has been detected.\n\n**Issue:** `").Append(title).Append("`\n**Severity:** `").Append(severity).Append("`\n**File Type:** `").Append(fileType).Append("`\n**Description:** ").Append(description).Append("\n**Expected Value:** ").Append(expectedValue).Append("\n**Actual Value:** ").Append(actualValue).Append("\n**Problematic Line:** ").Append(lineNum).Append("\n\n");
            sb.Append("Your task is to remediate using the internal codeRemediation tool (type \"iac\"). Apply **only** to the code at line ").Append(lineNum).Append(".\n\n");
            sb.Append("Step 1. Call codeRemediation with type \"iac\", metadata title/description/remediationAdvice. Step 2. Execute remediation_steps in order. Step 3. OUTPUT: Prefix with `").Append(AgentName).Append(" -`. Summary. CONSTRAINTS: Only modify the problematic line segment.\n");
            return sb.ToString();
        }

        public static string BuildASCARemediationPrompt(string ruleName, string description, string severity, string remediationAdvice, int? problematicLineNumber)
        {
            var lineNum = problematicLineNumber.HasValue ? (problematicLineNumber.Value).ToString() : "[unknown]";
            var sb = new StringBuilder();
            sb.Append("You are the ").Append(AgentName).Append(".\n\n")
                .Append("A secure coding issue has been detected.\n\n**Rule:** `").Append(ruleName).Append("`  \n**Severity:** `").Append(severity).Append("`  \n**Description:** ").Append(description).Append("  \n**Recommended Fix:** ").Append(remediationAdvice).Append("  \n**Problematic Line:** ").Append(lineNum).Append("\n\n");
            sb.Append("Remediate using the internal codeRemediation tool (type \"sast\"). Apply fix **only** to the code at line ").Append(lineNum).Append(".\n\n");
            sb.Append("Step 1. Call codeRemediation with type \"sast\", metadata ruleID/description/remediationAdvice. Step 2. Execute remediation_steps. Step 3. OUTPUT: Prefix with `").Append(AgentName).Append(" -`. Remediation Summary. CONSTRAINTS: Only modify the identified line segment.\n");
            return sb.ToString();
        }
    }
}
