# CLAUDE.md

Standardized documentation for the Checkmarx One Visual Studio Extension repository.

## Project Overview
A custom visual studio extension (VSIX) that gets published on microsoft visual studio marketplace.
Supported for VS 2022, VS2026. Must be backward compatible with VS 2022 version.

**Name:** Checkmarx One Visual Studio Extension

**Purpose:** IDE plugin that integrates Checkmarx One's security scanning capabilities (SAST, SCA, IaC Security) directly into Visual Studio 2022, enabling developers to identify and remediate vulnerabilities as they code.

**Status:** Production (active development and maintenance)

**Repository:** https://github.com/Checkmarx/ast-visual-studio-extension

**Key Features:**
- Run security scans from within Visual Studio
- Import scan results from Checkmarx One cloud account
- Result filtering, grouping, and triaging
- Direct navigation from vulnerability to source code
- Codebashing lesson links
- Error List integration with VS diagnostics

## Architecture

### High-Level Design
The extension follows a layered architecture:

1. **VS Package Layer** (`ast_visual_studio_extensionPackage.cs`) — Entry point for VS, async initialization
2. **Tool Window Layer** (`CxWindow.cs`, `CxWindowControl.xaml`) — Main UI container for scan results
3. **Service Layer** (`CxWrapper`) — CLI wrapper that executes `cx.exe` and handles API communication
5. **Display Layer** (Panels, Error List, Markers) — Results visualization
6. **Settings Layer** (`CxPreferences`) — Configuration management

### Key Components

**CLI Wrapper:**
- `CxWrapper.cs` — Executes Checkmarx CLI (`cx.exe`) with project/branch/scan parameters
- `Execution.cs` — Process management and output capture
- Parses JSON responses into model objects

**UI Components:**
- `CxWindowControl.xaml(.cs)` — Main scan results display with tabs, tree view, and filtering
- `ResultsTreePanel.cs` — Hierarchical result grouping (by file, severity, scanner)
- `ResultInfoPanel.cs` — Detailed vulnerability view with code path and remediation
- `Toolbar/` — Async dropdowns for Projects, Branches, and Scans

**Settings:**
- `CxPreferences/` — DialogPage for API key, tenant, environment configuration

### Data Flow: Scan Execution
1. User selects project/branch/scan from toolbar dropdowns
2. Click "Scan" or "Import Results" triggers CxWrapper
3. CxWrapper executes `cx.exe` CLI with parameters
4. JSON response parsed into response model objects
5. Results displayed in tree panel, error list, and editor markers

### Threading Model
- **UI Thread:** VS main thread (XAML event handlers)
- **Worker Threads:** Scan execution via `ThreadHelper.JoinableTaskFactory`
- **Synchronization:** Lock objects for singleton services

## Repository Structure

```
ast-visual-studio-extension/
├── ast-visual-studio-extension/              # Main extension project
│   ├── CxExtension/                          # Core extension logic
│   │   ├── Services/                         # Scanner services
│   │   ├── Panels/                           # Result display panels
│   │   │   ├── ResultsTreePanel.cs          # Grouped result tree
│   │   │   ├── ResultInfoPanel.cs           # Vulnerability details
│   │   │   └── ResultVulnerabilitiesPanel.cs
│   │   ├── Toolbar/                          # Dropdown controls
│   │   │   ├── ComboboxBase.cs              # Base async dropdown
│   │   │   ├── ProjectsCombobox.cs          # Project selector
│   │   │   ├── BranchesCombobox.cs          # Branch selector
│   │   │   └── ScansCombobox.cs             # Scan selector
│   │   ├── Commands/                         # VS command handlers
│   │   ├── Utils/                            # Utility classes
│   │   │   ├── CxUtils.cs                   # General utilities
│   │   │   ├── CxConstants.cs               # Magic strings, timeouts
│   │   │   ├── OutputPaneUtils.cs           # Output window logging
│   │   │   ├── ResultsFilteringAndGrouping.cs  # Filter logic
│   │   │   └── StateManager.cs              # State persistence
│   │   ├── Enums/                            # Filter enums (Severity, State, etc.)
│   │   ├── Resources/                        # Images, theme resources
│   │   ├── CxWindow.cs                      # Tool window pane
│   │   ├── CxWindowControl.xaml(.cs)        # Main UI control
│   │   └── CxInitialPanel.xaml(.cs)         # First-run credential setup
│   ├── CxWrapper/                            # CLI wrapper
│   │   ├── CxWrapper.cs                     # Main CLI executor
│   │   ├── Execution.cs                     # Process management
│   │   ├── Models/                           # Response DTOs (Results.cs, Scan.cs, Asca.cs, etc.)
│   │   ├── Exceptions/                       # Custom exceptions
│   │   └── Resources/                        # CLI configuration
│   ├── CxPreferences/                        # Settings UI (DialogPage)
│   │   └── Resources/                        # Settings RESX files
│   ├── Properties/                           # Assembly info
│   ├── ast-visual-studio-extension.csproj   # Project file
│   ├── source.extension.vsixmanifest        # VSIX metadata
│   └── log4net.config                        # Logging configuration
├── ast-visual-studio-extension-tests/        # Unit tests (xUnit)
│   ├── ast-visual-studio-extension-tests.csproj
│   ├── test-data/                            # Sample JSON responses
│   └── [test classes]
├── UITests/                                  # UI automation tests
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                           # PR build/test pipeline
│   │   ├── release.yml                      # VSIX publish pipeline
│   │   ├── ast-scan.yml                     # Security scanning
│   │   └── [other workflows]
│   ├── ISSUE_TEMPLATE/                      # Issue templates
│   └── PULL_REQUEST_TEMPLATE.md
├── docs/
│   ├── contributing.md                      # Contribution guidelines
│   └── code_of_conduct.md
├── ast-visual-studio-extension.sln          # Visual Studio solution
├── README.md
└── LICENSE.txt
```

## Technology Stack

**Language:** C# (.NET Framework 4.7.2 + .NET 6.0)

**IDE Integration:** Visual Studio 2022 (minimum required)

**UI Framework:** WPF (Windows Presentation Foundation) with XAML

**Package Manager:** NuGet

**Build System:** MSBuild

**Testing Framework:**
- xUnit (unit tests)
- Moq (mocking)
- coverlet (code coverage)

**Key NuGet Dependencies:**
- `Microsoft.VisualStudio.Shell.15.0+` — VS SDK
- `EnvDTE` — VS automation (DTE)
- `Newtonsoft.Json` — JSON parsing
- `log4net` — Logging
- `System.Configuration.ConfigurationManager` — Config management

**External Tools:**
- `cx.exe` — Checkmarx CLI (must be in PATH or configured)

**Database:** None (stateless, all data from Checkmarx API)

**Cloud Services:**
- Checkmarx One API (cloud tenant)
- (Optional) Codebashing platform

## Development Setup

### Prerequisites
- **.NET 6.0 Windows Desktop Runtime**
- **NuGet** (included with VS)
- **MSBuild** (included with VS)
- **Git**

### Local Environment Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Checkmarx/ast-visual-studio-extension.git
   cd ast-visual-studio-extension
   ```

2. **Restore NuGet packages:**
   ```bash
   nuget restore .
   dotnet restore .
   ```

3. **Open in Visual Studio:**
   - Open `ast-visual-studio-extension.sln`
   - Let Visual Studio restore packages automatically

4. **Configure Checkmarx CLI:**
   - Ensure `cx.exe` is available in system PATH or set `CX_CLI_PATH` environment variable
   - Test: `cx --version` in PowerShell

5. **Set debug configuration:**
   - Right-click `ast-visual-studio-extension` project → Properties
   - Debug tab: Start Action = "Start external program" = `devenv.exe`
   - Start Arguments: `/rootsuffix Exp` (uses experimental hive)
   - This isolates debug sessions from main VS installation

### Build Commands

**Build (Release):**
```bash
msbuild /p:Configuration=Release /p:DeployExtension=False
```

**Build (Debug):**
```bash
msbuild /p:Configuration=Debug
```

**Clean:**
```bash
msbuild /t:Clean
```

### Run/Debug

**Debug in VS (F5):**
1. Set `ast-visual-studio-extension` as startup project
2. Press F5
3. New VS instance launches with `/rootsuffix Exp`
4. Extension loaded in experimental hive
5. Breakpoints work as expected

**Install VSIX manually:**
```bash
# After build, VSIX is at: ast-visual-studio-extension\bin\Release\ast-visual-studio-extension.vsix
# Right-click → Install
# Or: devenv.exe /setup
```

### IDE Integration

**Hot Reload:**
- Not supported for VS extensions (requires rebuild + restart)

**Intellisense:**
- Automatic after restore (review project file for any MSBuild issues)

## Coding Standards

### Naming Conventions
- **Classes, Methods, Properties:** PascalCase
  ```csharp
  public class CxWindowControl { }
  public void ExecuteScan() { }
  public string ProjectName { get; set; }
  ```

- **Private Fields:** _camelCase
  ```csharp
  private CxWrapper _cxWrapper;
  private readonly ILog _logger;
  ```

- **Local Variables:** camelCase
  ```csharp
  var projectId = selectedProject.Id;
  string temporaryFile = Path.GetTempFileName();
  ```

- **Constants:** UPPER_SNAKE_CASE
  ```csharp
  private const int DEBOUNCE_DELAY = 2000;
  private const string EXTENSION_TITLE = "Checkmarx One";
  ```

### Code Style
- **Indentation:** 4 spaces (no tabs)
- **Line Length:** Max 120 characters (soft limit)
- **Async/Await:** Use throughout for UI responsiveness
- **Null Handling:** Null-coalescing operator (`??`), null-conditional (`?.`)
- **LINQ:** Prefer method syntax over query syntax
- **Comments:** Minimal; code should be self-documenting
  - Only add comments for WHY, not WHAT
  - Example: `// Debounce to prevent excessive CLI calls on rapid text changes`

### File Organization
- One public class per file
- Namespace matches folder structure
- `using` statements sorted alphabetically

### Error Handling
- Try-catch at system boundaries (CLI calls, file I/O)
- Log all exceptions via `ILog`
- Surface user-friendly messages via InfoBar
- Don't swallow exceptions silently

### Async/Threading
- Use `ThreadHelper.JoinableTaskFactory` for VS async operations
- Debounce file changes with `System.Timers.Timer`
- Synchronize singletons with `lock` statements
- Never block UI thread

### Documentation
- XML doc comments on public APIs
- Keep comments current with code changes
- No redundant comments ("Gets the name" when property is `Name`)

## Project Rules

### Don'ts
1. **Do not** hardcode credentials, API keys, or secrets in code
   - Store via VS Settings DialogPage (persisted to Windows Registry)
   - Never commit `.user` files or `.env` files

2. **Do not** make blocking calls on the UI thread
   - Use `async/await` and `ThreadHelper.JoinableTaskFactory`
   - Test with UI responsiveness metrics

3. **Do not** implement any REST API invocation neither in extension or wrapper source code
   - AST-CLI wrapper orchestrates AST-CLI command invocations instead
   - Stop when a wrapper AST-CLI command for functionality is not found.

4. **Do not** execute CLI commands without timeout
   - All CLI calls must have 30-second timeout
   - Handle timeout gracefully (user notification, retry logic)

5. **Do not** create new WPF windows/dialogs directly
   - Use VS services (InfoBar, MessageBox, Error List)
   - Maintain consistency with VS theme

6. **Do not** log sensitive information
   - Never log credentials, tokens, or personal data
   - Review log messages for security implications

7. **Do not** deploy to production without:
   - Unit test coverage for new code
   - Manual testing in experimental hive
   - Code review approval
   - CI/CD pipeline success

8. **Do not** modify .resx files via Visual Studio designer
   - Edit XML directly (known dotnet CLI compilation issue)
   - Use VS IDE for building (workaround for .resx bug)

### Constraints
- **Minimum VS Version:** 2022
- **Target Framework:** .NET Framework 4.7.2 (compatibility) + .NET 6.0 (modern)
- **Architecture:** x86, x64, ARM64 support
- **Backward Compatibility:** Maintain for last 2 versions of Checkmarx CLI
- **Performance:** Scans must complete within 30 seconds or timeout

### Branch Naming
- Feature: `feature/AST-<ticket#>-<description>`
- Bugfix: `hotfix/AST-<ticket#>-<description>`
- Release: `release/v<version>`

### Commit Message Format
```
[AST-12345] Brief description of change

Optional longer explanation if needed.

- Bullet point 1
- Bullet point 2
```

## Testing Strategy

### Unit Tests
**Location:** `ast-visual-studio-extension-tests/`

**Framework:** xUnit + Moq

**Coverage Target:** Minimum 70% for new code

**Patterns:**
- Arrange-Act-Assert (AAA)
- Mock CLI responses via `Moq`
- Mock UI components (ILog, IVsHierarchy, etc.)
- Test edge cases (null inputs, timeouts, malformed responses)

**Example:**
```csharp
[Fact]
public async Task ExecuteScan_WithValidProject_ReturnsResults()
{
    // Arrange
    var mockWrapper = new Mock<CxWrapper>();
    mockWrapper.Setup(w => w.GetScanResults(It.IsAny<string>()))
        .ReturnsAsync(new CxResults { /* ... */ });

    // Act
    var results = await mockWrapper.Object.GetScanResults("proj-123");

    // Assert
    Assert.NotNull(results);
    Assert.NotEmpty(results.Scans);
}
```

### Test Data
**Location:** `ast-visual-studio-extension-tests/test-data/`

**Contents:** Sample JSON responses from `cx.exe` for various scanners and scenarios

### Integration Tests
- Test CLI execution with real `cx.exe` (requires API key)
- Environment variable: `CX_APIKEY` (set in CI/CD only)
- Run in GitHub Actions only (not locally by default)

### Manual Testing Checklist
- [ ] Run SAST/SCA/IaC scan from toolbar
- [ ] Import scan results from cloud account
- [ ] Filter results by severity/state
- [ ] Navigate from result to source code
- [ ] Test settings save/load
- [ ] Test error handling (invalid credentials, network timeout)
- [ ] Test dark theme compatibility
- [ ] Test with VS 2022 17.2+ versions

### Test Execution
```bash
# Run all tests
vstest.console.exe /InIsolation .\ast-visual-studio-extension-tests\bin\Release\net60-windows\ast-visual-studio-extension-tests.dll

# Run specific test
vstest.console.exe /InIsolation .\ast-visual-studio-extension-tests\bin\Release\net60-windows\ast-visual-studio-extension-tests.dll /Tests:MyTest.MyMethod

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Limitations & Known Issues

### Current Issues
1. **.resx compilation error with dotnet CLI**
   - Workaround: Use Visual Studio IDE to build instead of `dotnet build`

### Platform Constraints
- Minimum VS version: 2022
- Timeout: 30 seconds (hard-coded)
- Lock file handling: simplified (does not support all lock file types)
- Storage: Results in memory (per session), settings in Windows Registry

### Quick Troubleshooting
- Slow dropdowns → Restart VS
- Settings not saved → Close/reopen tool window  
- Markers not appearing → Rebuild and reload extension

## Deployment & Release

### Release Process
1. **Tag on main branch:** `git tag v<version>`
2. **Push tag:** `git push origin v<version>`
3. **GitHub Actions:** `release.yml` workflow triggered
4. **Build VSIX:** Release configuration build
5. **Sign VSIX:** (if required by organization)
6. **Publish to Marketplace:** Via Checkmarx account
7. **User Install:** Download from Marketplace or right-click VSIX

### VSIX Artifacts
- **Location after build:** `ast-visual-studio-extension\bin\Release\ast-visual-studio-extension.vsix`
- **Size:** ~5-10 MB (includes dependencies)
- **Dependencies:** Included in VSIX (self-contained)
- Validate extension (VSIX) schema compatibility 

### Installation Methods
1. **From Marketplace:** Visual Studio → Extensions → Manage Extensions → search "Checkmarx"
2. **Manual VSIX:** Right-click `.vsix` file → Install Extension
3. **Command line:** `devenv.exe /setup` (repairs installations)

### Rollback
- User can uninstall via Extensions → Manage Extensions → Uninstall
- Previous version available on Marketplace history
- No database/state cleanup required

### Compatibility
- Tested on: Windows 10, Windows 11
- Target: Visual Studio 2022 (all editions)
- Framework: .NET Framework 4.7.2, .NET 6.0

## Performance Notes

- **Import results:** 5-15 sec (result count dependent); **Timeout:** 30 sec
- **Memory:** Typical 100-200 MB; with 1000+ results: 300-500 MB (tree virtualization via WPF)
- **Threading:** All long-running ops on background threads (no UI blocking)
- **Network:** API calls are blocking (optimization opportunity: make async)

## External APIs & Interfaces

### Checkmarx Integration
- **Checkmarx One API:** Accessed via `cx.exe` CLI (not direct REST); endpoints: Projects, Branches, Scans, Results
- **Checkmarx CLI (cx.exe):** Min version 2.3.0; path via `CX_CLI_PATH` or system PATH
- **Authentication:** API key via Windows Registry; 30-second timeout; manual retry only

### VS Automation
- **DTE:** `TextDocument` (editor), `Project` (solution navigation), `IVsOutputWindowPane` (logging)
- **Error List:** `ITableDataSource` integration for diagnostics
- **Future Interfaces:** `IRealtimeScannerService` contract for multi-scanner support

### Optional Services
- **Codebashing:** Hyperlinked lessons (no API calls required)
- **GitHub:** Marketplace hosting, CI/CD workflows (GitHub Actions)

## Security & Credentials

- **API Key Storage:** Windows Registry (plaintext ⚠️) — future: migrate to IVsCredentialStore (DPAPI)
- **API Scope:** Checkmarx One roles: `ast-scanner`, `view-policy-management`
- **Network:** HTTPS enforced by CLI; no proxy support yet
- **Sensitive Data:** Never log credentials/tokens; never commit `.user` or `.env` files
- **VSIX Signing:** Not implemented; Assembly signing enabled

## Operations & Troubleshooting

### Logging (log4net)
- **Levels:** DEBUG (verbose), INFO (events), WARN (recoverable errors), ERROR (fatal)
- **Output:** VS Output window ("Checkmarx One" pane), Debug output, optional file via log4net.config
- **Sanitization:** Mask API keys; avoid logging file paths or PII

**Example messages:**
```
[DEBUG] Executing: cx.exe scan --project proj-123 --branch main
[INFO] Scan completed. Found 5 issues.
[ERROR] CLI failed: cx.exe not found
```

### Debugging Checklist

| Issue | Solution |
|-------|----------|
| Extension not loading | Check Output window; verify API key set; `devenv.exe /setup` |
| cx.exe not found | Verify installed: `cx --version`; add to PATH or set `CX_CLI_PATH` |
| Scan timeout | Check network; verify API key permissions (ast-scanner); check Output window |
| Settings not saved | Close/reopen tool window; check `HKCU\Software\Checkmarx` registry |
| .resx build error | Use VS IDE to build (not `dotnet build`) |
| Slow dropdowns | Large project list (API latency); restart VS |

**Debug mode:** Build with `msbuild /p:Configuration=Debug`; F5 to launch; enable DEBUG in log4net.config

---

**Last Updated:** 2026-04-21

**Maintained By:** Checkmarx AST Integrations Team

**Contact:** See repository README for support channels
