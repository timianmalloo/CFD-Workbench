---
id: kb-hw-data-and-constants
title: "Domain data, constants, formulae and invariants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, generated, data-and-constants]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
review-by: 2026-12-19
summary: >-
  Formulae with units and validity envelopes, datasets, constants and invariants per area, compiled from the area files.
---

# Domain data, constants, formulae and invariants

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

## CAD programs and their UI/UX paradigms for a precision parametric foil editor

*Source: [01-cad-programs-and-ux.md](01-cad-programs-and-ux.md) (`kb-hw-cad-programs-and-ux`).*

**Curvature of a planar parametric curve** (comb ordinate). For r(t) = (x(t), y(t)):
κ(t) = (x' y'' - y' x'') / (x'^2 + y'^2)^(3/2), units 1/length (1/mm for outlines; 1/chord for normalised sections). Valid wherever |r'(t)| > 0; a comb must not be drawn at a point with zero speed (a degenerate knot), and the sign convention (left-hand normal positive) must be stated on the comb legend. The comb tooth at t is drawn along the unit normal with length s·κ(t), where s is the user-visible scale (Fusion "Scale", Rhino CurvatureGraph scale) *(Inferred from standard differential geometry; the vendor exposure of the scale is Verified, [S19])*.

**Alias CV count.** n_CV = degree + n_spans per direction; degree 3 minimum for 3D shaping; degree <= 9 in Alias *(Verified, [S14])*. For our degree-5 sections a single span has 6 controls; each added span adds one.

**Degrees of freedom of a planar curve with locks.** DOF = 2·n_controls - n_scalar_constraints. Invariant: DOF >= 0 and every remaining DOF is attributable to a named control; DOF < 0 or a redundant set is "infeasible" and must be reported with the removable-constraint list (SolveSpace pattern). Note SolveSpace's caveat that fewer constraints than DOF can still be inconsistent if they double-constrain one DOF *(Verified rule, [S39]; formula Inferred)*.

**Farin-Sapidis fairness criterion.** Fair iff the curvature plot κ(t) is continuous and has as few monotone pieces as possible. A practical invariant for a distribution curve: count sign changes of dκ/dt over the half-span, display the count next to the comb, and treat an increase caused by an edit as a warning, not an error *(Verified criterion, [S58]; the counter is Inferred)*.

**Rhino Fair contract.** Inputs: tolerance (max allowed geometry change), PreserveEnds in {Position, Tangency, Curvature}. Best on degree 3; repeat as needed *(Verified, [S2])*. If we ship an automatic fair for degree-5 curves this contract does not transfer without a spike.

**Nudge step table (proposed, extending A4).**

| Unit family | Fine (Ctrl/Cmd+arrow) | Default (arrow) | Coarse (Shift+arrow) |
|---|---|---|---|
| Outline length (mm) | 0.1 | 1 | 10 |
| Normalised section (chord fraction) | 0.0001 | 0.001 | 0.01 |
| Angle (degrees) | 0.01 | 0.1 | 1 |
| Influence weight (dimensionless, > 0) | x1.01 | x1.1 | x2 (multiplicative, so it never reaches 0) |

*(Inferred from [S8] three-step convention; the weight row is a proposal with no comparable.)*

**Keyboard orbit steps (Onshape reference).** 15 degrees per arrow; 90 with Shift; 5 with Ctrl; Z / Shift+Z zoom; Ctrl+Shift+arrows pan *(Verified, [S26])*.

**Invariants and edge cases that must always hold.**
1. A weight is strictly positive and finite; zero or non-finite disables Apply (A4, GEO-13) — consistent with NURBS weight semantics where w = 0 is degenerate *(Inferred)*.
2. A cancelled preview leaves the undo stack unchanged *(Blender rule, Verified [S32])*.
3. The comb is evaluated on the evaluated curve, never at control ordinates *(Onshape rule, Verified [S28])*.
4. A split tangent always produces a visible break marker, since the comb alone is parallel-but-different-length at G1 (continuity vocabulary Verified in `bench-cad-ux`).
5. Control count and degree are displayed, knots are not editable.
6. DPI change never bitmap-stretches the viewport (PMv2) *(Verified, [S48])*.

## Parametric curves, lofts, splines and surfaces for foil profiles and wings

*Source: [02-parametric-curves-lofts-and-surfaces.md](02-parametric-curves-lofts-and-surfaces.md) (`kb-hw-parametric-curves-lofts-and-surfaces`).*

#### Curves

| Item | Formula / value | Units · envelope | Label |
|---|---|---|---|
| B-spline curve | C(u)=Σ_{i=0}^{n} N_{i,p}(u)P_i, knots u_0…u_m, m=n+p+1 | any; u in [u_p, u_{n+1}] | Inferred [S1] |
| Cox–de Boor | N_{i,0}=1 on [u_i,u_{i+1}); N_{i,p}=((u−u_i)/(u_{i+p}−u_i))N_{i,p−1}+((u_{i+p+1}−u)/(u_{i+p+1}−u_{i+1}))N_{i+1,p−1}, 0/0:=0 | stable (convex combinations) | Verified stability [S25] |
| Continuity at a knot of multiplicity s | C^{p−s} (degree 5, simple knot → C⁴; s=5 → C⁰ corner) | interior knots only; ends undefined | Inferred [S1] |
| Knot insertion (Boehm) | Q_i = α_iP_i + (1−α_i)P_{i−1}, α_i=(ū−u_i)/(u_{i+p}−u_i) for i in (k−p+1..k), else copy | exact; executed max dev 6.9e-17 | Verified [S29] |
| Planar curvature | κ=(x'y''−y'x'')/(x'²+y'²)^{3/2}; 3D κ=|C'×C''|/|C'|³ | undefined where |C'|=0 | Inferred |
| Chord-length parameters | ū_0=0, ū_k=ū_{k−1}+|Q_k−Q_{k−1}|/L, ū_n=1 | Q_k distinct | Inferred [S1] |
| Centripetal parameters | as above with |Q_k−Q_{k−1}|^{1/2} | avoids cusps/self-intersection | Verified [S24] |
| Knot averaging | u_{j+p}=(1/p)Σ_{i=j}^{j+p−1}ū_i, j=1..n−p; ends multiplicity p+1 | guarantees Schoenberg–Whitney | Flagged [S1] |
| Weighted LSQ (splrep) | minimise Σ_k (w_k(y_k−g(x_k)))², s=0 with fixed knots; w=1/σ | w>0; even k discouraged | Verified [S7] |
| Smoothing spline | minimise Σ w_i|y_i−f(x_i)|² + λ∫(f'')² ; λ by GCV (Wahba 1990) | w linear (not squared) | Verified [S8] |
| KKT constrained LSQ | [2(NᵀWN+λF) Aᵀ; A 0][P;μ]=[2NᵀWQ;b] | needs full column rank of N restricted to null(A) | Inferred |
| Rational (NURBS) point | C(u)=Σ N w P / Σ N w; w→∞ pulls the curve onto P_i | w>0 | Verified [S29] |
| Rhino/openNURBS tolerances | zero 2⁻³²≈2.33e-10; relative 2⁻⁴²; angle 1° default (use 0.1°); distance 0.01 mm default; Rhino abs 0.01–0.001 units, floor 1e-5 | model units | Verified [S15][S16] |

#### Airfoils

| Item | Formula | Envelope | Label |
|---|---|---|---|
| CST | ζ=ψ^{0.5}(1−ψ)^{1}·Σ_{i=0}^{n}A_iC(n,i)ψ^i(1−ψ)^{n−i} + ψζ_TE (+ A_LE ψ(1−ψ)^{n+0.5}) | ψ∈[0,1]; per surface | Verified (code) [S5] |
| CST ↔ LE radius / boat-tail | A_0=√(2R_LE/c); A_n=tanβ+ζ_TE | — | Flagged [S4] |
| CST exact Bézier | ψ=t² ⇒ ζ(t) polynomial of degree 2n+3 in t | — | Inferred |
| PARSEC | y=Σ_{n=1}^{6}a_nx^{n−1/2} per surface; 11 parameters | — | Verified [S27] |
| Hicks–Henne | f_i(x)=sin^t(πx^{ln0.5/ln x_i}) added to a baseline | x∈[0,1] | Verified [S26] |
| NACA 4-digit thickness | y_t=5t(0.2969√x−0.1260x−0.3516x²+0.2843x³−0.1015x⁴); −0.1036 closes TE | x∈[0,1], t=thickness/c | Verified [S5] |
| NACA 4-digit camber | y_c=(m/p²)(2px−x²), x≤p; (m/(1−p)²)((1−2p)+2px−x²), x>p; thickness normal to camber | — | Verified [S5] |
| NACA 6-series | conformal mapping (Theodorsen); TM 4741 program; ~5e-5 c agreement | no closed form | Verified [S28] |
| Coverage of UIUC database | 20–25 design variables for all seven methods; SVD most efficient | Masters 2017 | Verified [S9] |

#### Surfaces and integrals

| Item | Formula | Label |
|---|---|---|
| Tensor-product B-spline surface | S(u,v)=ΣΣ N_{i,p}(u)N_{j,q}(v)P_{ij} | Inferred |
| Skinning compatibility | same degree (elevate) + same knot vector (insert union) → interpolate columns in v | Verified [S33] |
| Fundamental forms | E,F,G = S_u·S_u, S_u·S_v, S_v·S_v; L,M,N = S_uu·n, S_uv·n, S_vv·n; K=(LN−M²)/(EG−F²); H=(EN−2FM+GL)/(2(EG−F²)) | Inferred |
| Chordal deviation | ε=R(1−cos(Δθ/2)); Δθ_max=2acos(1−ε/R) | Inferred |
| Reference area (A4) | S=2·(b/2)∫₀¹c(η)dη (unpitched); AR=b²/S; MGC=S/b | Verified (spec) [S30] |
| Wetted area / volume | A_w=∫∫|S_u×S_v|dudv; V=(1/3)∮S·n dA | Inferred |
| Gauss–Legendre exactness | m points exact for polynomial degree ≤ 2m−1 per span | Inferred |

#### Invariants that must always hold

- Knot vector non-decreasing; clamped ends of multiplicity p+1; count = n+p+2 (state the convention if openNURBS' n+p count is used).
- All weights = 1 unless the curve is declared rational; a rational curve cannot be exported as `B_SPLINE_CURVE_WITH_KNOTS`.
- Schoenberg–Whitney satisfied before any interpolation/LSQ solve; otherwise reject with "insufficient controls".
- Knot insertion reports deviation 0 (≤ identity tolerance); any other refit reports a measured deviation > 0 or the measured value.
- Section: x(u) monotone from TE to LE and LE to TE; upper−lower thickness along the normal > 0 in the interior; LE vertical tangent when the closure says round LE.
- Channels: chord(η) > 0 except an explicitly declared zero-chord tip; stations strictly ordered in η; two stations never share η.
- Twist rotation is about the station LE, positive nose-up (A4) — the loft evaluator and the exporter must share one rotation matrix.
- Derived quantities (S, b, AR, A_w, V) are never persisted; on load, recomputed values equal the previously displayed ones within identity tolerance or a revision is flagged.

## Marine and board CAD tooling: stations, master curves and loft UX

*Source: [03-marine-and-board-cad-tooling.md](03-marine-and-board-cad-tooling.md) (`kb-hw-marine-and-board-cad-tooling`).*

**Planar curvature of a parametric curve** (used by every comb): for `r(t) = (x(t), y(t))` in model units,
`κ(t) = (x′ y″ − y′ x″) / (x′² + y′²)^{3/2}` [1/m]; radius `R = 1/κ` [m]. Valid where `|r′| > 0` (no stationary points; a degenerate control polygon breaks it and must be reported, not drawn as zero). Shape3d draws κ or R either perpendicular to the curve or against x [S1]. *(Verified maths; standard.)*

**Fairness metric (Farin–Sapidis).** Count `m` = number of monotone pieces of `κ(t)` on the curve (equivalently extrema of κ + 1). A distribution curve or section is "fair" in their sense when `m` is as small as the design intent allows; report `m` and the locations of extrema next to the comb. Validity: planar curves; for 3D curves use curvature and torsion separately [S27]. *(Flagged source; the metric itself is standard.)*

**Ruled ("straight") loft between adjacent stations** `i`, `i+1` with section curves `C_i(u)`, `C_{i+1}(u)` sharing a normalized parameter `u ∈ [0,1]` (LE-aligned, TE-aligned): `S(u, v) = (1 − v) C_i(u) + v C_{i+1}(u)`, `v ∈ [0,1]`. This is Rhino "Straight sections", MultiSurf "Ruled" and AVL's linear chord/incidence interpolation [S9][S16][S17]. Invariant: `C_i` and `C_{i+1}` must be parameterized on the same normalized chord and orientation or the loft twists (Onshape's vertex-matching problem) [S20].

**Cosine spanwise spacing** (AVL `Sspace = 1.0`, XFLR5 "cosine"): station `k` of `N` at `s_k = ½ (1 − cos(π k / N))`, `k = 0…N`, mapped to `y = s_k · b/2` [S17][S18]. Use for analysis sampling only; authored stations are user-chosen.

**Root-mirror tangent invariant (derived).** For a mirrored wing whose distribution curves are defined on the starboard half, G1 continuity of the full surface across the symmetry plane requires each channel `f(y)` in {chord, thickness, twist, elevation} to have `df/dy = 0` at `y = 0`; sweep `x_LE(y)` likewise for a smooth LE across the root. This is exactly Shape3d's "horizontal tangent" kind at a symmetric board's nose/tail stringer [S1]. It must be a hard constraint by default in Through-points mode and a named lock in Smooth mode, else "Break symmetry" later inherits a kink that was never visible. *(Inferred; not asserted by any tool's documentation — mark `assume:` in the geometry design until fixtured.)*

**Keyboard nudge semantics (comparative data).** Shape3d: Auto step scales with zoom; Shift ½, Ctrl ¼, Shift+Ctrl 1/10 [S1]. AkuShaper: "Point Movement Size" setting [S5]. Spec: 1 mm default, 0.1 mm fine, ratio/angle/normalized steps separately visible [S32]. The spec's fixed, visible step is the more auditable choice; add the zoom-scaled Auto only as an opt-in.

**Edition/price constants (2026-09-20):** Shape3d Lite 0 EUR; Design 5 EUR/mo or 49 EUR/yr; Design Pro 18/179; Design Pro + 3D Export 52/519; Design Pro + CNC 81/809 [S3]. AkuShaper 12.95 / 29.95 / 49.95 USD per month, annual discounts USD 30–36 [S5][S6]. Orca3D reseller GBP 365–1,385 [S22]. All Proprietary; none redistributable.

**Interop invariants across the surveyed tools:** every board tool reads/writes at least one other tool's native file (`.s3dx`, `.brd`, `.srf`, `.kms`, `.pbd`) [S2][S5]; every wing tool speaks a plain-text station table (AVL, XFLR5) [S17][S18]; every tool exports STL and IGES/STEP; only Shape3d and BoardCAD emit G-code [S2][S11].

## Hydrofoil disciplines and design data for water-sports foils

*Source: [04-hydrofoil-disciplines-and-design-data.md](04-hydrofoil-disciplines-and-design-data.md) (`kb-hw-hydrofoil-disciplines-and-design-data`).*

**Water properties (ITTC 7.5-02-01-03 Rev03, Verified [S1]).** Fresh water at standard atmospheric pressure; seawater at absolute salinity 35 g/kg.

| T °C | ρ fresh kg/m³ | ν fresh m²/s | ρ sea kg/m³ | ν sea m²/s | p_v fresh kPa | p_v sea kPa |
|---|---|---|---|---|---|---|
| 5 | 999.97 | 1.5182×10⁻⁶ | 1027.72 | 1.5762×10⁻⁶ | 0.8726 | 0.8547 |
| 10 | 999.70 | 1.3063×10⁻⁶ | 1027.00 | 1.3604×10⁻⁶ | 1.2282 | 1.2030 |
| 15 | 999.10 | 1.1386×10⁻⁶ | 1026.02 | 1.1892×10⁻⁶ | 1.7058 | 1.6709 |
| 20 | 998.21 | 1.0034×10⁻⁶ | 1024.81 | 1.0508×10⁻⁶ | 2.3393 | 2.2914 |
| 25 | 997.05 | 8.9266×10⁻⁷ | 1023.39 | 9.3713×10⁻⁷ | 3.1699 | 3.1050 |
| 30 | 995.65 | 8.0071×10⁻⁷ | 1021.77 | 8.4253×10⁻⁷ | 4.2470 | 4.1600 |

(The vapour-pressure column was identified by the table's header order — density, dynamic viscosity, kinematic viscosity, vapour pressure — and its 15 °C fresh value 1.7058 kPa matches the standard steam-table value; Verified with that caveat.)

**Reynolds number.** Re = V·c/ν, with V in m/s, c the local (or mean, S/b) chord in m, ν from the table. Worked once: V = 20 kn = 10.289 m/s, c = 0.12 m, seawater 15 °C → Re = 10.289 × 0.12 / 1.1892×10⁻⁶ = 1.038×10⁶. Temperature/salinity sensitivity at fixed V·c = 0.926 m²/s (20 kn, 0.09 m):

| T °C | Re fresh | Re sea |
|---|---|---|
| 5 | 6.10×10⁵ | 5.88×10⁵ |
| 10 | 7.09×10⁵ | 6.81×10⁵ |
| 15 | 8.13×10⁵ | 7.79×10⁵ |
| 20 | 9.23×10⁵ | 8.81×10⁵ |
| 25 | 1.04×10⁶ | 9.88×10⁵ |
| 30 | 1.16×10⁶ | 1.10×10⁶ |

Invariant: at equal V and c, Re_sea < Re_fresh at every temperature in the table (ν_sea/ν_fresh = 1.038–1.052). Invariant: force at fixed CL scales by ρ_sea/ρ_fresh = 1.0269 (15 °C), 1.0266 (20 °C) — but CL itself is Re-dependent, so the two effects must be applied through the polar, not as a multiplier (this confirms the reconciliation register's treatment).

**Discipline Re bands (seawater 15 °C; chord and speed inputs stated).**

| Discipline | c̄ m (from table) | V kn | Re | Label |
|---|---|---|---|---|
| Surf | 0.17–0.20 (1500–2000 cm², AR 5–7) | 7–15 | 5.2×10⁵–1.3×10⁶ | Inferred; chords from AR/area bands (spec [S3] MA1750: c̄ 0.152) |
| Pump | 0.12–0.15 (APF, Leviathan) | 8–12 | 4.2×10⁵–8.0×10⁵ | Inferred [S3][S9] |
| Downwind | 0.085–0.115 (Leviathan Pro/Blackbird) | 10–20 | 3.7×10⁵–1.0×10⁶ | Inferred [S9][S10] |
| Wingfoil freeride | 0.10–0.14 (MA, SK8, Progression) | 10–22 | 4.3×10⁵–1.3×10⁶ | Inferred [S3][S8][S11] |
| Wingfoil race | 0.075–0.095 (ART Pro, HA-X) | 12–32 | 3.9×10⁵–1.3×10⁶ | Inferred [S6][S31]; speeds Flagged |
| Windfoil iQFOiL/slalom | 0.085–0.10 (900 cm² one-design; span Flagged) | 15–35 | 5.5×10⁵–1.5×10⁶ | Inferred/Flagged [S13] |
| Kitefoil race | 0.07–0.095 | 18–38 | 5.5×10⁵–1.6×10⁶ | Inferred [S33]; speeds [S19] |
| E-foil | 0.15–0.25 | 10–24 | 6.5×10⁵–2.6×10⁶ | Flagged (no e-foil wing spec opened) |

**Lift and lift coefficient.** L = ½ ρ V² S CL; CL = L/(q S). Validity: steady, attached flow, S the projected planform area of the wing whose lift is meant, deep water (h/c ≥ 5) unless a free-surface correction is applied. Wing loading W/S in kPa is the cavitation-relevant quantity: for a given depth, the cavitation-limited top speed scales as V_cav = √(2 (p_∞ − p_v) / (ρ · (−Cp_min))) and −Cp_min rises with CL, so higher loading pushes the ceiling down.

**Cavitation number, seawater 15 °C, h = 0.5 m, p_∞ − p_v = 104.69 kPa (Verified inputs, Inferred table).**

| V kn | V m/s | q Pa | σ |
|---|---|---|---|
| 20 | 10.29 | 54,320 | 1.93 |
| 25 | 12.86 | 84,850 | 1.23 |
| 30 | 15.43 | 122,200 | 0.86 |
| 35 | 18.01 | 166,400 | 0.63 |
| 40 | 20.58 | 217,200 | 0.48 |
| 45 | 23.15 | 274,900 | 0.38 |
| 50 | 25.72 | 339,400 | 0.31 |

Invariant: σ falls as V⁻²; each extra metre of depth adds ρ g ≈ 10.07 kPa to p_∞ (sea) — ≈+9.6% σ at 0.5 → 1.5 m, which is why depth belongs in the operating point. Inception speed for −Cp_min = 0.35 / 0.5 / 0.7: 46.9 / 39.3 / 33.2 kn (Inferred).

**Free-surface Froude number.** Fr_h = V/√(g h). At 20 kn: h = 0.5 m → 4.65; h = 0.3 m → 6.0. Validity floor for "deep": h/c ≥ 5 and use the CEHINAV correction trend below that [S16].

**Strouhal number for pumping.** St = f·A/U with A the peak-to-peak heave (m), f cadence (Hz), U forward speed (m/s); propulsive optimum ≈0.25–0.40 in the flapping-foil literature [S21]; human pumping ≈0.05–0.15 (Inferred, inputs Flagged). Quasi-steady strip theory is acceptable below ≈2–3 Hz at these amplitudes [S38, secondary].

**Aspect ratio.** AR = b²/S with b the full projected span and S the projected planform area — the repo's stated convention. The product table shows makers deviating by −3% to +2%; the tool must display "AR (projected, b²/S)" and may show a maker's stated value only as an annotation.

**Invariants and edge cases.** V ≤ 0 → no operating point. h ≤ 0 → surface-piercing state: estimator unavailable, ventilation flag only. σ ≤ −Cp_min → cavitation flag, result qualified. CL_required > CL_max(Re) → "no supported take-off at this speed". Re < 2×10⁵ → outside every catalog polar cited here; Flagged output. Any preset value in this file carrying Flagged is displayed as a source-based assumption, never as a constant.

## File formats and grammars for curves, surfaces, meshes and CFD data

*Source: [05-file-formats-and-grammars.md](05-file-formats-and-grammars.md) (`kb-hw-file-formats-and-grammars`).*

- **Selig invariant:** `x[0] ≈ 1`, `x` decreases to a minimum ≈ 0 exactly once, then increases to ≈ 1; polygon simple (no self-intersection); `n ≥ ~40`. **Lednicer invariant:** counts `NU, NL` are integral floats ≥ 2; block 1 has `NU` rows with `x` non-decreasing from ≈0 to ≈1; block 2 has `NL` rows likewise; conversion to Selig = `reverse(upper) ++ lower[1:]` if the LE point is duplicated, else `reverse(upper) ++ lower` (Inferred, [S3]).
- **Trailing-edge thickness:** `t_te = |y_upper(1) − y_lower(1)|` (chord units); catalog TEs are 0–0.08 mm at 80 mm chord (repo, [S42]) — below any CNC/print floor; the exporter must state whether it closed the TE and how.
- **Storage resolution:** shortest round-trip double = exact (probe [S36]); AeroSandbox `%f` = 1e-6 chord units [S4]; KiCad = 1 nm [S23]; DOC-02 = 1 µm model space ⇒ at chord ≥ 1 m a 6-decimal chord-unit export is at the tolerance edge.
- **STEP knot rule:** `Σ u_multiplicities = n_u_control + u_degree + 1` (and likewise v); `knot_spec` ∈ {UNIFORM_KNOTS, QUASI_UNIFORM_KNOTS, PIECEWISE_BEZIER_KNOTS, UNSPECIFIED} (Flagged: enumeration from recall; where-rule existence Verified [S8]).
- **SU2 mesh:** node indices 0-based; element line = `vtk_type n0 … nk`; boundary elements in 2D are lines (3), in 3D triangles (5)/quads (9) [S13].
- **OpenFOAM polyMesh:** internal faces first; each internal face's normal points into the higher-numbered cell (owner < neighbour); boundary faces contiguous per patch (`startFace`, `nFaces`); boundary normals point out of the domain [S15].
- **3MF:** default unit millimetre; every edge shared by exactly two triangles; outward normals [S11].
- **Content hash:** `revision_hash = "sha256:" + hex(SHA-256(JCS(surface_revision_subtree)))`; JCS numbers per ECMA-262 §7.1.12.1 (`1e-7`, not `1E-07`; `0`, not `-0`) [S21][S36].
- **Units policy invariant:** every numeric field name ends in a unit suffix from a closed list (`_m`, `_m2`, `_rad`, `_kg`, `_N`, `_Pa`, `_K` or `_degC`, `_mps`) or is dimensionless and documented as such; `weight` is dimensionless and positive; a reader rejects an unknown suffix (Inferred from GAP-04 [S43]).

## Foil section catalog for hydrofoil wings, stabilizers, struts and fins

*Source: [06-foil-section-catalog.md](06-foil-section-catalog.md) (`kb-hw-foil-section-catalog`).*

**Cavitation screening.**
σ = (p_atm + ρ g h − p_v) / (½ ρ V²)  [dimensionless]; p in Pa, ρ in kg/m³, g = 9.80665 m/s², h = depth of the section below the free surface in m, V in m/s. Screening inception: cavitation is predicted when −Cp_min(α, Re) ≥ σ. Critical speed at a given (α, Re):
V_crit = √( 2 (p_atm + ρ g h − p_v) / (ρ · (−Cp_min)) )  [m/s]; 1 kn = 0.514444 m/s.
Validity: attached, steady, single-phase Cp from a 2D solver; nuclei content, roughness, unsteadiness and 3D tip/junction effects are not in it; the Delft/Caltech data show inception can depart from −Cp_min through laminar separation and nuclei effects [S37][S7] *(Verified formula structure via [S22]; the caveats Inferred)*.
Worked values (Verified by computation from the stated inputs; inputs Flagged where recalled): ρ = 1025 kg/m³, p_atm = 101,325 Pa, p_v ≈ 2,300 Pa (≈20 °C — use the admitted ITTC table, ANA-15), h = 0.8 m ⇒ p_atm + ρ g h − p_v = 107,069 Pa. Then Cp_min = −0.5 → V_crit = 20.4 m/s = 39.7 kn; −0.8 → 16.2 m/s = 31.4 kn; −1.0 → 14.5 m/s = 28.1 kn. The repo's "4412 31.4 kn / E818 42.1 kn" numbers correspond to Cp_min ≈ −0.80 and ≈ −0.445 under inputs of this kind — a consistency check, not a validation.

**Reynolds number in water.** Re = V c / ν; ν ≈ 1.05 × 10⁻⁶ m²/s (sea water ≈ 20 °C; **Flagged** recall, the pinned ITTC 7.5-02-01-03 table in the repo governs). c = 0.12 m at V = 8 m/s → Re ≈ 9.1 × 10⁵; c = 0.06 m at 5 m/s → 2.9 × 10⁵; c = 0.15 m at 12 m/s → 1.7 × 10⁶.

**Ncrit and turbulence level (Mack correlation as implemented in XFOIL):** Ncrit = −8.43 − 2.4 ln(Tu), Tu = turbulence intensity as a fraction. Tu = 0.6 % → Ncrit ≈ 3.85 (matches Day et al.'s "Ncrit 4 ≈ 0.6 %" [S12]); Ncrit 3 → Tu ≈ 0.85 %; Ncrit 2 → Tu ≈ 1.3 %; Ncrit 1 → Tu ≈ 2.0 %; Ncrit 9 → Tu ≈ 0.07 % *(formula Flagged as recall; the numerical mapping Verified by computation)*.

**Evidence table for water Ncrit** *(all Verified as quotations)*:

| Source | Value | Basis | Weight |
|---|---|---|---|
| Speer, boatdesign.net, 5 Feb 2004 [S11] | "3 or even lower" | Particulates/organisms promote transition; talk by W. Feifel (Boeing) | Practitioner opinion; author adds "I don't have any test data to say what Ncrit should be" |
| Forum hearsay, same thread [S11] | 1 "a bit too extreme" | Unnamed yacht-design company | Hearsay |
| Sailing Anarchy / repo carry-over [S28] | 1 < Ncrit < 3 | Restatement of Speer | Tertiary |
| Day, Cocard, Troll 2019 [S12] | 4 (≈0.6 % Tu) | Used against full-scale tank data; "reasonably accurate in most cases" | Primary comparison, one foil |

Invariant for the product: a polar without a stored Ncrit is not admissible (existing ANA-15); the Ncrit preset must expose a **range** and the sources above, not a constant.

**Geometry invariants for admission (ANA-09, A3):**
- Chord normalised to [0, 1] with LE at (0, 0); TE gap recorded as-filed; format (Selig/Lednicer) detected, never assumed — all eight Eppler hydrofoil files on UIUC are Lednicer, the NACA files are mostly Selig, and `n63412.dat` is Lednicer *(Verified by execution)*.
- For a generated NACA section: t/c from the coordinates must equal the designation's last two digits within 0.05 % chord (measured here: 63-209 → 9.00 %, 64A410 → 9.99 %, 16-012 → 12.00 %, 4412 → 12.02 %); camber for a 6-series a = 1.0 line at design Cl 0.2 ≈ 1.1 %, at 0.4 ≈ 2.2 % (measured: 1.10 %, 2.20 %) *(Verified by execution)*.
- SHA-256 of the exact bytes vendored or generated, plus the generator name/version/command (TM 4741 build, point count, closure option) — the hash of a generated file is meaningless without the command.

**Design Cl (ANA-16).** Known from designation only for NACA 16- and 6-series (digit after the dash ÷ 10) and 5-digit (first digit × 0.15). Eppler and H105 design Cl are **Unknown** until read from the JSR papers/book with a citation — the UI must show "Unknown", not a computed Cl at zero α *(Verified rule; values Flagged)*.

## Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity

*Source: [07-low-order-hydrodynamics.md](07-low-order-hydrodynamics.md) (`kb-hw-low-order-hydrodynamics`).*

Units are SI throughout: lengths m, speeds m/s, pressures Pa, densities kg/m³, forces N, moments N·m, angles **radians inside formulae** (degrees only on the UI). Speed conversion 1 kn = 0.514444 m/s (exact by definition of the nautical mile, 1852 m/h). g = 9.80665 m/s² (ITTC standard gravity, used in [S5]). *(Verified [S5] for g and the water properties; the knot definition is Inferred, standard.)*

#### Water snapshot (from area file 04, [S5])

Seawater S_A = 35.16504 g/kg ("35 g/kg" on the UI), 15 °C: ρ = 1026.02 kg/m³, ν = 1.1892×10⁻⁶ m²/s, p_v = 1.671 kPa. Fresh water 20 °C: p_v = 2.339 ± 0.014 kPa (U_t = ±0.1 °C). Every operating point pins (T, S_A, procedure revision) — ANA-15. *(Verified [S5]; the 15 °C row is the one area file 04 uses for the goal state.)* Depth head: ρ g = 1026.02 × 9.80665 = 10 062 Pa per metre of seawater (≈ 10.06 kPa/m; area file 04 rounds to 10.07). *(Verified arithmetic.)*

#### 2D section — thin-airfoil theory (estimator tier)

| Quantity | Formula | Units | Validity envelope | Label |
|---|---|---|---|---|
| Lift coefficient | C_l = 2π (α − α_L0) | α, α_L0 in rad; per degree 2π/57.296 = 0.1097 /deg | \|α − α_L0\| ≲ 10°, t/c ≲ 15 %, attached flow, Re high enough for a thin boundary layer; no C_l,max, no drag | Inferred (textbook; [S15][S27] agree on the 2π limit) |
| Zero-lift angle | α_L0 = −(1/π) ∫₀^π (dz_c/dx)(cos θ − 1) dθ, with x = (c/2)(1 − cos θ) | rad | same | Inferred |
| Quarter-chord moment | C_m,c/4 = (π/4)(A₂ − A₁), Aₙ = (2/π)∫₀^π (dz_c/dx) cos(nθ) dθ; independent of α (aerodynamic centre at c/4) | — | same | Inferred (sign convention: nose-up positive; check against a NACA 4412 fixture, C_m,c/4 ≈ −0.10, Flagged value) |
| Ideal (design) angle | the α at which A₀ = 0 (no leading-edge singularity; stagnation point on the LE); NACA 6-series "design C_l" is defined there | rad | camber lines only | Inferred |
| Thickness effect on lift slope | inviscid slope rises with thickness; the Joukowski estimate 2π(1 + 0.77 t/c) is recalled only | — | — | Flagged |

Invariant: at the estimator tier the only honest drag is the friction/form build-up below; the thin-airfoil C_l is capped at the polar tier's converged range or at 1.0, whichever is lower, and is labelled "Computed estimate — inviscid, no stall". *(Inferred.)*

#### 2D section — XFOIL / NeuralFoil polar tier

| Quantity | Formula / definition | Validity envelope | Label |
|---|---|---|---|
| Squire–Young drag | C_d = 2 θ_w (u_e,w / V)^((H_w + 5)/2), evaluated at the **last wake point** (≈ 1 chord downstream), where θ_w, H_w and u_e,w are the wake momentum thickness, shape factor and edge velocity there | attached or mildly separated flow; wake reached steady state; "always reasonable" at the last wake point, not at the trailing edge | Verified [S1] |
| Transition | e^N with user Ncrit; Ncrit table: sailplane 12–14, motorglider 11–13, clean tunnel 10–12, average tunnel 9, dirty tunnel 4–8; bypass ≈ Ncrit ≤ 1 | Ncrit is an *input*; polar keyed by (profile hash, Re, Ncrit, α) | Verified [S1] |
| Mack correlation | N = −8.43 − 2.4 ln(Tu), Tu the free-stream turbulence intensity (fraction); Ncrit 9 ↔ Tu ≈ 0.07 % | air-tunnel correlation; no water primary source | Flagged [S42] |
| Reynolds number | Re = V c / ν (section, chord-based); wing reference Re uses c̄ = S/b; strips use c(y) | ν from the water snapshot; Re ≳ 10⁵ for XFOIL convergence | Verified definition; envelope Inferred [S1] |
| Mach in water | M = V / c_sound ≈ 10 / 1480 ≈ 0.0068; Kármán–Tsien factor differs from 1 by < 10⁻⁴ | always in water | Inferred (arithmetic) |
| NeuralFoil surrogate error | MAE vs XFoil: C_L 0.012, ln C_D 0.020 (≈ 2 %), C_M 0.002 ("xxxlarge") | inside the CST-representable space; α ∈ [−27.9°, 28.6°]; Ncrit ∈ [0, 18]; analysis_confidence high | Verified [S2][S3] |
| Cp from NeuralFoil | Cp = 1 − (u_e/u_∞)² at 64 stations; Cp_min is the minimum over 64 nodes and **under-reads** a sharp suction peak by an unquantified amount | screening only | Verified formula [S3]; under-read magnitude Flagged |

#### Fully turbulent friction fallback and form factor

C_F(Re) = 0.075 / (log₁₀ Re − 2)²  — ITTC 1957 model–ship correlation line, Re on the reference length; a *correlation* line (carries a form allowance), not a pure flat-plate law. Values: Re 5×10⁵ → 0.00548; 10⁶ → 0.00469; 10⁷ → 0.00300. *(Formula Inferred — standard ITTC; URL Flagged [S25]; arithmetic this session.)*

Section form factor (Hoerner, streamline sections): FF = 1 + 2 (t/c) + 60 (t/c)⁴ → t/c 0.09: 1.184; 0.10: 1.206; 0.12: 1.252; 0.14: 1.303. Fully turbulent section drag bound: C_d0,turb ≈ 2 C_F(Re_c) · FF → at Re 5×10⁵, t/c 0.10: 0.0132; at Re 10⁶: 0.0113. *(Inferred [S26]; the Hoerner coefficients are recalled and the book was not opened — Flagged until checked.)* Validity: fully turbulent, attached, t/c ≤ 0.2. Use: the pessimistic (rough / fouled / tripped) bound shown beside the Ncrit polar; a laminar-bucket section at Re 10⁶ can sit at roughly half this value (Flagged magnitude; measure with an XFOIL sweep).

#### Cavitation screening

| Quantity | Formula | Units | Validity | Label |
|---|---|---|---|---|
| Cavitation number | σ = (p_∞ − p_v) / (½ ρ V²), p_∞ = p_atm + ρ g h | Pa / Pa → dimensionless; h = depth of the point below the undisturbed surface [m] | steady, deep enough that the free surface does not lower the local static pressure (h/c ≳ 1); p_v at the water temperature and salinity | Verified [S5] for p_v, ρ; definition standard (Inferred) |
| Inception screen (sheet cavitation) | cavitation *possible* where Cp ≤ −σ, i.e. when σ ≤ −Cp_min | — | necessary condition only; measured σ_i deviates with Re, nuclei, water quality | Inferred [S28] |
| Critical speed | V_crit = √( 2 (p_atm + ρ g h − p_v) / (ρ (−Cp_min)) ) | m/s | requires −Cp_min > 0; Cp_min from a resolved distribution at the operating (Re, Ncrit, α) | Inferred (algebra from the two lines above) |
| σ-required curve (bucket) | σ_req(C_l) = −Cp_min(C_l) at fixed Re, Ncrit | — | one section, one Re/Ncrit per curve — ANA-10 | Inferred |

Worked numbers (seawater 15 °C, p_atm = 101.325 kPa, h = 0.5 m): p_∞ − p_v = 101 325 + 5 031 − 1 671 = 104 685 Pa. σ at 10 kn (5.144 m/s, q = 13 575 Pa) = 7.71; at 32 kn (16.46 m/s, q = 139 000 Pa) = 0.753. V_crit for −Cp_min = 0.50: 20.20 m/s = 39.3 kn; for 0.75: 16.49 m/s = 32.1 kn; for 1.0: 14.28 m/s = 27.8 kn; for 0.35: 24.15 m/s = 46.9 kn. These reproduce area file 04's σ column and its 39.3 / 46.9 kn figures. *(Verified arithmetic on [S5] data.)* Invariants: σ ∝ V⁻² at fixed depth; each metre of seawater adds ≈ 10.06 kPa to p_∞ (≈ +9.6 % of p_∞ − p_v at 0.5 m); V_crit ∝ (−Cp_min)^(−½), so halving −Cp_min raises V_crit by √2. Edge cases: −Cp_min ≤ 0 → screening Undefined (no suction; do not print ∞); missing p_v → Unavailable (ANA-15); Cp_min from fewer than the polar's full station set → "screening estimate, unresolved suction peak" (ANA-02).

#### Free-surface image system (sign conventions)

Let a bound vortex of circulation Γ sit at depth h below z = 0, the foil advancing at V. Linearised free-surface condition on z = 0: φ_tt + g φ_z = 0 in the earth frame, i.e. V² φ_xx + g φ_z = 0 for steady advance. The two limits:

| Limit | Boundary condition on z = 0 | Image at (x, +h) | Effect on 2D lift | Label |
|---|---|---|---|---|
| Fr_h = V/√(g h) → 0 (gravity dominant) | φ_z = 0 (rigid wall) | **opposite** sign, −Γ | lift increases (ground effect) | Inferred (from the b.c.) |
| Fr_h → ∞ (gravity negligible) | φ = 0 (constant pressure) | **same** sign, +Γ — Faltinsen's "negative image" of the potential | lift decreases; L/L_∞ → ½ as h/c → 0 (planing-plate limit C_l = πα) | Inferred; the closed form L/L_∞ = (1 + 16 (h/c)²)/(2 + 16 (h/c)²) is Flagged [S20] |

Practical rule: for the goal-state foil Fr_h ≈ 2.6 (10 kn, h 0.4 m) to 8.3 (32 kn) — the high-Fr_h limit is the right *sign* (lift loss), and the JMSA 2026 fit is the only wingfoil-scale data: C(α, h/c) = C(α, ∞) [1 − a exp(−(h/c)ⁿ)] with (a, n) = (0.45, 0.70) for lift, (0.50, 0.40) for drag, (0.45, 0.90) for moment [S6] — constants re-read from the paper at the gate; a first draft had transcribed a single a = 0.50 and n = 0.75 for lift, which is wrong. Re-evaluated with the paper's constants: lift factor 0.757 at h/c 0.5, 0.834 at 1, 0.911 at 2, 0.948 at 3, 0.968 at 4, 0.979 at 5, 0.994 at 8; drag factor 0.894 at h/c 3, 0.925 at 5. **The fit is depth-only**: it carries no Froude dependence, whereas the same paper measures ≈17 % lift loss between Fr_h 2 and 5 at fixed h/c = 4 (a Froude delta the fit cannot represent) — so any layer built on it must print Fr_h beside h/c with "Froude dependence not modelled" (the fit says drag *falls* near the surface in their data — Verified fit, physical interpretation Flagged). Validity envelope of the fit: NACA 63-210 wing (AR 6.8 by b²/S; 10.9 by the paper's S/c² convention), h/c 0.5–9.5, α −5° to +10°, Re 7.3×10⁴–2.9×10⁵, Fr_h ≲ 5; goal-state h/c 3.3–6.3 (h 0.3–0.5 m, c̄ 0.079–0.091 m per area file 04) lies inside the h/c range but outside the Re range and at the top of the Fr_h range — label "empirical correction, model-scale Re" (ANA-04/13). *(Verified [S6]; arithmetic this session.)* Invariant: never apply the correction with h ≤ 0 (surface-piercing → Unavailable, ventilation flag only); never apply it without Fr_h shown beside h/c.

#### Finite wing — lifting line, Helmbold, induced drag

| Quantity | Formula | Validity | Label |
|---|---|---|---|
| Aspect ratio | AR = b²/S (S = projected planform area; derive, never store) | — | Verified (repo rule; spec A3) |
| Induced angle, elliptic | α_i = C_L / (π AR) [rad] | AR ≳ 4, unswept, attached, linear section lift | Inferred [S38] |
| Lift slope, Prandtl | C_Lα = a₀ / (1 + a₀/(π AR)); with a₀ = 2π → 2π AR/(AR + 2) [/rad] | AR ≳ 4 | Inferred [S38] |
| General planform | C_Lα = a₀ / (1 + (a₀/(π AR))(1 + τ)); C_Di = C_L²/(π AR) (1 + δ); e = 1/(1 + δ) | τ, δ ≥ 0 from the Fourier-series solution | Inferred |
| Induced drag | C_Di = C_L² / (π e AR); D_i = W² / (π e q b²) at L = W | e ≤ 1 for a planar wing; e is *computed* from the loading (ANA-04), never entered | Inferred [S38] |
| Helmbold | C_Lα = 2π AR / (2 + √(AR² + 4)) = 2π / (√(1 + (2/AR)²) + 2/AR) [/rad] | AR ≲ 4 (ESDU: within ±2 % for βA ≥ 1.5); converges to Prandtl for large AR | Inferred [S27]; ESDU statement Flagged |
| Values | AR 2: Helmbold 2.60, Prandtl 3.14; AR 4: 3.88 vs 4.19; AR 8: 4.91 vs 5.03 (2.4 %); AR 12: 5.32 vs 5.39 (1.2 %) | — | Verified arithmetic |
| (L/D)_max, parabolic polar | C_D = C_D0 + C_L²/(π e AR) → (L/D)_max = ½ √(π e AR / C_D0) at C_L* = √(π e AR C_D0) | wing-only, no cavitation, no free surface | Inferred [S38] |
| Values | AR 8, e 0.9, C_D0 0.012: C_L* 0.52, (L/D)_max 21.7; AR 12: 0.64, 26.6; AR 13, e 0.95, C_D0 0.008: 0.54, 34.8; AR 8, e 0.85, C_D0 0.015: 0.57, 18.9 | — | Verified arithmetic |
| Downwash at a tail, far field | ε ≈ 2 C_L / (π AR e) [rad], many spans downstream | **not** valid at a 0.6–0.8 m fuselage: use the lattice-evaluated induced velocity | Inferred; direction of error Flagged |
| Strip theory | α_eff(y) = α_geo + θ_twist(y) − α_i(y), α_i = w(y)/V; Re(y) = V c(y)/ν; with sweep Λ: V_n = V cos Λ, Re_n = Re cos Λ, C_l,n = C_l/cos²Λ (simple sweep theory) | away from tips and stall; section polar evaluated at (Re(y), Ncrit, α_eff) | Inferred [S9] |

#### Vortex lattice — placement, Trefftz plane, near-field forces

- **Lattice:** bound vortex (or ring leading edge) at the panel quarter-chord, control point at the panel three-quarter-chord, no-penetration (V + v_induced)·n = 0 at each control point; horseshoe legs or ring wake aligned with the freestream (or the x-axis) to a far downstream distance ≥ 20 spans. *(Inferred; [S15] not rendered this session — Flagged citation.)*
- **Lift (near field):** F = ρ (V_∞ + v_ind) × Γ ℓ per bound segment (Kutta–Joukowski); sum → L, and the moment about the named datum (ANA-11). *(Inferred.)*
- **Induced drag (Trefftz plane):** D_i = −(ρ/2) ∫_{−b/2}^{b/2} Γ(y) w_T(y) dy, w_T the downwash induced in the Trefftz plane by the trailing wake alone (twice the lifting-line value); then e = C_L² / (π AR C_Di). The near-field Kutta–Joukowski drag is panel-sensitive; the workbench reports the Trefftz value and shows both when they differ by > 5 % (a lattice-quality signal). *(Inferred.)*
- **Convergence:** results reported with the lattice (n_span × n_chord) and a refinement pair; a fixture passes only if the refined answer moves by < 1 % in C_L and < 3 % in C_Di. *(Inferred — rule proposed here.)*
- **Symmetry invariant:** a symmetric planform at β = 0 must give zero side force, zero rolling and yawing moment to solver tolerance; failure is a lattice-indexing defect (ANA-13). *(Inferred.)*

#### Unsteady 2D basics (for the later pumping slice; not a v1 tier)

- Reduced frequency k = ω b / V with b = c/2 (so k = ω c / (2V)); Theodorsen's function C(k) = H₁⁽²⁾(k) / (H₁⁽²⁾(k) + i H₀⁽²⁾(k)) with C(0) = 1 and C(k) → 0.5 as k → ∞. Lift of a flat plate in pitch α about the axis at x = a b (a from −1 to 1) and plunge h (positive down): L = π ρ b² (ḧ + V α̇ − b a α̈) + 2π ρ V b C(k) [ḣ + V α + b (½ − a) α̇]. First bracket = non-circulatory (added-mass) term, second = circulatory. *(Existence Verified [S22]; the expression is recalled from Bisplinghoff/Fung and is Flagged until the NTRS PDF is read.)*
- Added mass per unit span of a flat plate in heave: m_a = ρ π b² = ρ π (c/2)² [kg/m]. *(Flagged — standard, not opened.)*
- Garrick: mean thrust and propulsive efficiency of a flapping/oscillating plate in terms of C(k) = F + iG; efficiency → 1 as k → 0 for pure heave. *(Existence Verified [S23]; formula Flagged.)*
- Sears: lift response to a sinusoidal transverse gust, S(k). *(Existence Verified [S24].)*
- Validity of all three: 2D, inviscid, small amplitude, planar wake, infinite depth, no cavitation. Unsteady lift near a free surface at finite Fr_h is outside every source opened here.

#### Verification fixtures the implementation must pass

| Fixture | Input | Expected | Tolerance | Label |
|---|---|---|---|---|
| ANA-17 arithmetic | ρ 1000, V 8 m/s, S 0.14 m², C_L 0.6, C_D 0.035 | L = 2688 N, D = 156.8 N, C_L/C_D = 17.142857 | exact to display precision | Verified (spec ANA-17) |
| Force unit | 4.4482216152605 N | 1 lbf; line loads convert with the length factor | exact | Verified (spec ANA-18) |
| Cavitation arithmetic | seawater 15 °C, h 0.5 m, −Cp_min 0.5 | V_crit = 20.20 m/s = 39.3 kn; σ(10 kn) = 7.71 | 0.1 % | Verified arithmetic on [S5] |
| 2D panel vs analytic | Kármán–Trefftz section (finite TE angle) or Joukowski (cusped), α 4° | surface velocity and C_l equal the conformal-map solution | C_l within 0.5 % at 200 panels; Cp_min converging monotonically 100 → 200 → 400 panels | Inferred (analytic solutions standard; oracle to be scripted) |
| 2D flat plate in the VLM | AR → ∞ (e.g. AR 1000, 1 chordwise panel) | C_Lα → 2π/rad | 1 % | Inferred |
| Elliptic planar wing | untwisted, AR 8, α 5°, no camber | lifting line: C_Lα 5.03/rad → C_L 0.439, C_Di 0.00767, e = 1.000; Helmbold 4.91/rad → C_L 0.428 | VLM C_L inside [0.42, 0.44]; e ≥ 0.98; e > 1.005 is a defect | Verified arithmetic; theory Inferred [S38][S27] |
| Rectangular wing AR 5 (Bertin–Smith class) | λ = 1, untwisted; unswept and Λ = 45° | unswept: Helmbold 4.25/rad, Prandtl 4.49/rad; Λ 45°: the Bertin & Smith hand-VLM example gives ≈ 3.44/rad | VLM inside the Helmbold–Prandtl band (unswept); swept value Flagged | Flagged (Bertin & Smith not opened; number recalled) |
| Warren-12 | AR 2.83, taper 0.5, LE sweep 53.54°, S_ref 2.83, c_ref 1.0 | C_Lα ≈ 2.743/rad, C_Mα ≈ −3.10/rad about the reference point Mason specifies | ±2 % | Flagged [S15] — PDF not rendered; confirm before the fixture is admitted |
| Twist sign | linear washout −3° at the tip | tip strips show α_eff lower than root by ≈ 3° minus the induced change; C_L falls vs the untwisted case | sign only | Inferred |
| Anhedral/dihedral | ±10° anhedral, same planform | C_L drops ≈ cos²(10°) = 0.970 of the planar value at fixed α (projected-area convention stated) | 1 % | Inferred (geometry) |
| Tandem interaction | front wing + stabilizer, arm 0.7 m | stabilizer α_eff < its geometric incidence by the lattice-evaluated downwash; the far-field 2 C_L/(π AR e) over-predicts ε | sign and ordering only | Inferred; magnitude Flagged |
| NeuralFoil vs XFOIL gold master | NACA 0012, Re 10⁶, Ncrit 9, α 0–8° | C_l within 0.02, ln C_d within 0.03 of a process-invoked XFOIL run | per [S3] | Verified bounds [S3]; run is a spike task |

Invariants across all tiers: L ∝ V² at fixed α when coefficients are Re-independent; C_L(α) monotone in the supported bracket (a non-monotone estimator output means the bracket crossed the polar's converged range → "no supported solution", ANA-05/12); e ≤ 1 for planar wings; C_Di ≥ 0; Total drag = Σ named components to the solver tolerance (ANA-11); a section result is N/m and a wing result is N — the two are never added (ANA-03).

## Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

*Source: [08-simulation-openfoam-su2-interop.md](08-simulation-openfoam-su2-interop.md) (`kb-hw-simulation-openfoam-su2-interop`).*

**First-cell height (ITTC 7.5-03-02-03 Eq. 7 and Eq. 10).** For a target `y+` on a body of reference length
`L` [m] at `Re_L = V·L/ν`:

```
Cf  = 0.455 / [ln(Re_L)]^2.58                      (turbulent, smooth; transitional: subtract 1700/Re_L; laminar: 1.328/sqrt(Re_L))
y   = y+ · L / ( Re_L · sqrt(Cf/2) )               [m]   — distance of the first grid point from the wall
```
Validity: flat-plate zero-pressure-gradient estimate; ignores curvature and pressure gradient, so the
achieved y+ must be checked a posteriori and the mesh corrected. *(Verified, [S33])* Worked example
(Inferred arithmetic on the verified formula): chord L = 0.20 m, V = 8 m/s, seawater 15 °C ν ≈ 1.19e-6 m²/s
→ Re ≈ 1.34e6; Cf ≈ 4.9e-4; y(y+=1) ≈ 9.5 µm; y(y+=30) ≈ 0.29 mm; y(y+=50) ≈ 0.48 mm. With expansion 1.2 and
20 layers the wall-resolved stack is ≈ 1.8 mm thick, i.e. ≈ 0.9 % chord. *(Inferred; ν value from the
grounding doc's ITTC water tables, not re-fetched.)*

**Mesh floor (ITTC Table 1).** Wall-resolved: y+ ≤ 1, expansion 1.2, ≥ 20 points in BL. Wall functions: 30 <
y+ < 100, expansion 1.2, ≥ 15 points. Domain: inlet ≥ 10 L, outlet ≥ 20 L (lifting), other boundaries ≥ L.
Structured 3×3 determinant > 0.3 (locally ≥ 0.15). *(Verified, [S33])*

**Grid-convergence (ITTC 7.5-03-01-01 §3).** Three grids with refinement ratio r, solutions S1 (fine), S2,
S3; ε21 = S2 − S1, ε32 = S3 − S2; convergence ratio R = ε21/ε32 (monotonic 0 < R < 1, oscillatory R < 0,
divergent R > 1 — classification per the procedure's §2; only the monotonic case admits RE):
```
p     = ln(ε32/ε21) / ln(r)                        observed order
δ_RE  = ε21 / (r^p − 1)                            error estimate (Eq. 5)
U_FS  = F_S · |δ_RE|                               grid uncertainty (Eq. 6)
P     = p_RE / p_th ;  F_S(P) = 2.45 − 0.85 P (0 < P ≤ 1) ;  16.4 P − 14.8 (P > 1)     (Xing & Stern, Eqs. 7–8)
```
Least-squares (Eça & Hoekstra) on n_g ≥ 4 grids: fit φ_i = φ0 + α h_i^p; δ_RE = α h_i^p; data range Δφ =
(φ_max − φ_min)/(n_g − 1); if σ < Δφ then U(φ_i) = F_S ε_φ + σ + |φ_i − φ_fit| with F_S = 1.25 for 0.5 ≤ p <
2.1, else U = 3(σ/Δφ)(ε_φ + σ + |φ_i − φ_fit|); ε_φ chosen by p (δ_RE for 0.5 ≤ p ≤ 2; the smaller-σ of δ1 =
αh, δ2 = αh² for p > 2; smallest σ among δ1, δ2, δ12 for p < 0.5). Validation: |E| = |D − S| compared with
U_V = sqrt(U_SN² + U_D²). *(Verified, [S32]; the R classification thresholds are the procedure's standard
statement — Inferred wording.)*

**Convergence criterion.** Scaled residuals dropped ≥ 3 orders of magnitude from initial; otherwise
force/moment convergence and key-region quantities monitored; for implicit transient runs convergence must
be met at every time step. *(Verified, [S33])* SU2 expresses the residual target as `CONV_RESIDUAL_MINVAL`
in log10 (template example −8) and force convergence as a Cauchy series on `CONV_FIELD` over
`CONV_CAUCHY_ELEMS` with tolerance `CONV_CAUCHY_EPS`. *(Verified, [S23])*

**Coefficient conventions.** OpenFOAM `forceCoeffs`: Cd/Cl/Cm from `magUInf`, `lRef`, `Aref`, `rhoInf`, axes
`e1`(drag)/`e3`(lift), `pitchAxis`; front/rear split Cd{f,r} = 0.5 Cd ± CmRoll. SU2: `REF_AREA` (0 =
automatic — never allow 0 in a workbench-generated case), `REF_LENGTH`, `REF_ORIGIN_MOMENT_*`, coefficients
on `MARKER_MONITORING`. *(Verified, [S14][S23])* Invariant (spec A4): the harvested dimensional force must
reconcile with the coefficient × ½ρV²·S using the run's pinned ρ, V and S to the declared tolerance; a
mismatch is a provenance defect, not a rounding note.

**Free-stream turbulence inputs.** SU2 defaults: TI = 5 %, μ_t/μ = 10, `FREESTREAM_NU_FACTOR` = 3.
*(Verified, [S20])* For a transition-model run these are physics inputs; the workbench must set and record
them and must not inherit the template defaults. *(Inferred.)*

**Invariants that must always hold.** (1) A case is never solved on a mesh that failed the quality gate
(spec CFD-02). (2) A result is "Converged" only if the declared criterion fired *and* the artefact oracle
passed; "Single mesh" until an ITTC-style grid study exists. (3) Every coefficient carries its reference
set. (4) Free surface, cavitation, transient physics are unsupported on SU2 and must be reported
`Unsupported` at *queue* time, not at run time (sweep sample lifecycle). (5) The typed case model
round-trips: serialise → parse → equal, and never emits `#codeStream`/`#calc`. (6) Version pins are
digests/hashes, never floating tags.

## Optimization strategies for 2D foil sections and 3D hydrofoil wings

*Source: [09-optimization-strategies.md](09-optimization-strategies.md) (`kb-hw-optimization-strategies`).*

**Multipoint objective (Garg form) [S2].** C̄_D = Σ_k w_k C_D,k with w_k = p(CL_k)·ΔCL, Σ w_k = 1; η̄ = Σ_k w_k CL_k/CD_k. Valid for steady points; weights must be recorded with their basis (PDF from data, or "engineering experience").

**Smoothed cavitation-area constraint [S2].** χ = 1/(1+e^{2k(Cp+σ)}), A_cav = (1/A_ref)∬_A χ dA ≤ 5×10⁻⁴, k = 10. σ = (P_ref − P_vap)/(½ρV²), P_ref = P_atm + ρ g h. Inception-speed read-out V_cav = √((P_ref − P_vap)/(½ρ·(−Cp_min))) (Garg: 10.6 → 15.4 m/s at h = 1 m for the single-point optimum). Valid for attached, steady, subcavitating flow; a screening (2D, Cp_min) version is what the workbench's ANA tier already computes [S48].

**Lift constraint per point.** CL_k(α_k, δ_k) = L_k/(q_k S), q_k = ½ρV_k², α_k (and flap δ_k where present) are design variables [S2][S4][S8].

**KS aggregation [S2].** KS(g) = g_max + (1/ρ_KS) ln Σ exp(ρ_KS (g_i − g_max)) ≥ max g_i — conservative, smooth; used for von Mises stress ≤ σ_f/1.1. (Form from the cited Wrenn 1989 / Poon & Martins 2007 references; exact ρ_KS used by Garg not extracted — Flagged.)

**Regularity [S8].** local_thickness(x/c) > 0 ∀ x; thickness(0.33) ≥ 0.128, thickness(0.90) ≥ 0.014, TE angle ≥ 6.03° (DAE-11 case values — example, not a hydrofoil rule); wiggliness w = ∫(curve second derivative)² type measure (Wahba); `analysis_confidence > 0.90`.

**Minimum induced drag under a moment constraint (spike, Verified by execution).** Lifting-line, Γ(θ) = 2bV Σ_n A_n sin nθ, symmetric loading ⇒ odd n only; C_L = πAR·A_1, C_Di = πAR·Σ n A_n². With lift fixed and span free: fixed *integrated* BM ⇒ A_3/A_1 = −1/3, Γ ∝ sin³θ = (1−x²)^{3/2}, b/b_ell = √1.5 = 1.2247, D_i/D_i,ell = 0.8889. Fixed *root* BM with modes {1,3} ⇒ b ×1.25, D_i ×0.853; with {1,3,5,7,9} ⇒ b ×1.32, D_i ×0.844 (non-negative-load constraint active). Without the non-negativity constraint the problem is unbounded (span → ∞, negative tip load cancels the moment). Jones's published 15 %/15 % [S15] is not reproduced by this family — Flagged; the analytic form in TN 2249 should be transcribed before this is used as a fixture.

**Goal-state operating points (re-executed; inputs from [S23], Flagged where [S23] flags them).** Seawater 15 °C, ρ = 1026.02 kg/m³, ν = 1.1892×10⁻⁶ m²/s, P_ref − P_vap = 104.69 kPa at h = 0.5 m, L = 1050 N.

| k | Wind | S cm² · b m | V kn | CL | Re (c̄) | σ | Critical metric (Garg pattern) | Weight w_k (Inferred assumption) |
|---|---|---|---|---|---|---|---|---|
| 1 | 10 | 1000 · 1.10 | 10 | 0.773 | 3.9×10⁵ | 7.7 | take-off: CL ≤ CL_max(Re, Ncrit 2/4) − margin; laminar-bubble drag | 0.05 |
| 2 | 10 | 1000 · 1.10 | 14 | 0.395 | 5.5×10⁵ | 3.9 | drag (upwind) | 0.20 |
| 3 | 10 | 1000 · 1.10 | 18 | 0.239 | 7.1×10⁵ | 2.4 | drag (downwind) | 0.15 |
| 4 | 20 | 750 · 0.95 | 15 | 0.458 | 5.1×10⁵ | 3.4 | post-take-off drag; bubble | 0.05 |
| 5 | 20 | 750 · 0.95 | 22 | 0.213 | 7.5×10⁵ | 1.6 | drag (upwind) | 0.25 |
| 6 | 20 | 750 · 0.95 | 28 | 0.132 | 9.6×10⁵ | 0.98 | drag (downwind); suction-side −Cp_min ≤ 0.98 − margin | 0.20 |
| 7 | 20 | 750 · 0.95 | 32 | 0.101 | 1.1×10⁶ | 0.75 | cavitation: −Cp_min ≤ 0.75 − margin; pressure-side check | 0.10 |

Two wings (10 kn and 20 kn) are two design vectors sharing sections and a family; the GWA cap of 5 wings/season makes "one all-wind foil" a legitimate alternative formulation (points 1–7 on one 850 cm² wing) [S23]. Weights are a time-on-task guess, to be replaced by GPS race data (open question in [S23]).

**Cost-tier constants (evaluation wall time).** NeuralFoil `xxxlarge` ~6 ms (README benchmark table [S7]; column interpretation Inferred); XFOIL 0.1–1 s per converged point (Flagged); 2D RANS minutes, 3D RANS hours (Flagged, general practice). Book scaling: 67 → 206 (gradient, exact derivatives) vs 103 → 32,000 (gradient-free) function calls from 1 to 30 variables [S6].

**Invariants.** (1) No candidate is "certified" on estimator or surrogate evidence (COMMIT-01). (2) Every evaluation record carries tier, method version, Ncrit, water record, converged flag, confidence — a missing field makes the evaluation inadmissible for ranking. (3) The design vector is a function of the record's controls; the inverse map (candidate → record) is the identity, never a fit. (4) Every optimizer loop has a termination variant (max evaluations, KKT tolerance, no-improvement window) and its firing is recorded as the termination reason. (5) A run with any non-computable evaluation must show the count and the classifier's exclusion region. (6) The weights Σ w_k = 1 and their basis are stored with the goal state. (7) σ, Re, h/c, Fr_h are recomputed per operating point from the water record — never carried as constants.

## Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

*Source: [10-integration-and-ai-workflows.md](10-integration-and-ai-workflows.md) (`kb-hw-integration-and-ai-workflows`).*

**Cache / identity key (Inferred design; hash algorithm Verified [S41]).**

```
run_key = BLAKE3( canon(surface_revision) ‖ canon(profile_revisions[]) ‖ fluid_property_revision_id
                 ‖ canon(operating_point_SI) ‖ canon(reference_quantities) ‖ method_id ‖ method_version
                 ‖ backend_build_digest ‖ canon(settings) )
```
`canon()` is a canonical serialization (sorted keys, fixed float formatting, SI units, no display state). Invariants:
(i) re-serializing an unchanged document yields the same key; (ii) changing display units or file path does not change
the key (A7 Compatibility); (iii) changing method version changes the key even when inputs are identical (Snakemake
`code`/`software-env` triggers [S48]); (iv) a result is *Current* iff `run_key(result) == run_key(current document)`
for its tier, else *Historical* (ANA-07).

**Recompute policy per tier (Inferred; latency floors from spec A7 [S51]).**

| Tier | Trigger | Latency target | Cancellation |
|---|---|---|---|
| DRC validators, estimator | Every edit, synchronous | ≤100 ms editing feedback p95 | n/a |
| 2D polars (catalog lookup / NeuralFoil sidecar) | Debounced after edit settles (order 200–500 ms; value is a design choice, not evidence) | ≤250 ms preview regeneration when cached | Cancel superseded query (salsa semantics [S40]) |
| VLM + strip theory | Debounced; may run on a worker | seconds | Cancel superseded run |
| CFD (SU2 / OpenFOAM process) | **Never automatic**; explicit Run creates an Analysis run and queues it | minutes–hours | Graceful then forceful kill (CliWrap [S45]); ack ≤250 ms; shutdown ≤5 s (A7) |
| Sweeps | Explicit; embarrassingly parallel over (V, α) pairs; priority = user-selected sample first | — | Per-sample cancel; retained attempt provenance (A3 Sweep) |

**Multi-fidelity correction forms (Inferred from [S15][S16][S17][S18]; model form from recall).**

- Additive discrepancy at a verified point `x*`: `δ_q = q_hi(x*) − q_lo(x*)`; corrected low-fidelity `q̃_lo(x) =
  q_lo(x) + δ_q`, valid only in a declared neighbourhood (same Surface revision, same Re decade, same tier pair, same
  fluid).
- Multiplicative: `ρ_q = q_hi(x*)/q_lo(x*)`, `q̃_lo = ρ_q · q_lo`; prefer for positive quantities (C_D, L/D); never
  for quantities that cross zero (C_M, C_L near α₀).
- Kennedy–O'Hagan autoregressive form: `q_hi(x) = ρ · q_lo(x) + δ(x)`, `δ ~ GP(0, k)`, giving a posterior variance;
  requires ≥ several high-fidelity points — not a v1 capability.
- First-order consistency (AMMO): a corrected surrogate `f̃` used to steer a step must satisfy `f̃(x_k) = f(x_k)` and
  `∇f̃(x_k) = ∇f(x_k)`; acceptance ratio `r_k = [f(x_k) − f(x_k + s_k)] / [f̃(x_k) − f̃(x_k + s_k)]` computed with the
  **high-fidelity** `f`, shrinking the trust region when `r_k` is small. With VLM/CFD gradients unavailable in v1,
  only the value condition can be checked — which is exactly "verify at higher tier before certify".

**NeuralFoil envelope constants (Verified [S1][S2]).** Inputs: 18 Kulfan parameters, α [deg], Re, n_crit, x_tr
(upper/lower). Training: Re median 296k, 2.5–97.5 % ≈ 1.87k–262M; n_crit ∈ [0, 18]; α observed −27.9°…+28.6°
(post-stall cases present but less accurate for C_M); M = 0. Test MAE (xxxlarge): C_L 0.012, ln C_D 0.020 (≈2 %
relative), C_M 0.002, x_tr 0.007. `analysis_confidence = σ(logit_converged − d_M²)` where `d_M` is the Mahalanobis
distance of the input to the training distribution (form as described in the paper text; exact constants not
extracted).

**LLM-agent benchmark figures (Verified [S26]–[S29]).** Foam-Agent: 88.2 % (110 basic) / 62.5 % (advanced OOD).
ChatCFD: 82.1 % execution / 68.12 % physical fidelity; 192.1k tokens and $0.208 per case. Harness study: 96.4 %
single-agent vs 88.2 % multi-agent; repair loop 71.8→96.4 %; tutorials 80.9→96.4 %. MetaOpenFOAM: 85 % on 8 tasks,
$0.22/case. These are tutorial-derived setups, not validated physics.

**Invariants that must always hold.**
1. No result is displayed as Current whose `run_key` differs from the current document's key for that tier.
2. A model-produced proposal has no effect on any aggregate until deterministic validation passes and the user
   accepts; a proposal created against a document that has since changed is blocked until re-diffed (AI-02).
3. Every numeral in assistant prose exists in the packed context; otherwise the reply is rejected (product rule;
   SPIKE-04).
4. Disagreement between tiers is stored as a discrepancy record, displayed as a finding, and never averaged (ANA-06).
5. A DRC *error* blocks Apply; a *warning* is visible on the revision and in the run manifest; an exclusion carries a
   reason and the revision at which it was made.
6. A surrogate's own confidence below threshold demotes its row to "explicitly unsupported observation" (A6) — it
   never promotes.

## Visualization for hydrofoil design and optimization

*Source: [11-visualization.md](11-visualization.md) (`kb-hw-visualization`).*

**Pressure coefficient.** Cp = (p − p∞)/(½ ρ V∞²) [–]; incompressible stagnation Cp = 1; suction Cp < 0. Valid for any incompressible steady point; in a RANS field p is the modeled mean static pressure (kinematic p/ρ in OpenFOAM incompressible solvers — the sidecar must multiply by ρ or use the kinematic definition consistently; *Flagged* recall of OpenFOAM's kinematic pressure convention). *(Verified definition [S2].)*

**Cavitation screen.** σ = (p_atm + ρ g h − p_v)/(½ ρ V²) [–]; inception when −Cp_min ≥ σ; V_crit = √(2 (p_atm + ρ g h − p_v)/(ρ (−Cp_min))) [m/s]. Values of ρ, p_v and the σ table by speed and depth are in file 04 (ITTC Rev03). Envelope: steady, deep-water, non-ventilated, Cp_min from a resolved peak — a coarse sample under-reads the peak and over-reads V_crit. *(Verified definition via file 04; envelope Inferred.)*

**Spanwise loading normalization.** Local lift per unit span l(y) = ½ ρ V² c(y) Cl(y) [N/m]; normalized loading Cl(y)·c(y)/c̄ with c̄ = S/b. For an elliptical distribution l(y) = l₀ √(1 − (2y/b)²) the total lift is L = l₀ π b/4, and with L = ½ ρ V² S CL and S = b c̄ the normalized elliptical reference is **(4/π) CL √(1 − (2y/b)²)**. Root bending moment of one half-wing M_root = ∫₀^{b/2} l(y) y dy [N·m]; for the elliptical case the half-wing lift L/2 acts at 4/(3π)·(b/2) ≈ 0.4244·(b/2), so M_root = 0.2122 L b/2 ≈ 0.106 L b. Envelope: symmetric flight, no dihedral projection correction (anhedral shortens the projected arm — the projected span rule in the grounding register applies). *(Algebra checked here; provenance of the standard result Flagged — no text opened.)*

**Streamline integration.** dx/ds = u(x)/|u(x)| (arc-length parameterization) or dx/dt = u(x); RK4 local error O(h⁵); RK45 adaptive to a tolerance. Step h in cell-length units 0.25–0.5 is the practical default (*Flagged*); termination by domain, length, step count (default in VTK 2000 — *Flagged*), or |u| < terminal speed. Vorticity ω = ∇ × u [1/s]. *(Semantics Verified [S15]; defaults Flagged.)*

**Vortex criteria** (∇u = S + Ω): Q = ½(‖Ω‖² − ‖S‖²) [1/s²] > 0; λ2(S² + Ω²) < 0 [1/s²]; Δ = (Q/3)³ + (R/2)² > 0 with R = −det(∇u) (incompressible, P = 0); λ_ci = Im of the complex eigenvalue pair of ∇u [1/s]. Envelope: interior field, velocity gradient resolved by the mesh; threshold-dependent; **no wall-separation meaning**. *(Jeong–Hussain Verified abstract [S36]; others citation-Verified, formulas Flagged recall.)*

**Wall diagnostics.** τ_w = μ (∂u_t/∂n)|_wall [Pa]; C_f = τ_w/(½ ρ V∞²) [–]; skin-friction lines integrate dx/ds = τ_w/|τ_w| on the wall; 2D reversal indicator C_f,x < 0; y+ = u_τ y/ν with u_τ = √(τ_w/ρ) [–]. Envelope: first-cell-centre y for y+; low-Re wall treatment vs wall functions changes the meaning of y+ bands (1–5 resolved; 30–300 wall-function) — the manifest records which. *(Standard definitions; Flagged for provenance, no text opened; separation topology per [S40][S41].)*

**LIC.** I(x₀) = ∫_{−L}^{L} k(s) · T(σ_{x₀}(s)) ds over the streamline σ through x₀, kernel k, noise texture T. *(Cabral–Leedom citation Verified [S31]; formula recall Flagged.)*

**Data-size model (VTK unstructured, hex-dominant).** bytes ≈ N·(24 points + 72 connectivity + 8 per scalar + 24 per vector) in double/64-bit ids; halve for float/32-bit. Surface patch ≈ F·(12 + 12 normals + 4 per float scalar + 12 per float vector) with F faces. Streamlines ≈ lines × points × 24 B (position + tangent, float). *(Inferred arithmetic on VTK's array model.)*

**Colour-map invariants.** Sequential maps are lightness-monotonic; a diverging map is used only when 0 (or another physical centre) lies inside the range and the centre is pinned to that value, not to the range midpoint; the legend names the map and its version; range basis is explicit; CVD-safe and grayscale-readable by construction (Crameri/cividis) [S3][S5][S8]. Invariant: **the same field in chart, flood and table shows the same numbers** (derive-don't-store: one Cp array, three representations). *(Verified rules; invariant Inferred.)*

**Replay invariants.** frame ↔ exactly one admitted sample; no field between samples; camera/slice/seeds/range fixed per compatible series unless unlocked; a failed sample clears field and metrics; physical time only with recorded timesteps. *(Verified spec [S51].)*

**Constants in this file.** CVD prevalence ≈ 8 % men, 0.5 % women [S5]; Crameri visual error of rainbow "> 7 %" [S5]; Kovesi flat spot "one tenth of the total data range" [S7]; ParaView SPDX from 5.12.0 [S17]; OpenFOAM v12 reader module targets ParaView 5.10.1 [S21].

## Structures, materials, manufacturing and safety for water-sports hydrofoils

*Source: [12-structures-materials-and-manufacturing.md](12-structures-materials-and-manufacturing.md) (`kb-hw-structures-materials-and-manufacturing`).*

Unless stated otherwise: SI units; y is the spanwise coordinate from the root (m), b the full span (m), c(y) the local chord (m), h(y) the local maximum thickness (m), q = ½ρU² the dynamic pressure (Pa), ρ_w = 1025 kg/m³ seawater [S1]. Labels: **V** Verified this session, **I** Inferred, **F** Flagged (recall or single/weak source).

#### 1. From strip loads to internal loads (per half-wing, cantilever at the root)

The VLM/strip tier already produces, per strip, the sectional lift l(y) [N/m], drag d(y) [N/m] and quarter-chord pitching moment m_c/4(y) [N·m/m] (spec ANA-11 exposes the centre of lift and the root bending moment). The beam needs these resolved onto the **elastic axis** at chordwise position x_ea(y):

- Distributed transverse load (normal to the chord plane): w(y) = l(y)·cos α + d(y)·sin α ≈ l(y) for small α. **I**
- Distributed torque about the elastic axis, nose-up positive: t(y) = m_c/4(y) + l(y)·(x_ea(y) − x_c/4(y)) + [any offset of the drag line]. **I**
- Shear force: V(y) = ∫_y^{b/2} w(η) dη. Bending moment: M(y) = ∫_y^{b/2} w(η)(η − y) dη. Torque: T(y) = ∫_y^{b/2} t(η) dη. All vanish at the tip; the root values are the mount loads. **I** (statics)
- **Elliptic check values.** For an elliptic lift distribution the half-wing lift L/2 acts at ȳ = 4/(3π)·(b/2) = 0.4244·(b/2) from the root, so M_root = (L/2)·0.4244·(b/2) = **0.1061·L·b**. *(Verified by execution — centroid computed in the spike below; the elliptic result is textbook)* For the goal case (90 kg rider + ≈12 kg board/foil ≈ 1000 N, front wing carrying ≈1050 N with a down-loaded stabiliser, b = 1.0 m): V_root ≈ 525 N, M_root ≈ **111 N·m** steady. **I** (system mass and stabiliser share are assumptions — mark them `assume:` in any fixture)
- **Invariant:** Σ over strips of l(y)·Δy must reconcile with the reported total lift to the solver tolerance before the beam consumes it (ANA-11 already requires drag-component reconciliation; extend to lift and moment). **I**

#### 2. Load cases and load factors

| Case | Lift multiplier n (× steady weight) | Basis | Label |
|---|---|---|---|
| Steady cruise | 1.0 | statics | **I** |
| Take-off at CL_max | 1.0–1.3 (rider pumps board up; speed lowest, CL highest, moment arm shifts outboard as tip loads rise) | practitioner reasoning | **F** |
| Pumping (surge / dock-start) | ≈2–3 peak, at 0.8–1.5 Hz; cycle count = frequency × session time (1 Hz × 30 min ≈ 1.8×10³ cycles/session; 10⁵ cycles ≈ 55 h of pumping) | no measured hydrofoil load-factor dataset was reachable; the 10⁵-cycle fatigue programme of [S9] is the only cited cycle count | **F** for n; **V** for [S9] |
| Breach / re-entry slam | impulsive; water-entry load on the wing at the fall speed; magnitude not established | — | **F** |
| Ventilation shock | lift "would suddenly drop by about half" on sudden ventilation [S1 citing Young et al. 2017] — a load *reversal* case for the rider, and an asymmetric torsion case for a partially ventilated wing | [S1] | **V** (wording) / **I** (torsion case) |
| Grounding / impact | low-velocity impact; layup bias to 30° did not change damage, preload increased it 8 % [S26] | [S26] | **V** |

**Safety factors.** No hydrofoil-specific normative factor was found. ISO 12215-9:2026 provides "design stresses" for sailing-craft appendages (keel/centreboard) [S12] — text not opened; DNV-ST-C501 (composite components) uses partial safety factors on load and resistance with knock-downs for fatigue, moisture and temperature — **F** (recall, not opened). Aerospace practice (1.5 ultimate on limit load; composites with additional environmental and damage knock-downs) is the usual proxy in sailing-foil design — **F**. The tool must therefore expose the factor as an *input with provenance*, never a built-in constant (fail-closed on missing science).

#### 3. Beam formulas with validity envelopes (standard content of Timoshenko & Gere / Megson; pages not opened — **F** until checked, but the forms are elementary)

- **Euler–Bernoulli bending with variable stiffness:** d²/dy² [EI(y)·d²w/dy²] = q(y). Curvature κ = M/(EI). For the cantilever, integrate twice with w(0) = w′(0) = 0. Valid when the span-to-depth ratio of the beam (semispan / section thickness, ≈ 500 mm / 9 mm ≈ 55 for the goal case) is large (> ≈10) and rotations are small (tip slope ≲ 0.1 rad). Deflection scales as L⁴/(EI) for distributed load — the reason semispan is the most expensive planform variable structurally. **F** (form) / **I** (envelope)
- **Shear (Timoshenko) correction:** add w_s(y) = ∫ V(y)/(κ_s G A(y)) dy, with κ_s ≈ 0.85–0.9 for thin-walled foil sections (**F**). Matters for short, thick struts (mast chord ≈110–140 mm, length 0.6–1.0 m → L/h ≈ 40–60 still long) and for stub fuselage joints, not for the wing. The 9-DOF beam of [S1] includes warping; a Timoshenko element is the minimum for the mast. **I**
- **Torsion of a closed thin-walled single-cell section (Bredt–Batho):** J = 4A_m² / ∮(ds/t_s), twist rate dθ/dy = T/(GJ), shear flow q_s = T/(2A_m); A_m the area enclosed by the skin midline, t_s the skin thickness, G the in-plane shear modulus of the skin laminate (a ±45° skin gives G12 ≈ 30–47 GPa for carbon vs 5 GPa for 0°/90°, table in §5). Valid for thin skins (t_s ≪ h) and a closed cell; an *open* trailing edge (unbonded TE) destroys it — J collapses to Σ(b t³/3) — which is why TE bond failure is a torsional failure mode. For a monolithic solid section use the Saint-Venant J from the section polygon (numerical). **F** (forms) / **I** (consequences)
- **Section properties from the section polygon (what the tool computes, exactly):** A = ½Σ(x_i z_{i+1} − x_{i+1} z_i), I_xx = (1/12)Σ(x_i z_{i+1} − x_{i+1} z_i)(z_i² + z_i z_{i+1} + z_{i+1}²) about the centroid, per material region; for a laminate treat each ply region with its own E via the transformed-section (E-weighted) method, ΣE_k I_k. **F** (Green's-theorem polygon formulas from recall; trivially unit-testable against a rectangle)
- **Spike result (this session, `section_inertia.py` in the session scratchpad, NACA 4-digit thickness form at 10 %, 20 000 strips):** I_solid = **0.0394·c·h³** for a monolithic section (a rectangle would be 0.0833); I_skin = **0.276·t_s·c·h²** for a thin uniform skin (arc length 2.03 c; a rectangular box would be 0.5); section area 0.0685 c². Worked magnitude: c = 90 mm, h = 9 mm, E = 70 GPa (a woven/quasi-isotropic laminate), uniform 525 N over a 0.5 m semispan: monolithic I = 2586 mm⁴, EI = 181 N·m², tip deflection 45 mm = **9.1 % of semispan**; a 1.2 mm skin over an inert core gives I = 2418 mm⁴, 48 mm. Sensitivity at fixed chord: EI_mono ∝ (t/c)³ → 8 % t/c has 0.51× the stiffness of 10 %, 12 % has 1.73×; EI_skin ∝ (t/c)² → 0.64× and 1.44×. *(Verified by execution for the constants; the worked case uses assumed E, uniform chord and uniform load and is an order-of-magnitude illustration only)* — The illustration lands exactly on the "about 8 % of semispan" that Ng et al. flag as a structural-failure concern [S1], which is why production race wings use unidirectional HM carbon spars (E1 135–175 GPa, [S15]) aligned spanwise and stay at ≈10 % root t/c: the tool's thickness floor is a stiffness floor.

#### 4. Composite beam stiffness and bend-twist coupling

- **Coupled constitutive law (beam level):** [M; T] = [[EI, K]; [K, GJ]]·[κ; θ′], with K the bend-twist coupling stiffness (N·m²). K ≠ 0 whenever the laminate has unbalanced off-axis plies (e.g. +15° UD not mirrored by −15°). The dimensionless coupling ψ = K/√(EI·GJ) lies in (−1, 1); |ψ| ≈ 0.2–0.4 is what tailored foils and wind-turbine blades achieve. **F** (form standard; ψ range recall)
- **Sign:** positive fibre angle = toward the leading edge → K such that positive (upward) bending produces nose-down twist → wash-out → lower CL and later divergence [S1][S2][S3]. **V** (sources) — the tool must store the angle *and* its convention.
- **Laminate to beam (what a section-analysis step must do):** classical lamination theory gives the ABD matrix per ply stack (Q̄ transformed by ply angle; A = Σ Q̄_k t_k; B = ½ Σ Q̄_k (z_k² − z_{k−1}²); D = ⅓ Σ Q̄_k (z_k³ − z_{k−1}³)); integrate the skin's in-plane and coupling stiffness around the section contour to obtain EI, GJ and K (the route Faye et al. call "section analysis" [S7]). **F** (CLT formulas from recall; Jones / Daniel & Ishai are the references to check against)
- **Effective incidence:** α_eff(y) = α_geo(y) + θ(y); Young et al. show all foils collapse against α_geo + θ_tip,generalised [S2]. **V**
- **Static aeroelastic iteration:** solve K_s u = f(u); Picard: u_{k+1} = K_s⁻¹ f(u_k) with under-relaxation, or Newton–Raphson as in [S1]. Convergence test on ‖u_{k+1} − u_k‖ and on total lift; divergence is signalled by non-convergence at increasing q — but the proper test is the eigenproblem below. **V** for [S1]'s method / **I** for the Picard variant.
- **Static divergence (uniform cantilever, torsion-only, thin-airfoil):** GJ·θ″ + q·c·e·C_Lα·(α + θ) = 0 has its first non-trivial solution at q_D = GJ·(π/(2L))² / (c·e·C_Lα), with e the distance (m) from the aerodynamic centre to the elastic axis (positive when the AC is ahead of the EA), c the chord (m), C_Lα the section lift-curve slope (1/rad) and L the semispan (m); U_D = √(2 q_D/ρ_w). Coupling shifts it: nose-up coupling (K with fibres aft) lowers q_D; nose-down raises it [S2][S3]. Valid only as a first screen: uniform section, no sweep, no free surface. In the beam model compute divergence as the smallest q for which det(K_s − q·K̂_f) = 0 (generalised eigenproblem), which is what [S3]'s "frequency and damping of the new mode go to zero" reduces to statically. **F** (closed form from recall) / **I** (eigenproblem statement) / **V** (coupling effect)
- **Flutter screen:** 2-DOF section with Theodorsen C(k) [S29], plus water added mass ≈ ρ_w·π·(c/2)² per unit span and added moment of inertia ≈ ρ_w·π·(c/2)⁴/8 about the mid-chord (flat-plate values — **F**). Report as *screen only*; [S3] shows the water mechanism differs (single-mode). **I**

#### 5. Material properties (starting table)

From the Performance Composites reference table [S15] — epoxy, 120 °C cure, dry, room temperature; UD at Vf 60 %, fabric at 50 %; "for reference / information only and NOT a guarantee of performance". **V** (as published; not a datasheet for any specific prepreg).

| Material | E1 (GPa) | E2 (GPa) | G12 (GPa) | ν12 | Xt (MPa) | Xc (MPa) | Yt (MPa) | S (MPa) | ε_t,1 (%) | ε_c,1 (%) | ρ (g/cm³) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Std carbon UD | 135 | 10 | 5 | 0.30 | 1500 | 1200 | 50 | 70 | 1.05 | 0.85 | 1.60 |
| HM carbon UD | 175 | 8 | 5 | 0.30 | 1000 | 850 | 40 | 60 | 0.55 | 0.45 | 1.60 |
| M55 UD (calc.) | 300 | 12 | 5 | 0.30 | 1600 | 1300 | 50 | 75 | — | — | 1.65 |
| Std carbon fabric 0/90 | 70 | 70 | 5 | 0.10 | 600 | 570 | 600 | 90 | 0.85 | 0.80 | 1.60 |
| HM carbon fabric | 85 | 85 | 5 | 0.10 | 350 | 150 | 350 | 35 | 0.40 | 0.15 | 1.60 |
| E-glass UD | 40 | 8 | 4 | 0.25 | 1000 | 600 | 30 | 40 | 2.50 | 1.50 | 1.90 |
| E-glass fabric | 25 | 25 | 4 | 0.20 | 440 | 425 | 440 | 40 | 1.75 | 1.70 | 1.90 |
| Kevlar UD | 75 | 6 | 2 | 0.34 | 1300 | 280 | 30 | 60 | 1.70 | 0.35 | 1.40 |
| Std carbon ±45 (calc.) | 17 | 17 | 33 | 0.77 | 110 | 110 | — | 260 | — | — | — |
| HM carbon ±45 (calc.) | 17 | 17 | 47 | 0.83 | 110 | 110 | — | 210 | — | — | — |
| Steel S97 | 207 | 207 | 80 | — | 990 | — | — | — | — | — | — |
| Aluminium L65 | 72 | 72 | 25 | — | 460 | — | — | — | — | — | — |
| Titanium DTD 5173 | 110 | 110 | — | — | — | — | — | — | — | — | — |

Moisture expansion coefficients from the same table: UD carbon β2 = 0.30 strain per unit moisture content transverse vs 0.01 longitudinal; glass UD β2 0.30 — the transverse (matrix-dominated) properties are the ones water degrades. **V** (numbers) / **I** (interpretation). Also from the table: HM carbon's compressive strain to failure is 0.45 % (UD) and 0.15 % (fabric) — compression at the *lower* surface root of a lifting wing is the design driver for HM layups. **I**

Recall-only values, all **F** (verify against a datasheet before use): PVC structural foam (Divinycell H80 class) ρ 80 kg/m³, E ≈ 90 MPa, G ≈ 27 MPa, compressive strength ≈ 1.4 MPa, shear ≈ 1.15 MPa; PU/PET foams similar per density; SLS PA12 E ≈ 1.7 GPa, σ_t ≈ 48 MPa, ρ 1.01 g/cm³; chopped-carbon PA (Onyx) E ≈ 2.4 GPa tensile, **71 MPa flexural strength and 145 °C HDT [S30, V]**, water uptake of PA12 0.76 % (SLS, untreated, [S21, V]) with a matching modulus drop when wet (F); continuous-carbon nylon prints E ≈ 50–60 GPa along the fibre (F). Cured ply thickness: ≈0.1–0.15 mm per 100–150 g/m² UD carbon ply, ≈0.2–0.25 mm per 200 g/m² woven ply at 55–60 % Vf (F) — the number that sets the minimum skin and therefore the minimum TE thickness.

**Degradation and joints.** Salt water attacks the matrix and the fibre–matrix interface (transverse and shear properties), not the carbon; UV chalks unprotected epoxy — both are recall (**F**). Aluminium in contact with carbon in seawater forms a galvanic couple with the carbon cathodic; the pitting sensitivity of EN AW-6060-T6 in a CFRP/aluminium joint has been simulated by FE (Mandel & Krüger 2013, Corrosion Science — title Verified [S33], content not opened). Practice: isolate with glass plies, anodising, sealant and titanium or stainless fasteners (F). Axis's aluminium mast is a 19 mm section [S25]; every aluminium-mast / carbon-fuselage or carbon-wing interface is a galvanic joint by construction (I).

#### 6. Failure modes and criteria the beam tier can express

- **First-ply failure, max strain:** ε_1 ≤ ε_1t / SF (tension) and |ε_1| ≤ ε_1c / SF (compression), per ply, from the beam curvature and twist rate: ε(y, s) = −κ·z(s) + (torsional shear via q_s/(t_s·G)). The compressive allowable of HM carbon (0.45 %) is the usual limiter. **I** (mechanics) / **V** (allowables from [S15])
- **Tsai–Wu (quadratic):** F₁σ₁ + F₂σ₂ + F₁₁σ₁² + F₂₂σ₂² + F₆₆τ₁₂² + 2F₁₂σ₁σ₂ ≤ 1, with F₁ = 1/Xt − 1/Xc, F₁₁ = 1/(Xt·Xc), F₂ = 1/Yt − 1/Yc, F₂₂ = 1/(Yt·Yc), F₆₆ = 1/S², F₁₂ ≈ −½√(F₁₁F₂₂) [S32 for the criterion; coefficient forms **F**]. Report the reserve factor R (scale on load such that the criterion reaches 1), never a binary "passes".
- **Modes a beam cannot see (must be listed as "not assessed"):** skin buckling under compression on the lower surface (thin skins over a soft core); delamination at ply drops and at the spar–skin bond; trailing-edge bond-line cracking (turns the closed cell into an open section — torsional stiffness collapses); mast–fuselage and fuselage–wing bolted-joint bearing failure and fastener corrosion; cavitation erosion [S1 citing Yamatogi]; impact damage and its interaction with preload [S26]. **I** / **V** where cited.

#### 7. Manufacturing floors and finishes (design-space constraints, GAP-07)

| Constraint | Value | Source and label |
|---|---|---|
| CNC dimensional tolerance | ±0.1 mm typical, ±0.02 mm feasible (ISO 2768 medium/fine) | [S18] **V** (generic vendor) |
| Minimum machined wall | plastics 1.5 mm recommended / 1.0 mm feasible; metals 0.8 / 0.5 mm | [S18] **V** |
| Internal corner radius | always ≥ tool radius; features aligned to principal directions for 3-axis; 5-axis relaxes access | [S18] **V** |
| Injection-mould draft | ≥ 2° recommended; 0.25–0.5° absolute minimum | [S19] **V** |
| Composite lay-up mould draft | 1–3° on vertical walls; a foil's convex surfaces self-release, the *TE and root fillet* are where lock occurs | **F** practitioner |
| SLS shrinkage | 3–3.5 % on cooling, software-compensated; warping on large flat areas | [S20] **V** |
| SLS PA12 water absorption | 0.76 ± 0.08 % untreated, 0.35 ± 0.04 % vapour-smoothed (ASTM D570) | [S21] **V** |
| Minimum molded-carbon TE thickness | ≈0.5–0.8 mm as-molded with two skins meeting, ≈1.0–1.5 mm with a core or a bonded TE; below 0.5 mm the TE is resin, chips, and is a bond-line failure site | **F** practitioner; derivable floor = 2 × (skin plies × cured ply thickness) → 2 × 2 × 0.12 = 0.48 mm for two 100 g/m² UD plies per side |
| Minimum printed TE thickness | ≥ 1.0 mm (SLS PA12) and ≥ 1.2–1.5 mm (FFF CF-PA, ≥ 2 perimeters) | **F** practitioner (Hubs "thin wall" guidance not opened) |
| Minimum LE radius | ≥ the finishing ball-end radius used on the mould (≈1.5–3 mm) for a machined LE; ding tolerance argues for ≥ 1 % chord | **F** |
| Surface finish, machined mould + hand polish | Ra ≈ 0.4–0.8 µm after wet-sanding P800–P2000 and polish; as-machined tooling board Ra ≈ 1.6–3.2 µm with scallop height h_s = R − √(R² − (a/2)²) for ball radius R and stepover a | **F** (Ra bands recall); scallop formula elementary (**F**) |
| Surface finish, SLS as-printed | Ra ≈ 8–15 µm; vapour smoothing halves it | **F** |
| Roughness equivalence | equivalent sand-grain k_s ≈ (2–5)×Ra for random finishes; hydraulically smooth if k_s ≤ k_adm ≈ 100·ν/U (Schlichting) → at U = 10 m/s, ν = 1.19×10⁻⁶ m²/s (15 °C seawater): k_adm ≈ 12 µm | **F** (recall); the *tripping* criterion Re_k ≈ 250–600 is **V** [S17] |

Consequence for the polar tier (GAP-10): with k_s ≈ 3 µm (polished mould part) the foil is hydraulically smooth at all sport speeds; an as-printed SLS surface (k_s ≈ 30–50 µm) is not, and a single 0.3 mm ding at 10 % chord has Re_k = u_k k/ν of order 10³ — above the tripping threshold — so the polar downstream of it is a tripped polar, not a clean one. Run both (clean; tripped at the roughness x/c) and show the band. **I** from [S16][S17].

**Mold design workflow (what the tool must export, EXP-02):** CAD surface (STEP shell, mm, with tolerance) → offset/shell by the skin thickness for a core-machined build → split by a **parting line** along the LE and TE (two-part mould: upper and lower cavities) or along the mid-thickness for solid cores → mould blocks with **draft** on any wall not self-releasing, **registration datums** (three dowel positions), flange and vacuum-seal margins → CNC roughing/finishing → sealing/release → lay-up. Anhedral, twist and tip down-turn do not by themselves prevent a two-part mould as long as the parting curve is the LE/TE silhouette in the pull direction and no surface normal crosses the pull direction beyond the draft angle; a highly down-turned tip or a strongly cambered root fillet does, and needs a third piece or a rotated pull. The exportable, checkable quantity is therefore: for a chosen pull direction p, the set of surface points where n·p < sin(draft) — the tool can compute and colour it. **I** (workflow is practitioner-standard, **F** as a source; the undercut test is geometry)

#### 8. Regulatory and safety facts (GAP-14)

- RCD 2013/53/EU Art. 2(2)(a): excludes "(iii) surfboards designed solely to be propelled by wind …; (iv) surfboards; … (xi) hydrofoils" from Part A design/construction requirements. **V** [S10]
- ISO 25649-1:2024 scope: "not applicable to … surf sports type devices …; water ski, wakeboard or kite surfing board; devices made from rigid materials e.g. wood, aluminium, hard or non-deformable plastic". **V** [S11]
- ISO 12215-9:2026 (2026-09-10): loads, scantlings, design stresses, load cases and computational guidance for keel, centreboard and attachments on monohull sailing craft ≤ 24 m. **V** scope [S12]; applicability to a board foil by analogy only (**F**).
- ISO 21853:2020: kiteboarding release systems. **V** [S13].
- CPSC public recall API, 2026-09-20: 0 results for "hydrofoil", "efoil", "foilboard". **V-negative** [S14]. EU Safety Gate not queried (site requires JavaScript). EU GPSR 2023/988 general duty — **F** (not opened).
- Invariant for the product: **no output of the hydrodynamic tiers is a strength statement**; the spec's Safety row already says so (A2 "No fabrication or structural safety certification is implied"; the Safety principle "No strength, manufacturing, ride-safety or certification claim from hydrodynamic output").

## Validation data, special hydrofoil physics and testing numerical design software

*Source: [13-validation-special-physics-and-numerical-testing.md](13-validation-special-physics-and-numerical-testing.md) (`kb-hw-validation-special-physics-and-numerical-testing`).*

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

