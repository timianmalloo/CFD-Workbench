---
id: decision-design-iteration
title: Section scope and evidence-based design alternatives
type: decision-note
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [geometry, authoring, ux, provenance, alternatives]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: spec-foildsl, rel: refines}
  - {to: adr-foildsl-authority, rel: relates-to}
review-by: 2026-12-22
summary: >-
  Defines visible section editing, shared-profile scope, draft-safe inspection, named design alternatives
  and explicit dimensional intent. Corrects thickness, interpolation and file-opening inconsistencies
  without adding a competing shape authority or a simulation implementation.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Section scope and evidence-based design alternatives

**Proposed, for review:** product revision 1.5 makes the complete task visible: create an alternative → edit a
middle section → inspect span impact → compare against a fixed baseline → Keep or Discard with rationale.
This follows the user's authorization of the five review recommendations and specification inconsistencies.
Production implementation, application/backend selection, deployment and merging remain out of scope.

**Verified by source inspection:** the previous product wording mixed full t/c with half-thickness, described
shared-knot/parameter blending while the language specifies normalized-chord correspondence, and exposed
unmapped loft names beside a single Rule A evaluator. These contradictions are corrected, not offered as
new optional scientific models. **Inferred:** the workflow below makes impact and decision intent more
discoverable. **Flagged:** that usability claim needs the formative task in UX-26; no participant result is
claimed. Prototype geometry samples do not certify geometry or predict performance.

## Conceptual model and affected surfaces

One shape authority remains accepted FoilDSL. Source projections, section thumbnails, geometry overlays and
comparison measures are derived. A design alternative is a named branch referencing accepted Design
revisions, distinct from an optimizer Candidate. Its name is project metadata; its accepted revisions are
immutable. A pinned baseline is one fixed accepted revision reference. One design-decision fact is exactly
one Keep/Discard action on one alternative against one baseline, with rationale and revision identities.
Decision facts are append-only. Discard archives a branch; it never deletes source or run evidence.

The affected surface list is: project references and decision facts → accepted FoilDSL/profile/channel
records → immutable geometry identity and analysis provenance → persistent station card and edit-scope
controls → read-only draft inspection → dimension previews → baseline comparison and missing evidence →
functional/UX/UI acceptance → v7 mockup and proof → graph/audit discovery. Native storage implementation and
solver execution are not part of this change. Existing immutable runs retain their original manifest.

## Settled interaction contracts

| Decision | Contract and reason |
|---|---|
| Section entry | Persistent actual section thumbnail, station/profile revision, effective full t/c and visible Edit section. The action survives maximized/narrow workspace layouts and has keyboard access. |
| Shared or independent | Edit shared changes all referencing assignments. Make independent changes only the selected assignment reference, but its geometry impact includes neighboring blend intervals. List assignments and affected intervals before Apply. |
| Thickness | Keep current thickness preserves the effective t/c channel. Use source thickness proposes explicit targets on that same curve at the scoped assignments, reports residual/locks and actual affected span. No per-station override competes with it. |
| Read while drafting | Selection, navigation, orbit and inspection remain available. The draft stays pinned to its original target/base. Other writes, branch changes, repinning and decisions wait for Apply/Cancel. |
| Named alternative | Create from an accepted revision; name it; edit its own revision chain. Baseline is explicitly pinned and cannot silently follow the alternative. |
| Comparison | Label accepted pair, alignment, units, sampled geometry basis and evidence compatibility. Show Not run/Historical/Incompatible and reason. No synthetic performance deltas fill missing cells. |
| Keep/Discard | Require rationale, record the accepted alternative/baseline identities. Keep leaves that alternative active. Discard archives it and returns to baseline while preserving history. |
| Chord intent | Hold leading edge edits trailing; Hold trailing edge edits leading. The held complete curve is unchanged. Solved pins may alter neighboring span; disclose actual effect and block infeasible constraints. |
| Span intent | Keep relative station positions retains eta. Keep absolute station positions retains interior physical distance by recomputing eta; root/tip remain boundaries. Both leave channel ordinates unchanged. Outside/duplicate positions block; none are silently dropped. |
| Frame | Every dimension preview states unrotated planform, x aft/y starboard/z up, and leading-edge twist pivot. Moving the pivot may change placed 3D TE despite an unchanged unrotated TE record. |

The span labels deliberately name **station positions**. “Keep proportions” would imply that dimensional
chord/elevation scale with span, which this operation does not do. No implicit uniform-3D scale exists.

## Corrected contracts and compatibility

- Effective t/c is maximum **full** upper-minus-lower distance at equal normalized chord x. Camber is the
  half-sum. Placement uses `camber ± (t/c)*unitThickness/2`; halving twice is an error.
- Rule A linearly blends normalized camber and thickness shape at equal normalized chord x and renormalizes
  the blended shape before applying the single effective t/c. Each side uses its own x inversion; equal CV
  counts, equal knots and equal spline parameter are not correspondence requirements. Any conversion to a
  shared spline basis is derived and must disclose its measured residual.
- The sole display label is Rule A · linear normalized profile blend. Straight/Through stations/Blended
  are removed because no compatible semantics were specified. A future different evaluator would need its
  own version and migration evidence; it cannot change an existing document silently.
- Open project restores `.cfdw.json` history, assets, alternatives, baselines, decisions and run references.
  Open FoilDSL previews one foil or normalized section. Shape-only `.foil` opening never invents project
  history. Save project and Export FoilDSL are distinct operations.

These changes need no new FoilDSL grammar productions. Section cloning, scoped assignment replacement,
dimensional pin commands and span changes serialize existing profile/channel/assignment records. Project
alternatives and decisions stay outside the shape language. The unapproved 4.0 rail correction and v3
migration policy remain unchanged.

## Review and falsification

Product CAD-09–13, UX-26–27, UI-31–35 and language DSL-15–18 are the acceptance contract. The v7 proof must
exercise the uninterrupted task, cancel and undo/redo, shared versus independent reach, source-thickness
infeasibility, inspection during each draft kind, pinned baseline stability, absent evidence, and archive
recovery. Geometry/data and UX/accessibility vetoes are independently reviewed; this author does not clear
them. The five-person formative task is an unverified obligation, separate from browser automation.

A subsequent gap review reports findings against the completed changes. New findings are not automatic
authorization to expand into production work or unrelated features.
