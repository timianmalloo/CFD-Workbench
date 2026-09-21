---
id: kb-hw-optimization-strategies
title: "Optimization strategies for 2D foil sections and 3D hydrofoil wings"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, optimization, multipoint, cavitation-constraint, surrogate, multi-fidelity, adjoint, pareto, goal-state, provenance, licences]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes how a rider brief becomes a constrained multipoint optimization problem (objectives,
  operating-point sets, cavitation/structural/manufacturing/class constraints, robustness), which
  algorithms fit which evaluation cost, what the hydrofoil-optimization literature (Garg/Young 2017–2019,
  Ng/Yildirim 2025, Drela 1998) proves about optimizer exploitation and multipoint design, and which
  optimizer tooling is licence-compatible. Main design implication: v1 must model the goal state,
  constraint set, design vector (= the explicit curve/station definition) and per-evaluation provenance
  now, so the deferred optimizer can plug in behind COMMIT-01 gates without a second geometry truth.
---

# Optimization strategies for 2D foil sections and 3D hydrofoil wings

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. How a rider brief (90 kg, race wingfoil, salt water, 10–20 kn wind) becomes an optimization problem: objectives, operating-point sets, constraints, robustness.
2. Design variables and parameterization *for optimization* (CST, B-spline, PARSEC, Hicks–Henne, FFD, mode-based, the workbench's own curves/stations) and regularity constraints.
3. Algorithms by tier and evaluation cost: gradient-free, Bayesian/surrogate, multi-fidelity, gradient-based (FD / complex-step / AD / adjoint), constrained solvers; convergence, restarts, exploitation detection.
4. Section-level specifics: cavitation-bucket widening, transition robustness, XFOIL/NeuralFoil-in-the-loop pitfalls, inverse design's place.
5. Wing-level specifics: planform/twist/section blending, induced drag under bending-moment constraints, free-surface, stabilizer/trim, structural coupling, pumping.
6. Tooling with licences and where each may run under COMMIT-02.
7. Presenting results honestly: Pareto fronts, constraint activity, provenance, the re-verification ladder, candidate → editable design.

Labels: **Verified** = primary source opened this session (or executed); **Inferred** = reasoned from Verified sources; **Flagged** = recall, single/dated source, or not opened. A spike was run and disposed per the Spike Protocol (the brief forbids extra files): a 60-line SciPy/SLSQP lifting-line probe whose formulation is fully stated in the Data section so it can be re-created in minutes; results are quoted where used.

## Headline findings

1. **Single-point optimization of a section is unsafe by construction.** Drela (1998) showed a 1-point drag minimization "raises a bump on the surface to 'fill' the transitional separation bubble", which helps only at the sampled CL and worsens both lower and higher CL; his conclusion: "number of operating points sampled = O(number of design parameters)" and "sampling somewhat beyond the expected operating range appears to be best". — *(Verified, [S1])*
2. **The same pathology is measured in 3D hydrofoils with RANS + FEM + adjoint.** Garg, Kenway, Martins & Young (2017): single-point hydrostructural optimization of a NACA 0009 trapezoidal foil gave +12.4 % L/D and +45 % cavitation-inception speed but "was found to be worse than the baseline at off-design conditions"; a five-point probabilistic multipoint design gave +8.5 % weighted L/D and +38 % inception speed *over the whole range*; only the coupled hydrostructural optimum satisfied the stress constraint at the highest load. — *(Verified, [S2])*
3. **An optimized hydrofoil has been validated in a cavitation tunnel.** Garg et al. (2019): predicted vs measured mean lift, drag and moment differ by 2.9 %, 5.1 %, 3.0 %; tip deflection 3.4 %; measured L/D +29 % over CL −0.15…0.75 with "significantly delayed cavitation inception", attributed to redistributed camber, twist, thickness and LE radius. This is what A6 "Experimentally compared" must mean for an optimizer output. — *(Verified, [S3])*
4. **Cavitation is a smooth, differentiable constraint in the state of the art.** Garg 2017 constrains the *area* susceptible to cavitation, A_cav = (1/A_ref)∬χ dA with χ = 1/(1+e^{2k(Cp+σ)}), k = 10, A_cav ≤ 5×10⁻⁴; Ng, Yildirim, Youngren & Lamkin (JST 2025, AC75 sections with flaps) implement "a minimum pressure constraint to control cavitation onset" in a RANS-based multipoint framework and then check that "the optimizations did not significantly exploit weaknesses" by independent XFOIL comparison. — *(Verified, [S2][S4][S5])*
5. **Multipoint is the discrete special case of optimization under uncertainty.** Martins & Ning: "A multipoint optimization is a simplified example of OUU. Effectively, we have treated the Mach number as a random parameter with a given probability at three discrete values. We then minimized the expected value of the drag." Garg 2017 sets weights w_k = p(CL_k)·ΔCL from an operating-condition PDF, and admits the PDF was "based on engineering experience". — *(Verified, [S6][S2])*
6. **Gradient-based methods scale; gradient-free ones do not.** Martins & Ning Fig. 1.23: gradient-based with exact derivatives grows from 67 to 206 function calls over 1–30 variables, gradient-free from 103 to over 32,000. For the workbench's cheap tiers (NeuralFoil-class ms, VLM ms–s) gradient-free/Bayesian is affordable; for CFD it is not. — *(Verified, [S6])*
7. **NeuralFoil is differentiable, C∞, and self-reports trust.** It "assesses its own trustworthiness, yielding an `analysis_confidence` output"; the AeroSandbox optimization tutorial constrains `analysis_confidence > 0.90`, local thickness > 0, a Wahba-type "wiggliness" penalty, CM ≥ −0.133, thickness at x/c 0.33 and 0.90, TE angle ≥ 6.03°, and weighted CL targets 0.8–1.6 (Drela's DAE-11 6-point case). Drela's own explanation of why NeuralFoil does not show the bubble-bump pathology: "its smaller geometry design space … bumps … cannot appear in this space". — *(Verified, [S7][S8])*
8. **AeroSandbox's automatic differentiation is CasADi + IPOPT, not JAX.** `aerosandbox/optimization/opti.py` does `import casadi as cas` and `class Opti(cas.Opti)`; `pyproject.toml` (4.2.10) requires `casadi>=3.7.2`; CasADi is **LGPL-3.0** and IPOPT **EPL-2.0**. NeuralFoil 0.3.3 depends on `aerosandbox>=4.2.7,<4.3.0`, so a NeuralFoil sidecar transitively ships CasADi. The research brief's "JAX in AeroSandbox" is incorrect. — *(Verified, [S9][S10][S11][S12])*
9. **Licences as of 2026-09-20 (GitHub API + licence files):** OpenMDAO Apache-2.0; MPhys Apache-2.0; OpenAeroStruct Apache-2.0; pyGeo Apache-2.0; pymoo Apache-2.0; SciPy BSD-3-Clause; SMT BSD-3-Clause; pycma BSD-3-Clause; Optuna MIT; BoTorch MIT; Ax MIT; AeroSandbox MIT; NeuralFoil MIT; Xoptfoil2 MIT; Math.NET Numerics MIT; argmin Apache-2.0 (+MIT dual, Flagged); **pyOptSparse LGPL-3.0; SU2 LGPL-2.1; ADflow LGPL-2.1; Dakota LGPL-2.1; NLopt LGPL overall (Luksan parts LGPL-2.1+); CasADi LGPL-3.0; DAFoam GPL-3.0**. Only the first group may be linked; the LGPL/GPL group is process-only or excluded. — *(Verified, [S13])*
10. **Prandtl's 1933 bell spanload is reproducible by a 20-line constrained optimization** and is the right *shape* of a wing-level objective: the spike minimized induced drag at fixed lift and fixed *integrated* bending moment and found span ×1.2247 (+22.5 %), induced drag ×0.8889 (−11.1 %), load exactly (1−x²)^{3/2} — Bowers' "11 percent more efficient and 22 percent greater span … same amount of structure". With the *root* bending moment fixed instead (Jones 1950: "15-percent reduction of the induced drag with a 15-percent increase in span"), the spike's optimum kept pushing span outward (+25 % with 2 modes, +32 % with 5) and was unbounded until a non-negative-load constraint was added — the optimizer exploited the formulation. — *(Verified by execution and [S14][S15]; Jones discrepancy Flagged)*
11. **Surrogate optimization must plan for non-computable points.** Sacher et al. (SMO 2018) add a probabilistic classifier for "non-computable areas" (unphysical configuration, ill-posed problem, solver failure) so EGO does not "stop prematurely"; XFOIL "is not guaranteed to produce a solution, and often crashes when 'ambitious' calculations are attempted"; XFOIL "exhibits hysteresis" between α-up and α-down sweeps. — *(Verified, [S16][S7][S17])*
12. **Multi-fidelity Kriging in SMT is AR1 co-Kriging (y_hi = ρ(x)·y_lo + δ(x)) and "only uses nested sampling training points"**; BoTorch's multi-fidelity route is a joint design×fidelity GP with knowledge-gradient acquisition scalarized by an affine cost model (fixed cost + per-fidelity weight). Non-nested infilling exists (Sacher 2021). Estimator → NeuralFoil → VLM → RANS is therefore a legitimate fidelity ladder *only if* the tiers are correlated, which GAP-06 has not measured. — *(Verified, [S18][S19][S20]; correlation gap Flagged)*
13. **Wing optimization is multimodal in practice-relevant cases.** Bons, He, Mader & Martins (AIAA J. 2019) documented multimodality in aerodynamic wing design; the book's decision tree routes "Multimodal? → Multistart" for differentiable problems. Restarts are a floor, not an option. — *(Verified title/venue [S21]; content Flagged; tree Verified [S6])*
14. **Inverse design remains the right tool for single-point section problems.** XFOIL MDES/QDES edit a target speed distribution and re-solve the shape with closure modes added automatically; Eppler's program specifies "the velocity distribution … not for one but many different angles of attack". Drela: "single-point problems are often better handled with traditional inverse methods". — *(Verified, [S17][S22][S1])*
15. **The goal-state arithmetic in area file 04 re-executes exactly** (seawater 15 °C, L = 1050 N: 1000 cm² at 10/14/18 kn → CL 0.773/0.395/0.239, σ 7.7/3.9/2.4; 750 cm² at 15/22/28/32 kn → CL 0.458/0.213/0.132/0.101, σ 3.4/1.6/0.98/0.75). The optimizer's operating-point set for the example is those seven rows, with the speeds still **Flagged** (no race dataset). — *(Verified by execution against [S23])*

## State of the art

### 1. From rider brief to optimization problem

**The brief is design intent, not a constraint (A3).** The spec already says "a riding brief is design intent; it is never silently a geometry constraint or a solved operating point" [S24]. The translation therefore has to be explicit and inspectable: brief → **goal state** (a value object) → operating-point set + objective(s) + constraint set → design vector → evaluation ladder → candidate set. Each arrow carries provenance.

**Objectives that the literature actually optimizes.** (a) Minimum drag at one or more lift constraints — the canonical form in Drela [S1], Garg [S2], Ng [S4] and the book's Mach-robust airfoil example [S6]. (b) Maximum weighted L/D over a CL set: Garg's η̄ = Σ w_k CL_k/CD_k with w_k = p(CL_k)ΔCL [S2]. (c) Cavitation inception speed as an objective or a constraint (Garg reports V_cav = √((P_ref−P_vap)/(½ρ·(−Cp_min))), P_ref = P_atm + ρ g h [S2]; file 04 gives the σ tables [S23]). (d) Structural mass or stress as a second objective/constraint (Garg 2017 KS-aggregated von Mises ≤ fatigue strength / 1.1) [S2]. (e) For race wingfoil specifically (Inferred from [S23]): minimize the wind-band-weighted drag at the seven operating points, with take-off feasibility (CL_required ≤ CL_max(Re) with margin) and top-end cavitation margin as constraints rather than objectives, because both are pass/fail for the rider.

**Operating-point sets.** Drela used 1, 2, 4 and 6 points and found the multipoint optimum can "scallop" between sampled points when the design space is rich [S1]; his rule "points = O(design parameters)" and "sample somewhat beyond the expected operating range" are the only quantitative guidance found. Garg used five CL points with weights 0.10/0.15/0.25/0.45/0.05 and a "critical metric" per point (pressure-side cavitation at CL = −0.15; drag at 0.30/0.50/0.65; stress and suction-side cavitation at 0.75) [S2] — a pattern worth copying verbatim: *each operating point declares what it is there to protect*. For the goal-state example the seven rows of file 04 are the sample; **Inferred** weights that reflect race time-on-task (upwind/downwind legs dominate; take-off and top-end are short but critical) are shown in the Data section, labelled as assumptions.

**Constraint catalogue (what to model in v1, whether or not the optimizer exists).**

| Constraint | Form used in the literature | Evidence tier needed | Label · source |
|---|---|---|---|
| Lift = load at each point | CL_k(α_k) = W/(q_k S), α_k a design variable per point | any tier | Verified [S2][S8] |
| Cavitation | A_cav ≤ 5×10⁻⁴ (smoothed area, k = 10) [S2]; min-pressure constraint [S4]; screening −Cp_min ≤ σ − margin [S23] | 2D polar for screening; RANS to certify | Verified |
| Structural | KS-aggregated von Mises ≤ σ_f/1.1 [S2]; beam bending/deflection/twist (GAP-02) | FEM or beam model; **not modelled in v1** | Verified [S2]; gap Flagged |
| Manufacture | TE thickness ≥ 1.1 × baseline at 20 spanwise points (0.242 → 0.121 mm root→tip) [S2]; local thickness at x/c (0.33, 0.90) and TE angle ≥ 6.03° [S8]; GAP-07 min TE, layup floor | geometry only | Verified [S2][S8] |
| Geometric validity | local thickness > 0 everywhere (no crossing) [S8]; fixed LE (10 constraints) [S2]; min LE radius (Inferred) | geometry only | Verified/Inferred |
| Regularity | Wahba-type wiggliness measure on the CST curve [S8]; curvature bounds (Drela: "constraints such as on surface curvature could instead be imposed … in effect a reduction in the effective number of free geometric design parameters") [S1] | geometry only | Verified |
| Model trust | `analysis_confidence > 0.90` [S8] | surrogate tier | Verified |
| Class rules | GWA front ≥ 700 cm², rear ≥ 150 cm², mast ≤ 115 cm, fuselage ≤ 100 cm [S23] | geometry only | Verified in [S23] |
| Pitching moment | CM ≥ −0.133 [S8]; stabilizer trim (Inferred) | 2D/VLM | Verified/Inferred |
| Robustness | expected value over the point PDF [S6][S2]; CVaR (Rockafellar–Uryasev 2000) as a tail measure [S25] | any | Verified book/paper; CVaR use in aero Flagged |

**Robustness formulations.** The book's Chapter 12 distinguishes robust design (statistics of the objective) from reliable design (probability of constraint violation) [S6]. Expected value with discrete weights is what every hydrofoil paper found uses [S2][S4]. Worst-case over the band is the conservative alternative; CVaR_α (mean of the worst (1−α) tail) is the standard smooth compromise from finance [S25] and is a natural fit for "the foil must not fail at the top-end" — its use on hydrofoils was not found this session (Flagged).

**Worked problem statement for the goal state (Inferred; every number's label is in the Data table).**

```
given   goal state G = { rider 90 kg (given); board 5.5, foil 4.5, hand wing 2.4, kit 3.0 kg (Flagged);
                         W = 1030 N; front-wing load L = 1050 N (Flagged trim share);
                         water = ITTC seawater 35 g/kg 15 °C record (Verified); depth h ∈ [0.3, 0.5] m (Flagged);
                         wind band 10–20 kn → operating points k = 1..7 (speeds Flagged);
                         class preset = GWA race box (Verified); Ncrit pair {2, 4} (practitioner range) }
find    x = [ chord(η) controls, twist(η) controls, sweep(η) controls, elevation(η) controls, t/c(η) controls,
              CST deltas per authored station (≤ 18 each), α_k for k = 1..7 ]            — the record's own controls
minimize  J(x) = Σ_k w_k · D_k(x) / D_k(x₀)            (weighted drag, normalised to the start design)
          — or the pair ( Σ_k w_k D_k ,  −V_cav,min ) for a two-objective front
subject to  L_k(x, α_k) = 1050 N                       k = 1..7            (lift equality; α_k free)
            CL_1 ≤ CL_max(Re_1, Ncrit) − 0.10          (take-off margin, both Ncrit)  — Flagged margin
            −Cp_min,k(η) ≤ σ_k − Δσ   ∀ strips, k = 6, 7 (suction side), k = 1 (pressure side); Δσ = 0.1 Flagged
            analysis_confidence_k(η) ≥ 0.90            ∀ strips, k                     (surrogate trust)
            S ≥ 700 cm², rear ≥ 150 cm², mast ≤ 115 cm (GWA)                           (class box)
            t_TE(η) ≥ t_TE,min ; t(x/c, η) > 0 ; r_LE(η) ≥ r_LE,min ; TE angle ≥ θ_min   (manufacture, validity)
            wiggliness(section_j) ≤ w_max ; |dchord/dη| bounded ; stations non-crossing    (regularity)
            structural proxy: M_root(x) ≤ M_root(x₀)  OR  ∫M dη ≤ ∫M dη |x₀  OR  tip twist ≤ θ_tip  (choose one; GAP-02)
            CM_k ≥ CM_min (stabilizer authority)                                          (trim placeholder)
            x_lb ≤ x ≤ x_ub                                                               (bounds)
robustness  expected value over w_k (default) | worst case over k | CVaR_0.8 over k        (user-visible switch)
tiers       T1 sections (NeuralFoil-class) → T2 wing (VLM + strips) → T3 CFD re-check (points 1 and 7)
provenance  every evaluation: x-hash, tier, method version, Ncrit, water id, converged, confidence, ms
```

Reading the constraint set: two of the rows (take-off margin and the cavitation margin) are the rider-safety rows and are constraints, never objectives; the structural row is a *proxy* until GAP-02 closes; the trim row is a placeholder because v1 authors one lifting wing. A problem with the seven-point set and one all-wind 850 cm² wing is the same statement with S fixed; the two-wing formulation is two runs sharing the section family and the class preset. The normalisation D_k/D_k(x₀) keeps the seven drag values commensurable (they span q from 13.6 to 139 kPa). A weighted sum of *forces* without normalisation would let the top-end point dominate by q alone — a silent weighting error (Inferred).

**How the weights should be set (what the sources allow).** Garg: w_k = p(CL_k)·ΔCL from a PDF of operating conditions, admitted to be experience-based for a canonical foil [S2]. Drela: weights "are arbitrary, and their appropriate values cannot be easily estimated without prior experience" [S1]. Martins & Ning: the discrete-probability reading makes the weights a *distribution*, so their basis (race GPS time-on-task, or a stated guess) must be stored with the goal state [S6]. The seven weights in the Data table are a guess and are labelled so.

### 2. Design variables and parameterization for optimization

File 02 already establishes the representation facts: Masters et al. (2017) needed 20–25 variables for any of seven methods to cover the UIUC database, SVD most efficiently; a 2025 review ranks CST first for optimization with < 22 variables; CST coefficients have global support and are poor direct-manipulation handles; CST is retained as *optimizer coordinates derived from the record* [S26]. This file adds only the optimization consequences.

- **CST order in practice.** NeuralFoil's input is 8 CST weights per side + LE-modification + TE thickness = 18 parameters [S7]; the tutorial bounds them to [−0.5, 0.25] lower / [−0.25, 0.5] upper and LE weight [−1, 1] [S8]. SU2's 2D tutorial uses 30 Hicks–Henne bumps [S27]. Garg used 210 FFD shape variables with a coupled adjoint [S2]. Drela's hypothesis — richer geometry ⇒ finer exploitable scale ⇒ more sample points — is the reason to keep the section design space small at cheap tiers [S1]; Drela's own reading of NeuralFoil's 18-parameter space confirms the benefit [S7].
- **3D: FFD vs the workbench record.** FFD (Garg [S2]; SU2 3D tutorials [S27]; pyGeo Apache-2.0 [S13]) moves a mesh; that is the right tool *inside* a CFD-adjoint loop but it violates the workbench's "one explicit parametric surface" if its output became the record. The workbench's design vector should be the record's own controls: the five distribution curves' control values/weights (Smooth mode) or anchor values/tangents (Through-points mode), plus per-station profile CST deltas from the catalog profile (Inferred from [S24][S26]). A CFD-adjoint tier that needs FFD must project its shape gradient back onto those controls (chain rule through the loft evaluator) or be used only as a *re-verification* tier, never as the design-variable owner (Inferred).
- **Mode-based and generative parameterizations.** SVD/PCA modes from an airfoil database (Poole, Allen & Rendall 2015 [S28]; Masters 2017 via [S26]) give the most compact linear space; Bézier-GAN (Chen, Chiu & Fuge 2020) and FFD-GAN (Chen & Ramamurthy 2021) learn a nonlinear latent space that "accelerate[s] design optimization convergence by improving the representation compactness" [S29][S30]; file 02 flags 2025–26 latent-diffusion generators as "watch, not adopt" [S26]. All of these are optimizer-side coordinate systems; the record stays the B-spline definition with a reported fit residual [S26].
- **Regularity constraints are part of the design vector definition, not an afterthought:** thickness > 0, TE thickness floor, TE angle floor, LE fixed/LE radius floor, wiggliness/curvature bound, monotone-or-bounded chord and thickness distributions along span, no station crossing, and the class-rule box [S1][S2][S8][S23].

### 3. Algorithms by tier

**Selection logic (book decision tree [S6]):** convex? → LP/QP; discrete? → branch-and-bound/GA; differentiable? → BFGS (unconstrained) or SQP/IP (constrained), with multistart if multimodal; not differentiable → gradient-free (DIRECT, GPS, GA, PS, Nelder–Mead), noisy/expensive → surrogate-based (Ch. 10, EGO with expected improvement), uncertainty → Ch. 12, multiple disciplines → Ch. 13.

**By evaluation cost (Inferred mapping of the workbench tiers onto that tree):**

| Tier | Cost per evaluation | Derivatives available | Appropriate algorithms | Evidence for the mapping |
|---|---|---|---|---|
| Closed-form estimator | µs | analytic / AD trivially | anything; use it for DOE, sensitivity screening and as the lowest MF level, never for certification (COMMIT-01) | Inferred |
| NeuralFoil-class polar | ms (README benchmark table lists ~6 ms per evaluation for `xxxlarge`; column reading Inferred) | exact via CasADi in AeroSandbox; C∞ by construction | IPOPT/SLSQP with multistart; CMA-ES/DE/NSGA-II affordable (10⁴–10⁵ evals) | Verified speed/derivatives [S7][S9]; mapping Inferred |
| XFOIL polar | ~0.1–1 s (Flagged recall) | none (hysteresis, non-C¹ [S7]) | gradient-free only (Xoptfoil2 pattern [S31]); treat unconverged points as non-computable [S16] | Verified properties; timing Flagged |
| VLM + strip theory | ms–s | AD if the VLM is written in an AD framework (AeroSandbox, OpenAeroStruct [S32]) or FD/complex-step | SQP/SLSQP; NSGA-II for planform Pareto fronts | Verified tools; mapping Inferred |
| 2D RANS (SU2/OpenFOAM) | minutes | adjoint (SU2 continuous/discrete [S27]; DAFoam GPL-3.0 process-only [S13]) | EGO/MFBO with tens of evaluations, or adjoint + SLSQP | Verified tools; mapping Inferred |
| 3D RANS (+FEM) | hours | coupled adjoint (ADflow LGPL-2.1 + TACS in Garg [S2]) | adjoint + SQP (SNOPT in Garg, licence-restricted) | Verified [S2] |

**Gradient-free.** Nelder–Mead, pattern search, DE, PSO, CMA-ES, GA are all in pymoo (Apache-2.0) with SRES/ISRES for constraints [S33]; CMA-ES's reference is Hansen's tutorial (arXiv 1604.00772) [S34] and pycma (BSD-3) [S13]. Their cost grows explosively with dimension (book: 103 → 32,000 calls from 1 to 30 variables) [S6], so they belong to the µs/ms tiers or to ≤ 10-variable planform problems.

**Multi-objective.** NSGA-II (Deb et al. 2002) [S35] and NSGA-III for many objectives (pymoo lists NSGA-II, R-NSGA-II, NSGA-III, U-NSGA-III, MOEA/D, C-TAEA, AGE-MOEA, RVEA, SMS-EMOA…) [S33]; Optuna ships TPE, CMA-ES, GP, NSGA-II, NSGA-III, QMC and grid samplers [S36]. The book warns the weighted-sum method "is easy to use, but it is not particularly efficient" and cannot reach non-convex parts of the front; ε-constraint and NBI are the alternatives [S6]. For the workbench the practical front is two-dimensional (cruise drag vs take-off feasibility margin, or drag vs cavitation-inception speed) and NSGA-II at the NeuralFoil/VLM tier is affordable (Inferred).

**Bayesian / surrogate-based.** EGO (Jones, Schonlau & Welch 1998) with Kriging + expected improvement [S37]; Forrester, Sóbester & Keane (2008) is the engineering text [S38, not opened — Flagged]; SMT (BSD-3) provides Kriging, KPLS, gradient-enhanced Kriging and MFK [S18][S39]; BoTorch/Ax (MIT) provide batched, constrained and multi-fidelity acquisition functions [S19]. Ploé's thesis is the direct hydrofoil precedent: GP surrogate on RANS data with a sequential acquisition function balancing multiple criteria [S40]. Sacher's classifier for non-computable points [S16] is mandatory when XFOIL or a CFD backend is in the loop.

**Multi-fidelity.** Kennedy & O'Hagan (2000) autoregressive model [S41]; Han & Görtz hierarchical Kriging (2012) [S42]; SMT MFK: y_hi = ρ(x)·y_lo + δ(x), nested samples only, `propagate_uncertainty` option [S18]; BoTorch MFKG: joint GP over (x, s), `AffineFidelityCostModel(fixed_cost, fidelity_weights)`, `InverseCostWeightedUtility` — "the MFKG acquisition function optimizes the ratio of information gain to cost" [S19]; Sacher 2021 non-nested infilling [S20]; Toal 2023 assesses multi-fidelity multi-output Kriging for design optimization [S43]. **Precondition:** the low-fidelity tier must be *informative* about the high one; the workbench has no measured tier correlation (GAP-06), so MF is "after the ladder is calibrated" (Inferred).

**Gradient-based and derivatives.** Finite differences (step-size error), complex step (book Fig. 6.9: error falls to machine precision for h → 10⁻²⁰⁰) [S6], algorithmic differentiation (CasADi in AeroSandbox [S9]; JAX Apache-2.0 exists but is not used by AeroSandbox [S13]), and adjoints (Jameson 1988 [S44]; SU2 continuous and discrete adjoint selectable by flag, Hicks–Henne/FFD variables, SLSQP driver via `shape_optimization.py`, `OPT_ITERATIONS = 100` [S27]; DAFoam for OpenFOAM, GPL-3.0 [S13][S45]; ADflow LGPL-2.1 [S13]). Constrained solvers: SLSQP (SciPy, BSD-3), IPOPT (EPL-2.0), SNOPT (proprietary — used by Garg), pyOptSparse wraps ALPSO, CONMIN, IPOPT, NLPQLP, NSGA2, PSQP, SLSQP, ParOpt, Uno, SNOPT but is LGPL-3.0 [S46]. OpenMDAO (Apache-2.0) drivers in the source tree: `scipy_optimizer`, `pyoptsparse_driver`, `pymoo_driver`, `modopt_driver`, `differential_evolution_driver`, `genetic_algorithm_driver`, `doe_driver`, `analysis_driver` [S47].

**Convergence, restarts, and exploitation detection.** (i) SQP convergence = KKT residual + feasibility tolerance; report both, never "converged" alone (book Ch. 5 [S6]). (ii) Multistart from ≥ N random feasible starts and from the catalog profile; if the best two optima differ in objective by more than the tier's stated model error, the problem is multimodal at that tier (Bons 2019 [S21]; Inferred rule). (iii) Exploitation tells, all Verified in the sources: bumps at bubble location [S1]; drag polar scalloping between sampled points [S1]; `analysis_confidence` dropping toward the constraint bound [S7][S8]; optimum sitting on a parameterization bound; an unbounded direction (the spike's span blow-up, Finding 10). (iv) The cross-check that the AC75 paper used — re-analyse the optimum with an *independent* method and require the gain to survive [S4][S5] — is exactly COMMIT-01's re-evaluation rule.

### 4. Section-level specifics

- **Cavitation-bucket widening.** Eppler's hydrofoil trilogy and the E817/E818 aft-loaded "rooftop" family are in file 06 [S48]; the design tool for that family is inverse: Eppler's program specifies the velocity distribution "not for one but many different angles of attack" and iterates the TE angle [S22]. In optimization form, the bucket is widened by constraining −Cp_min (or A_cav) at *both* ends of the CL band (Garg's CL = −0.15 for pressure-side and 0.75 for suction-side cavitation [S2]; Ng's ranges of flap angle, CL and speed [S4]). For the goal state: constrain −Cp_min ≤ σ(32 kn) − margin at CL ≈ 0.10 and pressure-side −Cp_min at CL ≈ 0 (Inferred; σ values [S23]).
- **Transition robustness.** File 06 already requires polars at Ncrit 2 and 4 and treats the spread as transition uncertainty [S48]; XFOIL's own Ncrit table (sailplane 12–14 … dirty wind tunnel 4–8) has no water row [S17]. Optimization consequence (Inferred): evaluate every operating point at the Ncrit pair and take the worse value (min-max) or the mean — never a single Ncrit; and constrain `analysis_confidence` because bubble-dominated regimes are exactly where the surrogate is least trustworthy [S7].
- **Thickness/structure trade.** Garg 2019's optimized foil "is significantly thicker to withstand higher loads than the baseline" and still measured +29 % L/D [S3]; without a structural model, thickness has no hydrodynamic optimum and drifts to the manufacturing floor (GAP-02/07 [S49]). v1 therefore needs a *fixed* thickness distribution or a beam-model proxy before any section optimizer runs (Inferred).
- **XFOIL/NeuralFoil-in-the-loop pitfalls (Verified):** XFOIL non-C¹ outputs, hysteresis, crashes; NeuralFoil always answers but flags low confidence; larger networks risk "overfitting to XFoil's inherent non-smoothness" [S7]; XFOIL's INIT/iteration-limit/gradual-α advice [S17]. Rules that follow: treat an unconverged XFOIL point as non-computable (classifier [S16]), never interpolate across it; freeze the surrogate version hash into the run provenance; re-verify the optimum with XFOIL at the polar level and with VLM at the wing level before showing it as anything but "surrogate candidate".
- **Inverse design in a workbench.** MDES/QDES are cursor-edited target-Q distributions with automatic closure corrections [S17]; PROFOIL (Selig) is the multipoint inverse code behind S1223 [S48] (site not opened — Flagged). Place (Inferred): a "target Cp" editor is a *profile editing mode* in the Profile catalog context, not an optimizer; it produces a Profile revision with provenance "inverse design from target Cp v…".

### 5. Wing-level specifics

- **Planform + twist + section blending.** The design vector is the five channels plus station profiles [S24]; induced drag is a planform/twist problem, profile drag and cavitation are section problems, and they couple through the local CL(η) at each operating point — which is why the optimizer must evaluate strip polars at the *local* Re and CL, not one wing-average (Inferred from [S23] Re bands).
- **Induced drag under structural constraints.** Prandtl 1933: fixed lift and *integrated* bending moment → bell load Γ ∝ (1−x²)^{3/2}, span +22 %, drag −11 % (Bowers TP-2016-219072 [S14]; spike reproduces 1.2247 / 0.8889). Jones 1950: fixed lift and *root* bending moment → "15-percent reduction … with a 15-percent increase in span" [S15]. Which moment is fixed changes the optimum; a wingfoil's limiting structure is the mast–fuselage–wing junction (root), but tip-twist under load matters at AR 11–13 (GAP-02). The workbench should let the goal state name the structural proxy (root BM, integrated BM, tip deflection) and show which one the optimizer used (Inferred).
- **Anhedral/dihedral near the free surface.** No primary quantification of anhedral effects for consumer foils was found in this or the file-04 session [S23]; Wadlin & Christopher 1958 (finite-depth lift with dihedral to 30°) is cited only through secondaries there. Treat anhedral as a *stability/feel* variable with a Flagged hydrodynamic effect, and keep h/c and Fr_h in every operating point [S23].
- **Stabilizer sizing and decalage.** Trim requires Σ moments = 0 about the rider/mast reference at each operating point; iQFOiL's −2° … +1° shim range is the concrete envelope [S23]. AC75 VPP-driven work (Tannenberg et al. JST 2023) argues foil lift/drag "do not directly translate to the performance of the yacht on the race course" and optimizes inside a whole-craft VPP [S50] — the wing-only optimizer must state that trim and stabilizer drag are held, not optimized (Inferred; v1 authors one lifting wing [S24]).
- **Structural coupling.** OpenAeroStruct couples a VLM with "a 6 degrees of freedom 3-dimensional spatial beam model" under OpenMDAO (Apache-2.0) [S32]; Garg's coupled RANS–FEM adjoint is the high end [S2]; composite bend–twist coupling changes the loaded twist (GAP-02 [S49]). A beam model is the cheapest way to make thickness a *solved* variable; it is the single most valuable addition before an optimizer is trusted (Inferred, consistent with [S49]).
- **Pumping efficiency.** Human pumping sits at St ≈ 0.05–0.15, below the 0.25–0.40 flapping-foil optimum, so glide L/D at CL 0.4–0.7 dominates [S23]; a Strouhal-based unsteady objective is a *third* objective for later, needing an unsteady tier the workbench does not have (GAP-05).

### 6. Workflow tooling and where it may run

See the Comparables table for licences. Under COMMIT-02 (permissive linked dependencies; process invocation allowed) [S24]:

- **In-process (.NET):** own SLSQP/SQP or an interior-point on Math.NET Numerics (MIT) [S13] — no permissive, maintained, constrained-NLP library for .NET was verified this session (Flagged); Nelder–Mead/CMA-ES/NSGA-II are small enough to own (Inferred). **Rust door:** argmin (Apache-2.0 verified; MIT dual Flagged) [S13].
- **Optional Python sidecar (process):** AeroSandbox + NeuralFoil (MIT) with CasADi (LGPL-3.0) + IPOPT (EPL-2.0) [S9]–[S12]; SciPy, pymoo, SMT, pycma, Optuna, BoTorch/Ax, OpenMDAO, OpenAeroStruct (all permissive) [S13]. Distribution of the sidecar's LGPL wheels requires the licence-review step COMMIT-02 names; process isolation keeps the product itself clean (Inferred from the sequence's SU2 reasoning [S51]).
- **Process-only solvers:** SU2 (LGPL-2.1) shape optimization via `shape_optimization.py` [S27]; DAFoam (GPL-3.0) [S45]; Dakota (LGPL-2.1) [S13]; MACH-Aero (no licence file in repo — Flagged) [S13]. **Excluded from linking:** pyOptSparse, NLopt, ADflow, CasADi (as a .NET link) [S13].

### 7. Presenting results honestly

- **Pareto front and trade-off plots** — each point labelled with the tier that produced it and whether it has been re-verified; dominated points shown greyed, not deleted (Inferred; A6 "out-of-envelope values remain visible" [S24]).
- **Constraint-activity display** — active/inactive/violated per constraint per operating point, with the Lagrange multiplier or shadow price where SQP gives one (book Ch. 5 [S6]); Garg's "critical metric per point" table is the model [S2].
- **Sensitivity/tornado** — first-order sensitivities from the AD/adjoint gradient at the optimum, or from a DOE at the estimator tier; label the tier.
- **Provenance per evaluation** — design-vector hash, tier/method/version (NeuralFoil model size, XFOIL build, VLM settings, CFD case hash), Ncrit, water record id, `analysis_confidence`, converged flag, wall time; per run — algorithm, seed, iterations, evaluations, termination reason, restarts (instrumentation-over-inference; A3 Analysis run invariants [S24]).
- **Re-verification ladder** — "surrogate candidate" (NeuralFoil/estimator) → "VLM-checked" (wing forces at each point re-evaluated) → "CFD-checked" (RANS at the critical points, the Ng/Yildirim cross-check pattern [S4][S5]) → "Experimentally compared" only with data (A6 [S24]; Garg 2019 shows what that costs [S3]). COMMIT-01 forbids certification on estimator evidence; "certified" at any tier means *re-evaluated at that tier with the gains surviving* — never a structural sign-off.
- **Candidate → editable design** — because the design vector *is* the record's controls, a candidate is a Geometry edit draft (A3) over the base Design revision; accepting it creates Surface/Profile revisions whose provenance names the optimizer run and evaluation ids; no mesh, no FFD lattice, no coordinate dump becomes the record (COMMIT-03 [S24]).

**Provenance record shape (Inferred; a sketch for /define-architecture, not a schema decision).**

| Record | Fields the sources make necessary | Why (source) |
|---|---|---|
| Goal state (value object) | masses with labels; water record id; depth band; wind→speed band + label; class preset id + rule version; operating points [speed, depth, load, critical metric, weight, Ncrit pair]; objective form; constraint rows (typed, with margins and their labels); robustness rule; structural proxy choice | brief → intent, never a constraint (A3 [S24]); per-point critical metric and weights [S2]; margins Flagged until calibrated |
| Design vector definition | ordered (curve id, control index, component, lb, ub, frozen) + (station id, CST index, lb, ub, frozen); base Design revision id | record's own controls (A4 [S24]; file 02 [S26]); bounds from the NeuralFoil tutorial pattern [S8] |
| Evaluation | design-vector hash; tier; method + version (NeuralFoil size, XFOIL build, VLM settings, CFD case hash); Ncrit; water id; per-point outputs; converged; analysis_confidence (nullable → "not recorded"); non-computable flag; wall time ms | XFOIL non-convergence/hysteresis [S7][S17]; classifier [S16]; instrumentation rule |
| Optimizer run | algorithm + library version; seed; restarts and their start points; iterations; evaluations by tier; termination reason; final KKT and feasibility residuals; constraint activity per point; Pareto set ids | KKT reporting [S6]; multimodality [S21]; termination variant (GO) |
| Candidate | run id; evaluation id; status ∈ {surrogate-candidate, vlm-checked, cfd-checked, experimentally-compared}; promotion evidence (higher-tier run id, gain survived %) ; Geometry edit draft id | COMMIT-01 [S24]; cross-check [S4]; validation costs [S3] |

**What "certified" may mean at each rung (Inferred from A6 and COMMIT-01 [S24]).** *Surrogate candidate*: "improves the weighted objective by X % under NeuralFoil/estimator; not verified." *VLM-checked*: "re-evaluated with VLM + strip polars at all seven points; gain Y % survives; free-surface/junction/ventilation omitted." *CFD-checked*: "RANS at points 1 and 7 (take-off and top-end) reproduces the section Cp_min within Z; wing-level drag not re-verified." *Experimentally compared*: only with admitted measured data of a comparable configuration — the Garg 2019 standard [S3]. None of these is a structural sign-off; the spec's own wording ("without calling that a structural sign-off") stands.

## Comparables

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| Garg/Kenway/Martins/Young 2017 + Garg 2019 [S2][S3] | Multipoint hydrostructural optimization of a 3D foil with cavitation, stress, TE-thickness constraints | RANS + FEM coupled adjoint, FFD (210 vars), SNOPT, KS aggregation, smoothed A_cav | Validated in tunnel (2.9/5.1/3.0 % force diffs); shows single-point failure | Hours per evaluation; SNOPT proprietary; ADflow LGPL | paper (ADflow LGPL-2.1, TACS Apache-2.0 Flagged) | Verified |
| Ng/Yildirim/Youngren/Lamkin JST 2025 [S4][S5] | AC75 sections with flaps, multipoint, cavitation-onset (min-pressure) constraint | RANS-based shape optimization; XFOIL cross-check | Explicit "did not exploit weaknesses" verification | AC75 speeds > 50 kn, not wingfoil Re | paper | Verified abstract |
| Drela 1998 [S1] | Airfoil optimization pros and cons (DAE-11 case) | MSES + gradient optimizer, 1/2/4/6-point | Names the exploitation pathology and the O(n) sampling rule | 1998 tooling | paper | Verified |
| NeuralFoil + AeroSandbox [S7][S8][S9] | Differentiable 2D polar surrogate + NLP in Python | 18-param CST; CasADi/IPOPT; confidence constraint | ms evaluations, exact gradients, smoothness, confidence | Python-only; CasADi LGPL-3.0 transitive; not validated in water | MIT (deps LGPL-3.0/EPL-2.0) | Verified |
| SU2 shape optimization [S27] | 2D/3D adjoint shape design | Continuous/discrete adjoint; Hicks–Henne/FFD; SciPy SLSQP driver | Reference open adjoint; process-driven | Mesh-based variables; LGPL-2.1 | LGPL-2.1 | Verified |
| DAFoam [S45] | Discrete adjoint for OpenFOAM in OpenMDAO/MACH-Aero | "hundreds of design variables and constraints" | Full RANS adjoint on OpenFOAM | GPL-3.0; Linux/Docker | GPL-3.0 | Verified |
| OpenMDAO + OpenAeroStruct + pyGeo [S32][S47] | Coupled aerostructural low-fidelity optimization | VLM + 6-DOF beam; derivatives via OpenMDAO | Apache-2.0 end-to-end; beam model exists | Aircraft assumptions; needs pyOptSparse (LGPL) or SciPy driver | Apache-2.0 | Verified |
| pymoo [S33] | General single/multi-objective evolutionary | GA/DE/PSO/CMA-ES/NM/NSGA-II/III/MOEA/D… | Constraint handling variants; Pareto tools | Evaluation-hungry | Apache-2.0 | Verified |
| SMT [S18][S39] | Surrogates incl. multi-fidelity Kriging | Kriging/KPLS/GEK/MFK, derivatives | MFK with nested data; BSD-3 | Nested-sample requirement | BSD-3-Clause | Verified |
| BoTorch / Ax [S19] | Bayesian optimization | GP + KG/EI acquisitions; MFKG with cost model | Batched, constrained, multi-fidelity | Torch dependency; heavy | MIT | Verified |
| Optuna [S36] | Hyper-parameter style black-box optimization | TPE, CMA-ES, GP, NSGA-II/III, QMC samplers | Easy pruning/DB provenance | Not physics-aware | MIT | Verified |
| Xoptfoil2 [S31] | XFOIL-in-the-loop airfoil optimizer | Gradient-free (PSO) around XFOIL | Practitioner-proven; MIT | Slow; XFOIL fragility | MIT | Verified licence |
| Ploé thesis [S40] | Surrogate-based hydrofoil shape optimization on RANS | GP + sequential acquisition | Direct hydrofoil precedent for EGO | Thesis; French abstract only opened | thesis | Verified abstract |
| Sacher 2018/2021 [S16][S20] | EGO with non-computable domains; non-nested MF infilling | GP + classifier | Handles solver failures in the loop | — | papers | Verified abstract (2018); title (2021) |
| Tannenberg et al. JST 2023 [S50] | VPP-driven AC75 foil design | Parametric study inside FS-Equilibrium | Whole-craft objective | Not open; AC75 only | paper | Verified abstract |
| Martins & Ning 2022 [S6] | Textbook | Decision tree; derivatives; surrogates; OUU; MDO | Open online; authoritative | Aircraft examples | book (read online free; licence not stated) | Verified |

## Reference information

- **Martins & Ning, *Engineering Design Optimization*, CUP 2022** [S6] — read online at mdobook.github.io; chapters 4–13 are the method reference. Requires of us: the decision tree as the tier→algorithm rule, KKT-based convergence reporting, complex-step/AD for derivatives, EGO for expensive tiers, multipoint as discrete OUU.
- **Drela 1998** [S1] — the exploitation pathology and multipoint rules; requires: points ≥ O(design variables), sample beyond the range, geometric regularity constraints, a posteriori smoothing reported as a deviation.
- **Garg et al. 2017/2019** [S2][S3] — the constraint formulations (A_cav smooth area, KS stress, TE thickness at 20 points, fixed LE), the weight table, and the validation numbers; requires: constraint-per-point "critical metric" and tunnel-grade evidence before "Experimentally compared".
- **Ng/Yildirim 2025** [S4][S5] — independent-method cross-check as a gate.
- **Bowers NASA/TP-2016-219072** [S14] and **Jones NACA TN 2249** [S15] — the two bending-moment-constrained minimum-induced-drag results; requires: the goal state names its structural proxy.
- **XFOIL doc** [S17] — MDES/QDES semantics, Ncrit table, convergence advice; **Eppler & Somers (NTRS 19790011865)** [S22] — multi-α inverse design.
- **NeuralFoil README + AeroSandbox tutorial** [S7][S8] — the concrete constraint set and confidence metric; **AeroSandbox source/pyproject** [S9][S10] — CasADi/IPOPT backend.
- **Kennedy & O'Hagan 2000, Han & Görtz 2012, Jones et al. 1998, Deb et al. 2002, Hansen 2016, Rockafellar & Uryasev 2000, Jameson 1988, Kulfan 2008, Hicks & Henne 1978, Sobieczky 1999, Poole et al. 2015, Bons et al. 2019, Li/Du/Martins 2022** [S41][S42][S37][S35][S34][S25][S44][S52][S53][S54][S28][S21][S55] — DOIs verified through Crossref; contents beyond title/abstract are Flagged unless quoted.
- **Licence files** [S13] — read from each repository's licence file via the GitHub API on 2026-09-20.

## Data, constants, formulae and invariants

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

## Design implications for CFD-Workbench

1. **Model the goal state now (A3, new value object `Goal state`)** *(Inferred)*: rider mass; board/foil/hand-wing/equipment masses with labels; water record id; depth band; wind band → speed band with source label; discipline/class-rule preset id; **operating-point set** (each: speed, depth, water, required load, critical metric, weight, Ncrit pair) ; **objective definition** (weighted-drag / weighted-L/D / inception speed / take-off margin, with the weights' basis); **constraint set** (typed rows: lift-equality per point, cavitation form + margin, TE thickness floor, thickness floors at x/c, TE angle floor, LE radius floor, curvature/wiggliness bound, class-rule box, CM bound, confidence floor, structural proxy placeholder); **robustness rule** (expected value | worst case | CVaR_α). It is authored in v1 as *intent* and evaluated by the analysis tier as a **feasibility report** (which constraints the current design satisfies at each point), which is valuable before any optimizer exists.
2. **Define the design vector as the explicit definition (A4/GEO)** *(Inferred)*: an ordered list of (curve id, control index, component, bounds) over the five channels plus (station id, profile CST index, bounds) — with `frozen` flags. This is the contract the optimizer will consume; the exporter and the geometry edit draft already consume the same controls, so no second geometry truth is created (file 02 implication 8 [S26]).
3. **Evaluation provenance (A3 Analysis run)** *(Inferred from A3 invariants and the sources)*: add `design_vector_hash`, `tier`, `method_version`, `ncrit`, `converged`, `analysis_confidence` (nullable, with "not recorded"), `wall_time_ms`, `optimizer_run_id`, `evaluation_index`. Optimizer run entity: algorithm, seed, restarts, iterations, evaluations, termination reason, KKT/feasibility residuals, non-computable count.
4. **COMMIT-01 ladder as data, not prose** *(Inferred)*: candidate status enum `surrogate-candidate → vlm-checked → cfd-checked → experimentally-compared`; promotion requires a re-evaluation run at the higher tier whose per-point results are stored and whose gains are compared against the lower tier (the Ng/Yildirim cross-check [S4]); the UI never shows "optimized" without the status chip.
5. **Recommended optimization architecture (tiers and gates)** *(Inferred; the only recommendation in this file that is not a source's statement)*:
   - **Tier 0 — feasibility & sensitivity** (estimator, µs): DOE/tornado over the design vector; no candidate output.
   - **Tier 1 — section multipoint** (NeuralFoil-class, ms): SLSQP/IPOPT with ≥ 5 multistarts, ≤ 18 CST variables per station, Ncrit pair, `analysis_confidence ≥ 0.9`, regularity rows; outputs "surrogate candidates". Gate: re-run the polar with XFOIL (process) at every operating point; a candidate whose XFOIL gain is < 50 % of its surrogate gain is flagged "exploited" (threshold is a placeholder to be calibrated — Flagged).
   - **Tier 2 — wing Pareto** (VLM + strip polars, ms–s): NSGA-II (≤ 12 planform/twist variables + section family index) or SLSQP on the weighted objective; constraints: lift per point, root/integrated BM proxy, class box, tip-twist bound; outputs a front labelled "vlm-checked".
   - **Tier 3 — CFD re-verification** (SU2/OpenFOAM process, minutes–hours): the ≤ 3 selected front points at the critical operating points (take-off and top-end); no design variables move at this tier in v1; later EGO/MFBO once GAP-06 correlation is measured.
   - **Every tier**: termination variant recorded; non-computable classifier active whenever XFOIL/CFD is in the loop; seeds fixed and stored.
6. **Structures before optimizer** (GAP-02): ship a beam-model proxy (EI(η), root stress, tip deflection/twist under the per-point load) or a *frozen* thickness distribution before Tier 1 is exposed; otherwise thickness collapses to the manufacturing floor (Finding 2/3 [S2][S3], GAP-07 [S49]).
7. **Section inverse design as an editing mode, not an optimizer** (Profile catalog context): target-Cp editing with XFOIL-QDES-like closure semantics, producing Profile revisions with provenance; this covers the single-point cases Drela says optimization handles badly [S1][S17].
8. **UI (G5 uncertainty-first)** *(Inferred)*: Pareto/trade-off plots with per-point tier chips and confidence colouring; constraint-activity matrix (points × constraints) with active/violated markers; "what protected this point" text from the critical-metric field; termination reason shown, never hidden; a candidate opens as a Geometry edit draft with the deviation-from-base readout — the same surface the user already edits.
9. **Tooling decisions (COMMIT-02)**: own C# SQP/NSGA-II/CMA-ES on Math.NET (MIT) for Tiers 0–2 in-process; optional Python sidecar (AeroSandbox/NeuralFoil/SciPy/pymoo/SMT) with licence review of CasADi (LGPL-3.0) and IPOPT (EPL-2.0) redistribution; SU2/OpenFOAM(+DAFoam) process-only; never link pyOptSparse, NLopt, ADflow, CasADi, Dakota [S13].
10. **Borrow / avoid**: borrow Garg's constraint-per-point table and A_cav form, NeuralFoil's confidence constraint and regularity rows, SU2's "adjoint type is a flag" separation, OpenAeroStruct's VLM+beam pairing; avoid FFD lattices as the record, weighted-sum-only fronts, single-Ncrit or single-point runs, "optimized" labels without a tier, and any solver whose licence forces a link.

### Contradicts existing repo knowledge

- **Research brief (this task's prompt): "algorithmic differentiation (JAX in AeroSandbox; CasADi)".** AeroSandbox 4.2.10 uses CasADi (LGPL-3.0) with IPOPT; JAX is not a dependency (`pyproject.toml`, `opti.py`) [S9][S10]. Consequence: any in-process reuse of AeroSandbox's AD is impossible under COMMIT-02 and irrelevant to .NET anyway; the sidecar's LGPL wheel distribution needs review.
- **`proposal-sequence.md` §3 (via file 06): "NeuralFoil (pure Python, MIT-ish)".** NeuralFoil 0.3.3 declares `aerosandbox>=4.2.7,<4.3.0` as a hard dependency, which pulls CasADi [S11][S10]; "pure NumPy at runtime" is true of the network, not of the installed package graph.
- No contradiction with file 04's goal-state numbers: re-executed and matched to rounding [S23].

## Open questions and domain failure modes

| Open question | Cheapest next probe |
|---|---|
| Jones 1950 optimum (15 %/15 %) vs the spike's family (+25–32 % span) — which admissible family and constraint did Jones use? | Download NTRS 19760012005 PDF, transcribe the load expression, add it to the spike as a fixture |
| Tier correlation estimator ↔ NeuralFoil ↔ VLM ↔ RANS on the catalog sections (GAP-06) | One 3-tier sweep on E817 at the seven goal-state points; compute the AR1 ρ per quantity |
| Exploitation threshold (surrogate gain that survives XFOIL/VLM) | Run Tier 1 on NACA 0012 → E817-like target; compare NeuralFoil vs XFOIL gains at Ncrit 2/4 |
| KS ρ and A_cav k in a 2D screening setting | Reproduce A_cav with a Cp distribution from XFOIL; test k = 10 sensitivity |
| Forrester/Sóbester/Keane (2008), NSGA-III (Deb & Jain 2014), Hansen tutorial contents, PROFOIL | Open the book chapters/papers; these are cited by DOI/title only |
| A permissive, maintained constrained-NLP library for .NET | Survey NuGet (e.g. Math.NET Numerics optimization namespace scope); else own SLSQP with SciPy as fixture oracle |
| Race wingfoil speed/time-on-task PDF for weights | GPS track of one race day with wind log |
| CVaR in aerodynamic OUU — precedent | Search Li/Padula-type robust airfoil papers for tail-risk objectives |
| Anhedral/dihedral near a free surface | NTRS: Wadlin & Christopher 1958 |

Failure modes: **silent** — an optimum on a parameterization bound or with confidence pinned at its floor (exploitation); a single-Ncrit polar giving a laminar bucket that does not exist in water; weights with no basis becoming "the objective"; a multistart that never ran. **Expensive** — CFD re-verification of a candidate that a 1 s XFOIL check would have rejected; optimizing thickness with no structural model. **Irreversible** — labelling a candidate "certified"/"cavitation-free" from surrogate evidence; a rider at 30+ kn on a foil optimized at one point.

## Disconfirming views sought

- **"Multipoint is enough; the exploitation problem is a 1998 artefact."** Counter-evidence: Drela's 6-point case still scalloped between points; NeuralFoil's freedom from the pathology is attributed by Drela to the *smaller design space*, not to multipoint alone [S1][S7]; Garg 2017 (RANS, 2017) still saw off-design degradation from single-point [S2]. Fared: the finding stands; multipoint plus a bounded design space plus regularity are jointly needed.
- **"Adjoint/CFD optimization is the state of the art; the workbench should go straight there."** Counter: Garg's tool chain is LGPL/proprietary and hours per evaluation; Ng/Yildirim needed an XFOIL cross-check; the book's tree puts expensive/noisy problems on surrogates [S2][S4][S6]. Fared: CFD is a re-verification tier in v1, an optimization tier later.
- **"Gradient-free is fine because NeuralFoil is fast."** Counter: 32,000 vs 206 calls at 30 variables [S6]; but at ms per call 10⁵ calls is minutes, so gradient-free *is* acceptable at Tier 1–2 for ≤ ~20 variables. Fared: partially — tier-dependent, stated as such.
- **"Bell loading should be the default planform objective."** Counter: which moment is fixed changes the optimum (22 %/11 % vs 15 %/15 %), and the spike showed the root-BM version is unbounded without a load-sign constraint [S14][S15]; the structural proxy for a wingfoil is unmeasured (GAP-02). Fared: bell loading is a *reference*, not a default.
- **"Multi-fidelity will save the CFD budget."** Counter: MFK needs nested, correlated data [S18]; no tier correlation exists for this domain (GAP-06). Fared: deferred until measured.
- **"AeroSandbox's AD makes Python the obvious optimizer host."** Counter: CasADi LGPL-3.0 and IPOPT EPL-2.0 are transitive; permissible only as a process sidecar [S9]–[S13]. Fared: sidecar, optional, licence-reviewed.

## Glossary terms

- **Multipoint optimization** — minimizing a weighted sum of an objective evaluated at several operating points; the discrete special case of optimization under uncertainty. *(Verified, [S6][S2])*
- **Exploitation (of a model)** — an optimizer improving the objective by manipulating features at the smallest scale the model resolves (bubble-filling bumps, shock zones), yielding gains that vanish off-design or under an independent method. *(Verified, [S1][S4])*
- **A_cav** — smoothed fraction of surface area where Cp < −σ, χ = 1/(1+e^{2k(Cp+σ)}), used as a differentiable cavitation constraint. *(Verified, [S2])*
- **KS aggregation** — Kreisselmeier–Steinhauser function giving a smooth conservative maximum of many constraints so one adjoint solve serves them all. *(Verified use, [S2]; formula Flagged)*
- **analysis_confidence** — NeuralFoil's self-assessed trust output, constrained > 0.90 in optimization to keep designs out of delicate/out-of-distribution regimes. *(Verified, [S7][S8])*
- **EGO / expected improvement** — Kriging-surrogate global optimization that samples where the expected improvement over the incumbent is largest. *(Verified, [S37][S6])*
- **Multi-fidelity Kriging (AR1 co-Kriging)** — y_hi(x) = ρ(x)·y_lo(x) + δ(x); SMT's implementation needs nested samples. *(Verified, [S18][S41])*
- **Knowledge gradient (multi-fidelity)** — acquisition valuing the expected improvement of the *posterior mean at target fidelity* per unit cost. *(Verified, [S19])*
- **Bell spanload** — Prandtl 1933 load Γ ∝ (1−x²)^{3/2}: minimum induced drag at fixed lift and fixed integrated bending moment; +22 % span, −11 % drag vs elliptic. *(Verified, [S14]; executed)*
- **CVaR_α** — conditional value-at-risk: mean of the worst (1−α) fraction of outcomes; a smooth tail-risk objective. *(Verified DOI, [S25]; content Flagged)*
- **Inverse design (MDES/QDES)** — specifying a target surface-speed distribution and solving for the shape, with closure modes added automatically. *(Verified, [S17])*
- **Non-computable point** — a design at which the evaluator fails (unphysical, ill-posed, solver crash); handled with a classifier in surrogate optimization. *(Verified, [S16])*
- **Re-verification ladder** — surrogate-candidate → vlm-checked → cfd-checked → experimentally-compared; promotion only by re-evaluation at the higher tier. *(Inferred from COMMIT-01/A6 [S24] and [S4])*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | Drela, M., "Pros & Cons of Airfoil Optimization", Frontiers of CFD 1998 (doi:10.1142/9789812815774_0019); PDF opened | primary | https://web.mit.edu/drela/OldFiles/Public/papers/Pros_Cons_Airfoil_Optimization.pdf | 2026-09-20 | exploitation pathology, multipoint rules |
| S2 | Garg, Kenway, Martins, Young, "High-fidelity multipoint hydrostructural optimization of a 3-D hydrofoil", JFS 2017 (doi:10.1016/j.jfluidstructs.2017.02.001); MDO Lab PDF opened | primary | https://websites.umich.edu/~mdolaboratory/pdf/Garg2017a.pdf | 2026-09-20 | constraints, weights, results |
| S3 | Garg, Pearce, Brandner, Phillips, Martins, Young, "Experimental investigation of a hydrofoil designed via hydrostructural optimization", JFS 2019 (doi:10.1016/j.jfluidstructs.2018.10.010); PDF opened | primary | https://websites.umich.edu/~mdolaboratory/pdf/Garg2019a.pdf | 2026-09-20 | validation numbers |
| S4 | Ng, Yildirim, Youngren, Lamkin, "Design Optimization of America's Cup AC75 Hydrofoil Sections with Flaps", J. Sailing Technology 2025 (doi:10.5957/jst/2025.10.1.50) — abstract via Crossref | primary (abstract) | https://doi.org/10.5957/jst/2025.10.1.50 | 2026-09-20 | min-pressure constraint, cross-check |
| S5 | Yildirim, Ng, Youngren, Lamkin, "CFD-Based Design Optimization of Subcavitating Hydrofoil Sections with Flaps", CSYS 2025 (doi:10.5957/csys-2025-006) — abstract | primary (abstract) | https://doi.org/10.5957/csys-2025-006 | 2026-09-20 | same study, conference form |
| S6 | Martins & Ning, *Engineering Design Optimization*, CUP 2022; PDF opened (642 pp.) | primary (book) | https://mdobook.github.io/ · https://flowlab.groups.et.byu.net/mdobook.pdf | 2026-09-20 | decision tree, scaling, OUU, multiobjective |
| S7 | NeuralFoil README (GitHub, main) | primary (docs) | https://github.com/peterdsharpe/NeuralFoil | 2026-09-20 | confidence, CST 18 params, XFOIL caveats, Drela quote |
| S8 | AeroSandbox tutorial "02 - NeuralFoil Optimization.ipynb" | primary (source) | https://github.com/peterdsharpe/AeroSandbox/tree/master/tutorial/06%20-%20Aerodynamics | 2026-09-20 | constraint set |
| S9 | AeroSandbox `aerosandbox/optimization/opti.py` | primary (source) | https://github.com/peterdsharpe/AeroSandbox | 2026-09-20 | CasADi/IPOPT backend |
| S10 | AeroSandbox `pyproject.toml` (4.2.10) | primary (source) | https://github.com/peterdsharpe/AeroSandbox/blob/master/pyproject.toml | 2026-09-20 | dependencies |
| S11 | NeuralFoil `pyproject.toml` (0.3.3) | primary (source) | https://github.com/peterdsharpe/NeuralFoil/blob/main/pyproject.toml | 2026-09-20 | aerosandbox dependency |
| S12 | CasADi and IPOPT repository licences (GitHub API) | primary (licence) | https://github.com/casadi/casadi · https://github.com/coin-or/Ipopt | 2026-09-20 | LGPL-3.0 / EPL-2.0 |
| S13 | GitHub API licence fields and licence files: OpenMDAO, MPhys, OpenAeroStruct, pyGeo, MACH-Aero, pymoo, SciPy, SMT, pycma, Optuna, BoTorch, Ax, AeroSandbox, NeuralFoil, Xoptfoil2, Math.NET Numerics, argmin, pyOptSparse, SU2, ADflow, Dakota, NLopt, DAFoam, JAX | primary (licence) | https://api.github.com/repos/{owner}/{repo}/license | 2026-09-20 | licence table |
| S14 | Bowers, Murillo, Jensen, Eslinger, Gelzer, "On Wings of the Minimum Induced Drag", NASA/TP-2016-219072; PDF opened | primary | https://ntrs.nasa.gov/api/citations/20160003578/downloads/20160003578.pdf | 2026-09-20 | Prandtl 1933 numbers and formula |
| S15 | Jones, R. T., NACA TN 2249 (1950) — NTRS abstract (id 19760012005) | primary (abstract) | https://ntrs.nasa.gov/citations/19760012005 | 2026-09-20 | 15 %/15 % result |
| S16 | Sacher, Duvigneau, Le Maître, Durand, "A classification approach to EGO in presence of non-computable domains", SMO 2018 — HAL abstract | primary (abstract) | https://inria.hal.science/hal-01877105v1 | 2026-09-20 | non-computable handling |
| S17 | XFOIL 6.9 User Guide (xfoil_doc.txt) | primary (docs) | https://web.mit.edu/drela/Public/web/xfoil/xfoil_doc.txt | 2026-09-20 | MDES/QDES, Ncrit, convergence |
| S18 | SMT documentation, Multi-Fidelity Kriging (MFK) | primary (docs) | https://smt.readthedocs.io/en/latest/_src_docs/applications/mfk.html | 2026-09-20 | AR1 form, nesting |
| S19 | BoTorch tutorial `discrete_multi_fidelity_bo.ipynb` (repository) | primary (source) | https://github.com/pytorch/botorch/tree/main/tutorials/discrete_multi_fidelity_bo | 2026-09-20 | MFKG, cost model |
| S20 | Sacher, Le Maître, Duvigneau, Hauville, "A non-nested infilling strategy for multifidelity based EGO", IJUQ 2021 (doi:10.1615/int.j.uncertaintyquantification.2020032982) | primary (title) | https://doi.org/10.1615/Int.J.UncertaintyQuantification.2020032982 | 2026-09-20 | non-nested MF |
| S21 | Bons, He, Mader, Martins, "Multimodality in Aerodynamic Wing Design Optimization", AIAA J. 2019 (doi:10.2514/1.J057294) | primary (title) | https://doi.org/10.2514/1.J057294 | 2026-09-20 | multimodality |
| S22 | Eppler & Somers, "Low speed airfoil design and analysis" (NTRS 19790011865, 1979); NASA TM-80210 not opened | primary (abstract) | https://ntrs.nasa.gov/citations/19790011865 | 2026-09-20 | multi-α inverse design |
| S23 | Area file 04, `04-hydrofoil-disciplines-and-design-data.md` (this repo) | secondary (repo) | docs/knowledge/hydrofoil-workbench/04-hydrofoil-disciplines-and-design-data.md | 2026-09-20 | goal-state points, σ, class rules, physics |
| S24 | CFD-Workbench specification `docs/specs/cfd-workbench.md` (A2 deferrals, COMMIT-01..04, A3, A6) | primary (repo) | docs/specs/cfd-workbench.md | 2026-09-20 | commitments and domain model |
| S25 | Rockafellar & Uryasev, "Optimization of conditional value-at-risk", J. Risk 2000 (doi:10.21314/jor.2000.038) | primary (title) | https://doi.org/10.21314/JOR.2000.038 | 2026-09-20 | CVaR |
| S26 | Area file 02, `02-parametric-curves-lofts-and-surfaces.md` (this repo) | secondary (repo) | docs/knowledge/hydrofoil-workbench/02-parametric-curves-lofts-and-surfaces.md | 2026-09-20 | parameterization facts, licences |
| S27 | SU2 tutorial "Unconstrained shape design of a transonic inviscid airfoil" | primary (docs) | https://su2code.github.io/tutorials/Inviscid_2D_Unconstrained_NACA0012/ | 2026-09-20 | adjoint flag, Hicks–Henne, SLSQP |
| S28 | Poole, Allen, Rendall, "Metric-Based Mathematical Derivation of Efficient Airfoil Design Variables", AIAA J. 2015 (doi:10.2514/1.J053427) | primary (title) | https://doi.org/10.2514/1.J053427 | 2026-09-20 | SVD modes |
| S29 | Chen, Chiu, Fuge, "Airfoil Design Parameterization and Optimization using Bézier GANs", arXiv:2006.12496 | primary (abstract) | https://arxiv.org/abs/2006.12496 | 2026-09-20 | generative parameterization |
| S30 | Chen & Ramamurthy, "Deep Generative Model for Efficient 3D Airfoil Parameterization", arXiv:2101.02744 | primary (abstract) | https://arxiv.org/abs/2101.02744 | 2026-09-20 | FFD-GAN |
| S31 | Xoptfoil2 repository (licence via GitHub API) | primary (licence) | https://github.com/jxjo/Xoptfoil2 | 2026-09-20 | XFOIL-in-loop comparable |
| S32 | OpenAeroStruct README | primary (docs) | https://github.com/mdolab/OpenAeroStruct | 2026-09-20 | VLM + spatial beam |
| S33 | pymoo algorithm list | primary (docs) | https://pymoo.org/algorithms/list.html | 2026-09-20 | algorithms |
| S34 | Hansen, "The CMA Evolution Strategy: A Tutorial", arXiv:1604.00772 | primary (abstract) | https://arxiv.org/abs/1604.00772 | 2026-09-20 | CMA-ES |
| S35 | Deb, Pratap, Agarwal, Meyarivan, "A fast and elitist multiobjective genetic algorithm: NSGA-II", IEEE TEC 2002 (doi:10.1109/4235.996017) | primary (title) | https://doi.org/10.1109/4235.996017 | 2026-09-20 | NSGA-II |
| S36 | Optuna samplers package listing (repository) | primary (source) | https://github.com/optuna/optuna/tree/master/optuna/samplers | 2026-09-20 | sampler set |
| S37 | Jones, Schonlau, Welch, "Efficient Global Optimization of Expensive Black-Box Functions", JGO 1998 (doi:10.1023/A:1008306431147) | primary (title) | https://doi.org/10.1023/A:1008306431147 | 2026-09-20 | EGO |
| S38 | Forrester, Sóbester, Keane, *Engineering Design via Surrogate Modelling*, Wiley 2008 — not opened | secondary (recall) | — | — | surrogate text (Flagged) |
| S39 | Bouhlel et al., "A Python surrogate modeling framework with derivatives", AES 2019 (doi:10.1016/j.advengsoft.2019.03.005) | primary (title) | https://doi.org/10.1016/j.advengsoft.2019.03.005 | 2026-09-20 | SMT |
| S40 | Ploé, P., "Surrogate-based optimization of hydrofoil shapes using RANS simulations", thesis (Crossref abstract, French) | primary (abstract) | https://doi.org/10.70675/3c2f8530zc92ez4593z8cb0zb2f0855d6e75 | 2026-09-20 | hydrofoil EGO precedent |
| S41 | Kennedy & O'Hagan, "Predicting the output from a complex computer code when fast approximations are available", Biometrika 2000 (doi:10.1093/biomet/87.1.1) | primary (title) | https://doi.org/10.1093/biomet/87.1.1 | 2026-09-20 | co-Kriging |
| S42 | Han & Görtz, "Hierarchical Kriging Model for Variable-Fidelity Surrogate Modeling", AIAA J. 2012 (doi:10.2514/1.J051354) | primary (title) | https://doi.org/10.2514/1.J051354 | 2026-09-20 | hierarchical Kriging |
| S43 | Toal, "Applications of multi-fidelity multi-output Kriging to engineering design optimization", SMO 2023 (doi:10.1007/s00158-023-03567-z) — abstract | primary (abstract) | https://doi.org/10.1007/s00158-023-03567-z | 2026-09-20 | MF assessment |
| S44 | Jameson, "Aerodynamic design via control theory", J. Sci. Comput. 1988 (doi:10.1007/BF01061285) | primary (title) | https://doi.org/10.1007/BF01061285 | 2026-09-20 | adjoint origin |
| S45 | DAFoam site and LICENSE.md (GPL-3.0) | primary (docs/licence) | https://dafoam.github.io/ · https://github.com/mdolab/dafoam | 2026-09-20 | capability, licence |
| S46 | pyOptSparse README and LICENSE (LGPL-3.0) | primary (docs/licence) | https://github.com/mdolab/pyoptsparse | 2026-09-20 | optimizer list |
| S47 | OpenMDAO `openmdao/drivers` package listing and LICENSE.txt (Apache-2.0) | primary (source) | https://github.com/OpenMDAO/OpenMDAO/tree/master/openmdao/drivers | 2026-09-20 | drivers |
| S48 | Area file 06, `06-foil-section-catalog.md` (this repo) | secondary (repo) | docs/knowledge/hydrofoil-workbench/06-foil-section-catalog.md | 2026-09-20 | sections, Ncrit, cavitation screening |
| S49 | Bench gap register `bench-gap-register.md.txt` (GAP-02, GAP-06, GAP-07) | secondary (repo) | docs/knowledge/sources/bench-gap-register.md.txt | 2026-09-20 | structures, MF, manufacturing gaps |
| S50 | Tannenberg, Turnock, Hochkirch, Boyd, "VPP Driven Parametric Design of AC75 Hydrofoils", JST 2023 (doi:10.5957/jst/2023.8.9.161) — abstract | primary (abstract) | https://doi.org/10.5957/jst/2023.8.9.161 | 2026-09-20 | whole-craft objective |
| S51 | Proposal sequence `proposal-sequence.md.txt` (§ licence, S6 SU2 process-first, COMMIT-01 note) | secondary (repo) | docs/knowledge/sources/proposal-sequence.md.txt | 2026-09-20 | process-isolation precedent |
| S52 | Kulfan, "Universal Parametric Geometry Representation Method", J. Aircraft 2008 (doi:10.2514/1.29958) | primary (title) | https://doi.org/10.2514/1.29958 | 2026-09-20 | CST |
| S53 | Hicks & Henne, "Wing Design by Numerical Optimization", J. Aircraft 1978 (doi:10.2514/3.58379) | primary (title) | https://doi.org/10.2514/3.58379 | 2026-09-20 | bump functions |
| S54 | Sobieczky, "Parametric Airfoils and Wings", NNFM 1999 (doi:10.1007/978-3-322-89952-1_4) | primary (title) | https://doi.org/10.1007/978-3-322-89952-1_4 | 2026-09-20 | PARSEC |
| S55 | Li, Du, Martins, "Machine learning in aerodynamic shape optimization", Prog. Aerosp. Sci. 2022 (doi:10.1016/j.paerosci.2022.100849) | primary (title) | https://doi.org/10.1016/j.paerosci.2022.100849 | 2026-09-20 | ML/ASO review |
| S56 | Spike (executed 2026-09-20, then disposed): lifting-line Fourier series, SciPy 1.x SLSQP, constraints lift + one bending-moment integral + non-negative load; formulation in the Data section | primary (executed) | — (re-creatable from the Data section) | 2026-09-20 | Prandtl/Jones reproduction; goal-state arithmetic re-check |
