---
id: note-20260926-binary64-evaluator
title: "Core keeps one binary64 B-spline basis (SplineBasis); only the FoilSource copy is folded into it"
type: decision-note
status: in-review
owner: "@timianmalloo"
phase: architecture — spec 1.6 (CAD-first)
tags: [decision-note, geometry, evaluator, refactor]
links:
  - { to: adr-0006-driving-dimensions, rel: refines }
  - { to: architecture-application, rel: relates-to }
review-by: 2027-03-25
review-suggested: []
summary: >-
  The Wing estimates and every display path use the shared binary64 `SplineBasis`; the one duplicate private evaluator
  (`FoilSource.cs:574`) is folded into it under a byte-identical golden master of every fit and patch. The rational
  certificate and its display sampler are excluded, because they are the proof path.
---

# Core keeps one binary64 B-spline basis; only the FoilSource copy is folded into it

- **Kind:** decision
- **Confidence:** Verified (sites read on `feature/ui-cad-direction` at `f56d259`)
- **Made during:** `/define-architecture` for spec 1.6, repair cycle 1 (Simplifier and Enterprise council findings)

## The call

The first draft of ADR-0006 named four "private evaluators". The council checked them, and so did the author:

| Site | What it is | Disposition |
|---|---|---|
| `src/CfdWorkbench.Core/ConstrainedFit.cs:576-582` `SplineBasis` | the shared binary64 basis and derivative jet | **Keep — the one binary64 evaluator** |
| `src/CfdWorkbench.Core/ThicknessFit.cs:253-265` | already calls `SplineBasis.Values` | nothing to do |
| `src/CfdWorkbench.Core/FoilSource.cs:574-590` | a private de Boor evaluator | **fold into `SplineBasis`** |
| `src/CfdWorkbench.Core/AuthoringSession.cs:322-332` | display sampler over the rational Bernstein certificate spans | **exclude** (proof-path data) |
| `src/CfdWorkbench.Core/Geometry.cs:805` | exact rational evaluator of the certificate | **exclude** (the proof path) |

The fold can change the bytes of fitted or patched source if round-off differs, so it runs under a golden master:
every fit, patch and construction fixture must produce byte-identical source before and after. A difference stops the
fold; it is not accepted as "within tolerance".

## Alternatives dismissed

- Fold everything into one evaluator, including the certificate — no: the rational path is the proof and must stay
  independent of the fast path it checks.
- Leave the duplicate — no: two definitions of one quantity is a defect signature (domain modelling DM rules).

## Promotion rule

If the fold changes any accepted byte, promote to an ADR before merging it.
