# Exit script on any error
$ErrorActionPreference = "Stop"

# Step 1: Get the branch name and checkout
$BRANCH_NAME = Read-Host "Enter the branch name (leave blank to use the current branch)"

if ($BRANCH_NAME) {
    # Check if branch exists locally
    if (git rev-parse --verify "$BRANCH_NAME" 2>$null) {
        Write-Host "Branch $BRANCH_NAME exists locally. Checking out..."
        git checkout "$BRANCH_NAME"
    } else {
        Write-Host "Branch $BRANCH_NAME does not exist locally. Checking out from remote..."
        git checkout -t "origin/$BRANCH_NAME"
    }

    Write-Host "Pulling latest code..."
    git pull
} else {
    Write-Host "No branch specified. Using the current local branch."
}

# Navigate to the root directory
Set-Location -Path "../.."

# Step 2: Set up paths for Visual Studio executables
$MSBUILD_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$VSIXINSTALLER_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\VSIXInstaller.exe"
$VSTEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

# Step 4: Build the solution
Write-Host "Building solution..."
& "$MSBUILD_PATH" "$(Get-Location)\ast-visual-studio-extension\ast-visual-studio-extension.sln" /p:Configuration=Release /m:1

# Step 5: Install Checkmarx Extension
Write-Host "Installing Checkmarx Extension..."
& "$VSIXINSTALLER_PATH" /quiet "$(Get-Location)\ast-visual-studio-extension\ast-visual-studio-extension\bin\Release\ast-visual-studio-extension.vsix"
Start-Sleep -Seconds 20

# Step 6: Run UI Tests
Write-Host "Running UI Tests..."
& "$VSTEST_PATH" /InIsolation "./UITests/bin/Release/UITests.dll"

Write-Host "Script execution completed successfully."
