---
id: proof-m12b-u1a-red-runs
title: "M1.2b U1a red runs"
type: proof-pack
status: active
owner: "@track-u1a2"
phase: implementation
tags: [m12b, u1a, desktop, controller, red-first]
links:
  - { to: design-m12b-points, rel: depends-on }
  - { to: coordination-m12b-build, rel: implements }
review-by: "2026-10-30"
summary: >-
  Records the foreground red run of all 23 named U1a controller checks before the controller implementation,
  and the separate threshold mutant run required by the U1a exit evidence.
---

# U1a red-run receipt

## Named controller checks before implementation

Command: `tools/run-tests.sh` on `m12b-u1a-controller` at test commit `058d5c7`.
Exit: **1**. Build succeeded with 0 warnings and 0 errors; Core 376 PASS in 32 s, CLI 1 PASS in 1 s, Desktop 119 PASS in 47 s; wall 50 s (60 s budget).
The controller harness printed the following failures. The missing public gesture contract is the oracle: these checks cannot pass on the prior controller.

```text
FAIL GestureStateTable_EveryCell_TransitionOrIgnored RuntimeBinderException: WorkbenchController does not contain a definition for Gesture
FAIL Controller_MoveTwoPixels_NoDraft InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_MoveFourPixels_DraftOpened InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_DragPoint_OneUndoStepUndoExact InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_NudgeLadder_CommandPlainShift InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_NudgeRunHeld_OneRowOnKeyUp InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_NudgeRunFocusLost_CommitsOneRow InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_NudgeRunWindowDeactivated_CommitsOneRow InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_CaptureLostDuringDrag_Cancelled InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_EscapeDuringDrag_NoRowGeometryBack InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_SaveDuringDrag_CommitsThenSaves InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_StaleCommitCompletion_ReturnsToIdle InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_PointerDownDuringChordCommit_NoDraft RuntimeBinderException: WorkbenchController does not contain a definition for ApplyChordAsync
FAIL Gesture_ReleaseEdgesCross_RefusedGeometryUnchanged InvalidOperationException: Missing GestureInput controller contract
FAIL Gesture_ReleaseNotAssessed_RefusedDistinctCopy InvalidOperationException: Missing GestureInput controller contract
FAIL Gesture_BeginDuringCommit_NoDraftNoBusy InvalidOperationException: Missing GestureInput controller contract
FAIL Gesture_ThousandMoves_CoalescedFramesBounded InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_DuringDrag_EstimatesFromDraftGeneration InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_UndoDisabledDuringGesture_EnabledAfter InvalidOperationException: Missing GestureInput controller contract
FAIL Controller_Reconcile_MakeControlDropsHandleSelection RuntimeBinderException: WorkbenchController does not contain a definition for ApplyPointCommandAsync
FAIL GestureEnd_Committed_EmitsFramesAndP95 InvalidOperationException: Missing GestureInput controller contract
FAIL Telemetry_PointEdits_NoIdsOrPositions InvalidOperationException: Missing GestureInput controller contract
FAIL --controller-shell exited 1
RUN_TESTS_EXIT=1
```

## Threshold-zero mutant

The U1a controller implementation was built locally with the `UpdateGesture` activation comparison set to `px < 0` (zero-pixel threshold). This was **before the fix commit**. Command: `dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --controller-shell`. Exit: **1**.

```text
FAIL Controller_MoveTwoPixels_NoDraft InvalidOperationException: Two-pixel move opened a drag.
PASS Controller_MoveFourPixels_DraftOpened
MUTANT_EXIT=1
```

The focused pair distinguishes a 2 px click from a 4 px drag. The same run also found `ApplySpan_EdgesCross_Refused`, `Controller_SaveDuringDrag_CommitsThenSaves` and `GestureEnd_Committed_EmitsFramesAndP95` failing; these are tracked in the implementation pass. Other U1a named checks passed in this mutant run.

## Dispatch 2: §6.2 cell tests, first red run

Test-only commit: `7cba37e`. Command: `tools/run-tests.sh`; exit **1**, wall **50 s** (60 s budget). Build: 0 warnings, 0 errors. Core: 376 PASS / 32 s. Desktop: 142 PASS / 48 s. All 70 generated cell names failed before behavior could be exercised because the synthetic fixture searched the trailing rail for a `Fixed` point, while the point model assigns that freedom to leading index zero. Representative output:

```text
FAIL Controller_Gesture_Idle_PointerMovable_Pressed InvalidOperationException: Sequence contains no matching element
FAIL Controller_Gesture_Nudging_KeyUp_Busy InvalidOperationException: Sequence contains no matching element
FAIL Controller_Gesture_Busy_DirectCommand_Refused InvalidOperationException: Sequence contains no matching element
FAIL --controller-shell exited 1
```

This run is a test-fixture red, not yet evidence of a missing transition. The fixture is corrected before assessing behavior; the test-only commit remains the red-first checkpoint.
