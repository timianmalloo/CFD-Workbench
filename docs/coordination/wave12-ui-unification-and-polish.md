---
id: coordination-wave12-ui-unification-and-polish
title: "Coordination plan - wave 12: Round 2 /ui-design Unification, Broken-Flow Closure, Scientific Truth & Polish"
type: plan
status: active
owner: "@timianmalloo"
tags: [coordination, worktrees, ui-design, avalonia, desktop, shell, byok, results, agy-swarm, wave12]
links:
  - { to: review-ui-as-built-workbench-r2, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: design-byok-assistants, rel: refines }
  - { to: design-results-and-candidates, rel: refines }
  - { to: design-cfd-run-pipeline, rel: refines }
  - { to: coordination-wave11-byok-assistants-and-prior-carryforwards, rel: refines }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-24"
summary: >-
  Wave 12 closes all 31 Round 2 /ui-design findings (R2-01..R2-31: 7 Blockers, 14 Majors, 7 Minors, 3 Nits) from
  docs/reviews/ui-as-built-workbench-r2.md across four serial width-1 AGY swarm tracks under Ruling 277: W12-A unifies
  shell chrome (retiring duplicate Navbar area buttons, auto-collapsing CAD docks outside CAD/Analysis, ActivityRail
  RowSpan=2 + F6 + view.cad, global teal/HC control styles, DN-5 text scaling); W12-B unifies BYOK Assistant UX and
  fixes unreachable CAD Assistant, broken Base-Drift Refresh, Settings blowout, and busy/cancel states; W12-C fixes
  unreachable Replay, 6 dead Results layers, zero-pinned vik + viewport contrast, stuck Run action, live theme
  reactivity, and PerformanceCurveChart contrast; W12-D finishes responsive reflow, tabular Sweeps, Display Units,
  internal-ID/LAB-01 copy sanitization, DESIGN.md §5 sync, and P3 polish.
---

# Coordination plan: wave 12 — Round 2 `/ui-design` Unification, Broken-Flow Closure, Scientific Truth & Polish

**Input.** Operator directive of 2026-11-24 following Wave 11 + R2 review push (`86faf6f8`) and **Ruling 277**:
*"push main and kick off wave 12 per the Best next action"* — close all 31 Round 2 `/ui-design` findings
(`R2-01`–`R2-31`) from `docs/reviews/ui-as-built-workbench-r2.md` autonomously via `/prepare-for-coordination` and
`/execute-with-coordination` (AGY swarm), continuing the operator's standing exclusion of screen-reader /
accessibility-tree work (NVDA / VoiceOver / Narrator).

**Goal state.**
- **Goal:** Elevate the as-built CFD-Workbench Avalonia desktop client from `2.0 / 4.0 CONDITIONAL-BLOCK` to clean
  release craft by closing all **31** Round 2 findings (`R2-01`–`R2-31`) on `main` across four serial width-1 `share`
  tracks:
  1. **`W12-A` — Shell Chrome, Dock Auto-Collapse & Global Control Tokens (`R2-04`, `R2-05` [dock auto-collapse],
     `R2-09`, `R2-14`, `R2-20`, `R2-21`, `R2-24`):** Under **Ruling 277**, retire the 4 duplicate area toggles
     (`NavSetupButton`, `NavCadButton`, `NavAnalysisButton`, `NavSweepsButton`) from `ModelArea.Navbar` so
     `ActivityRail` is the single canonical area switcher; hide `ModelArea.Navbar` whenever
     `!ViewArrangementGrid.IsVisible` (CAD/Analysis viewport toolbar only); delete `SweepNavbarClearance` (56 px dead
     bottom margin) from Setup, Sweeps, Run, and Results; auto-collapse CAD Left/Right tool docks ONLY on entering
     Setup, Experiment, Run, Results, or SolverSetup (NEVER on initial `Cad`/Start, preserving `Shell_F4` 260 DIP
     default; restoring prior CAD dock state on return to CAD/Analysis, reclaiming `+260 px` at 1024×700); span
     `ActivityRail` across `RowSpan=2` (0 84×190 void when `AnalysisPanel` opens), add `ActivityRail` first in `F6`
     `MoveFocus` (updating `StatusStripTests.cs:111` reverse-wrap per `C2`), highlight `RailSolverSetupButton`
     `.active` while `SolverSetupDocument` is active, guard `Analysis`/`Experiment` clicks on Start (`!foilOpen`), add
     `view.cad` to `CommandTable` + `ShellHost.IsPaneCommand` + `ShellModelTests.cs:414`, align `"Experiment"` command
     title (ensuring `"Analysis"` palette search still matches exactly 1 row per `C3`), make `view.<area>` commands
     activate `ModelDocument` and enter the area cleanly even when `controller.Section != null`; re-target
     `ShellWindowTests.cs:568-569` `"analysis.nav.selected"` probe to `RailAnalysisButton` (`C1`); add global teal/HC
     styles for `ToggleButton`, `Slider`, `ProgressBar`, `Expander` (scoping cleanly so `ExpanderHeader`,
     `ToggleButton.navbar`, and reduced-motion `ProgressBar` stay intact per `C4`), and `Button.rail-item` hover/press
     states (reusing the closed 30-brush palette in `Styles.axaml` + HC keys in `App.axaml`); hoist `DN-5`
     `ApplyTextScale` to `ShellHost.Resources`; add `SetupItem`/idle `SweepItem` to `StatusStrip.FitItems()`.
  2. **`W12-B` — BYOK Assistant Unification & Broken Flows (`R2-01`, `R2-03`, `R2-08`, `R2-10`, `R2-11`):** Wire a
     discoverable CAD Assistant trigger (`ModelArea` viewport toolbar button with constructor `Click` handler +
     `CommandTable` `assistant.cad` `Modal: false` + `ShellHost.IsPaneCommand` + `ShellModelTests.cs:414`) to
     `ToggleCadAssistantBar()` (staying collapsed by default per `C6`); fix `R2-03` 100% inside `Desktop` per `C5`
     (pass `onRefreshOverCurrentRevision` callback from `ModelArea.HandleCadAssistantSubmitAsync` calling
     `controller.Assistant.PreparePreviewAsync(...)` over the current `AcceptedId`, returning cleanly to Step 1
     Consent Preview with a fresh `AI-04` `payloadSha256` — NEVER forging `BaseAcceptedId`!); wire
     `CancellationTokenSource` + visible busy status on `SendAsync` and `RunEvaluationNowAsync`; add wrapped
     `Before → After` provenance template preserving `"Stated"`/`"Defaulted"`/`"Reserved"` in `ToString()` (`C6`) and
     `"Edit prompt"`/`"Retry"` error recovery; fix `AssistantSettingsDialog` 92-char `EndpointUrlLabel` horizontal
     blowout, wrap `EvalItemsList`/`AuditEntriesList` items with `+tnum`/`MonoFontFamily`, and color `EvalStatusBanner`
     border by verdict (`PrimaryBrush`/`WarningBrush`/`DangerBrush`); unify all 6 active areas + SolverSetup on
     `AssistantPromptBar` while keeping test-pinned `x:Name` controls (`SetupWorkspaceView`, `SolverSetupView`) intact
     (`C6`), fix `SetupView` refresh in `OnAssistantStateChanged`, move `ExperimentAssistantBar` above empty/editor
     split, add collapsed-by-default prompt input + inline answer card in Analysis (preserving 5 exact tabs &
     `LoadingPlot >= 82 px`, `C6`), remove stale `"(offline)"` label in Results when live, enforce
     `IsReadyForProposals` in `SolverSetupView` (falling back cleanly when `assistantHub is null`), and show metered
     `Used X/Y today` chip.
  3. **`W12-C` — Results, Run & Chart Scientific Truth (`R2-02`, `R2-06`, `R2-07`, `R2-13` [Run], `R2-16`,
     `R2-17`, `R2-26`, `R2-30`):** Auto-build Replay inside `ResultsWorkspaceView.RenderSelectedSample()` + wire
     `ReplayHoldAxisPicker.SelectionChanged`; resolve the 6 dead Results layer `ToggleButton`s (convert non-2D-preview
     manifest layers to read-only manifest availability rows with `"Inspect in ParaView"` while keeping `SurfaceCp`,
     `SurfaceCf`, `VortexCore`, `Separation` interactive); pin `ResultsFieldCanvas` diverging `vik` symmetrically at
     physical `0.0` (`VikZeroBrush`), replace chrome `DangerBrush`/`WarningBrush` overlays on graphite with
     `PlanDangerBrush`/`PlanWarningBrush`, add adaptive legend precision & explicit `0` tick; fix
     `RunWorkspaceView.HandleRunAction` self-contained via `controller.ActiveSession` + `try/finally` (`C7`) so it
     invokes `SubstrateOverride` (or surfaces substrate-unconfigured state immediately without sticking disabled),
     split the 11-state pill cloud into a 7-step linear happy-path stepper + semantic terminal/error badge (updating
     `Wave10UiTests.cs:206` per `C7`), and fix live Light/Dark/HC theme reactivity on `RunWorkspaceView` &
     `SetupWorkspaceView`; fix `PerformanceCurveChart` Light-theme Series 3–4 contrast (`Batlow3`/`Batlow4` on white)
     and end-label Y-collision avoidance (preserving `ActualThemeVariantChanged` & zero `Typeface.Default` per `C7`).
  4. **`W12-D` — Responsive Reflow, Tabular Sweeps, Units, Copy Sanitization & Polish (`R2-05` [reflow], `R2-12`,
     `R2-13` [Setup], `R2-15`, `R2-18`, `R2-19`, `R2-22`, `R2-23`, `R2-25`, `R2-27`, `R2-28`, `R2-29`, `R2-31`):**
     Convert non-wrapping horizontal toolbars/button rows in Setup, Experiment, SweepResults, Run, Results, and
     SolverSetup to `WrapPanel`s + collapsible `SolverSetup` assistant drawer below 1100 px; disambiguate Area 4 local
     sub-tabs vs Areas 5–6 (keeping `"Sweep:"` selector label per `C9.3`); add `Classes="primary"` to
     Experiment/LocalRun primary CTAs; resolve `SetupWorkspaceView` read-only `ConstraintsList` promise; replace Sweeps
     ASCII-pipe strings with aligned `Grid` + `pnl-cell` (`+tnum`) rows; honor global `Metric`/`Imperial` display units
     across Setup/Experiment/Run/Results; derive dynamic `ActivityRail` Export badge; sanitize internal IDs
     (`DR-F3-1 A / DR-F2-3`, `DR-F2-1`, `COMMIT-01`, `A8.6`, `"9 required fields"`) and 3 residual `LAB-01`
     `"certified"` strings (`WorkbenchController.cs:719,741,742` + `COPY-584/585/630`, keeping `COPY-866..915` as
     `— proposed —` per `C9.6`); reconcile `DESIGN.md` §5 with the unified shell; extend `TokenDisciplineTests.cs` to
     ban numeric C# `FontSize = <n>` literals (`C8`); finish P3 polish (`R2-25` updating `Wave10UiTests.cs:227` per
     `C9.2`; `R2-29` same-band non-overlapping `AnalysisPanel` header preserving `LoadingPlot >= 82 px` per `C9.4`;
     `R2-31` `ExportDialog` `NoAssistantNoteText` in sibling Grid Col 0 beside `FooterButtons` Col 1 updating
     `Wave11AiUiTests.cs:464` per `C9.1`, and `StartView` `RecentListBox` `180 px`); update
     `docs/reviews/ui-as-built-workbench-r2.{md,html}` with post-Wave-12 closure ledger.
- **Done when:** All 4 serial tracks (`W12-A` → `W12-B` → `W12-C` → `W12-D`) pass red-first tests and live mutants,
  clear independent Adversary verification (`w12-verifier`: Marine-CAD UX + Hydrodynamicist + Security & Identity +
  Test Architect HARD VETO + Simplifier), and merge serially onto `main` via `conductor-join.py --no-push` with
  `TESTS-GREEN`, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `UI-CS-TOKEN` 0 findings,
  recursive `STORE-SUBSET` ok, `design-lint.py DESIGN.md --strict` 0 warnings, `check-proof-pii.py` 0 hits,
  `check-docs.py` exit 0, and `10/10` `run-verify-gates.py` PASS; swarm retired and Planned vs Actual committed on
  `main`.
- **Not in scope:** Screen-reader / accessibility-tree work (NVDA / VoiceOver / Narrator — operator-excluded);
  host-external items (`A5-RESUME` Umbrella TLS, `SPIKE-CAD-02` external CAD apps, ParaView 5.12+, Authenticode /
  macOS codesigning); pushing new Wave 12 commits to `origin` without operator confirmation.
- **Tier:** T2 · **Fan-out cap:** 1 concurrent author (width 1 across 4 serial `share` tracks — zero merge conflicts on
  `ShellHost.cs`, `ModelArea.axaml(.cs)`, `Styles.axaml`, `DESIGN.md`, `WorkbenchTests.cs`, and zero real-display
  Desktop harness contention).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w12-coord`). Owns plan, seams, join order, `conductor-join.py --no-push`
  merges, and closeout. Never authors track code.
- **Owner:** Independent swarm worker `w12-owner` (`workspace: "inherit"`, read-only on `src/`), spawned only if a
  mid-wave decision request arises.
- **Adversary Verifier:** Independent swarm worker `w12-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track in Adversary Mode (Marine-CAD UX + Hydrodynamicist + Security & Identity + Test Architect HARD VETO +
  Simplifier) before join.
- **Stall Monitor:** Swarm worker `w12-monitor` (`workspace: "inherit"`, loads `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3, 5, 6, 7, 8, 10, 11).

## Layer state

Measured 2026-11-25T05:30Z in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `86faf6f8` pushed to
`origin/main`, Ruling 277 recorded in `docs/notes/rulings.md` to commit with S0).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| sessions | `0 active session(s); 250 file(s) scanned` | clean start |
| requests | `45 terminal requests, 0 open` | zero open decision requests after Ruling 277 |
| worktrees | `1 (D:/Projects/CFD-Workbench 86faf6f8 [main])` | primary only |
| `DESIGN.md` high-water mark | `COPY-915` (50 proposed Wave 11 rows `COPY-866..915`; sanitized in `W12-D`) | `W12-D` updates `COPY-886` (`A8.6`) & sanitized copy rows cleanly |
| `Styles.axaml` closed theme brush set | 30 keys × 3 themes (`verify-application-adapters.py:123-130`) | **ZERO** new `<SolidColorBrush x:Key="...Brush">` keys inside `Styles.axaml` theme dictionaries; FluentTheme HC overrides go in `App.axaml` `ThemeDictionaries` |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifier/monitor) | each track gets a clean linked working copy; `git config core.longpaths true` active |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token wakeup; `w12-monitor` attached |
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
| `docs/notes/rulings.md` | authored, single writer | Coordinator / Owner only | serial |
| `src/CfdWorkbench.Desktop/{ActivityRail.axaml(.cs),App.axaml,ModelArea.axaml(.cs),Styles.axaml,Shell/{CommandTable.cs,ShellHost.cs,StatusStrip.axaml(.cs)},Setup/SetupWorkspaceView.axaml,Sweeps/SweepsWorkspaceView.axaml,Run/RunWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml}` + affected shell tests (`ControllerViewTests.cs`, `SetupViewTests.cs`, `SweepViewTests.cs`, `AnalysisToggleTests.cs`, `AnalysisPanelTests.cs`, `ShellWindowTests.cs`, `ShellModelTests.cs`, `StatusStripTests.cs`, `Wave10UiTests.cs`, `Wave11AiUiTests.cs`) | authored, single owner | `W12-A` owns in Wave 12A | serial |
| `src/CfdWorkbench.Desktop/{Assistant/**,ModelArea.axaml(.cs),Shell/{CommandTable.cs,ShellHost.cs},Setup/SetupWorkspaceView.axaml(.cs),Analysis/AnalysisPanel.axaml(.cs),Sweeps/ExperimentSetupView.axaml(.cs),Run/RunWorkspaceView.axaml(.cs),Results/ResultsWorkspaceView.axaml(.cs),SolverSetup/SolverSetupView.axaml(.cs),Styles.axaml (non-theme layout tokens only)}` + `tests/CfdWorkbench.Desktop.Tests/{Wave11AiUiTests.cs,ShellModelTests.cs,ControllerSectionTests.cs,SetupViewTests.cs,AnalysisPanelTests.cs,SweepViewTests.cs,SolverSetupViewTests.cs}` | authored, single owner | `W12-B` owns in Wave 12B (after `W12-A` joins) | serial |
| `src/CfdWorkbench.Desktop/{Results/{ResultsWorkspaceView.axaml(.cs),ResultsFieldCanvas.cs,ResultsCopy.cs},Run/{RunWorkspaceView.axaml(.cs),RunCopy.cs},Setup/SetupWorkspaceView.axaml(.cs),Sweeps/PerformanceCurveChart.cs,Styles.axaml (non-theme layout tokens/classes only)}` + `tests/CfdWorkbench.Desktop.Tests/{Wave10UiTests.cs,SweepViewTests.cs,SetupViewTests.cs}` | authored, single owner | `W12-C` owns in Wave 12C (after `W12-B` joins) | serial |
| `src/CfdWorkbench.Desktop/{Setup/SetupWorkspaceView.axaml(.cs),Sweeps/{SweepsWorkspaceView.axaml,ExperimentSetupView.axaml(.cs),LocalRunView.axaml(.cs),SweepResultsView.axaml(.cs)},Run/{RunWorkspaceView.axaml(.cs),RunCopy.cs},Results/{ResultsWorkspaceView.axaml(.cs),ResultsCopy.cs},SolverSetup/SolverSetupView.axaml(.cs),Analysis/AnalysisPanel.axaml(.cs),Assistant/{AssistantSettingsDialog.axaml,AssistantCopy.cs},ActivityRail.axaml(.cs),Shell/ShellHost.cs,ExportDialog.axaml,Panes/StartView.axaml,WorkbenchController.cs,Styles.axaml (non-theme layout tokens only)}`, `DESIGN.md`, `docs/reviews/ui-as-built-workbench-r2.{md,html}`, `tests/CfdWorkbench.Desktop.Tests/{TokenDisciplineTests.cs,PlanCanvasTests.cs,ShellWindowTests.cs,SetupViewTests.cs,SweepViewTests.cs,Wave10UiTests.cs,Wave11AiUiTests.cs}` | authored, single owner | `W12-D` owns in Wave 12D (after `W12-C` joins) | serial |

**Joint satisfiability of shared surfaces (GO14a).**
- Strictly **one** author active per wave (`W12-A` → `W12-B` → `W12-C` → `W12-D`): zero concurrent writers on
  `ShellHost.cs`, `ModelArea.axaml(.cs)`, `Styles.axaml`, `DESIGN.md`, or any test file, and zero real-display
  Desktop harness contention.
- **`Styles.axaml` closed theme-brush set (`tools/verify-application-adapters.py:123-130`) & `App.axaml`:**
  - Root: `src/CfdWorkbench.Desktop/Styles.axaml` theme dictionaries (`Light`/`Dark`/`HighContrast`) MUST retain
    their exact 30 `<SolidColorBrush x:Key="...Brush">` keys — ZERO added, ZERO removed.
  - Global `ToggleButton`, `Slider`, `ProgressBar`, `Expander`, and `Button.rail-item` styles in `Styles.axaml`
    reference existing `{DynamicResource ...Brush}` keys (`PrimaryBrush`, `OnPrimaryBrush`, `SelectionBrush`,
    `OnSelectionBrush`, `SurfaceBrush`, `SurfaceSoftBrush`, `InkBrush`, `MutedBrush`, `LineBrush`,
    `ControlLineBrush`, `FocusRingBrush`); FluentTheme HC key overrides (e.g., `ToggleButtonBackgroundChecked`,
    `SliderTrackValueFill`) live in `App.axaml` `ThemeDictionaries` (`HighContrast`), matching `App.axaml:87-95`.
    Jointly satisfiable.
- **`xaml-token-lint.py`, `tools/check-event-subscribers.py`, `TokenDisciplineTests.cs` (`UI-CS-TOKEN`),
  `Architecture_DockConfinedToShell`, & `ControllerSectionTests`:**
  - `xaml-token-lint.py`: root `src/CfdWorkbench.Desktop/`, zero raw colors/sizes.
  - `tools/check-event-subscribers.py`: root `src/`, every C# `event` must have ≥ 1 `+=` subscriber under `src/`.
  - `ActivityRail.axaml(.cs)`: ZERO `Dock.*` references.
  - `CommandTable.cs`: new `view.cad` (`Modal: false`, `W12-A`) and `assistant.cad` (`Modal: false`, `W12-B`) under
    `"View"` registered in `ShellModelTests.cs:414` gesture-less allowlist (unless given a gesture); `Modal: false`
    satisfies `ControllerSectionTests.Commands_ModalFlag_MatchesRowsThatShowADialog`.
  - `ShellHost.cs` auto-collapse of CAD docks outside CAD/Analysis: preserves `Shell_F4_LeftPane_Default260At1440And1280`
    on CAD/Analysis (`260 DIP` left pane when CAD/Analysis is active) while collapsing Left/Right docks when entering
    `Setup`, `Experiment`, `Run`, `Results`, or `SolverSetup` and restoring exact prior visibility on return to
    `CAD`/`Analysis`.
  - `AnalysisPanel`: `R2-29` non-overlapping header preserves exact 5 `TabItem` headers and `LoadingPlot.Bounds.Height >= 82`.
  - `ExperimentSetupView`: preserves ZERO Run/Start/Execute buttons (`XS-03`/`UX-17`) and zero `LAB-01` words.
  - `DESIGN.md`: `design-lint.py DESIGN.md --strict` 0 warnings, zero raw `#rgb`/`#rrggbb` hexes, `COPY-866..915`
    sanitized cleanly without mutating unrelated approved rows (`COPY-584/585/630` `"certified"` → `"checked"` updated
    explicitly per `R2-15` `LAB-01` compliance alongside `WorkbenchController.cs:719,741,742`, `PlanCanvasTests.cs`,
    and `ShellWindowTests.cs`). Jointly satisfiable.

**Quoted Ruling & Review trigger lines (DC-189 — exact citations, never paraphrased).**
- **Ruling 277** (`docs/notes/rulings.md`, 2026-11-24):
  > "(1) ActivityRail is now the single canonical 7-area switcher across the workbench; Rulings 271 (DR-SWP-6) and 272
  > (DR-SET-5) interim navbar bridge is cleanly retired by removing NavSetupButton, NavCadButton, NavAnalysisButton, and
  > NavSweepsButton from ModelArea.Navbar, hiding ModelArea.Navbar whenever ViewArrangementGrid is not visible (pure
  > CAD/Analysis viewport toolbar: Views, Display, Fit, Fit Selection, plus CAD Assistant trigger), and removing the
  > 56 px SweepNavbarClearance dead bottom margin from SetupWorkspaceView, SweepsWorkspaceView, RunWorkspaceView, and
  > ResultsWorkspaceView (R2-04). (2) CAD Left/Right tool docks auto-collapse when entering Setup, Experiment, Run,
  > Results, or SolverSetup and restore the user's prior CAD dock visibility when returning to CAD or Analysis (R2-05)."
- **Spec A6 `LAB-01`** (`docs/specs/cfd-workbench-v1.md:1229`):
  > "**Given** product copy, exports and assistant output, **then** a lint fails on "cavitation-free",
  > "ventilation-safe", "validated" (outside an ITTC statement), "optimized", "best", "recommended" or "certified"
  > without an A7 grant; every A7 label maps to exactly one string."

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **W12-A** Shell Chrome, Dock Auto-Collapse & Global Control Tokens (`R2-04`, `R2-05` [dock auto-collapse], `R2-09`, `R2-14`, `R2-20`, `R2-21`, `R2-24`) | `src/CfdWorkbench.Desktop/{ActivityRail.axaml,ActivityRail.axaml.cs,App.axaml,ModelArea.axaml,ModelArea.axaml.cs,Styles.axaml,Shell/CommandTable.cs,Shell/ShellHost.cs,Shell/StatusStrip.axaml,Shell/StatusStrip.axaml.cs,Setup/SetupWorkspaceView.axaml,Sweeps/SweepsWorkspaceView.axaml,Run/RunWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml}`, `tests/CfdWorkbench.Desktop.Tests/{ControllerViewTests.cs,SetupViewTests.cs,SweepViewTests.cs,AnalysisToggleTests.cs,AnalysisPanelTests.cs,ShellWindowTests.cs,ShellModelTests.cs,StatusStripTests.cs,Wave10UiTests.cs,Wave11AiUiTests.cs}` | S0 plan + Ruling 277 committed on `main` (Wave 12A) | T2 | 1 | 200 calls · 360k · 2 h · ceiling 400k | Red-first tests + ≥ 4 live mutants killed: (1) `R2-04` (Ruling 277): `NavSetupButton`/`NavCadButton`/`NavAnalysisButton`/`NavSweepsButton` removed from `ModelArea.Navbar`; `Navbar` visible ONLY when `ViewArrangementGrid.IsVisible` (and hidden when `!foilOpen`); `SweepNavbarClearance` removed from Setup, Sweeps, Run, Results (and unused token cleaned from `Styles.axaml`); (2) `R2-05`: CAD Left/Right docks auto-collapse on entering Setup/Experiment/Run/Results/SolverSetup and restore prior CAD dock visibility on return to CAD/Analysis (keeping 260 DIP default on CAD/Analysis); (3) `R2-14` & `R2-20`: `ActivityRail` `RowSpan=2` (0 84×190 void with `AnalysisPanel` open), `ActivityRail` first in `F6` `MoveFocus`, `RailSolverSetupButton` highlighted `.active` while `SolverSetupDocument` active, Start clicks on `Analysis`/`Experiment` surface `"Open or seed a foil first"` without falsely activating rail button, `view.cad` added to `CommandTable` (`Modal: false`, `ShellModelTests.cs:414`), `view.sweeps` titled `"Experiment"`, all `view.<area>` activate `ModelDocument` & enter target area, `BottomPanelInAnalysis` copy updated; (4) `R2-09`: global teal/HC styles for `ToggleButton`, `Slider`, `ProgressBar`, `Expander` in `Styles.axaml` (0 new theme `...Brush` keys!) + HC keys in `App.axaml`, `Button.rail-item:pressed TextBlock.rail-badge` uses `OnSelectionBrush` (HC contrast ≥ 4.5:1) + `.active:pointerover`; (5) `R2-21` & `R2-24`: `ApplyTextScale` hoisted to `ShellHost.Resources`; `SetupItem` & idle `SweepItem` added to `StatusStrip.FitItems()`; (6) Updated pre-rail Navbar tests + new `W12-A` assertions; `bash tools/run-tests.sh` `TESTS-GREEN`, `xaml-token-lint`, `UI-CS-TOKEN`, `check-event-subscribers`, `check-docs` all exit 0; Adversary pass (`w12-verifier`: Marine-CAD UX + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W12-B** BYOK Assistant Unification & Broken Flows (`R2-01`, `R2-03`, `R2-08`, `R2-10`, `R2-11`) | `src/CfdWorkbench.Desktop/Assistant/{AssistantCopy.cs,AssistantSettingsDialog.axaml,AssistantSettingsDialog.axaml.cs,AssistantConsentPreviewDialog.axaml,AssistantConsentPreviewDialog.axaml.cs,AssistantPromptBar.axaml,AssistantPromptBar.axaml.cs}`, `src/CfdWorkbench.Desktop/{ModelArea.axaml,ModelArea.axaml.cs,Shell/CommandTable.cs,Shell/ShellHost.cs,Setup/SetupWorkspaceView.axaml,Setup/SetupWorkspaceView.axaml.cs,Analysis/AnalysisPanel.axaml,Analysis/AnalysisPanel.axaml.cs,Sweeps/ExperimentSetupView.axaml,Sweeps/ExperimentSetupView.axaml.cs,Run/RunWorkspaceView.axaml,Run/RunWorkspaceView.axaml.cs,Results/ResultsWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml.cs,SolverSetup/SolverSetupView.axaml,SolverSetup/SolverSetupView.axaml.cs,Styles.axaml (non-theme layout tokens only)}`, `tests/CfdWorkbench.Desktop.Tests/{Wave11AiUiTests.cs,ShellModelTests.cs,ControllerSectionTests.cs,SetupViewTests.cs,AnalysisPanelTests.cs,SweepViewTests.cs,SolverSetupViewTests.cs}` | `W12-A` joined on `main` (Wave 12B) | T2 | 1 | 200 calls · 380k · 2 h 15 min · ceiling 400k | Red-first tests + ≥ 4 live mutants killed: (1) `R2-01`: discoverable CAD Assistant trigger in `ModelArea` viewport toolbar + `CommandTable` `assistant.cad` (`Modal: false`, `ShellModelTests.cs:414`) wired to `ToggleCadAssistantBar()` and auto-collapsed outside CAD; (2) `R2-03`: `AssistantConsentPreviewDialog.HandleRefreshBase()` genuinely re-bases/re-validates the geometry edit draft against current `AcceptedId` so `AcceptButton` re-enables once refreshed (verified by red-first test with drifted base revision!); (3) `R2-11`: `CancellationTokenSource` + visible busy indicator (`"Sending to model…"` / `"Running evaluation…"`) wired to `CancelButton` during `SendAsync` and `RunEvaluationNowAsync`; wrapped structured `DataTemplate` (`Tag` · `Field` · `Before → After` · `Reason`) on `ProvenanceRowsList`; `"Edit prompt"` / `"Retry"` recovery action on refused/withheld/transport error; (4) `R2-10`: `AssistantSettingsDialog` short `"Endpoint URL"` label + wrapped muted helper caption (eliminating 520 px horizontal blowout), wrapped `DataTemplate` items with `+tnum`/`MonoFontFamily` on `EvalItemsList`/`AuditEntriesList`, and `EvalStatusBanner` border bound to verdict (`PrimaryBrush` pass / `WarningBrush` unevaluated / `DangerBrush` fail); (5) `R2-08`: `ShellHost.OnAssistantStateChanged` refreshes `ModelView.SetupView` immediately on key save/remove; `ExperimentAssistantBar` moved above `EmptyStatePanel`/`EditorGrid` (works on empty Experiment + zero Run buttons per `XS-03`); Analysis `"Ask about this calculation"` opens real prompt input + inline answer card (no longer hardcoded prompt dumped to 24 px `StatusStrip`, while preserving 5 exact tabs & `LoadingPlot.Bounds.Height >= 82`); Results card title reflects Offline vs Live BYOK dynamically; `SolverSetupView` enforces `IsReadyForProposals`; `AssistantPromptBar` displays metered daily cap chip (`Used X/Y today`) + compact narrow-column layout; (6) `bash tools/run-tests.sh` `TESTS-GREEN` + all static/doc gates exit 0; Adversary pass (`w12-verifier`: Marine-CAD UX + Security & Identity HARD VETO + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W12-C** Results, Run & Chart Scientific Truth (`R2-02`, `R2-06`, `R2-07`, `R2-13` [Run], `R2-16`, `R2-17`, `R2-26`, `R2-30`) | `src/CfdWorkbench.Desktop/{Results/ResultsWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml.cs,Results/ResultsFieldCanvas.cs,Results/ResultsCopy.cs,Run/RunWorkspaceView.axaml,Run/RunWorkspaceView.axaml.cs,Run/RunCopy.cs,Setup/SetupWorkspaceView.axaml.cs,Sweeps/PerformanceCurveChart.cs,Styles.axaml (non-theme layout tokens/classes only)}`, `tests/CfdWorkbench.Desktop.Tests/{Wave10UiTests.cs,SweepViewTests.cs,SetupViewTests.cs}` | `W12-B` joined on `main` (Wave 12C) | T2 | 1 | 200 calls · 360k · 2 h · ceiling 400k | Red-first tests + ≥ 4 live mutants killed: (1) `R2-02`: `ResultsWorkspaceView.RenderSelectedSample()` auto-builds Replay via `BuildReplayForSample(sample.SampleId, OsAccessibility.IsReducedMotion())` and wires `ReplayHoldAxisPicker.SelectionChanged` so Play/Hold/Step/Scrub are live whenever ≥ 2 compatible samples exist (with explicit honest reason when `< 2`); (2) `R2-06`: eliminate all 6 dead unwired layer `ToggleButton`s in `ResultsWorkspaceView` (convert non-2D-preview layers — `SliceVelocity`, `Streamlines`, `Tke`, `ForceVectors`, `SpanwiseLoading`, `Pathlines` — to read-only manifest availability rows with `"Inspect in ParaView"` while keeping interactive `SurfaceCp`, `SurfaceCf`, `VortexCore`, `Separation` wired and verified); (3) `R2-07` & `R2-30`: pin `ResultsFieldCanvas` diverging `vik` colormap symmetrically at physical `0.0` (`maxAbs = Math.Max(Math.Abs(min), Math.Abs(max))`, `t = 0.5 + 0.5 * v / maxAbs` so `0.0` maps to `VikZeroBrush`), replace chrome `DangerBrush`/`WarningBrush` on graphite with `PlanDangerBrush`/`PlanWarningBrush`, add adaptive legend precision (`G3`/`0.###` or `0.00e+0` when `maxAbs < 0.05`) and explicit `0` tick on diverging `vik`; (4) `R2-13` (Run) & `R2-26`: wire `RunWorkspaceView.HandleRunAction` to actually execute `SubstrateOverride` (updating queue/state & resetting `RunButton`/`CancelButton` on completion/failure, or reporting substrate-unconfigured immediately without sticking disabled!), and separate the linear happy-path stepper (`Pending → Readiness → Meshing → Mesh gate → Solving → Harvesting → Completed`) from semantic terminal/error badge (`Unsupported`, `Residual not met`, `Failed`, `Cancelled`); (5) `R2-17`: live Light ↔ Dark ↔ HC reactivity on `RunWorkspaceView` & `SetupWorkspaceView` via CSS `Classes` / `ActualThemeVariantChanged` (no frozen one-shot brushes); (6) `R2-16`: `PerformanceCurveChart` Light-theme high-contrast series stops (≥ 4.5:1 on white `SurfaceBrush`) + greedy Y-collision avoidance on direct end labels; (7) `bash tools/run-tests.sh` `TESTS-GREEN` + all static/doc gates exit 0; Adversary pass (`w12-verifier`: Hydrodynamicist + Marine-CAD UX + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W12-D** Responsive Reflow, Tabular Sweeps, Units, Copy Sanitization, `DESIGN.md` §5 & Polish (`R2-05` [reflow], `R2-12`, `R2-13` [Setup], `R2-15`, `R2-18`, `R2-19`, `R2-22`, `R2-23`, `R2-25`, `R2-27`, `R2-28`, `R2-29`, `R2-31`) | `src/CfdWorkbench.Desktop/{Setup/SetupWorkspaceView.axaml,Setup/SetupWorkspaceView.axaml.cs,Sweeps/SweepsWorkspaceView.axaml,Sweeps/ExperimentSetupView.axaml,Sweeps/ExperimentSetupView.axaml.cs,Sweeps/LocalRunView.axaml,Sweeps/LocalRunView.axaml.cs,Sweeps/SweepResultsView.axaml,Sweeps/SweepResultsView.axaml.cs,Run/RunWorkspaceView.axaml,Run/RunWorkspaceView.axaml.cs,Run/RunCopy.cs,Results/ResultsWorkspaceView.axaml,Results/ResultsWorkspaceView.axaml.cs,Results/ResultsCopy.cs,SolverSetup/SolverSetupView.axaml,SolverSetup/SolverSetupView.axaml.cs,Analysis/AnalysisPanel.axaml,Analysis/AnalysisPanel.axaml.cs,Assistant/AssistantSettingsDialog.axaml,Assistant/AssistantCopy.cs,ActivityRail.axaml,ActivityRail.axaml.cs,Shell/ShellHost.cs,ExportDialog.axaml,Panes/StartView.axaml,WorkbenchController.cs,Styles.axaml (non-theme layout tokens only)}`, `DESIGN.md`, `docs/reviews/ui-as-built-workbench-r2.md`, `docs/reviews/ui-as-built-workbench-r2.html`, `tests/CfdWorkbench.Desktop.Tests/{TokenDisciplineTests.cs,PlanCanvasTests.cs,ShellWindowTests.cs,SetupViewTests.cs,SweepViewTests.cs,Wave10UiTests.cs,Wave11AiUiTests.cs}` | `W12-C` joined on `main` (Wave 12D) | T2 | 1 | 220 calls · 380k · 2 h 15 min · ceiling 400k | Red-first tests + ≥ 4 live mutants killed: (1) `R2-05` reflow & `R2-25`: convert non-wrapping horizontal button/picker `StackPanel`s in Setup, Experiment, SweepResults, Run, Results, and SolverSetup to `WrapPanel`s; make `SolverSetupView` assistant column collapsible below 1100 px; reduce nested wheel-capturing `ScrollViewer`s on short lists (`MeshGateFailuresList`, `CandidateList`, `TermsText`); (2) `R2-12`, `R2-13` (Setup), `R2-22`, `R2-23`: disambiguate Area 4 local sub-tab labels (`1. Sweep setup · 2. Local queue · 3. Polars`) vs Areas 5–6 (`CFD Run` / `Field Results`); resolve `SetupWorkspaceView` `ConstraintsList` `"editable —"` promise (either inline bound editor or honest read-only heading matching current capability); apply `Classes="primary"` to `EmptyNewSweepButton`/`QueueButton`/`RunLocalButton`; make `ActivityRail` Export badge dynamic (`Open foil` / `.dat · STL · 3MF` / `+ STEP/3DM`); (3) `R2-18` & `R2-19`: replace Sweeps ASCII-pipe strings with real `Grid` + `Classes="pnl-cell"` (`+tnum`, right-aligned numerics, `DynamicResource PropFontSize`); honor global `Metric`/`Imperial` display units across Setup, Experiment preview, Run, and Results; (4) `R2-15`, `R2-27`, `R2-28`: strip all 4 internal spec/DR IDs (`DR-F3-1 A / DR-F2-3`, `DR-F2-1`, `COMMIT-01`, `A8.6`, `"9 required fields"`) from user-facing XAML/Copy strings, replace the 3 residual `LAB-01` `"certified"` strings in `WorkbenchController.cs:719,741,742` (+ `COPY-584/585/630`, `PlanCanvasTests.cs`, `ShellWindowTests.cs`), reconcile `DESIGN.md` §5 with the unified shell, and extend `TokenDisciplineTests.cs` to ban hardcoded C# `FontSize =` literals across `src/CfdWorkbench.Desktop/**/*.cs`; (5) P3 polish (`R2-29` non-overlapping `AnalysisPanel` header preserving 5 tabs & `>= 82 px` height; `R2-31` `ExportDialog` `COPY-913` footer-left slot & `StartView` `RecentListBox` `180 px`); (6) Append post-Wave-12 closure table to `docs/reviews/ui-as-built-workbench-r2.md` & re-render `.html`; `bash tools/run-tests.sh` `TESTS-GREEN` + `design-lint --strict` + `xaml-token-lint` + `UI-CS-TOKEN` + `check-event-subscribers` + `check-docs` all exit 0; Adversary pass (`w12-verifier`: Marine-CAD UX + Hydrodynamicist + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Mandatory Adversary conditions (`C1`–`C9`, binding on workers & `w12-verifier`)
- **`C1` (`W12-A`):** Re-target `ShellWindowTests.cs:568-569` frozen `"analysis.nav.selected"` probe (`verify-application-adapters.py:185`) from `NavAnalysisButton` to `host.ActivityRail.FindControl<Button>("RailAnalysisButton")!`.
- **`C2` (`W12-A`):** Update `StatusStripTests.cs:111` reverse-wrap `F6` for `ActivityRail` at index 0 + add explicit `F6` reachability check.
- **`C3` (`W12-A`, `W12-B`):** Add `view.cad` (`W12-A`) and `assistant.cad` (`W12-B`) to both `ShellHost.IsPaneCommand` (`:1876-1884`) and `ShellModelTests.cs:414`; ensure `view.<area>` executes cleanly when `controller.Section != null` (`ControllerSectionTests.cs:761-768`); keep `"Analysis"` palette search at exactly 1 match (`AnalysisToggleTests.cs:264-266`); update `AnalysisToggleTests.cs:251`, `Wave10UiTests.cs`, `SetupViewTests.cs`, `SweepViewTests.cs`, `ControllerViewTests.cs`.
- **`C4` (`W12-A`):** Scope global `ToggleButton` styles away from `ExpanderHeader`, `ToggleButton.navbar`, and reduced-motion `ProgressBar`; auto-collapse CAD docks ONLY on `Setup`, `Experiment`, `Run`, `Results`, and `SolverSetup` — NEVER on initial `Cad`/Start (`Shell_F4_LeftPane_Default260At1440And1280`).
- **`C5` (`W12-B`, Security HARD VETO):** Solve `R2-03` 100% inside `CfdWorkbench.Desktop` via an optional `onRefreshOverCurrentRevision` callback from `ModelArea.HandleCadAssistantSubmitAsync` calling `controller.Assistant.PreparePreviewAsync(...)` against current `AcceptedId` and returning `AssistantConsentPreviewDialog` cleanly to Step 1 Consent Preview over a fresh `AI-04` `payloadSha256` — NEVER forging `BaseAcceptedId`.
- **`C6` (`W12-B`):** Keep test-pinned `x:Name`s (`SetupWorkspaceView`, `SolverSetupView`), fall back cleanly when `SolverSetupView`'s `assistantHub is null`, preserve `"Reserved"`/`"Stated"`/`"Defaulted"` in `DataTemplate` DTO `ToString()`, keep Area 3 Analysis prompt/answer & `CadAssistantBar` collapsed by default, and attach CAD toolbar Assistant button `Click` in `ModelArea`'s constructor.
- **`C7` (`W12-C`):** Update `Wave10UiTests.cs:206` (`DisplayedStateCount`) for the 7-step happy-path stepper + semantic terminal badge; implement `HandleRunAction` self-contained via `controller.ActiveSession` + `try/finally`; preserve `PerformanceCurveChart` `ActualThemeVariantChanged` & zero `Typeface.Default`.
- **`C8` (`W12-D`):** Ban any numeric C# `FontSize = <n>` literal in `TokenDisciplineTests.cs:87-91` after migrating all 8 literals across `Sweeps/{ExperimentSetupView,LocalRunView,SweepResultsView}.axaml.cs`.
- **`C9` (`W12-D`):** Place `ExportDialog` `NoAssistantNoteText` in sibling Grid Col 0 beside `FooterButtons` Col 1 (never inside `FooterButtons`) and update `Wave11AiUiTests.cs:464`; update `Wave10UiTests.cs:227` if `MeshGateFailuresList` becomes `ItemsControl`; keep `"Sweep:"` label in `SweepsWorkspaceView`; keep `AnalysisPanel` right-hand links in the same horizontal header band (`LoadingPlot >= 82 px`); sanitize `COPY-886` (`A8.6`) while keeping `COPY-866..915` as `— proposed —` (never self-approve).

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `w12-monitor` | `inherit` | `main` | inherit |
| Adversary verifier | `w12-verifier` | `inherit` | `main` | inherit |
| `W12-A` (Wave 12A) | `w12-shell-tokens` | `share` | `main` | inherit |
| `W12-B` (Wave 12B) | `w12-byok-ui` | `share` | `main` | inherit |
| `W12-C` (Wave 12C) | `w12-results-run` | `share` | `main` | inherit |
| `W12-D` (Wave 12D) | `w12-polish-copy` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** Ruling 277 authorizes `W12-A`'s retirement of Rulings 271/272's interim 4-button `ModelArea.Navbar`
  bridge and CAD dock auto-collapse. If any track hits a new spec/ruling ambiguity, Coordinator spawns `w12-owner`
  (`workspace: "inherit"`) to rule into `docs/notes/rulings.md`.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board &
  `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only`, pack CLI `coord-mail.py` doorbells
  `unsupported`.
- **Deadline / Fallback:** Each track has a 2–2.25 h wall budget and 360–400k context ceiling.
- **Termination condition:** Track ends when all exit evidence is independently verified by `w12-verifier` (`PASS` or
  `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py --no-push`, or when the 2-repair-cycle / budget
  cap fires.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit Ruling 277 + this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites Ruling 277 & this plan | Coordinator (`agy-w12-coord`) |
| S1: `W12-A` joins before `W12-B`, `W12-C`, `W12-D` | Reshapes `ShellHost`, `ModelArea.Navbar`, `ActivityRail`, `Styles.axaml`, `App.axaml`, and `DN-5` scaling that every downstream view builds on (`GO5`) | Coordinator (`agy-w12-coord`) |
| S2: Serial UI tracks `W12-B` → `W12-C` → `W12-D` | Share `RunWorkspaceView`, `ResultsWorkspaceView`, `SetupWorkspaceView`, `Styles.axaml`, `Wave10UiTests.cs`, `Wave11AiUiTests.cs`, and the single real-display Desktop harness (`GO5`) | Coordinator (`agy-w12-coord`) |
| S3: Joins run one at a time via `conductor-join.py --no-push` | Serial on `main`; every join runs full `TESTS-GREEN` + `xaml-token-lint` + `check-event-subscribers` + `10/10` verify gates | Coordinator (`agy-w12-coord`) |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W12-A` → `W12-B` | Viewport-only `ModelArea.Navbar`, `ActivityRail` canonical nav, global `ToggleButton`/`Slider`/`Expander` styles, `ShellHost` dock auto-collapse | Hard wave boundary: `W12-A` joins before `W12-B` branches |
| `W12-B` → `W12-C` | Unified `AssistantPromptBar` slots on `Setup`, `Run`, `Results`, `SolverSetup` | Hard wave boundary: `W12-B` joins before `W12-C` branches |
| `W12-C` → `W12-D` | Fixed `ResultsWorkspaceView` (6 dead toggles removed/wired, live Replay), `RunWorkspaceView` (split stepper/status), `SetupWorkspaceView` theme reactivity | Hard wave boundary: `W12-C` joins before `W12-D` branches |

## Struck tracks

| track | why it was not worth its multiplier / why struck |
|---|---|
| Accessibility / screen-reader tracks (NVDA / VoiceOver / Narrator) | Explicitly excluded per operator directives of 2026-11-23 & 2026-11-24. |
| Host-external items (`A5-RESUME`, `SPIKE-CAD-02`, ParaView 5.12+, codesigning) | External network/software/certificate prerequisites outside this repository. |
| Concurrent `W12-B` / `W12-C` / `W12-D` | Share Desktop `.axaml`/`.cs` views, `Styles.axaml`, test suites, and the single real-display Desktop harness; width-1 serial execution prevents merge conflicts and focus flakes. |

## Cost justification (GO6 — 15× multiplier)

Honest Simplifier assessment: **width 1 across 4 bounded Desktop `share` tracks is the smallest reliable shape.**
Splitting by architectural responsibility (`W12-A` Shell/Tokens → `W12-B` BYOK UX → `W12-C` Scientific Truth →
`W12-D` Reflow/Units/Copy/Polish) keeps every track cleanly inside 200 calls / < 400k context, eliminates merge
conflicts across overlapping `.axaml` views, and ensures each high-risk cluster clears independent Adversary review
before the next builds on it.

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect HARD VETO + Marine-CAD UX + Simplifier SOFT VETO + Tech Lead casting vote); fold all verified conditions | 1 subagent pass | Clears vetoes & catches any hidden Desktop test traps before S0 |
| 2 | Render `docs/coordination/wave12-ui-unification-and-polish.html`, run `docs-graph.py derive`, `tools/check-proof-pii.py`, `verify-ruling-citations.py`, `tools/check-docs.py`, append `prepare-for-coordination` audit entry, commit Ruling 277 + S0 on `main` | 1 commit | Clean primary required before any join |
| 3 | Update swarm title (`wave12-ui-unification: W12-A -> W12-B -> W12-C -> W12-D`), write `plan.json`, add `w12-monitor` + `w12-verifier`, dispatch **Wave 12A** `w12-shell-tokens` (`W12-A`, `share`) → `w12-verifier` → join → kill | width 1 + 1 join | Highest-leverage shell & token foundation first |
| 4 | Dispatch **Wave 12B** `w12-byok-ui` (`W12-B`, `share`) → `w12-verifier` → join → kill | width 1 + 1 join | Fixes broken BYOK flows & unifies 7 areas |
| 5 | Dispatch **Wave 12C** `w12-results-run` (`W12-C`, `share`) → `w12-verifier` → join → kill | width 1 + 1 join | Fixes unreachable Replay, 6 dead toggles, zero-pinned `vik`, stuck Run & contrast |
| 6 | Dispatch **Wave 12D** `w12-polish-copy` (`W12-D`, `share`) → `w12-verifier` → join → kill `w12-polish-copy` / `w12-verifier` / `w12-monitor` | width 1 + 1 join | Closes remaining P2/P3 reflow, tabular Sweeps, units, copy & R2 ledger |
| 7 | Post `[done]` on board, append Planned vs Actual, render `.html`, run `docs-graph.py derive` + `check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main` (no push), report final results | 1 close commit | Clean Wave 12 closure |

## Gate record

| gate | reviewer | verdict | conditions folded |
|---|---|---|---|
| Independent Adversary Plan Review | `Wave 12 Adversary Plan Reviewer` (`758e0281`; `Test Architect` hard veto · `Security & Identity` hard veto · `Marine-CAD UX` · `Simplifier` soft veto · `Tech Lead` casting vote) | **PASS-WITH-CONDITIONS → CLEARED** | Verified 31/31 Round 2 findings (`R2-01`–`R2-31`) covered; folded all 10 conditions (`C1`–`C10`): (C1) re-target `ShellWindowTests.cs:568-569` frozen `"analysis.nav.selected"` probe to `RailAnalysisButton`; (C2) update `StatusStripTests.cs:111` reverse-wrap `F6` for `ActivityRail` at index 0; (C3) add `view.cad`/`assistant.cad` to `ShellHost.IsPaneCommand` & `ShellModelTests.cs:414`, safe with `controller.Section != null`, keep `"Analysis"` palette search == 1 match, update `AnalysisToggleTests.cs:251` & `Wave10UiTests.cs`; (C4) scope global `ToggleButton` styles away from `ExpanderHeader`/`ToggleButton.navbar`/reduced-motion `ProgressBar` and auto-collapse docks ONLY on non-CAD workspaces (never initial `Cad`/Start); (C5) solve `R2-03` 100% in Desktop via `onRefreshOverCurrentRevision` callback returning to Step 1 Consent Preview over a fresh `AI-04` `payloadSha256` (never forging `BaseAcceptedId`); (C6) keep test-pinned `x:Name`s, preserve `ToString()` tokens on `DataTemplate` DTOs, keep Area 3 prompt/answer & `CadAssistantBar` collapsed by default; (C7) update `Wave10UiTests.cs:206` for 7-step stepper + terminal badge, self-contained `HandleRunAction` via `controller.ActiveSession` + `try/finally`, preserve `PerformanceCurveChart` `ActualThemeVariantChanged` & zero `Typeface.Default`; (C8) verified all 8 C# `FontSize =` literals live in `W12-D`'s `Sweeps/*.cs`; (C9) `ExportDialog` `NoAssistantNoteText` in sibling Grid Col 0 beside `FooterButtons`, update `Wave10UiTests.cs:227`/`Wave11AiUiTests.cs:464`, keep `"Sweep:"` label, same-band `AnalysisPanel` header (`>= 82 px`), keep `COPY-866..915` proposed; (C10) explicit `R2-05 [dock auto-collapse]` vs `R2-05 [reflow]` split. Tech Lead casting vote: APPROVE 4 serial width-1 `share` tracks. |

## Status

| | |
|---|---|
| **Completed** | Pushed Wave 11 + R2 review (`86faf6f8`) to `origin/main`, recorded Operator Ruling 277, and drafted & Adversary-cleared `docs/coordination/wave12-ui-unification-and-polish.md` (`C1`–`C10` folded, 31/31 Round 2 findings covered) |
| **Remaining** | Commit Ruling 277 + S0 → serial swarm execution (`W12-A` → `W12-B` → `W12-C` → `W12-D`) → Planned vs Actual closeout |
| **Best next action** | Render `.html`, run docs/PII/ruling/verify gates, commit Ruling 277 + S0 on `main`, and dispatch `w12-monitor` + `w12-verifier` + Wave 12A `w12-shell-tokens` (`W12-A`) |
