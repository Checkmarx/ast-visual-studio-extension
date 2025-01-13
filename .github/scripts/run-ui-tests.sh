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
cd ../..

# Step 2: Set up paths for Visual Studio executables
MSBUILD_PATH="C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
VSIXINSTALLER_PATH="C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/VSIXInstaller.exe"
VSTEST_PATH="C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

# Step 4: Build the solution
echo "Building solution..."
"powershell -Command" "$MSBUILD_PATH" "$(pwd)/ast-visual-studio-extension.sln" /p:Configuration=Release

# Step 5: Install Checkmarx Extension
echo "Installing Checkmarx Extension..."
"$VSIXINSTALLER_PATH" /quiet "$(pwd)/ast-visual-studio-extension/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix"
sleep 20

# Step 6: Run UI Tests
echo "Running UI Tests..."
"$VSTEST_PATH" /InIsolation ./UITests/bin/Release/UITests.dll

echo "Script execution completed successfully."
