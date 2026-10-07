---
id: proof-ring-split-moved-2
title: "Ring headroom 2: checks moved to readiness, Core parts rebalanced"
type: proof-pack
status: active
owner: "@trk-rh2"
phase: implementation
tags: [ring, test-cost, readiness]
links:
  - { to: proof-ring-split-moved, rel: relates-to }
summary: "Ruling 129: 13 Desktop checks and one Core check moved into RunReadiness, two Core group swaps; ring net 49.97 s to 44.6-47.5 s, PASS union diff empty, readiness green."
review-by: "2026-11-07"
---

# Ring headroom 2 (Ruling 129, 2026-10-07)

Bodies unchanged. Each check moved into its class's `RunReadiness` (new where absent, wired in `WorkbenchTests.cs`). Cost is the per-check wall
time in a fast ring at load 3-9 (`COST` lines; Core from a sequential instrumented run, temporary timer not committed).
`ThemeMatrix_ShellControls_AppliedContrast` (7.2 s) stays: the adapters gate reads it.

| Check | Suite | Cost ms |
|---|---|---|
| `ProfileEvaluator_BoundToCertificate_Within1e9Chord` | Core (`DisplayProfileTests`) | 3,768 |
| `RebuildPopover_StepperReadoutsFromCore` | Desktop (`PlanCanvasTests`) | 2,270 |
| `Properties_RebuildLink_OpensCurvePopover` | Desktop (`PlanCanvasTests`) | 2,063 |
| `PlanCanvas_BackspaceOrDelete_RemovesSelectedControlSelectsNearest` | Desktop (`PlanCanvasTests`) | 1,977 |
| `PropertiesPane_TypedTipBelowMinimum_KeepsTextRefusesAndOffersUseOnlyOnClick` | Desktop (`GestureLimitTests`) | 1,937 |
| `Properties_SectionPointTypedX_OneStepExact` | Desktop (`PointsPaneTests`) | 1,790 |
| `PropertiesPane_HeldTipDrag_WingRowStripAndPointNameSpeakTheSameWords_MarkerRenders` | Desktop (`GestureLimitTests`) | 1,727 |
| `StatusStrip_CrossingCleared_Copy124RenderedOnce` | Desktop (`PointsPaneTests`) | 1,700 |
| `AnalysisLayers_WindowRendersAndPeersFollowVisibility` | Desktop (`AnalysisLayerTests`) | 1,683 |
| `Shell_AllModelTabs_ReentryRealizesContent` | Desktop (`ShellWindowTests`) | 1,640 |
| `Properties_SectionPointTypedMmX_ConvertedAtStationChord` | Desktop (`PointsPaneTests`) | 1,610 |
| `Toast_Hold_ClosesAfterHold_PausedWhileHovered` | Desktop (`StatusStripTests`) | 1,574 |
| `PropertiesPane_StateBrushes_InAllThreeThemes` | Desktop (`PropertiesViewTests`) | 1,496 |
| `SectionStep_Refused_RestoresCertificate` | Desktop (`ControllerSectionTests`) | 1,413 |

Tried and put back, with the reason:
- `Store_InFlightDispose_...` and `Store_EquivalentCaseAlias_...` (5 s waits each): `tools/verify-application-core.py` runs the Core harness on the
  `Store_` subset and expects 84 checks; moving them made readiness fail (`STORE-SUBSET ... expected 84`). They stay in the ring.
- `ModelArea_Navbar_ViewsDisplayFitFitSelection_SameActionsAsMenus`: shares a local function (`NavItems`) with a check that stays; moving it needs a body edit.

## Core rebalance (registration order, `IdentityTests.cs`)

Two swaps of `Run()` lines: `PlacementTests` with `GroupGestureTests`, and `SectionEditTests.RunMultiProfile` with `ReopenSectionDraftTests`.
The search ran over the measured per-check costs and the registration indices of each group. Core parts before: 34.0 / 30.5 / 28.4 s (1/3, 2/3, 3/3);
after: 29.4 / 28.0 / 29.1 s (spread 1.4 s) in the last run; 30.5-32.8 s each, spread under 2 s, in the others.

## Proof

- PASS union (ring plus readiness, sorted): 1,770 lines before and after; `union-diff-2.txt` is empty. Ring 1,667 -> 1,653 PASS lines, readiness 103 -> 117.
- Baseline (main e2aa3bfe, load 8.8): net 47,633 ms here; the CPV join read 49.97 s.
- Fast ring after, net ms (wall - build), 0 failures, start load in brackets: 45,755 [8.4], 46,495 [18.9], 46,905 [22.6], 46,229 [25.6, over the gate], 47,495 [24.7, over the gate], 44,571 [15.2].
  The gate-clean runs (start load <= 24) read 44.6-46.9 s; two runs started above 24 and are listed, not counted.
- Last run per part: Core 29.4 / 28.0 / 29.1 s, Desktop 41.8 s, Analysis 4.5 / 4.5 s.
- Readiness on the final head: GREEN, total 188.9 s of 240 s (178.6 s before the moves), load 25.0 -> 6.0.
- Known flake, untouched check: `SectionEditor_DragMove_DrawsWithinOneFrame` failed once in a ring run (load 8 -> 17.6) and passes alone; it failed once in the earlier pass too.
