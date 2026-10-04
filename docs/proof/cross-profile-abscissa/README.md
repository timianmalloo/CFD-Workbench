---
id: proof-cross-profile-abscissa
title: "XPA probe — compatible fit and knot propagation across station profiles (Ruling 71 option 1)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: design
tags: [section, profile, abscissa, compatible-fit, knot-insertion, knot-removal, certificate, blend, ruling-71, xpa, probe]
links:
  - { to: design-cross-profile-abscissa, rel: relates-to }
  - { to: proof-m12c-certificate-spike, rel: relates-to }
  - { to: adr-0005-point-types, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Measured with the as-built Core. Compatible fit (each section keeps its own points; a derived copy is fitted onto
  one shared spacing): a 6-point Tip fits a 10-point Root's spacing to 1.5e-16 chord (the app's sqrt spacings nest),
  an 8-point Example section fits a 10-point NACA spacing to 3.9e-5 chord on the record (5.7 um on the placed surface at 127 mm); the reverse direction
  (10 onto 6) is 2.3e-4 chord (28.7 um at 127 mm, over); a Tip anchor missing from the shared spacing costs 1.2e-3
  chord. Fit and measure take about 100-130 ms per profile, assess 25-70 ms. Knot propagation: insertion moves the
  partner 6.0e-17 chord with bitwise-equal x. Both options hit the certificate's blend capacity: at most 5 Bezier
  spans as built, so any anchor on a 10-point section needs the budget raised; with it raised, 6 spans certify.
review-suggested: []
---

# XPA probe — compatible fit and knot propagation across station profiles

Evidence for `docs/design/cross-profile-abscissa.md`. Raw rows: `output/table.md` (as-built Core) and
`output/table-budget-variant.md` (one-line budget patch, below). Apple M4 Max, .NET SDK 10.0.203, Release, 2026-10-04.

## Re-run

```sh
cd docs/proof/cross-profile-abscissa
dotnet build -c Release probe
dotnet probe/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll .
```

Every row except the `ms` timings is deterministic. The output directory is the first argument; the Example foil is
found from the binary upward, so any output directory works. The probe is outside `CFDWorkbench.slnx`. It reuses the
Core test assembly's `InternalsVisibleTo` grant (the GSPK and SPK pattern). It calls the as-built `SectionEdits.Apply`,
`FoilSource.InsertOnce`/`RemoveOnce`/`SelectRemovable`/`WriteSurfaces`/`WriteSideTangents`/`InsertProfileKnot`/
`DeleteProfileVertex`, `DatImport.FitToBasis`, `ProfileFair.Rebuild`, `Geometry.Assess`,
`FoilSource.MaxOrdinateDeviation`, `Placement.Frame`/`Sections`, and by reflection the private `SectionEdits.Refit` and
`FoilSource.SqrtProfileBasis`. It re-implements no geometry. Deviations are sampled lower bounds (2001 equal-x samples
over [0, 1]; `MaxOrdinateDeviation`, or the same sampling through `ProfileEvaluator.OrdinateAt`).

**Budget variant.** A disposable copy of `src/CfdWorkbench.Core` in the session scratchpad, with one line changed in
`Geometry.Assess` (`Geometry.cs:388`): `const int blendNodes = 256;` → `int blendNodes = Math.Max(256, spanCount * 48);`.
Run with `dotnet build -c Release probe -p:CoreProject=<copy>/CfdWorkbench.Core.csproj -o <bin>` and
`dotnet <bin>/CfdWorkbench.Core.Tests.dll . -budget-variant` (the second argument suffixes the output file names).
Nothing in `src/` was changed.

## Fixtures

- **Default:** `FoilSource.NewDefault()`, then `MakeIndependent(…, "naca-0012", 0)`: Root holds `naca-0012-i1`, Tip
  holds `naca-0012`. Ten vertices per surface, degree 5, 5 Bézier spans. Root chord 0.127 m, Tip chord 0.0127 m. Root
  is made distinct by a y move of `cv-5` (x = 0.34) by +0.005 chord; the document then certifies (row A0).
- **Example:** `docs/examples/foildsl/foil-basic.foil` (`section-a`, 8 vertices, 3 spans), Make unique to Root.
- **Tip6:** `section-a`'s shape fitted once onto the 6-vertex sqrt spacing (`SqrtProfileBasis(6)`, one span) and
  written as Tip's authored profile in the default document.

## Results — compatible fit (section L, the primary option)

Each row fits the authored shape of one profile onto the other's spacing with `DatImport.FitToBasis` (161 cosine
samples per surface, endpoint pins), splices the result in as that station's profile, measures the fitted profile
against the authored one, and certifies the pair. Micrometres are the chord fraction × 12.7 mm and × 127 mm.

| Pair | Shared spacing (vertices, spans) | Fitted deviation from authored | Certificate (as built / budget variant) | Fit + measure / assess |
|---|---|---|---|---|
| Root NACA 10 + Tip6 (authored, unchanged) | — | — | Unsupported "abscissae differ" | — |
| P1: Tip6 fitted onto Root 10 | 10, 5 | **1.5 × 10⁻¹⁶ c** (both sqrt spacings carry the same x(t); one span nests in five) | Certified | 123 ms / 56 ms |
| P1r: Root 10 fitted onto Tip6 | 6, 1 | **2.3 × 10⁻⁴ c** = 2.9 µm / **28.7 µm** — over at 127 mm | Certified | 268 ms (includes first-call JIT) / 25 ms |
| P2: Example `section-a` 8 fitted onto NACA 10 | 10, 5 | **3.9 × 10⁻⁵ c** = 0.50 µm / 5.0 µm | Certified | 169 ms / 70 ms |
| P3: Tip6 fitted onto Root 10 + anchor at 0.58 | 15, 6 | 3.2 × 10⁻¹⁶ c | NotAssessed (node budget) / Certified | 110 ms / 43 ms |
| P4: Tip6 + anchor at x ≈ 0.3, onto Root 10 with that anchor inserted (Root change 5.7 × 10⁻¹⁷ c) | 15, 6 | 7.4 × 10⁻¹⁶ c | NotAssessed (node budget) / Certified | 106 ms / 39 ms |
| P4n: the same Tip, onto Root 10 without Tip's anchor | 10, 5 | **1.2 × 10⁻³ c** = 14.7 µm / 147 µm — over | Certified | 98 ms / 58 ms |

**Surface level.** Rule A on the authored profiles at equal chord fraction (what `Placement` draws from the record
today, certified or not) against Rule A on the compatible pair, at η = 0, 0.1 … 1, in metres at this wing's chords:
P1 1.3 × 10⁻¹¹ µm · P1r **38.4 µm** (η = 0) · P2 **5.7 µm** (η = 0, Root's 127 mm) · P3 2.1 × 10⁻¹¹ µm ·
P4 5.2 × 10⁻¹¹ µm · P4n **85.3 µm at η = 0.7**. The surface number can exceed the record number scaled by a station
chord (P4n: 14.7 µm at Tip's chord on the record, 85 µm on the interior surface), because Rule A renormalises each
profile by its own maximum thickness before blending.

## Results — knot propagation (sections A–K, the fallback option)

| Case | Result |
|---|---|
| A — Root Control → Anchor, Tip untouched | Unsupported: "abscissae differ from neighbouring profile" (the Ruling 71 defect, reproduced) |
| B — same 5 Boehm insertions at u\* = 0.5831 on Tip, both surfaces | Tip knots equal Root's bitwise; 0 of 30 control x differ; Tip shape change 6.0 × 10⁻¹⁷ c; Tip's new vertex is a derived anchor; its handle-line distance 5.8 × 10⁻¹⁹ c (τ_s = 10⁻⁹) |
| B — certificate | as built **NotAssessed, node budget** (6 spans × 48 > 256). Budget variant: Certified, also with `smooth` rows on Tip's new anchor |
| C1 — Root Anchor → Control, Tip untouched since B | same two removals on Tip: x bitwise equal; Tip change 4.4 × 10⁻¹³ µm; placed-surface change 1 × 10⁻¹¹ µm |
| C2 — as C1, Tip's anchor triple first moved +0.3 % c in y | raw Tip change 16.95 µm; Tip refit 4.98 µm at 12.7 mm; **placed-surface change 27.3 µm at η = 0.7** (the record-level number understates the surface-level one) |
| D — Root control x move by Δx, Tip x paired, Tip refit on vertices ±3 | Δx = 0.002 / 0.01 / 0.03 c: unrefit 0.54 / 2.70 / 8.24 µm; refit **0.070 / 0.347 / 1.018 µm** at Tip's 12.7 mm; placed-surface change 0.38 / 1.87 / 5.51 µm at η = 0.7; Certified as built |
| D′ — reverse: edit Tip, Root (0.127 m) follows | refit **1.02 / 5.08 / 15.0 µm** at 127 mm (3 % refused); placed-surface 1.09 / 5.46 / 16.4 µm at η = 0 |
| E — Delete Root `cv-5`, same delete on Tip | x bitwise equal; raw Tip change 5.51 µm; Tip refit 0.37 µm; Root's own change 373.8 µm |
| F — Insert at x = 0.5, same on Tip | x bitwise equal; Tip change 6.3 × 10⁻¹⁷ c; as built NotAssessed (node budget), budget variant Certified |
| G — SetTangent on Root's anchor | Horizontal: x unchanged. Symmetric: Root's surfaces stay equal; one handle x (counted on both surfaces) differs from Tip. **Angle 10°: Root's upper and lower x differ at one handle → "Independent profile x mappings are not assessed"**, partner or not |
| H — Rebuild to 8 / 10 / 14 at 10⁻⁴ c | Root and Tip rebuild onto the same knots and x bitwise; neither within 10⁻⁴ c on this fixture |
| I — chain Root(A) – 50 %(B) – Tip(C), anchor on B | to A only: still Unsupported at the B–C seam; to A and C: shared; as built node budget; budget variant **"All-query operation bound exceeds one million"** |
| J — 1–4 anchors on Root, each propagated | spans 6 / 7 / 8 / 9: as built all NotAssessed (node budget); budget variant 6 spans Certified (26 ms), 7–9 NotAssessed (all-query bound); a fifth anchor hits the 32-vertex ceiling |
| K — Example foil, Make unique to Root, anchors on Root propagated | 2 anchors (13 → 18 vertices, 4 → 5 spans): **Certified as built**, Tip change ≤ 8.3 × 10⁻¹⁷ c; the 3rd (6 spans) needs the budget variant |

## What this does not show

- Two shapes (NACA 0012 and `section-a`) on their own spacings. Not run: a cambered thin tip, a 32-vertex section,
  differing TE closures, a three-station chain under the compatible fit.
- The split of "fit + measure" between the fit and the 2001-sample measure.
- The fallback refit rows reuse `SectionEdits.Refit` as built, which frees the inner handle of a bounding anchor.
  Partner tangent rows after a refit are not measured.
- The budget variant is a measurement device, not a proposal of the code; it does not re-derive `QueryFeasibility`'s
  operation model or the query-time budget behind it.
- Old-build behaviour on the files this design writes, and cross-platform bit equality of a fitted profile, are not
  observed.
