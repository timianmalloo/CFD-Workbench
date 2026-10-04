---
id: note-backlog-2d-section-workbench
title: "Backlog: a 2D section workbench (simulate, iterate, optimise to a goal, save)"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: backlog
tags: [backlog, section, 2d, analysis, optimisation, goal-state, my-sections]
links:
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: design-area3-analysis, rel: relates-to}
  - {to: design-m12d-catalog, rel: relates-to}
review-by: 2027-01-04
summary: >-
  Operator request (2026-10-04, Ruling 72), parked for later: tune a foil section on its own in 2D - start from a
  section, simulate and analyse it, iterate by hand or optimise toward a goal state (e.g. lowest stall speed while
  keeping efficiency), then save the result as a section - so 2D section tuning is separate from 3D wing optimisation.
review-suggested: []
---

# Backlog: a 2D section workbench

**Operator (2026-10-04):** "we should be able to do 2D simulation and optimization of a foil section... e.g. start from
a section... simulate and analyze iteratively or with a goal state in mind (e.g. optimize for minimum stall speed while
preserving efficiency) then save the section. This would allow us to separate tuning the 2D section(s) from the whole
3D optimization." Not now; recorded so it is designed as one flow later.

## The pieces it would join (already planned separately)
- **Section 2D analysis:** Area 3 slice A3b (estimator section tier, the inviscid panel method for Cp and the cavitation
  screen — DR-ANA-2) in `docs/design/area3-analysis.md`.
- **Polars (Cl, Cd, Cm, transition, Cl_max/stall):** the in-process NeuralFoil port behind SPIKE-ANA-1 (DR-ANA-1, A3c).
- **Goal states and optimisation:** spec areas 4 (Experiment setup: objective, constraints, design vector, multipoint
  set) and 5 (Run), and the COMMIT-01 ladder (surrogate → vlm-checked → cfd-checked) — a 2D section optimiser would be a
  section-only design vector with a section objective (e.g. Cl_max at a Reynolds number, L/D band), re-verified by a
  higher tier before it is trusted.
- **Saving the result:** My sections (M1.2d, ADR-0008), with provenance naming the optimisation run.
- **Editing the result:** the section editor (M1.2c) and the catalog Replace rule (Ruling 72) for putting the tuned
  section back on a wing station.

## Questions for its design
- Which tier is trusted for "stall speed" in 2D (Cl_max from a surrogate is weakly predicted; needs a stated envelope).
- Multipoint objectives (several Re/α points) and how "preserving efficiency" is expressed (a constraint band).
- The design vector: section control points, a CST parameterisation, or both; the section's point budget and the
  blend certificate's span limit when the tuned section goes back on a wing (cross-profile design, DR-XPA-1).
- Whether the workbench is a mode of the section editor or its own area.
