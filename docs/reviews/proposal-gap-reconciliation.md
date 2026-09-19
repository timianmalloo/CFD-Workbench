---
id: review-proposal-gap-reconciliation
title: Final proposal gaps — requirement reconciliation
type: doc
status: accepted
owner: "@timianmalloo"
tags: [proposal, traceability, gaps]
links:
  - {to: spec-cfd-workbench, rel: documents}
  - {to: kb-cfd-workbench-grounding, rel: depends-on}
review-by: 2026-12-19
summary: Maps the completed Grok proposal's explicit CAD, section-analysis and wing-analysis v1 tables into acceptance criteria. Conflicting earlier wording and all eight final open questions have explicit dispositions.
review-suggested:
  - { by: spec-cfd-workbench, on: 2026-09-19, reason: "Full curves/stations and completed-proposal v1 contract now ready for design iteration; compare implementation and UI against this revision." }
---

# Completed proposal gap reconciliation

The user confirmed Grok had finished in `~/projects/CFD-WorkBench-Proposal`. A fresh directory/hash/diff check found the files already matched the snapshots captured earlier in this session. Therefore this is a final completeness pass over the completed packet, not a claim that another unseen revision was imported. Sequence SHA-256: `8bc340ea6659…` (full digest in the source manifest).

The source's final S12–S14 and sections 3.6, 4.5, 5.5 name **v1 obligations**, not an optional backlog. They take precedence over earlier conflicting “optional later” wording in the same proposal.

| Final proposal item | Specification realization |
|---|---|
| Catalog as parameterized-start picker | A4 catalog roles; CAT-01/03 |
| Named Eppler, NACA, strut roles; H105/6/16-series | A4 inventory; additions pending rights/generation/admission, disclosed fallback |
| Numeric inspector, anchors/levers, nudging | GEO-03/05, UX-01/02 |
| Root/tip distance and span percentage | GEO-12 |
| Per-station profile | GEO-07/08 |
| TE thickness readout and floor | GEO-12; explicit policy or Not assessed |
| AR definition in chrome | GEO-02/12; unpitched reference area clearly defined |
| Surf/wing/SUP/race presets | CAT-03 |
| Add Selig/Lednicer DAT section | CAT-02, F2 validation/preview/recovery; distinct from project import |
| Catalog→editable conversion | GEO-08; measured fit error |
| Symmetry lock/break | GEO-11; whole-wing independent materialization preview and Undo |
| Second-wing ghost | GEO-12; read-only with alignment datum/revision |
| Ncrit water control | ANA-15; 2.0 proposed screening preset explicitly an assumption |
| Re-dependent catalog ranking | CAT-01; no context-free best-profile claim |
| Cavitation envelope vs Cl | ANA-10/14/16; σ and critical-speed screening with limits |
| Dimensional forces | ANA-03; section N/m unless width supplied, wing N |
| Design vs operating Cl | ANA-16; source design Cl or Unknown |
| Envelope enforcement | ANA-01/04/05; unsupported results never extrapolated into recommendations |
| Fresh/salt fluid records | ANA-15; temperature, sourced density/viscosity/vapor pressure and freshness |
| Two-section overlay | ANA-14; conditions and revisions named |
| Load-support operating α | ANA-05; not whole-craft trim |
| Take-off screening speed | ANA-12; bracket/resolution and attached-flow limitation |
| Wing CL/CD/L/D/Cm vs α and speed | ANA-16 with ANA-08 gaps and sweep identity |
| Drag split, CoL and attachment moment | ANA-11 |
| Local Re, computed e, effective α | ANA-04 and B4; selected strip uses effective α |
| First separating station | ANA-14; section-based inference, no fabricated 3D stall |
| Loading and simple root moment | ANA-11; not structural adequacy |
| Tip-depth hint | ANA-13; static geometry only |
| Optional failed-run diagnosis | AI-05; cited observations, typed draft diff, separate user Run |

## Eight final open questions

| Question | Disposition |
|---|---|
| One substrate per OS or several? | First supported backend/substrate matrix is an architecture spike; one proven substrate per OS is sufficient for S5. Multiple paths do not gate initial release. |
| SU2 or OpenFOAM first? | Both intended targets; first version/backend selected by single-phase smoke/convergence/cancel proof, not this UI task. Free-surface remains separately gated. |
| Python on a clean machine? | Bundled catalog analysis works without Python (CAT-03); custom sections explain an optional compatible backend. No embedded runtime commitment. |
| Mesh import now? | After first product; inferred fit and explicit acceptance remain required. |
| Recover Windows trees or begin shell? | Source recovery is nonblocking separate work; neither seed clone proves a recovered product implementation. |
| Claude model id? | Configurable/pinned optional setting; saved geometry invariant under model changes (AI-06). |
| Catalog-only or CST AI seed? | Catalog-only first; CST proposals need a separately evaluated capability (AI-06). |
| H105 rights? | Admission gate remains open; do not claim bundled data or silently invent substitute coordinates. |

Independent reviewer `proposal_evidence` reread the final changed passages and returned **PASS with no remaining proposal-coverage blockers**. Scope: specification completeness and acceptance clarity. This does not establish numerical implementation, catalog admission, native readiness or full prototype interaction coverage.
