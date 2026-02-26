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

        /// <summary>
        /// When true, CxAssist findings are also added to the built-in Error List.
        /// When false, the hover popup shows only one block (our Quick Info).
        /// </summary>
        public const bool SyncFindingsToBuiltInErrorList = true;

        /// <summary>
        /// When true and SyncFindingsToBuiltInErrorList is true: Error List task Text is set empty so the hover
        /// popup does not show a second duplicate block (VS still shows the task in the list with File/Line/Column;
        /// full details are in our Quick Info on hover). When false: full description is shown in the Error List
        /// but the same text appears again in the hover (duplicate).
        /// </summary
    }
}
