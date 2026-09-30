---
id: proof-m12b-u1a
title: "M1.2b U1a controller proof pack"
type: proof-pack
status: in-review
owner: "@track-u1a2"
phase: implementation
tags: [m12b, u1a, desktop, controller, proof]
links:
  - { to: design-m12b-points, rel: depends-on }
  - { to: proof-m12b-u1a-red-runs, rel: depends-on }
  - { to: coordination-m12b-build, rel: implements }
review-by: "2026-10-30"
summary: >-
  Records U1a controller red-first and current green evidence, the 3 px activation mutant,
  readiness measurements, and the remaining Core Span seam before this track can exit.
---

# U1a controller proof pack

## Contract and oracle

The controller consumes Core's `Planform.View`, `BeginPointGesture`, `UpdatePointGesture`, `Validate`, `Apply`, `ApplyPointCommand`, `ApplyChord`, and `WingEstimates.From`. It owns gesture state, latest-target frame coalescing, history availability, command Busy state, visible status copy, and `gesture.end` telemetry. Accepted source remains in Core's append-only history. A controller refusal or cancellation must leave the accepted source unchanged; one committed drag or nudge run must be undone by one cursor move.

The U1a names are in `docs/design/m12b-points.md` §12.4. Their pre-implementation red output and the threshold-zero mutant are in [the red-run receipt](../m12b-u1a-red-runs.md). The mutant run observed `Controller_MoveTwoPixels_NoDraft` FAIL and `Controller_MoveFourPixels_DraftOpened` PASS; the committed threshold is 3 px.

## Current green evidence

`tools/run-tests.sh`: exit **0**, build 3 s, Core 376 PASS in 31 s, CLI 1 PASS in 1 s, Desktop 142 PASS in 48 s, wall 51 s (60 s budget).

`python3 tools/check-named-tests.py U1a --design docs/design/m12b-points.md`: exit **0**, 23/23 PASS. `D3a` 40/40, `D1` 12/12, `D2` 21/21, `C1` 14/14, `P1` 23/23: each exit **0** against the default design. These checkers read the fast-run logs; they do not themselves execute the code.

| Claim | Checks and red oracle | Confidence | Residual |
|---|---|---|---|
| A click stays a click; release flushes the last drag target | `Controller_MoveTwoPixels_NoDraft`, `Controller_MoveFourPixels_DraftOpened`, `Controller_ReleaseWithPendingFrame_CommitsLastPointerTarget`; missing API red, threshold-zero mutant red | Verified in controller harness | Rendered interaction belongs to U1b |
| A drag or nudge run is one history row | `Controller_DragPoint_OneUndoStepUndoExact`, `Controller_NudgeRunHeld_OneRowOnKeyUp`, `Controller_NudgeRunFocusLost_CommitsOneRow`, `Controller_NudgeRunWindowDeactivated_CommitsOneRow`, `Controller_UndoDisabledDuringGesture_EnabledAfter` | Verified in controller harness | Native shortcut routing belongs to U2 |
| Cancellation and refusal preserve accepted geometry | `Controller_CaptureLostDuringDrag_Cancelled`, `Controller_EscapeDuringDrag_NoRowGeometryBack`, `Gesture_ReleaseEdgesCross_RefusedGeometryUnchanged`, `Gesture_ReleaseNotAssessed_RefusedDistinctCopy` | Verified in controller harness | Certificate quality remains Core's responsibility |
| Busy does not admit another draft; completion exits Busy | `Controller_StaleCommitCompletion_ReturnsToIdle`, `Controller_PointerDownDuringChordCommit_NoDraft`, `Gesture_BeginDuringCommit_NoDraftNoBusy` | Verified for the exercised transitions | Busy tests currently lack an injected deterministic commit barrier |
| Frame work is coalesced; draft estimates carry the draft generation | `Gesture_ThousandMoves_CoalescedFramesBounded`, `Controller_DuringDrag_EstimatesFromDraftGeneration` | Verified in controller harness | Frame scheduling is not a rendered-tier test |
| Type changes reconcile selection; instrumentation excludes point identities and positions | `Controller_Reconcile_MakeControlDropsHandleSelection`, `GestureEnd_Committed_EmitsFramesAndP95`, `Telemetry_PointEdits_NoIdsOrPositions` | Verified in controller harness | A full privacy register sweep belongs to the wave join |
| Save commits a drag before publishing | `Controller_SaveDuringDrag_CommitsThenSaves`, with the real project store under the runner's non-symlinked temp directory | Verified in fast ring | Other shell verbs have not been directly exercised here |

`GestureStateTable_EveryCell_TransitionOrIgnored` and `Controller_NudgeLadder_CommandPlainShift` also PASS; the table check exercises representative Idle, Pressed, Dragging, Nudging and Busy transitions, but does **not yet** assert every §6.2 cell. That gap is not promoted to full table coverage by the name or PASS line.

## Readiness (one invocation, on-screen timing is not a gate)

Command: `dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --readiness` with the runner's non-symlinked temp directory. Exit **0**. It is excluded from `run-tests.sh`.

| Check | Measured p95 | Target | Samples |
|---|---:|---:|---:|
| `Readiness_NewFoilDrag_FrameP95Under100Ms` | 16.105 ms | 100 ms | 40 frames |
| `Readiness_NewFoilCommit_P95Under250Ms` | 67.862 ms | 250 ms | 5 commits |

## Open seam and exit status

**S-U1a-Core-Span:** `AuthoringSession.PrepareSpan` currently checks only `spanSi > 0`; it accepts `1000000000` (the Desktop input is millimetres). The existing default-design `ApplySpan_EdgesCross_Refused` check fails if U1a removes its legacy `spanSi >= 1e6` guard. U1a does not own `src/CfdWorkbench.Core/AuthoringSession.cs`; the Core owner must put the upper-bound refusal at the Core authority, then U1a can remove the Desktop copy and route Span through the async command path. The old synchronous guard is retained in this checkpoint so D2 remains green. **F-6 is open, and this track has not exited.**

`python3 tools/check-docs.py` initially exited **1** because this new proof pack had an unregistered `verifies` relation and was absent from the generated index. The relation was corrected to `depends-on`; the index and final docs gate are pending at this checkpoint.
