---
id: proof-a3a-vlm-red-first
title: "A3a VLM red-first receipt"
type: proof-pack
status: active
owner: "@track-vlm"
phase: implementation
tags: [a3a, vlm, analysis, red-first, fixtures]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: note-area3-fixture-arithmetic, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The red run of the A3a VLM track. Planting the O(1) wake mutant (wake length per panel instead of per wing)
  turned F-6 red. The mutant was removed before the commit.
---

# A3a VLM red-first receipt

Design: `docs/design/area3-analysis.md` §13.2 F-6 and §18.2 (VLM). Session `track-vlm`, 2026-10-04.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| `F6_ObservedOrder` | wake length = `WakeSpans * span / nStrips` for every lattice, not only the planted `LatticePlant.WakePerPanel` path | `FAIL F6_ObservedOrder InvalidOperationException: p(CL) -0.63208432840675044 outside [0.8, 1.2]` | mutant removed, `PASS` (p(CL) = 1.0696906502361061) |

The same check, on the unplanted lattice, also builds the wake-per-panel trio and fails the run if that order stays inside 1 ± 0.2. F-2 fails its run if the mid-panel control-point mutant's Richardson CL stays inside [0.4156, 0.4198].
