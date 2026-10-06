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
