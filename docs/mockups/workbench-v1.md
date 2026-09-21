---
id: mockup-workbench-v1
title: CFD-Workbench interactive design mockup v1
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, hydrofoil, curves, stations, analysis, v1]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-language, rel: depends-on}
  - {to: workbench-direction, rel: refines}
  - {to: mockup-workbench, rel: supersedes}
  - {to: review-ui-workbench-v1, rel: relates-to}
  - {to: proof-native-ui-workbench, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
review-by: 2027-03-19
summary: >-
  Self-contained HTML workbench built against specification v1: Brief with the seven-point goal state computed
  live, Shape with a genuine constrained weighted least-squares B-spline evaluator, Sections with admission
  classes and a two-layout DAT detector, Analyze where every number carries its basis (tier, depth, Ncrit band,
  surface state, omissions, fixed strings), a Checks drawer, a gated export dialog and the assistant's honest
  states. A review harness switches persona, viewport, state, theme, density, capability, navigation preset,
  modifier scheme, trackpad mode and reduced motion. Illustrative throughout; no kernel, solver or file I/O.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Revision 1.1 (2026-09-21): seven first-class areas, AI prompt entry per area, Experiment/Run/Results contracts — re-read against the new stories and the verb × area table." }
---

# Interactive workbench mockup v1

[Open the mockup](workbench-v1.html) · [Specification v1](../specs/cfd-workbench-v1.html) · [Design language](../../DESIGN.md) · [Review](../reviews/ui-workbench-v1.md) · [Direction brief](../design/workbench-direction.md) · [Browser evidence](../proof/workbench-v1-browser-check.json)

Open the HTML directly in a browser. No build, server, API key, solver, internet or external font is needed;
the page makes zero external requests. The review bar at the top is review chrome and never ships. Everything
inside the window is the design under review.

## What it demonstrates (and what it does not)

- **Brief.** The goal state of A5.5 with per-field Stated · Inferred · Flagged provenance; the seven operating
  points with q, required CL and σ computed live from the pinned inputs of GOAL-02 (1 kn = 0.514444 m/s,
  ρ 1026.02 kg/m³, p_v 1.671 kPa, p_atm 101.325 kPa, W = 1050 N, h_ref 0.5 m) on the two Examples; editable
  h_ref inside the depth band; the Constraint set the discipline preset wrote, editable per row; the
  feasibility matrix with satisfied · violated · Unavailable-with-reason cells, a tier chip and both Ncrit.
- **Shape.** Five channels as clamped degree-5 B-splines. *Through points* interpolates the anchors;
  *Smooth · weighted controls* is a genuine constrained weighted least-squares solve with the root-mirror
  tangent and the tip value as KKT equality rows (A4.2). The GEO-13 monotone-approach property is observed
  by the browser oracle, not claimed. Dashed control polygon, distinct influence-control and evaluated-station
  glyphs, lock glyphs with their source, live comb, Tracing pointer readout, three nudge steps, unit
  expressions with echo and dimensional errors, Return/Esc modal rule, Apply as one undo item that detaches
  the recipe and makes the run Historical, lines-plan layout at 1440 px, ghost of a prior revision.
- **Sections.** Catalog with GEN · Pending admission — reason · LINK chips and design-point metadata (Unknown
  where unsourced); a source-linked section draft with deviation, locks and the conversion residual; the
  two-layout DAT detector with the Lednicer-forced-through-Selig rejection.
- **Analyze.** Conditions band whose operating point comes from a Goal state point or is Custom; derived q,
  Re, h/c, Fr_h, σ, V_crit; every quantity with its tier chip and basis; the Ncrit band and surface-state
  band; the A5.1 deep-water string when h/c < 5; the cavitation and ventilation strings verbatim; the JMSA
  correction *beside* the deep-water value; the Loads panel in the **Structural: Not assessed** state with
  the safety string; comparison with reference-quantity blocking and a stored Discrepancy record; Cp coloured
  by vik pinned at Cp = 0 and spanwise loading strips in batlow, each with legend fields and a table twin.
- **Checks, Export, Assistant, Settings.** DRC findings with rule id, version and severity; exclusion with a
  reason stored on the finding; the export matrix with STEP gated by its string and the export safety
  string; the assistant's no-key, unevaluated-model, rejected-field, withheld-numeral and no-action states;
  the TE floor setting with its label, two navigation presets, the modifier scheme.

Every result view carries **Illustrative — not computed for this design**. The section geometry is a
closed-form NACA 2409 stand-in scaled to the channel t/c, labelled as such in the inspector; the product
generates 66-209 through the TM 4741 generator. The polar, Cp and wing numbers are labelled fixtures. The
evaluator is an illustrative implementation of the A4.2 contract, not the production kernel; it certifies
nothing about production fairness or continuity. Native accessibility trees, file I/O, solvers and AI
requests are outside this artifact (see the [native proof obligations](../proof/native-ui-workbench.md)).

## Executable control

`node tools/check-mockup-v1.mjs [<node_modules dir with playwright>]` launches local Chrome, walks every
destination in three themes and eight states, and asserts the interaction contracts named in the spec:
the seven GOAL-02 triples, the GEO-13 monotone weight property, the modal rule and the unchanged undo stack on
Cancel, Apply → Historical → Undo → Current, the depth-unset absence strings, the A5.1/A5.3/A5.4/A5.6 strings
verbatim, reference-quantity blocking, the ITTC range string, admission classes and the DAT rejection, the
Checks exclusion, the gated STEP row and the TE-floor repeat, the assistant states by capability, reduced
motion, the modifier scheme, zero external requests and zero page errors. Evidence lands in
`docs/proof/workbench-v1-browser-check.json`. The deterministic craft gate
(`ui-craft-gate.py docs/mockups/workbench-v1.html`) and `design-lint.py DESIGN.md --strict` are run beside it.

Best first review: in Shape switch to Smooth, focus the fourth control and step its weight 1 → 2 → 4 → 8 → 16,
watching the evaluated station marker approach the control without reaching it; press Escape; type
`#root_chord * 0.35` into the value field and Apply; open Analyze, choose a Custom point at 0.30 m and read the
deep-water string beside the numbers; open Export and read what STEP says.
