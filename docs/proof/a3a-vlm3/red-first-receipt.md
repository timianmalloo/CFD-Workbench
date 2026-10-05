---
id: a3a-vlm3-red-first
title: "VLM-3 red-first receipt"
type: proof-pack
status: verified
owner: "@vlm3"
phase: implementation
tags: [analysis, vlm, red-first]
links:
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2027-04-04"
summary: "The three VLM-3 fixtures fail on the old camber-surface horseshoes and front-bound sweep."
---

# VLM-3 red-first receipt

Before the lattice or sweep repair, `CFD_TEST_ONLY=F18_,F19_,F20_` on the Analysis
Release harness exited 1 (8.656 s wall, 2026-10-04, macOS arm64, .NET 10).

| Test | Red result | Planted product mutant after repair |
|---|---|---|
| `F18_Camber4_DefaultLatticeTipConverges` | n64 tip α_i −2,341,626.45°; CL 0.620533; κ₁ 11,788.98; n256 raw residual 1.07e−7; `FAIL` | Return horseshoe legs to the camber-surface bound points. |
| `F19_Washin1_DefaultLatticeTipConverges` | n64/n128 tip α_i 725.306°/29,942.931°; `FAIL` | Return horseshoe legs to the twisted bound points. |
| `F20_EllipticStraightQuarterChord_SweepZero` | n16 maximum strip sweep 54.1231°; `FAIL` | Read sweep from the front chordwise panel's bound line. |

The original checks measure the default 4-panel cosine chord law and cosine span law.
F18 and F19 measure n32, 64, 128, 256 and compare n64 with n128. F20 checks every
strip at n16, 32, 64, 128, 256.

Correction: the first red harness line called `RunDiagnostics.ResidualInf` a backward error. That member is the raw
‖AΓ − b‖∞ residual. `SolveDense` separately computes the normalized backward error and refuses values above 1e−10.
The old n256 camber solve passed that normalized gate despite the large raw residual; the tip α_i and CL checks
still made F-18 red. The green fixture now reports `residualInf` by its actual name.
