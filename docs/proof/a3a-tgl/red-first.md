---
id: proof-a3a-tgl-red-first
title: A3a TGL red-first receipt
type: proof-pack
status: active
owner: "@track-a3a-tgl"
phase: implementation
tags: [analysis, desktop, tdd]
links:
  - { to: design-area3-analysis, rel: depends-on }
review-by: "2026-11-05"
summary: Records the failing TGL named checks before each implementation step and the mutant each check must catch.
---

# TGL red-first receipt

| Check | Red commit | Mutant caught | Green commit |
|---|---|---|---|
| `Toggle_RoundTrip_CameraSelectionStationViewportEqual` | `0da6ef5` | entering Analysis refits the Plan camera or changes selection/layout | `1816502`; camera mutant red, `27ef32a` restored |
| `Toggle_NeverEvaluates` | `0da6ef5` | entering Analysis starts a run without Evaluate | `1816502` |
| `Toggle_PreviewOpen_HiddenThenRestoredUntouched` | `065e5f8`; held-release `da759d4` | Analysis draws or discards the point draft; a held gesture release commits it while hidden | `9290b30`; `27ef32a` |
| `Toggle_PreviewOpen_LayersOverAcceptedRevision` | `065e5f8` | layers use draft geometry as their base | `9290b30` |
| `Toggle_SectionDraftOpen_EditorRestoredOnReturn` | `065e5f8` | returning from Analysis lands in Workspace or loses the editor | `9290b30`; rendered proof `27ef32a` |
| `Analysis_EditVerb_RefusedWithInertMessage` | `065e5f8`; dimming input `b1de549` | a Points-pane gesture or direct dimension edit changes geometry in Analysis; view points lack dimming input | `9290b30`; `27ef32a` |
| `ConditionsBand_Running_EvaluateBecomesCancelAnnounced` | `06f131e` | the action stays Evaluate while a run is in flight | `27ef32a` |
| `ConditionsBand_At1024_MoreHoldsDerivedExceptQAndSigma` | `06f131e` | the derived group clips at 1024 px | `27ef32a` |
| `StatusStrip_AnalysisItem_FollowsRunState` | `06f131e` | Historical is displayed as Current | `27ef32a` |
| `Toggle_HistoricalRun_BannerInBothModes` | `f296903` | Historical banner draws only in Analysis; geometry ordinal wording removed | `27ef32a`; banner mutant red, restored |
| `Telemetry_AnalysisProject_FreshnessOnRebuild` | `f296903` | the Historical projection rebuild omits its event | `27ef32a` |
| `Toggle_NavbarAndMenuReachable` | `d57a7dc` | the visible segment, menu command or Shift-Command-A shortcut is missing | `27ef32a` |
| `Toggle_LayersFirstFrame_P95WithinPreviewBudget` | `262b5b5` | the first Analysis frame has no run layers or p95 exceeds 250 ms | `27ef32a` |
| `ConditionsBand_CliRunKey_EqualsAnalyse` | `9e2a109` (alpha +1 mutant; test in `da759d4`) | GUI and CLI operating points produce different run keys | `7ece8b2` |

Both checks printed `FAIL` under `CFD_TEST_ONLY=Toggle_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before implementation. The missing toggle was the observed red condition.

The four added checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen,Toggle_SectionDraftOpen,Analysis_EditVerb_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before the accepted-geometry and refusal changes.

The three band and status checks each printed `FAIL` under `CFD_TEST_ONLY=ConditionsBand_,StatusStrip_AnalysisItem_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before those controls existed.

The Historical and project-event checks each printed `FAIL` under `CFD_TEST_ONLY=Toggle_HistoricalRun_,Telemetry_AnalysisProject_ dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` before a selected-run projection existed. Their final assertions will exercise a Historical run in both modes.

The reachability check printed `FAIL` under `CFD_TEST_ONLY=Toggle_NavbarAndMenuReachable dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis` because the navbar segment was absent.

The readiness check printed `FAIL` under `CFD_TEST_ONLY=Toggle_LayersFirstFrame_P95WithinPreviewBudget dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --readiness` because the first frame had no selected-run layers. The existing readiness suite also printed its independent drag-frame measurement; the selector made only the TGL name affect the exit.

The edit check printed a second `FAIL` under `CFD_TEST_ONLY=Analysis_EditVerb_RefusedWithInertMessage dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis`: the view layer had no Analysis dimming input.

The preview round-trip check printed a second `FAIL` under `CFD_TEST_ONLY=Toggle_PreviewOpen_HiddenThenRestoredUntouched dotnet run -c Release --no-restore --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --analysis`: releasing an already-held point gesture in Analysis committed its hidden draft.

The planted Plan-camera refit (`PlanCamera = new PlanCamera()` inside `ToggleAnalysis`) printed `FAIL Toggle_RoundTrip_CameraSelectionStationViewportEqual` with the expected `1733, 41, -23` camera replaced by `1000, 0, 0`. It was restored before `27ef32a`.

The Historical-banner mutant returned only `Historical` in place of the geometry revision transition. `Toggle_HistoricalRun_BannerInBothModes` printed `FAIL`, then passed after the banner was restored. The rendered check observes the banner in both CAD and Analysis.

The conditions-band mutant added one degree to `OperatingPoints.Custom`. `ConditionsBand_CliRunKey_EqualsAnalyse` printed `FAIL` with CLI key `c625b701cf8768f0ced6521bd602ba8b2426a576038e19929e5e8529323350c2` and band key `63c37053575dc5ef5881d954038158d679206679d3fdfe25872034c624c78a7e`; restoring the builder made it pass.

## Rebase and integration seam (2026-10-05, `trk-tgl`)

The branch rebased onto local `main` at `c826909`. `docs/docs-index.js` was the only conflict and was regenerated with `docs-graph.py derive` from frontmatter before the rebase continued.

| Claim and source | Red observation | Green observation | Residual |
|---|---|---|---|
| View ▸ Analysis is in the menu, palette and shortcut row (`Shell/CommandTable.cs`; `ShellModelTests.cs`) | `CommandTable_Parity_EveryRowInMenuPaletteKey` failed: `View ▸ Analysis must have menu, palette, and shortcut parity` while `PaletteEntries` excluded `view.analysis` | The same named check passed after the exclusion was removed | The native menu already owned the `⇧⌘A` binding; this change adds no duplicate binding |
| The palette actually enters Analysis (`MainWindow.axaml.cs`; `AnalysisToggleTests.cs`) | With the `view.analysis` route absent, `Toggle_NavbarAndMenuReachable` failed: `Palette Enter did not enter Analysis.` | The same rendered-window check passed after the one-line route called `ToggleAnalysis` | The route uses the existing controller transition and its normal `analysis.toggle` event |
| Tab follows CAD → Analysis → Views (`ModelArea.axaml`; `ControllerViewTests.cs`) | Swapping CAD and Analysis in a temporary XAML mutant made `ModelArea_Navbar_KeyboardPath_TabAfterTheViews` fail: `Tab from CAD does not reach Analysis` | Restoring the approved XAML order made the check pass; the mutant left no diff | Keyboard focus is checked through Avalonia's actual navigation handler |

The Release ring built with zero warnings and zero errors. It printed every TGL `--analysis` PASS line and the two shared checks passed. Its Desktop child failed in `--section-editor --part=2/2`: `SectionEditor_DragMove_DrawsWithinOneFrame` failed under an end load of 99.57, followed by five `DSL-DRAFT-OWNED` failures. The full ring reported 122 s wall time, start load 26.40 and end load 99.57; `COST-MISS` for C-2/C-3/C-4 was load-exempt, but C-5 reported three over-limit measurements (F6 2107.211 ms, F18 741.916 ms, F19 938.588 ms). This ring is **red**, not accepted as green.

The instructed isolated rerun of `--section-editor --part=2/2` passed the original six failing checks, then failed two SaveDialog checks later in that part. The three C-5 checks passed in an isolated Analysis run at 465.659 ms, 230.145 ms and 224.730 ms, respectively. The readiness toggle check passed at p95 1.131 ms. With that readiness PASS in `.tmp-tests/TGL-readiness.log`, `check-named-tests.py TGL` counted **12/12 named tests PASS**, but exited 1 because it also rejects the Desktop failures in the original ring log. These are distinct observations; the red full ring remains unresolved at this session's two-cycle repair cap.

Ruling 92's approved tip wording is `Not judged — tip strip` (COPY-220). Local `main` at the rebase tip has no `Labels.TipNotJudged` symbol, and the TGL-owned UI receives the projection's verdict text rather than rendering a tip reason code. The label migration remains an upstream integration dependency; this track has not invented a second copy string.
