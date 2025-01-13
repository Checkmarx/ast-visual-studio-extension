#!/bin/bash

# Exit script on any error
set -e

# Step 1: Get the branch name and checkout
read -p "Enter the branch name (leave blank to use the current branch): " BRANCH_NAME

if [[ -n "$BRANCH_NAME" ]]; then
  # Check if branch exists locally
  if git rev-parse --verify "$BRANCH_NAME" >/dev/null 2>&1; then
    echo "Branch $BRANCH_NAME exists locally. Checking out..."
    git checkout "$BRANCH_NAME"
  else
    echo "Branch $BRANCH_NAME does not exist locally. Checking out from remote..."
    git checkout -t "origin/$BRANCH_NAME"
  fi

  echo "Pulling latest code..."
  git pull
else
  echo "No branch specified. Using the current local branch."
fi

# Navigate to the root directory
cd "$(git rev-parse --show-toplevel)"

# Step 2: Set up paths for Visual Studio executables
MSBUILD_PATH="C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe"
VSIXINSTALLER_PATH="C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\IDE\\VSIXInstaller.exe"
VSTEST_PATH="C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\Common7\\IDE\\CommonExtensions\\Microsoft\\TestWindow\\vstest.console.exe"

# Step 3: Build the solution using PowerShell
powershell -Command "& {
    Write-Host 'Building solution...';
    Start-Process -FilePath '$MSBUILD_PATH' -ArgumentList '$(pwd)/ast-visual-studio-extension/ast-visual-studio-extension.sln', '/p:Configuration=Release', '/m:1' -Wait;
}"

# Step 4: Install Checkmarx Extension using PowerShell
powershell -Command "& {
    Write-Host 'Installing Checkmarx Extension...';
    Start-Process -FilePath '$VSIXINSTALLER_PATH' -ArgumentList '/quiet', '$(pwd)/ast-visual-studio-extension/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix' -Wait;
    Start-Sleep -Seconds 20;
}"

# Step 5: Run UI Tests using PowerShell
powershell -Command "& {
    Write-Host 'Running UI Tests...';
    Start-Process -FilePath '$VSTEST_PATH' -ArgumentList '/InIsolation', '$(pwd)/UITests/bin/Release/UITests.dll' -Wait;
}"

# Final message
echo "Script execution completed successfully."
