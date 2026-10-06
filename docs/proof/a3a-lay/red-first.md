---
id: proof-a3a-lay-red-first
title: A3a LAY red-first receipt
type: proof-pack
status: active
owner: "@trk-lay"
phase: implementation
tags: [a3a, lay, analysis, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: proof-a3a-lay-pack, rel: relates-to }
review-by: 2026-11-05
summary: >-
  The LAY Desktop ring was committed red before its three canvas-layer classes existed. A planted removal of both the
  dashed outside outline and its text count later made the named Plan test fail on the implemented code.
---

# Track LAY red-first receipt

Ring: Desktop `--analysis`. The three scene tests were written in the test-first commit `30f34085`; the focused run exited 1
with `CS0103` for the absent `PlanLoadLayer`, `View3dLoadLayer`, and `ElevationDepthLayer`. The code and tests first
passed together at `905d2608`. The real-window integration check was added with that implementation; its missing-name
mutant was committed red at `2e7aa924` and fixed green at `e0662238`.

| Test | What it catches | Red commit | Green commit |
|---|---|---|---|
| `PlanLayer_OutsideStrips_DashedOutlineAndCount` | A visible outside strip needs both a dashed outline and a text count; a hidden layer has no strips. | `30f34085` (missing class); planted mutant on the green tree: `FAIL` | `905d2608` |
| `View3dLayer_NormalAndMissingVector` | A lift arrow uses the run's local normal and skips absent vector data. | `30f34085` (missing class) | `905d2608` |
| `ElevationLayer_DepthUnsetNoBand` | Depth unset or hidden draws no band; a present margin remains available. | `30f34085` (missing class) | `905d2608` |
| `AnalysisLayers_WindowRendersAndPeersFollowVisibility` | The real shell renders the layers, names the canvas/table twins, hides a toggled layer, and measures the layers-on camera step and event with zero pane refresh. | `2e7aa924` (3D peer omitted its layer name); the first layers-on full-step measurement was 10.30 ms | `e0662238` |

## Planted outline mutant

In `PlanLoadLayer.Build`, set each strip's `DashedOutline` to `false` and the scene's `CountText` to `null`. The focused
command `CFD_TEST_ONLY=PlanLayer_OutsideStrips_DashedOutlineAndCount dotnet run -c Release --project
tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` exited 1 and printed
`FAIL PlanLayer_OutsideStrips_DashedOutlineAndCount Exception: Outside strip must have a dashed outline and a text count`.
Both edits were reverted; the same check then printed `PASS`.

The committed 3D peer mutant at `2e7aa924` removed “strip lift arrows” from the accessible name while leaving the
overlay and numeric twin visible. `AnalysisLayers_WindowRendersAndPeersFollowVisibility` printed `FAIL` with the
incorrect peer name. Commit `e0662238` restored the name and the check printed `PASS`.

The selected-run overlay paths were tested against a real `ShellHost` window. The camera measurement used the
four-view 1440×900 layout from `Readiness_CameraStep_NoPaneRefresh_Under8Ms`; the full-step p95 varied from 6.1 to
9.4 ms across focused runs, while event p95 stayed below 0.11 ms and pane refreshes stayed zero. The event path is
gated at 3 ms, and the official Desktop readiness check remains the 8 ms full-step gate. One focused `COST` was
2056.5 ms. The three scene checks cost 1.9, 0.8, and 0.4 ms respectively in that run. No new build or test file
was needed; the suite was already registered in `WorkbenchTests.cs`.
