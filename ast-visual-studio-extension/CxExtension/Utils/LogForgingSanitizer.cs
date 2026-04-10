using System.Text;

namespace ast_visual_studio_extension.CxExtension.Utils
{
    /// <summary>
    /// CWE-117 / log forging: neutralizes line-termination and related characters in data written to logs
    /// so user-controlled values cannot inject extra log lines (see Checkmarx Log_Forging / Improper Output Neutralization for Logs).
    /// </summary>
    internal static class LogForgingSanitizer
    {
        internal static string StripLineTermination(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\r' || ch == '\n' || ch == '\u0085' || ch == '\u2028' || ch == '\u2029')
                    sb.Append(' ');
                else
                    sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}
