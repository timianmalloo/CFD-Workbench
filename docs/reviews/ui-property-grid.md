---
id: review-ui-property-grid
title: UI review — the Properties pane as a property grid (F-1)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, properties, property-grid, native-ui, accessibility, m1.2b, m1.2b2]
links:
  - {to: mockup-property-grid, rel: documents}
  - {to: review-m12b-native, rel: refines}
  - {to: design-m12b-points, rel: relates-to}
  - {to: design-m12b2-3d-elevations, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-12-30
summary: >-
  Elevate-mode review of the M1.2b Properties pane (build c43711a). Measured first: three equal headings, four value
  x-positions in one block, four of twelve quantities without a unit, nine of nine dark tokens drifted from DESIGN.md,
  input boundaries at 1.49:1 and no visible current tangent kind. Verdict BLOCK on the as-built pane. The fix is one
  reusable property grid (identity, groups, label | value | unit rows); building it is the highest-leverage change,
  and the implementation brief in section 10 maps it to Avalonia for the M1.2b fix track and M1.2b2's PNL track.
review-suggested: []
---

# UI review — the Properties pane as a property grid

*Produced by `/ui-design`, mode **elevate**. Author: track UID (session `f19a2b12-uid`). Governed by
`ui-design-craft.md` DX22–DX25 over `ui-interaction-design.md` U1–U20, `technical-ui-design.md` TQ1–TQ12 and the
native-client rows of the trigger table.*

**Surface reviewed:** `src/CfdWorkbench.Desktop/Panes/PropertiesPane.axaml` + `.axaml.cs`, `PropertiesView.cs`,
`Styles.axaml` at `3b396e5` (pane code identical to the reviewed build `c43711a`), and the operator captures
`docs/proof/m12b-native/operator/plan-select.png` and `anchor-handle.png` (1400 × 907 window crops).
**Reviewed against:** spec Part B (B11, B12) and Part C (UI-36, UI-37, UI-39, UI-40) of
`docs/specs/cfd-workbench-v1.md`; `DESIGN.md`; `docs/design/m12b-points.md` §11.4–11.6; `docs/design/m12b2-3d-elevations.md`
§11.4, §14; the v10 mockup; `docs/lessons/defect-classes.md`.
**Proposal:** [`docs/mockups/property-grid.html`](../mockups/property-grid.html) (hub [`property-grid.md`](../mockups/property-grid.md)).
**Reviewers so far:** the author only. **The UX & Accessibility veto and the Marine-CAD lens are pending** — the
Coordinator convenes them on this document and the mockup. Nothing here clears WCAG 2.2 AA.
**Date:** 2026-10-01.

## 1. Verdict

> **BLOCK (as built)** — the pane is a stacked document, not an inspector: no shared column, missing units, an
> invisible current tangent kind and sub-3:1 input boundaries. **PASS-WITH-CONDITIONS (proposal), pending the
> independent accessibility review.**
> **Highest-leverage change:** build one property-grid component — selection identity, property group, property row
> (label | value | unit on one shared column, with kinds and states) — and render every Properties block through it.
> One change in two files (plus tokens) clears F-1, O-4, O-6, the F-4 display half and the UI-36 fit, and it is the
> component PNL reuses.

| | As built | Proposal (author's measure) |
|---|---|---|
| Blockers (sev 4, or accessibility ≥ 3) | 3 (#10, #11, #12) | 0 claimed — pending independent review |
| Majors (sev 3) | 9 | 0 known |
| Minors (sev 2) | 6 | 1 (#20, chrome deviation) |
| Nits (sev 1) | 1 | 0 |

**Accessibility veto:** **pending independent review.** It clears when someone other than the author confirms, on the
mockup and then on the native build: every number field's accessible name holds its label and unit; the tangent group
exposes its name and the checked kind; input boundaries are ≥ 3:1 in light, dark and high contrast; focus order and
Escape behave as in §10.3; and a VoiceOver trace of the anchor, handle and unavailable states reads as §10.4.

## 2. Direction brief (Stage 1 — written before the mockup)

**Who and in what state.** A foil designer mid-edit, eyes on the Plan view, glancing left to read a number or type
one. They arrive focused and impatient: the pane must answer "what did I select, what are its numbers, what can I
change" in one glance and take a typed value without a hunt. The operator's words: it "doesn't look like a properties
sheet but just a bunch of text" (F-1).

**Job to be done.** Read the selection's exact values and the wing's running estimates; type a precise value; change
a point's type or tangent. Reading is parallel (scan a column of numbers); entering is serial (Tab, type, Return).

**Archetype.** Stays inside DESIGN.md's `ParametricWorkbench` (catalog §G1). The pane is its **property inspector**
sub-component: `Inspector { Layout:LabelValueUnitGrid; Density:Compact; Input:KeyboardFirst+Precision;
Grouping:Collapsible; Feedback:Inline+StatusLine; Motion:Micro }`. Checked against the task shape: a two-column grid
serves both halves — the value column is scanned in parallel, and Tab walks the inputs serially. A form layout
(labels above fields) serves only entry and doubles the height; that is the as-built layout and the reason the Wing
falls below the fold.

**Three adjectives / their opposites.** *Tabular* (not prose) · *Exact* (not approximate-looking; every number with
its unit and precision) · *Quiet* (not decorated; state shows only when there is state).

**Named references, what is taken, what is refused.** Sources are listed in §12.

| Reference | Adopted | Rejected, and why |
|---|---|---|
| **VS Code Settings editor** | Grouped sections; a description under the setting rather than in a tooltip; a coloured bar at the row's left edge — here a **state rail** (focus, warning, error, unavailable) paired with an icon and text, never colour alone | The gear menu with "Reset to default" and copy-ID actions: a geometry position has no default, and Undo is the reset. Search/filter: a point has at most nine rows (YAGNI) |
| **VS Code Variables view / Debug Adapter Protocol** | A read-only value is a different kind of row (DAP `presentationHint.attributes: readOnly`), not a disabled edit box: plain text, lock glyph, reason | The expandable object tree: the selection has no nesting deeper than one group |
| **Adobe Premiere Pro Effect Controls** | Twirl-down groups whose open/closed state is remembered; one aligned label column with values to the right; a header summary when collapsed; arrow-key stepping in a value with modifier steps (proposal DR-UID-2, mapped to the canvas nudge ladder 0.01 · 0.1 · 1 mm) | **Scrubbable values (drag on the number):** a 1 px drag has no honest mapping to a 0.01 mm field, it fights text selection in an expression field, and the canvas already gives direct manipulation (TQ6). Keyframe stopwatches: no time axis. Reset-parameter buttons: no default, Undo is the reset |
| **CAD property sheets (Fusion 360 dimension entry; Rhino Properties)** | Expressions with units and parameter names in a dimension field, echoed in the document unit (`15 cm = 150.00 mm.`); the panel follows the selection and falls back to the document (Foil) when nothing is selected; type shown read-only for named objects | Boxed group borders on every section (the "everything in a card" tell); a modal PropertyManager with OK/Cancel — every commit here is already one undo step |
| **v10 mockup (operator-approved)** | The 56 px label column at the 200 px dock, the Wing block pinned at the foot, "Estimates · definitions" | v10's editable multi-selection: M1.2b ruled it read-only (OI-3) |

**Anti-goals.** No cards per group; no tooltip as the only place for a unit or a reason; no blank or dash for a value
that failed; no second identity header; no animation beyond the chevron.

**Constraints.** Native desktop, macOS first (Avalonia 11.3.14), Windows deferred. Dock widths 200–300 px; window
1280 × 800 minimum target. Tokens from DESIGN.md only. WCAG 2.2 AA obligation (DESIGN.md archetype `A11y:WCAG_2.2_AA`).

**Personality — type, colour, space.** *Type:* three sizes only — 14 px semibold for the identity (the one focal point),
13 px for labels and mono values, 12 px for units, descriptions and messages — because a pane at 260 px cannot carry an
18 px heading per block, and weight plus position gives enough hierarchy. *Colour:* the chrome stays neutral; colour
appears only as state (focus rail, warning, error) and as the checked tangent, so a coloured row always means
something. *Space:* tight inside a group (rows abut, 24–32 px), a rule and a header band between groups — grouping by
space, not by boxes.

## 3. Triggered standards (mapped at Stage 1)

| Trigger | Fires? | Why | Applied as |
|---|---|---|---|
| UI-T1 expert / quantitative | **Yes** | precision CAD inspector of lengths, angles, ratios and estimates | TQ2 tabular right-aligned unit-bearing numbers at one precision; TQ5 "≈" estimates and an Unavailable state; TQ6 canvas drag plus typed fields; TQ7 units everywhere; TQ9 "≈ preview" and "Checking…" |
| UI-T2 generated assets | No | no generated imagery | — |
| UI-T3 fronts a model | No | no model call in Properties | — |
| UI-T4 native client | **Yes** | Avalonia desktop app | Medium `native-desktop`; platform `cross-platform` (macOS first); framework `avalonia` 11.3.14; distribution unsigned `.app` (dev); accessibility API NSAccessibility (macOS), UIA (Windows, deferred); HIG: Apple HIG (macOS). `xaml-token-lint.py` run (§4). Native proof rows in §11, all pending. **Flagged:** the knowledge pack `docs/knowledge/native-client-ui-design/` named by the trigger table does not exist in this repo; the template `docs/ai-forward-pack/templates/native-ui-proof-pack.template.md` does |
| Floor | Yes | always | U1–U20, DX1–DX25, CD1–CD20, S2/S7 — Part B (B11, B12) exists and covers the error and recovery paths for Properties, so the UX layer is settled |

## 4. Measurements of the as-built pane (DX23)

| Metric | Value | Evidence · confidence |
|---|---|---|
| Interactive controls, anchor selected / handle selected | 10 / 8 | `PropertiesPane.axaml` · Verified |
| Simultaneous sections | 5 (document heading, Foil, point, Wing, "Estimates") | `.axaml`:24-120 · Verified |
| Headings at the same 18 px `HeadingSize` | 3 ("Untitled", the point, "Wing") — three competing focal points; the selection is the second | `.axaml`:24, 38, 79 · Verified |
| Distinct type sizes | 3 (18 · 14 · 12) | `Styles.axaml` · Verified |
| Value x-positions in one block | 4 (Span 63, Aft 47, Angle 66, Length 74 px from the pane edge) — the input starts where its label ends | `anchor-handle.png` · Verified |
| Label/value columns in the Wing | 0 — every label sits above its value | both captures · Verified |
| Quantities shown / without a unit / non-standard unit | 12 / 4 (Root chord, Tip chord, Mean chord, MAC) / 2 ("deg"; Max t/c as the ratio 0.12) | captures + `.axaml.cs`:218-222 · Verified |
| Quantities rendered twice | 2 (Area, Aspect ratio: "≈" rows and "Estimates" captions, different formats); a third Area format (m², F3) in `PropertiesView.cs`:112 | Verified |
| Precision against UI-40 | 3 violations: Wing Span 0.1 mm (typed: 0.01), Mean chord / MAC 0.01 mm (derived: 0.1), Max t/c ratio (0.1 %) | Verified |
| Current tangent kind visible | **No** — three plain Buttons, no checked state, no group label | `.axaml`:55-59 · Verified |
| Number fields with an accessible name | 1 of 7 (only `SpanInput`; no `LabeledBy` on the others) | `.axaml` · Verified in source; VoiceOver not run |
| Input boundary contrast (`LineBrush` on `SurfaceBrush`) | light 1.49:1, dark 2.61:1 (need 3:1) | token arithmetic · **Inferred** for the rendered control (Fluent's template may add a stroke; measure natively) |
| Wing fully visible at 1400 × 907 | No — cut below MAC; Aspect ratio, Area and the disclosure need scrolling | `plan-select.png` · Verified; at 1280 × 800 Inferred worse |
| Raw values in pane markup | 17 (15 `Spacing` literals in `.axaml`, `Width = 100` and `Spacing = 4` in `.axaml.cs`) | grep · Verified |
| `xaml-token-lint.py PropertiesPane.axaml` | clean — it does not check `Spacing` literals (a lint gap, finding #16) | run · Verified |
| `design-lint.py DESIGN.md --strict` | clean before and after this run | run · Verified |
| Dark / high-contrast token drift, `Styles.axaml` vs DESIGN.md | 9 of 9 dark colours differ (e.g. surface `#17272c` vs `#1e2d31`, primary `#66ddc8` vs `#88d8c6`); HC accent `#ffff00` vs `#ffee58`; no warning, selection or control-line brush exists | `Styles.axaml`:6-19 vs DESIGN.md frontmatter · Verified |
| Network calls, LCP/INP | n/a (native, local) | — |

## 5. Findings (structure before surface)

Severity 4 Blocker · 3 Major · 2 Minor · 1 Nit. Accessibility ≥ 3 is a Blocker (U16).

| # | Location | Dimension | Sev | Evidence | Recommended fix | Confidence |
|---|---|---|---|---|---|---|
| 1 | Whole pane | Archetype fit | 3 | A stacked document: 3 equal headings, labels above values in the Wing, ragged rows elsewhere (§4) | The property grid: identity · groups · rows on one shared column (§10) | Verified |
| 2 | Wing block | Flow & IA | 3 | The Wing scrolls with the selection; MAC is the last visible row in the capture; UI-36 requires selection + Wing to fit at 1280 × 800 | Pin the Wing at the foot (≤ 55 % of the pane, own scroll only if it must); label-left rows about halve its height (277 px measured in the mockup; the stacked Wing runs ≈ 370 px from its header to MAC in `plan-select.png` with five rows still below — Inferred ≥ 500 px) | Verified (as built); measured (proposal) |
| 3 | Handle selection | IA / identity (O-6) | 3 | Header "Trailing edge · point 7 of 14" and the anchor helper while Type reads "Anchor handle" (`.axaml.cs`:282, 303) | One identity per selection: "Handle toward the tip", crumb links to its anchor | Verified |
| 4 | Tangent | Findability (F-4) | 3 | Unlabelled buttons, shown only on the anchor (`.axaml.cs`:311); the handle hides its anchor's kind | A labelled "Tangent" group; on a handle, the parent's kind as an editable radio group | Verified |
| 5 | Type change | Flow (F-5) | 3 | "Anchor point" reads as "add a point"; 10 → 13 points appear | Option label COPY-153 and report COPY-154 counting handles and points | Verified (operator) |
| 6 | Wing estimates | State completeness | 3 | "≈ —" with no reason; one non-finite MAC blanks all five (`.axaml.cs`:217); the reason line shows a raw `Compute.Outcome` | Per-quantity availability; "Unavailable" + COPY-155 reason; class BLANK-ESTIMATE | Verified |
| 7 | Typed chord above the fit limit | State / copy | 3 | Rendered in `MutedBrush`, no icon (`.axaml`:100, `.axaml.cs`:426); m12b §11.4 requires the warning style and an icon | Warning rail + icon + text in the warning token; status line keeps the full report | Verified |
| 8 | Read-only freedoms | State completeness | 2 | Disabled inputs; the reason is one shared line above them | Read-only facts as text with a lock glyph and the reason in the group | Verified |
| 9 | Busy / loading | State completeness | 2 | "Checking the last change…" (m12b §11.4) has no pane surface | Wing chip "Checking…" + status line | Verified |
| 10 | Text fields | Accessibility (1.4.11) | 4 | Input boundary 1.49:1 light, 2.61:1 dark | Border `control-line` (3.43:1 light, 4.27:1 dark) | Inferred (render not measured) |
| 11 | Number fields | Accessibility (4.1.2, 2.5.3) | 4 | 6 of 7 fields have no accessible name; none but Span names its unit | `AutomationProperties.Name` = label + unit ("Aft position in millimetres") on every field | Verified in source |
| 12 | Tangent buttons | Accessibility (4.1.2, 1.4.1) + F-4 | 4 | The current kind is not shown or exposed at all | RadioButtons in a named group; checked = fill + weight + check mark | Verified |
| 13 | Units and precision | Content (TQ2, TQ7, UI-40) | 3 | §4: 4 missing units, "deg", t/c ratio, three precision violations | One formatter table per quantity (§10.2) | Verified |
| 14 | Area / Aspect ratio | Content (derive, don't store) | 2 | Rendered twice in two formats; a third in `PropertiesView` | Delete the "Estimates" captions and the m² row; one formatter | Verified |
| 15 | `Styles.axaml` | Token discipline | 3 | 10 colours drifted from DESIGN.md; no warning/selection/control-line brush | Re-sync dark and HC to DESIGN.md; add the missing brushes (UXR owns the file) | Verified |
| 16 | Pane markup | Token discipline | 3 | 17 raw spacing/width literals; the linter does not see them | Spacing and width tokens; extend `xaml-token-lint.py` to `Spacing`/`Width` (pack-level) | Verified |
| 17 | Copy | Content | 2 | "deg"; bare "Anchor handle" with no "Type" label; raw outcome codes | "°"; a labelled Type row; reasons in words | Verified |
| 18 | Hierarchy | Craft | 2 | The document name "Untitled" outranks the selection | Identity first; the foil name only when nothing is selected | Verified |
| 19 | Status line | Content (O-5) | 1 | "No point change." on a fresh foil | Empty until a report exists (the mockup does this) | Verified |
| 20 | Proposal, frame and tab strip | Craft (detector) | 2 | Craft gate `cramped-padding` × 4 on `.frame` and `.tabs` | Recorded deviation: window chrome whose bars are full-bleed by design; not product rows | Verified |

## 6. Scorecard (as built → proposal)

| # | Dimension | As built | Proposal | Worst as-built finding |
|---|---|---|---|---|
| 1 | Visibility of system status | Fail | Pass | #6 |
| 2 | Match to the real world | Partial | Pass | #5 |
| 3 | User control & freedom | Pass | Pass | — |
| 4 | Consistency & standards | Fail | Pass | #13 |
| 5 | Error prevention | Partial | Pass | #5 |
| 6 | Recognition over recall | Fail | Pass | #4 |
| 7 | Flexibility & efficiency | Partial | Pass | #2 |
| 8 | Aesthetic & minimalist design | Fail | Pass | #1 |
| 9 | Error recovery | Partial | Pass | #6 |
| 10 | Help & documentation | Pass | Pass | — |
| 11 | Archetype fit | Fail | Pass | #1 |
| 12 | State completeness | Fail | Pass (22 states) | #6 |
| 13 | Token discipline | Fail | Pass (0 off-token) | #15 |
| 14 | Accessibility (WCAG 2.2 AA) | **Fail** | **Pending independent review** | #12 |
| 15 | Performance & stability | Pass | Pass (no layout shift: rows reserve their message line only when present; noted) | — |
| 16 | Content & copy | Fail | Pass | #13 |
| 17 | Craft | Fail | Pass | #18 |
| 18 | AI-surface honesty | n/a | n/a | — |

## 7. Generic-tells self-check (DX3) — the proposal

| Tell | Present? | Justification |
|---|---|---|
| Violet gradient / lone saturated blue | No | DESIGN.md teal, used only for state |
| Same-radius cards everywhere | No | groups are header bands and rules; no boxes |
| Three equal stat tiles | No | — |
| Uniform spacing | No | rows abut inside a group; band + rule between |
| One or two type sizes | Three, deliberately | §2 personality; weight and position carry the rest |
| Placeholder content | No | captured fixture values; the long-name and −175.60 / 1 234.56 overflow fixture |
| Emoji icons | No | inline SVG glyphs that match the Plan view |
| Happy-path only | No | 22 states incl. empty, loading, error, unavailable, warning, read-only, overflow |
| Symmetry everywhere | No | left labels, right values, units in a narrow third column |
| Motion on everything / none | One moment | the chevron (120 ms; instant under reduced motion) |

## 8. The Simplifier's delete-list

```
delete:  the "Estimates" caption pair (Area, Aspect ratio) — a second rendering of two rows
delete:  hidden RootChordText / TipChordText duplicates of the chord inputs
delete:  the document-name heading above a selection (the identity replaces it)
delete:  the identity crumb when it repeats the Type row
delete:  the Area (m², F3) row in PropertiesView — a third format of one quantity
shrink:  three helper paragraphs → one description line under the row each explains
yagni:   per-row reset / copy menus (VS Code gear) — no default exists; Undo is the reset
yagni:   search/filter over at most nine rows
native:  Expander's ExpandCollapse pattern and RadioButton's SelectionItem pattern instead of hand-rolled buttons
net: -7 elements possible.
```

## 9. Copy record (quoted from DESIGN.md §7; the mockup uses these strings verbatim)

| Situation | Row | String |
|---|---|---|
| Control point description | COPY-117 | A control point pulls the curve toward it. The curve does not pass through it. |
| Anchor point description | COPY-149 | An anchor point is on the curve. Its handles set the curve's direction on each side. |
| Tangent descriptions | COPY-150 / 151 / 152 | Handles stay in line. Their lengths can differ. · Handles stay in line and equal in length. · Each handle moves on its own. The curve has a corner here. |
| Type option (anchor) | COPY-153 | Anchor point — adds 2 handles |
| Type change report | COPY-154 | <Curve> point <i> is now an anchor point with 2 handles. The rail has <n> points (was <m>). Largest change <d> mm. |
| Estimates unavailable | COPY-155 | Unavailable — <reason>. Undo, or edit again, to recompute. |
| Root-mirror handle | COPY-156 | Square to the centre line (root mirror). Only its length can change. |
| Expression echo | COPY-157 | <typed> = <value> mm. |
| Field errors, tip closes, section editor, empty, pane error | COPY-106, 118, 108, 122, 136, 137, 138 | as written in DESIGN.md §7 |
| Chord report / warning, not checked, recovered, busy, tangent change | `docs/design/m12b-points.md` §11.4 | quoted there; the mockup uses the 152.09 and 190 mm strings verbatim |

COPY-149 to COPY-157 are proposals for the spec owner (the spec's C3 table is the authority). COPY-154's counts and
COPY-155's reason are substituted from the operation's own result, never typed as constants.

## 10. Implementation brief (for the M1.2b fix track and M1.2b2 PNL)

### 10.1 The model (`PropertiesView.cs`) — one row type, filled per selection

Today `PropertyRow` has `Label, Value, IsEditable, Unit, HelperText` and two kinds. Proposed shape (names are a
suggestion; the contract is the fields):

| Type | Fields |
|---|---|
| `PropertiesModel` | `Identity` (`Glyph` {Foil, Control, Anchor, End, Handle, Several, Station}, `Title`, `Crumb?`, `CrumbTarget?`), `Groups`, `Wing?`, `Banner?`, `EmptyState?` |
| `PropertyGroup` | `Id` (stable, for remembered collapse), `Title`, `Summary` (formatted values, shown collapsed), `Collapsible` (Wing: false), `Chip?` {Preview, Checking, Unavailable}, `Rows`, `Note?` (reason text + kind) |
| `PropertyRow` | `Key` (stable: `p:span`, `h:angle`, `w:root`…), `Label`, `Kind` {Input, Fact, Estimate, Choice, Segmented, Action}, `Value` (pre-formatted by §10.2), `Unit` (`null` only with `Dimensionless = true`), `State` {Normal, Warning, Error, Unavailable, Mixed, Locked}, `Description?`, `Message?` (text + kind {Report, Echo, Warning, Error, Reason}), `AutomationName`, `Options?` |

Per-quantity availability: each estimate row carries its own `State`; one non-finite MAC makes MAC Unavailable, not
Mean chord. `PropertiesView.Build` stays the single place that decides rows, so M1.2b2 adds Height / Twist / t/c rows
by data (§3.6 table of the M1.2b2 design), not by layout.

### 10.2 One formatter per quantity (UI-40, DESIGN.md §12.0f)

| Quantity | Shown | Unit | Example |
|---|---|---|---|
| Typed or placed length (Span, Aft, Root/Tip chord, Wing Span, handle length, Height) | 0.01 | mm | 1000.00 |
| Derived length (Mean chord, MAC, station chord) | 0.1, "≈ " | mm | ≈ 107.1 |
| Angle (handle, twist) | 0.01 in a field | ° | −21.08 |
| Ratio (Max t/c, t/c) | 0.1 | % | ≈ 12.0 |
| Aspect ratio | 0.00, "≈ " | dimensionless | ≈ 10.04 |
| Area | 1, "≈ " | cm² | ≈ 996 |
| η | 0.000 | dimensionless | 1.000 |

Negative numbers use U+2212 in read-only text; fields accept both minus signs.

### 10.3 Avalonia control mapping

Verified present in `Avalonia.Controls` 11.3.14 (`~/.nuget/packages/avalonia/11.3.14`): `Expander`, `ToggleButton`,
`NumericUpDown`, `HeaderedContentControl`, `Grid.IsSharedSizeScope`, `ColumnDefinition.SharedSizeGroup`.

| Part | Control | Notes |
|---|---|---|
| Pane | `DockPanel`: Wing `Border` docked Bottom with `MaxHeight` = 55 % of the pane (bind in code on `Bounds`); selection `ScrollViewer` fills | the Wing never scrolls away (UI-36) |
| Identity | `Grid` 16 px glyph `Path` (shared geometry with `PlanCanvas` glyphs) + `TextBlock` 14 semibold + optional crumb `HyperlinkButton` | one per model |
| Group | `Expander` restyled: header = chevron + title + summary (summary visible when collapsed); `IsExpanded` two-way to a per-`Id` dictionary owned by the pane | native ExpandCollapse automation pattern; no expand animation (HardCut) |
| Wing group | `HeaderedContentControl` (not collapsible) with the chip in the header | — |
| Row | `Grid ColumnDefinitions="{PropLabelWidth},*,{PropUnitWidth}"` — fixed token widths, switched to the narrow pair by a `narrow` class when the pane is under 230 px. `SharedSizeGroup` is available but fixed widths are deterministic and testable | `Border BorderThickness="3,0,0,0"` = the state rail, brush by state class |
| Input | `TextBox` `TextAlignment=Right`, mono font, height 28 in a 32 px row, border `ControlLineBrush` | not `NumericUpDown`: its spinner buttons cost width at 200 px and it cannot take expressions |
| Fact | `SelectableTextBlock` (values are copyable) + lock `PathIcon` when locked | never a disabled `TextBox` |
| Estimate | `TextBlock` "≈ " + value; "Unavailable" when unavailable | — |
| Choice (Type) | `ComboBox` | commit on selection close only |
| Segmented (Tangent) | three `RadioButton`s, one `GroupName`, in a named `StackPanel` (`AutomationProperties.Name="Tangent"`), styled as segments; checked = `PrimaryBrush` fill + semibold + check glyph | arrows move and commit (one undo step each) |
| Message | `TextBlock` with icon, full row width; errors also set `AutomationProperties.HelpText` on the field and `LiveSetting=Assertive` on the message | reports stay in the polite status line |

### 10.4 Keyboard map

| Key | In | Does |
|---|---|---|
| Tab / ⇧Tab | pane | next / previous: group headers, inputs, the Type box, the Tangent group (one stop), the crumb link |
| Return | input | commit (one undo step); focus stays |
| Leaving the field | input | commit, focus goes where it was sent (UI-39) |
| Escape | input | restore the shown value; a second Escape returns focus to the canvas target (existing `OnPointKeyDown`) |
| Escape | handle selection, outside a field | select its anchor point |
| ↑ / ↓ (⌘ 0.01 · plain 0.1 · ⇧ 1) | number input | step and commit (proposal DR-UID-2; Return-free) |
| ← → ↑ ↓ Home End | Tangent group | change the kind |
| Space / Return | group header | collapse / expand |
| F6 | anywhere | next region (Plan ↔ Properties), existing |

### 10.5 Automation names (each contains the visible label — SC 2.5.3)

| Row | Name | Help text |
|---|---|---|
| Span (point) | Span position in millimetres | the message, if any |
| Aft | Aft position in millimetres | " |
| Type | Type | the description (COPY-117 / COPY-149) |
| Tangent group | Tangent (on a handle: "Tangent of anchor point 7") | the kind's description |
| Angle (Smooth/Symmetric) | Angle, both handles, in degrees | — |
| To root / To tip | To root, handle length in millimetres · To tip, handle length in millimetres | — |
| Corner handle rows | Angle of the handle toward the root, in degrees · Length of the handle toward the root, in millimetres (and toward the tip) | — |
| Handle selection | Angle in degrees, from the span axis, positive aft · Length in millimetres | — |
| Wing | Span in millimetres · Root chord in millimetres · Tip chord in millimetres | the chord report or error |
| Estimates | label + value + unit as text ("MAC, approximately 107.1 millimetres"); "MAC, unavailable" | the group's reason |
| Group header | the title; state collapsed/expanded from the pattern | the summary when collapsed |

### 10.6 Tokens (`Styles.axaml`, owned by UXR)

Add `PropLabelWidth` 88, `PropLabelNarrowWidth` 56, `PropUnitWidth` 32, `PropUnitNarrowWidth` 28, `PropHeadHeight` 24,
`PropRowReadOnlyHeight` 24, `ControlHeightDense` 28; brushes `ControlLineBrush` (`control-line` / `dark-control` /
`contrast-ink`), `WarningBrush` (`warning` / `dark-warning` / `contrast-primary`), `SelectionBrush`. Re-sync the nine
dark colours and the high-contrast accent to DESIGN.md (finding #15). Replace the 17 literals (finding #16).

### 10.7 Named tests proposed for the fix track (red first, fast ring, headless)

| Test | Protects |
|---|---|
| `PropertiesView_EveryQuantityRow_HasUnitOrIsDimensionless` | O-4 |
| `PropertiesView_Formatter_OnePrecisionPerQuantity` (table §10.2) | UI-40 |
| `PropertiesView_HandleSelection_OwnIdentity_ParentTangentRow` | O-6, F-4 |
| `PropertiesView_OneNonFiniteEstimate_OthersStillShown` | BLANK-ESTIMATE |
| `PropertiesPane_EstimatesUnavailable_ShowReasonNeverDash` | D-4 symptom |
| `PropertiesPane_Rows_ShareOneLabelColumn` (every row's value column starts at one x) | F-1, UI-TRANSLATION-LOSS |
| `PropertiesPane_SelectionAndWing_FitAt1280x800` (anchor state, 260 px dock) | UI-36 |
| `PropertiesPane_NumberFields_AutomationNameHasLabelAndUnit` | finding #11 |
| `PropertiesPane_Tangent_CheckedKindExposed` | finding #12 |
| `PropertiesPane_FieldBlur_CommitsAndKeepsFocusTarget` | UI-39, UI-C |

### 10.8 Who builds what

The mockup proves the component; the build splits by file ownership. **Recommendation (DR-UID-4):** the M1.2b fix
track builds the component in `PropertiesPane.axaml`(.cs) and the row model in `PropertiesView.cs` together with F-4
(review §6 already pairs them); M1.2b2's PNL track then only adds channel rows through `PropertiesView.Build`; UXR
lands §10.6 tokens. PNL owns `PropertiesView.cs` in M1.2b2 §14, so the fix track must join before PNL dispatches.

## 11. Native proof pack (UI-T4) — all rows pending

| Row | Status | What closes it |
|---|---|---|
| Platform HIG (macOS) | Pending | Native Desktop lens on the build: disclosure triangles, segmented control, field focus ring |
| Keyboard traversal | Pending | §10.4 walked on the build; no trap |
| Accessibility tree | Pending | VoiceOver trace + AX dump of the anchor, handle and unavailable states against §10.5 |
| Light / Dark / High contrast | Pending | captures in three themes; the boundary pairs of finding #10 measured on the render |
| DPI / windowing | Pending | 200 % Retina and a 100 % display; the 200 px dock |
| Large-list responsiveness | n/a | ≤ 12 rows per selection |
| OS integration, distribution trust | n/a for this surface | unchanged from `docs/proof/m12b-native/index.md` |

## 12. Sources (comparables established, not recalled)

- VS Code Settings editor — groups, "the colored bar on the left of the setting", the gear menu with reset/copy:
  <https://code.visualstudio.com/docs/configure/settings> (fetched 2026-10-01). Verified.
- Debug Adapter Protocol `VariablePresentationHint.attributes` (`readOnly`, `hasSideEffects`) and `SetVariable`:
  <https://microsoft.github.io/debug-adapter-protocol/specification> (fetched). Verified.
- Premiere Pro Effect Controls — typed values and dragging values both adjust a property; Up/Down arrow keys adjust
  values; Shift for coarse and Ctrl for fine scrub steps; "Reset Parameter"; "Toggle animation": Adobe community
  reports <https://community.adobe.com/bug-reports-728/effect-controls-values-reset-when-adjusted-using-up-down-arrow-keys-in-premiere-pro-26-5-0-1641430>
  and a course page <https://codefinity.com/courses/v2/e41f12de-1ac6-433b-9521-4652f1cb987e/5245ad60-875e-4180-b9aa-e31e942b6d46/a4e7ee82-825b-406d-96a7-653d26bb7506>.
  The official help page <https://helpx.adobe.com/premiere-pro/using/effect-controls-panel.html> returned 403.
  **Inferred** (secondary sources). Twirl-down memory is from product knowledge, **Inferred**.
- Rhino 8 Properties — "the availability of the properties is based on the selected object types"; viewport
  properties when nothing is selected: <https://docs.mcneel.com/rhino/8mac/help/en-us/commands/properties.htm>. Verified.
- Fusion 360 dimensions — "enter a mathematical expression that contains values, parameters, or a mix of both":
  <https://help.autodesk.com/cloudhelp/ENU/Fusion-Sketch/files/SKT-CREATE-DIMENSIONS.htm>. Verified.
- SolidWorks PropertyManager — not established this run; no convention here depends on it.

## 13. Ranked plan

**Must fix before M1.2b closes (Blockers and the operator's finding)**
1. **#1, #2, #3, #13 — build the property grid** (§10.1–10.3) and render every block through it. Owner: M1.2b fix
   track. Est: one track, ≈ 2 h on the M1.2b priors.
2. **#12, #4 — Tangent as a named radio group with the checked kind; the parent's kind on a handle.** Same track.
3. **#11, #10 — automation names with units; the `control-line` input border.** Same track + UXR token.
4. **#6 — per-quantity availability with the reason**, landing with D-4's fix (the D-4 root cause is separate).

**Should fix next (Majors)**
5. #15, #16 — token re-sync and literals (UXR, `Styles.axaml`).
6. #5 — COPY-153 / COPY-154 for the type change (F-5).
7. #7 — chord warning style.

**Worth doing**
- #14, #17, #18, #19 — fall out of item 1 when the grid replaces the old blocks.
- DR-UID-2 arrow stepping; `xaml-token-lint.py` checking `Spacing`/`Width` (pack-level, `/updatepack` upstream).

> **Do this one first:** item 1, the property-grid component — because it alone turns "a bunch of text" into a sheet,
> fixes units, precision, identity and fit in the same two files, and is exactly what PNL needs to reuse.

## 14. Decisions for the operator

| ID | Question | Recommendation |
|---|---|---|
| DR-UID-1 | Precision: `m12b-points.md` §11.4 says "Lengths display at 0.01 mm" and its status copy reads "MAC 101.30 mm"; spec UI-40 says derived lengths show 0.1 mm | **Follow UI-40** (the spec is the authority, and DESIGN.md §12.0e already states it): MAC "≈ 107.1 mm" in the Wing and "MAC 101.3 mm" in the status report |
| DR-UID-2 | Arrow-key stepping in number fields (Premiere-style keyboard scrub on the canvas nudge ladder) | **Adopt, as "worth doing"** — not an M1.2b exit condition |
| DR-UID-3 | Drag-to-scrub on values (Premiere) | **Reject** — imprecise at 0.01 mm, conflicts with expression entry, duplicates the canvas drag |
| DR-UID-4 | Who builds the component | **The M1.2b fix track**, with F-4; PNL then adds rows by data |

## 15. Residual risk and what this review did not cover

- No native render was measured: contrast of the as-built field boundary (#10) is token arithmetic; the AX tree and
  VoiceOver are not run. The mockup is direction evidence only (UI-T4).
- The mockup's estimates are re-derived from S and b with illustrative arithmetic; the chord reports for 152.09 and
  190 mm are quoted, not computed. The "did not converge" reason is one example value of COPY-155's `<reason>`; D-4's
  real cause is for `/investigate`.
- Fit at 1280 × 800 with the 260 px dock: every state fits except the overflow fixture. At the 200 px dock the anchor,
  chord-warning, unavailable and overflow states scroll inside the selection area (`docs/proof/property-grid-browser-check.json`).
- Browser, Rail controls and the section editor's own Properties rows were not reviewed (UI-TRANSLATION-LOSS sweep is
  Properties only).
- The native-client knowledge pack named by UI-T4 is absent from this repo (Flagged).

## 16. Deterministic control (craft gate) — a floor, not a verdict

`python3 docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/property-grid.html --markdown --a11y-obligation`:
**4 Minor, 0 Major, 0 Blocker** — all `cramped-padding` on `.frame` and `.tabs` (the window chrome, whose bars are
full-bleed by design; deviation recorded as finding #20). **0 off-token colours, fonts, sizes or radii.** The corpus was
live: a copy with one injected off-token colour (`#7b3fe4`) produced a fifth finding naming it (CD9). The detector
cannot judge archetype fit, IA, state existence or copy truth; those are §5 and the pending lenses.
Browser oracle: `node tools/check-mockup-property-grid.mjs <playwright root>` — 396 cells, 0 failing checks, 0 fit
failures at 260 px, 13/13 interactions, 0 page errors; a planted unit-less MAC turns it red (exit 1).

## 17. Defect classes registered (CI1)

| Class | Shape | Control | Status |
|---|---|---|---|
| UI-TRANSLATION-LOSS | an approved mockup's layout contract with no native assertion | mockup oracle "one label column", "every quantity carries its unit"; native tests §10.7 | controlled in the mockup; native uncontrolled until §10.7 lands |
| BLANK-ESTIMATE | a failed computation shown as a dash, blanking its neighbours | mockup oracle "no bare ≈ —" + interaction; native tests §10.7 | same |

## Status

| | |
|---|---|
| **Completed** | Elevate-mode review of the Properties pane; property-grid mockup with 22 states and harness; DESIGN.md rows, tokens and copy; browser oracle and evidence; two defect classes |
| **Remaining** | Independent UX & Accessibility and Marine-CAD review; the native build (§13 items 1–4); native proof rows (§11); spec-owner adoption of COPY-149 to COPY-157; operator rulings DR-UID-1 to DR-UID-4 |
| **Best next action** | Convene the UX & Accessibility and Marine-CAD lenses on this review and the mockup, then dispatch the M1.2b fix track with §10 as its brief |
