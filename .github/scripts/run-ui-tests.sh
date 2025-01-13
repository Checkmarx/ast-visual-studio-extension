# Exit script on any error
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Step 1: Get the branch name and checkout
$branchName = Read-Host "Enter the branch name (leave blank to use the current branch)"

if ($branchName -ne "") {
    # Check if branch exists locally
    $branchExists = git rev-parse --verify $branchName *>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Branch $branchName exists locally. Checking out..."
        git checkout $branchName
    } else {
        Write-Host "Branch $branchName does not exist locally. Checking out from remote..."
        git checkout -t "origin/$branchName"
    }

    Write-Host "Pulling latest code..."
    git pull
} else {
    Write-Host "No branch specified. Using the current local branch."
}

# Navigate to the root directory
Set-Location (git rev-parse --show-toplevel)

# Step 2: Set up paths for Visual Studio executables
$msbuildPath = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe"
$vsixInstallerPath = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\IDE\\VSIXInstaller.exe"
$vsTestPath = "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\IDE\\CommonExtensions\\Microsoft\\TestWindow\\vstest.console.exe"

# Step 3: Build the solution
Write-Host "Building solution..."
Start-Process -FilePath $msbuildPath -ArgumentList "$(Get-Location)/ast-visual-studio-extension/ast-visual-studio-extension.sln", "/p:Configuration=Release", "/m:1" -Wait

# Step 4: Install Checkmarx Extension
Write-Host "Installing Checkmarx Extension..."
Start-Process -FilePath $vsixInstallerPath -ArgumentList "/quiet", "$(Get-Location)/ast-visual-studio-extension/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix" -Wait
Start-Sleep -Seconds 20

# Step 5: Run UI Tests
Write-Host "Running UI Tests..."
Start-Process -FilePath $vsTestPath -ArgumentList "/InIsolation", "$(Get-Location)/UITests/bin/Release/UITests.dll" -Wait

# Final message
Write-Host "Script execution completed successfully."
