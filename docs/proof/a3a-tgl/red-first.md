---
id: proof-a3a-tgl-red-first
title: A3a TGL red-first receipt
type: proof
status: draft
owner: trk-tgl
phase: A3a
tags: [analysis, desktop, tdd]
links:
  - type: depends-on
    target: design-area3-analysis
review-by: 2026-11-05
summary: Records the failing TGL named checks before each implementation step and the mutant each check must catch.
---

# TGL red-first receipt

| Check | Red commit | Mutant caught | Green commit |
|---|---|---|---|
| `Toggle_RoundTrip_CameraSelectionStationViewportEqual` | pending commit | entering Analysis refits the Plan camera or changes selection/layout | pending |
| `Toggle_NeverEvaluates` | pending commit | entering Analysis starts a run without Evaluate | pending |

Both checks printed `FAIL` under `CFD_TEST_ONLY=Toggle_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before implementation. The missing toggle was the observed red condition.
