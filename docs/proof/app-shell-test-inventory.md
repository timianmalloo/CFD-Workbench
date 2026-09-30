---
id: proof-app-shell-test-inventory
title: App-shell test inventory — WorkbenchTests.cs assertions bound to controls the shell removes or changes
type: proof-pack
status: in-review
owner: "@track-d3a"
phase: implementation — D3b completion
tags: [app-shell, desktop, d3a, test-inventory, harness-migration, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2026-10-27
summary: >-
  Every throw-new assertion in WorkbenchTests.cs (212, measured) is classified.
  All 60 ported rows have destination tests and recorded red evidence; 33 rows stay
  unchanged under Ruling 55. None are deleted.
---

# App-shell test inventory (checkpoint D3a-0)

**D3b completion (2026-09-30).** The 21 rows whose disposition was still `ported` after THEME now have recorded red evidence: seven from D3a dispatch 3, and fourteen in D3b runs 45–57 (with :389 and :1141 sharing one run). Each reflection-bound Dock row (:387, :453, :513) has its own run; the locked-control row :1326 has D3a run 9. All 60 ported rows are marked done.

**THEME checkpoint (2026-09-30).** `ThemeMatrix_ShellControls_AppliedContrast` passes in the shell suite after red run 34. Its 39 rows are marked `done (THEME, red run N)`. The 8 reflection-bound rows among them (:721, :740, :977, :1018, :1215 and the framework rows :1219, :1221, :1228) each cite their own red run (35–42); :1446 cites run 34. The kept-unchanged matrix rows (:1295, :1297, :1315, :1333, :1353) stay in `WorkbenchTests.cs`.

**Result.** `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs` holds **212** `throw new` lines (grep, at `bf54a54`).
The design's figure of 213 (§1, §12.5) is one high. The design counted at `494aef2`. Commit `d27dc03` then removed two
child-exit throws and added the self-launch failure probe (`:21`), so the count is 212. **92 lines are bound** to a
control that the shell removes, moves or rewrites. Line `:1757` holds two assertions with different dispositions, so the
table has **93 rows**. The other **120 lines** are not bound to a removed control, and they stay as they are (§3).

| Disposition | Rows |
|---|---|
| ported | 60 |
| deleted | 0 |
| kept-unchanged (reflection-bound, or the rail editor that moves unchanged into a pane) | 33 |
| **total** | **93** |

**Method (measured).** The line numbers come from `grep -n 'throw new'` on the file at `bf54a54`. Each line was read in
context and bound to the control it reaches. The controls come from `src/CfdWorkbench.Desktop/MainWindow.axaml` and
the statics in `MainWindow.axaml.cs`. What the shell removes, keeps or moves comes from design §6.7, §8 and §11,
§3.7 ("Point rows arrive with M1.2b"), spec B7 (the CAD-first window), the spec Appendix G superseded table and
Ruling 55 (`docs/notes/rulings.md`), which amends design §6.7 at `8c6260d`.

**Name sources.** A ported name is a D3a name from design §9/§12.4. Seven of them were proposed by this inventory
and adopted as D3a names by Ruling 55 (design §12.4, "D3a-0 additional names"). One
design name belongs to D1: `FocusRing_HiddenRegionsAndFloats_Order`. `NextRegionIndex` moves into D1's `FocusRing`
(design §5.1), so its assertion moves with it. `tools/check-named-tests.py D3a` requires a `PASS` for every name in
the `ported` column, so D3a's run must include D1's merged `--shell-model` suite.

**Appendix G clauses (spec `docs/specs/cfd-workbench-v1.md` §Appendix G, "Superseded").** After Ruling 55 no row
is deleted, so no row cites a clause. The two clauses stay here because the first version of this inventory cited
them, and G-CAD04 is why the locked-CV row is ported and not deleted.

- **G-B7:** "B7's Navigator dock, right-hand Properties dock, palette, options strip and four-viewport default (CAD
  area)" → superseded by "B7's CAD-first window".
- **G-CAD04:** "CAD-04 'opens the one draft' for point gestures" → superseded by "DR-6 default, A3.2". What stands:
  "elevations, lock refusals, focus ring, accessible values".

**Names this inventory added (adopted as D3a names by Ruling 55).**

| Name | Covers |
|---|---|
| `ThemeMatrix_ShellControls_AppliedContrast` | the `--theme-controls` applied-theme matrix (run by `tools/verify-application-adapters.py:644`), retargeted: toolbar → app-bar button, `DocumentTabs` → Dock document tabs, station rows → Browser rows, numeric TextBox → Wing **Span** field, `SourceText` → "Foil source" tab; the pointer-RED and numeric-paint RED repro modes, whose `ControlList` and `NumericInput` are now found in the rail-editor pane |
| `DockTabFocus_FreshBatch_ReadyAndTwoRing` | the `--focus-readiness` and `--focus-diagnostic` modes, retargeted from the first `DocumentTabs` item to the first Dock document tab |
| `F6_RegionEntry_FocusesSelectedTabOrRow` | `MainWindow.FocusCandidates` for the document-tab region and the station region → Dock tab and Browser row |
| `Browser_AcceptedIdentity_KeepsOrReplacesRows` | `MainWindow.BindNavigatorItems` → Browser pane rows (same identity keeps AX items; new identity replaces them) |
| `Review_Persona_FocusesShellRegion` | `MainWindow.ReviewFocusTarget`, whose `numeric-or-open` / `stations` / `controls` targets move (Open to the menu, stations to Browser, CV list and numeric field to the rail-editor pane) |
| `ModelArea_MinimumWindow_PlotWidthAtLeast250` | `Viewport.PlotWidth(1024 - 240 - 300 - 24, 178)`: the constants are the removed 240 px Navigator and 300 px Properties columns |
| `Controller_LockedRailControl_RefusesDraft` | the locked-CV no-draft assertion. G-CAD04 keeps "lock refusals", so it cannot be deleted. It is retargeted to `WorkbenchController.BeginEdit` → `DSL-LOCK` (`WorkbenchController.cs:254`), which no Desktop assertion covers today |

## 1. Inventory

Column key: `ported` is the new test name, empty when the row is deleted or kept. `superseded clause` is filled for
deleted rows; after Ruling 55 there are none. `reflection?` is **yes** when the assertion reaches our private members (`workbench`, `closeApproved`,
`UnsavedDialogAsync`) by reflection, directly or through a controller obtained that way. It is **framework** when the
reflection is Avalonia's protected `PseudoClasses`.

| WorkbenchTests.cs line | assertion (short) | control | disposition | ported | superseded clause | reflection? |
|---|---|---|---|---|---|---|
| 113 | frozen 18+60+21 per-theme row table (toolbar, tab, station, cv, numeric rows) | ExampleButton, DocumentTabs, StationList, ControlList, NumericInput, SourceText | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 284 | closed-callback repro: controller field readable | MainWindow `workbench` field | kept-unchanged | | | yes |
| 328 | closed window callback does not escape; next window stays open | MainWindow close, second window | kept-unchanged | | | yes |
| 334 | next window publishes its accepted update | MainWindow + controller (via :281-284) | kept-unchanged | | | yes |
| 342 | unsaved close offers the safe modal | unsaved-close modal (CAD-20 carries it) | kept-unchanged | | | yes |
| 347 | modal Cancel keeps accepted and draft state | unsaved-close modal | kept-unchanged | | | yes |
| 351 | cancelled-close window accepts a later draft update | MainWindow + controller | kept-unchanged | | | yes |
| 359 | no callback escapes after cleanup (`closeApproved` set by reflection) | MainWindow `closeApproved` field | kept-unchanged | | | yes |
| 387 | focus-readiness: controller field readable | MainWindow `workbench` field | ported · done (D3b, red run 45) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 389 | focus-readiness: `DocumentTabs` found | DocumentTabs → Dock document tabs | ported · done (D3b, red run 46) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 434 | readiness tab has a compositor | first DocumentTabs item | ported · done (D3b, red run 47) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 448 | readiness finds `FoilViewport` by name | FoilViewport → model-area view (pane namescope) | ported · done (D3b, red run 48) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 453 | opened fixture bound to the viewport before the barrier | FoilViewport | ported · done (D3b, red run 49) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 455 | readiness compositor not lost | first DocumentTabs item | ported · done (D3b, red run 50) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 458 | stale pre-fixture batch refused as a fresh barrier | first DocumentTabs item | ported · done (D3b, red run 51) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 513 | readiness result is `ready` (`closeApproved` set by reflection) | first DocumentTabs item, FoilViewport | ported · done (D3b, red run 52) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 694 | focus-hover reset button exists | ExampleButton (toolbar) → app-bar button | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 696 | focus-hover reset takes focus | ExampleButton → app-bar button | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 721 | TextBox probe has an owned draft identity | NumericInput rail draft → Span field (direct commit, no draft; clause dropped for Span) | ported · done (THEME, red run 39) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 724 | TextBox probe focus-reset button exists | ExampleButton → app-bar button | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 740 | TextBox state and authority (accepted id, draft id, generation) match | NumericInput, SourceText → Span field, Foil source tab | ported · done (THEME, red run 38) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 796 | typed replacement input applied | NumericInput → Span field | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 964 | theme barrier tab has a compositor | DocumentTabs item → Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 972 | theme barrier finds `FoilViewport` | FoilViewport → model-area view | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 977 | opened Example bound before the focus barrier | FoilViewport | ported · done (THEME, red run 36) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 979 | barrier tab receives keyboard focus | DocumentTabs item → Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 987 | barrier compositor not lost | DocumentTabs item → Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1018 | theme focus composition readiness is `ready` | DocumentTabs item, FoilViewport | ported · done (THEME, red run 37) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1032 | numeric-paint RED: `DocumentTabs` found | DocumentTabs → Dock document tabs (numeric-paint RED mode) | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` |  | no |
| 1035 | numeric-paint RED: controller field readable | MainWindow `workbench` field | kept-unchanged |  |  | yes |
| 1038 | numeric-paint RED: CV list found | ControlList → rail-editor pane (Ruling 55), found in the pane's namescope | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` |  | no |
| 1042 | numeric-paint RED: an editable authored CV exists | ControlList selection | kept-unchanged |  |  | yes |
| 1048 | numeric-paint RED: numeric field found | NumericInput → rail-editor pane (Ruling 55), found in the pane's namescope | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` |  | no |
| 1052 | numeric-paint RED: owned non-empty numeric field | NumericInput + rail draft, in the rail-editor pane (Ruling 55) | kept-unchanged |  |  | yes |
| 1057 | numeric-paint RED: TextBox sibling panel | NumericInput template (moves unchanged) | kept-unchanged |  |  | no |
| 1060 | numeric-paint RED: TextBox text-host sibling | NumericInput template (moves unchanged) | kept-unchanged |  |  | no |
| 1064 | numeric-paint RED: sibling paint order | NumericInput template (moves unchanged) | kept-unchanged |  |  | no |
| 1104 | numeric-paint RED: focus reset exists | ExampleButton → app-bar button | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` |  | no |
| 1118 | numeric-paint RED: text input replaces the selection | NumericInput, in the rail-editor pane (Ruling 55) | kept-unchanged |  |  | no |
| 1141 | focus-diagnostic: `DocumentTabs` found | DocumentTabs → Dock document tabs | ported · done (D3b, red run 46) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1146 | focus-diagnostic: tab takes keyboard focus | first DocumentTabs item → Dock tab | ported · done (D3b, red run 53) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1149 | focus-diagnostic: adorner layer present | first DocumentTabs item → Dock tab | ported · done (D3b, red run 54) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1156 | focus-diagnostic: two-ring adorner present | first DocumentTabs item → Dock tab | ported · done (D3b, red run 55) | `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1210 | pointer RED: `DocumentTabs` found | DocumentTabs "FoilDSL source" tab → "Foil source" Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1215 | pointer RED: controller field readable | MainWindow `workbench` field | ported · done (THEME, red run 35) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1219 | pointer RED: protected `PseudoClasses` readable | source tab → Foil source Dock tab | ported · done (THEME, red run 40) | `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1221 | pointer RED: `IPseudoClasses` readable | source tab → Foil source Dock tab | ported · done (THEME, red run 41) | `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1224 | pointer RED: tab position resolvable | source tab → Foil source Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1228 | pointer RED: PointerEntered sets `:pointerover` | source tab → Foil source Dock tab | ported · done (THEME, red run 42) | `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1277 | pointer RED: always throws (reproduces the HC selected-hover low contrast) | source tab → Foil source Dock tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1280 | matrix: toolbar button loads | ExampleButton → app-bar button | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1282 | matrix: document tabs load | DocumentTabs → Dock document tabs | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1284 | matrix: CV list loads | ControlList → rail-editor pane (Ruling 55), found in the pane's namescope | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` |  | no |
| 1286 | matrix: numeric field loads | NumericInput → Span field | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1288 | matrix: read-only source field loads | SourceText → Foil source tab (pane namescope) | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1290 | matrix: station list loads | StationList → Browser rows | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1291 | matrix: empty-state numeric field disabled | NumericInput → Span field absent or disabled with no foil ("No foil open", §11) | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1295 | matrix: controller field present | MainWindow `workbench` field | kept-unchanged | | | yes |
| 1297 | matrix: controller value present | MainWindow `workbench` field | kept-unchanged | | | yes |
| 1301 | matrix: exactly 3 tabs "Section sample", "FoilDSL source", "Section" | DocumentTabs → model-area Dock tabs (views, Foil source, section editor) | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1313 | matrix: Example station binds and selects | StationList → Browser row | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1315 | matrix: authored rails available for CV selection | ControlList precondition (controller) | kept-unchanged |  |  | yes |
| 1326 | locked CV creates no draft and leaves the numeric field disabled | ControlList + NumericInput → controller `BeginEdit` | ported · done (D3a, red run 9) | `Controller_LockedRailControl_RefusesDraft` | | yes |
| 1333 | editable CV opens an owned draft and enables the numeric field | ControlList + NumericInput, in the rail-editor pane (Ruling 55) | kept-unchanged |  |  | yes |
| 1348 | station item takes keyboard focus | StationList item → Browser row | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1353 | CV item takes keyboard focus | ControlList item, in the rail-editor pane (Ruling 55) | kept-unchanged |  |  | no |
| 1373 | sibling-only low-contrast mutation refused by the painter oracle | NumericInput → Span field | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1380 | viewport annotation present | ViewportProvenance → model-area view header | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1382 | section annotation present | SectionReadout ("Section sample" tab; placement open, §2 Q2) | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1391 | active source tab keeps read-only accepted text | SourceText → Foil source tab | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1397 | unsaved dialog method reachable | MainWindow `UnsavedDialogAsync` (private) | kept-unchanged | | | yes |
| 1399 | unsaved dialog task returned | MainWindow `UnsavedDialogAsync` | kept-unchanged | | | yes |
| 1401 | unsaved modal owned by the window | unsaved-close modal | kept-unchanged | | | yes |
| 1403 | unsaved modal inherits the owner theme | unsaved-close modal | kept-unchanged | | | yes |
| 1407 | unsaved modal body present | unsaved-close modal | kept-unchanged | | | yes |
| 1419 | modal Cancel is default, cancel and focused | unsaved-close modal | kept-unchanged | | | yes |
| 1422 | modal closes with safe Cancel | unsaved-close modal | kept-unchanged | | | yes |
| 1446 | applied theme matrix: no row failures | whole matrix | ported · done (THEME, red run 34) | `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1467 | review persona picks a real focus region (`numeric-or-open`, `stations`, `controls`) | `MainWindow.ReviewFocusTarget` → NumericInput, OpenButton, StationList, ControlList | ported · done (D3a, red run 25) | `Review_Persona_FocusesShellRegion` | | no |
| 1479 | fresh review draft gives its numeric value and unit | `MainWindow.DraftField` (NumericInput projection; the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1551 | minimum-window plot width ≥ 250 and dense annotation scrolls | 240 px Navigator + 300 px Properties columns | ported · done (D3a, red run 10) | `ModelArea_MinimumWindow_PlotWidthAtLeast250` | | no |
| 1669 | resumed recovery binds its draft value and unit | `MainWindow.DraftField` (resumed recovery edits in the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1687 | clause `MainWindow.TryDraftField(...) is not null` (the whole line is kept unchanged) | `MainWindow.TryDraftField` (NumericInput projection; the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1745 | ⌘Z / ⌘⇧Z on macOS, Ctrl+Z / Ctrl+Y on Windows | `MainWindow.AcceptedHistoryShortcut` (window `KeyDown`) → command table gestures | ported · done (D3b, red run 56) | `NativeMenu_MainWindow_BuiltFromTable` | | no |
| 1751 | F6 skips unavailable regions in both directions | `MainWindow.NextRegionIndex` → `FocusRing` (D1) | ported · done (D3b, red run 57) | `FocusRing_HiddenRegionsAndFloats_Order` | | no |
| 1757 (a) | F6 into the document region focuses the selected tab | `MainWindow.FocusCandidates` over DocumentTabs → Dock tabs | ported · done (D3a, red run 11) | `F6_RegionEntry_FocusesSelectedTabOrRow` | | no |
| 1757 (b) | Enter and Space re-edit the selected CV; Down does not | `MainWindow.IsReeditKey` (ControlList item `KeyDown`, `MainWindow.axaml.cs:262-266`; the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1799 | queued programmatic value neither advances an untouched draft nor blocks input | `NumericBindingGuard` (NumericInput; the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1802 | disabled unprojectable recovery is not invalid numeric input | `NumericBindingGuard` (NumericInput; the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |
| 1806 | F6 navigator region offers its focusable station item | `MainWindow.FocusCandidates` over StationList → Browser | ported · done (D3a, red run 11) | `F6_RegionEntry_FocusesSelectedTabOrRow` | | no |
| 1811 | same accepted identity keeps station AX items | `MainWindow.BindNavigatorItems` (StationList) → Browser rows | ported · done (D3a, red run 7) | `Browser_AcceptedIdentity_KeepsOrReplacesRows` | | no |
| 1814 | new accepted identity replaces stale station items | `MainWindow.BindNavigatorItems` → Browser rows | ported · done (D3a, red run 8) | `Browser_AcceptedIdentity_KeepsOrReplacesRows` | | no |
| 1827 | Cancel leaves no stale numeric text; the same selected CV can be re-edited | `MainWindow.AcceptedControlField`, `CanRestartSelectedEdit` (the rail-editor pane (Ruling 55)) | kept-unchanged |  |  | no |

## 2. Findings for the Coordinator (decision requests, not decided here)

All four questions are **ruled** by Ruling 55 (`docs/notes/rulings.md`, request
`req-01M3JBBB0AYHCG5PGC0K120VVF`). Design §1, §6.7, §12.4 and §12.5 are amended at `8c6260d`.

- **Q1 — ruled: keep the rail CV editor in a pane.** The Navigator `ControlList` and the Properties `NumericInput` with
  rail Preview, Apply and Cancel (`MainWindow.axaml:42`, `:159-167`) move unchanged into a pane in M1.2a, until M1.2b
  replaces them. A resumed recovery draft edits there, so the Resume hazard does not arise. The 22 rows first deleted
  under the opposite assumption are now kept-unchanged (17) or ported to `ThemeMatrix_ShellControls_AppliedContrast`
  (5: :1032, :1038, :1048, :1104, :1284, where a window-level lookup or the toolbar focus reset changes). Row :1669,
  the resumed-recovery `DraftField`, follows the editor into the pane and is kept unchanged.
- **Q2 — ruled: model-area tab.** The "Section sample" tab (`MainWindow.axaml:67-79`) becomes a model-area document
  tab beside "Foil source". Rows :1301 and :1382 stay ported.
- **Q3 — ruled: the count is 212.** The design owner corrected §1 and §12.5 from 213.
- **Q4 — ruled: the seven names are D3a names.** The `proposed:` markers are removed.

## 3. Assertions not bound to a removed control (120 lines, stay as they are)

These lines stay in `WorkbenchTests.cs` unchanged. Some of them are helpers that the ported theme suite calls, so they
move with it only if D3a relocates the suite. Their text does not change.

| Group | Lines | Count |
|---|---|---|
| self-launch failure probe | 21 | 1 |
| painter and contrast oracle helpers (control-agnostic) | 129, 142, 152, 155, 157, 160, 162, 167, 172, 175, 183, 186, 188, 192, 197, 199, 203, 206, 218, 237 | 20 |
| focus-placement negatives (pure) | 377 | 1 |
| `TextRow` helper (includes the source-text clip branch; SourceText stays read-only) | 521, 531, 532, 534, 539, 548, 549 | 7 |
| pointer and pseudo-class helpers | 559, 562, 568, 577, 590 | 5 |
| `InteractionRow` helper (includes the source clip branch) | 608, 609, 611, 615, 651 | 5 |
| `ProbeStates` keyboard-focus check | 699 | 1 |
| `ProbeTextBoxStates` generic checks (accepted Example, selection, focus border, caret) | 719, 751, 756, 758, 762 | 5 |
| `FocusRow` helper | 829, 856, 861, 862, 868, 872, 877, 881, 883, 908, 912, 917, 923, 924 | 14 |
| theme key | 1023 | 1 |
| native review options and controller review states | 1458, 1462, 1470, 1476, 1484, 1491, 1498, 1508, 1514, 1520 | 10 |
| viewport semantics and certification (standalone `Viewport`, controller) | 1539, 1543, 1548, 1552, 1553 | 5 |
| controller: open refusals, Preview, Cancel, Apply, Undo, Redo, save conflict, uncertain save, late save | 1558, 1566, 1574, 1575, 1577, 1579, 1584, 1590, 1593, 1595, 1602, 1603, 1605, 1616, 1618, 1621, 1636 | 17 |
| controller: recovery save, reopen and resume (DraftField lines excluded) | 1651, 1656, 1658, 1664, 1673, 1688, 1692 | 7 |
| controller: tip-side retarget, invalid numeric input | 1708, 1713, 1717, 1727, 1728, 1733, 1738 | 7 |
| viewport frame revision; `NativeRenderCorrelation`, `NativeMetricRecord` (pure) | 1765, 1768, 1772, 1777, 1782, 1787, 1792 | 7 |
| controller repeated edit | 1829 | 1 |
| loaded `Styles.axaml` theme and focus brushes, shadow mutation | 1862, 1879, 1888 | 3 |
| test doubles (`CapturingStore`, `DelayedStore`; `:1936` is a `NotSupportedException` stub, not an assertion) | 1914, 1933, 1936 | 3 |
| **total** | | **120** |

**Coverage (measured).** §1 holds 92 distinct lines, and this section holds 120. They do not overlap, and together
they are exactly the 212 grep lines. Line :1687 is counted in §1 only: one clause is deleted and the rest of the line
stays.

`NativeRenderCorrelation`, `NativeMetricRecord` and `NumericBindingGuard` are declared in `MainWindow.axaml.cs`
(`:1235`, `:1259`, `:1269`). D3a owns that file. If D3a moves the first two, their assertions (:1772-1792) stay
unchanged against the new location.

## 4. Reflection-bound rows (each needs its own red run when ported or re-verified — Ruling 54 P3)

These rows reach private members of `MainWindow` by reflection, or run on a controller obtained that way. D3a
rewrites `MainWindow.axaml(.cs)`. A renamed field fails loudly: the lookups throw or hit the `!` operator. Each
row therefore needs a recorded red run against the rewritten window, not only a green one.

| Member | Rows |
|---|---|
| `workbench` (field), directly or through the controller it yields | kept: 284, 328, 334, 342, 347, 351 (closed-callback); 1035, 1042, 1052 (numeric-paint RED); 1295, 1297, 1315, 1333 (matrix). Ported: 387, 453, 513 (focus-readiness); 721, 740 (TextBox probe); 977, 1018 (theme barrier); 1215 (pointer RED); 1326 (locked CV); 1446 (matrix) |
| `closeApproved` (field) | 359; also the teardown before 513 and after every theme window |
| `UnsavedDialogAsync` (method) | kept: 1397, 1399, 1401, 1403, 1407, 1419, 1422 |
| Avalonia `PseudoClasses` (framework, stable) | ported: 1219, 1221, 1228 |

**Count (measured by the §3 cross-check).** 31 rows have reflection? = yes, and 3 have framework. All 31 yes rows
are live after Ruling 55: 21 kept and 10 ported. Each needs a red run against the rewritten window.
