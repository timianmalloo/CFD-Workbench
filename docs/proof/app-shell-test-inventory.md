---
id: proof-app-shell-test-inventory
title: App-shell test inventory — WorkbenchTests.cs assertions bound to controls the shell removes or changes
type: proof-pack
status: in-review
owner: "@track-d3a"
phase: implementation — checkpoint D3a-0
tags: [app-shell, desktop, d3a, test-inventory, harness-migration, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2026-10-27
summary: >-
  Checkpoint D3a-0 (design §12.5). Every throw-new assertion in tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs
  (212, measured) is classified. 93 rows for the 92 lines bound to a control in MainWindow.axaml(.cs) that the
  shell removes, moves or rewrites: ported (with a D3a, D1 or proposed name), deleted (with the spec Appendix G
  clause) or kept unchanged (reflection-bound). The other 120 lines are not bound to a removed control and stay.
---

# App-shell test inventory (checkpoint D3a-0)

**Result.** `tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs` holds **212** `throw new` lines (grep, at `bf54a54`).
The design's figure of 213 (§1, §12.5) is one high. The design counted at `494aef2`. Commit `d27dc03` then removed two
child-exit throws and added the self-launch failure probe (`:21`), so the count is 212. **92 lines are bound** to a
control that the shell removes, moves or rewrites. Line `:1757` holds two assertions with different dispositions, so the
table has **93 rows**. The other **120 lines** are not bound to a removed control, and they stay as they are (§3).

| Disposition | Rows |
|---|---|
| ported | 55 |
| deleted | 22 |
| kept-unchanged (reflection-bound, inside a ported or kept block) | 16 |
| **total** | **93** |

**Method (measured).** The line numbers come from `grep -n 'throw new'` on the file at `bf54a54`. Each line was read in
context and bound to the control it reaches. The controls come from `src/CfdWorkbench.Desktop/MainWindow.axaml` and
the statics in `MainWindow.axaml.cs`. What the shell removes, keeps or moves comes from design §6.7, §8 and §11,
§3.7 ("Point rows arrive with M1.2b"), spec B7 (the CAD-first window) and the spec Appendix G superseded table.

**Name sources.** A ported name is a D3a name from design §9/§12.4 where one fits. Where none fits, the name is marked
`proposed:`. The Coordinator adds it to the design after an Owner ruling. This file does not edit the design. One
design name belongs to D1: `FocusRing_HiddenRegionsAndFloats_Order`. `NextRegionIndex` moves into D1's `FocusRing`
(design §5.1), so its assertion moves with it. `tools/check-named-tests.py D3a` requires a `PASS` for every name in
the `ported` column, so D3a's run must include D1's merged `--shell-model` suite.

**Appendix G clauses cited (spec `docs/specs/cfd-workbench-v1.md` §Appendix G, "Superseded").**

- **G-B7:** "B7's Navigator dock, right-hand Properties dock, palette, options strip and four-viewport default (CAD
  area)" → superseded by "B7's CAD-first window".
- **G-CAD04:** "CAD-04 'opens the one draft' for point gestures" → superseded by "DR-6 default, A3.2". What stands:
  "elevations, lock refusals, focus ring, accessible values".

**Proposed names (need an Owner ruling before D3a codes them).**

| Proposed name | Covers |
|---|---|
| `ThemeMatrix_ShellControls_AppliedContrast` | the `--theme-controls` applied-theme matrix (run by `tools/verify-application-adapters.py:644`), retargeted: toolbar → app-bar button, `DocumentTabs` → Dock document tabs, station rows → Browser rows, numeric TextBox → Wing **Span** field, `SourceText` → "Foil source" tab; the pointer-RED repro mode |
| `DockTabFocus_FreshBatch_ReadyAndTwoRing` | the `--focus-readiness` and `--focus-diagnostic` modes, retargeted from the first `DocumentTabs` item to the first Dock document tab |
| `F6_RegionEntry_FocusesSelectedTabOrRow` | `MainWindow.FocusCandidates` for the document-tab region and the station region → Dock tab and Browser row |
| `Browser_AcceptedIdentity_KeepsOrReplacesRows` | `MainWindow.BindNavigatorItems` → Browser pane rows (same identity keeps AX items; new identity replaces them) |
| `Review_Persona_FocusesShellRegion` | `MainWindow.ReviewFocusTarget`, whose `numeric-or-open` / `stations` / `controls` targets are removed |
| `ModelArea_MinimumWindow_PlotWidthAtLeast250` | `Viewport.PlotWidth(1024 - 240 - 300 - 24, 178)`: the constants are the removed 240 px Navigator and 300 px Properties columns |
| `Controller_LockedRailControl_RefusesDraft` | the locked-CV no-draft assertion. G-CAD04 keeps "lock refusals", so it cannot be deleted. It is retargeted to `WorkbenchController.BeginEdit` → `DSL-LOCK` (`WorkbenchController.cs:254`), which no Desktop assertion covers today |

## 1. Inventory

Column key: `ported` is the new test name, empty when the row is deleted or kept. `superseded clause` is filled for
deleted rows. `reflection?` is **yes** when the assertion reaches our private members (`workbench`, `closeApproved`,
`UnsavedDialogAsync`) by reflection, directly or through a controller obtained that way. It is **framework** when the
reflection is Avalonia's protected `PseudoClasses`.

| WorkbenchTests.cs line | assertion (short) | control | disposition | ported | superseded clause | reflection? |
|---|---|---|---|---|---|---|
| 113 | frozen 18+60+21 per-theme row table (toolbar, tab, station, cv, numeric rows) | ExampleButton, DocumentTabs, StationList, ControlList, NumericInput, SourceText | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 284 | closed-callback repro: controller field readable | MainWindow `workbench` field | kept-unchanged | | | yes |
| 328 | closed window callback does not escape; next window stays open | MainWindow close, second window | kept-unchanged | | | yes |
| 334 | next window publishes its accepted update | MainWindow + controller (via :281-284) | kept-unchanged | | | yes |
| 342 | unsaved close offers the safe modal | unsaved-close modal (CAD-20 carries it) | kept-unchanged | | | yes |
| 347 | modal Cancel keeps accepted and draft state | unsaved-close modal | kept-unchanged | | | yes |
| 351 | cancelled-close window accepts a later draft update | MainWindow + controller | kept-unchanged | | | yes |
| 359 | no callback escapes after cleanup (`closeApproved` set by reflection) | MainWindow `closeApproved` field | kept-unchanged | | | yes |
| 387 | focus-readiness: controller field readable | MainWindow `workbench` field | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 389 | focus-readiness: `DocumentTabs` found | DocumentTabs → Dock document tabs | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 434 | readiness tab has a compositor | first DocumentTabs item | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 448 | readiness finds `FoilViewport` by name | FoilViewport → model-area view (pane namescope) | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 453 | opened fixture bound to the viewport before the barrier | FoilViewport | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 455 | readiness compositor not lost | first DocumentTabs item | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 458 | stale pre-fixture batch refused as a fresh barrier | first DocumentTabs item | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 513 | readiness result is `ready` (`closeApproved` set by reflection) | first DocumentTabs item, FoilViewport | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | yes |
| 694 | focus-hover reset button exists | ExampleButton (toolbar) → app-bar button | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 696 | focus-hover reset takes focus | ExampleButton → app-bar button | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 721 | TextBox probe has an owned draft identity | NumericInput rail draft → Span field (direct commit, no draft; clause dropped for Span) | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 724 | TextBox probe focus-reset button exists | ExampleButton → app-bar button | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 740 | TextBox state and authority (accepted id, draft id, generation) match | NumericInput, SourceText → Span field, Foil source tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 796 | typed replacement input applied | NumericInput → Span field | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 964 | theme barrier tab has a compositor | DocumentTabs item → Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 972 | theme barrier finds `FoilViewport` | FoilViewport → model-area view | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 977 | opened Example bound before the focus barrier | FoilViewport | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 979 | barrier tab receives keyboard focus | DocumentTabs item → Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 987 | barrier compositor not lost | DocumentTabs item → Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1018 | theme focus composition readiness is `ready` | DocumentTabs item, FoilViewport | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1032 | numeric-paint RED: `DocumentTabs` found | DocumentTabs | deleted | | G-B7 (the mode exists only to paint-probe the right-hand Properties NumericInput) | no |
| 1035 | numeric-paint RED: controller field readable | MainWindow `workbench` field | deleted | | G-B7 | yes |
| 1038 | numeric-paint RED: CV list found | ControlList (Navigator) | deleted | | G-B7 | no |
| 1042 | numeric-paint RED: an editable authored CV exists | ControlList selection | deleted | | G-B7; G-CAD04 | yes |
| 1048 | numeric-paint RED: numeric field found | NumericInput | deleted | | G-B7 | no |
| 1052 | numeric-paint RED: owned non-empty numeric field | NumericInput + rail draft | deleted | | G-B7; G-CAD04 | yes |
| 1057 | numeric-paint RED: TextBox sibling panel | NumericInput template (still asserted by kept helper :183-192) | deleted | | G-B7 | no |
| 1060 | numeric-paint RED: TextBox text-host sibling | NumericInput template (kept helper :183-192) | deleted | | G-B7 | no |
| 1064 | numeric-paint RED: sibling paint order | NumericInput template (kept helper :183-192) | deleted | | G-B7 | no |
| 1104 | numeric-paint RED: focus reset exists | ExampleButton | deleted | | G-B7 | no |
| 1118 | numeric-paint RED: text input replaces the selection | NumericInput | deleted | | G-B7 | no |
| 1141 | focus-diagnostic: `DocumentTabs` found | DocumentTabs → Dock document tabs | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1146 | focus-diagnostic: tab takes keyboard focus | first DocumentTabs item → Dock tab | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1149 | focus-diagnostic: adorner layer present | first DocumentTabs item → Dock tab | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1156 | focus-diagnostic: two-ring adorner present | first DocumentTabs item → Dock tab | ported | proposed: `DockTabFocus_FreshBatch_ReadyAndTwoRing` | | no |
| 1210 | pointer RED: `DocumentTabs` found | DocumentTabs "FoilDSL source" tab → "Foil source" Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1215 | pointer RED: controller field readable | MainWindow `workbench` field | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1219 | pointer RED: protected `PseudoClasses` readable | source tab → Foil source Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1221 | pointer RED: `IPseudoClasses` readable | source tab → Foil source Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1224 | pointer RED: tab position resolvable | source tab → Foil source Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1228 | pointer RED: PointerEntered sets `:pointerover` | source tab → Foil source Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | framework |
| 1277 | pointer RED: always throws (reproduces the HC selected-hover low contrast) | source tab → Foil source Dock tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1280 | matrix: toolbar button loads | ExampleButton → app-bar button | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1282 | matrix: document tabs load | DocumentTabs → Dock document tabs | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1284 | matrix: CV list loads | ControlList (Navigator) | deleted | | G-B7 | no |
| 1286 | matrix: numeric field loads | NumericInput → Span field | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1288 | matrix: read-only source field loads | SourceText → Foil source tab (pane namescope) | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1290 | matrix: station list loads | StationList → Browser rows | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1291 | matrix: empty-state numeric field disabled | NumericInput → Span field absent or disabled with no foil ("No foil open", §11) | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1295 | matrix: controller field present | MainWindow `workbench` field | kept-unchanged | | | yes |
| 1297 | matrix: controller value present | MainWindow `workbench` field | kept-unchanged | | | yes |
| 1301 | matrix: exactly 3 tabs "Section sample", "FoilDSL source", "Section" | DocumentTabs → model-area Dock tabs (views, Foil source, section editor) | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1313 | matrix: Example station binds and selects | StationList → Browser row | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1315 | matrix: authored rails available for CV selection | ControlList precondition | deleted | | G-B7; G-CAD04 | yes |
| 1326 | locked CV creates no draft and leaves the numeric field disabled | ControlList + NumericInput → controller `BeginEdit` | ported | proposed: `Controller_LockedRailControl_RefusesDraft` | | yes |
| 1333 | editable CV opens an owned draft and enables the numeric field | ControlList + NumericInput | deleted | | G-B7; G-CAD04 (controller half kept at :1571-1584) | yes |
| 1348 | station item takes keyboard focus | StationList item → Browser row | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1353 | CV item takes keyboard focus | ControlList item | deleted | | G-B7 (G-CAD04 "focus ring" stands for view points: M1.2b) | no |
| 1373 | sibling-only low-contrast mutation refused by the painter oracle | NumericInput → Span field | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1380 | viewport annotation present | ViewportProvenance → model-area view header | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1382 | section annotation present | SectionReadout ("Section sample" tab; placement open, §2 Q2) | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1391 | active source tab keeps read-only accepted text | SourceText → Foil source tab | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | no |
| 1397 | unsaved dialog method reachable | MainWindow `UnsavedDialogAsync` (private) | kept-unchanged | | | yes |
| 1399 | unsaved dialog task returned | MainWindow `UnsavedDialogAsync` | kept-unchanged | | | yes |
| 1401 | unsaved modal owned by the window | unsaved-close modal | kept-unchanged | | | yes |
| 1403 | unsaved modal inherits the owner theme | unsaved-close modal | kept-unchanged | | | yes |
| 1407 | unsaved modal body present | unsaved-close modal | kept-unchanged | | | yes |
| 1419 | modal Cancel is default, cancel and focused | unsaved-close modal | kept-unchanged | | | yes |
| 1422 | modal closes with safe Cancel | unsaved-close modal | kept-unchanged | | | yes |
| 1446 | applied theme matrix: no row failures | whole matrix | ported | proposed: `ThemeMatrix_ShellControls_AppliedContrast` | | yes |
| 1467 | review persona picks a real focus region (`numeric-or-open`, `stations`, `controls`) | `MainWindow.ReviewFocusTarget` → NumericInput, OpenButton, StationList, ControlList | ported | proposed: `Review_Persona_FocusesShellRegion` | | no |
| 1479 | fresh review draft gives its numeric value and unit | `MainWindow.DraftField` (NumericInput projection) | deleted | | G-B7; G-CAD04 | no |
| 1551 | minimum-window plot width ≥ 250 and dense annotation scrolls | 240 px Navigator + 300 px Properties columns | ported | proposed: `ModelArea_MinimumWindow_PlotWidthAtLeast250` | | no |
| 1669 | resumed recovery binds its draft value and unit | `MainWindow.DraftField` (NumericInput projection) | deleted | | G-B7; G-CAD04 (controller half kept at :1673) | no |
| 1687 | clause `MainWindow.TryDraftField(...) is not null` only; the rest of the line is kept unchanged | `MainWindow.TryDraftField` (NumericInput projection) | deleted | | G-B7; G-CAD04 | no |
| 1745 | ⌘Z / ⌘⇧Z on macOS, Ctrl+Z / Ctrl+Y on Windows | `MainWindow.AcceptedHistoryShortcut` (window `KeyDown`) → command table gestures | ported | `NativeMenu_MainWindow_BuiltFromTable` | | no |
| 1751 | F6 skips unavailable regions in both directions | `MainWindow.NextRegionIndex` → `FocusRing` (D1) | ported | `FocusRing_HiddenRegionsAndFloats_Order` | | no |
| 1757 (a) | F6 into the document region focuses the selected tab | `MainWindow.FocusCandidates` over DocumentTabs → Dock tabs | ported | proposed: `F6_RegionEntry_FocusesSelectedTabOrRow` | | no |
| 1757 (b) | Enter and Space re-edit the selected CV; Down does not | `MainWindow.IsReeditKey` (ControlList item `KeyDown`, `MainWindow.axaml.cs:262-266`) | deleted | | G-B7; G-CAD04 | no |
| 1799 | queued programmatic value neither advances an untouched draft nor blocks input | `NumericBindingGuard` (NumericInput) | deleted | | G-B7 (class removed with its control, HYG-A) | no |
| 1802 | disabled unprojectable recovery is not invalid numeric input | `NumericBindingGuard` (NumericInput) | deleted | | G-B7 | no |
| 1806 | F6 navigator region offers its focusable station item | `MainWindow.FocusCandidates` over StationList → Browser | ported | proposed: `F6_RegionEntry_FocusesSelectedTabOrRow` | | no |
| 1811 | same accepted identity keeps station AX items | `MainWindow.BindNavigatorItems` (StationList) → Browser rows | ported | proposed: `Browser_AcceptedIdentity_KeepsOrReplacesRows` | | no |
| 1814 | new accepted identity replaces stale station items | `MainWindow.BindNavigatorItems` → Browser rows | ported | proposed: `Browser_AcceptedIdentity_KeepsOrReplacesRows` | | no |
| 1827 | Cancel leaves no stale numeric text; the same selected CV can be re-edited | `MainWindow.AcceptedControlField`, `CanRestartSelectedEdit` (NumericInput, ControlList) | deleted | | G-B7; G-CAD04 (controller re-edit kept at :1829) | no |

## 2. Findings for the Coordinator (decision requests, not decided here)

- **Q1 — the rail CV editor has no M1.2a home. Consequential.** Design §3.7 says "Point rows arrive with M1.2b", and
  §6.7 moves only the section panel (`MainWindow.axaml:82-145`). The Navigator `ControlList` and the Properties
  `NumericInput` with rail Preview, Apply and Cancel (`MainWindow.axaml:42`, `:159-167`) have no placement. The 22
  deleted rows assume they are removed in M1.2a under G-B7 and G-CAD04.
  `assume:` M1.2a removes the rail CV editor. Confirmed by an Owner ruling. If false, the rows at
  :1032-1118, :1284, :1315, :1333, :1353, :1479, :1669, :1687, :1757 (b), :1799, :1802 and :1827 become
  kept-unchanged or ported, and the rail editor moves unchanged into a pane, as §6.7 does for the section panel.
  **Hazard if true:** the recovery alert band (§6.7) still offers **Resume**. A resumed rail draft then has no UI to
  edit, Preview, Apply or Cancel it until M1.2b ships. The controller path stays tested (:1552-1738, kept).
- **Q2 — the "Section sample" tab has no placement.** `MainWindow.axaml:67-79` holds SectionPosition, SectionViewport
  and SectionReadout. Design §6.7 places the section editor ("Section" tab) and "Foil source", but not this tab. Rows
  :1301 and :1382 are ported. `assume:` it becomes a model-area document tab beside "Foil source". If false, :1382 is
  deleted, but no Appendix G clause covers that deletion. So it needs a ruling, not a silent drop.
- **Q3 — the design count.** §1 and §12.5 say 213. The measured count is 212, for the `d27dc03` reason given above.
  This is a correction for the design owner. The inventory does not edit the design.
- **Q4 — seven proposed names.** No D3a design name fits them. They are listed above for the Owner ruling.

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
| `workbench` (field), directly or through the controller it yields | kept: 284, 328, 334, 342, 347, 351 (closed-callback); 1295, 1297 (matrix). Ported: 387, 453, 513 (focus-readiness); 721, 740 (TextBox probe); 977, 1018 (theme barrier); 1215 (pointer RED); 1326 (locked CV); 1446 (matrix). Deleted: 1035, 1042, 1052, 1315, 1333 |
| `closeApproved` (field) | 359; also the teardown before 513 and after every theme window |
| `UnsavedDialogAsync` (method) | kept: 1397, 1399, 1401, 1403, 1407, 1419, 1422 |
| Avalonia `PseudoClasses` (framework, stable) | ported: 1219, 1221, 1228 |

**Count (measured by the §3 cross-check).** 31 rows have reflection? = yes, and 3 have framework. The live yes rows
are 26: 16 kept and 10 ported. Each needs a red run against the rewritten window. The 5 deleted yes rows need none.
