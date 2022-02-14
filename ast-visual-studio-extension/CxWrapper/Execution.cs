using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ast_visual_studio_extension.CxCLI
{
    internal class Execution
    {
        private readonly static string executablePath = Path.Combine(Environment.CurrentDirectory, "CxWrapper", "Resources", "cx.exe");

        public static string ExecuteCommand(List<string> arguments)
        {
            return InitProcess(arguments);
        }

        public static string ExecuteCommand(List<string> arguments, string directory, string file)
        {
            InitProcess(arguments);

            return File.ReadAllText(Path.Combine(directory, file));
        }

        private static string InitProcess(List<string> arguments)
        {
            string outputData = string.Empty;
            string errorData = string.Empty;

            using (var process = new Process
            {
                StartInfo = GetProcessStartInfo(arguments)
            })
            {
                process.OutputDataReceived += (s, args) => outputData += string.IsNullOrEmpty(outputData) ? args.Data : Environment.NewLine + args.Data;
                process.ErrorDataReceived += (s, args) => errorData += string.IsNullOrEmpty(errorData) ? args.Data : Environment.NewLine + args.Data;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new CxException(process.ExitCode, errorData.Trim());
                }
                
                return !string.IsNullOrEmpty(errorData.Trim()) ? errorData.Trim() : outputData.Trim();
            }
        }

        private static ProcessStartInfo GetProcessStartInfo(List<string> arguments)
        {
            return new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = BuildArguments(arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private static string BuildArguments(List<String> arguments)
        {
            // TODO: check if it is possible to use some process builder
            return String.Join(" ", arguments);
        }
    }
}
