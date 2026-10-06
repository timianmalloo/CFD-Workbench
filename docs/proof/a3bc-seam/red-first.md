---
id: proof-a3bc-seam-red-first
title: "Track SEAM red-first ledger"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, tests, red-first]
links:
  - { to: proof-a3bc-seam, rel: relates-to }
  - { to: design-area3-analysis, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Red commits, observed failure modes and green implementation commits for the A3b/A3c seam.
review-suggested: []
---

# Red-first ledger

Each red commit was made with the failing test present before its implementation. Compile failures are named where the contract type did not yet exist. The targeted `dotnet run` tests are Analysis harness checks, not `dotnet test` projects.

| Test | What the red run caught | Red commit | Green commit |
|---|---|---|---|
| `PanelCp_DefaultResolution_CpMinWithin4Percent` | Default remained 400 instead of ruled 200; method identity omitted panel/TE choice | `c0ceb4e` | `06f0bac` |
| `PolarSample_DerivedFlags_EqualEvaluateAtWriteTime` | `Sample` returned stored `PolarSample`, dropping the derived bracket/confidence/CST metadata | `1ee9072` | `b90f8e1` |
| `Section_WingRun_PanelValuesAtEveryStation` | Missing derived section tier and projected section values | `c9260d5` | `ac3246e` |
| `RecordRun_PanelProvisionalCode_ValidatedOldRowReads` | Validator rejected the additive reason on the existing optional field | `b36e076` | `ac3246e` |
| `Cavitation_SurfacePiercingAndInvalidWater_DistinctReasons` | User-facing prose instead of stable cavitation reason codes | `d045c1c` | `365f88c` |
| `Provenance_RunInputsWaterAndChangedSince` | Projection lacked revision, water source and shared Historical banner data | `22a840b` | `ac3246e` |
| `F13b_WaterWithPolar_RetrievesAtBothNewRe` | No polar/section resolver overload for per-strip Re and both Ncrit | `faac1cb` | `ac3246e` |
| `RunKey_PolarWeightsHashAndSize_ChangeKey` | Method settings omitted the source's weight hash/size identity | `faac1cb` | `ac3246e` |
| `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` | No run-level named drag component calculation | `e6304ce` | `ac3246e` |
| `Polar_ProductRun_ReachesStripsAndSectionProjection` | Product polar values stopped before the section projection | `e18c905` | `ac3246e` |
| `FindAlpha_TargetCL_RootWithinTolerance` | Search contract absent | `4d07088` | `ac3246e` |
| `FindAlpha_NoRoot_UnavailableWithReason` | No bounded no-root result | `4d07088` | `ac3246e` |
| `FindTakeoff_RetrievesLoadAtEachSpeed` | No speed-root search | `4d07088` | `ac3246e` |
| `FreeSurface_A52_FactorsBesideDeepWater` | No separate A5.2 factors or validity data | `4d07088`, `319041d` | `ac3246e` |
| `Tip_LowRe_TipReasonPrecedesReUnavailable` | Tip envelope row followed profile Re failure rather than taking precedence | `f4c0be9` | `ac3246e` |
| `Tip_ConsistencyEdge_RecomputedWithPolarSlope` | Missing polar-slope edge and later missing Cl delta | `f4c0be9`, `967d42e` | `ac3246e` |
| `Tip_ConfidenceNeverClearsNotJudged` | Tip judgement contract absent | `f4c0be9` | `ac3246e` |
| `Polar_ProductRun_ReachesStripsAndSectionProjection` (event assertion) | `AnalysisEvent.PanelUnderreadFraction` absent; compile red | `5f21b38` | `ac3246e` |
| `FindAlpha_ZeroLift_BracketedRoot` | Search rejected a zero-CL target | `61427a0` | `ac3246e` |
| `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` (craft total assertion) | Wing-only drag was claimed as craft Total drag | `ce387ef` | `ac3246e` |
| `Section_WingRun_PanelValuesAtEveryStation` (per-span assertion) | Section tier omitted lift per span | `6f31493` | `ac3246e` |
| `Section_SurfacePiercing_EstimatorUnavailable` | Surface-piercing section could project numeric estimator rows | `5fe4ef5` | `ac3246e` |
| `Polar_ProductRun_ReachesStripsAndSectionProjection` (Cp curves/event equality) | Full section curves were absent from the view model; compile red | `f848769` | `85bad41` |

`RunKey_PanelCountAndTeExclusion_ChangeKey` and `Section_ProvisionalAndProjectionRows` were added as companion verification during implementation; they were not separately observed red. Their predecessor assertions were red in `c0ceb4e` and `c9260d5`. The edited-profile polar availability assertion was strengthened after the initial resolver test turned green. This is a red-first evidence limit, not an implied red observation.

## Repair cycle 1

The focused `CFD_TEST_ONLY` Analysis harness runs observed these failures before the repair code:

| Check | Red observation | Green repair commit |
|---|---|---|
| `Section_ProvisionalAndProjectionRows` | Cl panel note missing; later the numeric V_crit tier note missing | `c922dcc`, `2d4fdb6` |
| `Polar_LowConfidence_AdvisoryReachesDragSums`, `Tip_ConfidenceNeverClearsNotJudged` | Compile failed because strip and consistency advisory fields did not exist | `c1b959d` |
| `Section_CamberedPreparedPanel_ReusesMatrix` | Compile failed without prepared panel solve; the full cambered wing then failed on an unused infinite leading-edge slope | `f6d526e` |
| `Section_ProvisionalAndProjectionRows` | Legacy outside verdict rendered as `ANA-PANEL-UNDERREAD` | `5f0f82a` |
| `FreeSurface_A52_EachEnvelopeAxis` | Compile failed without axis-aware result and drag band | `34648b7` |
| `Section_ProvisionalAndProjectionRows` | Governing Cp_min row lacked the provisional flag | `a3f7781` |
| `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` | Wing-only row absent beside craft total | `d7f3fc6` |
| `DragBand_NcritValueOrderAndWingRatio` | Reversed Ncrit input rendered `30.4–20.4` | `a7ef960` |
| `PolarSample_LowConfidence_DerivedEqualsWriteTime` | Planted read-time `false` flag yielded false against true write-time advisory; restored policy then passed | `47208e2` |
| `PolarReRange_ShowsValidatedBounds` | Printed `5.432E+5`, the strip's Re, instead of the validated range | `3bd8ea0` |
| `AnalysisPanel_Tabs_BoundToSelectedRun` (numeric tier note) | A planted empty table note rendered Cl without its tier label; the real row rendering restored and passed | repair close commit |

The reviewer's 6.9–7.7 % measurement was documentation evidence supplied by the operator; no red runtime test was claimed for that measurement. The Re-outside read-time case passed on first run as a regression guard, so it is not claimed red.
