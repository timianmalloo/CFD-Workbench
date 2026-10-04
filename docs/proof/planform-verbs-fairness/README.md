---
id: proof-planform-verbs-fairness
title: "SPK — fairness and Rebuild evidence for 4- and 5-vertex channels (planform point verbs)"
type: proof-pack
status: in-review
owner: "@track-spk"
phase: implementation
tags: [planform-verbs, fairness, rebuild, adr-0001, amendment-2, ruling-62, ruling-64, spk, spike]
links:
  - { to: design-planform-point-verbs, rel: depends-on }
  - { to: adr-0001-master-curve-degree, rel: relates-to }
review-by: 2026-10-31
summary: >-
  Measured with the as-built Core (SplineBasis, ChannelEvaluator, ConstrainedFit, FoilSource's own New foil
  construction, WingEstimates' area integral): at 4 and 5 vertices the five Example curves are fairer (lower κ′
  energy) and have no curvature breaks, but no longer pass through their six anchors (LE 0.84 mm, chord 1.0 mm at 4).
  Support is global. The 4-point New foil (Ruling 64) moves the rails by LE 3.05 mm and TE 8.83 mm at 467.5 mm and
  turns the TE tip 35.0°. Its κ′ energy is about 10⁻⁷ of today's 10-point rails. Each 4-point rail has one comb sign
  change 24 mm from the root. The other three channels are constants, exact at any count.
review-suggested: []
---

# SPK — fairness and Rebuild evidence for 4- and 5-vertex channels

This is the evidence that ADR-0001 Amendment 2 names, and the New foil default of Ruling 64. Track SPK of
`docs/design/planform-point-verbs.md` §14 owns it. The full table is in `output/table.md` and the raw numbers,
control points and knots are in `output/results.json`. The ADR carries the compact table.

## Re-run (reproduces `output/` byte for byte)

```sh
cd docs/proof/planform-verbs-fairness
dotnet build -c Release probe
dotnet probe/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll .
git diff --exit-code output/
```

The probe is a console project outside `CFDWorkbench.slnx`. It reuses the Core test assembly's `InternalsVisibleTo`
grant (the GSPK pattern). It calls `SplineBasis.Evaluate`, `ChannelEvaluator.Value`/`Parameter`,
`ConstrainedFit.Solve` and `FoilSource.Parse`/`NewDefault` directly. It calls `FoilSource.FitOrdinates`,
`TipClusteredChannel`, `UnitChord` and `WingEstimates.IntegrateOrdinate` by reflection. It does not reimplement any
evaluator. Run 2026-10-03 on macOS arm64, .NET 10.0.203; the run takes about 3 s.

## Fixture (`fixture.json`)

- **The five Example curves** are ADR-0001's 2026-09-21 fixture: the six anchors per curve at η = 0, 0.2 … 1 of the
  mockup's *Example · race light* (`docs/mockups/workbench-v5.html`, `EXAMPLES.light`). The original fixture file
  was never committed, so this one is reconstructed from the mockup and labelled synthetic. The units are as the
  mockup stores them: m, m, m, degrees, chord fraction.
- **The shipped Example** is `src/CfdWorkbench.Desktop/Assets/example.foil`. Four of its channels are constants and
  only twist varies.
- **New foil** is `FoilSource.NewDefault()` (10-point rails). The 4- and 5-point rails use the same construction
  replayed at that count: tip-clustered knots, a least-squares fit to the analytic elliptical target with the root,
  root-square and tip pins, then scaled to 0.1 m². The replay at 10 reproduces `NewDefault()` bit for bit (§6 of
  the table).

## Definitions (the 2026-09-21 definitions were not recorded, so these are now the reference)

- **Fit at d3 · N:** clamped uniform knots, abscissae at the Greville points (so η = t). Least squares through the
  six anchors with the root-tangent row P₁.y = P₀.y as a hard constraint. At 7 this interpolates.
- **Anchor residual:** the largest |v(ηₖ) − anchorₖ| over the six anchors, in the curve's unit. For a New foil
  rail it is the largest distance (mm) from the analytic target scaled to 0.1 m², at 401 uniform η.
- **κ:** the curvature of the graph (η, v), with η ∈ [0, 1] and v in the stored unit, as in the ADR's original
  table. **κ′ energy** is ∫(dκ/dη)² dη, using 400 samples per knot span, central differences inside the span and
  the trapezoid rule. Lower is fairer.
- **Pieces:** the number of monotone pieces of κ(η), which is the sign changes of dκ/dη plus one. **Comb sign
  changes:** the sign changes of κ (inflections). A value at or below max(10⁻⁶ · max|·|, 10⁻⁹) carries no sign.
- **Breaks (A4.3):** interior knots where κ jumps by more than 10⁻⁶ · max|κ| between t ∓ 10⁻⁹.
- **Support and lever:** the middle vertex (index ⌊n/2⌋) moved by +1 in v. Support is the fraction of 2,001
  uniform η where |Δv| > 10⁻⁹. Lever is max |Δv| per unit move.
- **Rebuild deviation:** design §6.4 Rebuild (knots follow the current spacing; abscissae at the new Greville
  points; least squares at 401 uniform η plus 4 Gauss points per span; root, tip and root-square pins). It is
  measured on the A4.5 oracle `MaxChange` (201 uniform η plus every interior knot's η of both curves) and is a
  sampled lower bound.
- **Tip and root direction:** the end tangent in the plan, with η scaled by the half span (0.5 m) and v in m.

## Results (compact; full table in `output/table.md`)

Cell: κ′ energy · pieces · comb sign changes · anchor residual.

| Curve (unit) | d3 · 4 | d3 · 5 | d3 · 7 (recomputed) | Rebuild 7 → 4 · 7 → 5, max Δ @ η |
|---|---|---|---|---|
| LE rail (m) | 0.00238 · 1 · 0 · 8.4e-4 | 0.0766 · 2 · 0 · 4.4e-4 | 0.962 · 4 · 0 · 0 | 8.7e-4 @ 0.910 · 5.2e-4 @ 0.925 |
| TE rail, chord (m) | 0.0121 · 1 · 0 · 1.0e-3 | 0.145 · 2 · 0 · 2.3e-4 | 0.253 · 2 · 0 · 0 | 1.5e-3 @ 0.860 · 3.4e-4 @ 0.235 |
| Dihedral (m) | 0.00193 · 1 · 0 · 3.4e-4 | 0.00781 · 2 · 0 · 2.3e-4 | 0.123 · 4 · 0 · 0 | 6.2e-4 @ 0.270 · 3.5e-4 @ 0.235 |
| Twist (°) | 16.6 · 1 · 0 · 0.042 | 17.7 · 2 · 1 · 0.025 | 35 280 · 4 · 4 · 0 | 0.104 @ 0.915 · 0.080 @ 0.920 |
| Thickness t/c | 0.00444 · 1 · 1 · 4.5e-4 | 0.0243 · 2 · 1 · 3.1e-4 | 0.235 · 4 · 4 · 0 | 8.2e-4 @ 0.270 · 4.7e-4 @ 0.235 |
| Support · lever (all five) | 0.999 · 0.444 | 0.999 · 0.500 | 0.999 · 0.667 | — |
| Breaks inside the curve (all) | 0 | 0 | 0 | — |

New foil rails. Columns 4 and 5 are NewDefault's construction at that count; the last column is today's 10.
The residual is to the analytic target, in mm.

| Rail | d3 · 4 | d3 · 5 | d3 · 10 (today) |
|---|---|---|---|
| LE | 0.0388 · 1 · 1 (24 mm) · 2.99 | 38.4 · 1 · 0 · 0.48 | 4.7e5 · 1 · 0 · 0.017 |
| TE | 0.298 · 1 · 1 (24 mm) · 8.98 | 193 · 2 · 0 · 1.43 | 7.2e5 · 3 · 0 · 0.050 |
| Support · lever · breaks | 0.999 · 0.444 · 0 | 0.999 · 0.463 · 0 | 0.361 · 0.668 · 0 |

New foil 10 → 4 (Ruling 64), measured against today's 10-point rails:

| Route | Rail | max Δ @ from root | tip direction | tip turn | root turn | area |
|---|---|---|---|---|---|---|
| NewDefault at 4 (what PVC ships) | LE | 3.05 mm @ 467.5 mm | 33.70° → 10.25° | −23.45° | 0 | 1000.0 → 1000.0 cm² |
| NewDefault at 4 (what PVC ships) | TE | 8.83 mm @ 467.5 mm | −63.44° → −28.47° | +34.97° | 0 | 1000.0 → 1000.0 cm² |
| Rebuild 10 → 4 (the popover) | LE | 2.99 mm @ 467.5 mm | 33.70° → 10.22° | −23.47° | 0 | 1000.0 → 997.7 cm² |
| Rebuild 10 → 4 (the popover) | TE | 8.97 mm @ 467.5 mm | −63.44° → −28.42° | +35.02° | 0 | 1000.0 → 997.7 cm² |

## Read (Computational Geometry judgement)

1. **The mockup's estimate is reproduced (Verified).** Rebuild 10 → 4 on the New foil measures TE 8.97 mm and
   LE 2.99 mm at 467.5 mm, as the mockup said. The TE tip turn is 35.02° (the design says 35.03°). The area change
   is 1000.0 → 997.7 cm² (the design says 998.2). The area comes from WingEstimates' own integral; why the mockup
   differs is not known.
2. **What PVC ships differs from the Rebuild estimate (Verified).** `NewDefault` at 4 fits the analytic ellipse and
   rescales to 0.1 m², so its rails move LE 3.05 mm and TE 8.83 mm, and the area holds at 1000.0 cm². PVC's tests
   should take their numbers from this row, not from the mockup's 8.97 / 2.99.
3. **Fairness at 4 (Verified, on this definition).** The 4-point rails' κ′ energy is about 10⁻⁷ of today's 10-point
   rails. The tip-clustered 10-point fit chases the elliptical tip, and the 4-point Bézier cannot. The cost is shape:
   about 9 mm of TE and a 35° tip turn. The tip is the one place the 4-point rail is not the elliptical planform.
4. **One inflection per 4-point rail (Verified; a finding for PVX and the marine-CAD review).** With the root square
   (P₁ = P₀) and one free vertex, v″ is linear in t and changes sign once, 24 mm from the root. The overshoot is
   tiny: the LE goes 0.013 mm forward of the root LE and the TE goes 0.040 mm aft of the root TE, both at 46.5 mm.
   The comb will still show the sign flip. There is none at 5 or 10.
5. **A4.3 at 4 and 5 (Verified).** No curvature breaks inside any curve. At 4 there is no interior knot; at 5 the
   one simple knot measures 0 breaks.
6. **Support is global at 4, 5 and 7 (Verified).** Support is 0.999: the vertex's basis is zero only at the two
   ends. It is local only on New foil's 10 (0.361). The lever falls from 0.667 at 7 to 0.444 at 4.
7. **Anchors stop being interpolated (Verified).** At 4 the anchor residual is LE 0.84 mm, chord 1.0 mm, dihedral
   0.34 mm, twist 0.042° and t/c 0.00045. At 5 it is roughly half of that. A 4- or 5-vertex curve is a fit, not a
   curve through six anchors.
8. **The recomputed d3 · 7 column does not reproduce 2026-09-21 exactly (Verified difference; cause Inferred).**
   κ′ energy is within 1–5 % on four curves (LE 0.962 vs 0.918; chord 0.253 vs 0.251; dihedral 0.123 vs 0.117;
   t/c 0.235 vs 0.225) and +32 % on twist (35 280 vs 26 790). Pieces (4/2/4/4/4 vs 1/1/2/1/2), support (0.999 vs
   0.98–0.99) and lever (0.667 vs 0.594) also differ. The original's fit weights, knot vector, piece rule and lever
   vertex were never recorded and its fixture was never committed. The degree-3-versus-degree-5 decision is not
   re-tested here (degree 5 was not re-run). This script is now the reference for the d3 columns.

## New foil's other three channels (Ruling 64: report, do not change)

Dihedral and twist are 0 and thickness is 0.12, all at 10 vertices. A constant is exact at any count: Rebuild
10 → 4 and 10 → 5 measure max Δ = 0, and κ′ energy is 0 (thickness < 10⁻⁹, rounding). **The numbers give no reason
to change their defaults.** Accuracy and fairness are the same at 4 and 10. The only consideration is not
geometric. The thickness refit ("From this section", R-3) holds fewer station t/c values with fewer points, which
argues for keeping 10 on thickness. That is a product call and is not changed here.

## Residual risks

- κ′ energy is in mixed units (η dimensionless, v in the stored unit), as in the original table. It compares counts
  on one curve, not curves with each other.
- `MaxChange` is a sampled lower bound (A4.5). It measures vertical Δv at fixed η, which overstates the normal
  distance near the steep tip.
- The pieces count uses a 10⁻⁶ relative / 10⁻⁹ absolute sign floor. A different floor changes the count on
  near-flat stretches.

## Duration

Box 30 min (no same-class prior). Measured wall clock from grounding to the evidence written: 10 min (start
marker to `date`, both observed); docs-graph derive, check-docs and the commit follow. This is the first same-class
prior for a fairness probe against the Core assembly.
