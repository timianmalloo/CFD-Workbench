---
id: adr-0010-one-placement-rule
title: "ADR-0010: one placement rule — FoilDSL §6 is written once in Core and instantiated over the certificate's interval arithmetic and the display's binary64"
type: adr
status: accepted
owner: "@timianmalloo"
phase: design — M1.2b2 (3D view and elevations, Ruling 56)
tags: [geometry, placement, certificate, display, evaluator, twist, dihedral, rule-a, adr, m1.2b2, oi-1]
links:
  - { to: spec-foildsl, rel: implements }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: note-20260926-binary64-evaluator, rel: refines }
  - { to: design-m12b-points, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "none while accepted"
summary: >-
  FoilDSL §6 (Rule A section blend and the twist/dihedral placement) is written once, as a generic Core function over
  an arithmetic domain. The certificate instantiates it over rational intervals (its output bits unchanged, proved by a
  golden master); every display instantiates it over binary64 and is bound to the certificate by a measured test (at
  most 1 nm outside the certified enclosure). Curve evaluation stays two paths, as note-20260926 ruled; placement is one.
review-suggested: []
---

# ADR-0010: one placement rule

- **Status:** Accepted (Ruling 58, operator, 2026-10-01). Proposed 2026-09-30 by M1.2b2 `/design-slice`; gate record in `docs/design/m12b2-3d-elevations.md`
- **Date:** 2026-09-30
- **Deciders:** the operator (Ruling 56 OI-1 made this the first decision of M1.2b2); Computational Geometry lens (hard
  veto, narrow); Patterns Expert and Simplifier.
- **Context:** Ruling 56 created slice M1.2b2 (a 3D view beside the Plan; Front and Side elevations; dihedral, twist and
  thickness editing) and required it to start with "one shared placement rule with the certificate". The M1.2b design
  (§0.2, OI-1) and its geometry lens refused a display copy of the rule: "a second geometry definition".

## Context — what exists (read, 3a37f5f)

- The rule is normative in FoilDSL §6 (`docs/specs/foildsl.md` "For q=(x,z) …"): `X = L + c(x cos φ + z sin φ)`,
  `Y = h·η`, `Z = D + c(−x sin φ + z cos φ)`; φ = twist(η) × `0.017453292519943295`, rounded once; Rule A section
  `q = (x, C ± t(η)·T/2)` with `T = T0 / max_x T0`.
- In code it exists **once**, inside the certificate: `Geometry.PointAt` (`src/CfdWorkbench.Core/Geometry.cs`:85-119)
  over `RationalInterval`, with the section in `SectionExact`/`ProfileSection`/`ProfileComponents` (`:140-195`). The
  bit-size proof `QueryFeasibility.Prove` (`:640-655`) models the same operation sequence abstractly.
- Every display today calls the certificate: the M1.2a sample plot (`WorkbenchController.cs`:937-947, 15 points) and
  CLI `inspect` (`Program.cs`:106-115).
- The reviewed mockup is the counter-example: `workbench-v10.html` `draw3d`/`drawSide` (`:690-697`) place sections as
  `(le + x·c, y, dih + z·c)` — **no twist rotation** — although the example foil carries −2° at the tip. A display that
  re-derives placement drifts silently. This is recorded as defect class GEOM-AUTHORITY.

## Evidence (spike `docs/proof/cad-first-spikes/m12b2-placement/`, run 2026-09-30, Apple M4 Max, Release)

| Fixture | Certified `PointAt` per point | binary64 §6 per point (pointwise) | binary64 outside the certified enclosure | Separable binary64, 41 × 101 × 2 points (warm) | its worst distance outside |
|---|---|---|---|---|---|
| Example (−2° twist, flat) | 13.5 ms | 166 µs | 2.8 × 10⁻¹⁷ m | 14.8 ms | 1.43 × 10⁻¹⁰ m |
| Example + 60 mm dihedral, −8° twist | 14.1 ms | 115 µs | 2.8 × 10⁻¹⁷ m | 13.1 ms | 1.43 × 10⁻¹⁰ m |
| Two profiles blended + dihedral + twist | 37.9 ms | 530 µs | 2.8 × 10⁻¹⁷ m | 24.0 ms | 1.43 × 10⁻¹⁰ m |
| New foil | 24.0 ms | 120 µs | 0 | 17.2 ms | 1.07 × 10⁻¹⁰ m |

A display mesh of about 16,500 points through the certificate costs 3.7–10 minutes; through binary64 it costs
13–24 ms. The separable figure's 0.1 nm comes from its grid-plus-parabola maximum of the unit-thickness shape; the
pointwise binary64 path agrees with the enclosure midpoints to 3.7 × 10⁻¹⁵ m.

## Decision

1. **One rule, one site.** `src/CfdWorkbench.Core/Placement.cs` holds `PlacementRule`: the station selection (which
   assignments bracket η, collapsed when neighbours are the same geometry), the Rule A pieces (`Components`: camber and
   unit thickness; `Section`; `Blend`) and the placement, as generic static methods over `T : IPlacementScalar<T>` (the
   BCL `+ − ×` operator interfaces plus `Half`, `Point`, `BlendWeight`, `Radians`, `SinCos`). It composes channel and
   profile **values**; it never evaluates a curve. The radians-per-degree constant, the Taylor depth and the angle grid
   are declared there and nowhere else in `src/`.
2. **Two instantiations.** `RationalInterval` implements `IPlacementScalar` with today's operations (`Radians` = the
   once-rounded product, nearest-rounded endpoints, then the 64-bit dyadic grid; `SinCos` = the certified Taylor
   enclosure). `Geometry.PointAt`/`SectionAt` call the rule; their outward enclosures are **bit-identical** to the code
   at 3a37f5f (golden master captured before the change). `Binary64` (a `readonly record struct` over `double`) implements
   it with IEEE operations and `Math.SinCos`, and serves every display. `RationalInterval` arithmetic is exact per
   endpoint (`Geometry.cs`:747-753), so the same expression tree gives the same bits; the golden master (bits and refusal
   codes, captured at the implementing track's base commit, with a planted-mutant receipt) proves it.
3. **Two evaluators stay** (note-20260926-binary64-evaluator): the certificate evaluates channels and profiles through
   its Bernstein enclosures; displays through the one binary64 `SplineBasis` channel evaluator. The rule sits above both.
   The display's profile maximum is an evaluator choice, bounded by test: a grid bracket, then a safeguarded Newton step
   on the derivative with knot images as candidates (the spike's grid-and-parabola measured 3–7 × 10⁻⁴ relative error at
   a C⁰ peak and is not used).
4. **Displays receive geometry, never compute it.** Core exposes `Placement.Surface` (a placed mesh of the starboard
   half) and `Placement.Frame` (channel values at η). The Desktop applies only a camera and the language's symmetry map
   `(x, y, z) → (x, −y, z)` (`Point3.Port()`, defined in Core).
5. **Binding controls:** `Placement_DisplayWithinCertifiedEnclosure_Fixtures` (display points at most 1 nm outside
   the certified `PointAt` enclosure, on fixtures that include a 2 m chord and a C⁰ thickness peak, plus a property
   set); `Placement_DisplayMaximum_WithinCertifiedMaximum`; `PlacementRule_RadiansConstant_SingleSiteInSource`;
   `PlacementRule_SelectBlend_SameStationsAsCertificate`; the certificate golden master, whose failure message names the
   three hand-kept bound models (`QueryFeasibility`, `PlacementWidth`, `BlendPlacementWidth`); the sign fixture.

## Alternatives rejected

- **Display calls `Geometry.PointAt`** — the only zero-copy option, measured at 13.5–37.9 ms per point: minutes per frame.
- **A display implementation of §6 bound by a differential test** — two definitions of one quantity (DM7) and the
  failure the v10 mockup demonstrates; a test catches drift after it is written, a single site prevents it.
- **Fold the evaluators too** (one generic B-spline evaluator for proof and display) — note-20260926 refuses it: the
  proof path must stay independent of the fast path.
- **Instantiate `QueryFeasibility` over a bit-size domain** — it would remove the last structural copy, but the witness
  path strings are certificate output and would change. Bounded instead: shared constants and the golden master's
  failure message. The golden master pins outputs and refusals, not the operation tree: four math-preserving tree changes
  kept it green, because outward rounding absorbs them. A structural trace pin is therefore REQUIRED before the first
  change to §6 and before VW1: an `IPlacementScalar<Trace>` instantiation of the same rule whose operation string is
  goldened. It is not built yet (open item OI-11 in the design §13). `simplify:` ceiling: a rule change needs a hand
  review of the three bound models, and the trace pin before it; upgrade trigger: the first real change to §6.

## Consequences

- The M1.2a viewport doctrine "draws only certified physical point samples" (`Viewport.cs`:12) is superseded for the 3D
  view and the elevations: they draw binary64 evaluations of the one rule, bound to the certificate by test. Their
  captions say "display"; segment interpolation between display points remains Not assessed.
- A future evaluator (`cfdw-cv/3`) changes the rule in one place and both instantiations follow.
- Rollback: the refactor is internal; reverting `Placement.cs` and the `Geometry.cs` call sites restores 3a37f5f
  behaviour exactly (the golden master proves both directions).
