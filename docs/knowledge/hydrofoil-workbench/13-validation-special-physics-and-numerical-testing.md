---
id: kb-hw-validation-special-physics-and-numerical-testing
title: "Validation data, special hydrofoil physics and testing numerical design software"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, validation, cavitation, free-surface, ventilation, unsteady, pumping, verification, uncertainty, testing, numerical-software, gci, ittc, v-and-v]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes which measured datasets a hydrofoil design tool can honestly compare against (and what each
  actually contains), the cavitation, free-surface, ventilation and unsteady/pumping physics a screening
  tier can and cannot claim, the ITTC/ASME/Roache verification-and-validation vocabulary that makes the A6
  label ladder concrete, and the testing practice (analytic references, manufactured solutions, metamorphic
  and golden-master tests, cross-platform float rules) that must exist before any number is labelled
  Verified. Main implication: no closed-form free-surface or cavitation correction is Verified today; the
  tool ships them as Computed estimates with named datasets and the comparison error E and validation
  uncertainty U_V displayed, never a single fudge factor.
---

# Validation data, special hydrofoil physics and testing numerical design software

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. Which validation datasets can a hydrofoil tool honestly use, what do they contain, and how do they rank on
   the A6 ladder?
2. What cavitation physics does a Cp_min screen rest on, and what can steady RANS cavitation models predict?
3. How do lift, drag and wave resistance change with depth and Froude number, and which regime do racing foils
   occupy?
4. How does ventilation start, what regimes exist, and what can a design tool screen?
5. What does unsteady/pumping theory (Theodorsen, Garrick, Strouhal scaling) let a quasi-steady vs unsteady
   estimator claim?
6. How are validation results reported (E, U_V, GCI) and how should model discrepancy be shown to a user?
7. How is numerical design software tested (analytic references, MMS, golden masters, metamorphic and property
   tests, cross-platform determinism)?
8. How can the product table become estimator sanity bounds without treating marketing numbers as
   measurements?

Budget note: the session's WebSearch quota was exhausted before this area started (200/200); every source
below was reached by direct URL, API (NTRS, Crossref, GitHub) or local text extraction of downloaded PDFs.
Where a document could only be cited, not opened, the claim is labelled accordingly.

## Headline findings

1. The strongest public T-foil dataset for a consumer-scale hydrofoil is Day, Cocard & Troll (CSYS 2019): a
   0.988 m span International Moth main foil with 35 %-chord flap towed at the Kelvin Hydrodynamics Laboratory
   (Strathclyde, 76 m × 4.6 m × 2.5 m tank), 0.5–4.5 m/s, mean-chord Re 0.4–3.6 × 10⁵, submergence 457 mm
   (h/c̄ = 4.8) and 100 mm (h/c̄ = 1.05), α 0–6°, flap −6° to +6°, ≈400 runs incl. repeats — reported as lift
   and drag *areas* of the foil-plus-strut assembly, with an explicitly unresolved zero of α and a flap
   pointer that moved >2° under load at the highest loads. — *(Verified, [S2])*
2. NACA TR-1232 (Wadlin, Shuford & McGehee 1955) is the classical free-surface dataset: 8-inch-chord NACA
   64₁A412 hydrofoils of aspect ratio 10 and 4 on an NACA 66₁-012 strut, Langley tanks (10.64 ft and 6.0 ft
   deep), 5–45 ft/s, Re 0.18–2.04 × 10⁶, submergence 0.59–4.09 chords; it found no appreciable effect of the
   critical (shallow-water) speed on lift-curve slope or zero-lift angle and a *gradual* drag rise approaching
   critical speed, contrary to the abrupt rise theory predicts. The full PDF is public on NTRS. — *(Verified,
   [S3])*
3. DTIC ADA032272 (Layne, DTNSRDC 1976, 107 pp.) measured lift and drag of NACA 16-309 and modified 64A309
   foils "geometrically similar to the forward foil system of the PCH hydrofoil craft" in the High-Speed Tow
   Facility and Rotating Arm Facility, flaps to 17.5°, pitch to 12°, with notable depth influence below one
   chord; it is a *foil-system* test, not a section polar, and no uncertainty statement appears in the record.
   — *(Verified metadata, [S1])*
4. ITTC 7.5-03-01-01 is now Revision 05 (effective 2024, 30th ITTC) and carries two verification methods —
   Stern et al. (three grids, leading term) and Eça & Hoekstra (≥4 grids, least-squares fit) — with factor of
   safety F_S = 1.25 for 0.5 ≤ p < 2.1 and well-behaved data, F_S = 3 otherwise; validation is E = D − S
   compared with U_V² = U_D² + U_SN², and |E| < U_V means "validated at the U_V level". Revision 04 (2021)
   used the same definitions. — *(Verified, [S8])*
5. The infinite-Froude image-vortex correction used in the Moth study (Daskovsky's K = (16(h/c)² +
   1)/(16(h/c)² + 2) with induced factor σ = 1/(1 + 12h/b)) predicts only a 2–4 % lift reduction for an AR-8
   wing at h/c = 1.5–6, whereas the CEHINAV tank test already in the repo reports ≈17 % lift-coefficient loss
   at h/c = 4 between Fr_h = 2 and 5. A steady closed-form correction therefore cannot be shipped as Verified;
   depth *and* Froude number must be inputs and the discrepancy displayed. — *(Verified inputs [S2][S36],
   executed arithmetic [S40], Inferred conclusion)*
6. For racing foils (5–20 m/s, 0.3–0.6 m depth, 0.10–0.20 m chord) the operating envelope is Fr_h ≈ 2–12, Fr_c
   ≈ 3.6–20, h/c = 1.5–6 and depth-corrected σ ≈ 8.0 (5 m/s) to 0.50 (20 m/s) in seawater — i.e. always the
   high-Froude (anti-symmetric image) limit and, above ≈15 m/s, inside the cavitation-relevant band for any
   section with −Cp_min > 1. — *(Executed arithmetic [S40]; p_v value Flagged)*
7. Cavitation inception is not equal to −Cp_min in general: Amromin & Rozhdestvensky (2022) show fully
   turbulent CFD predicts cavitation-free pressure well but inception "remains a challenge", and Amromin
   (2001) computed different scale effects for the E817 suction and pressure sides vs NACA 0012; the ITTC
   numerical-prediction procedure states that "for the most part only sheet cavitation can be predicted with
   reliability" and that for cloud cavitation "there appears to be no reliable means of prediction". —
   *(Verified, [S28][S10])*
8. Ventilation on surface-piercing foils has three regimes — fully wetted, partially ventilated, fully
   ventilated — whose stability regions overlap, producing bistable zones with hysteresis; formation needs air
   ingress into separated flow at sub-atmospheric pressure, elimination happens through upstream flow of the
   re-entrant jet, and a semi-theoretical scaling on re-entrant-jet angle collapses the washout boundary
   across Froude and Reynolds numbers (Harwood, Young & Ceccio, JFM 800, 2016; review in Young et al., AMR
   2017). — *(Verified abstracts, [S4][S5])*
9. Oscillating-foil propulsion depends on *both* Strouhal number and reduced frequency; where viscous drag is
   small, thrust follows a near-linear dependence on reduced frequency (Floryan, Van Buren, Rowley & Smits,
   JFM 822, 2017), and the phase between heave and pitch is "a critical parameter" (Van Buren et al. 2018). No
   public rider-pumping kinematics dataset (cadence, amplitude, power) was found. — *(Verified abstracts,
   [S17]; gap Flagged)*
10. The oracle problem is the defining difficulty of testing scientific software; Kanewala & Bieman's 62-study
    review catalogues the responses — analytic solutions, experimental results, pseudo-oracles/N-version,
    metamorphic testing, statistical checks — and names the cultural failure of treating "the code and the
    model that it implements as inseparable entities". — *(Verified, [S7])*
11. Real numerical codes test by tolerance, not bitwise equality: SU2's regression harness compares logged
    values with an *absolute* per-case tolerance (`self.tol`, default 0.0 unless the case sets it);
    AeroSandbox's lifting-line tests compare CL to Prandtl theory at `rel=0.05` and CDi at `rel=0.3`. —
    *(Verified by reading source, [S31][S32])*
12. Cross-platform equivalence (macOS ARM64 vs Windows x64) is not guaranteed for transcendental functions:
    .NET's `Math.Sin` docs state the method "calls into the underlying C runtime, and the exact result or
    valid input range may differ between different operating systems or architectures"; Rust documents
    `sin/cos/exp/ln/powf/…` as "non-deterministic … varies by platform, Rust version, and can even differ
    within the same execution", while `mul_add` (FMA) is "guaranteed to be the rounded infinite-precision
    result". Golden masters must therefore be tolerance-based; bitwise equality is achievable only for
    +,−,×,÷,√ and FMA. — *(Verified, [S33][S34])*
13. The NASA Turbulence Modeling Resource NACA 0012 case (Re 6 × 10⁶, M 0.15, Ladson tripped data "most
    appropriate" for fully turbulent CFD; Gregory & O'Reilly for Cp) is a *turbulence-model*
    verification/validation case in air; it verifies a RANS backend, not a water hydrofoil at Re 10⁵–10⁶ with
    transition. — *(Verified, [S6])*
14. Licences (checked 2026-09-20): FsCheck BSD-3-Clause; proptest Apache-2.0 (LICENSE-APACHE; MIT dual per
    recall, Flagged); Verify MIT; AeroSandbox MIT; NeuralFoil MIT; Hypothesis and ApprovalTests.Net report
    NOASSERTION on GitHub (MPL-2.0 and Apache-2.0 respectively from recall — Flagged); SU2 NOASSERTION
    (LGPL-2.1 from recall — Flagged, process-invoke only under COMMIT-02). — *(Verified via GitHub licence
    API, [S35])*

## State of the art

### Validation datasets a hydrofoil tool can honestly use

**Day, Cocard & Troll 2019 (Moth T-foil, Strathclyde).** The foil is a c. 2006 Bladerider Moth main foil:
horizontal span 0.988 m, root chord 0.125 m (extrapolated through the bulb), chord at 90 % span 0.045 m, mean
chord 0.095 m, t/c 12.8 %, camber 3.1 %; vertical 1.0 m span, chord 0.118→0.1135 m, t/c 14.5 %, section
"matched very closely" a NACA 66012 scaled to 14.5 %. Tests ran in the Kelvin Hydrodynamics Laboratory tank
(76 m × 4.6 m × 2.5 m, water depth 2.1 m). Load cells were calibrated in three axes with cross-coupling
determined per cell; the rig was verified under representative horizontal and vertical loads. Results are
presented as lift area L/(½ρV²) and drag area D/(½ρV²) *of the whole assembly* so that no decomposition
between horizontal and vertical is required — which means these numbers are **not** wing-only coefficients
(A6). Uncertainty is discussed qualitatively (zero of α unknown because shims set the horizontal relative to
the vertical; flap pointer moved >2° at high load; background turbulence not measured) but no GUM-style budget
is stated. Free-surface effect: at h/c̄ = 1.05 and low lift, drag at a given lift was *lower* than at h/c̄ =
4.8, attributed to the loss of strut drag outweighing the horizontal's lift loss. The simplified prediction
used XFOIL (3126 cases: 6 Re from 10⁵ to 10⁶, 13 α, 17 flap angles, free transition, **Ncrit = 4**
"corresponding to a turbulence level of around 0.6 %"), a 21-station lifting line, Daskovsky's free-surface
factor, Gibbs & Cox strut interference γ = 0.8 t/b, Coffee & McKann spray drag, Hoerner junction drag; model 2
"generally performs better" but agreement is "noticeably less good for higher angles of attack, which may
affect prediction of take-off speed". — *(Verified, [S2])*

**NACA TR-1232 (1955).** Two 8-inch-chord NACA 64₁A412 hydrofoils (AR 10 and AR 4; the 64₁A412 is the 64₁-412
with the trailing-edge cusp removed) on an NACA 66₁-012 strut intersecting the upper surface without fillets;
polished stainless steel. AR 10: Langley tanks no. 1 (mean depth 10.64 ft = 15.98 chords) and no. 2 (6.0 ft =
9.0 chords), 0.84 and 3.84 chords submergence, 5–45 ft/s, Re 0.18–1.64 × 10⁶. AR 4: tank no. 2,
0.59/1.09/2.09/3.09/4.09 chords, 15–35 ft/s, Re 0.873–2.04 × 10⁶. Depth is measured from the undisturbed
surface to the quarter-chord. Theory: multiple-image horseshoe vortices for the free surface and tank bottom,
plus wave drag; "the agreement between theory and experiment at both supercritical and subcritical speeds is
satisfactory for engineering calculations of hydrofoil characteristics from aerodynamic data". Struts were
also run alone for tares. This is the primary source behind every "Wadlin correction" cited second-hand in the
repo's area 04. — *(Verified, [S3])*

**DTIC ADA032272 (1976).** See headline 3. What the record establishes: two section families (16-309, modified
64A309), two facilities, flap and pitch sweeps, depth sensitivity below one chord, and the conclusion that the
64A309 "can achieve a higher lift-to-drag ratio than the 16-309". What it does not establish from the
metadata: the strut/pod geometry, Reynolds numbers, or measurement uncertainty. The coefficient tables have
still not been extracted (open since the gap register). — *(Verified metadata, [S1]; tables Flagged)*

**Beaver & Zseleczky 2009 (USNA, full-scale Moth foils).** Full-scale tow tests of "various home built and
commercially available T-foil configurations" plus hull drag at several displacements; SNAME CSYS paper, not
open access. Day et al. chose 457 mm submergence partly to match this study. Together the two form the only
pair of independent full-scale T-foil datasets located. — *(Verified abstract, [S24])*

**CEHINAV 2026 (kitefoil/windfoil wing near the free surface)** and **Delft surface-piercing foils (JFM
2026)** are already established in area 04 [S36] (0.059 m², span 0.634 m, c̄ 0.0735 m, Re 0.7–2.9 × 10⁵, Fr_h
0.4–6.7, h/c 0.5–9.5; ventilation maps at Fr 0.5–2.5). This file does not repeat them; it ranks them.

**NASA TMR 2D NACA 0012.** Re 6 × 10⁶, M 0.15, α from 0° to the highest steady angle; compares CL(α), CD(CL),
Cp and Cf at α = 0°, 10°, 15°; data files for Ladson (tripped), Gregory & O'Reilly (Re 3 × 10⁶, better
leading-edge Cp resolution) and digitised Abbott & von Doenhoff; caveats: drag "greatly affected" by tripping,
tripped CD at Re 3 × 10⁶ ≈ 10 % above 6 × 10⁶, dataset differences grow near stall, and supplied grids "are
likely not fine enough when high accuracy is required". — *(Verified, [S6])*

**Cavitation-tunnel section data.** Shen & Dimotakis (JFE 111(3), 1989) "Viscous and Nuclei Effects on
Hydrodynamic Loadings and Cavitation of a NACA 66(MOD) Foil Section" is the canonical sheet-cavitation dataset
used by Kunz et al. (Computers & Fluids 29, 2000) to validate a preconditioned two-phase Navier–Stokes method.
Only the citations were confirmed this session; the Re, σ and load values are **Flagged** until the paper is
opened. The Delft twisted hydrofoil (Foeth, van Terwisga & van Doorne, JFE 130, 2008; Foeth et al., Exp.
Fluids 40, 2006) is the reference for 3D sheet-cavity structure and collapse (time-resolved PIV), not for
force coefficients. — *(Verified citations, [S25][S26][S27])*

**Duncan 1983.** Surface-height profiles and velocity/total-head distributions behind a fully submerged 2D
hydrofoil resolved drag into a breaking-wave part and a non-breaking wave-train part; at incipient breaking
the first wave exists in either state. It validates a free-surface CFD's wave pattern and breaking onset, not
a foil polar. — *(Verified abstract, [S22])*

**Hydroelastic tests.** Marimon Giovannetti, Banks, Ledri & Turnock (Ocean Eng. 167, 2018) is the Southampton
bend–twist-coupled hydrofoil study; relevant to GAP-02, not to steady polars. — *(Verified citation, [S30])*

**Field GPS/IMU.** No public dataset for a consumer race foil was found here or in area 04. — *(Flagged gap)*

**Ranking for the A6 ladder (usefulness to CFD-Workbench, highest first):** (1) Day 2019 — closest
configuration, public PDF, low Re, foil+strut, free-surface pair; (2) TR-1232 — public, wide depth/Fr series,
AR 4 and 10, foil+strut, 64A section family already in the catalog; (3) CEHINAV 2026 — race-type planform,
Fr_h to 6.7, but Re one decade below full scale and no data licence; (4) NASA TMR NACA 0012 — RANS backend
verification only; (5) ADA032272 — needs table extraction and configuration reconstruction; (6) Delft
ventilation maps — envelope flags only; (7) Shen & Dimotakis / Delft twist — future cavitation-CFD validation;
(8) Duncan — free-surface CFD only. — *(Inferred ranking)*

### Cavitation physics for screening

**Definitions.** σ = (p_∞ − p_v)/(½ρV²) with p_∞ = p_atm + ρ g h at the foil depth h; the local pressure
coefficient Cp = (p − p_∞)/(½ρV²); cavitation is *possible* wherever Cp < −σ, so the classical inception
estimate is σ_i ≈ −Cp_min. This is the basis of ANA-10's "σ_required = −Cp_min" bucket display. — *(Standard
definitions; the equality σ_i = −Cp_min is a nuclei-rich upper bound, see next paragraph — Inferred from
[S28][S29])*

**Why inception scatters.** Brennen's monograph (Oxford 1995; Caltech open copy permits "individual,
educational, research and non-commercial reproduction") treats nucleation, nuclei populations and their effect
on inception in Chapters 1 and 6. The precise sentences could not be extracted this session, so the mechanism
statements below are labelled Flagged (recall): water with few free-stream nuclei sustains tension, so σ_i <
−Cp_min; dissolved-gas content and residence time change the nuclei spectrum; laminar separation bubbles trap
nuclei and can raise σ_i above the attached-flow −Cp_min; facility comparisons on the same headform
historically spread by tens of percent in σ_i. What *is* verified: Amromin (2001) computed different scale
effects on suction and pressure sides of the E817 relative to NACA 0012 owing to Reynolds- and Weber-number
effects on sheet-cavity equilibrium in the boundary layer; Amromin & Rozhdestvensky (2022, CC BY) state that
fully turbulent CFD "satisfactorily predict[s] pressure distribution around cavitation-free blades" but
"analysis of blade cavitation inception is a difficult task for these tools", and propose correlating the
inception number with the cavitation-free pressure minimum via validated 2D multizone solutions; Amromin
(2017) shows hydrofoil *material* (via flow-induced vibration) shifts inception and desinence numbers. —
*(Verified [S28][S42][S43]; mechanisms Flagged [S29])*

**Regimes.** Sheet (attached, from the leading edge), cloud (shed from sheet closure, erosive), bubble
(travelling nuclei), tip-vortex (lowest σ_i on a finite wing; requires resolving the vortex core),
supercavitation (cavity closes downstream of the trailing edge). The ITTC numerical-prediction procedure
(7.5-02-03-03.4 Rev 03, 2024) is explicit: "for the most part only sheet cavitation can be predicted with
reliability"; tip-vortex inception "requires a direct simulation of the larger, energy-containing turbulent
scales" and remains "primarily a research topic"; "for cloud cavitation there appears to be no reliable means
of prediction". Erosion risk sits with cloud collapse and is outside any steady screen. — *(Verified, [S10])*

**Discrete-method under-read of Cp_min.** Panel/IBL and neural surrogates sample Cp at finite stations and can
miss or smear a narrow leading-edge suction peak; the polar surrogate's Cp_min is therefore a screening
estimate (ANA-02 already says so). Practitioner margins of 10–20 % on σ (i.e. require σ_operating > 1.1–1.2 ×
(−Cp_min)) are widely used but were not confirmed in a primary source this session. — *(Flagged practitioner
practice)*

**What steady RANS cavitation models can claim.** Homogeneous-mixture, mass-transfer models (Kunz;
Schnerr–Sauer; Zwart) reproduce sheet-cavity length and the load reduction it causes when calibrated, and were
validated against the NACA 66(MOD) data by Kunz et al. (2000). They cannot predict inception from nuclei-poor
water, cloud shedding statistics (unsteady, needs scale-resolving turbulence), or erosion. — *(Verified
citation [S27]; capability statement Inferred from [S10])*

### Free-surface physics

**Two limits.** A foil at depth h under a free surface sees an image system whose sign depends on Fr_h =
V/√(gh) (TR-1232 uses the depth-based Froude number for the critical-speed definition and the images for the
free surface and rigid bottom). At Fr → ∞ the free surface behaves as a constant-pressure boundary and the
image bound vortex has *opposite* sign (lift-curve slope reduced); at Fr → 0 it behaves as a rigid wall with a
same-sign image (lift-curve slope increased). Hough & Moran (JSR 13(1), 1969) solved the 2D arbitrary-camber,
arbitrary-Froude problem by singularity distributions and collocation; Kennell & Plotkin (JSR 28(1), 1984)
extended thin-hydrofoil theory to second order. — *(Verified citations and TR-1232 text [S3][S21][S23]; limit
interpretation Inferred, standard)*

**The infinite-Froude correction actually used in practice.** Day et al. multiply the 3D lift coefficient by
(1 + 2/AR)/(1 + 2K(1 + σ)/AR), K = (16(h/c)² + 1)/(16(h/c)² + 2), σ = 1/(1 + 12h/b), and the induced-drag
coefficient by (1 + σ); they report this "yield[s] similar results to the model proposed by Wadlin (1955)".
For finite Froude number they add a wave-drag term of the classical 2D form C_Dw =
(C_L²/(2Fr_c²))·exp(−2(h/c)/Fr_c²) (Daskovsky; "broadly similar" to Vladimirov 1955) — the exponent in the
extracted PDF text is garbled, so the form here is the classical submerged-vortex result and is labelled
Inferred. — *(Verified [S2]; wave-drag form Inferred)*

**Measured vs corrected.** Running the K-factor at AR 8 gives lift ratios 0.960 (h/c = 1.5), 0.964–0.978 (h/c
= 3–6) [S40]; the CEHINAV test reports ≈17 % C_L loss at h/c = 4 between Fr_h = 2 and 5, effects concentrated
at h/c < 4 and the deep-water asymptote only at h/c > 5 [S36]. The steady infinite-Froude image cannot produce
a Froude-dependent 17 % change; the difference must be wave-induced downwash at finite Fr_h, low-Re section
behaviour (Re 0.7–2.9 × 10⁵ in the tank), or both. This is the single most important reason not to label any
depth correction Verified. — *(Executed arithmetic [S40]; conclusion Inferred)*

**Wave resistance and breaking.** TR-1232 measured a gradual drag rise toward the critical speed; Duncan
measured the breaking/non-breaking split behind a 2D foil. Spray and breaching are surface-piercing phenomena
handled under ventilation. — *(Verified, [S3][S22])*

**Which regime racing foils occupy.** From the executed table [S40]: at 10 m/s, h = 0.3–0.6 m → Fr_h 4.1–5.8,
h/c 1.5–6; at 20 m/s → Fr_h 8.3–11.7. Every racing case is deep in the high-Froude, anti-symmetric-image
regime; the low-Froude "ground effect" limit is irrelevant except at displacement speeds during take-off (5
m/s, Fr_h 2–3). The CEHINAV authors' "competition regime Fr_h ≈ 5" [S36] agrees with the 10–15 m/s rows. —
*(Executed arithmetic, Inferred placement)*

### Ventilation and surface-piercing struts

**Mechanisms and regimes.** Harwood, Young & Ceccio (JFM 800, 2016) tested a vertically cantilevered
surface-piercing hydrofoil with an immersed free tip at low-to-moderate Froude and Reynolds numbers: fully
wetted, partially ventilated and fully ventilated regimes; stability regions overlap so that flow history
selects the regime (bistability, hysteresis); formation "requires air ingress into separated flow at
sub-atmospheric pressure"; elimination occurs by "upstream flow of the re-entrant jet"; a semi-theoretical
scaling on the re-entrant-jet angle captures the washout boundary. Young, Harwood, Miguel Montero & Ward (AMR
69, 2017) review the physics and the scaling implications of reduced-scale tests. The Delft 2026 JFM paper
(area 04) adds the nose/tail/base trigger taxonomy at Fr 0.5–2.5 and a bistable region wider than earlier
maps; Augier et al. (MARINE 2025) show on-water kitefoil-mast inception correlates with the *rate* of α change
and vertical acceleration. — *(Verified abstracts, [S4][S5][S36])*

**Inception paths relevant to a wing designer.** (a) Strut-induced: separation on the mast at leeway angle
connects the atmosphere to the low-pressure side; (b) tip-vortex/breaching: a wing tip approaching the surface
lets the tip-vortex core or a surface depression admit air; (c) wave/impact: a breaking or ventilated cavity
from the mast swept onto the wing. Geometric mitigations found in practice — fences, anti-ventilation strakes,
sharper mast leading edges, forward rake (Day et al. note the Moth vertical "is installed in the boat with
forward rake, intended to reduce the incidence of ventilation") — are documented as practice, not as validated
design rules. — *(Verified for the rake statement [S2]; others Flagged practitioner practice)*

**What a design tool can screen.** Only geometric and kinematic envelope flags: tip depth margin from the
static geometry (ANA-13 already provides "tip-depth hint … Static geometry, no wave/free-surface prediction"),
a surface-piercing state (h ≤ 0 anywhere on the lifting surface), and a Froude-number annotation. A steady
result must never be labelled ventilation-safe (area 04 D-implication, kept here). — *(Inferred from
[S4][S5][S36]; matches spec A6/ANA-04/ANA-13)*

### Unsteady / pumping physics

**Theory.** Theodorsen (NACA TR-496, 1935; NTRS 19930090935) gives the incompressible unsteady lift of a 2D
flat plate in harmonic heave h and pitch α as a non-circulatory (added-mass) part plus a circulatory part
reduced by the complex function C(k), k = ωb/U = ωc/(2U); Garrick (NACA TR-567, 1936) derived the mean thrust
and propulsive efficiency of the same motions. NASA re-computed and compared TR-496's numbers in
TP-2015-218765 and TM-2017-219667 (both on NTRS). The formulas themselves were not transcribed from the
reports this session, so their exact form is **Flagged** (standard textbook content) while the reports'
existence and public availability are Verified. — *(Verified availability [S19][S20]; formula content
Flagged)*

**Scaling.** Floryan, Van Buren, Rowley & Smits (JFM 822, 2017; arXiv 1704.07478): performance "depends on
both Strouhal number and reduced frequency, but for motions where the viscous drag is small the thrust closely
follows a linear dependence on reduced frequency", validated by water-tunnel experiments with "excellent
collapse" and consistent with biological data; Van Buren, Floryan & Smits (AIAA J 2018): heave–pitch phase φ
"proves to be a critical parameter"; Van Buren et al. (2017): the correct velocity scale is the mean
trailing-edge velocity, not the flow speed. Triantafyllou, Triantafyllou & Grosenbaugh (J. Fluids Struct. 7,
1993) established the optimal-thrust Strouhal band for oscillating foils; the commonly quoted range 0.25–0.35
(and the gap register's "St ≈ 0.4") is from recall, not read this session. — *(Verified abstracts [S17]; St
band Flagged [S18])*

**Measured pumping on foil boards.** Nothing public. Crossref searches (2018–2026) returned no
rider-kinematics or rider-power study for wingfoil, pump-foil or Moth pumping; the closest item is Liu et al.,
FDMP 2026, "Heave-Induced Thrust and Free-Surface Deformation of an Oscillating Hydrofoil" (title only). A
rough order-of-magnitude from the executed table [S40] with **guessed** inputs (c = 0.15 m, peak-to-peak heave
0.3 m, 0.5–1.5 Hz, 3–8 m/s) gives k ≈ 0.03–0.24 and St ≈ 0.02–0.15 — i.e. pump-foiling probably sits *below*
the classical high-efficiency Strouhal band and in the quasi-steady-to-mildly-unsteady k range, which would
make a quasi-steady + added-mass estimator plausible. That is an hypothesis to test with instrumented
sessions, not a finding. — *(Flagged; inputs are guesses)*

**What each estimator can claim.** Quasi-steady: instantaneous lift from the steady polar at the effective
α(t), no phase lag, no added mass — labelled "Computed estimate · quasi-steady, k < 0.05 assumed". Unsteady
linear: Theodorsen/Garrick with C(k) and added mass — valid for small amplitude, attached flow, deep water;
near a free surface the image system also becomes unsteady (Liu 2026 title suggests measurable free-surface
deformation). A "pumpability" metric could be defined as cycle-averaged thrust per unit rider mechanical power
at a given (k, St, φ) from Garrick-class theory, but it has no measurement to validate against today. —
*(Inferred)*

### Multi-fidelity reconciliation on the measurement side

**Vocabulary (ITTC 7.5-03-01-01).** Simulation error δ_S = S − T = δ_SM + δ_SN (modelling + numerical);
verification estimates U_SN² = U_I² + U_G² + U_T² + U_P² (iteration, grid, time step, other parameters);
validation compares E = D − S with U_V² = U_D² + U_SN². If |E| < U_V "the combination of all the errors in D
and S is smaller than U_V and validation is achieved" at that level; if |E| ≫ U_V, E ≈ δ_SM and can be used to
improve the model. Convergence ratio R = ε₂₁/ε₃₂ classifies grid studies (monotonic convergence 0 < R < 1;
oscillatory convergence −1 < R < 0; monotonic divergence R > 1; oscillatory divergence R < −1). Minimum three
solutions; r = √2 suggested for industrial CFD; F_S = 1.25 for 0.5 ≤ p < 2.1 with σ < Δφ, else 3. —
*(Verified, [S8])*

**GCI (Roache 1994).** p = ln[(f₃ − f₂)/(f₂ − f₁)]/ln r; Richardson estimate f_{h=0} = (r^p f₁ − f₂)/(r^p −
1); GCI₁₂ = F_S |ε₁₂|/(r^p − 1) with ε₁₂ = (f₁ − f₂)/f₁, F_S = 3 for two grids, 1.25 for three or more;
asymptotic-range check GCI₂₃/(r^p GCI₁₂) ≈ 1; r ≥ 1.1. — *(Verified, [S12])*

**ASME V&V 20.** The ITTC CFD guideline names "ASME Guide on Verification and Validation in Computational
Fluid Dynamics and Heat Transfer" (V&V 20) as a reference; the ASME page could not be opened this session.
From recall (Flagged): V&V 20-2009 defines E = S − D and u_val² = u_num² + u_input² + u_D², and the 2016
supplement adds a multivariate metric. Coleman & Stern (JFE 119, 1997) originated the E-vs-U_V criterion;
Stern, Wilson, Coleman & Paterson (JFE 123, 2001) is the methodology paper the ITTC procedure cites. Oberkampf
& Roy (CUP 2010) is the textbook; a new edition "Verification, Validation, and Uncertainty Quantification in
Scientific Computing" (CUP 2025, DOI 10.1017/9781009031004) exists. — *(Verified citations [S9][S15][S16]; V&V
20 content Flagged)*

**Presenting discrepancy to a user (GAP-06).** The honest presentation is the pair (E, U_V) per quantity
against a *named* dataset and configuration, plus the numerical uncertainty of the tool's own tier (GCI for
CFD; convergence-with-panels for VLM; the surrogate's stated error for NeuralFoil-class polars). The ITTC CFD
guideline adds a practical rule: once uncertainty is estimated for one case of a parameter study, it can be
reused for similar cases provided the flow does not change character — which licenses caching a verification
result per method/geometry family. Never collapse E into a multiplicative "correction factor" applied
silently; a learned discrepancy model (correction as a function of Re, h/c, Fr_h) is legitimate only when
displayed as its own layer with its own training-set provenance. — *(Verified guideline statement [S9];
presentation rule Inferred)*

**The label ladder made concrete (A6).**
| Label | Evidence required | Example in this file |
|---|---|---|
| Illustrative mockup | none; sample numbers | mockup readouts |
| Computed estimate | method named, envelope named, no verification proof | estimator L/D; K-factor depth correction; Cp_min screen |
| Verified numerical implementation | analytic-reference tests pass; MMS/convergence for kernels; cross-platform golden masters within stated tolerance; GCI reported for CFD | elliptical-wing e = 1, thin-airfoil 2π, NACA closed form; TMR NACA 0012 grid study |
| Experimentally compared | E and U_V (or "U_D not stated") displayed against a named dataset with comparable configuration | Day 2019 lift/drag areas (foil+strut); TR-1232 depth series; CEHINAV h/c–Fr_h grid |

### Testing numerical design software (GAP-09)

**The oracle problem and its answers.** Kanewala & Bieman (IST 56, 2014; 62 primary studies) group challenges
into those intrinsic to scientific software (oracle problems — no independent way to know the right answer)
and cultural ones (scientists "viewing the code and the model that it implements as inseparable entities").
Catalogued techniques: analytic/closed-form solutions, experimental data, pseudo-oracles and N-version
comparisons (another code), metamorphic testing, statistical tests, and code-clone detection to find
duplicated numerics. — *(Verified abstract [S7]; technique list Inferred from the review's scope)*

**Code verification by manufactured solutions.** Roache (JFE 124, 2002) and Salari & Knupp (Sandia 2000):
choose an analytic field, substitute into the governing operator to obtain a source term, run the code with
that source, and confirm the *observed* order of accuracy p against the design order on refined grids. For
CFD-Workbench this applies to any discretised operator the tool owns (loft/curve evaluators, strip
integration, VLM influence matrices): the manufactured analogue is "manufactured geometry" — an analytic
surface (e.g. exact elliptic planform with an exactly known area, centroid and second moment) whose derived
quantities must converge at the expected rate as station count grows. — *(Verified citations [S13]; adaptation
Inferred)*

**Analytic references (the floor for "Verified numerical implementation").** Thin-airfoil theory: Cl_α = 2π
per radian, flat plate α₀ = 0, Cm_{c/4} = 0 for a symmetric section; NACA 4-digit ordinates from the
closed-form thickness polynomial and parabolic mean line, and 6-series/16-series from NASA TM 4741 (area 06
[S37]); Joukowski sections give an exact Cp from the conformal map (standard, Flagged only in the sense that
no source was opened); Prandtl lifting line: elliptical loading e = 1, C_Di = C_L²/(π AR), rectangular wing
with Glauert correction τ ≈ 0.05 (this is exactly what AeroSandbox asserts at `rel=0.05` for CL and `rel=0.3`
for CDi on an AR 10, 10 m × 1 m NACA 0012 wing at 5°, 25 m/s [S32]). Symmetry: a symmetric section at α = 0
must return Cl = 0 and Cm = 0 to round-off; an antisymmetric aileron must yield a rolling moment while the
undeflected wing gives `Cl ≈ 0 (abs 1e-6)` [S32]. — *(Verified for the AeroSandbox assertions; theory
standard)*

**Golden-master / approval tests.** SU2 stores per-case reference values and compares logged residuals/forces
at a given iteration with an absolute tolerance the case sets (`abs(float(data[j]) - self.test_vals[j]) >
self.tol`, default 0.0), with timeouts and sanitizer-aware enabling [S31]. Lesson: tolerances are per quantity
and per case, stored beside the reference, and the reference carries provenance (commit, platform). Approval
frameworks for .NET (Verify, MIT; ApprovalTests.Net) serialise outputs to reviewed files; for numerics they
need a custom comparer with tolerance, never string equality of floats. — *(Verified [S31][S35]; practice
Inferred)*

**Float comparison rules.** (i) Relative tolerance for O(1) coefficients (|a − b| ≤ ε_rel·max(|a|,|b|)); (ii)
absolute tolerance near zero (Cl at α = 0, Cm of a symmetric section) because relative error is undefined
there; (iii) ULP comparison only for operations IEEE 754 makes exact-rounded (+ − × ÷ √ FMA), where bitwise
reproducibility across macOS ARM64 and Windows x64 is achievable if the compiler does not contract (.NET:
`Math.FusedMultiplyAdd` is explicit and "rounded as one ternary operation"; whether RyuJIT ever auto-contracts
a*b+c is **Flagged** — it did not in versions known from recall, and a spike must confirm for .NET 10); (iv)
transcendental functions (sin, cos, exp, log, pow, atan2) differ across libm implementations by up to a few
ULP — documented for .NET (`Math.Sin` "may differ between different operating systems or architectures") and
for Rust ("non-deterministic … varies by platform") — so any golden master containing them must use tolerance
≈ 1e-12 relative (or ship its own polynomial implementations for bitwise parity). — *(Verified [S33][S34];
rules Inferred)*

**Metamorphic relations for this domain.** Scale invariance: scaling all lengths by λ at fixed Re and α leaves
Cl, Cd, Cm unchanged and scales forces by λ² (with V adjusted for Re) — a test that catches unit bugs; mirror
symmetry: mirroring the planform about the centreline leaves lift and drag unchanged and flips roll and side
force; unit round-trip: SI → imperial → SI equals identity within 1 ULP of the conversion constants; station
insertion: adding an authored station at a location where the loft is interpolated changes the evaluated
surface by less than the DOC-02 tolerance (1 µm); reparametrisation: reversing the parameter direction of a
curve leaves the evaluated shape and derived area invariant; monotonic depth: with everything else fixed,
increasing h toward the deep asymptote must move the corrected lift monotonically toward the deep-water value.
Chen et al. (ACM CSUR 51, 2018) is the review of the method. — *(Verified citation [S14]; relations Inferred)*

**Property-based testing.** FsCheck (BSD-3-Clause) for .NET, proptest (Apache-2.0/MIT) for Rust, Hypothesis
(licence unconfirmed via API; MPL-2.0 from recall) for Python spikes: generate random valid
geometries/operating points and assert invariants (derived AR = b²/S never stored; area > 0; Cl(α) monotonic
in the linear range; σ decreases with V). — *(Verified licences [S35]; practice Inferred)*

**Parsers and regression corpora.** The Phase-0 defect (Lednicer read as Selig, yielding a plausible 6 %-thick
NACA 0012) is a parser-oracle failure; controls: a corpus of `.dat` files with SHA-256, source URL and licence
per file (area 06 already forbids vendoring UIUC files, so the corpus is generated or user-supplied),
round-trip tests (parse → write → parse identical), differential parsing (Selig and Lednicer readers on the
same geometry must agree), fuzzing of the tokenizer with FsCheck/proptest-generated byte streams, and
thickness/closure invariants after parse. — *(Inferred; defect from grounding)*

**CI rings.** Ring 0 (every push): analytic references, metamorphic and property tests, parser fuzz smoke,
cross-platform golden masters on macOS ARM64 and Windows x64 runners; Ring 1 (readiness): panel/VLM
convergence sweeps, MMS order-of-accuracy, SU2/OpenFOAM smoke on a tiny mesh if a backend is detected; Ring 2
(post-merge/nightly): the Day 2019 and TR-1232 comparisons producing E and U_V tables, TMR NACA 0012 grid
study with GCI. — *(Inferred from [S8][S9][S31] and the repo's CI standard)*

### Product benchmarks as sanity bounds (GAP-12)

The product table in area 04 gives area, span, AR and manufacturer take-off/top-speed bands per discipline.
Usable as *range checks*, not as validation: (a) wing loading W/S from rider + system mass against the product
band; (b) CL at cruise from W/(½ρV²S) must fall inside the section's polar bucket at the operating Re; (c) L/D
of the wing-only estimator must not exceed the elliptical-wing bound 1/(C_D0/C_L + C_L/(π e AR)) with e ≤ 1;
(d) take-off speed from CL_max(Re) must lie inside the maker's stated band ±30 % (band width is itself
Flagged). Manufacturers' numbers are marketing envelopes with unstated rider mass, water and method; they
bound plausibility and never label a result. — *(Inferred; product data from area 04 [S36])*

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| ITTC 7.5-03-01-01 Rev 05 (2024) | uncertainty of a simulation result | E vs U_V; Stern et al. and Eça–Hoekstra verification | precise vocabulary, factor-of-safety rules | assumes ≥3–4 grids; silent on surrogates/panel codes | ITTC © (procedure text) | Verified [S8] |
| ITTC 7.5-03-02-03 Rev 02 (2024) | practical CFD guidance | grid, y⁺ (1 for wall-resolved; 30–100 wall functions), V&V pointer, reuse of uncertainty across a parameter study | actionable defaults | ship-oriented, not hydrofoil-specific | ITTC © | Verified [S9] |
| ASME V&V 20 | validation metric for CFD/heat transfer | u_val from u_num, u_input, u_D | standard in the US aerospace/CFD community | paywalled; page not reachable this session | ASME © | Flagged [S15] |
| Roache GCI (NPARC page) | discretisation uncertainty | Richardson + F_S | simple, universal | needs asymptotic range | public | Verified [S12] |
| Day et al. 2019 | benchmark T-foil + simplified prediction | tank + XFOIL/lifting line + corrections | closest configuration; open PDF | foil+strut areas; no formal uncertainty | author PDF (Strathprints) | Verified [S2] |
| NACA TR-1232 | boundary effects on hydrofoils | multi-image theory + tank | depth and Fr series, two AR | 1950s scan; strut tares needed | US Government work (public domain) | Verified [S3] |
| NASA TMR NACA 0012 | turbulence-model verification | grids + Ladson/Gregory data | free grids and data | air, Re 6 × 10⁶, tripped | public | Verified [S6] |
| SU2 TestCases | regression of a CFD code | absolute per-case tolerance on logged values | provenance per case | exact (tol 0) references brittle across platforms | LGPL-2.1 (Flagged) | Verified [S31] |
| AeroSandbox tests | verification of a low-order aero library | pytest.approx vs Prandtl theory | analytic oracle pattern to copy | loose tolerances (5 %/30 %) | MIT | Verified [S32] |
| Kanewala & Bieman 2014 | testing scientific software | systematic review | maps the oracle problem | no hydrodynamics specifics | Elsevier © | Verified [S7] |
| Chen et al. 2018 | metamorphic testing | survey | catalogue of relations | generic | ACM © | Verified citation [S14] |
| Harwood/Young/Ceccio 2016; Young et al. 2017 | ventilation regimes and scaling | tank + theory | mechanism, hysteresis, scaling | strut-like geometry; not a consumer wing | Cambridge/ASME © | Verified abstracts [S4][S5] |
| Floryan et al. 2017 | unsteady propulsion scaling | water-tunnel + scaling laws | St and k both matter; linear thrust in k | 2D rigid foils | arXiv (open) | Verified [S17] |
| Amromin & Rozhdestvensky 2022 | inception vs pressure minimum | validated 2D multizone correlations | honest about CFD inception limits | propeller focus | CC BY 4.0 | Verified [S28] |

## Reference information

- **ITTC 7.5-03-01-01 Rev 05 (2024)** — requires: ≥3 grids (Stern) or ≥4 (Eça–Hoekstra), report R, p, U_SN;
  validation reported as E with U_V; the tool's CFD tier must emit these per case. [S8]
- **ITTC 7.5-03-02-03 Rev 02 (2024)** — requires: y⁺ ≈ 1 for wall-resolved RANS, 30 < y⁺ < 100 for wall
  functions, hex/prism near-wall cells, refinement in feature regions, and that "any numerical results should
  be considered together with its numerical uncertainty". [S9]
- **ITTC 7.5-02-03-03.4 Rev 03 (2024)** — establishes that only sheet cavitation is reliably predicted;
  tip-vortex and cloud are research; V&V baseline is experimental data. [S10]
- **ITTC 7.5-02-02-02 Rev 03 (2021)** — GUM-based uncertainty for resistance tests (Type A/B, expanded U =
  k·u); the template for how a tank dataset *should* state U_D. [S11]
- **NACA TR-1232** — public-domain depth/Froude data and image theory; requires strut tares when used as
  wing-only. [S3]
- **NACA TR-496 / TR-567** — unsteady lift and thrust; requires transcription and a unit test against NASA
  TP-2015-218765's recomputed tables before any unsteady estimator is labelled Verified. [S19][S20]
- **Roache 1994/2002; Salari & Knupp 2000** — GCI and MMS; requires observed-order tests for owned
  discretisations. [S12][S13]
- **Oberkampf & Roy 2010/2025** — V&V textbook; the 2025 edition adds UQ; recommended as the shared vocabulary
  source. [S15]
- **Coleman & Stern 1997; Stern et al. 2001** — origin of E/U_V. [S16]
- **Kanewala & Bieman 2014; Chen et al. 2018** — the testing-method canon. [S7][S14]
- **.NET and Rust float docs** — platform variability of transcendental functions; FMA is exact-rounded.
  [S33][S34]

## Data, constants, formulae and invariants

**Cavitation number with depth.** σ = (p_atm + ρ g h − p_v)/(½ ρ V²). Units Pa, kg/m³, m, m/s. p_v from the
pinned ITTC water record (ANA-15). Validity: steady, uniform inflow, h measured to the section under
consideration. Screening rule: cavitation-free claim requires σ > −Cp_min at every station and α in the
envelope; inception may occur earlier or later by the nuclei/Re effects above. Executed example (seawater ρ =
1025 kg/m³, h = 0.5 m, p_v ≈ 1.7 kPa **Flagged**): V_cav = √(2(p_∞ − p_v)/(ρ(−Cp_min))) gives 39 kn (Cp_min
−0.5), 28 kn (−1.0), 20 kn (−2.0), 16 kn (−3.0). [S40]

**Froude numbers.** Fr_h = V/√(g h); Fr_c = V/√(g c); g = 9.80665 m/s². Executed envelope (5–20 m/s, h 0.3/0.6
m, c 0.1/0.2 m): Fr_h 2.1–11.7, Fr_c 3.6–20.2, h/c 1.5–6, σ 8.2→0.50. [S40]

**Infinite-Froude lift and induced-drag correction (Daskovsky, as used by Day et al.).** C_L,corr = C_L · (1 +
2/AR)/(1 + 2K(1 + σ_i)/AR), K = (16(h/c)² + 1)/(16(h/c)² + 2), σ_i = 1/(1 + 12 h/b); C_Di,corr = C_Di (1 +
σ_i). Validity: Fr_h → ∞, attached flow, h/c ≳ 1. Executed values (AR 8, b = 1 m): ratio 0.960 at h/c 1.5,
0.978 at h/c 6. [S2][S40] — label **Computed estimate**; disagrees with CEHINAV by ≈4× at h/c = 4 [S36].

**2D wave drag (classical, Inferred form).** C_Dw ≈ (C_L²/(2 Fr_c²)) · exp(−2 (h/c)/Fr_c²), reference area c
per unit span; vanishes as Fr_c → ∞ or h/c → ∞. [S2]

**Strut/junction/spray terms (Day et al., after Gibbs & Cox 1954, Hoerner 1965, Coffee & McKann 1953).** Strut
interference on a fully submerged foil: C_L/(1 + γ), C_Di (1 + γ)², γ = 0.8 t/b for a central strut; junction
drag D_j = ½ρV² t̄² (17 (t̄/c)² − 0.05); strut spray/wave drag D_ws = ½ρV² · 0.0275 · c t (12 % strut,
moderate Fr_c) or 0.54 t² (Hoerner 10-13). Validity: empirical, 1950s struts. [S2]

**Unsteady parameters.** Reduced frequency k = ω c/(2U) = π f c/U; Strouhal St = f A/U with A the peak-to-peak
trailing-edge excursion. Executed order-of-magnitude for pumping guesses: k 0.03–0.24, St 0.02–0.15 (inputs
Flagged). [S40]

**Verification and validation.** E = D − S; U_V² = U_D² + U_SN²; U_SN² = U_I² + U_G² + U_T² + U_P²; R =
ε₂₁/ε₃₂; GCI = F_S |ε|/(r^p − 1); F_S 1.25 (≥3 grids, 0.5 ≤ p < 2.1) else 3; r ≥ 1.1, √2 suggested. [S8][S12]

**Analytic test oracles.** Cl_α = 2π rad⁻¹ (thin airfoil); C_Di = C_L²/(π AR) with e = 1 (elliptic); Cl = Cm =
0 for a symmetric section at α = 0; AR = b²/S (derived, never stored — grounding); NACA 4-digit closed form
with closed-TE coefficient −0.1036 (area 06). [S32][S37]

**Invariants and edge cases.** V ≤ 0 → no operating point; h ≤ 0 on any lifting-surface point →
surface-piercing state, estimator unavailable, ventilation flag only; σ ≤ −Cp_min → cavitation flag; a golden
master without a stored tolerance and platform provenance is invalid; a Verified label without a passing
analytic-reference test in CI is a defect; an "Experimentally compared" label without a dataset id,
configuration statement (wing-only vs foil+strut) and E/U_V is a defect.

## Design implications for CFD-Workbench

1. **A6 label semantics become mechanical.** "Verified numerical implementation" is granted per *method* by a
   CI ring-0 suite (analytic references + metamorphic + cross-platform golden masters) and per *CFD case* by a
   reported GCI; "Experimentally compared" is granted per *result* only when the UI shows dataset id,
   configuration comparability (wing-only / foil+strut / near-surface), E and U_V (or "U_D not stated by
   source"). Spec A6 already forbids a global badge; this makes the proof concrete.
2. **Validation ladder datasets (GAP-01) in priority order:** Day 2019 (transcribe lift/drag areas; treat as
   foil+strut), TR-1232 (extract AR 10/AR 4 depth series and strut tares from the NTRS PDF), CEHINAV 2026
   (request data; no licence stated), TMR NACA 0012 (backend verification), ADA032272 (extract tables;
   establish the strut/pod configuration first). Add each as a provenance-carrying fixture under the same
   hashing rule as the section catalog.
3. **Operating point (A3) must carry depth h, and derive Fr_h, Fr_c, h/c and σ** with p_v from the pinned
   water record; display "deep-water assumption (h/c ≥ 5)" when h is absent (area 04 D2 stands). The K-factor
   correction may ship as a *Computed estimate · infinite-Froude image* layer, never as the displayed lift.
4. **Cavitation screening (ANA-02/10/14):** show σ_required = −Cp_min per station with the surrogate's
   sampling caveat; show the operating σ with its depth term; expose an explicit user-chosen margin (default
   proposal 15 %, labelled "practitioner assumption, not sourced") and never the phrase "cavitation-free";
   label tip-vortex and cloud cavitation as *not screened*.
5. **Ventilation screen:** tip-depth margin from static geometry (ANA-13), surface-piercing detection, Fr_h
   annotation, and a fixed text "steady analysis cannot predict ventilation onset; onset is dynamic and
   hysteretic [Harwood 2016; Augier 2025]". No geometry rule (fences, strakes) is recommended by the tool
   until a source quantifies it.
6. **Future pumping objective (GAP-05):** define the unsteady estimator interface now (inputs f, heave
   amplitude, pitch amplitude, phase φ, U, c; outputs cycle-mean thrust, power, efficiency, k, St) with two
   implementations — quasi-steady and Theodorsen/Garrick — both labelled Computed estimate; add a unit test
   against NASA TP-2015-218765's recomputed C(k) tables before either is called Verified; record that no rider
   dataset exists and plan an instrumented-session capture (GPS + IMU + strain) as the first field
   measurement.
7. **Multi-fidelity display (GAP-06):** when the estimator, VLM and CFD disagree, show all three with their
   tier uncertainties (surrogate error, panel convergence, GCI) and, where a dataset applies, E per tier; the
   recommended value is the highest tier whose uncertainty is *reported*, not the lowest number. No hidden
   correction factor.
8. **Testing strategy for the geometry kernel:** manufactured geometries (exact elliptic and trapezoidal
   planforms with closed-form area/centroid/inertia), observed-order tests on station refinement, DOC-02's 1
   µm round-trip on both platforms, reparametrisation and station-insertion metamorphic tests, and
   FsCheck/proptest invariants (area > 0, chord > 0, monotone span parameter).
9. **Testing strategy for analysis tiers:** thin-airfoil and Joukowski oracles for the 2D path;
   elliptical-wing e = 1 and Prandtl rectangular-wing τ for lifting line/VLM at tolerances tighter than
   AeroSandbox's (propose rel 1 % for CL, 5 % for CDi, to be set after a spike); NACA 0012 TMR for any RANS
   backend, with GCI.
10. **Cross-platform equivalence (CLI-01, DOC-02):** publish the per-operation tolerance CLI-01 requires as:
    bitwise for geometry evaluation restricted to +,−,×,÷,√,FMA (verify by spike that .NET 10 RyuJIT does not
    auto-contract); relative 1e-12 for anything using transcendental functions; store golden masters with
    platform, runtime version and commit; run both OS runners in ring 0.
11. **Sanity bounds (GAP-12):** implement the four range checks of the product-benchmark section as *advisory*
    findings labelled "outside the market envelope (marketing data)", never as a validity gate.
12. **Borrow:** SU2's per-case stored tolerance + reference provenance; AeroSandbox's analytic-oracle tests;
    ITTC's E/U_V vocabulary verbatim in the UI glossary. **Avoid:** SU2's default exact (tol = 0) comparison
    for anything containing libm calls; any single-number "correction factor" UI.

## Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Exact contents and strut/pod configuration of DTIC ADA032272 | download the 107-page PDF from archive.org and read the model description and Table 1 |
| Shen & Dimotakis NACA 66(MOD) Re, σ and load values | open JFE 111(3) 1989 (DOI 10.1115/1.3243645) |
| ASME V&V 20-2009 (and 2016 supplement) exact metric definitions | obtain the standard; until then cite ITTC/Coleman–Stern |
| Whether .NET 10 RyuJIT ever contracts a*b+c into FMA on ARM64/x64 | 20-line `dotnet run app.cs` spike computing (1+ε)(1−ε)−1 on both platforms |
| Theodorsen C(k) and Garrick thrust formulas transcribed and checked | transcribe from TR-496/TR-567 and test against NASA TP-2015-218765 tables |
| Classical Strouhal efficiency band (0.25–0.35 vs 0.4) | open Triantafyllou et al. 1993 (DOI 10.1006/jfls.1993.1012) |
| Brennen's inception/nuclei sentences | fetch the Caltech chapter HTML or PDF (chapters 1 and 6) |
| CEHINAV data access and licence | e-mail the authors; the article states no data licence (area 04) |
| Rider pumping kinematics/power | instrumented session (GPS + IMU + strain-gauged mast) — no literature exists |
| The "AMC T-foil" dataset named in the gap register | ask the author of the register for the citation; nothing at AMC was located |

**Failure modes.** *Silent:* labelling a K-factor-corrected lift Verified when the only tank data disagree by
≈4×; comparing wing-only coefficients with foil+strut lift areas; a golden master that passes on macOS and
fails on Windows by 1 ULP being "fixed" by loosening tolerance globally; σ computed without the depth term; a
parser returning plausible wrong geometry. *Expensive:* running a CFD validation without a grid study (no
U_SN, so no validation statement is possible); building an unsteady estimator before the C(k) test exists.
*Irreversible:* shipping "cavitation-free"/"ventilation-safe" wording; a foil designed to a Cp_min screen
without margin failing at 30 kn.

## Disconfirming views sought

- **"Day 2019 is too slow (≤ 4.5 m/s) and too old a foil to matter."** True that Re ≤ 3.6 × 10⁵ is the low end
  of wingfoil racing and the foil is a 2006 Bladerider; but it is the only open dataset with a flap sweep,
  repeats and a depth pair on a T-foil of the right size, and low Re is exactly where the catalog's Eppler
  ranking flips (area 04). Fared: stands as rank 1 with the Re caveat displayed.
- **"The image-vortex correction is Verified theory (Wadlin) — ship it."** TR-1232 verified it against 1950s
  AR 10/4 data at Fr_h up to the critical speed with satisfactory engineering agreement; the CEHINAV 2026 wing
  shows a much larger Froude-dependent loss at h/c = 4. Two verified sources, different regimes; the
  correction is Verified *for TR-1232's configuration* and unverified for a race wing. Fared: keep as Computed
  estimate.
- **"σ_i = −Cp_min is good enough for a screen."** Amromin's work and the ITTC procedure show inception
  depends on nuclei, Re, Weber number and even material; but as a *conservative bound in nuclei-rich seawater*
  the equality is the right screening variable if a margin and the words "screening estimate" accompany it.
  Fared: kept, with margin and label.
- **"Pumping is governed by St ≈ 0.3–0.4, so an efficiency metric is straightforward."** Floryan et al. show
  dependence on both St and k and on heave–pitch phase; the order-of-magnitude estimate places pump-foiling
  below the classical band; and no rider data exist. Fared: the gap register's "regardless of body geometry"
  claim is weakened (see contradictions).
- **"Bitwise cross-platform determinism is achievable in .NET/Rust."** Verified only for IEEE-exact
  operations; both runtimes document platform-dependent transcendental results. Fared: tolerance-based golden
  masters are mandatory.
- **"Tank data need no U_D if the paper gives none."** ITTC 7.5-02-02-02 shows what a proper budget looks
  like; when absent, the UI must say "U_D not stated", and validation can only be claimed at the level of U_SN
  alone with that caveat. Fared: stands.

**Contradicts existing repo knowledge.** (1) The gap register attributes T-foil towing-tank data to the
"Australian Maritime College"; the only T-foil tank dataset actually cited in the repo (area 06 S12) is Day,
Cocard & Troll at the **Kelvin Hydrodynamics Laboratory, University of Strathclyde, Glasgow** [S2]; no AMC
dataset was located — the AMC attribution is Flagged until a citation appears. (2) The gap register states
"peak propulsive efficiency clusters near St ≈ 0.4 regardless of body geometry or speed (Verified)"; Floryan
et al. 2017 [S17] establish that performance depends on both St and reduced frequency and Triantafyllou et al.
1993 [S18] are usually quoted at 0.25–0.35 — the "regardless" clause is not supported by the sources opened
here. (3) The gap register describes ADA032272 as measuring "NACA 16-309 and NACA 64A309 hydrofoils"; the
archive record says the models are "geometrically similar to the forward foil system of the PCH hydrofoil
craft" [S1], i.e. a foil-system model — consistent with spec A6's foil-plus-strut caveat, sharper than the
register's wording. (4) The repo grounding cites ITTC 7.5-03-01-01 nowhere, but area references assume the
2021 revision family; the current document is Revision 05 (2024) [S8].

## Glossary terms

- **Comparison error E** — E = D − S, data minus simulation; the combination of all data and simulation
  errors. *(Verified, [S8])*
- **Validation uncertainty U_V** — √(U_D² + U_SN²); the level at which validation can be claimed when |E| <
  U_V. *(Verified, [S8])*
- **Simulation numerical uncertainty U_SN** — √(U_I² + U_G² + U_T² + U_P²): iteration, grid, time-step and
  other-parameter contributions. *(Verified, [S8])*
- **Grid Convergence Index (GCI)** — F_S|ε|/(r^p − 1), a Richardson-based band on discretisation error with
  factor of safety F_S. *(Verified, [S12])*
- **Method of manufactured solutions (MMS)** — code verification by inserting an analytic solution through a
  source term and checking observed order of accuracy. *(Verified citation, [S13])*
- **Metamorphic relation** — a necessary property between outputs of related inputs (scale, mirror, unit
  round-trip) used when no oracle exists. *(Verified citation, [S14])*
- **Oracle problem** — the absence of an independent way to decide whether a scientific program's output is
  correct. *(Verified, [S7])*
- **Golden master / approval test** — regression comparison against a stored, provenance-carrying reference
  output with a per-quantity tolerance. *(Inferred from [S31])*
- **Cavitation number σ** — (p_∞ − p_v)/(½ρV²) with p_∞ including hydrostatic depth. *(Standard; Inferred
  label)*
- **Inception number σ_i** — the σ at which cavitation first appears; ≤ −Cp_min in nuclei-poor water, can
  exceed it with laminar separation. *(Flagged mechanisms, [S28][S29])*
- **Sheet / cloud / tip-vortex cavitation** — attached leading-edge cavity; shed erosive clouds; vortex-core
  cavitation at the lowest σ_i. Only sheet is reliably predicted numerically. *(Verified, [S10])*
- **Depth Froude number Fr_h** — V/√(gh); selects the free-surface image sign (anti-symmetric at high Fr_h).
  *(Verified usage, [S3])*
- **Chord Froude number Fr_c** — V/√(gc); governs 2D wave drag. *(Inferred, [S2])*
- **Ventilation regimes** — fully wetted, partially ventilated, fully ventilated, with overlapping stability
  regions (bistability, hysteresis). *(Verified, [S4])*
- **Re-entrant jet** — the upstream flow at cavity closure whose angle governs washout/elimination of a
  ventilated cavity. *(Verified, [S4])*
- **Reduced frequency k** — ωc/(2U); measures unsteadiness of an oscillating foil. *(Standard; formula Flagged
  as not transcribed from TR-496)*
- **Strouhal number St** — fA/U with A the peak-to-peak trailing-edge excursion; propulsion depends on St and
  k jointly. *(Verified, [S17])*
- **Theodorsen function C(k)** — complex lift-deficiency function of the circulatory unsteady lift. *(Verified
  existence of TR-496; content Flagged)*
- **Lift area / drag area** — L/(½ρV²), D/(½ρV²): force divided by dynamic pressure, used when reference area
  is ambiguous (foil+strut). *(Verified, [S2])*
- **Ncrit = 4** — the e^N amplification used by Day et al. for tank/sailing water ("around 0.6 %" turbulence);
  distinct from the repo's proposed Ncrit 2 screening preset. *(Verified, [S2])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | Layne, D. E., "Lift and Drag Characteristics of NACA 16-309 and NACA 64A309 Hydrofoils", DTNSRDC, Oct 1976, DTIC ADA032272 (archive.org record, metadata/abstract) | primary (report record) | https://archive.org/details/DTIC_ADA032272 | 2026-09-20 | PCH foil-system test; facilities; depth influence; no uncertainty statement |
| S2 | Day, Cocard, Troll, "Experimental measurement and simplified prediction of T-foil performance for monohull dinghies", 23rd CSYS 2019 (full PDF, text extracted) | primary (peer-reviewed conference) | https://strathprints.strath.ac.uk/67655/1/Day_etal_CSYS2019_Experimental_measurement_and_simplified_prediction_of_T_foil.pdf | 2026-09-20 | Moth T-foil tank data, Ncrit 4, Daskovsky K, strut/junction/spray terms, accuracy statements |
| S3 | Wadlin, Shuford, McGehee, NACA Report 1232 (1955), NTRS 19930092241 (PDF downloaded, text extracted) | primary (report) | https://ntrs.nasa.gov/citations/19930092241 | 2026-09-20 | models, tanks, Re/depth ranges, image theory, conclusions |
| S4 | Harwood, Young, Ceccio, "Ventilated cavities on a surface-piercing hydrofoil at moderate Froude numbers…", JFM 800:5–56 (2016) | primary (peer-reviewed; abstract) | https://doi.org/10.1017/jfm.2016.373 | 2026-09-20 | regimes, formation/elimination, hysteresis, scaling |
| S5 | Young, Harwood, Miguel Montero, Ward, "Ventilation of Lifting Bodies: Review of the Physics and Discussion of Scaling Effects", AMR 69 (2017) | primary (review; abstract via Crossref) | https://doi.org/10.1115/1.4035360 | 2026-09-20 | ventilation review, scaling |
| S6 | NASA Turbulence Modeling Resource, 2D NACA 0012 Airfoil Validation Case | primary (documentation) | https://tmbwg.github.io/turbmodels/naca0012_val.html | 2026-09-20 | Re, M, datasets, caveats, data files |
| S7 | Kanewala & Bieman, "Testing scientific software: A systematic literature review", IST 56(10) (2014) (abstract via Semantic Scholar) | primary (peer-reviewed review) | https://doi.org/10.1016/j.infsof.2014.05.006 | 2026-09-20 | oracle problem, technique catalogue |
| S8 | ITTC 7.5-03-01-01 "Uncertainty Analysis in CFD Verification and Validation, Methodology and Procedures", Rev 05 (2024) and Rev 04 (2021) (PDFs downloaded, text extracted) | standard | https://www.ittc.info/media/11950/75-03-01-01.pdf ; https://www.ittc.info/media/9765/75-03-01-01.pdf | 2026-09-20 | E, U_V, U_SN, R, F_S, Eça–Hoekstra |
| S9 | ITTC 7.5-03-02-03 "Practical Guidelines for Ship CFD Applications", Rev 02 (2024) | standard | https://www.ittc.info/media/11958/75-03-02-03.pdf | 2026-09-20 | y⁺, grids, V&V pointer, uncertainty reuse |
| S10 | ITTC 7.5-02-03-03.4 "Cavitation-Induced Pressure Fluctuations: Numerical Prediction Methods", Rev 03 (2024) | standard | https://www.ittc.info/media/11820/75-02-03-034.pdf | 2026-09-20 | sheet-only reliability, tip-vortex/cloud limits |
| S11 | ITTC 7.5-02-02-02 "General Guideline for Uncertainty Analysis in Resistance Tests", Rev 03 (2021) | standard | https://www.ittc.info/media/11786/75-02-02-02.pdf | 2026-09-20 | GUM-based U_D template |
| S12 | NASA NPARC Alliance V&V, "Examining Spatial (Grid) Convergence" (Roache 1994 GCI) | primary (documentation) | https://www.grc.nasa.gov/www/wind/valid/tutorial/spatconv.html | 2026-09-20 | p, Richardson, GCI, F_S, r ≥ 1.1 |
| S13 | Roache, "Code Verification by the Method of Manufactured Solutions", JFE 124:4–10 (2002); Salari & Knupp, SAND2000-1444 | primary (citations via Crossref) | https://doi.org/10.1115/1.1436090 ; https://doi.org/10.2172/759450 | 2026-09-20 | MMS |
| S14 | Chen, Kuo, Liu, Poon, Towey, Tse, Zhou, "Metamorphic Testing: A Review of Challenges and Opportunities", ACM CSUR 51(1) (2018) | primary (citation via Crossref) | https://doi.org/10.1145/3143561 | 2026-09-20 | metamorphic testing |
| S15 | Oberkampf & Roy, "Verification and Validation in Scientific Computing", CUP 2010; new edition CUP 2025 | primary (citations via Crossref) | https://doi.org/10.1017/cbo9780511760396 ; https://doi.org/10.1017/9781009031004 | 2026-09-20 | V&V textbook; ASME V&V 20 pointer (page not reachable) |
| S16 | Coleman & Stern, "Uncertainties and CFD Code Validation", JFE 119:795–803 (1997); Stern, Wilson, Coleman, Paterson, JFE 123:793–802 (2001) | primary (abstracts via Crossref) | https://doi.org/10.1115/1.2819500 ; https://doi.org/10.1115/1.1412235 | 2026-09-20 | origin of E/U_V |
| S17 | Floryan, Van Buren, Rowley, Smits, "Scaling the propulsive performance of heaving and pitching foils", JFM 822 (2017), arXiv 1704.07478; Van Buren, Floryan, Smits, AIAA J (2018); Van Buren et al. arXiv 1707.05608 | primary (abstracts) | https://arxiv.org/abs/1704.07478 | 2026-09-20 | St and k dependence, phase, velocity scale |
| S18 | Triantafyllou, Triantafyllou, Grosenbaugh, "Optimal Thrust Development in Oscillating Foils with Application to Fish Propulsion", J. Fluids Struct. 7:205–224 (1993) | primary (citation via Crossref; content Flagged) | https://doi.org/10.1006/jfls.1993.1012 | 2026-09-20 | Strouhal band |
| S19 | Theodorsen, NACA TR-496 (1935), NTRS 19930090935; Perry, NASA TP-2015-218765 and TM-2017-219667 | primary (records via NTRS API) | https://ntrs.nasa.gov/citations/19930090935 | 2026-09-20 | unsteady lift theory availability |
| S20 | Garrick, NACA TR-567 "Propulsion of a flapping and oscillating airfoil" (1936) (cited via J. Franklin Inst. 223:402, 1937) | primary (citation) | https://doi.org/10.1016/s0016-0032(37)90737-3 | 2026-09-20 | unsteady thrust theory |
| S21 | Hough & Moran, "Froude Number Effects on Two-Dimensional Hydrofoils", J. Ship Res. 13(1):53–60 (1969) | primary (abstract via Crossref) | https://doi.org/10.5957/jsr.1969.13.1.53 | 2026-09-20 | arbitrary-Froude 2D theory |
| S22 | Duncan, "The breaking and non-breaking wave resistance of a two-dimensional hydrofoil", JFM 126:507–520 (1983) | primary (abstract via Crossref) | https://doi.org/10.1017/s0022112083000294 | 2026-09-20 | wave-breaking drag |
| S23 | Kennell & Plotkin, "A Second-Order Theory for the Potential Flow About Thin Hydrofoils", J. Ship Res. 28(1):55–64 (1984) | primary (citation via Crossref) | https://doi.org/10.5957/jsr.1984.28.1.55 | 2026-09-20 | second-order free-surface theory |
| S24 | Beaver & Zseleczky, "Full Scale Measurements on a Hydrofoil International Moth", 19th CSYS (2009) | primary (abstract via Crossref) | https://doi.org/10.5957/csys-2009-013 | 2026-09-20 | second Moth dataset |
| S25 | Foeth, van Terwisga, van Doorne, JFE 130 (2008); Foeth et al., Exp. Fluids 40:503–513 (2006) | primary (citations via Crossref) | https://doi.org/10.1115/1.2928345 ; https://doi.org/10.1007/s00348-005-0082-9 | 2026-09-20 | Delft twisted-foil cavitation |
| S26 | Shen & Dimotakis, "Viscous and Nuclei Effects on Hydrodynamic Loadings and Cavitation of a NACA 66(MOD) Foil Section", JFE 111(3) (1989) | primary (citation; content Flagged) | https://doi.org/10.1115/1.3243645 | 2026-09-20 | cavitation-tunnel section data |
| S27 | Kunz, Boger, Stinebring, Chyczewski et al., "A preconditioned Navier–Stokes method for two-phase flows with application to cavitation prediction", Comp. & Fluids 29:849–875 (2000) | primary (citation via Crossref) | https://doi.org/10.1016/s0045-7930(99)00039-0 | 2026-09-20 | RANS cavitation validation |
| S28 | Amromin & Rozhdestvensky, "Correlation between Pressure Minima and Cavitation Inception Numbers…", JMSE 10(7):871 (2022), CC BY 4.0 (abstract via Crossref) | primary (peer-reviewed, open) | https://doi.org/10.3390/jmse10070871 | 2026-09-20 | inception vs Cp_min; CFD limits |
| S29 | Brennen, "Cavitation and Bubble Dynamics", OUP 1995 (Caltech open copy; access statement read, chapters not extracted) | primary (monograph) | https://authors.library.caltech.edu/25017/ | 2026-09-20 | nucleation/inception (Flagged content) |
| S30 | Marimon Giovannetti, Banks, Ledri, Turnock, Ocean Eng. 167:1–10 (2018) | primary (citation via Crossref) | https://doi.org/10.1016/j.oceaneng.2018.08.018 | 2026-09-20 | hydroelastic test reference |
| S31 | SU2 `TestCases/TestCase.py` and TestCases directory listing (GitHub) | primary (source) | https://github.com/su2code/SU2/blob/master/TestCases/TestCase.py | 2026-09-20 | absolute per-case tolerance, timeouts |
| S32 | AeroSandbox `test_lifting_line.py` (GitHub, MIT) | primary (source) | https://github.com/peterdsharpe/AeroSandbox/blob/master/aerosandbox/aerodynamics/aero_3D/test_aero_3D/test_lifting_line.py | 2026-09-20 | analytic-oracle tests and tolerances |
| S33 | Microsoft Learn, `Math.FusedMultiplyAdd` and `Math.Sin` (.NET 10 docs) | primary (documentation) | https://learn.microsoft.com/en-us/dotnet/api/system.math.fusedmultiplyadd ; https://learn.microsoft.com/en-us/dotnet/api/system.math.sin | 2026-09-20 | FMA rounding; platform-dependent libm |
| S34 | Rust std `f64` documentation (`mul_add`, "Unspecified precision" notes) | primary (documentation) | https://doc.rust-lang.org/std/primitive.f64.html | 2026-09-20 | determinism guarantees |
| S35 | GitHub licence API for FsCheck, Hypothesis, proptest, ApprovalTests.Net, Verify, AeroSandbox, NeuralFoil, SU2, pyparsing | primary (API) | https://api.github.com/repos/<owner>/<repo>/license | 2026-09-20 | SPDX ids |
| S36 | Repo area 04, `docs/knowledge/hydrofoil-workbench/04-hydrofoil-disciplines-and-design-data.md` (CEHINAV 2026, Delft 2026, Augier 2025, product table) | secondary (repo knowledge) | local | 2026-09-20 | free-surface and ventilation anchors, product bands |
| S37 | Repo area 06, `docs/knowledge/hydrofoil-workbench/06-foil-section-catalog.md` (TM 4741, UIUC GPL, Day et al. section note) | secondary (repo knowledge) | local | 2026-09-20 | NACA generation, LSAT licence |
| S38 | Liu, Zhou, Qu, Zhao, "Heave-Induced Thrust and Free-Surface Deformation of an Oscillating Hydrofoil", FDMP (2026) (title only via Crossref) | primary (citation; Flagged) | https://doi.org/10.32604/fdmp.2026.087716 | 2026-09-20 | unsteady near-surface pointer |
| S39 | Kunz/Amromin-adjacent: Amromin, "Scale Effect of Cavitation Inception on a 2D Eppler Hydrofoil", JFE 124:186–193 (2001) (abstract via Crossref) | primary (peer-reviewed) | https://doi.org/10.1115/1.1427689 | 2026-09-20 | E817 inception scale effects |
| S40 | Scratch computation `regime.py` (Fr_h, Fr_c, h/c, σ, K-factor ratio, V_cav, k/St) executed 2026-09-20 in the session scratchpad | executed result | local (session scratchpad, not committed) | 2026-09-20 | regime tables in this file |
| S41 | ITTC Recommended Procedures index (media links for all procedures cited) | primary (index) | https://www.ittc.info/downloads/quality-systems-manual/recommended-procedures-and-guidelines/ | 2026-09-20 | current procedure URLs |
| S42 | Amromin, "Impact of Hydrofoil Material on Cavitation Inception and Desinence", JFE 139 (2017) (abstract via Crossref) | primary (peer-reviewed) | https://doi.org/10.1115/1.4035949 | 2026-09-20 | material effect on inception |
| S43 | ITTC 7.5-02-01-03 Rev 03 (2024) fresh/sea water properties (already in repo grounding) | standard | https://www.ittc.info/media/11764/75-02-01-03.pdf | 2026-09-20 | p_v and ρ provenance rule |

