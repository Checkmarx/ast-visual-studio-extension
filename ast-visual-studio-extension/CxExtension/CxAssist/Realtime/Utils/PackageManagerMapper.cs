namespace ast_visual_studio_extension.CxExtension.CxAssist.Realtime.Utils
{
    /// <summary>
    /// Maps package manager names returned by ast-cli (gradle, sbt, cocoapods, carthage) to the
    /// legacy names expected by the Checkmarx remediation API (mvn, swift).
    /// Aligned with ast-jetbrains-plugin PackageManagerMapper.
    /// </summary>
    internal static class PackageManagerMapper
    {
        /// <summary>
        /// Mapping rules:
        /// - gradle → mvn
        /// - sbt → mvn
        /// - cocoapods → swift
        /// - carthage → swift
        /// - all others → unchanged
        /// </summary>
        public static string MapToRemediationFormat(string packageManager)
        {
            if (string.IsNullOrEmpty(packageManager)) return packageManager;

            switch (packageManager.ToLowerInvariant())
            {
                case "gradle":
                case "sbt":
                    return "mvn";
                case "cocoapods":
                case "carthage":
                    return "swift";
                default:
                    return packageManager;
            }
        }
    }
}
