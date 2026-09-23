---
id: kb-hw-glossary
title: "Hydrofoil workbench glossary"
type: glossary
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [glossary, hydrofoil, generated]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
review-by: 2026-12-19
summary: >-
  The ubiquitous language of hydrofoil design, analysis, simulation and optimization as used by CFD-Workbench, merged alphabetically from every area file. A term defined by more than one area lists every definition so a conflict is visible rather than silently resolved.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
---

# Hydrofoil workbench glossary

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

200 terms. A term with more than one definition shows each, labeled by its source area; reconcile in the area files, not here.

## "A" modification (6A-series)

- 6-series thickness form with straight rear surfaces from ≈0.8c to a thicker TE. *(Inferred, [S16])* *— from `kb-hw-foil-section-catalog`*

## 16-series

- NACA 4-digit-modified thickness form with LE index 4 and maximum thickness at 0.5c; designation 16-*d*ₗ*tt*. *(Verified, [S17])* *— from `kb-hw-foil-section-catalog`*

## `.eMesh`

- feature-edge mesh produced by `surfaceFeatureExtract`, referenced in `castellatedMeshControls.features`. *(Verified reference, [S12]; utility name Flagged.)* *— from `kb-hw-simulation-openfoam-su2-interop`*

## `MARKER_*`

- SU2 boundary-condition assignment by mesh marker tag. *(Verified, [S23])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## A_cav

- smoothed fraction of surface area where Cp < −σ, χ = 1/(1+e^{2k(Cp+σ)}), used as a differentiable cavitation constraint. *(Verified, [S2])* *— from `kb-hw-optimization-strategies`*

## AFP (automated fibre placement)

- robotic tow placement enabling curvilinear fibre paths for tailored bend-twist foils. *(Verified, [S27][S28])* *— from `kb-hw-structures-materials-and-manufacturing`*

## analysis_confidence

- NeuralFoil's self-assessed trust output, constrained > 0.90 in optimization to keep designs out of delicate/out-of-distribution regimes. *(Verified, [S7][S8])* *— from `kb-hw-optimization-strategies`*

## analysis_confidence (NeuralFoil)

- Classifier of XFoil convergence with a Mahalanobis-distance penalty so it *— from `kb-hw-integration-and-ai-workflows`*

## Approximation/Model Management Optimization (AMMO)

- NASA (Alexandrov & Lewis) framework using corrected *— from `kb-hw-integration-and-ai-workflows`*

## Artefact oracle

- the workbench's completion check based on output files, independent of exit code. *(Inferred, this file.)* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Aspect ratio (AR)

- b²/S with b the full projected span and S the projected planform area; makers' stated values may use other conventions. *(Verified convention, Inferred deviations, [S2][S3][S31])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## B-lofted / C-lofted / X-lofted / Arc-lofted / Foil-lofted / Ruled

- MultiSurf lofted surfaces whose lofting curve is a B-spline / interpolating spline / explicit spline / arc / NACA foil / line through the master-curve points. *(Verified, [S9])* *— from `kb-hw-marine-and-board-cad-tooling`*

## B-spline (degree p)

- piecewise polynomial curve C(u)=ΣN_{i,p}(u)P_i over a non-decreasing knot vector; C^{p−s} at a knot of multiplicity s; local support of p+1 spans per control point. *(Inferred, [S1])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## B_SPLINE_SURFACE_WITH_KNOTS

- STEP entity: B-spline surface with explicit distinct knots and multiplicities in u and v; rational form via the `RATIONAL_B_SPLINE_SURFACE` complex entity. *(Verified, [S8])* *— from `kb-hw-file-formats-and-grammars`*

## Base-vented section

- a blunt-TE strut/foil section whose base is deliberately ventilated to atmosphere to stabilise the wake. *(Verified description, [S33][S20])* *— from `kb-hw-foil-section-catalog`*

## Bell spanload

- Prandtl 1933 load Γ ∝ (1−x²)^{3/2}: minimum induced drag at fixed lift and fixed integrated bending moment; +22 % span, −11 % drag vs elliptic. *(Verified, [S14]; executed)* *— from `kb-hw-optimization-strategies`*

## Bend-twist coupling (K)

- off-diagonal beam stiffness linking curvature and twist rate, created by unbalanced off-axis plies; sign set by fibre angle relative to the elastic axis. *(Verified effect [S1][S2]; form Flagged)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Blend (OpenVSP)

- the rule for LE/TE sweep and dihedral continuity between adjacent wing sections (`BLEND_FREE`, `BLEND_ANGLES`, match trapezoids/angles). *(Verified, [S14])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Body plan / half-breadth plan / sheer plan

- the three lines-plan views: transverse sections at stations; waterlines; profile and buttocks. *(Verified, [S25])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Box rule

- class rule fixing equipment minima/maxima and production status rather than a one-design. *(Verified, [S12])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Bredt–Batho torsion

- closed thin-walled section torsion constant J = 4A_m²/∮(ds/t); requires a closed cell (bonded TE). *(Flagged — standard, not re-opened)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Bypass transition

- transition that skips linear T–S growth because of roughness or freestream turbulence; outside XFOIL's e^N model. *(Verified, [S16])* *— from `kb-hw-structures-materials-and-manufacturing`*

## C^k / G^k continuity

- parametric derivative agreement to order k / agreement up to reparameterization (G1 tangent direction, G2 curvature vector, G3 curvature rate). *(Inferred, [S15][S30])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Cavitation bucket

- the region in the (Cl, σ) plane where −Cp_min < σ; its width is the α/Cl tolerance and its depth the speed tolerance. *(Verified, [S5][S22])* *— from `kb-hw-foil-section-catalog`*
- the curve of σ_required = −Cp_min against C_l (or α) for one section at one Re and Ncrit; the operating σ must lie above it. *(Inferred, [S28])* *— from `kb-hw-low-order-hydrodynamics`*
- the region of (Cl, σ) where σ_required = −Cp_min < σ; width = α tolerance, depth = speed tolerance. *(Inferred; proposal [S53])* *— from `kb-hw-visualization`*

## Cavitation number (σ)

- (p_∞ − p_v)/(½ρV²) at foil depth; inception when −Cp_min ≥ σ. *(Verified definition; values [S1])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Cavitation number σ

- (p∞ − p_v)/(½ρV²) with p∞ including hydrostatic depth. *(Verified, [S22])* *— from `kb-hw-foil-section-catalog`*
- (p_∞ − p_v)/(½ ρ V²), with p_∞ including the depth head ρ g h; dimensionless. *(Verified, [S5])* *— from `kb-hw-low-order-hydrodynamics`*
- (p∞ − p_v)/(½ρV²); drives `interPhaseChangeFoam` mass-transfer models Kunz/Merkle/SchnerrSauer. *(Model names Verified, [S10]; definition standard.)* *— from `kb-hw-simulation-openfoam-su2-interop`*
- (p_∞ − p_v)/(½ρV²) with p_∞ including hydrostatic depth. *(Standard; Inferred *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Centerline (Fusion Loft)

- a rail to which sections are held normal; a suggestion, not a shape definition. *(Inferred, [S19])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Chord Froude number Fr_c

- V/√(gc); governs 2D wave drag. *(Inferred, [S2])* *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Chordal deviation

- max distance between a curve/surface and its polyline/polygon approximation; ε=R(1−cos(Δθ/2)). *(Inferred)* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Clamped knot vector

- end knots of multiplicity p+1, so the curve starts/ends at its first/last control points tangent to the polygon. *(Inferred, [S1])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Closure / Trim (OpenVSP)

- trailing-edge closure `NONE/SKEWLOW/SKEWUP/SKEWBOTH/EXTRAP`; trimming by x or thickness. *(Verified, [S14])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Co-Kriging (Kennedy–O'Hagan)

- Autoregressive multifidelity Gaussian process: `q_hi = ρ·q_lo + δ(x)` with a *— from `kb-hw-integration-and-ai-workflows`*

## Comparison error E

- E = D − S, data minus simulation; the combination of all data and simulation *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Computed estimate

- the A6 label for every low-order result in this file: method, envelope, inputs, "model uncertainty not quantified". *(Verified, spec A6)* *— from `kb-hw-low-order-hydrodynamics`*

## Content hash / run_key

- Digest of the canonical inputs, method identity and version that identifies a result; *— from `kb-hw-integration-and-ai-workflows`*

## Control point (CV)

- A coefficient of a NURBS basis function; need not lie on the curve; moving it changes only a local region. *(Verified, [S1][S14])* *— from `kb-hw-cad-programs-and-ux`*

## Cp (pressure coefficient)

- (p − p∞)/(½ρV∞²), dimensionless; 1 at stagnation (incompressible); plotted more-negative-up. *(Verified, [S2])* *— from `kb-hw-visualization`*

## Critical roughness Reynolds number Re_k,t

- u_k·k/ν at the particle top at which turbulent spots form; 250–600 experimentally, 600 as the working value. *(Verified, [S17])* *— from `kb-hw-structures-materials-and-manufacturing`*

## Critical speed V_crit

- the speed at which σ = −Cp_min at the given depth; the sheet-cavitation screening limit, not an inception prediction. *(Inferred)* *— from `kb-hw-low-order-hydrodynamics`*

## CST (class–shape transformation)

- ζ=ψ^{N1}(1−ψ)^{N2}·Σ A_i B_{i,n}(ψ)+ψζ_TE; N1=0.5, N2=1 for round-LE/sharp-TE foils; linear in A_i. *(Verified, [S5])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## CST (Kulfan) parameter set

- class exponents N1, N2, upper/lower Bernstein weights, LE modifier weight, TE thickness, plus the sampling law. *(Verified, [S5])* *— from `kb-hw-file-formats-and-grammars`*

## Curvature comb

- Teeth drawn along the curve normal with length proportional to curvature; Onshape evaluates it at evenly spaced isolines and uses it for continuity up to G3. *(Verified, [S28])* *— from `kb-hw-cad-programs-and-ux`*
- the curve offset by a scaled curvature along the normal; parallel-but-unequal teeth = G1 only; continuous = G2. *(Verified, [S30])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## CVaR_α

- conditional value-at-risk: mean of the worst (1−α) fraction of outcomes; a smooth tail-risk objective. *(Verified DOI, [S25]; content Flagged)* *— from `kb-hw-optimization-strategies`*

## CVD (colour-vision deficiency)

- ≈ 8 % of men, 0.5 % of women worldwide. *(Verified, [S5])* *— from `kb-hw-visualization`*

## Decalage

- incidence difference between front wing and stabilizer; iQFOiL exposes it as a −2° to +1° shim. *(Verified, [S13])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Deep-water assumption

- h/c ≥ 5, where free-surface effects are within the test's asymptote. *(Verified, [S16])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Degenerate edge

- a surface boundary where all control points coincide (zero-chord tip); legal, but normals must be taken as limits. *(Inferred)* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Degrees of freedom (sketch)

- Count of free scalar parameters after constraints; SolveSpace displays it and reports overconstraint with a removable-constraint list. *(Verified, [S39])* *— from `kb-hw-cad-programs-and-ux`*

## Depth Froude number Fr_h

- V/√(g h); governs which free-surface limit (rigid wall at Fr_h → 0, constant-pressure at Fr_h → ∞) the foil is nearer. *(Verified usage, [S6][S7])* *— from `kb-hw-low-order-hydrodynamics`*
- V/√(gh); selects the free-surface image sign (anti-symmetric at high Fr_h). *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Design Cl

- the lift coefficient the section's mean line was designed for (NACA: encoded in the designation); distinct from the solved operating Cl. *(Verified, [S16])* *— from `kb-hw-foil-section-catalog`*

## Design-rule check (DRC)

- Rule-based validation with per-rule severity, per-violation exclusion and navigable *— from `kb-hw-integration-and-ai-workflows`*

## Discrepancy record

- Stored `δ = q_hi − q_lo` and `ρ = q_hi/q_lo` between two runs on identical inputs, with *— from `kb-hw-integration-and-ai-workflows`*

## Diverging colour map

- two hues about a light centre pinned to a physical central value; only for data divergent about that value. *(Verified, [S5][S6])* *— from `kb-hw-visualization`*

## Driver (OpenVSP)

- a wing-section parameter chosen to drive the section; others are derived. *(Verified, [S14])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Edit point / fit point

- A point on the curve (Rhino: evaluated at knot averages); moving it generally changes the whole curve. *(Verified, [S1])* *— from `kb-hw-cad-programs-and-ux`*

## Effective incidence

- geometric incidence plus generalised tip twist; collapses load coefficients of rigid and flexible foils onto one curve. *(Verified, [S2])* *— from `kb-hw-structures-materials-and-manufacturing`*

## EGO / expected improvement

- Kriging-surrogate global optimization that samples where the expected improvement over the incumbent is largest. *(Verified, [S37][S6])* *— from `kb-hw-optimization-strategies`*

## Elastic axis

- the spanwise locus of shear centres; a transverse load through it bends without twisting. Ng et al. place it at midchord for their T-foil. *(Verified, [S1])* *— from `kb-hw-structures-materials-and-manufacturing`*

## Embarrassingly parallel sweep

- A sweep whose (V, α) samples are independent jobs with no shared state. *— from `kb-hw-integration-and-ai-workflows`*

## Equivalent sand-grain roughness k_s

- the Nikuradse sand-grain height with the same friction effect as a real finish; ≈ (2–5)×Ra for random surfaces. *(Flagged)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Evenly spaced streamlines

- placement with a minimum separating distance (Jobard & Lefer). *(Citation Verified, [S32])* *— from `kb-hw-visualization`*

## Exploitation (of a model)

- an optimizer improving the objective by manipulating features at the smallest scale the model resolves (bubble-filling bumps, shock zones), yielding gains that vanish off-design or under an independent method. *(Verified, [S1][S4])* *— from `kb-hw-optimization-strategies`*

## Fair (Rhino command)

- Removes large curvature variations within a stated tolerance with PreserveEnds Position/Tangency/Curvature; best on degree 3. *(Verified, [S2])* *— from `kb-hw-cad-programs-and-ux`*

## Fair curve

- A curve whose curvature plot is continuous and consists of relatively few monotone pieces (Farin and Sapidis). *(Verified, [S58])* *— from `kb-hw-cad-programs-and-ux`*

## Fair curve (Farin–Sapidis)

- curvature plot with relatively few monotone pieces. *(Flagged, [S27])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Farin–Sapidis fairness

- a curve is fair if its curvature plot has few monotone pieces; local fairing by knot removal/reinsertion. *(Verified, [S11])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Filtering / fusion / adaptation

- Peherstorfer et al.'s three multifidelity model-management strategies. *— from `kb-hw-integration-and-ai-workflows`*

## Free-surface image

- the mirror vortex used to satisfy the linearised surface condition; same-sign at infinite Fr_h (lift loss), opposite-sign at zero Fr_h (ground effect). *(Inferred, [S20] Flagged)* *— from `kb-hw-low-order-hydrodynamics`*

## Froude number based on submergence (Fr_h)

- V/√(g h); governs free-surface lift loss; ≈5 in kite/wind racing. *(Verified, [S16])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Function object

- an OpenFOAM run-time post-processing plugin (e.g. `forces`) writing to `<timeDir>/…dat`. *(Verified, [S32])* *— from `kb-hw-file-formats-and-grammars`*
- OpenFOAM run-time post-processing plug-in configured in `controlDict.functions`; writes `postProcessing/<name>/<time>/*.dat`. *(Verified, [S13][S15])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Ghost board / guideline

- a passive overlay or target marker that informs but never constrains. *(Verified, [S1][S5][S12])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Golden master / approval test

- regression comparison against a stored, provenance-carrying reference *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Gordon surface / Coons patch

- surface interpolating two intersecting curve families / blending four boundary curves. *(Flagged, recall)* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Grid Convergence Index (GCI)

- F_S|ε|/(r^p − 1), a Richardson-based band on discretisation error with *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Helmbold equation

- C_Lα = 2π AR/(2 + √(AR² + 4)); low-aspect-ratio lift slope. *(Inferred, [S27])* *— from `kb-hw-low-order-hydrodynamics`*

## Hicks–Henne bump

- sin^t(πx^{ln0.5/ln x_i}) perturbation of a baseline, peaking at x_i. *(Verified, [S26])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## HOPs (hypothetical outcome plots)

- animated draws from an outcome distribution; outperform error bars for ordering judgements; need an ensemble. *(Verified, [S44])* *— from `kb-hw-visualization`*

## Illuminated streamlines

- line lighting with tangent-based normals for depth cues (Zöckler, Stalling & Hege). *(Citation Verified, [S33])* *— from `kb-hw-visualization`*

## Image digest

- content hash (`@sha256:…`) identifying one exact container image; tags move, digests do not. *(Verified that `latest` moved, [S26])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Inception number σ_i

- the σ at which cavitation first appears; ≤ −Cp_min in nuclei-poor water, can *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Incremental computation (query memoization)

- Model in which pure derived values are memoized with tracked *— from `kb-hw-integration-and-ai-workflows`*

## Induced angle α_i

- the downwash angle w/V at a spanwise station; α_eff = α_geo − α_i. *(Inferred, [S38])* *— from `kb-hw-low-order-hydrodynamics`*

## Inverse design (MDES/QDES)

- specifying a target surface-speed distribution and solving for the shape, with closure modes added automatically. *(Verified, [S17])* *— from `kb-hw-optimization-strategies`*

## Inviscid vs viscous Cp

- XFOIL's dashed inviscid curve at the same α vs the solid viscous solution; the gap is the boundary-layer displacement effect. *(Verified, [S1])* *— from `kb-hw-visualization`*

## ISolverBackend façade

- Proposal's single interface over estimator/VLM/CFD tiers; must expose capability flags *— from `kb-hw-integration-and-ai-workflows`*

## JCS (RFC 8785)

- JSON Canonicalization Scheme: sorted keys by UTF-16 code units, ECMAScript number serialisation, no whitespace; the input to a content hash. *(Verified, [S21])* *— from `kb-hw-file-formats-and-grammars`*

## kkLOmega

- Walters–Cokljat laminar-kinetic-energy transition model; incompressible-only in OpenFOAM ESI. *(Verified location, [S9]; author attribution Flagged.)* *— from `kb-hw-simulation-openfoam-su2-interop`*

## KKT system

- the saddle-point linear system that solves an equality-constrained least-squares problem; infeasible constraints appear as rank/consistency failures. *(Inferred)* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Knot insertion (Boehm)

- adding a knot without changing the curve; exact to round-off. *(Verified, [S29])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Knowledge gradient (multi-fidelity)

- acquisition valuing the expected improvement of the *posterior mean at target fidelity* per unit cost. *(Verified, [S19])* *— from `kb-hw-optimization-strategies`*

## KS aggregation

- Kreisselmeier–Steinhauser function giving a smooth conservative maximum of many constraints so one adjoint solve serves them all. *(Verified use, [S2]; formula Flagged)* *— from `kb-hw-optimization-strategies`*

## Laminar (drag) bucket

- the Cl range over which a laminar-flow section keeps low drag; for 6-series, centred on the design Cl with half-width given by the subscript. *(Inferred, [S16][S27])* *— from `kb-hw-foil-section-catalog`*

## Lednicer format

- airfoil `.dat` layout: name line, a count line `NU. NL.`, blank, upper surface LE→TE, blank, lower surface LE→TE. *(Verified header/first block, Inferred second block, [S3])* *— from `kb-hw-file-formats-and-grammars`*

## LIC (line integral convolution)

- noise convolved along streamlines to produce a dense direction image (Cabral & Leedom). *(Citation Verified, [S31])* *— from `kb-hw-visualization`*

## Lift area / drag area

- L/(½ρV²), D/(½ρV²): force divided by dynamic pressure, used when reference area *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Loose / Normal / Tight / Straight sections / Uniform (Rhino Loft)

- loft styles governing how the surface follows input curve control points. *(Verified, [S16])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Magnet / Snake

- a point / a curve constrained to lie on a surface, giving durable joins. *(Verified, [S9])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Master curve

- an authored 2D curve (outline, rocker, thickness; for a foil, a spanwise distribution) from which the surface is derived; in MultiSurf, the curves a lofted surface interpolates through. *(Verified, [S1][S9])* *— from `kb-hw-marine-and-board-cad-tooling`*

## MCP tool annotations

- `readOnlyHint`, `destructiveHint` etc.; hints that clients MUST treat as untrusted. *— from `kb-hw-integration-and-ai-workflows`*

## MEC / MVC

- minimum energy curve (∫κ²) / minimum variation curve (∫(dκ/ds)²); MVC is fairer and circle-preserving. *(Verified, [S12])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Metamorphic relation

- a necessary property between outputs of related inputs (scale, mirror, unit *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Method of manufactured solutions (MMS)

- code verification by inserting an analytic solution through a *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Modal operator

- Blender's interactive command that previews until confirmed (LMB/Return) or cancelled (RMB/Esc); a cancel creates no undo step. *(Verified, [S32])* *— from `kb-hw-cad-programs-and-ux`*

## Multi-fidelity Kriging (AR1 co-Kriging)

- y_hi(x) = ρ(x)·y_lo(x) + δ(x); SMT's implementation needs nested samples. *(Verified, [S18][S41])* *— from `kb-hw-optimization-strategies`*

## Multipoint optimization

- minimizing a weighted sum of an objective evaluated at several operating points; the discrete special case of optimization under uncertainty. *(Verified, [S6][S2])* *— from `kb-hw-optimization-strategies`*

## NativeMenu (Avalonia)

- Control that builds the macOS application menu bar and is ignored on other platforms; NativeMenuBar renders the same items in-window elsewhere. *(Verified, [S49])* *— from `kb-hw-cad-programs-and-ux`*

## Navigation preset

- A named orbit/pan/zoom mapping copied from another product (Fusion, Onshape). *(Verified, [S16][S27])* *— from `kb-hw-cad-programs-and-ux`*

## Ncrit

- the e^N amplification exponent at which XFOIL/NeuralFoil declare transition; lower means earlier transition; related to turbulence intensity by Mack's correlation. *(Verified use, [S11][S12]; formula Flagged)* *— from `kb-hw-foil-section-catalog`*
- the e^N amplification exponent at which XFOIL/NeuralFoil declare transition; a facility/water-quality input, not a fluid property. *(Verified, [S1][S3])* *— from `kb-hw-low-order-hydrodynamics`*
- log of the amplification factor of the most-amplified frequency that triggers transition in the eⁿ method. *(Verified, [S1])* *— from `kb-hw-visualization`*

## Ncrit = 4

- the e^N amplification used by Day et al. for tank/sailing water ("around 0.6 %" turbulence); *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Non-computable point

- a design at which the evaluator fails (unphysical, ill-posed, solver crash); handled with a classifier in surrogate optimization. *(Verified, [S16])* *— from `kb-hw-optimization-strategies`*

## Not assessed

- the spec's mandatory readout state when a policy, layup or load case is absent (GEO-12). *(Verified spec wording)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Nudge keys

- Rhino's Alt+arrow moves with three configurable distances (plain, Ctrl, Shift). *(Verified, [S8])* *— from `kb-hw-cad-programs-and-ux`*

## Numeral check

- Product rule: every numeral in assistant prose must exist in the packed context. *(Internal, *— from `kb-hw-integration-and-ai-workflows`*

## One-design

- every competitor uses identical registered equipment (iQFOiL). *(Verified, [S13])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Oracle problem

- the absence of an independent way to decide whether a scientific program's output is *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Out-of-distribution (OOD)

- Inputs outside the surrogate's training support (geometry, Re, α); the dominant *— from `kb-hw-integration-and-ai-workflows`*

## Parametric sweep replay

- a Sequence over admitted samples; one frame = one case; no interpolation. *(Verified spec, [S51])* *— from `kb-hw-visualization`*

## PARSEC

- 11-parameter airfoil (LE radius, crest positions/heights/curvatures, TE angle/wedge/thickness/offset) with y=Σa_nx^{n−1/2}. *(Verified, [S27])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Parting line / pull direction / draft

- the curve splitting a two-part mould, the direction the part leaves the mould, and the wall angle from that direction that lets it release. Injection moulding recommends ≥ 2°. *(Verified for injection [S19]; composite practice Flagged)* *— from `kb-hw-structures-materials-and-manufacturing`*

## PCHIP / CEDIT

- piecewise cubic Hermite interpolating polynomial through points; cubic Bézier control editing. *(Verified, [S14])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Per-monitor DPI awareness v2 (PMv2)

- Windows mode in which the app receives WM_DPICHANGED and re-lays out instead of being bitmap-stretched. *(Verified, [S48])* *— from `kb-hw-cad-programs-and-ux`*

## Perceptually uniform colour map

- equal data steps produce equal perceived colour steps; lightness monotonic for sequential maps. *(Verified, [S5][S7])* *— from `kb-hw-visualization`*

## Physical fidelity (ChatCFD)

- Metric of whether an executed CFD case is scientifically meaningful, distinct from *— from `kb-hw-integration-and-ai-workflows`*

## Physical group / marker / patch

- the boundary-naming concept of Gmsh / SU2 / OpenFOAM respectively; the hook by which boundary conditions attach to mesh faces. *(Verified, [S12][S13][S15])* *— from `kb-hw-file-formats-and-grammars`*

## polyMesh

- OpenFOAM's face-addressed mesh directory: `points`, `faces`, `owner`, `neighbour`, `boundary`. *(Verified, [S15])* *— from `kb-hw-file-formats-and-grammars`*

## PROV-DM

- W3C provenance data model: Entity, Activity, Agent and relations `used`, `wasGeneratedBy`, *— from `kb-hw-integration-and-ai-workflows`*

## Provenance strip

- the per-view record of run, sample, variable, units, range basis, association, physical basis, method tier, colormap and evidence label. *(Inferred; VIZ-01)* *— from `kb-hw-visualization`*

## Q criterion

- Q = ½(‖Ω‖² − ‖S‖²) > 0; vortex-core indicator (Hunt, Wray & Moin). *(Flagged citation, [S37])* *— from `kb-hw-visualization`*

## Quadric decimation

- triangle reduction by quadric error metrics; `TargetReduction` fraction; topology not guaranteed. *(Verified, [S49])* *— from `kb-hw-visualization`*

## RANS

- Reynolds-averaged Navier–Stokes: mean-flow equations with a turbulence model closing the Reynolds stresses; steady RANS produces a mean field, never resolved eddies. *(Verified, [S33])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Re-entrant jet

- the upstream flow at cavity closure whose angle governs washout/elimination of a *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Re-verification ladder

- surrogate-candidate → vlm-checked → cfd-checked → experimentally-compared; promotion only by re-evaluation at the higher tier. *(Inferred from COMMIT-01/A6 [S24] and [S4])* *— from `kb-hw-optimization-strategies`*

## Rebuild

- Re-approximate a curve with a chosen degree and control count and evenly spaced knots. *(Verified, [S3])* *— from `kb-hw-cad-programs-and-ux`*

## Rebuild / Refit (Rhino Loft)

- rebuild section curves to N control points / refit to tolerance before lofting. *(Verified, [S16])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Recipe (authoritative flag)

- generative parameters that emit stations; authoritative until the first manual edit, then provenance only. *(Verified repo intent, [S42])* *— from `kb-hw-file-formats-and-grammars`*

## Record History

- Rhino's opt-in link between a command's inputs and its result so the result updates when inputs change. *(Verified, [S5])* *— from `kb-hw-cad-programs-and-ux`*

## Reduced artifact

- a surface, slice, streamline set or isosurface extracted from a volume by the sidecar and the only field data the UI process loads. *(Inferred)* *— from `kb-hw-visualization`*

## Reduced frequency k

- ωc/(2U); measures unsteadiness of an oscillating foil. *(Standard; formula Flagged *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Relabeling

- MultiSurf's reparameterization of a curve to change point/panel spacing (e.g., cosine). *(Verified, [S9])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Relational Geometry (RG)

- AeroHydro's patented model where entities reference others by name in a directed graph and dependents update selectively. *(Verified, [S9])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Rerun trigger

- A class of change (code, input, mtime, params, software-env) that marks a downstream result *— from `kb-hw-integration-and-ai-workflows`*

## Reserve factor

- the multiplier on applied load at which a failure criterion reaches unity. *(Inferred)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Richardson extrapolation / GCI

- three-grid error estimate δ_RE = ε21/(r^p − 1) and uncertainty U = F_S|δ_RE|. *(Verified, [S32])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Rooftop pressure distribution

- a design target with constant (minimum) pressure over the forward upper surface at the design angle, maximising the cavitation-free Cl range for a given −Cp_min; Eppler's hydrofoil philosophy. *(Verified, [S9][S5])* *— from `kb-hw-foil-section-catalog`*

## Run manifest

- The persisted PROV record of one Analysis run: inputs by hash, method/version/build, settings, *— from `kb-hw-integration-and-ai-workflows`*

## S-blend (BoardCAD)

- cross-section interpolation that tolerates differing control-point counts. *(Verified, [S12])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Schoenberg–Whitney condition

- t_j < x_j < t_{j+k+1} for a subset of data parameters; necessary and sufficient for a non-singular spline collocation/LSQ system. *(Verified, [S7])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Selig / Lednicer format

- Selig: one loop from upper TE around the LE to lower TE; Lednicer: a point-count line then upper LE→TE, then lower LE→TE. *(Verified, [S1] and by execution)* *— from `kb-hw-foil-section-catalog`*

## Selig format

- airfoil `.dat` layout: name line, then points from the upper-surface TE forward over the upper surface, around the LE, aft along the lower surface to the TE. *(Verified, [S1][S3])* *— from `kb-hw-file-formats-and-grammars`*

## Sheet / cloud / tip-vortex cavitation

- attached leading-edge cavity; shed erosive clouds; vortex-core *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Shim

- a wedge between stabilizer and fuselage changing stabilizer incidence in ≈0.5–1° steps. *(Verified range for iQFOiL, [S13])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Shortest round-trip formatting

- the fewest decimal digits that parse back to the identical IEEE-754 double; .NET Core 3.0+ default. *(Verified, [S22][S36])* *— from `kb-hw-file-formats-and-grammars`*

## Sidecar

- a separately stored binary artifact (HDF5, Parquet, VTU) referenced from the JSON document by path and SHA-256. *(Inferred)* *— from `kb-hw-file-formats-and-grammars`*

## Simulation numerical uncertainty U_SN

- √(U_I² + U_G² + U_T² + U_P²): iteration, grid, time-step and *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Single-mode flutter (in water)

- dynamic instability from vanishing damping of a new low-frequency mode created by speed-dependent fluid loads, distinct from coalescence flutter in air. *(Verified, [S3])* *— from `kb-hw-structures-materials-and-manufacturing`*

## Single-span curve

- A curve with one span (a Bezier segment of the given degree); Alias' recommended form for smooth curves. *(Verified, [S13][S14])* *— from `kb-hw-cad-programs-and-ux`*

## Skin-friction line (limiting streamline)

- integral curve of the wall shear stress vector; separation/attachment lines are convergence/divergence lines of this field (Tobak & Peake). *(Citation Verified, [S40])* *— from `kb-hw-visualization`*

## Skinning (lofting)

- interpolating a surface through compatible section curves (same degree and knots) column-wise in the second parameter. *(Verified, [S33])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Slice / station / bulkhead

- a planar cross-section at a fixed position along the length or span; authored in Shape3d and BoardCAD, derived in Orca3D/Maxsurf, both in the spec. *(Verified, [S1][S12][S21])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Smoothing spline

- minimiser of Σw_i|y_i−f(x_i)|²+λ∫(f'')²; λ trades fit against bending energy; GCV chooses λ. *(Verified, [S8])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Snap To TimeSteps vs Sequence

- ParaView animation over dataset time values vs generated frames. *(Verified, [S19])* *— from `kb-hw-visualization`*

## snappyHexMesh

- OpenFOAM's castellate/snap/add-layers hex-dominant mesher driven by `snappyHexMeshDict`. *(Verified, [S12])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Span

- The curve segment between consecutive distinct knots; Alias: number of CVs = degree + spans. *(Verified, [S14])* *— from `kb-hw-cad-programs-and-ux`*

## Span efficiency e

- C_L²/(π AR C_Di); 1 for elliptic loading on a planar wing; computed from the loading, never entered. *(Inferred, [S38])* *— from `kb-hw-low-order-hydrodynamics`*

## Squire–Young formula

- C_d = 2θ (u_e/V)^((H+5)/2) evaluated at the last wake point; XFOIL's drag. *(Verified, [S1])* *— from `kb-hw-low-order-hydrodynamics`*

## Sspace (AVL)

- spanwise vortex spacing code in [-3, 3]: equal, sine, cosine. *(Verified, [S17])* *— from `kb-hw-marine-and-board-cad-tooling`*

## SST (k–ω SST)

- Menter's two-equation shear-stress-transport model; SU2 variants V1994m/V2003m. *(Verified, [S20])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Static divergence

- loss of static equilibrium when the hydrodynamic twisting moment growth exceeds the torsional restoring stiffness; governs nose-up-coupled foils. *(Verified, [S2][S3])* *— from `kb-hw-structures-materials-and-manufacturing`*

## Streamline / pathline / streakline / timeline

- integral curve of one velocity snapshot / trajectory in time-varying velocity / locus of particles from one point / line of simultaneously released particles; identical only in steady flow. *(Verified semantics, [S15][S19]; McLoughlin [S30])* *— from `kb-hw-visualization`*

## Strip theory

- evaluating a 2D polar at each spanwise station's (Re(y), α_eff(y)) and integrating; profile drag comes from strips, induced drag from the 3D solve. *(Inferred, [S9])* *— from `kb-hw-low-order-hydrodynamics`*

## Strouhal number (St)

- f·A/U for an oscillating foil; propulsive optimum 0.25–0.40; human pumping ≈0.05–0.15. *(Inferred, [S21])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Strouhal number St

- fA/U with A the peak-to-peak trailing-edge excursion; propulsion depends on St and *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Structured outputs / strict tool use

- Constrained decoding guaranteeing schema-valid model output; excludes *— from `kb-hw-integration-and-ai-workflows`*

## Surface-piercing strut

- a strut crossing the free surface (mast); its ventilation stability depends on Fr, AR and α. *(Verified, [S20])* *— from `kb-hw-foil-section-catalog`*

## Table of offsets

- half-breadths and heights at every station/waterline/buttock intersection. *(Verified, [S25])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Take-off CL

- the lift coefficient required at the speed where the board leaves the water; 0.6–0.8 in this file's derivations. *(Inferred, [S9][S10])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## Tangent kinds (Shape3d)

- continuous, angular, vertical, horizontal, continuous with fixed angle. *(Verified, [S1])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Theodorsen function C(k)

- the complex lift-deficiency function of reduced frequency k = ωc/(2V) for a harmonically oscillating flat plate. *(Verified existence, [S22])* *— from `kb-hw-low-order-hydrodynamics`*
- complex lift-deficiency function of the circulatory unsteady lift. *(Verified *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## ToolClips

- Tooltips extended with short contextual video; 7x task completion versus online help in Grossman and Fitzmaurice's study. *(Verified, [S53])* *— from `kb-hw-cad-programs-and-ux`*

## Tracing (Shape3d)

- the pointer-position readout of curve measurements including distance from tail and nose. *(Verified, [S1])* *— from `kb-hw-marine-and-board-cad-tooling`*

## Trefftz plane

- the far-downstream plane in which induced drag is computed from the wake's kinetic energy; the robust VLM drag. *(Inferred)* *— from `kb-hw-low-order-hydrodynamics`*

## Trimmed vs untrimmed export

- OpenVSP terms: untrimmed = whole parametric surfaces (no slivers/gaps); trimmed = surfaces cut to intersections that can form a watertight BREP solid. *(Verified, [S34])* *— from `kb-hw-file-formats-and-grammars`*

## Tsai–Wu criterion

- quadratic interaction failure criterion for anisotropic materials (1971). *(Verified existence [S32]; coefficients Flagged)* *— from `kb-hw-structures-materials-and-manufacturing`*

## Type 2 polar

- XFOIL polar at constant lift where Re is interpreted as Re√CL. *(Verified, [S1])* *— from `kb-hw-visualization`*

## Validation uncertainty U_V

- √(U_D² + U_SN²); the level at which validation can be claimed when |E| < *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## Ventilation

- air ingress along a low-pressure region connected to the atmosphere (surface-piercing strut or breaching tip), replacing water on part of the foil and collapsing lift; onset is dynamic and hysteretic. *(Verified, [S17][S18])* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*
- atmospheric air drawn onto a low-pressure surface (nose, tail or base) of a surface-piercing body; distinct from vapour cavitation. *(Verified, [S20])* *— from `kb-hw-foil-section-catalog`*
- entrainment of atmospheric air into a sub-atmospheric separated region connected to the surface; regimes fully wetted / partially / fully ventilated, with bi-stable hysteresis. *(Verified, [S7][S8])* *— from `kb-hw-low-order-hydrodynamics`*

## Ventilation regimes

- fully wetted, partially ventilated, fully ventilated, with overlapping stability *— from `kb-hw-validation-special-physics-and-numerical-testing`*

## VOF

- volume-of-fluid interface capturing; `interFoam` (MULES algebraic) and `interIsoFoam` (isoAdvector geometric) in OpenFOAM ESI. *(Verified existence, [S11]; algorithm attribution Flagged.)* *— from `kb-hw-simulation-openfoam-su2-interop`*

## VPAT / ACR

- Voluntary Product Accessibility Template and the completed Accessibility Conformance Report; the vendor's own statement of exceptions. *(Verified, [S21][S22])* *— from `kb-hw-cad-programs-and-ux`*

## Wall function

- analytic log-law bridge between the first cell and the wall; invalid in strong adverse pressure gradients. *(Verified, [S33])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Warren-12

- a standard swept, tapered VLM verification planform (AR 2.83, LE sweep 53.54°). *(Flagged, [S15])* *— from `kb-hw-low-order-hydrodynamics`*

## Wash-out / wash-in

- nose-down / nose-up twist toward the tip; fibres toward the LE give load-dependent wash-out. *(Verified, [S1][S2][S3])* *— from `kb-hw-structures-materials-and-manufacturing`*

## Weighted least-squares spline

- control points minimising Σw_k‖Q_k−C(ū_k)‖² on fixed knots; raising w_k pulls the curve toward Q_k without reaching it at finite w. *(Verified, [S7][S29])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## Wing loading

- weight (or lift) per projected area, kPa; race wing foils ≈10–14 kPa in this file's derivation. *(Inferred)* *— from `kb-hw-hydrofoil-disciplines-and-design-data`*

## y+

- dimensionless wall distance y·u_τ/ν; targets ≤ 1 (resolved) or 30–100 (wall functions). *(Verified, [S33])* *— from `kb-hw-simulation-openfoam-su2-interop`*
- u_τ y/ν at the first cell centre; bands 1–5 (resolved) vs 30–300 (wall function). *(Flagged, standard)* *— from `kb-hw-visualization`*

## Zebra analysis

- Reflected-stripe rendering to reveal surface continuity; stripe kinks indicate discontinuity. *(Verified, [S6])* *— from `kb-hw-cad-programs-and-ux`*

## Zero tolerance / absolute tolerance / angle tolerance

- openNURBS 2⁻³² model units; Rhino document 0.01–0.001 units; 1° default (0.1° recommended). *(Verified, [S15][S16])* *— from `kb-hw-parametric-curves-lofts-and-surfaces`*

## γ–Reθ (Langtry–Menter) transition model

- two extra transport equations (intermittency γ, transition-onset momentum-thickness Reynolds number Reθt) coupled to SST; `kOmegaSSTLM` in OpenFOAM, `LM` in SU2. *(Verified, [S7][S20])* *— from `kb-hw-simulation-openfoam-su2-interop`*

## Δ criterion / swirling strength

- complex eigenvalues of ∇u (Chong–Perry–Cantwell) / their imaginary part (Zhou et al.). *(Citations Verified, [S38][S39])* *— from `kb-hw-visualization`*

## λ2 criterion

- second eigenvalue of S² + Ω² negative; objective vortex-core definition (Jeong & Hussain). *(Verified abstract, [S36])* *— from `kb-hw-visualization`*

