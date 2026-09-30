---
id: proof-shellfix-red-runs
title: Shell visual defect red runs
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: m1.2a-shellfix
tags: [app-shell, rendered-ui, regression]
links:
  - {to: design-app-shell, rel: tested-by}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-30
summary: >-
  Red-first rendered-control checks for the operator's first native shell findings.
  Each row records the observed failure before its production fix.
---

# Shell visual defect red runs

All runs used `AGENT_SESSION=track-shellfix AGENT_WI=SHELLFIX` and the Release Desktop named-check harness.

| Finding | Red check and observed failure | Cause and green evidence |
|---|---|---|
| F7 | `Shell_F7_ModelTabReentry_RendersAcceptedFoil`: `FAIL ... re-entry: model viewport was not attached and drawn; root=Window, visible=True, bounds=0, 14, 907, 599, render=2, revision=0/1`. A forced frame invalidation after returning from Foil source did not draw. | Dock's nonzero deferred reveal duration held the document presentation: a diagnostic run with `RevealDuration = TimeSpan.Zero` passed without another change. Setting zero in `ShellHost.InstallTheme` follows design §11's “Motion: none”; the same check then passed with the diagnostic override removed. |
| F6 | `Shell_F6_ModelArea_OnlyDockDocumentTabs`: `FAIL ... Model area has 1 inner tab rows and 4 Dock document tabs`. | The pre-Dock `DocumentTabs` remained visible inside the Dock Plan + 3D document. Its three other bodies were already moved into Dock, leaving a dead inner strip. Removed that tab control; the four Dock document tabs are now the only row. The check passed. |
| F9 | `Shell_F9_SectionSelectedStation_DrawsProfile`: `FAIL ... Selected station Section canvas has no realized profile drawing inputs`; `Shell_F9_SectionNoStation_ShowsEmptyCopy`: `FAIL ... Section document omitted its No station selected empty state`. | The moved `SectionEditorView` was never bound by `ShellHost.RefreshPanes`; its canvas kept a null profile. Bind the selected station's `SectionView` and show “No station selected.” over the canvas when no station is selected. Both realized-control checks passed. |
| F1 | `Shell_F1_NoSidebarHeaderBand`: `FAIL ... Standalone Sidebar header band is visible`. | The only child of the shell's extra top row was a button labelled “Sidebar”. Moved its toggle into Dock's document-tab row as a named glyph button and removed the extra row. The check and existing focus-return checks passed. |
| F2 | `Shell_F2_LeftPaneChromeButtons_NamedAndDrawn`: `FAIL ... Left pane has blank or unnamed chrome buttons`; the realized controls were `PART_MenuButton` and `PART_CloseButton`, each with `AutomationProperties.Name = null` and a `Viewbox` body. | Dock's tool chrome supplied pane-menu and close buttons whose package glyphs did not appear in the native capture. Preserve their Dock actions, set visible “⋯” and “×” content, and name them “Pane menu” and “Close left side bar”. The check and `UI_DEAD_CONTROL_ShellButtonsHaveActions` passed. |
| F3 | `Shell_F3_Properties_OneTopTabLabel`: `FAIL ... Properties has 3 visible labels; tab top=777`. The labels were in Dock's tab, `PropertiesPane`, and `ToolChromeControl`; Dock placed the tab at the bottom. | A candidate moved the tab to 33 DIP and reduced the label count to one, but it failed six existing `ThemeMatrix_ShellControls_AppliedContrast` Browser rows. The candidate and its check were rolled back to keep the subset green. **Not fixed.** |
| F4 | `Shell_F4_LeftPane_Default260At1440And1280`: `FAIL ... At 1440 DIP, left pane is 359 DIP rather than 260`. | `ShellLayoutFactory` starts the left tool dock at `Proportion = 0.25`. Two candidate changes converted 260 DIP to a proportion and invalidated the proportional panel, but the realized pane remained 359 DIP while the model proportion became 0.181058495821727. The two-cycle repair cap fired; both candidates and the still-red check were rolled back. **Not fixed.** |
| F5 | `Shell_F5_Start_FirstCardFocusedWithRing`: `FAIL ... First start card focus/ring absent: focused=False, ring=False`. | The production shell's window-opened path does not focus the first card; only the review-mode branch does. The check was removed from the green subset when the F4 cap stopped the track. **Not fixed.** |

The retained control `Shell_AllModelTabs_ReentryRealizesContent` visits each of the four Dock documents, leaves it, returns, and checks attached nonzero content with document-specific render inputs. It passed in the 116-check Desktop suite.

## Green subset verification

Three consecutive `tools/run-tests.sh` runs exited 0 with 281 Core and 116 Desktop PASS lines each. Sorted PASS sets
were byte-identical (`shasum`: `66af9a015bc631f96747957508066599ca7ace62`). The D3a, D1, D2, C1, and P1
named checks exited 0 (40/40, 12/12, 21/21, 14/14, 23/23). XAML token lint and `dotnet restore
CFDWorkbench.slnx --locked-mode` exited 0. The desktop app's foreground startup smoke printed
`NATIVE-STARTUP smoke-opened`, exited 0, and left no `CfdWorkbench` process.

`check-docs.py` exits 1 on one Docs Explorer index-drift item for this new proof-pack artifact. The derived
`docs/docs-index.js` is outside SHELLFIX ownership. Its owner must run `docs-graph.py derive` after merging this
proof, then rerun `check-docs.py`. Readiness remains pending on that seam and the unfinished F3–F5 repairs.
