---
id: proof-m12c-pnl-red-first
title: M1.2c PNL red-first receipt (product mutants)
type: proof-pack
status: accepted
owner: "@track-pnl"
phase: implementation
tags: [m12c, pnl, red-first, mutants, tests]
links:
  - {to: design-m12c-section-editor, rel: depends-on}
review-by: 2026-11-03
summary: >-
  PNL wrote code before tests for ten named checks. Each was then turned red by a planted product mutant and
  back to PASS on revert; none is a tautology. Per check: mutant file:line, the FAIL line, revert confirmed.
review-suggested: []
---

# PNL red-first receipt (retroactive mutation proof)

Ten PNL named checks passed on their first run (code was written before tests). For each, the smallest plausible
mutant was planted in product code, the one check was run (`CFD_TEST_ONLY=<name>`, Release, Desktop suite named
below), the FAIL line recorded, the file reverted with `git checkout`, and the check re-run to PASS.
Every check went red; none is a tautology. Tree clean after each revert (0 tracked changes).

| # | Check (suite) | Mutant (product file:line, change) | FAIL line | Revert |
|---|---|---|---|---|
| 1 | Properties_StationGroup_EndsWithEditSectionLink (--properties-view) | `Shell/ShellHost.cs:137` entry origin `EntryOrigin.Properties` -> `EntryOrigin.Palette` | the link did not open the section editor from Properties | PASS |
| 2 | PointsPane_RowClick_SelectsPointInCanvas (--status-strip) | `Panes/PointsPane.axaml.cs:139` `point.Click` handler `SelectRow(row)` -> `{ }` | row click selected Points {...}; strip item 'Nose' | PASS |
| 3 | PointsPane_CanvasSelection_RowSelected (--status-strip) | `Panes/PointsPane.axaml.cs:158` `Classes.Set("on", row.Selected)` -> `false` | row on False, partner True, '⇄ 6' | PASS |
| 4 | StatusStrip_ShowAction_FramesBlockingPoint (--status-strip) | `Shell/ShellHost.cs:1202` `ShowBlocker(moved, range)` -> `ShowBlocker(null, range)` | Show selected Foil { }; frame request (, (0.2222, 0.5236)) | PASS |
| 5 | Workspace_Precision_ShowsPointsInHomeRegion (--status-strip) | `Persistence/LayoutCodec.cs:31` home `("points", Right)` -> `Bottom` | Points pane not in the window | PASS |
| 6a | Presets_DesktopEqualsCodec_EveryWorkspace (--shell-model) | `Shell/WorkspacePresets.cs:24` `ShownIn` drops the `region.Open` test | pane homes: properties@Left, browser@Left, points@Right | PASS |
| 6b | same check, byte-equality half | `Shell/WorkspacePresets.cs:18` desktop preset omits the "browser" pane | Planform: desktop preset differs from the codec's | PASS |
| 7 | Layout_SavedMessagesPane_DroppedWithCode (--shell-model) | `Persistence/LayoutCodec.cs:269` `Take` no longer drops unregistered panes (`!registered.Contains(pane)` removed) | codes [LAYOUT-PANE] dropped 1 panes [properties,browser,points,messages] | PASS |
| 8 | StatusStrip_SectionInsertReport_CountAndDeviation (--status-strip) | `Shell/StatusStrip.axaml.cs:176` "Inserted {what} at" -> "Added {what} at" | insert report 'Added a point at 45.00 % chord: ...' | PASS |
| 9 | StatusStrip_CrossingCleared_Copy124RenderedOnce (--status-strip) | `Shell/ShellHost.cs:1204` clearing branch drops `&& sectionCrossing` (COPY-124 on every uncrossed assessment) | COPY-124 shown without a crossing | PASS |
| 10 | PointsPane_InvalidValue_FieldErrorNoStep (--status-strip) | `PointsView.cs:177` y-in-mm refusal (`YRefusesMm`) removed | y in mm made a step or no reason: '' | PASS |

Notes
- A first attempt at #1 (empty click handler in `PropertiesPane.axaml.cs:709`) did not build (CS0067); the harness treated
  it as no result and it was discarded, not counted. `one.sh` aborts on a build failure.
- #6 is two mutants: the desktop preset delegates to the codec, so the byte-equality half only goes red when the two
  disagree (6b); the home assertions go red on 6a.
- Not covered by this proof: the other eight PNL checks (red earlier), and the rendered-look of the rows (Inferred).
