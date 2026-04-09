using System;

namespace ast_visual_studio_extension.CxExtension.Commands
{
    /// <summary>
    /// Command set GUIDs defined in CxWindowPackage.vsct.
    /// These constants must match the GuidSymbol values in the .vsct file.
    /// </summary>
    internal static class CommandGuids
    {
        /// <summary>
        /// GUID for Error List context menu command set.
        /// Defined in CxWindowPackage.vsct as guidCxAssistErrorListCmdSet.
        /// </summary>
        public static readonly Guid ErrorListCommandSetGuid = new("b7e8b6e3-8e3e-4e3e-8e3e-8e3e8e3e8e40");

        /// <summary>
        /// GUID for Show Findings Window command set.
        /// Defined in CxWindowPackage.vsct as guidShowFindingsWindowCommandSet.
        /// </summary>
        public static readonly Guid ShowFindingsWindowCommandSetGuid = new("a6e8b6e3-8e3e-4e3e-8e3e-8e3e8e3e8e3f");
    }
}
