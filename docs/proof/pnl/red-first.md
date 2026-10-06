---
id: proof-pnl-red-first
title: "PNL red-first ledger (adaptive panels)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, panel-method, red-first]
links:
  - { to: proof-pnl-timing, rel: relates-to }
  - { to: proof-pnl-step0-other-stations, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Red then green receipts for Ruling 103: the governing station's screen and Cp_min from the 400-panel solve,
  and the method version change that turns older runs Historical. Test 3 is not done (waits on the step-0 ruling).
review-suggested: []
---

# PNL red-first ledger

Commands run from the worktree, Release, one check each:
`CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`.
Both checks were written first (`SectionSeamTests.cs`) and run against the unchanged `src/`.

| # | Check | Red on the old code (observed) | Green after (observed) |
|---|---|---|---|
| 1 | `Section_GoverningStation_ScreenAndCpMinFrom400Panels` | `FAIL ... InvalidOperationException: governing station panel count expected 400; actual 200` | `RESULT failures=0` (full Analysis run, below) |
| 2 | `Section_AdaptivePanelMethod_OldRunReadsHistorical` | `FAIL ... InvalidOperationException: the method version still names the 200-everywhere method` | `RESULT failures=0` |
| 3 | A non-governing station above 10 % under-read carries the provisional flag | **Not done.** Waits on the step-0 ruling (`step0-other-stations.md`); no message with the ruling arrived. | n/a |

Check 1 builds a fixture (default NACA 0012 wing, 17 stations, α 3°, h_ref set) whose governing Cp_min differs between
200 and 400 panels by more than 1e-3 (it fails with a named message otherwise; observed delta 1.335 % at η 0.098 for the
129-station wing). It asserts: the governing station's `Estimate.Panel.StationCount` is 400 and its `CpMin`, its
`Cavitation.CpMin` and the wing screen's `CpMin` equal an independent 400-panel `PanelMethod.Solve` to 1e-12; every other
station's panel count and screen resolution are 200; `PanelUnderreadFraction` equals (S400 − S200)/S400 recomputed in the
test to 1e-12.

Check 2 builds a run keyed under `1.2.0/panel200-te3` and asks `Freshness.State` against the current method: it must be
Historical, and a run keyed under the current method must be Current. The run still records the governing station's
under-read: `PanelUnderreadFraction` is asserted in check 1 and the unchanged `Section_WingRun_PanelValuesAtEveryStation`
still checks it is finite and prints it.

Existing checks edited for the new identity and the new governing station: `LatticeFixtureTests.cs` (method version
literal, **outside the owned list**; see the Return), `PanelCpTests.cs` (`panel200-gov400-te3`),
`Section_WingRun_PanelValuesAtEveryStation` (the governing station reads 400, the rest 200).

Green evidence: full Analysis harness after the change, `RESULT failures=0`, then `tools/run-tests.sh` (see the Return).

## Repair cycle 1 (Ruling 110, CFD review conditions)

Receipt kind: the new checks use API that did not exist, so on the cycle-0 `src/` the harness does not compile
(`dotnet build` of the Analysis tests, `src/` stashed): `CS0117 'SectionTier' does not contain a definition for
'MaxPanelCandidates'` and `'UnderreadAt'`; `CS1061 'SectionTierResult' ... 'PanelCandidateCount'`; `CS1061
'SectionStationResult' ... 'PanelUnderread'`, `'PanelUnderreadMeasured'`, `'Provisional'`. That is a compile-red, not a
behavioural red; the near-tie check's key assertion (governing eta 1.0, where the cycle-0 code kept the 200-pass winner
at eta 0.5) is the behaviour it pins. All checks green after (`RESULT failures=0`, full Analysis harness).

| Check | What it pins | Observed |
|---|---|---|
| `Section_NearTie_GoverningReSelectedAt400` (CFD condition 1, Ruling 110 (2), test 3) | Planted: a thick station (eta 0.5) reads 1.03x the suction of a 2 %-thick station (eta 1.0, alpha 3) at 200 panels, so it wins at 200. At 400 the thin station wins: `GoverningEta` is 1.0 and the wing screen's station equals it; both stations solved at 400; the thin station is provisional; the run's under-read is the re-selected station's | thick 200 ratio 0.7658, thin 0.7889; thin under-read 16.51 %, thick 3.93 %; governing eta 1 |
| `Section_ThinStationOutsideNearTie_NotMeasured` (Ruling 110 (4)) | A thin station outside the near-tie width stays at 200, carries no measured under-read (`PanelUnderread` null = not measured) and is not provisional; `UnderreadAt` returns its measured value | `UnderreadAt` 10.81 % at alpha 0.5 |
| `Section_UniformWing_CandidateCountCappedAtFour` (Ruling 110 (1)) | A uniform wing ties at every station; exactly 4 are solved at 400 | 4 of 17 |
| `Section_GoverningEstimate_Cambered_Matches400Panels` (CFD condition 2) | Governing Cl, Cm_c/4, alpha_L0 and lift per span equal an independent 400-panel `SectionEstimator.Estimate` (cambered source) | equal |
| `Section_SixPercentThick_TwoGridUnderread_Measured` (Ruling 110 (6)) | The 6 %-thick 200-vs-400 under-read | 3.598 % at alpha 3, 4.385 % at alpha 6 (`underread-measurements.md`) |

Test 3 (planted thin section above 10 % carries the provisional state, governing re-selected) is the first row. A thin
station that is near-tied at 200 becomes governing at 400, because its under-read exceeds the width 2u; a thin station
outside the width stays not measured (second row). The data-level state is `SectionStationResult.Provisional`; the
COPY-312 row text belongs to track DX.

The provisional flag is the **two-grid, p assumed 1** under-read: (S400 - S200)/S400 on the suction peak. It is a
two-grid difference, not an error bound (CFD condition 3). The label is in the `SectionTier.cs` doc comments. The run
trace line is not done (see the Return): it needs a field in `Core/RunRecord.cs`.

Wording (CFD condition 5): the `MethodRecord.cs` doc comment and the `PanelCpTests` message now cite Rulings 90, 103, 110.
