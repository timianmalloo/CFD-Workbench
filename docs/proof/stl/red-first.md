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

Notes: after the red run the five checks were merged into two (one band edit sequence; one water, depth and Evaluate sequence) with every assertion kept, to save Desktop ring time (about 3 s); the merged checks pass. The "no new run" claim is counted by a solver wrapper (`Solves` unchanged across an edit and its revert); the run key
and `RunRecord.RecomputedKey` are compared before and after. Check 2 pins the controller's existing α wording exactly; check 1
pins the speed wording `Historical — operating point changed (speed)`, the one new use of the approved prefix (see Return).
Capture after a speed edit: `after-speed-edit.png` (strip "Analysis: Historical", Properties chip "Historical · VLM + strip").

## Ruling 158 status-line copy (second STL track, base dc25c632)

Each check failed on main's `src/` (or without the new `Labels` API) and passes with the change. Raw logs were in the track scratch (`scratch/stl/red1.log`, `red2.log`, `red3.log`).

| Behaviour | Check | Red (old code) | After |
|---|---|---|---|
| No status line leads with a code | `Labels_NoStatusLineLeadsWithACode_Ruling158` (Analysis, `CauseCopyTests`) | `4 status lines lead with a code, first: WorkbenchController.cs: Status = $"{SectionDraftPrefix()}{assessment.Code}: {assessment.Status}. ...` | PASS |
| Texts (1) to (4) exact | `Labels_SaveRetryRows_Ruling158_ExactText` | build error CS0117, `'Labels' does not contain a definition for 'ReadBackFailed'` (also `RetryNotConfirmed`, `DiskChangedAfterSave`) | PASS |
| OK code never printed | `Labels_SaveRetryNotConfirmed_OkCode_NeverPrintsOk_Ruling158` | same build error | PASS |
| Texts (5) to (7), each status, with and without reasons and station | `Labels_DraftUnavailable_Ruling158_ExactText` | same build error (`DraftUnavailable`) | PASS |
| Texts (1) to (4) as the status strip renders them | `SaveRetry_StatusLines_Ruling158` (Desktop `--section-editor`) | `expected 'Couldn't check the saved file (DOC-IO). ...', got 'DOC-IO: Uncertain save remains dirty; disk image could not be compared.'` | PASS |
| A non-section draft status is plain (no draft id, no leading code) | `Recovery_SaveReopenResume_KeepsDraftSeparateFromAccepted` (assertion added after `PreviewAsync`) | `Unavailable draft status is not the Ruling 158 sentence: DSL-VERSION: Invalid. Source does not satisfy the version contract.` | PASS |

Seam for the save-retry paths: the existing `WorkbenchController(Func<..., IProjectStore>)` store seam, with a `RetryStore` beside the older `UncertainStore` pattern. The OK case is a retry answering `OK` with `DurabilityConfirmed = false`.
Not driven end to end: a section draft with a station name for each status (no existing seam produces an Invalid, Unsupported or NotAssessed section draft cheaply); that wording is proved at the `Labels` level.
