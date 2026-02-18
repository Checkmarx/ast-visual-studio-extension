# DevAssist POC: What Microsoft Visual Studio Gives vs What We Built Ourselves

A short explanation, in simple words, of what Visual Studio already provides, what it cannot do, and what we built ourselves (and why).

---

## 1. Gutter icon (small icons left of the code)

### What Visual Studio already does

Visual Studio can show a small icon in the left margin (the "gutter") when there is an error or warning on that line. It uses fixed built-in icons (e.g. red for error, yellow for warning).

If we used only that, we would get:

- One standard icon per line
- Standard error/warning look

The extension point is the **glyph margin** plus **IGlyphTag**, **IGlyphFactory**, **IGlyphFactoryProvider** (Microsoft.VisualStudio.Text.Editor, Microsoft.VisualStudio.Text.Tagging). The platform does **not** ship a built-in tag type that shows different icons per severity; diagnostics typically use the squiggle and Error List only.

### Why we did not use it directly

The built-in option:

- Does not allow our own custom images
- Does not support different icons per severity (Critical, High, Medium, Low, Malicious)
- Does not allow custom styling

We wanted:

- Different icons (e.g. per severity)
- Clear visual difference by severity
- A JetBrains-style experience

### What we built instead

We used Visual Studio's gutter mechanism, but plugged in our own logic and icons.

**We implemented:**

- **DevAssistGlyphTag** – marks "this line has this severity"
- **DevAssistGlyphTagger** – decides which lines get icons
- **DevAssistGlyphFactory** – draws our custom icon (16×16 per severity)
- **DevAssistGlyphTaggerProvider** / **DevAssistGlyphFactoryProvider** – MEF exports so VS discovers our tagger and factory

**Visual Studio provides:**

- **IGlyphTag**, **IGlyphFactory**, **IGlyphFactoryProvider**
- Glyph margin (hosts the icons)

We use the mechanism, but control the icon.

### Why we did not use the old line marker API

Visual Studio has an older API:

- **IVsTextLineMarker**
- **IVsTextLines.CreateLineMarker**
- **IVsTextMarkerClient**

We did not use it because:

- It is the old system.
- It does not support custom images.
- Our squiggles already use the newer tagging system.
- We wanted consistency.

### In short

We use the VS gutter mechanism; we only supply the custom tag type and icon.

**Key files:** `DevAssistGlyphTagger.cs`, `DevAssistGlyphTaggerProvider.cs`, `DevAssistGlyphFactory.cs`, `DevAssistGlyphTag` (in same file).

---

## 2. Underline (squiggly line under the code)

### What Visual Studio already does

Visual Studio can:

- Draw squiggly lines under text
- Show a tooltip on hover

We use this fully as-is.

**Visual Studio provides:** the **built-in error layer** plus **IErrorTag** / **ErrorTag** (Microsoft.VisualStudio.Text.Tagging). Any tagger that returns **ErrorTag** (error type + optional tooltip) gets a squiggle and tooltip drawn by VS. Same mechanism as compiler errors.

### What we added

Visual Studio does not know where our vulnerabilities are. We had to tell it:

- "This line is an error. Show this message."

**We implemented:**

- **DevAssistErrorTagger** – produces **ErrorTag** per line with a finding (tooltip = title/description)
- **DevAssistErrorTaggerProvider** – MEF export, creates/caches tagger per buffer

Visual Studio then:

- Draws the squiggle
- Shows the tooltip

### In short

We did not draw anything ourselves. We only tell Visual Studio where and what.

**Key files:** `DevAssistErrorTagger.cs`, `DevAssistErrorTaggerProvider.cs`.

---

## 3. Problem window (list of findings)

### What Visual Studio already provides

Visual Studio has the **Error List** window (View → Error List):

- Shows build errors, warnings, diagnostics
- Supports filtering
- Supports "click to go to line"

Extensions can add items via **EnvDTE80.ErrorList**, **ErrorItems**, **ErrorItem**, or **IVsErrorList** / **ErrorListProvider**.

### Why we did not use it (in this POC)

We wanted:

- Custom grouping (per file, JetBrains-like)
- Custom columns and layout
- Our own tool window (Checkmarx → DevAssist tab)
- List driven by our in-memory model and **IssuesUpdated** events

So we built:

- **DevAssistFindingsWindow** – our Checkmarx tool window with DevAssist tab (`ToolWindowPane`)
- **DevAssistFindingsControl** – custom WPF tree/list (FileNode, VulnerabilityNode; subscribes to **IssuesUpdated**; click to navigate)
- **DevAssistDisplayCoordinator** – holds per-file findings, raises **IssuesUpdated**, provides **GetAllIssuesByFile()**

This is our own tool window.

**Note:** We **do** use the Error List (EnvDTE80) in **DevAssistCompilerErrorsForLine** only to show compiler errors in the hover popup ("Also on this line (Compiler / VS)"), not for the DevAssist findings list.

### In short

We built our own list instead of using the built-in Error List.

**Key files:** `DevAssistFindingsWindow.cs`, `DevAssistFindingsControl.xaml(.cs)`, `DevAssistDisplayCoordinator.cs`.

---

## 4. Mouse hover popup

### What Visual Studio already does

Visual Studio has **Quick Info**:

- Shows a small popup when hovering
- Good for plain text
- Supports basic formatting (ContainerElement, ClassifiedTextElement, ClassifiedTextRun)

**Provided by:** **IQuickInfoSource**, **IQuickInfoSourceProvider**, **IQuickInfoBroker**, **IIntellisenseControllerProvider**.

We use this system for integration (e.g. trigger, session).

### Why we also built our own popup

Default Quick Info:

- Is mainly for text
- Does not reliably support rich layout
- Does not easily support: logos, severity badges, clickable links, custom layout, multiple cards, "Also on this line (Compiler / VS)" section

So we added:

- **DevAssistHoverPopup** – custom UserControl (XAML): logo, severity icon, single/multiple cards, compiler errors section, action links (Fix, View details, Ignore, etc.). Theme-aware.
- **DevAssistQuickInfoController** – subscribes to MouseHover; gets vulnerabilities from **DevAssistErrorTagger**; shows a **Popup** with **DevAssistHoverPopup** as content.
- **DevAssistQuickInfoSource** – optional simple text fallback via ContainerElement.
- **DevAssistSuggestedActionsSource** + **FixWithCxOneAssistSuggestedAction** / **ViewDetailsSuggestedAction** – light bulb actions.
- **DevAssistCompilerErrorsForLine** – reads **EnvDTE80.ErrorList** for file+line and passes compiler errors into the popup.

This gives us: logo, severity icon, styled layout, clickable actions ("Fix", "View details"), and compiler errors on the same line.

### In short

We use Quick Info for basic integration. We use our own popup for rich UI.

**Key files:** `DevAssistQuickInfoController.cs`, `DevAssistQuickInfoControllerProvider.cs`, `DevAssistHoverPopup.xaml` / `.xaml.cs`, `DevAssistQuickInfoSource.cs`, `DevAssistSuggestedActionsSource.cs`, `DevAssistQuickFixActions.cs`, `DevAssistCompilerErrorsForLine.cs`.

---

## Summary table

| Feature       | What Visual Studio gives                                      | What we implemented                                                                 |
|---------------|---------------------------------------------------------------|-------------------------------------------------------------------------------------|
| **Gutter icon** | IGlyphTag, IGlyphFactory, IGlyphFactoryProvider, glyph margin | DevAssistGlyphTag, DevAssistGlyphTagger, DevAssistGlyphFactory (+ providers)        |
| **Squiggle**  | IErrorTag / ErrorTag (VS draws it)                            | DevAssistErrorTagger, DevAssistErrorTaggerProvider                                  |
| **Problem list** | Error List, EnvDTE80.ErrorList, IErrorList                  | DevAssistFindingsWindow, DevAssistFindingsControl, DevAssistDisplayCoordinator      |
| **Hover**     | Quick Info system (IQuickInfoSource, IQuickInfoBroker, etc.)   | DevAssistHoverPopup, DevAssistQuickInfoController, DevAssistQuickInfoSource (+ light bulb) |

---

## Where we already use built-in features

| Area     | Built-in usage                                                                 |
|----------|---------------------------------------------------------------------------------|
| **Squiggle** | Fully built-in. We only provide ErrorTag; VS draws the squiggle and tooltip.  |
| **Gutter**   | Use VS gutter mechanism; we only supply custom tag and icon.                   |
| **Hover**    | Use Quick Info system; extend with custom popup.                                |
| **Theme**    | Use EnvironmentColors so UI follows light/dark theme.                          |

---

## Suggestions: where we could use more built-in features

### 1. Use Error List (IErrorList / EnvDTE80) for findings too

Instead of only using our custom findings window, we could:

- Add DevAssist findings into the built-in Error List
- Get filtering and navigation for free
- Let users see DevAssist issues together with build errors

We could: keep our rich window **and** feed the Error List.

### 2. Use only Quick Info (simpler hover)

If we accept: no logo, no clickable links, simpler formatting, we could:

- Use only **ContainerElement** / **ClassifiedTextElement** / **ClassifiedTextRun**
- Remove the custom popup

This would reduce complexity but we would lose the rich UI.

### 3. Use standard gutter icons (if custom icons not needed)

If we stop needing severity-based custom icons, we could:

- Return only **IErrorTag** (or use a single built-in glyph type)
- Let Visual Studio draw its default error/warning icon

We would remove: DevAssistGlyphTag, DevAssistGlyphFactory (and custom icon set).  
We would lose: custom severity visuals.

---

## Final summary

We already use built-in features from Microsoft Visual Studio wherever possible:

- **Squiggles** → fully built-in
- **Gutter mechanism** → built-in hosting, custom icon
- **Hover** → built-in Quick Info + custom UI
- **Theme** → built-in theme system

The only major area where we **completely replaced** a built-in feature is the **Error List**, where we chose to build our own findings window for flexibility and richer UI.

---

## Single update path (how the four surfaces stay in sync)

All four surfaces are updated from **one place**: **DevAssistDisplayCoordinator.UpdateFindings(buffer, vulnerabilities, filePath)**. The coordinator updates the glyph tagger and error tagger (gutter and underline), and updates its per-file map and raises **IssuesUpdated** (problem window). The popup and Quick Fix read from the same tagger data when the user hovers or invokes the light bulb. One logical source of truth; no separate update path per surface.
