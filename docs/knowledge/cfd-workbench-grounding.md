---
id: kb-cfd-workbench-grounding
title: CFD-Bench and proposal grounding
type: knowledge
status: in-review
owner: "@timianmalloo"
tags: [hydrofoils, sources, geometry, proposals]
links:
  - {to: spec-cfd-workbench, rel: relates-to}
review-by: 2026-12-19
summary: Source map, precedence, confidence and conflicts carried from CFD-Bench and the proposal into Workbench. Includes the follow-up requirements for weighted section/outline editing, water-dependent loads, Cartesian simulation sweeps and scientifically labeled 2D/3D replay.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
---

# CFD-Bench and proposal grounding

Read on 2026-09-19. **Verified** means the cited source was inspected; it does not establish that its scientific claim was experimentally reproduced. **Inferred** names a proposed product interpretation. **Flagged** names an unresolved empirical, licensing, usability, or implementation question.

## Source of intent

The user requests a Mac/PC hydrofoil design and simulation tool, a full specification, and iterative HTML designs. The available proposal folder is `~/projects/CFD-WorkBench-Proposal`, containing `README.md`, `proposal.md`, `sequence.md`, and `inventory.md`. The requested plural name does not exist. Its README and content identify the intended packet. The later `sequence.md` explicitly evolves `proposal.md`; where they conflict its product sequence governs, subject to the current request and the Workbench instruction that the stack is unselected.

The 19 September follow-up explicitly requires editable predefined foil sections (NACA 0012 is the named example), spline/curve reshaping and weighted smoothing for both sections and outlines; Cl, Cd, their ratio and total lift/drag at speed and fresh/salt water; N by default with imperial force conversion; fresh/salt simulation setup; velocity and incidence sweeps; and richer ParaView-inspired results/replay in 2D/3D to inspect separation and turbulence. **Verified user intent.** The interpretation of “constant curve” as continuous fairing rather than constant mathematical curvature is **Inferred** from the user's stated contrast between weighted influence and forced interpolation. The specification makes that interpretation visible and testable.

The canonical CFD-Bench source clone is `~/projects/cfd-bench`, remote `https://github.com/timianmalloo/cfd-bench`, inspected revision `496a0a8ca2fae9026927167a8f3e5da0a53f2233`. This record uses committed source links below; working-copy source hashes are recorded in `source-manifest.json` for exact provenance.

Portable verbatim snapshots: [proposal](sources/proposal-proposal.md.txt), [later sequence](sources/proposal-sequence.md.txt), [spikes and gaps](sources/proposal-inventory.md.txt), [parametric geometry](sources/bench-parametric-geometry.md.txt), [CAD UX](sources/bench-cad-ux.md.txt). The `.txt` extension keeps archived metadata out of the active documentation graph. The [manifest](source-manifest.json) records original paths and SHA-256 values.

| Source | Grounding contribution | Confidence |
|---|---|---|
| Proposal README, proposal, sequence, inventory | Mac/Windows product scope, six-step sequence, retained commitments, open research | Verified source intent |
| [Parametric geometry](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/cfd-hydrofoil-simulation/parametric-geometry.md) | assembly/surface/station/loft; one-way generator; section representations | Verified source contract |
| [CAD UX](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/cad-ux-and-geometry/index.md) | five distributions; 2D editing; 3D display/select; fairness | Verified source direction |
| [Design automation](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/design-automation/index.md) | generative parameters and estimator/VLM confidence gates | Verified source intent |
| [Knowledge gaps](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/knowledge-gap-register.md) | structures, units, empirical validation, uncertainty, first launch | Verified gaps; no closure by prose |
| [AI in the product](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/ai-in-the-product/index.md) | language/numerics separation, additive optional AI | Verified source intent |
| [CFD orchestration](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/cfd-orchestration/index.md) | process lifecycle, setup, backend capability boundaries | Verified source direction |
| [Flow visualization](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/flow-visualization/index.md) | Cp, slices, streamlines, evidence provenance | Verified source direction |
| [Data and constants](https://github.com/timianmalloo/cfd-bench/blob/496a0a8ca2fae9026927167a8f3e5da0a53f2233/docs/knowledge/cfd-hydrofoil-simulation/data-and-constants.md) | Water-property provenance, coefficient normalization and Re; inspected again for the follow-up | Verified local source contract; numerical validity is not established by this read |

## Reconciliation register

| Source conflict / gap | Workbench specification treatment |
|---|---|
| Earlier CLI-first vs later shell-first | Shell is first product slice; headless parity remains verification and automation surface |
| Detect-only CFD vs tool-owned installation | App detects, explains and guides an explicit install; existing installed solvers run offline; initial downloads require a network |
| WPF/CUDA vs Mac/PC | Both platforms required; stack and kernel/backend chosen only after spikes; NVIDIA is optional |
| Five distributions contain ambiguous “outline” and chord | Outline means leading-edge offset; trailing edge is derived from leading edge plus chord |
| Generator loses editability on burst | Preserve recipe provenance; explicit shape remains parametric in its curves, station sections and named dimensions; no inverse fit presented as exact |
| Quintic automatically G4 | Degree alone does not guarantee continuity at repeated knots, boundaries or intentional breaks; measure continuity and display breaks |
| Catalog → CST allegedly lossless | Fit is an approximation with a reported residual and user acceptance; retain original source |
| Proposal §3.1a assumes on-curve anchors everywhere; current user asks weighted smoothing | Through points remains available. Smooth uses off-curve weighted controls plus explicit hard constraints. Station values are evaluated from the same distribution; control ordinates never become a second geometry authority |
| Catalog sections start locked in proposal §3.1a | Edit section opens a source-linked curve draft directly, including NACA 0012. Apply creates a modified revision; the immutable source and its polar applicability remain distinct |
| Bench says seawater yields about 2.7% more force at identical geometry/speed/angle | Density scaling alone holds only with coefficients fixed. Water viscosity also changes Re, and compatible coefficients must be recomputed/looked up; a fixed-coefficient what-if is labeled separately |
| Bench fluid tables cite ITTC Rev02 (2011) | Rev03 (2024) was inspected on the primary ITTC site. New admitted fluid records must pin their actual source version and range; historical data is not silently rewritten |
| Proposal §4 requests section forces in newtons | Section line load is N/m; an explicit strip width permits strip N. Only a supported finite-wing result is Total Lift/Total Drag, wing only, with reference area and method |
| Bench sweep object names one variable; user requests velocities and angles | Resolve a Cartesian speed × incidence sample schedule, then replay one axis at a held coordinate with one outcome per pair and traceable attempts |
| Proposal §7.2 groups Q/λ₂ with separation criteria | Vortex diagnostics do not establish wall separation. A separation overlay requires supported wall/skin-friction evidence and a documented criterion |
| Animated fields could imply transient turbulence | Operating-point replay is discrete case selection. Physical time requires actual transient samples; steady streamlines, pathlines, modeled k and resolved structures receive distinct labels and capability gates |
| Inventory AR formula uses span/area | Correct requirement is aspect ratio = full projected span squared / projected planform area |
| Sequence disagrees on external section `.dat` in v1 | Final S12 and explicit §3.6 v1 table take precedence over earlier “optional later” wording: bounded Selig/Lednicer profile import is v1 with preview/provenance. Native project import remains separate; arbitrary CAD import is deferred |
| No interviews recorded | Primary persona is source-backed intent; usability success remains Flagged until observed sessions |
| Existing Windows implementations absent locally | Do not claim reuse or runtime validation from seed repositories; recovery is a future task |

## External comparables and platform references

- [Shape3d manual](https://www.shape3d.com/Support/Manual.aspx): orthogonal curves and slices as a discoverable fairing metaphor; inspiration only, no copied assets. Verification is of documentation, not hands-on performance.
- [Autodesk Fusion loft](https://help.autodesk.com/view/fusion360/ENU/?contextId=SFC-LOFT): profiles and explicit rails; rail/profile compatibility demonstrates why independent competing constraints need rejection. Use explicit dependencies, not a full generic feature tree.
- [NASA OpenVSP cross-sections](https://www.nasa.gov/reference/openvsp-cross-sections/): profile families and CST fitting; a fit is an approximation. Use transparent section provenance.
- [Apple macOS HIG](https://developer.apple.com/design/human-interface-guidelines/designing-for-macos/): menus, resizable windows, precision input and keyboard workflows.
- [Microsoft keyboard interactions](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-interactions): focus and keyboard access are first-class requirements.
- [WCAG 2.2](https://www.w3.org/TR/WCAG22/): contrast, non-color cues, focus and target-size requirements for the review HTML; corresponding native accessibility proof is still required for the app.

These are primary documentation sources, not evidence of user interviews or measured product quality. The brief adapts patterns without reusing third-party artwork or code.

## Follow-up evidence and product mapping

Primary sources inspected on 2026-09-19; **Verified documentation** means the relevant source text was read, not that Workbench implements it. **Inferred mapping** names the resulting product decision. No new plotting library, geometry kernel or simulation backend is selected by these references.

| Source | Verified documentation | Inferred Workbench mapping / limits |
|---|---|---|
| [Autodesk control-point splines](https://www.autodesk.com/products/fusion-360/blog/sketch-control-point-splines-faq/) | Distinguishes fit-point handles from underlying control points and shows control frames with separate curve constraints | Expose Through points and Smooth modes with a visible control polygon, influence weights, hard locks and measured conversion error. The exact evaluator and constraint solver remain undecided |
| [ParaView filtering](https://docs.paraview.org/en/latest/UsersGuide/filteringData.html) | Stream tracing consumes vector data and seeded point/line sources; slices and derived filters expose input relationships | Results has a compact source/field/layer list, movable rake, slice/probe controls and actual field availability; no general-purpose filter editor is required |
| [ParaView animation](https://docs.paraview.org/en/latest/UsersGuide/animation.html) | Non-temporal scene sequences and recorded dataset timesteps are separate modes | Keep velocity/incidence replay separate from physical time. Every replay frame selects one case; camera, seeds and scalar range stay stable by default |
| [Kitware particle/path tracing](https://www.kitware.com/improvements-in-path-tracing-in-vtk/) | Particle and pathline integration use temporal flow data; pathlines and streamlines have different meanings for unsteady fields | A steady field does not acquire transient physics from animation. Pathlines require a distinct supported capability and time-dependent source |
| [ITTC 7.5-02-01-03 Rev03 (2024)](https://www.ittc.info/media/11764/75-02-01-03.pdf) | Published fluid-property guidance names temperature, salinity and pressure basis and supplies fresh/standard-seawater property tables | Pin source/version and conditions; admitted ranges govern computation. Compare physical water changes through Re-dependent analysis, not only a force multiplier |
| [NIST SI force conversion](https://www.nist.gov/pml/special-publication-811/nist-guide-si-appendix-b-conversion-factors/nist-guide-si-appendix-b9) | Distinguishes pound-force from mass and provides its newton conversion | Default N, optional lbf; line-load conversion also changes denominator length. Coefficients, geometry and freshness are unchanged by display conversion |
| [Turbulence Modeling Resource: SST](https://tmbwg.github.io/turbmodels/sst.html) | Documents modeled turbulent kinetic energy and variants of the SST turbulence model | Show modeled k with units and model/averaging provenance. Do not portray steady RANS as resolved turbulent motion; missing k or wall data disables its layer |

The proposal's relevant read path was `sequence.md` §3.1–3.1c (curve/station/catalog edits) → §4.1 (2D scope) → §7.1–7.2 (charts, fields and sweep scrubbing), reconciled with the Bench CAD UX, parametric geometry, fluid-data and flow-visualization documents. This traversal reaches specification A3/A4 (one geometry/condition authority), A5 stories GEO-08/13/14, ANA-03/15/17/18, CFD-05/06 and VIZ-01–04, then UX F2–F4 and the corresponding UI traceability rows.

**Remaining proof:** smooth-curve evaluator/constraint fixtures; imported/catalog conversion residuals; water-table admission and numerical references; backend field and transient capabilities; scientific validation; native accessibility; and observed user interpretation of control weights and replay. The mockup's numerical and spatial examples remain Illustrative, including any displayed separation or modeled turbulence.
