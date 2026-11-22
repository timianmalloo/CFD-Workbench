---
id: coordination-wave7-ui-polish-and-at-proof
title: "Coordination plan - wave 7: Desktop UI P1/P2 polish, Sweeps/Setup theme reactivity, and Windows UIA native proof"
type: plan
status: active
owner: "@timianmalloo"
tags: [coordination, worktrees, ui-polish, accessibility, uia, avalonia, wave7]
links:
  - { to: review-ui-as-built-workbench, rel: implements }
  - { to: coordination-wave6-and-area5-wsl, rel: refines }
  - { to: design-language, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-22"
summary: >-
  Wave 7 closes all remaining UI review P1 Majors (F-18..F-29 incl. F-23 200% modal scaling), P2 Minors/Nits
  (F-32..F-46), Wave 6 carry-forward live theme reactivity on PerformanceCurveChart and PlanformPreviewControl,
  a new UI-CS-TOKEN static test guard, and real OS-level Windows UIA (UIAutomationCore.dll) native tree verification.
  Per GO5/GO6, because every item touches CfdWorkbench.Desktop and the real-display Desktop harness, Wave 7 runs as
  three serial, isolated share-workspace tracks (W7-SHELL -> W7-CAD -> W7-ANA-AT) at width 1 for zero merge conflicts
  and zero Desktop harness timeouts.
---

# Coordination plan: wave 7 — Desktop UI P1/P2 polish & Windows UIA native proof

**Input.** Operator directive of 2026-11-22 following Wave 6 push (`0b578678`) and **Ruling 273** (`6da064d4`): plan and
execute **all of item (4)** — every remaining UI review P1 Major (`F-18`–`F-29`, including `F-23` 200% modal scaling),
every remaining P2 Minor/Nit (`F-32`–`F-46`), Wave 6 carry-forward live `ActualThemeVariantChanged` repainting on
`PerformanceCurveChart` and `PlanformPreviewControl`, plus automated Windows native UIA accessibility-tree and expanded
`--theme-matrix` verification — using `/prepare-for-coordination` and `/execute-with-coordination --swarm`.

**Goal state.**
- **Goal:** Elevate the Avalonia Desktop workbench from "blockers fixed" (Wave 6 `UI-P0`) to full P1/P2 craft,
  responsiveness, Windows/macOS HIG, async UI-thread hygiene, and real OS-level Windows UIA tree proof across all
  surfaces (`Start`, `Setup`, `CAD`, `Section Editor`, `Analysis`, `Sweeps`, and all modal dialogs).
- **Done when:** All three tracks (`W7-SHELL`, `W7-CAD`, `W7-ANA-AT`) pass red-first tests and mutants, clear
  independent Adversary review (`w7-verifier`: UX & Accessibility hard veto + Test Architect hard veto + Native Desktop
  + Simplifier), merge serially onto `main` via `conductor-join.py` (`--no-push`) with `TESTS-GREEN`,
  `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `check-docs.py` exit 0, `10/10`
  `run-verify-gates.py` PASS, live Windows `IUIAutomation` COM tree verification recorded, and
  `docs/reviews/ui-as-built-workbench.{md,html}` updated with the post-Wave-7 status & remaining Flagged obligations.
- **Not in scope:** Installing NVDA or running macOS live display/VoiceOver captures (this host is Windows ARM64 with
  no `nvda.exe` installed — live NVDA speech, live macOS VoiceOver, and Authenticode/Gatekeeper signing stay honestly
  `Flagged`); the 27 remaining Wave 2 proposed `COPY-*` rows (`COPY-611..633`, `COPY-591..594`) not included in Ruling
  273 (carried forward explicitly in the review addendum for future operator ruling); Area 5 OpenFOAM
  `--mode openfoam-resume` (network-dependent); Area 7 `SPIKE-CAD-02` commercial CAM runs; pushing Wave 7 commits to
  `origin` without operator confirmation.
- **Tier:** T2 · **Fan-out cap:** 1 concurrent author (deliberate Simplifier/GO5 serialization across 3 isolated
  share-workspace tracks; zero file overlap, zero Desktop display-harness contention).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w7-coord`). Owns plan, seams, join order, `conductor-join.py` merges, and
  ruling commits. Never authors track code.
- **Owner:** Independent swarm worker `w7-owner` (`workspace: "inherit"`, read-only on `src/`), spawned only if a
  mid-wave decision request arises (zero open DRs at Wave 7 start — Ruling 273 closed all Wave 6 DRs and copy rows).
- **Adversary Verifier:** Independent swarm worker `w7-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track's committed diff, red-first tests, mutants, UIA/contrast proofs, and doc updates in Adversary Mode before join.
- **Stall Monitor:** Existing swarm worker `monitor` (`workspace: "inherit"`, `swarm_monitor` skill loaded).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3, 5, and 6).

## Layer state

Measured 2026-11-22 15:37Z–15:41Z by `python docs/ai-forward-pack/scripts/coord-core.py doctor`,
`python docs/ai-forward-pack/scripts/coord-core.py session list`, `git worktree list`, and native Windows UIA/NVDA
probes in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `6da064d4`, clean).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| heartbeat | `1 session(s) beating; newest beat 3539641 s ago; 0 stalled, 0 live, 0 blocked, 0 done` | clean start |
| sessions | `0 active session(s); 247 file(s) scanned` | clean start |
| requests | `ok - 41 request(s), 0 open, 41 terminal` | zero open decision requests |
| lease overlap | `none (0 live lease(s))` | clean start |
| worktrees | `1 (D:/Projects/CFD-Workbench 6da064d4 [main])` | primary only |
| Windows UIA / NVDA host probe | `C:\Windows\System32\UIAutomationCore.dll` present & loaded (`True`); `nvda.exe` absent (`False`) | real OS-level `IUIAutomation` COM tree verification is available natively on this Windows 11 host; live NVDA speech remains honestly `Flagged` |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifier/monitor) | each track gets a clean linked working copy; `git config core.longpaths true` active |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token wakeup; `monitor` attached |
| agy ownership hooks (`.agents/hooks.json`) | `observed-only` | `pre-commit` floor + coordinator diff inspection enforce path boundaries |
| pack CLI mail doorbells (`coord-mail.py`) | `unsupported` | board `@mention` + `swarm_wait` is the sole enforced wakeup mechanism |

The coordination layer is installed **once, in the primary checkout** (`D:/Projects/CFD-Workbench`). Every share
workspace created by `swarm{action:"add", workspace:"share"}` **inherits** `.git/config` merge drivers and `.git/hooks`
from the primary checkout — workers verify inherited drivers via `git config --get-regexp 'merge\..*\.driver'` and
**never** run `coord install` inside a share workspace.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | `coord-register` union merge | **no** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen`: `audit-log.py --root docs --project CFD-Workbench render` | **no** |
| `docs/docs-index.js` | derived | `coord-regen`: `docs-graph.py derive` | **no** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | `coord-register` union merge | **no** |
| `docs/lessons/defect-classes.md` | register | `defect-register` (`tools/merge-defect-register.py`) | **no** |
| `docs/notes/rulings.md` | authored, single writer | Coordinator / Owner only (no Wave 7 ruling expected) | serial |
| `DESIGN.md` (§5 shell reconciliation) | authored, single owner | `W7-SHELL` only (updates §5 shell description & StatusStrip row to match built Dock + `Setup \| CAD \| Analysis \| Sweeps` shell; zero changes to approved `COPY-*` text) | single owner (`W7-SHELL`) |
| `src/CfdWorkbench.Desktop/Styles.axaml`, `MainWindow.axaml(.cs)`, `Shell/*`, `Panes/StartView*`, `ExportDialog*`, `App.axaml(.cs)` | authored, serial across waves | `W7-SHELL` owns in Wave 7A; `W7-CAD` / `W7-ANA-AT` rebase on joined `main` | serial (`W7-SHELL` → `W7-CAD` → `W7-ANA-AT`) |
| `src/CfdWorkbench.Desktop/{ModelArea*,PlanCanvas.cs,ElevationView.cs,View3d.cs,CombPlate*,SectionEditorView*,SectionCanvas.cs,CatalogDialog*,SaveSectionDialog*,RebuildPopover*,PropertiesView.cs,PointsView.cs,WorkbenchController.cs,PointContextMenu.cs (new),Shell/ShellHost.cs,Panes/PropertiesPane*,Panes/BrowserPane*,Panes/PointsPane*}` | authored, serial | `W7-CAD` owns in Wave 7B (after `W7-SHELL` joins) | serial |
| `src/CfdWorkbench.Desktop/Analysis/**`, `src/CfdWorkbench.Desktop/ElevationView.cs` (only if `ElevationDepthLayer.Draw` takes `Units`), `src/CfdWorkbench.Desktop/Sweeps/PerformanceCurveChart.cs`, `src/CfdWorkbench.Desktop/Setup/PlanformPreviewControl.cs`, `src/CfdWorkbench.Analysis/{SectionDisplay.cs,AnalysisProjection.cs,Labels.cs}`, `docs/reviews/ui-as-built-workbench.{md,html}` | authored, serial | `W7-ANA-AT` owns in Wave 7C (after `W7-CAD` joins) | serial |
| `tests/CfdWorkbench.Desktop.Tests/**` and `tests/CfdWorkbench.Analysis.Tests/DxSectionTests.cs` | authored, serial | strictly one owner per wave (serial at width 1; tracks may update existing component test suites when refactored behavior changes) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- Strictly **one** author active per wave (`W7-SHELL` → `W7-CAD` → `W7-ANA-AT`): zero concurrent writers on `src/`,
  `tests/`, `Styles.axaml`, `DESIGN.md`, or the real-display Desktop harness.
- `xaml-token-lint.py` & `check-event-subscribers.py` (`docs/coordination/join.json`): root `src/CfdWorkbench.Desktop/`;
  recursion `*.axaml` / `*.cs` excluding `bin/`/`obj/`; token set raw hex/brush/size literals in `.axaml` and event
  subscription balance; allowlist none for `xaml-token-lint.py`, 4-entry `ALLOWED` set in `tools/check-event-subscribers.py`
  (`CanExecuteChanged`, `VertexSelected`, `VertexMoved`, `FocusedTargetChanged`). Every wave runs both and leaves 0
  findings: jointly satisfiable.
- `design-lint.py DESIGN.md --strict`: root `DESIGN.md`; recursion none; token set `{token.ref}` references; allowlist
  none. `W7-SHELL` preserves all token definitions and approved `COPY-*` rows verbatim: jointly satisfiable.
- `tools/check-reader-sharing.py` & `tools/coordination/check-test-paths.py`: root `src/` (`**/*.cs`, `UserFile.OpenRead`)
  and `tests/CfdWorkbench.Core.Tests/` (`*.cs`, zero relative `"docs/..."` reads); allowlist none: jointly satisfiable.
- `PROOF-PII` & `verify-no-machine-paths.py`: root all tracked & untracked non-ignored text files; token set
  `%USERPROFILE%`, machine SIDs, hostnames, raw machine paths; allowlist 1 existing file in `check-proof-pii.py`.
  `W7-ANA-AT`'s Windows UIA proof and updated `ui-as-built-workbench.{md,html}` scrub all hostnames/user paths and commit
  zero binary captures: jointly satisfiable.
- `UI-CS-TOKEN` static guard (added in `W7-ANA-AT`): root `src/CfdWorkbench.Desktop/`; recursion `**/*.cs` excluding
  `bin/`/`obj/`; token set `TryFindResource`/`FindResource`/`ResolveBrush`/`Brush` string resource keys (must exist in
  `Styles.axaml`/`App.axaml`), hardcoded `FontSize = <n>` or `DrawText`/`FormattedText` size literals `< 11`, and raw
  `Color.Parse(...)` hexes; allowlist documented empty or explicit non-theme constants only. Added in Wave 7C *after*
  `W7-SHELL` and `W7-CAD` finish cleaning C# styling: jointly satisfiable.

**Quoted ADR / Ruling trigger lines (DC-189 — exact citations, never paraphrased).**
- **ADR-0010 §Decision (4)** (`docs/adr/0010-one-placement-rule.md:81-83`):
  > "4. **Displays receive geometry, never compute it.** Core exposes `Placement.Surface` (a placed mesh of the starboard
  > half) and `Placement.Frame` (channel values at η). The Desktop applies only a camera and the language's symmetry map
  > `(x, y, z) → (x, −y, z)` (`Point3.Port()`, defined in Core)."
- **Ruling 271** (`docs/notes/rulings.md:2025`):
  > "DR-SWP-6: option (a) approved. Add a Sweeps workspace segment beside CAD | Analysis in the existing ModelArea navbar
  > (CAD | Analysis | Sweeps), containing three discrete sub-tabs (Experiment setup, Run (local), Results) enforcing XS-03
  > / UX-17 (zero Run button in Experiment setup; Queue hands off to Run (local)), dockable onto future 7-area rail slots
  > 4-6 without view rewrite."
- **Ruling 272** (`docs/notes/rulings.md:2031`):
  > "SET-BUILD integrates Setup as the non-modal first workspace segment in the existing navbar (Setup | CAD | Analysis |
  > Sweeps) plus a CommandTable and View-menu entry (and StartView entry path) switching the center workspace in place while
  > preserving CAD selection and camera (UX-04, UX-19), without requiring an unbuilt activity rail and without using a modal
  > dialog (option (b) rejected)."
- **Ruling 273** (`docs/notes/rulings.md:2037`):
  > "All 52 proposed copy rows in DESIGN.md — Compatible Fit COPY-686..693 (8 rows, with COPY-692 and COPY-693 retained
  > as approved reserved), Area 4 Sweeps COPY-706..727 (22 rows), and Area 1 Setup COPY-728..749 (22 rows) — are approved
  > byte for byte as written and promoted from proposed to approved citing this ruling; no text and no code constant changes."

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **W7-SHELL** Shell, HIG, 200% Modals (`F-23`), Dirty Title & `DESIGN.md` §5 | `src/CfdWorkbench.Desktop/{App.axaml,App.axaml.cs,Styles.axaml,CfdWorkbench.Desktop.csproj,Info.plist,MainWindow.axaml,MainWindow.axaml.cs,ExportDialog.axaml,ExportDialog.axaml.cs,Assets/app-icon.ico (new)}`, `src/CfdWorkbench.Desktop/Shell/{CommandTable.cs,NativeMenuBuilder.cs,ShellHost.cs,ShellLayout.cs,StatusStrip.axaml,StatusStrip.axaml.cs}`, `src/CfdWorkbench.Desktop/Panes/{StartView.axaml,StartView.axaml.cs}`, `DESIGN.md` (§4 StatusStrip + §5 shell reconciliation; zero changes to approved `COPY-*` text), `tests/CfdWorkbench.Desktop.Tests/{UiW7ShellTests.cs (new),ShellWindowTests.cs,WindowsShellTests.cs,SectionMainAreaTests.cs,StatusStripTests.cs,FolderRefusalDialogTests.cs,SaveBlockedDialogTests.cs,ExportTests.cs,WorkbenchTests.cs}` | S0 plan committed on `main` (Wave 7A) | T2 | 1 | 150 calls · 320k · 2 h · ceiling 400k | Red-first tests + mutants killed (wired into `DesktopChecks.Spawn` & `ScaleModes` in `WorkbenchTests.cs` as `--ui-w7-shell` so `bash tools/run-tests.sh` runs them permanently): **`F-23`** (WCAG 1.4.4 — `ExportDialog` plus co-located dialogs in `MainWindow.axaml.cs` (`UnsavedDialogAsync`, `FolderRefusalDialog`, `SaveBlockedDialog`) use bounded `MaxHeight` + scrollable body + sticky footer and do not clip at 200% text / 1024×700); **`F-18`** (shell: wire `view.toggle-right`, `window.reset-layout`, and macOS `About CFD Workbench` dialog; wire `window.maximize-pane` cleanly OR prune its dead `CommandTable` row per YAGNI if Dock internals make it fragile); **`F-19`** (hide/disable `Section` & `Foil source` tabs on Start; preserve Ruling 125 `COPY-404` `"Section"` verbatim while disambiguating the tab via `ToolTip`/`AutomationProperties.Name` `"Section analysis"`; add `"Close foil"` vs `"Close window"`; align `DESIGN.md` §5 with built Dock + `Setup \| CAD \| Analysis \| Sweeps` shell); **`F-20`** (Windows HIG: fix `⌃⌘F`→`Ctrl+F` hijack, add Windows `_File`/`_Edit`/`_View`/`_Section`/`_Window` mnemonics, add Windows `Ctrl+Y` Redo, refresh `CanExecuteChanged` across all menu commands); **`F-22`** (filename + dirty dot `•` in `MainWindow.Title` and `Plan` tab, updating `SectionMainAreaTests.cs` for loaded-file tab title); **`F-32`** (Dock `Size ▸ Narrow/Default/Wide` updates local `ProportionalStackPanel.Proportion`); **`F-34`** (`StartView` alert `WrapPanel`, filename+parent Recent trimming, disable `CardsPanel` while opening + cancellable `OpenExampleAsync`, guard `file.save` when empty, include `AnalysisItem` in `StatusStrip.FitItems()`); **`F-35`** (generated branded geometric `WindowIcon` / `Assets/app-icon.ico`, consistent dialog button order, `CenterOwner` on `ExportDialog`); **`F-40`** (`StartView.axaml:66` Open card copy includes `.cfdw.json` alongside `.foil`); **`F-36`/`F-42`/`F-43`/`F-44`** (promote `Button.primary`, `MonoFontFamily`, `DialogPadding`, `DialogSpacing`, `WarningBandBorder` into `Styles.axaml`; prune unused `Styles.axaml` legacy tokens `TitleSize`/`SectionThumbnail*`/`SectionPanelHeight`; remove dead `RailControlsTool` while retaining tested `FloatFrame.cs`/`FloatPlacement.cs` primitives for `ShellModelTests.cs`; gate `DockHost.LayoutUpdated` scans); `bash tools/run-tests.sh` green, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `design-lint.py --strict` clean, `check-docs.py` exit 0; Adversary pass (`w7-verifier`: UX & Accessibility + Native Desktop + Simplifier + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W7-CAD** CAD Viewports, Async `PumpUi` Removal, Precision Editing & Inspector Polish | `src/CfdWorkbench.Desktop/{ModelArea.axaml,ModelArea.axaml.cs,PlanCanvas.cs,ElevationView.cs,View3d.cs,CombPlate.axaml,CombPlate.axaml.cs,SectionEditorView.axaml,SectionEditorView.axaml.cs,SectionCanvas.cs,CatalogDialog.axaml,CatalogDialog.axaml.cs,SaveSectionDialog.axaml,SaveSectionDialog.axaml.cs,RebuildPopover.axaml,RebuildPopover.axaml.cs,PropertiesView.cs,PointsView.cs,WorkbenchController.cs,PointContextMenu.cs (new),Shell/ShellHost.cs}`, `src/CfdWorkbench.Desktop/Panes/{PropertiesPane.axaml,PropertiesPane.axaml.cs,BrowserPane.axaml,BrowserPane.axaml.cs,PointsPane.axaml,PointsPane.axaml.cs}`, `src/CfdWorkbench.Desktop/Styles.axaml` (only if CAD tokens needed), `tests/CfdWorkbench.Desktop.Tests/{UiW7CadTests.cs (new),PlanCanvasTests.cs,SectionEditorTests.cs,SectionCanvasTests.cs,PropertiesCellsTests.cs,PropertiesViewTests.cs,PointsPaneTests.cs,ElevationTests.cs,View3dTests.cs,CatalogDialogTests.cs,WorkbenchTests.cs}` | `W7-SHELL` joined on `main` (Wave 7B) | T2 | 1 | 180 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + mutants killed (wired into `DesktopChecks.Spawn` & `ScaleModes` in `WorkbenchTests.cs` as `--ui-w7-cad` so `bash tools/run-tests.sh` runs them permanently): **`F-21`** (remove synchronous `PumpUi` / `Dispatcher.UIThread.RunJobs` 8 s spin loops in `PropertiesPane` and `BrowserPane` → true `async Task` handlers with per-control pending guard); **`F-24`** (single-point double-click/`Return` focuses `PropertiesPane`; `PointsPane` `LostFocus` commit; `PropertiesPane` Shift+Tab/second-Esc routes via `ModelArea` by active mode/curve; `WorkbenchController.Select` preserves `StationFlyoutKind.Match` on re-select); **`F-25`** (`SectionCanvas` pointer-wheel zoom, empty/middle-drag pan, `Ctrl±` zoom, `Alt+Arrows` pan, `Key.Delete`, `C` toggle syncs `CurvatureToggle` + `ModeCombPlate`, curve proximity hit-test on double-click CV insert); **`F-26`** (Front `"port"` clear of scale plate; Four-View Elevation lanes usable at 1024×700; `SectionEditorView` `ScopeChip` `*`-width with `CharacterEllipsis` + full tooltip never clipping `Cancel`/`Finish section` at 1024 px; `PlanCanvas` error banner `y ≥ 36`, station chip `maxWidth`, measured tooltip box, clamped `GestureLimitText` plate); **`F-27`** (`BrowserPane` clears `StationList.SelectedItem` on non-station selection, diffs rail items in place keeping focus, gates context menu via `ShellHost.CanRun`, single-parse `FoilSource` `F-44`, role-matched glyphs `F-41`); **`F-33`** (`PointsPane.ApplyTextScale` wired from `ShellHost.ApplyTextScale` + responsive widths; `CombPlate` `MaxWidth="360"` + `Stretch` in `CombDisclosure`); **`F-37`/`F-39`/`F-40`/`F-41`/`F-42`/`F-45`/`F-46`** (resolve `PropFontFamily`/`PropFontSize` in CAD canvases replacing `"Inter"`/`Typeface.Default`; `+tnum,+lnum` on `RebuildPopover`/`BrowserPane`; clean up `intentField`/`intentControl`; inspect `PropertiesFieldNudge` — enable + test if clean or document explicit gate invariant; OS-branched `View3d` help text & wheel zoom; `PropertiesView.RoleText` in `PlanCanvas` tooltips/AT; plain-English fallback errors in dialogs/`PointsView`/`PropertiesPane`; clear prior `CatalogDialog` `ItemStatus`; full-opacity HC comb with stroke/dash distinction; hide `ThreeDDrawing` on render error; shared `PointContextMenu.cs`; structured numeric bounds instead of regex copy parsing & cached `LongestWord` in `PropertiesPane`; verify `PropertiesView.cs:1399` `"CDi (Trefftz)"` matches `ProjectionTests.cs`; `"Foil source text"` AT name & model-derived parent title; **`F-46`** distinct pane headers/tooltips/`AutomationProperties.Name` distinguishing Browser navigation tree vs Points tabular coordinate editor); `bash tools/run-tests.sh` green, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings; Adversary pass (`w7-verifier`: UX & Accessibility + Native Desktop + Simplifier + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W7-ANA-AT** Analysis/Sweeps/Setup Polish, `UI-CS-TOKEN` Guard & Real Windows UIA Native Tree Proof | `src/CfdWorkbench.Desktop/Analysis/**`, `src/CfdWorkbench.Desktop/ElevationView.cs` (only if passing `Units` to `ElevationDepthLayer.Draw`), `src/CfdWorkbench.Desktop/Sweeps/PerformanceCurveChart.cs`, `src/CfdWorkbench.Desktop/Setup/PlanformPreviewControl.cs`, `src/CfdWorkbench.Analysis/{SectionDisplay.cs,AnalysisProjection.cs,Labels.cs}`, `src/CfdWorkbench.Desktop/Styles.axaml` (only if Analysis tokens needed), `tests/CfdWorkbench.Desktop.Tests/{UiW7AnalysisTests.cs (new),WindowsUiaTreeTests.cs (new),TokenDisciplineTests.cs (new),ShellWindowTests.cs,ComparePaneTests.cs,AnalysisPanelTests.cs,DxSectionPanelTests.cs,SweepViewTests.cs,SetupViewTests.cs,WorkbenchTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/DxSectionTests.cs`, `docs/reviews/ui-as-built-workbench.{md,html}`, `docs/lessons/defect-classes.md` (register controlled `UI-CS-TOKEN`, `UI-HOVER-INVERT`, `UI-ENV-ONLY-A11Y`, `UI-DETACHED-DUP`) | `W7-CAD` joined on `main` (Wave 7C) | T2 | 1 | 180 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + mutants killed (wired into `DesktopChecks.Spawn` & `ScaleModes` in `WorkbenchTests.cs` as `--ui-w7-analysis` and Windows-guarded `--uia-probe` so `bash tools/run-tests.sh` runs them permanently): **`F-18`** Analysis (stop rendering inert static `"Overlay a section ▾"` dropdown label in `SectionDisplay.cs` legend while keeping Ruling 108 `Labels.OverlayMenu` constant preserved for future interactive overlay selection, updating `DxSectionTests.cs` accordingly); **`F-28`** (`SectionChartView` compact series legend; right-aligned `+tnum` numeric/delta columns across Analysis/Compare/Section/LoadingChart tables; header rows on `ComparePane` `InputsTable`/`OutputsTable`; `SectionTabView` tabular unit-labeled twin at `PropFontSize` ≥ 11 px); **`F-29`** (`ConditionsBand` plain-English `DangerBrush` wrapping sub-row for `ANA-INPUT-*`; `ComparePane` wrapping/stretching pickers at 1024 px); **`F-36`/`F-37`/`F-38`/`F-40`/`F-41`** Analysis (wire `Styles.axaml` `Vik*Color` into `SectionProfileView`, safe `is ISolidColorBrush` checks, aligned induced-drag key swatch; theme-keyed `PlanLoadLayer.ramp`; `PropFontFamily`/`PropFontSize`/`+tnum` across `ConditionsBand`, `FindAlphaDialog`, `LoadingChart`, `SectionChartView`, `SectionProfileView`, `LoadLayerText`; Imperial `kn` in `FindAlphaDialog` & `CompareProjection.Inputs` + `row.Unit` + scrollable `FindAlphaDialog`; Imperial depth units in `ElevationDepthLayer.cs:42` & `View3dLoadLayer.cs:107`; `"Show chart"` toggle text; read-only `Flyout` for `MoreButton`; aligned `ComparePane.TabOrder`; measured/clamped `SectionChartView` X-title & markers); **Wave 6 carry-forward** (`ActualThemeVariantChanged += (_, _) => InvalidateVisual()` + ≥ 11 px ticks on `PerformanceCurveChart` & `PlanformPreviewControl`); **`UI-CS-TOKEN` static test** (`TokenDisciplineTests.cs`: every `TryFindResource`/`FindResource`/`ResolveBrush`/`Brush` key in Desktop `.cs` resolves in `Styles.axaml`/`App.axaml`, zero `FontSize < 11` or `DrawText`/`FormattedText` size `< 11`, zero raw `Color.Parse` hexes); **Real Windows `IUIAutomation` COM tree verification** (`WindowsUiaTreeTests.cs`, `--uia-probe`, executing COM queries on a **dedicated MTA background thread** while the Avalonia UI thread pumps `Dispatcher.UIThread.RunJobs()` to prevent `WM_GETOBJECT` STA deadlock, attaching via `UIAutomationCore.dll` to the real `MainWindow` HWND across Start, Setup, CAD, Section Editor, Analysis, Sweeps, and modals verifying OS UIA `Name`, `ControlType`, non-empty `BoundingRectangle`, and keyboard focusability, skipping cleanly off Windows); expanded `--theme-matrix` emitting **`THEME-EXT-ROW`** (never altering frozen `THEME-ROW` checked by `verify-application-adapters.py`) covering `ComparePane`, `FindAlphaDialog`, `AnalysisPanel`, `Sweeps`, `Setup`, and 200% modal non-clipping; post-Wave-7 addendum in `docs/reviews/ui-as-built-workbench.{md,html}` (recording the 27 unruled Wave 2 proposed `COPY-*` rows + live NVDA/VoiceOver/signing as still open/Flagged) + 4 defect classes registered with live controls in `docs/lessons/defect-classes.md` (cross-referencing `UI-INTERACTION-STATE-ORACLE` from `UI-HOVER-INVERT`); `bash tools/run-tests.sh` green, `--theme-matrix` & `--uia-probe` green, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `check-proof-pii.py` 0 hits, `check-docs.py` exit 0; Adversary pass (`w7-verifier`: UX & Accessibility hard veto + Hydrodynamicist + Test Architect + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `monitor` | `inherit` | `main` | inherit (existing) |
| Adversary verifier | `w7-verifier` | `inherit` | `main` | inherit |
| `W7-SHELL` (Wave 7A) | `w7-shell-hig` | `share` | `main` | inherit |
| `W7-CAD` (Wave 7B) | `w7-cad-polish` | `share` | `main` | inherit |
| `W7-ANA-AT` (Wave 7C) | `w7-analysis-uia` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** Zero open DRs at start (Ruling 273 settled all Wave 6 DRs and copy). If an unexpected spec or copy
  ambiguity arises, Coordinator spawns `w7-owner` (`workspace: "inherit"`) to rule into `docs/notes/rulings.md` via
  `coord-decide.py rule next --request <id>`.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board &
  `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only` (backed by `pre-commit` floor +
  coordinator diff inspection), pack CLI `coord-mail.py` doorbells `unsupported`.
- **Deadline / Fallback:**
  - Each track has a 2–2.5 h wall budget and 400k context ceiling (warm `fork` at ~300–350k tokens if needed).
  - `W7-ANA-AT` Windows UIA probe fallback: if Avalonia 11.3.14's `Win32` automation bridge omits any specific UIA pattern
    on a custom control, record the **exact measured OS UIA observation** honestly in `docs/reviews/ui-as-built-workbench.md`
    (never falsify a pass or substitute an in-process property check for an OS UIA claim!).
- **Termination condition:** Track ends when all exit evidence is independently verified by `w7-verifier` (`PASS` or
  `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py`, or when the 2-repair-cycle / budget cap fires.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` before dispatching | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-w7-coord`) |
| S1: `W7-SHELL` joins to `main` before `W7-CAD` dispatches | Both touch `Styles.axaml`, `ShellHost.cs`, and `Desktop.Tests` (`GO5` shared-file & real-display harness exclusivity) | Coordinator |
| S2: `W7-CAD` joins to `main` before `W7-ANA-AT` dispatches | Both touch `Styles.axaml`, `Desktop.Tests`, and `W7-ANA-AT`'s `UI-CS-TOKEN` static test + Windows UIA tree walker validate the final post-`W7-SHELL` + post-`W7-CAD` Desktop codebase (`GO5` data/gate edge) | Coordinator |
| S3: Joins run one at a time via `conductor-join.py` (`--no-push`) | Serial on `main`; runs full `tools/join-ring.sh` (`TESTS-GREEN`) + `xaml-token-lint` + `check-event-subscribers` + `run-verify-gates.py` per join | Coordinator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W7-SHELL` -> `W7-CAD` | Shared `Styles.axaml` tokens (`Button.primary`, `MonoFontFamily`, `DialogPadding`/`DialogSpacing`), clean `ShellHost` / `CommandTable` / `StartView`, and updated `DESIGN.md` §5 | Hard wave boundary: `W7-SHELL` joins before `W7-CAD` branches from `main` |
| `W7-CAD` -> `W7-ANA-AT` | Clean async `PropertiesPane`/`BrowserPane`, `PropFontFamily`/`PropFontSize` in CAD canvases, clean `CatalogDialog`/`SaveSectionDialog` (no raw `Color.Parse` / `FontSize < 11` left for `UI-CS-TOKEN` guard) | Hard wave boundary: `W7-CAD` joins before `W7-ANA-AT` branches from `main` |
| `W7-ANA-AT` -> `docs/reviews/ui-as-built-workbench.md` | Final post-Wave-7 status update, real Windows UIA native tree receipt, expanded `--theme-matrix`, and registered defect classes | Committed inside `W7-ANA-AT` after all code/test proofs pass |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Running `W7-SHELL`, `W7-CAD`, and `W7-ANA-AT` concurrently | All three edit `src/CfdWorkbench.Desktop/` (`Styles.axaml`, `ShellHost.cs`, `WorkbenchController.cs`, `Desktop.Tests`) and all three run the real-display Desktop pixel/focus harness (known exit-124 timeout under concurrent runs). Fails GO5 and GO6; running serially at width 1 gives 0 merge conflicts and 0 timeouts while still preserving fresh-context isolation per track. |
| Separate UIA-only micro-track detached from `W7-ANA-AT` | `WindowsUiaTreeTests.cs` probes all surfaces including Analysis/Sweeps/Setup and immediately verifies `W7-ANA-AT`'s fixes in the same test pass; splitting adds a redundant 6-min `conductor-join.py` ring run with zero parallelism gain. |
| Live NVDA / macOS VoiceOver / Authenticode signing track | `nvda.exe` is not installed, this host is Windows ARM64 (no macOS display), and no code-signing cert exists in this repo — honestly kept `Flagged` in the Native Proof Pack rather than faking proof. |

## Cost justification (GO6 — 15× multiplier)

Honest Simplifier assessment: **width 1 is the right answer here.** Because every Wave 7 deliverable lives inside
`CfdWorkbench.Desktop` + `Desktop.Tests` (one shared display harness and coupled `Styles.axaml` / `ShellHost` /
`WorkbenchController` surfaces), concurrent execution would fail GO5 and pay the 15× multiplier only to hit merge
conflicts and exit-124 timeouts. Using three sequential `workspace: "share"` tracks instead of one monolithic session
earns **context hygiene** (each track stays well under the 400k token ceiling with focused domain context) and
**independent Adversary verification** (`w7-verifier` gates every milestone independently on its committed branch).

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect hard veto + Simplifier + Tech Lead); fold any conditions | 1 subagent pass | Clears Test Architect hard veto before S0 commit |
| 2 | Render `docs/coordination/wave7-ui-polish-and-at-proof.html`, run `docs-graph.py derive`, `check-docs.py`, `verify-ruling-citations.py`, `check-proof-pii.py`, commit S0 on `main` | 1 commit | Clean primary required before any join |
| 3 | Update swarm title (`wave7-ui-polish: W7-SHELL -> W7-CAD -> W7-ANA-AT`), update `plan.json`, add `w7-verifier` (`inherit`), and dispatch **Wave 7A** `w7-shell-hig` (`W7-SHELL`, `share`) | width 1 + verifier | Establishes shared `Styles.axaml` tokens, `F-23` 200% modals, and clean shell baseline |
| 4 | Route `W7-SHELL` to `w7-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w7-shell-hig` | 1 serial join | Unblocks Wave 7B `W7-CAD` |
| 5 | Dispatch **Wave 7B** `w7-cad-polish` (`W7-CAD`, `share`, branched from updated `main`) | width 1 | Clean CAD/Inspector/Section polish & async `PumpUi` removal |
| 6 | Route `W7-CAD` to `w7-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w7-cad-polish` | 1 serial join | Unblocks Wave 7C `W7-ANA-AT` |
| 7 | Dispatch **Wave 7C** `w7-analysis-uia` (`W7-ANA-AT`, `share`, branched from updated `main`) | width 1 | Finishes Analysis/Sweeps/Setup polish, `UI-CS-TOKEN` guard, real Windows UIA tree proof, defect classes, and review addendum |
| 8 | Route `W7-ANA-AT` to `w7-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w7-analysis-uia w7-verifier` | 1 serial join | Full Wave 7 convergence on `main` |
| 9 | Post `[done]` on swarm board, append Planned vs Actual to this plan, render `.html`, run `docs-graph.py derive` + `check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main`, and report full results to operator | 1 close commit | Clean Wave 7 closure |

## Gate record

| gate | reviewer | verdict | conditions folded |
|---|---|---|---|
| Independent Adversary Plan Review | `Wave 7 Adversary Plan Reviewer` (`Test Architect` hard veto · `Simplifier` soft veto · `Tech Lead` casting vote) | **PASS-WITH-CONDITIONS → CLEARED** | Folded all 6 conditions (`C-1..C-6`) + `S-1/S-2`: (C-1/S-1) retained tested `FloatFrame.cs`/`FloatPlacement.cs` for `ShellModelTests.cs` while deleting dead `RailControlsTool`; (C-2) preserved Ruling 125 `COPY-404` (`"Section"`) verbatim with `"Section analysis"` tooltip/AT disambiguation, preserved Ruling 108 `Labels.OverlayMenu` constant while removing inert `▾` legend rendering, and added `SectionMainAreaTests.cs` (`W7-SHELL`) + `DxSectionTests.cs` (`W7-ANA-AT`); (C-3) added `Shell/ShellHost.cs` to `W7-CAD`, marked all 7 `(new)` files, noted co-located `MainWindow.axaml.cs` dialogs, added `ElevationView.cs` to `W7-ANA-AT`; (C-4) restored `F-40` Start `.cfdw.json`, `F-38` Imperial depth layers, `F-45` `"CDi (Trefftz)"` check, explicit `F-39`/`F-46` dispositions, and 27 unruled Wave 2 `COPY-*` rows noted as out-of-scope; (C-5) required `DesktopChecks.Spawn` + `ScaleModes` ring wiring, dedicated MTA thread for `IUIAutomation` (anti-deadlock), `THEME-EXT-ROW` frozen-row guard, and corrected GO14a allowlist + `UI-CS-TOKEN` wrapper/size scan; (C-6) exact DC-189 quotes & line numbers (`ADR-0010:81-83`, Rulings `271:2025`, `272:2031`, `273:2037`); (S-2) YAGNI escape hatch for `window.maximize-pane`. |

## Status

| | |
|---|---|
| **Completed** | Pushed Wave 6 (`0b578678`), recorded Operator Ruling 273 (`6da064d4`), grounded layer state (`coord doctor` exit 0, `UIAutomationCore.dll` loaded, `nvda.exe` absent), drafted & Adversary-cleared `docs/coordination/wave7-ui-polish-and-at-proof.md` (`C-1..C-6`, `S-1..S-2` folded) |
| **Remaining** | S0 commit → serial swarm execution (`W7-SHELL` → `W7-CAD` → `W7-ANA-AT`) → independent Adversary verification & `conductor-join.py` merges → Planned vs Actual closeout |
| **Best next action** | Commit S0 (`wave7-ui-polish-and-at-proof.{md,html}` + graph + audit) and dispatch `w7-verifier` + Wave 7A `w7-shell-hig` (`W7-SHELL`) |
