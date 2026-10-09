---
id: proof-v3d-red-first
title: Track V3D red-first record (DPI-A item 5)
type: proof-pack
status: active
owner: "@trk-v3d"
phase: implementation
tags: [dpi-a, v3d, proof]
links:
  - { to: proof-dpr-red-first, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Item 5 repaired: the View3d chip top border is sampled from a device-resolution shot with DevicePixel.NearestAtDevice.
---

# Track V3D red-first (Ruling 179 item 5, DPI-A)

Scale on this Mac: 2 (headless). Windows evidence: the 5 x 5 block at `ControllerViewTests.cs:611-621` (DPR), device row 646 at 1.5 is StationColor.

## Change (`tests/CfdWorkbench.Desktop.Tests/View3dTests.cs`)

- `Shot(Window, double scale = 1)` renders at `scale` device pixels per DIP and exposes `Scale`. `View3dTests.Shot` is private to its class, so it mirrors `ControllerViewTests.Shot` (`:1352-1374`) rather than sharing it; `ControllerViewTests.cs` is untouched.
- `Fixture.ShootAtDeviceResolution()` keeps a second shot (`deviceShot`); the 96-dpi `shot` stays, because `RgbAtView`, `Thickness` and the plate read address window DIPs.
- `Fixture.NearestAtDevice` calls `DevicePixel.NearestAtDevice(deviceShot.Rgb, ToWindow(local), deviceShot.Scale, target, 2)`.
- `View3d_SelectedStation_RenderedWidthAndChip` reads the chip top border with it (distance <= 30, as before).

## Red (pure form, the committed Windows block)

`View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock` feeds the block to both reads:

| Read | Pixel | Distance to StationColor (102,221,200) |
|---|---|---|
| Old `:609`, 96-dpi shot, floor(430.667) = 430 | (52,105,100) | 266 (> 30: the blend, fails) |
| New sampler, device shot at 1.5, radius 2 | (102,221,200) | 0 (found) |

The check asserts the old read misses and the new sampler returns StationColor exactly.

## Green on the Mac (scale 2), observed

- `CFD_TEST_ONLY=View3d_ChipBorderSampler tools/run-suite.sh dotnet <dll> --views`: exit 0, `PASS View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock`.
- `CFD_TEST_ONLY=View3d_SelectedStation_RenderedWidthAndChip ...`: exit 0, `PASS` (the real check, with the new capture).

## Other `RgbAtView` reads in the check (unchanged)

- Chip plate (`chip.Right - 2.5`, plate interior, uniform): not a seam; not expected to fail.
- `Thickness` (vertical run of a 3 DIP stroke, counted at 96 dpi): a count, not an exact colour; not expected to fail, but its `< 3` and `> root` bounds are unmeasured at 1.5.

Elsewhere in the file, exact-colour reads of 1 DIP features at `:560-563` (ring edge samples) could blend at 1.5 by the same mechanism. Not run at 1.5; findings only.

## The PC must see

`View3d_SelectedStation_RenderedWidthAndChip` PASS at 150 %.
