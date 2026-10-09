---
id: proof-fvt-red-first
type: proof-pack
status: active
owner: "@trk-fvt"
phase: implementation
tags: [dpi-a, fvt, proof]
title: FVT red-first - four-views arrangement assertions at a fractional scale (Ruling 179 item 2)
links:
  - { to: proof-wri-probe-windows-scale, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Item 2 repaired with a device-pixel tolerance; Rounded cannot state both axes; sweep of 23 sites found 2 more exposed.
---

# FVT red-first (track trk-fvt, T0)

Check: `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` (`tests/CfdWorkbench.Desktop.Tests/ControllerViewTests.cs`).
Windows evidence: `docs/proof/wri-probe/r177-150-views.stdout.txt:7` (`expected 647, actual 647.3333333333334`).

## Item 1 - red on the Mac, as a pure function

`docs/proof/fvt/pure.py` models the old assertion (`Near(req, act, 0)`) and the new one
(`Near(req, act, DevicePixel.Tolerance(scale, 0))`) with no window. Run: `python3 docs/proof/fvt/pure.py`, exit 0.

```
width: requested 647, actual 647.3333, scale 1.5: old=FAIL new=PASS | Rounded(647,1.5)=646.6667 (== actual: False)
height: requested 487, actual 486.6667, scale 1.5: old=FAIL new=PASS | Rounded(487,1.5)=486.6667 (== actual: True)
scales 1 and 2: tolerance 0, old == new on exact and off-by-one-device-pixel layouts
648 at 1.5: 972.0 488 at 1.5: 732.0 (whole device pixels, layout is exact)
```

Findings from the run:

- The height line fails too on the old code (487 vs 486.667), as the brief inferred. It was masked by the width line failing first.
  The height actual (730 px = 486.667) is the **Inferred** Avalonia value from the brief's arithmetic, not a logged number.
- **Why a tolerance and not `Rounded`:** `Rounded(487, 1.5)` equals the actual height, but `Rounded(647, 1.5)` = 646.667 (970.5 px,
  half to even = 970) while the logged width is 647.333 (971 px). The width rounds up, the height to even, so one `Rounded`
  expectation cannot state both. One device pixel (`DevicePixel.Tolerance`, 0.667 DIP at 1.5, 0 at 1 and 2) bounds both.

## Item 2 - the repair

Changed only the two `Near` lines (plus a two-line DPI-A comment above them): the tolerance `0` became
`DevicePixel.Tolerance(area, 0)`. The boundary meaning is carried by `AssertFourOrOne` and the table
`(648, 488, four) (647, 488, one) (648, 487, one)`, both unchanged. At 1.5, 648 and 488 DIP are 972 and 732 whole device
pixels, so the four-view case lays out exactly; 647 and 487 are half pixels and lay out within one device pixel of the request,
which is what the new line allows. The one-view result at 647 x 488 and 648 x 487 is still asserted by `AssertFourOrOne`.

Mac run at scales 1 and 2 (the scale is 1 headless; tolerance is 0 at integer scales, so the decisions are unchanged):

```
CFD_TEST_ONLY=ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView tools/run-suite.sh dotnet <Desktop.Tests.dll>
PASS ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView
```

(The wrapper exits 1 because the other harness modes print `FAIL SELECTOR ... matched no check`; the mode that owns the check passed.)
Scale 2 is not run headless on the Mac in this track; the pure model shows tolerance 0 at scale 2, so old == new there.

## Item 3 - sweep (tests/CfdWorkbench.Desktop.Tests/)

`Near(..., 0, ...)` count: 23 (15 in ControllerViewTests, 8 in ViewCameraTests). Requested size vs laid-out size:

| Site | Compares | Class |
|---|---|---|
| ControllerViewTests:640-641 (now :642-643) | requested 647 / 487 vs grid Bounds | **Exposed at 1.5, repaired here** |
| ControllerViewTests:1171-1172 | `host.Bounds` vs one view rect + 2 (frame) | Exposed at 1.5 (Inferred: the 1 px frame renders 1.333 DIP); not repaired, finding |
| AnalysisPanelTests:590 `Equal(HeightFor(host.Height), panel.Bounds.Height)` | computed DIP height vs laid-out | Possibly exposed (Inferred: depends on whether `HeightFor` is a whole device pixel); finding |
| ControllerViewTests:544 | two slots' laid-out heights, same row | Safe by construction (same row, same rounding); Inferred |
| ControllerViewTests:792 | slot height vs its drawing's height | Probably safe (both laid-out, drawing fills slot); Inferred |
| ControllerViewTests:785-786 | label inset 6 DIP | Safe: 6 x 1.5 = 9 whole device pixels |
| ControllerViewTests:791 | drawing offset 0 | Safe: zero |
| SectionForceViewTests:54 `Equal(464.0, view.Bounds.Height)` | 420 + 44 DIP | Safe: 696 whole device pixels |
| PropertiesCellsTests:423 `chevron.Bounds.Width != 10` | 10 DIP | Safe: 15 whole device pixels |
| ControllerViewTests:123, 260, 1510, 1520; :607-608 | model values or pure arithmetic | Safe: no layout |
| ViewCameraTests (8 sites) | camera math | Safe: no layout |

Left for the owner of those files: :1171-1172 and AnalysisPanelTests:590 (both need the PC's 150 % run to settle).
