---
id: proof-ring-split-moved
title: "Ring split: checks moved from the fast ring to readiness"
type: proof-pack
status: active
owner: "@trk-spl"
phase: implementation
tags: [ring, test-cost, readiness]
links:
  - { to: proof-ring-split-profile, rel: relates-to }
summary: "Eleven costly fast-ring checks moved into their suite's RunReadiness (Ruling 123): list with cost, ring and readiness proof, PASS union diff empty."
review-by: "2026-11-06"
---

# Checks moved to readiness (Ruling 123, 2026-10-06)

Bodies are unchanged; only the registration moved into the same class's `RunReadiness` (new where none existed, called from the
`--readiness` switch of `IdentityTests.cs` or `WorkbenchTests.cs`). Costs are per-check wall times: Desktop from the `COST` lines of a
fast ring at load 10-20 (contended children); Core from a sequential instrumented run (temporary timer, not committed).

| Check | Suite | Cost ms |
|---|---|---|
| `SectionCommands_EveryRow_RunsOrNamesReason` | Desktop (`PointsPaneTests`) | 6,798 |
| `StatusSlot_CompletionRacingNewerWrite_NewerAlwaysWins` | Desktop (`ControllerShellTests`) | 7,345 |
| `PlanCanvas_CrossingMarker_OnlyWhileReleaseWouldBeRefused` | Desktop (`PlanCanvasTests`) | 6,987 |
| `ModelArea_Views_SeparatedByGutterAndFramed` | Desktop (`ControllerViewTests`) | 3,787 |
| `PropertiesPane_TypedRootAboveMaximum_LeadsWithRootAndUseAppliesTheMaximum` | Desktop (`GestureLimitTests`) | 2,850 |
| `PropertiesPane_B_EveryEditableValueIsTabStop` | Desktop (`PropertiesCellsTests`) | 2,766 |
| `ContextMenu_MakeAnchor_SameEffectAsProperties` | Desktop (`ShellWindowTests`) | 2,659 |
| `Elevation_NudgeLadders_PerChannelTable` | Desktop (`ElevationTests`) | 2,240 |
| `Placement_DisplayWithinCertifiedEnclosure_Fixtures` | Core (`PlacementTests`) | 6,180 |
| `GroupGesture_ReleaseNeverThrowsTipChordMin` | Core (`GroupGestureTests`) | 4,129 |
| `TipChord_Drag_EveryHeldFrame_IsAdmitted_ReleaseNeverRefused` | Core (`TipChordTests`) | 2,042 |

Not moved: `ThemeMatrix_ShellControls_AppliedContrast` (8.1 s, the largest) is the theme evidence that `tools/verify-application-adapters.py`
reads; it stays in the fast ring. The three `Store_*` checks (5 s each) are waits on Core part 2, not the long part.

## Proof

- PASS union (fast ring plus readiness, sorted, duplicates kept): 1,754 lines before and after; `union-diff.txt` is empty (`diff` exit 0).
  The fast ring alone goes 1,679 -> 1,668 PASS lines; readiness goes 75 -> 86.
- Fast ring, four runs after the moves (net = wall - build), 0 failures, end load in the last column:

| Run | start load | net ms | Core 1/3, 2/3, 3/3 ms | Desktop ms | CPU-s | end load |
|---|---|---|---|---|---|---|
| 1 | 13.6 (GO) | 47,240 | 33,603 / 30,737 / 28,549 | 44,494 | 485 | 22.1 |
| 2 | 22.1 (GO) | 47,424 | 33,865 / 31,058 / 29,501 | 44,675 | 494 | 25.0 |
| 3 | 25.0 (WAIT, started anyway) | 47,174 | 34,058 / 31,097 / 28,952 | 44,417 | 493 | 27.9 |
| 4 | 4.8 (GO) | 47,611 | 33,880 / 30,826 / 29,222 | 44,897 | 497 | 13.5 |

  Before (same tree, 4 runs): net 49,290-50,520 ms, Core 3/3 45 s, Desktop 46.5-47.8 s, 533-548 CPU-s.
  Core 3/3 fell from 45 s to 29 s because the three moves also rebalanced the registration index across the parts (a part is `index % 3`).
- Readiness, one run on the committed head b3224b05b: GREEN, total 173.7 s of 240 s (141.8 s before the moves), load 22.5 -> 5.4.
- No limit changed; no test body edited; `tools/` untouched.
