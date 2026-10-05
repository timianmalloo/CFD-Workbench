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
| `Toggle_RoundTrip_CameraSelectionStationViewportEqual` | `0da6ef5` | entering Analysis refits the Plan camera or changes selection/layout | this step |
| `Toggle_NeverEvaluates` | `0da6ef5` | entering Analysis starts a run without Evaluate | this step |
| `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | pending commit | Analysis draws the point draft or discards it | pending |
| `Toggle_PreviewOpen_LayersOverAcceptedRevision` | pending commit | layers use draft geometry as their base | pending |
| `Toggle_SectionDraftOpen_EditorRestoredOnReturn` | pending commit | returning from Analysis lands in Workspace or loses the editor | pending |
| `Analysis_EditVerb_RefusedWithInertMessage` | pending commit | a Points-pane gesture or direct dimension edit changes geometry in Analysis | pending |

Both checks printed `FAIL` under `CFD_TEST_ONLY=Toggle_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before implementation. The missing toggle was the observed red condition.

The four added checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen,Toggle_SectionDraftOpen,Analysis_EditVerb_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before the accepted-geometry and refusal changes.
