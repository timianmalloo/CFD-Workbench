---
id: proof-m12c-certificate-spike
title: "GSPK — per-surface certificate spike verdict (M1.2c)"
type: proof-pack
status: in-review
owner: "@track-gspk"
phase: implementation
tags: [m1.2c, certificate, b6, per-surface, gspk, spike]
links:
  - { to: design-m12c-section-editor, rel: depends-on }
  - { to: note-m12c-rulings, rel: depends-on }
  - { to: adr-0005-point-types, rel: depends-on }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
review-by: 2026-10-31
summary: >-
  NO-GO, measured at stage 1. The x-overlay certificate admits per-surface bases on one profile (F1 at 2 m:
  17.6 M bit-work, 0.06 s, 7 atoms). It cannot enclose the blend maximum max T0 between profiles whose abscissae
  differ (F4 at 2 m) within the 1e9 bit-work, 4,096-atom and 1 s budgets. Per OD-4 a, SPT builds the paired
  list (SPTF), and DR-11 goes back to the operator with these numbers.
review-suggested: []
---

# GSPK — per-surface certificate spike verdict

**Verdict: NO-GO**, measured at stage 1 on F4 at 2 m. The design row says a measured no-go there ends the spike, so
stage 2 (F2, F3, F5) was not run.

- **What SPT builds:** the **SPTF** paired list (OD-4 a, ruled 2026-10-03). The SPTG list and GCRT are cut.
- **DR-11** goes back to the operator with the numbers below.
- **What failed:** the blend. The x-overlay certificate for **one** profile with per-surface bases passes with a wide
  margin (F1). What fails is the Rule A blend maximum max T0 between two profiles whose abscissae differ (F4, the B6
  case), uniformly in w at a 2 m chord.

Environment: this Mac (Apple silicon), .NET 10.0.203, Release. The probe is at `probe/`. It runs against Core at
`f174864`, the worktree base; no src/ or tests/ file changed. The oracle is the as-built admission proof code, called
by reflection: `PlacementWidth` (Geometry.cs :501), `BlendPlacementWidth` (:545) and `QueryFeasibility.Prove` (:634).
Work is the as-built bit-work unit (`Rational.Work`) under a real `ProofBudget` (limit 1e9). Times are warm (the
second of two passes). Cold times are in `output/stage1.log`.

## 1. Results against the §3.5 go criteria (stage 1)

| Fixture | Separation | max (u − l) / max T0 | Admission width | Atoms (cap 4,096) | Work (limit 1e9) | Time (1 s) | Within budget |
|---|---|---|---|---|---|---|---|
| **F1** section-a, upper anchor at 35 %, 120 mm | certified (nose line, TE wedge, 4 atoms) | [0.121245830646755, 0.121245830646843], gap 7.29e-13 | `PlacementWidth` 8.78e-14 m | 7 | 17,657,211 | 0.062 s | **yes** |
| **F1**, 2 m | certified | same | `PlacementWidth` 8.07e-13 m | 7 | 17,609,868 | 0.064 s | **yes** |
| **F4** +0.02 at vertex 3, 120 mm, δ = 1e-12 | certified (both profiles) | refused, `GEOMETRY-BUDGET` at the work limit (gap 2.1e-9) | — | 601 when refused | 1.0e9 | 2.2 s | no |
| **F4** +0.02, 120 mm, per-document δ = 5.21e-9 | certified | certified, gap 5.20e-9 | `BlendPlacementWidth` at the achieved gap: 9.99e-9 m | 354 | 651,946,475 | **1.36 s** | **no (time)** |
| **F4** +0.02, **2 m**, per-document δ = 3.12e-10 | certified | **refused**, `GEOMETRY-BUDGET` at the work limit (gap 2.1e-9) | — | 601 when refused | 1.0e9 | 2.0 s | **no** |
| **F4** +0.20, 120 mm, per-document δ = 5.21e-9 | certified | **refused**, `GEOMETRY-BUDGET` at the work limit (gap 1.4e-7) | — | 681 when refused | 1.0e9 | 1.8 s | **no** |
| **F4** +0.20, **2 m**, per-document δ = 3.12e-10 | certified | **refused**, `GEOMETRY-BUDGET` at the work limit (gap 1.4e-7) | — | 681 when refused | 1.0e9 | 1.8 s | **no** |

The constant tolerance (δ = 1e-12, which today's `BlendPlacementWidth` assumes) is refused at the work limit for
every F4 case. The per-document δ is the largest subdivision gap that `BlendPlacementWidth` admits for that
document. It is derived with a replica of `BlendPlacementWidth` in which only the 1e-12 constant is a parameter. The
replica is **bit-equal** to the as-built method at 1e-12 on every F4 run (`replica equal: True`).

**What the 2 m cases would need.** These numbers come from a diagnostic run, outside the criteria, with the as-built
δ, the work limit raised to 5e10 and no atom cap:

| F4 at 2 m, as built | Atoms | Work | Time | Envelope lines stored | Query ops |
|---|---|---|---|---|---|
| +0.02 | 1,416 | 2.45e9 (2.5 × the limit) | 4.6 s | 5,652 | 493,808 |
| +0.20 | **12,602 (3.1 × the cap)** | **2.21e10 (22 × the limit)** | **39.9 s** | 50,396 | 851,760 |

So +0.20 at 2 m fails on the **atom count**, not only on work. A cheaper atom (§4) cannot rescue it under the as-built
admission proof.

## 2. Derived tolerance per document (closes the §3.5 Inferred figures)

| Case | δ needed, measured | §3.5 reduction (Inferred) |
|---|---|---|
| single profile, 0.12 m | 1.67e-7 | 1.7e-7 — confirmed |
| single profile, 2 m | 1.00e-8 | 1e-8 — confirmed |
| blend, 0.12 m | 5.21e-9 | — |
| blend, 2 m | **3.12e-10** | 1.9e-10 — the reduction was 1.6× conservative |

The figures are derived from the certified chord and thickness hulls, as §3.5 requires. The probe uses one constant
(1e-12) for its single-profile target and for the `BlendPlacementWidth` replica. The three Core uses (:946, :540,
:384) are untouched, because GSPK writes nothing in src/.

## 3. The tightened bound (a proposal that needs its own review; not the go criterion)

§3.5 allows GSPK to propose tightening the bound. The probe measured two changes in the replica:

- the max T0 floor becomes **1/2 − width**: a convex combination of unit shapes peaks at ≥ max(w, 1 − w) ≥ 1/2
  (Geometry.cs :560 already states this). Today the floor is 1/4.
- the thickness bound becomes the **thickness channel hull maximum + 10⁻¹⁴** instead of 1.

Together they raise the admitted gap **33×** (2 m: 3.12e-10 → 1.04e-8; 0.12 m: 5.21e-9 → 1.74e-7).

| F4 under the proposal | Atoms | Work | Time | Within budget |
|---|---|---|---|---|
| +0.02, 120 mm | 53 | 146,424,669 | 0.31 s | yes |
| +0.02, 2 m | 199 | 382,035,371 | 0.72 s | yes |
| +0.20, 120 mm | 452 | 746,728,668 | 1.37 s | no (time) |
| +0.20, 2 m | 1,805 (diagnostic) | 2.87e9 (2.9 × the limit) | 5.2 s | **no** |

Even with the proposal, +0.20 at 2 m fails on work and time in this probe. It passes on atom count.

## 4. What the probe built (the overlay, as specified in §3.5)

- **Atoms:** cells of the x-range, cut at every Bézier piece end abscissa of every side. A cut **piece** (one side's
  sub-Bézier) is split by dyadic de Casteljau halving; the atom splits the side with the largest weighted deviation.
- **Second-order enclosure.** On each piece, y − ℓ(x) for the chord line ℓ is itself a Bézier with coefficients
  y_j − ℓ(x_j), so its hull bounds the side over the piece's whole x-range. Its width falls 4× per halving. The
  plain y-hull falls only 2×. For comparison, M1.1 reached 3.3e-10 for +0.02 after 2,048 splits (section-editor.md:155).
- **Nose line and TE wedge**, exactly as §3.5 states them. On section-a, the line m = 0.0075 (F1) or 0 (F4) proves
  x ∈ (0, 0.28]. The TE wedge proves x ∈ [0.62, 1) for the closed TE. Neither needed subdivision. The drooped-nose
  case (F5) was not run.
- **Uniform in w.** Each atom contributes two lines in w per scale pair. The lines come from its window ends, with
  the scales 1/M_lo and 1/M_hi. The exact upper envelopes U(w) and L(w) are compared at every breakpoint.
  Termination is U ≤ (1 + δ)L for all w ∈ [0, 1]. The stored result is the envelope lines, not the atoms.
- **Outward dyadic rounding** (128 bits) of every stored bound, and a 96-bit reference slope. This is sound, because
  any affine reference keeps the deviation coefficients exact. It cut the widest operand on F4 from 9,020 to ≤ 3,945 bits.
- **QueryFeasibility atom model.** The section Size was rebuilt from `Prove`'s own span witnesses. A query evaluates
  V0 + (V1 − V0)·w at the blend-weight size (2200, 4300). Stored lines are N129/D129, evaluation needs 9,381 bits, the
  normalized shape needs 19,137 bits (≤ 32,768), and ops are ≤ 851,760 (≤ 1e6) in every case. **QueryFeasibility is
  not the blocker.**

**Where the cost is.** Measured: ~320 K bit-work per split (total work ÷ splits, every F4 run). Inferred, not profiled: section-a's knots
(1/3 in binary64) give its exact Bernstein span coefficients ~1,000-bit denominators. Each piece's chord line is
evaluated at those exact abscissae.
assume: A piece kept as dyadic-rounded coefficients with a carried error radius would cut the per-atom work several
times over. What confirms it: a probe of that representation. What breaks if it is false: nothing in this verdict.
The +0.20 at 2 m case fails the as-built proof on atom count anyway.

## 5. Findings

1. **[Major] (Verified, measured)** The blocker is B6, not per-surface bases. F1 shows that one profile with
   per-surface knot vectors certifies at 1.8 % of the work limit and 6 % of the time budget. The certificate would
   admit per-surface section types on foils where the edited profile does not blend with a different profile. DR-11
   can be re-asked in that narrower form.
2. **[Major] (Inferred from Geometry.cs :265–274 and :387, not run)** The OD-4 fallback has the same seam.
   - Paired Control → Anchor inserts knots on **both** surfaces of the edited profile, so its span count and
     abscissae change.
   - When that profile blends with a different neighbouring profile, `SharedAbscissa` refuses it as `Unsupported`.
   - So "certifies today" holds for SPTF only when there is no different neighbour (the Example foil uses one profile
     at root and tip).
   - Check: an SPTF test with two different profiles and an anchor insert on one of them.
   - Owner: SPT/Coordinator. This is not chased here.
3. **[Minor] (Verified)** The §3.5 single-profile reduction is confirmed (1.67e-7 at 0.12 m, 1.0e-8 at 2 m). The
   blend figure at 2 m is 3.12e-10, not 1.9e-10.
4. **[Minor] (Verified)** The F1 fixture reproduces §3.4's measurement: section-a upper vertex 3 (35 %, 6.5 %),
   u* = 0.400339, 8 → 13 points, Δy = 0.878 % chord.

## 6. Assumptions (written before the runs)

- **F4 "vertex 3"** is 0-based index 3, x = 0.35, as §3.4 numbers the nose as vertex 0. The x move is applied to both
  surfaces (paired x, as the M1.1 B6 case was). With +0.20 it lands on x = 0.55, equal to vertex 4: the abscissa
  stays non-decreasing, and `Assess`'s monotonicity rule accepts it. If the row meant 1-based vertex 3 (x = 0.15),
  the max T0 sweep would differ. The verdict does not depend on it, because +0.02 at 2 m already fails.
- **F1** writes no `smooth` row for the new anchor. The Boehm insertion leaves the anchor's handles collinear and
  the equal Δy moves keep them so. The row check belongs to GCRT's profile-row branch, not to the overlay.

## 7. Durations (the new prior, Ruling 54 P1)

| Stage | Box | Measured |
|---|---|---|
| Stage 1 (grounding, probe written from scratch, F1 and F4, two probe-performance cycles, diagnostics) | 60 min | **24 min** to the measured verdict |
| Stage 2 (F2, F3, F5) | 60 min | **not run** (the stage-1 no-go ends the spike) |
| Verdict, gates and commit | — | recorded in the commit and the report |

The repair cap (2 cycles) was reached on probe performance: the outward rounding, then split memoisation. No third
cycle was started.

## 8. Reproduce

```
cd docs/proof/m12c-certificate-spike
dotnet build -c Release probe
dotnet probe/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll stage1 .
```

This writes `fixtures/*.foil`, `output/stage1.log` and `output/stage1.json`. The full run takes ~2.5 min, most of it
in the two unbounded diagnostics.

## 9. Residual risk

- **Inferred:** a dyadic piece representation could make +0.20 at 2 m pass under the §3 proposal (1,805 atoms is
  within the cap; the work is 2.9× over). This was not measured, and the as-built proof still refuses it on atom
  count.
- **Not covered:** F2 (NACA 0012 at 12/10 points), F3 (open TE) and F5 (drooped nose) were not run. The 1 s figures
  are from one Mac.
