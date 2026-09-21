---
id: kb-hw-simulation-openfoam-su2-interop
title: "Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: knowledge
tags: [hydrofoil, cfd, openfoam, su2, meshing, interop, installation, validation, licensing]
links:
  - { to: kb-hydrofoil-workbench, rel: refines }
  - { to: kb-cfd-workbench-grounding, rel: depends-on }
  - { to: spec-cfd-workbench, rel: relates-to }
review-by: 2026-12-19
summary: >-
  Establishes which OpenFOAM (ESI v2606 / Foundation 14) and SU2 (v8.5.0) solvers, models and
  meshes answer which hydrofoil question at Re 5e5–2e6, what the ITTC verification procedure
  requires before a result may be called converged, and why both solvers must be driven as
  child processes over generated files (never linked) from C#/Rust. Main design implication:
  meshing, not solving, is the gating capability; SU2 ships no mesher and no free-surface or
  cavitation solver, so the v1 backend matrix is a two-solver matrix by physics, not a choice.
---

# Simulation with OpenFOAM and SU2 for hydrofoils, and process-driven interop from C#/Rust

**Compiled:** 2026-09-20 · **Lead:** Domain Researcher · **Questions this answers:**
1. Which solver and model for which hydrofoil question (steady RANS, transition, wall treatment, free surface, cavitation, unsteady) and what a laptop can honestly run.
2. Meshing: snappyHexMesh, cfMesh, Gmsh, SU2 formats; quality metrics; y+ / first-cell height; mesh-independence (ITTC 7.5-03-01-01) before "converged".
3. Case generation without hand-edited dictionaries: the OpenFOAM case tree, function objects, the dictionary grammar, SU2's `.cfg`; what a typed case model serialises; how existing wrappers do it.
4. Process-driven interop from C#/Rust: launching, streaming, cancelling, exit codes vs completion, restart, Docker/WSL2/native, parsing outputs, why native bindings are out.
5. Installation accountability per OS: what tool-owned install can mean on macOS and Windows; binaries, pins, digests, elevation, licences.
6. Validation cases for hydrofoils: what each dataset validates, access and terms.
7. Result harvesting: what to read back, formats, and the run-record metadata.

**Method note.** The session's WebSearch budget was already exhausted when this area started (200/200
session-wide), so every source below was reached by direct URL fetch of primary documentation, source trees,
registry APIs (Docker Hub, conda-forge, Crossref) and two ITTC procedure PDFs extracted locally. Papers were
located through Crossref by title/DOI only; their abstracts were **not** read, so paper-level claims are
labelled Flagged. Anything from recall that no fetched source confirmed is labelled Flagged.

## Headline findings

1. The current releases are **OpenFOAM-v2606 (OpenCFD/ESI, 26 June 2026)**, **OpenFOAM 14 (Foundation, 14 July 2026)** and **SU2 v8.5.0 "Harrier" (April 2026)**; the brief's "v2506/v2412" and "Foundation v13" are one release behind on each line. — *(Verified, [S1][S4][S16])*
2. **SU2 has no VOF/free-surface, no cavitation and no mesh generator.** Its documented `SOLVER` list is EULER/NAVIER_STOKES/RANS, INC_* variants, NEMO, FEM, HEAT, ELASTICITY and MULTIPHYSICS; meshes come from Pointwise, Gmsh, CGNS or the trivial ASCII `.su2` format. Free surface, ventilation and cavitation are OpenFOAM-only in this stack. — *(Verified, [S19][S21])*
3. OpenFOAM ESI provides the whole hydrofoil physics ladder as separate executables: `simpleFoam`/`pimpleFoam` (single phase), `kOmegaSSTLM` (Langtry–Menter γ–Reθ, 4-equation) and `kkLOmega` (incompressible-only) transition, `interFoam`/`interIsoFoam` (VOF), `interPhaseChangeFoam` with **Kunz, Merkle, SchnerrSauer** cavitation models, `cavitatingFoam`, and a new **GEKO** RANS model in v2606. — *(Verified, [S2][S7][S9][S10][S11])*
4. SU2 v8 has a **Langtry–Menter LM transition model** (`KIND_TRANS_MODEL= LM`, `LM_OPTIONS` with LM2015 and seven correlation variants) and **wall functions per marker** (`MARKER_WALL_FUNCTIONS` with STANDARD/ADAPTIVE/SCALABLE/EQUILIBRIUM/NONEQUILIBRIUM), so an SU2 single-phase hydrofoil case can be run either wall-resolved (y+ ≈ 1) or wall-modelled. — *(Verified, [S20][S23])*
5. **ITTC 7.5-03-02-03 Rev 02 (2024)** fixes the mesh floor: first cell y+ ≤ 1 with ≥ 20 points in the boundary layer and expansion ≤ 1.2 for wall-resolved, or 30 < y+ < 100 with ≥ 15 points for wall functions; inlet ≥ 10 L upstream and outlet ≥ 20 L downstream of a lifting surface; the first-cell height formula is `y = y+ · L / (Re_L · sqrt(Cf/2))` with `Cf = 0.455/[ln Re_L]^2.58`; the recommended convergence criterion is a ≥ 3-order drop of scaled residuals, with force convergence as the fallback. — *(Verified, [S33])*
6. **ITTC 7.5-03-01-01 Rev 05 (2024)** defines what "mesh-converged" means: three-grid generalized Richardson extrapolation (`p = ln(ε32/ε21)/ln r`, `δ_RE = ε21/(r^p − 1)`, `U = F_S·|δ_RE|`, `F_S` from Xing & Stern's P-ratio) or the Eça–Hoekstra least-squares method on ≥ 4 grids with `F_S = 1.25` when 0.5 ≤ p < 2.1 and the fit scatter σ is below the data range Δφ. A tool that labels a result "converged" without this study is making a claim the procedure does not support. — *(Verified, [S32])*
7. The **NASA TMR NACA 0012 validation case** (M = 0.15, Re = 6e6, Ladson tripped force data, Gregory & O'Reilly pressure data) is the correct fully-turbulent oracle for a single-phase backend smoke test; untripped data show ≈ 10 % higher drag, so a transition-model run must be compared against the *untripped* set, never the tripped one. — *(Verified, [S35])*
8. Both official OpenFOAM lines have **no native macOS or Windows binaries**: OpenCFD lists Docker, WSL and MinGW cross-compilation for Windows and Docker for macOS; the Foundation lists WSL for Windows and Multipass for macOS. The community **OpenFOAM.app** (gerlero) is Apple-silicon-only (macOS 14+), GPL-3.0, installed via `brew install gerlero/openfoam/openfoam`, and **not notarized by Apple**. The official `opencfd/openfoam-default` image is multi-arch (amd64 + arm64) for 2306–2512; 2606 was not yet tagged on 2026-09-20. — *(Verified, [S3][S4][S26][S29])*
9. **SU2 on macOS and Windows means the GitHub release binaries**: the `su2` Homebrew formula does not exist (404) and conda-forge `su2` 8.5.0 ships only `linux-64` and `linux-aarch64`; there is no `su2code/su2` Docker Hub repository. This contradicts the proposal's "SU2 via Homebrew/conda" assumption. — *(Verified absence on 2026-09-20, [S18][S24][S25][S27])*
10. **Docker Desktop is free for personal use, education, non-commercial open source and businesses with < 250 employees AND < $10 M revenue**; larger organisations need a paid subscription; Docker Engine on Linux is unaffected. A product that routes CFD through Docker Desktop inherits this term for its users. — *(Verified, [S28])*
11. Process termination is the interop's hardest correctness problem, not launching: .NET `Process.Kill(entireProcessTree: true)` kills descendants but **silently skips processes it lacks permission to inspect** and `WaitForExit` does not wait for descendants; CliWrap (MIT) adds graceful (Ctrl-C-equivalent) then forceful cancellation; neither reaches inside a Docker container or a WSL distro, where the tree must be terminated by `docker kill <name>` / a signal inside the distro. — *(Verified for .NET/CliWrap, Inferred for the container/WSL seam, [S37][S38])*
12. Since **OpenFOAM 11 the Foundation line runs modular solvers (`foamRun -solver incompressibleFluid`); `simpleFoam` survives only as a compatibility script** that runs that module. A typed case model cannot target "OpenFOAM" — it must target one line and version (ESI dictionaries and application names differ from Foundation ones). — *(Verified, [S5])*
13. OpenFOAM's native evidence emitters exist as function objects with documented outputs: `forces` (`force.dat`, `moment.dat`), `forceCoeffs` (`coefficient.dat`, Cd/Cl/Cm split into pressure/viscous), `solverInfo` (initial/final residual, iterations, **converged flag** per field). Convergence and forces are therefore file-harvestable, not stdout-scraped, on the ESI line. — *(Verified, [S13][S14][S15])*
14. The Python ecosystem (foamlib GPL-3.0, PyFoam, pysu2) proves the process-plus-files boundary works and also shows every mature wrapper is GPL or Python-bound; there is no maintained .NET or Rust OpenFOAM/SU2 parser, so the workbench writes its own serialiser and reads VTK/CSV interchange. — *(Verified for foamlib and SU2 outputs, Flagged for absence of .NET/Rust libraries — asserted from a failed registry lookup budget, [S30][S22])*
15. **Recommended v1 backend (Inferred):** OpenFOAM ESI (`snappyHexMesh` + `simpleFoam`, pinned image digest) for the 3D wing-in-a-box slice on both OSes, because the gating capability is unattended 3D meshing with boundary layers and only OpenFOAM ships that; SU2 `INC_RANS` as the first **2D section** backend where the workbench writes the `.su2` mesh itself. The cheapest probe that could flip this is a Gmsh boundary-layer wing mesh exported to `.su2` across AR 5/8/12.

## State of the art

### Solver and model selection for hydrofoil questions (Re 5e5–2e6)

**Single-phase steady RANS.** For attached flow at moderate angle of attack in unbounded water,
incompressible steady RANS is the workhorse: OpenFOAM ESI `simpleFoam` (SIMPLE-family pressure–velocity
coupling) or SU2 `SOLVER= INC_RANS` with `INC_DENSITY_MODEL= CONSTANT`, `VISCOSITY_MODEL=
CONSTANT_VISCOSITY`, `INC_DENSITY_INIT` and `MU_CONSTANT` set from the run's fluid snapshot (SU2's template
default of 998.2 kg/m³ is a placeholder, not the workbench's sourced water). Turbulence: `kOmegaSST` in
OpenFOAM, `KIND_TURB_MODEL= SST` with `SST_OPTIONS= V2003m` in SU2 (SU2 also lists V1994m and production
limiters VORTICITY/KATO_LAUNDER/UQ). *(Verified, [S8][S19][S20][S23])* SU2's SA model is available with
NEGATIVE/EDWARDS/BCM variants and QCR2000; SA is the TMR's reference model for the NACA 0012 case and is a
sensible second opinion, but SST is the better default for adverse-pressure-gradient separation on thick
water-sports sections. *(Verified availability [S20]; Inferred preference.)*

**Why transition matters at these Re.** A 0.2 m chord at 6–10 m/s in 15 °C seawater is Re ≈ 1.0–1.7e6; at
these Reynolds numbers laminar separation bubbles and natural transition move with α and roughness, and the
drag difference between tripped and free-transition data on a NACA 0012 at Re 3–6e6 is about 10 % in the
wind-tunnel record the TMR curates. *(Verified for the TMR statement [S35]; Inferred for the hydrofoil Re
range.)* Fully turbulent SST therefore over-predicts drag and mis-places Cp_min for a polished foil; a
transition model is required if the 2D-polar tier (NeuralFoil/XFOIL with Ncrit) and the CFD tier are to be
reconciled rather than merely different. OpenFOAM ESI provides `kOmegaSSTLM` (4-equation Langtry–Menter
2009, fields `ReThetat` and `gammaInt`, coefficients under `kOmegaSSTCoeffs` including `ca1 ca2 ce1 ce2
cThetat sigmaThetat`) and `kkLOmega` (incompressible-only, under
`src/TurbulenceModels/incompressible/turbulentTransportModels/RAS`). SU2 provides `KIND_TRANS_MODEL= LM`
with `LM_OPTIONS` (version LM2015; correlations MALAN, SULUKSNA, KRAUSE, KRAUSE_HYPER, MEDIDA,
MEDIDA_BAEDER, MENTER_LANGTRY) and the free-stream inputs that drive it (`FREESTREAM_TURBULENCEINTENSITY`
default 0.05 = 5 %, `FREESTREAM_TURB2LAMVISCRATIO` default 10). *(Verified, [S7][S9][S20])* The design
consequence: **free-stream turbulence intensity is now a case input with physical meaning** (it is the
CFD-side analogue of Ncrit), must be stored with the run, and a tool that leaves SU2's 5 % default in place
is silently simulating a very turbulent tank, not open water. *(Inferred.)* Recent literature confirms the
class of problem: a 2022 Physics of Fluids paper on capturing transition around a low-Re hydrofoil with a
zero-equation transition model, a 2023 Fluids assessment of a RANS transition model on foils at moderate Re,
and a 2024 Ocean Engineering paper applying the Wray–Agarwal algebraic transition model to unsteady
cavitating hydrofoil flow all exist (titles/DOIs located via Crossref; abstracts not read). *(Flagged,
[S42])*

**Wall treatment.** ITTC 7.5-03-02-03 Table 1: wall-resolved needs first point y+ ≤ 1, expansion ratio 1.2,
≥ 20 points in the boundary layer; wall functions need 30 < y+ < 100, expansion 1.2, ≥ 15 points, and the
procedure warns that wall functions "become less valid, or even invalid, with increasing adverse pressure
gradients". *(Verified, [S33])* Transition models require the resolved treatment (the γ–Reθ correlations act
in the laminar and transitional sub-layers), so "transition on" implies "y+ ≈ 1", which multiplies cell
count and aspect ratio. *(Inferred from [S7][S33].)* OpenFOAM uses wall-function boundary conditions on the
`nut`/`k`/`omega` fields (`nutkWallFunction` etc.); SU2 selects per marker with `MARKER_WALL_FUNCTIONS= (
wing, STANDARD_WALL_FUNCTION )` and defaults to `NO_WALL_FUNCTION`. *(Verified for SU2 [S23]; OpenFOAM BC
names Flagged — from recall.)*

**Free surface.** OpenFOAM ESI ships `interFoam` (algebraic VOF with MULES) and `interIsoFoam` (geometric
isoAdvector VOF) as separate solvers, plus `potentialFreeSurfaceFoam`; the Foundation line has
`incompressibleVoF` as a module. SU2 has none. *(Verified, [S11][S19]; Foundation module name Flagged.)*
Near-surface lift loss, wave drag and the onset of ventilation are the hydrofoil-specific questions here;
RANS-VOF on 2D hydrofoils near a free surface is an active 2022–2024 topic (a Chesapeake Sailing Yacht
Symposium 2022 / Journal of Sailing Technology 2023 paper on free-surface effects on 2D hydrofoils by
RANS-VOF, and a 2024 Applied Ocean Research paper comparing BEM and RANS for a hydrofoil near the free
surface). *(Flagged — located by title/DOI only, [S41].)* ITTC explicitly ties outlet distance to "the
reflection of gravity waves" for free-surface computations, so the domain must be longer and damped
downstream; this is why the spec keeps free surface behind a separate de-risk slice. *(Verified for the ITTC
statement [S33]; Inferred mapping.)*

**Cavitation.** `interPhaseChangeFoam` (VOF + mass transfer) with the `Kunz`, `Merkle` and `SchnerrSauer`
phase-change models, and `cavitatingFoam` (barotropic compressibility model) are present in ESI master.
*(Verified, [S10][S11])* Cavitation needs the sourced vapour pressure the spec already demands (ANA-15) and
the cavitation number σ = (p∞ − p_v)/(½ρV²) as an input. Delft twist-11 remains the reference 3D sheet/cloud
cavitation case: 2018–2023 papers cover turbulence-model comparison (CAV2018), multi-process cavitation
modelling (2021), erosion-risk assessment with hybrid Eulerian–Lagrangian methods (2023) and LES of
cavitation tones (2022). *(Flagged — titles/DOIs via Crossref, [S42].)* SU2 has no cavitation model in the
documented solver set. *(Verified absence in docs [S19]; Inferred that none exists in the release.)*

**Unsteady, pumping and 6-DoF.** `pimpleFoam` and the dynamic-mesh/overset solver variants exist in the ESI
line, with `sixDoFRigidBodyMotion` as the rigid-body library; these are the tools for a pumping foil or a
heave/pitch response. *(Flagged — solver family from recall; the multiphase listing in [S11] does not show
overset variants because ESI keeps them inside the base solver directories.)* SU2 supports `TIME_DOMAIN=
YES` dual-time stepping and mesh deformation (`SU2_DEF`, RBF deformation since v8.3.0) but not overset in
the documented options fetched here. *(Verified for TIME_DOMAIN and RBF [S17][S23]; Flagged for overset.)*

**What is realistic on a laptop.** No fetched source gives wall-clock figures, so the following are
**Inferred cost classes** built from the ITTC mesh floor and typical cell budgets, to be replaced by the
workbench's own `solver.process` measurements (IO-series rule): a 2D section RANS at y+ ≈ 1 is 5e4–2e5 cells
and converges in minutes on 4 cores; a 3D half-wing at y+ 30–100 with wall functions is 2–5 M cells and 1–4
h on 8 cores; a 3D half-wing wall-resolved with transition is 8–20 M cells and a workstation-day; a VOF
free-surface 3D case with a resolved wave field is 10–40 M cells and unsteady, i.e. cloud or cluster class.
*(Inferred; every number here is a placeholder the instrumented product must overwrite.)*

**Decision table (question → solver → model → mesh class → cost class → honest claim).**

| Hydrofoil question | Solver (ESI / SU2) | Turbulence / physics | Mesh class | Cost class (Inferred) | What the result can honestly claim |
|---|---|---|---|---|---|
| 2D section Cl/Cd/Cp, attached, Re 5e5–2e6 | `simpleFoam` / `INC_RANS` | SST, fully turbulent, y+ 30–100 | Structured C-mesh or snappy 2D extrusion, 5e4–1e5 cells | minutes, laptop | Tripped-equivalent section coefficients; drag high by ~10 % vs free transition |
| 2D section with laminar bubble / free transition | `simpleFoam` + `kOmegaSSTLM` or `kkLOmega` / `INC_RANS` + `LM` | Transition on, y+ ≤ 1, TI stated | 1e5–2e5 cells | tens of minutes, laptop | Free-transition coefficients at the stated TI; Cp_min for cavitation screening |
| 3D wing lift/drag/spanwise loading, deep submergence | `simpleFoam` / `INC_RANS` | SST, wall functions | snappyHexMesh with layers or Gmsh BL, 2–5 M cells | hours, 8 cores | Wing-only forces in unbounded water; no free surface, no strut, no ventilation |
| 3D wing, resolved BL, transition | `simpleFoam` + `kOmegaSSTLM` | y+ ≤ 1, ≥ 20 BL points | 8–20 M cells | workstation-day | As above with transition; sensitivity to TI must be reported |
| Lift loss / wave drag near surface | `interFoam` or `interIsoFoam` | VOF + SST, gravity, wave damping zones | Interface-refined, long domain, 5–40 M cells | cloud / cluster | Depth-dependent lift and wave resistance at one Froude number; validated only if Duncan-class data agree |
| Ventilation onset | `interFoam` (+ transient) | VOF, air entrainment path | as above plus strut/surface-piercing refinement | cluster | Qualitative onset; no accepted quantitative oracle in v1 |
| Sheet / cloud cavitation | `interPhaseChangeFoam` (Kunz / Merkle / SchnerrSauer) or `cavitatingFoam` | VOF + mass transfer, σ input | Suction-side refined, 2–10 M cells, unsteady | cluster | Cavity extent and shedding frequency vs Delft twist-11 class data |
| Pumping / heave-pitch response | `pimpleFoam` + dynamic mesh / `sixDoFRigidBodyMotion` | URANS | moving or overset mesh | cluster | Phase-averaged forces; not resolved turbulence |

*(Solver/model availability Verified [S7][S9][S10][S11][S19][S20]; mesh and cost classes Inferred; claim column Inferred from [S33][S35].)*

### Meshing

**snappyHexMesh pipeline.** `blockMesh` background hex → `surfaceFeatureExtract` (ESI) producing an `.eMesh`
feature-edge file → `snappyHexMesh` in three phases (`castellatedMesh`, `snap`, `addLayers`). The annotated
ESI dictionary documents: `features` ("Specifies a level for any cell intersected by explicitly provided
edges. This is a featureEdgeMesh"), `refinementRegions` (distance / inside / outside modes),
`locationInMesh`, and the `addLayersControls` entries `relativeSizes`, `expansionRatio`,
`finalLayerThickness`, `firstLayerThickness`, `thickness`, `minThickness` ("If for any reason layer cannot
be above minThickness do not add layer"), `nGrow`, `featureAngle` ("When not to extrude surface. 0 is flat
surface, 90 is when two faces are perpendicular"), `slipFeatureAngle`, `nRelaxIter`,
`nSmoothSurfaceNormals`, `nSmoothNormals`, `nSmoothThickness`, `maxFaceThicknessRatio` ("Stop layer growth
on highly warped cells"), `maxThicknessToMedialRatio` ("Reduce layer growth where ratio thickness to medial
distance is large"), `minMedialAxisAngle`, `nBufferCellsNoExtrude`, `nLayerIter`, `nRelaxedIter` ("Max
number of iterations after which relaxed meshQuality controls get used"). *(Verified, [S12])*

**Layer-addition failure modes on thin trailing edges.** The documented mechanisms are exactly the ones that
bite a hydrofoil: a sharp or thin TE has a small medial-axis distance, so `maxThicknessToMedialRatio`
collapses layer thickness near the TE; the TE edge angle exceeds `featureAngle`, so extrusion stops at the
edge; if the collapsed thickness falls under `minThickness` the layer is dropped entirely and
`nGrow`/`nBufferCellsNoExtrude` step the neighbouring layers down. The result is a wing with full layers
mid-chord and none at the TE, i.e. a Cd that is mesh-dependent in precisely the region that sets pressure
drag. *(Inferred from the documented parameter semantics [S12]; the practitioner remedy — a finite TE
thickness floor, a local refinement box, `featureAngle` raised, `nRelaxedIter` bounded — is Flagged, from
practice.)* This is why the spec's TE thickness floor (GEO stories) is also a meshing requirement, not only
a manufacturing one.

**cfMesh and Gmsh.** cfMesh (`cartesianMesh`) is bundled with the ESI distribution as a plugin and is GPL;
Gmsh is GPL-2.0-or-later and can export the `.su2` format directly; Pointwise/Fidelity are commercial. Both
GPL meshers are process-invocable under COMMIT-02. *(Gmsh→SU2 export Verified [S21]; licences Flagged — from
recall, not fetched this session.)* SU2 itself has no mesher; `SU2_DEF` deforms an existing mesh (RBF
deformation since v8.3.0) and `SU2_MSH` handles adaptation/periodicity preprocessing. *(Verified for RBF
[S17]; SU2_MSH role Flagged.)*

**SU2 mesh format.** ASCII `.su2` with `NDIME`, `NPOIN` (coordinates), `NELEM` (VTK element ids: line 3,
triangle 5, quad 9, tetra 10, hexa 12, prism 13, pyramid 14), `NMARK`, `MARKER_TAG`, `MARKER_ELEMS`;
`MESH_FORMAT= CGNS` is the alternative and SU2 has a built-in CGNS→SU2 converter. *(Verified, [S21][S23])*
The format is simple enough that the workbench can write a 2D structured C-mesh around a section itself —
that is the cheapest possible "no external mesher" path for the 2D slice. *(Inferred.)*

**Mesh quality that must pass.** OpenFOAM `checkMesh` reports non-orthogonality, skewness, aspect ratio,
face pyramids and determinant; snappyHexMesh's `meshQualityControls` gate the same metrics during
snapping/layering (`maxNonOrtho` — "Set to 180 to disable", `maxBoundarySkewness`, `maxInternalSkewness`,
`maxConcave`, `minVol`, `minTetQuality`, `minArea`, `minTwist`, `minDeterminant`, `minFaceWeight`,
`minVolRatio`, `minTriangleTwist`, `nSmoothScale`, `errorReduction`, `relaxed`). ITTC adds: all volumes
positive, "the 3x3 determinant for structured grids should be greater than 0.3", with a few cells down to
0.15 tolerable only with smaller time steps or more under-relaxation. *(Verified, [S12][S33])* The spec's
CFD-02 "failed mesh-quality threshold blocks solve" therefore has concrete measures: `checkMesh` must report
"Mesh OK" (or every failed check must be one the case explicitly tolerates), and the y+ *achieved* must be
checked a posteriori — ITTC: "y+ should always be checked a posteriori once the solutions are obtained".
*(Verified, [S33])*

**Mesh independence before "converged".** See Data section for the ITTC 7.5-03-01-01 equations. The product
rule that follows: a single-mesh result is labelled **Single mesh — grid uncertainty not quantified**; a
result may carry a grid uncertainty only after ≥ 3 systematically refined meshes (ratio r, ideally √2 in
every direction) with the same models, or ≥ 4 for the least-squares method. *(Verified for the procedure
[S32]; Inferred labelling rule.)*

### Case generation without hand-edited dictionaries

**OpenFOAM case as a typed structure.** `system/controlDict` (`application`, `startFrom`, `startTime`,
`stopAt`, `endTime`, `deltaT`, `writeControl`, `writeInterval`, `functions {…}`), `system/fvSchemes`,
`system/fvSolution` (solvers, `SIMPLE { residualControl … }`, relaxation), `system/blockMeshDict`,
`system/snappyHexMeshDict`, `system/decomposeParDict`; `constant/turbulenceProperties` (`simulationType RAS;
RAS { RASModel kOmegaSST; }`), `constant/transportProperties` (ESI) versus `constant/physicalProperties`
(Foundation ≥ 11), `constant/triSurface/*.stl`; `0/U 0/p 0/k 0/omega 0/nut` (plus `0/ReThetat 0/gammaInt`
for `kOmegaSSTLM`) with `boundaryField` entries per patch. *(File and keyword names Verified where they
appear in [S7][S12][S15][S40]; the full list is Inferred from those and from recall — Flagged for exact
Foundation-14 names.)*

**Dictionary grammar.** Free-form C++-commented text; every file opens with a `FoamFile { version; format ascii|binary; class; object; }` header; keyword entries end in `;`, sub-dictionaries in `{}`, lists in `()` optionally prefixed by a count; dimensions as a 7-vector `[kg m s K mol A cd]` or a named unit; macro expansion `$keyword`, scoped `$../x`, `$:a.b`, `${…}`; directives `#include`, `#includeIfPresent`, `#includeEtc`, `#calc`, `#codeStream`, `#remove`, `#if…#else…#endif`. *(Verified, [S40])* The serialiser must therefore emit **only** the closed subset it can also parse back (no `#calc`, no `#codeStream`, no macros), because `#codeStream` compiles arbitrary C++ at run time — a security boundary the spec's "hostile data never becomes commands" row must name.

**Function objects to emit by default** (ESI): `forces` (`type forces; libs (forces); patches (wing); rhoInf
<ρ>; CofR (…)` → `force.dat`, `moment.dat` with total/pressure/viscous columns), `forceCoeffs` (`magUInf`,
`lRef`, `Aref`, `liftDir`/`dragDir`/`pitchAxis` or `e1`/`e3`, `coefficients (Cd Cl CmPitch …)` →
`coefficient.dat`), `solverInfo` (`fields (U p k omega)` → per-field initial/final residual, iterations,
converged flag, and optional residual *fields*), `yPlus`, `wallShearStress`, and `sample`/`surfaces` for
slices and surface Cp. *(Verified for forces/forceCoeffs/solverInfo [S13][S14][S15];
yPlus/wallShearStress/sample names Flagged — from recall.)* Note `forceCoeffs` needs `Aref` and `lRef` — the
derived projected area and reference chord the spec says are derived, never stored: the run record must pin
the values used, with their derivation revision.

**SU2 single `.cfg`.** Key groups verified from the template: problem (`SOLVER`, `KIND_TURB_MODEL`,
`SST_OPTIONS`, `KIND_TRANS_MODEL`, `MATH_PROBLEM`, `RESTART_SOL`); incompressible fluid
(`INC_DENSITY_MODEL`, `INC_DENSITY_INIT`, `INC_VELOCITY_INIT`, `INC_NONDIM` —
INITIAL_VALUES/REFERENCE_VALUES/DIMENSIONAL, `VISCOSITY_MODEL`, `MU_CONSTANT`, `REYNOLDS_NUMBER`,
`REYNOLDS_LENGTH`); references (`REF_AREA` — "0 implies automatic calculation", `REF_LENGTH`,
`REF_ORIGIN_MOMENT_X/Y/Z`); markers (`MARKER_HEATFLUX` or `MARKER_ISOTHERMAL` for no-slip walls,
`MARKER_FAR`, `MARKER_INLET`, `MARKER_OUTLET`, `MARKER_SYM`, `MARKER_MONITORING`, `MARKER_WALL_FUNCTIONS`);
convergence (`CONV_FIELD` e.g. `DRAG` or `RMS_DENSITY`, `CONV_RESIDUAL_MINVAL` as log10, `CONV_STARTITER`,
`CONV_CAUCHY_ELEMS`, `CONV_CAUCHY_EPS`); I/O (`MESH_FORMAT`, `MESH_FILENAME`, `SOLUTION_FILENAME`,
`RESTART_FILENAME`, `CONV_FILENAME`, `OUTPUT_FILES`, `OUTPUT_WRT_FREQ` — one frequency per output file,
`HISTORY_OUTPUT`, `VOLUME_OUTPUT`, `SCREEN_WRT_FREQ_INNER`, `HISTORY_WRT_FREQ_INNER`); time (`TIME_DOMAIN`,
`TIME_ITER`, `INNER_ITER`, `ITER`). *(Verified, [S19][S22][S23])* For an incompressible water case the
workbench should use `INC_NONDIM= DIMENSIONAL` so that the history file's forces are in SI and the fluid
snapshot is applied verbatim. *(Inferred.)*

**Marker naming from geometry.** The workbench owns the loft, so it owns the patch/marker names: recommend a
fixed vocabulary `wing`, `wing_upper`/`wing_lower` only if separately needed, `symmetry` (root plane of a
half model), `inlet`, `outlet`, `farfield` (sides/top/bottom as one slip or far-field patch), and later
`strut`, `fuselage`, `freeSurfaceTop`. The same names are the STL solid names given to `snappyHexMesh`
(`refinementSurfaces { wing { … patchInfo { type wall; } } }`) and the `MARKER_TAG`s written into `.su2`, so
the harvest layer reads forces by the same identifier on either backend. *(Inferred; snappy patchInfo
Flagged.)*

**How existing wrappers do it.** foamlib (GPL-3.0) exposes `FoamFile`/`FoamFieldFile` typed dict-like
read/write of ASCII and binary (optionally compressed) files and `FoamCase`/`AsyncFoamCase` runners for both
openfoam.com and openfoam.org distributions. *(Verified, [S30])* PyFoam and fluidsimfoam are
older/alternative Python layers of the same shape (generate dictionaries, run utilities, parse logs).
*(Flagged — not fetched.)* SU2's own automation is the `SU2_PY` package with `pysu2` (a rebuilt Python
wrapper since v8.0.0; enhancements in v8.3.0), `shape_optimization.py` and the FADO framework. *(Verified
for SU2_PY/pysu2 existence [S17][S18]; FADO Flagged.)* Every one of these is a process-plus-files design,
and every one is Python or GPL — none can be a dependency of a permissive C#/Rust product (COMMIT-02), but
their file-level behaviour is the specification to reproduce.

### Process-driven interop from C# and Rust

**Pipeline as child processes.** OpenFOAM: `blockMesh` → `surfaceFeatureExtract` → `snappyHexMesh
-overwrite` → `checkMesh` → (`decomposePar` → `mpirun -np N simpleFoam -parallel` → `reconstructPar`) or
serial `simpleFoam` → `foamToVTK` / `postProcess`. SU2: `SU2_CFD case.cfg` (serial) or `mpirun -np N SU2_CFD
case.cfg`; SU2 writes its own VTK/CSV outputs so no conversion step is needed. *(Verified for SU2 outputs
[S22]; OpenFOAM utility names Verified where they appear in [S12][S15], the rest Flagged from recall.)* In a
container the same argv runs under `docker run --rm --name <run-id> -v <case>:/case -w /case
<image@sha256:…> <argv>`; under WSL2 under `wsl.exe -d <distro> -e bash -lc '<source bashrc>; <argv>'`.
*(Inferred.)*

**Streaming residuals and forces.** SU2 writes the history file (`CONV_FILENAME`, CSV with a header row
naming the `HISTORY_OUTPUT` fields such as `RMS_DENSITY`, `RMS_MOMENTUM-X`, `LIFT`, `DRAG`, `EFFICIENCY`)
every `HISTORY_WRT_FREQ_INNER` iterations, plus screen output at `SCREEN_WRT_FREQ_INNER`. *(Verified,
[S22][S23])* OpenFOAM ESI writes `postProcessing/<FO>/<time>/*.dat` for `solverInfo`, `forces` and
`forceCoeffs`; the classic `foamLog` script scrapes stdout into per-variable columns and is the fallback
when a function object is absent. *(Verified for the FO outputs [S13][S14][S15]; `foamLog` Flagged.)*
Design: tail the **files**, not stdout; stdout is for the log archive and for `AI-05` diagnosis, with the
file-based harvest as the only source of numbers.

**Cancellation and process trees.** .NET 10 `Process.Kill(bool entireProcessTree)` "immediately stops the
associated process, and optionally its child/descendent processes"; processes the caller cannot inspect "are
silently skipped", `WaitForExit`/`HasExited` "do not reflect the status of descendant processes", and an
`AggregateException` is raised when not all descendants could be terminated. *(Verified, [S37])* CliWrap
(MIT; .NET Standard 2.0+/.NET Core 3.0+) offers `PipeTarget`/`ListenAsync` line streaming,
`WithValidation(CommandResultValidation.None)` so a non-zero exit is data rather than an exception, and two
tokens — forceful and graceful (Ctrl-C-equivalent, "inherently cooperative"). *(Verified, [S38])* In Rust,
`duct` (MIT) provides shell-like pipelines with `.reader()` incremental output and error-by-default
semantics; `tokio::process` is the async baseline. *(Verified for duct's licence and reader [S39]; its
process-group/job-object kill semantics Flagged — the README fetch did not include them.)* The seam that
none of these libraries cross: killing `docker` or `wsl.exe` on the host does **not** guarantee the solver
inside dies. Cancellation must therefore be *addressed to the substrate*: `docker kill <run-id>` for a
container started with a known `--name`, and a signal delivered inside the distro (`wsl.exe -d <distro> -e
pkill -TERM -f <case-path>` or a recorded PGID) for WSL2, followed by host-side tree kill and a bounded
wait. *(Inferred; must be spiked on each substrate.)* MPI adds a layer: killing `mpirun` normally terminates
its ranks, but an orphaned rank after a hard host kill is a known failure shape to detect (`pgrep -f
<case-path>` after cancel). *(Flagged — practice, not fetched.)*

**Exit codes versus completion.** Both solvers return 0 when they stop at `endTime`/`ITER` regardless of
whether residuals converged; OpenFOAM also returns 0 when `residualControl` triggers an early "converged"
stop. *(Inferred from the residual-control design; Flagged for exact behaviour by version.)* The only
trustworthy completion oracle is the artefact set: for OpenFOAM the last time directory exists and contains
`U`/`p`, `postProcessing/solverInfo/<t>/solverInfo.dat` shows the final `converged` flags, and
`coefficient.dat`'s last row is within the declared averaging window; for SU2 the restart file named by
`RESTART_FILENAME` exists, `history.csv` ends at the iteration where `CONV_FIELD` met
`CONV_RESIDUAL_MINVAL`/Cauchy criteria or at `ITER`, and the `OUTPUT_FILES` were written. *(Verified for
what the files are [S15][S22][S23]; Inferred as an oracle.)* This is the concrete content of CFD-03 "a zero
exit code without required outputs is not success".

**Checkpoint / restart.** OpenFOAM: `startFrom latestTime` in `controlDict` resumes from the last written
time directory — so `writeInterval` is the checkpoint cadence and disk cost. SU2: `RESTART_SOL= YES` with
`SOLUTION_FILENAME` pointing at the previous `RESTART_FILENAME` (and `RESTART_ITER` for time-dependent
runs). *(Verified for SU2 [S19][S23]; OpenFOAM `startFrom latestTime` Flagged — recall.)* CFD-04's "resume
only where backend checkpoints actually support it" maps to: resume is offered only if the last checkpoint
is complete (all fields present, not truncated by the kill), which the harvester must verify before enabling
Retry-from-checkpoint.

**Substrates.** Official images: `opencfd/openfoam-default` (Docker Hub) is multi-arch amd64+arm64 for tags
2306–2512 and `latest` (last updated 2026-01-23); tags 2012–2212 are amd64 only; no `2606` tag existed on
2026-09-20. *(Verified, [S26])* There is no `su2code/su2` Docker Hub repository; SU2's containers (used for
CI) are elsewhere and are not a distribution channel. *(Verified absence [S27]; location Flagged.)* Native
macOS: OpenFOAM.app (gerlero) — ESI v2606/v2512 builds, Apple silicon only (Intel ended at v2.1.2 / v2506),
macOS 14+, GPL-3.0, Homebrew tap install, **not notarized**, runs from a read-only case-sensitive disk image
with user binaries in `$FOAM_USER_APPBIN`. *(Verified, [S29])* Foundation macOS: Multipass VM. *(Verified,
[S4])* Windows: WSL2 (both lines), Docker Desktop, MinGW cross-compilation (ESI) and the unofficial native
blueCFD-Core (latest 2024-1; "in continuous development"; OpenFOAM version bundled not shown on the page).
*(Verified, [S3][S4][S31])*

**Native bindings are out.** OpenFOAM's C++ API has no ABI stability across the six-monthly releases and is
GPL-3.0 — linking it would put the product under GPL (COMMIT-02 forbids). SU2's `pysu2` is a SWIG-generated
**Python** module around `CDriver`; it is not callable from C#/Rust without embedding Python, and it ties
the product to SU2's build. Both conclusions leave exactly one boundary: **generated files in, child
process, files out.** *(Verified for SU2's LGPL-2.1 and Python wrapper [S18]; GPL-3.0 for OpenFOAM Verified
via [S29]'s licence of the ESI-derived app and general knowledge — Flagged for the ESI tree's own LICENSE
file, not fetched.)*

**Parsing OpenFOAM/SU2 outputs in .NET/Rust.** No maintained .NET or Rust crate for OpenFOAM field/mesh
files or `.su2` meshes was identified this session (NuGet/crates.io were not reachable within budget).
*(Flagged.)* The interchange that avoids the problem: `foamToVTK` (legacy `.vtk`/`.vtu` per time step, with
`-fields`, `-surfaceFields`, `-patches`) on the OpenFOAM side and `OUTPUT_FILES= (PARAVIEW_MULTIBLOCK,
SURFACE_PARAVIEW, RESTART)` on the SU2 side; the workbench needs one VTK reader (XML `.vtu`/`.vtm` legacy
ASCII/binary) plus CSV/`.dat` parsers. *(Verified for SU2 outputs [S22]; `foamToVTK` flags Flagged.)* ESI
v2606 additionally advertises "VTK-HDF support" via plugins, which may become the better interchange once
stable. *(Verified mention [S2]; maturity Flagged.)*

### Installation accountability per OS

| OS | Substrate | What "tool-owned install" can mean | Elevation / reboot | Pin | Licence exposure |
|---|---|---|---|---|---|
| macOS (Apple silicon) | OpenFOAM.app via Homebrew tap | Detect Homebrew; run `brew install gerlero/openfoam/openfoam`; detect `/Applications/OpenFOAM-v2606.app`; smoke test; explain the **unsigned/un-notarized** dialog *before* first launch | none for brew; Gatekeeper prompt | app version + OpenFOAM version string | GPL-3.0 process use; the app's own terms |
| macOS (Intel) | Docker Desktop + `opencfd/openfoam-default:2512@sha256:…` | Detect Docker; pull by digest; smoke test | Docker Desktop install needs admin once | image digest | Docker Desktop subscription terms for orgs ≥ 250 staff or ≥ $10 M |
| macOS (any) | SU2 GitHub release binary | Download release asset, verify hash, place under app-support, smoke test | none (quarantine attribute must be handled; notarization of SU2 binaries **unverified**) | SU2 tag + asset hash | LGPL-2.1 binary, process use |
| Windows | WSL2 + Ubuntu + OpenCFD apt repo or Foundation Ubuntu package | Enable WSL feature, install distro, `apt install openfoam2606-default`, smoke test | admin + possible reboot for WSL enablement | package version + distro | GPL-3.0 process use |
| Windows | Docker Desktop (WSL2 backend) + image digest | as macOS Intel | admin + WSL reboot | digest | Docker Desktop terms |
| Windows | blueCFD-Core native (unofficial) | detect only in v1 | installer | version | GPL; unofficial port — "supported backend matrix" must say so |
| Windows | SU2 GitHub release win64 binary | download + hash + smoke test | none | tag + hash | LGPL-2.1 |
| Linux (future) | apt/rpm packages, conda-forge `su2` (linux-64/aarch64) | package manager | sudo | version | GPL/LGPL process |

*(Substrate facts Verified [S3][S4][S18][S24][S26][S28][S29][S31]; the "what it can mean" column Inferred; apt package names and WSL reboot behaviour Flagged.)* Disk/RAM budgets from fetched sources: none; the ESI image is multi-GB and a 5 M-cell RANS case needs of order several GB of RAM — treat as Flagged placeholders that the `backend.setup` telemetry must replace with measured values. Version pinning: image **digest**, not tag (`latest` moved on 2026-01-23 to 2512 [S26]); SU2 release tag + asset SHA-256; OpenFOAM.app version + `foamVersion` output. Reproducibility across OSes is *not* guaranteed even at equal versions (compiler, MPI build, ARM vs x86 floating-point differences) — the run record must store OS/arch and the exact solver build string, and cross-platform agreement is a test, not an assumption. *(Inferred.)*

### Validation cases

| Case | What it validates | Data / access | Terms |
|---|---|---|---|
| NASA TMR 2D NACA 0012 (M 0.15, Re 6e6; Ladson tripped forces, Gregory & O'Reilly Cp; Abbott & von Doenhoff untripped) | Fully turbulent RANS implementation and the workbench's coefficient/reference conventions; Cp resolution at the LE peak | tmbwg.github.io/turbmodels (digitised data, grids, CFL3D/FUN3D reference results) | US-government public data *(Verified data set, terms Inferred)* [S35] |
| NASA TMR 2D zero-pressure-gradient flat plate (M 0.2, Re 5e6 per unit length, plate length 2) | Wall treatment, Cf and u+ vs y+; the first thing to run after install (verification, not validation) | same site; multiple grid levels; used for DPW-5 | same [S36] |
| Duncan (1983), *J. Fluid Mech.* 126 — towed 2D hydrofoil under a free surface, breaking and non-breaking wave resistance | interFoam/interIsoFoam wave field, wave drag and lift loss vs depth/Froude; the free-surface de-risk oracle | Journal paper (data digitised from figures by later authors) | Cambridge UP paywall; DOI **not confirmed** this session *(Flagged — citation from recall; Crossref lookup of the recalled DOI returned 404)* [S46] |
| Delft twist-11 hydrofoil (Foeth et al., TU Delft) | 3D sheet/cloud cavitation dynamics, shedding frequency; erosion-risk methods | Thesis + papers; geometry published; 2018–2023 CFD papers exist [S42] | Academic; access to raw data varies *(Flagged)* [S47] |
| NACA 66(mod) (Shen & Dimotakis 1989 cavitating/non-cavitating; Brockett section) | 2D Cp and cavitation inception on a marine section | Journal/report | *(Flagged — recall)* [S48] |
| 2D NACA 0015 near free surface (several tank experiments) | Near-surface lift/drag at fixed depth ratios | Literature; used by 2022–2024 RANS-VOF papers [S41] | *(Flagged — specific dataset not identified)* |
| ONR / INSEAN hydrofoil and appendage sets | Strut/junction and full-scale marine cases | Not reached this session | *(Flagged)* |

### Result harvesting and evidence

Read back per run: force and moment components with the reference values used (ρ, V, `Aref`/`REF_AREA`,
`lRef`/`REF_LENGTH`, moment centre, lift/drag axes), residual history per equation with the converged flag,
y+ distribution on the wing (min/max/mean and the field for masking), surface Cp, wall shear stress / Cf
(the only supported separation criterion — τ_w·x̂ < 0 or Cf sign change along a surface streamline),
velocity on named slices, modeled k and ω (or ν̃ for SA) with the model name, and for transition runs
`gammaInt`/intermittency. *(Verified availability of these fields: OpenFOAM
`forces`/`forceCoeffs`/`solverInfo` [S13][S14][S15]; SU2 `VOLUME_OUTPUT` groups `PRIMITIVE`, `SOLUTION`,
`RESIDUAL`, `SKIN_FRICTION`, `Y_PLUS` [S22]; separation criterion Inferred and consistent with the grounding
doc's rule that vortex criteria are not separation.)* Formats: OpenFOAM time directories → `foamToVTK` →
`.vtk`/`.vtu`; SU2 → `.vtu`/`.vtm`/`.szplt`/`.csv`; CGNS is a mesh input for SU2, not a results interchange
here. *(Verified for SU2 [S21][S22].)*

**Run-record metadata that must be pinned** (ties to the spec's Analysis run / Sweep sample / Field evidence
aggregates): backend line and version string (`OpenFOAM-v2606` / `SU2 v8.5.0`), substrate and pin (image
digest, app version, release asset hash), OS/arch, MPI ranks, geometry revision hash and STL export
tolerance, fluid snapshot (ρ, μ, T, salinity, source version, p_v if cavitation), operating pair (V, α, β),
turbulence and transition model with TI/viscosity ratio, wall treatment and target y+, mesh generator
settings hash, mesh stats (`checkMesh` summary, cell count, achieved y+), schemes/solver settings hash,
convergence criteria and whether they fired, averaging window for reported coefficients, checkpoint cadence,
wall-clock and CPU seconds, exit code **and** the artefact-oracle verdict, and the
function-object/output-file inventory that defines the "available field inventory" the UI shows. *(Inferred
from [S13][S15][S22][S23][S32][S33] and the spec.)*

## Comparables

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

## Reference information

- **ITTC 7.5-03-01-01 Rev 05 (2024) — Uncertainty Analysis in CFD Verification and Validation, Methodology and Procedures.** Requires: systematic grid (and time-step) refinement, classification of the convergence ratio, Richardson-extrapolation error estimate, a safety factor to convert error to uncertainty, and validation as comparison of |E| to the combined numerical and experimental uncertainty. The product must show this study before "converged" is displayed. *(Verified, [S32])*
- **ITTC 7.5-03-02-03 Rev 02 (2024) — Practical Guidelines for Ship CFD Applications.** Requires: domain ≥ 10 L upstream, ≥ 20 L downstream for lifting bodies, ≥ L laterally; y+ per Table 1; positive volumes, determinant > 0.3; residual drop ≥ 3 orders or force convergence; a posteriori y+ check; roughness treated explicitly when k_s is not small. *(Verified, [S33])*
- **NASA Turbulence Modeling Resource** (now at tmbwg.github.io): SST/SA definitions (already cited by the grounding doc), NACA 0012 validation and flat-plate verification. *(Verified, [S35][S36])*
- **OpenFOAM ESI source (gitlab.com/openfoam/core/openfoam — the former develop.openfoam.com now redirects there)**: model names, function-object contracts, annotated `snappyHexMeshDict`. *(Verified, [S7]–[S15])*
- **OpenFOAM user guide (CFD Direct, v13) — file format.** Dictionary grammar and directives. *(Verified, [S40])*
- **SU2 documentation and `config_template.cfg`** (v8.5.0 master). *(Verified, [S19]–[S23])*
- **Licences:** OpenFOAM GPL-3.0 (process use only), SU2 LGPL-2.1 (binary process use; conda-forge metadata reports "GPL-2.0-or-later", a discrepancy to check before any redistribution), Docker Desktop subscription terms, OpenFOAM.app GPL-3.0, foamlib GPL-3.0, CliWrap MIT, duct MIT, Gmsh GPL-2.0-or-later (Flagged), cfMesh GPL (Flagged). *(Labels as marked, [S18][S24][S28][S29][S30][S38][S39])*

## Data, constants, formulae and invariants

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

## Design implications for CFD-Workbench

1. **Supported backend matrix is physics × substrate × OS, and it is small (S5 release gate).** Publish exactly: ESI OpenFOAM v2512 or v2606 via `opencfd/openfoam-default@sha256:…` (mac arm64/x86, Windows via Docker Desktop or WSL2), OpenFOAM.app v2606 (mac arm64, unsigned — disclosed), SU2 v8.5.0 GitHub release binaries (mac, win). Foundation OpenFOAM 14 is **not** in v1 (different grammar, §"Case generation"). *(Verified facts [S1][S4][S18][S26][S29]; Inferred matrix.)*
2. **Physics gating at queue time (CFD-05/06, sample lifecycle).** `Unsupported` is decided from the backend capability record before a sample leaves `Pending`: SU2 → single-phase steady/unsteady only; free surface, ventilation, cavitation → OpenFOAM only; transition → both but implies wall-resolved mesh class. *(Verified capabilities [S10][S11][S19]; Inferred rule.)*
3. **Meshing is the gating spike, not solving (A2 S5).** SPIKE-03 (unattended snappyHexMesh across AR 5/8/12) stays first; add SPIKE-03b: Gmsh boundary-layer mesh of the same wings exported to `.su2`, judged by achieved y+ and layer coverage at the TE. The v1 3D backend decision waits for both. *(Inferred.)*
4. **Recommended v1 backend (Inferred, evidence S2/S11/S12/S18/S21/S26):** OpenFOAM ESI `snappyHexMesh` + `simpleFoam` + SST for the 3D wing-in-a-box on both OSes, because it is the only backend in the stack with an automatic layered mesher and a single multi-arch substrate; SU2 `INC_RANS` first for the 2D section slice, where the workbench writes the `.su2` C-mesh itself and validates against TMR NACA 0012 — a no-Docker, no-WSL install that gives S5 a "validated single-phase case on each platform" in the cheapest way. Counter-evidence and how it fared: the proposal's "SU2 first for programmatic control" (S43 §6.2) is right about the `.cfg`/CSV ergonomics but silent on meshing; once a 3D mesh is required the ergonomics advantage is smaller than the mesher gap. If SPIKE-03b shows Gmsh→`.su2` wing meshes pass ITTC's floor unattended, SU2 becomes viable as the sole 3D single-phase backend and the recommendation flips.
5. **Typed case model = closed subset serialiser + parser with round-trip tests (CFD-02).** One model, two emitters (ESI dictionaries, SU2 `.cfg`), no macros/`#codeStream`/`#calc`, deterministic ordering and formatting, and a golden-file test per backend version. Version-specific keyword differences are data (a keyword map per pinned version), not code branches. *(Inferred from [S40][S23].)*
6. **Evidence by files, not stdout (CFD-03, VIZ-01–04).** Always emit `forces`, `forceCoeffs`, `solverInfo`, `yPlus`, `wallShearStress`, surface sampling on the wing, and one velocity slice set; on SU2 set `HISTORY_OUTPUT` and `VOLUME_OUTPUT` to include `RMS_RES, AERO_COEFF, WALL_TIME` and `PRIMITIVE, RESIDUAL, SKIN_FRICTION, Y_PLUS`. The harvester tails these files for live plots and reads them for the final record. *(Verified availability [S13][S14][S15][S22]; Inferred policy.)*
7. **Completion oracle over exit code (CFD-03).** Implement the artefact checks listed in §"Exit codes versus completion"; a zero exit with a missing final time directory / restart file is `Failed` with reason "no outputs". *(Inferred.)*
8. **Cancellation addressed to the substrate (CFD-03/06).** Every run gets a run-id used as `docker --name`, as a WSL process-group marker and as the case path; Cancel = substrate-level kill → host tree kill → bounded wait → orphan scan → status `Cancelled` with partial outputs retained. Spike each substrate. *(Verified library limits [S37][S38]; Inferred design.)*
9. **Resume only from a verified checkpoint (CFD-04).** OpenFOAM `startFrom latestTime` / SU2 `RESTART_SOL` are offered only after the harvester validates the last checkpoint; `writeInterval`/`OUTPUT_WRT_FREQ` are shown as the storage cost they are. *(Verified SU2 [S19][S23]; Inferred rule.)*
10. **Mesh gate with named measures (CFD-02).** `checkMesh` summary parsed into typed results; thresholds default to snappy's `meshQualityControls` (max non-orthogonality 65°, boundary skewness 20, internal skewness 4 — Flagged defaults from recall); block on any failed check; show achieved y+ after the run and flag if outside the model's window (≤ 1 resolved; 30–100 wall functions). *(Verified ITTC windows [S33]; snappy defaults Flagged.)*
11. **"Converged" is a three-part label (A4, VIZ-01).** Residual criterion fired · artefact oracle passed · grid uncertainty: *Not quantified (single mesh)* or *U_G = x % (ITTC 7.5-03-01-01, 3 grids, r = √2)*. A Sweep never shows a grid-uncertainty it did not compute. *(Verified procedure [S32]; Inferred label.)*
12. **Transition and turbulence inputs are first-class case fields (ANA-15 analogue).** TI, μ_t/μ, model and variant (`SST V2003m`, `kOmegaSSTLM`, `LM/LM2015+MALAN`), wall treatment and target y+ live in the run record and the UI; the polar tier's Ncrit and the CFD tier's TI are shown side by side as different, non-convertible knobs. *(Verified options [S7][S20]; Inferred product rule.)*
13. **Installation is a state machine with disclosure (CFD-01).** Each substrate step names: download size, disk, admin/reboot, the licence it accepts on the user's behalf (Docker Desktop terms; GPL notice; unsigned OpenFOAM.app dialog) and the pin it will record; "Ready" = detection + pinned smoke test (TMR flat plate for SU2; `checkMesh` + `simpleFoam` on a bundled 2D case for OpenFOAM). *(Verified facts [S28][S29][S35]; Inferred flow.)*
14. **Free-surface de-risk slice (A2).** Oracle = Duncan-class 2D submerged foil in `interFoam`/`interIsoFoam` at one Froude/depth pair, domain with ≥ 20 L outlet and wave damping, grid study per ITTC; success = wave profile and wave resistance within the paper's uncertainty; only then does S5 gain a "near-surface" physics option. *(Verified ITTC domain rule [S33]; Duncan citation Flagged [S46].)*
15. **Licence review artefact (COMMIT-02, release row).** Record per backend: OpenFOAM GPL-3.0 (process), SU2 LGPL-2.1 (binary, process; resolve the conda-forge "GPL-2.0-or-later" metadata discrepancy before any redistribution), Gmsh/cfMesh GPL (process), Docker Desktop subscription; the product never redistributes solver binaries in v1 — it downloads from the vendor's channel and verifies hashes. *(Verified [S18][S24][S28]; Inferred policy.)*
16. **Instrumentation (IO rule).** `solver.process` must record wall-clock, CPU seconds, peak RSS, cells, ranks and substrate for every run so that the cost classes in this file — all Inferred placeholders — are replaced by measured distributions within the first release. *(Inferred.)*

## Open questions and domain failure modes

**Open questions (cheapest next probe first).**
1. Does Gmsh produce an unattended boundary-layer mesh on the AR 5/8/12 wings that passes ITTC's y+ and layer floor and imports to SU2? — Probe: SPIKE-03b, one script, three wings, report achieved y+ and layer coverage at the TE. (Decides finding 15.)
2. Does killing `docker run`/`wsl.exe` on macOS/Windows leave the solver running? — Probe: start `sleep`-wrapped `simpleFoam` under each substrate, cancel via CliWrap/`Process.Kill(true)`, `pgrep` inside the substrate.
3. Are the SU2 macOS release binaries signed/notarized, and do they run on Apple silicon natively or under Rosetta? — Probe: download v8.5.0 asset, `codesign -dv`, `file`, run the TMR flat plate.
4. Exact ESI v2606 keyword names for `yPlus`, `wallShearStress`, `sample` and `startFrom latestTime`; snappy default quality thresholds — Probe: fetch the corresponding headers from gitlab.com/openfoam/core/openfoam (the develop.openfoam.com host now redirects there).
5. When is `opencfd/openfoam-default:2606` published and multi-arch? — Probe: Docker Hub tags API (S26 URL).
6. Duncan (1983) exact DOI and digitised data availability; Delft twist-11 geometry/data access terms; NACA 66(mod) source. — Probe: Crossref by title, TU Delft repository search.
7. Does SU2 v8.5.0 support overset or 6-DoF for pumping? — Probe: grep `config_template.cfg` for `OVERSET`/`GRID_MOVEMENT`.
8. What is the conda-forge licence metadata for `su2` actually saying, and does SU2's `LICENSE.md` still read LGPL-2.1? — Probe: fetch both files.
9. Measured cost classes on the target laptops. — Probe: instrumented runs, not estimates.

**Domain failure modes.**
- *Silent wrong answer:* layers missing at the TE (Cd wrong, mesh "passes"); wall functions used with y+ ≈ 5 (buffer layer); transition model with SU2's 5 % TI default; `REF_AREA= 0` auto-computed from a marker; forces in non-dimensional mode misread as newtons; `latest` image drifting under a saved run.
- *Expensive:* wall-resolved 3D transition runs on a laptop; free-surface domain too short (wave reflection, oscillating lift never converging); grid study never performed and then required for a release claim.
- *Irreversible / product-level:* linking any GPL component (licence contamination); redistributing solver binaries without licence review; killing the host client and leaving a container writing into the case directory the user then edits.

## Disconfirming views sought

- **"SU2 first" (proposal §6.2).** Strongest case: single `.cfg`, CSV history, prebuilt binaries on all three OSes (Verified), LGPL. How it fared: it holds for the 2D slice and for install ergonomics; it fails for 3D unless an external mesher is added, because SU2 has no mesher and no free-surface/cavitation path (Verified absence). Result: split recommendation (finding 15), flip condition stated.
- **"OpenFOAM is too hard to install to be v1."** Counter-evidence: the official multi-arch image and OpenFOAM.app make the mac path one Homebrew command or one image pull (Verified); the residual costs are Docker Desktop terms and the unsigned-app dialog, both disclosable. Held partially: Windows still needs WSL2 or Docker Desktop with admin rights.
- **"Fully turbulent SST is enough at Re ≈ 1e6."** Counter: TMR shows ≈ 10 % drag difference tripped vs untripped at Re 3–6e6 (Verified) and the polar tier already models transition via Ncrit; ignoring it makes tiers disagree by construction. Held: transition is required for reconciliation, but it is a second step behind a working fully turbulent pipeline.
- **"A converged residual means a converged result."** Counter: ITTC separates iterative convergence from grid uncertainty (Verified). Held fully; the three-part label follows.
- **"Native bindings would be faster/better."** Counter: OpenFOAM GPL + ABI churn; SU2 wrapper is Python (Verified). Held fully.
- **"The Foundation line is interchangeable with ESI."** Counter: modular solvers since v11, `simpleFoam` a script, different property files (Verified). Held; v1 pins ESI.

## Glossary terms

- **RANS** — Reynolds-averaged Navier–Stokes: mean-flow equations with a turbulence model closing the Reynolds stresses; steady RANS produces a mean field, never resolved eddies. *(Verified, [S33])*
- **SST (k–ω SST)** — Menter's two-equation shear-stress-transport model; SU2 variants V1994m/V2003m. *(Verified, [S20])*
- **γ–Reθ (Langtry–Menter) transition model** — two extra transport equations (intermittency γ, transition-onset momentum-thickness Reynolds number Reθt) coupled to SST; `kOmegaSSTLM` in OpenFOAM, `LM` in SU2. *(Verified, [S7][S20])*
- **kkLOmega** — Walters–Cokljat laminar-kinetic-energy transition model; incompressible-only in OpenFOAM ESI. *(Verified location, [S9]; author attribution Flagged.)*
- **y+** — dimensionless wall distance y·u_τ/ν; targets ≤ 1 (resolved) or 30–100 (wall functions). *(Verified, [S33])*
- **Wall function** — analytic log-law bridge between the first cell and the wall; invalid in strong adverse pressure gradients. *(Verified, [S33])*
- **VOF** — volume-of-fluid interface capturing; `interFoam` (MULES algebraic) and `interIsoFoam` (isoAdvector geometric) in OpenFOAM ESI. *(Verified existence, [S11]; algorithm attribution Flagged.)*
- **Cavitation number σ** — (p∞ − p_v)/(½ρV²); drives `interPhaseChangeFoam` mass-transfer models Kunz/Merkle/SchnerrSauer. *(Model names Verified, [S10]; definition standard.)*
- **snappyHexMesh** — OpenFOAM's castellate/snap/add-layers hex-dominant mesher driven by `snappyHexMeshDict`. *(Verified, [S12])*
- **`.eMesh`** — feature-edge mesh produced by `surfaceFeatureExtract`, referenced in `castellatedMeshControls.features`. *(Verified reference, [S12]; utility name Flagged.)*
- **Richardson extrapolation / GCI** — three-grid error estimate δ_RE = ε21/(r^p − 1) and uncertainty U = F_S|δ_RE|. *(Verified, [S32])*
- **Function object** — OpenFOAM run-time post-processing plug-in configured in `controlDict.functions`; writes `postProcessing/<name>/<time>/*.dat`. *(Verified, [S13][S15])*
- **`MARKER_*`** — SU2 boundary-condition assignment by mesh marker tag. *(Verified, [S23])*
- **Image digest** — content hash (`@sha256:…`) identifying one exact container image; tags move, digests do not. *(Verified that `latest` moved, [S26])*
- **Artefact oracle** — the workbench's completion check based on output files, independent of exit code. *(Inferred, this file.)*

## Sources

| # | Title / source | Type | URL | Accessed | Used for |
|---|---|---|---|---|---|
| S1 | OpenFOAM.com news — release list (v2606 2026-06-26 … v2006) | primary | https://www.openfoam.com/news/main-news | 2026-09-20 | Current ESI version |
| S2 | OpenFOAM v2606 release notes | primary | https://www.openfoam.com/news/main-news/openfoam-v2606 | 2026-09-20 | GEKO, VTK-HDF, platforms |
| S3 | OpenFOAM.com download page | primary | https://www.openfoam.com/download | 2026-09-20 | Windows/macOS/Linux install routes |
| S4 | OpenFOAM Foundation download (v14, 2026-07-14) | primary | https://openfoam.org/download/ | 2026-09-20 | Foundation version, WSL/Multipass |
| S5 | OpenFOAM 11 release notes — modular solvers | primary | https://openfoam.org/version/11/ | 2026-09-20 | `simpleFoam` as script |
| S6 | OpenFOAM 14 release notes | primary | https://openfoam.org/version/14/ | 2026-09-20 | v14 features |
| S7 | `kOmegaSSTLM.H` (ESI master) | primary source | https://gitlab.com/openfoam/core/openfoam/-/raw/master/src/TurbulenceModels/turbulenceModels/RAS/kOmegaSSTLM/kOmegaSSTLM.H | 2026-09-20 | Transition model contract |
| S8 | ESI RAS model directory listing (GitLab API) | primary source | https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tree?path=src/TurbulenceModels/turbulenceModels/RAS | 2026-09-20 | Model inventory incl. GEKO |
| S9 | ESI incompressible RAS listing (kkLOmega) | primary source | https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tree?path=src/TurbulenceModels/incompressible/turbulentTransportModels/RAS | 2026-09-20 | kkLOmega location |
| S10 | `interPhaseChangeFoam/phaseChangeTwoPhaseMixtures` listing | primary source | https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tree?path=applications/solvers/multiphase/interPhaseChangeFoam/phaseChangeTwoPhaseMixtures | 2026-09-20 | Kunz/Merkle/SchnerrSauer |
| S11 | ESI multiphase solver listing | primary source | https://gitlab.com/api/v4/projects/openfoam%2Fcore%2Fopenfoam/repository/tree?path=applications/solvers/multiphase | 2026-09-20 | interFoam, interIsoFoam, cavitatingFoam |
| S12 | Annotated `snappyHexMeshDict` (ESI master) | primary source | https://gitlab.com/openfoam/core/openfoam/-/raw/master/etc/caseDicts/annotated/snappyHexMeshDict | 2026-09-20 | Layer/quality controls |
| S13 | `forces.H` (ESI master) | primary source | https://gitlab.com/openfoam/core/openfoam/-/raw/master/src/functionObjects/forces/forces/forces.H | 2026-09-20 | forces FO contract |
| S14 | `forceCoeffs.H` (ESI master) | primary source | https://gitlab.com/openfoam/core/openfoam/-/raw/master/src/functionObjects/forces/forceCoeffs/forceCoeffs.H | 2026-09-20 | Coefficient FO contract |
| S15 | `solverInfo.H` (ESI master) | primary source | https://gitlab.com/openfoam/core/openfoam/-/raw/master/src/functionObjects/utilities/solverInfo/solverInfo.H | 2026-09-20 | Residual/converged-flag output |
| S16 | SU2 releases Atom feed (v8.5.0 2026-04-28, v8.4.0 2026-01-12, v8.3.0 2025-09-01) | primary | https://github.com/su2code/SU2/releases.atom | 2026-09-20 | Current SU2 version and dates |
| S17 | SU2 v8.5.0 release page | primary | https://github.com/su2code/SU2/releases/latest | 2026-09-20 | Release notes (ITER_TIME, PaStiX 6, RBF in 8.3.0) |
| S18 | SU2 README | primary | https://github.com/su2code/SU2 | 2026-09-20 | LGPL-2.1, prebuilt binaries, SU2_PY, meson |
| S19 | SU2 docs — Solver Setup | primary | https://su2code.github.io/docs_v7/Solver-Setup/ | 2026-09-20 | SOLVER list, restart, time options |
| S20 | SU2 docs — Physical Definition | primary | https://su2code.github.io/docs_v7/Physical-Definition/ | 2026-09-20 | Turbulence/transition options |
| S21 | SU2 docs — Mesh File | primary | https://su2code.github.io/docs_v7/Mesh-File/ | 2026-09-20 | `.su2` format, CGNS, Gmsh export |
| S22 | SU2 docs — Custom Output | primary | https://su2code.github.io/docs_v7/Custom-Output/ | 2026-09-20 | HISTORY/VOLUME/OUTPUT_FILES |
| S23 | SU2 `config_template.cfg` (master) | primary source | https://raw.githubusercontent.com/su2code/SU2/master/config_template.cfg | 2026-09-20 | Wall functions, INC_*, REF_*, MARKER_*, CONV_*, restart |
| S24 | conda-forge `su2` package metadata | primary (registry) | https://api.anaconda.org/package/conda-forge/su2 | 2026-09-20 | Platforms linux-only; licence metadata discrepancy |
| S25 | Homebrew formula `su2` (HTTP 404) | primary (registry) | https://formulae.brew.sh/api/formula/su2.json | 2026-09-20 | Absence of Homebrew SU2 |
| S26 | Docker Hub tags — `opencfd/openfoam-default` | primary (registry) | https://hub.docker.com/v2/repositories/opencfd/openfoam-default/tags?page_size=30 | 2026-09-20 | Multi-arch tags, dates |
| S27 | Docker Hub — `su2code/su2` (HTTP 404) | primary (registry) | https://hub.docker.com/v2/repositories/su2code/su2/tags | 2026-09-20 | Absence of official SU2 Hub image |
| S28 | Docker Desktop licence terms | primary | https://docs.docker.com/subscription/desktop-license/ | 2026-09-20 | Subscription thresholds |
| S29 | gerlero/openfoam-app README | primary | https://github.com/gerlero/openfoam-app | 2026-09-20 | macOS app facts, notarization |
| S30 | gerlero/foamlib README | primary | https://github.com/gerlero/foamlib | 2026-09-20 | Typed wrapper precedent, GPL-3.0 |
| S31 | blueCFD-Core site | primary | https://bluecfd.github.io/Core/ | 2026-09-20 | Native Windows port status |
| S32 | ITTC 7.5-03-01-01 Rev 05 (2024) — Uncertainty Analysis in CFD V&V | standard | https://www.ittc.info/media/11950/75-03-01-01.pdf | 2026-09-20 | Grid-convergence equations |
| S33 | ITTC 7.5-03-02-03 Rev 02 (2024) — Practical Guidelines for Ship CFD Applications | standard | https://www.ittc.info/media/11958/75-03-02-03.pdf | 2026-09-20 | y+, domain, quality, convergence |
| S34 | ITTC procedures index (PDF with links) | standard | https://www.ittc.info/downloads/quality-systems-manual/recommended-procedures-and-guidelines/ | 2026-09-20 | Locating 7.5-03 procedures |
| S35 | NASA TMR — 2D NACA 0012 validation case | primary | https://tmbwg.github.io/turbmodels/naca0012_val.html | 2026-09-20 | Validation oracle, tripped vs untripped |
| S36 | NASA TMR — 2D ZPG flat plate verification | primary | https://tmbwg.github.io/turbmodels/flatplate.html | 2026-09-20 | Install smoke-test case |
| S37 | Microsoft Learn — `Process.Kill` (.NET 10) | primary | https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0 | 2026-09-20 | Tree-kill semantics |
| S38 | CliWrap README | primary | https://github.com/Tyrrrz/CliWrap | 2026-09-20 | .NET process idioms, MIT |
| S39 | duct.rs README | primary | https://github.com/oconnor663/duct.rs | 2026-09-20 | Rust process idioms, MIT |
| S40 | CFD Direct OpenFOAM v13 user guide — basic file format | primary | https://doc.cfd.direct/openfoam/user-guide-v13/basic-file-format | 2026-09-20 | Dictionary grammar |
| S41 | Crossref search — hydrofoil free-surface RANS-VOF papers 2018–2026 | secondary (index) | https://api.crossref.org/works?query.bibliographic=hydrofoil+free+surface+OpenFOAM+interFoam+RANS+VOF+ventilation&filter=from-pub-date:2018-01-01 | 2026-09-20 | Titles/DOIs: 10.5957/csys-2022-012; 10.5957/jst/2023.8.2.24; 10.1016/j.apor.2024.104157 |
| S42 | Crossref search — hydrofoil transition / Delft twist cavitation papers | secondary (index) | https://api.crossref.org/works?query.bibliographic=hydrofoil+transition+model+gamma+ReTheta+RANS+Reynolds+cavitation+twisted+Delft&filter=from-pub-date:2018-01-01 | 2026-09-20 | Titles/DOIs: 10.1063/5.0097859; 10.3390/fluids8010023; 10.1016/j.oceaneng.2024.118876; 10.1016/j.ijmecsci.2023.108618; 10.1016/j.oceaneng.2022.111313 |
| S43 | Repo — `docs/knowledge/sources/proposal-sequence.md.txt` §6 | repo (verbatim snapshot) | docs/knowledge/sources/proposal-sequence.md.txt | 2026-09-20 | Prior backend/interop intent |
| S44 | Repo — `docs/specs/cfd-workbench.md` (A2, CFD-01–06, VIZ-01–04) | repo | docs/specs/cfd-workbench.md | 2026-09-20 | Stories this file must serve |
| S45 | Repo — `docs/knowledge/cfd-workbench-grounding.md` | repo | docs/knowledge/cfd-workbench-grounding.md | 2026-09-20 | Reconciliation register, TMR SST citation |
| S46 | Duncan, J. H. (1983) "The breaking and non-breaking wave resistance of a two-dimensional hydrofoil", *J. Fluid Mech.* 126 | primary (not fetched) | — (Crossref lookup of the recalled DOI returned 404) | — | Free-surface oracle — **Flagged** |
| S47 | Foeth, E.-J. et al., Delft twist-11 hydrofoil cavitation experiments (TU Delft) | primary (not fetched) | — | — | Cavitation oracle — **Flagged** |
| S48 | Shen & Dimotakis (1989) NACA 66(mod) cavitating/non-cavitating data | primary (not fetched) | — | — | Marine section oracle — **Flagged** |
