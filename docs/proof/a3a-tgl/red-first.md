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
| `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | `065e5f8`; held-release red pending | Analysis draws or discards the point draft; a held gesture release commits it while hidden | pending |
| `Toggle_PreviewOpen_LayersOverAcceptedRevision` | `065e5f8` | layers use draft geometry as their base | this step |
| `Toggle_SectionDraftOpen_EditorRestoredOnReturn` | `065e5f8` | returning from Analysis lands in Workspace or loses the editor | this step |
| `Analysis_EditVerb_RefusedWithInertMessage` | `065e5f8`; second red pending | a Points-pane gesture or direct dimension edit changes geometry in Analysis; view points lack dimming input | pending view step |
| `ConditionsBand_Running_EvaluateBecomesCancelAnnounced` | pending commit | the action stays Evaluate while a run is in flight | pending |
| `ConditionsBand_At1024_MoreHoldsDerivedExceptQAndSigma` | pending commit | the derived group clips at 1024 px | pending |
| `StatusStrip_AnalysisItem_FollowsRunState` | pending commit | Historical is displayed as Current | pending |
| `Toggle_HistoricalRun_BannerInBothModes` | pending commit | Historical banner draws only in Analysis | pending |
| `Telemetry_AnalysisProject_FreshnessOnRebuild` | pending commit | the Historical projection rebuild omits its event | pending |
| `Toggle_NavbarAndMenuReachable` | pending commit | the visible segment, menu command or Shift-Command-A shortcut is missing | pending |
| `Toggle_LayersFirstFrame_P95WithinPreviewBudget` | pending commit | the first Analysis frame has no run layers or p95 exceeds 250 ms | pending |

Both checks printed `FAIL` under `CFD_TEST_ONLY=Toggle_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before implementation. The missing toggle was the observed red condition.

The four added checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen,Toggle_SectionDraftOpen,Analysis_EditVerb_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before the accepted-geometry and refusal changes.

The three band and status checks each printed `FAIL` under `CFD_TEST_ONLY=ConditionsBand_,StatusStrip_AnalysisItem_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before those controls existed.

The Historical and project-event checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_HistoricalRun_,Telemetry_AnalysisProject_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before a selected-run projection existed. Their final assertions will exercise a Historical run in both modes.

The reachability check printed `FAIL` under `CFD_TEST_ONLY=Toggle_NavbarAndMenuReachable dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` because the navbar segment was absent.

The readiness check printed `FAIL` under `CFD_TEST_ONLY=Toggle_LayersFirstFrame_P95WithinPreviewBudget dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --readiness` because the first frame had no selected-run layers. The existing readiness suite also printed its independent drag-frame measurement; the selector made only the TGL name affect the exit.

The edit check printed a second `FAIL` under `CFD_TEST_ONLY=Analysis_EditVerb_RefusedWithInertMessage dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis`: the view layer had no Analysis dimming input.

The preview round-trip check printed a second `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen_HiddenThenRestoredUntouched dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis`: releasing an already-held point gesture in Analysis committed its hidden draft.
