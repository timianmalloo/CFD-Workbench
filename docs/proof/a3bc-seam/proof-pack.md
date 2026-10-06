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
| 4. Ruling 90 check | A 200-vs-400 governing-station suction delta is measured once per product run and emitted as `analysis.run.PanelUnderreadFraction`. Above 10%, the governing station's Cp_min, Cavitation and V_crit rows carry `ANA-PANEL-UNDERREAD`; no neighboring strip is marked. A legacy strip flag remains a note beside its unchanged envelope verdict | `Polar_ProductRun_ReachesStripsAndSectionProjection` checks the event; `Section_ProvisionalAndProjectionRows` checks the rows and legacy verdict |
| 5. Polar source | `IPolarSource.Sample` returns a non-persisted `PolarResult`. Stored samples derive flags through `BracketFlags(Naca0012Reference.Matches(CstFit.Fit(section)), α, Re, Ncrit)` and the stored confidence threshold | `PolarSample_DerivedFlags_EqualEvaluateAtWriteTime`, `Section_EditedProfileNoPolar_Unavailable` |
| 6. Strip numerics | Product method uses the run source's profile at each strip and retrieves Cd at both Ncrit with Re_local and α_eff; weight hash prefix and size enter `RunSettings.Polar` and the key | `F13b_WaterWithPolar_RetrievesAtBothNewRe`, `RunKey_PolarWeightsHashAndSize_ChangeKey` |
| 7. Drag | `Loads.WingDrag` is induced plus profile for the wing only. Wing-only drag and CL/CD bands appear beside the unavailable craft Total drag code, ordered by value. Low polar confidence remains advisory on strip Cd, profile drag and wing drag. The estimator bound is not substituted for missing polar Cd | `Loads_TotalDrag_InducedPlusProfileOrNamesMissing`, `DragBand_NcritValueOrderAndWingRatio`, `Polar_LowConfidence_AdvisoryReachesDragSums` |
| 8. Searches and free surface | Bracketed Find α/take-off return value, basis, bounds, iterations and termination code; the 1% target is from spec ANA-05/12. A5.2 is a separate depth correction within source [S6]'s Re, Fr_h, α and h/c envelope and carries an Ncrit 2/4 drag band plus omission codes | `FindAlpha_TargetCL_RootWithinTolerance`, `FindAlpha_NoRoot_UnavailableWithReason`, `FindAlpha_ZeroLift_BracketedRoot`, `FindTakeoff_RetrievesLoadAtEachSpeed`, `FreeSurface_A52_FactorsBesideDeepWater`, `FreeSurface_A52_EachEnvelopeAxis` |
| 9. Tip rule | Polar-slope and zero-lift define the measured `C/C_root < 0.9` edge; the tip retains its primary not-judged code, even at high confidence or low Re; per-strip polar/lattice Cl delta is available | `Tip_ConsistencyEdge_RecomputedWithPolarSlope`, `Tip_LowRe_TipReasonPrecedesReUnavailable`, `Tip_ConfidenceNeverClearsNotJudged` |
| 10. Provenance | Revision ordinal, surface/profile/evaluator manifest, water record/source and the controller's exact Historical banner reach projection | `Provenance_RunInputsWaterAndChangedSince` |

## Measurements and limits

Repair cycle 1 measured the complete 129-station NACA 2412 (2 % camber) section tier, including the governing
400-panel check, at **430.265 ms warm** on macOS arm64. `Section_CamberedWing129_WarmTime` exercises panel
zero-lift solves at every station. `Section_CamberedPreparedPanel_ReusesMatrix` compares four angles from one
factorization against fresh solves. The panel matrix is factorized once per section estimate and its right side
is reused for operating α and α_L0. The section estimator no longer rejects an imported cambered foil solely
because its leading-edge camber derivative is singular; the panel solve uses camber coordinates, not that derivative.
The CFD review measured **6.9–7.7 %** Cp_min under-read at 200 panels for 6 %-thick sections at α 3–6°.
At the 7.7 % endpoint, about **7.3 %** of the 15 % screening margin remains. The earlier 3.71 % was
the worst of a narrower tested-foil set, not a general bound.

## Repair cycle 1 review evidence

| Condition | Oracle and observation | Confidence / limit |
|---|---|---|
| 1. Tier labels | `Section_ProvisionalAndProjectionRows` reads every numeric section row, the panel-derived V_crit and both polar rows; a missing note failed red. `AnalysisPanel_Tabs_BoundToSelectedRun` walks the actual Section table cells and checks each numeric note; a planted dropped-note render failed red. Section drag is `ANA-SECTION-ITTC1957-BOUND` rather than Cd. COPY-66 is the surrogate note. | Verified through the rendered panel. The new bound code needs operator copy. |
| 2. Advisory confidence | `Polar_LowConfidence_AdvisoryReachesDragSums` exercises a low-confidence polar through `StripCoupler`, `Loads` and projection; the value stays available. `Tip_ConfidenceNeverClearsNotJudged` changes confidence and checks a separate advisory. | Verified at the seam; 0.5 is a flag threshold, not an error bar. |
| 3. Cambered timing | `Section_CamberedWing129_WarmTime` measured 430.265 ms warm at 129 NACA 2412 stations with the 400-panel check. `Section_CamberedPreparedPanel_ReusesMatrix` compares four reused and fresh solves. | Verified on macOS arm64; Windows timing unmeasured. |
| 4 and 6. Governing station | `Section_ProvisionalAndProjectionRows` checks the governing Cp_min/Cavitation rows and V_crit carry the code above 10%; a legacy strip's outside verdict survives with the code in its note. | Verified for the 11% fixture; no adjacent strip receives a new panel flag. |
| 5. Free surface | `FreeSurface_A52_EachEnvelopeAxis` checks both Re endpoints, Fr_h, both α endpoints and both h/c endpoints; `FreeSurface_A52_FactorsBesideDeepWater` checks the Ncrit drag band and omission codes. | Verified against [S6](../../knowledge/hydrofoil-workbench/data-and-constants.md); out-of-envelope values remain unavailable. |
| 7 and 8. Drag display | `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` checks wing subtotal adjacent to unavailable craft total; `DragBand_NcritValueOrderAndWingRatio` reverses the Ncrit order and checks drag and ratio bands. | Verified in projection; craft drag remains unavailable. |
| 9. Derived polar flags | `PolarSample_ReOutside_DerivedEqualsWriteTime` and `PolarSample_LowConfidence_DerivedEqualsWriteTime` compare read derivation to write-time policies; a planted false low-confidence read failed red. | Verified; no PolarSample schema change. |
| 10. Search and Re range | `FindAlpha_TargetCL_RootWithinTolerance` checks the spec's 1% rule; `PolarReRange_ShowsValidatedBounds` failed red on strip Re, then reads 2×10⁵–10⁶. | Verified against spec ANA-05 and the polar spike bracket. |
| 11. Cp under-read wording | Design §5.1 and this proof state the reviewer's 6.9–7.7%, 7.3% residual and the tested-foil limit of 3.71%. | Reviewer measurement reported by the operator; not independently remeasured in this repair. |

The Analysis harness measured a warm default-station whole-wing 200-panel section pass of **449.814 ms** on macOS arm64 in `.tmp-tests/analysis-inner-final.log`; it includes the 400-panel governing check. `Settings.Default` places 129 stations (root, centres and edges for 64 strips per half). The same run measured a **1.335%** governing-station suction under-read at η **0.098**. These numbers describe that fixture and run, not a universal error bound. The Kármán–Trefftz 200-panel Cp_min relative error is recorded by its own fixture and is not used as the per-run delta. The product service emits its own measured delta in the normal `analysis.run` event.

The first full concurrent ring measured the default 129-station section pass at **505.307 ms**, below the 1 s whole-wing target, but its encompassing check cost **515.063 ms** exceeded C-5's 500 ms per-check limit. The check now uses a complete 97-station wing (48 strips per half); the targeted repair run measured **360.960 ms** for the section pass and **414.875 ms** for the check. The product default is still 129 stations. The per-check cost rule also moved `TipStrip_ExampleFoil_OutermostProvisional` to the existing Analysis readiness harness after a measured 1,285 ms ring-0 cost; it passes under `--readiness`. The 54 DX states and remaining data gaps are enumerated in [state-coverage.md](state-coverage.md). The tests' red evidence is in [red-first.md](red-first.md).

The initial SEAM ring (`tools/run-tests.sh`, `.tmp-tests/Analysis.part1of2.log` and `part2of2.log`) passed all suites in **56 s**: Analysis parts **3.747 s** and **3.920 s**, 57 and 118 PASS respectively; the 97-station section check cost **447.845 ms** and measured **436.988 ms** for the whole-wing tier, with a **1.335%** governing delta. `check-test-costs.py` reported zero failures and one `COST-MISS C-3` because host load rose to 26.34; it did not claim a quiet-run performance measurement. `check-docs.py` passed; all 12 verify gates passed. The first ring's C-5 failure consumed one repair cycle.

Repair cycle 1 ran one full `tools/run-tests.sh` after the eleven changes: **56 s wall,
60 + 122 Analysis PASS, 710 Desktop PASS, 700 Core PASS, 6 CLI PASS; zero failures**.
The two cost checks C-3 and C-4 were `COST-MISS` at recorded end load **27.96**, so the
ring makes no quiet-load cost claim. `python3 tools/check-docs.py` passed; it reported
131 pre-existing review suggestions but zero graph problems or stale entries.
`run-verify-gates.py` reported **12/12 passed**. The single full ring did not run the
readiness-only 129-station cambered timing check; its focused readiness result is above.

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

## Repair codes awaiting operator copy

COPY-66 is approved for the surrogate note, and `PanelMethod.ModelLabel` is the existing panel note.
The following newly exposed codes need an operator wording decision before DX substitutes prose:

| Surface | Codes |
|---|---|
| Section estimator and resolution | `ANA-SECTION-ITTC1957-BOUND`, `ANA-PANEL-UNDERREAD` |
| Polar confidence and validated range | `ANA-POLAR-LOW-CONFIDENCE`, `ANA-POLAR-RE-OUTSIDE` |
| Wing-only subtotal and ratio | `ANA-WING-ONLY-DRAG`, `ANA-WING-ONLY-RATIO`, `ANA-WING-RATIO-UNAVAILABLE` |
| Craft total | `ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY` |
| A5.2 envelope | `ANA-FREE-SURFACE-DEPTH-OUTSIDE`, `ANA-FREE-SURFACE-RE-OUTSIDE`, `ANA-FREE-SURFACE-FROUDE-OUTSIDE`, `ANA-FREE-SURFACE-ALPHA-OUTSIDE`, `ANA-FREE-SURFACE-SURFACE-PIERCING` |
| A5.2 model omissions | `ANA-FREE-SURFACE-DEPTH-ONLY`, `ANA-FREE-SURFACE-WAVE-DRAG-OMITTED`, `ANA-FREE-SURFACE-FROUDE-NOT-MODELLED` |

The existing `ANA-CAV-*` reason codes in the preceding table also await copy where no approved COPY row is cited.
