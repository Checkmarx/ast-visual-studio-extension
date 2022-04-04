using System;

namespace ast_visual_studio_extension.CxWrapper.Exceptions
{
    public sealed class CxException : Exception
    {
        private readonly int exitCode;

        public CxException(int exitCode, string message) : base(message) 
        {
            this.exitCode = exitCode;
        }

        public int ExitCode { get { return exitCode; } }
    }
}
