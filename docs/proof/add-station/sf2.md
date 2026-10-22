---
id: proof-add-station-sf2
title: "Add station follow-ups proof (SF2): the measure-path speed-up and Match the blend"
type: proof-pack
status: draft
owner: "@track-sf2"
phase: implementation
tags: [add-station, promote, rule-a, ruling-262, bit-identity]
links:
  - { to: design-add-station, rel: implements }
review-by: "2027-01-31"
summary: >-
  The observed lines of wave-2 track SF2. Node SF2-1 (the Placement.Sections speed-up under Ruling 262 DF-Q3) is recorded: a
  bit-for-bit guard over every Station fixture and the Placement fixtures, its one-ulp mutant red, the timings before and after.
  Node SF2-2 (Match the blend) is not yet recorded.
---

# Add station follow-ups proof (SF2)

Design `docs/design/add-station.md` section 13 and `docs/specs/amendments/spec-1.7.9.md`; Ruling 262. Session `w2b-sf2`,
base `main` at `b0da43bc`.

## Node SF2-1: the Placement.Sections speed-up (Ruling 262 DF-Q3)

### What changed

`Placement.SectionsForMeasure` (internal, new) is called only by `StationEdits.Measure` through `SectionsParallel`. It does the
same checks and the same `ReadStation` and `OrdinatesAt` as `Placement.Sections`, so Camber, Thickness and Frame carry the same
bits, and it leaves out what the measure never reads: `CamberSlope` at every x and the two `Place` calls per sample for the placed
midline. `Placement.Sections` and every other caller (Analysis, DatExport, the tests) are unchanged: the diff to existing code in
`Placement.cs` is additions only.

The brief expected the blended-maximum search to be skippable. It is not: `ReadStation` derives `Reciprocal` from `BlendedMaximum`
and `PlacementRule.Blend` multiplies by it, so every Camber and Thickness bit depends on it. Skipping it would change the bits the
measure reads, so it stays. The remaining cost (about 31 ms per blended eta, per S1-a) is that search; a cheaper maximum search
that keeps the bits is a separate, larger change and is not attempted.

### The guard, red first by its mutant

`Station_MeasurePath_BitIdenticalToSections_EveryStationAndPlacementFixture` (in `StationTests.cs`, which owns the fixtures)
compares `Placement.Sections` with `Placement.SectionsForMeasure` by `BitConverter.DoubleToInt64Bits` over: the ten Station
documents (F-CAMBER, F-CAMBER3, F-PEAK, F-THREE, F-SAMEPEAK, F-REAL, F-SIX, F-SEVEN, F-SPACING, F-PEAK x10 chord), the Placement
golden sources in `docs/proof/m12b2-golden/certificate-bits.json`, the m12b2 fixtures and the FoilDSL examples; 23 etas, three
chord grids each. A document the product refuses must be refused with the same code on both paths.

- guard on a first `SectionsForMeasure` that only wrapped `Sections` (today's output): `PASS`, `INFO SF2-1 bit guard: 87 document x grid pairs, 412873 doubles compared by bits`
- one-ulp mutant (`Math.BitIncrement` on Camber[1] of every section of the measure path): `FAIL Station_MeasurePath_BitIdenticalToSections_EveryStationAndPlacementFixture InvalidOperationException: F-CAMBER eta 0 camber[1]: 0000000000000000 against 0000000000000001`; reverted
- guard on the real fast path: `PASS`, the same 87 pairs and 412873 doubles

### Timings (Release, 20-core ARM64 laptop, median of three)

`CFD_SF2_TIMING=1` enables `Station_MeasurePath_Timing_GatedByEnvironment`; without it the check prints `SKIP`. The call is one
258 eta x 2001 x call on F-CAMBER (a blended document); the plan is `PreviewPromote` of F-PEAK at 50 % on the production grid.

| | before (today's path) | after (measure path) |
|---|---|---|
| one 258 x 2001 call | 8121 ms (`Placement.Sections`) | 3813 ms (`SectionsForMeasure`) |
| one promotion plan (F-PEAK, 50 %) | 9741 ms | 4682 ms |

The "before" row is the repo with the guard and a wrapper only (the same work as `Sections`); `Placement.Sections` itself stays at
about 8.2 s (8177 ms measured again in the same run as the after row). Reproduce:
`CFD_SF2_TIMING=1 CFD_TEST_ONLY=Station_MeasurePath CfdWorkbench.Core.Tests --harness` from the repo root.

### Harnesses

Core 858 PASS before, 860 after (the two new checks; the timing check prints `SKIP` and `PASS`), Analysis 296 and Cli 26 PASS both
times, every exit 0.
