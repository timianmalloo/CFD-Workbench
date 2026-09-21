---
id: kb-hydrofoil-workbench
title: "Hydrofoil workbench — domain knowledge base"
type: knowledge
status: in-review
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, index, cad, geometry, hydrodynamics, cfd, optimization, visualization, structures, validation]
links:
  - { to: kb-cfd-workbench-grounding, rel: refines }
  - { to: spec-cfd-workbench, rel: relates-to }
  - { to: kb-hw-cad-programs-and-ux, rel: documents }
  - { to: kb-hw-parametric-curves-lofts-and-surfaces, rel: documents }
  - { to: kb-hw-marine-and-board-cad-tooling, rel: documents }
  - { to: kb-hw-hydrofoil-disciplines-and-design-data, rel: documents }
  - { to: kb-hw-file-formats-and-grammars, rel: documents }
  - { to: kb-hw-foil-section-catalog, rel: documents }
  - { to: kb-hw-low-order-hydrodynamics, rel: documents }
  - { to: kb-hw-simulation-openfoam-su2-interop, rel: documents }
  - { to: kb-hw-optimization-strategies, rel: documents }
  - { to: kb-hw-integration-and-ai-workflows, rel: documents }
  - { to: kb-hw-visualization, rel: documents }
  - { to: kb-hw-structures-materials-and-manufacturing, rel: documents }
  - { to: kb-hw-validation-special-physics-and-numerical-testing, rel: documents }
  - { to: kb-hw-glossary, rel: uses-term }
review-by: 2026-12-19
summary: >-
  The sourced, confidence-labelled evidence base for designing, analysing, simulating and optimising
  water-sports hydrofoils in a Mac/Windows workbench: thirteen area files, 591 sources, 200 glossary
  terms. Headline: the spec's five-channel station model is the established marine paradigm and its
  weighted-control mode has an executed mathematical formulation; the analysis tiers may claim only
  Computed estimate until per-method fixtures exist; the catalog's bundled coordinates have no
  established redistribution right; depth and Froude number are mandatory operating-point inputs;
  and structures, manufacturing and safety must be modelled as vocabulary now even though no solver
  consumes them in v1.
---

# Hydrofoil workbench — domain knowledge base

**Domain & problem:** a desktop workbench (macOS + Windows) for designing, analysing, simulating and optimising
water-sports hydrofoil wings (wingfoil, windfoil, surf, pump, downwind, parawing, e-foil, kite), where the geometry of
record is one explicit parametric surface (five spanwise distribution curves plus authored stations with editable
section profiles), analysis runs on a ladder of tiers (closed-form estimator, 2D polars, VLM + strip theory, local CFD),
and every number carries its method, envelope and provenance.

**Canonical framing:** the field frames this as three separate problems that established tools solve separately: a
*lines-plan / master-curve* CAD problem (Shape3d, MultiSurf, naval architecture), an *airfoil-and-wing analysis* problem
(XFOIL/XFLR5/AVL/OpenVSP, aircraft-framed, no water, no depth, no cavitation), and a *CFD post-processing* problem
(ParaView). The user's framing joins them under one parametric definition with a validation ladder. That framing is not
idiosyncratic — Shape3d already bridges board CAD to XFLR5, and WingHopper bridges planform to STL/STEP/XFLR5 — but no
comparable product carries water-specific operating points (depth, Froude number, cavitation number, salinity) as
first-class inputs, and none exposes an influence-weighted curve mode. Those two are the workbench's genuine novelty and
its two largest usability risks.

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher (thirteen parallel area runs; one area completed in a second pass
after a rate-limit termination) · **Status:** fresh · **Session:** `kb-experts-spec-20260920`

## How this base is organised

| File | Area | Lines | Sources |
|---|---|---|---|
| [01](01-cad-programs-and-ux.md) | CAD programs and UI/UX paradigms | 438 | 62 |
| [02](02-parametric-curves-lofts-and-surfaces.md) | Parametric curves, lofts, splines and surfaces (kernel mathematics) | 420 | 33 |
| [03](03-marine-and-board-cad-tooling.md) | Marine and board CAD tooling: Shape3d, AkuShaper, MultiSurf, OpenVSP, lines plans | 281 | 34 |
| [04](04-hydrofoil-disciplines-and-design-data.md) | Hydrofoil disciplines, product data, class rules, operating physics, the goal state | 418 | 43 |
| [05](05-file-formats-and-grammars.md) | File formats and grammars (DAT, station grammars, STEP, meshes, CFD data, native format) | 471 | 43 |
| [06](06-foil-section-catalog.md) | Foil section catalog with provenance and rights | 373 | 39 |
| [07](07-low-order-hydrodynamics.md) | Low-order hydrodynamics: sections, cavitation, finite wings, free surface, ventilation | 394 | 46 |
| [08](08-simulation-openfoam-su2-interop.md) | OpenFOAM and SU2: physics, meshing, case generation, process interop, installation | 610 | 48 |
| [09](09-optimization-strategies.md) | Optimization strategies from goal state to candidate | 384 | 56 |
| [10](10-integration-and-ai-workflows.md) | Integration, composition, provenance, ML surrogates, LLM assistance | 781 | 54 |
| [11](11-visualization.md) | Visualization: charts, colormaps, flow techniques, tooling, budgets | 457 | 56 |
| [12](12-structures-materials-and-manufacturing.md) | Structures, materials, manufacturing and safety (what the brief omitted) | 368 | 35 |
| [13](13-validation-special-physics-and-numerical-testing.md) | Validation datasets, cavitation, free surface, ventilation, pumping, numerical testing | 762 | 43 |

Roll-ups compiled from the area files by `tools/compile-knowledge.py` (derived, never hand-edited):
[state-of-the-art](state-of-the-art.md) · [comparables](comparables.md) · [references](references.md) ·
[data and constants](data-and-constants.md) · [glossary](glossary.md) (200 terms) · [open questions](open-questions.md) ·
[sources](sources.md) (591 rows, ids area-prefixed as `NN·Sn`).

Each area file follows one contract: headline findings, state of the art, comparables, reference information,
data/constants/formulae, design implications, open questions and failure modes, disconfirming views sought, glossary
terms, sources. Labels: **Verified** (primary source opened or a result executed this session), **Inferred** (reasoned
from Verified material), **Flagged** (recall, single or dated source, or a source that could not be opened).

## Headline findings

**Geometry and editing**

1. The spec's five distribution channels plus authored stations *is* the surfboard and naval "master curves plus slices"
   paradigm restated for a wing: Shape3d's outline/profile/thickness curves with slices, MultiSurf's relational
   lofted surfaces, the lines plan's stations and table of offsets. Every surveyed tool converged on "few authored
   curves, everything derived". — *(Verified, 03; 01)*
2. The Through points / Smooth split is the industry's on-curve-point versus control-point dichotomy (Rhino edit
   points vs control points, Alias edit points vs CVs, Fusion fit-point vs control-point splines). The novel element is
   the first-class **influence weight**, which no comparable exposes; it therefore needs the usability fixture the spec
   already lists. — *(Verified, 01; 03)*
3. "Smooth · weighted controls" has an executed mathematical formulation: a weighted least-squares B-spline on fixed
   knots with hard constraints as KKT equality rows. Raising a weight attracts the curve toward the control
   monotonically without reaching it at finite weight (executed: gap 0.0385 → 0.0000 for weight 1 → 1000); conflicting
   locks appear as rank or consistency failures, never as silent relaxation. Rational NURBS weights are rejected for
   shape because they break polynomial STEP export. — *(Verified by execution, 02)*
4. Degree 5 gives C⁴ only at simple interior knots; the guarantee vanishes at repeated knots, curve ends, the
   LE junction between upper and lower curves, the mirror plane, and after any approximate refit. Continuity must be
   measured per knot with the openNURBS three-tolerance pattern (zero 2⁻³², angle 1° default, distance 0.01 mm), and the
   spec's 1 µm is sensible only as an identity/round-trip tolerance, never as an operational join tolerance. —
   *(Verified constants; Inferred rule, 02)*
5. The exact A4 surface (channels evaluated, then twist rotation) is not a tensor-product B-spline in span; any STEP or
   3DM skin is a derived approximation whose deviation must be measured and reported — the MultiSurf lesson. The
   architecture must choose loft rule A (channels authoritative) or B (skin the station nets) explicitly. —
   *(Inferred, 02; Verified MultiSurf precedent, 03)*
6. rhino3dm (MIT) contains no loft, interpolation, closest-point or continuity maths; own kernel maths is not optional.
   Permissive fallbacks for the maths are curvo (Rust, MIT) or own code with geomdl/scipy as fixture oracles; SISL
   (AGPL), OCCT (LGPL) and NLopt (LGPL) are excluded. — *(Verified licences, 02)*
7. The editable record for a modified profile should be a clamped degree-5 B-spline pair; CST, PARSEC, NACA formulae
   and DAT files are conversions with measured residuals, and NACA 6-series sections have no closed form (Theodorsen
   mapping, NASA TM 4741). CST remains the right *optimizer* coordinate. — *(Verified, 02; 06)*

**Disciplines, operating points and the goal state**

8. Manufacturers' aspect-ratio conventions are inconsistent (Axis ART Pro +1.6–2.0 % above b²/S across nine sizes,
   Armstrong exact); the consumer market sits in AR 7.1–13.1, area 480–1880 cm², span 690–1380 mm. The tool derives AR
   from projected geometry and shows the convention. — *(Inferred from Verified product pages, 04)*
9. Racing classes are box or one-design rules: GWA race front wing ≥ 700 cm², rear ≥ 150 cm², mast ≤ 115 cm, fuselage
   ≤ 100 cm; iQFOiL 900 cm² one-design with a −2° to +1° stabilizer shim; Formula Kite registers production foils with
   ±2 mm span tolerance. A design tool needs rule presets that validate a recipe. — *(Verified, 04)*
10. **Depth and Froude number are mandatory operating-point inputs.** A 2026 towing-tank study of a wingfoil-class wing
    shows ≈ 17 % lift-coefficient loss at h/c = 4 between Fr_h = 2 and 5, effects concentrated at h/c < 4, deep-water
    asymptote only at h/c > 5; race foils at 20 kn and 0.3–0.5 m depth sit at Fr_h 4.6–6 and h/c 3–6, inside the
    affected region. Every closed-form correction in this base is **depth-only**: the infinite-Froude image K-factor
    gives 2–4 % and the paper's own depth fit gives ≈ 5 % at h/c = 4; the ≈ 17 % is a *Froude* delta at fixed depth that
    no closed form here captures. So no correction may ship as the displayed lift; depth and Fr_h are both inputs, the
    depth-only layer prints "Froude dependence not modelled", and the measured Froude loss is shown beside it. The tank
    Re (10⁵) and the presence of a stabilizer and strut are named confounds. — *(Verified tank data; executed arithmetic
    re-checked at the gate, 04; 07; 13)*
11. Seawater is not a 2.7 % force multiplier: ν_sea/ν_fresh = 1.044 at 15 °C, so Re is 4.3 % lower at the same speed and
    chord, and a 10 → 30 °C swing moves Re by ×1.6–1.9. Salt and temperature must go through the polar, never through a
    density factor. — *(Verified ITTC tables; executed arithmetic, 04)*
12. The cavitation ceiling is arithmetic on ITTC seawater at 15 °C and 0.5 m depth: σ = 0.86 at 30 kn, 0.63 at 35 kn,
    0.48 at 40 kn; a section holding −Cp_min ≤ 0.63 at cruise CL reaches 35 kn, matching the observed 37.9 kn wingfoil
    record. −Cp_min is a necessary-condition *screen*, not an inception prediction; the ITTC procedure states only sheet
    cavitation is reliably predicted. — *(Executed on Verified inputs, 04; 07; 13)*
13. The user's goal state (90 kg rider, race wingfoil, salt water, 10–20 kn wind) resolves to a ≈ 105 kg system,
    W ≈ 1030 N, and seven operating points: 1000 cm²/AR 12 at 10 kn wind (CL 0.77 take-off → 0.24 downwind) and
    750 cm²/AR 12 at 20 kn wind (CL 0.46 → 0.10 at 32 kn), Re 3.9×10⁵–1.1×10⁶, σ 7.7–0.75, wing loading 10.5–14 kPa.
    Board speeds per wind band are **Flagged** (no race GPS dataset could be opened); the arithmetic was re-executed by
    two area runs and matched, which is replication, not disconfirmation — the disconfirming view (light-wind take-off is
    pump-assisted and the 10 kn-wind row is the least trustworthy) is recorded in area 04 and stands. — *(Inferred on
    Flagged speeds, 04; 07; 09)*

**Catalog and rights**

14. The UIUC coordinate database states **no licence**; the UIUC wind-tunnel data is **GPL** with pass-through
    obligations; Airfoil Tools forbids reproduction; Speer's H105 page has no coordinates and no terms. Bundling any of
    these is an unestablished right under COMMIT-02. The safe v1 path is *generated-at-build* NACA sections (TM 4741
    covers 4/4-mod/5/16/6/6A) with bundled precomputed polars, and Eppler sections admitted only after written terms. —
    *(Verified absence of terms, 06; 05)*
15. The repo's "E818 best measured cavitation 42.1 kn" is a NeuralFoil Cp_min-derived computation, not a measurement
    (Speer's H105 page was opened once by area 06 and was unreachable to area 04 the same day — the "no coordinates"
    finding rests on that single opening);
    "1 < Ncrit < 3 for water" traces to a 2004 forum post that disclaims test data, and the one tank-compared Moth
    study used Ncrit 4. The evidence supports a band 2–4 run as a pair, not a constant. — *(Verified, 06)*
16. Every Eppler hydrofoil file on UIUC is Lednicer format; a default-Selig parser fails on exactly the flagship set with
    a plausible wrong shape. The CAT-02 parser must be a two-layout detector that fails closed. — *(Verified by
    download, 05; 06)*

**Analysis tiers, simulation and validation**

17. Estimator, polar and VLM results are all **Computed estimate** with "model uncertainty not quantified";
    **Verified numerical implementation** is earned per method by fixture (flat-plate 2π, elliptic wing e → 1 within the
    Helmbold–Prandtl band, symmetry invariants, knot-insertion exactness); **Experimentally compared** requires an
    admitted dataset and an ASME V&V 20 / ITTC 7.5-03-01-01 Rev 05 statement E = D − S with U_V. Nothing in the base
    lets any tier claim more. — *(Inferred from Verified standards, 07; 13)*
18. NeuralFoil (MIT) is a surrogate *of XFOIL* (7.9 M converged points, Ncrit uniform in [0, 18], MAE 0.012 in CL); its
    accuracy claims are relative to XFOIL, not experiment, and its `analysis_confidence` is a convergence/in-distribution
    flag, not an error bar. NeuralFoil 0.3.3 hard-depends on AeroSandbox, which pulls CasADi (LGPL-3.0), so the sidecar's
    package graph is not permissive-only. — *(Verified, 07; 09; 10)*
19. SU2 has no VOF, no cavitation and no mesher; OpenFOAM ESI has the whole hydrofoil physics ladder as separate
    executables. Neither official OpenFOAM line ships native macOS or Windows binaries; OpenFOAM.app is Apple-silicon
    only and unsigned; SU2 on Mac/Windows means GitHub release binaries (no Homebrew formula, conda-forge Linux only);
    Docker Desktop is free only under 250 staff and $10 M revenue. Meshing, not solving, is the gating spike. —
    *(Verified, 08)*
20. Process-tree termination is the interop's hardest correctness problem: `Process.Kill(entireProcessTree)` silently
    skips processes it cannot inspect, and neither .NET nor CliWrap reaches inside a container or WSL distro. Cancel must
    be addressed to the substrate. — *(Verified library behaviour; Inferred design, 08)*
21. The strongest public validation datasets for a consumer-scale foil are Day, Cocard & Troll (CSYS 2019, Kelvin
    Laboratory Moth T-foil, ≈ 400 runs, foil+strut) and NACA TR-1232 (64₁A412 near the surface, public on NTRS); DTIC
    ADA032272 is a foil-*system* test, not a section polar. No public rider-pumping kinematics dataset exists. —
    *(Verified, 13)*
22. Cross-platform bitwise equivalence is possible only for +, −, ×, ÷, √ and FMA; .NET and Rust both document
    transcendental functions as platform-dependent. Golden masters must be tolerance-based with platform provenance. —
    *(Verified from docs, 13)*

**Optimization, composition and AI**

23. Single-point section optimization is unsafe by construction (Drela 1998's bubble-filling bump; Garg 2017's
    off-design failure in 3D RANS+FEM); a multipoint, cavitation-constrained, stress-constrained hydrofoil design has
    been validated in a cavitation tunnel (Garg 2019: 2.9/5.1/3.0 % force differences, +29 % L/D). Multipoint is the
    discrete case of optimization under uncertainty and its weights are a distribution whose basis must be stored. —
    *(Verified, 09)*
24. Optimizers exploit formulations: an executed spike reproduced Prandtl's 1933 bell loading exactly under an
    integrated-bending-moment constraint, but the root-bending-moment variant was unbounded until a non-negative-load
    constraint was added. COMMIT-01's re-verification ladder must be data (a status enum with promotion evidence), not
    prose. — *(Verified by execution, 09)*
25. The composition kernel should be a pull-based incremental dependency graph (salsa semantics) with freshness defined
    as content-hash key equality over geometry, profiles, fluid revision, operating point, method id *and version*; the
    multi-fidelity canon (Alexandrov–Lewis, Peherstorfer, Kennedy–O'Hagan) never averages tiers. — *(Verified pattern;
    Inferred design, 10)*
26. LLM agents for OpenFOAM reach 84–96 % execution success on tutorial tasks but 62.5 % out of distribution and
    68 % physical fidelity against 82 % execution success; a wrong text-to-CAD intent header degrades output below the
    unconditioned baseline while executability stays high. Anthropic structured outputs guarantee schema validity but
    cannot express numeric bounds. This is the empirical ground for "language in, numbers out" and for deterministic
    range validation. — *(Verified, 10)*

**Visualization**

27. Turbo is not a compliant replacement for jet (Google's own post concedes lightness ambiguity and grayscale failure);
    diverging maps are used only about a physical centre, which for Cp is Cp = 0, never stagnation. Vortex criteria (Q,
    λ2, Δ, λ_ci) identify interior cores; wall separation is defined by skin-friction-line topology and needs τ_w. —
    *(Verified, 11)*
28. VTK has no permissive .NET binding (ActiViz is commercial); Veldrid is unmaintained and rend3 archived. A 10 M-cell
    RANS volume is ≈ 1.5 GB in VTK double precision, so the UI process only ever receives reduced artifacts produced by
    a sidecar. — *(Verified; data budget Inferred, 11)*

**Structures, manufacturing and safety**

29. A 1D composite beam coupled to a lifting line or VLM is the published, validated fidelity for static
    hydroelasticity; bend–twist coupling is load-dependent and its sign decides the failure mode (fibres toward the LE
    give wash-out; toward the TE give nose-up twist, accelerated stall and divergence). An executed spike shows a
    generic-laminate 90 mm-chord race wing at 10 % t/c deflecting ≈ 9 % of semispan under race loads — the structural
    concern level the Michigan study flags. — *(Verified; spike executed, 12)*
30. **No product standard governs hydrofoil-wing strength**: RCD 2013/53/EU excludes hydrofoils and surfboards,
    ISO 25649-1:2024 excludes rigid surf-sport devices, ISO 12215-9:2026 is keel/centreboard only. Safety copy and
    "Not assessed" readouts are the only defence, and they are content, not tone. — *(Verified texts opened, 12)*
31. XFOIL polars are clean-surface by construction (e^N assumes Tollmien–Schlichting transition); Braslow's
    Re_k ≈ 250–600 means a 0.3 mm ding trips the layer at sport speeds, so polars must carry a surface state and show
    clean-versus-tripped as a band. — *(Verified, 12)*

## Confidence summary

| Label | Count (token occurrences across the 13 area files) |
|---|---|
| Verified | 1,107 |
| Inferred | 544 |
| Flagged | 660 |

Flagged is high by design: every price, version, textbook formula recalled without opening the book, product speed,
practitioner floor and any source that returned 403/404 this session is Flagged rather than asserted. Nine of thirteen
area runs reported that the session's web-search quota was exhausted before they started; they sourced by direct URL,
registry APIs (GitHub, NuGet, crates.io, PyPI, conda-forge, Docker Hub, Crossref, NTRS, arXiv) and local PDF extraction,
and every citation-only verification is labelled "citation Verified, content Flagged".

**Load-bearing claims that are Flagged and must be closed before they bear weight:**

- Race wingfoil board speeds at 10 and 20 kn wind (04) — no GPS dataset opened; the whole goal-state speed axis rests
  on them.
- Foil-specific trailing-edge thickness floors (0.5–1.5 mm) (12) — practitioner values; ship as editable policy with an
  "unverified" label.
- Kulfan's CST leading-edge-radius identity (02); Warren-12 and Bertin–Smith VLM reference numbers (07); Mason ch. 6
  could not be fetched — do not admit as fixtures until re-read.
- The "within 20 %" hydroelastic stiffness agreement the gap register labels Verified (12) — paper could not be
  opened; downgraded to Flagged.
- snappyHexMesh default quality thresholds (08); OpenFOAM ESI force-output column layout (05, 08).
- Speer H105 rights, coordinates and design numbers (04, 06) — site unreachable; stays Pending admission.
- Racing class dimensional maxima for Formula Kite and iQFOiL 900 span (04) — PDFs not opened.

## Contradiction register — what this base overturns or refines in the repo

Each row names the repo artifact, the prior claim, the evidence, and the disposition. Refinements keep the prior
conclusion; reversals change it.

| Repo artifact | Prior claim | Evidence (area) | Disposition |
|---|---|---|---|
| `bench-cad-ux` §1 | Rhino "is non-history" | Rhino's `History` command records parent–child links; a loft updates when its input curves change (01) | Refinement — Rhino's default is non-history; the opt-in `History` shows the workbench's always-on curves→loft model has a precedent |
| `bench-cad-ux` §1 | MultiSurf is "notably not a NURBS model" | AeroHydro: "NOT limited to NURBS"; the relational model as a whole is non-NURBS and its export is an approximate one-way conversion (03) | Refinement — conclusion strengthened: measure and report export deviation |
| `bench-cad-ux`, repo term | Shape3d "slice flow" readout; "display thickness between any two curves" | The manual calls the pointer readout **Tracing**; the "between two curves" feature was not found in the V9 manual (03) | Terminology correction; second claim Flagged |
| `bench-parametric-geometry` | "Pick CST as the representation; others are conversions" | Bernstein global support, no insertion, no local constraints; executed weighted-LSQ behaviour (02) | Design choice changed for the editable record (B-spline, on an executed probe plus Masters 2017 at abstract level); upheld for the optimizer vector |
| `bench-parametric-geometry` | "NACA 4-series, CST and PARSEC are all exactly Bézier" | Cambered NACA 4-digit applies thickness normal to the camber line (cos θ term) — not polynomial (02) | Reversal for cambered 4-digit; upheld for CST/PARSEC/symmetric |
| `decision-0001` | "Take rhino3dm if NURBS maths gets hard" | rhino3dm has no loft/interpolation/projection API (02) | Reversal — rhino3dm is 3DM interchange only |
| `decision-0001`, `bench-cad-ux` | OpenVSP's single-entity STEP proves one surface entity is CNC-ready | OpenVSP 3.21 distinguishes untrimmed surfaces from trimmed watertight BREP solids; CAM wants a face/shell (05) | Refinement — EXP-02's gate must test a shell form |
| `proposal-sequence` §3 | "E818 best *measured* cavitation 42.1 kn vs 4412's 31.4" | No measurement exists; values are Phase-0 NeuralFoil Cp_min → V_crit (06) | **Reversal of the word "measured"** — must read "computed" with provenance |
| `proposal-sequence` §4.5 | "Speer: 1 < Ncrit < 3" | Primary post says "3 or even lower" with a no-data disclaimer; Day 2019 used 4 (06) | Refinement — band 2–4 run as a pair |
| `proposal-sequence` §3 | "H105 coordinates exist on tspeer.com / boatdesign posts" | Neither page has coordinates (06) | Reversal — source is the author directly; Pending admission |
| `proposal-sequence` §3 | "11 UIUC files vendored with hashes" (implying admissibility) | Hash ≠ rights; UIUC states no licence (05, 06) | Refinement — class "VEND? — pending permission" |
| `proposal-sequence` §3.1a | Preset AR bands surf 4–6, freeride 6–8 | Lowest production AR opened is 7.1; freeride lines run 8–9.5 (04) | Refinement — re-centre presets on the product table |
| `proposal-sequence` §3, §4.2 | "NeuralFoil (pure Python, MIT-ish)"; "JAX in AeroSandbox" | NeuralFoil 0.3.3 depends on AeroSandbox, which uses CasADi (LGPL-3.0) + IPOPT; JAX is not a dependency (09) | Reversal — sidecar licence review required |
| `proposal-sequence` §6 | "SU2 via Homebrew/conda" on macOS | No Homebrew formula; conda-forge Linux only (08) | Reversal — GitHub release binaries |
| `proposal-sequence` §6 | "OpenFOAM" as one target | Foundation ≥ 11 runs modular solvers with different dictionaries (08) | Refinement — pin the ESI line for v1 |
| `proposal-sequence` §7.2 | "Propose turbo or a diverging map for Cp" | Turbo is not lightness-monotonic and fails grayscale (11) | Reversal for turbo (rejected); upheld for the diverging map, now pinned at Cp = 0 |
| `bench-gap-register` GAP-02 | Beam + lifting-line study attributed to TU Delft; "within 20 %" labelled Verified | Authors are Ng, Jonsson, Liao, He, Martins (Michigan); the 20 % paper could not be opened (12) | Attribution correction; Verified → Flagged |
| `bench-gap-register` GAP-05 | "Peak propulsive efficiency near St ≈ 0.4 regardless of geometry or speed (Verified)" | Floryan 2017 shows joint St + k dependence; classical band 0.25–0.35 (13) | Refinement — "regardless" unsupported |
| `bench-gap-register` GAP-01 | T-foil tank data at the Australian Maritime College | Only T-foil dataset located is Kelvin Laboratory, Strathclyde (13) | Attribution Flagged |
| `bench-gap-register` GAP-01 | ADA032272 = "NACA 16-309 and 64A309 hydrofoils" | Archive record: models "geometrically similar to the forward foil system of the PCH" — a foil-system test (13) | Refinement — ranking reference only |
| Grounding register | Bench "+2.7 % seawater force" | Confirmed numerically (ρ ratio 1.0269) *and* Re drops 4.3 % (04) | Refinement — the register's objection is strengthened |
| Task brief (this session) | "Göttingen 796/797 classic hydrofoil" | Unverified (06) | Excluded |
| File 05 (this base) | VTK "BSD-3 (Flagged)", SU2 "LGPL-2.1 (Flagged)" | Verified from `Copyright.txt` and `LICENSE.md` (11) | Upgrade; conda-forge metadata for SU2 reads GPL-2.0-or-later — licence review item |

## Design implications (what `/specify`, `/define-architecture` and `/design-slice` must honour)

Numbered so the specification can cite them as `KB-n`.

1. **The record is knots + control points + constraints + rule + evaluator version** (02 §8). Store the full clamped
   knot vector, explicit all-ones weights, typed constraint rows with their source, the loft rule id, the blend rule id,
   and the evaluator version; never store evaluated coordinates, span, area or AR. Three named tolerances: identity
   (1 µm absolute *and* 10⁻⁶ relative), model/join (10 µm), angle (0.1°).
2. **Through points and Smooth share one solver** (constrained weighted least squares on fixed knots); Through points is
   the zero-residual special case. Hard constraints are typed rows (endpoint, tangent linked/split, curvature match at
   the LE junction, value-at-station, symmetry, closure, **root-mirror tangent**) and conflicts are named, never relaxed
   (02, 03). Nudge steps are three per unit family with a multiplicative weight nudge (01).
3. **Decide the loft rule explicitly** (A: channel-evaluated analytic surface, skin is a derived export with reported
   deviation; B: skin the station nets). Blend profiles as normalized camber and unit-thickness shape on a shared knot
   vector so sections are skinning-compatible by construction (02, 03). Expose a three-word loft vocabulary (Straight ·
   Through stations · Blended) and hide the Rhino/Fusion loft options that exist only for unowned inputs (03).
4. **Adopt the established vocabularies**: OpenVSP's driver set (AR/span/area/taper/average chord/root/tip/sweep) and
   closure names (None/Skew lower/Skew upper/Skew both/Extrapolate); Shape3d's Tracing pointer readout and five tangent
   kinds; Rhino Fair's tolerance + PreserveEnds contract; SolveSpace's DOF count and removable-constraint list for
   infeasible locks; Blender's modal rule (Return/Esc, one undo step on Apply, none on Cancel, rollback before error);
   Onshape's stepped keyboard orbit (15°/90°/5°) and unit-aware expressions (01, 03).
5. **Operating point carries depth h and derives Fr_h, Fr_c, h/c and σ** from the pinned water record (ITTC Rev03:
   T, absolute salinity, ρ, ν, p_v); missing depth makes σ, Fr_h and V_crit Unavailable and the deep-water label is never
   silently applied (04, 07, 13). Add a **Goal state** value object (rider and equipment masses with labels, water,
   depth band, wind → speed band with source label, class-rule preset, operating-point set with per-point weights and
   critical metric, objective definition, typed constraint set, robustness rule) authored in v1 as intent and evaluated
   as a feasibility report (09).
6. **Discipline presets expand from four to at least eight** (surf, pump, downwind, parawing, wingfoil freeride,
   wingfoil race, windfoil, kitefoil, e-foil), each with a recipe, context and per-field label, re-centred on the
   product table; **class-rule presets** validate GWA/iQFOiL/Formula Kite constraints with rule version and date (04).
7. **Sanity bounds are advisory findings** ("outside every catalogued product", "outside the method's envelope"), never
   validity gates: area 480–1900 cm², span 690–1380 mm, AR 7–13.1 and root t/c 0.09–0.12 (Verified product pages);
   wing loading 8–20 kPa, take-off CL 0.6–0.8 and cruise CL 0.1–0.45 (**Inferred, resting on Flagged board speeds and a
   Flagged system mass** — replace when an instrumented session exists); wing-only (L/D)_max 19–35, computed e 0.85–1.00,
   Re 2×10⁵–2×10⁶ for catalog polars (Inferred from Verified formulae) (04, 07, 13). AR is always derived as b²/S from
   projected geometry, and the convention (b²/S versus a maker's or a paper's S/c²) is stored and displayed with every
   imported reference figure (04, 07).
8. **Catalog admission has three classes** — generated at build (all NACA families via a TM 4741-faithful generator),
   vendored with written terms (currently none), link-only/pending (H105, Airfoil Tools, LSAT polars); presets default to
   generated sections; strut/mast is a separate role with E836–E838 and NACA 66-012/16-012/00xx; E862–E864 are fairings
   and must never appear as mast candidates (06). Each `Profile revision` carries design-point metadata as first-class
   fields — design Cl, design Re, intended σ range, source of each — populated from designation parsing for NACA
   16/6/5-series and left Unknown for Eppler/H105 until sourced (06). The DAT parser is a two-layout detector with
   fail-closed fixtures (05).
9. **Ncrit is a pair (2 and 4), displayed together and labelled** "practitioner range; no measured water N-factor";
   every polar carries a surface state (clean / tripped with k_s) and the clean and tripped polars are shown as a band
   (06, 12).
10. **Labels are mechanical**: Computed estimate for every low-order tier; Verified numerical implementation earned per
    method by a CI ring-0 fixture suite (analytic references, metamorphic tests, tolerance-based cross-platform golden
    masters with platform provenance); Experimentally compared only with dataset id, configuration comparability and
    E ± U_V. Undefined (denominator ≤ 0 or non-physical) is distinct from Unavailable (input or component missing);
    neither is ever 0, ∞ or a nearby profile's value (07, 13).
11. **Cavitation and ventilation screens say what they are**: σ_required = −Cp_min per station with the surrogate's
    sampling caveat, an explicit user margin (default 15–20 %, labelled unsourced), never "cavitation-free"; tip-vortex
    and cloud cavitation "not screened"; ventilation shown as depth margin + Fr_h + fixed text "steady analysis cannot
    predict ventilation onset" (07, 13).
12. **Free-surface correction ships only as a labelled Computed-estimate layer that carries h/c only** (the 2026 JMSA
    depth fit with the paper's constants, or the K-factor image), prints Fr_h beside it with "Froude dependence not
    modelled", and is never the displayed lift; "Find operating α" states whether it was applied because the depth-only
    correction (2–5 % at goal-state depths) already exceeds the 1 % load tolerance, and the measured Froude loss is
    larger (07, 13).
13. **Composition kernel**: an in-process incremental graph with salsa semantics, freshness by content-hash key
    equality (BLAKE3 or SHA-256 over canonical geometry + profiles + fluid revision + operating point + method id and
    version), tier recompute policy as configuration (estimator/DRC synchronous, polars and VLM debounced with
    cancellation, CFD never automatic), a job queue for sweeps, a run manifest using PROV-DM *terms* (in v1 a plain JSON
    record: input hashes, method id and version, settings hash, output hashes), a Discrepancy record per tier pair, and
    a DRC subsystem modelled on KiCad (rule id, severity policy, located violation, exclusion with reason) (10).
14. **Native format**: the explicit definition plus provenance and nothing derived; hash the RFC 8785 canonical form,
    not the saved bytes (.NET's shortest-round-trip output is bit-exact but not JCS canonical); `Utf8JsonWriter` with
    source-generated contexts; `null` for not-recorded, never NaN; run documents reference fields by path + hash and
    never embed them; the knot-vector count convention (Piegl–Tiller n+p+1 versus openNURBS two fewer) is stated in the
    format; sweep tables and extractions are JSON/CSV/`.vtp` in v1, with Parquet (Parquet.Net, MIT) and HDF5 (PureHDF,
    MIT) as the upgrade once a measured sweep exceeds what a text table can carry (05, 02).
15. **Export matrix**: v1 writes `.cfdw.json`, DAT, AVL (sampling the distributions, documenting that `Ainc` is a
    camber-line boundary condition), STL/3MF with declared units, `.su2`/`polyMesh`, VTK; STEP as surface + faces +
    shell in millimetres, released only after open-and-measure in ≥ 2 CAM systems (05). Later, not v1: the mould workflow's
    offset (shelled) surface, parting curve, datums and pull direction (12); G-code (03).
16. **CFD backend matrix is small and published**: OpenFOAM ESI (v2512/v2606 by image digest; OpenFOAM.app arm64
    unsigned, disclosed) and SU2 v8.5.0 release binaries; Foundation OpenFOAM is not in v1. Recommended v1 (Inferred):
    ESI snappyHexMesh + simpleFoam + SST for the 3D wing-in-a-box on both OSes, SU2 INC_RANS for the 2D section slice
    with a workbench-written `.su2` C-mesh validated against NASA TMR NACA 0012; flip condition is SPIKE-03b (Gmsh
    boundary-layer wing mesh → `.su2` passing the ITTC floor unattended). Physics gating at queue time; "converged" is a
    three-part label (residual criterion, artifact oracle, grid uncertainty per ITTC 7.5-03-01-01 Rev 05); cancellation
    addressed to the substrate; installation is a state machine with licence disclosure; a fully turbulent run is
    compared to the *tripped* TMR NACA 0012 data and a transition-model run to the *untripped* set, never crosswise (08).
17. **Optimizer plug-in contract now, optimizer later**: design vector = the record's own curve controls plus per-station
    CST deltas with bounds and frozen flags; evaluation provenance fields (design-vector hash, tier, method version,
    Ncrit, converged, analysis_confidence, optimizer run id); COMMIT-01 as a status enum (surrogate-candidate →
    vlm-checked → cfd-checked → experimentally-compared) with promotion evidence; four tiers with gates; a beam-model
    proxy or frozen thickness before any section optimizer is exposed (09). Later, not v1: inverse design (target Cp) as a
    profile editing mode (09).
18. **Structural and manufacturing vocabulary is reserved in v1; the fields are not modelled until a consumer exists.**
    v1 carries the glossary terms (load case, layup, manufacturing policy, fibre-angle sign convention: positive toward
    the LE gives wash-out under lift), the **Not assessed** state on every strength- or manufacturability-adjacent
    readout, a relative-stiffness readout (EI ∝ (t/c)³ or (t/c)²) beside the t/c channel that reads geometry which
    already exists, and the *beam-hook interface* as a documented contract with the Michigan Moth T-foil fixture as its
    first regression case. Typed `LoadCase`/`LayupSpec`/`ManufacturingPolicy` objects are added under expand-migrate
    when the beam tier or the manufacturing DRC ships — the native format is additive, so deferring costs nothing and
    modelling fields nobody writes would fail the reader-trace rule (12; DM15). The one exception is the TE-floor policy
    value GEO-12 already consumes, which stays a single labelled setting.
19. **Safety copy is content, fixed and tested** at three points (Loads panel, Export dialog, future Beam tier) with the
    regulatory basis named; a regulatory register re-checked yearly (12).
20. **Chart set and colormap policy**: sixteen named charts (Cp inverted with named axis and Cp_min marker, polars with
    Re/Ncrit families, x_tr, cavitation bucket and V_crit–Cl, spanwise loading vs elliptical, wing polar vs speed with
    bands, histories) each with gaps for failed samples and two-series overlay; batlow (MIT) or cividis (CC0) sequential,
    vik or Moreland cool–warm diverging pinned at the physical zero, discrete bands for y+, isolines on every flood, no
    rainbow, no turbo; legend = variable, unit, range + basis, map name, association, sample (11).
21. **Results render reduced artifacts only**: a sidecar (pvbatch / foamToVTK / SU2 SURFACE_PARAVIEW) writes `.vtp` +
    JSON manifest per sample; in-process charts (ScottPlot 5, MIT), floods, slices, precomputed streamlines and probes on
    an own Avalonia OpenGL/Silk.NET renderer (or wgpu if the Rust door opens); "Open in ParaView" for the full volume;
    never link VTK. Separation overlays need τ_w and name their criterion; vortex layers are "vortex-core candidates";
    streamline dash animation is a labelled UI affordance (11).
22. **Assistant surface**: official `Anthropic` NuGet (MIT, v12.49.0) with structured outputs for AI-02 proposals and
    AI-05 case diffs, `strict` tool use, deterministic range validation after parsing (the API's grammar cannot carry
    numeric bounds; the SDK validates `minimum`/`maximum` after the response and raises rather than clamps, which is
    SDK-version-dependent), read-only MCP-style tools with
    `outputSchema`, human acceptance for every proposal, SQLite FTS5/BM25 retrieval (no vector index in v1), and a
    ten-item eval harness (extraction, schema + domain validity, unsupported-answer, numerical meaning, attribution,
    prompt injection, coordinate/force refusal, physical-fidelity for AI-05, drift on model change, cost/latency) (10).
23. **Testing strategy floors**: manufactured geometries with closed-form area/centroid/inertia, observed-order tests on
    station refinement, knot-insertion exactness (≤ round-off), reparametrisation and station-insertion metamorphic
    tests, FsCheck/proptest invariants, thin-airfoil and Joukowski oracles, elliptic-wing e = 1 (rel 1 % CL, 5 % CDi
    proposed), NACA 0012 TMR with GCI for any RANS backend, DAT fixtures in both layouts, both-OS ring-0 runners (13, 05,
    07).
24. **Accessibility must be proven at product level**: desktop CAD accessibility is thin by the vendors' own VPATs
    (Fusion lists keyboard and assistive-technology exceptions; Blender has no accessibility tree), so the spec's
    keyboard-first and accessibility-tree targets exceed the market and cannot be inherited from a toolkit (01). Every
    cost class in this base is an Inferred placeholder until measured (the pack's standing IO rule applies).

**Borrow / avoid, in one line each.** Borrow Shape3d's gesture set and Tracing, MultiSurf's selective re-evaluation,
OpenVSP's driver and closure vocabulary, XFOIL's Ncrit-as-input, NeuralFoil's confidence flag as a validity gate,
Garg's constraint-per-point table, KiCad's DRC shape, ParaView's Sequence-vs-timesteps distinction, ITTC's E/U_V
vocabulary verbatim. Avoid Shape3d's equal-control-count slice rule, aircraft framing without depth or water, any
"cavitation-free" or "ventilation-safe" label, a whole-craft L/D from a wing-only run, Oswald e as a user input, FFD
lattices or meshes as the record, weighted-sum-only Pareto fronts, single-number correction factors, turbo, ActiViz,
Veldrid, rend3, and any solver whose licence forces a link.

## Open questions that gate design (the cheapest probe for each is in the area file)

1. Loft rule A vs B deviation on a representative wing (02 Q3) — a Python fixture, half a day.
2. Real-time constrained direct manipulation on degree 5 in C# (01 Q1, 02 Q5) — a `dotnet run app.cs` micro-benchmark.
3. UIUC coordinate redistribution terms and H105 rights (06) — two e-mails.
4. Race wingfoil speeds per wind band (04) — one instrumented session or a GPS Formula export.
5. Gmsh boundary-layer wing mesh → `.su2` unattended across AR 5/8/12 (08 SPIKE-03b) — decides the 3D backend.
6. Cancellation across Docker/WSL seams; Avalonia accessibility tree for a custom viewport control; Avalonia OpenGL on
   macOS with picking (08, 01, 11) — three spikes before the architecture fixes the stack.
7. Whether .NET 10 RyuJIT auto-contracts FMA on both platforms (13) — a 20-line spike; decides the CLI-01 tolerance.
8. Measured water N-factor (06, 07) — none exists; calibrate Ncrit 2/3/4 against Day 2019.
9. Tier correlation for multi-fidelity (09, 10; GAP-06) — unmeasured; blocks any surrogate fusion.
10. Rider pumping kinematics (13; GAP-05) — no dataset exists; plan an instrumented capture.

## How to use this base

Personas and the design skills cite the area files by id (`kb-hw-…`) and this index by `kb-hydrofoil-workbench`; the
specification cites design implications as `KB-n`. When a source contradicts an area file, edit the area file (never a
roll-up), record the change in its "Contradicts existing repo knowledge" paragraph, re-run
`python3 tools/compile-knowledge.py`, and bump this index's compiled date. Re-run `/collectknowledge` when a class rule,
solver release, licence or product line moves; the regulatory register (12) is re-checked yearly.

## Gate record

`GATE collectknowledge · 2026-09-20 · peers: Domain Researcher (lead, thirteen area runs), Product Strategist (framing) ·
adversaries (independent run, not the author): The Simplifier + Domain Researcher in Adversary Mode — 36 tool calls,
index and areas 02/06 read in full, 08/13 headlines and disconfirmation, eight Verified claims re-opened at the primary ·
first verdict: BLOCK (soft) on five Majors — (1) area 07 transcribed the JMSA 2026 fit constants wrongly under a Verified
label; (2) the same tank wing carried AR 6.8 in area 04 and 10.9 in area 07 without the convention; (3) area 02 labelled
five headline claims Verified on search-summary sources; (4) index KB-18 modelled value objects with no v1 consumer and
no written rationale; (5) index KB-7 shipped bounds derived from Flagged speeds untagged and headline 13 had no
disconfirming view · resolution, all applied by the author and recorded above and in the area files: constants
corrected and re-evaluated with the paper's (a, n) pairs; AR convention stated in both files; five labels relabelled
"citation Verified, content Flagged"; KB-18 reduced to reserved vocabulary + Not-assessed readouts + a documented
beam-hook interface with the expand-migrate rationale; KB-7 tagged and two disconfirming entries written in area 04;
headline 10 and KB-12 reconciled (depth-only fits 2–5 %, Froude delta 17 %, neither ships as lift); four missing
implications added (design-point metadata, AR convention, TMR tripped/untripped pairing, knot-count convention); KB-25
deleted and KB-24 shrunk; two register rows relabelled Refinement; NeuralFoil's transitive CasADi caveat added to areas
06/07/10 · Simplifier delete-list on area files (≈ −70 lines: SubD and visual-programming paragraphs in 01, SLS material
in 12, web-tech lines in 11, the survey paragraph in 13, workflow-engine comparison in 10): **overridden by written
rationale** — area 01's SubD paragraph is cited by its own implication 3 (dual cage display grammar); the others are the
disconfirming evidence for decisions the spec will inherit (print route, no WebView, oracle problem, no workflow engine)
and cost nothing at read time because personas cite by section; the roll-up duplication is the skill's contract and is
carried as residual · verdict after resolution: PASS-WITH-CONDITIONS · conditions: the seven load-bearing Flagged claims
listed above are closed before the corresponding acceptance criteria are marked Verified; Day 2019's Ncrit = 4 was not
independently re-read by the adversary (PDF text not extractable on the reviewing machine); nine area runs sourced without
web search after quota exhaustion and their citation-only rows are labelled as such · residual risk: seminal textbooks
(Piegl & Tiller, Farin, de Boor, Brennen, Faltinsen, Hoerner) were cited from recall and are Flagged wherever a formula
depends on them; only four of thirteen area files were read in full by the adversary.`
