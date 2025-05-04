# Step 1: Define paths
$msbuildPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
$vsixInstallerPath = "C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/VSIXInstaller.exe"

# Step 2: Validate paths
if (-not (Test-Path $msbuildPath)) { throw "MSBuild executable not found at $msbuildPath" }
if (-not (Test-Path $vsixInstallerPath)) { throw "VSIX Installer not found at $vsixInstallerPath" }

# Step 3: Build the solution
Write-Host "🔧 Building the solution..."
$solutionPath = "$(Get-Location)/ast-visual-studio-extension.sln"
Start-Process -FilePath $msbuildPath -ArgumentList "`"$solutionPath`"", "/p:Configuration=Release" -Wait -NoNewWindow

# Step 4: Install the VSIX extension
Write-Host "📦 Installing the Checkmarx Extension..."
$vsixPath = "$(Get-Location)/ast-visual-studio-extension/bin/Release/ast-visual-studio-extension.vsix"
Start-Process -FilePath $vsixInstallerPath -ArgumentList "/quiet", "`"$vsixPath`"" -Wait -NoNewWindow

Write-Host "✅ Extension installed successfully."
