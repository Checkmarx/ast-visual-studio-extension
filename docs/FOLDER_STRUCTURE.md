# DevAssist Folder Structure - Visual Studio Extension (.NET Solution)

## 📋 Overview

This document outlines the folder structure for implementing DevAssist real-time scanners across the Visual Studio extension solution.

**Solution Structure:**
- **Production Code**: `ast-visual-studio-extension/` (Main VSIX project)
- **Unit Tests**: `ast-visual-studio-extension-tests/` (MSTest project)
- **UI Tests**: `UITests/` (FlaUI project)

---

## 🎯 1. Production Code - ast-visual-studio-extension/

### CxExtension/DevAssist/ (NEW)

```
CxExtension/DevAssist/
│
├── Core/                                       # Epic 1: Core Infrastructure
│   ├── Models/
│   │   ├── Vulnerability.cs
│   │   ├── Severity.cs
│   │   ├── ScannerType.cs
│   │   ├── ScanResult.cs
│   │   └── DiagnosticInfo.cs
│   │
│   ├── Diagnostics/
│   │   ├── DiagnosticProvider.cs
│   │   ├── DiagnosticManager.cs
│   │   └── ProblemWindowIntegration.cs
│   │
│   ├── GutterIcons/
│   │   ├── GutterIconProvider.cs
│   │   ├── SeverityIconMapper.cs
│   │   └── GutterIconFactory.cs
│   │
│   ├── Hover/
│   │   ├── HoverProvider.cs
│   │   ├── VulnerabilityHoverContent.cs
│   │   └── HoverPopupFormatter.cs
│   │
│   └── MockData/
│       ├── MockScannerA.cs
│       ├── MockScannerB.cs
│       ├── MockScannerOSS.cs
│       └── mock-vulnerabilities.json
│
├── UI/                                         # Epic 2 & 9: UI Components
│   ├── Settings/
│   │   ├── DevAssistSettingsPage.xaml
│   │   ├── DevAssistSettingsPage.xaml.cs
│   │   ├── ScannerSettingsControl.xaml
│   │   ├── ScannerSettingsControl.xaml.cs
│   │   └── ThemeAdapter.cs
│   │
│   ├── Welcome/
│   │   ├── WelcomeDialog.xaml
│   │   ├── WelcomeDialog.xaml.cs
│   │   ├── WelcomeViewModel.cs
│   │   └── FirstRunDetector.cs
│   │
│   └── Ignore/
│       ├── IgnoreWindow.xaml
│       ├── IgnoreWindow.xaml.cs
│       ├── IgnoreListControl.xaml
│       └── IgnoreListControl.xaml.cs
│
├── Configuration/                              # Epic 2: MCP & Configuration
│   ├── McpInstaller.cs
│   ├── McpConfigManager.cs
│   ├── OAuthIntegration.cs
│   ├── ScannerSettingsManager.cs
│   └── DevAssistConfig.cs
│
├── Remediation/                                # Epic 1: Copilot Integration
│   ├── CopilotIntegration.cs
│   ├── RemediationProvider.cs
│   ├── RemediationSuggestion.cs
│   └── CopilotCommandHandler.cs
│
├── Scanners/                                   # Epics 4-8: Real-Time Scanners
│   ├── Common/
│   │   ├── IScannerService.cs
│   │   ├── IScannerCommand.cs
│   │   ├── BaseScannerService.cs
│   │   ├── ScannerRegistry.cs
│   │   ├── FileEventListener.cs
│   │   ├── ScannerConfig.cs
│   │   └── ScannerBase.cs
│   │
│   ├── OSS/                                    # Epic 4
│   │   ├── OssScannerService.cs
│   │   ├── OssScannerCommand.cs
│   │   ├── OssDetectionEngine.cs
│   │   ├── MaliciousPackageDetector.cs
│   │   ├── ManifestFileParser.cs
│   │   └── OssRemediationProvider.cs
│   │
│   ├── Secrets/                                # Epic 5
│   │   ├── SecretsScannerService.cs
│   │   ├── SecretsScannerCommand.cs
│   │   ├── SecretsDetectionEngine.cs
│   │   ├── SecretPatternMatcher.cs
│   │   └── SecretsRemediationProvider.cs
│   │
│   ├── Containers/                             # Epic 6
│   │   ├── ContainersScannerService.cs
│   │   ├── ContainersScannerCommand.cs
│   │   ├── ContainersDetectionEngine.cs
│   │   ├── DockerfileParser.cs
│   │   └── ContainersRemediationProvider.cs
│   │
│   ├── ASCA/                                   # Epic 7
│   │   ├── AscaScannerService.cs
│   │   ├── AscaScannerCommand.cs
│   │   ├── AscaDetectionEngine.cs
│   │   ├── AscaCodeAnalyzer.cs
│   │   ├── AscaRemediationProvider.cs
│   │   └── AscaServiceAdapter.cs               # Adapter for existing ASCAService
│   │
│   └── IaC/                                    # Epic 8
│       ├── IacScannerService.cs
│       ├── IacScannerCommand.cs
│       ├── IacDetectionEngine.cs
│       ├── IacFileParser.cs
│       └── IacRemediationProvider.cs
│
├── Ignore/                                     # Epic 9: Ignore & Revive Backend
│   ├── IgnoreService.cs
│   ├── IgnoreFileManager.cs
│   ├── ReviveService.cs
│   ├── TemporaryStorage.cs
│   └── IgnoreRules.cs
│
├── Telemetry/                                  # Epic 10: Telemetry
│   ├── TelemetryService.cs
│   ├── TelemetryClient.cs
│   ├── MetricsCollector.cs
│   ├── AgentNameProvider.cs
│   └── TelemetryEvents.cs
│
└── Utils/                                      # Shared Utilities
    ├── FileTypeDetector.cs
    ├── PathHelper.cs
    ├── JsonHelper.cs
    └── ThreadHelper.cs
```

### CxWrapper/DevAssist/ (NEW)

```
CxWrapper/DevAssist/                            # Epic 3: CLI Wrappers
├── ICliWrapper.cs
├── BaseCliWrapper.cs
├── OssCliWrapper.cs
├── SecretsCliWrapper.cs
├── ContainersCliWrapper.cs
├── AscaCliWrapper.cs
└── IacCliWrapper.cs
```

---

## 🧪 2. Unit Tests - ast-visual-studio-extension-tests/

### DevAssist/ (NEW)

```
ast-visual-studio-extension-tests/DevAssist/
│
├── Core/
│   ├── Models/
│   │   ├── VulnerabilityTests.cs
│   │   ├── SeverityTests.cs
│   │   └── ScanResultTests.cs
│   │
│   ├── Diagnostics/
│   │   ├── DiagnosticProviderTests.cs
│   │   ├── DiagnosticManagerTests.cs
│   │   └── ProblemWindowIntegrationTests.cs
│   │
│   ├── GutterIcons/
│   │   ├── GutterIconProviderTests.cs
│   │   └── SeverityIconMapperTests.cs
│   │
│   └── Hover/
│       ├── HoverProviderTests.cs
│       └── HoverPopupFormatterTests.cs
│
├── Configuration/
│   ├── McpInstallerTests.cs
│   ├── McpConfigManagerTests.cs
│   └── ScannerSettingsManagerTests.cs
│
├── Remediation/
│   ├── CopilotIntegrationTests.cs
│   └── RemediationProviderTests.cs
│
├── Scanners/
│   ├── Common/
│   │   ├── BaseScannerServiceTests.cs
│   │   ├── ScannerRegistryTests.cs
│   │   └── FileEventListenerTests.cs
│   │
│   ├── OSS/
│   │   ├── OssScannerServiceTests.cs
│   │   ├── ManifestFileParserTests.cs
│   │   ├── MaliciousPackageDetectorTests.cs
│   │   └── OssDetectionEngineTests.cs
│   │
│   ├── Secrets/
│   │   ├── SecretsScannerServiceTests.cs
│   │   ├── SecretPatternMatcherTests.cs
│   │   └── SecretsDetectionEngineTests.cs
│   │
│   ├── Containers/
│   │   ├── ContainersScannerServiceTests.cs
│   │   ├── DockerfileParserTests.cs
│   │   └── ContainersDetectionEngineTests.cs
│   │
│   ├── ASCA/
│   │   ├── AscaScannerServiceTests.cs
│   │   ├── AscaCodeAnalyzerTests.cs
│   │   ├── AscaServiceAdapterTests.cs
│   │   └── AscaDetectionEngineTests.cs
│   │
│   └── IaC/
│       ├── IacScannerServiceTests.cs
│       ├── IacFileParserTests.cs
│       └── IacDetectionEngineTests.cs
│
├── Ignore/
│   ├── IgnoreServiceTests.cs
│   ├── IgnoreFileManagerTests.cs
│   └── ReviveServiceTests.cs
│
├── Telemetry/
│   ├── TelemetryServiceTests.cs
│   ├── MetricsCollectorTests.cs
│   └── AgentNameProviderTests.cs
│
└── Wrappers/
    ├── OssCliWrapperTests.cs
    ├── SecretsCliWrapperTests.cs
    ├── ContainersCliWrapperTests.cs
    ├── AscaCliWrapperTests.cs
    └── IacCliWrapperTests.cs
```

### test-data/devassist/ (NEW)

```
test-data/devassist/
├── mock-oss-results.json
├── mock-secrets-results.json
├── mock-containers-results.json
├── mock-asca-results.json
├── mock-iac-results.json
├── sample-package.json
├── sample-requirements.txt
├── sample-pom.xml
├── sample-dockerfile
├── sample-terraform.tf
├── sample-cloudformation.yaml
└── sample-kubernetes.yaml
```

---

## 🎨 3. UI Tests - UITests/

### DevAssist/ (NEW)

```
UITests/DevAssist/
├── SettingsPageTests.cs
├── WelcomeDialogTests.cs
├── IgnoreWindowTests.cs
├── GutterIconsUITests.cs
├── HoverPopupUITests.cs
├── ProblemWindowUITests.cs
├── ThemeTests.cs
└── ScannerToggleTests.cs
```

---

## 📊 Epic-to-Folder Mapping

### Epic 1: POC & Mock Data
**Production:**
- `CxExtension/DevAssist/Core/Models/`
- `CxExtension/DevAssist/Core/Diagnostics/`
- `CxExtension/DevAssist/Core/GutterIcons/`
- `CxExtension/DevAssist/Core/Hover/`
- `CxExtension/DevAssist/Core/MockData/`
- `CxExtension/DevAssist/Remediation/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Core/`
- `UITests/DevAssist/GutterIconsUITests.cs`
- `UITests/DevAssist/HoverPopupUITests.cs`
- `UITests/DevAssist/ProblemWindowUITests.cs`

### Epic 2: Settings, Welcome & MCP
**Production:**
- `CxExtension/DevAssist/UI/Settings/`
- `CxExtension/DevAssist/UI/Welcome/`
- `CxExtension/DevAssist/Configuration/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Configuration/`
- `UITests/DevAssist/SettingsPageTests.cs`
- `UITests/DevAssist/WelcomeDialogTests.cs`
- `UITests/DevAssist/ThemeTests.cs`

### Epic 3: CLI Wrapper Implementation
**Production:**
- `CxWrapper/DevAssist/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Wrappers/`

### Epic 4: OSS Real-Time Scanner
**Production:**
- `CxExtension/DevAssist/Scanners/OSS/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Scanners/OSS/`
- `test-data/devassist/mock-oss-results.json`
- `test-data/devassist/sample-package.json`
- `test-data/devassist/sample-requirements.txt`

### Epic 5: Secrets Real-Time Scanner
**Production:**
- `CxExtension/DevAssist/Scanners/Secrets/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Scanners/Secrets/`
- `test-data/devassist/mock-secrets-results.json`

### Epic 6: Containers Real-Time Scanner
**Production:**
- `CxExtension/DevAssist/Scanners/Containers/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Scanners/Containers/`
- `test-data/devassist/mock-containers-results.json`
- `test-data/devassist/sample-dockerfile`

### Epic 7: ASCA Real-Time Scanner
**Production:**
- `CxExtension/DevAssist/Scanners/ASCA/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Scanners/ASCA/`
- `test-data/devassist/mock-asca-results.json`

### Epic 8: IaC Real-Time Scanner
**Production:**
- `CxExtension/DevAssist/Scanners/IaC/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Scanners/IaC/`
- `test-data/devassist/mock-iac-results.json`
- `test-data/devassist/sample-terraform.tf`
- `test-data/devassist/sample-cloudformation.yaml`

### Epic 9: Ignore & Revive
**Production:**
- `CxExtension/DevAssist/Ignore/`
- `CxExtension/DevAssist/UI/Ignore/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Ignore/`
- `UITests/DevAssist/IgnoreWindowTests.cs`

### Epic 10: Telemetry
**Production:**
- `CxExtension/DevAssist/Telemetry/`

**Tests:**
- `ast-visual-studio-extension-tests/DevAssist/Telemetry/`

---

## 🏗️ Architecture Patterns

### 1. Scanner Pattern (Based on JetBrains/VSCode)

```csharp
// CxExtension/DevAssist/Scanners/Common/IScannerService.cs
public interface IScannerService
{
    Task ScanAsync(string filePath);
    bool ShouldScanFile(string filePath);
    void ClearDiagnostics();
    void Enable();
    void Disable();
}

// CxExtension/DevAssist/Scanners/Common/BaseScannerService.cs
public abstract class BaseScannerService : IScannerService
{
    protected ScannerConfig config;
    protected DiagnosticProvider diagnosticProvider;
    protected ICliWrapper cliWrapper;

    public abstract Task ScanAsync(string filePath);
    public abstract bool ShouldScanFile(string filePath);

    public virtual void ClearDiagnostics()
    {
        diagnosticProvider.ClearAll();
    }

    public virtual void Enable()
    {
        config.Enabled = true;
    }

    public virtual void Disable()
    {
        config.Enabled = false;
        ClearDiagnostics();
    }
}

// CxExtension/DevAssist/Scanners/OSS/OssScannerService.cs
public class OssScannerService : BaseScannerService
{
    private readonly ManifestFileParser parser;

    public override async Task ScanAsync(string filePath)
    {
        if (!ShouldScanFile(filePath)) return;

        var manifest = parser.Parse(filePath);
        var result = await cliWrapper.ScanAsync(manifest);
        diagnosticProvider.PublishDiagnostics(result);
    }

    public override bool ShouldScanFile(string filePath)
    {
        return parser.IsManifestFile(filePath);
    }
}
```

### 2. Scanner Registry Pattern

```csharp
// CxExtension/DevAssist/Scanners/Common/ScannerRegistry.cs
public class ScannerRegistry
{
    private readonly Dictionary<ScannerType, IScannerService> scanners;

    public void RegisterAllScanners()
    {
        RegisterScanner(ScannerType.OSS, new OssScannerService());
        RegisterScanner(ScannerType.Secrets, new SecretsScannerService());
        RegisterScanner(ScannerType.Containers, new ContainersScannerService());
        RegisterScanner(ScannerType.ASCA, new AscaScannerService());
        RegisterScanner(ScannerType.IaC, new IacScannerService());
    }

    public IScannerService GetScanner(ScannerType type)
    {
        return scanners.TryGetValue(type, out var scanner) ? scanner : null;
    }

    public IEnumerable<IScannerService> GetAllScanners()
    {
        return scanners.Values;
    }
}
```

### 3. File Event Listener Pattern

```csharp
// CxExtension/DevAssist/Scanners/Common/FileEventListener.cs
public class FileEventListener
{
    private readonly ScannerRegistry registry;

    public void RegisterListeners()
    {
        // Register VS file system events
        DTE.Events.DocumentEvents.DocumentSaved += OnDocumentSaved;
        DTE.Events.DocumentEvents.DocumentOpened += OnDocumentOpened;
    }

    private async void OnDocumentSaved(Document document)
    {
        var filePath = document.FullName;
        var scanners = registry.GetAllScanners()
            .Where(s => s.ShouldScanFile(filePath));

        foreach (var scanner in scanners)
        {
            await scanner.ScanAsync(filePath);
        }
    }
}
```

### 4. Diagnostic Provider Pattern

```csharp
// CxExtension/DevAssist/Core/Diagnostics/DiagnosticProvider.cs
public class DiagnosticProvider
{
    private readonly IVsTaskList taskList;

    public void PublishDiagnostics(IEnumerable<Vulnerability> vulnerabilities)
    {
        foreach (var vuln in vulnerabilities)
        {
            var task = new ErrorTask
            {
                Category = TaskCategory.CodeSense,
                ErrorCategory = MapSeverity(vuln.Severity),
                Text = vuln.Title,
                Document = vuln.FileName,
                Line = vuln.Line,
                Column = vuln.Column
            };

            taskList.Tasks.Add(task);
        }
    }

    private TaskErrorCategory MapSeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => TaskErrorCategory.Error,
            Severity.High => TaskErrorCategory.Error,
            Severity.Medium => TaskErrorCategory.Warning,
            Severity.Low => TaskErrorCategory.Message,
            _ => TaskErrorCategory.Message
        };
    }
}
```

---

## 📝 Implementation Notes

### .NET Project Structure Best Practices

1. **Namespace Convention:**
   ```csharp
   // Production code
   namespace ast_visual_studio_extension.CxExtension.DevAssist.Core.Models

   // Test code
   namespace ast_visual_studio_extension_tests.DevAssist.Core.Models
   ```

2. **Test Class Naming:**
   ```csharp
   // For class: OssScannerService.cs
   // Test class: OssScannerServiceTests.cs

   [TestClass]
   public class OssScannerServiceTests
   {
       [TestMethod]
       public async Task ScanAsync_WithValidManifest_ReturnsVulnerabilities()
       {
           // Arrange, Act, Assert
       }
   }
   ```

3. **Resource Files:**
   - XAML files must have `.xaml.cs` code-behind files
   - JSON files should be marked as "Embedded Resource" or "Content"
   - Icons should be in `Resources/` folders

4. **Project References:**
   - Test projects reference the main project
   - UI tests reference both main and test projects
   - Use NuGet packages for external dependencies

---

## 🚀 Quick Start Commands

### Create Production Folders

```powershell
# Navigate to production project
cd ast-visual-studio-extension

# Create DevAssist folder structure
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Core\Models"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Core\Diagnostics"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Core\GutterIcons"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Core\Hover"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Core\MockData"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\UI\Settings"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\UI\Welcome"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\UI\Ignore"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Configuration"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Remediation"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\Common"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\OSS"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\Secrets"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\Containers"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\ASCA"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Scanners\IaC"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Ignore"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Telemetry"
New-Item -ItemType Directory -Force -Path "CxExtension\DevAssist\Utils"
New-Item -ItemType Directory -Force -Path "CxWrapper\DevAssist"

Write-Host "✅ Production folder structure created!" -ForegroundColor Green
```

### Create Test Folders

```powershell
# Navigate to test project
cd ..\ast-visual-studio-extension-tests

# Create DevAssist test folder structure
New-Item -ItemType Directory -Force -Path "DevAssist\Core\Models"
New-Item -ItemType Directory -Force -Path "DevAssist\Core\Diagnostics"
New-Item -ItemType Directory -Force -Path "DevAssist\Core\GutterIcons"
New-Item -ItemType Directory -Force -Path "DevAssist\Core\Hover"
New-Item -ItemType Directory -Force -Path "DevAssist\Configuration"
New-Item -ItemType Directory -Force -Path "DevAssist\Remediation"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\Common"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\OSS"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\Secrets"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\Containers"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\ASCA"
New-Item -ItemType Directory -Force -Path "DevAssist\Scanners\IaC"
New-Item -ItemType Directory -Force -Path "DevAssist\Ignore"
New-Item -ItemType Directory -Force -Path "DevAssist\Telemetry"
New-Item -ItemType Directory -Force -Path "DevAssist\Wrappers"
New-Item -ItemType Directory -Force -Path "test-data\devassist"

Write-Host "✅ Test folder structure created!" -ForegroundColor Green
```

### Create UI Test Folders

```powershell
# Navigate to UI test project
cd ..\UITests

# Create DevAssist UI test folder
New-Item -ItemType Directory -Force -Path "DevAssist"

Write-Host "✅ UI test folder structure created!" -ForegroundColor Green
```

---

## ✅ Verification Checklist

After creating the folder structure:

- [ ] Production folders created in `ast-visual-studio-extension/`
- [ ] Test folders created in `ast-visual-studio-extension-tests/`
- [ ] UI test folders created in `UITests/`
- [ ] Folders added to Visual Studio solution
- [ ] Namespaces follow convention
- [ ] Ready to start Epic 1 implementation

---

## 📚 Related Documentation

- **BRANCHING_STRATEGY.md** - Git branching strategy for all 10 epics
- **IMPLEMENTATION_SUMMARY.md** - Overview, timeline, and milestones
- **QUICK_START_GUIDE.md** - Step-by-step getting started guide

