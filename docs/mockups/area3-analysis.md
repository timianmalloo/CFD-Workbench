---
id: mockup-area3-analysis
title: Area 3 analysis — the Analysis surface (local tiers) in today's shell
type: design
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [mockup, area-3, analysis, vlm, loads, conditions-band, toggle, hard-states, operator-show]
links:
  - {to: design-area3-analysis, rel: documents}
  - {to: mockup-m12b2-views, rel: refines}
  - {to: mockup-status-bar, rel: relates-to}
  - {to: design-language, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: implements}
review-by: 2026-12-31
summary: >-
  Nine screens of the 1280 × 800 shell for the Example foil in Analysis: no result with depth unset, evaluating, the
  wing result Current with loading drawn on the geometry, the same result with depth unset, a station selected (the strip
  with its own envelope verdict, Re against the polar range and omissions) with the Loads tab, Historical after a CAD
  edit with a draft hidden and the Provenance tab, a failed evaluation, a result outside the method envelope, and a run that failed its integrity check. Every load number is computed in the page
  by a reference vortex lattice; every label, envelope, depth basis and omission is shown. For the operator's approval
  before any build.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
---

# Area 3 analysis — the Analysis surface

Open [`area3-analysis.html`](area3-analysis.html) over `file://`. Nine screens are stacked; the buttons top right switch
the chrome to dark and the shell to 1024 × 700. The strip under the title is the in-artifact audit (text contrast,
target size, NaN/placeholder scan, text size, and a lattice sanity check: e, Σ strips = L, zero rolling moment).

## Direction brief (Stage 1, in words before pixels)

- **Who and state on arrival:** the engineer/maker who has just shaped a wing in CAD and wants to know what it does at
  one operating point — and whether to believe it (A1 "reader of evidence").
- **Job to be done:** set speed, water, depth and α; evaluate on a local tier; read lift, induced drag and loads with
  their basis, envelope and omissions; see them on the same geometry; go back to CAD without losing the view.
- **Archetype:** G1 Parametric Modeling Workbench with G2 charts (spec C1), unchanged — reading is parallel (results,
  layers, charts) but entering is serial (one conditions band, one Evaluate), so the workbench archetype holds.
- **Adjectives:** quiet (not busy) · direct (not form-driven) · honest (not persuasive). Anti-goals: a dashboard of
  cards; a number without its basis; a colour that says "good".
- **References:** XFLR5 / AVL (loading plots, station tables — borrow the conventions, avoid the aircraft framing);
  ParaView (legend fields, table twin); the shipped CFD-Workbench shell (m12b2 views, V2 strip) — the surface must read
  as the same application.
- **Type, colour, space:** the shell's tokens unchanged (DESIGN.md); batlow for magnitudes on the graphite viewport;
  the one new chrome row is the 40 px conditions band (DESIGN.md §4 "Conditions band" row, already specified).
- **Triggered standards:** UI-T1 fires (expert quantities → G1/G2, batlow, legends, table twins); UI-T2 does not (no
  generated imagery); UI-T3 does not for this mockup (the "Ask about this calculation" entry is M5, not A3a); UI-T4
  fires at handoff (Avalonia) — native proof belongs to the build, not to this HTML.

## What the operator is asked

Approve the look and the states before any build (memory rule: operator sees the mockup before build). The four
choices at the foot of the page map to DR-ANA-6 (explicit Evaluate), DR-ANA-8 (toggle place and shortcut), DR-ANA-1/2
(section numbers Unavailable until a method is chosen) and the out-of-page list.

## Measurements (Stage 3)

- In-artifact audit, light and dark, 1280 × 800 and 1024 × 700: text contrast ≥ 4.5:1 (lowest 5.66 light, 6.51 dark);
  targets ≥ 24 px; no NaN or placeholder; text ≥ 11 px; no row overflow; lattice sane (e 1.003 at 64 × 4).
- `ui-craft-gate.py`: one Minor — `side-tab` on `.sb .msg.error`, the 3 px inset rail. It is the DESIGN.md Status strip
  row's specified error rail ("a warning or error also draws a 3 px inset rail of its colour at the strip's left"), so
  it is kept as a recorded deviation (CD16). A clean detector run is a floor, never a verdict.
- Rev 3 (repair cycle 2, 2026-10-03): the audit is clean again in light and dark at 1280 × 800 and 1024 × 700 (lowest
  contrast 5.66 light, 6.51 dark; no page errors). Properties now scrolls (`overflow-y: auto`) instead of clipping: at
  1280 × 800 the failed screen scrolls by 35 px; at 1024 × 700 four screens scroll by 7–135 px. The Conditions group is
  closed by default (the band shows the same values).
- UX & Accessibility lens: PASS WITH CONDITIONS, no veto; repair cycle 1 folded all five Majors
  (`docs/reviews/area3-analysis-personas.md` § Mockup review).
- The page's own lattice check caught two defects during authoring (Trefftz downwash at 1/(4π) gave e 2.03; 32 × 6
  gave e 1.013) — both fixed, and the second became DR-ANA-7's evidence.
