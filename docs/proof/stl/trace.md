---
id: proof-stl-trace
title: "STL trace — how an edited conditions band reaches Current/Historical"
type: proof-pack
status: active
owner: "@trk-stl"
phase: implementation
tags: [stl, ruling-140, analysis, freshness, conditions-band]
links:
  - { to: design-area3-analysis, rel: depends-on }
review-by: "2026-11-07"
summary: >-
  Trace for Ruling 140: before the fix the band's live inputs reached the controller only at Evaluate, so an edit left the
  result Current; one projection (AnalysisView) feeds every surface, so one controller input fixes all of them.
---

# STL trace (Ruling 140)

Observed by reading the code at `main` ba900875 (all line numbers are before this track's edits).

## 1. How the band's inputs reached the controller (the defect)

- `ConditionsBand.axaml.cs` constructor: the four inputs only called `RefreshDerived()` (the derived q, Re, h/c cells).
  Nothing told the controller. The only outbound path was `Activate()` -> `EvaluateRequested(op, water)`, wired in
  `ModelArea.axaml.cs:64` to `controller.EvaluateAnalysisAsync`. `OpenFindAlpha` -> `ApplyFoundAlpha` also writes the pending α.
- `WorkbenchController.cs:395-401` `SetAnalysisConditions` is the one writer of `analysisOp` / `analysisWater`; it was called
  only by Evaluate, Apply of Find α, and tests. So between an edit and Evaluate, `analysisOp` still equalled the run's point.

## 2. How Current/Historical is decided

- `Freshness.State` / the projection: key equality. `AnalysisView` (`WorkbenchController.cs:~304`) builds
  `Freshness.Current(snapshot, analysisWater, analysisOp, method, settings)` and `AnalysisProjection.Build` compares
  `RunRecord.RecomputedKey(run)` with `Freshness.CurrentKey(current)` (`AnalysisProjection.cs:54,170`). Equal -> Current,
  else Historical with banner `HistoricalText ?? "Historical — " + WhatChanged`.
- The controller supplies `HistoricalText` from `HistoricalBanner` (geometry: "geometry changed (r.. -> r..)"; α:
  "operating point changed (α a° -> b°)"; anything else fell through to raw key-field names such as "op.speed").
- The cache key of the projection includes `CurrentKey`, so a changed pending point re-projects by itself; `SetAnalysisConditions`
  also nulls the key and calls `Notify()`.
- Nothing stores a freshness flag and the run key is never edited (decision-freshness-by-run-key): the fix changes only the
  pending point, never a run.

## 3. Every surface that shows the state (all read `controller.AnalysisView`; no second source)

| Surface | Reads | Code |
|---|---|---|
| Status strip "Analysis: Current/Historical" | `AnalysisState` -> `AnalysisView.State` | `ModelArea.axaml.cs:436` -> `StatusStrip.ShowAnalysisState` |
| Properties tier chip and rows | `AnalysisView` (Tier row, `Labels.HistoricalChip`) | `PropertiesPane.axaml.cs:288` |
| Model-area Historical banner | `AnalysisView.Banner` | `ModelArea.axaml.cs:433-434` |
| Bottom panel banner and Provenance "Changed since" | `AnalysisView` | `AnalysisPanel.axaml.cs:98` |
| Section document header / pane | `AnalysisView` through `SectionPaneInputs` | `ShellHost.cs:645` |
| Plan, 3D, Elevation layers | `AnalysisView.Layers` after `LayersChanged` | `PlanCanvas.cs:89`, `View3d.cs:196`, `ElevationView.cs:194` |
| Properties Conditions summary | `AnalysisOperatingPoint` (now follows the live band) | `PropertiesPane.axaml.cs:292` |

Because one projection feeds all of them, they agree whenever the controller's pending point is right. The test
`StaleConditions_SpeedEdit_...` asserts projection state, strip text, Properties chip, and model-area banner together.
No Section or Properties view was edited.

## 4. The change (surface list)

1. `ConditionsBand.axaml.cs`: each input change calls `Edited()`, which builds the point (a `ContractError` leaves it null),
   hands it to the controller found through the `ShellHost` ancestor (the route `OpenFindAlpha` already uses), then refreshes derived cells.
2. `WorkbenchController.SetPendingConditions(op?, water)`: a valid point goes through `SetAnalysisConditions`; null sets
   `pendingUnreadable`, and `ComparedOp` then carries a datum no run holds, so the key differs and the result reads Historical.
   `SetAnalysisConditions` clears the flag.
3. `HistoricalBanner`: edited op-or-water inputs read "Historical — operating point changed (...)": α keeps its approved arrow
   form; speed, depth, water are named; an unreadable input reads "Historical — operating point changed".
4. Tests: `StaleConditionsTests.cs` (two checks), registered in `--analysis`.

Evaluate and Find α stay explicit: neither is triggered by an edit; no run is recorded by an edit.
