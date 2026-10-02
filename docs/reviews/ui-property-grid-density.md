---
id: review-ui-property-grid-density
title: UI review — property grid density pass (after the native build)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, properties, property-grid, density, native-ui, accessibility, typography]
links:
  - {to: mockup-property-grid, rel: documents}
  - {to: review-ui-property-grid, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: property-grid-rulings, rel: relates-to}
review-by: 2026-12-30
summary: >-
  Elevate-mode density pass on the built property grid (dade554). The operator found the sheet "too large". The cause
  is measured in source: values at 14 px beside 12 px labels, rows at 28/32 px, 33 px group headers and 32 px Kind
  options from Avalonia Fluent defaults, 12 px insets, a value column far from its label. The proposal (after repair
  cycle 1 and the operator's 11 px ruling) is one 11/14 type size with nothing below 11, 18/24 px rows with a 24 px
  target around a 20 px field, 24 px headers, an app Text size setting, and the value next to its
  label. On the anchor state, selection content falls from 625 to 423 px and the Wing from 339 to 231 px, with every
  WCAG floor kept.
review-suggested: []
---

# UI review — property grid density pass

*`/ui-design`, mode **elevate**, on the built property grid (`dade554`). Track UID, session `f19a2b12-uid`, 2026-10-01.*

**The operator, verbatim:** "i still think the property sheet is not on par: the font sizing, spacing and structure is
too 'large'".

**Proposal:** [`docs/mockups/property-grid.html`](../mockups/property-grid.html). The harness has **Density: Dense
(proposal) / As built (native, dade554)** and **Text: 100 % / 200 %**, and a live before/after table under the pane.
Tokens and rules: `DESIGN.md` frontmatter (`typography.prop*`, `spacing.prop-*`) and §12.0f "Density".

## 1. Verdict

> **As built: the density is wrong in ways the source explains.** Most of the "large" feel is not in our tokens. It
> comes from Avalonia Fluent defaults the build inherited: 14 px control text, a 32 px chevron button in every Expander
> header, and a 32 px grid inside every RadioButton template. On top of those sit our own 28/32 px row pitch and 12 px
> insets.
> **Highest-leverage change:** one grid type token, `PropFontSize` 11 / `PropLineHeight` 14 (DR-DEN-3; 12/16 in cycle 0), applied to label, value,
> unit and input. Together with it, the row-pitch tokens (20 / 24). It is a `Styles.axaml` change with no new control,
> and it removes the most visible defect: values louder than their labels, with units off their baseline.

## 2. Direction (words before pixels)

- **Who.** The same foil designer as before, glancing from the Plan view. The pane must show more of the selection
  and the Wing at once and read as an *instrument*: Premiere's Effect Controls, Fusion's and Rhino's property panels,
  VS Code's compact trees. It must not read as a *settings form* (the VS Code Settings editor's 12/14/18 px item
  padding is the shape to avoid; see §4).
- **Three adjectives / their opposites.** *Compact* (not roomy) · *even* (not loud: one type size, values the same
  size as labels) · *tabular* (not proportional: digits align in a column).
- **Structure.**
  - Groups are separated by a rule, not by an 8 px gap plus a 33 px band.
  - The value sits next to its label (fixed value column), with the free space at the right edge rather than between
    label and value.
  - "How these are measured" becomes a link-style disclosure.
  - The identity block shrinks to one 13 px title line.
- **Personality.**
  - *Type:* one 11/14 size for every grid row and for notes and messages, nothing below 11 (DR-DEN-3; cycle 0
    proposed 12/16); 13/18 for the identity.
  - *Space:* rows abut; 8 px pane inset; 4 px inside the rail.
  - *Colour:* unchanged.

## 3. Measurements of the build (DX23)

Exact values come from the source at `dade554`: `Styles.axaml`, `PropertiesPane.axaml`, `PropertiesPane.axaml.cs`, and
the Avalonia 11.3.14 Fluent templates (fetched from the `release/11.3.14` tag). The capture column is the
Coordinator's reading of `docs/proof/property-grid-density/as-built-foil-wing.png` (Retina; about 1 pt per 2 px).
**I did not view the capture**, to keep it out of context (CTX-E); its numbers are quoted.

| What | From source (exact) | Capture (≈) | Why |
|---|---|---|---|
| Value font | **14** (Window `BodySize`; `TextBlock.prop-value` sets no size) | 15 | inherited from `Window`, not a grid token |
| Label / unit font | **12** (`CaptionSize`) | 13 | `prop-label`, `prop-unit` |
| Unit vs value baseline | different sizes, each `VerticalAlignment=Center` in its own TextBlock → baselines differ; **1.6 px** in the emulation | 2 (unit lower) | two font sizes centred separately; the sign depends on line-box centring (Inferred) |
| Fact / estimate row pitch | `prop-value` MinHeight **24** + `StackGap1` **4** = **28** | 28 | |
| Input height / row pitch | `TextBox.prop-input` MinHeight **28** (Fluent padding 10,6,6,5) + 4 = **32** | 27 / 32 | |
| Group header | `PropHeadHeight` 24 is overridden in practice by the Fluent chevron button, **32 × 32** (`ExpanderChevronButtonSize`), + 1 | 33 | Fluent `Expander.xaml` :58, :94–95 |
| Kind option | `KindOptionHeight` 26 sets MinHeight, but the Fluent RadioButton template's grid is `Height={DynamicResource RadioButtonMinHeight}` = **32** | not captured | Fluent `RadioButton.xaml` :14, :35 — the build's 26 never applies |
| Insets | selection and Wing `Margin=Space3` **12**; row `PropRowPadding` **8** inside a 3 px rail; **8** between groups | — | |
| Columns (260 px dock) | label **88** · value **1 fr ≈ 105**, right-aligned · unit **32** | label→value gap ≈ 125 | the value sits at the far edge |
| "How these are measured" | a `ToggleButton` with Fluent `ButtonPadding` 8,5,8,6 at 14 px on a filled background | ≈ 25 | |
| Value figures | proportional (no font feature) | — | digits do not align in a column |

**As built vs dense at 11 px (repair cycle 1, DR-DEN-3), measured in the mockup** (light, 260 px dock, 1280 × 800;
`docs/proof/property-grid-browser-check.json` `densityBeforeAfter`; "as built" is the CSS emulation of the numbers
above, so treat it as Inferred). The cycle-0 12 px column is kept for comparison.

| Metric | As built (emulated) | Dense 12 px (cycle 0) | **Dense 11 px (ruled)** |
|---|---|---|---|
| Label / value / unit font (px) | 12 / 14 / 12 | 12 / 12 / 12 | **11 / 11 / 11** |
| Messages, notes, descriptions, summaries, crumb (px) | 12 | 11 | **11** (nothing below 11) |
| Line height (px) | ~14–17 (Fluent default) | 16 | **14** |
| Unit baseline − value baseline (px) | −1.6 | 0 | **0** |
| Label start → value end, Mean chord (px) | 191 | 160 | **150** |
| Label text end → value text start, Mean chord (px) | 82 (capture ≈ 125) | 56 | **47** |
| Fact row pitch (px) | 28 | 20 | **18** |
| Input row pitch (px) | 32 | 24 | **24** |
| Field target / drawn (px) | 28 / 28 | 24 / 20 | **24 / 20** |
| Group header (px) | 33 | 24 | **24** |
| Kind option (px) | 32 | 24 | **24** |
| "How measured" control (px) | 30.6 (filled button) | 24 | **24** (link-style disclosure) |
| Wing block content (px) | 339 | 231 | **221** |
| Selection content — foil / control / anchor / handle / TE root / twist anchor / worst case (px) | 207 / 284 / 625 / 468 / 527 / 755 / 587 | 155 / 204 / 423 / 306 / 373 / 527 / — | **151 / 202 / 421 / 306 / 349 / 525 / 418** |
| Rows visible without scrolling — anchor / twist anchor | 12 / 12 | 16 / 16 | **16 / 15** |
| Row pitch under the SC 1.4.12 override (px) | — | — | **21** (grows from 18, nothing cut) |

**Room for the selection** at 1280 × 800 is about 439 px on the 260 px dock (dense) and 340 px as built.

**Fits:**
- At 260 px, every state except the twist anchor (525) and the overflow fixture (457).
- At 200 px, everything except the anchor, the worst case, the overflow fixture and the twist anchor.

States that do not fit scroll, as DR-UID-5 allows.

## 4. Comparables — numbers, with sources

| Product | Numbers | Source | Confidence |
|---|---|---|---|
| **macOS AppKit** control sizes (the HIG's "small") | regular: 13 pt font, 16 pt line, text field 24, popup 24, radio 16 · **small: 11 pt font, 13 pt line, text field 22, popup 20, radio 14** · mini: 9 pt, field 19, popup 16 · `NSTableView` / `NSOutlineView` default row **24** · `labelFontSize` 10 · `monospacedDigitSystemFont` available | measured on this Mac (Darwin 27) by `NSFont.systemFontSize(for:)` and `intrinsicContentSize`, script `appkit-metrics.swift` (scratchpad) | **Verified (measured)** |
| **VS Code** (compact trees and panes) | workbench text **13 px**, line-height 1.4 em · Explorer and Debug Variables rows **22 px** · pane header **22 px** with an **11 px** title · input padding 4 × 6 px | `src/vs/workbench/browser/media/style.css` (`.monaco-workbench { font-size: 13px }`), `explorerViewer.ts` `ITEM_HEIGHT = 22`, `variablesView.ts` `getHeight() → 22`, `paneview.ts` `DEFAULT_PANE_HEADER_SIZE = 22`, `paneviewlet.css` `font-size: 11px`, `inputBox.css` `padding: 4px 6px` (github.com/microsoft/vscode, main, fetched 2026-10-01) | **Verified (source)** |
| **VS Code Settings editor** (the "form" to avoid) | 13 px; each setting padded **12 / 14 / 18 px** | `settingsEditor2.css` `.setting-item-contents { padding: 12px 14px 18px }` | Verified (source) |
| **Avalonia Fluent 11.3.14** (what the build inherited) | `ControlContentThemeFontSize` 14 · `TextControlThemePadding` 10,6,6,5 · `TextControlThemeMinHeight` 32 · `ComboBoxMinHeight` 32 · `RadioButtonMinHeight` 32 (the template grid's height) · `ExpanderMinHeight` 48 · `ExpanderChevronButtonSize` 32 · `ExpanderHeaderPadding` 16,0,0,0 | `src/Avalonia.Themes.Fluent/Accents/BaseResources.xaml`, `Controls/{Expander,TextBox,ComboBox,RadioButton}.xaml` at tag `release/11.3.14` | **Verified (source)** |
| Adobe Premiere Pro Effect Controls | compact rows around 20–22 px, 11–12 px UI text, twirl-down headers no taller than a row | recall | **Flagged** — no number here is load-bearing |
| Fusion 360 / Rhino 8 Properties | Rhino for Mac uses native AppKit panels (small/regular controls); Fusion's property panels use about 12 px text with rows around 24 | recall | **Flagged** |

**What this sets.**
- The dense grid sits between AppKit *small* (11 pt text, 20–22 px fields) and VS Code's compact trees (13 px text,
  22 px rows). Cycle 0 proposed **12/16 px text, 20 px read-only rows, 24 px targets**; the operator ruled **11/14 and
  18 px read-only rows** (AppKit *small* is 11 pt on a 13 pt line), with 24 px targets unchanged.
- 24 is AppKit's regular field and table-row height and the WCAG 2.5.8 floor.
- 18 px read-only rows are below AppKit small's 20 px popup and VS Code's 22. They are allowed because they are not
  targets (§6).

## 5. Findings (structure before surface)

| # | Location | Dimension | Sev | Evidence | Fix | Confidence |
|---|---|---|---|---|---|---|
| D-1 | Value text | Hierarchy / type | 3 | values 14 px (inherited) beside 12 px labels and units — the value shouts, the unit whispers | one `PropFontSize` 11 for label, value, unit and input text (DR-DEN-3) | Verified |
| D-2 | Units | Craft | 2 | unit and value baselines differ (≈ 2 px capture, 1.6 px emulated) | same size and line height; baseline alignment | Verified (cause) |
| D-3 | Row pitch | Density | 3 | 28 px facts and 32 px inputs: about 4 rows fewer than VS Code's 22 px on the same height | 18 px facts, 24 px input rows | Verified |
| D-4 | Group headers | Structure | 3 | 33 px headers: the 32 px Fluent chevron button, plus an 8 px gap between groups | 24 px header via the Expander resources, a 10 px chevron at the left, a rule between groups | Verified |
| D-5 | Kind options | Density | 2 | 32 px each (the template grid); the build's 26 px token is dead | `RadioButtonMinHeight` 24 as a local resource | Verified |
| D-6 | Columns | Structure / scanning | 3 | the value is right-aligned at the far edge, so the eye travels about 125 px from label to value | fixed value column next to the label, free space last | Verified |
| D-7 | "How these are measured" | Hierarchy | 2 | a filled 14 px button competes with the data | a link-style disclosure, 11 px, 24 px target | Verified |
| D-8 | Insets | Density | 2 | 12 px insets plus 8 px row padding inside the rail | 8 px inset, 4 px inside the rail | Verified |
| D-9 | Figures | Numeric legibility (TQ2) | 2 | proportional digits | `FontFeatures` tnum and lnum on values and inputs (`TextElement.FontFeatures` and `TemplatedControl.FontFeatures` exist in 11.3.14) | Verified (API exists); rendering Inferred |
| D-10 | Token | Dead token | 1 | `KindOptionHeight` 26 never takes effect | replaced by the local `RadioButtonMinHeight` | Verified |

Accessibility: no new accessibility finding. Every floor is carried in §6, and the UX & Accessibility lens re-reviews
the trade-offs (**pending**; the author does not clear them).

## 6. Where density meets a floor, and how it is resolved (repair cycle 1, at 11 px)

| Floor | Trade | Resolution | Evidence |
|---|---|---|---|
| **SC 2.5.8 target size**: inputs and choices | the drawn box is 20 px | **one element** (DN-2): a 24 px TextBox or ComboBox whose outer `PART_BorderElement` is a transparent 2 px band, with an inner border drawing the 20 px box. The target is 24, so the rule is met by size; the spacing exception is not used | oracle "field target 24, drawn 20" in every state; a planted 20 px target turns it red (cycle 0) |
| SC 2.5.8: headers, Kind options, definitions link, crumb link | none | all **24**; the oracle's target check now also covers `summary` (the definitions link) and the crumb button | oracle "targets ≥ 24 × 24" |
| SC 2.5.8: read-only rows (18 px) | a row context menu would be an 18 px pointer target | **no row context menus** (DN-3). Per-row "Copy <label>" and "Copy <label> with unit" items move to the group header's menu (24 px target) beside "Copy values". Keyboard copy stays | brief §8.2 |
| DESIGN.md §4 "dense controls ≥ 32" | 24 px grid targets | **ruled DR-DEN-1:** the grid is the one exception | DESIGN.md §4 |
| **SC 1.4.4 resize text** | Avalonia on macOS has no system text scaling | **the app's own Text size setting (DN-5):** 100 / 125 / 150 / 200 %, ⌘+ / ⌘−, persisted; one multiplier on every Prop token; ≥ 150 % stacks the rows | oracle at 200 %: 31 states × 3 themes, nothing clipped, targets ≥ 24; the setting steps, stacks and persists across a reload |
| DR-UID-5 "Wing fully visible" at 200 % | the Wing's content (687 px) exceeds its cap | **ruled DR-DEN-2:** it stays pinned and scrolls inside itself; a focused field and its message line are brought into view (DN-6) | oracle "at 200 % a focused Wing field and its message are in view" |
| **SC 1.4.12 text spacing** at 11 px | with line height 1.5 × 11 = 16.5 px, paragraph 2 × 11 = 22 px, letter 0.12 × 11 = 1.32 px and word 0.16 × 11 = 1.76 px, an 18 px row needs 16.5 + 4 = 20.5 px | rows use minimum heights, so they grow to **≈ 21 px** (measured) and labels wrap. Single-line inputs keep their text and scroll inside themselves (the content stays reachable). On native, Avalonia exposes no user text-spacing override, so under WCAG2ICT the criterion applies to the mockup only | oracle 1.4.12 sweep (31 states): nothing clipped except input text, which scrolls; rows recorded at 21 px |
| Legibility of 11 px (not a WCAG criterion) | the reviewers recommended 12: Windows' 100 % default UI text is about 12 px, and 11 px is weaker on non-Retina displays | **ruled DR-DEN-3: 11 px, nothing below 11**; the residual risk is recorded; B-2 adds a Windows-class 100 % display capture | §11 |
| SC 1.4.3 / 1.4.11 contrast | 11 px text | token pairs unchanged: ink-mute on surface 6.15:1 light, 7.82:1 dark. 11 px is not "large text", so 4.5:1 applies, and it is met | page audit contrast rows |
| SC 2.4.7 / 2.4.11 focus | a 20 px drawn box | a 2 px ring on the 24 px band, separate from the 1 px drawn boundary (DN-1); never clipped | oracle "drawn field boundary is 1 px in every state" and the DN-1 path |
| Cleared behaviour (Kind list, Wing pinned, Unavailable reasons, B1–B10, DR-UID-1..5) | none | unchanged; all 39 earlier paths still pass | oracle |

## 7. Ranked plan

1. **Do this first: the 11 px grid type token and the Text size multiplier.** `PropFontSize` 11, `PropLineHeight` 14,
   tabular figures, one baseline. It is the most visible change, and the multiplier is the 1.4.4 path.
2. **The one-element field** (24 px band, 20 px drawn, 1 px boundary, 2 px ring) and the row pitch (18 / 24).
3. **A focused or dirty field spans the free column** (DC-1), and digits align (DC-3).
4. **Expander header 24, Kind 24** (native RadioButton template, local resource, one dot size).
5. **Value column next to the label**; the Corner subheads; the definitions link; the compact identity.
6. **Header-menu copy items replace the row context menus** (DN-3).
7. **Density and Text size tests**, red first.

## 8. Build brief (owner: the density build track; no `src/` or `tests/` edits were made here)

### 8.1 Tokens (`Styles.axaml`)

| Key | Value | DESIGN.md token | Was (build) |
|---|---|---|---|
| `PropFontSize` (new) | **11** | `typography.prop` | values 14 (inherited), labels 12 |
| `PropLineHeight` (new) | **14** | `typography.prop` | — |
| `PropNoteSize` / `PropNoteLineHeight` (new) | **11 / 14** (messages, notes, descriptions, summaries, crumb) | `typography.prop-note` | 12 |
| `PropTitleSize` / `PropTitleLineHeight` (new) | 13 / 18, SemiBold | `typography.prop-title` | 14 |
| `PropTextScale` (new, from the user setting) | 1 · 1.25 · 1.5 · 2 | `typography.prop-text-scale` | — |
| `PropLabelWidth` / narrow | **78 / 64** | `spacing.prop-label(-narrow)` | 88 / 56 |
| `PropValueWidth` / narrow (new) | **72 / 64** | `spacing.prop-value(-narrow)` | value was `*` |
| `PropUnitWidth` / narrow | **28 / 24** | `spacing.prop-unit(-narrow)` | 32 / 28 |
| `PropRowReadOnlyHeight` | **18** | `spacing.prop-row-ro` | 24 + 4 gap |
| `PropRowInputHeight` (new) | 24 | `spacing.prop-row-input` | 28 + 4 gap |
| `PropFieldDrawnHeight` (new) | 20 | `spacing.prop-field-drawn` | — |
| `PropHeadHeight` | 24 (now enforced) | `spacing.prop-head` | 33 rendered |
| local `RadioButtonMinHeight` (replaces `KindOptionHeight`) | 24 | `spacing.prop-kind` | 32 rendered |
| `PropInset` (new Thickness) | 8 | `spacing.prop-inset` | 12 |
| `PropRowPadding` | 4,2,0,2 (facts), 4,0,0,0 (inputs) | `spacing.prop-row-pad` | 8,0,0,0 |
| `PropFactDigitInset` (new) | 6 (a fact's right padding = band 2 + TextBox padding 4) | — (DC-3) | — |
| row and group spacing | 0, with a 1 px `LineBrush` rule between groups | — | 4 / 8 |

Every Prop size token is computed as base × `PropTextScale`; heights are minimums.

### 8.2 Changes per control

| Control | Change |
|---|---|
| `TextBlock.prop-label`, `.prop-value`, `.prop-unit` | `FontSize={PropFontSize}`, `LineHeight={PropLineHeight}`, `VerticalAlignment=Top` for all three, so there is one baseline (this replaces the separate `Center` that caused D-2). `prop-value`: `TextElement.FontFeatures="+tnum,+lnum"` and `Padding=0,0,{PropFactDigitInset},0` (DC-3). **assume:** that `FontFeatureCollection` string parses; `PropertiesPane_Density_ValuesUseTabularFigures` confirms it, otherwise set the collection in code |
| `prop-note`, `prop-message`, `prop-subhead`, summary, chip, `IdentityCrumb` | `{PropNoteSize}` / `{PropNoteLineHeight}` (11/14). **Nothing below 11** |
| Fact / estimate row (`Border.prop-row`) | `MinHeight={PropRowReadOnlyHeight}` (18), `Padding=4,2,0,2`. **No `ContextMenu`** (DN-3) |
| Group header menu | "Copy values", then one "Copy <label>" and one "Copy <label> with unit" per row of the group (DN-3) |
| Row `Grid` (`SetColumns`) | **four** columns: `PropLabelWidth` · `PropValueWidth` · `PropUnitWidth` · `*` (narrow: 64 · 64 · 24 · `*`). Description and message span 4. A choice, Kind list or text-valued fact spans 1–3. **DC-1:** while the TextBox has keyboard focus or its text differs from the committed value, it spans columns 1–3 and the unit TextBlock collapses (`IsVisible=False`). Under 230 px the editing row stacks: label in row 0, field in row 1 across all columns. **Large text:** class `prop-stacked` when `PropTextScale ≥ 1.5` |
| `TextBox.prop-input` (**one element**, DN-2) | `Height={PropRowInputHeight}` (24) × scale, `MinHeight=0`, `Padding=4,0` inside the drawn box, `FontSize={PropFontSize}`, `LineHeight={PropLineHeight}`, `FontFeatures="+tnum,+lnum"`, `VerticalContentAlignment=Center`. Template parts: `Border#PART_BorderElement` becomes the **transparent 24 px band** (`Background=Transparent`, `BorderThickness=0`); an added inner `Border` with `Margin=0,2` draws the **20 px box** with a **1 px** `ControlLineBrush` boundary in every state. Error: a 1 px `DangerBrush` boundary plus the row's rail, icon and text (DN-1). Focus: one 2 px `FocusRingBrush` ring on the band, inset. The IBeam cursor, caret, selection and context menu stay native because it is still one TextBox. The unit TextBlock beside it: `VerticalAlignment=Center`, the same size and line height |
| `ComboBox.prop-choice` | the same band and inner box structure (24 / 20, 1 px boundary, 2 px ring); `FontSize={PropFontSize}`; local `ComboBoxMinHeight=24` |
| `Expander.prop-group` | local resources `ExpanderMinHeight=24`, `ExpanderChevronButtonSize=16`, `ExpanderHeaderPadding=8,0,0,0`, `ExpanderChevronMargin=0` (all read as `DynamicResource` by the Fluent template, Verified). The header template has a 10 px chevron at the left, the title in `{PropFontSize}` SemiBold and the summary in `{PropNoteSize}` with tnum. `Border#ExpandCollapseChevronBorder IsVisible=False`. `ContentTransition=null` |
| Kind `RadioButton.prop-kind` (**keep the Fluent template**, DN-7) | `TangentGroup.Resources`: `RadioButtonMinHeight=24`. Style the parts: `Ellipse#OuterEllipse` and `#CheckOuterEllipse` 12 × 12, `Ellipse#CheckGlyph` **6 × 6** (one dot size); `FontSize={PropFontSize}`; spacing 0. Remove the dead `KindOptionHeight` |
| Corner handle subheads (`prop-subhead`) | 11 px SemiBold `InkBrush` ("Handle toward the root" / "toward the tip") (DC-4) |
| `HowMeasuredButton` | class `prop-link`: transparent, no border, `Padding=0`, `MinHeight=24`, `{PropNoteSize}`, `PrimaryBrush`, underline; content "Estimates · definitions"; keeps its Toggle pattern |
| Identity | title `{PropTitleSize}`/`{PropTitleLineHeight}` SemiBold; glyph 12; crumb `{PropNoteSize}` (the crumb `HyperlinkButton` keeps `MinHeight=24`) |
| Scrolling (DN-6) | on `GotFocus` of any field, and again after a commit re-renders, `BringIntoView` the whole row including its message line, inside `SelectionScroll` or `WingScroll` |
| Margins | `{PropInset}` (8) on the selection and Wing panels |

### 8.3 Text size setting (DN-5; owner: the density build track)

- **View ▸ Text size** submenu with radio items 100 % · 125 % · 150 % · 200 %, plus "Bigger" ⌘+ (Ctrl++ on Windows)
  and "Smaller" ⌘− (Ctrl+− on Windows). It steps along the ladder and never goes below 100 % or above 200 %.
- **Persisted per user** in the existing user-preferences store; it applies on launch.
- **One multiplier.** `PropTextScale` multiplies every Prop type token (font size, line height) and every Prop row
  token (row, field, header and Kind heights; column widths). At ≥ 1.5 the pane applies `prop-stacked`. The new value is
  announced in the polite status: "Text size 150 %.".
- **DR-DEN-4 (shortcut scope, pending the operator).** ⌘= / ⌘− (Ctrl on Windows) already zoom the Plan and 3D views.
  Recommendation: in a model view they keep zooming; with focus in a pane (Properties, Browser) or a dialog they step
  the Text size; the menu items always work.

### 8.4 Named tests (headless, red first)

Density (**19**):
`PropertiesPane_Density_OneFontSizeForLabelValueUnit` (11) ·
`PropertiesPane_Density_NothingBelow11` ·
`PropertiesPane_Density_UnitOnValueBaseline` ·
`PropertiesPane_Density_FactRowPitch18_InputRowPitch24` ·
`PropertiesPane_Density_FieldHitArea24_Drawn20` (a press inside the band focuses; one element) ·
`PropertiesPane_Density_OneFocusRing` ·
`PropertiesPane_Density_FocusRingVisibleInThreeThemes` ·
`PropertiesPane_Density_FocusedAndErrorFieldTextNotClipped` (TextBox and ComboBox, at 11 px) ·
`PropertiesPane_Density_DrawnBoundaryOnePxInEveryState` ·
`PropertiesPane_Density_FocusedInputShowsWholeExpression` (`#root_chord × 0.35` and `(#span − 2 cm) / 2` at 200, 260 and 300 px) ·
`PropertiesPane_Density_WorstCaseNumbersNotClippedAt200` (Aft −1234.56, Span 12000.00, Area ≈ 38125, Angle −89.99) ·
`PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` ·
`PropertiesPane_Density_GroupHeader24_NoChevronButton` ·
`PropertiesPane_Density_KindOption24_OneDotSize` ·
`PropertiesPane_Density_ValueColumnNextToLabel` (value right edge − label left ≤ 150) ·
`PropertiesPane_Density_DefinitionsIsLinkDisclosure` ·
`PropertiesPane_Density_ValuesUseTabularFigures` ·
`PropertiesPane_Density_HeaderMenuCopiesEachRow_NoRowContextMenus` ·
`PropertiesPane_Density_EveryTargetAtLeast24` (including the crumb `HyperlinkButton` and the "How measured" link).

Text size (**6**):
`TextSize_Setting_ScalesEveryPropToken` ·
`TextSize_CommandPlusMinus_StepsLadder_Clamps100To200` ·
`TextSize_PersistsPerUser` ·
`TextSize_ModelViewKeepsZoomShortcut` (DR-DEN-4) ·
`PropertiesPane_Density_StackedRowsAtLargeText` (driven through the setting, at 150 % and 200 %) ·
`PropertiesPane_Density_FocusedWingFieldInViewAtLargeText` (scale 2 via the setting; the field and its message after a
commit).

**B-5 regression set (DN-7), named.** The 30 tests in `docs/reviews/ui-property-grid.md` §10.7 (25 named plus the 5 N1
tests, B1). In particular these must stay green with the restyled RadioButton template:
`Tangent_KindChange_KeepsFocusOnChecked`, `PropertiesPane_KindArrows_MoveCheckOnly_OneUndoRowPerIntent`,
`PropertiesPane_Tangent_CheckedKindExposed`, `TypeCombo_ArrowWhileClosed_DoesNotCommit`,
`TypeCombo_PendingThenLeave_DoesNotCommit` and `Expander_FocusedHeader_ExposesNameAndExpandedState`.

**Total: 25 new tests (19 density + 6 Text size), plus the 30-test B-5 set.**

### 8.5 Acceptance (native; the author clears none)

- **B-1** The 25 tests land red first; the B-5 set stays green.
- **B-2** Native captures at 1280 × 800, 260 px dock, light / dark / high contrast, of the foil, anchor and handle
  states, measured against §3's dense column (±1 px). They include **a Windows-class 100 % (non-Retina) display**:
  11 px legibility, DR-DEN-3 residual risk.
- **B-3** The UX & Accessibility lens re-checks on the build: SC 2.5.8 (the band), 2.4.7 (one ring on the band), the
  1 px boundary in every state, and the 11 px notes' contrast.
- **B-4** Text size: each step, persistence after a restart, the stacked rows at 150 % and 200 %, and the shortcut
  scope (DR-DEN-4).
- **B-5** The named regression set (§8.4) stays green.
- **B-6** No row context menus; the header-menu copy items work by pointer and keyboard.

## 9. Rulings (operator, 2026-10-01; `docs/notes/property-grid-rulings.md`)

| ID | Ruling |
|---|---|
| DR-DEN-1 | **Accepted:** 24 px targets for the property grid, the one exception to DESIGN.md §4's 32 px rule |
| DR-DEN-2 | At 200 % text the Wing stays pinned and scrolls inside itself; a focused field is always brought into view |
| DR-DEN-3 | **11 px, nothing below 11**, for labels, values, units, messages, notes, descriptions, summaries and the crumb. The reviewers' 12 px reasons are recorded as residual risk, and B-2 adds a Windows-class 100 % capture. DN-4 (messages at 12) is superseded: messages are 11, the same as values |
| DN-5 | Build an app **Text size** setting (§8.3); in the mockup it is the real control in the title bar |
| **DR-DEN-4 (open)** | Shortcut scope for ⌘= / ⌘−, which already zoom model views. Recommendation: zoom in a model view, Text size elsewhere, menu items always (§8.3) |

## 10. Deterministic control and browser check (a floor, not a verdict)

- **Craft gate:** 6 Minor, 0 Major, 0 Blocker, 0 off-token values.
  - 5 × `cramped-padding`, all on window chrome: two frames, two tab strips, and the title bar that now holds the Text
    size control. This is the standing deviation.
  - 1 × `em-dash-overuse`, which comes from the spec's fixed strings and is recorded as a deviation.
- **Browser check: 682 cells, 0 failing checks, 51/51 interaction paths, 0 page errors.** The cells are:
  - 540 dense combinations at 100 % text (31 states, with density pins at 11 px);
  - 93 at 200 % text (31 states × 3 themes);
  - 31 under the 1.4.12 text-spacing override;
  - the before/after table.

  The 12 new paths cover:
  - DC-1 (two expressions × three docks);
  - DC-2;
  - DN-1;
  - Text size stepping, stacking and persistence across a reload;
  - DN-6 at 200 %;
  - the step-down clamp.
- **Planted regressions** (both exit 1):
  - **DC-1**, with the focused or dirty span rule removed: all six DC-1 paths fail.
  - **DN-1**, with a 2 px error boundary: "drawn field boundary is 1 px in every state" and the DN-1 path fail.

## 11. Residual risk

- **11 px legibility (DR-DEN-3).** The reviewers' reasons for 12 px stand as risk: Windows' 100 % UI default is about
  12 px, and 11 px is weaker on non-Retina displays. Mitigations: the Text size setting (125 % gives 13.75 px), and the
  Windows-class capture in B-2.
- The "as built" column of §3 is a CSS emulation of source numbers, not the native render (B-2 is the proof). The
  capture's ≈ 125 px label gap does not match the emulation's 82 px; I did not open the capture (Flagged).
- The one-element TextBox restyle (DN-2) depends on Avalonia hit-testing a transparent `PART_BorderElement` band
  (Inferred until `FieldHitArea24_Drawn20` runs).
- **DR-DEN-4** is open: a wrong scope would steal the model views' zoom.
- Under the 1.4.12 override, single-line inputs scroll their text rather than wrap. The content stays reachable; on
  native the criterion does not apply (no user override).
- Comparables for Premiere, Fusion and Rhino are recall (Flagged).

## 12. Repair cycle 1 ledger (both lenses cleared with conditions at `4bc94e6`)

| Item | Disposition |
|---|---|
| DR-DEN-1 / 2 / 3, DN-5 | **Recorded** in the rulings note, §9, DESIGN.md |
| 11 px recompute | **Done** — tokens, the before/after table (§3), row pitches (18 / 24), the 1.4.12 arithmetic (§6) |
| Text size setting (DN-5) | **Done** — brief §8.3, the real control in the mockup, 6 tests named; shortcut conflict raised as DR-DEN-4 |
| DC-1 (Major) | **Done** — the focused or dirty field spans the free column (and the row stacks at the narrow dock); 6 oracle paths; planted failure red; test named |
| DC-2 | **Done** — worst-case state; no clip at 200 px with the narrow value column at 64 px (measured); test named |
| DC-3 | **Done** — 6 px digit inset on facts; oracle check "decimals align"; test named |
| DC-4 | **Done** — semibold ink subhead for the side |
| DC-6 | **Satisfied** by DR-DEN-3 |
| DN-1 (Major) | **Done** — 1 px boundary in every state, a 2 px ring on the band, error = 1 px danger + rail/icon/text; oracle check plus path; planted failure red; test named |
| DN-2 (Major) | **Done (brief)** — one-element TextBox and ComboBox restyle; tests for the band press, one ring, and the ring in three themes |
| DN-3 | **Done (brief)** — row context menus dropped; per-row copy items in the header menu; test named |
| DN-4 | **Superseded** by DR-DEN-3 (messages are 11, the same as values) |
| DN-6 | **Done** — the focused field and its message are brought into view; oracle path at 200 %; test named |
| DN-7 | **Done (brief)** — keep the Fluent RadioButton template; one dot size (12 / 6); B-5 regression set named |
| `EveryTargetAtLeast24` covers the crumb and the "How measured" link | **Done** — the oracle target check includes `summary`; the test text names both |

## Status

| | |
|---|---|
| **Completed** | Density pass plus repair cycle 1: 11 px tokens, the Text size setting (spec + mockup control), DC-1..4, DN-1..7, oracle 682 cells / 51 paths with two planted failures red, a build brief with 25 named tests plus the B-5 set |
| **Remaining** | DR-DEN-4 (operator); the build track; native captures including Windows-class 100 % (B-2) |
| **Best next action** | Rule DR-DEN-4, then dispatch the density build track with §8 |
