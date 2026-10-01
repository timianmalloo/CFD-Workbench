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
  - {to: property-grid-rulings, rel: relates-to}
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
**Reviewers:** the author; then, on `2c014a6`, the UX & Accessibility lens (BLOCK, veto held: PG-01 to PG-18,
conditions N1 to N7) and the Marine-CAD lens (pass with conditions C1 to C4: MC-1 to MC-18). **Repair cycle 1**
answers both (§18) under the operator's rulings (§14). The author does not clear the veto; the UX & Accessibility lens
re-reviews cycle 1.
**Date:** 2026-10-01.

## 1. Verdict

> **BLOCK (as built)** — the pane is a stacked document, not an inspector: no shared column, missing units, an
> invisible current tangent kind and sub-3:1 input boundaries. **PASS-WITH-CONDITIONS (proposal), pending the
> independent accessibility review.**
> **Highest-leverage change:** build one property-grid component — selection identity, property group, property row
> (label | value | unit on one shared column, with kinds and states) — and render every Properties block through it.
> One change in two files (plus tokens) clears F-1, O-4, O-6 and the F-4 display half, keeps the Wing estimates always in view, and it is the
> component PNL reuses.

| | As built | Proposal (author's measure) |
|---|---|---|
| Blockers (sev 4, or accessibility ≥ 3) | 3 (#10, #11, #12) | cycle 0: 2 found by the lens (PG-01, PG-02); cycle 1: both addressed, re-review pending |
| Majors (sev 3) | 9 | cycle 0: PG-03..PG-11, MC-1..MC-10; cycle 1: mockup-side items done, native-only items carried in §10.10 (§18) |
| Minors (sev 2) | 6 | 1 (#20, chrome deviation) |
| Nits (sev 1) | 1 | 0 |

**Accessibility veto:** **held by the UX & Accessibility lens (cycle 0); re-review pending on cycle 1.** The lens's
design-stage predicate:
1. PG-01 is closed: every fact and estimate speaks its unit, and the oracle checks the accessible text.
2. PG-02 is closed: the Kind list is vertical, and the oracle checks clipping of options and labels.
3. PG-04, PG-06 and PG-07 are ruled in the brief.

Cycle 1 addresses all three (§18). The native veto clears only on the evidence listed in §10.10 (N1–N7).

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
| **Adobe Premiere Pro Effect Controls** | Twirl-down groups whose open/closed state is remembered; one aligned label column with values to the right; a header summary when collapsed; arrow-key stepping in a value with modifier steps — **ruled DR-UID-2: adopted for point and handle fields only, as the canvas Nudging gesture** (MC-10: one undo row on release, Esc cancels; MC-18: ⌘ / Ctrl) | **Scrubbable values (drag on the number), ruled DR-UID-3 rejected:** a 1 px drag has no honest mapping to a 0.01 mm field, it fights text selection in an expression field, and the canvas already gives direct manipulation (TQ6); precision CAD fields do not scrub (Marine-CAD, KB-01). Keyframe stopwatches: no time axis. Reset-parameter buttons: no default, Undo is the reset |
| **CAD property sheets (Fusion 360 dimension entry; Rhino Properties)** | Expressions with units and parameter names in a dimension field, echoed in the field's unit (`15 cm = 150.00 mm.`, `0.35 rad = 20.05°.`); the panel follows the selection and falls back to the document (Foil) when nothing is selected; type shown read-only for named objects; curve facts (degree, point count, ceiling) as read-only rows (MC-16) | Onshape-style *associative* variables: the `#` reference looks live but is evaluated once, so the echo says "set once" (MC-3, ruled). Boxed group borders on every section (the "everything in a card" tell); a modal PropertyManager with OK/Cancel — every commit here is already one undo step | Boxed group borders on every section (the "everything in a card" tell); a modal PropertyManager with OK/Cancel — every commit here is already one undo step |
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
| Kind descriptions | COPY-150 / 151 / 152 | Handles stay in line. Their lengths can differ. · Handles stay in line and equal in length. · Each handle moves on its own. The curve can turn a corner here. |
| Type option (anchor) | COPY-153 | Anchor point — adds handles (rail gains up to 3 points) |
| Type change report, to anchor | COPY-154 | <Curve> point <i> is now an anchor point with 2 handles. The rail gained <k> points (<m> → <n>). Largest change <d> mm. |
| Type change report, to control | COPY-165 | <Curve> point <i> is now a control point. Its handles are removed; the rail has <n> points (was <m>). Largest change <d> mm. |
| Estimates unavailable (group note) | COPY-155 | Unavailable — <reason>. Undo, or edit again, to recompute. |
| Availability in the status line | COPY-160 | <What> unavailable — <reason>. |
| Root-mirror handle | COPY-156 | Square to the centre line (root mirror). Only its length can change. |
| Expression echo | COPY-157 | <typed> = <value> <unit>. |
| Expression with a reference | COPY-161 | <typed> = <value> <unit> (set once; doesn’t follow <reference>) |
| Angle out of range | COPY-158 | Enter an angle between −90° and 90°, from the span axis, + aft. <Field> is unchanged. |
| Root-chord authority (TE root Aft) | COPY-159 | This is the root chord. Typing here moves only this point; Root chord under Wing rescales the planform. |
| Handle under its parent's kind | COPY-162 | Keeps this handle; the other one moves. |
| Field nudge help | COPY-163 | Up and Down arrows step 0.1 <unit>; with Command (Ctrl on Windows) 0.01; with Shift 1. Release to apply; Esc cancels. |
| Angle reference | COPY-164 | Angles are measured from the span axis, + aft. |
| Pending Type | COPY-166 | Press Return to change the type, or Esc to keep it. |
| Pending Kind | COPY-167 | Press Return or Space to make it <kind>, or Esc to keep <kind>. |
| Field errors, tip closes, section editor, empty, pane error | COPY-106, 118, 108, 122, 136, 137, 138 | as written in DESIGN.md §7 |
| Chord report / warning, not checked, recovered, busy, tangent change, locks, twist clamp, t/c range | `docs/design/m12b-points.md` §11.4, `m12b2-3d-elevations.md` §11.4 | quoted there; the mockup uses the 152.09 and 190 mm strings verbatim |

COPY-149 to COPY-167 are proposals for the spec owner (the spec's C3 table is the authority). Counts, deltas and
reasons are substituted from the operation's own result, never typed as constants. Cycle 1 changed COPY-152, 153,
154 and 157 (MC-15, MC-9, MC-4); the cycle-0 strings are superseded.

## 10. Implementation brief (for the dedicated property-grid track, then M1.2b2 PNL)

*Revised in repair cycle 1 against the UX & Accessibility review (PG-01 to PG-18, N1 to N7) and the Marine-CAD review
(MC-1 to MC-18), under the rulings in [`docs/notes/property-grid-rulings.md`](../notes/property-grid-rulings.md).*

### 10.1 The model (`PropertiesView.cs`) — one row type, filled per selection

Today `PropertyRow` has `Label, Value, IsEditable, Unit, HelperText` and two kinds. Proposed shape (the names are a
suggestion; the contract is the fields):

| Type | Fields |
|---|---|
| `PropertiesModel` | `Identity` (`Glyph` {Foil, Control, Anchor, End, Handle, Several, Station}, `Title`, `Crumb?`, `CrumbTarget?`), `Groups`, `Wing?`, `Banner?` (AlertBand, PG-14), `EmptyState?` |
| `PropertyGroup` | `Id` (stable, for remembered collapse), `Title`, `Summary` (formatted values, shown collapsed), `Collapsible` (Wing: false), `Chip?` {Preview, Checking, Unavailable}, `Rows`, `Notes` (text + kind) |
| `PropertyRow` | `Key` (stable: `p:from`, `p:aft`, `h:angle`, `w:root`…), `Label`, `Kind` {Input, Fact, Estimate, Choice, KindList, Action}, `Value` (pre-formatted by §10.2), `Unit` (`null` only with `Dimensionless = true`), `Family` {Length, Angle, Percent, None}, `State` {Normal, Warning, Error, Unavailable, Mixed, Locked}, `Description?`, `Message?` (text + kind {Report, Echo, Warning, Error, Reason, Info}), `AutomationName` (inputs) or `SpokenText` (facts and estimates, §10.5), `Options?`, `Nudge` (true for point and handle fields only) |

**Per-quantity availability.** Each estimate row carries its own `State`. One non-finite MAC makes MAC Unavailable,
not Mean chord (mockup state *partial-unavailable*). The group note says which quantity is unavailable and why
(COPY-155). DESIGN.md §12.0f now says this too; the cycle-0 wording "every row reads Unavailable" applied only when all
of them fail.

**Per-curve row sets (MC-17).** `PropertiesView.Build` reads one table. Rows are data, never layout:

| Curve | Position rows | Value row (unit, family) | Handle rows on an anchor | Handle selection rows | Clamp / range |
|---|---|---|---|---|---|
| Leading / Trailing edge | From root (mm) · η (fact) | Aft (mm, Length) | Kind; Smooth: Angle + To root + To tip; Symmetric: Angle + Length; Corner: per handle Angle + Length | Angle (°) + Length (mm); the parent anchor's Kind | angle in (−90°, 90°) from the span axis, + aft (COPY-158) |
| Dihedral | From root · η | Height (mm, Length) | as the rails, the angle labelled **"Dihedral angle"** | Dihedral angle + Length | — |
| Twist | From root · η | Twist (°, Angle) | per handle From root + Twist | From root + Twist | ±57.30° (`Geometry.TwistDomainDegrees`, probe formatter): state **Clamped** = warning rail + the m12b2 copy |
| Thickness | From root · η | t/c (%, Percent, shown 0.01 %) | per handle From root + t/c | From root + t/c | 0 % < t/c < 100 % |
| Named points | Type fact + lock; root/tip ends: From root locked | per curve | root-mirror handle: Angle locked 0.00°, Length only (COPY-156); tip handle free | — | — |

Every curve also gets a collapsed group titled with the curve name ("Trailing edge"). Its summary reads
"cubic · 14 points (16 max)" (MC-16) and its rows are Degree and Points. It is the home of the ceiling copy "14 of 16".

### 10.2 Entry grammar and one formatter per quantity (MC-4, DR-UID-1)

**Entry.** Every numeric field takes `value [unit]` and expressions (+ − × ÷ and parentheses). A bare number is in the
field's own unit. The parse column depends on the unit family:

| Family | Fields | Units accepted | References | Echo (COPY-157) |
|---|---|---|---|---|
| Length | From root, Aft, Height, chords, Span, lengths | `mm` · `cm` · `m` | `#span`, `#root_chord`, `#tip_chord` (the `#` sigil stays, MC-3) | "15 cm = 150.00 mm." · with a reference: "#root_chord × 0.1 = 12.67 mm (set once; doesn't follow Root chord)" (COPY-161) |
| Angle | handle angle, Dihedral angle, Twist | `°` · `deg` · `rad` | none | "0.35 rad = 20.05°." |
| Percent | t/c | `%` | none | "0.12 × 100 = 12.00 %." — a chord fraction is entered as an expression; a bare 0.12 is 0.12 %, never silently reinterpreted (class UI-R) |

Refusals: a non-number → COPY-118. A length ≤ 0 where a length must be positive → COPY-106. An angle outside its
domain → COPY-158 (rails) or the twist clamp copy. t/c outside (0, 100) % → the m12b2 copy. The geometry stays
unchanged, Escape restores the shown value, and the error is an alert once (PG-18).

**Display (precision follows the quantity, DR-UID-1):**

| Quantity | Shown | Unit | Example |
|---|---|---|---|
| Typed or placed length (From root, Aft, Height, Root/Tip chord, Wing Span, handle length; a station chord at the root or tip) | 0.01 | mm | 1000.00 · 12.67 |
| Derived length (Mean chord, MAC; a station chord between the ends) | 0.1, "≈ " | mm | ≈ 107.1 |
| Placed or typed angle (handle, Dihedral angle, Twist) | 0.01 | ° | −21.08° |
| Derived angle | 0.1 | ° | — |
| Placed t/c (Thickness channel point) | 0.01 | % | 12.35 |
| Max t/c | 0.1, "≈ " | % | ≈ 12.0 |
| AR | 0.00, "≈ " | `b²/S` in the unit column (MC-8) | ≈ 10.04 |
| Area | 1, "≈ " | cm² | ≈ 996 |
| η | 0.000 | dimensionless | 0.643 |
| Δ, largest change | 0.01 | mm (° / %) | Δ +1.00 mm |

Negative numbers use U+2212 in read-only text. Fields accept both minus signs.

### 10.3 Avalonia control mapping

Verified present in `Avalonia.Controls` 11.3.14 (`~/.nuget/packages/avalonia/11.3.14`): `Expander`, `ToggleButton`,
`RadioButton`, `NumericUpDown`, `HeaderedContentControl`, `Grid.IsSharedSizeScope`, `ColumnDefinition.SharedSizeGroup`.
The UX & Accessibility lens confirmed the peer types `ExpanderAutomationPeer`, `RadioButtonAutomationPeer`,
`TextBlockAutomationPeer` and `ComboBoxAutomationPeer` in the same binary.

| Part | Control | Notes |
|---|---|---|
| Pane | `DockPanel`: Wing `Border` docked Bottom, `MaxHeight` = 55 % of the pane (bound in code on `Bounds`); selection `ScrollViewer` fills | the Wing never scrolls away (UI-36) |
| Identity | `Grid` 16 px glyph `Path` (geometry shared with `PlanCanvas`) + `TextBlock` 14 semibold + optional crumb `HyperlinkButton` | one per model |
| Group | `Expander` restyled: header = chevron + title + summary (summary shown when collapsed); `IsExpanded` two-way to a per-`Id` dictionary owned by the pane | **PG-10:** set `AutomationProperties.Name` = title on the Expander **and** on its focusable header ToggleButton, `HelpText` = summary; test that the focused part exposes name and expanded state. **PG-16:** `ContentTransition` = null, no chevron animation |
| Wing group | `HeaderedContentControl` (not collapsible) with the chip in the header | — |
| Row | `Grid ColumnDefinitions="{PropLabelWidth},*,{PropUnitWidth}"`, fixed token widths switched to the narrow pair by a `narrow` class under 230 px | the label `TextBlock` has `TextWrapping=Wrap` and **no** `TextTrimming` (PG-03); `Border BorderThickness="3,0,0,0"` = the state rail, brush by state class |
| Input | `TextBox`, `TextAlignment=Right`, mono font, height 28 in a 32 px row, border `ControlLineBrush` | not `NumericUpDown`: its spinners cost width at 200 px and it cannot take expressions |
| Fact / Estimate | a row container (`Grid`) **named** with `AutomationProperties.Name` = the spoken line of §10.5; its child TextBlocks `AutomationProperties.AccessibilityView=Raw` (PG-01). The value is a `SelectableTextBlock` that is Tab-reachable when the row is a fact, so ⌘C/Ctrl+C copies it (PG-15) | never a disabled `TextBox` |
| Choice (Type) | `ComboBox` that commits only on `DropDownClosed` with a changed value, or on Return; arrow keys while it is closed set a *pending* value with the info line "Press Return to change the type, or Esc to keep it." (PG-07) | — |
| Kind (Tangent) | a **vertical** list of three native `RadioButton`s, one per line, 26 px, no trimming (PG-02); the container is `ControlTypeOverride=Group` with Name "Tangent kind" (on a handle: "Tangent kind of anchor point 7") and each option carries `PositionInSet`/`SizeOfSet` (PG-10) | **Ruled APG deviation (PG-06 = MC-1):** arrows move the check only and do not wrap; Return or Space, or leaving the group, commits one undo row; Esc restores the committed kind. Tab lands on the checked option (implement and test; not built into Avalonia, Inferred) |
| Action | `Button` / `HyperlinkButton` | — |
| Message | `TextBlock` with icon, full row width. An error sets `AutomationProperties.HelpText` on the field **and** is announced once through `LiveSetting=Assertive`; reports and availability changes go to the polite status line | **PG-09:** if VoiceOver does not speak `AXLiveRegion` in the native app (unproven; the attach is blocked), fall back to moving focus to the message or to native announcement interop. PG-18: announce a given error once, not on every re-render |
| Banner | the product's AlertBand pattern for the recovered edit and not-checked banners: role alert, focus to the first action (PG-14); the Discard boundary uses `MutedBrush` on the band (5.34:1, PG-13) | — |
| Focus visual | inset focus ring (PG-05) on TextBox, RadioButton, ComboBox and the Expander header, specified in the pseudo-class selectors of all three theme variants (PG-11) | the high-contrast variant inherits Light today (`Program.cs`:40); every Fluent pseudo-class brush (`:focus`, `:pointerover`, `:checked`) is overridden explicitly |

### 10.4 Keyboard map (both OS columns, MC-18)

| Key — macOS | Key — Windows | In | Does |
|---|---|---|---|
| Tab / ⇧Tab | Tab / Shift+Tab | pane | next / previous stop: group headers, inputs, the Type box, the Kind group (one stop, lands on the checked option), the crumb link |
| Return | Enter | input | commit (one undo row); focus stays |
| leaving the field | leaving the field | input | commit; focus goes where it was sent (UI-39) |
| Esc | Esc | input | restore the shown value; a second Esc returns focus to the canvas target |
| ↑ / ↓ · ⌘ ↑/↓ 0.01 · plain 0.1 · ⇧ 1 | ↑ / ↓ · Ctrl ↑/↓ 0.01 · plain 0.1 · Shift 1 | **point and handle** fields only (never Wing) | the field run IS the canvas Nudging gesture (MC-10): begin on keydown, Δ on the model value from the per-curve ladder (mm / ° / %), "≈ preview" chip, one undo row and the new value announced on KeyUp (PG-08), Esc while held cancels with no row, ignored while the field text is dirty. Its HelpText (COPY-163) names the steps. **Caret conflict, for the Native Desktop lens:** ⌘↑/⌘↓ move the caret to the start/end and ⇧↑/⇧↓ extend a selection in a macOS single-line field; Ctrl+↑/↓ is free on Windows. An AT pass is required before close (PG-08) |
| ↑ ↓ ← → Home End | the same | Kind group | move the check only, no wrap (ruled deviation); Return/Space or leaving commits; Esc keeps the committed kind |
| ↑ ↓ on the closed box | the same | Type | pending only; Return commits; Esc keeps (PG-07) |
| Space / Return | Space / Enter | group header | collapse / expand |
| Esc | Esc | a handle selection, outside a field | select its anchor or end point; the status says "Selected …" (PG-12) |
| F6 | F6 | anywhere | next region (Plan ↔ Properties), existing |

### 10.5 Accessible names and spoken text

Every name contains its visible label (SC 2.5.3). Input names carry the unit; the visual unit is hidden from AT only
on input rows (PG-01).

| Row | Name / spoken text | Help text |
|---|---|---|
| From root (point) | From root, position along the span in millimetres | COPY-163 (nudge steps) + the message, if any |
| Aft | Aft position in millimetres | COPY-163; on the TE root end also COPY-159 |
| Type | Type | the description (COPY-117 / COPY-149) |
| Kind group | Tangent kind · on a handle: Tangent kind of anchor point 7 | the kind's description (COPY-150 to COPY-152) |
| Angle (Smooth/Symmetric) | Angle, both handles, in degrees | COPY-163 |
| To root / To tip | To root, handle length in millimetres · To tip, handle length in millimetres | COPY-163 |
| Corner handle rows | Angle of the handle toward the root, in degrees · Length of the handle toward the root, in millimetres (and toward the tip) | COPY-163 |
| Handle selection | Angle in degrees, from the span axis, positive aft · Length in millimetres | COPY-163 |
| Twist / t/c / Height | Twist in degrees · t/c in percent of chord · Height in millimetres | COPY-163 |
| Wing | Span in millimetres · Root chord in millimetres · Tip chord in millimetres | the chord report or error |
| **Fact row (new, PG-01)** | the container is named "<label>, <value> <unit spoken>[, locked]": "η, 0.643" · "From root, 0.00 millimetres, locked" · "Chord, 12.67 millimetres" · "From root, mixed" | the group's reason note |
| **Estimate row** | "<label>, approximately <value> <unit spoken>": "MAC, approximately 107.1 millimetres" · "AR, approximately 10.04 b squared over S" · "MAC, unavailable" | the group's reason |
| Group header | the title; collapsed/expanded from the pattern | the summary when collapsed |

### 10.6 Tokens (`Styles.axaml`, owned by UXR)

Add `PropLabelWidth` 88, `PropLabelNarrowWidth` 56, `PropUnitWidth` 32, `PropUnitNarrowWidth` 28, `PropHeadHeight` 24,
`PropRowReadOnlyHeight` 24, `ControlHeightDense` 28. Add the brushes `ControlLineBrush` (`control-line` / `dark-control` /
`contrast-ink`), `WarningBrush` (`warning` / `dark-warning` / `contrast-primary`), `SelectionBrush` and `FocusRingBrush`,
each set in the pseudo-class selectors of all three variants (PG-11). Re-sync the nine dark colours and the
high-contrast accent to DESIGN.md (finding #15). Replace the 17 literals (finding #16).

### 10.7 Named tests (red first, fast ring, headless) — the build track's list

| Test | Protects |
|---|---|
| `PropertiesView_EveryQuantityRow_HasUnitOrIsDimensionless` | O-4 |
| `PropertiesView_Formatter_PrecisionFollowsQuantity` (table §10.2) | UI-40, DR-UID-1 |
| `PropertiesView_AngleField_AcceptsDegreeExpression` (`−21°`, `3 deg`, `0.35 rad`, `1+2`) | MC-4 |
| `PropertiesView_ReferenceExpression_EchoSaysSetOnce` | MC-3 |
| `PropertiesView_HandleSelection_OwnIdentity_ParentTangentRow` | O-6, F-4 |
| `PropertiesView_RowSet_FollowsCurveTable` (rails angle + length; twist and t/c From root + value) | MC-17 |
| `PropertiesView_OneNonFiniteEstimate_OthersStillShown` | BLANK-ESTIMATE |
| `PropertiesPane_EstimatesUnavailable_ShowReasonNeverDash` | D-4 symptom |
| `PropertiesPane_Rows_ShareOneLabelColumn` | F-1, UI-TRANSLATION-LOSS |
| `PropertiesPane_Wing_AlwaysFullyVisibleAt1280x800` (260 px dock) | UI-36 (see DR-UID-5) |
| `PropertiesPane_NumberFields_AutomationNameHasLabelAndUnit` | finding #11 |
| `PropertiesPane_Tangent_CheckedKindExposed` | finding #12 |
| `PropertiesPane_FieldBlur_CommitsAndKeepsFocusTarget` | UI-39, UI-C |
| `PropertiesPane_KindArrows_MoveCheckOnly_OneUndoRowPerIntent` | PG-06, MC-1 |
| `PropertiesPane_FieldNudge_EscCancelsNoRow_KeyUpOneRow` | MC-10 |
| `PropertiesPane_FieldNudge_IgnoredWhileTextDirty` | MC-10 |
| `PropertiesPane_TeRootAft_DescribesRootChordAuthority` | MC-2 |

**UX & Accessibility N1, verbatim:** `Fact_And_Estimate_NameHasUnit`, `TypeCombo_ArrowWhileClosed_DoesNotCommit`,
`Tangent_KindChange_KeepsFocusOnChecked`, `Unavailable_AnnouncedInStatus`,
`Expander_FocusedHeader_ExposesNameAndExpandedState`. Each must be red first.

### 10.8 Who builds what (DR-UID-4, ruled)

A **dedicated property-grid track** builds the component (`PropertiesPane.axaml`/`.cs`, the row model in
`PropertiesView.cs`, the §10.6 tokens in `Styles.axaml` together with UXR). It runs **after the M1.2b fix track joins
and before M1.2b2 PNL dispatches**. PNL then only adds channel rows by data through `PropertiesView.Build` (§10.1 curve
table).

### 10.9 Cross-surface item: "From root" (MC-6, ruled)

The rename from "Span" (a point's spanwise coordinate) to **"From root"**, with η beside it, must reach every surface
that names that coordinate. "Span" stays only for the wing span b. The build track owns the list:

| Surface | Today | After |
|---|---|---|
| Properties (this grid) | "Span" | "From root" + η fact (done in the mockup) |
| Tracing probe | "span 206.00 mm" (m12b §0.1 step 2) | "from root 206.00 mm" (η 0.412 stays) |
| Point tooltip and `PlanPointPeer` name | "…, span 180.00 mm, aft 118.52 mm" (m12b §11.4) | "…, from root 180.00 mm, aft 118.52 mm" |
| Handle name, M1.2b2 channel names | "span 200.00 mm" (m12b2 §11.4) | "from root 200.00 mm" |
| Points grid (M1.2c) | Span column | From root column |
| Browser rail rows | span in the row label, if shown | from root |
| Copy rows and the spec | m12b §11.4 hover strings, m12b2 §11.4 strings, spec CAD-15/UI-37 point names | spec owner amends; DESIGN.md row text follows |

### 10.10 Native acceptance list (UX & Accessibility N1–N7; not provable in the mockup)

- **N1** Land §10.7, including the five N1 tests verbatim; each red first.
- **N2** A VoiceOver trace and an AX dump (macOS) of: anchor, handle, unavailable, field error, chord warning, type change and a collapsed group. Each reads as §10.5. Every announcement is spoken; if AXLiveRegion is silent, apply the PG-09 fallback.
- **N3** Walk §10.4 on the build with no trap: Tab into Kind lands on the checked radio; arrows behave as ruled; a second Escape reaches the canvas; F6 cycles regions.
- **N4** Measure the render in light, dark and high contrast: the input boundary (only 0.43 above 3:1 in light), the focus visual on TextBox, RadioButton, ComboBox and the Expander header, and the checked Kind. Include the Fluent pseudo-class states (PG-11). Re-check at 100 % and 200 % scale.
- **N5** No truncated label or Kind option at the 200 px dock on the native build.
- **N6** No native motion, or motion gated on the OS setting (PG-16).
- **N7** Windows/Narrator stays an open row; this clearing covers macOS only until a Narrator/UIA pass exists.

## 11. Native proof pack (UI-T4) — all rows pending

| Row | Status | What closes it |
|---|---|---|
| Platform HIG (macOS) | Pending | Native Desktop lens on the build: disclosure triangles, the vertical Kind radio list, field focus ring |
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

**Must fix before M1.2b2 PNL (dedicated property-grid track, DR-UID-4)**
1. **#1, #2, #3, #13: build the property grid** (§10.1–10.3) and render every block through it. Owner: the dedicated
   track, after the M1.2b fix track joins. Est: one track, ≈ 2–3 h on the M1.2b priors.
2. **#12, #4: the Kind as a vertical, named radio list** with the ruled arrow semantics, and the parent's kind on a
   handle. Same track.
3. **#11, #10, PG-01: names and spoken text with units on every row; the `control-line` input border.** Same track
   plus the UXR tokens.
4. **#6: per-quantity availability with the reason, announced (PG-04)**, landing with D-4's fix (D-4's root cause is a
   separate investigation).
5. **MC-2, MC-3, MC-4, MC-6: authority copy, set-once echo, unit-aware entry, "From root".** Same track; the
   cross-surface part of MC-6 follows §10.9.

**Should fix next (Majors)**
6. #15, #16: token re-sync and literals (UXR, `Styles.axaml`).
7. #5 / MC-9: the type-change copy (COPY-153 / COPY-154). The role of each added vertex waits on the Computational
   Geometry lens.
8. #7: the chord warning style.
9. DR-UID-2: the field nudge as the Nudging gesture (adopted; conditions MC-10, MC-18, PG-08).

**Worth doing**
- #14, #17, #18, #19 fall out of item 1 when the grid replaces the old blocks.
- `xaml-token-lint.py` should also check `Spacing`/`Width` (pack-level, `/updatepack` upstream).

> **Do this one first:** item 1, the property-grid component. It alone turns "a bunch of text" into a sheet. It
> fixes units, precision and identity in the same two files, and it is exactly what PNL reuses.

## 14. Decisions — operator rulings (2026-10-01)

Recorded in [`docs/notes/property-grid-rulings.md`](../notes/property-grid-rulings.md).

| ID | Ruling |
|---|---|
| DR-UID-1 | **Follow UI-40 with the Marine-CAD amendments.** Precision follows the quantity (§10.2). Status reads "MAC 101.3 mm". The `m12b-points.md` §11.4 sentence and the angle text of spec UI-40 need spec-owner amendment (flagged review-suggested, not edited) |
| DR-UID-2 | **Adopted** for point and handle fields only, under MC-10, MC-18 and PG-08 (§10.4) |
| DR-UID-3 | **Rejected** (drag-to-scrub) |
| DR-UID-4 | **A dedicated track** builds the component after the M1.2b fix track joins and before PNL |
| MC-2 | Both root-chord fields stay editable and are labelled: COPY-159 under the TE root Aft |
| MC-3 | Expressions are set once and say so (COPY-161); the `#` sigil stays |
| MC-6 | "From root" with η; "Span" means only b; cross-surface list §10.9 |
| PG-06 = MC-1 | Recorded APG deviation: Kind arrows move the check only; Return/Space or leaving commits; no wrap |

**Still open — DR-UID-5 (new, for the operator).** With the reviewers' row set in place, the selection area no longer
fits beside the Wing at 1280 × 800 on the 260 px dock in six states. The additions are the η fact, the angle-reference
line, the vertical Kind list and the curve group. Measured selection heights against the room available: anchor
534/401 px, handle 407/401, TE root end 434/401, unavailable 407/345, overflow 560/401, twist anchor 630/401.
Spec UI-36 asks for both to fit without scrolling. **Recommendation:** amend UI-36 to "the Wing block is always fully
visible; the selection area may scroll". The operator's ask was a running estimate that is *always shown*, and the
oracle now gates exactly that: the Wing is fully visible in every state at the 260 and 300 px docks. The alternative
is to cut rows the reviewers required. Until this is ruled, the oracle records the selection fit and does not gate it.

## 15. Residual risk and what this review did not cover

- No native render was measured. The as-built field boundary (#10) is token arithmetic, and the AX tree and VoiceOver
  were not run. The mockup is direction evidence only (UI-T4); §10.10 lists what only the native build can prove.
- The mockup's estimates are re-derived from S and b with illustrative arithmetic. The chord reports for 152.09 and
  190 mm are quoted, not computed. "did not converge" is one example value of COPY-155's `<reason>`; D-4's real cause
  is for `/investigate`.
- The type-change report counts the rail (10 → 13) from the operation's result. Naming each added vertex's role
  (MC-9) waits on the Computational Geometry lens, so the copy does not name them yet.
- Selection fit at 1280 × 800 (`docs/proof/property-grid-browser-check.json`, `selectionFitAt1280x800`): six states
  scroll at 260 px (DR-UID-5). At 200 px the Wing itself scrolls in the chord-warning, unavailable,
  partial-unavailable and section-editor states.
- Not reviewed: Browser, Rail controls, and the section editor's own point rows (the UI-TRANSLATION-LOSS sweep covers
  Properties only). The Dihedral row set is specified in §10.1 but not mocked.
- The native-client knowledge pack named by UI-T4 is absent from this repo (Flagged).

## 16. Deterministic control (craft gate) — a floor, not a verdict

`python3 docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/property-grid.html --markdown --a11y-obligation`
(repair cycle 1): **4 Minor, 0 Major, 0 Blocker.** All four are `cramped-padding` on `.frame` and `.tabs`, the window
chrome, whose bars are full-bleed by design (deviation recorded as finding #20). **0 off-token colours, fonts, sizes
or radii.** In cycle 0 an injected off-token colour produced a fifth finding (CD9), so the gate scans a live corpus.
The detector cannot judge archetype fit, IA, state existence or copy truth.

**Browser oracle (cycle 1):** `node tools/check-mockup-property-grid.mjs <playwright root>` — 30 states × 3 themes ×
3 widths × 2 windows = 540 cells, 0 failing checks, 30/30 interactions, 0 page errors.
- **Planted PG-01** (the spoken line removed from fact and estimate rows) turns it red: "every quantity's accessible
  text carries its unit" fails, and the interaction "a fact or estimate speaks its unit" fails.
- **Planted PG-02** (the Kind list put back as a horizontal, ellipsized row) turns it red: "nothing clipped" and "no
  label or option is ellipsized" both fail.
- Both plants exit 1.

## 17. Defect classes registered (CI1)

| Class | Shape | Control | Status |
|---|---|---|---|
| UI-TRANSLATION-LOSS | an approved mockup's layout contract with no native assertion | mockup oracle "one label column", "every quantity carries its unit"; native tests §10.7 | controlled in the mockup; native uncontrolled until §10.7 lands |
| BLANK-ESTIMATE | a failed computation shown as a dash, blanking its neighbours | mockup oracle "no bare ≈ —" + interactions (unavailable, partial-unavailable); native tests §10.7 | same |

## 18. Repair cycle 1 ledger (UX & Accessibility PG-*, Marine-CAD MC-*)

| Item | Disposition | Where |
|---|---|---|
| PG-01 Blocker: units hidden on facts and estimates | **Done** — a spoken line per fact or estimate row; unit hidden only on inputs; oracle check on the accessible text; planted failure red | mockup `row()`, §10.3, §10.5 |
| PG-02 Blocker: Kind truncates | **Done** — vertical radio list, one per line; oracle clipping covers options and labels; planted failure red | mockup `.radios`, §10.3 |
| PG-03 label ellipsis | **Done** — labels wrap, never ellipsize; oracle checks `.lbl` | mockup CSS, §10.3 |
| PG-04 Unavailable announced | **Done** — status "Estimates unavailable — did not converge." (COPY-160) | mockup, §10.3 |
| PG-05 inset focus ring | **Done in the mockup** (Kind options); native in §10.3 / N4 | — |
| PG-06 = MC-1 Kind arrows | **Done** — move the check only, no wrap, commit on Return/Space or leaving; recorded deviation | mockup, §10.3, §10.4, rulings note |
| PG-07 Type combo | **Done** — arrows on the closed box are pending; Return or a pointer pick commits | mockup, §10.3 |
| PG-08 field nudge AT | **Done in the mockup** — HelpText (COPY-163), value announced on release; AT pass deferred to N2 (native) | — |
| PG-09, PG-10, PG-11, PG-15, PG-16 | **Deferred (native-only)** — specified in §10.3 and the N list | §10.3, §10.10 |
| PG-12 selection announced | **Done** — "Selected …" | mockup |
| PG-13 Discard boundary | **Done** — ink-mute on the band, audited (5.34:1) | mockup CSS |
| PG-14 recovery banner | **Done** — role alert, focus to Apply | mockup |
| PG-17 ✓ in the name; landmarks | **Done** — check mark replaced by an aria-hidden dot; groups are divs | mockup |
| PG-18 alert re-announced | **Done** — an error is an alert once per text | mockup `msgHtml` |
| N1–N7 | **Folded in** as the build track's acceptance list, N1 test names verbatim | §10.7, §10.10 |
| MC-2 root chord authority | **Done** (COPY-159) | mockup *root-te*, §10.1 |
| MC-3 set-once expressions | **Done** (COPY-161) | mockup, §10.2 |
| MC-4 unit-aware entry | **Done** — a grammar per family; `°`/`deg`/`rad`, `%`; echo in the field's unit; named test | mockup `parseQty`, §10.2, §10.7 |
| MC-5 precision per quantity | **Done** per DR-UID-1 (station chord 0.01 mm; t/c point 0.01 %) | mockup, §10.2, DESIGN §12.0f |
| MC-6 "From root" | **Done** in the grid; cross-surface list for the build | mockup, §10.9 |
| MC-7 Mean chord (geometric) | **Done** | mockup definitions, DESIGN |
| MC-8 AR with b²/S | **Done** | mockup, §10.2 |
| MC-9 type-change copy | **Partly done** — the option says "adds handles (rail gains up to 3 points)" and the report counts the rail from the result. **Deferred:** naming each added vertex's role waits on the Computational Geometry lens | COPY-153, COPY-154 |
| MC-10 nudge as the Nudging gesture | **Done** in the mockup and the oracle (preview, Esc no row, KeyUp one row, dirty ignored) | mockup, §10.4 |
| MC-11 kind-change copy from the result | **Done** — on a handle the report names the kept handle; COPY-162 under the parent | mockup |
| MC-12 states | **Done** — tip end, tip handle, root-mirror handle, TE root handle length, partial-unavailable; §12.0f reconciled with per-quantity availability | mockup |
| MC-13 angle reference and range | **Done** — reference line per group, COPY-158 refusal, overflow fixture −84.95° | mockup |
| MC-14 largest change on Make control | **Done** | COPY-154 / the control report |
| MC-15 "can turn a corner" | **Done** | COPY-152 |
| MC-16 curve facts | **Done** — collapsed curve group "cubic · N points (16 max)" | mockup |
| MC-17 per-curve rows, twist anchor/handle, t/c 0.01 %, Clamped | **Done** in the mockup; Dihedral specified in §10.1, not mocked | mockup, §10.1 |
| MC-18 both OS columns | **Done**; caret conflicts handed to the Native Desktop lens | §10.4 |
| N-1 Mixed without a unit · N-2 "Handle length" · N-3 "Kind" | **Done** | mockup |
| UI-36 fit with the new rows | **Open — DR-UID-5** (operator) | §14 |

## Status

| | |
|---|---|
| **Completed** | Elevate-mode review of the Properties pane; repair cycle 1 against both adversarial reviews: mockup (30 states), oracle (540 cells, 30 interactions, two planted failures red), DESIGN.md §12.0f and copy rows, brief §10, rulings note |
| **Remaining** | UX & Accessibility re-review for clearing the design-stage veto; MC-9 vertex roles (Computational Geometry); DR-UID-5 (operator); spec-owner amendments (UI-40 angle text, m12b §11.4 sentence, A4.8 expression semantics, COPY-149..163, "From root" in CAD-15/UI-37); the native build and §10.10 |
| **Best next action** | Re-convene the UX & Accessibility lens on cycle 1 to clear the design-stage veto, rule DR-UID-5, then dispatch the dedicated property-grid track with §10 as its brief |
