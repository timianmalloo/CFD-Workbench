---
id: proof-flk-investigation
title: "SectionEditor DragMove flake investigation"
type: proof-pack
status: active
owner: "@trk-flk"
phase: implementation
tags: [flake, section-editor, investigation]
links:
  - { to: proof-ring-split-moved-2, rel: relates-to }
summary: "No failure reproduced in about 90 runs; stale-step hypothesis refuted; root cause unverified; capture-first plan."
review-by: "2026-11-07"
---

# Investigation: SectionEditor_DragMove_DrawsWithinOneFrame flake (trk-flk)

Status: **root cause NOT verified.** No failure was reproduced in about 120 runs. This note records what was ruled out, what is still open, and the next step. Per the brief, no product code changed and nothing was fixed.

## The check and its assumptions
`tests/CfdWorkbench.Desktop.Tests/SectionEditorTests.cs:278-322`. Shared `Fixture` (line ~89), `fixture.Reset()` first (line 1058: `CancelSection`, `Select`, `EnterSectionAsync`, one `RunJobs`).

Timing assumptions the check makes (all Verified by reading):
1. After `Press` (`RunJobs`), the controller is quiet: no work started by `Reset`/`Press` posts a `Changed` or `SectionChanged` later. The counter subscribes only after `Press` (line 292-293).
2. Each `Move` is synchronous: `RaiseEvent` + `Dispatcher.UIThread.RunJobs()` (line 1113-1118). Every dispatcher job queued at that moment, including late continuations from earlier checks, runs inside `Move`.
3. `RenderTargetBitmap.Render` draws the new frame at once; `canvas.DrawnCurves[0]` is the current frame (no frame loop, no render timer is awaited).
4. `changes == 0` and `Draft.Generation` unchanged on all 6 moves ("notified the shell N times or applied a step"; the same line catches a generation change, so the message does not say which).
5. No debounce or timer is involved: the drag path (`SectionCanvas.OnPointerMoved` 785-816, `UpdateSectionGesture` 1609-1625) only sets `pendingGestureTarget` and `previewPoint`, and calls `InvalidateVisual`.

## Where a late notification could come from (Verified by reading, `WorkbenchController.cs`)
`Changed` is raised only by `Notify()` (line 3303); `SectionChanged` by `RaiseSectionChanged()` (1613) and line 1031. With a fresh section and no step, these reach an open draft only through:
- the surface projection: `CompleteSurface` (874-890) and the 250 ms `surfaceBehindTimer` (831), both via `OnUiThread` (a dispatcher post). Gated by `surfaceWanted` (813); `ModelArea` only ever sets it false (lines 165, 347, 443), so in this fixture it is Inferred to be off;
- `AssessCurrentSectionAsync` (1207-1245), guarded by `sectionAssessmentTicket` and generation, bumped by `CancelSectionAssessment` (1253);
- a queued step's `land` (1044-1090, 1098-1123), guarded by `SectionStale` (DraftId, line 1186) and by the DraftId check in `RunQueuedAsync` (1156);
- `RefreshAcceptedAsync` (3076), not reachable from `Reset` or `Press`.

## Reproduction attempts (measured; none failed)
Machine: 16 cores, macOS. Load generators: `yes > /dev/null` x N, all killed afterwards. Scripts in the session scratchpad `trk-flk/`.

| Condition | Runs | Failures |
|---|---|---|
| Check alone, no load | 1 | 0 |
| Check alone, 32 hogs (load 31) | 10 | 0 |
| Check alone, 64 hogs (load 120-157) | 40 | 0 |
| `SectionEditor_Drag*` pair, 150 hogs (load 109) | 4 | 0 |
| `--section-editor --part=2/2`, no load | 1 | 0 |
| `--section-editor --part=2/2`, 48 hogs (load 61) | 4 | 0 |
| `--section-editor --part=2/2`, 100 hogs (load 148) | 6 | 0 |
| 8 concurrent copies of part 2/2 + 16 hogs (load 85-100), 3 rounds | 24 | 0 |

Failure rate measured here: 0 of 90, so under about 3 % per run by this method (rule of three). The recorded failures (`docs/proof/ring-split/moved-2.md:57`) came at load 8 to 17.6 in a real ring run, so **"CPU starvation" is not supported**: low load failed in the field, and 150 hogs did not fail here.

Temporary instrumentation (a stack trace in `Count`) printed nothing in any run.

## Hypotheses tested deterministically (no load needed)
Test-only env knobs, reverted (`git checkout tests/`):
- **Stale step from the previous check lands during the drag.** `FLK_STEP_DELAY_MS=300` and `1500` via the `sectionStepGate` seam, so `SectionEditor_DragUnderPointer_WithinTwoPixels`' un-awaited release step applies on the pool after the next `Reset`. Result: both pass. **Refuted** (the `SectionStale`/DraftId guards hold).
- **Slow surface projection completes mid-drag.** `FLK_SURFACE_DELAY_MS=700` via the `surfaceCompute` seam. Result: pass. **Not a valid test**: `surfaceWanted` is Inferred false in this fixture, so the projection never ran; the hypothesis is neither confirmed nor refuted for the Section Editor fixture, and I did not prove the path unreachable (I did not print `SurfaceWanted`).

## Verdict
- Root cause: **unknown (not Verified, not even a labelled Inferred candidate with evidence).** Two candidates ruled out or untestable; do not record either as the cause.
- Product vs test defect: **undetermined.** What can be said: the test's oracle is exact (`changes != 0`) on a shared fixture, and any dispatcher job that reaches `Move`'s `RunJobs` counts. That is a test-oracle weakness only if the notifier is a legitimate late job; if the notifier is a stale job from an earlier check, it is a product guard gap. Only the failing stack can tell which.
- The lesson entry's claim that the flake is "load" is not supported by data.

## Siblings (same shape: exact notification count on the shared fixture, asserted across `RunJobs`)
- `SectionEditorTests.cs:142-176` `SectionEditor_NudgeRun_NoShellRefreshPerKey` (subscribes `Changed`, `SectionChanged`, `SelectionChanged` at 153-155).
- `ControllerViewTests.cs:38, 158, 1515-1516` (per-controller, new controller each check, lower risk).
- Frame-budget family already gated by `DesktopChecks.RequireFrameBudget` (`WorkbenchTests.cs:527`): `Readiness_SectionDragMove_Under16Ms` (SectionEditorTests.cs:760), `View3dTests.cs:747`, `AnalysisLayerTests.cs:91`.

## Phased plan (proposal only, to the operator)
1. **Capture, not fix** (test-only, ring: Desktop fast ring, cost about +0 s): in `Count` record the stack (`Environment.StackTrace`) and a one-line `DRAGMOVE-NOTIFY` message with `changes`, generation and `Dispatcher.UIThread.CheckAccess()`; split the failure text so "notified" and "generation changed" are separate. Red-first: a planted `Controller.Select(...)` post between moves fails the check and the text names the notifier. Do this first; the next real failure then names the cause, and the READINESS-MISS gate decision follows from that text (as the lesson already says).
2. **Make the cause reproducible** once the stack is known: add a CTL seam gate (like `sectionStepGate`) that holds the named notifier and releases it mid-drag; red-first deterministic check, no load.
3. **Fix** at the right side: product guard if the notifier is stale; otherwise drain the fixture (`Reset` waits for `SectionStepPending == false` and no pending assessment) before `Press`, or count only notifications raised by the move itself.
Cost: step 1 is a few lines in one file; steps 2 and 3 depend on step 1.

## Not done
No failure was caught, so no mechanism proof. Left running: nothing (all `yes` processes killed, scripts exited). Nothing outside this file is committed.
