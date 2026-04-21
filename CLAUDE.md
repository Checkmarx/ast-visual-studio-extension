# CLAUDE.md

Standardized documentation for the Checkmarx One Visual Studio Extension repository.

## Project Overview

**Name:** Checkmarx One Visual Studio Extension

**Purpose:** IDE plugin that integrates Checkmarx One's security scanning capabilities (SAST, SCA, IaC Security) directly into Visual Studio 2022, enabling developers to identify and remediate vulnerabilities as they code.

**Status:** Production (active development and maintenance)

**Repository:** https://github.com/Checkmarx/ast-visual-studio-extension

**Key Features:**
- Run security scans from within Visual Studio
- Import scan results from Checkmarx One cloud account
- Real-time ASCA (AI Secure Coding Assistant) scanning on file changes
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
4. **Scanner Layer** (`ASCAService`) — Real-time code analysis with debouncing
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

**Real-time Scanner:**
- `ASCAService.cs` — Monitors text changes, debounces (2000ms), executes scan
- `ASCAUIManager.cs` — Updates error list and editor markers with diagnostics
- Scans: .java, .cs, .go, .py, .js, .jsx files

**Settings:**
- `CxPreferences/` — DialogPage for API key, tenant, environment configuration
- `SettingsModule.cs` — Validates and loads settings at startup

### Data Flow: Scan Execution
1. User selects project/branch/scan from toolbar dropdowns
2. Click "Scan" or "Import Results" triggers CxWrapper
3. CxWrapper executes `cx.exe` CLI with parameters
4. JSON response parsed into CxResults, CxScan, CxScanDetail objects
5. Results displayed in tree panel, error list, and editor markers

### Threading Model
- **UI Thread:** VS main thread (XAML event handlers)
- **Worker Threads:** Scan execution via `ThreadHelper.JoinableTaskFactory`
- **Debouncing Timer:** `System.Timers.Timer` for ASCA (2000ms)
- **Synchronization:** Lock objects for singleton services

## Repository Structure

```
ast-visual-studio-extension/
├── ast-visual-studio-extension/              # Main extension project
│   ├── CxExtension/                          # Core extension logic
│   │   ├── Services/                         # Scanner services
│   │   │   ├── ASCAService.cs               # Real-time ASCA scanner
│   │   │   └── ASCAUIManager.cs             # ASCA diagnostics display
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
│   │   ├── Models/                           # Response DTOs
│   │   │   ├── CxResults.cs
│   │   │   ├── CxScan.cs
│   │   │   ├── CxScanDetail.cs
│   │   │   ├── CxAsca.cs
│   │   │   └── [other models]
│   │   ├── Exceptions/                       # Custom exceptions
│   │   └── Resources/                        # CLI configuration
│   ├── CxPreferences/                        # Settings UI
│   │   ├── CxOneAssistSettingsModule.cs
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
- **Visual Studio 2022** (Community, Professional, or Enterprise)
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
   - Use VS Settings/DialogPage and Windows Credential Manager
   - Never commit `.user` files or `.env` files

2. **Do not** make blocking calls on the UI thread
   - Use `async/await` and `ThreadHelper.JoinableTaskFactory`
   - Test with UI responsiveness metrics

3. **Do not** execute CLI commands without timeout
   - All CLI calls must have 30-second timeout (CxConstants.CLI_TIMEOUT)
   - Handle timeout gracefully (user notification, retry logic)

4. **Do not** create new WPF windows/dialogs directly
   - Use VS services (InfoBar, MessageBox, Error List)
   - Maintain consistency with VS theme

5. **Do not** log sensitive information
   - Never log credentials, tokens, or personal data
   - Review log messages for security implications

6. **Do not** deploy to production without:
   - Unit test coverage for new code
   - Manual testing in experimental hive
   - Code review approval
   - CI/CD pipeline success

7. **Do not** modify .resx files via Visual Studio designer
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
- [ ] Test ASCA on Python/C# files
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

## Known Issues

### Current Issues
1. **.resx compilation error with dotnet CLI**
   - Symptom: `Non-string resources require System.Resources.Extensions assembly`
   - Root cause: .resx files in CxPreferences folder
   - Workaround: Use Visual Studio IDE to build instead of `dotnet build`
   - Status: Pre-existing, not caused by recent changes

### Limitations
- Minimum VS version is 2022 (no support for older versions)
- Scan execution timeout is hard-coded to 30 seconds
- Real-time ASCA scanner is limited to .java, .cs, .go, .py, .js, .jsx files
- Lock file handling in OSS scanning is simplified (does not support all lock file types)

### Known Workarounds
- If dropdown population is slow: Restart VS (clears cache)
- If settings are not saved: Close/reopen tool window
- If markers don't appear: Rebuild project and reload extension

## Database Schema

**No database used.** Extension is stateless:
- Results stored temporarily in memory during session
- Settings stored in Windows Registry via VS DialogPage
- No local persistence between sessions

**Future:** Consider migration to IVsCredentialStore for encrypted credential storage.

## External Integrations

### Checkmarx One API
- **Purpose:** Fetch projects, branches, scans, and results
- **Authentication:** API key (stored in Windows Registry, plaintext ⚠️)
- **Timeout:** 30 seconds per request
- **Retry Logic:** None (user must retry manually)
- **Error Handling:** User notification via InfoBar

### Checkmarx CLI (cx.exe)
- **Purpose:** Execute local ASCA scans, validate projects
- **Path:** System PATH or `CX_CLI_PATH` environment variable
- **Minimum Version:** 2.3.0
- **Invocation:** Synchronous with output capture and JSON parsing

### Codebashing Platform (Optional)
- **Purpose:** Lesson links in vulnerability details
- **Integration:** Hyperlinks in UI (no API calls)
- **Status:** Not required for core functionality

### GitHub
- **Purpose:** Repository hosting, issues, pull requests
- **CI/CD:** GitHub Actions workflows for build, test, release
- **Marketplace:** VSIX published to Visual Studio Marketplace

## Deployment Info

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

## Performance Considerations

### Scan Execution
- **ASCA scan:** 2-5 seconds (file size dependent)
- **Import results:** 5-15 seconds (result count dependent)
- **Timeout:** 30 seconds (hard-coded)

### Memory Usage
- **Typical:** 100-200 MB
- **With large scan results (1000+ items):** 300-500 MB
- **Mitigation:** Tree virtualization via WPF ItemsControl

### UI Responsiveness
- **Debounce delay:** 2000ms on text changes (prevents excessive scans)
- **Async operations:** All long-running operations on background threads
- **UI freezes:** Should not occur (design enforces async)

### Network
- **API calls:** Blocking (not async) — could be optimized
- **Timeout:** 30 seconds per request
- **Retry:** None (manual retry only)

### Optimization Opportunities
- Implement async API calls (currently blocking)
- Cache scan results locally between sessions
- Implement incremental scanning (scan only changed files)
- Add connection pooling for CLI invocations

## API / Endpoints / Interfaces

### Checkmarx One API Endpoints Used
(Indirect via CLI wrapper, not direct REST calls)

- `GET /api/projects` — Fetch project list
- `GET /api/branches` — Fetch branches for project
- `GET /api/scans` — Fetch scan list
- `GET /api/results` — Fetch scan results

### Extension Public Interfaces
- **IRealtimeScannerService** (future) — Scanner contract for ASCA, Secrets, etc.
- **ITableDataSource** (VS API) — Error List integration
- **ISuggestedAction** (VS API) — Quick Fix bulbs

### DTE Automation
- `EnvDTE.TextDocument` — Editor content access
- `EnvDTE.Project` — Solution/project navigation
- `IVsOutputWindowPane` — Output window logging

## Security & Access

### Credentials
- **API Key Storage:** Windows Registry (plaintext ⚠️)
  - **Issue:** Not encrypted, vulnerable to local admin access
  - **Mitigation:** Use IVsCredentialStore (VS Credential Manager with DPAPI) — planned for future
- **Tenant URL:** Stored in registry
- **Environment:** Selection stored in registry

### Access Control
- **No role-based access:** Extension inherits VS user's permissions
- **API key scope:** Defined at Checkmarx One (roles: ast-scanner, view-policy-management)
- **File access:** Read-only for source files

### Code Signing
- **VSIX Signing:** Not currently implemented (can be added via cert)
- **Assembly Signing:** Enabled (internal key)

### Sensitive Data Handling
- **Never log:** API keys, credentials, tokens
- **Never commit:** `.user` files, `.env` files, local settings
- **Minimize:** Personal identifiable information in results display

### Audit
- **Logging:** All CLI invocations logged to Output window (sanitized)
- **No audit trail:** Extension does not track user actions

### Network Security
- **HTTPS Only:** Checkmarx API calls (enforced by CLI)
- **No proxy support:** Currently unsupported (can be added)
- **Certificate validation:** Standard OS validation

## Logging

### Framework
- **Library:** log4net
- **Configuration:** `log4net.config`

### Log Levels
- **DEBUG:** Verbose tracing (CLI command invocations, file operations)
- **INFO:** User-visible events (scan started, completed, results imported)
- **WARN:** Recoverable errors (invalid config, retry on failure)
- **ERROR:** Unrecoverable errors (CLI crash, network failure)

### Log Destinations
1. **Output Window:** "Checkmarx One" pane (visible to user)
2. **Debug Output:** `Debug.WriteLine()` (visible in VS Debugger)
3. **File:** Optional (can be configured in log4net.config)

### Log Sanitization
- Remove/mask API keys before logging
- Avoid logging full file paths (security)
- Avoid logging personal data

### Example Log Messages
```
[DEBUG] Executing CLI command: cx.exe scan asca --file-source C:\temp\file.cs
[INFO] ASCA scan completed successfully. Found 5 issues.
[WARN] Timeout retry #1 for GetScanResults (30s timeout exceeded)
[ERROR] CLI command failed: The system cannot find the path specified (cx.exe)
```

### Troubleshooting via Logs
- Check Output window for error messages
- Enable DEBUG level for verbose tracing
- Look for CLI invocation messages to understand parameter passing
- Search for "Error" or "Warning" to identify issues

## Debugging Steps

### Common Issues & Solutions

**1. Extension not loading in VS**
- Check Output window for initialization errors
- Verify API key is set (Tools → Options → Checkmarx One)
- Restart VS
- Try: `devenv.exe /setup` to repair

**2. "cx.exe not found"**
- Verify Checkmarx CLI installed: `cx --version` in PowerShell
- Add to PATH or set `CX_CLI_PATH` environment variable
- Restart VS after PATH change

**3. Scan hangs/times out**
- Check network connectivity to Checkmarx One
- Verify API key has correct permissions (ast-scanner role)
- Check Output window for CLI error messages
- Try: Restart VS, kill stray cx.exe processes

**4. Settings not saved**
- Close tool window and reopen
- Check Windows Registry: `HKCU\Software\Checkmarx`
- Verify no read-only registry permissions

**5. .resx build error**
- Use Visual Studio IDE to build (not `dotnet build`)
- Clean solution and rebuild
- Delete `obj/` and `bin/` folders, retry

**6. Slow dropdown population**
- Large number of projects/branches (API latency)
- Network connectivity issues
- Workaround: Restart VS (clears cache)

**7. Diagnostic markers not appearing**
- ASCA must be enabled in settings (default: on)
- File extension must be .java, .cs, .go, .py, .js, .jsx
- Text change must be saved (not unsaved edits)
- Wait 2+ seconds after typing (debounce delay)
- Check Output window: "Start ASCA scan On File: ..."

### Debug Mode Execution
```bash
# Build in Debug configuration
msbuild /p:Configuration=Debug

# F5 to launch experimental VS instance
# Breakpoints work as normal
# Output window shows Debug.WriteLine() messages
```

### Enable Verbose Logging
```xml
<!-- In log4net.config -->
<level value="DEBUG" />  <!-- Change from INFO to DEBUG -->
```

### Inspect State
- **CxWindow** — View open tool window state
- **StateManager** — Inspect persisted state (Registry)
- **Output Pane** — All logged operations
- **Breakpoints** — Pause on specific methods

---

**Last Updated:** 2026-04-21

**Maintained By:** Checkmarx AST Integrations Team

**Contact:** See repository README for support channels
