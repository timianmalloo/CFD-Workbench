---
id: proof-stl-red-first
title: "STL red-first receipt"
type: proof-pack
status: active
owner: "@trk-stl"
phase: implementation
tags: [stl, ruling-140, red-first]
links:
  - { to: proof-stl-trace, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  All five Ruling 140 checks failed on the code before the fix (commit 1ad61f16, raw output in red-run.txt) and pass after it.
---

# STL red-first receipt

Command: `CFD_TEST_ONLY=StaleConditions tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --analysis`

| Check | Red line (old code, `red-run.txt`) | After |
|---|---|---|
| `StaleConditions_SpeedEdit_EverySurfaceReadsHistorical_ThenRevertsToCurrentWithNoNewRun` | `projection state: expected Historical, got Current` | PASS |
| `StaleConditions_AlphaEdit_UsesTheApprovedOperatingPointWording` | wording: expected `Historical — operating point changed (α 2.00° → 3.00°)`, got empty | PASS |
| `StaleConditions_WaterAndDepthEdits_ReadHistorical_EvaluateMakesTheNewPointCurrent` | `a water change reads Historical: expected Historical, got Current` | PASS |
| `StaleConditions_MalformedOrBlankInput_NoCrash_ReadsAsChanged_DepthBlankStaysCopy45` | `speed '' reads as changed: expected Historical, got Current` | PASS |
| `StaleConditions_Edits_LeaveTheStoredRunAndItsKeyUntouched` | `edited: expected Historical, got Current` | PASS |

Notes: the "no new run" claim is counted by a solver wrapper (`Solves` unchanged across an edit and its revert); the run key
and `RunRecord.RecomputedKey` are compared before and after. Check 2 pins the controller's existing α wording exactly; check 1
pins the speed wording `Historical — operating point changed (speed)`, the one new use of the approved prefix (see Return).
Capture after a speed edit: `after-speed-edit.png` (strip "Analysis: Historical", Properties chip "Historical · VLM + strip").
