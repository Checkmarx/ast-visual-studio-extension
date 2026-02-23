namespace ast_visual_studio_extension.CxExtension.CxAssist.Core
{
    /// <summary>
    /// Shared constants for CxAssist (display names, log categories, theme and resource names).
    /// Use these instead of magic strings to maintain consistency and simplify changes.
    /// </summary>
    internal static class CxAssistConstants
    {
        /// <summary>Product name shown in UI (Quick Info header, messages, Error List).</summary>
        public const string DisplayName = "Checkmarx One Assist";

        /// <summary>Log category for debug/trace output (e.g. Debug.WriteLine).</summary>
        public const string LogCategory = "CxAssist";

        /// <summary>Theme folder name for dark theme icons.</summary>
        public const string ThemeDark = "Dark";

        /// <summary>Theme folder name for light theme icons.</summary>
        public const string ThemeLight = "Light";

        /// <summary>Badge image file name (header in Quick Info).</summary>
        public const string BadgeIconFileName = "cxone_assist.png";
    }
}
