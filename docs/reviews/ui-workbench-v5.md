---
id: review-ui-workbench-v5
title: UI review — workbench mockup v5 (control-vertex splines, four viewports, a tool palette)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, ux, accessibility, geometry, marine-cad, mockup, v5]
links:
  - { to: spec-cfd-workbench-v1, rel: documents }
  - { to: mockup-workbench-v5, rel: relates-to }
  - { to: design-language, rel: relates-to }
  - { to: workbench-direction, rel: relates-to }
  - { to: control-vertex-workspace, rel: relates-to }
  - { to: review-ui-workbench-v4, rel: supersedes }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-21
summary: >-
  Elevate-mode review of the v5 CAD experience (control-vertex splines with levers, four viewports with title
  menus, a nine-verb tool palette and options strip, the display cage, the measured station residual) against
  specification 1.3. Four independent lenses: Computational Geometry and UX Researcher / IA on the spec delta,
  UX & Accessibility (hard veto) and Marine CAD UX on the artifact. All four returned BLOCK or PASS-WITH-CONDITIONS
  on first read; every Blocker, Major and condition was fixed in place and became an oracle row whose value the
  proof records. The accessibility veto cleared on the second pass; the marine veto on the third.
review-suggested:
  - { by: mockup-workbench-v5, on: 2026-09-21, reason: "Mockup v5 (control-vertex splines, four viewports, tool palette) supersedes v4; spec 1.3 GEO-03/05/13/15, CAD-01/04/06/07/08, A4.2, A4.12, UX-24, UI-25–27; oracle tools/check-mockup-v5.mjs" }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
---

# UI review — workbench mockup v5 (control-vertex splines, four viewports, a tool palette)

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` (U1–U20), `technical-ui-design.md` (TQ1–TQ12) and the native-client trigger UI-T4. Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surface(s) reviewed:** `docs/mockups/workbench-v5.html` — the four-viewport workspace (Top · Perspective / Front · Starboard, title menus, maximise), the control frames of the five master curves (vertices, levers, locks, the η-plot twin), the tool palette and options strip, the display cage and the 3D camera, the station document with its measured conversion residual.
**Reviewed against:** spec v1.3 `docs/specs/cfd-workbench-v1.md` (A3 glossary, A4.1, A4.2, A4.3, A4.5, A4.9, A4.12, GEO-03/05/13/14/15, CAD-01/04–08, ANA-22, B1, B7, UX-14/15/23/24, UI-25–27, the C2 strings) · `DESIGN.md` §4 (Control vertex · Tool palette · Viewport title bar), §5, §7 (COPY-98–102), §12.0d · the direction brief's v5 section · `docs/lessons/defect-classes.md` (UI-A…UI-M).
**Reviewers:** Computational Geometry (narrow hard veto on the record) · UX Researcher / IA (UX-specification veto) · UX & Accessibility (lead, a11y hard veto) · Marine CAD UX (soft veto on precision-CAD conventions) · the author (measurements and fixes only; cleared nothing).
**Date:** 2026-09-21 · **Mode:** elevate

## 1. Verdict

> **PASS (all four vetoes cleared after fix passes)** — first reads: Computational Geometry **PASS-WITH-CONDITIONS** (five Majors on the spec and the mockup's evaluator), UX Researcher / IA **PASS-WITH-CONDITIONS** (four Majors on flow integrity and pointer-only verbs), UX & Accessibility **BLOCK** (one Blocker: Insert CV and Measure pointer-only; four keyboard/focus Majors; a copy-truth Major; the palette hidden at reflow), Marine CAD UX **BLOCK** (eight Majors: handedness, twist sign and pivot, an invented cage, a phantom lock, Z/⇧Z, the Fair contract, Tracing, unit entry). Second pass: accessibility **PASS-WITH-CONDITIONS** with one Major left (a vertex nudged in the η-plot re-focused its Top twin) — fixed with slot-scoped focus and an oracle row; marine **BLOCK (narrow)** on two leftovers (the cage rows still on the old transform; "monotone pieces" counting inflections) — fixed with one shared station transform and a Farin–Sapidis count, both oracle rows. Third marine pass: **PASS-WITH-CONDITIONS, veto cleared** — both leftovers verified closed in the file and the oracle (`stationXform` shared by skin and cage rows; the Farin–Sapidis count), its condition (the proof postdating the fix) met by the tenth full run; its last HYG-A nit (the skin's LE/TE points and the body plan re-deriving the rotation) applied in the same pass. The gate is the oracle: `tools/check-mockup-v5.mjs` — 16 oracles, 77 measurements, 30 shell cells, 0 page errors, 0 external requests — and every clearing observation named by a lens is an assertion whose value `docs/proof/workbench-v5-browser-check.json` records.

## 2. Measurements (DX23 — measure before you diagnose)

| Quantity | v4 (before) | v5 (after) | How measured |
|---|---|---|---|
| Visible chrome controls, CAD at 1280 × 800, on entry | 71 | **45** | `chromeCount()` over `#window` after a fresh load (buttons, links, inputs, selects, sliders, summaries; closed-menu items excluded) |
| Same, mid-session (docks open, a station selected, a draft closed) | — | 52 | the same count without reload |
| Regions competing for the workspace | 10 | 10 | shell regions with a box |
| Editing controls with two homes (toolbar + pane + row + dock) | 4 homes | 1 home each (palette · strip · Properties · title menu) | B1 verb table walk |
| Vertex → curve gap after ten 1 mm nudges of an interior vertex | n/a (through points) | gap ≈ 0.3 × the move; the curve moves ≈ 0.6 × the move | `gapToCurve` / `curveAt` over 800 samples |
| Local support: η 0.10 after moving vertex 6 of 7 | n/a | unchanged to 10⁻¹²; η 0.90 moved | `chanAt` before/after |
| Station conversion residual (13-point NACA 66-209, 89.6 mm chord) | "0.000" (constant) | **7.5 µm upper · 7.1 µm lower with 12 vertices** (uniform-index fit: 841 µm) | `secResidual` at the catalog points; shown identically on three surfaces |
| Focused vertex ring on the graphite viewport | 9.35 / 9.31 / 17.62 : 1 (v4) | 9.35 / 9.31 / 17.62 : 1, 3 px | computed stroke vs viewport background in three themes |
| Smallest rendered SVG text at five presets × six areas | ≥ 12 px | ≥ 12 px after the walk (11.7 px before the resize rule) | `smallestText` over `#window svg text` |
| Targets under 24 px after every resize path | 0 | 0 (52 closed-menu items and seven scaled handles before the rules) | in-page audit |
| Handedness: TE x − LE x in the body plan / Starboard camera | +/− (mirrored) | −/− (both nose right) | `data-le-x`, `data-te-x` on rows and stations |
| Twist sign at +5°: TE y − LE y (skin / cage / body plan) | nose-down, cage disagreeing | all > 0 (TE drops), cage = skin within 0.6 px | `data-te-y` on `[data-st3d]`, `[data-cage]`, `[data-bp]` |
| Craft gate (`ui-craft-gate.py`) | 14 Minors | 14 Minors (dispositions in §3) | `docs/proof/ui-craft-findings-v5.json` |

## 3. Findings

Location · dimension · severity · evidence · fix · confidence. **Every Blocker and Major below is fixed in the artifact and asserted by the oracle line named.**

### Computational Geometry (spec A4.2 / A4.12 / GEO stories; the mockup's evaluator probed under node)

1. `deleteCV` · copy truth · **Major** · Delete reported no shape change though GEO-05 requires it · measures the deviation on 81 η samples and reports it (chip and status); oracle group 6 · Verified.
2. A4.1 vs A4.2 · one floor · **Major** · "six to ten" vs "p + 2 = 5" · one floor (six, `CVS.min`), Rebuild 6–10, the Schoenberg–Whitney justification dropped · Verified.
3. A4.2 · pin linearity · **Major** · a value-at-η pin is linear only if x(t) = η · the abscissa invariant (strictly increasing, single-valued v(η)) and the Newton-on-t loop around the KKT projection written into A4.2 · Verified.
4. A4.5 · missing oracle · **Major** · no sample set for distribution-curve deviations; the export metric unstated · a fourth oracle kind (201 η samples + every knot, Δv in the channel unit) and closest-point export deviation with knot lines and the tip · Verified.
5. A4.12 vs KB index item 6 · kernel contradiction · **Major** · item 6 excluded OCCT · the kernel ADR re-decides item 6 explicitly; the spike adds a zero-chord tip and the maximum-twist example · Verified.
6. Minors: the mockup comb is finite-difference and wrong at both ends (labelled; product analytic); the mockup's Insert splits a polygon leg (copy reworded; product Boehm, 10⁻¹²); "clamped uniform" claim dropped; the section fit needs m ≥ n, centripetal parameters and a dense measure (done: n ≤ m, centripetal, knots by averaging); dead functions deleted; the ledger row on Smooth retired; the root mirror's G-claim stated (G1; G2 by even extension; G3 not enforced).

### UX Researcher / IA (the UX delta)

7. `removeStation` · flow integrity · **Major** · applied with no undo item while Escape put a draft on the stack · Add and Remove station push history and are undoable; oracle group 6 (⌘Z restores, ⌘⇧Z removes) · Verified.
8. Lock checkboxes · one-draft model · **Major** · silently dropped a vertex draft · refuse with the open-draft string and revert; oracle group 6 · Verified.
9. Measure and Add station · findability · **Major** · palette promised pointer paths that did not exist · click paths in any elevation (Add station at the picked η; Measure by two picks) plus keyboard twins on the strip; oracle group 6 · Verified.
10. "Fit points" · naming · **Major** · opened the DAT dialog · Fit points refits the active curve through its anchors with the residual reported; DAT import moved to the Catalog tab's button · Verified.
11. Minors: Rebuild/Insert/Delete refused under a draft like Fair (one rule, UX-23); the curve selector leads every tool state; "η-plot" unified; the ≤ 48 rationale recorded in D4 (45 measured; the ≤ 35 target not met, floor explained); oracle rows for the forced single viewport, the quiet entry and UX-15; the stale B1 concept rows rewritten.

### UX & Accessibility (hard veto)

12. Insert CV / Measure · keyboard operability (SC 2.1.1) · **Blocker** · pointer-only · Insert at η and Measure between two η values on the options strip; oracle group 6 asserts both with `activeElement` · Verified.
13. `renderShape`, `renderPalette`, `renderOptions` · focus (SC 2.4.3, class UI-C) · **Major** · not `withFocus`-wrapped; Enter-apply, cage Enter, palette Enter and the resize re-render dropped focus · wrapped; `focusKey` learns cv · cage · st3d · tool · qview · qtoggle · qmax · bp · comb, the vertex key slot-qualified; oracle asserts focus after each · Verified.
14. Title menu · name/role + focus (SC 4.1.2) · **Major** · `role=menu` with no keys; items closed under focus · WAI-ARIA keys (open → first item, arrows, Home/End, Escape → summary, Tab leaves, a choice focuses the summary before `open` is removed), `aria-haspopup`, sections as `role=group`; oracle group 13 · Verified.
15. Fair chip · copy truth (U11) · **Major** · unconditional "Return applies" above tolerance · the C2 string branch; one `draftLabel()` formatter for "Draft open — <kind> · deviation <d>"; oracle group 6 and 13 · Verified.
16. Reflow preset · hidden functionality · **Major** · palette `display:none` at 640 × 400 · a 44 px row above the viewports (names visually hidden); oracle group 13 counts nine 44 px tools and drives a verb by keyboard · Verified.
17. Second pass · η-plot twin · **Major** · the vertex re-query found the Top twin first · slot-scoped re-focus in `wireElevation`, `focusCV` and `focusKey`; oracle asserts the nudge and the Return stay in slot d · Verified.
18. Minors (all applied): ⇧F label; off-token 10 px fonts → `--fs-caption`; tracing joins; `aria-pressed` only on toggles; `aria-readonly` on locked vertices; the maximise item named; comb ± keyboard focus; the strip measured with a Measure result (no clip at 1024 px); the rail badge token. Recorded, not changed: the 114 px viewport at the reflow preset behind a 44 px palette row (orientation, not editing — rationale in CAD-07).

### Marine CAD UX (soft veto)

19. Starboard body plan · handedness · **Major** · nose left in 2D, right in the Starboard camera · `X = ox + (1.1·c_root − x)·s`, caption "nose →"; oracle asserts TE left of LE in both · Verified.
20. Twist · sign and pivot · **Major** · nose-down about the quarter chord against A4.4/A4.7 · one `stationXform(u)` (LE pivot, `z0 − dx·sin tw`) shared by skin and cage rows; oracle asserts the TE drop at +5° in the body plan, the skin and the cage row · Verified.
21. Cage · false CAD model · **Major** · twist/t/c polygons at invented 3D positions · only section and rail polygons; the group renamed; oracle asserts the absence · Verified.
22. Root vertex · phantom lock · **Major** · "Locked by closure" not in Properties; the lever rewrote it silently · vertex 0 unlocked, both root vertices named "coupled", either moves; oracle · Verified.
23. Z/⇧Z · per-OS table · **Major** · inverted · Z out, ⇧Z in, `cameraHelp` updated; oracle · Verified.
24. Fair · contract · **Major** · no PreserveEnds, no monotone-piece count, fixed comb scale, no κ · PreserveEnds position · tangency · curvature (curvature falls back with an honest label when six end rows cannot be held), the monotone-piece count (sign changes of dκ/dη, dead band — second pass) beside a scale stepper, the comb scaled to its longest tooth and never clipped, κ and R in Tracing; oracle rows · Verified.
25. Tracing · pointer probe · **Major** · selected-station only · mousemove over any elevation (body-plan rows by hit test); oracle · Verified.
26. Properties · unit entry · **Major** · `parseFloat` dropped units · `parseQuantity` (mm · m · cm · in · ° · % · chord) with the resolved echo; oracle ("0.1 m", "95 mm", "9 %") · Verified.
27. Minors applied: Rebuild 6–10; ↑↓ value · ←→ η · Shift ×10; diamond = end vertex, lock ring = locked; the Ghost named a fixture ghost; double-click on a rail polygon maximises its elevation; Fair keeps the curve's vertex count; the twist lane's sign cue; κ's units. Recorded deviation: the master-curve comb shows graph curvature in mixed units (A4.9).

### Craft gate (floor, never a verdict) — `docs/proof/ui-craft-findings-v5.json`, 14 Minors

Nine *cramped-padding* on `.quads`, `.quad` and `.qtitle` (edge-to-edge viewport panes and 32 px title bars — accepted: every comparable draws viewports edge to edge); one *side-tab* stripe on the pressed palette tool (accepted: the pressed-state indicator the rail uses); one *monotonous-spacing* (4 px in the palette); two *clipped-overflow-container* on `.app` / `.docbody` (carried from v3, the positioned children are the drawers and the More ▾ menu, both measured visible); the em-dash count (a recorded deviation, the spec's fixed strings).

### Operator finding after publication

28. Control frames · findability · **Major** · the operator: "I still don't see the control handles for the CV splines" — only the *selected* curve carried its frame, so Front and Starboard showed nothing to grab until a curve was clicked · every curve's frame now renders in its elevation (active emphasised, others at 0.62 opacity, all draggable; a press selects), glyphs 13 px; oracle group 6 asserts five frames in their slots with one active · Verified. *Lesson:* the v4 review's "one frame at a time" cut for busyness hid the primary affordance; busyness is measured in chrome, not in handles on the geometry.

29. Vertex drag by trackpad · interaction truth · **Major** · the operator: "some UX goofiness if I attempt to select and manually move (with trackpad) a point" — the drag mapped the pointer through the SVG captured at press time, which the first re-render detached, so the vertex jumped to its ordering limit; the direction-only oracle test had passed on the jump (class UI-N); two overlaps let a press grab the neighbour that painted last, and a focus-restoring re-render re-selected the previously focused vertex · the drag maps through the live SVG and mapping on every move, a press goes to the nearest vertex centre (hit circles never past the midpoint to a same-frame neighbour, never under the 24 px floor; levers paint over the ends they overlap, the active frame paints on top), and the pressed vertex takes focus before the re-render; oracle group 6 asserts the glyph stays under the pointer to ≤ 2 px over twelve moves and that a press at every vertex centre selects that vertex · Verified.

## 4. Rubric scorecard (DX22)

| Dimension | v4 | v5 | Note |
|---|---|---|---|
| Archetype fit (four viewports + palette + Properties for a wing designer) | 3 | 5 | the IA lens: the Rhino/Fusion shape; nothing left with two homes |
| Structure before surface (IA, one draft, undo items) | 4 | 5 | every entry point refuses under a draft with one string; structural edits undoable |
| Geometry truth (the record, measured numbers) | 3 | 5 | vertices only; gap, support, residual, deviation all measured; no constant on a surface |
| Precision-CAD conventions | 3 | 4 | handedness, sign, pivot, per-OS keys fixed; the rail comb stays in graph units (recorded) |
| Accessibility (WCAG 2.2 AA for the change) | 4 | 5 | veto cleared on pass two; every focus path asserted |
| Copy truth | 3 | 5 | every rendered count/residual/deviation from its operands |
| Density and chrome | 2 | 4 | 71 → 45 on entry; the ≤ 35 target not met, floor explained |
| Visual craft | 4 | 4 | fourteen Minors on the floor, all dispositioned |

## 5. Ranked plan (highest leverage first)

1. **UX-05 formative session on v5** — the one measurement no lens can replace: whether a shaper finds the frame, the levers and the palette without the tooltip; the ≤ 35 chrome question is answered by a person, not a count.
2. **The master-curve degree ADR** (Open decisions) — degree 3 with seven vertices is Inferred; a fairing and continuity fixture on both degrees decides it.
3. **The geometry-kernel spike** (A4.12): OCCT `ThruSections` with a zero-chord tip and the maximum-twist example, STEP in FreeCAD, rhino3dm, the licence reading of KB item 6.
4. A distribution-curve oracle fixture in the product (A4.5, 201 samples + knots) so Fair/Rebuild/Fit/Insert/Delete deviations stop being mockup arithmetic.
5. The reflow preset's 114 px viewport — decide whether the palette row hides behind a disclosure at 640 × 400 (record either way).
6. A screen-reader trace (VoiceOver/NVDA) of the `role=slider` frame and the `details` menu — the a11y lens's residual risk.

## 6. Evidence

- `tools/check-mockup-v5.mjs` → `docs/proof/workbench-v5-browser-check.json` (16 oracles, 77 measurements, 30 shell cells, 0 page errors, 0 external requests) — group 6 the record (gap, local support, levers, locks, root coupling, constructions, one draft, pointer and keyboard paths, undo items, unit entry, the station residual), group 13 the workspace (viewports, menus with keys, cage, camera, handedness, twist sign, Z/⇧Z, comb, Tracing, reflow, quiet entry, chrome count).
- `docs/proof/ui-craft-findings-v5.json` (14 Minors) · `design-lint.py DESIGN.md --strict` clean · spec rendered with parity (`docs/proof/spec-html-check-cfd-workbench-v1.json`, 149 requirement ids, 0 missing).
- Lens reports: Computational Geometry (PASS-WITH-CONDITIONS → conditions applied), UX Researcher / IA (PASS-WITH-CONDITIONS → applied), UX & Accessibility (BLOCK → PASS-WITH-CONDITIONS, condition fixed and asserted), Marine CAD UX (BLOCK → BLOCK narrow → third pass recorded below).
- Third marine pass: PASS-WITH-CONDITIONS (veto cleared); residual risks named by the lens — break markers on split tangents, the section-editor comb, keyboard-only GEO-05 and trackpad GEO-10 runs, the per-OS shortcut table not re-examined on that pass, the mockup's finite-difference comb (A4.9 admits it).
