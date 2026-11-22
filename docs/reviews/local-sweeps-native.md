---
id: review-local-sweeps-native
title: "Native implementation proof pack: Area 4 local-tier sweeps & performance curves (SWP-BUILD)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation — Area 4 (Wave 6 SWP-BUILD, Ruling 271)
tags: [sweeps, area-4, analysis, local-tier, performance-curves, native, proof-pack, wave6]
links:
  - { to: design-local-sweeps, rel: implements }
  - { to: proof-swp-old-build, rel: tested-by }
  - { to: adr-0011-analysis-run-storage, rel: implements }
  - { to: design-language, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: coordination-wave6-and-area5-wsl, rel: implements }
review-by: "2027-02-22"
summary: >-
  Implementation proof pack for Wave 6 Track SWP-BUILD (Nodes SWP-1..5) authorized by Ruling 271:
  additive cfdw-project-2 experiments store with active retention pinning & Archive unpinning,
  GoalStateSweepSnapshot seam for Wave C, dedicated sequential local SweepService (max 120 cases,
  idempotent runKey reuse, Cancel <= 250 ms, op.Load = requiredLoadN on Goal-state runs),
  frame-origin Cm and Ncrit 2-4 non-bridged curve projections, CLI sweep preview/run/inspect parity,
  and native Avalonia Sweeps workspace (Experiment setup, Run (local), Results) with T1..T18 red-first
  verification and zero LAB-01 / XAML-token / event-subscriber / PII findings.
---

# Native implementation proof pack: Area 4 local-tier sweeps (`SWP-BUILD`)

**Scope & Authorization.** Wave 6 Track `SWP-BUILD` (`docs/coordination/wave6-and-area5-wsl.md`), authorized by
**Ruling 271** (`DR-SWP-1 (a)`, `DR-SWP-2 (a)`, `DR-SWP-3 (a)`, `DR-SWP-4 (b)` coordinated with `DR-SET-2 (a)`,
`DR-SWP-5 (a)`, `DR-SWP-6 (a)`). Delivered across five sequential green nodes (`SWP-1..5`).

## 1. Delivered surfaces (E7 traceability)

| Node | Files authored / edited | Ruling 271 contract satisfied |
|---|---|---|
| **SWP-1** Core store & retention | `src/CfdWorkbench.Core/SweepRecord.cs`, `RunRecord.cs`, `AuthoringSession.cs`, `tests/CfdWorkbench.Core.Tests/SweepStoreTests.cs`, `IdentityTests.cs`, `ProjectStoreTests.cs`, `docs/proof/swp-old-build/{README.md,one-sweep.cfdw.json}` | `DR-SWP-1 (a)` additive optional `experiments` on `cfdw-project-2`, unified `RunRecord.Format(runCount, experimentCount, hasSetup)`, staged fail-closed `Exact(..., ["analysis", "experiments"])` (keeping `"setup"` refused with `DOC-UNSUPPORTED-FIELD` until Wave C), `.v1.bak` on runless `-1 → -2` transition; `DR-SWP-2 (a)` `KeepPlan()` pinning of selected `Completed` runs of non-archived sweeps + append-only `Archived` unpinning into `PrunedRun` tombstones; `DR-SWP-4 (b)` positive `OperatingPoint.Load` guard in Core; `GoalStateSweepSnapshot` + `AuthoringSession.CurrentGoalSweepSnapshot()` seam returning `null` until Wave C. |
| **SWP-2** Analysis schedule, service & projection | `src/CfdWorkbench.Analysis/{SweepSchedule,SweepService,SweepProjection}.cs`, `AnalysisService.cs`, `Compare.cs`, `OperatingPoint.cs`, `Labels.cs`, `tests/CfdWorkbench.Analysis.Tests/{SweepTests,AnalysisChecks,DxSectionTests}.cs` | `DR-SWP-3 (a)` `MaxCasesPerSweep = 120`, sequential execution on dedicated `AnalysisService` with revision-pinned `EvaluateAtRevisionAsync` for both `Tier.VlmStrip` and `Tier.Estimator`, idempotent `runKey` reuse, Cancel ≤ 250 ms, `runId == null` on all non-Completed attempts, safe `DOC-SIZE` stop; `DR-SWP-4 (b)` relaxed `OperatingPoints.Validate` for positive finite `op.Load` and Goal-state `FindAlpha` populating `op.Load = requiredLoadN`; `DR-SWP-5 (a)` frame-origin `Cm` (`NChord ≥ 2`, `Not resolved · 1 chordwise panel` at `NChord == 1`, `Unavailable` on Estimator), `Ncrit 2–4` bands never averaged, explicit non-bridged gap segment splitting, depth-unset `free surface not modelled` (never `"deep water"`), and `sweep.*` normal-path telemetry (`IO1–IO12`). |
| **SWP-3** CLI parity | `src/CfdWorkbench.Cli/SweepVerb.cs`, `Program.cs`, `tests/CfdWorkbench.Cli.Tests/{SweepCliTests,CliTests}.cs` | `CLI-01` `cfd-workbench sweep preview`, `sweep run [--out]`, and `inspect <file.cfdw.json> --sweeps` sharing `SweepSchedule`, `SweepService`, `SweepProjection`, and `RunRecord` with bitwise `runKey` parity and `1e-12` scalar parity (`CL`, `CDi`, `Cm`). |
| **SWP-4** Desktop UI & charts | `src/CfdWorkbench.Desktop/Sweeps/{SweepController,PerformanceCurveChart,ExperimentSetupView.axaml(.cs),LocalRunView.axaml(.cs),SweepResultsView.axaml(.cs),SweepsWorkspaceView.axaml(.cs)}`, `Styles.axaml`, `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/{StatusStrip.axaml(.cs),CommandTable.cs,ShellHost.cs}`, `tests/CfdWorkbench.Desktop.Tests/{SweepViewTests,WorkbenchTests}.cs` | `DR-SWP-6 (a)` third navbar segment `Sweeps` (`CAD | Analysis | Sweeps`) plus Command Palette `view.sweeps`, hosting three discrete sub-tabs (`Experiment setup` · `Run (local)` · `Results`), zero `Run` button in `Experiment setup` (`XS-03`/`UX-17`), live Cancel in Run console and `StatusStrip` (`UX-21`), theme-aware `PerformanceCurveChart` (distinct dash/marker/end-label, non-bridged gaps, `×` markers), one-click `Show table` twin (`UI-17`), synchronized case selection (`ANA-08`), and Light/Dark/High Contrast themes. |
| **SWP-5** Copy & Proof Pack | `DESIGN.md` (`COPY-706..727`), `docs/reviews/local-sweeps-native.{md,html}` | Proposed copy rows `COPY-706..727` (`SWP-COPY-1..22`, `LAB-01` 0 hits) and full repository verification. |

## 2. Red-first test matrix (`T1`–`T18`)

All 18 specification tests from `docs/design/local-sweeps.md` §8 were observed **RED** before their corresponding
node implementation and **GREEN** afterward:

| Test ID | Harness test name | Project | Verified contract |
|---|---|---|---|
| T1 | `SweepSchedule_Cartesian3x4_DerivesTwelveCasesAndConditions` | Analysis | 3×4 sorted grid, exact `q`, `Re_ref`, `h/c`, `Fr_h`, `σ` within `1e-12` relative; `EstimatedWallMs == null` until measured (`IO8`). |
| T2 | `SweepSchedule_InvalidInputs_BlockWithInputNamed` | Analysis | Empty lists, duplicate speed (`14, 14`), non-number, `NaN`, speed `≤ 0`, depth `≤ 0`, water `55 °C`, and `121` cases each refuse with stable code naming input. |
| T3 | `SweepSchedule_DepthUnset_UnavailableAndNoDeepWaterLabel` | Analysis | `h_ref = null` yields `DepthNotSet` and `free surface not modelled`, never `"deep water"`. |
| T4 | `SweepRecord_ImmutableOnceQueued_EditRefused` | Core | Draft editable before Queue; attempt before Queue refused (`DOC-SCHEMA`); edit after Queue refused (`DOC-SCHEMA`); Failed attempt with `runId != null` refused. |
| T5 | `SweepStore_RunlessExperiment_FormatProject2AndV1Bak` (+ `ProjectStoreTests.Backup_V1Bak_RunlessSweep_FormatProject2`) | Core | 12-case runless queued sweep transitions `-1 → -2`, writes byte-equal `.v1.bak`, omits `analysis`, and round-trips byte-identically. |
| T6 | `SweepStore_OmittedWhenEmpty_ExistingBytesIdentical` | Core | `-1` and run-only `-2` documents omit `"experiments"` and verify unified `RunRecord.Format(runs, experiments, hasSetup)`. |
| T7 | `SweepStore_OldBuildReader_FailsClosedCopy130` | Core | Wave B refuses `"setup"` and unknown fields with `DOC-UNSUPPORTED-FIELD`; pre-SWP `90b4af4f` probe verified `DOC-UNSUPPORTED-FIELD` (`COPY-130`) with unchanged SHA-256 on `docs/proof/swp-old-build/one-sweep.cfdw.json` (read via `PlacementTests.RepoRoot()`). |
| T8 | `SweepStore_TamperedDefinitionOrRun_ReadsUnavailable` | Core | 1-byte tamper in `definition.anglesDeg` or referenced strip surfaces `PayloadFailedCheck` without deleting data. |
| T9 | `SweepRetention_ActivePinsSelected_ArchiveUnpinsAndTombstones` | Core | Active 12-case sweep on unreachable branch survives `SaveImage()` on top of 20-per-tier quota; superseded attempt unpinned; `Archived` event releases pins into `PrunedRun` tombstones and reopens cleanly. |
| T10 | `SweepService_LocalRun12Cases_ReusesRunKeyAndPreservesCancel` | Analysis | Pre-computed case 0 reused idempotently (single run row); Cancel on case 7 completes ≤ 250 ms, preserving cases 0..5 `Completed`, case 6 `Cancelled` (`runId == null`), 7..11 `Pending`. |
| T11 | `SweepService_OneFailedCase_StatusCompletedAndRetryAppendsAttempt` | Analysis | 11/12 completed -> `Completed`; failed `runId == null`; `RetrySampleAsync` appends `attemptIndex = 2` with `priorAttemptId` chaining; 0/4 -> `Failed`. |
| T12 | `SweepService_DocSizeCap_StopsSafelyWithoutCorrupting` | Analysis | Lowered `envelopeCap` marks overflowing case `Failed (DOC-SIZE)` (`runId == null`), leaves remaining cases `Pending`, and preserves prior runs intact. |
| T13 | `SweepProjection_CurvesAndGaps_NoSegmentAcrossFailedCase` | Analysis | `Cm < 0` for positive lift aft of LE; distinct `Ncrit 2` and `Ncrit 4` series (never averaged); interior failed case splits both vs-`α` and vs-`Speed` polylines into disjoint segments with zero bridging edge. |
| T14 | `SweepProjection_EstimatorAndOnePanel_CmAndDragHonestUnavailable` | Analysis | Estimator reports `CL`, `CDi (e = 1)`, and `Unavailable` for `Cm` & `L/D`; 1-chordwise-panel VLM reports `Not resolved · 1 chordwise panel` for `Cm`. |
| T15 | `SweepProjection_GeometryEditAfterSweep_HistoricalByRunKey` | Analysis | Twist edit `r1 → r2` reads `Historical — surface changed (r1 → r2, a twist point moved)` without mutating stored sweep; Undo restores `Current`. |
| T16 | `SweepService_GoalStatePoints_SolvesAlphaPerPoint` | Analysis | Solves `α` within 1 % of `RequiredLoadN`, records `SolvedAlphaRecord` and `OperatingPoint.Load == RequiredLoadN`, and marks impossible load `Failed (ANA-FIND-NO-SIGN-CHANGE)` without clamping. |
| T17 | `Cli_SweepRunAndInspect_MatchesGuiRunKeysBitwise` | Cli | Bitwise `runKey` parity and `1e-12` scalar parity (`CL`, `CDi`, `Cm`) between CLI `sweep run` / `inspect --sweeps` and GUI `SweepService`. |
| T18 | `Desktop_Sweeps_NoRunButtonInSetup_LinkedSelection_Lab01Clean` (+ `LiveCancelFromStatusStripAndConsole`, `ThemesLightDarkHighContrast_RenderCleanly`) | Desktop | Zero `Run` button in `ExperimentSetupView`; synchronized selection across chart/table-twin/small-multiples/inspector; failed case clears scalars and shows gap box; live Cancel in console & `StatusStrip`; Light/Dark/High Contrast themes; 0 `LAB-01` hits. |

## 3. Repository gates

All mandatory repository verification gates pass with zero findings:
- `bash tools/run-tests.sh` (`TESTS-GREEN 355 s`: Core `961 passed, 7 skipped, 0 failed`; Analysis `324 passed, 1 skipped, 0 failed`; Desktop `934 passed, 1 skipped, 0 failed`; Cli `37 passed, 1 skipped, 0 failed`; Python `10 ran, 0 failed`)
- `python docs/ai-forward-pack/scripts/xaml-token-lint.py --root src/CfdWorkbench.Desktop Sweeps ModelArea.axaml Shell/StatusStrip.axaml` (`clean - no XAML token findings`)
- `python tools/check-event-subscribers.py` (`38 event(s), 4 allowed, 0 finding(s)`)
- `python tools/coordination/check-test-paths.py` (`0` relative repo reads in Core tests)
- `python tools/check-reader-sharing.py` (`0` readers without delete sharing under `src/`)
- `python tools/check-proof-pii.py` (`0` Windows user paths or machine SIDs)
- `python docs/ai-forward-pack/scripts/verify-ruling-citations.py` (`272` rulings cited, `272` defined)
- `python tools/check-docs.py` (`Documentation checks passed`, `637` artifacts, `0` problems)

## 4. Residual items for independent Adversary review (`w6-verifier`)

- Operator review of proposed copy rows `COPY-706..727` in `DESIGN.md` (Ruling 271 allows revisiting wording).
- Wave C (`SET-BUILD`) will populate `AuthoringSession.CurrentGoalSweepSnapshot()` with live Area 1 Goal states without modifying `src/CfdWorkbench.Desktop/Sweeps/**`.
- Full 200 % modal scaling and live screen-reader (Narrator / VoiceOver) traces remain tracked under the workbench-wide product accessibility gate.
