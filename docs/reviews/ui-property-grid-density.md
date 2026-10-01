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
  options from Avalonia Fluent defaults, 12 px insets, a value column far from its label. The proposal is one
  12/16 type size, 20/24 px rows with a 24 px target around a 20 px field, 24 px headers, and the value next to its
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
> **Highest-leverage change:** one grid type token, `PropFontSize` 12 / `PropLineHeight` 16, applied to label, value,
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
  - *Type:* one 12/16 size for every grid row, 13/18 for the identity, 11/14 for notes and messages.
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

**As built vs dense, measured in the mockup** (light, 260 px dock, 1280 × 800; `docs/proof/property-grid-browser-check.json`
`densityBeforeAfter`; "as built" is the CSS emulation of the numbers above, so treat it as Inferred):

| Metric | As built (emulated) | **Dense** |
|---|---|---|
| Label / value / unit font (px) | 12 / 14 / 12 | **12 / 12 / 12** |
| Unit baseline − value baseline (px) | −1.6 | **0** |
| Label text end → value text start, Mean chord (px) | 82 (capture ≈ 125) | **56** |
| Fact row pitch (px) | 28 | **20** |
| Input row pitch (px) | 32 | **24** |
| Field target / drawn (px) | 28 / 28 | **24 / 20** |
| Group header (px) | 33 | **24** |
| Kind option (px) | 32 | **24** |
| "How measured" control (px) | 30.6 (filled button) | **24** (link-style disclosure) |
| Wing block content (px) | 339 | **231** |
| Selection content — foil / control / anchor / handle / TE root / twist anchor (px) | 207 / 284 / 625 / 468 / 527 / 755 | **155 / 204 / 423 / 306 / 373 / 527** |
| Rows visible without scrolling — anchor / twist anchor | 12 / 12 | **16 / 16** |

The room for the selection at 1280 × 800 on the 260 px dock is about 448 px (dense) and 340 px (as built). In the
dense layout the anchor, handle and TE root states now fit; as built they scrolled. The twist anchor (527) and the
overflow fixture still scroll, as DR-UID-5 allows.

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
  22 px rows): **12/16 px text, 20 px read-only rows, 24 px targets**.
- 24 is AppKit's regular field and table-row height and the WCAG 2.5.8 floor.
- 20 px read-only rows match AppKit small's popup and are below VS Code's 22. They are allowed because they are not
  targets (§6).

## 5. Findings (structure before surface)

| # | Location | Dimension | Sev | Evidence | Fix | Confidence |
|---|---|---|---|---|---|---|
| D-1 | Value text | Hierarchy / type | 3 | values 14 px (inherited) beside 12 px labels and units — the value shouts, the unit whispers | one `PropFontSize` 12 for label, value, unit and input text | Verified |
| D-2 | Units | Craft | 2 | unit and value baselines differ (≈ 2 px capture, 1.6 px emulated) | same size and line height; baseline alignment | Verified (cause) |
| D-3 | Row pitch | Density | 3 | 28 px facts and 32 px inputs: about 4 rows fewer than VS Code's 22 px on the same height | 20 px facts, 24 px input rows | Verified |
| D-4 | Group headers | Structure | 3 | 33 px headers: the 32 px Fluent chevron button, plus an 8 px gap between groups | 24 px header via the Expander resources, a 10 px chevron at the left, a rule between groups | Verified |
| D-5 | Kind options | Density | 2 | 32 px each (the template grid); the build's 26 px token is dead | `RadioButtonMinHeight` 24 as a local resource | Verified |
| D-6 | Columns | Structure / scanning | 3 | the value is right-aligned at the far edge, so the eye travels about 125 px from label to value | fixed value column next to the label, free space last | Verified |
| D-7 | "How these are measured" | Hierarchy | 2 | a filled 14 px button competes with the data | a link-style disclosure, 11 px, 24 px target | Verified |
| D-8 | Insets | Density | 2 | 12 px insets plus 8 px row padding inside the rail | 8 px inset, 4 px inside the rail | Verified |
| D-9 | Figures | Numeric legibility (TQ2) | 2 | proportional digits | `FontFeatures` tnum and lnum on values and inputs (`TextElement.FontFeatures` and `TemplatedControl.FontFeatures` exist in 11.3.14) | Verified (API exists); rendering Inferred |
| D-10 | Token | Dead token | 1 | `KindOptionHeight` 26 never takes effect | replaced by the local `RadioButtonMinHeight` | Verified |

Accessibility: no new accessibility finding. Every floor is carried in §6, and the UX & Accessibility lens re-reviews
the trade-offs (**pending**; the author does not clear them).

## 6. Where density meets a floor, and how it is resolved

| Floor | Trade | Resolution | Evidence |
|---|---|---|---|
| **SC 2.5.8 target size** — inputs | a 20 px drawn field is under 24 | the **target is 24** and only the *drawn* box is 20: a 2 px transparent band above and below is part of the control. The rule is met by size; the spacing exception is not needed | oracle "field target 24, drawn 20" in every state; a planted 20 px target turns the oracle red |
| SC 2.5.8 — headers, Kind options, definitions link, crumb link | none | all **24** | oracle "targets ≥ 24 × 24" (630 cells) |
| SC 2.5.8 — read-only rows (20 px) | a row's context-menu Copy is a pointer verb on a 20 px row | the **equivalent** exception: the group header's "Copy values" (24 px target) and keyboard copy (PG-25/26 in the build) do the same job. Rows are otherwise not targets | brief B-6 |
| DESIGN.md "dense controls ≥ {spacing.target-dense} 32" (project rule, stricter than WCAG) | 24 px grid targets break the project rule (the build already broke it at 28) | DESIGN.md §4 now records the property grid as the one exception at 24 (SC 2.5.8 met by size) | **operator decision DR-DEN-1** |
| **SC 1.4.4 resize text** | fixed columns would clip at 2× | at **≥ 150 % text** the row reflows: label on its own line, value and unit beneath. Row heights are minimums, so rows grow | oracle at 200 % text: 30 states × 3 themes, nothing clipped or ellipsized, targets ≥ 24 |
| SC 1.4.4 — native | Avalonia on macOS has no system text scaling (Inferred) | needs an app text-size setting (it does not exist; it was not in scope before this pass either). The dense rows are *ready* for it via the stacked class | **Flagged**; brief B-11 |
| DR-UID-5 "Wing always fully visible" vs 200 % text | at 2× the Wing's content (687 px) exceeds its 55 % cap (373 px) | at large text the Wing stays pinned and scrolls inside itself, the same as the four 200 px-dock states already recorded | recorded in the oracle (`textAt200Recorded`); **operator decision DR-DEN-2** |
| SC 1.4.12 text spacing | — | rows use minimum heights and wrap, never fixed heights; no truncation | oracle "no label or option is ellipsized" |
| SC 1.4.3 / 1.4.11 contrast | 12 px labels and 11 px notes | token pairs unchanged: ink-mute on surface 6.15:1 light, 7.82:1 dark; the input boundary is unchanged (3.43:1 light, the known thin margin) | page audit contrast rows (all themes) |
| SC 2.4.7 / 2.4.11 focus | a 20 px drawn field | an inset 2 px ring at the 24 px target edge, outside the drawn box, never clipped | mockup CSS; native B-4 |
| Cleared behaviour (Kind list, Wing pinned, Unavailable reasons, B1–B10, DR-UID-1..5) | none | unchanged; the 39 interaction paths still pass | oracle |

## 7. Ranked plan

1. **Do this first: one grid type token.** `PropFontSize` 12 and `PropLineHeight` 16 on `prop-label`, `prop-value`,
   `prop-unit` and `TextBox.prop-input`, plus tabular figures. It fixes D-1, D-2 and D-9 in `Styles.axaml` alone,
   and it is the most visible change.
2. **Row pitch, insets and the field hit band** (D-3, D-8; §8.2 rows 3–5).
3. **Expander header to 24, chevron to the left, a rule between groups** (D-4).
4. **Kind option 24** (D-5, D-10).
5. **Fixed value column next to the label** (D-6).
6. **Definitions as a link-style disclosure; compact identity** (D-7).
7. **Density tests** (§8.3), red first.

## 8. Build brief

No `src/` or `tests/` edits were made here; a build track follows. Every value below is a DESIGN.md token, and
`Styles.axaml` mirrors it under the key named.

### 8.1 Tokens (`Styles.axaml`)

| Key (new or changed) | Value | DESIGN.md token | Was |
|---|---|---|---|
| `PropFontSize` (new) | 12 | `typography.prop` | — (values inherited 14) |
| `PropLineHeight` (new) | 16 | `typography.prop` | — |
| `PropTitleSize` / `PropTitleLineHeight` (new) | 13 / 18, SemiBold | `typography.prop-title` | 14 (inherited) |
| `PropNoteSize` / `PropNoteLineHeight` (new) | 11 / 14 | `typography.prop-note` | 12 (`CaptionSize`) |
| `PropLabelWidth` | **84** | `spacing.prop-label` | 88 |
| `PropLabelNarrowWidth` | **68** | `spacing.prop-label-narrow` | 56 |
| `PropValueWidth` (new) | 76 | `spacing.prop-value` | value was `*` |
| `PropValueNarrowWidth` (new) | 64 | `spacing.prop-value-narrow` | — |
| `PropUnitWidth` | **28** | `spacing.prop-unit` | 32 |
| `PropUnitNarrowWidth` | **24** | `spacing.prop-unit-narrow` | 28 |
| `PropRowReadOnlyHeight` | **20** | `spacing.prop-row-ro` | 24 (+4 gap) |
| `PropRowInputHeight` (new) | 24 | `spacing.prop-row-input` | 28 (+4 gap) |
| `PropFieldDrawnHeight` (new) | 20 | `spacing.prop-field-drawn` | — |
| `PropHeadHeight` | 24 (now enforced) | `spacing.prop-head` | 24 set, 33 rendered |
| `KindOptionHeight` → replaced by a local `RadioButtonMinHeight` | 24 | `spacing.prop-kind` | 26 set, 32 rendered |
| `PropInset` (new Thickness) | 8 | `spacing.prop-inset` | `Space3` 12 |
| `PropRowPadding` | **4,0,0,0** | `spacing.prop-row-pad` | 8,0,0,0 |
| row stack spacing (`StackGap1` in the Wing and group bodies) | **0** (rows carry their own height) | — | 4 |
| `BlocksPanel` spacing | **0**, with a 1 px `LineBrush` rule between groups | — | `StackGap2` 8 |

### 8.2 Changes per control

| Control | Change |
|---|---|
| `TextBlock.prop-label`, `.prop-value`, `.prop-unit` | `FontSize={PropFontSize}`, `LineHeight={PropLineHeight}`, `VerticalAlignment=Top` for all three (same size and line height means one baseline; this replaces the separate `Center` alignments that caused D-2). `prop-value`: `TextElement.FontFeatures="+tnum,+lnum"`. **assume:** the separator `FontFeatureCollection` parses; confirm with `PropertiesPane_Density_ValuesUseTabularFigures`; if the string form fails, set the collection in code. |
| Fact / estimate row (`Border.prop-row` around the Grid) | `MinHeight={PropRowReadOnlyHeight}` (20), `Padding=4,2,0,2`; no `StackGap1` between rows |
| Row `Grid` (`SetColumns`) | **four** columns: `PropLabelWidth` · `PropValueWidth` · `PropUnitWidth` · `*` (narrow: the narrow trio). The description and message span 4. A `Choice`, `KindList` or text-valued fact spans columns 1–3. At large text, class `prop-stacked`: label in row 0 spanning all columns, value and unit in row 1 |
| `TextBox.prop-input` | **The hit band:** wrap the TextBox in `Border.prop-field` with `MinHeight={PropRowInputHeight}` (24), `Padding=0,2`, `Background=Transparent` (so the 2 px bands are hit-testable), and `PointerPressed → box.Focus()`. The TextBox: `Height={PropFieldDrawnHeight}` (20), `Padding=6,1,6,1` (replacing Fluent 10,6,6,5), `FontSize={PropFontSize}`, `LineHeight={PropLineHeight}`, `FontFeatures="+tnum,+lnum"`, `VerticalContentAlignment=Center`, `MinHeight=0`. The unit TextBlock beside it: `VerticalAlignment=Center`, same size and line height, so the baselines meet. The focus ring is drawn on the `Border.prop-field` (inset, 2 px, `FocusRingBrush`) |
| `ComboBox.prop-choice` | the same `Border.prop-field` band; ComboBox `Height=20`, `Padding=6,0`, `FontSize={PropFontSize}`; local resource `ComboBoxMinHeight=20` |
| `Expander.prop-group` | Local resources on the class style: `ExpanderMinHeight=24`, `ExpanderChevronButtonSize=16`, `ExpanderHeaderPadding=8,0,0,0`, `ExpanderChevronMargin=0` (the Fluent template reads all four as `DynamicResource`, Verified). Header content template: a 10 px chevron `Path` at the left, title `{PropFontSize}` SemiBold, summary `{PropNoteSize}` with tnum, at the right. Hide the Fluent chevron (`Border#ExpandCollapseChevronBorder IsVisible=False`). Keep `ContentTransition=null` (B6). Header `MinHeight=24` keeps SC 2.5.8 |
| Kind `RadioButton.prop-kind` (in `TangentGroup`) | `TangentGroup.Resources`: `RadioButtonMinHeight=24`; ellipses `OuterEllipse` / `CheckOuterEllipse` 14 × 14, `CheckGlyph` 6 × 6 (styles on `/template/ Ellipse#…`); `FontSize={PropFontSize}`; `StackPanel.Spacing=0`. Remove the dead `KindOptionHeight` |
| `HowMeasuredButton` (`ToggleButton`) | class `prop-link`: `Background=Transparent`, `BorderThickness=0`, `Padding=0`, `MinHeight=24`, `FontSize={PropNoteSize}`, `Foreground={PrimaryBrush}`, underline; content "Estimates · definitions"; it keeps the Toggle pattern and its name |
| Identity (`IdentityTitle`, glyph, crumb) | title `{PropTitleSize}` / `{PropTitleLineHeight}` SemiBold; glyph 12; crumb `{PropNoteSize}` |
| `prop-note`, `prop-message`, `prop-subhead`, chip | `{PropNoteSize}` / `{PropNoteLineHeight}` |
| Selection and Wing `StackPanel` margins | `{PropInset}` (8) instead of `Space3`; `WingBlock` keeps its top hairline |

### 8.3 Named tests that pin density (headless, red first; the oracle's in-page pins are the mockup twins)

| Test | Pins |
|---|---|
| `PropertiesPane_Density_OneFontSizeForLabelValueUnit` | label, value, unit and input text all 12 |
| `PropertiesPane_Density_UnitOnValueBaseline` | \|unit baseline − value baseline\| ≤ 0.5 on a fact row and on an input row |
| `PropertiesPane_Density_FactRowPitch20_InputRowPitch24` | adjacent Wing estimate rows 20 apart; Span → Root chord 24 apart |
| `PropertiesPane_Density_FieldHitArea24_Drawn20` | `Border.prop-field` 24 tall; the TextBox 20; a press at y = 1 inside the band focuses the field (SC 2.5.8) |
| `PropertiesPane_Density_GroupHeader24_NoChevronButton` | the header ToggleButton is 24 tall; no 32 × 32 chevron border is visible |
| `PropertiesPane_Density_KindOption24` | each Kind RadioButton is 24 tall |
| `PropertiesPane_Density_ValueColumnNextToLabel` | at the 260 px dock, value right edge − label left ≤ 160 |
| `PropertiesPane_Density_DefinitionsIsLinkDisclosure` | `HowMeasuredButton` has a transparent background, its target ≥ 24 and its Toggle pattern intact |
| `PropertiesPane_Density_ValuesUseTabularFigures` | value TextBlocks and inputs carry tnum |
| `PropertiesPane_Density_AnchorStateFitsAt1280x800` | at the 260 px dock the anchor selection's content ≤ the room above the Wing (dense: 423 ≤ 448) |
| `PropertiesPane_Density_StackedRowsAtLargeText` | with a text scale ≥ 1.5, rows take `prop-stacked` and no label, value or option is clipped |
| `PropertiesPane_Density_EveryTargetAtLeast24` | every focusable or pointer-actionable descendant of the pane (except read-only rows, which use the equivalent exception) is ≥ 24 × 24 |

**12 tests.** They join the 30 in `docs/reviews/ui-property-grid.md` §10.7.

### 8.4 Acceptance (native, for the build track; the author clears none)

- **B-1** The 12 tests land red first.
- **B-2** A native capture at 1280 × 800, 260 px dock, light / dark / high contrast, of the foil, anchor and handle
  states, measured against §3's dense column (±1 px).
- **B-3** The UX & Accessibility lens re-checks SC 2.5.8 (the hit band), 2.4.7 (the inset ring on a 20 px field) and
  the contrast of the 11 px notes on the build.
- **B-4** The focus ring on `Border.prop-field` is visible in all three themes.
- **B-5** The B1–B10 list of the property-grid review still holds; the 39 mockup paths are unchanged.
- **B-6** Read-only rows: the group header's "Copy values" and keyboard copy remain the equivalent of the row context
  menu.
- **B-11** If an app text-size setting is added, `prop-stacked` engages at ≥ 150 %. Until then SC 1.4.4 on native stays
  Flagged.

## 9. Decisions for the operator

| ID | Question | Recommendation |
|---|---|---|
| DR-DEN-1 | DESIGN.md asks dense scientific controls to be ≥ 32 px (stricter than WCAG). Accept 24 px targets for the property grid (SC 2.5.8 met by size)? | **Accept.** 32 px targets cannot give an inspector density; the build already sits at 28. 24 is AppKit's regular field and table-row height |
| DR-DEN-2 | At 200 % text the Wing cannot be fully visible in an 800 px window. Allow it to scroll inside its pinned block at large text? | **Allow.** It is a physical limit; the Wing stays pinned and its estimates stay in its own scroll, never behind the selection |
| DR-DEN-3 | One grid size: 12 px (between AppKit small 11 and VS Code 13) or 11 px (AppKit small)? | **12.** It keeps the existing caption size, and 11 px labels next to 13 px Plan probe text would read as a different app. Revisit after the native capture (B-2) |

## 10. Deterministic control and browser check (a floor, not a verdict)

- **Craft gate** (`ui-craft-gate.py docs/mockups/property-grid.html --markdown --a11y-obligation`): **5 Minor, 0 Major,
  0 Blocker, 0 off-token values.**
  - 4 × `cramped-padding` on the window chrome (the standing deviation).
  - 1 × `em-dash-overuse`: the spec's fixed strings use the em dash ("Unavailable — …", COPY-108, COPY-153), so this is
    a recorded deviation, as on the earlier mockups.
- **Browser check** (`node tools/check-mockup-property-grid.mjs <playwright root>`): **630 cells, 0 failing checks,
  39/39 interaction paths, 0 page errors.**
  - The cells are 540 dense combinations at 100 % text (state × theme × dock × window), whose page audit now also pins
    the density, plus 90 at 200 % text (30 states × 3 themes).
  - Planted regressions both exit 1:
    - values back at 14 px fails "one type size" and "row pitch";
    - a 20 px target with no band fails "targets ≥ 24" and "field target 24, drawn 20".

## 11. Residual risk

- The "as built" column of §3 is a CSS emulation of source numbers, not the native render. The native capture (B-2)
  is the proof. The capture's ≈ 125 px label gap does not match the emulation's 82 px. I did not open the capture, so
  the row it was measured on is not known (Flagged).
- No point, anchor or handle selection was captured natively. Their before numbers come from source and the emulation
  (Inferred).
- Comparables for Premiere, Fusion and Rhino are recall (Flagged). Those for AppKit, VS Code and Avalonia are measured
  or from source.
- Avalonia `FontFeatures` string parsing and the hit-testing of a transparent band are Inferred until the tests in
  §8.3 run.

## Status

| | |
|---|---|
| **Completed** | Density review of the built grid: source measurements, comparables, the dense token set in DESIGN.md, the mockup with an As built / Dense toggle, 200 % text and a live before/after table, the oracle pins, the build brief |
| **Remaining** | UX & Accessibility re-check of the floor trade-offs (§6); operator rulings DR-DEN-1 to DR-DEN-3; the build track and its native capture (B-2) |
| **Best next action** | Rule DR-DEN-1..3, then dispatch the build track with §8 |
