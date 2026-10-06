---
id: proof-a3bc-seam
title: "A3b/A3c service and projection seam proof"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, polar, projection, numerics, ruling-100]
links:
  - { to: design-area3-analysis, rel: implements }
  - { to: design-dx-screen-states, rel: relates-to }
  - { to: proof-a3c-polar-source, rel: depends-on }
  - { to: proof-a3bc-seam-red-first, rel: relates-to }
  - { to: proof-a3bc-seam-state-coverage, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Connects the section and polar tiers to run and projection, adds A3c-2 numerical values, and preserves
  Ruling 100's persisted PolarSample grain by deriving validity metadata on read.
review-suggested: []
---

# Proof Pack: A3b/A3c service and projection seam

**Contract:** Rulings 63, 85, 86, 88 D8/D14, 90, 91, 93 and 100; `area3-analysis.md` §5.1, §5.4–5.6, §6, §13.3 and §16. **Tier:** T2. **Branch:** `feature/a3bc-seam`. **No new UI view or pane and no proposed DX prose was added.**

## Domain and storage decision

The accepted foil revision is the source identity. A run references that revision by `RunInputs.AcceptedId` and manifest hashes. The section tier and polar validity are derived read models from the run's own source. `PolarSample` keeps its existing grain: one computed point at one profile, Re, Ncrit, surface state and α. Its persisted shape is unchanged. `PolarResult` holds the derived bracket reasons, low-confidence flag and CST residuals. `RunPolarResolver.SectionAt` resolves via `AuthoringSession.AcceptedSourceOf`, and `SectionTier.Derive` checks the source manifest before projecting. An edited NACA 0012 no longer receives the validated catalog-family bracket. The optional `StripLoad.ProvisionalReason` accepts `ANA-PANEL-UNDERREAD`; old rows without the field still read. `AnalysisEvent.PanelUnderreadFraction` is event-only and is not an `AnalysisRun` or `PolarSample` field.

## Surface path and checks

| Step | Data path | Evidence |
|---|---|
| 1. Panel identity | 200 cosine panels at each section station; three TE panels excluded per side; `MethodRecord` version `panel200-te3` enters the run key | `PanelCp_DefaultResolution_CpMinWithin4Percent`, `RunKey_PanelCountAndTeExclusion_ChangeKey`; Kármán–Trefftz 200-panel error is fixture-only |
| 2. Section into run | Service computes `SectionTier.Evaluate` from the accepted source and strip α_eff/Re at every station, including Cl, Cm, α_L0, turbulent drag bound, Cp and lift per span | `Section_WingRun_PanelValuesAtEveryStation`, `Section_ProvisionalAndProjectionRows` |
| 3. Cavitation | Local depth and σ/−Cp_min choose the governing station; unavailable/undefined states are reason codes; a surface-piercing station suppresses estimator rows | `Cavitation_GoverningStation_AtAlphaEffAndLocalDepth`, `Cavitation_GoverningStation_MinLocalRatio`, `Section_SurfacePiercing_EstimatorUnavailable` |
| 4. Ruling 90 check | A 200-vs-400 governing-station suction delta is measured once per product run, emitted as `analysis.run.PanelUnderreadFraction`, and marks the nearest judged strip provisional above 10% | `Polar_ProductRun_ReachesStripsAndSectionProjection` checks the event; `Section_ProvisionalAndProjectionRows` checks the code |
| 5. Polar source | `IPolarSource.Sample` returns a non-persisted `PolarResult`. Stored samples derive flags through `BracketFlags(Naca0012Reference.Matches(CstFit.Fit(section)), α, Re, Ncrit)` and the stored confidence threshold | `PolarSample_DerivedFlags_EqualEvaluateAtWriteTime`, `Section_EditedProfileNoPolar_Unavailable` |
| 6. Strip numerics | Product method uses the run source's profile at each strip and retrieves Cd at both Ncrit with Re_local and α_eff; weight hash prefix and size enter `RunSettings.Polar` and the key | `F13b_WaterWithPolar_RetrievesAtBothNewRe`, `RunKey_PolarWeightsHashAndSize_ChangeKey` |
| 7. Drag | `Loads.WingDrag` is induced plus profile for the wing only. Craft Total drag remains unavailable with named missing components. The estimator bound is not substituted for missing polar Cd | `Loads_TotalDrag_InducedPlusProfileOrNamesMissing`, `Polar_ProductRun_ReachesStripsAndSectionProjection` |
| 8. Searches and free surface | Bracketed Find α/take-off return value, basis, bounds, iterations and termination code; A5.2 is a separate depth correction with source, model-scale Re range and measured Froude finding | `FindAlpha_TargetCL_RootWithinTolerance`, `FindAlpha_NoRoot_UnavailableWithReason`, `FindAlpha_ZeroLift_BracketedRoot`, `FindTakeoff_RetrievesLoadAtEachSpeed`, `FreeSurface_A52_FactorsBesideDeepWater` |
| 9. Tip rule | Polar-slope and zero-lift define the measured `C/C_root < 0.9` edge; the tip retains its primary not-judged code, even at high confidence or low Re; per-strip polar/lattice Cl delta is available | `Tip_ConsistencyEdge_RecomputedWithPolarSlope`, `Tip_LowRe_TipReasonPrecedesReUnavailable`, `Tip_ConfidenceNeverClearsNotJudged` |
| 10. Provenance | Revision ordinal, surface/profile/evaluator manifest, water record/source and the controller's exact Historical banner reach projection | `Provenance_RunInputsWaterAndChangedSince` |

## Measurements and limits

The Analysis harness measured a warm default-station whole-wing 200-panel section pass of **449.814 ms** on macOS arm64 in `.tmp-tests/analysis-inner-final.log`; it includes the 400-panel governing check. `Settings.Default` places 129 stations (root, centres and edges for 64 strips per half). The same run measured a **1.335%** governing-station suction under-read at η **0.098**. These numbers describe that fixture and run, not a universal error bound. The Kármán–Trefftz 200-panel Cp_min relative error is recorded by its own fixture and is not used as the per-run delta. The product service emits its own measured delta in the normal `analysis.run` event.

The first full concurrent ring measured the default 129-station section pass at **505.307 ms**, below the 1 s whole-wing target, but its encompassing check cost **515.063 ms** exceeded C-5's 500 ms per-check limit. The check now uses a complete 97-station wing (48 strips per half); the targeted repair run measured **360.960 ms** for the section pass and **414.875 ms** for the check. The product default is still 129 stations. The per-check cost rule also moved `TipStrip_ExampleFoil_OutermostProvisional` to the existing Analysis readiness harness after a measured 1,285 ms ring-0 cost; it passes under `--readiness`. The 54 DX states and remaining data gaps are enumerated in [state-coverage.md](state-coverage.md). The tests' red evidence is in [red-first.md](red-first.md).

## Residual risks and explicit omissions

- Windows numerical behavior and latency were not measured in this macOS track.
- The critical-section stall margin is omitted. `tip-handling.md` §4.4 explicitly marks its method unsourced; no `Cl_max` or stall verdict was invented.
- The 42 proposed strings in `dx-screen-states.md` await the operator. This branch supplies numeric fields, existing approved text and stable codes. Cavitation's former free-form reasons now use `ANA-CAV-*` codes.
- A full polar α sweep, tripped-surface polar, transition overlay curve, cavitation bucket curve and complete Find α dialog reason taxonomy are not projected. The per-state boundary is recorded in `state-coverage.md` for DX and future slices.
- `docs/design/dx-screen-states.md` still describes the pre-SEAM 400-panel implementation in its “Findings from reading the code” section. It is owned by DX and should be refreshed when their screen contract is revised.

## Cavitation copy awaiting the operator

The five former unapproved `Cavitation.cs` reason sentences have these stable codes. DX can map them after copy approval; this branch does not ship replacement prose.

| Code | Former unapproved sentence / state |
|---|---|
| `ANA-CAV-PV-MISSING` | Vapour pressure missing |
| `ANA-CAV-WATER-INVALID` | Water invalid |
| `ANA-CAV-DEPTH-INVALID` | Local depth invalid |
| `ANA-CAV-SURFACE-PIERCING` | Local station surface piercing |
| `ANA-CAV-PRESSURE-NONPOSITIVE` | Static pressure not above vapour pressure |

`ANA-CAV-DEPTH-NOT-SET` uses approved COPY-45 where available. `ANA-CAV-NO-SUCTION` has the approved COPY-69 state; its cause remains a code until approved.
