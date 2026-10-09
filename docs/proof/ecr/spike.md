---
id: proof-ecr-spike
title: Track ECR spike - Elevation overlay absent from device-resolution captures (DPI-A item 4)
type: proof-pack
status: active
owner: "@trk-ecr"
phase: implementation
tags: [dpi-a, ecr, proof]
links:
  - { to: proof-dpr-red-first, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Verdict: capture defect, Inferred. At scale 1.5 and 2 a RenderTargetBitmap drops what the Overlay child visual draws, with or without opacity and clip; the repair is a 96-dpi sampler that accepts a split line.
---

# Track ECR spike (Ruling 179 item 4)

Machine: this Mac, window `RenderScaling` 2 (real platform window; `Screens.Primary.Scaling` reads 1). Probe: a scratch method in
`ElevationTests.cs` and temporary edits to `ElevationView.cs`, both reverted (`git checkout`); nothing from the probe is committed.

## 1. Reproduce (Verified, probe output)
Window rendered with `RenderTargetBitmap` at scale s, tip station selected, Four views. Ink = pixels more than 30 (sum of channels) from the viewport colour.

| scale | Side chip border (nearest to `#66ddc8`, radius 2) | chip text ink | Side lane caption ink | Front starboard ink | Side band (`Render`: ground line, foil) ink |
|---|---|---|---|---|---|
| 1 (`Shot.Of`) | (102,221,200) = exact | 280 | 1905 | 245 | 13485 |
| 1.5 | (52,75,80) = flat field | 60 | 0 | 0 | 18294 |
| 2 (`Shot.AtDeviceResolution`) | (52,75,80) = flat field | 80 | 0 | 0 | 30940 |

Image of the scale 2 window: `elevation-device-res-s2.png` (no Side chips, no lane, no caption, no starboard/port; the Plan view's own chips and the View3d overlays are present).

## 2. Isolate (Verified)
| variant (scratch edit of `ElevationView.cs`) | scale 1 chip / caption | scale 1.5 | scale 2 |
|---|---|---|---|
| base | present / 1905 | absent / 0 | absent / 0 |
| no `PushOpacity` (`:1115`) | present / 1905 | absent / 0 | absent / 0 |
| no chip `PushClip` (`:1199`) | present / 1905 | absent / 0 | absent / 0 |
| neither | present / 1905 | absent / 0 | absent / 0 |
| `RenderOverlay` called from `ElevationView.Render` (the child draws nothing) | present / 1905 | chip present (102,221,200), caption 0 | chip present (102,221,200), caption 0 |

- The lane caption and starboard/port are drawn outside both layers (`:1124`, `:1130`) and are absent too, so the opacity and clip hypothesis is **falsified**.
- `Overlay.Render` runs (`owner.RenderOverlay` was called five times per capture) and the child is arranged (`:1076`, bounds 563 x 394.5), yet a red rectangle drawn first, outside every layer, is absent at 1.5 and 2 (red 0 / lime 0 pixels; at scale 1: 1031 / 961). A blue rectangle inside `PushClip(band)` survives in a Side-only render and in the window render at 1.5, so what is lost depends on position, not on the layer.
- A grid of 6 DIP squares over the Side overlay, Side-only render: all present at 1.5; at 2 only x up to about 499 and y up to about 340 DIP of 563 x 394 (`ECR-REDBOX`). In the window render at 1.5 only squares in the lower right; at 2 none. So the child's content is dropped by region, increasing with scale and window offset: a renderer culling or clip defect in the one-shot `RenderTargetBitmap` path for a child visual. The exact line in Avalonia 11.3.14 is **not located** (Inferred).
- Drawing the overlay from the view's own `Render` makes the chip appear but not the caption, so the region loss also reaches the view's own drawing (Inferred: same culling).

## 3. Capture or product (Inferred)
- Not Verified: an OS screenshot. `screencapture -x` exits 0 but shows only the desktop picture; the test window is not on screen (`scratch/ecr/screen.png`, not committed).
- Reason: the live window is drawn by the compositor, not a one-shot `RenderTargetBitmap`; the same `Overlay` draws at scale 1 in the same capture; View3d and the Plan canvas draw correctly in the very same device-resolution captures; the failure moves with the capture's scale and the region, not with the drawn content. All point to the capture path. **Residual risk:** if the compositor shares the culling defect, chips are missing on a Retina screen. Close it with one look at the running app (Four views, Side) on this Mac; it costs a minute and is the operator's call.

## 4. Repair
No device-resolution capture contains the overlay, so item 4 samples a 96-dpi capture, where it is present, and tests the chip border for **at least half the line colour**: a 1 DIP line at a fractional position splits over two pixels and the better holds at least half (`DevicePixel.HoldsHalfOf`). The chip check at `ElevationTests.cs` (`Elevation_SideSelectedStation_RenderedFullWeight`) uses it. A pure check names it (`Elevation_ChipBorderSampler_HoldsHalfOfASplitLine`). `DevicePixel.NearestAtDevice` is not used: it needs a device-resolution capture that has the chip, and none does.
Limit: on the Mac the chip check was green before and after (scale 1 is exact); the Windows 1.5 proof is the PC ring.

## Seam request
None. `Shot` in `ControllerViewTests.cs` needs no change for item 4; item 5's View3d chip is unaffected (View3d is present at device resolution).
