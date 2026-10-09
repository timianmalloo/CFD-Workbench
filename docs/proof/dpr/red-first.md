---
id: proof-dpr-red-first
title: Track DPR red-first record (DPI-A items 3-5)
type: proof-pack
status: active
owner: "@trk-dpr"
phase: implementation
tags: [dpi-a, dpr, proof]
links:
  - { to: proof-wri-probe-windows-scale, rel: relates-to }
  - { to: review-pr-20, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Item 3 repaired with red and green runs; item 4 falsified (chip absent at device resolution); item 5 needs View3dTests.cs.
---

# Track DPR red-first (Ruling 178, DPI-A items 3-5)

Scale on this Mac: 2 (headless). Windows evidence: `docs/proof/wri-probe/instrumented.stdout.txt` lines 4, 13-14.

## Item 3 and the sampler: pure check `DevicePixel_RoundedAndDeviceSampler_MatchTheLoggedWindowsRun`
- RED (`red-pure-check.txt`): with `DevicePixel.Rounded` stubbed to return `dip` (the old `- 2` behaviour), the check fails:
  `1 DIP at 1.5: expected 1.3333333333333333, actual 1`.
- GREEN: with `Math.Round(dip * s, ToEven) / s`: PASS. It also asserts 1133.333 - 2 x Rounded(1, 1.5) = 1130.667 (the logged
  Windows actual), `NearestAtDevice` over the logged View3d device block finds (102,221,200), and the scale-1 block's
  centre pixel is 123 from StationColor.
- Real check `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack`: PASS on the Mac, with the new expectation.

## Item 4 (Elevation chip): NOT repaired - the brief's premise is falsified
Sampling `Shot.AtDeviceResolution` at the chip's left border fails on the Mac (scale 2) too:
`The chip has no station border (scale 2, (52, 75, 80) vs (102, 221, 200))`. A scan across and down the sample shows a flat
dark field and one faint grid line. `elevation-device-res-mac-scale2.png` (the whole window rendered at device
resolution) shows the Side view with **no station chips at all**, while a scale-1 shot draws them (the unchanged check
passes). The Windows 1.5 device block (`instrumented.stdout.txt:12`, max (52,75,80)) is the same field. So the Elevation
failure at 150 % is not a seam: the Side chips are absent from a device-resolution render. Cause not found within the
2-cycle cap (cycle 1: device sampler; cycle 2: scan + PNG diagnosis). ElevationTests.cs is reverted to main.

## Item 5 (View3d chip)
The check is `View3dTests.cs` (`View3d_SelectedStation_RenderedWidthAndChip`, line ~622), not an owned path. Not edited.
The helper `DevicePixel.NearestAtDevice` and the logged block (`instrumented.stdout.txt:14`, (102,221,200)) are ready for it.
