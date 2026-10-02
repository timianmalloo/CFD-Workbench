---
id: review-ui-property-grid-cells
title: UI review — the property sheet in structure B (Premiere Effect Controls), promoted to the full design
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, properties, property-grid, premiere, structure-b, accessibility, build-brief]
links:
  - {to: mockup-property-grid, rel: documents}
  - {to: mockup-property-grid-cells, rel: refines}
  - {to: review-ui-property-grid-density, rel: refines}
  - {to: property-grid-rulings, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-31
summary: >-
  The operator picked variant B, Premiere Pro Effect Controls, "as is" (DR-CELL-1). B is now the property sheet's
  structure in every state of the mockup, carrying every ruling still in force. Measured against the PGRID build:
  11 px one size, 20/24 px rows, 24 px twirl headers, the Wing from 339 to 230 px, the anchor selection from 549 to
  313 px. Editability is shown by colour plus a dotted underline (the non-colour cue); focus shows a box. This brief
  supersedes the density brief where they conflict.
review-suggested: []
---

# UI review — the property sheet in structure B

*`/ui-design`, track UID, session `f19a2b12-uid`, 2026-10-01. The record of the pick is
[`property-grid-cells.html`](../mockups/property-grid-cells.html), variant B. The full design is
[`property-grid.html`](../mockups/property-grid.html): **Structure: B · Effect Controls** (default) or **As built
(PGRID build)**; persona, pane width 200/260/300, light/dark/high contrast, reduced motion, the app's Text size
100–200 %, and a 1.4.12 text-spacing check.*

## 1. What B changes against the PGRID build (measured)

Light, 260 px dock, 1280 × 800, from `docs/proof/property-grid-browser-check.json` (`densityBeforeAfter`).
"As built" is the CSS emulation of `dade554`'s source numbers (Inferred). Its anchor state now carries the Kind as an
enum; with the build's 32 px radio list it measured 625 px.

| Metric | As built | **B** |
|---|---|---|
| Label / value / unit font (px) | 12 / 14 / 12 | **11 / 11 / 11** (nothing below 11) |
| Unit baseline − value baseline (px) | −1.6 | **0** |
| Read-only row pitch (px) | 28 | **20** |
| Editable row pitch (px) | 32 | **24** |
| Editable target (px) | a 28 px box | **the 24 px row**: no box at rest; a 20 px box on focus |
| Group header (px) | 33, shaded band, 32 px chevron button | **24**, no band, 10 px chevron |
| Identity block (px) | 58 | **45** |
| "How these are measured" (px) | 30.6, filled button | **24**, solid-underline link (DR-CELL-5) |
| Wing block (px) | 339 | **230** |
| Selection content — foil / control / anchor / handle / TE root / twist anchor / worst case (px) | 207 / 284 / 549 / 392 / 527 / 679 / 511 | **155 / 189 / 313 / 223 / 357 / 448 / 294** |
| Rows visible without scrolling — anchor / twist anchor | 12 / 12 | **16 / 17** |
| Label start → value end (px) | 191 | 201: B keeps values at the right edge, as approved |
| Editable vs read-only | an input box vs plain text | accent + dotted underline vs ink, no cue |

States rendered in B (31): no foil, opening, pane error, foil, control, anchor, handle (own identity, parent kind),
LE/TE root end (locks and reasons; TE root COPY-159), root and tip handles, tip end, three points (Mixed), station,
drag (≈ preview), checking, chord warning, field error, Unavailable and MAC-only Unavailable, tip closes, section editor,
not checked, recovery band, overflow, worst case, twist (point, anchor, handle, clamped), t/c point; and every one at
200 % text (stacked).

## 2. Visible changes to B's look

**Ruled DR-CELL-2 (2026-10-02): the operator OKs all three** — the two below and the Tangent kind as a ▾ dropdown
with Type's commit rules (amending PG-06 / MC-1). Changes a floor forces:

1. **The Wing header has no ▾.** B's sample drew "▾ Wing" like the other groups, but the Wing never collapses
   (UI-36, DR-UID-5). A chevron that does nothing would be a false affordance.
2. **Labels wrap instead of ellipsizing**, at the 200 px dock only (PG-03). At 260 px nothing truncates, so the look
   there is unchanged.

**Content B's sample did not include, drawn in B's own style** (these are not restyles):
- the collapsed curve group "Trailing edge · cubic · N points (16 max)" (MC-16);
- the "Estimates · definitions" link (UI-36's required route to the definitions);
- the row set per tangent kind: Smooth shows Angle / To root / To tip, while the sample showed the Corner set;
- state lines that appear only in their states: field errors, warnings, Unavailable reasons, banners, lock reasons,
  the clamp echo, and help under a focused row;
- COPY-159 under the TE root's Aft.

Kept exactly as in the sample: one "Point" group; Type and Tangent kind as accent text + ▾; accent, dotted-underline
editable values; ink read-only values; muted units; half-strength rules; no box until focus, then a 1 px accent box.

## 3. The two B-specific accessibility questions

### 3(a) Editability by colour and a dotted underline

- **SC 1.4.1 (use of colour).**
  - An editable value differs from a read-only one by colour (primary vs ink) **and** a dotted underline. An enum
    carries ▾ instead. The underline and the ▾ are the non-colour cues, so editability is not colour alone.
  - Is it enough? Yes, with conditions:
    - the underline must actually render (see the UI-N note below);
    - it must stay at 1 px with a 2 px offset at 11 px, which is visible at 1× and 2× in the captures;
    - the focused state must add a box, not just remove the underline.
  - The oracle gates "editable values carry a non-colour cue" and "read-only values carry no edit cue" in every state,
    theme and width.
- **SC 1.4.11 (non-text contrast) of the underline.** It is drawn in the text colour: primary #006c67 on surface 6.11:1
  (light), #88d8c6 on dark surface 8.60:1, and #ffee58 on black 17.62:1 in high contrast. All are ≥ 3:1. The page
  audit checks the accent/surface pair in all three themes.
- **Links vs values (DR-CELL-5).** Links ("Estimates · definitions", the crumb) take a solid underline, so a dotted
  underline always means an editable value.
- **A focused value in error (DR-CELL-3, CL-1)** keeps its accent focus box, with the danger box 1 px outside it, so
  focus stays visible in error. The oracle checks that focused differs from unfocused for every Tab stop.
- **High contrast.** The underline takes the accent (yellow) in the custom high-contrast variant, so it stays visible.
  On native, the brushes must be set per variant (density PG-11 still applies).
- **UI-N found in this run.** Chrome drops an `<input>`'s text underline when the input has an explicit height. The
  computed style said "underline" while nothing rendered. The mockup now gets its 24 px band from padding, and the audit
  adds "underlined values have no fixed height", a content-box proxy. A planted fixed height turns that check red.
  Natively, Avalonia's `TextBox` has no `TextDecorations` (Verified: only `TextBlock` and `Inline` expose them), so the
  brief draws the underline in the TextBox template (§5.2).

### 3(b) No box until clicked: focus, Tab and read-only

- **SC 2.4.7 (focus visible).** A focused value — by Tab or by click — shows a 20 px box with a 1 px primary boundary,
  and its text turns from accent to ink with the underline removed. That is a change of shape (a box appears) and of
  colour. Enums do the same. Group headers show the inset 2 px ring.
  - Oracle: "every Tab stop looks focused" in the anchor, handle, foil and twist-anchor states; it fails if any stop
    has neither a box nor an outline.
  - A 1 px boundary meets 2.4.7 (AA). 2.4.13's 2 px (AAA) is not claimed.
- **SC 2.1.1 (keyboard).** Every editable value and enum is a Tab stop, in DOM order with the rows. Oracle: "every
  editable value is reachable by Tab" in four states, with every group expanded.
- **SC 2.5.8 (target size).** The editable row is 24 px. Its label is the value's `<label>`, so a press anywhere on
  the row focuses the value. Oracle: "clicking a label focuses its value", plus "editable value target 24".
- **Read-only vs editable without colour.** Read-only values have no underline and no ▾, and are in ink. Locked
  values add a lock glyph and their reason. Gated as above.

## 4. Rulings carried into B (how B renders each)

| Ruling | In B |
|---|---|
| DR-DEN-3: 11 px, nothing below 11 | all grid text 11/14; identity 13/18. Gated "no text below 11" |
| DR-CELL-3: focused value in error | the accent focus box with the 1 px danger box just outside it; unfocused, the danger box only; rail, icon, message unchanged |
| DR-CELL-4 / DR-CELL-5 | the angle reference stays focus-only; links ("Estimates · definitions", the crumb) take a solid underline, dotted is for editable values only |
| DR-DEN-1: 24 px targets | the editable row is 24 and is the target; read-only rows are 20 and are not targets |
| DR-DEN-2: Wing at 200 % | pinned; scrolls inside; a focused field and its message are brought into view (oracle DN-6) |
| DN-5 Text size + DR-DEN-4 shortcut scope (ruled) | the title-bar control (100/125/150/200 %, ⌘+ / ⌘−, persisted); ≥ 150 % drops the value under its label; ⌘= / ⌘− zoom a focused model view and step the Text size elsewhere |
| DC-1: whole expression | while the text differs (dirty), the label column becomes Auto, the value column takes the rest, the unit collapses; the label stays visible; on plain focus B's 62 px box is kept (CB-1) |
| PG-06 / PG-07 / PG-19: Kind and Type | both are enums in B (spec UI-37). One rule: arrows on the closed box are pending with an info line, Return or a pointer pick commits one undo step, Esc keeps, leaving drops **and is announced** ("Tangent kind unchanged: Smooth." / "Type unchanged: Control point.", CL-2). **DR-CELL-2 amends PG-06 / MC-1:** the radio list's "leaving commits" is superseded |
| PG-25 / DN-3: keyboard copy | no row context menus; the group header's menu copies the group or one row (with or without unit); keyboard copy as built |
| PG-26: announcements | the status line (polite) carries reports, availability (COPY-160), selection changes and Text size; errors are alerts once per failed commit (PG-22) |
| PG-01: units spoken | input names carry the unit; facts speak "<label>, <value> <unit>[, locked]"; AR is "aspect ratio", t/c is "t over c" (PG-24) |
| DR-UID-1: precision per quantity | unchanged (typed 0.01 mm, derived ≈ 0.1 mm, placed angle 0.01°, placed t/c 0.01 %, Max t/c 0.1 %) |
| MC-6: From root + η | unchanged; "Span" only for b |
| MC-3: set-once echo | unchanged: "#root_chord × 0.1 = 12.67 mm (set once; doesn't follow Root chord)" under the row |
| MC-19 / MC-20 / MC-23 | Core clamps twist and t/c with a warning echo on a warning rail; t/c under 1 % warns; field runs stop at bounds |
| DC-3: decimals align | facts get the same 5 px right inset as an editable value's text |

## 5. Build brief (supersedes the density brief §8 where they conflict)

### 5.1 Tokens (`Styles.axaml`)

These keep the density pass's type tokens: `PropFontSize` 11, `PropLineHeight` 14, `PropNoteSize` 11,
`PropTitleSize` 13 / 18, `PropTextScale`.

| Key | Value | DESIGN.md | Note |
|---|---|---|---|
| `PropHeadHeight` | 24 | `spacing.prop-head` | twirl header, no band |
| `PropRowReadOnlyHeight` | **20** | `spacing.prop-row-ro` | (density: 18) |
| `PropRowInputHeight` | 24 | `spacing.prop-row-input` | |
| `PropEditBoxHeight` | 20 | `spacing.prop-edit-box` | **the focus box only**; there is no drawn field at rest (supersedes `PropFieldDrawnHeight`) |
| `PropValueMinWidth` | 62 | `spacing.prop-value` | right-aligned |
| `PropUnitWidth` | 24 | `spacing.prop-unit` | |
| `PropIndent` / `PropIndentSub` | 22 / 32 | `spacing.prop-indent(-sub)` | |
| `PropInset` | 8 | `spacing.prop-inset` | |
| **Removed** | `PropLabelWidth(-Narrow)`, `PropValueWidth`, `PropValueNarrowWidth`, `PropUnitNarrowWidth`, `PropFieldDrawnHeight`, `PropRowPadding`, the local `RadioButtonMinHeight` | — | the label is flex in B; Kind is an enum |

### 5.2 Changes per control

| Control | B |
|---|---|
| Row | a `Grid` with `ColumnDefinitions="*,Auto,{PropUnitWidth}"`: label (wraps, never trims), value, unit. Left padding `{PropIndent}` (or `{PropIndentSub}` under a subhead); `MinHeight` 20 (read-only) or 24 (editable). Bottom rule: 1 px `LineBrush` at 50 % opacity. The row's `PointerPressed` focuses its editor (the whole row is the target). The state rail (3 px, left) appears only for error, warning or unavailable |
| Editable value (one element, DN-2) | `TextBox` class `prop-b`. **At rest:** `Background=Transparent`, `PART_BorderElement` a transparent 2 px band, `Foreground={PrimaryBrush}`, `TextAlignment=Right`, `MinWidth=62`, `Padding=3,3`, `FontFeatures="+tnum,+lnum"`. **The dotted underline:** `TextBox` has no `TextDecorations` in 11.3.14, so the template adds a `Line` under `PART_TextPresenter`: `StrokeDashArray="1,1"`, `StrokeThickness=1`, `Stroke={PrimaryBrush}`, 2 px below the baseline, width bound to the presenter's text width (`TextLayout.WidthIncludingTrailingWhitespace`), right-aligned, `IsVisible` false on `:focus`. **assume:** the text-width binding updates on every edit; confirm with `PropertiesPane_B_EditableValueHasDottedUnderline` (render test). **On `:focus`:** an inner `Border` 20 px tall with a 1 px `PrimaryBrush` boundary, `Foreground={InkBrush}`, no underline. **Error (DR-CELL-3):** unfocused, a 1 px `DangerBrush` boundary plus the rail, icon and text; focused, the 1 px `PrimaryBrush` focus box with the 1 px `DangerBrush` box just outside it. **DC-1 (CB-1, what the mockup does):** while dirty, the label column becomes `Auto`, the value column becomes `*`, the unit column collapses to 0; the label stays visible. **Error (DR-CELL-3):** unfocused, a 1 px `DangerBrush` box; focused, the 1 px `PrimaryBrush` focus box with a 1 px `DangerBrush` box just outside it (in the band); rail, icon, message as before. **Underline (CL-3):** Option A only — the template `Line`, pixel-snapped (`UseLayoutRounding=True`, offset to a pixel centre: 2.5 px below the baseline at 1×, 2.25 at 2×), with a **pixel** render test at 1× and 2× in three themes (≥ 3:1 measured on pixels). **Fallback = overlay, not swap:** the `TextBox` stays the only focusable, named element; at rest a Raw, `IsHitTestVisible=False` `TextBlock` with a dotted `TextDecoration` draws the same text over a transparent `PART_TextPresenter` and hides on `:focus`. **A true swap (TextBlock at rest, TextBox on focus) is forbidden**: it moves focus and changes the element AT reads |
| Enum value (Type, Tangent kind) | `ComboBox` class `prop-b`: no background or border at rest, `Foreground={PrimaryBrush}`, ▾ in `MutedBrush`, `MinHeight=24`; on `:focus` the same 20 px boundary. Commit only on `DropDownClosed` with a changed value or on Return; arrows while closed are pending ("Press Return to make it <kind>, or Esc to keep <kind>." / COPY-166), and while pending the box's `HelpText` is "Return applies; Esc keeps <kind>" (CB-4); with the drop-down open, arrows then Esc leave no undo row and arrows then close leave one (CB-3); leaving drops the pending value and the status announces it politely: "Tangent kind unchanged: Smooth." / "Type unchanged: Control point." (CL-2) |
| Read-only fact or estimate | `TextBlock`s in `InkBrush`, no underline, no ▾; a lock glyph when locked; the container is named "<label>, <value> <unit>[, locked]" and its children are Raw (PG-01, D2); not a Tab stop |
| Group header | `Expander` with the density brief's local resources (`ExpanderMinHeight=24`, `ExpanderChevronButtonSize=16`, `ExpanderHeaderPadding=8,0,0,0`, `ExpanderChevronMargin=0`), header `Background=Transparent` (**no band**), a 10 px chevron at the left in `MutedBrush`, title SemiBold 11 |
| Point group | `PropertiesView.Build` emits one group "Point" for a point selection: Type, From root, η, Aft (or the channel value), Tangent kind, then the handle rows under subheads (`prop-subhead`: 11 px SemiBold, `MutedBrush`, indent 22) |
| Help | the row's description TextBlock is visible while the row has keyboard focus, or always when `DescriptionAlwaysVisible` (COPY-159); it is always the editor's `HelpText`. **CL-5:** when the help expands or collapses, `BringIntoView` the focused row including its help line (extends DN-6) |
| Wing | as density, with no ▾ (§2 change 1); "Estimates · definitions" as a solid-underline link (`prop-link`, DR-CELL-5) |
| Text size | as density §8.3 (owner: this build track) |
| Removed | the 4-column grid, the drawn field box at rest, the Kind radio list |

### 5.3 Named tests (headless, red first)

**B (new, 16):**
- `PropertiesPane_B_EditableValueHasDottedUnderline` (render, not style)
- `PropertiesPane_B_ReadOnlyValueHasNoEditCue`
- `PropertiesPane_B_EditCueContrastAtLeast3InThreeThemes`
- `PropertiesPane_B_FocusedValueShowsBox`
- `PropertiesPane_B_EveryEditableValueIsTabStop`
- `PropertiesPane_B_RowPressFocusesValue`
- `PropertiesPane_B_HelpShowsWhileFocused_AndIsDescription`
- `PropertiesPane_B_PointGroupHoldsPositionAndTangent`
- `PropertiesPane_B_KindIsEnum_CommitRulesMatchType`
- `PropertiesPane_B_RowPitch20_24`
- `PropertiesPane_B_HeaderTwirl24_NoBand`
- `PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused` (CL-1, DR-CELL-3)
- `KindBox_DropDownOpen_ArrowsThenEsc_NoUndoRow` (CB-3)
- `KindBox_DropDownOpen_ArrowsThenClose_OneUndoRow` (CB-3)
- `PropertiesPane_B_PendingDropIsAnnounced_PendingHelpTextNamesKeys` (CL-2, CB-4; both ComboBoxes)
- `PropertiesPane_B_FocusedRowStaysInViewWhenHelpToggles` (CL-5)

`PropertiesPane_B_EditableValueHasDottedUnderline` is a **pixel** test at 1× and 2×, in three themes, measuring ≥ 3:1
on pixels (CL-3). `PropertiesPane_Density_FocusedInputShowsWholeExpression` now includes a Length row under a handle
subhead at the 200 px dock with `#tip_chord × 0.1` (CB-1).

**Kept from the density brief (14):**
- `PropertiesPane_Density_OneFontSizeForLabelValueUnit`
- `PropertiesPane_Density_NothingBelow11`
- `PropertiesPane_Density_UnitOnValueBaseline`
- `PropertiesPane_Density_FocusedAndErrorFieldTextNotClipped`
- `PropertiesPane_Density_DrawnBoundaryOnePxInEveryState` (now: the focus and error box)
- `PropertiesPane_Density_FocusedInputShowsWholeExpression` (on dirty)
- `PropertiesPane_Density_WorstCaseNumbersNotClippedAt200`
- `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs`
- `PropertiesPane_Density_ValuesUseTabularFigures`
- `PropertiesPane_Density_HeaderMenuCopiesEachRow_NoRowContextMenus`
- `PropertiesPane_Density_EveryTargetAtLeast24`
- `PropertiesPane_Density_DefinitionsIsLinkDisclosure`
- `PropertiesPane_Density_StackedRowsAtLargeText`
- `PropertiesPane_Density_FocusedWingFieldInViewAtLargeText`

**Text size (4):**
- `TextSize_Setting_ScalesEveryPropToken`
- `TextSize_CommandPlusMinus_StepsLadder_Clamps100To200`
- `TextSize_PersistsPerUser`
- `TextSize_ModelViewKeepsZoomShortcut`

**Dropped from the density brief:**

| Test | Replaced by |
|---|---|
| `FieldHitArea24_Drawn20` | `RowPressFocusesValue` + target 24 |
| `OneFocusRing`, `FocusRingVisibleInThreeThemes` | `FocusedValueShowsBox` and `EditCueContrastAtLeast3InThreeThemes` |
| `GroupHeader24_NoChevronButton` | `HeaderTwirl24_NoBand` |
| `FactRowPitch18_InputRowPitch24` | `RowPitch20_24` |
| `KindOption24_OneDotSize` | none — Kind is an enum |
| `ValueColumnNextToLabel` | none — B keeps values at the right edge |

**B-5 regression set (from `ui-property-grid.md` §10.7), retargeted to the enum:**

| Was | Now |
|---|---|
| `Tangent_KindChange_KeepsFocusOnChecked` | `Tangent_KindChange_KeepsFocusOnKindBox` |
| `PropertiesPane_KindArrows_MoveCheckOnly_OneUndoRowPerIntent` | `PropertiesPane_KindBox_ArrowWhileClosed_IsPending_OneUndoRowPerIntent` |
| `PropertiesPane_Tangent_CheckedKindExposed` | `PropertiesPane_Tangent_SelectedKindExposed` |

`TypeCombo_ArrowWhileClosed_DoesNotCommit`, `TypeCombo_PendingThenLeave_DoesNotCommit` and
`Expander_FocusedHeader_ExposesNameAndExpandedState` are unchanged; the rest of the 30 stand as written.

**Total new or kept for this track: 34 (16 B + 14 density + 4 Text size), plus the B-5 set.**

### 5.4 Acceptance (native; the author clears none)

- **B2-1** The 34 tests (§5.3) land red first; the B-5 set is green.
- **B2-2** Native captures of the anchor, handle, field-error and Unavailable states, compared with
  `docs/proof/property-grid-cells/b-*.png` (light, dark, high contrast). They include a Windows-class 100 % display
  (DR-DEN-3 risk).
- **B2-3 Native rows** (the build fills these with native evidence; the author clears none):

  | Row | Evidence required |
  |---|---|
  | Underline ≥ 3:1 measured on pixels | pixel test at 1× and 2× in light, dark and high contrast |
  | Every value a Tab stop; focused ≠ unfocused (including in error) | keyboard walk and an AX dump; `FocusedErrorFieldDistinctFromUnfocused` |
  | A row press focuses its value | `RowPressFocusesValue` plus a pointer trace |
  | VoiceOver announces each editable value as a text field with name + unit, at rest and focused | a VoiceOver trace of the anchor and Wing rows |
  | DC-2 worst case: "−1234.56" and "12000.00" not clipped in the 62 px editor | captures at 100 % on Retina **and** on a Windows-class display |
- **B2-4** The operator OKed the three visible changes (DR-CELL-2) and the focused-error look (DR-CELL-3) on 2026-10-02.

## 6. Evidence

- **Browser check** (`node tools/check-mockup-property-grid.mjs <playwright root>`): **682 cells, 0 failing checks,
  65/65 interaction paths, 0 page errors** (repair cycle 1).
  - Cycle-1 paths (5):
    - CL-1: focused ≠ unfocused for every Tab stop in the anchor, field-error and foil states;
    - CB-1: a subhead Length row at 200 px with `#tip_chord × 0.1`;
    - CL-5: a focused row and its help in view at 200 %.
  - The CL-2 drop announcements are asserted inside the existing Kind and Type paths.
  - **CL-1 planted failure:** with the focused-error rule removed, "focused differs from unfocused … (field-error)"
    fails and the oracle exits 1.
  - The radio-list handlers, renderer and styles are deleted (CB-6, HYG-A).
  - The cells: 31 states × 3 themes × 3 docks × 2 windows in B at 100 % text, 93 at 200 % text, 31 under the 1.4.12
    override, and the before/after table.
  - New paths (12):
    - the Kind enum: pending, Return, leaving drops, pointer pick;
    - Tab reachability and visible focus in four states;
    - a label press focusing its value.
  - **Planted failure:** with the dotted underline removed from editable values, "editable values carry a non-colour
    cue (dotted underline or ▾)" fails and the oracle exits 1.
  - **Second plant (the UI-N guard):** a fixed input height fails "underlined values have no fixed height".
- **Craft gate:** 5 Minor, 0 Major, 0 Blocker, 0 off-token values. All 5 are `cramped-padding` on window chrome (frames, tab strips, title bar), the standing deviation.
- **Captures:** `docs/proof/property-grid-cells/b-anchor.png` (Aft focused), `b-field-error.png` (Aft focused in error:
  the accent box with the danger box outside it), `b-unavailable.png`
  (light, 260 px, 1440 × 900).

## 7. Residual risk

- The underline is drawn by an added template part in Avalonia; that is a spike-level assumption, with the overlay
  fallback named in §5.2.
- The "as built" column is a CSS emulation (Inferred); its anchor state uses the enum.
- DR-DEN-4 is ruled (⌘= / ⌘− zoom a focused model view, else Text size); DR-CELL-2 OKs §2's changes.
- The underline's pixel position at 1× vs 2× is a render detail that only the native pixel test proves (CL-3).
