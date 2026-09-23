---
id: review-ui-workbench-v4
title: UI review — workbench mockup v4 (CAD editing views)
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-review, ux, accessibility, cad, mockup, v4]
links:
  - { to: spec-cfd-workbench-v1, rel: documents }
  - { to: mockup-workbench-v4, rel: relates-to }
  - { to: design-language, rel: relates-to }
  - { to: workbench-direction, rel: relates-to }
  - { to: cad-editing-views, rel: relates-to }
  - { to: review-ui-workbench-v3, rel: supersedes }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-21
summary: >-
  Elevate-mode review of the CAD editing views (icon rail, splines, one free camera with named views and a view
  cube, editing elevations for the four control curves, the Station document) against specification 1.2. Two
  independent lenses: UX & Accessibility (hard veto) on the surface and UX Researcher / IA (UX-specification veto)
  on the 1.2 stories; both cleared their vetoes after two fix passes, with every clearing observation now an oracle
  assertion whose values the proof records.
review-suggested:
  - { by: mockup-workbench-v4, on: 2026-09-20, reason: "Mockup v4 (CAD editing views) supersedes v3; spec 1.2 CAD-04–06, UX-23, UI-24–25; oracle tools/check-mockup-v4.mjs." }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# UI review — workbench mockup v4 (CAD editing views)

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` (U1–U20), `technical-ui-design.md` (TQ1–TQ12) and the native-client trigger UI-T4. Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surface(s) reviewed:** `docs/mockups/workbench-v4.html` — the icon rail; the 3D viewport (one camera, named views, view cube, pointer and keyboard orbit, 3D selection, analysis layers in any camera); the lines-plan as editing elevations (Top: outline rails · Front: dihedral/anhedral and thickness · Starboard: a body plan with the twist handles); the Station document (full 2D section editor with grid, dimension, control points, palette on the toolbar, Properties); splines throughout.
**Reviewed against:** spec v1.2 `docs/specs/cfd-workbench-v1.md` (CAD-01–06, ANA-22, UX-14, UX-23, UI-18, UI-23–25, the B7 pointer contract, B1 verb table) · `DESIGN.md` §12.0c and the new component rows · the direction brief's v4 section · `docs/lessons/defect-classes.md`.
**Reviewers:** UX & Accessibility (lead, a11y hard veto) · UX Researcher / IA (UX-specification veto on the 1.2 delta) · the author (measurements and fixes only; cleared nothing).
**Date:** 2026-09-21 · **Mode:** elevate

## 1. Verdict

> **PASS (both vetoes cleared)** — the UX & Accessibility lens returned **BLOCK** on its first read (operable 3D sections as `img`, pointer-only pan, zero-area cube faces, a nested close control, a palette whose Return/Escape had no handler, a false-height free-surface plane, an undoable-less Escape), **BLOCK** again on the second (the new focus ring at 2.45:1 on graphite, a slider/pan key collision, thickness handles inside the dihedral handles' targets) and **PASS-WITH-CONDITIONS, veto cleared** on the third, whose two publication conditions (a stale Front label; the measurements written into the proof) landed before this review closed. The UX Researcher / IA lens returned **PASS-WITH-CONDITIONS** on the 1.2 stories (thickness assignment, camera verbs, one-draft model, the refusal path) and cleared the UX-specification veto on its second read. Verdict is on the HTML review artifact and specification 1.2.

> **Highest-leverage change:** one draft model for every geometric edit — an anchor dragged in Top, a lever nudged in Side, a control moved in the Station document and a point nudged in the curve pane all open the *same* Preview → Return / Escape draft on the same channel, so the four control curves became editable in their own elevations without a second editing path, and the oracle proves each with one keyboard and one pointer route.

| | UX & Accessibility | UX Researcher / IA | After fixes |
|---|---|---|---|
| Blockers (sev 4, or any a11y ≥ 3) | 2 (+ 7 a11y Majors under the obligation); 1 more on the second read | 0 | 0 |
| Majors (sev 3) | 7 (+2 second read) | 4 | 0 |
| Minors (sev 2) | 7 (+4 second read) | 7 | 0 open; 1 recorded (open decision) |
| Nits (sev 1) | 2 (+1) | 2 | 1 recorded |

**Accessibility veto:** **PASS** — cleared on the third read: every pointer operation has a keyboard route with a handler; 3D sections, elevation handles, cube faces and chevrons carry their roles, names and values; focus and refusal rings measure 9.35:1 / 9.31:1 / 17.6:1 on the viewport in the three themes (computed by the oracle, not inferred from tokens); no open Blocker or Major.
**UX-specification veto:** **PASS** — cleared by the UX Researcher / IA lens on its second read (need evidenced; IA coherent; happy, alternate and error paths written and built); its one condition, five oracle rows, landed in group 13 before this review closed.

## 2. Measurements (DX23 — measure before you diagnose)

| Metric | v3 | v4 | Note |
|---|---|---|---|
| Curves drawn as polylines in the viewport / elevations / section editor | all (60-point polylines) | 0 | oracle group 13: `polyline, polygon` count 0; ≥ 6 Bézier paths in the elevations |
| Camera | one projection per named view; iso rotated by keyboard only (azimuth) | one camera: az · el · zoom · pan; 7 presets; pointer orbit in both navigation presets; wheel zoom; view cube | group 13 |
| Control curves editable on the geometry | 0 (η-plots only) | 5 channels across 3 elevations (Top: le, chord · Front: elev, tc · Starboard: twist) | group 13: keyboard and pointer edits applied as revisions; refusals; spacing ≥ 32 px |
| Station editor | `<dialog>` 900 × 300 | document tab; section sized to the editor box; grid + chord dimension; palette on the toolbar | group 6 |
| Rail | numerals + names | icons + names; readiness in the accessible name | group 3 and 13 |
| Toolbar hidden groups (`More ▾`) | 0 at ≥ 1280 · ≤ 1 at 1024 · ≤ 3 at 640 | same | group 1 |
| Smallest rendered SVG text | ≥ 12 CSS px | ≥ 12 CSS px at five presets × six areas | group 2 |
| Worst required contrast pairing | 3.43:1 (control boundary) | 3.43:1; text worst 5.34:1 | token pairs + computed-colour scan |
| Craft gate | 1 recorded deviation (em-dash) | 1 recorded deviation (em-dash) on the final run | `docs/proof/ui-craft-findings-v4.json` |
| `design-lint.py --strict` | pass | pass (three component rows added) | |
| Artifact size | 272 KB | 321 KB | over the 300 KB direction budget by 7 %; dead v3 views and the dialog were deleted; the growth is the geometry block and the a11y fixes — recorded, budget to be revisited when the next surface lands |
| Browser oracle | 15 groups · 78 measurements | 16 groups · 78 measurements · 30 shell cells | `tools/check-mockup-v4.mjs` |

## 3. Findings

*Structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an accessibility finding at ≥ 3 is a Blocker under the obligation. Lens: A = UX & Accessibility, I = UX Researcher / IA.*

| # | Lens | Location | Dimension | Sev | Evidence | Fix applied | Disposition |
|---|---|---|---|---|---|---|---|
| 1 | A | 3D sections | Accessibility (4.1.2) | 4 | operable sections carried `role=img` (recorded v1 class recurring) | `role=button aria-pressed` in a `group`; port mirrors stay `img`; oracle reads the aria snapshot | fixed, observed |
| 2 | A | camera | Accessibility (2.1.1) | 4 | pan was pointer-only | Shift+arrows pan; `cameraHelp()` per preset and platform feeds label, svg and caption | fixed, observed |
| 3 | A | view cube | Accessibility (2.4.7, 2.5.8) | 3 | edge-on faces were zero-area focusable buttons | faces filtered by projected area and a 20 px box; four orbit chevrons reach hidden faces | fixed, observed (four named views + a free-orbit sliver check) |
| 4 | A | station tab | Accessibility (4.1.2), HTML content model | 3 | `role=button` span nested inside the tab button; 18 px | sibling `.dt-close` ≥ 24 px; Delete/Backspace on the tab | fixed, observed |
| 5 | A | station palette | Copy truth, UX-23 | 3 | Return/Escape claimed but unhandled from the toolbar palette | `stationKeys` at document level while the station document is active; `#sec-v` Return commits then applies | fixed, observed |
| 6 | A | named ortho views in CAD | Task completion | 3 | copy sent users to read-only 3D projections | Top/Front/Starboard presets render the editing elevation with handles in the single layout | fixed, observed |
| 7 | A | free-surface plane | Honesty (TQ2/TQ5), 1.3.1 | 3 | drawn at a false height labelled "clipped" | true h_ref; a tip → surface dimension that stays in frame carries h_tip and h_ref | fixed |
| 8 | A | station Escape | Error prevention, UX-23 | 3 | one keystroke destroyed a draft with "no undo step" | discard goes onto the undo stack; ⌘Z restores and reopens the tab; honest status | fixed, observed |
| 9 | A + I | Side elevation | Target spacing (2.5.8), IA | 3 | twist and thickness handles stacked (measured 10.5 px) | Starboard is a body plan (one row per station); thickness moved to Front 36 px below the band; pairwise spacing measured ≥ 32 px (twist 56.8 px) at 1440×900 and 1024×700 | fixed, observed |
| 10 | A (second read) | focus ring | Non-text contrast (1.4.11) | 4 | `--focus` on graphite = 2.45:1 (a surface token used on the viewport, the v3 lesson in reverse) | `--focus-viewport` / `--danger-viewport` per theme, in the audit and DESIGN.md; oracle computes the ring's contrast in three themes | fixed, observed |
| 11 | A (second read) | slider + camera | Consistency (2.1.1 side effect) | 3 | Shift+arrow on a slider also panned the camera | propagation stopped; pan branch ignores sliders | fixed, observed |
| 12 | I | CAD-04 vs direction | IA | 3 | thickness assigned to Side in the story, Front in the metaphor line | Front (with dihedral); the brief, spec and note agree | fixed |
| 13 | I | B1 verb table | IA (UX-17 oracle) | 3 | camera verbs in no row | "Viewport (one camera)" row | fixed |
| 14 | I | one draft model | Flow integrity (UX-23) | 3 | the Station document opened over an open draft; section Apply had no undo item | Edit section disabled with the reason; refusal from other-channel handles; section Apply/discard push the shared history record | fixed, observed |
| 15 | I | elevation refusal | Unhappy path (CAD-04, UX-03) | 3 | an infeasible or locked draft refused silently in Lines-plan | danger ring at the handle, reason in the accessible value and status line, Return a no-op with the reason; E-Refused / E-Locked in F2 | fixed, observed |
| 16 | I | CAD-05 edges | Unhappy paths | 2 | removal while open, selection change, ANA-22 toggle unstated | three sentences in CAD-05; built (tab closes on removal; document fixed to its station; draft hidden across the toggle) and observed | fixed, observed |
| 17 | I | naming | Findability | 2 | Side vs Port; "B" for Bottom and Back; "lever" undefined | Starboard/Port everywhere; TOP/FRT/STB/BTM/BCK/PRT; glossary rows for Station document and Twist handle | fixed |
| 18 | I | B7 | Rendered vs specified | 2 | context menu and trackpad orbit unrendered; ⇧ multiplier per axis unstated; no keyboard pan | product-only note; ⇧ per axis; Shift+arrows pan; Control-click orbit on macOS in the Rhino preset | fixed |
| 19 | I | Lines-plan below 1440 | Unhappy path | 2 | toggle available at any width | disabled with "Lines-plan needs ≥ 1440 px · single view"; observed | fixed |
| 20 | I | "free-form 3D" | Right problem | 2 | brief clause read as a free camera; no operator confirmation | open-decisions row; deferral kept with the risk named | recorded |
| 21 | A | body plan at many stations | Legibility | 2 | row pitch shrinks as stations are added | slices hidden below a 36 px row; authored stations only | fixed |
| 22 | A | `#sec-v` Return count | Copy truth | 2 | three presses after typing | compare with the model value; two presses | fixed, observed |
| 23 | A | rail label size | Token system (U3) | 2 | 10 px off-token | `--fs-caption`; rail 76 px | fixed |
| 24 | A | undo reopening the station document | Focus | 1 | the reopened document does not take focus | recorded | recorded |
| 26 | A (third read) | Front elevation label | Copy truth (2.4.6) | 2 | the svg label still said "on the band edge" | label says "drawn 36 px below the band edge" | fixed |
| 27 | A (third read) | proof record | Evidence (IO, CI6) | 2 | ring contrast, front gaps, pan and sliver results asserted but not written | group 13's proof carries `rings` (per theme), `minFrontGapLines` 39.4, `minFrontGapMinimum` 39.0, `panUnchanged`, `sliverFaces` | fixed |
| 28 | A (third read) | body plan beyond ~10 authored stations | Legibility | 1 | pitch below 32 px in a 380 px pane | recorded; a minimum row height with internal scroll is the eventual fix | recorded |
| 25 | craft gate | spacing cluster | Craft floor | 2 | "monotonous spacing ~4 px" | the 3–5 px gaps measured in the render are SVG chart geometry; the layout rhythm was varied (rail, tab row) and the finding cleared on the final run | fixed |

## 4. Rubric scorecard (DX22)

| Dimension | v3 | v4 | Note |
|---|---|---|---|
| 1 Archetype fit | 5 | 5 | ViewportWorkbench with ortho editing views and a perspective looking view |
| 2 Information architecture | 5 | 5 | Front = spanwise distributions, Starboard = body plan, camera verbs in the table — IA veto cleared |
| 3 Flow integrity | 4 | 5 | one draft model across four editing surfaces, refusals with reasons, every edit one undo item |
| 4 State completeness | 4 | 4 | free-surface plane clipped with an honest label; empty/tiny viewport states carried over |
| 5 Hierarchy | 4 | 4 | one focal viewport; the palette lives with the document it edits |
| 6 Density | 4 | 4 | |
| 7 Copy | 4 | 4 | keyboard claims backed by handlers and asserted |
| 8 Motion | 4 | 4 | |
| 9 Platform idiom | 4 | 5 | Rhino/Fusion camera and sketch-mode idioms; splines; icon rail |
| 10 Accessibility | 5 | SCORE-A11Y | lens verdict |
| 11 Craft floor | 5 | 5 | gate at its recorded floor (em-dash) on the final run |
| 12 Honesty | 5 | 5 | 3D drag deferred with the risk named; every number Illustrative |

## 5. Ranked plan

1. **Done in this run:** icons, splines, one camera with free orbit and named views, the editing elevations for the four control curves, the Station document, spec 1.2.
2. **Next:** a drag gizmo for the 3D view (axis-constrained handles) so direct 3D editing is unambiguous — then CAD-06's deferral lifts.
3. **Next:** context menus on stations, sections and control points (Edit section · Remove · Lock · Reset); area-specific state copy.
4. **At handoff (UI-T4):** the native proof pack — real toolkit accessibility of custom sliders and the view cube, pointer contract per OS, DPI.

## 6. Evidence

`docs/proof/workbench-v4-browser-check.json` · `docs/proof/ui-craft-findings-v4.json` · `DESIGN.md` §12.0c · `docs/notes/cad-editing-views.md` · spec Appendix D3.
