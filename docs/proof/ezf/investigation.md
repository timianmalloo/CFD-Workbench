---
id: investigation-ezf-zoompanfit
title: Investigation EZF - the flaky Elevation_ZoomPanFit check is a stale fit baseline, not CPU starvation
type: investigation
status: draft
owner: "@trk-ezf"
phase: investigation
tags: [ezf, flake, desktop-tests, surface, camera, fixture]
links:
  - { to: proof-a3a-pack, rel: relates-to }
review-by: 2026-11-08
summary: >-
  Elevation_ZoomPanFit_KeyboardAndPointerSameCamera fails when the previous check leaves an accepted-revision mesh in flight and
  that mesh lands after Fixture.Reset has fitted the camera. The fit baseline came from the draft mesh, the later fit from the
  accepted mesh. Verified necessary and sufficient with a planted mesh delay. Repair is test-side: Reset drains the mesh first.
---

# Investigation EZF: Elevation_ZoomPanFit_KeyboardAndPointerSameCamera

Status: draft, stopped for review. No fix landed. Worktree `/Users/mallalieut/projects/CFD-Workbench-inv-ezf-zoompanfit`,
branch `inv/ezf-zoompanfit`, base main `8d321816`.

## Root cause in one paragraph (Verified)

`Fixture.Reset()` (`tests/CfdWorkbench.Desktop.Tests/ElevationTests.cs:682`) calls `Front.Fit()` / `Side.Fit()` without
waiting for the controller's mesh job. `Fit()` computes the camera from `controller.Surface` bounds
(`src/CfdWorkbench.Desktop/ElevationView.cs:287-292`). The previous check in `--views --part=1/2`,
`Elevation_TwistDomainClamp_ProbeShowsReason`, ends with Escape on a twist drag. Escape returns the source key from the draft
(`d:<id>:<gen>`) to the accepted one (`a:<hash>`) and issues a new "accepted" mesh request
(`WorkbenchController.cs:811-835`); the mesh is computed on the thread pool (`:531`) and lands on the UI thread in
`CompleteSurface` (`:866-875`). The test's `Pump(... Gesture == Idle)` does not wait for it. So when the check starts, `Surface`
may still be the clamped-twist draft mesh. `Reset` fits to the draft mesh (`fitted`: Target Z 0.04689). The accepted mesh then
lands in the middle of the check (the check pumps the dispatcher in every `Settle`). The later `view.Fit()` or `⌘0` fits to the
accepted mesh (Target Z 0.00077). `SameCamera(fitted, ...)` fails. The product is correct: later meshes keep the user's camera
by design (`ModelArea.axaml.cs:618`). The test compares a fit from one mesh with a fit from another.

CPU load is only the amplifier. It stretches the 13-24 ms mesh job (comment at `WorkbenchController.cs:~650`) until it lands
inside the check's ~100 ms body instead of inside `Reset`'s own pumps. This matches the observed load rise from 1.9 to 16.
It is the same structure as SECTION-EDITOR-LOAD-FLAKE's verified cause (a background surface job completing between
dispatcher pumps).

## 1. Reproduction

Natural reproduction by load alone failed (the window is narrow and needs the job to land in a specific 100-ms span):

| Mode | Load generator | Runs | Fails |
|---|---|---|---|
| `CFD_TEST_ONLY=Elevation_ZoomPanFit` alone | 24 busy loops | 20 | 0 |
| `--views --part=1/2` (the part that holds the check) | 20 busy loops | 12 | 0 |
| `--views --part=1/2`, `--part=2/2`, unloaded | none | 1 each | 0 |

Alone, the check cannot fail: `Reset` runs right after the first mesh is drained in the Fixture constructor (`:677`) and no
mesh is in flight. So the precondition is ordering (a mesh-changing check before it) plus timing.

Deterministic reproduction with a planted mesh delay (Verified). Spike patch (not committed): scratch
`/Users/mallalieut/projects/cfd-workbench-continuation/scratch/ezf/spike.patch`. It has two hunks: (a) the default surface compute
sleeps `EZF_DELAY_MS` ms for basis "accepted" (`WorkbenchController.cs:531`); (b) `Pump(() => !Controller.SurfaceUpdating)` in
`Fixture.Reset` (the repair candidate). Recipe, from the worktree:

```
export AGENT_SESSION=trk-ezf
dotnet build -c Release tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj
EZF_DELAY_MS=100 CFD_TEST_ONLY=Elevation_TwistDomainClamp,Elevation_ZoomPanFit \
  dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --views
```

Scripts: `.../scratch/ezf/load.sh` (busy loops), `loop.sh <runs> <load> alone|part` (the natural attempts above).

Results, pair run twice per delay, hunk (a) only:

| Planted delay (ms) | Result | Failing assertion |
|---|---|---|
| 0, 30 | pass | none (mesh lands inside `Reset` pumps) |
| 40 | FAIL (also in the full `--views --part=1/2`, the only failure in the suite) | ⌘0 fits |
| 60, 100, 150 | FAIL, 2 of 2 each | ⌘0 fits |
| 200 | FAIL, 2 of 2 | ⌥→ pans 10 % (landed later in the body) |
| 300, 400, 800 | pass | mesh lands after the check ends (the next check is exposed) |

Captured failure text (delay 150):

```
FAIL Elevation_ZoomPanFit_KeyboardAndPointerSameCamera Exception: ⌘0 fits: expected ViewCamera { Target = Point3 { X = 0.05956907038973242, Y = 0, Z = 0.04688825974365625 }, AzimuthDegrees = 0, ElevationDegrees = 0, Distance = 0.7846210196827212, Projection = Orthographic, Name = Front, FitDistance = 0.7846210196827212, Title = Front }, actual ViewCamera { Target = Point3 { X = 0.06, Y = 0, Z = 0.0007679026871988369 }, AzimuthDegrees = 0, ElevationDegrees = 0, Distance = 0.7846210196827212, Projection = Orthographic, Name = Front, FitDistance = 0.7846210196827212, Title = Front }
```

Delay 200 text is in `.../scratch/ezf/fail-200.txt` (same Targets, assertion "⌥→ pans 10 %"). Only Target X and Z differ;
Distance, azimuth, elevation, projection are equal. That is the signature of a different mesh bounding box, not of a layout or
size change. A layout change would move Distance (it is the fit scale), and Distance is identical.

## 2. Mechanism

The check (`ElevationTests.cs:438-463`) stores `fitted = view.Camera` right after `Example.Reset()`, then compares every later
camera to `fitted.ZoomAbout/Pan(...)`, and re-fits with `⌘0` (`ElevationView.cs:724`) and `view.Fit()` (lines 454, 457) and
expects `fitted` again. All expectations assume one mesh across the whole check. Between the operations: `Settle()` runs 4x
`Dispatcher.RunJobs()` + `UpdateLayout()` (`:696`), which is where a thread-pool mesh completion is delivered.

First divergent value: Target.Z (0.0469 vs 0.0008) and Target.X (0.05957 vs 0.06). The draft mesh of the clamped twist moves the
tip trailing edge, so its box is taller in Z.

Necessary (Verified): adding `Pump(() => !Controller.SurfaceUpdating, ...)` to `Fixture.Reset` before the `Fit()` calls turns every
delay in the table (60, 100, 150, 200, 300, 800 ms) to PASS. Sufficient (Verified): the planted delay alone produces the failure
at 40-200 ms.

## 3. Ruled out (each with evidence)

- **Viewport size not settled (ResizeObserver-style).** Distance (the fit scale) is equal in expected and actual, and only the
  Target differs. `BandRect` comes from `Bounds` of a shown Window that `Settle()` has laid out. Not it.
- **Pending animation or dispatcher frame.** No planted-delay-free failure; with the mesh delayed 0 ms there is no failure; the
  drain alone removes it.
- **Pure CPU starvation.** Load 20-24 without ordering gave 0 of 32 failures. Load changes only how long the job takes.
- **Keyboard path vs pointer path differ.** The assertion that failed is the keyboard `⌘0` against the baseline; the `⌥→` failure
  at delay 200 is the same stale baseline. Both paths call `Fit()`/`SetCameraFor`.
- **Product bug (camera re-fits on a later mesh).** `ModelArea.CameraFor` fits only the first mesh (`current is not null`
  returns the user's camera). The product keeps the user's camera by design; no code path changes the elevation camera when a
  mesh lands.

Unverified (Inferred): the 2026-10-08 failure text was not kept, so I cannot prove that exact failure took this path. It
fits: failed in `--views --part=1/2`, as a camera mismatch after ⌘0, with the TwistDomainClamp check running directly before
(registration order, `p1.log` lines 69-71), and load 16. The assertion label "⌘0 fits" and the Target-only signature are what
this mechanism produces.

## 4. Class and sweep

**Class: FIXTURE-RESET-WITHOUT-DRAIN (a sibling of SECTION-EDITOR-LOAD-FLAKE).** A shared-fixture reset returns before a
background job of the previous check has landed, and the next check captures a baseline that depends on that job's output. Shape:
*baseline captured, then background work changes the input of the baseline's function, then the check recomputes and compares.*
Signature: a test-only failure at higher load, in a part but never alone, with a value difference in a derived quantity (here
the fit target), and no product defect.

Sweep (Verified by reading each; the planted delay across the whole `--views` suite at 40/100/200 ms hit only this check):

| Check or fixture | Exposed? | Why |
|---|---|---|
| `Elevation_ZoomPanFit_KeyboardAndPointerSameCamera` | Yes, the instance | stores a fit, then re-fits (lines 450, 454, 457) and expects the stored fit |
| `Elevation_ShiftDrag_..._EmptySpacePans` (`:358`) | No | `before` is taken after the drag and the pan is relative to the current camera; a mesh landing does not change the camera |
| `Elevation_Focus*`, `Side*` camera uses (`:123,:235,:273,:531`) | No for camera equality | they project through the current camera; no re-fit expected |
| Pixel-sampling checks after a mutating predecessor (`FrontBand_RenderedFromSurfaceView`, `LockedDihedralRoot`, flagged by `scratch/ezf/sweep.py`) | Possible, Inferred | a mesh landing between `Shoot()` and the read could move a sampled pixel; not reproduced by the 40/100/200 ms plants |
| `Fixture.Reset` in `SectionEditorTests.cs:1155` | Different fixture | already has the DragMove flake lesson; its constructor drains (`:1268,:1300`); `Reset` itself does not drain a surface (Inferred, not run) |
| `GroupDragTests.cs:224`, `PlanCanvasTests.cs:1099`, `View3dTests.cs:985` | No | they drain in the constructor; they do not re-fit after another check's mesh |
| `ControllerViewTests` | No | they `Await(WhenSurfaceSettledAsync())` before each read (lines 74-352) |

Markers harvested: `grep 'simplify:\|assume:'` in `ElevationTests.cs` and `ElevationView.cs` found none, so no triggered
marker. Register: append the instance to the SECTION-EDITOR-LOAD-FLAKE class or add the new class FIXTURE-RESET-WITHOUT-DRAIN;
not done here (the brief commits only `docs/proof/ezf/`). A recurrence of the same cause in a second fixture (Section Editor and
Elevation) says the control is wrong: both fixtures reset by hand and neither is checked.

## 5. Repair plan (for review)

**Which is right: the test.** The product is correct. It keeps the user's camera on later meshes, and changing that would make
users lose their zoom on every edit. The fix belongs where the contract is violated: `Fixture.Reset` promises "a shared window
back to its first state" (`:681`) but returns with a mesh in flight. Fixing the one check (drain after `Reset` inside ZoomPanFit)
would leave the other 28 `Reset()` users with the same hole.

| Phase | Scope (code + tests) | Failure mode removed | Validation | Depends on |
|---|---|---|---|---|
| P1 | `ElevationTests.cs`, `Fixture.Reset`: after `Select(Foil)` and before the first `Fit()`, `Pump(() => !Controller.SurfaceUpdating, "the last mesh")`. 1 line; verified green at 60-800 ms in the spike | the instance and every `Reset()` user | red-first control below, then the whole `--views` part | none |
| P2 | Red-first control (see below) as a named check | recurrence in `Reset` | observed failing on the unfixed `Reset`, then passing | P1 designed together |
| P3 | Class prevention: a lint in `tools/` (like `check-wallclock-asserts.py`) that fails a test-fixture `Reset()` in `tests/` which calls `.Fit()`/`Canvas.Fit()` with no `SurfaceUpdating`/`WhenSurfaceSettled` in the same method; run the same way for `SectionEditorTests.Reset` | the same shape in the next fixture | self-test red on a planted Reset without drain | P1 |
| P4 (optional, only if the lint finds one) | Drain in `SectionEditorTests` `Reset` | the Section Editor sibling | its own planted delay | P3 |

**Red-first control (P2).** A deterministic held seam, no sleep and no load: construct the Elevation `Fixture` with a
`SurfaceCompute` that returns a `TaskCompletionSource` task for basis "accepted" (the constructor parameter exists:
`WorkbenchController.cs:525`; today `Fixture` uses `new()`, so it needs an optional parameter). Steps: open the example, drag the
twist tip to the clamp (as in the TwistDomainClamp check), press Escape, so an "accepted" request is held; call `Reset()` on a
background release timed by the test: release the gate from the dispatcher after `Reset` has started pumping. Assertion:
`!Controller.SurfaceUpdating` after `Reset()`, and `Front.Camera == ElevationView.Fitted(Front.View, bounds of Controller.Surface, BandRect.Size)`.
On the unfixed `Reset`, `Reset` returns while the gate is held: `SurfaceUpdating` is true, and after release the camera is stale.
To make "unfixed" observable without hanging the test, release the gate on a one-shot dispatcher timer, not on the test thread:
the unfixed `Reset` returns first, then the check releases and fits again and sees the second fit differ. The planted
`EZF_DELAY_MS` spike is the proof the pattern fails before the fix (60-200 ms), and the held seam is its deterministic form.

**Ring and cost.** Ring: the Desktop `--views` part, run by every join that changes code (`tools/run-tests.sh`, AGENTS.md test
rings). Cost: P1 adds `SurfaceUpdating` check time ~0 ms when no mesh is pending, at most one mesh (13-24 ms) per `Reset`
when one is. The new check costs one Fixture (~1.5 s of window creation from `COST` lines; the shared `Example` fixture cannot
be reused because the controller needs the held compute) plus ~0.2 s. If that is too much, fold it into a one-line assertion
inside the existing ZoomPanFit check against the shared fixture and skip P2's separate fixture; the planted-delay run in
the recipe then stays the red-first record in `docs/proof/ezf/red-first.md`. P3's lint runs in `check-docs.py`, cost under 1 s
(file regex only).

## Residual risk and what would change the diagnosis

- If the next flake of this check shows a Distance difference (not only Target), the viewport-size hypothesis returns.
- The 2026-10-08 text was lost; the P1 drain makes the next occurrence either gone or a new, different failure.
- The pixel-sampling siblings are Inferred, not reproduced.
- Persona review: not run by me (investigator does not self-certify); Test Architect and SRE lenses are for the review stop.

## Not done

The defect-class register entry in `docs/lessons/defect-classes.md` (the brief limits this commit to `docs/proof/ezf/`), and
`docs/proof/ezf/red-first.md` (no fix landed, so no red-first run to record beyond the planted-delay table above).
