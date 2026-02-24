namespace ast_visual_studio_extension.CxExtension.DevAssist.Services
{
    public class PromptBuilderService
    {
        /// <summary>
        /// Build prompt for ASCA (SAST) vulnerabilities
        /// </summary>
        public string BuildASCAPrompt(string ruleName, string remediationAdvice, string severity, string filePath = null, int lineNumber = 0)
        {
            var locationInfo = !string.IsNullOrEmpty(filePath) && lineNumber > 0
                ? $"**Location**: {filePath} (Line {lineNumber})\n"
                : "";

            return $@"You are Checkmarx One Assist, an AI security expert.

I need help fixing a security vulnerability in my code:

**Rule**: {ruleName}
**Severity**: {severity}
{locationInfo}**Remediation Advice**: {remediationAdvice}

Please provide:
1. **Root Cause Analysis**: Explain what makes this code vulnerable
2. **Step-by-Step Fix**: Specific code changes to resolve the issue  
3. **Secure Code Example**: Show the corrected implementation
4. **Validation**: How to verify the fix works properly

Focus on practical, actionable solutions that I can implement immediately.";
        }

        /// <summary>
        /// Build prompt for SCA (package) vulnerabilities  
        /// </summary>
        public string BuildSCAPrompt(string packageName, string version, string severity, string packageManager = null)
        {
            var managerInfo = !string.IsNullOrEmpty(packageManager)
                ? $" (Package Manager: {packageManager})"
                : "";

            return $@"You are Checkmarx One Assist, an AI security expert.

I have a vulnerable dependency in my project:

**Package**: {packageName} v{version}{managerInfo}
**Severity**: {severity}

Please help me fix this vulnerability:
1. **Risk Assessment**: What security risks does this vulnerability pose?
2. **Update Strategy**: What version should I upgrade to?
3. **Implementation Steps**: Exact commands/changes needed
4. **Testing**: How to verify the update doesn't break functionality
5. **Alternatives**: If no safe version exists, suggest alternative packages

Provide specific, actionable remediation steps.";
        }

        /// <summary>
        /// Build prompt for Secrets detection
        /// </summary>
        public string BuildSecretsPrompt(string secretType, string description, string severity, string filePath = null, int lineNumber = 0)
        {
            var locationInfo = !string.IsNullOrEmpty(filePath) && lineNumber > 0
                ? $"**Location**: {filePath} (Line {lineNumber})\n"
                : "";

            return $@"You are Checkmarx One Assist, an AI security expert.

A secret has been detected in my code:

**Type**: {secretType}
**Severity**: {severity}
{locationInfo}**Description**: {description}

Please help me secure this secret:
1. **Risk Analysis**: Why is this secret exposure dangerous?
2. **Immediate Action**: Steps to remove the secret from code
3. **Secure Storage**: Best practices for storing this type of secret
4. **Code Changes**: Examples of secure implementation
5. **Prevention**: How to avoid similar issues in the future

Provide specific remediation steps with code examples.";
        }

        /// <summary>
        /// Build prompt for Infrastructure as Code (IaC) issues
        /// </summary>
        public string BuildIaCPrompt(string title, string description, string severity, string expectedValue = null, string actualValue = null)
        {
            var valueInfo = !string.IsNullOrEmpty(expectedValue) && !string.IsNullOrEmpty(actualValue)
                ? $"**Expected Value**: {expectedValue}\n**Actual Value**: {actualValue}\n"
                : "";

            return $@"You are Checkmarx One Assist, an AI security expert.

I have a security misconfiguration in my Infrastructure as Code:

**Issue**: {title}
**Severity**: {severity}
{valueInfo}**Description**: {description}

Please help me fix this configuration issue:
1. **Security Impact**: What risks does this misconfiguration create?
2. **Configuration Fix**: Exact changes needed in the IaC template
3. **Best Practices**: Secure configuration recommendations
4. **Validation**: How to test the fix works correctly

Provide specific configuration examples and remediation steps.";
        }

        /// <summary>
        /// Build prompt for Container security issues
        /// </summary>
        public string BuildContainersPrompt(string imageName, string imageTag, string severity, string issueType)
        {
            return $@"You are Checkmarx One Assist, an AI security expert.

I have a security issue in my container configuration:

**Container Image**: {imageName}:{imageTag}
**Issue Type**: {issueType}
**Severity**: {severity}

Please help me fix this container security issue:
1. **Vulnerability Analysis**: What security risks does this create?
2. **Image Fix**: Steps to resolve the issue (update, rebuild, etc.)
3. **Dockerfile Changes**: Any needed changes to container configuration
4. **Security Hardening**: Additional container security best practices
5. **Validation**: How to verify the container is now secure

Provide specific remediation steps for container security.";
        }

        /// <summary>
        /// Build generic prompt when vulnerability type is unknown
        /// </summary>
        public string BuildGenericPrompt(string description, string severity)
        {
            return $@"You are Checkmarx One Assist, an AI security expert.

I need help with a security issue:

**Issue**: {description}
**Severity**: {severity}

Please help me understand and fix this security vulnerability:
1. **Analysis**: What type of security issue is this?
2. **Risk Assessment**: What are the potential impacts?
3. **Remediation**: Step-by-step fix instructions
4. **Best Practices**: How to prevent similar issues

Provide specific, actionable guidance to resolve this security concern.";
        }
    }
}
