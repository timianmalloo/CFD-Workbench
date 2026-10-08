---
id: proof-fss-red-first
title: "Section Editor surface-settle: origin, red-first receipt, frequency"
type: proof-pack
status: active
owner: "@trk-fss"
phase: implementation
tags: [flake, section-editor, surface]
links:
  - { to: proof-flk-investigation, rel: relates-to }
summary: "The drag-time notification is a mesh job queued by Reset; a held-surface check fails without the drain and passes with it."
review-by: "2026-11-07"
---

# Section Editor flake: a surface completion inside the measured window (trk-fss)

## Item 1: where the request came from (Verified)
The surface job is queued by the fixture's `Reset`, before `Press`. Chain: `Fixture.Reset` (`SectionEditorTests.cs`,
`CancelSection` then `Select` then `EnterSectionAsync`) leads to `WorkbenchController.Notify` ->
`RequestSurfaceIfChanged` (`WorkbenchController.cs:3303`, defined `:811`) -> `IssueSurface` (`:821`). The key
`d:<draft id>:<generation>` (`SourceKey`, `:807`) is new for each entered draft. `SurfaceWanted` is true in the fixture
(`ModelArea.ShowFoilOpen(true)`; `ModelArea.axaml.cs:449` sets it outside Section mode); the held-seam assertion below is the
observation, this reading is the explanation. The job then runs `Placement.Surface`
on the pool (`:531`); its continuation posts `CompleteSurface` to the dispatcher (`:861`), which calls `Notify()` (`:888`).
`Press` pumps the dispatcher once, but a pool job that has not finished by then completes during a later `Move`.

Observed, not inferred: the new check `SectionEditor_DragMove_HeldSurfaceDrainedBeforeThePress` holds the mesh seam and
asserts, right after `Reset`, `Held.Pending > 0` and `Controller.SurfaceUpdating`. That assertion passed in both runs below,
so `Reset` alone leaves a job in flight. Verdict: test isolation. No product change.

## Item 3: red-first receipt (deterministic, no load)
Commit `445bc692` carries the check and the drain. Planted change (reverted with `git checkout`): delete the line
`held.SettleSurface();` before `Press` in the new check.

Planted run (`CFD_TEST_ONLY=SectionEditor_DragMove tools/run-suite.sh dotnet <dll> --section-editor`, exit 1):
```
FAIL SectionEditor_DragMove_HeldSurfaceDrainedBeforeThePress Exception: Move 2 notified the shell 1 times: [Changed generation=0 ui=True stack=WorkbenchController.Notify WorkbenchController.cs:3273 <- WorkbenchController.CompleteSurface WorkbenchController.cs:888 <- <>c__DisplayClass280_0.<RunSurfaceAsync>b__0 WorkbenchController.cs:861 <- WorkbenchController.OnUiThread WorkbenchController.cs:897 <- WorkbenchController.RunSurfaceAsync WorkbenchController.cs:861 <- Fixture.Move SectionEditorTests.cs:1223 ...]
```
Same notifier, same frames, and the same `Move 2` as the field failures (`docs/proof/ring-oct08/flake-run1a.txt`).
(The `Notify` line number is `:3273` here and `:3304` in the field text; not explained, the frames and names match.)

Committed run (drain in place, same command, exit 0):
```
PASS SectionEditor_DragMove_DrawsWithinOneFrame
PASS SectionEditor_DragMove_HeldSurfaceDrainedBeforeThePress
```

## Item 2: drains added and sweep
| Where | Action |
|---|---|
| `SectionEditorTests.cs` `SectionEditor_DragMove_DrawsWithinOneFrame`, before `Press` | `fixture.SettleSurface()` |
| `SectionEditorTests.cs` `SectionEditor_NudgeRun_NoShellRefreshPerKey`, before the probe | `fixture.SettleSurface()` |
| `SectionEditorTests.cs` `Fixture.SettleSurface` | new: pumps until `WhenSurfaceSettledAsync()` completes (releases a held seam first) |
| `ControllerViewTests.cs:38` (camera-only change) | no change: `SurfaceWanted` is never set, so no mesh job exists |
| `ControllerViewTests.cs:158` (`SlowSurface_UpdatingShownAfter250Ms`) | no change: the surface is the thing under test, gated and released by the test |
| `ControllerViewTests.cs:180` (planform read in handler) | no change: no surface wanted |
| `ControllerViewTests.cs:1515` (disposed controller) | no change: the check asserts nothing is raised after `Dispose` |
| `ControllerSectionTests.cs:487`, `ControllerShellTests.cs:168/:416`, `ShellWindowTests.cs:49`, `View3dTests.cs:782`, `WorkbenchTests.cs:264` | no change: they wait for a named event or state, not "zero notifications over a window" |
Only the two `NotifyProbe` sites assert a zero count over a window on a shared fixture.

## Item 4: frequency (measured)
12 alternating runs each of `--section-editor --part=1/2`, ambient load 2.1 to 3.7 at start (macOS, 16 cores),
before = test file at `476e09ba`, after = this branch. The drag check ran in both.

| Variant | Runs | `SectionEditor_DragMove_DrawsWithinOneFrame` fails |
|---|---|---|
| before | 12 | 0 |
| after | 12 | 0 |

The flake did not reproduce before the fix (0 of 12), so the frequency table proves nothing on its own; the deterministic
check above is the proof. Both field hits were one in a ring of about 700 checks, so a rate below 1 in 12 is consistent.
Side finding: the before build's part 1/2 also failed `SaveDialog_EmptyOrDuplicate_ErrorFocusStays` and
`SaveDialog_Save_LiveRegionFocusToSectionMenu` (`DOC-UNSUPPORTED-PERSISTENCE`) in all 12 runs. Parts are split by
registration index modulo 2 (`WorkbenchTests.cs:573-575`), and adding one check moved those two to part 2 (they no
longer appear in part 1 of the after build). Why they fail in part 1 is not investigated; the full `tools/run-tests.sh`
below is the check that they pass where they now run.
