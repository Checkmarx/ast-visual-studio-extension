# DevAssist POC: What Visual Studio Gives vs What We Built Ourselves

A short note in simple words: for each part of the POC, what Visual Studio already does, what it cannot do, and what we built ourselves (and why).

---

## 1. Gutter icon (the small icons left of the code)

**What Visual Studio already does:**  
Visual Studio can show a small icon in the strip on the left of the code (the “gutter”) when there is an error or warning on that line. It uses a fixed set of icons—for example, a red mark for errors and a yellow one for warnings. We could have used that and gotten one standard icon per line.

**Why we did not use it:**  
That built-in option only gives you the same error/warning style. You cannot use your own pictures (e.g. different icons for “Critical”, “High”, “Medium”, “Low”) or custom styling (like colours or layout). We wanted different icons for each severity so users can see at a glance how serious the finding is (like in reference). So we built our own.

**What we built instead:**  
We built our own way to show icons in that strip. Our code decides which line gets which icon and uses our own picture files (e.g. skull for malicious, shield for high). The main pieces are: **DevAssistGlyphTag** (stores “this line has this severity”), **DevAssistGlyphTagger** (figures out which lines need an icon), and **DevAssistGlyphFactory** (draws our icon in the gutter).

*Classes (for reference):*  
VS provides: **IErrorTag** / **ErrorTag** (default gutter icon for errors), and **IGlyphFactory** / **IGlyphFactoryProvider** (the mechanism to draw in the gutter). We implement: **DevAssistGlyphTag**, **DevAssistGlyphTagger**, **DevAssistGlyphTaggerProvider**, **DevAssistGlyphFactory**, **DevAssistGlyphFactoryProvider**.

**Why we did not use the older “line marker” API (IVsTextLineMarker):**  
Visual Studio also has an older way to put marks in the editor (used e.g. in our ASCA feature): **IVsTextLineMarker**, **IVsTextLines.CreateLineMarker**, **IVsTextMarkerClient**. We did not use that for DevAssist because: (1) It is the old system; the newer one fits better with the rest of our code. (2) It does not support our own images or styling—you only get the built-in mark types. (3) Our squiggles and hover already use the newer system, so we kept the gutter on the same system for consistency.

---

## 2. Underline (the squiggly line under the code)

**What Visual Studio already does:**  
Visual Studio can draw a squiggly line under text and show a small tooltip when you hover. We use that as-is. We do not draw the squiggle ourselves.

**What we had to add:**  
Visual Studio does not know where our “vulnerabilities” are. We had to tell it: “treat these lines as errors and show this text in the tooltip.” So we wrote **DevAssistErrorTagger**: it feeds the line and the message to Visual Studio, and Visual Studio does the rest (squiggle + tooltip).

*Classes (for reference):*  
VS provides: **IErrorTag**, **ErrorTag** (VS draws the squiggle and tooltip from these). We implement: **DevAssistErrorTagger**, **DevAssistErrorTaggerProvider** (they return **ErrorTag** for each line that has a vulnerability).

**In short:** We use the default squiggle and tooltip. We only added the logic that says which lines to underline and what text to show.

---

## 3. Problem window (list of findings)

**What Visual Studio already does:**  
Visual Studio has an “Error List” window (View → Error List) that shows build errors and warnings. Other add-ins can add their items there so everything appears in one list.

**Why we did not use it (for this POC):**  
In this POC we wanted our own window so we can show findings in our own way (e.g. grouped by file, with our own columns and actions). So we built a separate “DevAssist findings” window: **DevAssistFindingsWindow** and **DevAssistFindingsControl**. It is our own list/tree, not the built-in Error List.

*Classes (for reference):*  
VS provides: **Error List** window (extensions can add items via **IErrorList** and table sources). We implement: **DevAssistFindingsWindow**, **DevAssistFindingsControl**, **ShowFindingsWindowCommand** (our own tool window and UI).

**In short:** We did not put DevAssist findings into the standard Error List. We built our own window to show them.

---

## 4. Mouse hover popup (the box that appears when you hover)

**What Visual Studio already does:**  
When you hover over code, Visual Studio can show a small popup (Quick Info) with plain text—for example, type or description. We do use that system so our content can appear when Quick Info is shown.

**Why we also built our own popup:**  
That default popup is good for simple text only. It does not reliably show our rich layout: logo, severity icon, coloured badge, clickable links (“Fix with Checkmarx One Assist”, “View details”, etc.). So we added our own popup that appears on hover and shows our full design (**DevAssistHoverPopup**). That way users always see the logo, links, and layout we want.

*Classes (for reference):*  
VS provides: **IQuickInfoControllerProvider**, **IQuickInfoSourceProvider**, **IQuickInfoSource** (Quick Info session and default presenter). We implement: **DevAssistQuickInfoSource** (feeds content into Quick Info), **DevAssistQuickInfoController** (opens our popup on hover), **DevAssistQuickInfoSourceProvider**, **DevAssistQuickInfoControllerProvider**, **DevAssistHoverPopup** (our WPF popup with logo, links, layout).

**In short:** We still use Quick Info for simple content, but we show our own popup as well so the rich hover (logo, links, layout) always appears correctly.

---

## Summary (in one table)

| What we built        | What Visual Studio gives (VS classes) | What we implemented (our classes)                        |
|----------------------|---------------------------------------|---------------------------------------------------------|
| Gutter icon          | **IErrorTag**, **IGlyphFactory**, **IGlyphFactoryProvider** (one standard icon; we didn’t use for gutter) | **DevAssistGlyphTag**, **DevAssistGlyphTagger**, **DevAssistGlyphTaggerProvider**, **DevAssistGlyphFactory**, **DevAssistGlyphFactoryProvider** |
| Underline (squiggle) | **IErrorTag**, **ErrorTag** (VS draws squiggle and tooltip) | **DevAssistErrorTagger**, **DevAssistErrorTaggerProvider** (return ErrorTag per line) |
| Problem window       | **Error List**, **IErrorList** (we didn’t use) | **DevAssistFindingsWindow**, **DevAssistFindingsControl**, **ShowFindingsWindowCommand** |
| Hover popup          | **IQuickInfoSourceProvider**, **IQuickInfoSource** (Quick Info; we use for simple content) | **DevAssistQuickInfoSource**, **DevAssistQuickInfoController**, **DevAssistQuickInfoControllerProvider**, **DevAssistHoverPopup** |

---

## Where we already use Visual Studio’s built-in features

| Area | What we use from VS (inbuilt) |
|------|------------------------------|
| **Underline (squiggle)** | We use the built-in error layer fully. We only supply **IErrorTag** / **ErrorTag** (line + tooltip text); VS draws the squiggle and tooltip. No custom drawing. |
| **Gutter** | We use VS’s gutter mechanism (**IGlyphFactory**). We only supply our tag and our icon image; VS hosts the margin and calls our factory. We do not draw the margin ourselves. |
| **Hover** | We use the **Quick Info** system (**IQuickInfoSource**). We push content into it so the default session can show something. We also show our own popup for the rich layout. |
| **Theme** | We use VS theme colours in code (**EnvironmentColors**) and apply them after load so the popup respects light/dark theme. |

---

## Suggestions: where we could use more built-in features

1. **Problem window → Error List (IErrorList)**  
   **Current:** We have our own “DevAssist findings” window.  
   **Suggestion:** Also (or instead) add DevAssist findings to the **Error List** via **IErrorList** and a table source. Users would see findings in the same list as build errors, get “go to line” and filters for free, and might not need to open a separate window. We could keep our custom window for a richer view and use the Error List for a quick, built-in list.

2. **Hover → Rely more on Quick Info**  
   **Current:** We use Quick Info and a custom WPF popup so the rich hover (logo, links) always shows.  
   **Suggestion:** If we ever need a simpler experience, we could try feeding only **ContainerElement** / **ClassifiedTextElement** (text + basic formatting) into Quick Info and rely on the default presenter only—no custom popup. We would lose the custom logo and clickable links in the hover, but we would use the inbuilt feature only.

3. **Gutter → Standard error/warning icon (only if we drop custom icons)**  
   **Current:** We use a custom glyph tag and factory so we can show different icons per severity.  
   **Suggestion:** If we ever don’t need severity-specific icons, we could use the same **IErrorTag** we use for the squiggle and let VS show its default error/warning icon in the gutter. That would remove the need for **DevAssistGlyphTag** and **DevAssistGlyphFactory**, but we’d lose custom images (e.g. skull, shield).

4. **Navigating from Error List**  
   If we add items to the Error List (suggestion 1), we get built-in “click to go to line” and document opening. No extra code needed for that navigation.

**Summary:** We already use built-in features for the squiggle (fully), the gutter mechanism, and Quick Info. The main place to use *more* built-in is the **Error List (IErrorList)** for the list of findings; the rest is already aligned with VS where possible, with custom parts only where we need custom icons or rich hover layout.
