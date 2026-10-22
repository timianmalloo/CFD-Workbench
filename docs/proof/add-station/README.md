---
id: proof-add-station
title: "Add station build proof: red and green lines, mutants, measured deviations"
type: proof-pack
status: draft
owner: "@track-s1-add-station-build"
phase: implementation
tags: [add-station, remove-station, inspection-slice, promote, rule-a, cad-02, cad-09, geo-06]
links:
  - { to: design-add-station, rel: implements }
review-by: "2027-01-31"
summary: >-
  The observed red and green lines of the Add station build, by node. Node S1-a (Core and CLI visibility) is recorded: 21
  checks red on the stubs and green after, mutants M1 to M6, M8 and M9 red, the measured deviations and k of the real
  pairs, the plan cost. The S1-b sections (Desktop, copy id mapping, xaml-token-lint) are filled by that node.
---

# Add station build proof

Design `docs/design/add-station.md` (section numbers below are its). Session `s1-add-station-build`. Logs are kept
outside the tree; the lines that matter are copied here. Base `main` at `f2ff4304`.

## Node S1-a: Core and CLI visibility

### Baseline

`bash tools/run-tests.sh` on the base: Core 762 passed, 26 skipped; Analysis 297; Desktop 795 passed, 23 skipped; Cli 21
passed, 2 skipped; `TESTS-GREEN 106 s`.

### Red on the stubs (design 7, TA6)

`PreviewPromote`, `AddStation`, `PreviewRemove` and `RemoveStation` first returned the base unchanged (commit
`b17df80e`). `CFD_TEST_ONLY=Station_ <Core.Tests.exe> --harness` then printed `RESULT failures=21`, one failure per check,
each on the assertion the design names for its input:

| Check | Last line on the stubs |
|---|---|
| T1 `Station_PromoteAt035_SectionEqualsBlend_1e12` | `Expected 3; actual 2` (assignment count) |
| T1b `Station_PromoteLaterBracket_UsesRightNeighbours` | `Expected R1; actual ` |
| T2 `Station_PromoteR1_LoftUnchangedBothIntervals` | `Expected R1; actual ` |
| T3 `Station_PromoteR0_CopyIsBitIdentical` | `Expected R0; actual ` |
| T4 `Station_PromoteR2_ReportsMeasuredDeviation` | `Expected R2; actual ` |
| T5 `Station_CoincidentWithinJoinTolerance_RefusedExistingSelected` | `Expected STATION-COINCIDENT; actual ` |
| T6 `Station_EtaAtRootOrTip_Refused` | `Expected refusal STATION-COINCIDENT` |
| T7 `Station_RemoveRootOrTip_Refused` | `Expected refusal STATION-ENDPOINT` |
| T8 `Station_RemoveAboveAcceptance_ReportsAndRefuses` | `deviation 0` |
| T9 `Station_AddThenRemove_RestoresBytes` | `Expected True; actual False` |
| T10 `Station_UndoAddAndRemove_OneStepEach` | `Expected 2; actual 1` |
| T11 `Station_SaveReopen_StationPresent` | `Expected 3; actual 2` |
| T12 `Station_InspectProjection_ShowsStation` | `Expected 0.35; actual 1` |
| T13 `Station_SeventhDifferingSection_RefusedBudget` | `Expected STATION-BUDGET; actual ` |
| T14 `Station_MixedSpacing_PromoteRefused` | `Expected STATION-SPACING; actual ` |
| T15 `Station_Promote_ThicknessKeepCurrent_ChannelsByteIdentical` | `Expected 3; actual 2` |
| T16 `Station_ProfileClosure_ReferencedOnceAndClosedRule` | `Sequence contains no matching element` |
| T17 `Station_EditPromotedProfile_NeighbourBlendUpdates` | `Expected 3; actual 2` |
| T18 `Station_DraftOpen_PromoteAndRemoveRefused` | `Expected refusal DSL-DRAFT-OWNED` |
| T19 `Station_AssignProfileToPromoted_PreviewReplace` | `Expected 3; actual 2` |
| T-perf `Station_PreviewPromote_RecordsDuration` | `Expected R1; actual ` |

Two checks passed on the first stub run (T15 and T-perf asserted nothing the stub could not satisfy). Both were
strengthened to assert that the station was added, and then failed as listed.

### Green

After the implementation (commit `54ee36f9` and the test follow-up): `RESULT failures=0`, `SUBSET CFD_TEST_ONLY=Station_ ran=21`.

### Mutants (design 7)

Each mutant was applied under `src/`, built, run against its named check, and reverted with `git checkout -- src`
(`git diff --stat -- src` empty afterwards).

| Mutant | Check | Red line |
|---|---|---|
| M1 wrong bracket (`left = 0`) | T1b | `Expected R1; actual R2` |
| M2 camber scaled by 1/M | T1 | `Promote refused: the new station would change the loft by 11238.1 um (9.09E-002 of chord) at eta 0.3500` |
| M3 closure always `closed` | T16 | `Promote is not available: the loft is not assessed (Closed trailing endpoints must meet at zero.).` |
| M4 coincidence on bitwise eta only | T5 | `Expected STATION-COINCIDENT; actual ` |
| M5 Remove deletes a referenced block | T16 | `DSL-PATCH` |
| M6 two commits per command | T10 | `Expected 2; actual 3` |
| M7 stale label in a view | D4 | S1-b (a Desktop check) |
| M8 deviation sampled only at eta_p | T4 | `Expected R2; actual R1` |
| M9 regime from k, exact form (`k == 1`) | T2 | `Expected R1; actual R2` (F-CAMBER's k is 1 - 1.1e-16) |
| M9 regime from k, tolerance form (`abs(k - 1) <= 1e-5`) | T4 | `Expected R2; actual R1` (F-REAL's k is 1 - 9.3e-6) |

M5 survived its first run: the shared-profile row of T16 built the sharing from two promotions of one document, where
each new station owned its profile, so the removal was always of an orphan. T16 now opens three stations that all name
one profile and removes the middle one; M5 is red on it. M2 and M3 are stopped by the candidate's own refusal before the
named equality is reached; both are red.

### Measured (T4, T2; printed by the checks as `INFO` lines)

The deviation is the largest sampled ordinate change over both adjacent intervals on 129 eta x 2001 x, never a bound
(Ruling 252). `k` is the maximum of the blended unit thickness `(1 - w) U_L + w U_R`, which equals 1 exactly when both unit
shapes reach 1 at one chord fraction. Two columns: the check's own computation from the neighbours' curves (4001 x), and
the value `StationEdits` computed for P in the M9 run.

| Pair (eta_p) | Regime | Deviation, of chord | Metres at the local chord | Found at eta | k (check) | k (P, M9 run) |
|---|---|---|---|---|---|---|
| F-CAMBER (0.35) | R1 | at or below 1e-12 | - | - | - | 0.99999999999999989 |
| F-SAMEPEAK (0.35) | R1 | at or below 1e-12 | - | - | 1.0000000000000002 (G17 as printed; only k(P) is exactly 1) | 1 |
| F-PEAK (0.5) | R2 | 1.515e-5 | 1.91 um | 0.2461 | 0.994872420 | 0.99487242638668127 |
| F-REAL, NACA 0012 beside NACA 4412 (0.5) | R2 | 4.911e-10 | 0.0001 um | 0.2461 | 0.999990691 | 0.99999069068800761 |

F-REAL is the committed real pair: the design's model gave 3.4e-10 (Inferred); the product measures 4.9e-10, in R2 and
far under 10 um, so an approximate promotion is admitted. F-PEAK is admitted too (1.91 um). F-SAMEPEAK is R1 with k
exactly 1 on the plateau fixture (the design's residual risk did not occur: the fixture certifies). Both R2 pairs report
the largest change at eta 0.2461, a sample inside the left interval [0, 0.5].

### Cost (instrumentation, design 5.3 and 11)

`document.preview-promote` and `document.add-station` carry `DurationMilliseconds` (T-perf) and `DeviationMicrometres`
on add and remove. Measured on the 20-core ARM64 laptop, Release:

- one `Placement.Sections` call of 258 eta x 2001 x on the base: 8.2 s (about 31 ms per blended eta; the maximum of the
  blended unit thickness is searched per eta inside `Placement`, which is read only here);
- one promotion plan measures the base and the candidate: 16 s on one core, about 10 s with the etas split over cores
  (`SectionsParallel`; scaling is poor, 20 parallel one-eta calls took 0.56 s against 1.0 s serial);
- the press is a preview and then the command on the same slice, so the session keeps the last plan (one entry, keyed by
  accepted revision and slice); the command after its preview adds no second measurement;
- the 21 Station checks take 250 s in the Core harness, in series.

This is a finding for the leader: the design's grid (129 eta per interval) costs seconds per press on this machine. A
cheaper maximum search inside `Placement` would cut it, but `Placement.cs` is read only for this track.

### Where the build departs from the design text

- **STATION-ASSET is not implemented.** The parser refuses every asset profile (`FoilSource.cs`, `DSL-REFERENCE`), so an
  accepted source never holds one and `ProfileDefinition` has no asset notion; a branch for it would be dead code.
- **T14 reaches `STATION-SPACING` on bytes, not through a session.** A document whose adjacent stations differ in spacing
  is Unsupported and cannot be admitted, so `Station_MixedSpacing_PromoteRefused` calls `StationEdits.Promote` on the
  parsed bytes with an assessment that fails if called.
- **T1's section-equality oracle is `Placement.Sections`** on the document before against the document after (binary64,
  existing code), not `Geometry.SectionAt`: the certificate's enclosures are wider than 1e-12.
- **The spacing and record predicates** were restated in `StationEdits` at cycle 0 because `Placement.cs` was read only. Repair cycle 1 (Ruling 256) made `Placement.SameRecord` and `Placement.SameAbscissa` internal and deleted the copies.
- **Edit rails.** `station-add` and `station-remove` were added to the reopen validation (`EditReference`) and to the
  operation fingerprint, so a saved document with a station reopens (T11).
- **CLI.** `inspect --json` prints `Sections.Points(source)` (every assignment's section points) and no projection, so
  `Program.cs` needs no change; T12 asserts the assignments 0, 1 and 2 appear in that list after a promotion.

### Not run

`ui-craft-gate.py`: not run: no arm64 binary. (No UI in S1-a.)

## Repair cycle 1 (review S1-a: CG1 to CG3, T-C1 to T-C4)

| Item | Red (last line) | Green (last line) |
|---|---|---|
| T-C4 grid parameter | not a red item. Before: Station group 242 s (review run, 21 checks), full run 330 s (cycle-0 figure) | `StationGrid` (default 129 x 2001) on the session; 21 checks 62 s, then 25 checks 69 s; full `run-tests.sh` `TESTS-GREEN 160 s` |
| CG1 regime follows the measurement | `FAIL Station_Regime_MeasurementDecidesNotThePredicate InvalidOperationException: Expected R1; actual R0` (the predicate alone gave R0 for a measured 1e-13) | `PASS Station_Regime_MeasurementDecidesNotThePredicate` |
| CG2 grid label; Remove 129 eta per interval | `FAIL Station_PreviewGrid_NamesTheGridUsed_RemoveSamplesPerInterval InvalidOperationException: Expected sampled on 17 eta per interval x 201 x; actual sampled on 129 eta per interval x 2001 x` | `PASS` (Remove samples both intervals, 2 x grid; the removed eta is their shared end) |
| CG3 relative and metres from one eta | `FAIL Station_Deviation_RelativeAndMetresComeFromOneEta InvalidOperationException: 10 %: relative and metres are not from one eta` (7.2035e-6 x 0.1156 m = 0.8325 um against 0.8211 um) | `PASS` (nine slices; relative x chord = metres within 1e-9) |
| T-C1 above 10 um | the mutant that deletes the refusal SURVIVED the cycle-0 checks (observed `RESULT failures=0`) | F-PEAK on a chord ten times as long: 19.0663 um at eta 0.2500, refused, `STATION-INEXACT`, bytes unchanged; the mutant now fails `Expected False; actual True` |
| T-C2 eta_p section and P maximum | the mutant "P not rescaled by 1/k" SURVIVED the cycle-0 checks (observed) | section at eta_p within 1e-12 of the blend; P maximum equals t(eta_p) within 1e-12 relative; the mutant fails `P maximum 0.11938469116640175 vs t(eta_p) 0.12` |
| T-C3 CLI binary path | mutant (`sectionPoints` without assignment 1): `FAIL Cli_Inspect_PromotedStationHasSectionPoints Exception: assignments 0,2` | `PASS Cli_Inspect_PromotedStationHasSectionPoints` (promoted session bytes through `Cli.RunAsync inspect --json`) |

The coarse grid is 17 eta per interval x 201 x. Its checks assert exactness (a deviation of 0 or about 1e-16 at every sample), a
refusal whose deviation is over 10000 times the limit (T8), bytes, undo, closure, the budget and the CLI. None reads a sampled
maximum, so a coarser sample cannot change them. T4 and T-perf stay on 129 x 2001. The production grid and its label are unchanged.

Placement predicates: under Ruling 256, `Placement.SameRecord` and `Placement.SameAbscissa` are `internal` (the only change in
`Placement.cs`: two modifiers); `StationEdits` calls them and its three copies are deleted. The Station checks pass unchanged.

The check-side k for F-SAMEPEAK prints 1.0000000000000002; only k(P) is exactly 1. The red lines for T12, T16 and T-perf were
recorded on the `b17df80e` versions; the final versions are red by reading. T19 now asserts root and tip are unchanged after the
Replace. The dead `True(mutantName.Length > 0, ...)` is gone.
