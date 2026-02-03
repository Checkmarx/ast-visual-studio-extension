namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Models
{
    /// <summary>
    /// Severity levels for vulnerabilities
    /// Based on JetBrains SeverityLevel enum
    /// Matches: src/main/java/com/checkmarx/intellij/util/SeverityLevel.java
    /// </summary>
    public enum SeverityLevel
    {
        Malicious,  // Highest priority (precedence 1 in JetBrains)
        Critical,   // precedence 2
        High,       // precedence 3
        Medium,     // precedence 4
        Low,        // precedence 5
        Unknown,    // precedence 6
        Ok,         // precedence 7
        Ignored,    // precedence 8
        Info        // Additional level for informational messages
    }
}

