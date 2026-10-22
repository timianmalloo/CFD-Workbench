---
id: proof-a3d-compare
title: "A3d Compare build proof: red and green lines, mutants, receipts"
type: proof-pack
status: draft
owner: "@track-a3-compare-build"
phase: implementation
tags: [a3d, compare, estimator, analysis, discrepancy-record]
links:
  - { to: design-a3d-compare, rel: implements }
review-by: "2027-01-31"
summary: >-
  The observed red and green lines of the A3d Compare build, by node. Node A3d-1 (the wing estimator tier) is recorded;
  the A3d-2 sections (older-reader receipt, measured bytes, copy id mapping) are filled by that node.
---

# A3d Compare build proof

Design `docs/design/a3d-compare.md` (section numbers below are its). Session `a3-compare-build`. Logs are kept outside
the tree; the last lines that matter are copied here.

## Node A3d-1: the wing estimator tier (design 2.6, 4.1, 7.0; Ruling 250 Q1)

Baseline `bash tools/run-tests.sh` at `main` 93b5dfde: Core 762 passed, Analysis 260 passed, Desktop 782 passed,
Cli 19 passed; TESTS-GREEN.

### Red (the old code)

- Compile red: `WingEstimatorTests.cs(34,28): error CS0103: The name 'WingEstimator' does not exist in the current
  context` (six errors; the type did not exist). This is the red for `WingEstimator_HelmboldDiederich_SlopeAndInducedDrag`.
- With the type present and `RunRecord.CheckRow` and `AnalysisService` still old, the harness
  (`CFD_TEST_ONLY=<name>`) printed:
  - `FAIL EstimatorRun_Completed_NoDiagnostics_Accepted ContractError: DOC-SCHEMA`
  - `FAIL EstimatorRun_Failed_WithDiagnostics_Refused InvalidOperationException: expected refusal DOC-SCHEMA; none`
  - `FAIL UnknownTier_Refused InvalidOperationException: expected refusal DOC-SCHEMA; none`
  - `FAIL Service_EvaluateEstimator_RecordsIdempotent ContractError: A3a evaluates the VLM + strip tier only`
  - `PASS VlmRun_Completed_NoDiagnostics_StillRefused` (passes on the old rule, as the design says; its mutant below
    turns it red).

### Green (the new code)

`CFD_TEST_ONLY=WingEstimator_,EstimatorRun_,VlmRun_,UnknownTier_,Service_EvaluateEstimator_` on the Analysis harness:
six `PASS` lines and `RESULT failures=0`. Full oracle: Core 762, Analysis 266, Desktop 782, Cli 19, TESTS-GREEN.

### Mutants (each applied, the one check run, red recorded, reverted; `git diff` equal before and after)

| Mutant | Check | Red line |
|---|---|---|
| the `+ 4` term dropped | `WingEstimator_HelmboldDiederich_SlopeAndInducedDrag` | `CL_alpha at A 7.5, sweep 0 expected 4.8272350086; actual 4.9604094530` |
| the sweep term dropped | same | `CL_alpha at A 7.5, sweep 20 expected 4.6072951356; actual 4.8272350086` |
| CDi with e not 1 (0.9) | same | `CDi = CL^2 / (pi A) expected 0.0075314829; actual 0.0083683143` |
| the tier-keyed equality deleted (`vlm-strip => true`) | `VlmRun_Completed_NoDiagnostics_StillRefused` | `expected refusal DOC-SCHEMA; none` |
| the estimator clause keyed on Completed only | `EstimatorRun_Failed_WithDiagnostics_Refused` | `expected refusal DOC-SCHEMA; none` |
| an open tier set (`_ => true`) | `UnknownTier_Refused` | `expected refusal DOC-SCHEMA; none` |
| the idempotent lookups removed (both: before compute and under the lock) | `Service_EvaluateEstimator_RecordsIdempotent` | `ContractError: DOC-RUN-KEY` |

Finding on the last mutant: removing only the first lookup stays green, because the lookup under the lock catches the
repeat. That is the intended second guard (two services on one session); the check goes red when both are removed.

### Notes

- One full run of the Desktop suite failed three checks once (`UI_DEAD_CONTROL_PointAndWingControlsHaveActions` with
  `ContractError: DSL-LIMIT`, `--shell-window`, and the wrapper), and the next full run was green with no source change
  between them. The change touches no Desktop file. Recorded as a load-dependent flake, not diagnosed.
- Two local failures were real and fixed before the green run: degrees-to-radians may live in `Placement.cs` only
  (`PlacementRule_RadiansConstant_SingleSiteInSource`), so the estimator uses `VortexLattice.ToRadians` and the tangent
  of the sweep directly; and every `ANA-` code needs approved text or a not-a-cell prefix
  (`Labels_ReasonCodes_AllApprovedTextNoRawCode`), so the estimator's input refusal is `ANA-INPUT-ESTIMATOR`.
- Design 2.6 `assume:` on the mid-chord sweep is resolved: `WingEstimates` does not give it, so `WingEstimator` reads
  the root and tip stations from `Placement.Sections` (the one placement authority).
- The estimator's behaviour on a swept hydrofoil stays Inferred: the 20 degree row checks the formula, not the physics.
- The estimator takes no latest-wins slot, so an estimator evaluation never cancels a VLM evaluation in flight on the
  same scope.

## Node A3d-2: Compare

Not started. The older-reader receipt (7.3), the measured bytes per pinned run and per record, and the copy id mapping
(each `A3D-COPY-n` to the next free `COPY-` id after COPY-521) are recorded here by that node.
