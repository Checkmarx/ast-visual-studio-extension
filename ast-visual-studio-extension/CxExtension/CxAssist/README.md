# CxAssist (DevAssist) – Checkmarx One Assist

This folder implements the **DevAssist** (Checkmarx One Assist) feature for the Visual Studio extension. The folder is named **CxAssist** in code; **DevAssist** is the product/feature name used in docs and UI.

## Structure (reusable layout)

- **Core/** – Shared logic and models
  - **Models/** – `Vulnerability`, `SeverityLevel`, `ScannerType` (reusable across scanners and UI)
  - **CxAssistConstants** – Display strings, theme names, line-number helpers
  - **AssistIconLoader** – **Reusable** theme detection and icon loading (PNG/SVG) for Quick Info, gutter, Findings window, and toolbar
  - **CxAssistDisplayCoordinator** – Single source of truth for findings; notifies gutter, underline, and Findings window
  - **CxAssistErrorHandler** – Centralized error logging
  - **FindingsTreeBuilder** – Builds tree data from vulnerability lists
  - **CxAssistErrorListSync** – Optional sync to VS Error List
  - **GutterIcons/** – Glyph tagger and factory for severity icons in the gutter
  - **Markers/** – Error tagger, Quick Info source, suggested actions (light bulb)
- **UI/FindingsWindow/** – Tool window and tree UI for Assist findings

## Reusable components

| Component | Purpose |
|-----------|---------|
| **AssistIconLoader** | Theme (`GetCurrentTheme`, `IsDarkTheme`), severity icon file names, `LoadPngIcon`, `LoadSvgIcon`, `LoadSeverityPngIcon`, `LoadSeveritySvgIcon`, `LoadBadgeIcon`. Use from any CxAssist or extension UI that needs CxAssist icons. |
| **CxAssistConstants** | `GetRichSeverityName`, `To0BasedLineForEditor`, `To1BasedLineForDte`, `StripCveFromDisplayName`, and all display/label constants. |
| **Core/Models** | `Vulnerability`, `SeverityLevel`, `ScannerType` – shared data contracts. |

## Adding new scanners or UI

- **Icons:** Use `AssistIconLoader` for theme-aware PNG/SVG under `Resources/CxAssist/Icons/{Dark|Light}/`.
- **Severity display:** Use `CxAssistConstants.GetRichSeverityName(SeverityLevel)`.
- **Findings updates:** Push findings through `CxAssistDisplayCoordinator.UpdateFindings` so gutter, squiggles, and Findings window stay in sync.

## Related docs

- `docs/FOLDER_STRUCTURE.md` – Planned DevAssist layout and epics
- `docs/DEVASSIST_GUTTER_UNDERLINE_PROBLEMWINDOW_POPUP_SUMMARY.md` – What VS provides vs what we implement
