namespace ast_visual_studio_extension.CxPreferences.Configuration
{
    internal class McpInstallResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public bool Changed { get; set; }
        public string Message { get; set; }
        public string ConfigPath { get; set; }
    }
}
