---
id: proof-a3bc-seam-state-coverage
title: "A3b/A3c projection data against DX's 54 states"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: A3c
tags: [analysis, section, polar, projection, screen-states]
links:
  - { to: design-dx-screen-states, rel: relates-to }
  - { to: proof-a3bc-seam, rel: relates-to }
review-by: 2026-11-30
summary: >-
  Checks each of the 54 DX screen states against the non-persisted data made available by Track SEAM.
  Visual rendering and the 42 proposed strings remain with DX and the operator.
review-suggested: []
---

# A3b/A3c data coverage for DX

**Basis:** `docs/design/dx-screen-states.md` rows 1–54. “Yes” means the service or projection now provides the values or a stable code. “Partial” names the missing data. “No” names a state this seam does not produce. These are data results, not claims that DX renders the state. The operator has not approved the 42 proposed strings.

| # | Data | Evidence or gap |
|---|---|---|
| 1 | Yes | `SectionTierResult.Stations` carries panel curves. |
| 2 | Yes | `PanelResult.Upper/Lower`, `CpMin`, `StationCount`; method version carries TE exclusion. |
| 3 | Partial | `PanelMethod.ModelLabel` and estimator method exist; composed copy waits. |
| 4 | Yes | Section tier is typed separately from VLM. |
| 5 | Yes | Upper/lower profile Cp samples and accepted source are available; DX owns the view. |
| 6 | Yes | Section Cl, Cm, α_L0 and `LiftPerSpan`. |
| 7 | Yes | `CdTurbulentBound` stays separate from polar Cd. |
| 8 | Yes | Cp coordinates, side, N=200, TE3 method identity. |
| 9 | No | The pre-A3b stub is obsolete on a successful section feed. |
| 10 | Partial | A failed section feed falls back to Unavailable; the specific panel failure code is not carried to the view. |
| 11 | Yes | `CavitationResult`: σ, Cp_min, V_crit. |
| 12 | Yes | Approved screen string and actual station count. |
| 13 | Yes | Margin fraction and source label. |
| 14 | Yes | `CavitationState.Clear`. |
| 15 | Yes | `CavitationState.InsideMargin`. |
| 16 | Yes | `CavitationState.PossibleAboveCritical`. |
| 17 | Yes | Governing η and `GoverningDepth` from local ratio. |
| 18 | Yes | `ANA-CAV-DEPTH-NOT-SET` and approved depth text. |
| 19 | Yes | `ANA-CAV-PV-MISSING`. |
| 20 | Yes | `ANA-CAV-SURFACE-PIERCING`; section values suppressed. |
| 21 | Yes | `ANA-CAV-WATER-INVALID`, `ANA-CAV-DEPTH-INVALID`. |
| 22 | Yes | `ANA-CAV-NO-SUCTION`. |
| 23 | Yes | `ANA-CAV-PRESSURE-NONPOSITIVE`. |
| 24 | Yes | `analysis.run.PanelUnderreadFraction`, measured at governing station. |
| 25 | Yes | `ANA-PANEL-UNDERREAD` on the governing judged strip above 10%. |
| 26 | Yes | Existing approved `Labels.NoPolar`. |
| 27 | Partial | Both Ncrit samples at operating α are available; a sweep of α for chart curves is not projected. |
| 28 | Yes | `RunSettings.Polar` identifies the tier. |
| 29 | Partial | Surrogate method identity and evidence limits are data; operator copy waits. |
| 30 | Yes | NeuralFoil id, version, weight hash prefix and byte count reach run settings/key and Provenance. |
| 31 | Yes | Nullable `PolarSample.Confidence`. |
| 32 | Yes | Null confidence is distinguishable from zero. |
| 33 | Yes | Derived `PolarResult.LowConfidence`; no stored flag. |
| 34 | Yes | Derived CST RMS and max residuals from the run section. |
| 35 | Yes | Empty `OutsideBracketReasons` identifies the validated bracket. |
| 36 | Yes | Derived outside-bracket axes and stable availability codes. |
| 37 | Yes | Network/CST `ContractError` codes remain Unavailable. |
| 38 | Yes | Per-strip Re and supported Cd identify the inside case. |
| 39 | Yes | `ANA-POLAR-RE-OUTSIDE`; profile Cd is not extrapolated. |
| 40 | Yes | Existing no-polar label. |
| 41 | Yes | `PolarConsistencyStrip.ClDelta`, measured edge and exemption code. |
| 42 | Partial | VLM verdict and polar availability are separate data; an explicit per-strip “inside bracket” result is not projected. |
| 43 | No | Tripped-surface polar is not computed in this slice. |
| 44 | Partial | Two x_tr surfaces at the operating point are present; an α sweep/overlay series is not projected. |
| 45 | Partial | Current σ and V_crit are present; a cavitation bucket over α is not projected. |
| 46 | Yes | Per-strip Cd and wing profile-drag band at Ncrit 2/4. |
| 47 | Yes | Estimator turbulent bound remains a separate field. |
| 48 | Partial | Missing Cd carries a reason; the count of missing strips is not projected. The estimator bound is not substituted. |
| 49 | Yes | `WingDragNcrit2/4` is the wing-only induced plus profile subtotal. |
| 50 | Yes | Craft Total drag uses `ANA-TOTAL-DRAG-MISSING-JUNCTION-MAST-WAVE-SPRAY`. |
| 51 | Yes | Existing approved missing-profile string for a run without a polar. |
| 52 | No | Craft total CD is unavailable while non-wing components lack a source; no denominator is claimed. |
| 53 | Partial | `SearchResult` carries bracket, iterations, termination, basis and root; polar limit and CL_max are not returned by the search. |
| 54 | Partial | No-sign-change and upstream source reasons are returned; the full five-reason DX taxonomy is not assembled. |

Residual data gaps above are DX inputs or future design decisions. They are not filled with guessed text or persisted columns.
