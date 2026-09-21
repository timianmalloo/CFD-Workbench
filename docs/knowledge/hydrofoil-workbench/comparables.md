---
id: kb-hw-comparables
title: "Comparable solutions and problem framings"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, knowledge, generated, comparables]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
review-by: 2026-12-19
summary: >-
  How existing products, libraries and the literature frame and solve each part of the problem, with what each does well and badly and its licence, compiled from the area files.
---

# Comparable solutions and problem framings

> **Generated roll-up.** Compiled by `tools/compile-knowledge.py` from the area files in this directory on 2026-09-20. Edit the area file, then re-run the script; this file is a derived cache (DM7) and is checked for drift by `--check`.

## CAD programs and their UI/UX paradigms for a precision parametric foil editor

*Source: [01-cad-programs-and-ux.md](01-cad-programs-and-ux.md) (`kb-hw-cad-programs-and-ux`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence / cost (2026-09-20) | Confidence |
|---|---|---|---|---|---|---|
| Rhino 8 (McNeel) [S1-S10] | Freeform NURBS objects, no tree by default | Direct control/edit-point editing; opt-in History; command line; Fair, Rebuild, CurvatureGraph, Zebra; three-step Nudge; NamedView, 4View | Precise, documented point semantics; fairing and analysis vocabulary; genuinely native Mac UI with Cmd substitution | Unconstrained (can make a non-foil); NURBS weights hidden behind an expert command; right-click meaning differs by OS | Proprietary, perpetual, USD 995 commercial (vendor page via search excerpt) | Verified (docs), Flagged (price) |
| Autodesk Alias [S13-S15] | Class-A surfacing | CVs, hulls, degree up to 9; Golden Rules (minimum CVs, single span) | The clearest written rationale for few controls; continuity G0-G3 vocabulary | Windows-only per 2026 requirements; subscription; heavy | Proprietary subscription | Verified (rules), Flagged (platform) |
| Autodesk Fusion [S16-S24] | History-based parametric with sketch splines, Form (T-spline) environment | Fit-point vs control-point splines; comb Density/Scale; S-key searchable toolbox; navigation presets; Qt on Win and Mac | Preset navigation; palette search; comb setup; dual box/smooth display in Form | VPAT admits partial keyboard and AT coverage; multi-GB install; cloud account | Proprietary; USD 85/month or 680/year; free personal-use licence with revenue cap (search excerpts) | Verified (features), Flagged (price) |
| Onshape (PTC) [S25-S30] | Cloud parametric CAD | Variables and unit-aware expressions in every numeric field; real-time comb on drag; keyboard stepped rotation; product-named navigation presets; VPAT on request | The only documented keyboard-complete 3D navigation; expression grammar with unit rules | Browser only (no offline); free tier makes all documents public | Proprietary; Free plan is public-document, non-commercial (vendor FAQ via excerpt) | Verified (docs), Flagged (plan terms) |
| SolidWorks (Dassault) | Feature tree | The subject of the 2025 novice study | Industry default vocabulary | Windows-only; mode confusion measured in beginners | Proprietary | Flagged (no vendor page opened) |
| Blender [S31-S35] | DCC modelling and animation | Custom GHOST/OpenGL UI; modal operators with Return/Esc; advanced numeric input with units and Python expressions; SubD cage editing | Best-specified preview/confirm/cancel and undo-step rules; numeric input grammar | No native accessibility tree; 2025 accessibility work is tablets, not AT; GPL | GPL-2.0-or-later (recall; repository not opened) | Verified (docs), Flagged (licence) |
| FreeCAD 1.x [S36-S38] | Open-source parametric with Sketcher | planegcs constraint solver (DogLeg, LM, BFGS, SQP); Qt | Solver documentation (manual PDF) is a rare public description of a 2D GCS | Qt6 migration unstable; LGPL forbids linking under repo rule; OCCT-based | LGPL-2.0-or-later (repository LICENSE) | Verified |
| SolveSpace [S39][S40] | Constraint-first sketching and assemblies | Explicit DOF count; red background on inconsistency; removable-constraint list | Model UX for constraint feasibility reporting | GPL; small user base; no fairing tools | GPL-3.0-or-later (search excerpt) | Verified (docs), Inferred (licence) |
| OpenSCAD [S41] | Code-first CSG | Text program compiles to geometry | Reproducible, diffable | No direct manipulation at all; no NURBS | GPL-2.0-or-later (about page via excerpt) | Inferred |
| CadQuery / build123d [S42][S43] | Python code-first B-Rep on OCCT | Scripted, parametric | Apache-2.0 own code; good for generating fixtures and export tests out of process | Transitively OCCT (LGPL) so cannot be linked; no interactive editing | Apache-2.0 (repositories) | Verified |
| Plasticity [S44] | "CAD for artists": NURBS on Parasolid | Direct modelling; Win/macOS/Linux | Proof that a small team ships native NURBS on three OSes at indie price | Closed kernel; UI framework not established; no fairing analysis documented this session | Proprietary; Indie perpetual USD 175 with 12-month maintenance (vendor/press excerpts) | Verified (platforms), Flagged (price, stack) |
| MoI 3D [S45] | Lightweight NURBS for designers | Direct modelling; Win and Mac | Simplicity as a design goal | Sparse analysis tools | Proprietary, perpetual, USD 295 class (retailer excerpts; official page not opened) | Flagged |
| Shape3d (`bench-cad-ux`) | Surfboard design through orthogonal master curves | Outline/profile/thickness curves; keyboard steps; live width/rocker readouts | The closest UX analogue; already Verified in repo | Proprietary; board-specific | Proprietary | Verified (repo) |
| Grasshopper / Dynamo [S46] | Visual programming of geometry | Node graph | Explicit, inspectable history; automation | Second language; not an interactive fairing surface | Grasshopper ships with Rhino; Dynamo open-source (Apache-2.0, recall) | Flagged |

## Parametric curves, lofts, splines and surfaces for foil profiles and wings

*Source: [02-parametric-curves-lofts-and-surfaces.md](02-parametric-curves-lofts-and-surfaces.md) (`kb-hw-parametric-curves-lofts-and-surfaces`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| Rhino / openNURBS [S15][S16][S17] | Direct manipulation of NURBS with explicit tolerances | Control points, knots, weights; continuity classes C/G with per-order tolerances; document absolute/angle tolerance | Precise, well-documented tolerance semantics; MIT types library | rhino3dm omits all "compute" (loft, closest point, fairing) | MIT (rhino3dm) | Verified |
| Dierckx FITPACK via scipy [S7][S8][S31] | Weighted least-squares and smoothing splines with a global smoothing budget s or λ | `curfit` with w and s; `make_smoothing_spline` with w and λ (GCV) | Exactly the "weights attract, never force" behaviour (executed [S29]) | Function form y(x); no hard equality constraints; Fortran | BSD-3 (scipy) | Verified |
| Piegl & Tiller algorithms (via geomdl) [S1][S33] | Interpolation, approximation, skinning as linear algebra on B-splines | A9.x fitting, ch. 10 skinning with compatibility | Deterministic, textbook, testable | Knot merging inflates skins; no fairness term | MIT (geomdl) | Verified (geomdl), Flagged (book) |
| Farin & Sapidis fairing [S11] | Fairness = few monotone curvature pieces | Local knot removal/reinsertion | Local, cheap, explainable | Cubic-oriented; does not fair across a junction | Paper | Verified |
| Moreton & Séquin MVC [S12] | Fair = minimal curvature variation | Nonlinear energy minimisation | Fairest; circle-preserving | Nonlinear, slower, uniqueness | Paper | Verified |
| Kulfan CST / AeroSandbox [S4][S5] | Airfoil = class × Bernstein shape | Linear LSQ fit; LE modification; TE term | Compact optimizer basis; always-valid class | Global support; poor as off-curve editor; identities Flagged | MIT (AeroSandbox) | Verified (code) |
| Masters et al. 2017 / PAS 2025 review [S9][S10] | Which parameterization covers real airfoils with fewest variables | UIUC database coverage; optimization benchmarks | 20–25 DOF coverage figure; CST/SVD ranking | Not about direct editing or persistence | Papers | Verified (abstract level) |
| NASA TM 4741 [S28] | NACA ordinates as a program, not formulas | Conformal mapping for 6-series | Reproduces tables to ~5e-5 c | No closed form; iterative scaling | Public domain (NASA) — Flagged | Verified |
| Shape3d / Fusion control-point splines [S30] | Curves with fit points vs control frames | UI patterns already in the repo | Discoverable fairing metaphor | No maths published | Docs | Verified (repo) |
| curvo (Rust) [S19] | NURBS modelling incl. loft, closest point, tessellation | nalgebra-based | Feature-complete for our scope; MIT | Pre-1.0, single maintainer | MIT | Verified (licence), Flagged (maturity) |
| SISL / OCCT / NLopt [S20][S21] | Full kernels/optimizers | — | Mature | Copyleft: AGPL / LGPL | AGPL-3.0 / LGPL-2.1 / LGPL | Verified — excluded |

## Marine and board CAD tooling: stations, master curves and loft UX

*Source: [03-marine-and-board-cad-tooling.md](03-marine-and-board-cad-tooling.md) (`kb-hw-marine-and-board-cad-tooling`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| Shape3d X 9.1 (Lite/Design/Design Pro + 3D Export/CNC options) [S1–S4] | A board is 3 orthogonal master curves + N slices with equal control counts | 2D panel editing, tangent kinds, curvature radius, Tracing readout, guidelines, multi-curve links, 3D layers, IGES spline export, G-code | Precision gestures (nudge steps, group select, align), live measurements at pointer, ghost/guidelines, NACA/.dat/XFLR5 bridge, broad CNC reach | Equal-control-count slice rule; Mac is a wrapped port; subscription ladder; proprietary `.s3dx`; 3D editing admitted harder | Proprietary, subscription EUR 0–809/yr (2026-09-20) | Verified |
| AkuShaper (Backyarder/Up-and-Comer/Heavyweight) [S5] | Same paradigm, simplified curves (3 control points per segment) | Bézier outline/rocker/rails/thickness, 2D + 3D shaping bay, contour highlight, shaper readouts, CNC-centre network | Shaper-facing readouts (foot marks, rail thickness marks), imports `.s3dx`/`.srf`, contour highlight | 3D export gated to top tier; fewer curve controls; web-portal dependence | Proprietary, USD 12.95–49.95/mo (2026-09-20) | Verified |
| BoardCAD / BoardCAD LE (Java) [S11][S12] | Composite Bézier outline, rocker, cross-sections; two interpolation modes | Point-correspondence or S-blend loft; sliding cross-section; curvature; STEP/STL/DXF/g-code | Shows the correspondence-vs-blend fork explicitly; multi-select gestures | Legacy Swing UI; licence not shown on pages opened | Open source (licence Flagged) | Verified model; Flagged licence |
| OpenShaper (browser) [S13] | BoardCAD rebuilt for the web | Planshape/rocker/cross-section Bézier, live volume, STL/DXF/PDF | Zero-install reference implementation to read | Board-only physics; GPL | GPL-3.0-or-later (copyleft — read, do not link) | Inferred |
| MultiSurf 9.0 / SurfaceWorks (AeroHydro) [S7–S10] | Relational geometry: directed dependency graph of points, curves, snakes, magnets, surfaces | 30 surface types; six lofted types by lofting-curve family; durable joins; relabeling for spacing | Exactly the "few master curves, everything derived" idea; selective re-evaluation; foil-lofted surface type exists | Non-NURBS whole; NURBS export approximate, one-way, dense control nets; Windows only; opaque pricing | Proprietary (price Flagged) | Verified |
| Orca3D (Rhino plug-in) [S21][S22] | Hull = NURBS surface; sections derived live | Hull Assistants, live stations/buttocks/waterlines, hydrostatics | The "derive the lines plan from the surface" pattern; perpetual or annual | Requires Rhino; Windows-only Rhino plug-in (Flagged) | Proprietary, ~GBP 365–1,385 (reseller, 2025) | Verified features; tertiary price |
| Maxsurf Modeler (Bentley) [S23] | Trimmed NURB surfaces edited by control points | Shaded/contour curvature, parametric transformation, DXF/IGES/Rhino I/O | Curvature displays that update as you edit; parametric transform of an existing hull | Licence terms not on page; Windows only (Flagged) | Proprietary (Flagged) | Verified features |
| DELFTship Free/Pro [S24] | Subdivision-surface hull modeller | Control-net editing; Pro adds automatic fairing of control curves/surfaces | Automatic fairing as a first-class command; free tier | Subdivision model is not a NURBS export path; Pro-gated asymmetry | Proprietary freeware / paid Pro (Flagged) | Verified features |
| OpenVSP [S14][S15] | Wing = stacked sections; per-section driver group; named blends | Drivers (AR/span/area/taper/avg/root/tip/sweep), 8 blend modes, rich XSec families, closure/trim enums | The clearest vocabulary for drivers, blends and closure; CST/file/NACA sections; STEP B-spline export precedent | Aircraft framing; no curvature comb; GUI is dense | NASA-1.3 (Flagged, recall) | Verified from source |
| AVL [S17] | Explicit SECTION rows, linear interpolation | Xle Yle Zle Chord Ainc + NACA/AFILE; Sspace spacing codes | Minimal, diffable station table; cosine spacing guidance | Linear only; no geometry editing | GPL-2.0 (Flagged, recall) — process invocation only | Verified format |
| XFLR5 / Flow5 [S18] | Table of sections (y, chord, offset, dihedral, twist, foil, panels, spacing) | Tabular editing; twist about quarter-chord after dihedral | Practitioner-standard for wing polars; Shape3d exports to it | Tabular only; no curves; GPL for XFLR5 | GPL (Flagged) / commercial Flow5 (Flagged) | Inferred |
| WingHopper (browser) [S28] | Parametric hydrofoil wing: span, area, sweep, dihedral, blended sections, twist | Live 3D; STL/STEP/XFLR5 export | Proves the market wants exactly this export triad | No curvature/fairing evidence; distribution UI unknown | Unknown (Flagged) | Verified claims; Flagged licence |
| KaroroCAD, 3DFoil [S29][S30] | Wing-specific CAD (Karoro); finite-wing analysis (3DFoil) | — | Category evidence | Little public documentation | Proprietary (Flagged) | Flagged |
| Rhino 8 Loft [S16] | Generic loft over arbitrary curves | Loose/Normal/Tight/Straight/Uniform; Rebuild/Refit; tangent matching | Defines the industry vocabulary | Most options exist only because inputs are unowned | Proprietary (reference only) | Verified |
| Fusion / Onshape Loft [S19][S20] | Generic loft with rails/centerline, end conditions, vertex matching | — | Names for G1/G2 end conditions and correspondence | Centerline "does not precisely specify the shape" | Proprietary (reference only) | Inferred |

## Hydrofoil disciplines and design data for water-sports foils

*Source: [04-hydrofoil-disciplines-and-design-data.md](04-hydrofoil-disciplines-and-design-data.md) (`kb-hw-hydrofoil-disciplines-and-design-data`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| Manufacturer spec pages (Axis, Armstrong, Sabfoil) [S2][S3][S9] | area/span/AR/(thickness) per SKU, prose envelope | published planform numbers, sometimes take-off/top speed per rider mass | authoritative dimensions; consistent ladders | no section, taper, twist, anhedral; AR convention unstated | proprietary web pages, cite only | Verified |
| Retailer spec sheets [S6][S7][S8][S10][S11][S31][S32] | copied maker numbers | same | broad coverage | copy errors (Leviathan 1060 AR mismatch) | proprietary | Inferred/tertiary |
| GWA / IKA / iQFOiL / Moth rules [S12][S13][S14][S35] | equipment box or one-design | hard minima/maxima | unambiguous constraints | maxima for Formula Kite not accessible here | rule documents, cite | Verified |
| CEHINAV tank test [S16] | forces vs h/c, Fr_h, α on a race-type wing | towing tank | quantifies free-surface loss at competition Fr | Re one order below full scale | journal article (no data licence stated) | Verified |
| Delft surface-piercing ventilation [S18] | inception maps | towing tank | trigger taxonomy, revised stability map | simple sections, AR ≤1.5 | JFM, arXiv preprint (arXiv non-exclusive licence) | Verified |
| Augier et al. kitefoil mast [S17] | inception vs input dynamics | on-water rig | real Olympic hardware | abstract only, no numbers | conference abstract | Verified abstract |
| Flapping-foil St literature [S21][S38] | propulsive efficiency vs St | CFD/experiment | optimum St band | not human pumping | journals / arXiv | Inferred |
| Pump-foil simulator (foilphysics) [S27] | angle telemetry toy model | web app | cadence vocabulary | unvalidated | unknown | Flagged |
| Speer H105 write-up [S37] | low-Re, flat-rooftop hydrofoil section | analysis + coordinates | design rationale for kite/wing foils | site unreachable; rights on coordinates unknown | unknown (rights gate stands in spec) | Flagged |

## File formats and grammars for curves, surfaces, meshes and CFD data

*Source: [05-file-formats-and-grammars.md](05-file-formats-and-grammars.md) (`kb-hw-file-formats-and-grammars`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| UIUC `.dat` (Selig/Lednicer) [S1][S3] | A profile is a point list with a name | Two text layouts, one extension | Universal, tiny, XFOIL-native | Layout ambiguity, no units, no provenance, no licence statement | none stated | Verified |
| AVL `.avl` [S6] | A wing is sections + spacing + refs | Keyword text | Minimal, decades-stable, VLM-ready | Linear only; twist is a BC; Sref can disagree with geometry | GPL (tool) | Verified |
| MachUpX JSON [S24] | Distributions are functions of span fraction | JSON + airfoil JSON | Closest to five-channel model; step twist; airfoil list over span | No editing state; aerodynamics decoupled from outline | MIT | Verified |
| OpenVSP `.vsp3` + STEP/IGES [S7][S34] | Parametric components with export options | XML params; STEPcode writer | Trimmed watertight BREP since 3.21; Bezier `.bz`; CSV metadata | Trimming can create slivers/gaps; NOSA licence for code | NOSA 1.3 (Flagged) | Verified export behaviour |
| Shape3d `.s3dx` [S29] | Board/foil as orthogonal curves + slices | XML, app-private | In-domain; XFLR5 tutorial shows the interchange path | Version lock (V8 loses data); undocumented | proprietary | Flagged |
| STEP AP203 B-spline surface [S8] | Exact NURBS for CAD/CAM | Part 21 text | Universal CAM import; one entity suffices for a surface | Surface ≠ solid; knot multiplicity encoding; own writer unproven | ISO (fee); STEPcode BSD-3 | Verified entity, Inferred CAM behaviour |
| 3MF [S11] | Printable model with units and manifold rules | ZIP/OPC + XML | Units, manifold contract | Printing only; no CFD reader | spec: open (Flagged); lib3mf BSD-2 | Verified |
| CGNS [S14] | CFD data with BCs and units as a data model | HDF5 + SIDS | Units, BCs, structured/unstructured; SU2 reads it | Heavy library; no pure-.NET writer | zlib-style (Flagged) | Verified scope |
| OpenFOAM case dir [S15][S32] | The directory is the format | Text dictionaries and lists | Human-inspectable; ParaView-native | Two forks differ in outputs; no units | GPL-3.0 (process) | Verified core |
| SU2 `.su2` + outputs [S13][S17] | Minimal ASCII mesh + configurable outputs | Text + VTK/Tecplot/CSV | Trivial to write; markers by name | No units; binary restart is SU2-private | LGPL-2.1 (process) | Verified |
| KiCad S-expr [S23] | Readable, diffable EDA file | S-expressions, fixed decimals, version date, UUIDs | Best Git-friendly precedent | Custom parser; no schema validator | GPL (tool) | Verified |
| RFC 8785 JCS [S21] | Canonical JSON for hashing/signing | Sort keys, ES numbers, no whitespace | Deterministic hash independent of pretty form | Not a storage format; ES number rules ≠ .NET default | RFC (free) | Verified |
| AeroSandbox [S4][S5] | Airfoil/wing as Python objects | numpy; `.dat` regex reader; CST | CST parameter set; fixtures | No Lednicer detection; 6-decimal writer | MIT | Verified |

## Foil section catalog for hydrofoil wings, stabilizers, struts and fins

*Source: [06-foil-section-catalog.md](06-foil-section-catalog.md) (`kb-hw-foil-section-catalog`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| UIUC Airfoil Data Site [S1] | A coordinate archive, not a catalog | Flat file list, two formats, minimal metadata | Coverage; canonical names | No licence; no per-file provenance; mixed formats and point counts | Unstated | Verified |
| Airfoil Tools [S21] | Browse + XFOIL polars per airfoil | Precomputed XFOIL at fixed Ncrit/Re grid | Instant polars, comparisons | Air Ncrit, no cavitation, no-reproduction terms | Proprietary | Verified |
| AeroSandbox / NeuralFoil [S13][S14] | Programmatic airfoil object + surrogate polars | Python; bundled UIUC copy; 4-digit generator | Speed, differentiable, MIT | Not 6/16-series; accuracy vs XFOIL only | MIT | Verified |
| NASA TM 4741 generator [S16] | Reproducible NACA ordinates | Fortran 77 | Exactness, all NACA families | No non-NACA sections | US Gov | Verified |
| OpenVSP [S15] | CAD cross-section library | 4/5/16/6-series types + CST fit | Families built in | NOSA licence; heavy dependency | NOSA 1.3 | Verified |
| Speer H105 page [S9] | Design rationale essay | Prose + plots | Explains low-Re hydrofoil philosophy | No numbers, no coordinates, no terms | None stated | Verified |
| IHS "Hydrofoil, Rudder, and Strut Design Issues" [S33] | Practitioner Q&A compilation | Forum-style archive | Historic strut/ventilation lore, section list | Undated, unreviewed | IHS site | Verified text (tertiary) |
| Day et al. 2019 [S12] | Full-scale T-foil measurement + simplified prediction | Tank + XFOIL/LL | Real water Re data, Ncrit stated | One foil; free-surface model simplified | Conference paper | Verified |
| JST 2025 AC75 section optimisation [S23] | Cp_min-constrained multipoint RANS optimisation | Adjoint/RANS | Frontier method | AC75 speeds/Re; behind paywall | Journal | Verified abstract |

## Low-order hydrodynamics: 2D sections, cavitation screening, 3D lifting bodies, hydrofoil corrections, trim and multi-fidelity

*Source: [07-low-order-hydrodynamics.md](07-low-order-hydrodynamics.md) (`kb-hw-low-order-hydrodynamics`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| XFOIL 6.99 (Drela, MIT) [S1] | 2D section design/analysis | Linear-vorticity panel + integral BL, e^N, Squire–Young | Attached-flow polars Re 10⁵–10⁷; transition locations; Cp with 160+ panels | Massive separation; C_l,max; no water-specific transition data | GPL (version not stated in primer) | Verified |
| NeuralFoil ≥ 0.3 (Sharpe) [S2][S3] | XFOIL surrogate, differentiable | MLP on 7.9 M XFoil cases; CST 18-param; Ncrit ∈ [0,18] | Speed (30–1000×), Ncrit as input, confidence flag, BL outputs at 64 stations | Cp_min resolution; post-stall is empirical blend; error measured vs XFOIL only | MIT | Verified |
| AeroSandbox (Sharpe) [S4] | Design-optimisation framework | AeroBuildup, VLM, lifting line, nonlinear LL, NeuralFoil, XFoil wrapper | Reference implementations; CasADi gradients | Python-only; VLM wake/validation not documented on the landing page | MIT | Verified |
| MachUpX (USU) [S10] | General numerical lifting line | Goates–Hunsaker / Reid–Hunsaker; section databases | Sweep, dihedral, polar coupling, fast | Lifting-line limits near tips/stall; Python | MIT | Verified |
| OpenAeroStruct (MDO Lab) [S11] | Aerostructural optimisation in OpenMDAO | VLM + beam FEM, adjoint | Coupled deflection (GAP-02 pairing) | Heavy dependency chain; aircraft conventions | Apache-2.0 | Verified |
| AVL 3.x (Drela/Youngren) [S12] | Aircraft VLM + stability derivatives | Horseshoe VLM, Trefftz drag | De-facto VLM oracle; well-known test cases | Fortran; GPL — process only | GPL (version Flagged) | Inferred |
| XFLR5 [S14] | GUI around XFOIL + LLT/VLM/3D panel | XFOIL polars + 3D | Widely used by foil hobbyists; comparable numbers for users | GPL; aircraft framing; no free surface | GPL | Inferred |
| flow5 [S14] | Successor to XFLR5 | VLM/panel; viscous from polars | Modern UI | Was commercial; GPL-3.0 from 2026-01-01 — process only | GPL-3.0 (2026-01-01) | Inferred |
| VSPAERO / OpenVSP [S13] | Aircraft geometry + VLM/panel | VLM and panel solver | Geometry tooling | NOSA 1.3 (OSI-approved, GPL-incompatible); process only | NOSA-1.3 | Inferred |
| Tornado (MATLAB) | Educational VLM | Horseshoe VLM; Bertin–Smith comparison | Reference plots vs Bertin & Smith | MATLAB; licence GPL per recall | GPL (Flagged) | Flagged |
| PyVLM (aqreed) / VLMPy / pySailingVLM | Small Python VLMs | Vortex-ring VLM | Readable source for porting | Licences not confirmed this session | Flagged | Flagged |
| Thiart 1994 free-surface LL [S16] | Hydrofoil near surface | Image lifting line + wave potential + 2D panel sections | h_te/c ≥ 1/2 at Fr 4.3 | h_te/c ≤ 1/4 | Paper (no code) | Verified |
| JMSA 2026 windfoil tank tests [S6] | Free-surface effect on wingfoil-class foil | Towing tank, h/c 0.5–9.5, Fr_h to 5 | Empirical correction form; direct relevance | Re 10⁵ (model scale); rear wing/mast included in some data | Paper | Verified |
| Harwood/Young/Ceccio 2016; Aguiar Ferreira 2025 [S7][S8] | Surface-piercing ventilation | Towing tank regime maps | Necessary conditions, Fr_h/α/AR map | No design formula; AR ~ O(1) | Papers | Verified |

## Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

*Source: [08-simulation-openfoam-su2-interop.md](08-simulation-openfoam-su2-interop.md) (`kb-hw-simulation-openfoam-su2-interop`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| OpenFOAM ESI v2606 (`simpleFoam`, `snappyHexMesh`, `interFoam`, `interPhaseChangeFoam`) | Directory of dictionaries + family of executables | Process pipeline; function objects emit `.dat` | Full hydrofoil physics ladder incl. VOF, cavitation, transition; automatic hex meshing with layers; multi-arch Docker image | No native mac/win binaries; six-monthly breaking releases; layer addition on thin TEs; GPL means process-only | GPL-3.0 | Verified [S1][S2][S26] |
| OpenFOAM Foundation 14 (`foamRun -solver incompressibleFluid`, `incompressibleVoF`) | Modular solvers since v11 | Same process boundary; different dictionary/application names | Cleaner solver modules; units as a standard feature (v14) | Different case grammar from ESI; `simpleFoam` only a script; fewer multi-arch container facts found | GPL-3.0 | Verified [S4][S5][S6] |
| SU2 v8.5.0 `INC_RANS` | One `.cfg`, one executable | Process; CSV history; VTK/Tecplot output | Prebuilt Linux/macOS/Windows binaries; LM transition; wall functions; simple mesh format; dimensional incompressible mode | No mesher, no VOF, no cavitation, no overset found; conda/Homebrew unavailable for mac/win | LGPL-2.1 | Verified [S17][S18][S19][S20][S21][S23] |
| OpenFOAM.app (gerlero) | Native mac bundle of ESI OpenFOAM | Read-only case-sensitive disk image; Homebrew tap | Fastest mac install; current (v2606) | Apple-silicon only; not notarized; community-maintained | GPL-3.0 | Verified [S29] |
| blueCFD-Core | Native Windows MinGW port | Installer | Native Windows without WSL | Unofficial; version lag not shown; support burden on us | GPL (Flagged) | Verified existence [S31] |
| foamlib | Typed Python I/O + async case runner | Parse/serialise dictionaries and fields (ASCII/binary) | Proves typed round-trip is feasible for both lines | GPL-3.0 and Python — not a dependency | GPL-3.0 | Verified [S30] |
| PyFoam / fluidsimfoam / pysu2 / FADO | Python automation | Generate, run, parse | Mature practice | GPL/Python-bound | GPL / LGPL | Flagged |
| CliWrap (.NET) | Fluent process wrapper | Pipes, events, graceful+forceful cancel, exit validation | Exactly the launch/stream/cancel primitives needed | Does not cross docker/wsl seams | MIT | Verified [S38] |
| duct (Rust) | Shell-like pipelines | `.reader()` streaming, errors by default | Same primitives in Rust | Kill-tree semantics not confirmed this session | MIT | Verified licence [S39] |
| Docker Desktop | CFD substrate on mac/win | Container runtime | One pinned image, both OSes, arm64 | Commercial terms for larger orgs; admin install; volume performance (Flagged) | Proprietary subscription | Verified [S28] |
| ITTC 7.5-03-01-01 / 7.5-03-02-03 (2024) | V&V and practical CFD guidelines | Procedure | Defines "converged", mesh floor, domain size | Ship-oriented examples; no hydrofoil-specific y+ table | ITTC © | Verified [S32][S33] |
| NASA TMR | Verification/validation cases + reference results | Public grids/data | Oracle for install smoke test and turbulence-model correctness | Air, M 0.15–0.2; not water, no free surface | Public | Verified [S35][S36] |

## Optimization strategies for 2D foil sections and 3D hydrofoil wings

*Source: [09-optimization-strategies.md](09-optimization-strategies.md) (`kb-hw-optimization-strategies`).*

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

## Integration and composition of simulation, optimization, simple algorithms and AI into hydrofoil modeling workflows

*Source: [10-integration-and-ai-workflows.md](10-integration-and-ai-workflows.md) (`kb-hw-integration-and-ai-workflows`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| OpenMDAO [S11][S12][S13] | Model = graph of components with derivatives; driver outside | Problem/Group/Component, unified derivatives, SqliteRecorder | Provenance of every case; gradient plumbing; textbook MDO home | Python-only; batch, not interactive; no UI notion of staleness | Apache-2.0 | Verified |
| Dakota [S20] | Driver ↔ black-box simulation via files/processes | fork/system/direct interfaces; surrogate-based opt; UQ | Interface taxonomy; mature UQ | LGPL; heavy; not embeddable under COMMIT-02 | LGPL-2.1-or-later | Verified |
| AeroSandbox + NeuralFoil [S1][S2][S22] | One airplane model, several fidelities, differentiable | AeroBuildup/VLM/LiftingLine + NeuralFoil sections; CasADi Opti | Façade over fidelities; confidence output; MIT | Python; aircraft conventions; no provenance store | MIT | Verified |
| OpenVSP / VSPAERO [S23][S24][S25] | Parametric geometry + external VLM/panel run | DegenGeom + `.vspaero` → `.history/.lod/.adb` | Clear file contracts; API + CLI | Provenance by filename; NOSA-1.3 excludes linking | NOSA-1.3 (OSI-approved, GPL-incompatible) | Verified |
| preCICE [S21] | Partitioned multi-physics coupling | Library + adapters (OpenFOAM, CalculiX, …) | Co-simulation done right | Irrelevant for v1; LGPL-3.0 | LGPL-3.0 | Verified |
| salsa [S40] | Incremental computation as query memoization | Inputs, tracked functions, early cut-off, durability, cancellation | Exactly the staleness semantics ANA-07 wants | Rust only; no persistence of memo across sessions by default | MIT/Apache-2.0 | Verified |
| Snakemake [S48] | Files-as-DAG with rerun triggers | code/input/mtime/params/software-env | Invalidation on method and params, not just time | Batch CLI; Python | MIT | Verified |
| Dagster [S49] | Software-defined assets with lineage | Materialization + lineage UI | Asset staleness as a product concept | Cloud data-pipeline scale, not desktop | Apache-2.0 | Verified |
| ParaView state [S47] | Pipeline serialization | `.pvsm` / `.py` state; path-relocation dialog | Complete scene capture | Absolute paths; exports not linked to state | BSD-3-Clause (recall) | Verified behaviour; licence Flagged |
| KiCad DRC [S46] | Rules with severities over a design | Built-in + custom rule DSL; exclusions; navigable violations | The DRC model to copy | EDA-specific | GPL-3.0 (recall; pattern only) | Verified behaviour |
| Foam-Agent / ChatCFD / harness study [S27][S28][S29] | LLM agents set up and repair OpenFOAM cases | RAG over tutorials + run-repair loops | Runnable cases in-distribution | 62.5 % OOD; physical fidelity < execution success | Papers (code licences vary; not checked) | Verified |
| Text2CAD and successors [S32][S33] | Text → CAD program | Autoregressive / LLM code gen with critique loops | Benchmarks and metrics exist | Valid-but-wrong geometry; intent errors worse than none | Research code | Verified |
| Anthropic structured outputs + MCP [S7][S8][S9] | Schema-constrained model output; tool protocol | Constrained decoding; JSON-RPC tools with human consent | Guarantees the shape | Cannot express numeric bounds; annotations untrusted | API / spec | Verified |
| Zoo Zookeeper [S50] | Prompt → editable CAD | Hosted model + API, credits | Commercial proof that text-to-CAD ships | Limits page unreadable; hosted-only | Proprietary | Verified partial |

## Visualization for hydrofoil design and optimization

*Source: [11-visualization.md](11-visualization.md) (`kb-hw-visualization`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| ParaView [S17][S18][S19][S20] | General pipeline: sources → filters → views; time vs sequence animation | Qt GUI over VTK; pvpython/pvbatch scripts; extractors | Everything the workbench will never need; honest time model; export formats | Not embeddable in .NET without ActiViz; too general for a foil designer | BSD-3-Clause | Verified |
| VTK [S15][S16][S49] | Toolkit of filters and mappers | C++ + Python | Stream tracer semantics, decimation, formats | No permissive .NET binding | BSD-3-Clause | Verified |
| ActiViz [S28] | VTK in C# | Commercial wrapper | Full VTK in-process | Commercial, version-locked | proprietary | Verified |
| OpenFOAM `foamToVTK` / ParaView reader [S21][S22] | Case directory is the dataset | Utility conversion; reader module | Subset/patch-only export | GPL (process only); fork differences (file 05) | GPL-3.0 | Verified |
| SU2 outputs [S23] | Configurable writers | `OUTPUT_FILES` list | Surface VTK for free | LGPL (process only) | LGPL-2.1 | Verified |
| XFOIL [S1] | Section analysis with Cp/polar plots | Fortran, text UI | The chart conventions of record; Ncrit vocabulary | No 3D; no colour/accessibility | MIT-like (Flagged, not opened) | Verified content |
| XFLR5 [S50] | XFOIL + LLT/VLM/panel GUI | Qt app | Polar/Cp graph conventions users know | GPLv3; "Abandoned" | GPL-3.0 | Verified |
| Crameri Scientific colour maps [S3][S4][S5] | Colour as a measurement instrument | Perceptually designed maps + paper | Criteria; MIT; CVD tested | — | MIT | Verified |
| Moreland colour advice [S6][S11][S12] | Maps for 3D shaded surfaces | Diverging cool–warm; "Fast" | Surface-safe maps; midpoint rule | — | free tables (licence not stated on page — Flagged) | Verified |
| Turbo [S9] | Better rainbow | Polynomial fit of a smoothed jet | Removes banding | Lightness ambiguity; not grayscale-safe | not stated | Verified |
| ScottPlot [S24][S27] | Interactive .NET plotting | Skia/OpenGL controls | Avalonia control; MIT; active | Scientific axis idioms (inverted Cp, gaps) need own code (Flagged) | MIT | Verified metadata |
| LiveCharts2 / OxyPlot [S24][S27] | .NET charts | Skia | MIT | Prerelease line / stale Avalonia package | MIT | Verified metadata |
| Silk.NET / Avalonia GL [S24][S26] | Raw GPU in .NET | Bindings + GL control | Permissive, active | Everything above the API is ours to write | MIT | Verified metadata |
| wgpu / vtkio / egui_plot / plotters [S24][S27] | Rust viewport and charts | WebGPU-style API; VTK I/O; plots | Permissive, active | Hosting inside Avalonia is unproven (Flagged) | MIT/Apache-2.0 | Verified metadata |
| Veldrid / rend3 [S25][S24] | .NET / Rust graphics abstractions | — | — | Unmaintained / archived | MIT / Apache-2.0 | Verified |
| Plotly.js / D3 / three.js [S24] | Web charts/3D | Browser | Rich, permissive | Needs a WebView; offline/a11y proof burden | MIT / ISC / MIT | Verified metadata |
| Tecplot / FieldView / EnSight | Commercial CFD post | Desktop apps | Industry chart idioms | Cost; closed | proprietary | Flagged (not opened) |

## Structures, materials, manufacturing and safety for water-sports hydrofoils

*Source: [12-structures-materials-and-manufacturing.md](12-structures-materials-and-manufacturing.md) (`kb-hw-structures-materials-and-manufacturing`).*

| Solution / source | How it frames the problem | Approach | Does well | Does badly | Licence | Confidence |
|---|---|---|---|---|---|---|
| DCFoil.jl (mdolab, Michigan) [S22][S24][S1] | Composite marine appendage design optimisation with low-order models | 9-DOF composite beam FE + lifting line/strip theory; static Newton–Raphson; dynamic modes; reverse-mode AD | The exact fidelity this tool needs; T-foil meshing; published fixtures; permissive licence | Julia (process invocation or port required); research code; no manufacturing or failure-index layer | Apache-2.0 | Verified |
| Faye et al. equivalent-beam FE (ENSTA/IRENav) [S6][S7] | Replace shell/solid FE with 1D beam for composite foils | BEM flow + 1D beam, partitioned; validated on 4 foils, one mould | Demonstrates beam adequacy against experiment and 3D FE; section-analysis route to EI/GJ from layup | Code not published; NACA 0015 symmetric only | n/a (paper) | Verified |
| Young/Harwood 2-DOF FSI model [S2] | Bend-twist as a load-dependent effective-incidence shift | Two-DOF (bend + twist) with measured coefficients | Collapses all foils onto one curve; cheapest possible screen | Not a sizing tool; needs measured or beam-derived stiffness | n/a (paper) | Verified |
| Akcabay & Young plate models [S3][S4] | Divergence/flutter of composite plates in water | Coupled modal analysis with speed-dependent fluid loads | Names the water-specific single-mode flutter; sweep × fibre-angle map | Plates, not foils; no code | n/a (paper) | Verified |
| UNSW AFP hydrofoil programme [S27][S28][S9][S8] | Manufacture tailored layups automatically and test full scale | RTM mould reused as AFP tool; static cantilever, EMA, 10⁵-cycle fatigue with DOFS | Full-scale stiffness/fatigue evidence; manufacturing method for curvilinear fibres | Industrial AFP is out of reach for the target user; 20 % figure not re-opened | n/a | Verified abstracts / Flagged 20 % |
| Hubs/Protolabs design guides [S18][S19][S20] | Generic DFM floors | Vendor rules for CNC, injection moulding, SLS | Numbers with a source (tolerance, wall, draft, shrink) | Not foil- or composite-layup-specific | proprietary web content | Verified as published |
| FreeCAD CAM + OpenCAMLib [S23] | Open CAM for 3-axis surfaces | Path/CAM workbench; OCL drop-cutter | Free, scriptable, cross-platform | LGPL (process-only under COMMIT-02); 5-axis weak | LGPL-2.1 | Verified licence; capability Flagged |
| Fusion 360 / Mastercam / PowerMill / Vectric CAM | Commercial CAM for molds and plugs | 3-axis and 5-axis surface strategies | Industry default in foil/mold shops | Proprietary; not opened this session | proprietary | Flagged (recall) |

## Validation data, special hydrofoil physics and testing numerical design software

*Source: [13-validation-special-physics-and-numerical-testing.md](13-validation-special-physics-and-numerical-testing.md) (`kb-hw-validation-special-physics-and-numerical-testing`).*

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

