using System;
using System.Diagnostics;

class RepositoryInformation : IDisposable
{
    private bool disposed;
    private readonly Process gitProcess;

    public static RepositoryInformation GetRepositoryInformation(string workingDirectory)
    {
        var repositoryInformation = new RepositoryInformation(workingDirectory);
        if (repositoryInformation.IsGitRepository)
        {
            return repositoryInformation;
        }

        return null;
    }

    public string CurrentBranch
    {
        get
        {
            return RunCommand("rev-parse --abbrev-ref HEAD");
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            gitProcess.Dispose();
        }
    }

    private RepositoryInformation(string workingDirectory)
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            FileName = "git.exe",
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        gitProcess = new Process
        {
            StartInfo = processInfo
        };
    }

    private bool IsGitRepository
    {
        get
        {
            return !String.IsNullOrWhiteSpace(RunCommand("log -1"));
        }
    }

    private string RunCommand(string args)
    {
        gitProcess.StartInfo.Arguments = args;
        gitProcess.Start();
        string output = gitProcess.StandardOutput.ReadToEnd().Trim();
        gitProcess.WaitForExit();

        return output;
    }
}