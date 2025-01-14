# Exit script on any error
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Helper function for logging
function Log {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $args"
}

# Step 1: Get the branch name and checkout
$branchName = Read-Host "Enter the branch name (leave blank to use the current branch)"

if ($branchName -ne "") {
    if (git rev-parse --verify $branchName *>&1) {
        Log "Branch $branchName exists locally. Checking out..."
        git checkout $branchName
    } else {
        Log "Branch $branchName does not exist locally. Checking out from remote..."
        git checkout -t "origin/$branchName"
    }

    Log "Pulling latest code..."
    git pull
} else {
    Log "No branch specified. Using the current local branch."
}

# Navigate to the root directory
Set-Location (git rev-parse --show-toplevel)

# Step 2: Set up paths for Visual Studio executables
$msbuildPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
$vsixInstallerPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/VSIXInstaller.exe"
$vsTestPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

# Validate paths
if (-not (Test-Path $msbuildPath)) { throw "MSBuild executable not found at $msbuildPath" }
if (-not (Test-Path $vsixInstallerPath)) { throw "VSIX Installer not found at $vsixInstallerPath" }
if (-not (Test-Path $vsTestPath)) { throw "VSTest executable not found at $vsTestPath" }

# Step 3: Build the solution
Log "Building solution..."
$solutionPath = "$(Get-Location)/ast-visual-studio-extension.sln"
Start-Process -FilePath $msbuildPath -ArgumentList "`"$solutionPath`"", "/p:Configuration=Release", "/m:1" -Wait -NoNewWindow

# Step 4: Install Checkmarx Extension
Log "Installing Checkmarx Extension..."
$vsixPath = "$(Get-Location)/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix"
Start-Process -FilePath $vsixInstallerPath -ArgumentList "/quiet", "`"$vsixPath`"" -Wait -NoNewWindow
Start-Sleep -Seconds 20

# Step 5: Run UI Tests
Log "Running UI Tests..."
$testDllPath = "$(Get-Location)/UITests/bin/Release/UITests.dll"
Start-Process -FilePath $vsTestPath -ArgumentList "/InIsolation", "`"$testDllPath`"" -Wait -NoNewWindow

# Final message
Log "Script execution completed successfully."
