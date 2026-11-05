# Add OAuth/DCR authentication option for the MCP connection

## Context

Today the extension always configures the Checkmarx MCP server in `~/.mcp.json` using an API-key bridge: `McpConfigManager.BuildCheckmarxServer` writes a `command: npx` / `mcp-remote ... --header Authorization:<apiKey>` entry, and `McpInstallService.Install` requires `config.ApiKey` to be set.

Visual Studio (2022 17.14+/2026) has native support for MCP servers declared with `"type": "http"` in `mcp.json` — when such a server responds with an OAuth challenge, VS itself performs the OAuth/Dynamic Client Registration (DCR) flow (browser sign-in, token storage, refresh) with no bridge process and no custom code in the extension. This is offered as a second, user-selectable authentication mode **for the MCP connection only**. CLI authentication (the extension's own login, `cx.exe` invocations) stays API-key-based exactly as today — this change does not touch `CxConfig`, `CxWrapper`, or any CLI argument building.

Outcome: in the Checkmarx One Assist settings page, the user picks "API Key" (current behavior, default) or "OAuth" for how the MCP server entry in `.mcp.json` authenticates. The choice is persisted and only takes effect the next time MCP is (re)installed — same trigger points as today (Install MCP link, or the existing silent install on login/restore).

## Design

### 1. `McpAuthMode` enum (new file: `CxPreferences/Configuration/McpAuthMode.cs`)
```csharp
internal enum McpAuthMode { ApiKey, OAuth }
```

### 2. `McpConfigManager.cs`
- Keep `InstallOrUpdate(string apiKey, string mcpUrl, out configPath)` and `BuildCheckmarxServer` untouched (API-key path, existing tests keep passing).
- Add `InstallOrUpdateOAuth(string mcpUrl, out configPath)`: same read/merge/write pipeline as `InstallOrUpdate`, but built via a new `BuildCheckmarxServerOAuth(string mcpUrl)`:
  ```csharp
  new JObject {
      ["type"] = "http",
      ["url"] = mcpUrl,
      ["headers"] = new JObject { ["cx-origin"] = "VisualStudio" }
  }
  ```
  No `Authorization` header — VS performs DCR/OAuth itself when the server challenges. Reuse the same `ReadConfig`/`WriteConfig`/`StripJsonComments` helpers and the "changed if `desiredServer.ToString()` differs" comparison already used by `InstallOrUpdate`.
- `RemoveCheckmarxServer` is unchanged — it removes by server name regardless of shape, so uninstall/switch-mode cleanup works as-is.

### 3. `McpInstallService.cs`
- Add an `McpAuthMode authMode` parameter to `InstallAsync`, `InstallSilentlyAsync`, and the private `Install(...)` overloads (default `McpAuthMode.ApiKey` to keep any missed call site compiling/behaving as today).
- In `Install(...)`, after the existing tenant-enabled check, branch:
  - `ApiKey` → current call: `_configManager.InstallOrUpdate(config.ApiKey, mcpUrl, out configPath)`.
  - `OAuth` → `_configManager.InstallOrUpdateOAuth(mcpUrl, out configPath)`.
  - `mcpUrl` resolution (`ResolveMcpUrl(config.ApiKey)`) stays the same for both — it derives the tenant MCP URL from the API key JWT issuer, which is still needed even in OAuth mode since the user is still logged into the extension via API key.
  - Success message: OAuth path returns a distinct message, e.g. `"MCP configuration installed. Visual Studio will prompt you to sign in via your browser the first time the Checkmarx MCP server is used."`
- The `config.ApiKey` presence check stays required for both modes (it gates the tenant-enabled check and URL resolution; extension login is still API-key only).

### 4. `CxOneAssistSettingsModule.cs`
- Add `public McpAuthMode McpAuthMode { get; set; } = McpAuthMode.ApiKey;` — a plain public property, serialized by `DialogPage` exactly like the existing `bool`/`string` properties (`ContainersTool`, `McpEnabled`, etc.), no extra persistence code needed.

### 5. `CxOneAssistSettingsUI.cs` + `CxOneAssistSettingsUI.Designer.cs`
- Designer: add two `RadioButton` controls inside the existing `mcpGroupBox`, near `lnkInstallMcp`/`lnkEditMcp`: `rbMcpAuthApiKey` ("API Key") and `rbMcpAuthOAuth` ("OAuth (sign in via browser)"), grouped so only one can be checked.
- `Initialize` / `RefreshCheckboxesFromModule`: set `rbMcpAuthApiKey.Checked` / `rbMcpAuthOAuth.Checked` from `module.McpAuthMode`.
- `ApplyAuthenticationState` / `SetInteractiveControlsEnabled`: enable/disable the two radio buttons together with `lnkInstallMcp` (same `hasApiKey && mcpEnabled` gate) so they're inert until MCP is actually usable.
- `ApplyUiToModule`: persist `module.McpAuthMode = rbMcpAuthOAuth.Checked ? McpAuthMode.OAuth : McpAuthMode.ApiKey;` — set this **before** the existing `if (!module.McpEnabled) return;` guard, since the auth-mode choice is independent of the realtime-scanner checkboxes that guard protects.
- Add `RbMcpAuthApiKey_CheckedChanged` / `RbMcpAuthOAuth_CheckedChanged` handlers wired the same way as `CmbContainersTool_SelectedIndexChanged` (guard on `cxOneAssistSettingsModule == null || !CxPreferencesUI.IsAuthenticated()`, then `DebounceSyncAssistUi()`). This only updates the module property; it does **not** trigger a reinstall by itself — the actual `.mcp.json` rewrite happens only via the existing "Install MCP" link click or the existing silent-install paths on next login/restore.
- `LnkInstallMcp_LinkClicked`: pass `cxOneAssistSettingsModule.McpAuthMode` into `installService.InstallAsync(config, cxOneAssistSettingsModule.McpAuthMode, GetType())`.

### 6. `CxPreferencesUI.cs`
- At each existing `InstallSilentlyAsync(config, ownerType)` call site (login restore, fresh login welcome-dialog path, fresh login no-welcome-dialog path), fetch the `CxOneAssistSettingsModule` already in scope at that point and pass its `McpAuthMode`: `installService.InstallSilentlyAsync(config, oneAssistModule.McpAuthMode, ownerType)`. All these call sites already have the module reference in scope (`oneAssistRestore`, `oneAssistModule`).
- `ApplyAssistTenantLicenseAndMcpFlagsAsync`'s fallback branch (no Assist settings page found) keeps calling `InstallSilentlyAsync` with the default `McpAuthMode.ApiKey` (no module to read from — matches current behavior).

### 7. Tests
- `McpConfigManagerTests.cs`: add cases mirroring the existing `BuildCheckmarxServer_*` / `InstallOrUpdate_*` tests for the new OAuth path — `BuildCheckmarxServerOAuth_ContainsTypeAndUrl` (asserts `"type":"http"`, url, no `command`/`Authorization`), `InstallOrUpdateOAuth_WritesConfigAndReturnsChanged`, `InstallOrUpdateOAuth_WithNullUrl_UsesDefaultUrl`.
- `McpInstallServiceTests.cs` / `McpInstallServiceAsyncTests.cs`: update existing calls to pass `McpAuthMode.ApiKey` explicitly (keep coverage identical), add new cases for `McpAuthMode.OAuth` asserting `InstallOrUpdateOAuth` is invoked instead of `InstallOrUpdate`.
- `CxOneAssistSettingsModuleTests.cs`: add a test asserting `McpAuthMode` defaults to `ApiKey` and round-trips via `SaveSettingsToStorage`/`LoadSettingsFromStorage` like other properties.

## Verification
- `dotnet test ast-visual-studio-extension-tests` (or the `vstest.console.exe` command from CLAUDE.md) — all existing + new tests green.
- Build the extension (`msbuild /p:Configuration=Debug`), F5 into the experimental hive: open Tools → Options → Checkmarx One → Assist, confirm the two radio buttons appear, default to "API Key", persist across Options close/reopen.
- With a valid API key logged in and MCP tenant-enabled: select "OAuth", click "Install MCP", inspect `~/.mcp.json` — the `Checkmarx` entry should be `{"type":"http","url":...,"headers":{"cx-origin":"VisualStudio"}}` with no `command`/`Authorization`. Switch back to "API Key", click "Install MCP" again, confirm it reverts to the `npx`/`mcp-remote` shape.
- Trigger an MCP tool call in an OAuth-configured session and confirm VS prompts a browser sign-in (DCR flow) rather than failing silently.
