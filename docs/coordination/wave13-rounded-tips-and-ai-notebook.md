---
id: coordination-wave13-rounded-tips-and-ai-notebook
title: "Coordination plan - wave 13: Rounded Wing Tips (FoilDSL 4.2) and AI-Guided Hydrofoil Design Notebook"
type: plan
status: completed
owner: "@timianmalloo"
phase: "coordination — Wave 13 (execution plan for Wave 13A Rounded Wing Tips + Wave 13B AI-Guided Design Notebook)"
tags: [coordination, worktrees, parallelism, agy-swarm, foildsl, wingtip, ai, notebook, wave13]
links:
  - { to: design-rounded-wing-tips, rel: implements }
  - { to: design-ai-guided-design-notebook, rel: implements }
  - { to: spec-rounded-wing-tips, rel: implements }
  - { to: spec-ai-guided-design-notebook, rel: implements }
  - { to: coordination-wave11-byok-assistants-and-prior-carryforwards, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-27"
summary: >-
  Three-stage, four-track AGY swarm coordination plan for Wave 13 delivering Wave 13A (Rounded &
  Radiused Wing Tips / FoilDSL 4.2) and Wave 13B (AI-Guided Hydrofoil Design Notebook) with zero
  overlapping authored files per stage. Stage 1 (w13a-core, width 1) lands FoilDSL 4.2 Core, Geometry
  positive-chord BFS, TipCapMath, STL/3MF/STEP/3DM exports, Analysis COPY-941, and foildsl.md sync.
  Stage 2 (true parallel width 2, 100% file-disjoint) runs w13a-ui (Area 2 CAD Structure B Tip UI &
  PlanCanvas vertical handle) alongside w13b-core (AI Notebook Core/Persistence/Analysis Runtime/CLI
  + Eval Suite v2.0). Stage 3 (w13b-ui, width 1) delivers native Avalonia NotebookWorkspaceView
  matching the approved HTML mockup across all 7 states.
---

# Coordination plan: wave 13 — Rounded Wing Tips (`FoilDSL 4.2`) and AI-Guided Hydrofoil Design Notebook

**Input.** Operator directive of 2026-11-27 following push of design slices (`450afd5c`) and approval of `COPY-916..942`: plan and execute **Wave 13** (Wave 13A Rounded Wing Tips + Wave 13B AI-Guided Hydrofoil Design Notebook) using `/prepare-for-coordination` and `/execute-with-coordination` (`--swarm`) in isolated worktrees with explicit seam ownership.

**Goal state.**
- **Goal:** Implement, independently verify, and converge onto local `main` via `conductor-join.py --no-push`:
  1. **Stage 1 (`w13a-core`, width 1):** All non-Desktop Wave 13A deliverables per `docs/design/rounded-wing-tips.md` (`FoilDSL 4.2` parser/serializer/identity, `Bernstein.CertifyPositiveChord` BFS, `ChannelEdits`/`PointModel`/`ChordDimension` + public `AuthoringSession` tip-edit methods `SetTipTangent`/`SetTipRadius`/`SetTipCap` with structured Core refusal `LimitMeters` & `EditReference` tuples, analytic `OverallSpanMeters` & `s²` MAC, `TipCapMath`, nested `double.SinPi` sampling, watertight STL/3MF/STEP AP203/Rhino 3DM rounded tip caps + chordal `v` `CadExportExtractor`, `Analysis` `COPY-941` + `ExportSession` tip-closure row, `docs/specs/foildsl.md` sync, and red-first tests `T-RT-01..08`).
  2. **Stage 2 (true parallel width 2, zero overlapping authored files):**
     - **`w13a-ui` (Desktop ONLY):** Area 2 CAD Structure B `Tip` group in `PropertiesView`/`PropertiesPane`, locked `h:angle` on vertical `TipHandle`, `DR-CELL-3` `"Use <max> mm"` repair link, `PlanCanvas` vertical guide tick & `ValueOnly` drag, `SurfaceRenderer` rounded tip cap + wireframe rib fallback, `WorkbenchController` mapping Core tip-edit refusal `LimitMeters` onto Desktop `CommitOutcome.Refused` + local `EdgeHullCrossing` & `DeriveFeed` `RoundedTipCap`, `ExportDialog` binding, and `T-RT-09` Desktop harness tests.
     - **`w13b-core` (Core + Persistence + Analysis + CLI, zero Desktop UI edits):** Pure `STORE-SUBSET` `Core/Notebook/**` (`FoilReq 1.0` parser/validator, `GrillTreeEngine`, `CandidateIntentValidator`, `RequirementScorecard`, `NotebookRecords` `<= 1,024 B` line chunks), two-profile `RecipeSeeder.EmitFoilDsl` + `My sections` snapshot determinism, `Envelope.Notebook` + `RunRecord.Format` (new parameters optional/defaulted) + `HandoffNotebookCandidate`, `AssistantEvalSuite` v2.0 (`11` active items + `CheckModelEligibility` optional `modelIdOverride`), `OptionalFastModelId` + value-level secret screening in `AssistantSettingsStore`, `EffectiveModelId` in `AssistantService` (retaining backward-compatible 2-arg `RunEvaluationAsync(bool liveWire = false, CancellationToken ct = default)` overload alongside the `targetModelId` overload so Desktop compiles unchanged during Stage 2 join), direct `ProductWingMethod`/`PanelMethod`/`NeuralFoilPolarSource` `NotebookCandidateRunner` + `UntrustedExcerpt`-fenced `NotebookOrchestrator` (using `IProgress<T>` / existing `AssistantService.TelemetryEmitted`, declaring ZERO new unsubscribed C# `event`s), `Cli/NotebookVerb` + `AssistantVerb` dynamic count, single-line `Wave11AiUiTests.cs:145` dynamic count (`AssistantEvalSuite.GetSuiteDefinition().Count`), and red-first tests `T-NB-01..07`.
  3. **Stage 3 (`w13b-ui`, width 1):** Native Avalonia `NotebookWorkspaceView` matching `docs/mockups/ai-guided-design-notebook.html` across all 7 states (`empty`, `grill`, `simulating`, `iterated`, `refused`, `no-key`, `overflow`) with `COPY-924..934`, `CadHandoffDialog`, `AssistantConsentPreviewDialog` (`EffectiveModelId` + `ExtractProvenance`), `AssistantSettingsDialog.axaml(.cs)` (`OptionalFastModelId` + dynamic eval), `ActivityRail`/`ModelArea`/`StartView` 2×2 grid/`CommandTable` `view.notebook` (`Modal: false`)/`ShellHost`, and `T-NB-08` Desktop harness tests.
- **Done when:** All 4 tracks pass red-first tests + live mutants, clear independent Adversary verification (`w13-verifier`: Security & Identity HARD VETO + Hydrodynamicist + Geometry & Numerical Analyst + Data & Persistence + Marine-CAD UX + Test Architect HARD VETO + Simplifier), and converge cleanly via `conductor-join.py --no-push` with `bash tools/run-tests.sh` green, `xaml-token-lint.py`, `check-event-subscribers.py`, `TokenDisciplineTests`, `ArchitectureTests`, `store_subset.py`, `check-foildsl-spec-sync.py`, `check-docs.py`, and `run-verify-gates.py` all PASS; swarm retired and Planned vs Actual recorded on `main`.
- **Not in scope:** Pushing Wave 13 implementation commits to `origin` without operator confirmation; live OS screen-reader validation (NVDA / VoiceOver / Narrator — excluded by standing operator directive); live third-party cloud LLM API calls in automated CI tests (all wire paths proven against real local loopback `HttpListener` on `127.0.0.1`).
- **Tier:** T2 · **Fan-out cap:** 2 concurrent authors max (Stage 2 width 2) + 1 read-only verifier (`w13-verifier`) + 1 stall monitor (`w13-monitor`).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w13-coord`). Owns plan, seam arbitration, `conductor-join.py --no-push` joins, and closeout. Never authors track code.
- **Owner:** Designated Owner seat (`@timianmalloo`; ruling ledger `docs/notes/rulings.md` — any `DR-n` escalates to `@timianmalloo` and is recorded in `docs/notes/rulings.md`; requesters never self-rule).
- **Adversary Verifier:** Independent swarm worker `w13-verifier` (`workspace: "inherit"`, read-only). Reviews every track's committed diff & test evidence before join.
- **Stall Monitor:** Swarm worker `w13-monitor` (`workspace: "inherit"`, loads `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3–12).

## Layer state

Measured 2026-11-28T03:59Z in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `450afd5c` pushed to `origin/main`).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge automatically without coordination |
| leader | `released (epoch 5)` | not pinned (consistent with Waves 3–12) |
| sessions | `0 active session(s); 252 file(s) scanned` | clean start |
| requests | `ok - 46 request(s), 0 open, 46 terminal` | zero open decision requests |
| lease overlap | `none (0 live lease(s))` | clean start |
| worktrees | `1 (D:/Projects/CFD-Workbench 450afd5c [main])` | primary only |
| `pack-doctor.py --json` | `PASS: 22, WARN: 2, FAIL: 0` | pack rev 120 intact; Python 3.14.7 (`py -3` / `python`), Node v24.20.0 |
| `DESIGN.md` copy high-water mark | `COPY-942` (0 unapproved rows for Catalog, Notebook, or Rounded Tips) | `COPY-916..942` all approved 2026-11-27 |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifier/monitor) | each author gets an isolated linked working copy |
| agy swarm board & `swarm_wait` | `enforced` inside swarm session | zero-token sleep; `w13-monitor` active |
| agy ownership hooks (`.agents/hooks.json`) | `observed-only` | `pre-commit` floor + coordinator diff inspection enforce path ownership |
| pack CLI mail doorbells (`coord-mail.py`) | `unsupported` | board `@mention` + `swarm_wait` is the sole enforced wakeup channel |

The coordination layer is installed **once, in the primary checkout** (`D:/Projects/CFD-Workbench`). Every share workspace created by `swarm{action:"add", workspace:"share"}` **inherits** `.git/config` merge drivers and `.git/hooks` from the primary checkout — workers confirm inherited drivers via `git config --get-regexp 'merge\..*\.driver'` and **never** run `coord install` inside a share workspace.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | `coord-register` union merge | **no** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen`: `audit-log.py --root docs --project CFD-Workbench render` | **no** |
| `docs/docs-index.js` | derived | `coord-regen`: `docs-graph.py derive` | **no** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | `coord-register` union merge | **no** |
| `docs/lessons/defect-classes.md` | register | `defect-register` (`tools/merge-defect-register.py`) | **no** |
| `docs/notes/rulings.md` | authored, single writer | Coordinator / Owner (`@timianmalloo`) only | serial |
| `src/CfdWorkbench.Core/{Contracts,FoilSource,Geometry,ChordDimension,ChannelEdits,PointModel,PlanformCurvature,WingEstimates,TipCapMath,Placement,StlExport,ThreeMfExport,CadExportExtractor,StepAp203Export,Rhino3dmExport,AuthoringSession}.cs`, `src/CfdWorkbench.Analysis/{Labels,AnalysisProjection,Export/ExportSession,Export/ExportCopy}.cs`, `docs/specs/foildsl.md`, `tests/CfdWorkbench.Core.Tests/{RoundedTipTests.cs,IdentityTests.cs,...}`, `tests/CfdWorkbench.Analysis.Tests/{AnalysisChecks.cs,RoundedTipAnalysisTests.cs}` | authored, single owner | `w13a-core` owns exclusively in Stage 1 | Stage 1 serial |
| `src/CfdWorkbench.Desktop/{SurfaceRenderer,WorkbenchController,PropertiesView,Panes/PropertiesPane.axaml.cs,PlanCanvas,ExportDialog.axaml(.cs),TipCopy.cs}`, `tests/CfdWorkbench.Desktop.Tests/{WorkbenchTests.cs,RoundedTipDesktopTests.cs}` | authored, single owner | `w13a-ui` owns exclusively in Stage 2 (zero `Core`/`Persistence`/`Analysis`/`Cli` touches) | Stage 2 parallel (disjoint from `w13b-core`) |
| `src/CfdWorkbench.Core/Notebook/**`, `src/CfdWorkbench.Core/{RecipeSeeder,RunRecord,AuthoringSession}.cs`, `src/CfdWorkbench.Core/Assistant/**`, `src/CfdWorkbench.Persistence/Assistant/AssistantSettingsStore.cs`, `src/CfdWorkbench.Analysis/Assistant/AssistantService.cs`, `src/CfdWorkbench.Analysis/Notebook/**`, `src/CfdWorkbench.Cli/{NotebookVerb,AssistantVerb,Program}.cs`, `tests/CfdWorkbench.Core.Tests/{NotebookTests.cs,IdentityTests.cs,AssistantEvalTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/{AnalysisChecks.cs,NotebookOrchestratorTests.cs,AssistantServiceTests.cs}`, `tests/CfdWorkbench.Cli.Tests/{CliTests.cs,NotebookCliTests.cs}`, `tests/CfdWorkbench.Desktop.Tests/Wave11AiUiTests.cs` (L145 dynamic count only) | authored, single owner | `w13b-core` owns exclusively in Stage 2 (after `w13a-core` lands on `main`) | Stage 2 parallel (disjoint from `w13a-ui`) |
| `src/CfdWorkbench.Desktop/Notebook/**`, `src/CfdWorkbench.Desktop/Assistant/{AssistantConsentPreviewDialog.axaml.cs,AssistantSettingsDialog.axaml(.cs),AssistantCopy.cs}`, `src/CfdWorkbench.Desktop/{ActivityRail.axaml(.cs),ModelArea.axaml(.cs),Panes/StartView.axaml(.cs),Shell/CommandTable.cs,Shell/ShellHost.cs,WorkbenchController.cs}`, `tests/CfdWorkbench.Desktop.Tests/{WorkbenchTests.cs,NotebookDesktopTests.cs,ShellModelTests.cs,ControllerSectionTests.cs,Wave11AiUiTests.cs}` | authored, single owner | `w13b-ui` owns exclusively in Stage 3 (after both Stage 2 tracks join `main`) | Stage 3 serial |

**Joint satisfiability of shared surfaces (GO14a).**
- **Stage 2 zero-overlap proof (`w13a-ui` and `w13b-core`):** Every owned path set is **100% file-disjoint across all `src/` and `tests/` files (and 100% project-disjoint across all 5 `src/` projects)** — the only shared test project is `tests/CfdWorkbench.Desktop.Tests/`, where `w13a-ui` edits `WorkbenchTests.cs` + `RoundedTipDesktopTests.cs` and `w13b-core` makes only the 1-line dynamic-count edit at `Wave11AiUiTests.cs:145`. Only `w13a-ui` runs the real-display Desktop harness during Stage 2; `w13b-core` validates with `bash tools/run-tests.sh --no-desktop` in its share (avoiding OS display contention), and `conductor-join.py` runs the integrated full Desktop suite sequentially on each merge.
- **Scan-shaped static guards (GO14a):**
  - `tools/store_subset.py` — **root:** `src/CfdWorkbench.Core` and `tests/CfdWorkbench.Core.Tests` (**recursive** `*.cs`, skipping `bin/` and `obj/`); **token set (`SENSITIVE`):** `File.*` (except `File.ReadAllBytes`/`File.ReadAllText`), `Directory.*` (except `Directory.EnumerateFiles`/`Directory.GetFiles`), `FileStream`, `FileInfo`, `GetTempPath`, `GetEnvironmentVariable`, `DllImport`, `LibraryImport`, `ProjectStore`, and `CfdWorkbench.Persistence` (except `InternalsVisibleTo("CfdWorkbench.Persistence")`); **allowlist (`PARTITION_EXEMPT`):** 6 files (`ProjectStoreTests.cs`, `LayoutFileTests.cs`, `PreferenceStoreTests.cs`, `SectionLibraryTests.cs`, `IdentityTests.cs`, `WindowsProjectStoreTests.cs`). Jointly satisfied by `w13a-core` and `w13b-core` (all `Core/Notebook` and `Core/TipCapMath` are pure zero-I/O).
  - `tests/CfdWorkbench.Analysis.Tests/ArchitectureTests.cs` — **root:** built `CfdWorkbench.Analysis.dll` PE `MemberReferences` targeting `CfdWorkbench.Core` (assembly-wide, non-source); **token set:** exact names `ProfileAt`, `Cancel`, `Undo`, `Redo` and prefixes `Begin*`, `Update*`, `Apply*`; **allowlist:** empty. Jointly satisfied by `w13a-core` and `w13b-core`.
  - `tools/check-foildsl-spec-sync.py` — **root:** `git diff merge-base(HEAD, main)..HEAD` on `src/CfdWorkbench.Core/FoilSource.cs` from `class Grammar` declaration to EOF (single-file, non-recursive); **token set:** any changed line in `Grammar..EOF`; **allowlist / satisfaction:** `docs/specs/foildsl.md` changed in the same branch OR a branch commit message matching `\bRuling\s+\d+\b`. Owned cleanly inside `w13a-core` Stage 1.
  - `tools/check-event-subscribers.py` (`docs/coordination/join.json` gate) — **root:** `src/` (**recursive** `*.cs`); **token set:** every C# `event ... <name>` declaration without `<name> +=` anywhere in `src/`; **allowlist (`ALLOWED`):** 4 named events (`CanExecuteChanged`, `VertexSelected`, `VertexMoved`, `FocusedTargetChanged`), where stale or newly subscribed entries also fail. Jointly satisfied across Stages 1–3: `w13b-core` MUST NOT declare any new unsubscribed C# `event` in Stage 2 (use `IProgress<T>` or existing `AssistantService.TelemetryEmitted`), and both `w13a-ui` and `w13b-ui` verify `check-event-subscribers.py` before handback.
  - `docs/ai-forward-pack/scripts/xaml-token-lint.py` — **root:** `src/CfdWorkbench.Desktop` (**recursive** `*.xaml`/`*.axaml`); **token set:** raw hex/named colors in 6 visual attributes (`Background`, `Foreground`, `BorderBrush`, `Fill`, `Stroke`, `Color`) and raw numeric literals in 10 dimension attributes (`Margin`, `Padding`, `CornerRadius`, `Width`, `Height`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`, `FontSize`); **allowlist:** resource/binding markers (`{StaticResource`, `{DynamicResource`, `{ThemeResource`, `{Binding`) and `auto`/`nan`/`*`. Jointly satisfied by `w13a-ui` and `w13b-ui`.
  - `tests/CfdWorkbench.Desktop.Tests/TokenDisciplineTests.cs` — **root:** `src/CfdWorkbench.Desktop` (**recursive** `*.cs`, skipping `bin/` and `obj/`); **token set:** string resource keys passed to `TryFindResource`/`FindResource`/`ResolveBrush`/`Resource`/`Brush`/`GetResourceObservable` unresolved in `Styles.axaml` or `App.axaml`, any `Color.Parse(...)`, any C# `FontSize = <n>`, and any `DrawText`/`FormattedText` literal `< 11`; **allowlist:** empty. Jointly satisfied by `w13a-ui` and `w13b-ui`.
  - `tests/CfdWorkbench.Desktop.Tests/{ShellModelTests.cs:414, ControllerSectionTests.cs:747, Wave10UiTests.cs}` (`ActivityRail` `84 DIP`, `0 Dock.*` outside `Shell/`, `CommandTable` `Modal` parity & gesture-less allowlist) — owned cleanly by `w13b-ui`.
- **Quoted ADR / Ruling triggers (DC-189):**
  - `spec-foildsl §1` (`docs/specs/foildsl.md:37-38`): *"The accepted source is the canonical authored artifact. Its parsed record is a disposable, reproducible projection, never an independently editable authority."* · `ADR-0002` (`docs/adr/0002-foildsl-authority.md:102`): *"Analysis reads accepted revision identity only."*
  - `ADR-0010 §Decision 1 & 4` (`docs/adr/0010-one-placement-rule.md:63, 81`): *"One rule, one site."* and *"Displays receive geometry, never compute it."*
  - `ADR-0013 §Decision 2` (`docs/adr/0013-cad-export-kernel.md:56, 58`): *"emits millimeter unscaled STEP AP203"* and *"emit zero-PII headers (`1980-01-01T00:00:00` fixed timestamp, blank author/org/path, sanitized `DatImport.Slug`, mirroring `ThreeMfExport.cs` and Ruling 196 A6)"*.
  - `Ruling 93` (`docs/notes/rulings.md:957`): *"Minimum tip chord = max(5 mm, 2 % of root chord), ONE definition in Core, enforced by the planform edit verbs"* · `Ruling 94` (`docs/notes/rulings.md:963`, `TipChord.cs:19`): *"Tip chord can't go below `<min>` (the larger of 5 mm and 2 % of the root chord)."*

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | deadline / fallback / termination | exit evidence | harness & doorbell |
|---|---|---|---|---|---|---|---|---|
| `w13a-core` | `Core/{Contracts,FoilSource,Geometry,ChordDimension,ChannelEdits,PointModel,PlanformCurvature,WingEstimates,TipCapMath,Placement,StlExport,ThreeMfExport,CadExportExtractor,StepAp203Export,Rhino3dmExport,AuthoringSession}.cs`, `Analysis/{Labels,AnalysisProjection,Export/ExportSession,Export/ExportCopy}.cs`, `docs/specs/foildsl.md`, `Core.Tests/*`, `Analysis.Tests/{AnalysisChecks,RoundedTipAnalysisTests}.cs` | none (base `450afd5c`) | T2 | 0 | 90 calls / 400k ctx / 45 m | Deadline: 45 m; fallback: `w13-monitor` stall alert → Coordinator reassigns or finishes serially; termination: `w13-verifier` PASS + clean `conductor-join.py --no-push` | Red-first `T-RT-01..08` + killable mutants, `bash tools/run-tests.sh --no-desktop` green, `check-foildsl-spec-sync.py` & `store_subset.py` & `check-event-subscribers.py` & `ArchitectureTests` green, `w13-verifier` PASS | agy-swarm (`share`, enforced isolation); doorbell: board `@mention` + `swarm_wait` (enforced; `coord-mail` unsupported) |
| `w13a-ui` | `Desktop/{SurfaceRenderer,WorkbenchController,PropertiesView,Panes/PropertiesPane.axaml.cs,PlanCanvas,ExportDialog.axaml(.cs),TipCopy.cs}`, `Desktop.Tests/{WorkbenchTests.cs,RoundedTipDesktopTests.cs}` | `w13a-core` | T2 | 0 | 75 calls / 400k ctx / 35 m | Deadline: 35 m; fallback: `w13-monitor` stall alert → Coordinator reassigns or runs serially after `w13b-core`; termination: `w13-verifier` PASS + clean `conductor-join.py --no-push` | Red-first `T-RT-09` + mutants, full `bash tools/run-tests.sh` green, `xaml-token-lint.py` & `TokenDisciplineTests` & `check-event-subscribers.py` green, `w13-verifier` PASS | agy-swarm (`share`, enforced isolation); doorbell: board `@mention` + `swarm_wait` (enforced; `coord-mail` unsupported) |
| `w13b-core` | `Core/Notebook/**`, `Core/{RecipeSeeder,RunRecord,AuthoringSession}.cs`, `Core/Assistant/**`, `Persistence/Assistant/AssistantSettingsStore.cs`, `Analysis/Assistant/AssistantService.cs`, `Analysis/Notebook/**`, `Cli/{NotebookVerb,AssistantVerb,Program}.cs`, `Core.Tests/*`, `Analysis.Tests/*`, `Cli.Tests/*`, `Desktop.Tests/Wave11AiUiTests.cs` (L145 only) | `w13a-core` | T2 | 0 | 100 calls / 400k ctx / 45 m | Deadline: 45 m; fallback: `w13-monitor` stall alert → Coordinator reassigns or splits CLI into follow-on serial pass; termination: `w13-verifier` PASS + clean `conductor-join.py --no-push` | Red-first `T-NB-01..07` + killable mutants (real loopback `HttpListener`), `bash tools/run-tests.sh --no-desktop` green, `store_subset.py` & `check-event-subscribers.py` & `ArchitectureTests` green, `w13-verifier` PASS | agy-swarm (`share`, enforced isolation); doorbell: board `@mention` + `swarm_wait` (enforced; `coord-mail` unsupported) |
| `w13b-ui` | `Desktop/Notebook/**`, `Desktop/Assistant/{AssistantConsentPreviewDialog.axaml.cs,AssistantSettingsDialog.axaml(.cs),AssistantCopy.cs}`, `Desktop/{ActivityRail.axaml(.cs),ModelArea.axaml(.cs),Panes/StartView.axaml(.cs),Shell/CommandTable.cs,Shell/ShellHost.cs,WorkbenchController.cs}`, `Desktop.Tests/{WorkbenchTests.cs,NotebookDesktopTests.cs,ShellModelTests.cs,ControllerSectionTests.cs,Wave11AiUiTests.cs}` | `w13a-ui`, `w13b-core` | T2 | 0 | 95 calls / 400k ctx / 45 m | Deadline: 45 m; fallback: `w13-monitor` stall alert → Coordinator reassigns; termination: `w13-verifier` PASS + clean `conductor-join.py --no-push` | Red-first `T-NB-08` (all 7 states × 3 themes) + mutants, full `bash tools/run-tests.sh` green, `xaml-token-lint.py` & `TokenDisciplineTests` & `check-event-subscribers.py` & `ShellModelTests` & `ControllerSectionTests` & `Wave10UiTests` green, `w13-verifier` PASS | agy-swarm (`share`, enforced isolation); doorbell: board `@mention` + `swarm_wait` (enforced; `coord-mail` unsupported) |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| `w13a-core` | `w13a-core` | `share` | main | inherit |
| `w13a-ui` | `w13a-ui` | `share` | main | inherit |
| `w13b-core` | `w13b-core` | `share` | main | inherit |
| `w13b-ui` | `w13b-ui` | `share` | main | inherit |
| verifier | `w13-verifier` | `inherit` | main | inherit |
| monitor | `w13-monitor` | `inherit` | main | inherit |

**Cost & ~15× orchestrator-worker multiplier justification (GO6):**
The production orchestrator-worker topology carries roughly a **~15× token multiplier** compared to a single monolithic context. Splitting Wave 13 into exactly **4 tracks** (and striking 3 further splits below) is justified per track as follows:
- **`w13a-core` (Stage 1, width 1):** Justified by **context hygiene (`<400k` tokens)**, **share isolation**, and **independent verification** — `FoilDSL 4.2` grammar, rational Bernstein BFS certification, `TipCapMath`, and 4 CAD exporters (STL/3MF/STEP AP203/Rhino 3DM) form a dense computational-geometry domain that would otherwise exhaust a single session's context alongside Avalonia UI and AI Notebook code.
- **`w13a-ui` (Stage 2, width 2 alongside `w13b-core`):** Justified by **machine-time parallelism**, **genuine independence** (100% file-disjoint in `Desktop`), **exclusive OS display ownership**, and **context hygiene** — keeps Avalonia CAD canvas/properties work cleanly separated from AI Notebook runtime code.
- **`w13b-core` (Stage 2, width 2 alongside `w13a-ui`):** Justified by **machine-time parallelism**, **genuine independence** (zero `Desktop` UI touches; runs `--no-desktop` with zero display contention), **share isolation**, and **context hygiene** — isolates `FoilReq 1.0`, `GrillTreeEngine`, `AssistantEvalSuite` v2.0, loopback `HttpListener` wire tests, and `NotebookOrchestrator` within one clean `<400k` context.
- **`w13b-ui` (Stage 3, width 1):** Justified by **context hygiene (`<400k` tokens)**, **share isolation**, and **independent verification** — delivers the 5-tab, 7-state, 3-theme native Avalonia `NotebookWorkspaceView` + shell integration with exclusive OS display focus after both Stage 2 seams are frozen on `main`.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Stage 1 (`w13a-core`) before Stage 2 (`w13a-ui` and `w13b-core`) | Both `w13a-core` and `w13b-core` edit `AuthoringSession.cs` and `AnalysisChecks.cs` (fails GO5a); `w13a-ui` consumes `FoilDSL 4.2` Core APIs (`TipCapMath`, `AuthoredProjection.Tip`, `AuthoringSession.SetTip*`, `ProjectionContext.RoundedTipCap`) (fails GO5a/b). | `w13a-core` |
| Stage 3 (`w13b-ui`) after BOTH `w13a-ui` and `w13b-core` | Both `w13a-ui` and `w13b-ui` edit `WorkbenchController.cs` and `WorkbenchTests.cs` and need exclusive OS display focus for `Desktop.Tests` (fails GO5a/c); `w13b-ui` consumes `w13b-core`'s `NotebookOrchestrator` and `Core/Notebook` APIs. | `w13b-ui` |
| `conductor-join.py --no-push` merges onto `main` | Sequential single-writer primary branch convergence with full integrated `tools/join-ring.sh` + `run-verify-gates.py`. | Coordinator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `w13a-core -> w13a-ui` | `AuthoredProjection.{Tip, TipCapRatio}` (optional/defaulted ctor params so existing callers compile unchanged), public `AuthoringSession.{SetTipTangent, SetTipRadius, SetTipCap}` returning structured Core refusal with `LimitMeters` (mapped by `w13a-ui` onto Desktop `CommitOutcome.Refused` in `WorkbenchController.cs`; `ChannelEdits` stays `internal`), `Planform.TipRadiusMeters`, `WingEstimates.OverallSpanMeters`, `TipCapMath`, `Labels.RoundedTipCapNotice`, `ProjectionContext.RoundedTipCap`, `ExportSession` tip-closure row. | Frozen in `docs/design/rounded-wing-tips.md` and landed on `main` in Stage 1 before `w13a-ui` spawns. |
| `w13a-core -> w13b-core` | `AuthoringSession.cs` clean baseline (with `4.2` `EditReference` arms and tip-edit methods) and optional `FoilDSL 4.2` rounded-tip support available for `CandidateIntentDelta` seam. | Landed on `main` in Stage 1 before `w13b-core` spawns. |
| `w13b-core -> w13b-ui` | `Core/Notebook/*`, `Envelope.Notebook`, `AuthoringSession.HandoffNotebookCandidate`, `NotebookOrchestrator` (using `IProgress<T>` / existing `AssistantService.TelemetryEmitted`, zero new unsubscribed C# `event`s), `EffectiveModelId` on `AssistantService` & `PreparedAssistantCall`, backward-compatible 2-arg `AssistantService.RunEvaluationAsync(bool liveWire = false, CancellationToken ct = default)` preserved alongside the `targetModelId` overload so Desktop `AssistantCopy.cs:453` compiles during Stage 2 join, optional/defaulted new parameters on `RunRecord.Format`, `AssistantSettings`, `AssistantPreviewRequest`, `PreparedAssistantCall`, and `CheckModelEligibility`, and `AssistantEvalSuite` v2.0 (`GetSuiteDefinition().Count` = 11 active items). | Frozen in `docs/design/ai-guided-design-notebook.md` and landed on `main` in Stage 2 before `w13b-ui` spawns. |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| `w13a-export` (splitting STL/STEP/3DM export out of `w13a-core`) | Tightly coupled to `TipCapMath`, `Placement.MeshEtas`, and `FoilSource` `"4.2"` `Definition.TipCapRatio` (fails GO5 coupling test); running inside `w13a-core` avoids a redundant seam and ~15× multiplier. |
| `w13b-runtime` (splitting `Analysis/Notebook` out of `w13b-core`) | Tightly coupled to `Core/Notebook` DTOs, `AssistantValidators` internal helpers, and `AssistantEvalSuite` v2.0; building `Core + Persistence + Analysis + Cli` coherently in `w13b-core` lets loopback `HttpListener` end-to-end tests prove the entire stack in one clean share. |
| Parallel `w13a-core` and `w13b-core` | Both modify `src/CfdWorkbench.Core/AuthoringSession.cs` and `tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs` (violates GO5 single-owner rule). Running `w13a-core` in Stage 1 unlocks true zero-overlap width-2 parallelism (`w13a-ui` and `w13b-core`) in Stage 2. |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Commit `docs/coordination/wave13-rounded-tips-and-ai-notebook.{md,html}` + `docs-index.js` + audit entry on `main` (clean primary required before `conductor-join.py`). | T1 | Plan is the machine-readable contract; clean primary satisfies `COORD-JOIN-DIRTY-PRIMARY`. |
| 2 | Name swarm (`swarm{action:"title"}`), write `swarm/plan.json`, spawn `w13-monitor`, `w13-verifier` (`inherit`), and **Stage 1 `w13a-core`** (`share`). | T2 width 1 | Lands `FoilDSL 4.2` Core + Exports + Analysis on `main` first. |
| 3 | `w13-verifier` audits `w13a-core` (`VERDICT: PASS`) → Coordinator runs `conductor-join.py --no-push` for `w13a-core` → kill `w13a-core`. | T1 | Unblocks both Stage 2 tracks with zero shared authored files. |
| 4 | Spawn **Stage 2 in parallel (`w13a-ui` `share` and `w13b-core` `share`)** from updated `main`. | T2 width 2 | 100% file-disjoint; `w13a-ui` owns Desktop display, `w13b-core` runs `--no-desktop`. |
| 5 | As each finishes, `w13-verifier` audits (`VERDICT: PASS`) → Coordinator joins `w13a-ui` then `w13b-core` via `conductor-join.py --no-push` → kill both. | T1 × 2 | Sequential joins run full integrated `join-ring.sh` (including Desktop suite). |
| 6 | Spawn **Stage 3 `w13b-ui`** (`share`) from updated `main`. | T2 width 1 | Full native Avalonia `NotebookWorkspaceView` + Shell integration with exclusive OS display. |
| 7 | `w13-verifier` audits `w13b-ui` (`VERDICT: PASS`) → Coordinator joins `w13b-ui` via `conductor-join.py --no-push` → kill `w13b-ui` & `w13-verifier`, post `[done]` to quiet `w13-monitor`, kill `w13-monitor`. | T1 | Complete integrated Wave 13 landed cleanly on `main`. |
| 8 | Append **Planned vs Actual** to this plan, render HTML, run `docs-graph.py derive` + `check-docs.py`, append closing audit entry, and commit on local `main`. | T1 | Closes `/execute-with-coordination` DoD. |

## Planned vs Actual

| track | planned stage / width | actual outcome | verified HEAD → join commit on `main` | files changed | tests & live mutants | verifier & join gates |
|---|---|---|---|---|---|---|
| `w13a-core` | Stage 1 / width 1 (`45 m` cap) | Completed (~49 m incl. 1-min `PROOF-PII` test-string fix caught by `conductor-join.py` step 4) | `aafcba72` → `99ba06e7` | 27 (+2,252 / -109) | `T-RT-01..08` + live `Rhino3dm 8.35.0` `IsValid == true` + 8/8 mutants killed; full ring 2,446 PASS | `w13-verifier` PASS · `conductor-join.py` exit 0 · 10/10 verify gates |
| `w13a-ui` | Stage 2 / width 2 parallel (`35 m` cap) | Completed (~38 m, 100% file-disjoint from `w13b-core`, zero merge conflicts) | `03ba39bf` → `ef254d7a` | 9 (+1,180 / -54) | 9/9 `T-RT-09` (Light/Dark/HighContrast) + 3/3 mutants killed; full ring 2,455 PASS | `w13-verifier` PASS · `conductor-join.py` exit 0 · 10/10 verify gates |
| `w13b-core` | Stage 2 / width 2 parallel (`45 m` cap) | Completed (~38 m, finished 13 s after `w13a-ui`, 0 merge conflicts, backward-compatible seams compiled cleanly with untouched Desktop) | `204f3c5e` → `b911c987` | 32 (+5,779 / -80) | `T-NB-01..07` (real loopback `HttpListener` + local physics) + 4/4 mutants killed; full ring 2,463 PASS | `w13-verifier` PASS · `conductor-join.py` exit 0 · 10/10 verify gates |
| `w13b-ui` | Stage 3 / width 1 (`45 m` cap) | Completed (~61 m incl. 14 m remediation of 3 genuine `w13-verifier` P1 findings replacing synthetic curves/elevations with real 200p `PanelMethod`, real `ProductWingMethod` VLM loading, and real placed `SurfaceView` elevations) | `e4da15be` → `99f94510` | 24 (+5,295 / -56) | 8/8 `T-NB-08` (7 states × 3 themes + loopback `HttpListener`) + 3/3 mutants killed; full ring 2,471 PASS | `w13-verifier` PASS · `conductor-join.py` exit 0 · 10/10 verify gates |

**Execution takeaways (for next coordination plan):**
- **Stage 2 width-2 parallelism paid off cleanly:** `w13a-ui` and `w13b-core` finished within 13 seconds of each other with zero merge conflicts, zero OS display contention, and zero compile breaks because P0-1 backward-compatibility seams (`RunEvaluationAsync(bool, CancellationToken)`, optional ctor/record params, zero new unsubscribed C# `event`s) were enforced upfront in the plan and worker briefs.
- **Independent Adversary verification earned its cost twice:** `w13-verifier` caught 3 genuine scientific/ADR-0010 violations in `w13b-ui` (synthetic Cp/loading/elevation silhouettes) that green unit tests alone missed, resulting in 100% real `PanelMethod` / `ProductWingMethod` / `Placement.Surface` rendering across all 5 Notebook tabs.
- **Pre-join gate hygiene:** Remind Core/Desktop authors in every brief to run `py -3 tools/check-docs.py` (which enforces `PROOF-PII` on test fixtures) before posting final handback so `conductor-join.py` never has to stop at step 4.

---

## Status

| | |
|---|---|
| **Completed** | All 4 Wave 13 tracks (`w13a-core`, `w13a-ui`, `w13b-core`, `w13b-ui`) implemented, independently verified by `w13-verifier` (18/18 live mutants killed), and converged onto local `main` (`99f94510`) via `conductor-join.py --no-push` with **2,471 tests passing** (`Core` 1,041 · `Analysis` 357 · `Desktop` 1,014 · `Cli` 48 · `Python` 11), `xaml-token-lint.py` clean, `check-event-subscribers.py` clean (`57` events, `4` allowed, `0` findings), and `10/10` verify gates green. Swarm retired cleanly. |
| **Remaining** | Push local `main` to `origin/main` once operator confirms; run manual smoke exploration of Rounded Wing Tips + AI-Guided Design Notebook in the Avalonia desktop app (and external CAM `SPIKE-CAD-02` open-and-measure when ready). |
| **Best next action** | Review the Wave 13 summary on local `main`, then push `main` to `origin` when ready. |
