---
id: proof-a3a-ctx-red-first
title: "A3a CTX projection feed receipt"
type: proof-pack
status: active
owner: "@trk-ctx"
phase: implementation
tags: [a3a, ctx, analysis, projection, verdict, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  The red runs of the A3a CTX track: every non-tip strip read "Not judged - tip strip", and the controller fed the
  projection no stations, verdicts or root t/c. Verdicts are derived on read; no schema changed.
---

# A3a CTX receipt

Design: `docs/design/area3-analysis.md` §3 (stored facts only), §5.4 (verdict derived per strip), §18.5 rows 14, 16, 26, 30.
Session `trk-ctx`, branch `fix/a3a-ctx-projection-feed`, 2026-10-05.

**Verdict source: derived on read (option a).** The stored strip holds alpha_eff, Cl_local and the span edges, not the sweep or alpha_L0.
`MethodRecord.DeriveVerdicts(run, source)` calls the lattice's own sweep (`VortexLattice.StripSweeps`, the same `At`/`SweepOf`
over the stored strip edges and the run's mirrored sections) and alpha_L0 from `SectionEstimator` exactly at each strip's own eta
(repair 1: the interpolation and the re-derived sweep were removed). No run-row field was added.
A run on an earlier revision gets no feed (only the current accepted source is readable); its strips read "Unavailable".

| Test | Red line | Then |
|---|---|---|
| `Projection_NoVerdicts_NonTipStripNeverReadsTipNotJudged` | `FAIL ... strip 0 expected False; actual True` | `PASS` after the fall-through reads "Unavailable" |
| `Feed_InsideStrips_OnlyTheTwoTipsReadNotJudged` | `FAIL ... Unavailable: expected True, got False` | `PASS` |
| `Feed_OutsideStrips_LabelledOutsideAndOutlined` | `FAIL ... an outside strip: expected True, got False` | `PASS` |
| `Feed_Stations_DepthBandAndTipDepthRecorded` | `FAIL ... no depth-band layer (row 16)` | `PASS` |
| `Feed_RootThickness_ReadsTheRootStationRatio` | `FAIL ... Unavailable - root thickness not recorded` | `PASS` |
| `Layers_Visibility_ControllerFlagFeedsProjection` | compile red: no `SetLayerVisible` | `PASS` |

Cost (default 64 x 4 lattice, 126 judged strips): the derivation runs once per run and revision (cached in the controller);
a layer toggle re-projects in about 9 ms (`COST Feed_DefaultLattice_LayerToggleReprojection`). The derivation itself was
0.9 s measured on first projection before the cache. After repair 1 (exact alpha_L0 at 64 distinct strip etas, 200 panels) the
first projection is 594 ms (`COST Feed_DefaultLattice_FirstViewDerivation`) and a toggle stays about 11 ms.

Repair 1 tests: `DeriveVerdicts_TaperedPlanform_SweepIsTheLatticeSweepAndAlphaL0IsTheStripSection` (|sweep - solve sweep| < 1e-9 deg),
`DeriveVerdicts_AlphaBound_9p9InsideAnd10p1Outside`, `JudgeStrip_SweepBound_29p9InsideAnd30p1Outside`,
`DeriveVerdicts_OnlyTheTipReasonGetsTheTipRule`.
