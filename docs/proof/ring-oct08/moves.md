---
id: proof-ring-oct08-moves
title: "Ring headroom, third move: 24 checks to readiness, Desktop spawn re-ordered (Ruling 143)"
type: proof-pack
status: active
owner: "@trk-rgm"
phase: implementation
tags: [ring, test-cost, c-2, c-3]
links:
  - { to: plan-test-cost, rel: relates-to }
summary: "Net fast-ring time 50.6 s to 43.2-43.9 s and both Analysis parts to 3.8-4.2 s; PASS-name union of fast ring plus readiness identical (1811 names); readiness total 137.7 s to 141.6 s."
review-by: "2026-11-08"
---

# Ring oct08 (track trk-rgm)

Ruling 143, quoted: "move the costliest fast-ring checks into the readiness ring, starting with
ThemeMatrix_ShellControls_AppliedContrast (7.0 s), and measured until the fast ring has about 5 s of headroom under C-3
(net 50 s) and each Analysis part about 0.8 s under C-2 (5 s), on a quiet machine. No check is removed or weakened; each
moved check still runs before every push to main through run-readiness. The cost limits are unchanged. The readiness
total stays within its 240 s budget, and the move records each check's measured cost and the before/after ring times."

## Before and after (Verified, `tools/run-tests.sh`, Release)

The machine was not quiet: other tracks ran, and the ring itself raises the 1-minute load to 13-19 by its end. Numbers are
the harness's own. Targets: net <= 45.0 s, each Analysis part <= 4.2 s. No limit changed.

| Step | Change | Net (s) | Desktop (s) | Analysis 1/2, 2/2 (s) | Load start -> end | Result |
|---|---|---|---|---|---|---|
| 0 baseline | none | 50.6 (C-3 red) | 47.2 | 4.90, 4.79 | 9.4 -> 14.4 | C-3 FAILED |
| 1a | ThemeMatrix + 5 Analysis checks | 49.6 | 46.1 | 4.46, 4.07 | 4.6 -> 14.4 | Desktop red once (see note) |
| 1b | same, rerun | 50.4 | 46.9 | 4.19, 3.82 | 13.5 -> 17.7 | C-3 FAILED |
| 2a / 2b | + 7 Desktop checks | 48.7 / 49.6 | 45.3 / 46.2 | 4.21, 3.78 / 4.17, 3.76 | 6.7 -> 13.3 / 13.3 -> 16.7 | green |
| 3a / 3b | + 11 Desktop checks | 47.8 / 48.2 | 44.4 / 44.8 | 4.23, 3.77 / 4.20, 3.80 | 9.6 -> 16.7 / 16.7 -> 18.3 | green |
| 4a / 4b | Desktop spawn re-ordered longest first | **43.2 / 43.9** | 39.8 / 40.5 | 4.19, 3.78 / 4.19, 4.15 | 7.7 -> 13.8 / 13.8 -> 19.2 | green, both targets met |

Step 0 is one run here; the brief's figures at the join of TCV (net 47.5 s, Analysis 4.46 / 4.39 s, load 2-8) are the quiet-machine
reference. Moving 22 Desktop checks alone (steps 2-3) cut the wall by only 2-3 s; the large gain is step 4.

Why step 4: the Desktop job is 18 child processes on 10 slots, started in list order. The sum of their times fell from 320 s to
306 s, but `--analysis` (16.7 s) was last in the list, started at about 25 s when a slot freed, and so set the wall
(about 3 s of prefix + 25 + 17 = 44 s). Listing it with the second wave (the list's own rule is "longest first by measured
SUITE-TIME") ended the greedy fill about 4 s earlier. No check was added, removed or changed by the re-order.
Measure the wall of each move; a check's COST is not its share of the wall (the COST lines sum to 310 s over a 45 s wall).

Readiness (`tools/run-readiness.py`): baseline total 137.7 s (load 7.0), after 141.6 s (load 15.1 -> 4.9), budget 240 s. Its Desktop
step went from 64.2 s to 83.5 s and is the longest step of its group. Both runs GREEN, including `verify-application-adapters.py`
(52.4 s), which reads the ThemeMatrix rows (see below).

## Moved checks (24; COST ms from the Desktop or Analysis log of the run before the move)

| Check | File | COST ms |
|---|---|---|
| ThemeMatrix_ShellControls_AppliedContrast | ShellWindowTests.cs | 7137 |
| Properties_BackspaceInTextField_EditsTextNotPoint | PlanCanvasTests.cs | 2242 |
| RebuildPopover_CancelAndEscape_NoRowBytesAndFreshnessUnchanged | PlanCanvasTests.cs | 2064 |
| RebuildPopover_ReturnApplies_OneUndoRowPointSelectionCleared | PlanCanvasTests.cs | 1596 |
| PropertiesPane_B_EditableValueHasDottedUnderline | PropertiesCellsTests.cs | 1941 |
| PropertiesPane_B_ReadOnlyValueHasNoEditCue | PropertiesCellsTests.cs | 1773 |
| PropertiesPane_B_KindIsEnum_CommitRulesMatchType | PropertiesCellsTests.cs | 1569 |
| ModelArea_Navbar_ViewsDisplayFitFitSelection_SameActionsAsMenus | ControllerViewTests.cs | 1731 |
| PropertiesPane_B_EditCueContrastAtLeast3InThreeThemes | PropertiesCellsTests.cs | 2035 |
| PropertiesPane_B_TabFromClickedPoint_ReachesValuesInOrder | PropertiesCellsTests.cs | 1579 |
| PropertiesPane_B_FocusedValueShowsBox | PropertiesCellsTests.cs | 1525 |
| Properties_ShiftTabFromFirstValue_ReturnsToSelectedPoint | PropertiesCellsTests.cs | 1433 |
| Plan_TabFromSelectedPoint_GoesToPropertiesFirstValue | PropertiesCellsTests.cs | 1313 |
| KindBox_DropDownOpen_ArrowsThenClose_OneUndoRow | PropertiesCellsTests.cs | 1261 |
| RebuildPopover_FocusReturnsToPlanOnClose | PlanCanvasTests.cs | 1947 |
| RebuildPopover_TypedCountOutOfRange_FieldErrorNothingChanged | PlanCanvasTests.cs | 1906 |
| PlanCanvas_ArrowOnHandle_MovesHandleWithCoMotion | PlanCanvasTests.cs | 1565 |
| PlanCanvas_SecondaryClickPoint_SelectsAndOpensPointMenu | PlanCanvasTests.cs | 1401 |
| Properties_TwistHandle_SpanAndValueTyped | PropertiesViewTests.cs | 1533 |
| DeriveVerdicts_TaperedPlanform_SweepIsTheLatticeSweepAndAlphaL0IsTheStripSection | ProjectionTests.cs | 456 |
| DeriveVerdicts_AlphaBound_9p9InsideAnd10p1Outside | ProjectionTests.cs | 406 |
| DeriveVerdicts_OnlyTheTipReasonGetsTheTipRule | ProjectionTests.cs | 343 |
| Polar_ProductRun_ReachesStripsAndSectionProjection | PolarNumericsTests.cs | 304 |
| Section_EditedProfileNoPolar_Unavailable | PolarSeamTests.cs | 296 |

Desktop 19, Analysis 5. Test bodies are unchanged: each check is cut and pasted whole
into its class's `RunReadiness`, or into a new `RunReadiness` (ProjectionTests, PolarSeamTests, PolarNumericsTests) that
`AnalysisChecks.cs` now calls under `--readiness`. None of the Analysis checks is an A8.4 analytic oracle or observed-order
check; F6_ObservedOrder stays in ring 0.

## ThemeMatrix and the adapters gate

`tools/verify-application-adapters.py` requires the THEME-ROW lines and `PASS ThemeMatrix_ShellControls_AppliedContrast` from
`CfdWorkbench.Desktop.Tests --theme-evidence`, which had spawned both `--shell-window` parts. The check now lives in
`ShellWindowTests.RunThemeMatrix()`, called by `RunReadiness()` and by a new `--theme-matrix` mode; `--theme-evidence` spawns
that mode alone. The gate (itself in readiness) still reads the same rows and floors and passed; it now runs one check, not
the whole shell-window suite. `tools/` is untouched.

## PASS-name union (Verified, diffed)

`union-before.txt`: sorted unique `PASS` names over all fast-ring logs plus all readiness logs, before: 1811.
`union-after.txt`, after: 1811. `diff` printed nothing and exited 0. No name dropped, none added.

## Not changed

Cost limits, `run-tests.sh`, `run-readiness.py`, `join.json`, `tools/`. Not in the TCV track's files.

## Open, noted

`SectionEditor_DragMove_DrawsWithinOneFrame` failed once (step 1a, load 14): "Move 2 notified the shell 1 times". It is in
`--section-editor`, which this change does not touch, and passed in the next 7 ring runs. Not investigated; a load-sensitive check.
