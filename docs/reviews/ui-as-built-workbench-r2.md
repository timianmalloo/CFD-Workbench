---
id: review-ui-as-built-workbench-r2
title: "UI design review (Round 2): as-built CFD-Workbench desktop after Waves 8, 10, and 11"
type: doc
status: in-review
owner: "@timianmalloo"
phase: ui-design
tags: [ui-design, review, avalonia, desktop, wave10, wave11, byok, activity-rail, wave12]
links:
  - { to: review-ui-as-built-workbench, rel: refines }
  - { to: spec-cfd-workbench-v1, rel: relates-to }
  - { to: design-cfd-run-pipeline, rel: relates-to }
  - { to: design-results-and-candidates, rel: relates-to }
  - { to: design-byok-assistants, rel: relates-to }
  - { to: coordination-wave11-byok-assistants-and-prior-carryforwards, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2027-02-24"
summary: >-
  Full Round 2 /ui-design review of the as-built CFD-Workbench Avalonia desktop client at b5dc963e following Waves 8,
  10, and 11 (excluding screen-reader/NVDA/VoiceOver/Narrator work per operator directive): records DX23 measurements
  across 111 Desktop files (31 .axaml, 300 controls), 31 rubric findings (7 Blockers, 14 Majors, 7 Minors, 3 Nits),
  the Simplifier delete-list, a 10-dimension scorecard (2.0/4.0 CONDITIONAL-BLOCK), and a ranked 4-track Wave 12 fix
  plan naming Shell Chrome & Workspace Geometry Unification (W12-A) as the single highest-leverage change.
---

# UI design review (Round 2): as-built CFD-Workbench desktop (`main` `b5dc963e`)

**Mode:** `review` (read-only source inspection + deterministic token/lint/measurement run across all 111 files in
`src/CfdWorkbench.Desktop/` and `DESIGN.md`, conducted after Wave 11 closeout `b5dc963e`).  
**Scope note:** Screen-reader / assistive-technology tracks (NVDA, macOS VoiceOver, Narrator) are explicitly excluded
per operator directives of 2026-11-23 and 2026-11-24; keyboard traversal, focus routing, visual contrast, theme
variants, 1024×700 reflow, and 200% modal scaling remain in scope.

---

## 1. Stage 1 — Triggered Standards & Executive Verdict

| Trigger | Fires? | Evidence & obligation |
|---|---|---|
| **UI-T1** Expert / quantitative domain | **Fires** | Parametric CAD, VLM+strip/NeuralFoil analysis, local sweeps, CFD mesh gates/residuals, surface $C_p$/$C_f$ scalar fields (`TQ1`–`TQ12`: perceptually uniform colormaps pinned at zero, tabular numerals, units, provenance, uncertainty-first). |
| **UI-T2** Generated visual assets | **No (`n/a`)** | Zero generated imagery, personas, or video clips; all geometry, charts, and fields are vector/canvas drawn. |
| **UI-T3** Fronts an AI model | **Fires (NEW since R1)** | Wave 11 BYOK Assistant (`AI-01`–`AI-11`) is live across Setup, CAD, Analysis, Experiment, Run, Results, Solver Setup, plus `AssistantSettingsDialog` and `AssistantConsentPreviewDialog` (`U13`–`U15a`: HAX, Shape-of-AI, wrong-answer & metered cap states). |
| **UI-T4** Native client application | **Fires** | Avalonia `11.3.14` / `.NET 10` on Windows & macOS (`native-desktop`, cross-platform). Deterministic native floor: `xaml-token-lint.py`, `TokenDisciplineTests` (`UI-CS-TOKEN`), and real-display `CfdWorkbench.Desktop.Tests` (981/1/0 pass at `0472e931`). |

> **Verdict: `CONDITIONAL-BLOCK` — one focused unification & polish wave (`Wave 12`) is required before release.**
>
> **What Wave 7 fixed and kept solid:** Data safety (`Save As…` picker + dirty guard on New/Open/Recent), Command
> Palette routing & backdrop, soft hover/selection states on `Button`/`TabItem`/`ListBoxItem`, `PointsPane` HC
> contrast, `SectionModeEditor` & `ModePlate` cleanup, `ComparePane` tab binding & wrapping pickers, `LoadingChart`
> scrollable table twin, `SectionChartView` Y-ticks & legend, async `PropertiesPane`/`BrowserPane`, `ExportDialog`
> 200% scrollable chrome, `Color.Parse = 0`, `design-lint.py --strict` (0 warnings), and `xaml-token-lint.py` (0
> findings).
>
> **What drifted across Waves 8, 10, and 11 (+42.3% LOC, +97.4% AXAML controls):** The 7-Area `ActivityRail` was added
> without retiring the competing 4-area `ModelArea.Navbar` or collapsing the 260 px CAD Left Tool Dock on non-CAD
> workspaces—crushing `SolverSetupView` to 140 px and clipping toolbars at 1024×700. Three headline Wave 10/11 flows
> are unreachable or broken in live UI (`CadAssistantBar` has zero UI toggle, Area 6 Replay `BuildReplayForSample` has
> zero UI caller, and `AssistantConsentPreviewDialog` Base-Drift Refresh never clears drift), 6 of 10 Results layer
> toggles are unwired, `ResultsFieldCanvas` does not pin `vik` at physical zero and draws chrome `DangerBrush`/
> `WarningBrush` on graphite, 25+ new `ToggleButton`s leak stock Fluent blue accent, and BYOK Assistant UX fragmented
> into 7 inconsistent surface patterns.
>
> **Single highest improvement-to-effort change (`DX25`):** **Track `W12-A` — Shell Chrome, Dock Auto-Collapse &
> Global Control Tokens (~220–260 LOC).** Retiring the 4 duplicate area toggles from `ModelArea.Navbar` (showing
> `Navbar` only on CAD/Analysis), deleting the 56 px `SweepNavbarClearance` dead band across 4 views, auto-collapsing
> the 260 px CAD Left Tool Dock outside CAD/Analysis, spanning `ActivityRail` across `RowSpan=2` + adding it to `F6`,
> and adding global teal/HC `ToggleButton`/`Slider`/`ProgressBar`/`Expander` styles resolves 2 Blockers (`R2-04`,
> `R2-05`) and 2 cross-workbench Majors (`R2-09`, `R2-14`) across all 7 areas in one pass.

---

## 2. Stage 3 — Measurements Before Diagnosis (`DX23`, `285b765e` → `b5dc963e`)

### Deterministic Control Floor

| Control | Scope scanned | Result | Role (`CD13`/`CD14`) |
|---|---|---|---|
| `design-lint.py DESIGN.md --strict` | `DESIGN.md` (821 `COPY-*` rows; 50 `proposed` `COPY-866..915`) | **PASS** (0 warnings) | Inward token contract floor |
| `xaml-token-lint.py --root . src/CfdWorkbench.Desktop` | 31 `.axaml` files | **PASS** (0 findings) | Native XAML raw-value floor (blind to C# code-behind & Fluent accent fallthrough) |
| `ui-craft-gate.py src/CfdWorkbench.Desktop --markdown` | Avalonia `.axaml`/`.cs` | **CD9 `nothing scanned`** (web detector covers HTML/CSS/TSX only) | Explicitly reported as non-applicable for Avalonia native shell; not laundered as pass |
| `TokenDisciplineTests` (`UI-CS-TOKEN`, `0472e931`) | `src/CfdWorkbench.Desktop/**/*.cs` | **PASS** (0 findings; `Color.Parse = 0`, 0 `< 11 px` text) | Native C# token/font floor |
| `check-event-subscribers.py` (`0472e931`) | `src/**/*.cs` | **PASS** (55 events, 4 allowed, 0 unsubscribed) | Production wiring floor |

### Quantitative `DX23` Inventory

| Metric | R1 Baseline (`285b765e`) | R2 As-Built (`b5dc963e`) | Delta / Diagnosis | Confidence |
|---|---:|---:|---|---|
| Desktop source files (excl. `obj`/`bin`) | 77 (58 `.cs`, 19 `.axaml`) | **111** (80 `.cs`, 31 `.axaml`) | **+34 files (+44.2%)** | Verified |
| Desktop total LOC | 26,338 | **37,466** | **+11,128 LOC (+42.3%)**; largest: `WorkbenchController.cs` (4,776), `ShellHost.cs` (2,787), `PropertiesPane.axaml.cs` (2,323) | Verified |
| Interactive controls in `.axaml` | 152 (19 `.axaml`) | **300** (31 `.axaml`) | **+148 (+97.4%)** — top: `ModelArea` (32), `ResultsWorkspaceView` (31), `SetupWorkspaceView` (27), `SolverSetupView` (24), `ExportDialog` (24), `ExperimentSetupView` (18), `PropertiesPane` (14), `StartView` (13), `AssistantSettingsDialog` (12), `RunWorkspaceView` (11), `SweepResultsView` (10), `ActivityRail` (9) | Verified |
| Simultaneous shell regions | 5 + 6 modals | **6 shell regions + 8 modals** | `ActivityRail` (84 px) + Left Dock (260 px) leave only **680 px** center at 1024×700 (140 px main card in 3-col `SolverSetupView`) | Verified |
| Competing navigation surfaces | 0 | **3 overlapping layers** | `ActivityRail` (7 areas + Solver/Settings) vs `ModelArea.Navbar` (`Setup \| CAD \| Analysis \| Sweeps`) vs `SweepsWorkspaceView` (`Experiment setup · Run (local) · Results`) | Verified |
| Distinct type sizes | 6 (`10..24` px) | **5 token sizes** (`11..18` px) + **10 C# `FontSize=` literals** | Zero `< 11 px` text; `Sweeps/*.cs` hardcoded `11`/`12` bypasses `DN-5` text scaling | Verified |
| Theme brush tokens (`Styles.axaml`) | 30 × 3 themes + 10 colormap | **30 × 3 themes + 10 colormap** | Closed palette intact; 25+ unclassed `ToggleButton`/`Slider`/`ProgressBar`/`Expander` fall through to stock Fluent blue | Verified |
| C# hardcoded style literals | `Brushes.*` 88 · `Thickness` 27 · `Color.Parse` 5 · `FontSize=` 2 | **`Brushes.*` 128 · `Thickness` 41 · `Color.Parse` 0 · `FontSize=` 10** | `Color.Parse` 5 → 0; inline `FontSize=`, `Thickness`, and `Brushes.*` grew in Wave 8/10 C# views | Verified |
| Internal ID & `LAB-01` copy leaks | 3 `"certified"` hits | **3 `"certified"` hits + 4 internal IDs in headings** | `DR-F3-1 A / DR-F2-3`, `DR-F2-1`, `COMMIT-01`, `A8.6`, plus `WorkbenchController.cs:719,741,742` | Verified |

---

## 3. Stage 4 — Rubric Critique (`DX22` / `DX24`, Structure-Before-Surface)

*Severity mapping: `4` Blocker · `3` Major · `2` Minor · `1` Nit.*

### 3.1 Blockers (`Sev 4` — 7 findings)

| ID | Location | Dimension | Sev | Evidence (observed in source) | Recommended fix | Confidence |
|---|---|---|---:|---|---|---|
| **R2-01** | `ModelArea.axaml:89`; `ModelArea.axaml.cs:64-68`; `CommandTable.cs:104-111`; `Shell/ShellHost.cs` | AI-UX (`UI-T3`) & Findability | **4** | **Area 2 CAD Assistant (`AI-07`, `"Describe a change to the shape"`) is unreachable.** `CadAssistantBar` defaults `IsVisible="False"` (`ModelArea.axaml:89`), and `ModelArea.ToggleCadAssistantBar()` (`:64`) has **zero callers** anywhere in `src/CfdWorkbench.Desktop`. | Add a consistent Assistant trigger in CAD (viewport toolbar button + `CommandTable` `assistant.cad`) wired to `ModelView.ToggleCadAssistantBar()` and auto-collapse outside CAD. | Verified |
| **R2-02** | `Results/ResultsWorkspaceView.axaml:43-44, 82-90`; `Results/ResultsWorkspaceView.axaml.cs:289-358, 388-420` | State Completeness & Flow | **4** | **Area 6 Parametric Replay is unreachable in live UI.** `BuildReplayForSample(...)` (`:289`) has **zero callers** in `ResultsWorkspaceView.axaml.cs` or `ShellHost.cs`, and `ReplayHoldAxisPicker` has no `SelectionChanged` handler—leaving `ActiveReplay == null` permanently with all replay buttons disabled. | Call `BuildReplayForSample(sample.SampleId, OsAccessibility.IsReducedMotion())` automatically inside `RenderSelectedSample()` and on `ReplayHoldAxisPicker.SelectionChanged`. | Verified |
| **R2-03** | `Assistant/AssistantConsentPreviewDialog.axaml.cs:149-158, 241-248` | AI-UX (`UI-T3`) & Error Recovery | **4** | **Base-revision drift recovery is a dead loop.** `HandleRefreshBase()` (`:149-158`) updates `validationInput` and checks `IsBaseDrifted(edit)` against the **unchanged** `ExecutionResult.Validation.GeometryEdit` whose `edit.BaseAcceptedId` stays stale, so `drifted` remains `true` and `AcceptButton` can never re-enable. | On `"Refresh over current revision"`, re-validate/rebase the proposal against the current `AcceptedId` (producing a checked draft with updated `BaseAcceptedId`) or reopen Step 1 consent preview over the new revision. | Verified |
| **R2-04** | `ActivityRail.axaml:13-70`; `ModelArea.axaml:264-289`; `ModelArea.axaml.cs:591-645`; `SetupWorkspaceView.axaml:6`; `SweepsWorkspaceView.axaml:6`; `RunWorkspaceView.axaml:4`; `ResultsWorkspaceView.axaml:5` | IA, Archetype Fit & Navigation Redundancy | **4** | **Competing area navigation + broken non-viewport floating chrome + 56 px dead band.** `ModelArea.Navbar` (`Setup \| CAD \| Analysis \| Sweeps \| Views ▾ \| Display ▾ \| Fit \| Fit Selection`) floats across **every** workspace beside `ActivityRail`: duplicates 4 areas while omitting `Run`/`Results`, contradicts `"Experiment"` vs `"Sweeps"`, leaves CAD viewport buttons floating/enabled over non-CAD views (and when `!foilOpen`), and forces 4 workspaces to waste `56 px` (`SweepNavbarClearance`). | Remove the 4 area buttons from `ModelArea.Navbar` (`ActivityRail` becomes the single canonical switcher), hide `Navbar` whenever `!ViewArrangementGrid.IsVisible`, and remove `SweepNavbarClearance` from Setup, Sweeps, Run, and Results. | Verified |
| **R2-05** | `Shell/ShellHost.cs:283-288, 1181-1197`; `SolverSetup/SolverSetupView.axaml:34-36, 263`; `Run/RunWorkspaceView.axaml:9-25, 29-31, 131-137`; `Results/ResultsWorkspaceView.axaml:9-20, 82-89, 110-114`; `Setup/SetupWorkspaceView.axaml:10-33`; `Sweeps/ExperimentSetupView.axaml:10-22` | Visual Hierarchy, Density & 1024×700 Overflow | **4** | **260 px CAD Left Tool Dock stays open in non-CAD workspaces, crushing layouts and clipping controls at 1024×700.** `ActivityRail` (84 px) + Left Dock (260 px) leave only **680 px**: `SolverSetupView` (`250 + * + 290`) collapses its center card to **140 px**; `RunWorkspaceView` clips 3 of 5 buttons in its non-wrapping `StackPanel` (`:131-137`); `ResultsWorkspaceView` clips Replay & Difference controls and its ~950 px header clips even at 1280×800; `Setup` & `Experiment` headers overflow at 680 px. | Auto-collapse Left/Right CAD docks when entering `Setup`, `Experiment`, `Run`, `Results`, or `SolverSetup` (restoring prior CAD dock state on return to `CAD`/`Analysis`, reclaiming 260 px → 940 px at 1024×700), and replace non-wrapping horizontal button/picker `StackPanel`s with `WrapPanel`s + collapsible `SolverSetup` assistant drawer below 1100 px. | Verified |
| **R2-06** | `Results/ResultsWorkspaceView.axaml:133-161`; `Results/ResultsWorkspaceView.axaml.cs:61-80`; `Results/ResultsFieldCanvas.cs` | State Completeness & Dead Controls | **4** | **6 of 10 Field Evidence Layer toggles in Area 6 Results are dead.** Code-behind wires `Click` handlers only for `SurfaceCp`, `SurfaceCf`, `Separation`, and `VortexCore`; `SliceVelocity`, `Streamlines`, `Tke`, `ForceVectors`, `SpanwiseLoading`, and `Pathlines` have zero click wiring and trigger zero repaint. | Wire rendering/overlays for all 10 manifest layers in `ResultsFieldCanvas`, or render non-2D-preview layers as read-only manifest availability rows with `"Inspect in ParaView"` instead of interactive `ToggleButton`s. | Verified |
| **R2-07** | `Results/ResultsFieldCanvas.cs:123-124, 188-194, 209-213` | Quantitative Craft (`UI-T1`, `TQ2`) & Contrast | **4** | **Two `DESIGN.md` §2 violations in `ResultsFieldCanvas`:** (1) Diverging `vik` colormap is **not pinned at physical zero**—line 209 computes linear `(v - valMin) / (valMax - valMin)`, mapping `0` on asymmetric `Cp` (`[-2.0, +0.5]`) to `t = 0.8` (`VikPos1Brush` orange-brown!) instead of `VikZeroBrush` (`t = 0.5`); (2) Chrome `DangerBrush` (`2.30:1`) and `WarningBrush` (`2.56:1`) are painted directly on graphite `ViewportBrush` (`#17272c`) instead of `PlanDangerBrush`/`PlanWarningBrush` (`8.78:1`/`9.48:1`). | Normalize diverging `vik` fields symmetrically around `0` (`maxAbs = Max(|min|, |max|)`, `t = 0.5 + 0.5 * v / maxAbs`) so `0.0` always lands on `VikZeroBrush`; switch canvas overlays/banners to `PlanDangerBrush`/`PlanWarningBrush`. | Verified |

### 3.2 Majors (`Sev 3` — 14 findings)

| ID | Location | Dimension | Sev | Evidence (observed in source) | Recommended fix | Confidence |
|---|---|---|---:|---|---|---|
| **R2-08** | `SetupWorkspaceView.axaml:43-65`; `Shell/ShellHost.cs:1074-1088, 1109-1170`; `Analysis/AnalysisPanel.axaml:62`; `Sweeps/ExperimentSetupView.axaml:55`; `Run/RunWorkspaceView.axaml:123-145`; `Results/ResultsWorkspaceView.axaml:185-201`; `SolverSetup/SolverSetupView.axaml.cs:250-261, 653-703` | AI-UX (`UI-T3`, HAX / Shape-of-AI) | **3** | **7 inconsistent Assistant patterns with 6 defects:** (1) `OnAssistantStateChanged` (`ShellHost.cs:1076-1084`) omits `ModelView.SetupView` so configuring a key leaves Setup disabled; (2) Analysis `"Ask about this calculation"` has no prompt input and dumps multi-sentence answers into the 24 px `StatusStrip` (`:1167`); (3) `ExperimentAssistantBar` sits inside `EditorGrid` (`IsVisible="False"` when empty) and overflows its 280 px inspector; (4) Results card stays titled `"(offline)"` when BYOK is live; (5) `SolverSetupView` checks `HasApiKey` instead of `IsReadyForProposals`; (6) **zero metered daily cap (`Used X/Y today`)** visible outside Settings. | Standardize on `AssistantPromptBar` (compact stacked header + metered usage chip `X/Y today` + uniform `No key`/`Unevaluated`/`Ready` states), include `SetupView` in `OnAssistantStateChanged`, move `ExperimentAssistantBar` above `EmptyStatePanel`/`EditorGrid`, add real prompt input + answer card in Analysis, reflect live vs offline dynamically in Results, and enforce `IsReadyForProposals` in `SolverSetupView`. | Verified |
| **R2-09** | `Styles.axaml:391-392, 426-431`; `App.axaml:64-98`; `Setup`/`Sweeps`/`Results`/`SolverSetup` `.axaml` | Token Discipline, Theme & HC Contrast | **3** | **Stock Fluent blue accent leaks across 25+ unclassed controls + `ActivityRail` HC press defect.** No base style for `ToggleButton`, `Slider`, `ProgressBar`, or `Expander` in `Styles.axaml`/`App.axaml`, so 25+ new `ToggleButton`s, `ReplayScrubSlider`, `RunProgressBar`, and `SolverSetupView` expanders render Fluent blue. `Button.rail-item:pressed` (`Styles.axaml:392,430`) paints `MutedBrush` (`#ffffff`) on HC `SelectionBrush` (`#ffee58` = **`1.16:1`**), and `rail-item.active` lacks explicit `:pointerover`/`:pressed`. | Add global `ToggleButton`, `Slider`, `ProgressBar`, and `Expander` styles in `Styles.axaml` + HC keys in `App.axaml` using `PrimaryBrush`/`SelectionBrush`/`OnSelectionBrush`; fix `Button.rail-item:pressed TextBlock.rail-badge` (`OnSelectionBrush`) and `.active:pointerover`. | Verified |
| **R2-10** | `Assistant/AssistantSettingsDialog.axaml:78-96, 172-176, 191-193, 214-216`; `Assistant/AssistantCopy.cs:65` | Layout, Overflow & State Semantics | **3** | **Settings dialog horizontal blowout + nested scroll-inside-scroll + misleading eval banner border.** `EndpointUrlLabelText` (`Column 0`, `Auto`, no wrap) renders a 92-char string (`AssistantCopy.EndpointUrlLabel`), pushing `Column 0` to ~520 px inside the 720 px window and squeezing input fields; `EvalItemsList`/`AuditEntriesList` (`MaxHeight=160`) create nested scroll with unwrapped text; `EvalStatusBanner` hardcodes `WarningBrush` even when evaluation **PASSED**. | Shorten label to `"Endpoint URL"` with wrapped muted caption below the `TextBox`; use wrapped `DataTemplate` items with `+tnum`/`MonoFontFamily` for SHA/tokens; bind `EvalStatusBanner` border to `PrimaryBrush` (pass) / `WarningBrush` (unevaluated) / `DangerBrush` (fail). | Verified |
| **R2-11** | `Assistant/AssistantConsentPreviewDialog.axaml:150`; `Assistant/AssistantConsentPreviewDialog.axaml.cs:99-124, 160-239`; `Assistant/AssistantSettingsDialog.axaml.cs:117-137` | AI-UX (`UI-T3`), Busy States & Diff Scannability | **3** | **No loading state or cancellation during 5–120 s network calls, weak wrong-answer recovery, unwrapped provenance.** `SendAsync` and `RunEvaluationNowAsync` only disable buttons with no busy status text and no `CancellationTokenSource` wired to `CancelButton`; `ProvenanceRowsList` renders unwrapped strings; `GeometryEdit`/`CaseDiff` lack structured `Before → After` diff rows; `Refused`/`Withheld`/`TransportError` offer only `Discard`. | Wire `CancellationTokenSource` + visible busy status during network calls; render wrapped `DataTemplate` (`Tag` pill · `Field` · `Before → After` · `Reason`); add `"Edit prompt"` / `"Retry"` recovery on errors and withheld responses. | Verified |
| **R2-12** | `ActivityRail.axaml:37-59`; `Sweeps/SweepsWorkspaceView.axaml:15-20`; `Run/RunWorkspaceView.axaml`; `Results/ResultsWorkspaceView.axaml` | IA & Mental Model | **3** | **Duplicate `"Run"` and `"Results"` hierarchy between Area 4 `Sweeps` and Areas 5–6 (`Run` / `Results`).** `ActivityRail` shows `Experiment → Run → Results` while `SweepsWorkspaceView` repeats `Experiment setup · Run (local) · Results` with zero explanation of local VLM sweep vs external CFD pipeline. | Disambiguate labels (`"1. Sweep setup · 2. Local queue · 3. Polars"` in Area 4; `"CFD Run"` / `"Field Results"` in Areas 5–6) or converge local & CFD queues in a dedicated IA pass. | Verified |
| **R2-13** | `Setup/SetupWorkspaceView.axaml:179`; `Setup/SetupWorkspaceView.axaml.cs:389-406`; `Run/RunWorkspaceView.axaml.cs:409-434` | Honesty & Broken Promises | **3** | **Two controls promise live behavior they do not perform:** (1) `SetupWorkspaceView.axaml:179` labels `ConstraintsList` `"editable — appends immutable Goal version"` while rendering read-only `TextBlock`s; (2) `RunWorkspaceView.HandleRunAction()` (`:409-434`) disables `RunButton`, prints `"Starting CFD run…"`, and never invokes `SubstrateOverride`, sticking until `Cancel`. | Add inline bound editors on `ConstraintsList` (or remove `"editable —"` until built), and wire `HandleRunAction` to actually invoke `SubstrateOverride` (or surface unconfigured substrate immediately). | Verified |
| **R2-14** | `Shell/ShellHost.cs:283-291, 369-372, 1060-1071, 1297-1380`; `ModelArea.axaml.cs:616-622`; `ActivityRail.axaml.cs:55-76` | Shell Layout, Keyboard (`UI-T4`) & State Sync | **3** | **Four `ActivityRail` shell defects:** (1) `RowSpan=1` leaves an 84×190 px empty void beneath the rail when `AnalysisPanel` opens; (2) `MoveFocus` (`F6`) omits `ActivityRail`; (3) clicking `Experiment`/`Analysis` on Start highlights the rail button while showing `StartCardView` with no feedback; (4) opening `Solver setup` leaves the prior area highlighted instead of `RailSolverSetupButton`. | Set `Grid.SetRowSpan(ActivityRail, 2)`; add `ActivityRail` first in `F6` `MoveFocus`; surface `"Open or seed a foil first"` on Start when clicking `Analysis`/`Experiment`; highlight `RailSolverSetupButton` `.active` while `SolverSetupDocument` is active. | Verified |
| **R2-15** | `Run/RunWorkspaceView.axaml:82, 107`; `Run/RunCopy.cs:13-14`; `Results/ResultsWorkspaceView.axaml:46, 169`; `Results/ResultsCopy.cs:25, 35`; `Assistant/AssistantSettingsDialog.axaml:165`; `Assistant/AssistantCopy.cs:93`; `WorkbenchController.cs:719, 741, 742` | Copy Hygiene & `LAB-01` | **3** | **Internal spec/DR IDs & 3 `LAB-01` `"certified"` leaks in user-facing UI strings:** `"Mesh gate (DR-F3-1 A / DR-F2-3)"`, `"Residuals & force coefficients (DR-F2-1)"`, `"Candidate ladder (COMMIT-01)"`, `"Provenance strip (9 required fields)"`, `"Model evaluation (A8.6…)"`, and `"Open a certified foil in the views to…"` (`WorkbenchController.cs:719,741,742`). | Strip internal IDs from all user headings (`"Mesh quality gate"`, `"Residuals & force coefficients"`, `"Candidate ladder"`, `"Provenance"`, `"Model evaluation (9 active items)"`) and replace `"certified"` with `"valid"`/`"checked"` (+ update `DESIGN.md`). | Verified |
| **R2-16** | `Sweeps/PerformanceCurveChart.cs:27, 233-234, 293` | Quantitative Craft (`UI-T1`, `TQ2`) & Contrast | **3** | **Light-theme multi-series contrast failure & colliding end labels.** Series 3 (`Batlow3` `#f19d6b` = **2.15:1**) and Series 4 (`Batlow4` `#faccfa` = **1.32:1**) fail 3:1/4.5:1 on white `SurfaceBrush`, and direct end labels have no Y-collision avoidance. | Use high-contrast Light-theme stops (`PrimaryBrush`, `Batlow0`, `Batlow1`, `WarningBrush`, `DangerBrush`, `InkBrush`, all ≥ 4.5:1) and greedy Y-separation on end labels. | Verified |
| **R2-17** | `Run/RunWorkspaceView.axaml.cs:285-329`; `Setup/SetupWorkspaceView.axaml.cs:295-325, 380-436` | Theme Reactivity (`UI-CS-TOKEN`) | **3** | **Live Light ↔ Dark ↔ HC switches freeze stale brushes in Run and Setup.** `BuildElevenStatePills` and `SetupWorkspaceView` dynamic lists assign one-shot `ResolveBrush(...)` without `ActualThemeVariantChanged` or resource binding. | Use `Styles.axaml` CSS `Classes` or `GetResourceObservable(...)` / `ActualThemeVariantChanged` refresh. | Verified |
| **R2-18** | `Sweeps/ExperimentSetupView.axaml.cs:222-239`; `Sweeps/LocalRunView.axaml.cs:62-77`; `Sweeps/SweepResultsView.axaml.cs:80-116, 161-167` | Quantitative Craft (`UI-T1`, `TQ1`) | **3** | **Proportional-font ASCII pipe tables & hardcoded `FontSize = 11` in Sweeps.** Columns drift out of alignment and ignore `DN-5` text scaling. | Render preview and table-twin rows using real `Grid` columns with `Classes="pnl-cell"` (`+tnum,+lnum`, right-aligned numerics, `DynamicResource PropFontSize`). | Verified |
| **R2-19** | `Setup/SetupWorkspaceView.axaml.cs:373`; `Sweeps/ExperimentSetupView.axaml.cs:224`; `Run/RunWorkspaceView.axaml.cs:194`; `Results/ResultsWorkspaceView.axaml.cs:222, 522` | Quantitative Craft (`UI-T1`, Units) | **3** | **Global Display Units (`Metric`/`Imperial`) ignored across 4 workspaces.** Run, Results, and Experiment preview hardcode `m/s`; Setup Goal panel hardcodes `kn`. | Route displayed speeds through `controller.AnalysisUnits` / `Labels.Speed(...)` across Setup, Experiment, Run, and Results. | Verified |
| **R2-20** | `Shell/CommandTable.cs:104-110`; `Shell/ShellHost.cs:960, 1705-1716` | Navigation & Keyboard (`UI-T4`) | **3** | **Menu/Palette area-switch defects:** no `view.cad` command; `view.<area>` toggles back to CAD on repeat and fails to reactivate `ModelDocument` when `SolverSetupDocument`/`FoilSourceDocument` is active; `view.sweeps` says `"Sweeps"` vs rail `"Experiment"`; stale `BottomPanelInAnalysis` copy. | Add `view.cad` (`"CAD"`), align `"Experiment"` naming, make `view.<area>` activate `ModelDocument` and enter the target area cleanly, and update `BottomPanelInAnalysis` copy. | Verified |
| **R2-21** | `Shell/ShellHost.cs:1497-1504` | Scaling (`DN-5` 100–200%) | **3** | **`View ▸ Text size` (100–200%) stops at CAD panes.** `ActivityRail`, `Setup`, `Sweeps`, `Run`, `Results`, `SolverSetup`, `AnalysisPanel`, `ConditionsBand`, `ComparePane`, and `BrowserPane` do not receive scaled `PropFontSize`/`PropTitleSize`. | Hoist scaled font tokens to `ShellHost.Resources` so all children using `DynamicResource` scale together, and remove C# `FontSize =` literals. | Verified |

### 3.3 Minors (`Sev 2` — 7 findings) & Nits (`Sev 1` — 3 findings)

| ID | Location | Dimension | Sev | Evidence (observed in source) | Recommended fix | Confidence |
|---|---|---|---:|---|---|---|
| **R2-22** | `Sweeps/ExperimentSetupView.axaml:15, 44`; `Sweeps/LocalRunView.axaml:10` | Visual Hierarchy | **2** | `QueueButton`, `EmptyNewSweepButton`, and `RunLocalButton` omit `Classes="primary"`. | Apply `Classes="primary"` to the single primary CTA per surface. | Verified |
| **R2-23** | `ActivityRail.axaml:67`; `Shell/ShellHost.cs:1069` | System Status | **2** | Export rail badge is hardcoded `"STL · 3MF"`, omitting `.dat` and unlocked `STEP`/`3DM`. | Derive badge from open-foil & `CadGate` state (`"Open foil"` / `".dat · STL · 3MF"` / `"+ STEP/3DM"`). | Verified |
| **R2-24** | `Shell/StatusStrip.axaml:17-26`; `Shell/StatusStrip.axaml.cs:161-176` | Overflow | **2** | `StatusStrip.FitItems()` omits `SetupItem` and `SweepItem`, crowding `StatusMessage` at 1024 px. | Include `SetupItem` and idle `SweepItem` in narrow-window drop priority before `SolverItem`. | Verified |
| **R2-25** | `RunWorkspaceView.axaml:45, 99`; `ResultsWorkspaceView.axaml:26, 68, 123, 173`; `SolverSetupView.axaml:53, 123, 185` | Scroll Ergonomics | **2** | Nested `ListBox`/`ScrollViewer` regions capture mouse-wheel scrolling inside outer `ScrollViewer`s. | Use inline expansion or compact disclosure for short lists (`MeshGateFailuresList`, `CandidateList`, `TermsText`). | Verified |
| **R2-26** | `Run/RunWorkspaceView.axaml:59`; `Run/RunWorkspaceView.axaml.cs:19-32, 285-329` | Visual Hierarchy | **2** | All 11 `CfdRunState` values render as one flat pill cloud mixing sequential steps with error/terminal states. | Separate linear happy-path stepper (`Pending → … → Completed`) from semantic terminal/error badge (`WarningBrush`/`DangerBrush`). | Verified |
| **R2-27** | `DESIGN.md:282-293` & `COPY-866..915` | Doc & Copy Governance | **2** | `DESIGN.md` §5 still describes the pre-Wave-10 shell ("no activity rail; 4-button navbar"); 50 `COPY-866..915` rows await approval. | Reconcile `DESIGN.md` §5 with the unified shell and batch-approve `COPY-866..915` after sanitizing `COPY-886` (`A8.6`). | Verified |
| **R2-28** | `Sweeps/*.cs`; `RunWorkspaceView.axaml.cs:308-311`; `ResultsFieldCanvas.cs`; `SetupWorkspaceView.axaml.cs` | Token Discipline (`UI-CS-TOKEN`) | **2** | C# hardcoded style literals grew since Wave 7C (`Brushes.*` 88 → 128, `Thickness` 27 → 41, `FontSize =` 2 → 10). | Guard C# `FontSize =` literals in `TokenDisciplineTests.cs` and move pill/table styling to `Styles.axaml` classes. | Verified |
| **R2-29** | `Analysis/AnalysisPanel.axaml:60-68` | Layout / Collision | **1** | `AskCalculationButton`/`SolverSetupButton` float over `TabControl#PanelTabs` and collide with tab headers at narrow widths or 150–200% scale. | Place tab strip and right-hand links in a non-overlapping `DockPanel` header. | Verified |
| **R2-30** | `Results/ResultsFieldCanvas.cs:256-258` | Quantitative Craft (`TQ2`) | **1** | Legend min/max uses fixed `"0.00"` (collapsing small `Cf` like `0.003` to `"0.00"`) and omits a `"0"` tick on `vik`. | Use adaptive precision (`G3` / `0.###` or `0.00e+0` when `maxAbs < 0.05`) and draw an explicit `0` tick on `vik`. | Verified |
| **R2-31** | `ExportDialog.axaml:158`; `Panes/StartView.axaml:78` | Polish | **1** | `ExportDialog` `NoAssistantNoteText` sits at bottom of scroll body; `StartView` `RecentListBox` caps at `120 px` (~3 rows). | Move `NoAssistantNoteText` to footer-left metadata slot; raise `RecentListBox` `MaxHeight` to `180 px`. | Verified |

---

## 4. Simplifier Delete-List (`L9`)

```text
delete:      ModelArea.axaml:266-274 duplicate 4-area Navbar toggles (NavSetupButton, NavCadButton, NavAnalysisButton, NavSweepsButton)
delete:      SweepNavbarClearance (56 px dead bottom margin) on SetupWorkspaceView:6, SweepsWorkspaceView:6, RunWorkspaceView:4, ResultsWorkspaceView:5
delete:      ResultsWorkspaceView.axaml:139-152, 160-161 six dead unwired layer ToggleButtons (show as read-only manifest availability rows until canvas renders them)
delete:      5 internal spec/ADR IDs from user headings (DR-F3-1 A / DR-F2-3, DR-F2-1, COMMIT-01, A8.6, "9 required fields")
consolidate: 7 bespoke Assistant UI implementations → 1 canonical AssistantPromptBar (+ compact mode + metered usage chip)
shrink:      Auto-collapse 260 px CAD Left Tool Dock outside CAD/Analysis (+260 px horizontal workspace on 5 areas)
shrink:      Replace ASCII-pipe C# strings in ExperimentSetupView & SweepResultsView with shared Grid + pnl-cell helper
net:         -10 dead/duplicate controls, -4×56 px dead margins, -6 bespoke assistant card copies, +260 px reclaimed workspace on 5 areas.
```

---

## 5. Scorecard (`0–4` per Dimension)

| # | Dimension | Score | Verdict | Worst finding(s) |
|---|---|---:|---|---|
| 1 | **IA, Archetype Fit & Navigation** | **1.5 / 4** | Fail | `R2-04`, `R2-12`, `R2-20` |
| 2 | **State Completeness** | **1.5 / 4** | Fail | `R2-01`, `R2-02`, `R2-06`, `R2-13` |
| 3 | **Visual Hierarchy, Density & Layout (1024×700)** | **1.5 / 4** | Fail | `R2-05`, `R2-10` |
| 4 | **Quantitative / Technical UI Craft (`UI-T1`)** | **2.5 / 4** | Conditional | `R2-07`, `R2-16`, `R2-18`, `R2-19` |
| 5 | **AI-UX / HAX & Shape-of-AI (`UI-T3`)** | **1.5 / 4** | Fail | `R2-01`, `R2-03`, `R2-08`, `R2-11` |
| 6 | **Native Desktop HIG & Keyboard (`UI-T4`, excl. AT)** | **2.5 / 4** | Conditional | `R2-14`, `R2-20`, `R2-21` |
| 7 | **Token & Theme Discipline** | **2.0 / 4** | Conditional | `R2-09`, `R2-17`, `R2-28` |
| 8 | **Copy & Voice Hygiene** | **2.5 / 4** | Conditional | `R2-15`, `R2-27` |
| 9 | **Error Recovery & Resilience** | **2.0 / 4** | Conditional | `R2-03`, `R2-11`, `R2-24` |
| 10 | **Visual Craft & Restraint** | **2.5 / 4** | Conditional | `R2-04`, `R2-09`, `R2-26` |
| — | **Overall Workbench Mean** | **2.0 / 4** | **`CONDITIONAL-BLOCK`** | Strong Wave 7 CAD/Analysis core; Wave 8/10/11 surfaces need Wave 12 unification |

---

## 6. Ranked Next-Round Improvement Plan (`DX25` — Candidate Wave 12)

### Priority 0 — Highest-Leverage First Move
- **`W12-A` · Shell Chrome, Dock Auto-Collapse & Global Control Tokens (`R2-04`, `R2-05`, `R2-09`, `R2-14`, `R2-20`, `R2-21`, `R2-24`)**
  - Retire the 4 duplicate area toggles from `ModelArea.Navbar`; show `Navbar` only on CAD/Analysis; delete `SweepNavbarClearance` across 4 views.
  - Auto-collapse Left/Right CAD docks outside CAD/Analysis (restoring prior dock state on return to CAD/Analysis).
  - Set `ActivityRail` `RowSpan=2`, add `ActivityRail` to `F6`, highlight `RailSolverSetupButton` when active, guard Start clicks on `Analysis`/`Experiment`, add `view.cad` to `CommandTable`, and activate `ModelDocument` on `view.<area>` commands.
  - Add global teal/HC styles for `ToggleButton`, `Slider`, `ProgressBar`, `Expander`, and `Button.rail-item` press/hover in `Styles.axaml`/`App.axaml`; hoist `DN-5` `ApplyTextScale` to `ShellHost.Resources`; add `SetupItem`/`SweepItem` to `StatusStrip.FitItems()`.

### Priority 1 — Must-Fix Broken/Unreachable Flows & Scientific Truth
- **`W12-B` · BYOK Assistant Unification & Broken Flows (`R2-01`, `R2-03`, `R2-08`, `R2-10`, `R2-11`)**
  - Wire a discoverable CAD Assistant trigger (`ModelArea` toolbar + `CommandTable` `assistant.cad`) to `ToggleCadAssistantBar()`.
  - Fix `AssistantConsentPreviewDialog.HandleRefreshBase()` to re-base/re-validate and re-enable Accept; wire `CancellationTokenSource` + busy feedback on `SendAsync` and `RunEvaluationNowAsync`; add wrapped `Before → After` provenance template and `"Edit prompt / Retry"` recovery.
  - Fix `AssistantSettingsDialog` 92-char `EndpointUrlLabel` horizontal blowout, wrap `EvalItemsList`/`AuditEntriesList`, and bind `EvalStatusBanner` border color to verdict.
  - Unify all 6 active areas + SolverSetup on `AssistantPromptBar` (fix `SetupView` refresh in `OnAssistantStateChanged`, move `ExperimentAssistantBar` above empty/editor split, add real prompt input + answer card in Analysis instead of StatusStrip dump, remove stale `(offline)` in Results when live, enforce `IsReadyForProposals` in `SolverSetupView`, show `Used X/Y today` cap meter).
- **`W12-C` · Results, Run & Chart Scientific Truth (`R2-02`, `R2-06`, `R2-07`, `R2-13`, `R2-16`, `R2-17`, `R2-26`, `R2-30`)**
  - Auto-build Replay in `ResultsWorkspaceView.RenderSelectedSample()` + wire `ReplayHoldAxisPicker.SelectionChanged`.
  - Resolve the 6 unwired Results layer toggles (wire rendering or show as read-only manifest availability rows with `"Inspect in ParaView"`).
  - Pin `ResultsFieldCanvas` diverging `vik` symmetrically at physical `0.0` (`VikZeroBrush`), switch canvas overlays to `PlanDangerBrush`/`PlanWarningBrush`, add adaptive precision and explicit `0` tick.
  - Wire `RunWorkspaceView.HandleRunAction` cleanly (never stuck), split 11-state stepper from semantic terminal/error badge, and fix live Light/Dark/HC reactivity on `RunWorkspaceView` & `SetupWorkspaceView`.
  - Fix `PerformanceCurveChart` Light-theme series 3–4 contrast (`Batlow3`/`Batlow4` on white) and end-label Y-collision avoidance.

### Priority 2 — Should-Fix-Next (Reflow, Tabular Sweeps, Units & Copy Hygiene)
- **`W12-D` · Responsive Reflow, Tabular Sweeps, Units & Copy Sanitization (`R2-12`, `R2-15`, `R2-18`, `R2-19`, `R2-22`, `R2-23`, `R2-27`, `R2-28`)**
  - Convert non-wrapping horizontal toolbars/button rows in Setup, Experiment, SweepResults, Run, Results, and SolverSetup to `WrapPanel`s / responsive breakpoints (`SolverSetup` collapsible assistant pane below 1100 px).
  - Disambiguate Area 4 sub-tabs vs Areas 5–6 (`R2-12`); add `Classes="primary"` to Experiment/LocalRun primary CTAs; fix `SetupWorkspaceView` read-only `ConstraintsList` promise (`R2-13`).
  - Replace ASCII-pipe strings in Sweeps with aligned `Grid` + `pnl-cell` (`+tnum`) rows; honor global `Metric`/`Imperial` display units across Setup/Experiment/Run/Results; make `ActivityRail` Export badge dynamic (`R2-23`).
  - Sanitize all 4 internal IDs (`DR-F3-1 A`, `DR-F2-1`, `COMMIT-01`, `A8.6`) and 3 residual `LAB-01` `"certified"` strings (`WorkbenchController.cs:719,741,742`); reconcile `DESIGN.md` §5; extend `TokenDisciplineTests.cs` to guard C# `FontSize =` literals.

### Priority 3 — Worth-Doing Polish
- `R2-25` (reduce nested wheel-capturing `ScrollViewer`s in Run/Results/SolverSetup), `R2-29` (`AnalysisPanel` non-overlapping tab/action header), `R2-31` (`ExportDialog` `COPY-913` placement & `StartView` Recent height `180 px`).

### Residual Risk & Open External Gates
- **Accessibility / Screen-reader AT (`NVDA`, macOS `VoiceOver`, `Narrator`):** Explicitly excluded per operator directive; remains `Flagged` if future public accessibility certification is sought.
- **Distribution trust (Windows Authenticode / macOS codesign & notarization):** Unchanged `Flagged` external release prerequisite.
- **Note on existing test updates for `W12-A`:** Retiring the 4 duplicate area buttons from `ModelArea.Navbar` and auto-collapsing Left Dock outside CAD/Analysis will require coordinated updates to pre-rail Navbar tests (`ControllerViewTests.ModelArea_Navbar_KeyboardPath_TabAfterTheViews`, `Wave10UiTests` Navbar sync checks) as part of `W12-A` (with explicit operator/Owner ruling updating Rulings 271/272's interim navbar bridge!).

---

## 7. Post-Wave-12 Closure Ledger (`R2-01`–`R2-31`)

All 31 Round 2 `/ui-design` findings (7 Blockers, 14 Majors, 7 Minors, 3 Nits) were executed serially across Wave 12 (`W12-A` → `W12-B` → `W12-C` → `W12-D`) under Ruling 277. Screen-reader / assistive-technology tracks remain explicitly excluded per operator directive, and `COPY-866..915` remain `— proposed —` pending operator approval (`C9.6`).

| ID | Sev | Track | Closure evidence (verified) | Status |
|---|---:|---|---|---|
| **R2-01** | 4 | `W12-B` (`043186ea`) | Discoverable `NavCadAssistantButton` in `ModelArea.Navbar` + `CommandTable` `assistant.cad` (`Modal: false`) toggle `CadAssistantBar` and auto-collapse outside CAD (`W12B_CadAssistantTrigger_CommandPalette_AndAutoCollapse`). | Closed |
| **R2-02** | 4 | `W12-C` (`5338732d`) | `ResultsWorkspaceView.RenderSelectedSample()` auto-builds Replay via `BuildReplayForSample(...)` and wires `ReplayHoldAxisPicker.SelectionChanged`, explaining `< 2` compatible samples honestly (`Wave12C_Results_AutoReplay_ReadOnlyManifestLayers_ZeroPinnedVik_And_AdaptiveLegend`). | Closed |
| **R2-03** | 4 | `W12-B` (`043186ea`) | `AssistantConsentPreviewDialog.HandleRefreshBase()` invokes `onRefreshOverCurrentRevision` callback over current `AcceptedId`, returning to Step 1 Consent Preview with fresh `AI-04` `payloadSha256` without forging `BaseAcceptedId` (`W12B_BaseRevisionDriftRefresh_RebasesViaStep1ConsentPreviewNeverForges`). | Closed |
| **R2-04** | 4 | `W12-A` (`eab9438d`) | Ruling 277: retired 4 duplicate area toggles from `ModelArea.Navbar`, hid `Navbar` whenever `!ViewArrangementGrid.IsVisible`, and removed `SweepNavbarClearance` across Setup, Sweeps, Run, Results, and `Styles.axaml`. | Closed |
| **R2-05** | 4 | `W12-A` (`eab9438d`) + `W12-D` | CAD Left/Right docks auto-collapse outside CAD/Analysis (`+260 px` reclaimed at 1024×700) and restore on return (`W12-A`); non-wrapping toolbars converted to `WrapPanel`s across Setup, Experiment, LocalRun, SweepResults, Run, Results, and SolverSetup, plus collapsible `SolverSetupView` assistant drawer below 1100 px with `ToggleAssistantButton` (`W12-D`). | Closed |
| **R2-06** | 4 | `W12-C` (`5338732d`) | Retired 6 dead unwired layer `ToggleButton`s in `ResultsWorkspaceView`; replaced with read-only manifest availability rows (`"Available · Inspect in ParaView"` / `"Unavailable"`) while keeping 4 interactive 2D layers wired. | Closed |
| **R2-07** | 4 | `W12-C` (`5338732d`) | `ResultsFieldCanvas` pins diverging `vik` symmetrically around physical `0.0` (`VikZeroBrush`) and switches graphite canvas overlays to `PlanDangerBrush`/`PlanWarningBrush`. | Closed |
| **R2-08** | 3 | `W12-B` (`043186ea`) | Unified BYOK states across all 7 areas + SolverSetup: `OnAssistantStateChanged` refreshes `SetupView`, `ExperimentAssistantBar` moved above empty/editor split, Analysis inline prompt + answer card drawer, dynamic Results offline/BYOK title, `SolverSetupView` enforces `IsReadyForProposals`, and metered `Used X/Y today` chip everywhere. | Closed |
| **R2-09** | 3 | `W12-A` (`eab9438d`) | Added global teal/HC styles for `ToggleButton`, `Slider`, `ProgressBar`, `Expander` (0 new theme `...Brush` keys in `Styles.axaml` + HC overrides in `App.axaml`) and fixed `Button.rail-item:pressed` HC contrast (`OnSelectionBrush`). | Closed |
| **R2-10** | 3 | `W12-B` (`043186ea`) | Shortened `AssistantSettingsDialog` `"Endpoint URL"` label with wrapped caption (eliminating horizontal blowout), wrapped `+tnum`/`MonoFontFamily` `EvalItemsList`/`AuditEntriesList` templates, and bound `EvalStatusBanner` border to pass/warn/fail verdict. | Closed |
| **R2-11** | 3 | `W12-B` (`043186ea`) | Wired `CancellationTokenSource` + busy indicators on `SendAsync` and `RunEvaluationNowAsync`, wrapped `Before → After` provenance `DataTemplate`, and `"Edit prompt"`/`"Retry"` recovery on refused/withheld/error states. | Closed |
| **R2-12** | 3 | `W12-D` | Disambiguated Area 4 local sub-tabs (`1. Sweep setup · 2. Local queue · 3. Polars`, preserving `"Sweep:"` selector label per `C9.3`) vs Area 5 (`CFD Run Pipeline`) and Area 6 (`Field Results & Candidate Ladder`). | Closed |
| **R2-13** | 3 | `W12-C` (`5338732d`) + `W12-D` | Wired `RunWorkspaceView.HandleRunActionAsync` end-to-end with `CfdCasePipelineService` + `try/finally` (`W12-C`); wired inline constraint bound `TextBox` + `Apply` buttons in `SetupWorkspaceView` (`ConstraintsList`) calling `SetupController.UpdateConstraintBound` to append immutable Goal versions without mutating geometry (`W12-D`). | Closed |
| **R2-14** | 3 | `W12-A` (`eab9438d`) | Set `ActivityRail` `RowSpan=2`, placed `ActivityRail` first in `F6` ring, guarded Start clicks on `Analysis`/`Experiment` with status message, and highlighted `RailSolverSetupButton` `.active` when `SolverSetupDocument` is active. | Closed |
| **R2-15** | 3 | `W12-D` | Stripped all internal spec/DR IDs (`DR-F3-1 A / DR-F2-3`, `DR-F2-1`, `COMMIT-01`, `A8.6`, `"9 required fields"`) from XAML, `*Copy.cs`, and `DESIGN.md`, and replaced all 3 residual `LAB-01` `"certified"` strings in `WorkbenchController.cs:719,741,742` and `COPY-584/585/630` with `"checked"`. | Closed |
| **R2-16** | 3 | `W12-C` (`5338732d`) | Replaced low-contrast Light-theme `Batlow3`/`Batlow4` series strokes in `PerformanceCurveChart` with ≥ 4.5:1 stops and added greedy Y-collision separation on direct end labels. | Closed |
| **R2-17** | 3 | `W12-C` (`5338732d`) | Migrated `RunWorkspaceView` and `SetupWorkspaceView` dynamic lists/pills to `Styles.axaml` CSS classes so Light ↔ Dark ↔ HC switches react live without refresh. | Closed |
| **R2-18** | 3 | `W12-D` | Replaced proportional ASCII-pipe strings and all 8 numeric C# `FontSize = 11`/`12` literals across `Sweeps/{ExperimentSetupView,LocalRunView,SweepResultsView}.axaml.cs` with aligned `Grid` columns using `pnl-head`/`pnl-cell` (`+tnum`, right-aligned numerics, `DynamicResource PropFontSize`). | Closed |
| **R2-19** | 3 | `W12-D` | Routed displayed speeds through global `controller.AnalysisUnits` (`Metric` m/s ↔ `Imperial` kn via `Labels.Speed`/`Labels.SpeedUnit`) across Setup Goal operating points, Experiment preview, Run queue, and Results sample picker / replay status. | Closed |
| **R2-20** | 3 | `W12-A` (`eab9438d`) | Added `view.cad` (`Modal: false`), aligned `view.sweeps` title `"Experiment"`, updated `view.<area>` to activate `ModelDocument` without accidental toggle-back, and updated `BottomPanelInAnalysis` copy. | Closed |
| **R2-21** | 3 | `W12-A` (`eab9438d`) + `W12-D` | Hoisted `DN-5` `ApplyTextScale` tokens onto `ShellHost.Resources` (`W12-A`) and eliminated all remaining hardcoded C# `FontSize =` literals (`W12-D`). | Closed |
| **R2-22** | 2 | `W12-D` | Applied `Classes="primary"` to `EmptyNewSweepButton`, `QueueButton`, and `RunLocalButton` while keeping zero Run buttons in `ExperimentSetupView` (`XS-03`). | Closed |
| **R2-23** | 2 | `W12-D` | Made `ActivityRail` Export badge dynamic (`"Open foil"` when `!foilOpen`, `".dat · STL · 3MF"` when foil open & CAD gated, `"+ STEP/3DM"` / `"+ STEP"` / `"+ 3DM"` when unlocked). | Closed |
| **R2-24** | 2 | `W12-A` (`eab9438d`) | Added `SetupItem` and idle `SweepItem` to `StatusStrip.FitItems()` drop priority before `SolverItem` while protecting running `SweepItem` (`UX-21`). | Closed |
| **R2-25** | 2 | `W12-D` | Replaced nested wheel-capturing `ListBox`/`ScrollViewer` regions on short lists: `MeshGateFailuresList` → `ItemsControl` (`C9.2`), `CandidateList` → `ScrollViewer.VerticalScrollBarVisibility="Disabled"` with wrapped `DataTemplate`, and `SolverSetupView` `TermsText` → inline non-scrolling `Border`. | Closed |
| **R2-26** | 2 | `W12-C` (`5338732d`) | Split `RunWorkspaceView` 11-state cloud into a 7-step linear happy-path stepper plus dedicated semantic `TerminalStatusBadge` (`Failed`, `Residual not met`, `Cancelled`, `Unsupported`). | Closed |
| **R2-27** | 2 | `W12-D` | Reconciled `DESIGN.md` §5 with the shipped unified 7-Area `ActivityRail` + viewport-only `Navbar` + auto-collapsing CAD dock + BYOK shell (Ruling 277); sanitized `COPY-886` while keeping `COPY-866..915` as `— proposed —` pending operator approval (`C9.6`). | Closed |
| **R2-28** | 2 | `W12-D` | Extended `TokenDisciplineTests.cs` (`UI-CS-TOKEN`) to ban **any** numeric C# `FontSize = <n>` literal across `src/CfdWorkbench.Desktop/**/*.cs` (`C8`). | Closed |
| **R2-29** | 1 | `W12-D` | Placed `AnalysisPanel.PanelTabs` header strip inside a horizontal `PART_TabStripScroll` in the same horizontal band as `TabHeaderActions` (`AskCalculationButton`/`SolverSetupButton`) with dynamic right reserve — zero collision at narrow widths / 200% scale while keeping `LoadingPlot.Bounds.Height >= 82 px` at `ShortHeight = 150 px` (`C9.4`). | Closed |
| **R2-30** | 1 | `W12-C` (`5338732d`) | Added adaptive legend precision (`3.00e-3` for small `Cf`, never collapsing to `"0.00"`) and explicit `"0"` tick on diverging `vik` in `ResultsFieldCanvas`. | Closed |
| **R2-31** | 1 | `W12-D` | Moved `ExportDialog` `NoAssistantNoteText` (`COPY-913`) to sibling footer Grid Col 0 beside `FooterButtons` Col 1 (`C9.1`, safe on macOS `Children.Clear()`) and raised `StartView.RecentListBox` `MaxHeight` to `180 px` (`RecentListMaxHeight`). | Closed |

---

## Status

| | |
|---|---|
| **Completed** | Round 2 `/ui-design` review (`b5dc963e`) and full Wave 12 remediation across `W12-A` (`eab9438d`), `W12-B` (`043186ea`), `W12-C` (`5338732d`), and `W12-D`: all 31 findings (`R2-01`–`R2-31`) closed with red-first tests, live mutants killed, and deterministic static/doc/UI verification |
| **Remaining** | Operator approval of `COPY-866..915` (50 Wave 11 rows, currently `— proposed —` per `C9.6`) and explicit push confirmation for `main` |
| **Best next action** | Review Wave 12 closeout on `main`, rule on `COPY-866..915`, and confirm push to `origin/main` |
