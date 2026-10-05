---
id: proof-vlm-tip-study
title: "VLM tip-strip envelope study"
type: proof-pack
status: verified
owner: "@vlm3"
phase: implementation
tags: [analysis, vlm, tip-strip, camber, twist]
links:
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2027-04-04"
summary: "Measured tip-strip refinement, tolerance-law proposal, and the default lattice's camber and twist failure."
---

# Tip-strip envelope verdict — tolerance-law study (Ruling 75, DR-VLM-1)

Status: study, scratch only. Source: `feature/ui-cad-direction` at c4fc4ad (copied `src/CfdWorkbench.Core` and
`src/CfdWorkbench.Analysis` into scratch; no repo edit). Release build, .NET 10.0.203, osx-arm64, 2026-10-04.
Every number below was printed by `probe/` (`run-matrix.sh`, `run-falsify*.sh`, `run-nonplanar.sh`) and reduced by
`law.py` / `gap.py`. Confidence is **Verified** (observed) unless marked otherwise.

Lattice: the default, `Settings.Default` (n per half × 4 chordwise, cosine/cosine, wake 20 spans +x). Only n varies.
Wings: AR 8, half-span 1, straight quarter-chord line (xLe = −c/4), flat plate (α_L0 = 0), untwisted unless stated.
`ell` elliptic (c0 = 1/π, closing tip) · `rect` c = 0.25 · `tap` taper 0.5 (c_r = 1/3) · `tri` taper 0 (pointed, closing tip).

## 1. Measured α_i at the outermost (k1) and second (k2) strips, degrees

Strip width of k1 in η (all wings): 4.815e-3 / 1.205e-3 / 3.012e-4 / 7.530e-5 / 1.882e-5 at n = 16/32/64/128/256
(Δη·n² = 1.233 ≈ π²/8: Δη ∝ n⁻²). k1 midpoint: 1 − η = 2.41e-3 / 6.02e-4 / 1.51e-4 / 3.76e-5 / 9.41e-6.

| wing | α | k1: n16 / 32 / 64 / 128 / 256 | k2: n16 / 32 / 64 / 128 / 256 |
|---|---|---|---|
| ell | 2 | −0.442 / −1.090 / −2.032 / −3.227 / −4.563 | −0.012 / −0.427 / −1.091 / −2.059 / −3.255 |
| ell | 5 | −1.105 / −2.721 / −5.075 / −8.060 / −11.396 | −0.030 / −1.066 / −2.724 / −5.141 / −8.130 |
| ell | 8 | −1.764 / −4.346 / −8.103 / −12.870 / −18.198 | −0.048 / −1.702 / −4.349 / −8.210 / −12.982 |
| rect | 2 | 0.760 / 0.770 / 0.774 / 0.776 / 0.777 | 0.744 / 0.766 / 0.773 / 0.776 / 0.777 |
| rect | 5 | 1.898 / 1.923 / 1.934 / 1.939 / 1.941 | 1.857 / 1.914 / 1.932 / 1.938 / 1.941 |
| rect | 8 | 3.031 / 3.071 / 3.088 / 3.096 / 3.099 | 2.966 / 3.056 / 3.084 / 3.095 / 3.099 |
| tap | 2 | 0.667 / 0.684 / 0.691 / 0.694 / 0.695 | 0.639 / 0.677 / 0.689 / 0.694 / 0.695 |
| tap | 5 | 1.665 / 1.708 / 1.725 / 1.733 / 1.737 | 1.596 / 1.691 / 1.721 / 1.732 / 1.737 |
| tap | 8 | 2.659 / 2.727 / 2.755 / 2.768 / 2.773 | 2.549 / 2.700 / 2.748 / 2.766 / 2.773 |

Solve diagnostics: backward error below 10⁻¹⁰ in every run; 256 per half = 2,048 unknowns (at the cap), ≤ 6 s.

## 2. What the tip α_i scales with

1. **Not the strip, the tip shape.** With a finite tip chord (rect, tap) the outermost α_i converges on the default
   lattice: spread 32→256 ≤ 0.046°, k1 = k2 to 0.001° at 256. Γ_k1·n tends to a constant (rect 0.62→0.67), so the
   1/√ tip loading alone does **not** cause divergence. Strip width and log n are not the variable either.
2. **Closing tips (chord → 0) diverge** (ell; tri: k1 α_eff 9.5/11.1/12.9/14.9 at α 5, n 16–128). The divergence
   is in η, not in n: α_i converges at fixed η (at η* = 1 − 1.506e-4: n128 −5.546, n256 −5.548 at α 5; spread
   ≤ 0.005° at every α), and k1 at n ≈ k2 at 2n (−5.075 vs −5.141). The outermost midpoint walks to the tip as
   1 − η ∝ n⁻² into a singular profile α_i(η).
3. **Fit.** ell, all strips with 1 − η < 10⁻³: α_i = c0 + c1·ln(1 − η), c1/sin α = 22.50 °/ln at α 2, 5 and 8
   (exactly linear in sin α). Residual max 0.22 / 0.54 / 0.87°, rms 0.09 / 0.24 / 0.38° at α 2 / 5 / 8. The
   local slope keeps rising (α 5: 0.67 → 2.25 °/ln from n16 to n256), so a pure log is not the law and **no closed
   form τ(n) extrapolates**. The continuum tip value is unbounded (Inferred; whether this is lifting-surface physics
   at a closing tip or the swept-panel discretisation is not established — Hydrodynamicist).

## 3. Proposed law (planar lattice only — see §5)

Judge every strip outboard of a fixed station at that station.

- η* = ½(1 + cos(π/128)) = 1 − 1.506 × 10⁻⁴: the outermost midpoint of the verified default lattice (Ruling 76). A
  constant; it does not move with the run's n.
- α_i* = α_i at η*, linear in ln(1 − η) between the run's two strips that bracket η* (the outer two when η* is
  outboard of the run's k1 midpoint, i.e. n < 64).
- Every strip with η ≥ η* (k1 at n ≤ 64; k1 at 128; k1–k3 at 256) is judged on
  α_eff* = α + twist_s − α_i*. Equivalently, the lattice-scaled tolerance is τ_s(n) = α_i,s − α_i* (0 at n = 64).
- Error bar: U = c(n)·|α_i,k1 − α_i,k2| + 0.05°, c = 2.5 / 1.0 / 0.4 / 0.1 at n ≤ 16 / 32 / 64 / ≥ 128.
- Verdict: inside if α_eff* + U ≤ 10°; outside if α_eff* − U > 10°; otherwise "at the bound (±U)", shown provisional.

U is calibrated: smallest c covering every case is 2.39 / 0.83 / 0.37 at 16 / 32 / 64 (ell, rect, tap, tri). The
0.05° floor covers the finite-tip drift (max 0.046°). The k1−k2 gap is ~0 on finite tips, so U does not widen there.

| case | raw k1 α_eff n16 / 32 / 64 / 128 / 256 | law α_eff* ± U, verdict |
|---|---|---|
| ell α 2 | 2.4 / 3.1 / 4.0 / 5.2 / 6.6 | 3.2±1.1 … 4.22±0.18: in at all n |
| ell α 4 | 4.9 / 6.2 / 8.1 / **10.5 / 13.1** (flips) | 6.4±2.2 … 8.44±0.31: in at all n |
| ell α 5 | 6.1 / 7.7 / **10.1 / 13.1 / 16.4** (flips) | 7.96±2.74, 9.15±1.71, 10.07±0.99 at-bound; 10.55±0.34 OUT at 128, 256 |
| ell α 8 | 9.8 / **12.3 / 16.1 / 20.9 / 26.2** | 12.7±4.3 at-bound (n16); OUT 32–256 (16.86±0.57) |
| rect, tap α 2/5/8 | ≤ 5.4, stable | in at all n, U ≤ 0.08 for n ≥ 32 |
| tri α 5 | 9.5 / **11.1 / 12.9 / 14.9** / 11.0 | at-bound n16; OUT 32–256 |

Never inside at one n and outside at another, for n = 16…256. The raw verdict flips on ell at α 4, 5 and 8.

## 4. Falsifiers (a real excess the law must still flag)

- **rect α 18, flat** (tip genuinely at α_eff ≈ 11.1° at every n): law 11.02±0.41 / 11.15±0.08 / 11.14±0.05 /
  11.13±0.05 / 11.12±0.05, **OUT at every n**. A blanket per-n tolerance sized to stop the ell α 5 flip (≥ 8.7° at
  256) would read it inside: that is why τ is anchored to η* and U to the run's own tip gap.
- **ell α 14** (whole wing over): OUT at every n (α_eff* 22–29°).
- Cannot be built on this lattice: a twisted or cambered tip excess. See §5.

## 5. Where no law works: twist and camber (Blocker for the default lattice)

The horseshoe legs leave each bound point straight along +x. On a twisted or cambered panel the control point sits
off that plane by about slope·Δx/2. When the strip half-width Δy/2 is smaller than that offset (cosine tip strips:
Δy/2 = 1.5 × 10⁻⁴ half-span at n = 64), the strip's self-influence collapses and the solve is wild, while the
backward-error check passes.

| case, default chord law | n16 | n32 | n64 (default) | n128 |
|---|---|---|---|---|
| rect, 4 % parabolic camber, α 5: k1 α_i, ° | 18.1 | −1,696 | −2.34 × 10⁶ | 5.8 × 10⁸ |
| same: CL | 0.691 | 0.684 | **0.621** | 0.700 |
| rect, 1° linear washin, α 5: k1 α_i, ° | 7.9 | 51.0 | 725 | 29,943 |
| rect, 10° washin / tap, 8° washin, n256 | — | — | — | ANA-SOLVE-SINGULAR |

At n = 64 the cambered wing is wrong on strips k1–k9 (η ≥ 0.978, about the outer 2 % of span; |α_i| 2.3 × 10⁶° at k1 falling to 11.6° at k9), and κ₁ is 11,789
(the uniform-span 64 run gives 34, CL 0.686). Scratch experiment, not in src: put the horseshoes in the z = 0
plane and carry camber and twist only in the normals (AVL style). Camber then gives k1 α_i 4.50 / 4.61 / 4.66 / 4.68
and CL 0.690 / 0.685 / 0.6825 / 0.6812 (n16–128). 1° washin gives 2.366 / 2.371 / 2.374 at n64 / 128 / 256, and
n256 solves. So the cause is the non-planar legs, and a planar lattice removes it.

## 6. Recommendation to the operator

1. The tolerance law in §3 is defensible **only after** the lattice is fixed for non-planar panels (§5). Real foils
   are cambered, so today any α-verdict, and CL, near the tip of a cambered foil on the default lattice is not
   trustworthy. A law cannot repair a solve that is wrong by 10⁶°.
2. If the lattice is fixed: adopt §3. It reduces to the default-lattice verdict, flags real excess, and states U.
3. If the operator does not want η* (a convention, not physics): the honest alternative is "closing-tip strips
   outboard of η* are indeterminate". The study found no law that gives a physical tip verdict at a closing tip.
4. Separate flip, not in Ruling 75: the sweep part. `SweepOf` reads the leading chordwise panel's bound line, not the
   quarter-chord. On the elliptic wing (quarter-chord sweep 0) it reads 54° → 87° at k1, and the number of strips
   outside on sweep grows with n (α 5: 1 / 2 / 5 / 10 / 19 strips per half at n 16…256).

## 7. Residual risk

- U is calibrated on 4 planforms, flat, AR 8, nc 4 cosine. tri at n256 lost its tip strip to the 10 µm chord
  exclusion; its error, 0.22°, exceeds U (0.13°), and its fixed-η reference spread is 0.44°.
- Not covered: swept or dihedral wings, nc ≠ 4, uniform span spacing, other AR, α_L0 ≠ 0, near-surface/strut.
- Not measured: whether the elliptic upwash singularity is physical. No external lifting-surface reference was used.
