---
id: proof-a3a-stp-red-first
title: "A3a STP red-first receipt"
type: proof-pack
status: active
owner: "@track-stp"
phase: implementation
tags: [a3a, stp, analysis, red-first, fixtures, water]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: note-area3-fixture-arithmetic, rel: relates-to }
  - { to: proof-a3a-water-table, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The red run of the A3a STP track. Each owned check was observed red against its named mutant, then the mutant
  was removed before the commit. The water-table second check is signed in the water-table proof.
---

# A3a STP red-first receipt

Design: `docs/design/area3-analysis.md` §13 and §18.8. Session `stp`, 2026-10-04. The mutants were removed before the commit.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| `F8_Dihedral20_ClRatioToPlanar0p8938` | dihedral as a z rise on the full 450 mm half-span (product y is not foreshortened) | `FAIL F8_Dihedral20_ClRatioToPlanar0p8938 InvalidOperationException: dihedral +20° CL ratio 0.96633643` | mutant removed, `PASS`. The passing wing uses half-span 450·cos 20° mm and tip rise 450·sin 20° mm, uniform chord, 32×4, twist 0, shared S = 0.108 m² |
| `F9_Ana03Arithmetic_LiftDragAndRatio` | dynamic pressure without the ½ | `FAIL F9_Ana03Arithmetic_LiftDragAndRatio InvalidOperationException: q 64000 vs 32000` | mutant removed, `PASS` (q = 32000, L = 2688 N, D = 156.8 N) |
| `F10_Bookkeeping_NearFieldVsTrefftzWithinTolerance` | near-field trailing vortices omitted (`LatticePlant.NearFieldTrailingOmitted`) | `FAIL F10_Bookkeeping_NearFieldVsTrefftzWithinTolerance InvalidOperationException: near/Trefftz gap 1.00004` | plant left in the solver only, product path is `None`, `PASS` inside 1 % on cosine 32×4 |
| `F11_GeometryScaleK_CoefficientsInvariant` | scale the tip chord and leave the root chord (a taper, not a scale) | `FAIL F11_GeometryScaleK_CoefficientsInvariant InvalidOperationException: CL 0.42095467104965206 vs 0.41531980175160854` | mutant removed, `PASS` at 1e-12 relative, CL > 0.1 |
| `F12_SpeedScaleK_ForcesScaleK2` | expect forces to scale with k rather than k² | `FAIL F12_SpeedScaleK_ForcesScaleK2 InvalidOperationException: lift 1434.0571560397975 vs 717.02857801989876` | expectation restored, `PASS` |
| `F13a_FreshToSalt_ReFalls4p25Percent` | ν not read from the water record (fresh 1.1386e-6 used for every strip) | `FAIL F13a_FreshToSalt_ReFalls4p25Percent InvalidOperationException: Re fall 0 %` | mutant removed, `PASS` |
| `F17_GoldenMaster_ExampleFoilVector` | committed lift shifted by 1 N | `FAIL F17_GoldenMaster_ExampleFoilVector InvalidOperationException: lift 1247.8256810787905 vs 1248.8256810787905` | vector restored, `PASS` at 1e-12 relative |
| `Strip_ReLocal_UsesLocalChord` | c_ref = 0.12 m used for every strip | `FAIL Strip_ReLocal_UsesLocalChord InvalidOperationException: tip Re 2107851.7477604076 vs 1053925.8738802038` | mutant removed, `PASS` (tip Re is half the root) |
| `Reference_SrefAndSpan_FromWingEstimates` | S_ref multiplied by cos 20° (foreshortened planform) | `FAIL Reference_SrefAndSpan_FromWingEstimates InvalidOperationException: Sref 0.10148680304487782 vs 0.10799999999999969` | mutant removed, `PASS` |
| `Water_OutsideTable_Unavailable` | clamp temperature into 0–50 °C | `FAIL Water_OutsideTable_Unavailable InvalidOperationException: -0.1 was inside the table` | mutant removed, `PASS` |
| `Provenance_WaterTableHash_Shown` | pinned BLAKE3 flipped in the first hex digit | `FAIL Provenance_WaterTableHash_Shown ContractError: Unavailable — water table failed its check` | pin restored, `PASS` |
| `Loads_AttachmentMoment_TransferAboutNamedPoint` | (P − O) × F instead of (O − P) × F | `FAIL Loads_AttachmentMoment_TransferAboutNamedPoint InvalidOperationException: My -10 vs 10` | mutant removed, `PASS` (M_P = (0, +10, 0)) |
| `SolveResidual_FailedRunHasNoDiagnostics` | (the passing path) whole-row pivot on an 8×2 product lattice, translated to a contract error | the 4×2 lattice does not row-swap, so the plant is a no-op and the run completes | 8 per half row-swaps; `PASS`: Failed, code `ANA-SOLVE-RESIDUAL`, reason kept, diagnostics null, strips empty |

## Decisions

Stations on `Settings.Default`: starboard η of the cosine span law for 64 per half, including panel edges and strip centres, all in [0, 1], and the cosine chord abscissae for 4 panels including each edge, the bound point (¼) and the control point (¾). The method mirrors each starboard section to −Y. The root is kept once. Port η and span are negated; Z is unchanged. A one-half input would be a half wing, because the lattice spans the Y range it is given.

Density: `VortexLattice.Solve` already has the density overload. The product method calls it with `water.Rho`. Γ, α_i and Cl_local stay kinematic. Forces are not scaled after the solve.

`ANA-SOLVE-RESIDUAL` is caught inside `ProductWingMethod.Solve` and rethrown as `ContractError`. `AnalysisService.EvaluateAsync` records a Failed row. The 8×2 whole-row plant is the smallest product lattice that row-swaps; 2 and 4 per half do not, and the plant is then a no-op.

`cfdw analyse docs/examples/foildsl/foil-basic.foil --op {"speed":8,"alphaDeg":5}` exits 0. The run is completed, with 128 strips and diagnostics present. The embedded `example` token is not the same file as `foil-basic.foil`. A `.foil` without stamped ids left the session empty (`DOC-EMPTY`) while id insertion was refused; analyse now accepts the inserted ids in memory and does not write the file.
