---
id: review-foil-editing-flow-results
title: Editable foils and flow results — review and proof
type: proof-pack
status: in-review
owner: "@timianmalloo"
tags: [review, geometry, analysis, simulation, visualization]
links:
  - {to: spec-cfd-workbench, rel: documents}
  - {to: mockup-workbench, rel: documents}
  - {to: plan-foil-editing-flow-results, rel: documents}
  - {to: proof-native-ui-workbench, rel: depends-on}
review-by: 2026-12-19
summary: Review of the specification and interactive mockup iteration for weighted foil editing, dimensional loads, water-aware sweeps and linked field replay. This evidence concerns the HTML design artifact; numerical and native product validation remain separate gates.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Revision 0.2 adds editable weighted geometry, water and force semantics, Cartesian sweeps and linked replay; reconcile consumers with the new contract." }
  - { by: mockup-workbench, on: 2026-09-19, reason: "Prototype now demonstrates weighted curve edits, water-aware loads, sample sweeps and flow replay; review dependent design and proof." }
---

# Editable foils and flow results

Date: 2026-09-19. Mode: elevate. Authors: `spec_update` for the specification and `mockup_update` for the mockup; coordinator owns integration. Independent `review` agent applies Test Architect, Data/Geometry, UX/IA, UX & Accessibility and Simplifier lenses. The authors do not clear their own vetoes.

This is a review of specification revision 0.2 and the corresponding design prototype. The [earlier UI review](ui-workbench.md) and [specification gate](specification-gate.md) concern revision 0.1. Their measurements are historical baseline evidence; regenerated proof JSON describes the current artifact.

## Grounding and direction

All eight local sources in `docs/knowledge/source-manifest.json` were rehashed and matched their archived values. The current proposal and CFD-Bench geometry/flow knowledge were read, with updated primary sources documented in the grounding ledger. G1 remains the editing archetype; G2 remains the Results specialization. The user explicitly requested editable catalog profiles, approximating weighted smoothing, coefficients and physical loads, fresh/salt conditions, two-axis sweeps and richer flow replay.

The existing palette, compact spacing and one-canvas focus remain the direction. Root extended `workbench-direction.md` before mockup visual edits. Source controls, evaluated curve and hard constraints have distinct roles. Results use one selected immutable operating point across fields, plots, metrics and the sample table. Motion represents sample selection rather than fabricated transient physics.

## Baseline measurement

The independent coordinator executed the craft detector on both named HTML files using Impeccable 3.5.0. It found four Minor findings: workbench thin-border/wide-shadow pairing, task-strip padding, workbench type hierarchy and token-preview type hierarchy. No rule was suppressed. These are measured design findings, not an accessibility verdict.

The mockup author measured 43/37/28/25/30 visible controls on Shape/Sections/Analyze/Simulate/Results at 1600×1120 before edits, with no token-pair contrast or small-target failure. The coordinator inspected baseline source: section editing was a camber fixture; analysis inputs had no displayed-data compute reader; Results rendered static pressure. That bounded earlier fixture cannot stand in for the newly requested interactions.

## Proof obligations

| Claim | Oracle and red evidence | Final evidence / confidence |
|---|---|---|
| Catalog section is editable through curves | NACA source → draft control edit → accepted changed section/loft; Cancel/Undo preserve or restore accepted state | Verified browser oracle: upper/lower offsets and Smooth weights change actual paths; keyboard identity, cancel, accepted loft and Undo checked |
| Outline and section smoothing is weighted | Weight/mode changes evaluated path, distinct control geometry; invalid weight rejected; shared derived dimensions | Verified: reversible weighted preview, invalid weight and shared area/loft checks; new outline-mode oracle failed before UI edits |
| Analysis shows coefficients and dimensional force | Speed/water drive coherent fixture loads; N↔lbf affects force display only; section units remain per span | Verified formula/display checks. The fixture holds coefficients independent of Re and states that limitation; no physical-water prediction is validated |
| Simulation creates a Cartesian sweep | Exact velocity×AoA count and paired sample labels; invalid/zero steps rejected; held water pinned | Verified: 12 explicit samples; invalid schedules rejected; editing water setup preserves existing run snapshot |
| Replay synchronizes evidence | Step/scrub/play change one sample identity across fields/table/metrics; 2D/3D changes representation; missing sample clears evidence | Verified authored and independent browser paths: case/view/field/rake, playback, modeled k and signed wall-Cf illustration; failed sample clears evidence |
| Historical results remain immutable | Current geometry/setup edits do not rewrite pinned run inputs or geometry | Verified unchanged historical SVG plus new-run revision regression, dynamic inspector/dialog/banner labels and draft-target switch guards |
| Both specification editions agree | Full source-text/hash parity and edition-label equality | Verified: regenerated HTML contains all 535 checked source blocks, 82 requirement IDs, five flows and matching source hash/revision. Stale-edition red had missing changed blocks, hashMatches=false and revisionMatches=false |
| Accessible, self-contained review artifact | Nonzero frame; focus/targets/contrast/overflow; themes and hard states; no external requests or exceptions | Verified 144 measurements/22 behavior oracles; zero measured failures. Full WCAG/native proof remains outside the HTML claim |

Frozen authored [browser report](../proof/workbench-browser-check.json): 144 task/theme/state measurements, 22 behavioral oracles, 5.818 seconds, Chrome 153.0.8010.48. Zero reported page exceptions, HTTP(S) requests, horizontal overflow, token-pair text-contrast failures or controls below the checked minimum size. These checks do not cover every possible model or every harness Cartesian combination. Results uses a 350 px canvas and a bounded scrollable evidence pane; its application frame is 1600×1120 in the wide review fixture, with metrics and replay accessible together.

[Craft scan](../proof/ui-craft-findings.json): the same four baseline Minor findings remain; zero new token/accessibility detector findings. `design-lint.py DESIGN.md --strict` passes with zero warnings. No rule was suppressed. Human visual review separately inspected the final section editor, Analyze, Simulate, 3D flow and 2D wall-shear illustration. Scalar legends, units, source identity, fixture limits and sample controls are visible. Detector results are a floor, never a design or WCAG verdict.

## Independent gate and findings

Plan: **PASS-with-conditions** from Test Architect; **PASS** from Simplifier. Required final conditions are rendered behavior evidence and this populated proof record. Minor scan-scope detail was corrected in the plan: exact file targets, nonrecursive UI scope, rule/token authority and no new allowlist.

Final specification gate: **PASS** from the independent reviewer for the functional/Data/Test/UX-IA/technical-UI contract. GEO-13's initial universal sensitivity claim was narrowed to an unlocked, nondegenerate off-curve control; locked/infeasible cases have a separate outcome. Reviewer confirmed F2–F4 recovery paths and C2–C4 traceability. No unresolved specification blocker.

Rendered mockup gate: **PASS for the HTML prototype**, with no unresolved in-scope blockers. [Independent browser evidence](../proof/flow-results-independent-check.json) includes eight broad scenarios, timed playback/invalid-input/speed/narrow-window edge probes and six focused final checks. The reviewer inspected actual SVG changes and screenshots, not only selected-button state. It checked 1024 px and 760 px layouts, keyboard focus/names, real playback stopping at a failed case, and run r13 remaining identified after current geometry becomes r14. The final reviewed HTML SHA-256 is `7b08b41483f85ce25cce8cffc9124da4226b861c204db8e5fce9759d3d81977b`, independently matched by the coordinator.

| Location | Dimension / severity when found | Evidence → correction | Outcome |
|---|---|---|---|
| GEO-13 | Test validity / Major | Universal weight-sensitivity promise included constrained/degenerate fixtures → explicit preconditions and separate lock oracle | Independently closed |
| Results composition | View hierarchy / Minor | Initial 1606 px frame hid the combined view/transport/metrics relationship → 350 px canvas and bounded keyboard-scrollable evidence pane | Final frame 1600×1120 |
| Separation states | Coverage / Major | Only unavailable wall data gave no visual example of requested diagnosis → synthetic signed wall-Cf fixture and hatched reversal, with explicit criterion and nonphysical disclosure | Available and unavailable paths checked |
| 2D view switch | Evidence identity / Major | A local-name collision could throw and retain the prior 3D image → corrected scope and actual rendered-view assertion | Browser red then green |
| Geometry draft target | User control / Major | Switching station/channel could retarget a draft → explicit cancellation before target change | Independent focused check passed |
| Run labels | Provenance / Major | Hardcoded r12 remained in banner/dialog and later sidebar despite r13 run → every selected-run label derives its pinned revision | Sidebar, inspector, banner and dialog independently rechecked |
| Partial wall variable | Evidence scope / Minor | Copy claimed a missing sample and absent loads while valid metrics remained → field-specific heading/body and retained-load assertion | Independently closed |
| Spec edition header | Consistency / Minor | Hardcoded 0.1 contradicted canonical 0.2 → derive header and assert parity | Observed stale red and rendered green |

Native accessibility, geometry continuity tolerances, screen-reader conformance and scientific validity remain explicitly unverified release obligations; an illustrative deterministic fixture cannot discharge them. The existing 22-item UI definition-of-done ledger's native/full-WCAG/per-edit-performance limitations continue to apply. This iteration supplies its changed-surface evidence above rather than claiming those future obligations passed.

Repository verification: `python3 tools/check-docs.py` passed with 16 indexed artifacts, zero graph defects/orphans/stale items/index drift and valid foundation/audit records. Fifteen review-suggested flags preserve downstream/user review; they are reported suggestions, not silently waived failures.

## Ranked outcome

Must fix in this iteration: none remaining after independent closure. The four baseline Minor craft findings remain disclosed for a later visual pass; no token or accessibility detector rule was suppressed.

Highest-leverage review task: reshape a NACA 0012 section, smooth its outline, then compare and replay the pinned velocity/angle cases while reading the force units and source labels.

Production follow-up: geometry evaluator/continuity and backend field contracts, scientific validation and native keyboard/accessibility/performance evidence. These are specified obligations, not implementation work requested in this turn.

Simplifier: **net: -2 obsolete handler bindings** and one unused chart evaluator removed; no new dependency, backend or general-purpose visualization framework. The existing G1/G2 shell and token system carry the richer interaction. Defect-class controls are recorded in the always-loaded register and executable browser/parity checks.
