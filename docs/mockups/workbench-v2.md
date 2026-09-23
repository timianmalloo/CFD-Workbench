---
id: mockup-workbench-v2
title: CFD-Workbench interactive design mockup v2 — seven areas
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, setup, cad, analysis, experiment, run, results, export, v2]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench-v1, rel: supersedes}
  - {to: review-ui-workbench-v2, rel: relates-to}
  - {to: decision-seven-areas, rel: relates-to}
  - {to: proof-native-ui-workbench, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-20
summary: >-
  Self-contained HTML workbench built against specification v1.1: an area strip in flow order — Setup · CAD ·
  Analysis · Experiment · Run · Results · Export — with readiness chips, a prompt entry in every area whose
  output is that area's typed proposal, one canvas shared by CAD and Analysis, a process console for Run over a
  stepped fixture, and Results as sequences over admitted samples with every layer's basis. Run and Results
  render their full target state and carry the "gated (SPIKE-03/04)" chip. Illustrative throughout; no kernel,
  solver, file I/O or model call.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# Interactive workbench mockup v2 — seven areas

[Open the mockup](workbench-v2.html) · [Specification v1.1](../specs/cfd-workbench-v1.html) · [Design language](../../DESIGN.md) · [Review](../reviews/ui-workbench-v2.md) · [Direction brief](../design/workbench-direction.md) · [Browser evidence](../proof/workbench-v2-browser-check.json) · [Mockup v1 (superseded)](workbench-v1.md)

Open the HTML directly in a browser; no build, server, key, solver, internet or external font is needed, and the
page makes zero external requests. The review bar at the top is review chrome and never ships.

## The seven areas as built

1. **Setup** — "Describe a starting design" (a fixture setup-seed proposal with per-field Stated · Inferred ·
   Defaulted and a rejected field) beside the parameter form: purpose (eight), the four general dimensions as
   **soft targets** with weights and the seeded value's deviation, rider mass, water; the conflict string; a
   seeded preview; the goal state, operating points (the seven pinned triples computed live) and feasibility.
2. **CAD** — the Outline (LE and TE rails), Twist, Dihedral/anhedral and Thickness curves selected by one
   control; add a station at the hovered η (deviation 0 under rule A) and remove an authored one (refit deviation
   reported); the section editor opens from a station in a dialog; the catalog panel; the genuine weighted
   least-squares Smooth with the GEO-13 property; lines-plan; ghost.
3. **Analysis** — the same canvas with a layer list: per-strip lift vectors and the resultant, spanwise loading
   strips in batlow, the depth band with the ventilation margin; the 2D section with Cp in vik pinned at 0; the
   Ncrit and surface-state bands; the cavitation and ventilation strings; the Not-assessed Loads panel; compare.
   The **CAD ⇄ Analysis** toggle is navigation: the strip follows, camera and selection persist, an open preview
   is hidden and restored.
4. **Experiment** — sweep grid with a 12-case preview carrying Re, q, h/c, Fr_h, σ and an estimate that says
   "Not recorded" until measured; invalid samples blocked with the input named; the optimize form with the
   multipoint set, A_cav, the design vector with bounds and frozen flags, tier, budget and seed, and the
   single-point refusal string; Queue makes the version immutable; the area has no Run verb.
5. **Run** — backend environment (substrate, pinned digest, smoke test, capability record, limits) with "Prepare
   my environment" as allow-listed steps carrying only a step id, each with the bound command, consequence and
   terms, consent per step, and a refused step; the queue with per-case state strings; the console with the
   state machine, residual history on a log axis, force history with the averaging window, mesh-gate bars against
   named thresholds, resource meters with "Not recorded", Cancel (to the substrate), Retry sample, "Explain this
   failure". The run is a fixture stepped explicitly — nothing autoplays and no solver runs.
6. **Results** — sample list and layer list from a fixture manifest with absent reasons; the viewport with the
   Cp flood, isolines, probe, streamlines (the moving dash labelled as an affordance and stopped under reduced
   motion), a separation layer only where τ_w exists and "No supported criterion" where it does not, force vectors
   from the forces file, vortex-core candidates; the timeline as a sequence over admitted samples with the held
   variable; small multiples, a metric-vs-α plot with gaps, a difference flood pinned at zero; candidates on the
   COMMIT-01 ladder with a Pareto view and Accept → a CAD draft.
7. **Export** — the matrix with STL/3MF, gated STEP, Fusion-ready STEP (no native Fusion file promised), 3DM via
   rhino3dm, DAT, AVL, CSV, and the safety string.

A **prompt entry** sits under the inspector in every area with the area's fixed entry name and proposal kind;
no key, unevaluated model and cap states render their strings; Export says "No assistant action here".

Every number is Illustrative and says so. The evaluator is an illustrative implementation of the A4.2 contract;
the physics are labelled fixtures; the run and the results are fixtures; the Fusion and Rhino exports are rows,
not files. Native accessibility trees, file I/O, solvers and model calls are outside this artifact.

## Executable control

`node tools/check-mockup-v2.mjs [<node_modules dir with playwright>]` walks the seven areas in three themes,
eight states and five viewports (1024 · 1280 · 1440 · 1600 · 640) asserting contrast, target sizes, no NaN or
placeholder and text ≥ 12 CSS px, then the interaction contracts of the spec's new stories: area strip chips,
both Setup roads, the four curves and the Outline rails, station add/remove, GEO-13, focus after a nudge, the
toggle preserving camera and selection with the preview hidden, analysis layers with accessible names, the
experiment refusal and Queue, the run state machine with cancel and retry, the results layers, replay and reduced
motion, the export rows, and every prompt entry. Evidence lands in `docs/proof/workbench-v2-browser-check.json`;
`ui-craft-gate.py` and `design-lint.py --strict` run beside it.

Best first review: Setup → change max span to 800 and read the conflict string → Seed from parameters → CAD →
add a station at the hovered η → toggle to Analysis and watch the vectors appear over the same view → Experiment →
choose optimize and pick the single-point objective → Run → step the fixture through the mesh gate and cancel →
Results → select s04 and read "No supported criterion".
