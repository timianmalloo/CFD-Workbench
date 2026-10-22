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

## A3d-1 repair (cycle 1)

Review conditions 1, 4, 5 and 6 of the A3d-1 verdict (condition 2 and 3 are A3d-2 work).

- **Condition 1, twist.** Added. The zero-lift term is `alpha - (alphaL0 - twist)`, both chord-weighted means over the
  run's stations. Chord and twist are the placed frame's (`Placement.Sections`, `StationFrame.ChordMeters` and
  `TwistDegrees`), the source the lattice reads (`VortexLattice` line 124: `mid.Frame.TwistDegrees`). Sign convention:
  nose-up positive; the lattice rotates the section tangent by +twist (`SlopeNormal`) and the strip alpha_eff is
  `alpha + twist - alpha_i` (`MethodRecord`), so a positive twist raises CL. No second twist definition, no seam request.
  Method id and version stay `cfdw.estimator.wing` 1.0.0. Design 2.6 states the term (one paragraph edited).
- **Condition 4, citation.** The class comment cites DATCOM 4.1.3.2 for the mid-chord form, notes that Raymer 12.6 is on
  the maximum-thickness-line sweep, and states the straight root-to-tip mid-chord line is an approximation.
- **Condition 5, the value check** `WingEstimator_Derive_SweptCamberedTwisted_HandValues`. Fixture: rails linear in eta
  (leading 200 eta mm, trailing 120 + 300 eta mm, half span 450 mm), twist -4 eta degrees, one cambered profile. By hand:
  tan(mid-chord sweep) = 250/450 = 5/9 (leading-edge line 4/9); chords 120, 170, 220 mm and twists 0, -2, -4 degrees at
  eta 0, 0.5, 1 give a chord-weighted mean twist of -1220/510 = -2.3921568627 degrees (unweighted -2); A = 0.81/0.153;
  alpha_L0 is the section estimator's (asserted equal at the three stations, negative for positive camber); at alpha 4
  degrees the check's CL is 0.2516076147. The example foil gets `WingEstimator_Derive_ExampleFoilWashout_LowersCl`.
- **Condition 6**: `Service_EvaluateEstimator_UncertifiedGeometry_RefusedNothingRecorded`,
  `Service_EvaluateEstimator_FailedPath_RecordsFailedRowNoDiagnostics` and
  `Service_EvaluateEstimator_DoesNotCancelVlmInFlight`. No certified wing makes the derivation fail, so the Failed-row
  check replaces the derivation through one internal property (`AnalysisService.EstimatorDerivation`, visible to the
  Analysis tests only). 6(c) was done as a check, not by inspection.

Red on the old code (the commit before the repair): `WingEstimator_Derive_SweptCamberedTwisted_HandValues` ->
`expected 0.2516076147; actual 0.4173764551` (twist ignored); `WingEstimator_Derive_ExampleFoilWashout_LowersCl` ->
`washout lowers CL; expected True; actual False`; `Service_EvaluateEstimator_FailedPath_...` -> compile red
(`EstimatorDerivation` did not exist) and, before the seam, `the row failed; expected True; actual False`. The
uncertified check and the no-cancel check pass on old and new code; their mutants below kill them.

Green: `CFD_TEST_ONLY=WingEstimator_,EstimatorRun_,VlmRun_,UnknownTier_,Service_EvaluateEstimator_` -> eleven `PASS`,
`RESULT failures=0`.

| Mutant | Red line |
|---|---|
| sweep forced to 0 | `expected 0.2516076147; actual 0.2752150970` |
| leading-edge sweep | `expected 0.2516076147; actual 0.2593146003` |
| zero-lift sign flipped | `expected 0.2516076147; actual -0.0287708129` |
| unweighted mean | `expected 0.2516076147; actual 0.2787828345` |
| twist dropped | `expected 0.2516076147; actual 0.4173764551`; the washout check also red |
| twist sign wrong | `expected 0.2516076147; actual 0.5831452955`; the washout check also red |
| estimator certification check removed | `Service_EvaluateEstimator_UncertifiedGeometry_...: expected ContractError; the evaluation returned` |
| Failed-row catch removed | `Service_EvaluateEstimator_FailedPath_...: ContractError: forced` |
| estimator takes the latest-wins slot | `the VLM compute token was cancelled by the estimator; expected False; actual True` |

## Node A3d-2: Compare

Not started. The older-reader receipt (7.3), the measured bytes per pinned run and per record, and the copy id mapping
(each `A3D-COPY-n` to the next free `COPY-` id after COPY-521) are recorded here by that node.
