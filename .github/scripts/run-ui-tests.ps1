<#
.SYNOPSIS
This PowerShell script automates the process of checking out a Git branch, building a Visual Studio solution, 
installing a Visual Studio extension, and running UI tests.

.DESCRIPTION
This script performs the following steps:
1. Checks out the specified Git branch. If the branch does not exist locally, it attempts to fetch it from the remote.
2. Navigates to the root directory of the Git repository.
3. Validates the paths for required Visual Studio executables (MSBuild, VSIXInstaller, and VSTest).
4. Builds the Visual Studio solution in Release configuration.
5. Installs the Checkmarx extension (.vsix) into Visual Studio.
6. Executes UI tests using the VSTest console.

.WARNING
Ensure that Visual Studio is closed before running this script. Installing the Checkmarx extension will fail if 
Visual Studio is running.

.PARAMETER branchName
The name of the Git branch to check out. If not provided, the script assumes the current branch.

.EXAMPLE
.\run-ui-tests.ps1 -branchName "feature/new-ui-tests"
This checks out the branch `feature/new-ui-tests`, builds the solution, installs the extension, and runs UI tests.

.EXAMPLE
.\run-ui-tests.ps1
If no branch name is provided, the script uses the current branch.

.NOTES
- Ensure that Git, Visual Studio, and required dependencies are installed on the system before running the script.
- Paths to Visual Studio executables are hardcoded and should be updated if Visual Studio is installed in a non-default location.
- The script must be run from a PowerShell environment with necessary permissions.
#>

param (
    [string]$branchName = ""
)

# Exit script on any error
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Helper function for logging
function Log {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $args"
}

# Warn the user about closing Visual Studio
Log "WARNING: Ensure that Visual Studio is closed before running this script to avoid installation failures."

# Step 1: Get the branch name and checkout
if ($branchName -eq "") {
    Log "No branch name provided. Using the current local branch."
} else {
    try {
        # Check if the branch exists locally
        $localBranchExists = git branch --list $branchName | ForEach-Object { $_.Trim() }

        if ($localBranchExists) {
            Log "Branch $branchName exists locally. Checking out..."
            git checkout $branchName
        } else {
            # Check if the branch exists on the remote
            $remoteBranchExists = git ls-remote --heads origin $branchName | ForEach-Object { $_.Trim() }

            if ($remoteBranchExists -ne "") {
                Log "Branch $branchName does not exist locally. Checking out from remote..."
                git checkout -t "origin/$branchName"
            } else {
                throw "Branch $branchName does not exist locally or on the remote. Please verify the branch name."
            }
        }

        Log "Pulling latest code..."
        git pull
    } catch {
        Log "Error: $_"
        throw "Failed to switch to branch $branchName. Ensure the branch exists locally or remotely."
    }
}

# Navigate to the root directory
try {
    Set-Location (git rev-parse --show-toplevel)
} catch {
    Log "Error: $_"
    throw "Failed to navigate to the Git repository root directory."
}

# Step 2: Set up paths for Visual Studio executables
$msbuildPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
$vsixInstallerPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/VSIXInstaller.exe"
$vsTestPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

# Validate paths
if (-not (Test-Path $msbuildPath)) { throw "MSBuild executable not found at $msbuildPath" }
if (-not (Test-Path $vsixInstallerPath)) { throw "VSIX Installer not found at $vsixInstallerPath" }
if (-not (Test-Path $vsTestPath)) { throw "VSTest executable not found at $vsTestPath" }

# Step 3: Build the solution
try {
    Log "Building solution..."
    $solutionPath = "$(Get-Location)/ast-visual-studio-extension.sln"
    Start-Process -FilePath $msbuildPath -ArgumentList "`"$solutionPath`"", "/p:Configuration=Release" -Wait -NoNewWindow
} catch {
    Log "Error: $_"
    throw "Failed to build the solution."
}

# Step 4: Install Checkmarx Extension
try {
    Log "Installing Checkmarx Extension..."
    $vsixPath = "$(Get-Location)/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix"
    Start-Process -FilePath $vsixInstallerPath -ArgumentList "/quiet", "`"$vsixPath`"" -Wait -NoNewWindow
    Start-Sleep -Seconds 20
} catch {
    Log "Error: $_"
    throw "Failed to install the Checkmarx extension."
}

# Step 5: Run UI Tests
try {
    Log "Running UI Tests..."
    $testDllPath = "$(Get-Location)/UITests/bin/Release/UITests.dll"
    Start-Process -FilePath $vsTestPath -ArgumentList "/InIsolation", "`"$testDllPath`"" -Wait -NoNewWindow
} catch {
    Log "Error: $_"
    throw "Failed to run UI tests."
}

# Final message
Log "Script execution completed successfully."
