---
id: coordination-wave10-cfd-run-results-and-rail
title: "Coordination plan - wave 10: Area 5 CFD Run Pipeline, Area 6 Results & Candidate Ladder, and 7-Area Left Activity Rail"
type: plan
status: active
owner: "@timianmalloo"
tags: [coordination, worktrees, area-5, area-6, cfd-run, results, candidates, activity-rail, agy-swarm, wave10]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: decision-seven-areas, rel: implements }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: implements }
  - { to: adr-0012-openfoam-backend-macos, rel: depends-on }
  - { to: design-guided-solver-setup, rel: depends-on }
  - { to: design-local-sweeps, rel: relates-to }
  - { to: design-setup-seeding, rel: relates-to }
  - { to: coordination-wave8-remaining-w5-w6-w7-closure, rel: refines }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-23"
summary: >-
  Wave 10 completes the end-to-end 7-area product flow across Areas 5, 6, and shell navigation: W10-DESIGN produces
  Adversary-cleared design slices for Area 5 CFD Run Pipeline, Area 6 Results & Candidate Ladder, and the 7-Area Left
  Activity Rail; W10-RUN implements the pure STORE-SUBSET Area 5 typed case model, OpenFOAM/SU2 emitters (verified by
  cases/tools/foam-dict-lint.py), DR-F3-1 A mesh gate, 11-state pipeline, complete cfdw-project-2 additive cfd envelope,
  harvester, Cancel/Resume/orphan scan, recursive STORE-SUBSET widening, and CLI; W10-RESULTS implements Area 6
  FieldEvidenceManifest (separation gated on tau_w), ProvenanceStrip, ReplaySequence, DifferenceFlood, CandidateLadder
  with Accept-as-draft CAD handoff, ParaView process-only handoff, and CLI; W10-RAIL-UI implements the 7-Area Left
  Activity Rail docking existing Setup/CAD/Analysis/Sweeps plus new RunWorkspaceView and ResultsWorkspaceView without
  view rewrites (Rulings 271/272).
---

# Coordination plan: wave 10 — Area 5 CFD Run Pipeline, Area 6 Results & Candidate Ladder, and 7-Area Left Activity Rail

**Input.** Operator directive of 2026-11-23 following Wave 8 push (`254b0d51`) and **Ruling 275** (`687ca479`, 0
unapproved `COPY-*` rows remaining in `DESIGN.md`): plan and execute **Wave 10** — Area 5 CFD Run Pipeline, Area 6
Results & Candidate Ladder, and the 7-Area Left Activity Rail — autonomously via AGY swarm (continuing the operator's
2026-11-23 directive to exclude screen-reader/VoiceOver/NVDA features).

**Goal state.**
- **Goal:** Complete the 7-area end-to-end CFD-Workbench architecture on `main`: (1) design slices for Area 5 Run,
  Area 6 Results/Candidates, and the 7-Area Left Activity Rail (`W10-DESIGN`); (2) Area 5 deterministic CFD Run
  Pipeline (`W10-RUN`: `TypedCaseModel`, ESI OpenFOAM & SU2 `.cfg` emitters verified by `cases/tools/foam-dict-lint.py`,
  `DR-F3-1 A` / `DR-F2-3` `MeshGateEvaluator`, 11-state `CfdRunStateMachine`, complete additive `cfdw-project-2` `cfd`
  envelope preserving byte-identical `-1` & pre-W10 `-2` output, `CfdOutputHarvester` with `DR-F2-1` three-part
  residual check, substrate Cancel/Resume/orphan scan, recursive `tools/store_subset.py` widening, CLI); (3) Area 6
  Results, Replay & Candidate Ladder (`W10-RESULTS`: `FieldEvidenceManifest` with separation strictly gated on wall
  $\tau_w$, `ProvenanceStrip`, `ReplaySequence`, `DifferenceFlood` `"Unavailable — mesh differs"` guard,
  `CandidateLadder` + `Accept as draft` CAD handoff, process-only ParaView handoff with honest absence state, CLI); and
  (4) Desktop 7-Area Left Activity Rail + `RunWorkspaceView` + `ResultsWorkspaceView` (`W10-RAIL-UI`), docking existing
  `Setup`, `CAD`, `Analysis`, and `Sweeps` workspaces with zero rewrites (Rulings 271 & 272).
- **Done when:** All four serial tracks (`W10-DESIGN` → `W10-RUN` → `W10-RESULTS` → `W10-RAIL-UI`) pass red-first tests
  and live mutants, clear independent Adversary review (`w10-verifier`: Hydrodynamicist + Security & Identity + Data &
  Persistence + Marine-CAD UX + Test Architect + Simplifier), and merge serially onto `main` via
  `conductor-join.py --no-push` with `TESTS-GREEN`, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings,
  `UI-CS-TOKEN` 0 findings, recursive `STORE-SUBSET` ok, `check-proof-pii.py` 0 hits, `check-docs.py` exit 0, and
  `10/10` `run-verify-gates.py` PASS; Planned vs Actual is recorded and committed.
- **Not in scope (honestly Struck / Operator-excluded):**
  - Live WSL `openfoam2512` solver execution (`A5-RESUME`) — Cisco Umbrella `net.proxy-tls` still blocks
    `sourceforge.net` (`curl rc=60` measured 2026-11-23); Area 5 tests exercise the real `SolverLauncher` (`M1–M8`), real
    `cases/tools/foam-dict-lint.py` oracle, real filesystem case/harvester/checkpoint I/O, and an injected
    `ICfdRunSubstrate` so zero fake OpenFOAM solver receipts are ever claimed.
  - Live ParaView 5.12+ binary launch — host probe 2026-11-23 measured `PARAVIEW_FOUND=False`; `"Open in ParaView"`
    verifies both the honest absence string on this host and process-only case-directory handoff via an injected probe
    path (zero VTK linking per spec A5.11).
  - Accessibility / screen-reader work (NVDA / VoiceOver / Narrator) — explicitly excluded per operator directive of
    2026-11-23.
  - Cross-area live BYOK LLM calls (Wave 11) — Wave 10 provides deterministic offline key-free `AI-01`/`AI-11` slots
    (`Explain this failure`, `Preview case diff`, `Ask about this result`) with typed validators and zero network model
    calls.
  - Pushing new Wave 10 commits on `main` to `origin` without operator confirmation.
- **Tier:** T2 · **Fan-out cap:** 1 concurrent author (Simplifier/GO5 serialization across 4 right-sized `share` tracks
  giving 0 merge conflicts on `AuthoringSession.cs`, `RunRecord.cs`, `Labels.cs`, `Program.cs`, `Styles.axaml`,
  `DESIGN.md`, `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, `WorkbenchTests.cs`, and 0 real-display Desktop
  harness timeouts).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w10-coord`). Owns plan, seams, join order, `conductor-join.py --no-push`
  merges, and closeout. Never authors track code.
- **Owner:** Independent swarm worker `w10-owner` (`workspace: "inherit"`, read-only on `src/`), spawned only if a
  mid-wave decision request arises (e.g., if `W10-DESIGN` or `W10-RUN` surfaces a genuine spec ambiguity).
- **Adversary Verifier:** Independent swarm worker `w10-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track in Adversary Mode before join.
- **Stall Monitor:** Swarm worker `w10-monitor` (`workspace: "inherit"`, loads `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3, 5, 6, 7, and 8).

## Layer state

Measured 2026-11-23 15:47Z (full `coord-core.py doctor`) and re-verified 2026-11-23 20:36Z (`session list`, `leader who`,
`git worktree list`, `PARAVIEW_FOUND` host probe) in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `687ca479`,
clean).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| sessions | `0 active session(s); 249 file(s) scanned` | clean start |
| requests | `43 terminal requests (86 JSONL lines), 0 open` | zero open decision requests after Ruling 275 |
| worktrees | `1 (D:/Projects/CFD-Workbench 687ca479 [main])` | primary only |
| ParaView host probe | `PARAVIEW_FOUND=False` | process-only handoff tested with honest absence string + injected path; zero VTK linking |
| OpenFOAM TLS host probe | `dl.openfoam.com rc=28`; `sourceforge.net rc=60` (`net.proxy-tls`) | live WSL OpenFOAM execution struck; real `cases/tools/foam-dict-lint.py` + `SolverLauncher` + `ICfdRunSubstrate` tested |
| `DESIGN.md` high-water mark | `COPY-805` (0 unapproved rows after Ruling 275) | Wave 10 reserves `COPY-806..865` (proposed) |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifier/monitor) | each track gets a clean linked working copy; `git config core.longpaths true` active |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token wakeup; `w10-monitor` attached |
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
| `docs/design/{cfd-run-pipeline.{md,html},results-and-candidates.{md,html}} (new)` | authored, single owner | `W10-DESIGN` owns in Wave 10A | serial |
| `src/CfdWorkbench.Core/CfdRun/** (new — pure zero-I/O)`, `src/CfdWorkbench.Core/{AuthoringSession.cs,RunRecord.cs}`, `src/CfdWorkbench.Analysis/CfdRun/** (new — all I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{CfdRunVerb.cs (new),Program.cs}`, `tools/store_subset.py` (widen `.glob("*.cs")` → `.rglob("*.cs")` skipping `bin`/`obj`) | authored, single owner | `W10-RUN` owns in Wave 10B (after `W10-DESIGN` joins; lands complete `cfd: { runs, fieldEvidence, candidates }` envelope definition) | serial |
| `src/CfdWorkbench.Core/Results/** (new — pure zero-I/O)`, `src/CfdWorkbench.Core/AuthoringSession.cs` (only if candidate/evidence append helpers needed on `cfd` member), `src/CfdWorkbench.Analysis/Results/** (new — all I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{ResultsVerb.cs (new),Program.cs}` | authored, single owner | `W10-RESULTS` owns in Wave 10C (after `W10-RUN` joins) | serial |
| `src/CfdWorkbench.Desktop/{ActivityRail.axaml (new),ActivityRail.axaml.cs (new),Run/** (new),Results/** (new),ModelArea.axaml,ModelArea.axaml.cs,WorkbenchController.cs,MainWindow.axaml.cs,Styles.axaml (non-theme tokens only — ZERO new theme Brush keys)}`, `src/CfdWorkbench.Desktop/Shell/{ShellHost.cs,CommandTable.cs,StatusStrip.axaml (only if needed),StatusStrip.axaml.cs (only if needed)}`, `DESIGN.md` (`COPY-806..865` proposed; zero edits to approved rows) | authored, single owner | `W10-RAIL-UI` owns in Wave 10D (after `W10-RESULTS` joins) | serial |
| `tests/**` (incl. explicit harness runners `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, `ShellModelTests.cs`, `WorkbenchTests.cs`) | authored, serial | strictly one owner per wave (width 1) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- Strictly **one** author active per wave (`W10-DESIGN` → `W10-RUN` → `W10-RESULTS` → `W10-RAIL-UI`): zero concurrent
  writers on `AuthoringSession.cs`, `RunRecord.cs`, `Labels.cs`, `Program.cs`, `Styles.axaml`, `DESIGN.md`,
  `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, `WorkbenchTests.cs`, or the real-display Desktop harness.
- **`tools/store_subset.py` (`STORE-SUBSET`, run via `tools/check-docs.py`):** today scans top-level `.glob("*.cs")`;
  in `W10-RUN` (`C1`), `tools/store_subset.py:45-46` is widened to recursive `.rglob("*.cs")` (skipping `bin/`/`obj/`)
  over `src/CfdWorkbench.Core/` and `tests/CfdWorkbench.Core.Tests/` (token set filesystem/env/PInvoke/persistence
  primitives; allowlist 6 `PARTITION_EXEMPT` files only). ALL `Core/{SolverSetup,CfdRun,Results}/` types and
  `Core.Tests` are strictly pure zero-I/O; ALL filesystem/process/harvester/ParaView I/O lives in
  `src/CfdWorkbench.Analysis/{CfdRun,Results}/` and `tests/CfdWorkbench.Analysis.Tests/`: jointly satisfiable.
- **`ArchitectureTests.cs` (`Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt`, `C3`):** scans IL calls from
  `CfdWorkbench.Analysis` into `CfdWorkbench.Core` and fails on ANY called Core method named `ProfileAt`, `Cancel`,
  `Undo`, `Redo`, or prefixed `Begin*`, `Update*`, `Apply*`. All `Core/CfdRun/` and `Core/Results/` methods invoked by
  `CfdWorkbench.Analysis` MUST avoid those names/prefixes (use `TransitionToCancelled`, `EvaluateTransition`,
  `BuildGeometryDraft`, etc.): jointly satisfiable.
- **`xaml-token-lint.py`, `tools/check-event-subscribers.py`, `Styles.axaml` theme-brush closed set
  (`tools/verify-application-adapters.py:123-130`), and `TokenDisciplineTests.cs` (`UI-CS-TOKEN`):**
  - `xaml-token-lint.py`: root `src/CfdWorkbench.Desktop/` (`.xaml`/`.axaml`), zero raw colors/sizes, allowlist none.
  - `tools/check-event-subscribers.py`: root `src/` (`*.cs`), every `event` declaration under `src/` must have ≥ 1
    `<name> +=` subscriber under `src/`; 4-entry `ALLOWED` set.
  - `Styles.axaml` theme dictionaries (`Light`/`Dark`/`HighContrast`): **ZERO** new `<SolidColorBrush x:Key="...Brush">`
    keys added inside theme dictionaries; reuse existing theme brush keys only.
  - `TokenDisciplineTests.cs`: root `src/CfdWorkbench.Desktop/` (`**/*.cs`), all resource keys resolve, zero
    `FontSize < 11` / `DrawText`/`FormattedText` `< 11`, zero `Color.Parse(...)`, allowlist empty. Jointly satisfiable.
- **`ShellHost`, `ModelArea.Navbar`, `Architecture_DockConfinedToShell`, & `ControllerSectionTests` (`C4`):**
  - `ShellHost` uses `ColumnDefinitions="Auto,*"` keeping its 3 rows (`*,Auto,Auto`): `ActivityRail` at
    `(Col=0, Row=GetRow(DockHost), RowSpan=1)` (shifted in `ShowMenuBar()` alongside `DockHost`/`AnalysisPanel`/`StatusStrip`),
    `DockHost` & `AnalysisPanel` in `Col=1`, and `MenuBar`, `StatusStrip`, `paletteBackdrop` spanning `ColumnSpan=2`
    (preserving `Shell_F1_NoSidebarHeaderBand`, `ControllerShellOneDriveTests.NoticeRow`, and
    `StatusStrip_SitsAtWindowBottom_FullWidth_24px`).
  - `ModelArea.Navbar` keeps its existing 4 buttons (`Setup | CAD | Analysis | Sweeps`) synchronized with `ActivityRail`
    (preserving `ControllerViewTests.ModelArea_Navbar_KeyboardPath_TabAfterTheViews`).
  - `ActivityRail.axaml.cs` has ZERO `Dock.*` references (`Architecture_DockConfinedToShell`), and new `view.run` /
    `view.results` commands are `Modal: false` under `"View"` (`ControllerSectionTests.Commands_ModalFlag_MatchesRowsThatShowADialog`).
    Jointly satisfiable.
- **`tools/check-reader-sharing.py`, `tools/coordination/check-test-paths.py`, `tools/check-proof-pii.py`,
  `verify-no-machine-paths.py`, `verify-no-new-console-launches.py`, `design-lint.py --strict`:**
  - All `src/**/*.cs` readers use `UserFile.OpenRead` or explicit `FileShare.Read | FileShare.Delete` /
    `FileShare.ReadWrite | FileShare.Delete`.
  - Zero relative `"docs/..."` reads in `tests/CfdWorkbench.Core.Tests/*.cs` (allowlist `docs/examples/foildsl/` &
    `Fixtures/` only).
  - Zero `CREATE_NEW_CONSOLE`, zero Windows user-profile paths or machine SIDs, zero raw `#rgb`/`#rrggbb` hexes in
    `DESIGN.md`, zero committed binary captures (`.vtk`, `.vtp`, `.foam`, `.png`). Jointly satisfiable.
- **`cases/tools/foam-dict-lint.py` (`M4` emitter oracle, `C2`):** `allowSystemOperations 0;` lives in the app-owned
  HOME `.OpenFOAM/2512/controlDict` written by `SolverLauncher` (`M1`); `OpenFoamCaseEmitter` emits case
  `system/controlDict` with `runTimeModifiable false;` + `cfdw-manifest.json` in the case root, and EVERY emitted
  `type <v>` token stays strictly inside `cases/tools/foam-dict-lint.py`'s 21-entry `TYPES_ALLOWED` set (`forceCoeffs`,
  `solverInfo`, `yPlus`, `surfaces`, `wallShearStress`, `patch`, `wall`, `empty`, `symmetryPlane`,
  `freestreamVelocity`, `freestreamPressure`, `noSlip`, `zeroGradient`, `inletOutlet`, `fixedValue`, `calculated`,
  `triSurfaceMesh`, `box`, `cellSet`, `hePsiThermo`, `symmetry` — zero unlisted BC `type`s and zero `"forces"`).
  Jointly satisfiable.

**Quoted ADR / Ruling trigger lines (DC-189 — exact citations, never paraphrased).**
- **ADR-0010 §Decision (4)** (`docs/adr/0010-one-placement-rule.md:81-83`):
  > "4. **Displays receive geometry, never compute it.** Core exposes `Placement.Surface` (a placed mesh of the starboard
  > half) and `Placement.Frame` (channel values at η). The Desktop applies only a camera and the language's symmetry map
  > `(x, y, z) → (x, −y, z)` (`Point3.Port()`, defined in Core)."
- **ADR-0011 §Decision (1)** (`docs/adr/0011-analysis-run-storage.md:53-56`):
  > "1. **Representation.** Runs are append-only facts in the native project, under one new optional top-level member
  > `analysis: { runs: [...], polarSamples: [...], pruned: [...] }`. It is written only when at least one run exists
  > (`WhenWritingNull`). The format string is derived by **one function** from the run count: no run → `cfdw-project-1`,
  > byte-identical to today's writer; one or more → `cfdw-project-2`. Nothing else in the document changes."
- **Ruling 68** (`docs/notes/rulings.md:807`):
  > "… (1) Apply docs/notes/solver-security-right-size.md: ADR-0012 D2 becomes the six short rules (code off via the
  > app-owned controlDict, a live Disallowing banner proves it, argv only, only cases the app wrote, own run directory
  > with a clean environment, resource caps); spec A8.5 backend-substrate row and A5.10 readiness take the verified facts
  > and the M1/M2/M4 rules; Run is enabled when the install smoke test shows Disallowing; only S-1 and S-2 serial stay,
  > as the pin-change check. …"
- **Ruling 271** (`docs/notes/rulings.md:2025`):
  > "… DR-SWP-6: option (a) approved. Add a Sweeps workspace segment beside CAD | Analysis in the existing ModelArea
  > navbar (CAD | Analysis | Sweeps), containing three discrete sub-tabs (Experiment setup, Run (local), Results)
  > enforcing XS-03 / UX-17 (zero Run button in Experiment setup; Queue hands off to Run (local)), dockable onto future
  > 7-area rail slots 4-6 without view rewrite. …"
- **Ruling 272** (`docs/notes/rulings.md:2031`):
  > "… When the full 7-area left activity rail later ships, SetupView docks directly onto rail slot 1 with zero view
  > rewrite, matching DR-SWP-6 (a) shell migration symmetry. …"

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **W10-DESIGN** Design Slices: Area 5 CFD Run Pipeline, Area 6 Results & Candidates, and 7-Area Left Activity Rail | `docs/design/cfd-run-pipeline.{md,html} (new)`, `docs/design/results-and-candidates.{md,html} (new)` | S0 plan committed on `main` (Wave 10A, docs-only) | T2 | 1 | 80 calls · 220k · 1 h · ceiling 300k | Two Adversary-cleared `/design-slice` documents (rendered to `.html` via `render-markdown.py`, V2 frontmatter linked to `spec-cfd-workbench-v1`, `adr-0011-analysis-run-storage`, `design-guided-solver-setup`, `decision-seven-areas`, `rulings`): (1) `docs/design/cfd-run-pipeline.md`: E7 surface list, complete additive `cfdw-project-2` `cfd: { runs, fieldEvidence, candidates }` schema preserving byte-identical `-1` & pre-W10 `-2` output + fail-closed `DOC-UNSUPPORTED-FIELD` on older builds, `TypedCaseModel` (OpenFOAM ESI v2512 + SU2 v8.5.0 emitters honoring `cases/tools/foam-dict-lint.py` 21-entry `TYPES_ALLOWED`, `runTimeModifiable false;`, `cfdw-manifest.json`, and HOME `controlDict` M1 separation), `DR-F3-1 A` / `DR-F2-3` `MeshGateEvaluator`, 11-state `CfdRunStateMachine` (naming avoiding `ArchitectureTests` banned names), `DR-F2-1` three-part residual criterion, `LAB-01` three-part `Converged` label (*"not quantified (single mesh)"* part 3 in v1), `CfdOutputHarvester` fail-closed column contract, substrate Cancel/Resume/orphan scan, and test/mutant matrix; (2) `docs/design/results-and-candidates.md`: E7 surface list, `FieldEvidenceManifest` (separation layer strictly gated on wall $\tau_w$ with named criterion — never vortex criterion), `ProvenanceStrip` (all 9 spec A5.11 fields + table twin), `ReplaySequence` (fixed range/camera + failed-sample pause/clear + `"Unavailable — mesh differs"` difference flood), `CandidateLadder` (cross-tier Pareto/feasibility + `Accept as draft` CAD draft handoff + base-revision drift Rebase guard), process-only `"Open in ParaView"` handoff (zero VTK), and 7-Area Left Activity Rail `Auto,*` 3-row `ShellHost` layout docking existing `SetupWorkspaceView`, `CAD`/`Analysis`, `SweepsWorkspaceView` (slot 4), `RunWorkspaceView` (slot 5), `ResultsWorkspaceView` (slot 6), and `ExportDialog` (slot 7) without rewriting existing views; `tools/check-proof-pii.py`, `docs/ai-forward-pack/scripts/verify-ruling-citations.py`, `tools/check-docs.py` exit 0; Adversary pass (`w10-verifier`: Hydrodynamicist + Data & Persistence + Security + UX + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W10-RUN** Area 5 CFD Run Pipeline (Pure Core + Complete `cfdw-project-2` `cfd` Envelope + Recursive `STORE-SUBSET` + Analysis Emitters/Harvester/Pipeline + CLI) | `src/CfdWorkbench.Core/CfdRun/{TypedCaseModel.cs,OpenFoamCaseEmitter.cs,Su2CaseEmitter.cs,MeshGateEvaluator.cs,CfdRunRecords.cs,CfdRunStateMachine.cs} (new — strictly pure zero-I/O)`, `src/CfdWorkbench.Core/{AuthoringSession.cs,RunRecord.cs}`, `src/CfdWorkbench.Analysis/CfdRun/{CfdCasePipelineService.cs,CfdOutputHarvester.cs,CfdRunProjection.cs,ICfdRunSubstrate.cs} (new — all I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{CfdRunVerb.cs (new),Program.cs}`, `tools/store_subset.py` (widen `.glob("*.cs")` → `.rglob("*.cs")` skipping `bin`/`obj`), `tests/CfdWorkbench.Core.Tests/{CfdRunModelTests.cs (new),CfdRunEnvelopeTests.cs (new),IdentityTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/{CfdRunPipelineTests.cs (new),CfdHarvesterTests.cs (new),AnalysisChecks.cs}`, `tests/CfdWorkbench.Cli.Tests/{CfdRunCliTests.cs (new),CliTests.cs}` | `W10-DESIGN` joined on `main` (Wave 10B) | T2 | 1 | 220 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + live mutants killed: (1) **Pure `STORE-SUBSET` Core (`src/CfdWorkbench.Core/CfdRun/`, avoiding `ArchitectureTests` banned names `Cancel`/`Undo`/`Redo`/`ProfileAt`/`Begin*`/`Update*`/`Apply*`):** `TypedCaseModel` (SA/k-omega SST, wall-resolved vs wall-function `DR-F2-3` feasibility check, inflow-angle $\alpha$ imposition, single mesh hash per held geometry & speed), deterministic `OpenFoamCaseEmitter` (case `system/controlDict` with `runTimeModifiable false;` + `cfdw-manifest.json`, `fvSchemes`, `fvSolution`, `blockMeshDict`, `snappyHexMeshDict`, `0/U`, `0/p`, `0/nuTilda` using strictly `cases/tools/foam-dict-lint.py` 21-entry `TYPES_ALLOWED` — `forceCoeffs`, `solverInfo`, `yPlus`, `wallShearStress`, `surfaces`; `allowSystemOperations 0` stays in `SolverLauncher`'s app-owned HOME `controlDict` per M1) & `Su2CaseEmitter` (`.cfg`, zero `#codeStream`/`#calc`/macros), `MeshGateEvaluator` (`DR-F3-1 A`: validity set, non-orthogonality ≤ 70°, internal skewness ≤ 4, boundary skewness ≤ 20, positive volumes, layer coverage ≥ 20 on ≥ 95% / ≥ 10 on 100%, wall-resolved $y^+$ p95 ≤ 1 / max ≤ 2), 11-state `CfdRunStateMachine` (`Pending`..`Cancelled`, capability check returning `Unsupported` for SU2 + free surface/cavitation before leaving `Pending`, `exit 0` + missing outputs → `Failed("no outputs")`, three-part `Converged` label with `"not quantified (single mesh)"`), and complete `CfdRunRecords` (`CfdEnvelope`: `runs`, `fieldEvidence`, `candidates`, BLAKE3(JCS) tamper detection); (2) **Additive `cfdw-project-2` `cfd` envelope (`AuthoringSession.cs`, `RunRecord.cs`):** `Exact` optional list widened to `["analysis", "setup", "experiments", "cfd"]`; `RunRecord.Format(int runCount, int experimentCount = 0, bool hasSetup = false, bool hasCfd = false)` keeps `-1` and pre-W10 `-2` serialization **byte-identical** (`Project1_NoRun_ByteIdenticalToToday` stays green!), `AuthoringSession.RecordCfdRun` append-only under session lock with tamper-check on read, `.v1.bak` on `-1` → `-2`, `DOC-SIZE` preflight; (3) **Widen `tools/store_subset.py` (`C1`)** to `.rglob("*.cs")` skipping `bin/`/`obj/` and verify exit 0 across all Core/Core.Tests subdirectories; (4) **Analysis I/O (`src/CfdWorkbench.Analysis/CfdRun/`):** `CfdCasePipelineService` (case dir named strictly from `runKey`, never user text; invokes `SolverLauncher` `M1–M8`; `ICfdRunSubstrate` with real filesystem staging + Cancel escalation + orphan scan + harvester-validated checkpoint Resume), **`M4` CI test executing real `cases/tools/foam-dict-lint.py`** over emitted full OpenFOAM case tree (exit 0 clean / exit 3 hostile), `CfdOutputHarvester` parsing OpenFOAM `forceCoeffs`/`solverInfo`/`yPlus`/`wallShearStress`/surface samples & SU2 `history.csv` (fail-closed on unknown column layout; `DR-F2-1` three-part residual criterion: initial residual ≤ 1e-3, stationary $U_I$ half-band, clean zero-$\tilde{\nu}$-bounding window); (5) CLI `cfd-run emit|status|harvest --json`; (6) All new test classes explicitly registered in `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`; `bash tools/run-tests.sh` `TESTS-GREEN` + all static/doc gates exit 0; Adversary pass (`w10-verifier`: Hydrodynamicist + Security HARD VETO + Data & Persistence HARD VETO + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W10-RESULTS** Area 6 Results, Replay, Difference Flood & Candidate Ladder (Pure Core + Analysis Service/Projection + ParaView Handoff + CLI) | `src/CfdWorkbench.Core/Results/{FieldEvidenceManifest.cs,ProvenanceStrip.cs,ReplaySequence.cs,DifferenceFlood.cs,CandidateLadder.cs} (new — strictly pure zero-I/O)`, `src/CfdWorkbench.Core/AuthoringSession.cs` (only if candidate/evidence record append helpers needed on `cfd` member), `src/CfdWorkbench.Analysis/Results/{ResultsService.cs,ResultsProjection.cs,ParaViewHandoff.cs} (new — all I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{ResultsVerb.cs (new),Program.cs}`, `tests/CfdWorkbench.Core.Tests/{ResultsDomainTests.cs (new),CandidateLadderTests.cs (new),IdentityTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/{ResultsServiceTests.cs (new),AnalysisChecks.cs}`, `tests/CfdWorkbench.Cli.Tests/{ResultsCliTests.cs (new),CliTests.cs}` | `W10-RUN` joined on `main` (Wave 10C) | T2 | 1 | 200 calls · 380k · 2 h 15 min · ceiling 400k | Red-first tests + live mutants killed: (1) **Pure `STORE-SUBSET` Core (`src/CfdWorkbench.Core/Results/`, avoiding `ArchitectureTests` banned names):** `FieldEvidenceManifest` strictly enforcing spec A5.11 VIZ rule (**separation layer available ONLY when wall $\tau_w$ exists** with named criterion; Q/$\lambda_2$ labelled `"vortex-core candidates at threshold t"`, never separation), `ProvenanceStrip` (all 9 required provenance fields + table twin), `ReplaySequence` (admitted samples of one compatible series at held speed or angle, fixed range/camera with labelled unlock, failed sample pauses & clears, zero interpolation, explicit Play/Step/Scrub), `DifferenceFlood` (pinned at zero for matching meshes; strictly returns `"Unavailable — mesh differs"` when mesh hashes differ), `CandidateLadder` (Pareto/feasibility ranking across VLM + CFD tiers, cross-tier discrepancy pinning, **`Accept as draft`** via `BuildGeometryDraft` emitting a typed CAD `GeometryEditDraft` — never mutating geometry directly — and refusing with Rebase deviation offer when base `AcceptedId` moved); (2) **Analysis Service & Projection (`src/CfdWorkbench.Analysis/Results/`):** `ResultsService` & `ResultsProjection` unifying completed local VLM/Sweep runs and harvested Area 5 `CfdRunRecord`s into surface $C_p$/$C_f$ scalar arrays over `Placement.Surface`, sweep small-multiples, difference floods, table twins, and Candidate ladder; `ParaViewHandoff` (process-only `UseShellExecute = false`, `CreateNoWindow = true`, zero VTK linking, returns honest absence message when ParaView 5.12+ is absent on host and hands off case directory cleanly with an injected path in tests); (3) CLI `results inspect|candidates|paraview --json`; (4) All new test classes registered in `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`; `bash tools/run-tests.sh` `TESTS-GREEN` + all static/doc gates exit 0; Adversary pass (`w10-verifier`: Hydrodynamicist + Data & Persistence HARD VETO + Security + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W10-RAIL-UI** Desktop 7-Area Left Activity Rail + Area 5 `RunWorkspaceView` + Area 6 `ResultsWorkspaceView` | `src/CfdWorkbench.Desktop/{ActivityRail.axaml (new),ActivityRail.axaml.cs (new — zero Dock.* refs),ModelArea.axaml,ModelArea.axaml.cs,WorkbenchController.cs,MainWindow.axaml.cs,Styles.axaml (non-theme tokens only — ZERO new theme Brush keys)}`, `src/CfdWorkbench.Desktop/Run/{RunWorkspaceView.axaml (new),RunWorkspaceView.axaml.cs (new),RunCopy.cs (new)}`, `src/CfdWorkbench.Desktop/Results/{ResultsWorkspaceView.axaml (new),ResultsWorkspaceView.axaml.cs (new),ResultsFieldCanvas.cs (new),ResultsCopy.cs (new)}`, `src/CfdWorkbench.Desktop/Shell/{ShellHost.cs,CommandTable.cs,StatusStrip.axaml (only if needed),StatusStrip.axaml.cs (only if needed)}`, `DESIGN.md` (`COPY-806..865` proposed; zero edits to approved rows; zero raw hexes in body), `tests/CfdWorkbench.Desktop.Tests/{Wave10UiTests.cs (new),ShellModelTests.cs,ShellWindowTests.cs,StatusStripTests.cs,WorkbenchTests.cs}` | `W10-RESULTS` joined on `main` (Wave 10D) | T2 | 1 | 220 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + live mutants killed: (1) **7-Area Left Activity Rail (`ActivityRail.axaml(.cs)`, zero `Dock.*` refs per `Architecture_DockConfinedToShell`):** hosted in `ShellHost` via `ColumnDefinitions="Auto,*"` keeping exact 3 rows (`*,Auto,Auto` — `ActivityRail` at `Col=0, Row=GetRow(DockHost), RowSpan=1` shifted in `ShowMenuBar()`; `DockHost`/`AnalysisPanel` in `Col=1`; `MenuBar`/`StatusStrip`/`paletteBackdrop` `ColumnSpan=2`), preserving `Shell_F1_NoSidebarHeaderBand`, `NoticeRow`, and `StatusStrip_SitsAtWindowBottom_FullWidth_24px`. Displays all 6 document areas in flow order (**Setup · CAD · Analysis · Experiment · Run · Results**) with live derived readiness badges, plus **Export** (opens existing `ExportDialog`) and **Solver setup** footer button, docking existing `SetupWorkspaceView` (slot 1, zero rewrite per Ruling 272), `CAD`/`Analysis` (slots 2–3), and `SweepsWorkspaceView` (slot 4, zero rewrite per Ruling 271), keeping `ModelArea`'s existing 4-button `Navbar` (`Setup \| CAD \| Analysis \| Sweeps`) synchronized without altering its Tab order (`ControllerViewTests`); (2) **`RunWorkspaceView` (Area 5 Run, slot 5):** 11-state run queue & live status panel, `MeshGate` breakdown, residuals/forces table, `Run` / `Cancel` / `Retry sample` / `Set up a solver` (jumps to `SolverSetupView`), offline key-free `Explain this failure` / `Preview case diff` (`AI-01`/`AI-11` slot); (3) **`ResultsWorkspaceView` (Area 6 Results, slot 6):** sample picker, `FieldEvidenceManifest`-driven layer toggles (separation strictly gated on $\tau_w$), `ResultsFieldCanvas` rendering surface $C_p$/$C_f$ scalar field + isolines using `PropFontFamily`/`PropFontSize` ≥ 11 px and existing theme brush keys (0 `Color.Parse` calls, `UI-CS-TOKEN` 0 findings), 9-field `ProvenanceStrip` + linked table twin, Replay controls (Play / Step / Scrub / Hold / locked range; failed sample pauses & clears), Difference flood (`"Unavailable — mesh differs"`), `CandidateLadder` list with `Accept as draft` jumping cleanly to CAD draft, and `"Open in ParaView"` handoff; (4) `CommandTable.cs` `view.run` / `view.results` entries under existing `"View"` menu (`Modal: false`, `ShellModelTests.cs` parity updated, `ControllerSectionTests` safe), zero new theme `...Brush` keys in `Styles.axaml`, `DESIGN.md` `COPY-806..865` proposed (`design-lint --strict` 0 warnings); (5) `--wave10-ui` wired into `WorkbenchTests.cs` (`DesktopChecks.Spawn`, `ScaleModes`, AND `DesktopChecks.PrintScaleContext("wave10-ui")`), `bash tools/run-tests.sh` `TESTS-GREEN`, `xaml-token-lint`, `UI-CS-TOKEN`, `check-event-subscribers`, `check-docs` all exit 0; Adversary pass (`w10-verifier`: Marine-CAD UX + Hydrodynamicist + Security + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `w10-monitor` | `inherit` | `main` | inherit |
| Adversary verifier | `w10-verifier` | `inherit` | `main` | inherit |
| `W10-DESIGN` (Wave 10A) | `w10-arch-design` | `share` | `main` | inherit |
| `W10-RUN` (Wave 10B) | `w10-cfd-run` | `share` | `main` | inherit |
| `W10-RESULTS` (Wave 10C) | `w10-results-core` | `share` | `main` | inherit |
| `W10-RAIL-UI` (Wave 10D) | `w10-rail-ui` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** Zero open DRs at Wave 10 start (spec `A5.10`, `A5.11`, `B1`, `F7`, `F8`, `ADR-0011`, Rulings 68/69/70/271/272 fix all contracts). If `W10-DESIGN` or any track hits a real spec ambiguity (e.g., if an unlisted OpenFOAM BC type were ever needed in `cases/tools/foam-dict-lint.py`), Coordinator spawns `w10-owner` (`workspace: "inherit"`) to rule into `docs/notes/rulings.md` via `coord-decide.py rule next --request <id>`.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board & `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only` (backed by `pre-commit` floor + coordinator diff inspection), pack CLI `coord-mail.py` doorbells `unsupported`.
- **Deadline / Fallback:** Each track has a 1–2.5 h wall budget and 300–400k context ceiling (warm `fork` at ~300–350k tokens if needed). No track ever mutates host WSL distros (`Ubuntu-24.04` / `cfdw-openfoam2512-arm64`) or writes real HKCU registry entries.
- **Termination condition:** Track ends when all exit evidence is independently verified by `w10-verifier` (`PASS` or `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py --no-push`, or when the 2-repair-cycle / budget cap fires.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` before dispatching | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-w10-coord`) |
| S1: `W10-DESIGN` joins before `W10-RUN` starts | Freezes the exact `cfdw-project-2` `cfd` schema, `FieldEvidenceManifest`, `CandidateLadder`, and 7-Area Rail contracts before any C# code is written (`GO5` decision edge) | Coordinator (`agy-w10-coord`) |
| S2: Serial code waves `W10-RUN` → `W10-RESULTS` → `W10-RAIL-UI` | Share `AuthoringSession.cs`, `Labels.cs`, `Program.cs`, `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, and the real-display Desktop harness (`GO5`) | Coordinator (`agy-w10-coord`) |
| S3: Joins run one at a time via `conductor-join.py --no-push` | Serial on `main`; on `W10-DESIGN` (`--docs-only`), `conductor-join.py` invokes `tools/join-ring.sh` which self-skips `tools/run-tests.sh` (`RING-SKIPPED docs-only`) via merge-diff detection while running all doc/lint/verify gates; every code join runs full `TESTS-GREEN` + `xaml-token-lint` + `check-event-subscribers` + `10/10` verify gates | Coordinator (`agy-w10-coord`) |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W10-DESIGN` → `W10-RUN` & `W10-RESULTS` & `W10-RAIL-UI` | Frozen `cfdw-project-2` `cfd` envelope schema (`runs`, `fieldEvidence`, `candidates`), 11-state run contract, $\tau_w$ separation rule, 9-field `ProvenanceStrip`, and 7-Area Rail `Auto,*` slot map | Hard wave boundary: `W10-DESIGN` clears `w10-verifier` and joins onto `main` before `W10-RUN` branches |
| `W10-RUN` → `W10-RESULTS` | Complete `cfd: { runs, fieldEvidence, candidates }` envelope definition on `cfdw-project-2`, `CfdRunRecord`, `AuthoringSession.RecordCfdRun`, `CfdRunProjection` | Hard wave boundary: `W10-RUN` joins before `W10-RESULTS` branches |
| `W10-RESULTS` → `W10-RAIL-UI` | `ResultsService`, `ResultsProjection` (surface $C_p$/$C_f$ scalar arrays, table twins, replay frames, difference flood, `CandidateLadder` + `BuildGeometryDraft` CAD draft factory), `ParaViewHandoff` | Hard wave boundary: `W10-RESULTS` joins before `W10-RAIL-UI` branches |

## Struck tracks

| track | why it was not worth its multiplier / why struck |
|---|---|
| Live WSL `openfoam2512` solver execution (`A5-RESUME`) | Network Cisco Umbrella (`net.proxy-tls`) still blocks `sourceforge.net` (`curl rc=60`). Area 5 pipeline uses real `cases/tools/foam-dict-lint.py`, real `SolverLauncher` (`M1–M8`), and `ICfdRunSubstrate` so zero fake solver output is ever produced. |
| Live ParaView 5.12+ launch | Host probe measured `PARAVIEW_FOUND=False`; `"Open in ParaView"` proves both honest absence copy and injected-path process-only handoff (zero VTK linking). |
| Accessibility / screen-reader tracks (NVDA / VoiceOver / Narrator) | Explicitly excluded by operator directive of 2026-11-23. |
| Cross-area live BYOK LLM assistants (Wave 11) | Deferred to Wave 11; Wave 10 ships deterministic offline key-free `AI-01`/`AI-11` slots + validators. |
| Concurrent `W10-RUN` and `W10-RESULTS` | Share `AuthoringSession.cs`, `Labels.cs`, `Program.cs`, `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, and `tools/run-tests.sh` (`Desktop.Tests`); width-1 serial execution avoids merge conflicts and Desktop exit-124 timeouts. |

## Cost justification (GO6 — 15× multiplier)

Honest Simplifier assessment: **width 1 across 4 bounded-context `share` tracks is the smallest reliable shape.**
Separating `W10-DESIGN` (~15 min docs-only) prevents mid-build schema drift across `AuthoringSession.cs`, `CfdRun`,
`Results`, and the 7-Area Rail; separating `W10-RUN`, `W10-RESULTS`, and `W10-RAIL-UI` keeps every track comfortably
below the 400k context ceiling, eliminates merge conflicts and Desktop harness contention, and gives each domain
independent Adversary verification (`Hydrodynamicist` + `Security` + `Data & Persistence` + `UX` + `Test Architect`).

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect hard veto + Simplifier soft veto + Tech Lead casting vote); fold C1–C5 | 1 subagent pass | Clears Test Architect hard veto before S0 commit |
| 2 | Render `docs/coordination/wave10-cfd-run-results-and-rail.html`, run `docs-graph.py derive`, `tools/check-docs.py`, `docs/ai-forward-pack/scripts/verify-ruling-citations.py`, `tools/check-proof-pii.py`, append `prepare-for-coordination` audit entry, commit S0 on `main` | 1 commit | Clean primary required before any join |
| 3 | Update swarm title (`wave10-run-results-rail: DESIGN -> RUN -> RESULTS -> RAIL-UI`), write `plan.json`, add `w10-monitor` + `w10-verifier` (`inherit`), and dispatch **Wave 10A** `w10-arch-design` (`W10-DESIGN`, `share`) | width 1 + verifier | Freezes Area 5/6 schemas & 7-Area Rail seam before code |
| 4 | Route `W10-DESIGN` to `w10-verifier` → join via `conductor-join.py --no-push --docs-only` (`join-ring.sh` self-skips `run-tests.sh`) → `swarm kill w10-arch-design` | 1 docs join | Unblocks Wave 10B `W10-RUN` |
| 5 | Dispatch **Wave 10B** `w10-cfd-run` (`W10-RUN`, `share`) → `w10-verifier` → join via `conductor-join.py --no-push` → `swarm kill w10-cfd-run` | width 1 + 1 join | Unblocks Wave 10C `W10-RESULTS` |
| 6 | Dispatch **Wave 10C** `w10-results-core` (`W10-RESULTS`, `share`) → `w10-verifier` → join via `conductor-join.py --no-push` → `swarm kill w10-results-core` | width 1 + 1 join | Unblocks Wave 10D `W10-RAIL-UI` |
| 7 | Dispatch **Wave 10D** `w10-rail-ui` (`W10-RAIL-UI`, `share`) → `w10-verifier` → join via `conductor-join.py --no-push` → `swarm kill w10-rail-ui w10-verifier w10-monitor` | width 1 + 1 join | Full Wave 10 convergence on `main` |
| 8 | Post `[done]` on swarm board, append Planned vs Actual to this plan, render `.html`, run `docs-graph.py derive` + `tools/check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main`, and report full results to operator | 1 close commit | Clean Wave 10 closure |

## Gate record

| gate | reviewer | verdict | conditions folded |
|---|---|---|---|
| Independent Adversary Plan Review | `Wave 10 Adversary Plan Reviewer` (`15951d23`; `Test Architect` hard veto · `Simplifier` soft veto · `Tech Lead` casting vote) | **PASS-WITH-CONDITIONS → CLEARED** | Folded all 5 verified conditions (`C1–C5`) + 2 Simplifier advisories: (C1) added `tools/store_subset.py` to `W10-RUN` to widen `.glob("*.cs")` → `.rglob("*.cs")` skipping `bin/`/`obj/`; (C2) aligned `OpenFoamCaseEmitter` with `cases/tools/foam-dict-lint.py` (`allowSystemOperations 0` stays in `SolverLauncher` HOME `controlDict` M1; case `system/controlDict` emits `runTimeModifiable false;` + `cfdw-manifest.json`; all `type <v>` strictly within 21-entry `TYPES_ALLOWED`); (C3) added `Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt` guard forbidding Core method names `Cancel`/`Undo`/`Redo`/`ProfileAt`/`Begin*`/`Update*`/`Apply*` called from `Analysis`; (C4) specified `ShellHost` `Auto,*` 3-row / `ColumnSpan=2` docking, 4-button synchronized `ModelArea.Navbar`, `Dock`-free `ActivityRail.axaml.cs`, and `Modal: false` `view.run`/`view.results` commands; (C5) clarified `tools/join-ring.sh` `RING-SKIPPED docs-only` behavior and exact `docs/ai-forward-pack/scripts/verify-ruling-citations.py` path; folded Simplifier notes (`W10-RUN` lands complete `cfd` envelope; `StatusStrip` edited only if needed). Tech Lead casting vote: APPROVE 4 serial width-1 `share` tracks. |

## Status

| | |
|---|---|
| **Completed** | Pushed Wave 8 (`254b0d51`), recorded Operator Ruling 275 (`687ca479`, 0 unapproved `COPY-*` rows left in `DESIGN.md`), grounded Wave 10 layer state & ParaView host probe (`PARAVIEW_FOUND=False`), and drafted & Adversary-cleared `docs/coordination/wave10-cfd-run-results-and-rail.md` (`C1–C5` folded) |
| **Remaining** | S0 commit → serial swarm execution (`W10-DESIGN` → `W10-RUN` → `W10-RESULTS` → `W10-RAIL-UI`) → independent Adversary verification & `conductor-join.py --no-push` merges → Planned vs Actual closeout |
| **Best next action** | Render `.html`, run docs/PII/ruling/verify gates, commit S0 on `main`, and launch `w10-monitor` + `w10-verifier` + Wave 10A `w10-arch-design` (`W10-DESIGN`) |
