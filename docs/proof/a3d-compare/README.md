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

Order of the build: Core and Analysis, CLI, Desktop. Each half has its red commit (the checks, red on the old tree) and its
green commit. Red on the old tree, as the compiler says it: `CompareTests.cs`: `CS0246 The type or namespace name
'Coefficients' could not be found` and `'DiscrepancyRecord'` (8 errors); `DiscrepancyCliTests.cs`: `CS0117 'Cli' does not
contain a definition for 'DiscrepancyListing'`; `ComparePaneTests.cs`: `CS0246 'ComparePane' could not be found`.

Green: Analysis harness `CFD_TEST_ONLY=Compare_,Discrepancy_,Retention_RunReferenced,NativeReader_` -> 22 `PASS`,
`RESULT failures=0`; Desktop `--analysis` Compare checks 12 `PASS`; full `bash tools/run-tests.sh` -> TESTS-GREEN (Core 762,
Analysis 294, Desktop 794, Cli 21 passed).

### Mutants (each applied, its checks run, red recorded, reverted)

| Mutant | Red line (first failing check) |
|---|---|
| delta by the compared value | `shown dCL, base = VLM expected 0.0224653479; actual 0.0219717449` (also the delta and reversed-view checks) |
| delta by the mean | `base 0.4035, compared 0.4120 expected 0.0210656753; actual 0.0208461067` |
| delta by the signed base | `negative base: (c - b) / |b| expected 0.5; actual -0.5` (and `row 2 canonical ... actual 1.0`) |
| record not appended | `records expected 1; actual 0` |
| stored in the shown orientation, key not canonical | `ContractError: DOC-SCHEMA` in the tier, same-key and stores-one checks |
| lookup skipped | `Compare_SameKeyTwice_ReadNotNewRecord ContractError: DOC-DISCREPANCY-ID` |
| estimator e = 1 enters the delta | `not compared code expected ANA-CMP-ESTIMATOR-E; actual (none)` |
| envelope row dropped | `every section 2.1 field; expected ... Envelope ...` and the pane check `Sequence contains no matching element` |
| no Undefined at base 0 | `base 0 expected ANA-CMP-BASE-ZERO; actual ANA-CMP-BASE-FLOOR` |
| e floor only | `CL Undefined makes e Undefined expected ANA-CMP-E-ILL-CONDITIONED; actual (none)` |
| one percent tolerance | `S_ref near-equal: one block; expected 1; actual 0` |
| epsilon compare | `S_ref last ulp: one block; expected 1; actual 0` |
| a point field left out | `pAtm: one block; expected 1; actual 0` |
| water reported as point | `temperatureC is a water reason expected Water; actual Point` |
| a Preferred or mean member | `CompareOutputRow.Mean names a combined value; expected False; actual True` |
| discrepancies dropped on load | `records after reopen expected 1; actual 0` |
| member written as an empty list | `Discrepancy_OmittedWhenEmpty_...: ContractError: DOC-SCHEMA` |
| keep line removed | `the referenced run survives the save; expected True; actual False` |
| preflight skipped | `Discrepancy_PinnedRuns_...: ContractError: DOC-SIZE` (the cap is only reached by the unchecked record) |
| orientation check removed | `expected DOC-SCHEMA for a tier pair stored higher to lower; none` |
| stored hash trusted | `a changed line, the stored hash kept expected PayloadFailedCheck; actual NoLongerReproduces` |
| verified under the current rule | `an unknown rule version ... expected RuleUnknown; actual Intact` |
| dispatch to the newest rule only | `a 1.0.0 record under a build whose newest rule is 1.1.0 expected Intact; actual NoLongerReproduces` |
| analysis member open | `an unknown analysis member is refused expected DOC-UNSUPPORTED-FIELD; actual DOC-SCHEMA` |
| save report removed | `Discrepancy_SaveEvent_...: Sequence contains no matching element` |
| CLI: stored lines printed without recompute | `a tampered line is recomputed, not printed as Intact` |
| CLI: `discrepancies: []` for none | `no discrepancies member for a document with no record` |
| Desktop: headline from CDi | `the headline quotes the CL delta, not CDi's; expected True; actual False` |
| Desktop: empty state replaced | `the estimator button, enabled for a VLM run; expected True; actual False` |
| Desktop: delta rendered under the block | `the block copy; expected True; actual False` |
| Desktop: water shown with the point copy | `the water copy; expected True; actual False` |
| Desktop: delta hidden for a Historical run | `the Historical note; expected True; actual False` |
| Desktop: a Failed option enabled | `a Failed option is disabled; expected False; actual True` |
| Desktop: CDi cell without A3D-COPY-26 | `the CDi cell carries the elliptic-loading label in the cell; expected True; actual False` |
| Desktop: delta beside the error card | `the error card copy; expected True; actual False` |
| Desktop: focus not on the Base picker | `focus lands on the Base picker; expected True; actual False` |
| Desktop: a picker without a name | `Base picker name expected Base run; actual (empty)` |

The reversed-view theory has no separate mutant: "reversed view shows -d" and "by -d/(1+d)" are not code paths (the reversed view is
the same function called with the roles swapped), so the rows are killed by the divide-by-compared and divide-by-signed-base
mutants, which the negative-base row (base -1, compared -2) separates. "Runs rewritten" has no mutant either: nothing writes a
run; the check pins the runs untouched.

### The Analysis panel reads VLM + strip runs only (review condition 2)

`AnalysisPanel_ReadsOnlyVlmRuns_AfterEstimatorRunRecorded`. On today's behaviour (the controller's `Runs.LastOrDefault()` and
no tier guard in the projection, both reverted for the run): `the panel still shows the VLM run expected 4e05821...;
actual 89d1a3f...` (the panel showed the estimator row). With the controller filter and the projection guard it passes.

### Measured (Verified; replaces the Inferred figures of design 1.8 and 10)

From `Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` (`MEASURED` line, 128 strips per run with realistic digits):

- **bytes per pinned VLM + strip run (128 strips): 109,991** in the indented document (the design's Inferred figure was
  about 60,000). About **72 such runs** fit in `NativeProject.MaxBytes` (8,000,000), not 133.
- **bytes per record (three lines): 859** (Inferred: about 530). Records are still not the constraint.
- On that basis a document reaches `DOC-SIZE` through pinned runs sooner than design 1.8 said: about 72 VLM runs in all, so the
  DR-A3D-6 trigger (`pinnedRuns` above 20, or the first `DOC-SIZE` refusal in a document that holds records) can fire at
  a lower run count than the design said (Inferred, not measured end to end). The leader decides whether Ruling 251 still holds.

- The refusal at the cap left the saved image byte-equal and emitted `analysis.size` with its pinned-run count.

### Design digits, an observation

Design 7.1 prints the shown dCDi -0.0090154117 and the stored dCDi 0.0090974287. Those come from the unrounded estimator CDi
(`CL^2 / (pi A)` at CL 0.4212557233), not from its 10-digit print 0.0075314829 (which gives -0.0090154079). The check builds
the fixture CDi unrounded; with it the checks hold at 1e-9 against the design's figures.

### Older-reader receipt (design 7.3, P-9 shape)

At the base of this branch (`93b5dfde`, which holds the A3d design) `AuthoringSession.cs` line 2254 reads the `analysis` member
with `Exact(analysisElement, "runs", "polarSamples", "pruned")`, and `Exact` raises `DOC-UNSUPPORTED-FIELD` on any member outside
its lists (line 2234, `Guard.Require(names.All(allowed.Contains), "DOC-UNSUPPORTED-FIELD")`); both read with `git show
93b5dfde:src/CfdWorkbench.Core/AuthoringSession.cs`. So an older build refuses a document that carries `discrepancies` (CLI exit
3) and cannot drop the records on a later save. This is a source receipt; no older binary was run. The forward guard is
`NativeReader_AnalysisMember_ClosedSet`.

### Copy id mapping (DESIGN.md section 7, rows appended after COPY-521, status proposed)

A3D-COPY-1 -> COPY-522; -2 -> 523; -3 -> 524; -4 (Base run) -> 525; -4 (Compared run) -> 526; -5 -> 527; -6 -> 528; -7 -> 529;
-8 -> 530; -9 -> 531; -10 -> 532; -11 -> 533; -11b -> 534; -12 -> 535; -13 -> 536; -14 -> 537; -15 -> 538; -16 -> 539; -17 -> 540;
-18 -> 541; -19 -> 542; -20 -> 543; -21 -> 544; -22 -> 545; -23 -> 546; -24 -> 547; -25 -> 548; -26 -> 549; -27 -> 550 (built in
A3d-1); -28 -> 551; -29 -> 552. The rows sit at the end of the numeric run, after COPY-521.

### Owner's reading of the `CheckStore` parameter

`CheckStore` gained one parameter, `acceptedOrdinal`, because the orientation check cannot run without the accepted-row ordinal.
The leader read it as part of Ruling 250 Q4's "one `CheckStore` call", and the Owner replied "no objection" to that reading
(Ruling 250 Q4; the leader's relay). The `RunRecord.cs` header carries the design 1.8 sentence word for word.

### Deviations, each for the leader

- The save-time report (Ruling 251) is its own event, `analysis.save`, emitted on every save of a document that holds runs. The
  design says "on the existing `analysis.prune` event"; that event fires only when a prune happens and an existing check reads
  its outcome `planned`, so the always-on report could not share it. The `DOC-SIZE` refusal in a document with records is
  `analysis.size`, with `PinnedRuns` and `Records`.
- The theme row in `ShellWindowTests.cs` (design 7.5 last line) was not added: the theme matrix row set is frozen by
  `tools/verify-application-adapters.py`, which is not on this track's paths.
- Four mechanical test-registration edits outside the owned list: the `ANA-CMP-` prefix in the reason-code scan's not-a-cell list
  (`DxSectionTests.cs`); the Compare tab in the expected tab list (`AnalysisPanelTests.cs`); the two Compare rows in the
  no-default-gesture exceptions (`ShellModelTests.cs`); the group registrations in `AnalysisChecks.cs` and `WorkbenchTests.cs`.
- Two cases in `Shell/ShellHost.cs` (`RunCommand` and `CanRun`) run the two command rows; seam request
  `req-01M5GRSHQ2TACMMFXR569JNTH2` asked for the path, and the stated fallback was taken.
- `inspect --runs` against a `.cfdw.json` needs the native store, which is macOS-only here; the CLI checks call
  `Cli.DiscrepancyListing` in process, so they ran on this Windows machine. The end-to-end `inspect` output was not run.
- "Compare with estimator" evaluates the estimator on the current accepted revision at the base run's operating point and water. For
  a Historical base run on an older revision the planform may differ, and Compare then blocks on the reference quantities.
- `WingEstimator.Settings` became public (the controller computes the Current key of an estimator run).
