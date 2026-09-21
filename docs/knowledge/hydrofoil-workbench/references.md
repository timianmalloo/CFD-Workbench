---
id: kb-hw-references
title: "Reference information"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, generated, references]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
review-by: 2026-12-19
summary: >-
  Standards, specifications, primary documentation and seminal works per area, with what each requires of CFD-Workbench, compiled from the area files.
---

# Reference information

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

## CAD programs and their UI/UX paradigms for a precision parametric foil editor

*Source: [01-cad-programs-and-ux.md](01-cad-programs-and-ux.md) (`kb-hw-cad-programs-and-ux`).*

- **Rhino command documentation** (Fair, Rebuild, History, Zebra, NamedView, Nudge options, Mouse options, edit-point definition): the reference for fairing vocabulary (tolerance, PreserveEnds), point semantics, history semantics and nudge steps. Requires of us: adopt the terms, not the mouse defaults. *(Verified, [S1]-[S9])*
- **McNeel "Coming from Rhino for Windows?"**: the documented Mac/Windows delta (Cmd for Ctrl, sidebar command field, context menu on right-click, fn for function keys, gesture support, optional Windows theme). Requires of us: an explicit per-OS shortcut table and a decision on right-click. *(Verified, [S10])*
- **Alias Golden Rules 2 and 3; CVs, Hulls and Degree**: minimum CVs, single span, CV count = degree + spans. Requires of us: default to few anchors and report control count. *(Verified, [S13][S14])*
- **Onshape help** (Variable, Numeric fields, View navigation and the View Cube, Curve/Surface analysis, Visualizing curvature): expression and unit rules, keyboard navigation steps, comb evaluation rule. Requires of us: unit-aware expressions; stepped keyboard orbit; comb evaluated on the curve, not at controls. *(Verified, [S25][S26][S28])*
- **Blender manual and developer docs** (Numeric Input; Operators; UI toolkit/GHOST): advanced numeric grammar, modal confirm/cancel, cancelled-means-no-undo-step. Requires of us: the same transaction rule for Smooth preview. *(Verified, [S31]-[S33])*
- **Autodesk Fusion documentation and blogs** (navigation presets; S-key; control-point spline FAQ; curvature comb; Form display modes): preset panel, searchable palette, comb setup, dual display. *(Verified, [S16]-[S20])*
- **Autodesk Fusion 360 VPAT (2018/2019)** and Autodesk Section 508 index: the only comparable with published keyboard/AT exceptions. Requires of us: do not cite Fusion as an accessibility precedent; exceed it. *(Verified, dated, [S21][S22])*
- **SolveSpace tutorial and reference**: DOF and overconstraint UX. *(Verified, [S39])*
- **FreeCAD planegcs sources and solver manual**: the public description of a numerical 2D GCS. Requires of us: nothing to link; a reference if we write our own small constraint solve for locks. *(Verified, [S37])*
- **Apple HIG Keyboards; Microsoft High DPI desktop application development**: Cmd as main modifier and standard shortcuts; per-monitor DPI v2 with WM_DPICHANGED. Requires of us: Cmd/Ctrl mapping table and PMv2 as a floor. *(Verified, [S47][S48])*
- **Avalonia NativeMenu and Windows platform docs**: macOS menu bar via NativeMenu, ignored elsewhere; NativeMenuBar fallback; DPI and Screens support. *(Verified, [S49])*
- **Peer-reviewed**: Lee-Remond et al. 2025; Bhavnani and John 1999/2008; Grossman and Fitzmaurice 2010; Matejka et al. 2009; Bartels and Beatty 1989; Fowler and Bartels 1993; Finkelstein and Salesin 1994; Forsey and Bartels 1988; Farin and Sapidis 1989; Allwood 1994; Procedia CIRP 2015. *(Verified as cited; see Sources for depth of reading)*

## Parametric curves, lofts, splines and surfaces for foil profiles and wings

*Source: [02-parametric-curves-lofts-and-surfaces.md](02-parametric-curves-lofts-and-surfaces.md) (`kb-hw-parametric-curves-lofts-and-surfaces`).*

- **Piegl & Tiller, *The NURBS Book*, 2nd ed., Springer 1997** — basis functions, knot insertion (A5.1), knot removal (A5.8), degree elevation (A5.9), global interpolation (A9.1, A9.2 with end derivatives), weighted/least-squares approximation (A9.7), point inversion/projection (A6), skinning (ch. 10). *Requires of us:* store full clamped knot vectors; test Schoenberg–Whitney before solving; treat removal as approximate. *(Flagged: not opened this session)*
- **Farin, *Curves and Surfaces for CAGD*, 5th ed. 2002** — G^k continuity, Coons/Gordon, fairness via curvature plots. *(Flagged)*
- **de Boor, *A Practical Guide to Splines*, rev. 2001** — Cox–de Boor recurrence, the smoothing spline (`smooth`), total positivity/conditioning of the collocation matrix. *(Flagged)*
- **Dierckx, *Curve and Surface Fitting with Splines*, OUP 1993** — FITPACK `curfit` semantics: weighted residual Σ(w(y−g))² ≤ s [S7]. *(Verified via scipy documentation [S7]; book Flagged)*
- **Hoschek & Lasser, *Fundamentals of CAGD*, 1993** — variational curve design, fairing survey. *(Flagged)*
- **Kulfan, "Universal Parametric Geometry Representation Method", J. Aircraft 45(1), 2008; "Recent extensions…", Aeronaut. J. 2010** — CST, LE modification. *(Flagged: PDFs inaccessible this session; code-level definitions Verified in [S5])*
- **Sobieczky, "Parametric airfoils and wings", NNFM 68, 1998** — PARSEC [S27]. **Hicks & Henne, J. Aircraft 15(7), 1978** — bump functions [S26]. **Lee, CAD 21(6), 1989** — centripetal parameterization [S24]. **Farin & Sapidis, IEEE CG&A 9(2), 1989** [S11]. **Moreton & Séquin, SIGGRAPH 1992; Moreton thesis UCB/CSD-93-732** [S12]. **Farouki & Goodman, Math. Comp. 65(216), 1996** [S25]. **Ladson et al., NASA TM 4741, 1996** [S28]. **Masters et al., AIAA J. 55(5), 2017** [S9]. **Prog. Aerospace Sci. 158, 2025 review** [S10].
- **ISO 10303-42 / AP203–AP242** — `B_SPLINE_CURVE_WITH_KNOTS`, `B_SPLINE_SURFACE_WITH_KNOTS` (knot multiplicities + distinct knots; degree; control points; `RATIONAL_B_SPLINE_*` only when weights ≠ 1). *Requires of us:* non-rational polynomial splines export as the simplest entity; a channel-evaluated analytic surface (§5 option A) is exported as a fitted skin with a recorded deviation. *(Flagged from recall; the STEPcode/OpenVSP precedent is in decision-0001 [S30])*
- **openNURBS `opennurbs_defines.h`** — tolerance constants and `ON::continuity` semantics [S15]; **McNeel "Understanding tolerances"** [S16]. *Requires of us:* separate absolute, relative and angle tolerances; never a single epsilon.

## Marine and board CAD tooling: stations, master curves and loft UX

*Source: [03-marine-and-board-cad-tooling.md](03-marine-and-board-cad-tooling.md) (`kb-hw-marine-and-board-cad-tooling`).*

- **Shape3d User Manual V9 (9.1.3.4, 01/06/2026)** [S1] — the authoritative description of the master-curve/slice model, gestures, Tracing, tangent kinds, curvature displays, 3D layers, multi-curves, formats. Requires of us: match or exceed its precision-gesture set; cite "Tracing", not "slice flow".
- **Shape3d X product and price pages** [S2][S3][S4] — editions, options, exports, platforms, subscription prices; Mac as a third-party port. Requires: record prices with date; treat Windows-native/Mac-port as a competitive fact.
- **AkuShaper software page** [S5] — Bézier model, readouts, formats, plans. Requires: nothing binding; a source for shaper-facing readouts.
- **AeroHydro / Letcher, "Parametric design and gridding through relational geometry"** [S9] — the RG dependency-graph definition, six lofted types and three-stage evaluation. Requires: our station model is relational in this sense; selective re-evaluation on edit is the performance pattern.
- **Kasten Marine, "Why NURBS"** [S10] — practitioner account of MultiSurf→NURBS one-way approximation. Requires: measure and report export deviation (A4 already does for sections; extend to surface export).
- **OpenVSP `APIDefines.h`** [S14] — driver, blend, section, closure, trim and curve-type enumerations. Requires: adopt its names where our concepts coincide (drivers, closure, PCHIP/CEDIT).
- **AVL user guide** [S17] — SECTION semantics and spacing codes. Requires: our AVL export writes one SECTION per authored station, with `Sspace 1.0` (cosine) by default.
- **Rhino 8 Loft help** [S16] — loft style vocabulary. Requires: use the terms only in a glossary; do not expose Loose/Tight/Rebuild/Refit.
- **FAO, Lofting requirement and technique for a ferrocement hull** [S25] — lines plan, offsets, batten fairing. Requires: the three-view mutual-consistency invariant and the "table of offsets" reading of the station table.
- **Farin & Sapidis 1989** [S27] — fairness = few monotone curvature pieces. Requires: a curvature-extremum count beside the comb (Flagged until the paper is opened).

## Hydrofoil disciplines and design data for water-sports foils

*Source: [04-hydrofoil-disciplines-and-design-data.md](04-hydrofoil-disciplines-and-design-data.md) (`kb-hw-hydrofoil-disciplines-and-design-data`).*

- **ITTC 7.5-02-01-03 Rev03 (2024)** — fresh and standard-seawater density, viscosity, kinematic viscosity and vapour pressure with sensitivity coefficients; requires every operating point to pin temperature, salinity (35 g/kg standard) and pressure basis. Values used here are Verified [S1].
- **GWA Wingfoil World Tour race equipment rules** — minima/maxima above; require a "race preset" validator (area ≥700, rear ≥150, mast ≤115, fuselage ≤100 cm) [S12].
- **iQFOiL equipment definition** — fixed 900/255/115/95 configuration; requires a benchmark preset whose geometry is Flagged until span/section are obtained [S13].
- **IKA Formula Kite builder information** — tolerances and registration; requires a tolerance display (±2 mm span, −1 mm chord) when a design targets registration [S14].
- **International Moth class rules 2017 (World Sailing)** — 6.2 beam ≤2250 mm, 6.3.3 foil protrusion; requires only a note that Moth is a development class [S35].
- **Martínez-Barberá et al. 2026, J. Marine Sci. Appl. 25(1)** — free-surface test data; requires depth as an operating-point input and h/c ≥ 5 as the "deep" validity floor [S16].
- **Aguiar Ferreira et al. 2026, J. Fluid Mech. 1028 A25** and **Augier et al. MARINE 2025** — ventilation; require the estimator to never claim ventilation safety [S17][S18].
- **Hough & Moran 1969; Wadlin & Christopher 1958** — seminal free-surface lift corrections; not opened (Flagged) — the cheapest next probe is to fetch the NACA/NASA TR from NTRS.

## File formats and grammars for curves, surfaces, meshes and CFD data

*Source: [05-file-formats-and-grammars.md](05-file-formats-and-grammars.md) (`kb-hw-file-formats-and-grammars`).*

- **ISO 10303-21 / -42 / AP203/214/242** — clear-text encoding and geometric resource entities; requires the B-spline surface where-rules (knot multiplicities, degree, closed flags) to hold and, for a body, faces/shells with orientation. Entities Verified via the STEP Tools AIM pages [S8]; the ISO texts themselves are paywalled and were not opened.
- **UIUC Airfoil Coordinates Database** — ≈1,650 airfoils, Selig ordering, `#` comments, `coord/`, `coord_seligFmt/`, `coord_updates/` archives; "© 1994–2026 UIUC Applied Aerodynamics Group"; **no licence text found** — bundling any UIUC file needs written permission or an independent source for the coordinates (many Eppler sections are published in Eppler's book; NACA sections are computable) [S1][S2][S3].
- **AVL 3.40 user guide** — the `.avl` grammar and its semantics [S6].
- **3MF Core Specification** — units, container, manifold/orientation MUSTs [S11].
- **Gmsh reference manual (MSH 4.1)** — sections, physical groups, no units [S12].
- **SU2 docs: Mesh File; Custom Output** — `.su2` grammar, `OUTPUT_FILES`, history [S13][S17].
- **CGNS SIDS** — data model incl. BCs and `DimensionalUnits` [S14].
- **OpenFOAM v2xxx user guide 4.1 Mesh description; OpenFOAM-12 `forces.H`; CFD Direct v12 ParaView chapter** [S15][S32][S33].
- **RFC 8785** [S21]; **JSON Schema 2020-12** [S27]; **.NET standard numeric format strings** [S22]; **KiCad S-expression intro** [S23].
- **HDF5 LICENSE**, **PureHDF**, **Parquet.Net**, **crates.io** and **GitHub licence metadata** [S18][S19][S20][S30][S31].
- **Repo sources**: parametric-geometry grammar [S39], spec A3/A4/A5 [S40], decision-0001 [S41], proposal-sequence §3.2–3.4 [S42], gap register GAP-04/GAP-08 [S43].

## Foil section catalog for hydrofoil wings, stabilizers, struts and fins

*Source: [06-foil-section-catalog.md](06-foil-section-catalog.md) (`kb-hw-foil-section-catalog`).*

- **Eppler & Shen, JSR 23(3) 1979, 209–217; Shen & Eppler, JSR 25(3) 1981, 191–200; Shen, JSR 29(1) 1985, 39–50** [S4][S5][S6] — the design record of the hydrofoil family. Requires of us: cite them as the provenance of E817–E838; do not invent design Cl values; treat the sections as high-Re rooftop designs.
- **Eppler, *Airfoil Design and Data*, Springer 1990** [S25] — coordinate and polar plates for Eppler sections; the book is copyrighted, so plates are not a redistribution source.
- **NACA Report 824 (Abbott, von Doenhoff, Stivers, 1945)** [S27] — 4/5/6/7-series definitions, tables, LTPT polars. Public domain. Requires: nomenclature and the tabulated thickness forms as the "exact" reference for admission tests.
- **NASA TM 4741 (1996) and TM X-3284 (1975)** [S16][S17] — generators; the admission test for a generated NACA section is agreement with the tables within the report's own stated tolerance.
- **NACA TN 1546 (1948)** [S17] — 16-series aerodynamic data (air, M 0.3–0.8).
- **DTNSRDC ADA032272** [S18] — the only measured 16- vs 6-series hydrofoil ranking found; foil-plus-strut, flapped.
- **ASME JFE 122(1) 2000 and 124(1) 2002** [S7][S8] — E817 cavitation inception and scale effect.
- **Shen & Dimotakis, JFE 111(3) 1989** [S37]; **Foeth 2008 Delft Twist-11** [S38] — cavitation-tunnel fixtures for a future local-CFD validation tier.
- **Aguiar Ferreira et al. 2025, arXiv 2503.18015 (CC BY 4.0)** [S20] — ventilation stability map for surface-piercing struts.
- **Day, Cocard, Troll, CSYS 2019** [S12]; **Beaver & Zseleczky, CSYS 2009** [S29] — Moth full-scale data; Ncrit 4 precedent.
- **UIUC LSAT licence page** [S3] — the GPL/Manifesto conditions verbatim; **UIUC coordinate database** [S1] — no terms.
- **Sharpe & Hansman 2025 (NeuralFoil)** [S13] — accuracy envelope statements to quote in the UI's method/uncertainty status (ANA-01).
- **Selig & Guglielmo, J. Aircraft 34(1) 1997** [S31] — S1223 design record.

## Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity

*Source: [07-low-order-hydrodynamics.md](07-low-order-hydrodynamics.md) (`kb-hw-low-order-hydrodynamics`).*

- **XFOIL 6.9 User Primer (Drela, MIT, 2001)** — requires: Ncrit declared per polar; drag read at the last wake point; viscous vs inviscid Cp distinguished; non-convergence treated as "unsupported", never as a value. [S1]
- **NeuralFoil README and Sharpe & Hansman 2025** — requires: CST-representable shapes (fit residual reported, per the grounding register); analysis_confidence stored with every sample; the label "XFOIL-class surrogate"; Cp_min station count shown. [S2][S3]
- **ITTC 7.5-02-01-03 Rev 03 (2024) Fresh Water and Seawater Properties** — requires: ρ, μ, ν, p_v from IAPWS (fresh) and TEOS-10 / Sharqawy (seawater) with temperature and absolute salinity S_A; uncertainty per the procedure's sensitivity coefficients; the water snapshot pinned per run (already ANA-15). [S5]
- **ITTC 1957 correlation line (7.5-02-02-01 family)** — C_F = 0.075/(log₁₀Re − 2)²; a *correlation* line (contains a form allowance), used here only as the fully turbulent fallback. [S25]
- **ASME V&V 20-2009 (R2016); ITTC 7.5-03-01-01** — required vocabulary for any "experimentally compared" badge: comparison error, validation uncertainty, validation point. [S33][S34]
- **Phillips & Snyder 2000; Goates & Hunsaker 2021; Reid & Hunsaker 2020** — the modern lifting-line references a strip-coupled 3D tier should cite. [S9][S40]
- **Mason, Applied Computational Aerodynamics ch. 6 (VLM)** — source of the Warren-12 fixture; numbers below are Flagged until the PDF is rendered. [S15]
- **NASA TN D-3767 (Polhamus 1966)** — leading-edge suction analogy; applicability label only. [S21]
- **NACA Reports 496 (Theodorsen 1934), 567 (Garrick 1936); Sears 1941** — unsteady 2D theory for the later pumping slice. [S22][S23][S24]
- **NACA TR 824 (Abbott, von Doenhoff & Stivers 1945)** — polars with and without standard roughness; the roughness-collapsed polar is the pessimistic bound. [S35]
- **DTIC ADA032272 (NACA 16-309 / 64A309 hydrofoils)** — already in the gap register as the first validation case (GAP-01); its coefficient tables remain unextracted. [S39]
- **Wadlin & Christopher, NACA TN 4168 (1958) / NASA TR R-14 (1959)** — lift of rectangular surfaces AR 0.125–10 at finite submergence including the planing limit; dihedral to 10° (TN) and 30° (TR R-14); the reference a "shallow-depth" correction must cite until a modern dataset replaces it. [S18]
- **Harwood, Young & Ceccio, JFM 800 (2016) 5–56; Aguiar Ferreira et al., JFM 1028 A25 (2026)** — regime maps (fully wetted / partially / fully ventilated), bi-stable hysteresis regions and the revised inception boundaries; requires that any "ventilation" word in the UI be a screen with named necessary conditions, never a prediction. [S8][S7]
- **Hoerner, Fluid-Dynamic Drag (1965)** — form-factor and interference-drag data; the fallback drag build-up cites it, labelled as 1960s empirical data. [S26]
- **Faltinsen, Hydrodynamics of High-Speed Marine Vehicles (CUP 2005) ch. 6** — free-surface image sign conventions and the Fr_h limits; not opened this session, so every quote of it here is Flagged. [S20]

## Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

*Source: [08-simulation-openfoam-su2-interop.md](08-simulation-openfoam-su2-interop.md) (`kb-hw-simulation-openfoam-su2-interop`).*

- **ITTC 7.5-03-01-01 Rev 05 (2024) — Uncertainty Analysis in CFD Verification and Validation, Methodology and Procedures.** Requires: systematic grid (and time-step) refinement, classification of the convergence ratio, Richardson-extrapolation error estimate, a safety factor to convert error to uncertainty, and validation as comparison of |E| to the combined numerical and experimental uncertainty. The product must show this study before "converged" is displayed. *(Verified, [S32])*
- **ITTC 7.5-03-02-03 Rev 02 (2024) — Practical Guidelines for Ship CFD Applications.** Requires: domain ≥ 10 L upstream, ≥ 20 L downstream for lifting bodies, ≥ L laterally; y+ per Table 1; positive volumes, determinant > 0.3; residual drop ≥ 3 orders or force convergence; a posteriori y+ check; roughness treated explicitly when k_s is not small. *(Verified, [S33])*
- **NASA Turbulence Modeling Resource** (now at tmbwg.github.io): SST/SA definitions (already cited by the grounding doc), NACA 0012 validation and flat-plate verification. *(Verified, [S35][S36])*
- **OpenFOAM ESI source (gitlab.com/openfoam/core/openfoam — the former develop.openfoam.com now redirects there)**: model names, function-object contracts, annotated `snappyHexMeshDict`. *(Verified, [S7]–[S15])*
- **OpenFOAM user guide (CFD Direct, v13) — file format.** Dictionary grammar and directives. *(Verified, [S40])*
- **SU2 documentation and `config_template.cfg`** (v8.5.0 master). *(Verified, [S19]–[S23])*
- **Licences:** OpenFOAM GPL-3.0 (process use only), SU2 LGPL-2.1 (binary process use; conda-forge metadata reports "GPL-2.0-or-later", a discrepancy to check before any redistribution), Docker Desktop subscription terms, OpenFOAM.app GPL-3.0, foamlib GPL-3.0, CliWrap MIT, duct MIT, Gmsh GPL-2.0-or-later (Flagged), cfMesh GPL (Flagged). *(Labels as marked, [S18][S24][S28][S29][S30][S38][S39])*

## Optimization strategies for 2D foil sections and 3D hydrofoil wings

*Source: [09-optimization-strategies.md](09-optimization-strategies.md) (`kb-hw-optimization-strategies`).*

- **Martins & Ning, *Engineering Design Optimization*, CUP 2022** [S6] — read online at mdobook.github.io; chapters 4–13 are the method reference. Requires of us: the decision tree as the tier→algorithm rule, KKT-based convergence reporting, complex-step/AD for derivatives, EGO for expensive tiers, multipoint as discrete OUU.
- **Drela 1998** [S1] — the exploitation pathology and multipoint rules; requires: points ≥ O(design variables), sample beyond the range, geometric regularity constraints, a posteriori smoothing reported as a deviation.
- **Garg et al. 2017/2019** [S2][S3] — the constraint formulations (A_cav smooth area, KS stress, TE thickness at 20 points, fixed LE), the weight table, and the validation numbers; requires: constraint-per-point "critical metric" and tunnel-grade evidence before "Experimentally compared".
- **Ng/Yildirim 2025** [S4][S5] — independent-method cross-check as a gate.
- **Bowers NASA/TP-2016-219072** [S14] and **Jones NACA TN 2249** [S15] — the two bending-moment-constrained minimum-induced-drag results; requires: the goal state names its structural proxy.
- **XFOIL doc** [S17] — MDES/QDES semantics, Ncrit table, convergence advice; **Eppler & Somers (NTRS 19790011865)** [S22] — multi-α inverse design.
- **NeuralFoil README + AeroSandbox tutorial** [S7][S8] — the concrete constraint set and confidence metric; **AeroSandbox source/pyproject** [S9][S10] — CasADi/IPOPT backend.
- **Kennedy & O'Hagan 2000, Han & Görtz 2012, Jones et al. 1998, Deb et al. 2002, Hansen 2016, Rockafellar & Uryasev 2000, Jameson 1988, Kulfan 2008, Hicks & Henne 1978, Sobieczky 1999, Poole et al. 2015, Bons et al. 2019, Li/Du/Martins 2022** [S41][S42][S37][S35][S34][S25][S44][S52][S53][S54][S28][S21][S55] — DOIs verified through Crossref; contents beyond title/abstract are Flagged unless quoted.
- **Licence files** [S13] — read from each repository's licence file via the GitHub API on 2026-09-20.

## Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

*Source: [10-integration-and-ai-workflows.md](10-integration-and-ai-workflows.md) (`kb-hw-integration-and-ai-workflows`).*

- **W3C PROV-DM (Recommendation 2013-04-30)** [S10] — Entity/Activity/Agent and the six core relations. *Requires of
  us:* every Analysis run, Sweep sample, Field evidence and Assistance proposal must be expressible as these
  relations; use the vocabulary in the run manifest so exports are interoperable.
- **Model Context Protocol, revision 2026-07-28** [S8][S9] — JSON-RPC 2.0; tools with
  `inputSchema`/`outputSchema`/annotations; human-in-the-loop SHOULD; servers MUST validate inputs, rate-limit,
  sanitize outputs; clients SHOULD show inputs before calls, validate results, time out and log for audit. *Requires
  of us:* any assistant tool the workbench exposes is read-only or proposal-producing, its `outputSchema` is validated
  server-side, and the host logs every call under `assistant.request` (A7 observability).
- **Anthropic structured outputs** [S7] — `output_config.format` and `strict: true`; unsupported numeric bounds.
  *Requires of us:* deterministic post-validation of every field (range, unit, domain) after schema parsing; AI-02's
  Stated/Inferred/Defaulted per-field provenance is a schema field, not prose.
- **Anthropic C# SDK** [S3][S4] — official `Anthropic` NuGet, MIT, v12.49.0 (2026-09-18), netstandard2.0/net8/net9;
  note it does not list a net10 TFM, which is fine for a net10 consumer via netstandard2.0/net9 but should be
  smoke-tested. *Requires of us:* the sequence's "official .NET SDK" plan holds; pin the version and record it in
  `assistant.request` events.
- **MCP C# SDK / Rust `rmcp`** [S5][S6] — Apache-2.0; stdio and streamable HTTP; `rmcp` targets spec 2026-07-28.
  *Requires of us:* if the assistant's tools are hosted in-process, prefer stdio to a local child or in-process
  transport; never expose a network listener by default (A7 privacy).
- **NeuralFoil paper and repository** [S1][S2] — training envelope and `analysis_confidence` definition. *Requires of
  us:* store `analysis_confidence`, n_crit, Re, x_tr and NeuralFoil model size + package version on every polar row;
  treat confidence below a documented threshold as out-of-envelope (A6 "explicitly unsupported observations").
- **OpenMDAO** [S11][S12][S13] — Apache-2.0; component graph; case recording. *Requires of us:* nothing to link; the
  optimizer plug-in interface should be expressible as an OpenMDAO-style explicit component (inputs → outputs,
  optional partials) so an external OpenMDAO/AeroSandbox driver could call the headless CLI (CLI-01).
- **Alexandrov & Lewis AMMO, NASA NTRS 20000097390 and 20040086473** [S18] — first-order consistency and trust-region
  management of variable-fidelity models. *Requires of us:* if an optimizer ever uses the estimator or VLM to propose
  steps, the step is accepted only after a higher-tier evaluation confirms the predicted improvement (this is
  COMMIT-01 in trust-region form).
- **Peherstorfer–Willcox–Gunzburger 2018; Kennedy–O'Hagan 2000; Forrester–Sóbester–Keane 2007** [S15][S16][S17] — the
  multifidelity canon. *Requires of us:* discrepancy records now, so a fusion model is possible later; never present a
  fused number without its variance.
- **Snakemake rerun triggers** [S48] — code, input, mtime, params, software-env. *Requires of us:* the cache key
  includes method version and settings; timestamp is never a freshness criterion.
- **KiCad DRC** [S46] — severity per rule, exclusions per violation, navigable list. *Requires of us:* the validator
  subsystem's data model.
- **AirfRANS licence CC BY-NC-SA 4.0** [S36] — *Requires of us:* no bundling or commercial retraining on AirfRANS;
  cite only.
- **OpenVSP NOSA-1.3** [S23][S24] — OSI-approved, not GPL-compatible, not MIT/BSD/Apache. *Requires of us:* under
  COMMIT-02, OpenVSP/VSPAERO may only be invoked as an external process, never linked or redistributed inside the
  product.

## Visualization for hydrofoil design and optimization

*Source: [11-visualization.md](11-visualization.md) (`kb-hw-visualization`).*

- **XFOIL 6.9 user guide (Drela)** [S1] — Cp display semantics (viscous solid / inviscid dashed), CPMN, polar page pairs, Ncrit table and definition, Type 1/2/3 polars. Requires: our Cp chart names its basis (viscous/inviscid), our polar legend carries Re, Ncrit and polar type.
- **Pressure coefficient convention** [S2] — Cp definition and the negative-up plotting convention. Requires: the inverted axis, named on the axis (ANA-10).
- **Crameri et al. 2020; Scientific colour maps v8.0.1 (MIT)** [S3][S4][S5] — the four criteria and the diverging-only-about-a-centre rule. Requires: sequential default (batlow/cividis), diverging for signed fields about 0, no rainbow, CVD-safe.
- **Kovesi 2015** [S7] — lightness-gradient uniformity as the primary criterion; test image. Requires: any custom map is tested with the sine-on-ramp image before adoption.
- **Moreland 2009/2016 and advice page** [S6][S11][S12] — surface-safe maps; midpoint smoothness. Requires: the 3D-surface default is a light-ended map; diverging maps are smooth at the centre.
- **Nuñez et al. 2018 (cividis, CC0)** [S8]; **Turbo** [S9]; **Borland & Taylor 2007** [S10]; **Rogowitz & Treinish 1998** [S13]; **ColorBrewer** [S14].
- **vtkStreamTracer** [S15] — integrators, step control, termination reasons, seeding; **vtkQuadricDecimation** [S49] — LOD semantics. Requires: streamline artifacts carry integrator, step policy and termination reason; decimated meshes are never probed.
- **ParaView licence / executables / animation / saving** [S17][S18][S19][S20] — BSD-3 + SPDX; pvpython/pvbatch; Snap-To-TimeSteps vs Sequence; export formats. Requires: replay is a Sequence over cases; hand-off is a process launch.
- **OpenFOAM v12 ParaView chapter; foamToVTK source** [S21][S22] — reader module, cell vs point data, subset export flags. Requires: reduction uses `-noInternal`/subsets; field association is recorded.
- **SU2 `config_template.cfg`, LICENSE.md** [S23] — output formats and LGPL-2.1.
- **Flow-vis surveys and seminal papers** [S29]–[S43] — techniques and their claims; the wall-topology definition of separation.
- **Hullman et al. 2015; Bonneau et al. 2014** [S44][S45] — uncertainty display.
- **Munzner 2014** [S46]; **Tufte** [S47] (Flagged).
- **WCAG 2.2 SC 1.4.1** [S48].
- **Repo artifacts** [S51]–[S56] — spec VIZ-01–04, ANA-08/10/14/16, replay text; grounding register; proposal §7; KB files 04 and 05.

## Structures, materials, manufacturing and safety for water-sports hydrofoils

*Source: [12-structures-materials-and-manufacturing.md](12-structures-materials-and-manufacturing.md) (`kb-hw-structures-materials-and-manufacturing`).*

- **Ng, Jonsson, Liao, He, Martins — "Static hydroelastic study of composite T-foils with beam and lifting line models", IMDC 2024** [S1]. Requires of us: a beam along a declared elastic axis (midchord in the paper), sectional stiffness inputs, a lifting-line/VLM hydrodynamic stiffness matrix, a Newton–Raphson static solve, and a fibre-angle sign convention. Provides a fixture (geometry, material, loads, results above). **Authorship note:** the repo's gap register calls this "the TU Delft IMDC 2024 study"; the authors are the University of Michigan MDO Lab, published in the IMDC-2024 proceedings hosted by TU Delft.
- **Young, Garg, Brandner, Pearce — "Load-dependent bend-twist coupling effects on the steady-state hydroelastic response of composite hydrofoils", Composite Structures 2018** [S2]. Requires of us: report *effective incidence* alongside geometric incidence once twist is computed; treat nose-up coupling as a divergence/stall risk flag.
- **Akcabay & Young 2019, 2020, Composite Structures** [S3][S4]. Requires of us: any flutter/divergence screen must be water-specific (added mass, new low-frequency mode); air-derived margins are not transferable.
- **Giovannetti, Banks, Ledri, Turnock — Ocean Engineering 2018** [S5]. Validation approach (full-field deformation measurement) and the ≈30 % effective-AoA reduction figure.
- **Faye, Perali, Augier et al. — JST 2024; Faye, Nême, Hauville — JST 2025** [S6][S7]. Requires of us: a section-analysis step that turns a layup into EI(y), GJ(y) and the coupling term K(y); validation against foils built in one mould with different layups.
- **Maung, Prusty et al. (UNSW) 2019–2023; Shamsuddoha et al. 2021** [S27][S28][S8][S9]. Full-scale static and fatigue data on a composite hydrofoil; the manufacturing route (RTM mould reused for AFP).
- **Pearson, McAleavy, Millen — JST 2026** [S26]. Impact resistance is not degraded by bend-twist bias up to 30°; preload increases impact damage — a design note for the layup value object.
- **XFOIL 6.9 user guide (Drela)** [S16]. Ncrit table (sailplane 12–14; clean tunnel 10–12; average 9; dirty 4–8); trips; the bypass-transition caveat. Requires of us: every polar carries Ncrit and trip locations as provenance and is labelled "clean surface".
- **NACA TN 4363, Braslow & Knox 1958** [S17]. Re_k,t ≈ 250–600 (600 working value) for distributed 3-D roughness; zero pressure gradient, zero heat transfer, flat-plate/cone profiles.
- **Directive 2013/53/EU (RCD), Art. 2(2)(a)** [S10]. Excludes surfboards, sailboards and hydrofoils from Part A design/construction requirements. Requires of us: do not cite RCD/ISO 12217 as applicable.
- **ISO 25649-1:2024** [S11]. Scope excludes surf-sport devices, kite/wakeboards and rigid devices. Not applicable.
- **ISO 12215-9:2026 (edition 2, 2026-09-10, 72 pp.)** [S12]. Loads and scantlings for sailing-craft appendages (keel, centreboard, attachments) up to 24 m; design stresses; computational guidance. Closest existing normative text for a *boat's* foil; applicability to a board foil is by analogy only (Flagged).
- **ISO 21853:2020** [S13]. Kite release systems only.
- **Tsai & Wu 1971, "A general theory of strength for anisotropic materials", J. Composite Materials** [S32]. The quadratic failure criterion the beam tier should offer beside max-strain.
- **Theodorsen, NACA Report 496 (NTRS 19930090935); NASA re-computation 2015 (NTRS 20150014000)** [S29]. Unsteady 2-DOF section aerodynamics for a flutter screen.
- **Performance Composites mechanical-properties table** [S15]. The starting laminate property set (labelled "for reference / information only and NOT a guarantee").
- **Hubs CNC / injection-moulding / SLS guides; Protolabs guides** [S18][S19][S20]. Vendor DFM floors.
- **Trindade et al., ASME IAM 2022** [S21]. SLS PA12 water absorption 0.76 % (untreated) / 0.35 % (vapour-smoothed).
- **DCFoil.jl LICENSE and README** [S22]; **FreeCAD / OpenCAMLib GitHub licence fields** [S23]; **Ng et al. 2024 Composite Structures 346:118367** [S24].
- **Axis Foils mast collection page** [S25]. 19 mm aluminium; Power Carbon / HM / UHM; one-piece.
- **Textbooks (not opened this session — Flagged as sources):** Jones, *Mechanics of Composite Materials*; Daniel & Ishai, *Engineering Mechanics of Composite Materials*; Bisplinghoff, Ashley & Halfman, *Aeroelasticity*; Timoshenko & Gere, *Mechanics of Materials*; Megson, *Aircraft Structures*. The formulas below are standard content of these books; the pages were not re-read, so the formulas carry a Flagged label until the implementer checks them against one of these texts.

## Validation data, special hydrofoil physics and testing numerical design software

*Source: [13-validation-special-physics-and-numerical-testing.md](13-validation-special-physics-and-numerical-testing.md) (`kb-hw-validation-special-physics-and-numerical-testing`).*

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

