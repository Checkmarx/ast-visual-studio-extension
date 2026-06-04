using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts
{
    /// <summary>
    /// Builds explanation prompts for "View details".
    /// Aligned with VSCode extension prompts.ts for consistent prompt generation across IDEs.
    /// </summary>
    internal static class ViewDetailsPrompts
    {
        private const string AgentName = "Checkmarx One Assist";
        private const string ProductName = "Checkmarx";

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
                        v.ActualValue ?? "",
                        sameLineVulns ?? new[] { v });
                case ScannerType.ASCA:
                    return BuildASCAExplanationPrompt(
                        v.RuleName ?? v.Title ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        sameLineVulns ?? new[] { v });
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
            bool isMalicious = string.Equals(status, "Malicious", StringComparison.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.Append($@"You are the `{AgentName}`.

Your task is to **analyze and explain** the security issue affecting the package `{packageName}@{version}` with status: `{status}`.


### ❗ Important Instructions:
- 👉 **Do not change anything in the code. Just explain the risks and remediation steps.**
- ❌ **Never include references from {ProductName} competitors.**


### 🔍 Package Overview

- **Package:** `{packageName}`
- **Version:** `{version}`
- **Status:** `{status}`
");

            if (isMalicious)
            {
                sb.Append($@"

### 🧨 Malicious Package Detected

This package has been flagged as **malicious**.

**⚠️ Never install or use this package under any circumstances.**

#### 🔎 Web Investigation:

- Search the web for trusted community or vendor reports about malicious activity involving this package.
- If information exists about other versions but **not** version `{version}`, explicitly say:

> _""This specific version (`{version}`) was identified as malicious by {ProductName} Security researchers.""_

- If **no credible external information is found at all**, state:

> _""This package was identified as malicious by {ProductName} Security researchers based on internal threat intelligence and behavioral analysis.""_

Then explain:
- What types of malicious behavior these packages typically include (e.g., data exfiltration, postinstall backdoors)
- Indicators of compromise developers should look for (e.g., suspicious scripts, obfuscation, DNS calls)

**Recommended Actions:**
- ✅ Immediately remove from all codebases and pipelines
- ❌ Never reinstall or trust any version of this package
- 🔁 Replace with a well-known, secure alternative
- 🔒 Consider running a retrospective security scan if this was installed

");
            }
            else
            {
                sb.Append($@"

### 🚨 Known Vulnerabilities

Explain each known CVE affecting this package:
");

                if (vulnerabilities != null && vulnerabilities.Count > 0)
                {
                    int index = 1;
                    foreach (var vuln in vulnerabilities.Take(20))
                    {
                        sb.Append($@"
#### {index}. {vuln.CveName ?? vuln.Id ?? "CVE"}
- **Severity:** {CxAssistConstants.GetRichSeverityName(vuln.Severity)}
- **Description:** {vuln.Description ?? ""}
");
                        index++;
                    }
                }
                else
                {
                    sb.Append($@"
⚠️ No CVEs were provided. Please verify if this is expected for status `{status}`.
");
                }
            }

            sb.Append($@"

### 🛠️ Remediation Guidance

Offer actionable advice:
- Whether to remove, upgrade, or replace the package
- If malicious: clearly emphasize permanent removal
- Recommend safer, verified alternatives if available
- Suggest preventative measures:
  - Use SCA in CI/CD
  - Prefer signed packages
  - Pin versions to prevent shadow updates


### ✅ Summary Section

Conclude with:
- Overall risk explanation
- Immediate remediation steps
- Whether this specific version is linked to online reports
- If not, reference {ProductName} attribution (per above rules)
- Never mention competitor vendors or tools


### ✏️ Output Formatting

- Use Markdown: `##`, `- `, `**bold**`, `code`
- Developer-friendly tone, informative, concise
- No speculation — use only trusted, verified sources

");
            return sb.ToString();
        }

        public static string BuildSecretsExplanationPrompt(string title, string description, string severity)
        {
            return $@"You are the `{AgentName}`.

A potential secret has been detected: **""{title}""**  
Severity: **{severity}**


### ❗ Important Instruction:
👉 **Do not change any code. Just explain the risk, validation level, and recommended actions.**


### 🔍 Secret Overview

- **Secret Name:** `{title}`
- **Severity Level:** `{severity}`
- **Details:** {description ?? ""}


### 🧠 Risk Understanding Based on Severity

- **Critical**:  
  The secret was **validated as active**. It is likely in use and can be exploited immediately if exposed.

- **High**:  
  The validation status is **unknown**. The secret may or may not be valid. Proceed with caution and treat it as potentially live.

- **Medium**:  
  The secret was identified as **invalid** or **mock/test value**. While not active, it may confuse developers or be reused insecurely.


### 🔐 Why This Matters

Hardcoded secrets pose a serious risk:
- **Leakage** through public repositories or logs
- **Unauthorized access** to APIs, cloud providers, or infrastructure
- **Exploitation** via replay attacks, privilege escalation, or lateral movement


### ✅ Recommended Remediation Steps (for developer action)

- Rotate the secret if it's live (Critical/High)
- Move secrets to environment variables or secret managers
- Audit the commit history to ensure it hasn't leaked publicly
- Implement secret scanning in your CI/CD pipelines
- Document safe handling procedures in your repo


### 📋 Next Steps Checklist (Markdown)

```markdown
### Next Steps:
- [ ] Rotate the exposed secret if valid
- [ ] Move secret to secure storage (.env or secret manager)
- [ ] Clean secret from commit history if leaked
- [ ] Annotate clearly if it's a fake or mock value
- [ ] Implement CI/CD secret scanning and policies
```


### ✏️ Output Format Guidelines

- Use Markdown with clear sections
- Do not attempt to edit or redact the code
- Be factual, concise, and helpful
- Assume this is shown to a developer unfamiliar with security tooling

";
        }

        public static string BuildContainersExplanationPrompt(string fileType, string imageName, string imageTag, string severity)
        {
            bool isMalicious = string.Equals(severity, "Malicious", StringComparison.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.Append($@"You are the `{AgentName}`.

Your task is to **analyze and explain** the container security issue affecting `{fileType}` with image `{imageName}:{imageTag}` and severity: `{severity}`.


###  Important Instructions:
-  **Do not change anything in the code. Just explain the risks and remediation steps.**
-  **Never include references from {ProductName} competitors.**


### 🔍 Container Overview

- **File Type:** `{fileType}`
- **Image:** `{imageName}:{imageTag}`
- **Severity:** `{severity}`


### 🐳 Container Security Issue Analysis

**Issue Type:** {(isMalicious ? "Malicious Container Image" : "Vulnerable Container Image")}

");

            if (isMalicious)
            {
                sb.Append($@"### 🧨 Malicious Container Detected

This container image has been flagged as **malicious**.

**⚠️ Never deploy or use this container under any circumstances.**

#### 🔎 Investigation Guidelines:

- Search for trusted community or vendor reports about malicious activity involving this image
- If information exists about other tags but **not** tag `{imageTag}`, explicitly state:

> _""This specific tag (`{imageTag}`) was identified as malicious by {ProductName} Security researchers.""_

- If **no credible external information is found**, state:

> _""This container image was identified as malicious by {ProductName} Security researchers based on internal threat intelligence and behavioral analysis.""_

**Common Malicious Container Behaviors:**
- Data exfiltration to external servers
- Cryptocurrency mining operations
- Backdoor access establishment
- Credential harvesting
- Lateral movement within infrastructure

**Recommended Actions:**
- ✅ Immediately remove from all deployment pipelines
- ❌ Never redeploy or trust any version of this image
- 🔁 Replace with a well-known, secure alternative
- 🔒 Audit all systems that may have run this container

");
            }
            else
            {
                sb.Append(@"### 🚨 Container Vulnerabilities

This container image contains known security vulnerabilities.

**Risk Assessment:**
- **Critical/High:** Immediate action required - vulnerable to active exploitation
- **Medium:** Should be addressed soon - potential for exploitation
- **Low:** Address when convenient - limited immediate risk

**Common Container Security Issues:**
- Outdated base images with known CVEs
- Unnecessary packages and services
- Running as root user
- Missing security patches
- Insecure default configurations

");
            }

            sb.Append($@"

### 🛠️ Remediation Guidance

Offer actionable advice:
- Whether to update, replace, or rebuild the container
- If malicious: clearly emphasize permanent removal
- Recommend secure base images and best practices
- Suggest preventative measures:
  - Use container scanning in CI/CD
  - Prefer minimal base images (Alpine, distroless)
  - Implement image signing and verification
  - Regular security updates and patching
  - Run containers as non-root users
  - Use multi-stage builds to reduce attack surface


### ✅ Summary Section

Conclude with:
- Overall risk explanation for container deployments
- Immediate remediation steps
- Whether this specific image/tag is linked to online reports
- If not, reference {ProductName} attribution (per above rules)
- Never mention competitor vendors or tools


### Output Formatting

- Use Markdown: `##`, `- `, `**bold**`, `code`
- Developer-friendly tone, informative, concise
- No speculation — use only trusted, verified sources
- Include container-specific terminology and best practices

");
            return sb.ToString();
        }

        public static string BuildIACExplanationPrompt(string title, string description, string severity, string fileType, string expectedValue, string actualValue, IReadOnlyList<Vulnerability> vulnerabilities = null)
        {
            var sb = new StringBuilder();
            sb.Append($@"You are the `{AgentName}`.

");

            if (vulnerabilities != null && vulnerabilities.Count > 1)
            {
                sb.Append($@"Your task is to **analyze and explain** the **{vulnerabilities.Count} Infrastructure as Code (IaC) security issues** detected on this line in a `{fileType}` file.


### ❗ Important Instructions:
- 👉 **Do not change anything in the configuration. Just explain the risks and remediation steps.**
- ❌ **Never include references from {ProductName} competitors.**


### 🔍 IaC Security Issues Overview

Explain each IaC issue detected:
");
                int index = 1;
                foreach (var vuln in vulnerabilities.Take(20))
                {
                    sb.Append($@"
#### {index}. {vuln.Title ?? vuln.RuleName ?? "IaC Issue"}
- **Severity:** {CxAssistConstants.GetRichSeverityName(vuln.Severity)}
- **Description:** {vuln.Description ?? ""}
- **Expected Value:** `{vuln.ExpectedValue ?? ""}`
- **Actual Value:** `{vuln.ActualValue ?? ""}`
");
                    index++;
                }
                sb.Append($@"
- **File Type:** `{fileType}`
");
            }
            else
            {
                sb.Append($@"Your task is to **analyze and explain** the Infrastructure as Code (IaC) security issue: **{title}** with severity: `{severity}`.


### ❗ Important Instructions:
- 👉 **Do not change anything in the configuration. Just explain the risks and remediation steps.**
- ❌ **Never include references from {ProductName} competitors.**


### 🔍 IaC Security Issue Overview

- **Issue:** `{title}`
- **File Type:** `{fileType}`
- **Severity:** `{severity}`
- **Description:** {description}
- **Expected Value:** `{expectedValue}`
- **Actual Value:** `{actualValue}`
");
            }

            sb.Append($@"

### 🏗️ Infrastructure Security Issue Analysis

**Issue Type:** Infrastructure Configuration Vulnerability


### 🚨 Security Risks

This configuration issue can lead to:
- **Critical/High:** Immediate security exposure - vulnerable to active exploitation
- **Medium:** Potential security risk - should be addressed soon
- **Low:** Security hygiene - address when convenient

**Common IaC Security Issues:**
- Overly permissive access controls
- Exposed sensitive data or credentials
- Insecure network configurations
- Missing encryption settings
- Unrestricted public access
- Insecure service configurations


### 🛠️ Remediation Guidance

Offer actionable advice based on the file type:

**For {fileType} configurations:**
- Specific configuration changes needed
- Security best practices to follow
- Compliance considerations
- Testing and validation steps

**Preventative Measures:**
- Use IaC security scanning in CI/CD pipelines
- Implement infrastructure policy as code
- Regular security audits of infrastructure
- Follow cloud provider security guidelines
- Use secure configuration templates


### ✅ Summary Section

Conclude with:
- Overall risk explanation for infrastructure security
- Immediate remediation steps
- Impact on system security posture
- Long-term security considerations


### ✏️ Output Formatting

- Use Markdown: `##`, `- `, `**bold**`, `code`
- Infrastructure-focused tone, informative, concise
- No speculation — use only trusted, verified sources
- Include infrastructure-specific terminology and best practices

");
            return sb.ToString();
        }

        public static string BuildASCAExplanationPrompt(string ruleName, string description, string severity, IReadOnlyList<Vulnerability> vulnerabilities = null)
        {
            if (vulnerabilities != null && vulnerabilities.Count > 1)
            {
                var sb = new StringBuilder();
                sb.Append($@"You are the {AgentName} providing detailed security explanations.

**{vulnerabilities.Count} ASCA violations** have been detected on this line.

");
                int index = 1;
                foreach (var vuln in vulnerabilities.Take(20))
                {
                    sb.Append($@"#### {index}. {vuln.RuleName ?? vuln.Title ?? "ASCA Violation"}
- **Severity:** {CxAssistConstants.GetRichSeverityName(vuln.Severity)}
- **Description:** {vuln.Description ?? ""}

");
                    index++;
                }
                sb.Append($@"Please provide a comprehensive explanation of each security issue listed above.


### 🔍 Security Issues Overview

**Total Issues:** {vulnerabilities.Count}
**Highest Risk Level:** {severity}


### 📖 Detailed Explanation

For each issue listed above, explain:
- What the vulnerability means
- Why it's dangerous in context


### ⚠️ Why This Matters

Explain the potential security implications:
- What attacks could exploit these vulnerabilities?
- What data or systems could be compromised?
- What is the potential business impact?


### 🛡️ Security Best Practices

Provide general guidance on:
- How to prevent these types of issues
- Coding patterns to avoid
- Secure alternatives to recommend
- Tools and techniques for detection


### 📚 Additional Resources

Suggest relevant:
- Security frameworks and standards
- Documentation and guides
- Tools for static analysis
- Training materials


### ✏️ Output Format Guidelines

- Use clear, educational language
- Provide context for non-security experts
- Include practical examples where helpful
- Focus on actionable advice
- Be thorough but concise
");
                return sb.ToString();
            }

            return $@"You are the {AgentName} providing detailed security explanations.

**Rule:** `{ruleName}`  
**Severity:** `{severity}`  
**Description:** {description}

Please provide a comprehensive explanation of this security issue.


### 🔍 Security Issue Overview

**Rule Name:** {ruleName}
**Risk Level:** {severity}


### 📖 Detailed Explanation

{description}


### ⚠️ Why This Matters

Explain the potential security implications:
- What attacks could exploit this vulnerability?
- What data or systems could be compromised?
- What is the potential business impact?


### 🛡️ Security Best Practices

Provide general guidance on:
- How to prevent this type of issue
- Coding patterns to avoid
- Secure alternatives to recommend
- Tools and techniques for detection


### 📚 Additional Resources

Suggest relevant:
- Security frameworks and standards
- Documentation and guides
- Tools for static analysis
- Training materials


### ✏️ Output Format Guidelines

- Use clear, educational language
- Provide context for non-security experts
- Include practical examples where helpful
- Focus on actionable advice
- Be thorough but concise
";
        }
    }
}
