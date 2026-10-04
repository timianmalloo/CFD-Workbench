---
id: proof-a3a-svc2-red-first
title: "A3a SVC-2 red-first receipt"
type: proof-pack
status: active
owner: "@track-a3a-svc2"
phase: implementation
tags: [a3a, svc, svc-2, analysis, review-fixes, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: proof-a3a-svc-red-first, rel: relates-to }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The SVC-2 fixes for the adversarial C# review of the A3a analysis service: 10 new checks and 3 strengthened ones,
  each red under its own planted mutant (17 mutants, all red), plus 5 red lines observed against the unfixed code.
---

# A3a SVC-2 red-first receipt

Design `docs/design/area3-analysis.md` §5.6, §8 and the §18.8 SVC-2 rows. Session `svc2`, branch
`fix/a3a-svc-review`, 2026-10-04. Base `0845406` (SVC joined). Commits: `06c00e1` (service fixes), `bff8c25` (record
and key fixes), then the design rows and the merge of the integration head `f487c3b` (VLM joined; clean merge).

**Method.** For the service-only fixes the new checks were written first and run against the unfixed code: five were
red (below). The record fixes change types (`AnalysisRun.Diagnostics` nullable, `RunSettings` stations, the barrier
signature), so their checks cannot compile against the old code; for those the proof is the mutant. Every mutant was
planted in the committed tree, the owning project rebuilt, the check selected with `CFD_TEST_ONLY` (the Cli harness
runs whole), and the file restored (`git status` clean after each batch).

## Red against the unfixed code (base `0845406`)

| Check | Red line |
|---|---|
| `Evaluate_Supersede_CancelsOlderOutsideTheLock` | `the service lock held while the older's callbacks ran expected False; actual True` |
| `Telemetry_UnexpectedException_OutcomeNotOk` | `outcome expected ANA-UNEXPECTED; actual OK` |
| `Evaluate_CancelledBeforeIdempotentHit_Throws` | `expected OperationCanceledException; the evaluation returned` |
| `Evaluate_SameKeyFromTwoServices_ReturnsRecordedRow` | `ContractError: DOC-RUN-KEY` |
| `Evaluate_WaterOutsideTable_RefusedNoRow` | `expected ContractError; the evaluation returned` |

`Evaluate_SupersededBeforeCancel_RecordsNothing` was green at the base, as it must be: the identity check existed. Its
red is the mutant M2.

## Planted mutants

| # | Finding | Mutant | Red line |
|---|---|---|---|
| M1 | 8 (Supersede) | the older's `Cancel` removed | `Evaluate_Supersede_OlderCancelledViaBarrier`: `the older's compute token cancelled while it is held; expected True; actual False` |
| M2 | 8 (Supersede) | the record step's identity check removed | `Evaluate_SupersededBeforeCancel_RecordsNothing`: `expected OperationCanceledException; the evaluation returned` (Supersede itself stays green: each guard has its own check) |
| M3 | 5 | `Cancel` moved back under the service lock | `Evaluate_Supersede_CancelsOlderOutsideTheLock`: `expected False; actual True` |
| M4 | 2 | `analysis.run` outcome starts `"OK"` | `Telemetry_UnexpectedException_OutcomeNotOk`: `outcome expected ANA-UNEXPECTED; actual OK` |
| M5 | 4 | the hit returns without the token check | `Evaluate_CancelledBeforeIdempotentHit_Throws`: `expected OperationCanceledException; the evaluation returned` |
| M6 | 6 | no re-read of the key under the lock | `Evaluate_SameKeyFromTwoServices_ReturnsRecordedRow`: `ContractError: DOC-RUN-KEY` |
| M7 | 7 | the water record not validated | `Evaluate_WaterOutsideTable_RefusedNoRow`: `expected ContractError; the evaluation returned` |
| M8 | 9 | the barrier given `CancellationToken.None` | `Evaluate_Supersede_OlderCancelledViaBarrier`: `the barrier holds the older's cancelled token; expected True; actual False` |
| M9 | 8 (CloseMidCompute) | the record moved before the barrier | `Evaluate_CloseMidCompute_DocClosedNoRow`: `rows at the hold point, before the close expected 0; actual 1` |
| M10 | 1 | the outcome/diagnostics rule removed from `CheckStore` | `RecordRun_DiagnosticsByOutcome_CompletedOnly`: `expected refusal DOC-SCHEMA; none` |
| M11 | 1 | the service writes zeros on a Failed row | `Evaluate_ComputeFails_FailedRowHasNoDiagnostics` and `Telemetry_AnalysisRun_EmittedWithSubDurations`: `ContractError: DOC-SCHEMA` |
| M12 | 1 | the solve's diagnostics kept when the coupling fails | `Evaluate_ComputeFails_FailedRowHasNoDiagnostics`: `ContractError: DOC-SCHEMA` |
| M13 | 1 | the writer writes a null `diagnostics` member | `RecordRun_DiagnosticsByOutcome_CompletedOnly` and `Evaluate_ComputeFails_FailedRowHasNoDiagnostics`: `diagnostics member written … actual True`; `FAIL Cli_AnalyseFailedRun_PrintsNoDiagnostics` |
| M14 | 3 | `sectionEtas` left out of the settings hash | `Evaluate_SectionStationsChanged_NewKeyNotAHit`: `the 5-station run has the 3-station key; expected False; actual True`; `Freshness_EachSettingsField_Historical`: `SectionEtas changed alone: expected Historical; actual Current` |
| M15 | 3 | the service samples fixed stations | `Evaluate_SectionStationsChanged_NewKeyNotAHit`: `sections the finer method was given expected 5; actual 3` |
| M16 | 3 | settings without stations not refused | `Evaluate_SectionStationsChanged_NewKeyNotAHit`: `expected ContractError; the evaluation returned` |

`RunKey_PinnedVector_HexEqual` stayed green through the record change: a settings record without stations keeps its
hash, so the change is expand only.

## Decisions

- **Finding 3: fold the stations into the key.** The other option (require them equal to stations derived from the
  settings) needs a derivation that does not exist: the joined VLM derives its *panel* stations from the settings and
  the section span (`VortexLattice.SpanStations`), and interpolates between the sections it is given, so the sampled
  stations are an input of their own, not a function of the settings. Writing one in SVC would be a second definition
  of VLM's geometry. So the stations are `RunSettings.SectionEtas`/`SectionXs` (§3.4: settings hold every numeric choice)
  and `IWingMethod.Etas`/`Xs` are removed: the settings hash covers them with no change to `RunRecord.Key`, and the one
  settings read feeds the key, the sampling and the row. Optional and omitted when null, so VLM's 12-argument
  `Settings.Default` still compiles and the pinned vector holds; the service refuses null (`ANA-INPUT-STATIONS`).
- **Finding 1: diagnostics last and optional.** The reader runs with `RespectRequiredConstructorParameters`, so only an
  optional (defaulted) parameter may be absent; a defaulted parameter must be last. The content hash is JCS (sorted
  keys), so the move changes no hash.
- **Finding 5 opens a race, handled.** With `Cancel` outside the lock the older can finish and dispose its source in
  `Release` between the swap and the `Cancel`; that one `ObjectDisposedException` is caught, since nothing is left to
  cancel. The identity check now has a job of its own: the swap-to-Cancel window.
- **Finding 6 narrows, the store closes.** The re-read under the service lock returns the other service's row; a
  duplicate that lands after the re-read (two services, two locks) is still refused by the session's `DOC-RUN-KEY`.
- **Finding 9b: premise not reachable, no change.** `inspect --runs` reads a file; the reader refuses a run whose
  accepted id is missing (`AuthoringSession.cs`:2009, `RunRecord.CheckStore`, `DOC-REFERENCE`) and accepted rows are
  never removed while a session is open (only `Dispose`, :133). So `RevisionOf` cannot fail inside the listing.
- **Finding 8 (CloseMidCompute).** A closed session has no read, so "no row" is asserted as its two halves: no row at
  the hold point, and the one write after it refused `DOC-CLOSED`.
- **Water band.** 0–50 °C is the spec's admitted range; the salinity band 0–35.16504 g/kg is an `assume:` in
  `OperatingPoint.cs` until STP's table lands.

## Gates (final tree, after the merge of `f487c3b`)

- `tools/run-tests.sh`: green, exit 3 (TEST-BUDGET): `wall 92 s (budget 60 s) cpu 767 s load 23.65 -> 30.52`. Core
  parts 65 s and 85 s, Desktop 81 s, Analysis 6 s (49 PASS), Cli 2 s (5 PASS). The SVC-2 checks add about 0.6 s to
  Analysis under that load; the over-budget wall is the Core and Desktop jobs under contention.
- `check-named-tests.py SVC` (§18.2/§18.8): `(SVC) 26/26 named tests PASS · 0 failures`.
- `check-docs.py`: passed. `check-event-subscribers.py`: 0 findings.
- `verify-application-core.py` (reached by `RunRecord.cs`): `RESULT failures=0`. `recount-application-contracts.py`: exit 0.
- `check-test-costs.py`: not in the tree (RNG's tool has not landed).
