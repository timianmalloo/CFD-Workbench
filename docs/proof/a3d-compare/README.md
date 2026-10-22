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

From `Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` (`MEASURED` line, 128 strips per run; the strips are synthetic with 10-digit values):

- **bytes per VLM + strip run (128 strips): 109,991, a lower bound**, in the indented document (the design's Inferred figure was
  about 60,000). A solver prints up to 17 digits, which gives about 127 kB per run and about 63 runs (Inferred, a digit-count model). About **72 such runs** fit in `NativeProject.MaxBytes` (8,000,000), not 133.
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
(Ruling 250 Q4; recorded as coord mail `mail-01M5GTCYMMJAWYW0ZSGEYAKSRW`, kind note, opus-owner to opus-leader, 2026-10-22). The `RunRecord.cs` header carries the design 1.8 sentence word for word.

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

## A3d-2 repair (cycle 1)

Review conditions 1, 2 and 6 (must fix) and 3, 4, 5, 7, 8, 9, 10 (folds). Each code change has a check observed red on the old code, then green.

| Condition | Check | Red line (old code) |
|---|---|---|
| 1 KeepPlan order | `Retention_ReferencedRunInLatestTwenty_TakesNoQuotaSlot` | `the latest quota of unreferenced runs all survive; expected 20; actual 19` |
| 2 compare event bytes | `Discrepancy_CompareEvent_CarriesDocumentBytes` | `documentBytes on the compare event is the image size expected 16855; actual ` (not recorded) |
| 5 save outcome word | `Discrepancy_SaveEvent_CarriesPinnedRunsAndBytes` | `the save report claims a capture, not a completed save expected captured; actual saved` |
| 6 (b) tier headline | `Compare_TierHeadline_OnlyWhenBothRunsShareARevision` | `the revision headline, not the tier headline: Tiers disagree: ...` |
| 6 (a) estimator at the base revision | `ComparePane_EstimatorAfterTwistEdit_RunsOnTheBasesRevision` | `the estimator run is on the base run revision; expected 6909a82c-...; actual 96b385f8-...` |

Green: Analysis `Retention_,Discrepancy_,Compare_` `RESULT failures=0`; Desktop `ComparePane_` 12 `PASS`.

- Condition 6: `CompareWithEstimatorAsync` passes the base run to `AnalysisService.EvaluateAsync` (optional `atRun`, estimator only); the service reads the revision source through `AuthoringSession.AcceptedSourceOf` (`AnalysisService.RevisionView`). A base whose revision source is not held refuses before any evaluation with A3D-COPY-23 and the feed's revision-not-held reason, and stores nothing. The projection shows the tier headline only when the two runs share one accepted revision. `AnalysisService.cs` is outside the instruction's file list; seam request `req-01M5GTKD3CE94Y7P3EBXYBYFK4`. `ComparePane_IncompatibleReference_BlockedNoDelta` now pairs two VLM runs across the span edit, because the estimator command can no longer cross a planform change.
- Condition 8: the retention check asserts `pinnedRuns == 1` on `analysis.save`, with the estimator run on a reachable revision so only the VLM run is pinned. The size test's runs are reachable, not pinned: its figure is bytes per run, not a pin count.
- Condition 9: the size test's second phase fits four runs and two records under a cap just above their image; the next `RecordDiscrepancy` raises `DOC-SIZE` and the check asserts the call. (The first phase still ends in `RecordRun`, which is why a separate phase was needed.)
- Condition 10 (labelled): the pane's error path is checked with an injected error; the controller path is untested.
- Condition 11 is the leader's (needs W2 joined); `inspect --runs` is still not run end to end.
- Mutant rows: 7 (A3d-1) + 9 (A3d-1 repair) + 37 (Compare) = 53 before this cycle; this cycle adds the 5 red rows above, 58 in all. The reviewer's 46 counts the 37 Compare and 9 repair rows only.

## AF (wave 2): the dimensional deltas, the encode cost, the Cli harness gaps

Session `w2b-af`, built on `main` b0da43bc (the DF designs, Ruling 262). Design: `docs/design/a3d-compare.md` section 12 and
`docs/specs/amendments/spec-1.7.8.md`. Ruling 262 DF-Q1 settles the reading: the deltas are display-only, nothing is stored.
The harnesses were run as `<assembly> --harness` from the worktree root (Release), each to its own log; the exit code is the
line after the last check. Desktop ran only `--analysis`.

### What was built

| Surface | What |
|---|---|
| `Compare.Dimensional(run, coefficients)` | the one L and D_i formula, in newtons: coefficient times that run's own `0.5 rho V^2 S_ref` |
| `Compare.DimensionalApplies(blocks)` / `Compare.DimensionalLines` | exactly one Reference block whose fields are all among `S_ref`, `b_ref`, `c_ref`, `moment datum`; the two lines (L, D_i) as `DiscrepancyLine` values, never records |
| `CompareProjection` | the reference-only branch (state `ReferenceDiffers`): CL and CDi rows with values and AF-COPY-2, the e row with AF-COPY-3, L and D_i rows with the delta; the compatible-pair L and D_i rows now call `Compare.Dimensional` |
| `Labels.cs`, `DESIGN.md` | AF-COPY-1..7 as COPY-611 to COPY-617 (proposed, AF wave 2) |
| `CompareEvent.Dimensional` (Core) | one optional `bool?`, set on every `blocked-*` event |
| Pane | no code change: `ComparePane` renders the view model verbatim (notices, status, tables, spoken names), so the new state needed no new control |

Floors: the coefficient floors of the base run (`|CL| < 1e-4` for L, `|CDi| < 1e-6` for D_i; `<`, not `<=`), `ANA-CMP-BASE-ZERO` when
the base load is exactly 0, `ANA-CMP-VALUE-UNAVAILABLE` (A3D-COPY-24 in the value cell, no delta) when a coefficient is not finite.
The floor shown on an Undefined cell is the coefficient floor times the base run's q S_ref, computed by calling `Compare.Dimensional`
on the floor values (no second formula). AF-COPY-6 holds two clauses; the L cell shows the CL clause and the D_i cell the CDi clause.

### The `CompareEvent` member: added

Session events never reach the project image. Opened: `AuthoringSession.cs` line 181 `private readonly Queue<SessionEvent> events`;
lines 2360-2369 (`RecordCompareEvent`) enqueue into it; line 197 `ReadLocalEvents` reads it; the only other Core or
Persistence reader is `ProjectStore.ReadLocalEvents`. `EnvelopeCore` (the image) does not reference `events`. The member is
`CompareEvent.Dimensional` (`DiscrepancyRecord.cs`). Design 12.3 `assume:` held.

### `assume:` L from CL q S_ref equals the near-field strip lift

Verified on two real VLM runs (Desktop `--analysis`, 4 x 1 lattice, V 5.14 m/s, alpha 2 deg; the base run and the span-edited run), line
`INFO assume L from CL q S_ref` in `ComparePane_ReferenceDiffers_DimensionalDeltasOnly`:

| Run | L = Compare.Dimensional | strip lift (sum of -Fx sin a + Fz cos a) | relative difference |
|---|---|---|---|
| base | 174.48583978573973 N | 174.48583978573973 N | 0 |
| span-edited | 293.03570076991736 N | 293.03570076991736 N | 0 |

The check asserts `|L - strip lift| <= 1e-12 |L|`. The Analysis test `Dimensional_RoundTrip_EqualsStripLift` asserts the same on a 6-strip run.

### Red, then green (Analysis; the stubs returned null, the Desktop red used a mutant that disabled the branch)

| Test | Red (last line) |
|---|---|
| `Compare_ReferenceOnly_DimensionalDeltas` | `two lines: L and D_i expected 2; actual ` |
| `Compare_ReferenceOnly_CoefficientsStillBlocked` | `the five rows in order; expected CL,CDi,e,L,D_i; actual ` |
| `Compare_ReferenceOnly_ReversedRecomputed` (theory, both rows) | `the dimensional lines exist; expected True; actual False` |
| `Compare_Dimensional_BaseBelowFloor_Undefined` (theory) | `the dimensional lines exist; expected True; actual False` |
| `Compare_Dimensional_NoSecondFormula` | `and it is inside Compare.Dimensional; expected True; actual False` (the old `Q(run)` lambda was the one hit) |
| `Compare_Dimensional_NegativeBase_SignByAbs` | `the dimensional lines exist; expected True; actual False` |
| `Compare_Dimensional_OneDefinition` | `Nullable object must have a value.` |
| `Dimensional_RoundTrip_EqualsStripLift` | `Nullable object must have a value.` |
| `Compare_ReferenceOnly_EventFlag` | `dimensional = true; expected True; actual ` |
| `Compare_IncompatibleReference_BlocksDelta` (narrowed assertion) | `pane state expected ReferenceDiffers; actual Blocked` |
| `Compare_ReferenceOnly_StoresNothing`, `Compare_ReferenceAndPoint_NoDimensionalDelta` | green before the build (nothing was stored and no delta was shown); each is proved by its mutants below |
| Desktop `ComparePane_ReferenceDiffers_DimensionalDeltasOnly` | with `DimensionalApplies` forced false: `AF-COPY-1; expected True; actual False` |
| Cli (`InspectRuns_ChildHarness_*`) | compile red: `CS0103 The name 'ChildFailure' / 'ChildLimitSeconds' / 'ChildTimeoutVariable' / 'SkipLine' does not exist` |

Green after the build: Analysis 308 PASS, 0 FAIL (296 before); Core 857 PASS; Cli 29 PASS (26 before); Desktop `--analysis` 64 PASS (63 before: one
check replaced by two). Fixture values (F-PLANFORM): dL 0.1027509294, dD_i 0.1228454172, reversed -0.0931769148 and -0.1094054581, the negative-base rows +0.46 and -0.8518518519, the exact
1e-4 row 0.08.

### Mutants (each applied alone, built, run, reverted; last line is the first failing check)

| Mutant | Red line |
|---|---|
| m1 compared run takes the base S_ref (the wrong-reference mutant) | `dL: each run's own S_ref expected 0.1027509294; actual 0.0210656753` |
| m2 delta normalised to the compared value | `expected 0.1027509294; actual 0.0931769148` |
| m3 garbled reversal (a sign-flipped, re-divided delta) | `expected 0.1027509294; actual -0.1027509294` |
| m4 divide by the signed base | `row 2 forward expected 0.4600000000; actual -0.4600000000` |
| m5 absolute floor of 0.1 N in place of the coefficient floor | `base CL one ulp below 1e-4 expected ANA-CMP-BASE-FLOOR; actual ` |
| m6 `<=` in place of `<` | `Nullable object must have a value.` (the exact 1e-4 row has no delta) |
| m7 D_i floored on CL's floor | `base CDi 5e-7 floors the D_i line expected ANA-CMP-BASE-FLOOR; actual ` |
| m8 `axes` admitted to the dimensional branch | `axes and S_ref: no dimensional lines; expected True; actual False` |
| m9 reference admitted although the point also differs | `alpha and S_ref: no dimensional lines; expected True; actual False` and `dimensional = false when the point also differs` |
| m10 a coefficient delta unblocked in the projection | `CL has no numeric delta; expected True; actual False` |
| m11 a second L formula in `Loads` | `exactly one q * S_ref expression ... (AnalysisProjection.cs, 173), (Compare.cs, 225)` and `L base equals Compare.Dimensional bit for bit` |
| m12 event flag always true | `dimensional = false when the point also differs; expected False; actual True` |
| m13 q factor 0.4 in place of 0.5 | `|L - strip lift| <= 1e-12 |L|: L 206.31..., strips 257.89...` |
| m14 a record written for the reference-blocked pair | `Compare_ReferenceOnly_StoresNothing ContractError: DOC-SCHEMA` (the write is refused here for its orientation; any write that succeeded fails the record-count and byte checks) |
| m15 (Desktop) a percentage in a coefficient cell | `CL delta cell (AF-COPY-2) expected Blocked - reference differs; actual +1.00 %` |
| c1 Cli: `>= 0` in the limit | `an unset, empty, non-numeric or non-positive value gives the default 120 s` |
| c2 Cli: `win-only` in the SKIP line | `the SKIP line is 'SKIP <name> windows-only'` |
| c3 Cli: stderr dropped from the failure line | `the failure line carries stderr on one line: inspect --runs exited 3` |

Not representable: the design's `-d / (1 + d)` reversal mutant lives in a view that flips a stored delta. This projection never flips a delta; it
calls the function with the roles swapped. The negative-base row (-0.8518518519) is the check that the swapped call is the true value; m3 and m4 are
the nearest function-level mutants.

### Residual risks

- `Compare_Dimensional_NoSecondFormula` (a text scan of the sources) was dropped in repair 1: it walked to `CFDWorkbench.slnx` at runtime, which the Desktop `--self-launch` guard forbids. The guard of record for one definition of L and D_i is `Compare_Dimensional_OneDefinition` (the pane rows equal `Compare.Dimensional` bit for bit). A second formula that the pane does not use is no longer caught by a test.
- The narrowed assertion in `Compare_IncompatibleReference_BlocksDelta` edits an existing test (design 12.7 Q4); its seven reference rows are unchanged.
- No pixel check: the Desktop test reads the rendered text of the pane (logical tree), not a screenshot.

### Encode cost (item 4): left in `CompareFields`

Check `Discrepancy_CompareFields_EncodeCost_Measured` builds a document to the cap region (7,967,308 bytes; today's `NativeProject.MaxBytes` is
8,000,000), then records the same pair three times. A repeat is a key hit, so its event duration is the lock, the lookup and `CompareFields`.

| Run | reads (ms) | median (ms) |
|---|---|---|
| first measurement | 14.7, 14.8, 40.3 | 14.8 |
| final run | 15.0, 16.0, 32.5 | 16.0 |

Measured at **today's cap only**: MX's 64 MB cap is not on `main`, and a session cannot be built above 8,000,000 bytes (`AuthoringSession` refuses a larger
cap). **Inferred**: the encode scales with size, so 64,000,000 bytes would cost about 8 times the 8 MB figure, roughly 120 to 130 ms, which is over the 100 ms
rule. The gap that forced the model is the missing MX cap; re-measure with this check once MX joins, and take the size from the last save image if the
median is over 100 ms. The check also asserts the event's `documentBytes` equals the save image length.

#### Re-measured at the 32,000,000-byte cap (wave 2 MX continuation, session `w2b-mx`)

`NativeProject.MaxBytes` is 32,000,000 on this branch (Ruling 263), so the check now builds to `MaxBytes - 200,000` (it was a literal 7,800,000)
and reached 31,826,151 bytes. Three runs of the check, each a fresh process, three reads each:

| Run | reads (ms) | median (ms) |
|---|---|---|
| 1 | 63.2, 64.5, 71.7 | 64.5 |
| 2 | 67.1, 69.1, 90.8 | 69.1 |
| 3 | 62.1, 65.8, 70.6 | 65.8 |

Median of the three medians: **65.8 ms**, under the 100 ms rule, so `CompareFields` is left as it is (it does not take the size from the save image).
The machine was not strictly quiet: other agents' build servers were resident (8 `dotnet` processes in `tasklist`, no test host seen), and the
spread of the last read in each run shows it. The earlier model (about 8 times the 8 MB figure) over-estimated: 4 times the size cost about 4 times
the time (16.0 to 65.8 ms).

### Event name (item 3)

The save-time event is `analysis.save` (`AuthoringSession.cs` line 2118, `RecordCompareEvent("captured", ..., "analysis.save")`). Design section 6.1 on `main`
already says `analysis.save`; the stale `analysis.prune` wording is gone, so the design text needed no change.

### Cli harness (item 5)

`tests/CfdWorkbench.Cli.Tests/DiscrepancyCliTests.cs`: off Windows the two `InspectRuns_WindowsStore_Binary*` checks print `SKIP <name> windows-only`
(Windows: both run, `PASS`); the child process has a limit from `CFD_TEST_CHILD_TIMEOUT_SECONDS` (default 120 s, the Desktop harness's variable) and is killed with its
process tree at the limit; stdout and stderr are drained while it runs and stderr goes into the failure line (one line, cut at 400 characters). The three
pure checks are `InspectRuns_ChildHarness_SkipLineText`, `_TimeoutIsNamedAndBounded` and `_FailureCarriesStderr`. Repair 1 added the real hung child (see "Repair 1" below).

### Not changed

`RunRecord.cs`, `AuthoringSession.cs` (the claim was taken and released: nothing to change), the stored shape (the document bytes are equal before and after a
reference-only compare: `Compare_ReferenceOnly_StoresNothing`), `ComparePane.axaml(.cs)`, the CLI verbs.

### Repair 1 (review verdict conditions; session `w2b-af`)

Item 1, the guard. Red, on the branch as reviewed (Release build; `CfdWorkbench.Desktop.Tests.exe --harness --theme-evidence`, the mode that runs the `SelfLaunchTests` prefix and stops):
`System.Exception: SelfLaunchTests failed:  no test resolves the repo root at runtime: runtime repo-root walk toward CFDWorkbench.slnx outside CallerFilePath in tests\CfdWorkbench.Analysis.Tests\CompareTests.cs`
(exit 70). The walk (`FindRoot`) existed only for `Compare_Dimensional_NoSecondFormula`; both are deleted (leader decision on review N3). No other AF test needs the repo root.
Green after: `SelfLaunchTests: all 10 cases passed.` (exit 0). The guard of record for one definition is `Compare_Dimensional_OneDefinition`.
(Note: a bare `--harness --self-launch` is not a mode; the guard runs in the no-argument prefix, reached alone by `--theme-evidence`.)

Item 2, N1. `Compare_ReferenceOnly_StoresNothing` now uses a revision pair: two VLM runs on two revisions with a geometry edit between them, S_ref 0.100 then 0.108
(an estimator run beside a VLM run on one revision with a different S_ref cannot occur, design 12.2). The reviewer's realistic mutant m14b, a reference-only pair
through the normal store path in canonical orientation (`Compare.Record`, `if (blocks.Count > 0)` becomes `if (blocks.Count > 0 && !DimensionalApplies(blocks))`), is
red against it, caught by three checks:

| Mutant | Red lines |
|---|---|
| m14b | `FAIL Compare_ReferenceOnly_StoresNothing InvalidOperationException: blocked, no record; expected True; actual False`; `FAIL Compare_ReferenceOnly_EventFlag ... outcome expected blocked-reference; actual recorded`; `FAIL Compare_IncompatibleReference_BlocksDelta ... blocked, no record; expected True; actual False` (`RESULT failures=3`) |

Item 3, N2. New theory `Compare_ReferenceOnly_AdmissionFieldsAlone`: rows S_ref alone, b_ref alone, c_ref alone, moment datum alone; each shows two dimensional lines and
the `ReferenceDiffers` state with the L and D_i rows. Mutant n2, `"moment datum"` dropped from `DimensionalApplies`: `FAIL Compare_ReferenceOnly_AdmissionFieldsAlone
InvalidOperationException: moment datum alone: dL and dD_i exist; expected True; actual False` (`RESULT failures=1`; before this row it survived every check).

Item 4, N4. The replaced Desktop check `ComparePane_ReferenceAndWater_BlockedNoDelta` dropped the assertion `Equal(true, pane.InputsTable.Children.Count > 15, "the inputs
still show side by side;")`. It was dropped with the check it sat in; it is NOT restored. Why it is not needed: the inputs table is built from the view model's `Inputs` rows,
and `Compare_Inputs_SideBySide_AllFieldsBothRuns` pins that every section 2.1 field shows for both runs; a child count above 15 is a rendering detail of the same list,
not a separate contract. Residual: no Desktop check counts the rendered input rows of a two-reason block.

Item 5, N5. `DiscrepancyCliTests.RunChild(info, limit)` is the one start, drain, bounded wait and tree kill (used by `BinaryRuns`). New check
`InspectRuns_ChildHarness_SleepingChild_KilledAtTheLimit`: a child that sleeps 30 s (`cmd /c ping -n 30` on Windows, `sleep 30` elsewhere) under a 2 s limit; it asserts
not finished, a wait under 15 s, and the `TIMEOUT sleeping child after 2 s` line. Mutant kill (the `if (!finished) child.Kill(entireProcessTree: true);` line deleted):
`FAIL InspectRuns_ChildHarness_SleepingChild_KilledAtTheLimit InvalidOperationException: the wait ended near the limit, not at the child's end: 29.4 s`. Not asserted: that the
grandchild `ping` process is gone by name (the wait returning near the limit shows the pipes closed, because `ping` holds them).

Green after repair 1 (Release): Analysis `Compare_`, `Dimensional_RoundTrip` and `Discrepancy_` subset 35 PASS, `RESULT failures=0`; Cli 30 PASS, exit 0 (29 before); Desktop
`--analysis` 64 PASS, exit 0; `SelfLaunchTests` all 10 cases. Net Analysis check count in AF: one removed (NoSecondFormula), one added (AdmissionFieldsAlone).
Not in this repair (recorded follow-ups): N6 (`RecordDiscrepancy` accepts a reference-blocked record; a Data and Persistence next step) and the AF-COPY-5 wording (copy owner).
