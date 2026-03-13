using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ast_visual_studio_extension.CxExtension.CxAssist.Core;
using ast_visual_studio_extension.CxExtension.CxAssist.Core.Models;

namespace ast_visual_studio_extension.CxExtension.CxAssist.Core.Prompts
{
    /// <summary>
    /// Builds remediation prompts for "Fix with Checkmarx One Assist".
    /// Aligned with VSCode extension prompts.ts for consistent prompt generation across IDEs.
    /// </summary>
    internal static class CxOneAssistFixPrompts
    {
        private const string AgentName = "Checkmarx One Assist";
        private const string ProductName = "Checkmarx";

        public static string BuildForVulnerability(Vulnerability v, IReadOnlyList<Vulnerability> sameLineVulns = null)
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
                        v.LineNumber > 0 ? v.LineNumber - 1 : (int?)null,
                        sameLineVulns ?? new[] { v });
                case ScannerType.ASCA:
                    return BuildASCARemediationPrompt(
                        v.RuleName ?? v.Title ?? "",
                        v.Description ?? "",
                        CxAssistConstants.GetRichSeverityName(v.Severity),
                        v.RemediationAdvice ?? "",
                        v.LineNumber > 0 ? v.LineNumber - 1 : (int?)null,
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

        public static string BuildSCARemediationPrompt(string packageName, string packageVersion, string packageManager, string severity)
        {
            return $@"You are the {AgentName}.

A security issue has been detected in `{packageName}@{packageVersion}` (package manager: `{packageManager}`).
**Severity:** `{severity}`
Your task is to remediate the issue **completely and autonomously** using the internal PackageRemediation tool in {ProductName} MCP. Follow the exact instructions in `fix_instructions` — no assumptions or manual interaction allowed.

---

Step 1. ANALYSIS (AUTOMATED):

Determine the issue type:
- If `status` is one of: `Critical`, `High`, `Medium`, `Low`, `Info`, set: `issueType = ""CVE""`
- If `status = ""Malicious""`, set: `issueType = ""malicious""`

Call the internal PackageRemediation tool with:

```json
{{
  ""packageName"": ""{packageName}"",
  ""packageVersion"": ""{packageVersion}"",
  ""packageManager"": ""{packageManager}"",
  ""issueType"": ""{{determined issueType}}""
}}
```

Parse the response and extract the `fix_instructions` field. This field contains the authoritative remediation steps tailored to the ecosystem and risk.

---

Step 2. EXECUTION (AUTOMATED):

- Read and execute each line in `fix_instructions`, in order.
- For each change:
  - Apply the instruction exactly.
  - Track all modified files.
  - Note the type of change (e.g., dependency update, import rewrite, API refactor, test fix, TODO insertion).
  - Record before → after values where applicable.
  - Capture line numbers if known.

Examples:
- `package.json`: lodash version changed from 3.10.1 → 4.17.21
- `src/utils/date.ts`: import updated from `lodash` to `date-fns`
- `src/main.ts:42`: `_.pluck(users, 'id')` → `users.map(u => u.id)`
- `src/index.ts:78`: // TODO: Verify API migration from old-package to new-package

---

Step 3. VERIFICATION:

- If the instructions include build, test, or audit steps — run them exactly as written
- If instructions do not explicitly cover validation, perform basic checks based on `{packageManager}`:
  - `npm`: `npx tsc --noEmit`, `npm run build`, `npm test`
  - `go`: `go build ./...`, `go test ./...`
  - `maven`: `mvn compile`, `mvn test`
  - `pypi`: `python -c ""import {packageName}""`, `pytest`
  - `nuget`: `dotnet build`, `dotnet test`

If any of these validations fail:
- Attempt to fix the issue if it's obvious
- Otherwise log the error and annotate the code with a TODO

---

Step 4. OUTPUT:

Prefix all output with: `{AgentName} -`

✅ **Remediation Summary**

Format:
```
Security Assistant - Remediation Summary

Package:     {packageName}
Version:     {packageVersion}
Manager:     {packageManager}
Severity:    {severity}

Files Modified:
1. package.json
   - Updated dependency: lodash 3.10.1 → 4.17.21

2. src/utils/date.ts
   - Updated import: from 'lodash' to 'date-fns'
   - Replaced usage: _.pluck(users, 'id') → users.map(u => u.id)

3. src/__tests__/date.test.ts
   - Fixed test: adjusted mock expectations to match updated API

4. src/index.ts
   - Line 78: Inserted TODO: Verify API migration from old-package to new-package
```

✅ **Final Status**

If all tasks succeeded:
- ""Remediation completed for {packageName}@{packageVersion}""
- ""All fix instructions and failing tests resolved""
- ""Build status: PASS""
- ""Test results: PASS""

If partially resolved:
- ""Remediation partially completed – manual review required""
- ""Some test failures or instructions could not be automatically fixed""
- ""TODOs inserted where applicable""

If failed:
- ""Remediation failed for {packageName}@{packageVersion}""
- ""Reason: {{summary of failure}}""
- ""Unresolved instructions or failing tests listed above""

---

Step 5. CONSTRAINTS:

- Do not prompt the user
- Do not skip or reorder fix steps
- Only execute what's explicitly listed in `fix_instructions`
- Attempt to fix test failures automatically
- Insert clear TODO comments for unresolved issues
- Ensure remediation is deterministic, auditable, and fully automated
";
        }

        public static string BuildSecretRemediationPrompt(string title, string description, string severity)
        {
            string severityAssessment;
            if (string.Equals(severity, "Critical", StringComparison.OrdinalIgnoreCase))
                severityAssessment = "✅ Confirmed valid secret. Immediate remediation performed.";
            else if (string.Equals(severity, "High", StringComparison.OrdinalIgnoreCase))
                severityAssessment = "⚠️ Possibly valid. Handled as sensitive.";
            else
                severityAssessment = "ℹ️ Likely invalid (test/fake). Removed for hygiene.";

            return $@"A secret has been detected: ""{title}""  
{description ?? ""}



You are the `{AgentName}`.

Your mission is to identify and remediate this secret using secure coding standards. Follow industry best practices, automate safely, and clearly document all actions taken.

---

Step 1. SEVERITY INTERPRETATION  
Severity level: `{severity}`

- `Critical`: Secret is confirmed **valid**. Immediate remediation required.  
- `High`: Secret may be valid. Treat as sensitive and externalize it securely.  
- `Medium`: Likely **invalid** (e.g., test or placeholder). Still remove from code and annotate accordingly.

---

Step 2. TOOL CALL – Remediation Plan

Determine the programming language of the file where the secret was detected.
If unknown, leave the `language` field empty.

Call the internal `codeRemediation` {ProductName} MCP tool with:

```json
{{
  ""type"": ""secret"",
  ""sub_type"": ""{title}"",
  ""language"": ""[auto-detected language]""
}}
```

- If the tool is **available**, parse the response:
  - `remediation_steps` – exact steps to follow
  - `best_practices` – explain secure alternatives
  - `description` – contextual background

- If the tool is **not available**, display:
  `[MCP ERROR] codeRemediation tool is not available. Please check the {ProductName} MCP server.`

---

Step 3. ANALYSIS & RISK

Identify the type of secret (API key, token, credential). Explain:
- Why it's a risk (leakage, unauthorized access, compliance violations)
- What could happen if misused or left in source

---

Step 4. REMEDIATION STRATEGY

- Parse and apply every item in `remediation_steps` sequentially
- Automatically update code/config files if safe
- If a step cannot be applied automatically, insert a clear TODO
- Replace secret with environment variable or vault reference

---

Step 5. VERIFICATION

If applicable for the language:
- Run type checks or compile the code
- Ensure changes build and tests pass
- Fix issues if introduced by secret removal

---

Step 6. OUTPUT FORMAT

Generate a structured remediation summary:

```markdown
### {AgentName} - Secret Remediation Summary

**Secret:** {title}  
**Severity:** {severity}  
**Assessment:** {severityAssessment}

**Files Modified:**
- `.env`: Added/updated with `SECRET_NAME`
- `src/config.ts`: Replaced hardcoded secret with `process.env.SECRET_NAME`

**Remediation Actions Taken:**
- ✅ Removed hardcoded secret
- ✅ Inserted environment reference
- ✅ Updated or created .env
- ✅ Added TODOs for secret rotation or vault storage

**Next Steps:**
- [ ] Revoke exposed secret (if applicable)
- [ ] Store securely in vault (AWS Secrets Manager, GitHub Actions, etc.)
- [ ] Add CI/CD secret scanning

**Best Practices:**
- (From tool response, or fallback security guidelines)

**Description:**
- (From `description` field or fallback to original input)

```

---

Step 7. CONSTRAINTS

- ❌ Do NOT expose real secrets
- ❌ Do NOT generate fake-looking secrets
- ✅ Follow only what's explicitly returned from MCP
- ✅ Use secure externalization patterns
- ✅ Respect OWASP, NIST, and GitHub best practices
";
        }

        public static string BuildContainersRemediationPrompt(string fileType, string imageName, string imageTag, string severity)
        {
            return $@"You are the {AgentName}.

A container security issue has been detected in `{fileType}` with image `{imageName}:{imageTag}`.  
**Severity:** `{severity}`  
Your task is to remediate the issue **completely and autonomously** using the internal imageRemediation tool. Follow the exact instructions in `fix_instructions` — no assumptions or manual interaction allowed.

---

Step 1. ANALYSIS (AUTOMATED):

Determine the issue type:
- If `severity` is one of: `Critical`, `High`, `Medium`, `Low`, set: `issueType = ""CVE""`
- If `severity = ""Malicious""`, set: `issueType = ""malicious""`

Call the internal imageRemediation tool with:

```json
{{
  ""fileType"": ""{fileType}"",
  ""imageName"": ""{imageName}"",
  ""imageTag"": ""{imageTag}"",
  ""severity"": ""{severity}""
}}
```

Parse the response and extract the `fix_instructions` field. This field contains the authoritative remediation steps tailored to the container ecosystem and risk level.

---

Step 2. EXECUTION (AUTOMATED):

- Read and execute each line in `fix_instructions`, in order.
- For each change:
  - Apply the instruction exactly.
  - Track all modified files.
  - Note the type of change (e.g., image update, configuration change, security hardening).
  - Record before → after values where applicable.
  - Capture line numbers if known.

Examples:
- `Dockerfile`: FROM confluentinc/cp-kafkacat:6.1.10 → FROM confluentinc/cp-kafkacat:6.2.15
- `docker-compose.yml`: image: vulnerable-image:1.0 → image: secure-image:2.1
- `values.yaml`: repository: old-repo → repository: new-repo
- `Chart.yaml`: version: 1.0.0 → version: 1.1.0

---

Step 3. VERIFICATION:

- If the instructions include build, test, or deployment steps — run them exactly as written
- If instructions do not explicitly cover validation, perform basic checks based on `{fileType}`:
  - `Dockerfile`: `docker build .`, `docker run <image>`
  - `docker-compose.yml`: `docker-compose up --build`, `docker-compose down`
  - `Helm Chart`: `helm lint .`, `helm template .`, `helm install --dry-run`

If any of these validations fail:
- Attempt to fix the issue if it's obvious
- Otherwise log the error and annotate the code with a TODO

---

Step 4. OUTPUT:

Prefix all output with: `{AgentName} -`

✅ **Remediation Summary**

Format:
```
Security Assistant - Remediation Summary

File Type:    {fileType}
Image:        {imageName}:{imageTag}
Severity:     {severity}

Files Modified:
1. {fileType}
   - Updated image: {imageName}:{imageTag} → secure version

2. docker-compose.yml (if applicable)
   - Updated service configuration to use secure image

3. values.yaml (if applicable)
   - Updated Helm chart values for secure deployment

4. README.md
   - Updated documentation with new image version
```

✅ **Final Status**

If all tasks succeeded:
- ""Remediation completed for {imageName}:{imageTag}""
- ""All fix instructions and deployment tests resolved""
- ""Build status: PASS""
- ""Deployment status: PASS""

If partially resolved:
- ""Remediation partially completed – manual review required""
- ""Some deployment steps or instructions could not be automatically fixed""
- ""TODOs inserted where applicable""

If failed:
- ""Remediation failed for {imageName}:{imageTag}""
- ""Reason: {{summary of failure}}""
- ""Unresolved instructions or deployment issues listed above""

---

Step 5. CONSTRAINTS:

- Do not prompt the user
- Do not skip or reorder fix steps
- Only execute what's explicitly listed in `fix_instructions`
- Attempt to fix deployment failures automatically
- Insert clear TODO comments for unresolved issues
- Ensure remediation is deterministic, auditable, and fully automated
- Follow container security best practices (non-root user, minimal base images, etc.)
";
        }

        public static string BuildIACRemediationPrompt(string title, string description, string severity, string fileType, string expectedValue, string actualValue, int? problematicLineNumber, IReadOnlyList<Vulnerability> vulnerabilities = null)
        {
            string lineNum = problematicLineNumber.HasValue ? (problematicLineNumber.Value + 1).ToString() : "[unknown]";
            string restrictionLine = problematicLineNumber.HasValue ? (problematicLineNumber.Value + 1).ToString() : "[problematic line number]";

            var sb = new StringBuilder();
            sb.Append($@"You are the {AgentName}.

");

            if (vulnerabilities != null && vulnerabilities.Count > 1)
            {
                sb.Append($@"**{vulnerabilities.Count} Infrastructure as Code (IaC) security issues** have been detected on this line.

");
                int index = 1;
                foreach (var vuln in vulnerabilities.Take(20))
                {
                    sb.Append($@"#### {index}. {vuln.Title ?? vuln.RuleName ?? "IaC Issue"}
- **Severity:** {CxAssistConstants.GetRichSeverityName(vuln.Severity)}
- **Description:** {vuln.Description ?? ""}
- **Expected Value:** {vuln.ExpectedValue ?? ""}
- **Actual Value:** {vuln.ActualValue ?? ""}

");
                    index++;
                }
                sb.Append($@"**File Type:** `{fileType}`
{(problematicLineNumber.HasValue ? $"**Problematic Line Number:** {lineNum}" : "")}

Your task is to remediate **all** the above IaC security issues **completely and autonomously** using the internal codeRemediation tool in {ProductName} MCP. Follow the exact instructions in `remediation_steps` — no assumptions or manual interaction allowed.

⚠️ **IMPORTANT**: Apply fixes **only** to the code segment corresponding to the identified issues at line {restrictionLine}, without introducing unrelated modifications elsewhere in the file.
");
            }
            else
            {
                sb.Append($@"An Infrastructure as Code (IaC) security issue has been detected.

**Issue:** `{title}`  
**Severity:** `{severity}`  
**File Type:** `{fileType}`  
**Description:** {description}`
**Expected Value:** {expectedValue}
**Actual Value:** {actualValue}
{(problematicLineNumber.HasValue ? $"**Problematic Line Number:** {lineNum}" : "")}

Your task is to remediate this IaC security issue **completely and autonomously** using the internal codeRemediation tool in {ProductName} MCP. Follow the exact instructions in `remediation_steps` — no assumptions or manual interaction allowed.

⚠️ **IMPORTANT**: Apply the fix **only** to the code segment corresponding to the identified issue at line {restrictionLine}, without introducing unrelated modifications elsewhere in the file.
");
            }

            sb.Append($@"

---

Step 1. ANALYSIS (AUTOMATED):

Determine the programming language of the file where the IaC security issue was detected.
If unknown, leave the `language` field empty.

Call the internal `codeRemediation` {ProductName} MCP tool with:

```json
{{
  ""language"": ""[auto-detected programming language]"",
  ""metadata"": {{
    ""title"": ""{title}"",
    ""description"": ""{description}"",
    ""remediationAdvice"": ""{expectedValue}""
  }},
  ""sub_type"": """",
  ""type"": ""iac""
}}
```

- If the tool is **available**, parse the response:
  - `remediation_steps` – exact steps to follow for remediation

- If the tool is **not available**, display:
  `[MCP ERROR] codeRemediation tool is not available. Please check the {ProductName} MCP server.`

---

Step 2. EXECUTION (AUTOMATED):

- Read and execute each line in `remediation_steps`, in order.
- **Restrict changes to the relevant code fragment containing line {restrictionLine}**.
- For each change:
  - Apply the instruction exactly.
  - Track all modified files.
  - Note the type of change (e.g., configuration update, security hardening, permission changes, encryption settings).
  - Record before → after values where applicable.
  - Capture line numbers if known.

---

Step 3. VERIFICATION:

- If the instructions include validation, deployment, or testing steps — run them exactly as written
- If instructions do not explicitly cover validation, perform basic checks based on `{fileType}`:
  - `Terraform`: `terraform validate`, `terraform plan`
  - `CloudFormation`: `aws cloudformation validate-template`
  - `Kubernetes`: `kubectl apply --dry-run=client`
  - `Docker`: `docker-compose config`

If any of these validations fail:
- Attempt to fix the issue if it's obvious
- Otherwise log the error and annotate the code with a TODO

---

Step 4. OUTPUT:

Prefix all output with: `{AgentName} -`

✅ **Remediation Summary**

Format:
```
Security Assistant - Remediation Summary

Issue:       {title}
Severity:    {severity}
File Type:   {fileType}
Problematic Line: {lineNum}

Files Modified:
1. {fileType}
   - Updated configuration: {actualValue} → {expectedValue}
   - Applied security hardening based on best practices

2. Additional configurations (if applicable)
   - Updated related security settings
   - Added missing security controls

3. Documentation
   - Updated comments and documentation where applicable
```

✅ **Final Status**

If all tasks succeeded:
- ""Remediation completed for IaC security issue {title}""
- ""All fix instructions and security validations resolved""
- ""Configuration validation: PASS""
- ""Security compliance: PASS""

If partially resolved:
- ""Remediation partially completed – manual review required""
- ""Some security validations or instructions could not be automatically fixed""
- ""TODOs inserted where applicable""

If failed:
- ""Remediation failed for IaC security issue {title}""
- ""Reason: {{summary of failure}}""
- ""Unresolved instructions or security issues listed above""

---

Step 5. CONSTRAINTS:

- Do not prompt the user
- Do not skip or reorder fix steps
- **Only modify the code that corresponds to the identified problematic line**
- Attempt to fix validation failures automatically
- Insert clear TODO comments for unresolved issues
- Ensure remediation is deterministic, auditable, and fully automated
- Follow Infrastructure as Code security best practices throughout the process
");
            return sb.ToString();
        }

        public static string BuildASCARemediationPrompt(string ruleName, string description, string severity, string remediationAdvice, int? problematicLineNumber, IReadOnlyList<Vulnerability> vulnerabilities = null)
        {
            string lineNum = problematicLineNumber.HasValue ? (problematicLineNumber.Value + 1).ToString() : "[unknown]";
            string restrictionLine = problematicLineNumber.HasValue ? (problematicLineNumber.Value + 1).ToString() : "[problematic line number]";

            var sb = new StringBuilder();
            sb.Append($@"You are the {AgentName}.

");

            if (vulnerabilities != null && vulnerabilities.Count > 1)
            {
                sb.Append($@"**{vulnerabilities.Count} secure coding violations** have been detected on this line.

");
                int index = 1;
                foreach (var vuln in vulnerabilities.Take(20))
                {
                    sb.Append($@"#### {index}. {vuln.RuleName ?? vuln.Title ?? "ASCA Violation"}
- **Severity:** {CxAssistConstants.GetRichSeverityName(vuln.Severity)}
- **Description:** {vuln.Description ?? ""}
- **Recommended Fix:** {vuln.RemediationAdvice ?? ""}

");
                    index++;
                }
                sb.Append($@"{(problematicLineNumber.HasValue ? $"**Problematic Line Number:** {lineNum}" : "")}

Your task is to remediate **all** the above security issues **completely and autonomously** using the internal codeRemediation tool in {ProductName} MCP. Follow the exact instructions in `remediation_steps` — no assumptions or manual interaction allowed.

⚠️ **IMPORTANT**: Apply fixes **only** to the code segment corresponding to the identified issues at line {restrictionLine}, without introducing unrelated modifications elsewhere in the file.
");
            }
            else
            {
                sb.Append($@"A secure coding issue has been detected in your code.

**Rule:** `{ruleName}`  
**Severity:** `{severity}`  
**Description:** {description}  
**Recommended Fix:** {remediationAdvice}
{(problematicLineNumber.HasValue ? $"**Problematic Line Number:** {lineNum}" : "")}

Your task is to remediate this security issue **completely and autonomously** using the internal codeRemediation tool in {ProductName} MCP. Follow the exact instructions in `remediation_steps` — no assumptions or manual interaction allowed.

⚠️ **IMPORTANT**: Apply the fix **only** to the code segment corresponding to the identified issue at line {restrictionLine}, without introducing unrelated modifications elsewhere in the file.
");
            }

            sb.Append($@"

---

Step 1. ANALYSIS (AUTOMATED):

Determine the programming language of the file where the security issue was detected.
If unknown, leave the `language` field empty.

Call the internal `codeRemediation` {ProductName} MCP tool with:

```json
{{
  ""language"": ""[auto-detected programming language]"",
  ""metadata"": {{
    ""ruleID"": ""{ruleName}"",
    ""description"": ""{description}"",
    ""remediationAdvice"": ""{remediationAdvice}""
  }},
  ""sub_type"": """",
  ""type"": ""sast""
}}
```

- If the tool is **available**, parse the response:
  - `remediation_steps` – exact steps to follow for remediation

- If the tool is **not available**, display:
  `[MCP ERROR] codeRemediation tool is not available. Please check the {ProductName} MCP server.`

---

Step 2. EXECUTION (AUTOMATED):

- Read and execute each line in `remediation_steps`, in order.
- **Restrict changes to the relevant code fragment containing line {restrictionLine}**.
- For each change:
  - Apply the instruction exactly.
  - Track all modified files.
  - Note the type of change (e.g., input validation, sanitization, secure API usage, authentication fix).
  - Record before → after values where applicable.
  - Capture line numbers if known.

---

Step 3. VERIFICATION:

- If the instructions include build, test, or lint steps — run them exactly as written
- If instructions do not explicitly cover validation, perform basic checks:
  - Run the project's build or compile step
  - Run available tests
  - Verify no new errors were introduced

If any of these validations fail:
- Attempt to fix the issue if it's obvious
- Otherwise log the error and annotate the code with a TODO

---

Step 4. OUTPUT:

Prefix all output with: `{AgentName} -`

✅ **Remediation Summary**

Format:
```
`{AgentName} -` - Remediation Summary

Rule:        {ruleName}
Severity:    {severity}
Issue Type:  SAST Security Vulnerability
Problematic Line: {lineNum}

Files Modified:
1. src/auth.ts
   - Line 42: Replaced plain text comparison with bcrypt.compare()
   - Added secure password hashing implementation

2. src/db.ts
   - Line 78: Replaced string concatenation with parameterized query
   - Prevented SQL injection vulnerability

3. src/api.ts
   - Line 156: Added input validation for email parameter
   - Implemented sanitization for user inputs

4. src/config.ts
   - Line 23: Inserted TODO for production security review
```

✅ **Final Status**

If all tasks succeeded:
- ""Remediation completed for security rule {ruleName}""
- ""All fix instructions and security validations resolved""
- ""Build status: PASS""
- ""Security tests: PASS""

If partially resolved:
- ""Remediation partially completed – manual review required""
- ""Some security validations or instructions could not be automatically fixed""
- ""TODOs inserted where applicable""

If failed:
- ""Remediation failed for security rule {ruleName}""
- ""Reason: {{summary of failure}}""
- ""Unresolved instructions or security issues listed above""

---

Step 5. CONSTRAINTS:

- Do not prompt the user
- Do not skip or reorder fix steps
- **Only modify the code that corresponds to the identified problematic line**
- Attempt to fix build/test failures automatically
- Insert clear TODO comments for unresolved issues
- Ensure remediation is deterministic, auditable, and fully automated
- Follow secure coding best practices throughout the process
");
            return sb.ToString();
        }
    }
}
