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
