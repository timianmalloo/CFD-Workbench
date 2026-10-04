---
id: proof-spike-04-round3
title: SPIKE-04 round 3 verdict — searching for a monotone TMR grid family (OpenFOAM v2512)
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-04, openfoam, verification, gci, tmr, naca0012, spalart-allmaras, round-3, a4]
links:
  - {to: proof-spike-04-round2, rel: supersedes}
  - {to: plan-fluids-round3, rel: implements}
  - {to: rulings, rel: depends-on}
  - {to: adr-0012-openfoam-backend-macos, rel: relates-to}
  - {to: proof-spike-03-round3, rel: relates-to}
review-by: 2026-11-03
summary: >-
  DRAFT while R3-G2 (L3) runs. R3-G0 rejects H1: removing the limited laplacian moves L6 Cl by -1.03e-4, below the
  2.8e-4 threshold. Numerics cycle 1 (R3-G1b, limitedLinear 1 at relaxation 0.7) misses A4 on L6, on clause 3 (250
  clipped iterations in the window), so cycle 2 is not run. Per DR-F3-3 the L3 triplet runs with the D4 numerics
  under a 12 h cap.
review-suggested: []
---

# SPIKE-04 round 3 verdict — a monotone TMR grid family (DRAFT, R3-G2 running)

R3-G2 (L3) started at 19:14 UTC. This draft holds the finished parts. The GCI result follows when L3, L4 and L5 are
done.

## Runs so far

| Id (case file) | Grid · ranks | Change from D4 | Iterations · solver wall | A4 | Cl (window mean · U_I) | Cd (window mean · U_I) |
|---|---|---|---|---|---|---|
| R3-G0 ([g0-l6](../../../cases/spike04r3-g0-l6.yaml)) | L6 · 2 | laplacian `Gauss linear corrected`, snGrad `corrected` | 6,545 · 39 s | **met** | 1.051600898 · 7.9e-6 | 0.014812212 · 3.3e-7 |
| R3-G1b L6 ([g1b-l6](../../../cases/spike04r3-g1b-l6.yaml)) | L6 · 2 | nuTilda `bounded Gauss limitedLinear 1` at 0.7 | 40,000 (cap) · 203 s | **no**: clause 3 (250 clipped iterations in W; 5,013 of 40,000 overall) | 1.055092321 · 1.9e-6 | 0.015164504 · 8.3e-7 |
| R3-G2 L3 ([g2-l3](../../../cases/spike04r3-g2-l3.yaml)) | L3 1793×513 (917,504) · 6 | none (D4) | running | — | — | — |
