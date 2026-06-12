namespace ast_visual_studio_extension.CxExtension.Utils
{
    /// <summary>
    /// CWE-117 / log forging: neutralizes line-termination and related characters before values reach logs.
    /// Uses explicit <see cref="string.Replace(string, string?)"/> calls so static analysis can treat this as output neutralization.
    /// </summary>
    internal static class LogForgingSanitizer
    {
        internal static string StripLineTermination(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\u0085", " ")
                .Replace("\u2028", " ")
                .Replace("\u2029", " ");
        }
    }
}
