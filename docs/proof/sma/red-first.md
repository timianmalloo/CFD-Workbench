---
id: proof-sma-red-first
title: SMA red-first receipts (Rulings 124, 125)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sma, red-first, ruling-124]
links:
  - { to: mockup-section-main-area, rel: depends-on }
review-by: 2026-12-31
summary: >-
  For each SMA behaviour, the check that failed on the old code and the run that passed after the change.
---

# SMA red-first receipts

Command shape: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- <mode>`.

## 1. Chip strip (A)

Check `PlanCanvas_ChipStrip_OneRowBelowPlanform_AlignedAndClear` (mode `--plan-canvas`), on a tapered planform (tip trailing edge 50 mm forward of the root's).

- Old code (per-station rows at each station's trailing edge): `FAIL ... Chips are not one strip: tops 162.6, 131.5`.
- New code (one strip 20 px under the planform bounds): `PASS`. Also asserted: no chip rectangle intersects the planform bounds or the scale bar, each chip is centred on its station line, none leaves the canvas.

## 2. Section sample retired

Control: the retired-name scan in `ModelArea_SamplesTabRetired_NoReferencesRemain` now lists `SectionSampleDocument|SectionSampleBody|SectionViewport|SectionReadout|section-sample`.

- Old code (`git grep -nE "SectionSampleDocument|SectionViewport|SectionReadout|section-sample|SectionSampleBody" 42e2526c -- src/CfdWorkbench.Desktop`): 12 hits, so the scan fails.
- New code: the same grep over the working tree prints nothing; the scan passes (full Desktop run at the end).

## 3 and 4. Section document, bottom summary

Suite `SectionMainAreaTests` (readiness): five checks. They reference `ShellLayoutFactory.SectionDocument`, `AnalysisPanel.SectionView`, `SectionTabView.Summary`, `AnalysisPanel.CompactHeight` and `Labels.OpenInMainArea`, none of which exist at 42e2526c, so on the old code the suite does not compile (red by absence, not by assertion). After the change: `PASS` for
`SectionMainArea_Tabs_PlanSectionFoilSource_SampleGone`, `SectionMainArea_Document_FullSize_HeaderNamesSelectedStrip`, `SectionMainArea_Document_NoStrip_HeaderNamesGoverningStation`, `SectionMainArea_BottomTab_OneLineSummary_OpensDocument`, `SectionMainArea_Document_MinimumWindow_ChartsDrawn`; the DX checks `Section_TabBody_BuildsChartSelector`, `SectionView_OneView_SelectorDrivesChart`, `SectionTab_Desktop_RendersAllChartsWithoutThrowing`, `FindAlpha_ButtonBesideEvaluate_ApplyWritesAlphaOnly_Dxm6`, `Section_CpUnavailable_PanelSolveFailed_ShowsCopy358InApp` (readiness) and `AnalysisPanel_Tabs_BoundToSelectedRun` (`--analysis`) still PASS, moved to the Section document.

## Repair cycle 1

- Conditions band over the Section document: `SectionMainArea_ConditionsBand_OneInstance_EvaluateFromSectionTab`. Old code (band stays in the Plan document): `FAIL ... one band instance in the window: expected 1, got 0`. New code: PASS (one `ConditionsBand`, in `SectionDocumentBody`; Evaluate with alpha 4 from the Section tab records a new run and the document is bound to it; the band returns above the Plan).
- Compact Stations table: `SectionMainArea_StationsTable_FourColumns_ShownRowHighlighted`. On the old code it does not compile (`SectionView.StationTable` does not exist); new code PASS.
