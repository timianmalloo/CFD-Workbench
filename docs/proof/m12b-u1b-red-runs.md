---
id: proof-m12b-u1b-red-runs
title: U1b Plan canvas red and mutant runs
type: proof-pack
status: complete
owner: track-u1b
phase: m1.2b
tags: [plan-canvas, tdd, rendered-state]
links:
  - { to: design-m12b-points, rel: depends-on }
review-by: "2026-10-30"
summary: >-
  Records the observed red runs and mutation controls for the U1b Plan canvas, including
  whole-window rendered pixels and point automation peers.
---

# U1b red runs

| Run | Command | Exit | Evidence |
|---|---|---:|---|
| A1 | `dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --plan-canvas` | 1 | The first two named tests could not compile because `ModelArea.PlanCanvas` was absent (`/tmp/u1b-assumptions-red.log`). |
| A2 | same command after the first Plan surface and peer | 1 | Whole-window bitmap had a white point pixel; inspection of `/tmp/u1b-window.png` proved the window rendered, and the test oracle was corrected to compare the point to the viewport background. |
| A3 | same command | 0 | `PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` and `PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` PASS (`/tmp/u1b-assumptions-green.log`). |
| R1 | same command with first rendered batch | 1 | `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints`, `PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn`, and `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne` FAIL (`/tmp/u1b-render-batch-red.log`). |
| R2 | same command after theme brushes, selection glyphs and framebuffer oracle | 0 | Seven named checks PASS (`/tmp/u1b-render-batch-green.log`). |
| I1 | same command with hit, hover, selection, Tab, comb, camera, and layout tests added | 1 | Compilation failed for absent interaction members (`/tmp/u1b-interaction-batch-red.log`). |
| I2 | same command after those members were added | 1 | The comb rendered-tier assertion sampled between teeth; all other 17 names PASS (`/tmp/u1b-interaction-batch-green.log`). |
| I3 | same command after whole-window frame comparison replaced the one-pixel comb oracle | 1 | The moved samples tab had not settled when sampled; its readiness loop was extended (`/tmp/u1b-interaction-batch-green.log`). |
| A4 | same command with focus, high-contrast, re-entry, brush, live-region and handle-peer checks | 1 | `PlanCanvas_AutomationPeers_HandleNamesCarryAngleAndLength` FAIL; 23 other names PASS (`/tmp/u1b-access-batch-red.log`). |
| A5 | same command after the peer name carried angle and length | 0 | 24 names PASS (`/tmp/u1b-access-batch-green.log`). |
| E1 | same command with routed pointer and key events in place of direct helper calls | 1 | Shift/Command click and Space/Shift+Space selection failed before the event handlers (`/tmp/u1b-events-red.log`). |
| E2 | same command after routed pointer and key handlers | 0 | 24 names PASS (`/tmp/u1b-events-green.log`). |
| D1 | same command with drag frame, live delta, orthogonal drag, Tab, Escape and focused C checks | 1 | `PlanCanvas_DragDeltaReadout_Live` failed while the other five passed (`/tmp/u1b-drag-red.log`). |
| D2 | same command after drawing the tracing probe and appending drag deltas | 0 | 30 names PASS (`/tmp/u1b-drag-green.log`). |
| F1 | same command with station chip, collision, double-click, focus, lock and handle Escape checks | 1 | Compilation failed for absent chip and focus members (`/tmp/u1b-chips-focus-red.log`). |
| F2 | same command after chip and focus work | 0 | 37 names PASS (`/tmp/u1b-chips-focus-green.log`). |
| G1 | same command with final six names added | 1 | Final release, advisory, probe, certification and render-error names were red before their implementation (`/tmp/u1b-final-red.log`). |
| G2 | same command after those behaviours were implemented | 0 | All 43 names PASS (`/tmp/u1b-final-green.log`). |
| M1 | same command with `PlanCanvas.Render` drawing blanked | 1 | All 43 names FAIL on realized pixels or the bitmap precondition (`/tmp/u1b-render-blank-mutant.log`). |
| M2 | same command after restoring `PlanCanvas.Render` and the shared axis layer refactor | 0 | All 43 names PASS (`/tmp/u1b-plan-final.log`). |
| V1 | same command with a rendered fill assertion | 1 | `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints` detected absent planform fill (`/tmp/u1b-fill-red.log`). |
| V2 | same command after fill, centre line, polygon and scale bar; background oracle moved off the centre line | 0 | All 43 names PASS (`/tmp/u1b-fill-green2.log`). |

The raw `/tmp` files are local run logs. M1 killed these named tests:
- `PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` — FAIL under blank renderer
- `PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` — FAIL under blank renderer
- `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints` — FAIL under blank renderer
- `PlanCanvas_Orientation_SpanRightAftDown` — FAIL under blank renderer
- `PlanCanvas_ExampleOpen_RendersBothRailsAndEveryPoint` — FAIL under blank renderer
- `PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn` — FAIL under blank renderer
- `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne` — FAIL under blank renderer
- `PlanCanvas_HitTest_NearestWithin14Px` — FAIL under blank renderer
- `PlanCanvas_HoverPoint_TooltipCopyAndRing` — FAIL under blank renderer
- `PlanCanvas_HoverRail_TracingProbeReadout` — FAIL under blank renderer
- `PlanCanvas_ShiftClickAndCommandClick_ExtendAndToggle` — FAIL under blank renderer
- `PlanCanvas_SpaceAndShiftSpace_SelectAndToggle` — FAIL under blank renderer
- `PlanCanvas_TabOrder_LeadingThenTrailingThenChips` — FAIL under blank renderer
- `PlanCanvas_TabPastLastPoint_LeavesCanvas` — FAIL under blank renderer
- `PlanCanvas_CombToggle_RenderedTeethOnSelectedRail` — FAIL under blank renderer
- `PlanCanvas_ZoomPanFit_KeyboardAndPointerSameCamera` — FAIL under blank renderer
- `ModelArea_MinimumWindow_PlanAtLeast320x240` — FAIL under blank renderer
- `ModelArea_SamplesTab_IsometricMovedUnchanged` — FAIL under blank renderer
- `PlanCanvas_FocusRing_RenderedPixelsAtLeastThreeToOne` — FAIL under blank renderer
- `PlanCanvas_HighContrast_RenderedRingContrastPrimary` — FAIL under blank renderer
- `PlanCanvas_AfterDockReattach_RendersSameScene` — FAIL under blank renderer
- `PlanCanvas_Brushes_AllFromThemeResources` — FAIL under blank renderer
- `PlanCanvas_ProbeAndDelta_NotLiveRegions` — FAIL under blank renderer
- `PlanCanvas_AutomationPeers_HandleNamesCarryAngleAndLength` — FAIL under blank renderer
- `PlanCanvas_DragFrame_RenderedCurveThroughDraftSample` — FAIL under blank renderer
- `PlanCanvas_DragDeltaReadout_Live` — FAIL under blank renderer
- `PlanCanvas_ShiftDragFromPoint_OrthoLocked` — FAIL under blank renderer
- `PlanCanvas_TabWithMultiSelection_KeepsSelection` — FAIL under blank renderer
- `PlanCanvas_Escape_DismissTooltipThenClearSelection` — FAIL under blank renderer
- `PlanCanvas_CKeyInTipChordField_CombNotToggled` — FAIL under blank renderer
- `PlanCanvas_ClickStationChip_SelectsStation` — FAIL under blank renderer
- `PlanCanvas_CollidingChips_AlternateHiddenStillInBrowser` — FAIL under blank renderer
- `PlanCanvas_DoubleClickPoint_RaisesTypeValueRequest` — FAIL under blank renderer
- `PlanCanvas_FocusOffscreenPoint_PansIntoView` — FAIL under blank renderer
- `PlanCanvas_FocusUnderProbeOrChip_PansIntoView` — FAIL under blank renderer
- `PlanCanvas_LockedNudge_AssertiveLockCopy` — FAIL under blank renderer
- `PlanCanvas_EscapeOnHandle_FocusBackToPoint` — FAIL under blank renderer
- `PlanCanvas_ArrowOnHandle_MovesHandleWithCoMotion` — FAIL under blank renderer
- `PlanCanvas_ReleaseEdgesCross_PointRenderedAtOriginal` — FAIL under blank renderer
- `PlanCanvas_AdvisoryCrossingClear_CertificateStillDecides` — FAIL under blank renderer
- `PlanCanvas_HoverProbe_ParksPointerFirst` — FAIL under blank renderer
- `PlanCanvas_NotCertifiedFoil_PointsDimmedBannerNoDraft` — FAIL under blank renderer
- `PlanCanvas_RenderThrows_CopyTryAgainAndEvent` — FAIL under blank renderer

## Exit receipts

| Check | Exit | Observed result | Local log |
|---|---:|---|---|
| Final full run 1 | 0 | wall 39 s; Core 377, CLI 1, Desktop 256 PASS | `/tmp/u1b-full-final1.log` |
| Final full run 2 | 0 | wall 37 s; identical PASS sets | `/tmp/u1b-full-final2.log` |
| Final full run 3 | 0 | wall 37 s; identical PASS sets | `/tmp/u1b-full-final3.log` |
| U1b named check | 0 | 43/43 | `/tmp/u1b-named-U1b.log` |
| B0, B1a, B1b, U1a, D3a, D1, D2, C1, P1 named checks | 0 each | All named sets PASS | `/tmp/u1b-named-<track>.log` |
| XAML token lint | 0 | clean | `/tmp/u1b-xaml-lint.log` |
| Documentation checks | 0 | Documentation checks passed | `/tmp/u1b-check-docs-final.log` |
| Plan render readiness, one run | 0 | 7.97 ms against 8.00 ms; met; timing recorded only | `/tmp/u1b-readiness.log` |
| Native launch | 130 after foreground Ctrl-C | `main-window-assigned=True`, `window-opened`; final `pgrep -fl CfdWorkbench` found no process (exit 1) | `/tmp/u1b-native-final.log` |

The final three Core and Desktop PASS lists were compared byte for byte (`cmp` exit 0 for each pair). The CLI PASS name was the same in each full-run log. The native application was terminated after its window-open receipt; no launched application remains.
