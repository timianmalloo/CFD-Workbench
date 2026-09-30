---
id: proof-m12b-u1a-red-runs
title: "M1.2b U1a red runs"
type: proof-pack
status: active
owner: "@track-u1a2"
phase: implementation
tags: [m12b, u1a, desktop, controller, red-first]
links:
  - { to: design-m12b-points, rel: verifies }
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

Pending the controller implementation; this section will hold the distinct red mutation output before the fix commit.
