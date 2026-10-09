---
id: investigation-pce-escape-flake
title: Investigation PCE - PlanCanvas_Escape_DismissTooltipThenClearSelection fails when any refresh lands after the first Escape
type: investigation
status: draft
owner: "@trk-pce"
phase: investigation
tags: [pce, flake, desktop-tests, plan-canvas, tooltip, surface]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: investigation-ezf-zoompanfit, rel: relates-to }
review-by: 2026-11-08
summary: >-
  PlanCanvas dismisses its hover tooltip on Escape by nulling TooltipText, but every controller refresh (UpdatePlan) re-reads
  the saved hover position and rebuilds the tooltip. A mesh completion (or the 250 ms behind timer) that lands in the
  dispatcher pump after the first Escape brings the tooltip back, so the first assertion fails. Verified with a held mesh seam
  (fails 1 of 1 without a fix, passes with a spike fix). The natural trigger was not reproduced in 106 runs, so the link to the
  RG4 field failure is Inferred. The defect is in the product; the red-first control is a held-seam check.
---

# Investigation PCE: PlanCanvas_Escape_DismissTooltipThenClearSelection

Status: draft, stopped for review. No fix landed. Worktree `/Users/mallalieut/projects/CFD-Workbench-inv-pce-escape-flake`,
branch `inv/pce-escape-flake`, base main `7edaccdb`. The failure text of the RG4 failure (2026-10-09, load 14 to 19) was not
kept, so every link from the mechanism below to that one failure is Inferred; the mechanism itself is Verified.

## Root cause in one paragraph (Verified for the mechanism, Inferred for the field trigger)

`PlanCanvas.HoverAt` saves `lastHover`, hit-tests, and sets `TooltipText` (`src/CfdWorkbench.Desktop/PlanCanvas.cs:218-226`).
Escape dismisses the tooltip by setting `TooltipText = null` (`:624`). Nothing records the dismissal. `UpdatePlan` runs on
every controller `Changed` event (`:76`, `:697`); when the gesture is idle and `ProbeText` is set, it calls
`HoverAt(lastHover)` again (`:722`). That rebuilds the tooltip from the unchanged hover position. So any `Notify` after the
first Escape and before the assertion resurrects the tooltip, and `First Escape did not dismiss tooltip while keeping
selection` throws (`tests/CfdWorkbench.Desktop.Tests/PlanCanvasTests.cs:673`). The only pump between the Escape and the
assertion is `PlanFixture.KeyDown`'s `Settle()` (10 `RunJobs`, `:1496-1507`, `:1368-1375`). A mesh job still in flight at the
end of the fixture constructor completes there: `RunSurfaceAsync` posts `CompleteSurface` to the UI thread
(`WorkbenchController.cs:861`, `:864-895`), which ends in `Notify()` (`:893`). The 250 ms `surfaceBehindTimer` also posts a
`Notify` (`:834`). The fixture constructor does not wait for the mesh (`PlanCanvasTests.cs:1402-1414`; `SurfaceWanted` is true
in the Plan fixture, shown by the spike print `wanted=True`).

This is not the Section Editor cause by name, but it is the same family: a background surface completion lands inside a
measured window. The difference is the reader. There the reader counted notifications. Here the reader is product code that
turns a notification back into visible state.

## Q1. Reproduce

Recipe (scratch, all under `/Users/mallalieut/projects/cfd-workbench-continuation/scratch/pce/`):

- `repro.sh <hogs> <runs>` runs the single check `<runs>` times beside `<hogs>` busy-loop processes. Exact command inside it:
  `CFD_TEST_ONLY=PlanCanvas_Escape_DismissTooltipThenClearSelection tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --plan-canvas`
  (`--plan-canvas` is the mode flag, `WorkbenchTests.cs:88`).
- `repro-part.sh <hogs> <runs> <part>` runs a whole `--plan-canvas --part=N/2` beside the hogs.
- `apply-spike.py <test file> held|natural` plants the held seam or only prints the surface state. It is applied to the test
  file, built to a scratch output (`dotnet build -c Release ... -o scratch/pce/spike-bin`), then reverted with `git checkout`.
  `apply-product-fix.py` is the spike fix. `product-fix-spike.diff` is its diff.

Natural runs (the machine has 16 cores; ambient load was 5 to 25 and, from other tracks, 80 to 110 during the part runs):

| Run | Hogs | Runs | Fails |
|---|---|---|---|
| single check, release dll | 0 | 10 | 0 |
| single check | 16 (load 24) | 15 | 0 |
| single check | 48 (load 66) | 15 | 0 |
| single check, instrumented dll | 0 / 32 / 96 (load up to 150) | 20 / 20 / 20 | 0 |
| `--plan-canvas --part=1/2` and `2/2` (ambient load 81 to 111) | 0 | 1 + 1 | 0 |
| `--plan-canvas --part=2/2` | 48 (load 78 to 103) | 5 | 0 |
| **Total natural** | | **106** | **0** |

Why none failed: the instrumented runs print `WhenSurfaceSettledAsync().IsCompleted` at the end of the fixture constructor and
again after `HoverAt`. It was already `True` in 60 of 60 runs, including at load 150. The mesh (13 to 24 ms, comment at
`WorkbenchController.cs:664`) finishes during the constructor's frame render (`AssertGlyphPixel`), before the test body
starts. The window is real but narrow, and load did not widen it in this machine's single process. This matches the earlier
investigations: the cause is a race, and CPU load is only the amplifier. A reliable natural reproduction was not found.

Held seam (the deterministic reproduction): the constructor's `surfaceCompute` awaits a `TaskCompletionSource`; the test
releases it right after `HoverAt` and sleeps 400 ms so the completion is queued, then presses Escape. Result: 1 run, 1 fail.
Full output (`scratch/pce/spike-held.txt`):

```
PCE surfaceSettled=False wanted=True
PCE tooltip-before-escape=True
PCE after-first-escape tooltip=Trailing edge, point 5 of 7, control point, from root 315.00 mm, aft 120.00 mm surfaceSettled=True
FAIL PlanCanvas_Escape_DismissTooltipThenClearSelection Exception: First Escape did not dismiss tooltip while keeping selection
STACK PlanCanvas_Escape_DismissTooltipThenClearSelection System.Exception: First Escape did not dismiss tooltip while keeping selection |    at CfdWorkbench.Desktop.Tests.PlanCanvasTests.<>c.<Run>b__0_39() in .../PlanCanvasTests.cs:line 682 |    at CfdWorkbench.Desktop.Tests.DesktopChecks.Check(String name, Action assertion) in .../WorkbenchTests.cs:line 727
```

(The line number 682 is the spike's shifted copy of the throw at `:674`.) The tooltip text is back and `surfaceSettled`
went from False to True inside the Escape's `Settle`. This is the first value that diverges: `TooltipText` is non-null after the
first Escape, and the only event between is `CompleteSurface`'s `Notify`.

## Q2. Mechanism

What the check asserts: after `SelectPoint` and `HoverAt` on trailing point 5, Escape 1 sets `TooltipText` to null and leaves the
selection (`:622-628`: the first branch is the tooltip, so the selection branch is skipped); Escape 2 finds no tooltip, no
gesture, no handle focus, and runs `Controller.Select(new Selection.Foil())` (`:630`).

What runs between the two presses (Verified from source): the handler's `InvalidateVisual`, then `Settle()`: 10 passes of
`Dispatcher.UIThread.RunJobs()` plus `UpdateLayout()`. No tooltip timer exists: `TooltipText` is a plain property and
`PlanCanvas.cs` has no `DispatcherTimer` (grep for `Timer` returns nothing). Hover state is the saved `lastHover`. The background
sources that can post a `Notify` into that pump: the mesh completion (`:861`), the behind timer (`:834`), and any
`SizeChanged` (`:165`, which calls `UpdatePlan` directly).

Proof by a planted delay (necessary and sufficient, for this path):

- Necessary: with the mesh held and released before the first Escape, the check fails (above). With no hold, and the mesh
  settled before the Escape, it passes (3 of 3 instrumented runs print `after-first-escape tooltip=null`).
- Sufficient fix of that path: the spike product fix below makes the held-seam run pass:

```
PCE surfaceSettled=False wanted=True
PCE tooltip-before-escape=True surfaceSettled=False
PCE after-first-escape tooltip=null surfaceSettled=True
PASS PlanCanvas_Escape_DismissTooltipThenClearSelection
```

Only the first assertion can fail by this mechanism. A notification that lands after Escape 1's `Settle` returns cannot change the
outcome: Escape 2's handler runs before any pump, sees `TooltipText == null`, and clears the selection. So the field failure,
if it is this mechanism, was the message `First Escape did not dismiss tooltip while keeping selection`. Inferred, because the
RG4 failure text was not kept.

## Q3. Class and sweep

Class, proposed name: **TRANSIENT-STATE-REDERIVED · state a user gesture clears is rebuilt by the next refresh from a saved input.**
Signature: a view clears a derived value (tooltip, probe, hover ring) on a key; a refresh path recomputes it from a saved
position (`lastHover`) with no record of the dismissal. In the product this is a bug an operator can see: press Escape over a
point, then change a layer, resize, or wait for a mesh, and the tooltip returns with the pointer still. In the test it reads as a
flake because a background completion decides whether the refresh lands in the window.

The race itself is the same sub-class as `SECTION-EDITOR-LOAD-FLAKE` (root cause, trk-fss): a background completion inside a
measured window, with load only the amplifier.

Sweep of checks that use hover or tooltip state, a timer, or a two-step key gesture:

| Check (file:line) | Shape | Exposed? |
|---|---|---|
| `PlanCanvas_Escape_DismissTooltipThenClearSelection` (`PlanCanvasTests.cs:666`) | hover, Escape, assert, Escape | Yes. Subject of this report. |
| `PlanCanvas_HoverPoint_TooltipCopyAndRing` (`:368`) | hover, `Settle`, assert tooltip text present | No. A refresh rebuilds the same text, so a late notify cannot remove it. |
| `PlanCanvas_HoverRail_TracingProbeReadout` (`:380`) | hover, assert `ProbeText` immediately | No. No pump between. |
| `PlanCanvas_ProbeAndDelta_NotLiveRegions` (`:589`) | hover, assert automation properties | No. |
| `PlanCanvas_EscapeOnHandle_FocusBackToPoint` (`:835`) | Escape once, assert focus; no tooltip involved | No. Single press, no tooltip branch (no hover). |
| `PlanCanvas_ReleaseEdgesCross_...` Escape in drag (`:1154`) | gesture Escape, then `WaitGesture`, `Settle` | No. It waits for the gesture end before asserting. |
| `PlanCanvas_DragEmptyCanvas_...` (the pan-cancel check) | pan Escape cancel | No. Cancels a pan, not the tooltip. |
| `Elevation_*Probe*` (`ElevationTests.cs:533-589`) | `HoverAt`, read `ProbeText` at once | No. The view re-hovers only on its own paths (`ElevationView.cs:486`, `:888`), and the checks read before any pump (the comment at `:532` already names GUI-AMBIENT-INPUT). Elevation has no Escape-dismiss of a probe: not checked beyond this grep. |
| Other `Key.Escape` sites (Catalog, PropertiesCells, PropertiesView, StatusStrip, SectionEditor, Shell, Windows shell) | text-box or dialog cancel, single press | Not assessed for this class. No tooltip or hover state; none was opened line by line. Inferred from the grep only. |

Fixtures that start a mesh and do not drain it at construction: `PlanFixture` (this one). `Fixture.SettleSurface` exists for the
Section Editor (`SectionEditorTests.cs:1168`) and EZF added a drain to `Elevation Fixture.Reset`. The Plan fixture has none.

## Q4. Repair plan, for review

**Where the fix goes: the product.** The test is correct: it states the user-visible rule (Escape dismisses the tooltip; the next
Escape clears the selection). Draining the mesh in `PlanFixture` would make this one check pass every time but would hide a
real defect: in the app, any refresh after Escape (a mesh landing, a layer toggle, a resize) brings the tooltip back under a
still pointer. A test-side drain alone is the wrong layer: it removes the symptom and keeps the defect.

Phases (proposed, none done):

1. **Red first.** Add `PlanCanvas_Escape_TooltipStaysDismissedAcrossRefresh`. It builds a `PlanFixture` whose controller gets a
   held `surfaceCompute` (the constructor already takes one; `PlanFixture` needs a `Held` parameter, as `Fixture.Held` does in
   `SectionEditorTests.cs:325`), hovers, presses Escape once, then releases the hold, lets the completion run, and asserts
   `TooltipText is null` and the selection kept. Without the product fix it fails with `TooltipText` set (the output above);
   with it, it passes with no load and no sleep (release, then `Settle`). Order matters here: Escape first, completion after, so
   it asserts the user-visible rule rather than a window race.
2. **Product fix.** Record the dismissal: `tooltipDismissed` set on the Escape branch, cleared by the next `HoverAt` from a real
   pointer move; the refresh path (`UpdatePlan`) calls a private `ReadHover` that refreshes `ProbeText` and `hoveredPoint` but
   keeps `TooltipText` null while dismissed. The spike (`scratch/pce/product-fix-spike.diff`, 4 edits in `PlanCanvas.cs`) made the held
   run pass. Open question for the spec owner: should the probe line (`ProbeText`) also stay after Escape? The spike keeps it,
   which is today's behaviour on the refresh path.
3. **Prove.** Run the existing `PlanCanvas_Escape_*` and `PlanCanvas_Hover*` checks and `--plan-canvas` in full. Run the new check
   and the old one 20 times each at 48 hogs.
4. **Optional, test-side only after 1 and 2:** call a settle-surface drain in `PlanFixture` so the other Plan checks do not carry a
   mesh into their window. Only if a second Plan check shows the same shape; not needed for this defect (YAGNI).

Ring and cost: the new check is a Desktop check in `--plan-canvas` (one of the `run-tests.sh` Desktop parts), so the join ring
runs it on every code-changing join. Cost: one fixture and one `Settle`, 1.1 to 1.8 s measured for the existing check alone
(`COST` lines: 1252, 1111, 1105 ms; held spike 1777 ms including its 400 ms sleep, which the real check does not need). No
change to the ring budget.

Not done and why: the natural failure was not reproduced (0 of 106), so the claim that the RG4 failure was this exact path is
Inferred. The next real failure will show the message; the first-assertion text `First Escape did not dismiss tooltip while
keeping selection` would confirm it. If the second assertion text appears instead, the mechanism here is wrong and this report
needs reopening. No guard for the class in `tools/check-docs.py` is proposed: a check that reads state a key cleared is not
greppable.

Exit evidence is in the track Return.
