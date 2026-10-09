---
id: proof-ecr-red-first
title: Track ECR red-first record (DPI-A item 4)
type: proof-pack
status: active
owner: "@trk-ecr"
phase: implementation
tags: [dpi-a, ecr, proof]
links:
  - { to: proof-ecr-spike, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Pure check Elevation_ChipBorderSampler_HoldsHalfOfASplitLine: red with the old exact threshold, green with the half-line test.
---

# Track ECR red-first

Check: `Elevation_ChipBorderSampler_HoldsHalfOfASplitLine` (fast ring, no window, 0.2 s). It builds the 2/3, 1/2 and 1/3 splits of `#66ddc8` over the viewport colour.

- RED (`red.txt`): `HoldsHalfOf` stubbed to the old rule (`Distance <= 60`): `FAIL ... The 2/3 half of a split line is refused`.
- GREEN (`green.txt`): half-line rule: `PASS`. It also refuses the 1/3 half and a flat field.
- Real check `Elevation_SideSelectedStation_RenderedFullWeight`: PASS on the Mac before and after (exact at scale 1, so no Windows evidence).
