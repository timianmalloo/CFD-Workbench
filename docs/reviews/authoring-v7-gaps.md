---
id: review-authoring-v7-gaps
title: Remaining specification and UX decisions after v7
type: doc
status: in-review
owner: "@timianmalloo"
phase: specification
tags: [review, ux, geometry, foildsl, gaps]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: spec-foildsl, rel: documents}
  - {to: mockup-workbench-v7, rel: relates-to}
  - {to: decision-design-iteration, rel: relates-to}
review-by: 2026-12-22
summary: A bounded post-change review separates three remaining design decisions from already declared validation and production obligations. No additional implementation is authorized or performed by this review.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
  - { by: spec-foildsl, on: 2026-09-22, reason: "Revision 1.5 clarifies shared and independent profile edits, explicit t/c targets, draft-safe inspection, dimensional intent and project-level decisions without changing the shape grammar; review dependent artifacts." }
---

# Remaining specification and UX decisions after v7

Independent review after the authoring feature freeze, 2026-09-22. These are findings for user review,
not a continuation into implementation. The five approved authoring improvements and the corrected
thickness/blend/file-opening contracts are the completed scope. Assemblies, structural design,
backend selection and production implementation remain outside it.

## Ranked decisions

| Rank | Decision still needed | Existing coverage and precise gap | Proposed acceptance example |
|---|---|---|---|
| 1 · before native save implementation | **What happens when another editor changes the canonical source file?** | **Verified specification gap:** FoilDSL §7 defines stale in-app draft bases, and §8 requires preserving source bytes. Product DOC-02/04 covers save/recovery failures. Neither defines detection and disposition of an externally changed `.foil` between Open and Save. Canonical authored text makes this a plausible ordinary workflow, even without live file watching. | Open A, edit it in the workbench, then change its original file to B externally. Save must not silently replace B. Detect the changed original content hash; retain both versions and offer a named reload/compare or Save As route. Decide explicitly whether external editing is supported as live synchronization or only explicit re-import; either policy can be coherent. |
| 2 · before claiming arbitrary shape-preserving station promotion | **Can every evaluated inspection slice become a legal bounded source profile without changing the surface?** | **Inferred feasibility risk grounded in the contract:** product A4.5/6 and GEO-06 require an exact-operation tolerance when promoting a slice. FoilDSL §5 bounds profile sides to degree 5 and 6–32 CVs; §6 blends independently inverse-parameterized shapes at equal x and renormalizes thickness. The specifications do not supply representability evidence for the resulting blended function in that bounded record. V7 safely refuses multi-profile promotion instead of assigning the wrong profile. That is an explicit prototype limit, not proof of production feasibility. | Use profiles with different knots, CV counts, x mappings and thickness-peak locations. Promote a midpoint; save/reopen; measure the complete before/after surface with a stated error bound. If the exact gate cannot be met, the UI must explain why promotion is unavailable. Any future approximate redesign option must be separately named, previewed and accepted; it cannot quietly weaken “add a station without altering shape.” |
| 3 · before full comparison UX | **Which locations correspond when alternatives have different spans?** | **Verified underspecification:** product A4.14 requires fixed alignment and a stated sample basis; span intent can keep relative or absolute station locations. V7 compares normalized sections at the same eta and labels that percentage. The product has not explicitly chosen the physical correspondence rule for section comparisons after resizing. Equal eta can mean different root distances; equal distance can lie beyond one tip. | Compare a 1.10 m-span baseline with a 1.40 m alternative at eta 0.6. State that the compared sections lie 0.330 m and 0.420 m from the root. If a same-distance comparison is offered, name that mode and show Unavailable beyond the shorter tip. Never extrapolate or silently switch correspondence. |

## Already specified, still unverified

These are not additional feature requests or newly discovered omissions:

- **Unaided usability.** UX-26 now names the complete alternative → section edit → scope understanding →
  comparison → decision task. The five-person formative session has not run. Browser reachability and
  measured targets do not establish that people can find the section editor or correctly predict edit scope.
- **Native persistence and branch evidence.** The language and product specify immutable baselines,
  accepted/recovery source, assets, decisions and run pins across save/reopen. V7 deliberately retains
  alternatives for the page session. Native archive, conflict and recovery tests remain release gates.
- **Certified geometry and preview approximation.** V7 uses sampled maximum-thickness normalization.
  Independent review detected and resolved a sharp-leading-edge interpolation error: the preview now
  evaluates each requested normalized x directly. The stronger regression checks 99 cosine-distributed
  positions per side against an independent inverse-x evaluator. The shared 201-point normalization grid
  remains a prototype bound, not a continuous maximum or certified geometry proof. Production still needs
  the stated residual/tolerance evidence; a rendered smooth curve is not that evidence.
- **Scientific decisions.** Compatible-run comparison and uncertainty/provenance requirements already
  exist. V7 correctly supplies no fabricated performance delta. A user's Keep decision is a recorded design
  choice, not a scientific superiority claim.

The smallest next step is deciding the three policies above and running the already specified formative
task. Nothing in this review selects a stack, implements a solver, or authorizes further scope.
