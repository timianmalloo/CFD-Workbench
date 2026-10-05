---
id: proof-vlm-tip-repaired
title: "Tip-strip law on the repaired 1.1.0 lattice"
type: proof-pack
status: blocked
owner: "@vlm4"
phase: implementation
tags: [analysis, vlm, tip-strip]
links:
  - { to: proof-vlm-tip-study, rel: refines }
  - { to: design-area3-analysis, rel: relates-to }
review-by: "2027-04-04"
summary: "Tests the fixed-station tip convention against an analytic midspan anchor and local lift; the rectangular high-alpha falsifier blocks a universal tip verdict."
---

# Repaired-lattice calibration — Rulings 75 and 77(5)

**Measured, 2026-10-04; CFD veto remains.** The source lattice is `cfdw.vlm-strip` 1.1.0. Run
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

**U is discretisation of the η* convention, not model uncertainty or a physical error
bar.** The analytic cross-check missing from the original calibration is elliptic AR 8,
α=5°, n=64, nearest midspan strip to η=0 (actual η=0.0122706143): measured αᵢ=0.975759076° versus
CL/(π AR)=0.955300584° (CL=0.4190689, AR=8). The absolute gap is 0.020458492°;
the fixture's declared tolerance is 0.05°. This supports the midspan scale, not
the extrapolated tip angle. The latter reads 10.075° at n64 and 10.548° at n256,
while its own `Cl_local` implies 4.689° and 4.398° respectively under the stated
thin-airfoil slope **2π per radian** (`|Cl_local|/(2π) × 180/π`). Exact αᵢ from
CL/(π AR) is about 0.955°, making the analytic α_eff near 4.045° at α=5°.

The convention still evaluates α+twist−αᵢ* with U=c(n)·|αᵢ,k1−αᵢ,k2|+0.05° on
strips with |η|≥η*, plus k1 at n16/32. When that angle's uncertainty interval
reaches outside 10° but the strip's own `Cl_local` angle does not, the tip is
`Provisional` with `ANA-TIP-INCONSISTENT`; U cannot settle the disagreement.
Independent `Cl_local>1` or sweep>30° retains Outside. Inside and AtBound data
shapes remain; AtBound Text is empty because copy is operator-held.

The calibrated family is precisely AR 7.98–8.02 (AR 8 with section quadrature),
16/32/64/128/256 cosine span panels per half, four cosine chord panels, a 20-span
wake, straight quarter chord, zero dihedral, and either an elliptical chord law or
a linear taper with tip/root ratio 0.5–1. Sections are flat/untwisted; the
rectangular member also permits 4% parabolic camber without twist or flat sections
with 1° linear washin. Other geometry reads `ANA-TIP-UNCALIBRATED`. Missing or
nonfinite strip data and unsupported span counts/spacing read `ANA-TIP-PROVISIONAL`.
The solve path carries geometry derived from its sections/settings; a stored reader
must pass geometry rederived from the run's sections/settings. A stored read with
no geometry stays uncalibrated. Both paths use |η|, so port and starboard agree.
`StripLoad.Provisional` stays in the document for compatibility.

The 22-case table reports k1 for n=16/32/64/128/256 in that order. `Cl angle`
is the angle in degrees implied by that same strip's `Cl_local` with slope 2π/rad.
`P` is Provisional, `I` Inside, and `O` Outside. The reducer prints unrounded
input-derived values; this table rounds angles to 0.001°.

| case | raw | law | Cl angle, degrees at n16/32/64/128/256 |
|---|---|---|---|
| elliptic α2 | I/I/I/I/I | I/I/I/I/I | 2.029/1.957/1.878/1.809/1.761 |
| elliptic α4 | I/I/I/O/O | I/I/I/I/I | 4.056/3.911/3.753/3.617/3.520 |
| elliptic α5 | I/I/O/O/O | P/P/P/P/P | 5.067/4.886/4.689/4.519/4.398 |
| elliptic α8 | I/O/O/O/O | P/P/P/P/P | 8.092/7.802/7.488/7.216/7.022 |
| elliptic α14 | O/O/O/O/O | O/O/O/O/O | 14.066/13.563/13.016/12.543/12.207 |
| rectangle α2 | I/I/I/I/I | I/I/I/I/I | 0.282/0.147/0.075/0.038/0.019 |
| rectangle α4 | I/I/I/I/I | I/I/I/I/I | 0.564/0.294/0.150/0.076/0.038 |
| rectangle α5 | I/I/I/I/I | I/I/I/I/I | 0.705/0.368/0.188/0.095/0.048 |
| rectangle α8 | I/I/I/I/I | I/I/I/I/I | 1.126/0.587/0.300/0.151/0.076 |
| rectangle α18 | O/O/O/O/O | P/P/P/P/P | 2.499/1.303/0.666/0.336/0.169 |
| taper 0.5 α2 | I/I/I/I/I | I/I/I/I/I | 0.381/0.200/0.102/0.052/0.026 |
| taper 0.5 α4 | I/I/I/I/I | I/I/I/I/I | 0.762/0.400/0.205/0.104/0.052 |
| taper 0.5 α5 | I/I/I/I/I | I/I/I/I/I | 0.952/0.500/0.256/0.129/0.065 |
| taper 0.5 α8 | I/I/I/I/I | I/I/I/I/I | 1.521/0.798/0.408/0.207/0.104 |
| camber 4% α2 | I/I/I/I/I | I/I/I/I/I | 1.054/0.556/0.286/0.145/0.073 |
| camber 4% α4 | I/I/I/I/I | I/I/I/I/I | 1.335/0.703/0.361/0.183/0.092 |
| camber 4% α5 | I/I/I/I/I | I/I/I/I/I | 1.474/0.775/0.398/0.201/0.101 |
| camber 4% α8 | I/I/I/I/I | I/I/I/I/I | 1.890/0.992/0.509/0.257/0.130 |
| washin 1° α2 | I/I/I/I/I | I/I/I/I/I | 0.396/0.207/0.106/0.053/0.027 |
| washin 1° α4 | I/I/I/I/I | I/I/I/I/I | 0.677/0.354/0.181/0.091/0.046 |
| washin 1° α5 | I/I/I/I/I | I/I/I/I/I | 0.818/0.427/0.218/0.110/0.055 |
| washin 1° α8 | I/I/I/I/I | I/I/I/I/I | 1.238/0.646/0.330/0.167/0.084 |

No calibration case has both IN and OUT under the revised law. The original
100-solve readiness test took **228742.421 ms**; the revised
`TipLaw_Calibration_NoInsideOutsideFlip` passed in **41983.010 ms** (and
**42504.907 ms** in the final eight-row log) with all 20 cases and five
resolutions each. The rectangular α18 falsifier **does not hold**: its local lift angle is
2.499° at n16 and decreases to 0.169° at n256, so all five tips must stay
Provisional under the independent-angle rule. Elliptic α14 remains Outside at
all five n. `TipLaw_Falsifiers_OutsideEveryN` reported all ten cases, then failed
in **4993.917 ms** on the five rectangular tips. The fast Analysis ring gets no
n=256 solve from VLM-4. The repaired elliptic quarter-chord sweep is 0°
at every strip for n=16/32/64/128/256 in the calibration check, so its ≤30°
verdict is stable. The old study's 54°→87° front-bound sweep readings do not
describe this lattice.

The earlier four-tip readiness pass (before the independent-angle veto) cost
1569.550, 230296.282, 23368.220, and 12.714 ms and had `RESULT failures=0`.
It is superseded by this review; its green result does not certify the new rule.

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
calculate αᵢ* or U. `Verdicts` is the context-bearing reader API; stored reads
also need a `TipLawGeometry` reconstructed from the run sections and settings.

**Red-first receipts, this review.** `TipLaw_StoredAndSolve_MirroredTipsAgree`
failed with port `Provisional/ANA-TIP-PROVISIONAL` versus solve
`AtBound/ANA-TIP-AT-BOUND`; `TipLaw_EllipticAlpha5_InconsistentProvisionalEveryN`
failed with n16 `AtBound`; `TipLaw_OutOfFamily_ProvisionalAtEveryBound` failed with
chord count 2 reading Inside, then after the geometry guard was added failed
with 2° washin reading Inside. The analytic midspan anchor passed on its first
run. The rectangular α18 falsifier stays red intentionally and is the CFD veto.
The revised readiness checks measured 35.504 ms (stored flag), 78.204 ms
(mirrored read), 2888.123 ms (elliptic α5 at five n), 58.659 ms (midspan
anchor), and 13.583 ms (seven out-of-family geometries) in one selective
Release run. The cost of the revised 100-solve calibration is stated above.

**Post-merge gates.** The required merge by SHA `d65892a5de08e0ed738b6aab57854dccf8722d93`
was already up to date. The one fast `tools/run-tests.sh` invocation built cleanly
and its Analysis suite passed 63 checks, but Core part 1 failed the existing
single-site radians conversion check on the new `MethodRecord.cs` literal; wall
time was 80 s against the 60 s budget. The one-line repair uses
`VortexLattice.ToDegrees`, and the Core check then passed alone. The complete
VLM4 readiness log has seven PASS and one FAIL: rectangular α18 at n16/32/64/128/256.
The named-test gate reports 7/8 and also retains the fast-ring Core failure
from before that repair. `python3 tools/check-docs.py` passed. These results do
not clear the CFD veto or claim a green full ring after the final source edit.

**Residual limit and ruling need.** The η* extrapolation lacks a physical tip
anchor. The `Cl_local` check prevents a false Outside from that angle alone, but
the rectangular α18 case no longer supplies the required positive falsifier.
The law cannot be accepted as a universal tip verdict until a physical
resolution or a revised operator ruling settles this conflict. The bound's UI
wording and indeterminate label remain with the operator.
