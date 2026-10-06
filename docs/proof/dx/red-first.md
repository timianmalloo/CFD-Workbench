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

### Item 3: Row 10 (COPY-358) in the app

`WorkbenchController.DeriveFeed` swallowed the section ContractError, so the app always showed COPY-357. Change: `RunFeed.SectionFailureCode` (new optional last field), set from the caught error code in `DeriveFeed`, passed to `ProjectionContext.SectionFailureCode` at the one projection call (two extra lines beside DeriveFeed in the Analysis region; no gesture or selection region touched).

Red: `Section_CpUnavailable_PanelSolveFailed_ShowsCopy358InApp` (Desktop readiness). A mutant that restores `catch (ContractError) { return NoFeed; }` fails it: `FAIL ... the feed names the section failure: expected ANA-SECTION-RE, got ` (empty). Restored: PASS. The check drives `DeriveFeed` with a completed run whose speed puts the section Re under 100 (ANA-SECTION-RE) and projects with the feed exactly as the controller does; a window-level run could not make a completed run fail its feed (the service runs the same station solve, so such a run is recorded Failed instead), so the controller pass-through line itself is one expression, covered by the type (the field is passed by name) and by the Desktop readiness suite.

### Item 4: behavioural mutants (planted, failed for the right reason, restored via `git checkout`)

| Check | Mutant | Observed failure |
|---|---|---|
| `Projection_NoRawAnaCodeInAnyCell` | the ANA-OSWALD-UNDEFINED text in `Labels.ReasonTexts` set to `Unavailable — ANA-OSWALD-UNDEFINED` | `raw code shown ... actual ANA-OSWALD-UNDEFINED in d./e (computed)/Unavailable — ANA-OSWALD-UNDEFINED` (a first mutant in `AnalysisProjection.ReasonText` passed: that path is not the one the e row uses, so it was not a leak; replaced) |
| `Section_ShownStation_SelectedElseGoverning_Dxm9` | the Screen row appends " (400)" when the on-demand seam is supplied (the on-demand solve writes the screen) | `cavitation rows byte-equal with and without the call expected ... actual ... (400)` |
| `Section_UnderreadNotMeasured_ShowsRatifiedText` | the unsolved station renders `OK` | `shown but not yet solved: exact text expected Cp_min under-read not measured at this station (200 panels only); actual OK` |
| `Section_CpUnavailable_NoProfileAndSolveFailed_Row10` | both branches return COPY-357 | `panel solve failed expected Unavailable — the panel solve failed ...; actual Unavailable — no accepted section profile at this station` |
| `FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6` | `ApplyFoundAlpha` also runs Evaluate (records a run) | `Apply records no run: expected e09ae9cc..., got b2758586...` |
| `Polar_NonNaca0012InsideTrainingRange_ComputedAndFlagged_Ruling117` | `AvailabilityCode` returns the family code again (refuse again) | `a strip cd is computed, not refused expected True; actual False` |

Tool: scratchpad `dx2/mutate.py` (applies one exact replacement, builds, runs one check, restores the file). `git status` was clean after every run.

### Item 6: captures (app render, after all changes, example foil, V 5.14 m/s, alpha 3, h 0.6 m)

Folder `docs/proof/dx/captures/`. App renders are `01`..`11`; `mockup-*.png` are the approved mockup screens (`docs/mockups/dx-section-polar-states.html`, rendered by playwright). The example foil is not NACA 0012; since Ruling 117 it computes.

| Required state | App capture | Mockup state |
|---|---|---|
| Drag (Wing only) row, value band 13.1 to 14.613 N, COPY-364 flag in its note; Wing-only CL/CD 19.18 to 21.394, same flag | `11-wing-result-drag-band-flagged.png` (Properties, Wing result) and `06-loads-drag-wing-only.png` (strip cd row carries the flag) | `mockup-drag.png` (D5; the mockup predates Ruling 109 and 117: its band has no family flag) |
| not-measured cell beside a measured station (eta 0.049 reads 1.38 %, eta 0.061 reads "not measured at this station (200 panels only)") | `07-` and `08-section-tab-all-tables-*.png`, Stations table | the D7 state sheet row (`mockup-sheet.png`) |
| Section tab naming the selected strip ("Selected strip · eta 1") | `08-section-tab-all-tables-selected.png`, `02-section-cp-selected-strip.png` | `mockup-cp.png` (D1) |
| governing fallback ("Governing cavitation station · eta 0.012 (no strip selected)") | `07-section-tab-all-tables-governing.png`, `01-section-cp-governing-fallback.png` | `mockup-cp.png` (D1) |
| polar, transition, bucket charts | `03`, `04`, `05` | `mockup-polar.png`, `mockup-trans.png`, `mockup-bucket.png` |
| Find alpha found and no root | `09`, `10` | `mockup-find.png` (D6, drawn as a popover; built as a dialog, DXM-6) |
| Polar bracket row flagged for the section family (COPY-364) in the Section tab | bottom of `07`, `08` ("Polar bracket") | D7 sheet (row 36) |

Findings from the captures (not fixed, outside the brief): the Section tab Stations table lists every one of the 126 lattice stations, so the tab is about 3000 px tall and the Polar group sits at its bottom; the mockup shows four stations. The Stations table should list the section etas (the four stations of the run), not each strip.

### Numerical change for the CFD reviewer (Ruling 117)

Example foil (docs/examples/foildsl/foil-basic.foil, not NACA 0012), V 5.14 m/s, alpha 3 deg, h_ref 0.6 m, 64x4 lattice: before, every strip cd was Unavailable (ANA-POLAR-SECTION-UNVALIDATED) and the wing drag, CL/CD bands read Unavailable. After: strip cd 0.00799 (Ncrit 2, tip strip), Drag (Wing only) 13.1 to 14.613 N, Wing-only CL/CD 19.18 to 21.394, all flagged COPY-364. The values are the NeuralFoil outputs unchanged; only the refusal was removed. Residual risk: these are surrogate outputs for a section the surrogate was not validated on (the flag says so). Re, alpha and Ncrit bracket refusals (D14) are unchanged.

## Repair cycle 2 (trk-dx2): Stations table

The product tier samples every lattice span eta (126 on the example), so "the run section stations" is 126, not four. The Stations table now lists the stations that carry a measurement or are on screen: solved at 400 panels (at most four), the governing station and the shown station (about 5 rows on the example; tab height 3030 px to 1050 px). Red: `Section_StationsTable_ListsSolvedGoverningAndShownOnly` failed "expected 3; actual 42", passes now. `Section_UnderreadNotMeasured_ShowsRatifiedText` now selects the unmeasured station so it is listed. Captures 01, 02, 07, 08 re-rendered; the Polar group is at about 720 px of the 1050 px tab, so in the short 1500x870 bottom panel it is one scroll away, not visible at rest. No station marker words were added (no approved copy).

## Third pass (trk-dx3)

### Item 1: pre-117 runs go Historical

Red: `Freshness_Pre117MethodVersion_HistoricalAndEvaluateComputesNew` (Analysis fast ring, 0.17 s) on method version `1.3.0/panel200-gov400-te3`: `FAIL ... pre-117 run expected Historical; actual Current`. Green after the bump to `1.4.0/panel200-gov400-te3` (`MethodRecord.cs:21`, pin in `LatticeFixtureTests.cs:58` updated): `PASS ...`. Evaluate computes a second run (distinct RunKey, two stored).
