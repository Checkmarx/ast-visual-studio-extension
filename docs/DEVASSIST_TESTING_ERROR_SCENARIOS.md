# How to Test DevAssist Error-Handling Scenarios

This document describes how to verify that **third-party or VS errors** do not crash gutter, underline, problem window, or hover. Use a mix of **unit tests**, **manual tests**, and (optional) **test commands** that force exceptions.

---

## 1. Unit tests (automated)

### 1.1 DevAssistErrorHandler

Run the **DevAssistErrorHandlerTests** (see `ast-visual-studio-extension-tests/cx-unit-tests/cx-extension-tests/DevAssistErrorHandlerTests.cs`):

- **TryRun** – When the action throws, `TryRun` returns `false` and does not rethrow. When the action succeeds, it returns `true`.
- **TryGet** – When the function throws, `TryGet` returns the given default and does not rethrow. When it succeeds, it returns the function result.
- **LogAndSwallow** – When given a non-null exception, it does not throw (e.g. call with null and with a real exception; verify no throw).

Run from Visual Studio: **Test Explorer** → run **DevAssistErrorHandlerTests**.

Or from repo root (when the solution builds successfully):

```powershell
dotnet test ast-visual-studio-extension-tests\ast-visual-studio-extension-tests.csproj --filter "FullyQualifiedName~DevAssistErrorHandlerTests"
```

---

## 2. Manual tests (in the VS Experimental Instance)

Goal: confirm that when something goes wrong (or is simulated), the IDE stays stable and other features still work.

### 2.1 Gutter and underline (normal path)

1. Deploy the extension (F5 or install VSIX in Experimental Instance).
2. Open a **C# file** (e.g. any `.cs` in the solution).
3. After ~1 second you should see **gutter icons** and **underlines** (mock data).
4. **Verify:** No crashes; Output window may show `DevAssist:` debug lines but no unhandled exceptions.

### 2.2 Problem window

1. In the same instance, open **Checkmarx** tool window and switch to **DevAssist** tab (or use the command that shows DevAssist findings).
2. **Verify:** Findings list appears (mock or from last opened file). If you open the window before opening a C# file, mock data is shown; if after, data from the coordinator is shown.
3. **Verify:** No crash when opening the window or switching tabs.

### 2.3 Hover

1. Hover the mouse over a line that has a **gutter icon** / underline (e.g. line 1, 3, 5 in the mock data).
2. **Verify:** Rich hover popup appears; closing it or moving away does not crash.
3. Hover over a line **without** a finding. **Verify:** No popup, no crash.

### 2.4 Coordinator: one failure does not block others

This is hard to simulate without code changes. Option: temporarily make the **glyph tagger** return null for a buffer (or skip calling it in the coordinator) and confirm that **underline** and **problem window** still update when you open a file. Alternatively, rely on unit tests and code review for the coordinator’s per-step `TryRun` usage.

---

## 3. Optional: test command that forces exceptions (debug builds)

To simulate **third-party/VS-like** failures and confirm they are swallowed:

1. Add a **test menu command** (e.g. “DevAssist – Test error handling”) that:
   - Calls **DevAssistDisplayCoordinator.UpdateFindings** with a **null buffer** (expect no crash; coordinator logs and returns).
   - Or, in a **test/debug-only** code path, temporarily **throw** inside a callback (e.g. in `GetTags` or `GenerateGlyph`) and confirm:
     - The exception is caught and logged (e.g. in Debug Output with `[DevAssist]`).
     - The editor and other DevAssist features (other taggers, problem window, hover) continue to work.

2. Run the command from the Experimental Instance and check:
   - **Output** window (Show output from: Debug) for `[DevAssist]` messages.
   - That VS does **not** show an unhandled exception dialog and does not crash.

Example (pseudo-code) for a test command:

```csharp
// In a test command (e.g. TestDevAssistErrorHandlingCommand):
// 1. UpdateFindings(null, someList) → should not throw
DevAssistDisplayCoordinator.UpdateFindings(null, new List<Vulnerability>());
// 2. RefreshProblemWindow(null, ...) → should not throw
DevAssistDisplayCoordinator.RefreshProblemWindow(null);
// Show a message box: "If you see this, error handling did not crash."
```

---

## 4. What to check in Debug Output

When testing, open **Output** (View → Output), set “Show output from” to **Debug**. Look for:

- **Normal:** `DevAssist:`, `DevAssist Markers:`, `DevAssistDisplayCoordinator:` messages.
- **After a swallowed exception:** `[DevAssist] <context>: <exception message>` and stack trace. No unhandled exception dialog should appear.

---

## 5. Manual testing: path normalization and buffer-derived path

These checks verify the **file path handling** improvements (normalized key, path from buffer, real path in mock).

### 5.1 Real file path in findings

1. Deploy the extension (F5) and open a **C# file** from your solution (e.g. `SomeFolder\MyFile.cs`).
2. After ~1 second, gutter icons and underlines appear (mock data).
3. Open **Checkmarx** → **DevAssist** tab.
4. **Verify:** The findings list shows the **actual path** of the file you opened (e.g. full path to `MyFile.cs`), not a generic default path. This confirms the listener uses `GetFilePathForBuffer(buffer)` and passes it to mock data.

### 5.2 Multiple files (per-file storage)

1. Open **first** C# file (e.g. `FileA.cs`). Wait for mock findings.
2. Open **second** C# file (e.g. `FileB.cs`). Wait for mock findings.
3. Open **DevAssist** tab.
4. **Verify:** The problem window shows findings for **both** files (two file nodes). Closing one file does not clear the other file’s findings from the in-memory list until you call `UpdateFindings` with an empty list for that file (e.g. when real scan is integrated).

### 5.3 Optional: clear-on-empty (when integrated)

When the real scan is wired: after opening a file and showing findings, if the scan is re-run and returns **no issues**, the code should call `UpdateFindings(buffer, new List<Vulnerability>(), filePath: null)`. The coordinator will use the path from the buffer and **remove** that file from the per-file map, and the problem window will refresh via `IssuesUpdated`. Manual test: simulate that call (e.g. with a test command) and confirm the file disappears from the findings list.

---

## 6. Manual testing: DevAssist (Checkmarx) vs VS/compiler errors (hover)

Use these steps to verify **Checkmarx plugin findings** and **VS/compiler errors** separately and together.

### 6.1 Scenario A: Only DevAssist (Checkmarx) finding

**Goal:** See only our finding on a line (no C# error).

1. Deploy the extension (F5) and open any **C# file** (e.g. `DevAssistGlyphTagger.cs`).
2. Wait ~1 second. Mock data adds findings on **lines 1, 3, 5, 7, 9** (1-based). Pick a line that has **no** red squiggle from the compiler (e.g. line 5 if it’s valid code).
3. **Hover** over that line (or over the **gutter icon** / **our squiggle**).
4. **Verify:**
   - **Our custom popup** appears (DevAssist logo, severity, “Fix with Checkmarx One Assist”, “View details”).
   - Optional: hover **only** over our squiggle → our **ErrorTag** tooltip (short text).
   - No compiler error tooltip (no red squiggle on that line).

### 6.2 Scenario B: Only VS/compiler error

**Goal:** See only a C# compiler error on a line (no DevAssist finding).

1. In the same Experimental Instance, open a C# file (or use another file).
2. On a line that does **not** have a DevAssist mock finding (e.g. **line 2** or **line 10**), add a **compile error**, e.g.:
   - `NotARealType x = null;`  (CS0246 – type not found), or
   - `int a = undefinedVar;`  (CS0103 – name not found).
3. Save the file. You should see a **red squiggle** on that line (and in Error List).
4. **Hover** over the **red squiggle** (or the error text).
5. **Verify:**
   - **Compiler/VS tooltip** appears (e.g. “CS0246: The type or namespace name ‘NotARealType’ could not be found”).
   - No DevAssist popup (that line has no DevAssist finding).

### 6.3 Scenario C: Same line – both DevAssist finding and VS error

**Goal:** One line has both; verify you can see both (our popup + compiler tooltip).

**Reproduce (exact steps):**

1. **Start the extension**  
   Press **F5** to launch the Experimental Instance.

2. **Open a C# file**  
   e.g. **`DevAssistGlyphTagger.cs`** (or any `.cs` in the solution).  
   Wait **about 1 second** so mock data loads. You should see **gutter icons** and **squiggles** on **lines 1, 3, 5, 7, 9**.

3. **Put a compiler error on line 5**  
   - Go to **line 5** (Ctrl+G, type 5, Enter).  
   - **Replace the whole line** with this (invalid type so the compiler reports an error):
     ```csharp
     NotARealType x = null;
     ```
     So line 5 now has: **our mock finding** (from step 2) **and** a **C# error** (CS0246).

4. **Save the file** (Ctrl+S).  
   You should see **two** underlines on line 5 (or one combined): our DevAssist squiggle and the **red** compiler squiggle. The **Error List** may show CS0246 for that line.

5. **Hover over line 5** (over the text or the left part of the line):  
   - **Check:** The **DevAssist custom popup** appears (logo, severity, “Fix with Checkmarx One Assist”, “View details”). That is the Checkmarx finding.

6. **Hover over the red squiggle** (move the mouse onto the underlined `NotARealType` or the red underline):  
   - **Check:** The **compiler tooltip** appears, e.g. *“CS0246: The type or namespace name 'NotARealType' could not be found”*. That is the VS error.

7. **Summary**  
   On the same line you get: **our popup** for the DevAssist finding and **hover the red squiggle** for the compiler message. There is **no** single Quick Info bubble that shows both; the two are shown separately as above.

---

## 8. Manual testing: Multiple vulnerabilities on same line (JetBrains-style hover)

Use this to verify the **multi-issue hover UI**: when several findings are on the same line, the popup shows “N issues detected on this line” and **one card per vulnerability** (icon, title, description, Fix / View details / Ignore this).

**Steps:**

1. **Run the extension**  
   Press **F5** to start the Experimental Instance.

2. **Open any C# file**  
   e.g. `DevAssistGlyphTagger.cs` or `Program.cs`.  
   Wait **~1 second** so mock data loads. You should see **gutter icons** and **underlines** on lines **1, 3, 5, 7, 9**.  
   Mock data has **two vulnerabilities on line 5**: “High-Risk Package” (OSS) and “Medium Severity Finding” (ASCA).

3. **Hover over line 5**  
   Move the mouse over **line 5** (text or gutter).

4. **Verify the multi-issue popup**
   - Header: **“2 issues detected on this line Checkmarx One Assist”**.
   - **First card:** severity icon, “High-Risk Package”, description (e.g. Test High vulnerability…), links: Fix with Checkmarx One Assist | View details | Ignore this vulnerability.
   - **Second card:** severity icon, “Medium Severity Finding”, description (e.g. Test Medium vulnerability…), same three links.
   - Each card’s “Fix” / “View details” / “Ignore this” act on **that** vulnerability (e.g. View details shows the correct title/description).

5. **Single-issue line (sanity check)**  
   Hover over **line 1** or **line 7** (only one finding each).  
   **Verify:** The **single-issue** layout appears (one severity icon, one title, one description, one set of links), not the “N issues detected” header.

**Summary:** Line 5 triggers the JetBrains-style multi-vulnerability UI; other lines trigger the single-issue UI.

---

## 9. Summary

| Scenario | How to test (manual) |
|----------|----------------------|
| **Gutter / underline / problem window / hover** | Open C# file, open DevAssist tab, hover over lines with findings; confirm no crash. |
| **DevAssist only (Scenario A)** | Line with only mock finding (e.g. line 5, valid code); hover → our custom popup. |
| **VS/compiler only (Scenario B)** | Line with only C# error (e.g. line 2); hover red squiggle → compiler tooltip. |
| **Both on same line (Scenario C)** | Line 5 with mock + compile error; hover line → our popup; hover red squiggle → compiler tooltip. |
| **Multiple issues on same line (Section 8)** | Hover **line 5** → “2 issues detected on this line” + two full cards; hover line 1 or 7 → single-issue layout. |
| **Real file path in mock** | Open a specific C# file, open DevAssist tab; confirm path in list matches opened file. |
| **Per-file storage (multiple files)** | Open two C# files; confirm both appear in DevAssist findings. |
| **Coordinator with null/invalid input** | Optional test command: call `UpdateFindings(null, ...)`, `RefreshProblemWindow(null)`; confirm no throw. |
| **Simulated exception in callback** | Optional test command that throws in a guarded path; confirm log in Output and no crash. |

If all of the above pass, error handling for **gutter, underline, problem window, and hover** is behaving as intended when third-party or VS code causes (or simulates) errors, **path normalization and buffer-derived path** behave as designed, and the **multiple-vulnerability hover UI** matches the JetBrains-style design.
