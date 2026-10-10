---
id: proof-etc-c2-cost
title: "ETC C-2 cost headroom for Analysis part 1: measurements"
type: proof-pack
status: active
owner: "@trk-etc"
phase: implementation
tags: [etc, analysis, partition, c-2, timing]
links:
  - { to: proof-abl-measure, rel: relates-to }
  - { to: proof-etc-red-first, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Why Analysis part 1 of 2 failed C-2 under load, the per-group and per-check costs, the two-line fix (measured hints, one shared
  fixture), and before/after part walls. The 5000 ms cap and its load gate are unchanged.
---

# ETC C-2 cost headroom for Analysis part 1

Track `trk-etc`, branch `feat/etc-export-telemetry`, base `5778ec79`, 2026-10-10, Release, macOS, quiet machine (1-minute load 3 to 5).
Each wall below is `dotnet run --no-build -- --part=k/2` timed by a millisecond clock around the process, so it includes the ~0.5 s
process start. It is the same quantity C-2 reads. The ring's own parallel jobs add load on top; the numbers here are the floor.

## Cause (measured)

Part 1 was the heavier part: 4.35 to 4.77 s against 3.89 to 3.92 s for part 2 (a 12 % skew). The group hints in
`tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs` were stale: `TeFloor` was hinted 202 ms and costs 1 ms, `SectionSeam` 1528 ms
against 1299, `NotResolved` 842 against 985. The partition is a pure function of the hints, so stale hints put 4.0 s of groups
on part 1 and 3.4 s on part 2. Under a load of 15 to 31 the same part grew to 5.0 to 5.9 s.

Duplicate work: `NotResolvedTests` and `SectionForceTests` each built the identical Example run (same source, settings, fixture)
at one chordwise panel (about 0.3 s per build), in separate classes, so the build ran twice in the same part.

## Slowest checks (ms, one run per part, before)

Part 1: NotResolved_FourChordwisePanels 322, NotResolved_TwoChordwisePanels 320, SectionForce_OneChordwisePanel 312,
NotResolved_OneChordwisePanel 303, Section_WingRun_PanelValuesAtEveryStation 294, Section_GoverningEstimate_Cambered 190,
Section_NearTie_GoverningReSelectedAt400 177, Section_GoverningStation_ScreenAndCpMinFrom400Panels 170.
Part 2: F6_ObservedOrder 246, Analysis_DraftOpen_EvaluatesAcceptedRevision 196, SectionProjection_EstimatorLabel_DepthAware 192,
SectionView_Speeds_FollowUnitsSwitch 191, Section_StationsTable 129, Evaluate_SectionStationsChanged 118, Freshness_UndoToEqualKey 105,
Evaluate_TipChordBelowFloor 105.
No check is over 330 ms, so no single check was the lever; the groups are.

## Groups (ms, median of three whole-harness runs in one process)

SectionSeam 1299, Service 1084, DxSection 1060, NotResolved 985, RunStore 756, Freshness 630, Lattice 453, Strip 322, SectionForce 317,
NeuralFoil 100, SectionEstimator 61, ProvenanceSeam 50, Labels 48, OperatingSearch 40, Projection 31, PolarNumerics 29, PanelCp 25,
PolarSeam 14, LoadsView 9, Architecture 6, Cavitation 6, TipPolar 2, TeFloor 1. After the shared build, `NotResolved` and
`SectionForce` together: 937.

## The fix

1. Group hints set to those medians; the longest-first assignment now splits 3664 / 3664 by hint (was 4.0 s against 3.4 s measured).
2. `SectionForceTests.ExampleRun` calls `NotResolvedTests.ExampleRun`, so the identical run is built once per panel count; the two
   classes are one group (`NotResolved+SectionForce`, run in that order), so the sharing holds in either part. Same checks, same
   assertions: 248 PASS lines before and after (147 + 101 before; 102 + 146 after), 0 FAIL.

Not done: a third part. It needs `DEFAULT_JOBS` and the self-tests in `tools/check-test-costs.py`, which this track does not own.

## Walls (ms, part 1 / part 2)

| State | Run 1 | Run 2 | Run 3 | Run 4 | Median part 1 | Median part 2 |
|---|---|---|---|---|---|---|
| Before | 4774 / 3888 | 4385 / 3918 | 4345 / 3901 | | 4385 | 3901 |
| Hints only | 4430 / 4161 | 4243 / 4160 | 4212 / 4208 | | 4243 | 4161 |
| Hints and shared build | 4189 / 3922 | 4153 / 3942 | 4096 / 3991 | 4083 / 3993 | 4125 | 3957 |

The slower part fell from 4385 to 4125 ms (-260 ms, -5.9 %); headroom under the 5000 ms cap rose from 615 to 875 ms (+42 %).
The skew fell from 12 % to 4 %. The cap is not raised and C-2 is not made advisory.

The gain is a floor measurement on a quiet machine; the gate's failures were at load 15 to 31, where wall grows with load.

## Full ring after the fix

`tools/run-tests.sh`, one run, 1-minute load 3.45 at start and 28.85 at end (the same load band in which the part failed at 5035 and
5909 ms before): `Analysis.Tests 1/2 4917 ms, 102 PASS`; `Analysis.Tests 2/2 4506 ms, 146 PASS`; `test costs: 0 failures, 0 COST-MISS`;
`all test harnesses passed`. One ring is one sample, not a proof of the tail: the part passed at load 28.85 with 83 ms to spare, so the
margin under heavy load is thin; the next three joins' C-2 readings are the evidence to keep.
