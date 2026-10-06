---
id: proof-dx-red-first
title: "DX step 2: red-first record, exit evidence and gaps"
type: proof-pack
status: in-review
owner: "@trk-dx"
phase: implementation
tags: [analysis, a3b, a3c, dx, proof, red-first]
links:
  - {to: proof-dx-test-plan, rel: depends-on}
review-by: 2026-12-31
summary: >-
  What DX built, how each check was shown red, the observed run results and the known gaps (cost ring, unapproved
  validation copy, example foil family, two-sided defaults).
---

# DX step 2: record

## Red first (honest account)

The checks were written against the API they pin, in the same sitting as the code. For the new symbols (`SectionDisplay`,
`FindAlpha`, `Labels.ReasonTexts`, `ChartModel`) the first build of the test project failed to compile: that is the red run for
every check that needs a new symbol, not a behavioural red. Behavioural reds that were observed, each by running the
check on the pre-change behaviour:

| Behaviour | Pre-change observation |
|---|---|
| Reason cells show text, not codes | `Projection_OneMissingSpanEdge_WidthUnavailable` read `Unavailable — ANA-INDUCED-DRAG-MISSING-WIDTH` then `Unavailable — ANA-OSWALD-UNDEFINED`; they were updated to the approved text after the change (they were the red for `Projection_NoRawAnaCodeInAnyCell`) |
| Row 50: craft CL/CD with a polar reads COPY-331 | `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` and `TipStrip_ExampleFoil_OutermostProvisional` failed against the new text until their expectation moved from the no-polar string |
| COPY-212 retired | `Section_NoPolarStub_Unavailable` failed until it read COPY-357 |
| Under-read text | `Section_ProvisionalAndProjectionRows` failed on the raw `ANA-PANEL-UNDERREAD` note until it read COPY-312 |

Not shown red by a mutant (gap, stated): the other new checks were green on first run after the implementation; no planted
mutant was run for them. The test architect's "mutant" column is therefore not met for those names.

## Exit evidence (observed)

- `tools/run-tests.sh` (last run, load 19): every suite RESULT failures=0 except the cost gate: `C-2 Analysis.part2of2 took 5850 ms, over 5000 ms`.
  C-3 net 49.4 s (under 50 s). C-5 clean (0 COST-MISS). The C-2 figure is a loaded-machine reading (1-minute load 15 to 19, other tracks running); a quiet
  reading is needed. The Analysis fast group `DxSection` holds 28 checks; seven checks over the C-5 bound moved to the Analysis readiness ring.
- Readiness: Analysis `--readiness` 23 PASS, failures=0 (includes `Projection_NoRawAnaCodeInAnyCell`, 1.1 s). Desktop `--readiness`: `Section_TabBody_BuildsChartSelector`,
  `SectionView_OneView_SelectorDrivesChart`, `SectionTab_Desktop_RendersAllChartsWithoutThrowing`, `FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6` and
  `Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870` all PASS (the band stays 41 px with the Find α button).
- Captures: `docs/proof/dx/captures/*.png` (app render of the Section tab, four charts, selected strip and governing fallback, Find α found and no root, Drag (Wing only) on the default foil).

## Known gaps and decisions for the Coordinator

1. **Example foil polar.** The embedded example foil's section is outside the surrogate's NACA 0012 family, so the product's strip coupling makes every strip's cd
   Unavailable (`ANA-POLAR-SECTION-UNVALIDATED`, A3c as built) and the wing profile drag reads COPY-329. Ruling 108 DXM-2 says outside-bracket values are flagged, not
   refused; the coupling (not DX's file) still refuses by family. The Section tab itself draws the polar for any foil the CST fit can represent.
2. **Copy not in an approved row** (minimal, flagged for approval): the DXM-9 station naming words ("selected strip", "governing cavitation station"); the Find α
   validation message "Enter a target CL and an ordered α bracket." and its "Finding…" interim; the two low-confidence variants without a value (drag band notes);
   `ANA-FIND-MAX-ITERATIONS` still reads its code in the Find α sentence (no approved reason).
3. **Find α "h/c below floor"** uses `FreeSurfaceCorrection.MinHOverC` (0.5) as the floor: an interpretation, not a ruled number.
4. **Row 10 / 358 in the app**: `WorkbenchController.DeriveFeed` (not DX's region) swallows the section failure code, so the app shows COPY-357 for a panel failure.
   `ProjectionContext.SectionFailureCode` carries it once the controller passes it.
5. **Files touched outside the owned list** (additive): `src/CfdWorkbench.Analysis/AnalysisViewModel.cs` (`Run` property), and edits to existing tests whose expectations
   were the old raw-code strings: `ProjectionTests.cs`, `PolarNumericsTests.cs`, `TipPolarTests.cs`, `LoadsViewTests.cs`, `SectionSeamTests.cs`, `StripFixtureTests.cs`,
   `AnalysisPanelTests.cs` (Desktop). New Analysis files: `SectionDisplay.cs`, `FindAlpha.cs`.
6. **Not covered by a check** (named in the plan, thinner than planned): the Desktop Find α dialog is exercised through the controller and the buttons, not by typing into
   the dialog; unit switch (Ruling 115) out of scope; the profile-view Cp comb is geometry-checked, not pixel-checked.
7. The regex for raw codes is `(?<![A-Za-z-])ANA-[A-Z0-9-]+`: the approved COPY-259 text contains `DR-ANA-5`, which the plain `ANA-[A-Z0-9-]+` would flag.

## Repair cycle 1 (trk-dx2)

### Item 1: Ruling 117 (compute and flag unvalidated section families)

Red: `Polar_NonNaca0012InsideTrainingRange_ComputedAndFlagged_Ruling117` (Analysis readiness, 0.3 s) on the pre-change `src/` (stashed): `FAIL ... a strip cd is computed, not refused expected True; actual False` (every strip refused with `ANA-POLAR-SECTION-UNVALIDATED`). Green on the change: `PASS ...`. Full Analysis fast ring and readiness: failures=0 after `Section_EditedProfileNoPolar_Unavailable` moved from "availability code = SECTION-UNVALIDATED" to "SectionUnvalidated flag true, no availability code".

What changed (for the CFD reviewer): `PolarResult.AvailabilityCode` no longer returns the family code, so `StripCoupler.ProfileCd` no longer refuses a section outside the NACA 0012 family; it returns the unchanged NeuralFoil cd with `FlagCode` `ANA-POLAR-SECTION-UNVALIDATED` (joined with `ANA-POLAR-LOW-CONFIDENCE` by `|` when both apply, `StripFlags`). No number the polar computes changed: NeuralFoil weights, CST fit, `RefusalReason`, `BracketFlags` and the hashes (Ruling 85) are untouched. What changed is which runs now have numbers: runs on non-NACA-0012 sections inside the training range now carry strip cd, profile and wing drag bands, wing CL/CD and the polar-consistency inputs (previously Unavailable or NaN). Refusals that remain: Re, alpha and Ncrit outside the validated bracket (D14, unchanged, not in Ruling 117) and a CST residual above 0.00036 c or a point outside the training range. The persisted strip `FlagCode` can now hold the new code for new runs; old rows still read their stored refusal (the COPY-350 text stays in `Labels.ReasonTexts` for them). The COPY-364 family word is "Non-NACA 0012" (a derived revision has no family name).

### Item 2: Ruling 118 wording

Each text goes through the one `Labels` lookup (`StationName`, `FindNeedsInput`, `FindRunning`, `FindReason` with the iteration count, `LowConfidenceStrips`); `FindAlphaDialog` reads `Labels`, not literals. DESIGN.md rows COPY-394..399 appended in the same commit. `Labels_Ruling118_ApprovedTextsAndDesignRows` (fast ring) pins each text to its DESIGN.md row. Red: these checks reference new symbols, so on the old code the test project does not compile (symbol-level red, not behavioural); the behavioural updates `Section_ShownStation_SelectedElseGoverning_Dxm9` (names) and `Polar_LowConfidence_AdvisoryReachesDragSums` (strip count) were edited to the approved text. Find α depth floor: `FindAlpha.DepthFloorHOverC = FreeSurfaceCorrection.MinHOverC = 0.5`, and the reason reads "the foil is too shallow (h/c below 0.5)" (asserted). Analysis fast ring and readiness: failures=0.
