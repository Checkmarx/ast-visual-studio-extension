using System.Text;

namespace ast_visual_studio_extension.CxPreferences
{
    /// <summary>
    /// Neutralizes characters that can forge extra log lines when values flow into logs or CLI output.
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
