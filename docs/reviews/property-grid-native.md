---
id: review-property-grid-native
title: Property grid — the operator's native checklist (B2, B4, B7, B8)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, properties, property-grid, native-ui, accessibility, voiceover, narrator, contrast, m1.2b]
links:
  - {to: review-ui-property-grid, rel: refines}
  - {to: property-grid-rulings, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2026-12-30
summary: >-
  The acceptance rows of the property-grid build that only a native session can prove: the VoiceOver trace and AX
  dump (B2), rendered contrast in three themes at 100 % and 200 % (B4), the field-nudge close gates (B7, B8), and the
  shell's Return conflict found and fixed by the build. Each row says what to do, what to hear or see, and what passes. The
  headless build tests cover B1, B3, B5, B6, B9 and B10.
review-suggested: []
---

# Property grid — the operator's native checklist

The property grid (brief §10 of [`ui-property-grid.md`](ui-property-grid.md)) is built and passes its headless
checks (`tools/run-tests.sh`, suite `--properties-view`). These rows need a person at the native app. The author
does not clear them (§10.10).

**Set-up.** Build and launch the app on macOS (Apple silicon, the shipped Release build). Open the Example. Dock
Properties at 260 px unless a row says otherwise. Turn VoiceOver on with ⌘F5. Record each row's result, the build
SHA and the date in the *Result* column.

## B2 — VoiceOver trace and AX dump (macOS)

Use VO-arrows to read and Tab to move. Capture an AX dump (Accessibility Inspector, *Save Snapshot*) for each state.

| # | Do | Hear or see | Pass when | Result |
|---|---|---|---|---|
| B2.1 | Select a trailing-edge control point, then make it an anchor. Tab into Properties | "Position, expanded"; "Type, Anchor point — adds handles…, combo box"; "From root, position along the span in millimetres, …"; "η, 0.643"; "Aft position in millimetres" | Every name contains its visible label and unit (§10.5). The Type help is COPY-149 | |
| B2.2 | On the anchor, Tab to the Kind group | "Tangent kind, group"; the checked option "Smooth, selected, 1 of 3" | One Tab stop; it lands on the checked option | |
| B2.3 | Select the anchor's handle toward the tip | "Handle toward the tip"; the link "of Trailing edge · anchor point N of M · Esc"; "Tangent kind of anchor point N" | The handle has its own name; its anchor is a link | |
| B2.3a | On the handle, Tab to a group header (not a field) and press Esc | The status line speaks "Selected Trailing edge · point N of M."; the anchor is selected | Spoken once from the polite status line (PG-28) | |
| B2.4 | VO-arrow onto a fact row (η) and an estimate row (MAC, AR, Max t/c) | "η, 0.643"; "MAC, approximately 107.1 millimetres"; "AR, aspect ratio, approximately 10.04 b squared over S"; "Max t over c, approximately 12.0 percent" | One line per row; abbreviations spoken in full (PG-24); no focusable unnamed element (B9) | |
| B2.5 | Drag a trailing-edge point across the leading edge and hold (estimates unavailable) | The Wing chip "≈ preview"; the rows read "MAC, unavailable"; the note "Unavailable — …" | The note is announced politely once. If VoiceOver is silent, apply the PG-09 fallback and record it | |
| B2.6 | Partial-unavailable (a shape where only MAC fails) | "MAC, unavailable"; the neighbours keep their values | Only MAC is unavailable | |
| B2.7 | In Aft type `abc`, press Return. Press Return again with the same text | "Enter a number. Aft is unchanged." spoken twice — once per failed commit — and not on any re-render | B10 / PG-22 | |
| B2.8 | Type 10.1 × the root chord in Wing › Root chord, Return | The warning line "… above the limit …" on a warning rail | Announced once, politely | |
| B2.9 | Focus Type on a control point, press ↓ (closed box), then Return | "Press Return to change the type, or Esc to keep it."; then the report "Trailing edge point i is now an anchor point with 2 handles. The rail gained k points (m → n). Largest change d mm." | ↓ does not commit; Return does | |
| B2.10 | In the Kind group press ↓ twice, then Return | "Press Return or Space to make it corner, or Esc to keep smooth." | Arrows move the check only; Return commits one undo step | |
| B2.10a | After the Return in B2.10 | The new kind is spoken from the checked option, then the status line speaks the report "Trailing edge point N is now Corner." (on a handle also "Kept the handle toward the tip; the other one moved.") | The report comes from the operation (MC-11, PG-33) and is spoken once | |
| B2.11 | Collapse the Position group with Space on its header | "Position, collapsed"; the help text is the summary ("178.26, 120.40 mm") | Name and state exposed (PG-10) | |
| B2.12 | Nudge run (with the switch on, see B7): on Aft hold ↑, release; hold ↑ again and press Esc. Repeat on a handle's Angle | While held: the Wing chip "≈ preview". On release: "Aft 121.40 mm." spoken once. After Esc: nothing committed, no undo step | D3 trace: hold, release and Esc-cancel on a length and an angle field | |
| B2.13 | Keyboard only (no pointer): with a point selected press ⌘⇧C; then Tab to a group header, press Shift+F10 (or the menu key) and choose "Copy values"; paste each into TextEdit | The first paste is every selection row as "label value unit" lines ("Aft 120.40 mm", "η 0.643"); the second is that group's rows only. Facts are still not Tab stops | Copying needs no pointer (PG-25, WCAG 2.1.1); ⌘C after focus moves never copies a row clicked earlier | Also: click the MAC row, then ⌘C pastes MAC's value (PG-38) |
| B2.14 | Open a project with a recovered edit | The band "A recovered edit is open." is announced, and focus lands on Apply | Announced once; focus on Apply (PG-14, PG-28). **Expected to fail today:** focus-on-Apply is not built — the shell's open flow moves focus to the model area after the pane renders, a ShellHost change outside the property-grid track | |
| B2.14b | In the same session, open a second project with a recovered edit | The band is announced again | Announced on the second recovery too (PG-35). **Expected to fail until PG-35 lands:** the band's text is a constant that is never cleared, so a second recovery changes nothing |
| B2.15 | Nudge a field (switch on), commit a Kind, and press Esc on a handle | After the controller refresh settles, the status line still shows the pane's report ("Aft 121.40 mm.", "Trailing edge point 7 is now symmetric.", "Selected …") and VoiceOver speaks each, including a repeat of the same report | Each report shown and spoken once, not overwritten by the controller's status (PG-36) |

## B4 — Rendered contrast (light, dark, high contrast; 100 % and 200 %)

Switch themes in the review harness (`CFDW_REVIEW_THEME=light|dark|high-contrast`). Measure with a colour picker on
a screenshot at 100 % (an external display) and at 200 % (Retina).

| # | Element and state | Pass when | Result |
|---|---|---|---|
| B4.1 | Input boundary (`ControlLineBrush`) against the row surface, unfocused and `:pointerover` | ≥ 3:1 in all three themes (light was 0.43 above 3:1 by arithmetic) | |
| B4.2 | Input focus ring (`FocusRingBrush`, 2 px inset) | ≥ 3:1 against the surface and the field fill | |
| B4.3 | Kind radio ring and checked dot, unchecked, `:pointerover`, `:checked` | ≥ 3:1; the checked state also differs by weight | |
| B4.4 | Type box border, `:focus`, `:pointerover` | ≥ 3:1 | |
| B4.5 | Group header (`Expander` toggle) fill in `:checked` (expanded), collapsed, `:pointerover` and `:pressed`; the chevron stroke; its focus ring — in light, dark and high contrast | Text and chevron ≥ 4.5:1 (chevron ≥ 3:1 minimum); ring ≥ 3:1; no Fluent light-grey fill in high contrast | |
| B4.6 | Warning rail and text (`WarningBrush`), error rail and text (`DangerBrush`) | Text ≥ 4.5:1; rail ≥ 3:1 | |
| B4.7 | High contrast inherits Light (`Program.cs` `NativeReviewThemes.HighContrast`) | No Fluent light-grey surface shows through any grid part | |
| B4.8 | Kind RadioButton `:pressed` and `:checked:pointerover` (ring and dot); Type box `:pressed`, `:dropdownopen`, its popup and its items (normal, `:pointerover`, `:selected`) — in light, dark and high contrast | Ring and dot ≥ 3:1; item text ≥ 4.5:1 on its fill, including the selected item in high contrast (black on yellow) | |

## B5 — No truncation at the 200 px dock (native confirmation)

The headless check `PropertiesPane_Rows_ShareOneLabelColumn` asserts no label is trimmed and no Kind option is
narrower than its desired width at 200 px. Confirm on the native build: dock Properties at 200 px, select the overflow
fixture (a long foil name), an anchor with Corner, and a handle. Pass when no label, option or value is clipped or
ellipsized (a group summary may ellipsize; its name may not).

**B5a.** At the 200 px dock, Tab through a long anchor selection (Corner, all handle rows) from the identity to the last
field. Pass when every focused field scrolls fully into view and is never under the pinned Wing, and every target is at
least 24 px high.

## B6a — Reduced motion (macOS)

With System Settings › Accessibility › Display › Reduce motion off, then on, collapse and expand a group. Pass when
there is no expand or chevron animation in either setting (the headless check `PropertiesPane_NoNativeMotion` covers
the transitions; this row confirms the native render).

## B7 — Field-nudge close gate (macOS)

The nudge ships **off** behind `PropertiesFieldNudge.Enabled` (`src/CfdWorkbench.Desktop/Panes/PropertiesPane.axaml.cs`).

| # | Do | Pass when | Result |
|---|---|---|---|
| B7.1 | B2.12 passes with the switch on | Every announcement is spoken | |
| B7.2 | Native Desktop lens rules the caret chords: ⌘↑/⌘↓ (caret to start/end) and ⇧↑/⇧↓ (extend selection) in a macOS single-line field | A written ruling exists. Today the run takes ⌘↑/↓ and ⇧↑/↓ only while the field text is not being edited | |
| B7.3 | Only after B7.1 and B7.2: set `Enabled = true` in a reviewed change | The headless nudge checks stay green | |

## B8 — Windows and Narrator

| # | Do | Pass when | Result |
|---|---|---|---|
| B8.1 | On Windows, with the switch on, press ↑ in Aft | Nothing nudges (`PropertiesFieldNudge.OnWindows`); the headless check `FieldNudge_OffOnWindows_UntilNarratorPass` covers it | |
| B8.2 | A Narrator/UIA pass of B2.1–B2.12 | Recorded before the nudge is turned on for Windows | |

## Found and fixed by the build — the shell took Return

`ShellHost.OnShellKeyDown` (a tunnel handler) moved focus to the From root field on Return whenever a point was
selected and focus was not in a text box. That took Return from the Type box, the Kind group and the group headers
(§10.4). **Fixed (repair cycle 1):** the handler returns early when `Properties.IsKeyboardFocusWithin`, so Return
belongs to the focused control inside Properties. Return on the canvas with a point selected still focuses From root.

Headless evidence, all in the full shell: `Shell_ReturnInProperties_ReachesTheFocusedControl` (Type, Kind and a
group header), `TypeCombo_ArrowWhileClosed_DoesNotCommit`, `Tangent_KindChange_KeepsFocusOnChecked`,
`PropertiesPane_KindArrows_MoveCheckOnly_OneUndoRowPerIntent`, `PropertiesPane_KindLeave_CommitsPendingOnce`; the
canvas path is `Focus_ReturnOnPoint_SpanFieldEscapeBack`.

| # | Do | Pass when | Result |
|---|---|---|---|
| R.1 | On the native build: B2.9 and B2.10 in the shell, then Return on the canvas with a point selected | Return commits the pending Type and Kind and focus stays on the control; Return on the canvas still focuses From root | |

## Structure B — the cells build (docs/reviews/ui-property-grid-cells.md §5)

The property sheet is now structure B (DR-CELL-1). Headless evidence: `tools/run-tests.sh`, suites `--properties-cells`
(the 34 named tests of the cells review §5.3) and `--properties-view` (the B-5 set). The rows below need a person at the
native app; the author clears none.

### Step 0 — the dotted-underline spike (CL-3): option A holds

Avalonia 11.3.14's `TextBox` has no text decoration, so the build draws the cue as a part inside the editable value's
template: `PropertiesPane.EditCue`, added beside `PART_BorderElement` when the template applies. It reads the
presenter's `TextLayout` (the text's own extent after right alignment and scrolling, clamped to the scroll viewport),
draws one 1 px line with a 1,1 dash 2 px below the baseline, and snaps both ends and the line's top to whole DIPs. A whole
DIP is a pixel boundary at 1× and at 2×, so the line covers whole device pixels at both. It hides on focus and in error,
where the box takes over. The `TextBox` stays the only focusable, named element; the overlay fallback was not needed and
no control is swapped.

Measured on pixels by `PropertiesPane_B_EditableValueHasDottedUnderline` (a headless `RenderTargetBitmap` of the shell at
1280 × 800; a trailing-edge control point's Aft value; the reference colour is the surface 3 px left of the line):

| Theme | 1× (96 dpi) | 2× (192 dpi) |
|---|---|---|
| Light | 17 dash px of 34, 17 gap px, **6.11:1** | 34 of 68, 34 gaps, **6.11:1** |
| Dark | 17 of 34, 17 gaps, **8.60:1** | 34 of 68, 34 gaps, **8.60:1** |
| High contrast | 17 of 34, 17 gaps, **17.62:1** | 34 of 68, 34 gaps, **17.62:1** |

Every dash pixel is the exact accent (`PrimaryBrush`), so the measured ratios equal the token arithmetic in the cells
review §3(a). Verified headless on macOS (a Retina host; the test renders at both densities). Native confirmation is row
B3.1 below.

### What the headless build measured (macOS host, 1280 × 800, 260 px dock, light)

`--properties-cells` prints these `MEASURE` lines on every run:

| Quantity | Brief (cells §1) | Built |
|---|---|---|
| Label / value / unit font | 11 / 11 / 11 | 11 / 11 / 11 (`OneFontSizeForLabelValueUnit`); nothing below 11 in the anchor, handle and unavailable states |
| Read-only / editable row | 20 / 24 | 20 / 24 (`RowPitch20_24`; η, MAC 20; From root, Aft, Type, Span 24) |
| Group header | 24, no band | 24 for Point and the rail group, transparent, 10 px muted chevron at the left |
| Focus box | 20 px, 1 px accent | 58 × 20 inside the 62 × 24 band, by Tab and by pointer |
| Focused error | accent box, danger box 1 px outside | yes (`FocusedErrorFieldDistinctFromUnfocused`); unfocused: the danger box only |
| Identity block | 45 | 31. The approved capture `b-anchor.png` also measures 31 (62 px at 2×), so the brief's 45 is Flagged |
| Wing block | 230 | 235 (the approved capture measures 234) |
| Baselines | unit on the value's baseline | label, value and unit at the same y to 0.01 px, on an input row and on a fact row |
| DC-2 worst case at 200 px | not clipped in the 62 px editor | "−1234.56" 47 px and "12000.00" 46 px of the 52 px text room; "≈ 38125" one line |

**Face.** Tabular figures need a face that has them. The default face (Helvetica) has proportional digits and
ignores `tnum`. The macOS system face honours `tnum` but renders heavy at 11 px through Skia, and a fallback list does
not resolve it. So the pane uses Helvetica Neue on macOS (tabular digits) and Segoe UI elsewhere (honours `tnum`):
`PropFontFamily` in `Styles.axaml`. Row **B3.6** checks the look on both platforms.

### Text size (DN-5) — persistence is not built

View ▸ Text size (Bigger, Smaller, 100 / 125 / 150 / 200 %) and the DR-DEN-4 routing are built and tested. ⌘= / ⌘−
zoom a focused Plan, 3D or Section view and step the Text size anywhere else. **Persisting the setting is not built.**
`PreferenceStore` has only the frozen `cfdw-layout` v1 document and the Recent list; storing a Text size needs a
Persistence change, which this track may not make. `TextSize_PersistsPerUser` is deferred to a track that owns
`src/CfdWorkbench.Persistence/`. Until then the setting lasts for the session.

A clicked View ▸ Zoom in item takes the same route as its shortcut. With focus in a pane, that click steps the Text
size; the platform menu cannot tell a click from the key. Row B3.7 asks the operator to accept this.

### B2-2 / B3 — native rows (the author clears none)

| # | Do | Pass when | Result |
|---|---|---|---|
| B3.1 | Underline ≥ 3:1 on pixels: screenshot Aft at rest in light, dark and high contrast, on a Retina display and on a Windows-class 100 % display; pick a dash pixel and the surface beside it | Each ≥ 3:1. The headless figures are 6.11, 8.60 and 17.62 at 1× and 2× | |
| B3.2 | Captures of the anchor, handle, field-error and Unavailable states in light, dark and high contrast, compared with `docs/proof/property-grid-cells/b-*.png`, **including a Windows-class 100 % display** (DR-DEN-3: 11 px legibility) | Same structure and states as the approved captures; 11 px legible at 100 % | |
| B3.3 | Every value a Tab stop; focused ≠ unfocused, including in error: keyboard walk of the anchor (Smooth and Corner), the handle and a control point, plus an AX dump | Every editable value and enum is reached; each focused stop shows its box; a focused error shows both boxes. Headless: `EveryEditableValueIsTabStop`, `FocusedErrorFieldDistinctFromUnfocused` | |
| B3.4 | A row press focuses its value: click the label "Aft", the label "Type" and the gap between them, with a pointer trace | Focus lands on the value; a read-only row takes no focus. Headless: `RowPressFocusesValue` | |
| B3.5 | VoiceOver on the anchor and Wing rows, at rest and focused | Each editable value is a text field read as "<label> … millimetres" (name and unit); each enum is a pop-up button with its value; a fact reads "<label>, <value> <unit>" | |
| B3.6 | DC-2 worst case: type −1234.56 into Aft and 12000.00 into Span, Return, at 100 % on Retina **and** on a Windows-class display | Neither number is clipped in the 62 px editor; check the face (Helvetica Neue / Segoe UI) reads as the approved look | |
| B3.7 | Text size: each step from the View menu, ⌘= / ⌘− in Properties and in the Plan view | Properties: the size steps and "Text size 150 %." is spoken; Plan: the view zooms. Operator accepts that a menu click on Zoom in also follows focus | |
| B3.8 | Wing focus at 200 %: set Text size 200 %, Tab to Tip chord, type abc, Return | The field and its message are in view inside the Wing; the Wing scrolls inside itself (DR-DEN-2). Headless: `FocusedWingFieldInViewAtLargeText` | |
| B3.9 | Stacked rows at 150 % and 200 % on the anchor and the 200 px dock | Each value sits under its label, right-aligned; nothing clipped | |

Rows B2.2, B2.10 and B2.10a above describe the retired Kind radio list. On the B build, read them for the Tangent
kind ▾: ↓ stages ("Press Return to make it corner, or Esc to keep smooth."), Return applies, and leaving says
"Tangent kind unchanged: Smooth.".
