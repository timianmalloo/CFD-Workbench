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
| `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | `065e5f8` | Analysis draws the point draft or discards it | this step |
| `Toggle_PreviewOpen_LayersOverAcceptedRevision` | `065e5f8` | layers use draft geometry as their base | this step |
| `Toggle_SectionDraftOpen_EditorRestoredOnReturn` | `065e5f8` | returning from Analysis lands in Workspace or loses the editor | this step |
| `Analysis_EditVerb_RefusedWithInertMessage` | `065e5f8` | a Points-pane gesture or direct dimension edit changes geometry in Analysis | this step |
| `ConditionsBand_Running_EvaluateBecomesCancelAnnounced` | pending commit | the action stays Evaluate while a run is in flight | pending |
| `ConditionsBand_At1024_MoreHoldsDerivedExceptQAndSigma` | pending commit | the derived group clips at 1024 px | pending |
| `StatusStrip_AnalysisItem_FollowsRunState` | pending commit | Historical is displayed as Current | pending |
| `Toggle_HistoricalRun_BannerInBothModes` | pending commit | Historical banner draws only in Analysis | pending |
| `Telemetry_AnalysisProject_FreshnessOnRebuild` | pending commit | the Historical projection rebuild omits its event | pending |

Both checks printed `FAIL` under `CFD_TEST_ONLY=Toggle_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before implementation. The missing toggle was the observed red condition.

The four added checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen,Toggle_SectionDraftOpen,Analysis_EditVerb_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before the accepted-geometry and refusal changes.

The three band and status checks each printed `FAIL` under `CFD_TEST_ONLY=ConditionsBand_,StatusStrip_AnalysisItem_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before those controls existed.

The Historical and project-event checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_HistoricalRun_,Telemetry_AnalysisProject_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before a selected-run projection existed. Their final assertions will exercise a Historical run in both modes.
