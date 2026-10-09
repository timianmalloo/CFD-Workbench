---
id: proof-msp-receipt
type: proof-pack
title: "MSP committed scale prints"
status: complete
summary: "Committed SCALE_CONTEXT, ITEM6 and P3 prints for the Windows scale run, the subset route measurement, and the control."
owner: "@trk-msp"
links:
  - { to: proof-wri-probe-windows-scale, rel: relates-to }
review-by: 2026-12-01
---

# MSP receipt: committed scale prints (Ruling 182)

Mac, Release, branch `feat/msp-scale-prints`. Raw output is beside this file.

## The lines (grep these on the PC)

| Line | Printed by | When |
|---|---|---|
| `SCALE_CONTEXT mode=<mode> RenderScaling=<s> PrimaryScaling=<s> WorkingArea=<w>x<h> UseLayoutRounding=<bool>` | `DesktopChecks.PrintScaleContext`, from each mode entry in `WorkbenchTests.cs` | once per mode, before the first check |
| `ITEM6 name=<PointAftInput\|Value_p_eta\|PART_TextPresenter> BoundsDIP=... PaddingDIP=... DigitsEndXDIP=... *Device=... RenderScaling=...` | `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` | before its assertion, three lines |
| `P3 layout=... PlanVisible=... ThreeDVisible=... ThreeDWidth=... PlanContentWidth=... RoundedFrame=... WindowBounds=... ClientSize=... RenderScaling=...` | `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` | before its assertion |

Field names follow the PC's `R179_SCALE_CONTEXT`, `R179_ITEM6` and `R179_P3`, with two differences: `WorkingArea` is `<w>x<h>` as
the brief set it (the PC printed the `x, y, w, h` rect), and `ITEM6` adds `PaddingDIP` and `DigitsEndXDIP/Device`. A field the
platform cannot answer prints `not-recorded`. The prints run on PASS and FAIL and change no assertion.

The modes: `section-editor`, `properties-view`, `shell-window`, `plan-canvas`, `views`, `analysis`, `properties-cells`,
`status-strip` (spawned), plus `section-canvas`, `catalog-dialog`, `theme-matrix` (run alone) and `readiness`.
`controller-shell` and `shell-model` open no window and print none. The in-process prefix of the default run does not print one
(its only window suite, `SectionCanvasTests`, also runs as `--section-canvas`).
`readiness` prints after `PlanCanvasTests.RunReadiness` because that call sets up Avalonia and a second setup throws; the
line precedes every check except that suite's two frame-time lines. A subset run (below) shows it before its first PASS.

## Real Mac lines (`desktop-all-modes.stdout.txt`, one default Desktop run, exit 0)

```
SCALE_CONTEXT mode=section-editor RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=properties-view RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=shell-window RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=plan-canvas RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=views RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=analysis RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x898 UseLayoutRounding=True
SCALE_CONTEXT mode=properties-cells RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x898 UseLayoutRounding=True
SCALE_CONTEXT mode=status-strip RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x898 UseLayoutRounding=True
SCALE_CONTEXT mode=section-canvas RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=catalog-dialog RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=theme-matrix RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x891 UseLayoutRounding=True
SCALE_CONTEXT mode=readiness RenderScaling=2 PrimaryScaling=1 WorkingArea=1512x889 UseLayoutRounding=True   (subset run)
P3 layout=ViewLayout { Arrangement = One, Single = ThreeD } PlanVisible=False ThreeDVisible=True ThreeDWidth=1132 PlanContentWidth=1134 RoundedFrame=1 WindowBounds=0, 0, 1400, 870 ClientSize=1400, 870 RenderScaling=2
ITEM6 name=PointAftInput BoundsDIP=139, 0, 62, 24 PropertiesOriginDIP=161, 126 WidthDIP=62 HeightDIP=24 PaddingDIP=3,3,3,3 DigitsEndXDIP=NaN PropertiesXDevice=322 PropertiesYDevice=252 WidthDevice=124 HeightDevice=48 DigitsEndXDevice=NaN RenderScaling=2
ITEM6 name=Value_p_eta BoundsDIP=0, 0, 33, 14 PropertiesOriginDIP=190, 108 WidthDIP=33 HeightDIP=14 PaddingDIP=0,0,5,0 DigitsEndXDIP=218 PropertiesXDevice=380 PropertiesYDevice=216 WidthDevice=66 HeightDevice=28 DigitsEndXDevice=436 RenderScaling=2
ITEM6 name=PART_TextPresenter BoundsDIP=0, 0, 52, 14 PropertiesOriginDIP=166, 131 WidthDIP=52 HeightDIP=14 PaddingDIP=n/a DigitsEndXDIP=218 PropertiesXDevice=332 PropertiesYDevice=262 WidthDevice=104 HeightDevice=28 DigitsEndXDevice=436 RenderScaling=2
```

(The Mac is 2x, so the item-6 and P3 values differ from the 150 % PC values; the PC run is the evidence for those. The
`PointAftInput` row has no digits-end because the digits belong to its presenter.) Each part of a split mode prints its own line.

## Item 5: the subset route

```
CFD_TEST_ONLY=ModelArea_Views_SeparatedByGutterAndFramed,Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870 \
  tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --readiness
```

Result (`subset.stdout.txt`): exit 0, exactly 2 checks ran (2 PASS lines, 2 COST lines), wall 8.9 s (an earlier run that hit
the double-setup crash, wall 2.8 s, is not counted). It also prints two report-only `READINESS Readiness_NewFoil...` lines (frame
and commit timing from `ControllerShellTests.RunReadiness` and `PlanCanvasTests.RunReadiness`); they are not checks and have no PASS.
So the PC runner needs no instrumentation for items 10 and 11: the subset selects exactly those two checks.

## Control, red-first, cost

`Spawn_WindowModeWithoutScaleContext_Fails`: ring fast (in-process, before the spawn), cost 1.1 ms. It runs the real `SpawnWith`
against a fake child, with and without the line. Red-first runs are in `red-first.md`: removing the `views` print makes the
default Desktop run exit 1 with `FAIL SCALE_CONTEXT --views --part=1/2 printed no SCALE_CONTEXT line before its first check`.
The control covers spawned modes only; the standalone and readiness modes are covered by this receipt, not by a check.

## Cost delta of the Desktop harness

One probe window (300x200, shown and closed) per mode process, 18 child processes. Default Desktop run wall, same build
tree, alternating: with prints 43.6 s and 50.2 s, without 42.8 s and 64.8 s. The load moved between runs (the second without-print
run was 14 s slower than the first), so the delta is below the noise; no cost shift is claimed. `tools/run-tests.sh`: exit 0,
`all test harnesses passed`, Desktop 712 PASS in 54 s, ring wall 60 s at load 29 to 41 (`run-tests.stdout.txt`; the four
`COST-MISS` lines are load-gated, not failures).
