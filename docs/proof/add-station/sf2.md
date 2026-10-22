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

## Node SF2-2: Match the blend (Ruling 262 DF-Q2)

Design section 13. Match replaces an interior station's profile with the rule-A blend of its neighbours at the station's eta, built by
the same `StationEdits.Blend` that Promote uses, and writes it as a whole-block replacement.

### What changed

- `FoilSource.ReplaceProfileBlock(source, name, block)` (new, internal): replaces the block from the `profile` keyword to its closing
  brace, re-parses, and refuses with `DSL-PATCH` unless the profile count, every assignment eta (by bits) and every assignment's profile
  name are unchanged. `FoilSource.WriteSurfaces` is not used, so no tangent row, closure, knot or provenance of the old block survives.
- `StationEdits.Match` (plan), `AuthoringSession.PreviewMatchBlend` and `MatchBlend` (one accepted row, rail `station-match`, one undo
  step; the envelope validator and the replay fingerprint learn the rail). A profile another station also names is first made
  independent for this station (`MakeIndependent`). R3 (spacing) is a preview refusal `STATION-SPACING`; a change at or below 1e-12 is
  `STATION-MATCHED`; the budget and not-assessed refusals are Promote's. There is no `STATION-CLOSURE` (DR-DF-6 b) and no gate on the change.
- Two measures per preview, both on the Remove grid with `StationEdits.Measure`: the change (accepted against candidate) and the residual
  (candidate against `RemoveAssignment(candidate)`); the residual regime is `Regime(sameRecord(L, R), worst)`, decided by the measurement.
- Desktop: the Match flyout (title, measured line, Apply always enabled when the measure ran, Cancel), the Remove flyout's secondary action
  "Match the blend…" when the Remove deviation is above 1e-12, the station panel button, `station.match-blend` in the palette and the Edit
  menu. After Apply from the Remove flyout the Remove flyout reopens. Remove's 10 um gate is unchanged and the copy says so (COPY-625).
- Fixtures: `tests/fixtures/stations/f-peak3.foil` and `f-four.foil` (written once from the hand-built `Doc` documents: F-PEAK3 is F-PEAK with a
  station at 50 % carrying F-CAMBER's tip; F-FOUR has stations at 0, 30, 60, 100 % so the matched station has eta_L 0.3 and w = 0.4286).
  The tangent check builds its own 11-point foildsl 4.1 document in code (a profile tangent row needs a multiplicity-5 knot anchor and the
  whole loft one spacing).
- Copy: COPY-621 to COPY-632 in `DESIGN.md` (SF2-COPY-1 to 8 and 4b, plus the needs-foil reason, the flyout title and the "Measured" row).

### Red first

Core checks written before the types existed; the compile was red:
`StationTests.cs(625,20): error CS0246: The type or namespace name 'MatchPreview' could not be found`.
After the implementation `CFD_TEST_ONLY=MatchBlend_` ran 16 checks: 16 PASS. The Desktop checks were added with the controller code and
their red is the mutants below.

### Mutants (each built, the Match checks run, then reverted)

| mutant | red lines |
|---|---|
| weight w = eta_M instead of (eta_M - eta_L)/(eta_R - eta_L) | `FAIL MatchBlend_InteriorNeighbours_WeightFromNeighbours ... Remove after Match 0.0060729687801029386` (only this check) |
| blend of (L, M) instead of (L, R) | 8 FAIL, among them `MatchBlend_CommonPeak_ThenRemoveExact ... Remove after Match 0.021707620349042882` |
| no 1/k rescale in `Blend` | `FAIL MatchBlend_ChannelsAndEtaUnchanged ... F-PEAK3: P maximum 0.11938469116640175 against t(eta_M) 0.12` (F-THREE passes, as designed) |
| tangent rows kept on the written block | `FAIL MatchBlend_TangentsDropped_Counted ... the loft is not assessed (Tangent row 'cv-5' does not satisfy smooth.)` |
| closure left as M's | `FAIL MatchBlend_Closure_FromNeighbours ... the loft is not assessed (Open trailing endpoints are not separated.)` |
| shared block rewritten in place | `FAIL MatchBlend_SharedProfile_OnlyThisStation` (root's block bytes changed) |
| Desktop: Match not offered from the Remove flyout | `FAIL RemoveFlyout_AboveLimit_OffersMatchBlend Exception: Apply False, Match offered False` |
| Desktop: Apply gated by the residual | `FAIL MatchBlend_Flyout_ResidualAboveLimit_SaysRemoveStaysUnavailable Exception: Match is gated above 10 um` |

### Sampled numbers (grids as labelled; sampled, not bounds)

- F-PEAK3 station 1 (regime R2): the preview's residual equals `PreviewPromote` of F-PEAK at 50 % bit for bit (relative and metres), Remove
  after Match reports the same bits, and M's matched upper and lower control ordinates equal the P that Promote builds, bit for bit
  (`MatchBlend_DifferingPeaks_ResidualEqualsPromotionDeviation`; the `assume:` of design 13.4 held, no 1e-15 fallback was needed).
- F-THREE station 1 (common peak, R1): Remove before Match changes the loft by 0.0244 of chord (`MatchBlend_Change_EqualsRemoveBefore_WhenCommonPeak`
  asserts Match's change equals it); after Match, Remove is within 1e-12.
- Desktop, 8-point fixture with differing peaks on a 2400 mm chord (17 eta x 201 x grid): Match changes the loft by 43.94 um, and Remove
  afterwards would still change it by 166.02 um at eta 25.0 %, above the limit; Apply stays enabled, and Remove after Match stays refused.

### Harnesses (Release, exit codes read on their own lines)

Core 876 PASS, 0 FAIL, 1 SKIP (the SF2-1 timing check); Analysis 296 PASS; Cli 26 PASS; Desktop `--controller-shell` 150 PASS and
`--shell-model` 17 PASS (the station.match-blend row joins the no-gesture list). Not run: the windowed focus path (Escape on the Match
flyout returns focus to the invoking control) has no check; the Remove flyout's focus check is the only window check and was not extended.
