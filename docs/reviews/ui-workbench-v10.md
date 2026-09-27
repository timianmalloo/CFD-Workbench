---
id: review-ui-workbench-v10
title: UI review — v10 first run, focus-safe floats, point types, catalog and Wing block (elevate)
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, accessibility, properties, catalog, point-types, estimates, first-run, docking]
links:
  - {to: mockup-workbench-v10, rel: documents}
  - {to: review-ui-workbench-v9, rel: refines}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-12-26
summary: >-
  Elevated v9 after measuring it: restored the first-run and loading states, one precision per quantity, Properties
  readable at 200 px, and floats that move clear of a focused target (option a, operator-confirmed). Folded in the
  operator's four requests plus the chord, MAC and typed-dimension decisions. One repair cycle; the accessibility veto
  cleared by the lens, the Simplifier's veto cleared, 15 of 15 oracle gates green.
---

# UI review — v10 (elevate)

**Mode:** elevate (v9 → v10). **Artifact:** [`docs/mockups/workbench-v10.html`](../mockups/workbench-v10.html).
**Oracle:** `tools/check-mockup-v10.mjs` → [`docs/proof/workbench-v10-browser-check.json`](../proof/workbench-v10-browser-check.json);
craft gate → [`docs/proof/ui-craft-findings-v10.json`](../proof/ui-craft-findings-v10.json).

## 1. Operator direction (verbatim)

Standing bar: "a CAD experience … Fusion 360 and Shape3D … Clean, simple… focused on the model"; "everything 'is a curve
or a point' … handles … the end point of the le or te"; "properties … in a 'properties pane' … docked in a side panel or
floating"; "the left panel should be the default place for properties and not take too much space away from the cad
surfaces"; "this is much better … do one more /ui-design elevate run".

Added during this run:

| # | Operator words | How v10 answers |
|---|---|---|
| R1 | "the properties should always show a running estimate of: span, chord, thickness, aspect ratio, area" | The **Wing** block ends Properties on every screen with a foil open (20 of 20 screen × selection cases). Estimates are prefixed "≈" and have an "Estimates · definitions" disclosure plus `title` tooltips. They update live while a point is dragged (measured: 1287 → 1449 cm² during a drag). In section mode the station's chord and t/c are added. |
| R2 | "In the edit section — i should be able to choose an existing known foil profile from the catalog of foil sections … use known foils like eppler, naca etc but also i should be able to save a foil section to re-use" | **Section ▾ → Replace from catalog…**: a search combobox over a listbox grouped NACA / Eppler / Speer / My sections. Eppler rows are disabled with "Pending admission — terms requested from UIUC · GEN sections remain" (spec A4.10, line 1611). Speer H105 is disabled as "Cite only (LINK)". A dashed accent preview shows on the section. The detail line says how closely the editable points follow the catalog shape (NACA 0012: 0.03 mm). Replace is one undo step, and a chip then reads "Catalog original · NACA 0012", which turns into "Modified from NACA 0012" after an edit. **Save to My sections…** takes a name and records provenance ("Modified from NACA 0012"). |
| R3 | DECISION: "in the property sheet i should be able to change the behavior of a point on the curve: control point, anchor point, point with the specific semantics for them" | A **Type** row with Anchor point / Control point. Anchor tangents are Smooth / Symmetric / Corner; the section adds Horizontal / Vertical / Fixed angle. Named points keep a locked type: root end, tip end, nose and the trailing-edge ends. Changing a type redraws only the segments it touches (the trailing edge stays at 230,166 when a leading-edge point changes) and is undoable. This replaces v8's fit-point-only assumption. |
| R4 | DECISION: floating panes — option (a) confirmed | A control in the model area that takes focus is never left under a float. The float moves to the nearest corner of the model area that clears the target and announces it. If no corner clears it, the float docks back where it came from. This is not an assumption any more. |
| R5 | "show root chord and mean chord" | Root chord is a typed dimension. The estimates show Mean chord (S/b, the area-weighted mean). |
| R6 | "it would be good to show MAC as well" | The estimates show MAC, defined as (2/S)∫₀^{b/2} c(y)² dy and labelled "not the same as S / b". It covers the whole wing only; ȳ is left out for space. |
| R7 | "i should be able to enter the span and root (and tip) chord in text" | Span, Root chord and Tip chord are typed inputs in mm, set apart from the estimates. A value commits on Enter or blur; Escape reverts it. An invalid value gets an inline error and `aria-invalid`, and the geometry stays unchanged. One commit is one undo step, and the estimates update after it. Inside the section editor these three fields are read-only. |

### Decision requests (defaults applied, not asked)

| ID | Question | Default in v10 | What changes if the operator chooses otherwise |
|---|---|---|---|
| DR-1 | What does typing a span do? | Scales the planform spanwise; chords unchanged | `setSpan` only |
| DR-2 | What does typing a root or tip chord do? | Changes only that end; the chord factor blends linearly to 1 at the other end. **assume:** the leading edge stays fixed and the trailing edge moves. Scaling about the quarter-chord line would keep c/4 sweep; confirm with the operator; if wrong, `applyChordScale` changes its reference line and nothing else | `applyChordScale` |
| DR-3 | What is "tip chord"? | The chord at the outermost authored station. A tip that closes shows "Tip closes — edit the tip station", never an editable 0 | Copy COPY-108 |
| DR-4 | How is a catalog section brought into the editor? | It is fitted to the editor's points (nose + three per surface) and the fit deviation is shown. **assume:** the build stores the spec's degree-5 record and reports the spec's conversion residual (COPY-102); the mockup does not perform that conversion | `/design-slice` for the catalog conversion |
| DR-5 | How do anchors and control points mix on one curve? | A segment runs anchor to anchor. Control points between two anchors are extra Bézier vertices, so the segment's degree rises. **assume:** this is compatible with the spec's B-spline record through conversion; to be settled by `/design-slice` with the geometry lens | Spec Part A record, FoilDSL |

## 2. Direction (Stage 1, unchanged from v8/v9 except where noted)

Job, archetype (G1 Parametric Modeling Workbench), adjectives (quiet · direct · precise) and references (Fusion 360,
Shape3D, VS Code, Premiere Pro) are as in [the v8 review](ui-workbench-v8.md) §2 and [v9](ui-workbench-v9.md) §1. The
archetype was re-checked against the task: editing is serial (one point, one field at a time), and v10 adds no parallel
reading surfaces. The added reading surface is the Wing summary: 5 estimates, 3 dimensions. **Triggered standards:**
UI-T1 fires (quantities: one precision per quantity, units on every value, estimates labelled as estimates, MAC defined
apart from S/b). UI-T4 fires (native Avalonia; this HTML is direction evidence, and native proof stays a build
obligation, see v9 §5.2). UI-T2 does not fire (no generated imagery). UI-T3 does not fire (no model-backed feature).

## 3. Measured before diagnosing (v9, Stage 3)

| Measure (v9) | Value |
|---|---|
| Controls / canvas targets | Workspace 22 / 11 · Section 22 / 9 |
| Hard states in the harness | error ✓ · overflow ✓ · **empty / first run ✗ · loading ✗** (v8 had them) |
| Fields clipped at the 200 px dock | Workspace 2 of 7 ("245.0", "−175.6") · Section 5 of 7 |
| Same quantity, two precisions | Plan aft: Properties "13.0", grid "13.00"; a ⌘-nudge to 13.01 was invisible in Properties and on the canvas |
| Browser rows with no behaviour | 3 of 8 (Dihedral, Twist, Thickness) |
| Canvas hover affordance | none |
| Type sizes | 8 (cube label), 11, 12, 13, 14 px |
| Craft gate | no findings |

## 4. Rubric findings (structure before surface)

| # | Location | Dimension | Sev | Evidence | Fix in v10 | Conf |
|---|---|---|---|---|---|---|
| 1 | Harness / workspace | State completeness | Major | No first-run or loading state (§3) | First-run, opening (with Cancel) and open-failed states inside the workspace | Verified |
| 2 | Floats over the model | Accessibility 2.4.11 (carried from v9) | Major | v9 §5.1 | Option (a) plus a dock fallback. The sweep has no SVG exemption: 318 targets, 0 hidden, 0 overlapped | Verified |
| 3 | Properties / grid / canvas | Quantities (TQ) | Major | Two precisions for one quantity; the fine nudge was invisible | One precision per quantity (DESIGN.md §12.0e) | Verified |
| 4 | Properties at 200 px | Density | Major | Values clipped (§3) | 56 px labels; smooth anchors show Angle plus two lengths; 0 clipped in 16 cases | Verified |
| 5 | Browser | Truth / IA | Minor | Inert rows; copy promised curve selection | The Browser lists Sections; the copy says "point or station" | Verified |
| 6 | Canvas | Feedback | Minor | No hover | Hover ring (`line-strong`) | Verified |
| 7 | View cube | Craft | Nit | 8 px label | 11 px | Verified |

**Generic-tells self-check:** no gradients, glow or decorative illustration. The three start cards are the three start
actions (Fusion/Shape3D start pattern), not a feature grid. The accent is used only for selection, focus, primary
actions and the catalog preview. "⚠" marks warnings as in v8/v9. **Motion inventory:** none. The opening state is a
static skeleton, so the reduced-motion path is identical by construction.

## 5. Adversarial record

| Lens | Round 1 | Cycle 1 | Outcome |
|---|---|---|---|
| UX & Accessibility (hard veto) | **BLOCK.** Blockers: typed numbers failed silently ("12abc" → 12, no message; 3.3.1); catalog search had no status message (4.1.3). Majors: Escape on a section point discarded the edit; a floating Properties pane cut off the Wing block. Minors: undo crossed the draft boundary; no fallback when no corner clears; a chatty live region; opening semantics; copy truth (multi Height, Smooth OK); clipped TE label; control point versus handle glyph; F6 skipped dialogs | All fixed; each has a sweep assertion (`a11y1_*` … `a11y12_*`). Red-first: the 3.3.1 assertion fails on v9 and passes on v10. The other red states rest on the lens's source trace | **PASS, cleared by the lens.** Its two remaining Minor/Nit items (a silent crossing clear; redo crossing the draft boundary) were fixed after the pass; not re-reviewed by the lens |
| The Simplifier (soft veto) | **BLOCK.** M1: the Browser was a third copy of the points. M2: planform dims editable inside a section draft that Cancel rolls back. Nine Minors | M1 and M2 applied. Also applied: Wing not collapsible, no disabled Angle, no Symmetric note, "Span position" name. Overridden with rationale: toolbar Undo/Redo (Fusion quick-access convention, pointer route; v9 had them at line 155 — the lens's "v9 has no id=undo" is a grep for an id v9 never used), dt tooltips (operator asked for tooltip or detail), "Mid t/c" (operator asked for it), "New from example" (spec CAT-03 presets), Cancel while opening (the loading state must be cancellable), separate Type and Tangent (the operator's words are type-first), "≈" (each value self-describes), the catalog undo note (required) | **Cleared by the lens** |

Fan-out: 2 of 2 used (UX & Accessibility, Simplifier). Repair cycles: 1 of 2.

## 6. Final measurements

| Check | Result |
|---|---|
| Harness combinations: 8 screens × 7 layouts × 3 themes (macOS) + dark (Windows) + 1280/1920 selections | **232/232**: 0 contrast failures (15 token pairs per theme), 0 targets under 24×24 CSS px (HTML controls, summaries, listbox options, SVG targets) |
| Focus after every action | **39 checks, 0 failures**. The checks cover the v9 set plus Browser, type change, dimension commit and Tab, invalid entry, undo, catalog open / Replace / Escape, Save and its error, section Escape with edits, opening, cancel, open-failed and New foil |
| Occlusion with a float open (1280/1440/1920 × workspace, section, catalog, first run × centre, top right, bottom right) | **318 targets, 0 hidden, 0 overlapped by the float, float never outside the model area.** Keyboard Tab walk: 0 invisible. Narrow model area (left and right docks at 420 px, 1280): 11 targets, 0 overlapped, 11 docked. The test measures hit-testing (`elementFromPoint`, centre plus 8 samples) plus the raised clone of the selected point; it does not measure paint |
| Properties at the 200 px dock (workspace and section × 4 selections × 1280/1440) | 0 clipped values; the selection content and the Wing block fit at 1280 without scrolling |
| Controls, on the v9 counting rule (button, input, select) | Workspace with a point 22 → 25 (left dock 10 → 13: Type, 3 dimensions); section with a point 22 → 23; workspace with nothing selected 15 → 18 |
| Craft gate (`ui-craft-gate.py`) | 0 Blocker · 0 Major · 1 Minor: `main.window` clips a positioned child. Deliberate: the window is the OS window, and menus escape it as `position:fixed`. The gate caught an injected off-token colour, so it scanned a live corpus. This is a floor, not a verdict |
| `design-lint.py --strict` | clean |
| JS errors | 0 |

## 7. Open items and residual risk

1. **Spec alignment (not in scope):** the mixed anchor/control point model (R3, DR-5) and the catalog Replace/Save flows
   (R2, DR-4) must reach the spec: Part A record, Part B flow F2 and Part C. They then need `/design-slice` with the
   computational-geometry lens.
2. **Coincident points (should-fix-next):** at small scale the trailing-edge upper and lower end points share a hit
   area, so a pointer press picks the one drawn last. Keyboard, the Points grid and the raised paint reach both. The
   CAD convention is selection cycling on a repeated click.
3. **No screen-reader pass:** announcements are inferred from ARIA, not traced. VoiceOver and NVDA are needed at the
   next build slice (Test Architect handoff).
4. Carried from v9 §5.2: floats are native OS windows in the build; spike them on mixed-DPI dual monitors.
5. Mockup limits: every file opens the same fixture geometry; Smooth, Add station, Measure, Fit, Save and Import say
   they are not part of the mockup.

## 8. Ranked plan

| Rank | Change | Why |
|---|---|---|
| **1 — highest leverage** | `/design-slice` the mixed point model and the catalog conversion (DR-2, DR-4, DR-5), after updating the spec record | It changes the geometry of record. Properties, the canvas, FoilDSL and the catalog all depend on it |
| 2 | Build the shell with the v10 structure: left Properties with the Wing block, optional docks, and the focus contract for floats | Every other UI change sits on it |
| 3 | Selection cycling for coincident points | The only pointer gap left in the model area |
| 4 | Screen-reader pass on the built shell; spike native floats | No screen-reader evidence yet; the largest native risk |

| | |
|---|---|
| **Completed** | v10 mockup, all harness screens (first run, opening, open failed, workspace, section, Smooth, catalog, Save); oracle and craft gate; accessibility and Simplifier vetoes cleared |
| **Remaining** | Spec update for R2/R3; screen-reader pass; native float spike; selection cycling |
| **Best next action** | Update the spec record for the mixed point model, then `/design-slice` it with the computational-geometry lens |
