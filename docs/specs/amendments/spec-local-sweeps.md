---
id: spec-amendment-local-sweeps
title: "Spec amendment note — Area 4 local-tier sweeps, performance curves, and retention (XS-01, XS-03, ANA-16, RES-04)"
type: spec
status: proposed
owner: "@timianmalloo"
phase: specification
tags: [spec, amendments, sweeps, experiment, analysis, local-tier, retention, performance-curves]
links:
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: design-local-sweeps, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-12-19"
summary: >-
  Three targeted proposed clarifications to cfd-workbench-v1 for executing Area 4 sweeps over local in-process tiers
  (VLM + strip and wing Estimator) without requiring an OpenFOAM/SU2 backend: (1) local in-process attempt lifecycle
  and runId linkage, (2) wing-only CD/L/D Ncrit band and Cm datum/availability on local performance curves, and
  (3) non-archived sweep run pinning plus explicit append-only archive/unpin under the 32,000,000-byte project cap.
  Existing XS-01, XS-03, ANA-08, ANA-16, RES-04, and LAB-01 requirements remain unchanged.
---

# Spec amendment note — Area 4 local-tier sweeps & performance curves

**For:** the spec owner (`@timianmalloo`). **Spec:** [cfd-workbench-v1](../cfd-workbench-v1.md), revision 1.7.6.
**Design:** [`docs/design/local-sweeps.md`](../../design/local-sweeps.md). **Status:** all three rows are **proposed**
for operator review alongside `DR-SWP-1..6` and [`docs/mockups/local-sweeps.html`](../../mockups/local-sweeps.html).

## Scope statement

Core stories `XS-01`, `XS-03`, `ANA-08`, `ANA-16`, `RES-04`, and `LAB-01` require **no weakening or behavioural
change**. The three proposed clarifications below only resolve boundary details between the existing Area 3 local
analysis storage contract (`ADR-0011`, `Ruling 109`, `Ruling 161`, `Ruling 263`) and executing `Experiment` sweeps on
the `local · in-process` pin (VLM + strip and Estimator tiers) ahead of the OpenFOAM/SU2 backend (`SPIKE-03/04`).

## Proposed clarifications (3)

| ID | Spec clause | Before (quoted from rev 1.7.6) | After (exact proposed addition / clarification) | Source | Status |
|---|---|---|---|---|---|
| **AM-SWP-1** | A3.1 Case schedule (:255) & B6b Flow F7 node `S2` (:1751) | `one Sweep attempt fact per case per attempt. An attempt references at most one Analysis run; a failed attempt references none; the selected attempt is the latest one, and a failed latest attempt is a gap (ANA-08, RUN-06)` · `S2[Evaluate in process; attempts and evidence as for a backend]` | For a `local · in-process` sweep (`vlm-strip` or `estimator`), each attempt progresses through `Pending → Running → Completed \| Failed \| Cancelled` (omitting CFD-only `Meshing`, `Mesh gate`, `Harvesting`, and `Field evidence` stages). A `Completed` attempt references its intact `Completed` `AnalysisRun` in `analysis.runs` by `runId` and never duplicates strip data; a `Failed` or `Cancelled` attempt stores `runId = null` with its stable failure code and reason on its terminal `AttemptStateEvent`. | `design-local-sweeps` §1.2, §2.3; `ADR-0011` §2 | proposed |
| **AM-SWP-2** | A6 `ANA-16` (:1224) & `RES-04` (:1276) | `CL, CD, L/D and Cm plot vs α and vs speed with held variables, datum and the goal-state bands (take-off, cavitation ceiling)` · `small multiples per sample, CL/CD/L/D/Cm vs α and vs speed with gaps for failed cases, and a zero-pinned difference flood between two admitted samples of one series are available` | On `local · in-process` sweeps: (1) `CD` and `L/D` follow A5.6 (Ruling 109) — wing-only `CD` and wing-only `CL/CD` plot as the `Ncrit 2–4` band when polar drag is present (never averaged), `CDi (Trefftz)` plots as a separate labelled series, and craft `Total drag` / craft `CL/CD` stay `Unavailable`; (2) `Cm` is derived from `Σ StripLoad.My / (q · S_ref · c_ref)` about `Reference.MomentDatum` (frame origin, positive nose-up, A4.7) when `NChord ≥ 2`, reads `Not resolved · 1 chordwise panel` when `NChord == 1` (Ruling 161), and reads `Unavailable` on the Estimator tier; (3) zero-pinned 3D field difference floods remain gated on Area 5/6 (`SPIKE-03/04`), while local sweeps ship performance curves, small-multiples case cards, linked selection (`ANA-08`), and table twins. | `design-local-sweeps` §2.4; Ruling 109; Ruling 161; `VortexLattice.cs:261` | proposed |
| **AM-SWP-3** | A3.4 History and freshness (:341–351) & `ADR-0011` §7 | `Never prune a run that a Discrepancy record references (A3d).` *(ADR-0011 §7)* | A non-archived `SweepExperiment` pins the `AnalysisRun` referenced by each case's selected (latest) `Completed` attempt against save-time pruning, on top of the per-tier retention quota. Recording an append-only `Archived` event on an experiment preserves its definition and attempt history while releasing its run-pinning claim so save-time retention may reclaim unreachable strip rows under `NativeProject.MaxBytes` (32,000,000 bytes, Ruling 263), leaving `PrunedRun` tombstones that render `Unavailable — pruned at save`. | `design-local-sweeps` §1.5; `DR-SWP-2`; Ruling 263 | proposed |

## Unchanged contracts

- `XS-01` (preview 12 unique cases with derived `Re`, `q`, `h/c`, `Fr_h`, `σ` and resource estimate or `"Not recorded"`; invalid/duplicate/non-finite samples block queueing with the input named; post-preview geometry/water change invalidates Draft preview, never a Queued experiment).
- `XS-03` (Experiment setup has no Run verb; Queue seals an immutable version and copies the backend pin `local · in-process`).
- `ANA-07` (freshness is derived solely by recomputed run-key equality; nothing writes a freshness flag).
- `ANA-19` (depth unset → `σ`, `Fr_h`, `V_crit` read `Unavailable — depth not set` and fixed label reads `free surface not modelled`, never `deep water`).
- `LAB-01` (forbidden words lint remains strict and unchanged).
