---
id: proof-cpy-red-first
title: "Track CPY, round-oct06 — red-first receipts and captures"
type: proof-pack
status: accepted
owner: "@trk-cpy"
phase: implementation
tags: [proof, copy, layout, analysis, ruling-101, ruling-109]
links:
  - {to: proof-doc-oct06, rel: depends-on}
  - {to: rulings, rel: depends-on}
review-by: 2027-04-01
summary: >-
  For each Ruling 101 and 109 item, the check that failed on the old code and the run that passed after the change, plus the capture list at 1500x870 and 1280x800.
---

# Track CPY: red-first receipts

Commands: Analysis checks `CFD_TEST_ONLY=<names> dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`;
Desktop checks `CFD_TEST_ONLY=<names> dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --analysis`.
"Red" = the old behaviour: the projection (or the Desktop file named) taken back to its previous state with the new check in place.
Where a check needs a new symbol (a constant, `Units`, `HeightFor`) the old code does not compile; that row says "no symbol".

| # | Item | Check | Red (old code) | Green |
|---|---|---|---|---|
| 1 | R101 3a shallow station | `Depth_ShallowAtAnyStation_FreeSurfaceNotModelled` | FAIL COPY-224 expected "…free surface not modelled"; actual "…deep water" | PASS |
| 2 | R101 3b bare Unavailable | `Projection_NoBareUnavailable_EveryStateCarriesItsReason` (8 projection states, every row value, note and strip row) | FAIL default: Unavailable expected False; actual True | PASS |
| 3 | R101 3b one total-drag string | `Projection_TotalDrag_OneReasonString` | FAIL craft CL/CD expected "Unavailable — missing: profile (no polar method installed), junction, mast, wave, spray"; actual "Unavailable — total drag missing" | PASS |
| 4 | R109 Drag (Wing only) | `Projection_DragWingOnly_OneRow_ImperialLbf`, `Loads_PolarUnavailable_WingDragNamesProfile`, `Loads_MissingTerm_UnavailableNeverZero`, `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` | FAIL "Wing result one row expected 1; actual 0"; "Sequence contains no matching element" (no `Drag (Wing only)` row); "Ruling 109: one Drag (Wing only) row, no separate Total drag row" | PASS |
| 5a | R101 3c tampered note COPY-274 | `Projection_TamperedRun_NoteIsCopy274UnderCopy211`; Desktop `Tampered_RunView_UnavailableNoLayersRowKept` | FAIL COPY-274 expected the note; actual null | PASS |
| 5b | R101 3c Historical chip COPY-279 | `Projection_HistoricalResult_TierChipReadsHistoricalVlmStrip` | FAIL COPY-279 expected "Historical · VLM + strip"; actual "VLM + strip · local calculation" | PASS |
| 5c | R101 3c chart labels COPY-275..278 | `LoadingChart_Axes_TicksTitlesSeriesLabel_OnlyApprovedCopy`, `Labels_ChartAndChipConstants_MatchTheirRows` | no symbol (`Labels.ChartXTitle` and the others) | PASS |
| 5d | R101 3c Conditions summary COPY-280 | `Labels_ConditionsSummary_Copy280Form_FollowsUnits`; Desktop `Analysis_Units_…` | no symbol (`Labels.ConditionsSummary`) | PASS |
| 6 | R101 3d band speed follows Units | Desktop `Analysis_Units_BandSpeedFollowsResults_ConditionsSummaryAndHistoricalChip` | FAIL Imperial band unit: expected kn, got m/s (band not wired to the controller's units) | PASS |
| 7a | Q4 Envelope and Drag cells at the cell edge (AUX-F8) | Desktop `Analysis_Cells_NotCutAt1500x870_…` | FAIL 'Inside the method envelope (|α_eff − α_L0| ≤ 10°, … 2 Not judged — tip strip' ends at 659, the pane is 258 wide | PASS |
| 7b | Q4 band derived cells with depth unset | same check | FAIL 'σ Unavailable — depth not set' ends at 1392, the band is 1234 wide | PASS |
| 7c | Q4 x tick row while Running | same check | FAIL the plot ends at 212.5 of a 190 px panel and is 120 px tall while Running | PASS |
| 8 | R101 3d four views at 1280x800 (C + D) | Desktop `Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870` | FAIL the band is 52 px tall at 1280x800 (and the panel stayed 190, so the views were 503 x 239.5, under the floor) | PASS |

Measured by the item 8 check (floor `MinimumFourViewSize` pinned 320 x 240 in the check):

| Window | Client | Panel | Band | Each view |
|---|---|---|---|---|
| 1280x800 | 1280 x 800 | 150 | 41 | 503 x 265 (floor 320 x 240; options.md predicted 503 x 265) |
| 1500x870 | 1500 x 861 to 870 (the platform varies run to run) | 190 | 41 | 613 x 275.5 at client 861, 613 x 277.5 at 865, 613 x 280 at 870 (options.md: 613 x 275 at 860); the check expects 275 + (client − 860) / 2 within 1 px |

Layout C threshold: `AnalysisPanel.ShortWindowClientHeight = 820` (a window client under 820 px gets the 150 px panel; 800 fires, 860 does not).

## Captures

Harness: the A3a native-review harness (`docs/reviews/a3a-native.md` section 1) on this tree: a real `ShellHost` in a real Avalonia window, rendered with `RenderTargetBitmap`.

| File | State |
|---|---|
| `c1500/03-current.png`, `c1280/03-current.png` | Wing result Current, depth 0.6 m, four views |
| `c1500/02-running.png` | Running, previous result Historical; the x tick row and the axis title are inside the panel |
| `c1500/08-tampered.png` | Tampered stored run: COPY-211 with COPY-274 under it (Properties and the panel) |
| `c1500/05-historical-provenance.png` | Historical after a CAD edit; tier chip "Historical · VLM + strip" |
| `c1500/03b-current-depth-unset.png` | Depth unset: the derived cells move under More instead of running off the band |
| `c1500/09-shallow-hc-below-5.png` | h/c 2.5 |
| `c1500/10-imperial-band-kn-lbf.png`, `c1280/10-imperial-band-kn-lbf.png`, `c1500/11-metric-band-m-s-N.png` | the Units setting on the band and on the results |

## Seen in the captures, not changed

- Drag (Wing only) and Wing-only CL/CD show a reason code (`ANA-TOTAL-DRAG-MISSING-PROFILE`, `ANA-WING-RATIO-UNAVAILABLE`) where the polar is installed but a strip has no cd: their display text is COPY-334..352, awaiting the operator.
- The collapsed Conditions summary truncates at the pane edge ("… as th…"): the group-summary rule of DESIGN.md section 12.0f (the summary truncates, the name never does); its full text is the header's help text.
