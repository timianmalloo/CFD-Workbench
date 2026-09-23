---
id: adr-0001-master-curve-degree
title: "ADR-0001: master curves are degree-3 B-splines with seven vertices; the degree is a record field"
type: adr
status: accepted
owner: "@timianmalloo"
phase: specification 1.3
tags: [geometry, b-spline, degree, control-vertex, adr]
links:
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: control-vertex-workspace, rel: relates-to }
  - { to: kernel-spike-occt-loft, rel: relates-to }
  - { to: kb-hydrofoil-workbench, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Re-decides the knowledge base's degree-5 reading for the five master (distribution) curves: the record's default
  is a degree-3 clamped B-spline with seven control vertices (six to ten), the degree is stored per curve, and
  section curves stay degree 5. Decided on a measured fixture (fairness, anchor residual, support, lever effect)
  over the five example curves at both degrees, and on the loft spike showing the surface's spanwise continuity is
  the kernel's, measured, not the master curve's.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# ADR-0001: master curves are degree-3 B-splines with seven vertices; the degree is a record field

- **Status:** Accepted
- **Date:** 2026-09-21
- **Deciders:** the operator (product), Computational Geometry lens (record), Marine CAD UX lens (editing feel)
- **Context spec/architecture:** `docs/specs/cfd-workbench-v1.md` A4.1, A4.2, A4.3; `docs/knowledge/hydrofoil-workbench/data-and-constants.md` (Alias CV count; Rhino Fair "best on degree 3"); KB index item 7 (degree-5 record *for profiles*)

## Context

Specification 1.2 carried "degree 5" for every curve because the knowledge base argued C⁴ continuity inside one curve and a degree-5 record for *profiles*. Revision 1.3 makes the control vertices the record and the polygon the editing surface (Fusion control-point spline, Rhino control points); the operator asked for few, meaningful handles and levers. Alias practice is "few CVs, degree 3 for shaping"; Rhino Fair is documented as best on degree 3. The master curves are one-dimensional graphs v(η) whose job is a fair distribution with a handful of vertices; the *surface's* spanwise continuity comes from the loft (the kernel's v-degree, measured — see `kernel-spike-occt-loft`), not from the master curve's degree.

## Decision

We will store the degree per distribution curve and default the five master curves to **degree 3 with seven control vertices** (six to ten allowed, the record's floor and ceiling of A4.1); section curves remain **degree 5**. Continuity is measured per A4.3 whatever the degree.

## Alternatives considered

- **Degree 5 with seven vertices** — C⁴ inside the curve, but on the fixture it is *less* fair than degree 3 at the same vertex count on every curve, and with seven vertices its interior vertices are global (support 98–99 % of the span), so a vertex is a whole-curve lever. Lost on fairness and on the editing model the operator asked for.
- **Degree 5 with nine vertices** — fairer than degree 5 with seven on some curves and worse on others (chord 1.784 vs 0.282; LE 0.866 vs 1.196), still near-global support (94 %); more handles than the brief wants. Lost.
- **Degree 3 with nine vertices** — the fairest on four of five curves and the first count at which the middle vertex is local (65 % of span), but two more handles per curve than the seven the brief measured as right. Kept as the *reach* of the record (six to ten): Insert CV takes a curve there when the shape needs it.

## Evidence (the fixture: `spikes/degree-adr/fixture.json`, run 2026-09-21 on the mockup's evaluator)

Same six anchors fitted per curve with the root-tangent row; κ′ energy = ∫(dκ/dη)² over η (lower is fairer); support = the fraction of η an interior vertex moves (threshold 10⁻⁹); lever = curve change per unit lever move.

| Curve | d3 · 7 CV | d5 · 7 CV | d5 · 9 CV | d3 · 9 CV |
|---|---|---|---|---|
| LE rail — κ′ energy · pieces · support | 0.918 · 1 · 0.98 | 1.196 · 2 · 0.98 | 0.866 · 3 · 0.945 | 0.514 · 3 · 0.652 |
| TE rail (chord) | 0.251 · 1 · 0.98 | 0.282 · 1 · 0.98 | 1.784 · 3 · 0.945 | 0.558 · 2 · 0.652 |
| Dihedral | 0.117 · 2 · 0.98 | 0.152 · 2 · 0.98 | 0.199 · 1 · 0.945 | 0.080 · 2 · 0.652 |
| Twist | 26 790 · 1 · 0.99 | 51 910 · 2 · 0.99 | 5 609 · 2 · 0.99 | 2 050 · 2 · 0.662 |
| Thickness t/c | 0.225 · 2 · 0.99 | 0.288 · 2 · 0.99 | 0.420 · 2 · 0.99 | 0.126 · 2 · 0.662 |
| Anchor residual (all) | 0 | 0 | 0 | 0 |
| Lever effect (all) | 0.594 | 0.551 | 0.551 | 0.598 |
| Continuity inside the curve | C² | C⁴ | C⁴ | C² |

Read: at seven vertices, degree 3 is fairer than degree 5 on all five curves (a degree-5 curve with seven vertices has two interior spans and wiggles to interpolate); the lever effect is the same to 8 %; **local support at seven vertices is global for either degree** (the GEO-13 local-support test therefore uses an off-centre vertex, which is local — vertex 6 of 7), and locality arrives at nine vertices for degree 3 only. C² inside a master curve is enough because the surface's continuity across η is measured on the loft (A4.3), and the loft spike shows the kernel choosing its own v-degree (3–5) regardless of the master curve.

## Consequences

- A4.1 stores the degree per curve; A4.2's Insert CV and Rebuild move a curve between six and ten vertices; Fair and Fit points work at the curve's own count.
- The 1.2 sentence "degree 5 with simple interior knots is C⁴" becomes "degree p … C^(p−1)" (done in 1.3).
- Sections keep degree 5 (KB item 7 stands for profiles: the LE turn needs it, as the section-fit fixture in the v5 review shows — 7.5 µm with twelve vertices).
- The knowledge base's data-and-constants row on continuity is annotated to cite this ADR (the table stays true for any p).
- If a fairing study later shows degree-3 master curves cannot hold a required G3 at the root mirror, this ADR is superseded, not edited.
