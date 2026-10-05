---
id: proof-vlm-tip-repaired
title: "Tip-strip law on the repaired 1.1.0 lattice"
type: proof-pack
status: verified
owner: "@vlm4"
phase: implementation
tags: [analysis, vlm, tip-strip]
links:
  - { to: proof-vlm-tip-study, rel: refines }
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2027-04-04"
summary: "Recalibrates the fixed-station tip law on cfdw.vlm-strip 1.1.0, including camber and washin, and records the falsifiers and runtime cost."
---

# Repaired-lattice calibration — Rulings 75 and 77(5)

**Verified, 2026-10-04.** The source lattice is `cfdw.vlm-strip` 1.1.0. Run
`bash docs/proof/vlm-tip-study/run-repaired.sh` and
`python3 docs/proof/vlm-tip-study/repaired-law.py`. The former records the unrounded
strip data in `repaired-1.1.0.csv` and `repaired-falsifiers-1.1.0.csv`; the latter
prints every case, fixed-station αᵢ, tip gap, error, multiplier, raw verdict, and
law verdict. The probe uses the production `VortexLattice.Solve` and repaired
control-point camber slopes. Geometry is AR 8, half-span 1, straight quarter chord,
four cosine chord panels, 20-span wake, cosine span spacing. Cases: elliptic,
rectangular, taper 0.5, F18 4% parabolic camber, F19 1° linear washin, each at
α=2/4/5/8° and n=16/32/64/128/256 per half. The two falsifiers are rectangular
α=18° and elliptic α=14°, at all five n.

The repaired nonplanar fixtures agree with the §13.2 references:

| Fixture, α=5° | measured tip αᵢ n64 / n128 | design reference | measured CL n64 / n128 | design reference |
|---|---|---|---|---|
| F18 camber | 4.957617° / 4.977061° | 4.958° / 4.977° | 0.778423 / 0.776942 | 0.77842 / 0.77694 |
| F19 washin | 2.365957° / 2.371380° | 2.366° / 2.371° | 0.437050 / 0.436156 | 0.43705 / 0.43616 |

The station is η* = (1 + cos(π/128))/2 = 0.999849409348102. αᵢ* is linear in
ln(1−η) between the run's bracketing strips. For n=16/32 it extrapolates from k1/k2.
The finite-resolution reference for each case is the **mean** of n=128 and n=256
αᵢ*. This is a calibration reference, not an independently proven continuum value.
For each case and n, the smallest multiplier is
`max(0, (|αᵢ*(n) − reference| − 0.05°) / |αᵢ,k1 − αᵢ,k2|)`.
The maximum over all 22 cases, including the two falsifiers, gives:

| n | measured maximum | rounded-up c(n) | limiting case |
|---:|---:|---:|---|
| 16 | 2.393664234 | 2.40 | elliptic α=14° |
| 32 | 0.834187674 | 0.84 | elliptic α=14° |
| 64 | 0.193160169 | 0.20 | elliptic α=14° |
| 128 | 0 | 0 | 0.05° floor covers the reference difference |
| 256 | 0 | 0 | 0.05° floor covers the reference difference |

The law uses U = c(n)·|αᵢ,k1−αᵢ,k2| + 0.05°. It judges α+twist−αᵢ* on every
strip with η≥η*, plus k1 at n=16/32 where η* is beyond k1. A cosine 16–256-per-half
lattice with both tip strips and finite αᵢ is required. Unsupported spacing/count,
missing strips, or nonfinite values remain `ANA-TIP-PROVISIONAL`. Stored
`StripLoad.Provisional` is retained for document compatibility; the read-time
verdict clears it when this law applies. `AtBound` carries `ANA-TIP-AT-BOUND`,
U and the evaluated angle as data. The operator-held wording is not added.

| case | raw k1 n16/32/64/128/256 | law k1 n16/32/64/128/256 | α_eff* ± U (n16 → n256) |
|---|---|---|---|
| elliptic α=2° | IN/IN/IN/IN/IN | IN/IN/IN/IN/IN | 3.184±1.082 → 4.221±0.050° |
| elliptic α=4° | IN/IN/IN/OUT/OUT | IN/IN/IN/IN/IN | 6.367±2.114 → 8.440±0.050° |
| elliptic α=5° | IN/IN/OUT/OUT/OUT | BOUND/BOUND/BOUND/OUT/OUT | 7.957±2.628 → 10.548±0.050° |
| elliptic α=8° | IN/OUT/OUT/OUT/OUT | BOUND/OUT/OUT/OUT/OUT | 12.722±4.167 → 16.859±0.050° |
| rectangular, taper, camber, washin α=2/4/5/8° | IN at all n | IN at all n | see CSV/reducer for every value |
| rectangular α=18° | OUT at all n | OUT at all n | 11.018±0.399 → 11.121±0.050° |
| elliptic α=14° | OUT at all n | OUT at all n | 22.208±7.207 → 29.399±0.050° |

No calibration case has both IN and OUT under the law; the 100-solve readiness
test `TipLaw_Calibration_NoInsideOutsideFlip` measured **228742.421 ms** and
passed. `TipLaw_Falsifiers_OutsideEveryN` measured **51241.730 ms** and passed
both outside cases at all n. These are readiness checks; the fast Analysis ring
gets no n=256 solve from VLM-4. The repaired elliptic quarter-chord sweep is 0°
at every strip for n=16/32/64/128/256 in the calibration check, so its ≤30°
verdict is stable. The old study's 54°→87° front-bound sweep readings do not
describe this lattice.

The committed readiness suite repeated all four tip tests: COST 1569.550,
230296.282, 23368.220, and 12.714 ms respectively; `RESULT failures=0`.

Red-first receipt: commit `026fd7a` added `TipLaw_EllipticAlpha4_StableInside`
before the law. It failed at n128: raw α_eff 10.450681952936586° and Outside.
Planted multiplier mutant: set c(16)=0 in `MethodRecord.cs` and run only
`TipLaw_StoredFlag_DerivedOnRead`; it failed with `stored tip did not derive a
bounded verdict: Inside`, `RESULT failures=1`, COST 30.698 ms. Restoring
c(16)=2.40 made that check pass again. The mutant was not committed.

**Version rule.** The verdict is derived from the read-time strip data. This
change writes no new result field and does not change the 1.1.0 stored payload,
so `cfdw.vlm-strip` stays 1.1.0. The stored provisional flag remains until the
storage owner changes its schema. A caller using only `JudgeStrip` without the
run's neighboring strips still gets provisional for a flagged tip; it cannot
calculate αᵢ* or U. `Verdicts` is the context-bearing reader API.

**Residual limits.** This is an empirical envelope convention for the measured
AR 8 cosine/cosine family. The reference is not an external lifting-surface
solution. Swept, dihedral, other chordwise or span spacing, other AR, and section
zero-lift changes remain outside the calibration. The bound's UI wording and
indeterminate label remain with the operator.
