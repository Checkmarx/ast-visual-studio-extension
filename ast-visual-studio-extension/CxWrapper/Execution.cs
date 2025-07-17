using ast_visual_studio_extension.CxWrapper.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Text;

namespace ast_visual_studio_extension.CxCLI
{
    internal class Execution
    {
        private readonly static string executablePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CxWrapper", "Resources", "cx.exe");

        public static string ExecuteCommand(List<string> arguments, Func<string, string> lineParser)
        {
            return InitProcess(arguments, lineParser);
        }

        public static string ExecuteCommand(List<string> arguments, string directory, string file)
        {
            InitProcess(arguments, CheckValidJSONString);

            return File.ReadAllText(Path.Combine(directory, file));
        }

        /// <summary>
        /// Check if provided string can be parsed
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string CheckValidJSONString(string line)
        {
            line = line ?? string.Empty;
            bool isValidJsonString = true;

            try
            {
                JsonValue.Parse(line);
            }
            catch (Exception)
            {
                isValidJsonString = false;
            }

            return isValidJsonString ? line : string.Empty;
        }

        private static string InitProcess(List<string> arguments, Func<string, string> lineParser)
        {
            string outputData = string.Empty;
            string errorData = string.Empty;
            var cliOutput = new List<string>();

            using (var process = new Process
            {
                StartInfo = GetProcessStartInfo(arguments)
            })
            {
                process.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        errorData += string.IsNullOrEmpty(errorData) ? args.Data : Environment.NewLine + args.Data;
                        cliOutput.Add(args.Data);
                    }
                };

                process.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        string parsedValue = lineParser.Invoke(args.Data);
                        if (!string.IsNullOrEmpty(parsedValue))
                        {
                            outputData += string.IsNullOrEmpty(outputData) ? parsedValue : Environment.NewLine + parsedValue;
                        }
                        cliOutput.Add(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                // Raise event with collected output
                OnProcessCompleted?.Invoke(cliOutput);

                if (process.ExitCode != 0)
                {
                    throw new CxException(process.ExitCode, string.IsNullOrEmpty(errorData) ? outputData.Trim() : errorData.Trim());
                }

                return !string.IsNullOrEmpty(outputData) ? outputData.Trim() : errorData.Trim();
            }
        }

        public static event Action<List<string>> OnProcessCompleted;

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

        private static string BuildArguments(List<string> arguments)
        {
            string result = string.Empty;
            foreach(string arg in arguments)
            {
                result += " ";

                // Quote string if it contains spaces
                result += arg.Contains(" ") && (!arg.StartsWith("\"") && !arg.EndsWith("\"")) ? "\"" + arg + "\"" : arg;
           
            }
            return result;
        }
    }
}
