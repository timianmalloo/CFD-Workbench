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
summary: Source map, precedence, evidence confidence, and conflicts carried from CFD-Bench into the Workbench specification. Source statements are distinguished from scientific validation and new product decisions.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
---

# CFD-Bench and proposal grounding

Read on 2026-09-19. **Verified** means the cited source was inspected; it does not establish that its scientific claim was experimentally reproduced. **Inferred** names a proposed product interpretation. **Flagged** names an unresolved empirical, licensing, usability, or implementation question.

## Source of intent

The user requests a Mac/PC hydrofoil design and simulation tool, a full specification, and iterative HTML designs. The available proposal folder is `~/projects/CFD-WorkBench-Proposal`, containing `README.md`, `proposal.md`, `sequence.md`, and `inventory.md`. The requested plural name does not exist. Its README and content identify the intended packet. The later `sequence.md` explicitly evolves `proposal.md`; where they conflict its product sequence governs, subject to the current request and the Workbench instruction that the stack is unselected.

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
