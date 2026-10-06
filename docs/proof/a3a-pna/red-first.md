---
id: proof-a3a-pna-red-first
title: "A3a PNA panes and bottom panel receipt"
type: proof-pack
status: active
owner: "@trk-pna"
phase: implementation
tags: [a3a, pna, analysis, layers, bottom-panel, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: proof-a3a-ctx-red-first, rel: relates-to }
review-by: "2026-11-05"
summary: >-
  The red runs of the A3a PNA track: the Layers pane, the Analysis bottom panel with its chart twin, the Properties
  Analysis groups and the shell slots. Each owned test with its red commit and the planted mutant that turns it red.
---

# Track PNA red-first receipt (A3a, 2026-10-05)

Each owned test, what it catches, the commit where it was red, the commit where it is green, and the planted mutant that
turns it red on the finished code (design `area3-analysis.md` §18.8). Mutant runs: `pna-mutants.sh` (scratchpad), each
planted by a one-line edit, run alone with `CFD_TEST_ONLY=<name> ... --analysis`, and reverted (`git status` clean after).

| Test | Ring | Catches | Red commit | Green commit | Planted mutant, observed red |
|---|---|---|---|---|---|
| `LayoutCodec_LayersPane_HomeLeftOldFileOpens` | Core | the `layers` Homes row missing; an old three-pane file refused or reordered | `652dc51` (runtime red: `Expected True; actual False` on the Homes row) | `ec775c5` (LayoutCodec.cs) | red at `652dc51` is the mutant |
| `LayersPane_HideLayer_ViewDropsLayerTwinKept` | Desktop `--analysis` | hiding a layer hides its table twin, or the pane keeps no controller flag | `02de60f` (compile red: `LayersPane`, `AnalysisPanel`, `LoadingChart` do not exist) | `ec775c5` | `Fill` skips a table whose layer is hidden: `FAIL … table strips-table is absent` |
| `AnalysisPanel_Tabs_BoundToSelectedRun` | Desktop `--analysis` | tabs bound to the latest attempt (a Failed row) instead of the selected run | `02de60f` | `ec775c5` | a Failed view clears groups, loading and key: `FAIL … still bound to the selected run, not the latest attempt` |
| `LoadingChart_TableTwin_TogglesAndKeepsFocus` | Desktop `--analysis` | focus lost when the table twin re-renders | `02de60f` | `ec775c5` | `Update` detaches and re-attaches the content: `FAIL … focus kept when the twin re-renders: expected True, got False` |
| `PropertiesView_Analysis_StationShowsStripOfWingRun` | Desktop `--analysis` | a station shows the foil's groups, or rewords a row, instead of the `StripAt` group; only the outermost strip may read Not judged; no Edit-section link; Wing read-only | `02de60f` | `ec775c5` | the station branch is skipped: `FAIL … no strip group at eta 0.5` |
| `PropertiesPane_Analysis_BuildsWithAnalysisModeAndContext` (extra) | Desktop `--analysis` | the pane's Build call hard-coding `ShellMode.Workspace` and passing no Analysis context (G-5) | `02de60f` | `ec775c5` | the Build call passes `ShellMode.Workspace` again: `FAIL … Analysis groups rendered: Basic foil|Foil · nothing selected|Foil|Name…` |
| `Shell_SectionDraftHiddenInAnalysis_EvaluateRepaintsProperties` (extra) | Desktop `--analysis` | the cheap-refresh key `SectionPaneInputs` not holding the Analysis view: panes never repaint after Evaluate | `02de60f` | `ec775c5` | key without the Analysis view: `FAIL … Properties repainted after Evaluate … No analysis yet` |
| `Shell_AnalysisSlots_BottomPanelInAnalysisOnly_LayersTabInLeftDock` (extra) | Desktop `--analysis` | the bottom slot visible in CAD, ⌘J dead outside Analysis, Layers not a left tab, `window.layers` not a shell command | `02de60f` | `ec775c5`, `478b948` (row layout, Dock confinement) | — |

Existing checks updated in the same commits (never deleted): `LayoutParse_*`/`LayoutCodec_DeepestValid_*` (Core `Panes()` is the
four-pane set; the example carries `layers`, commit `0610eac`),
`CommandTable_Parity_EveryRowInMenuPaletteKey` (`window.layers` has no default gesture, like `window.points`),
`Shell_F3_Properties_OneTopTabLabel`, `ShellHost_PlanformLayout_ContainsModelAndSidePanes`, `Focus_CloseLastPane_DockToggle`,
`Shell_F1_NoSidebarHeaderBand` (three grid rows now), `ThemeMatrix_ShellControls_AppliedContrast` (+ `tool.Layers.*`,
`analysis.panel.tab.*`; `tool.Rail controls.*` retired with its tab) and `tools/verify-application-adapters.py` `SHELL_THEME_ROWS`
(same commit as the tests, S-A9).

## Red at `02de60f`

`dotnet build tests/CfdWorkbench.Desktop.Tests` on `02de60f` (tests and tools only, the `src/` changes held back):
`error CS0246: The type or namespace name 'AnalysisPanel' could not be found` (and `LayersPane`, `LoadingChart`). A compile
failure is a red, but a weak one for a behaviour test; the mutant column is the proof each test can fail on the finished code.

## Decisions recorded here

- **`layers` has a `LayoutCodec.Homes` row** (left, after Browser; G-T6). **The Analysis bottom panel has none**: it is a shell
  slot (`ShellHost.AnalysisPanel`, grid row 1), shown in Analysis only and folded by ⌘J, so no saved layout can place, float or
  close it. **`rail-controls` has no row and now no default tab**: four tabs measured 270 px in the 260 px left bar (the last
  tab clipped, 12 theme rows red), and the approved Area 3 mockup draws Properties, Browser, Layers. The tool stays
  findable (`FindDockable("rail-controls")`); one line in `ShellLayout.cs` restores the tab.
