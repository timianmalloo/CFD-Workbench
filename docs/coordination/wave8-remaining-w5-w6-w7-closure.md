---
id: coordination-wave8-remaining-w5-w6-w7-closure
title: "Coordination plan - wave 8: autonomous closure of remaining Wave 5, 6, and 7 open items"
type: plan
status: active
owner: "@timianmalloo"
tags: [coordination, worktrees, area-7, area-5, step, 3dm, guided-solver-setup, agy-swarm, wave8]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-0013-cad-export-kernel, rel: implements }
  - { to: design-guided-solver-setup, rel: implements }
  - { to: design-export, rel: refines }
  - { to: proof-spike-cad-01, rel: implements }
  - { to: proof-wsl-area5-build, rel: relates-to }
  - { to: coordination-wave5-next-steps, rel: refines }
  - { to: coordination-wave6-and-area5-wsl, rel: refines }
  - { to: coordination-wave7-ui-polish-and-at-proof, rel: refines }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-23"
summary: >-
  Wave 8 autonomously closes every executable remaining open item from Waves 5, 6, and 7 on this Windows ARM64 host:
  pruning the 6 verified ahead=0 stale remote branches on origin (S1), delivering Area 7 STEP AP203 & Rhino 3DM export
  (W8-EXP: Export Slice B design, pure Core CadExportExtractor/StepAp203Export/StepVerifier + MIT Rhino3dm 8.35.0
  export with KRN-SPK Phase 1 fixes, CLI export, Desktop ExportDialog gated cleanly on cad-open-measure.json / COPY-481,
  and cross-platform Python SPIKE-CAD-02 fixture harness), and delivering Area 5 Guided Solver Setup already approved
  by Rulings 68-70 (W8-SLV-CORE: pure Core T1 routes/records/selector/lint/validator + Analysis M1-M8 Product Launcher
  & T2/T5 Windows/WSL/SU2 & macOS I/O adapters obeying STORE-SUBSET + CLI; W8-SLV-UI: T3 Desktop Dock SolverSetupView
  + StatusStrip Solver item). Physically unavailable items (net.proxy-tls A5-RESUME, missing Fusion/FreeCAD/Vectric/
  Rhino binaries, impeccable win-arm64 binary, signing certs) and operator-excluded accessibility/VoiceOver/NVDA items
  are honestly struck.
---

# Coordination plan: wave 8 — autonomous closure of remaining Wave 5, 6, and 7 open items

**Input.** Operator directive of 2026-11-23 following Wave 7 completion and Ruling 274 push (`7febda40`): run
`/prepare-for-coordination` across all remaining "Still open" items from Waves 5, 6, and 7 — explicitly excluding
accessibility/screen-reader features (*"ignore any accessibility features (like voice over)"*) — and execute the
autonomous plan via `/execute-with-coordination` on AGY swarm.

**Goal state.**
- **Goal:** Autonomously close every executable open item remaining from Waves 5, 6, and 7 on `main`: (1) prune the 6
  verified merged (`ahead=0`) stale remote branches on `origin`, (2) deliver **Area 7 STEP AP203 & Rhino 3DM export**
  (Export Slice B design + Option B pure-C# STEP + MIT `Rhino3dm 8.35.0` `.3dm` + `KRN-SPK` Phase 1 refinements + CLI +
  Desktop `ExportDialog` wired to the `cad-open-measure.json` release gate preserving `COPY-481` until external CAM
  receipts exist + cross-platform `SPIKE-CAD-02` Python generator & schema verifier), and (3) deliver **Area 5 Guided
  Solver Setup** (`docs/design/guided-solver-setup.md` §11 `T1`–`T5` + right-sized `M1–M8` Product Launcher, already
  operator-approved in Rulings 68, 69, and 70).
- **Done when:** Serial Spine `S1` prunes all 6 stale `ahead=0` remote branches; all three isolated `share`-workspace
  tracks (`W8-EXP` → `W8-SLV-CORE` → `W8-SLV-UI`) pass red-first tests and live mutants, clear independent Adversary
  review (`w8-verifier`: Computational Geometry + Security & Identity + Data & Persistence + UX + Test Architect +
  Simplifier), and merge serially onto `main` via `conductor-join.py` (`--no-push`) with `TESTS-GREEN`,
  `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `UI-CS-TOKEN` 0 findings, `STORE-SUBSET` ok,
  `check-proof-pii.py` 0 hits, `check-docs.py` exit 0, and `10/10` `run-verify-gates.py` PASS; Planned vs Actual is
  recorded and committed.
- **Not in scope (honestly Struck / Operator-excluded):**
  - Accessibility / screen-reader features (live NVDA speech, live macOS VoiceOver, `PropertiesFieldNudge` Narrator
    gate) — explicitly excluded by operator directive of 2026-11-23.
  - `A5-RESUME` (`tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume`: `openfoam2512` apt + `petsc4Foam` `Allwmake`)
    — live host probe 2026-11-23 confirmed `dl.openfoam.com` (`curl exit 28`) and `sourceforge.net` (`curl exit 60`,
    untrusted Cisco Umbrella root `net.proxy-tls`); zero TLS/proxy bypass permitted.
  - External commercial CAM / Rhino manual open-and-measure receipts (`SPIKE-CAD-02` in Autodesk Fusion, FreeCAD CAM,
    Vectric VCarve/Aspire, Rhino 8), `ADR-0013` status flip to `accepted`, and `DR-KRN-5` Spec A4.12 amendment — live
    host probe 2026-11-23 confirmed `Fusion=False`, `FreeCAD=False`, `Vectric=False`, `Rhino=False` on this machine.
    Production STEP/3DM UI therefore stays honestly behind the `cad-open-measure.json` / `COPY-481` gate until real
    external receipts are recorded.
  - `ui-craft-gate.py` (`impeccable 0.1.27`) — live probe 2026-11-23 exited `2` (`no binary for windows-arm64`).
  - Authenticode / SmartScreen & macOS codesign / Gatekeeper notarization — no signing certs or macOS host in repo.
  - Pushing new Wave 8 commits on `main` to `origin` without operator confirmation (only the explicit S1 pruning of the
    6 verified `ahead=0` merged remote branches touches `origin`).
- **Tier:** T2 · **Fan-out cap:** 1 concurrent code/test author (Simplifier/GO5 serialization across 3 isolated
  `workspace: "share"` tracks for zero merge conflicts on `CfdWorkbench.Core.csproj`, `Labels.cs`, `Program.cs`,
  `Styles.axaml`, `DESIGN.md`, `IdentityTests.cs`, and `WorkbenchTests.cs`, and zero Desktop real-display harness
  contention).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w8-coord`). Owns plan, S0/S1 spine, seams, join order, `conductor-join.py`
  merges, and closeout. Never authors track code.
- **Owner:** Independent swarm worker `w8-owner` (`workspace: "inherit"`, read-only on `src/`), spawned only if an
  unexpected mid-wave decision request arises (zero open DRs at Wave 8 start: Rulings 68, 69, 70 already approved
  `M1–M8`, `DR-SEC-1 A`, `DR-F3-1 A`, `solver-setup.html`, `DR-SETUP-1..6`, and Spec 1.7.2; Rulings 270 & 273 already
  approved `DR-KRN-1,2,3,4,6`).
- **Adversary Verifier:** Independent swarm worker `w8-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track's committed diff, red-first tests, live mutants, and gates in Adversary Mode before join.
- **Stall Monitor:** Swarm worker `monitor` (`workspace: "inherit"`, loads `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned** (same verified deviation as Waves 3, 5, 6, and 7).

## Layer state

Measured 2026-11-23 15:40Z–15:47Z by `py -3 docs/ai-forward-pack/scripts/coord-core.py doctor`,
`py -3 docs/ai-forward-pack/scripts/coord-core.py session list`, `git worktree list`,
`git config --get-regexp 'merge\..*\.driver'`, and live host probes in the primary checkout
(`D:/Projects/CFD-Workbench`, HEAD `7febda40`, clean).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register files merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| heartbeat | `1 session(s) beating; newest beat 3626398 s ago; 0 stalled, 0 live, 0 blocked, 0 done` | clean start |
| sessions | `0 active session(s); 248 file(s) scanned` | clean start |
| requests | `ok - 42 request(s), 0 open, 42 terminal` | zero open decision requests |
| lease overlap | `none (0 live lease(s))` | clean start |
| worktrees | `1 (D:/Projects/CFD-Workbench 7febda40 [main])` | primary only |
| OpenFOAM TLS host probe | `dl.openfoam.com` `curl rc=28`; `sourceforge.net` `curl rc=60` (untrusted Cisco Umbrella root) | `A5-RESUME` genuinely blocked (`net.proxy-tls`); honestly struck |
| CAD/CAM & AT host probe | `FreeCAD=False`, `Fusion=False`, `Rhino=False`, `Vectric=False`, `NVDA=False`, `Docker=False` | external CAM/Rhino read-back stays honestly gated behind `COPY-481`; `W8-EXP` builds production code + `SPIKE-CAD-02` harness |
| `ui-craft-gate.py` host probe | `exit 2` (`impeccable 0.1.27`: no `windows-arm64` binary/checksum) | honestly struck on this host |
| WSL distro probe | `* Ubuntu-24.04 Stopped 2`; `cfdw-openfoam2512-arm64 Stopped 2` | read-only WSL survey testable against real Windows 11 ARM64 host without mutating either distro |
| Stale remote branches probe | 6 branches on `origin`, all `ahead=0` (`behind=149..572`, 100% merged into `main`) | safe to prune in Serial Spine S1 with zero commit loss |
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
| `docs/notes/rulings.md` | authored, single writer | Coordinator / Owner only (no Wave 8 ruling expected) | serial |
| `DESIGN.md` (copy range rule; high water `COPY-749`) | authored, serial across waves | `W8-EXP` reserves `COPY-750..765` (proposed, only if new Export Slice B copy is needed; zero mutations to `COPY-481` or approved rows); `W8-SLV-UI` reserves `COPY-766..805` (proposed, Guided Solver Setup strings passing `SETUP-04` no-command lint; zero mutations to approved rows) | serial (`W8-EXP` → `W8-SLV-UI`) |
| `src/CfdWorkbench.Core/{CfdWorkbench.Core.csproj,ICadStepExporter.cs (new),CadExportExtractor.cs (new),StepAp203Export.cs (new),StepVerifier.cs (new),Rhino3dmExport.cs (new — pure in-memory; or Analysis/Export if disk I/O needed)}`, `src/CfdWorkbench.Analysis/Export/{ExportSession.cs,ExportCopy.cs,CadExportReleaseGate.cs (new),Rhino3dmExport.cs (if disk I/O needed)}`, `src/CfdWorkbench.Cli/{ExportVerb.cs,Program.cs}`, `src/CfdWorkbench.Desktop/{ExportDialog.axaml,ExportDialog.axaml.cs,packages.lock.json,Styles.axaml (non-theme tokens only)}`, `THIRD-PARTY-NOTICES.md` (MIT `Rhino3dm 8.35.0`), `tools/spikes/cad-export-measure/{generate-fixture.py (new),verify-open-measure.py (new)}`, `docs/design/export-step-3dm.{md,html} (new)`, `docs/proof/cad-export-measure/README.{md,html} (new)` | authored, single owner | `W8-EXP` owns in Wave 8A | serial |
| `src/CfdWorkbench.Core/SolverSetup/{RouteCatalogue.cs,SolverSetupRecords.cs,StepSelector.cs,SetupCopyLint.cs,SetupReportBuilder.cs,SetupAssistantValidator.cs,SolverLauncherPolicy.cs} (new — strictly pure zero-I/O per STORE-SUBSET)`, `src/CfdWorkbench.Analysis/SolverSetup/{SetupFactStore.cs,SolverLauncher.cs,HostSurvey.cs,WindowsSetupAdapters.cs,MacSetupAdapters.cs,SolverSetupService.cs,SolverSetupProjection.cs} (new — all filesystem/process/HTTP/WSL/registry I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{SolverSetupVerb.cs (new),Program.cs}`, `docs/design/guided-solver-setup.md` (refresh frontmatter `status: approved (Ruling 69)` + record real Windows ARM64 survey receipt in §0) | authored, single owner | `W8-SLV-CORE` owns in Wave 8B (after `W8-EXP` joins) | serial |
| `src/CfdWorkbench.Desktop/SolverSetup/{SolverSetupView.axaml (new),SolverSetupView.axaml.cs (new),SolverSetupCopy.cs (new)}`, `src/CfdWorkbench.Desktop/Shell/{StatusStrip.axaml,StatusStrip.axaml.cs,CommandTable.cs,ShellHost.cs}`, `src/CfdWorkbench.Desktop/{MainWindow.axaml.cs,Styles.axaml (non-theme tokens only)}`, `src/CfdWorkbench.Desktop/Analysis/{AnalysisPanel.axaml,AnalysisPanel.axaml.cs}` | authored, single owner | `W8-SLV-UI` owns in Wave 8C (after `W8-SLV-CORE` joins) | serial |
| `tests/**` (incl. explicit harness runners `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, `ShellModelTests.cs`, `WorkbenchTests.cs`) | authored, serial | strictly one owner per wave (width 1) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- Strictly **one** author active per wave (`W8-EXP` → `W8-SLV-CORE` → `W8-SLV-UI`): zero concurrent writers on
  `CfdWorkbench.Core.csproj`, `packages.lock.json`, `Labels.cs`, `Program.cs`, `Styles.axaml`, `DESIGN.md`,
  `IdentityTests.cs`, `WorkbenchTests.cs`, or the real-display Desktop harness.
- **`tools/store_subset.py` (`STORE-SUBSET`, run via `tools/check-docs.py`):** root `src/CfdWorkbench.Core/*.cs` and
  `tests/CfdWorkbench.Core.Tests/*.cs` (recursive across `Core` / `Core.Tests` source files); token set filesystem,
  env, P/Invoke, and persistence primitives (`File.Write*`, `File.Delete`, `File.Exists`, `Directory.CreateDirectory`,
  `Directory.Delete`, `FileStream`, `FileInfo`, `GetTempPath`, `GetEnvironmentVariable`, `DllImport`, `LibraryImport`,
  `CfdWorkbench.Persistence`); allowlist 6 `PARTITION_EXEMPT` files only. **Compliance:** ALL Core additions in
  `W8-EXP` and `W8-SLV-CORE` are strictly pure in-memory/zero-I/O; ALL filesystem/process/HTTP/WSL/registry I/O lives
  in `src/CfdWorkbench.Analysis/` (and I/O tests in `tests/CfdWorkbench.Analysis.Tests/`): jointly satisfiable.
- **`xaml-token-lint.py` & `tools/check-event-subscribers.py` (`docs/coordination/join.json`):**
  - `xaml-token-lint.py`: root `src/CfdWorkbench.Desktop/`; recursion `os.walk` over `.xaml`/`.axaml` excluding
    `bin/`/`obj/`; token set raw color/named-color literals (`VISUAL_COLOR_ATTRS`, `Color`) and raw dimension literals
    (`DIMENSION_ATTRS`); allowlist none.
  - `tools/check-event-subscribers.py`: root `src/` (ALL projects via `src.rglob("*.cs")`, excluding `bin/`/`obj/`);
    token set `event` declarations under `src/` having ≥ 1 `<name> +=` subscriber under `src/` plus non-stale `ALLOWED`
    entries; allowlist 4-entry `ALLOWED` set (`CanExecuteChanged`, `VertexSelected`, `VertexMoved`,
    `FocusedTargetChanged`). Every declared event in `W8-EXP`, `W8-SLV-CORE`, and `W8-SLV-UI` has an active
    subscriber: jointly satisfiable.
- **`Styles.axaml` theme-brush closed set (`tools/verify-application-adapters.py:123-130`):** asserts an exact closed
  set of `<SolidColorBrush x:Key="...Brush">` keys inside the `Light` / `Dark` / `HighContrast` dictionaries in
  `Styles.axaml`. `W8-EXP` and `W8-SLV-UI` MUST reuse existing theme brush keys and add **zero** new
  `<SolidColorBrush x:Key="...Brush">` keys inside theme dictionaries: jointly satisfiable.
- **`TokenDisciplineTests.cs` (`UI-CS-TOKEN`):** root `src/CfdWorkbench.Desktop/`; recursion `**/*.cs` excluding
  `bin/`/`obj/`; token set `TryFindResource` / `FindResource` / `ResolveBrush` / `Brush` / `Resource(...)` /
  `GetResourceObservable(...)` keys (must exist in `Styles.axaml` / `App.axaml`), hardcoded `FontSize < 11` or
  `DrawText` / `FormattedText` size `< 11`, and ALL `Color.Parse(...)` calls; allowlist empty. Jointly satisfiable.
- **`design-lint.py DESIGN.md --strict`:** root `DESIGN.md`; recursion none; token set `{token.ref}` references and
  raw `#rgb`/`#rrggbb` hexes in the body; allowlist none. Jointly satisfiable.
- **`tools/check-reader-sharing.py` & `tools/coordination/check-test-paths.py`:**
  - `tools/check-reader-sharing.py`: root `src/**/*.cs` (zero allowlist; all readers must use `UserFile.OpenRead` or
    explicit `FileShare.Read | FileShare.Delete` / `FileShare.ReadWrite | FileShare.Delete`) AND `docs/proof/**/*.ps1` +
    `tools/**/*.ps1` (7 SHA-256-pinned `PS_GRANDFATHERED` entries). All Wave 8 scripts are Python (zero new `.ps1`).
  - `tools/coordination/check-test-paths.py`: root `tests/CfdWorkbench.Core.Tests/*.cs` (non-recursive); token set
    relative `"docs/..."` reads; allowlist `docs/examples/foildsl/` and `Fixtures/`. Jointly satisfiable.
- **`PROOF-PII`, `verify-no-machine-paths.py`, `verify-no-new-console-launches.py`, `verify-portable-text-io.py`,
  `verify-subprocess-utf8.py`, `COMMIT-02` / `tools/check-notices.py`:**
  - `tools/check-proof-pii.py`: all tracked + untracked non-ignored text files (forbids real Windows user-profile paths /
    machine SIDs; `%USERPROFILE%` placeholder allowed; 1-entry `ALLOWLIST`: `verify-no-machine-paths.py`).
  - `verify-no-machine-paths.py`: scans `pack/`, `tools/`, `tests/`, `web/`, `.claude/`, `.github/`, `.grok/`,
    `.agents/`, and root files.
  - `verify-no-new-console-launches.py`: scans `src/` and `tests/` for `CREATE_NEW_CONSOLE`.
  - `verify-portable-text-io.py` & `verify-subprocess-utf8.py`: scans `tools/` Python scripts (every text write and
    text-mode subprocess must state `encoding="utf-8"`).
  - `COMMIT-02` & `THIRD-PARTY-NOTICES.md`: zero LGPL/GPL/AGPL/NOSA NuGet links; MIT `Rhino3dm 8.35.0` recorded in
    `THIRD-PARTY-NOTICES.md`; zero committed binary `.step`/`.3dm`/`.dll`/`.nupkg`/`.wsl` files. Jointly satisfiable.
- **`SETUP-04` no-command lint (added in `W8-SLV-CORE`, verified over `W8-SLV-UI` strings):** root Guided Solver Setup
  copy constants (`SolverSetupCopy.cs` & `DESIGN.md` `COPY-766..805`); recursion none; token set code spans/blocks,
  prompt-prefixed lines, shell-command invocations (`wsl`, `apt`, `sudo`, `xattr`, `spctl`, `powershell`, `cmd`,
  `bcdedit`, `dism`, `brew`, `curl`, `Set-ExecutionPolicy` followed by flags/args); allowlist prose mentions (e.g.
  "WSL" in prose) and the Technical details command slot (`AM-SET-5`, Ruling 69). Jointly satisfiable.

**Quoted ADR / Ruling trigger lines (DC-189 — exact citations, never paraphrased).**
- **ADR-0010 §Decision (4)** (`docs/adr/0010-one-placement-rule.md:81-83`):
  > "4. **Displays receive geometry, never compute it.** Core exposes `Placement.Surface` (a placed mesh of the starboard
  > half) and `Placement.Frame` (channel values at η). The Desktop applies only a camera and the language's symmetry map
  > `(x, y, z) → (x, −y, z)` (`Point3.Port()`, defined in Core)."
- **ADR-0013 §Decision (4)** (`docs/adr/0013-cad-export-kernel.md:62`):
  > "4. **Mandatory `SPIKE-CAD-02` release gate (`EXP-02`, `EXP-04`, `EXP-05`):** STEP export remains disabled (`COPY-481`)
  > until `docs/proof/cad-export-measure/cad-open-measure.json` records both a red-first planted-defect failure and green
  > open-and-measure passes (units = mm unscaled, span, root/mid/tip chord `≤ 10 µm`, max thickness `≤ 10 µm`, TE
  > thickness `≤ 5 µm`, single watertight solid with `0` repair warnings, volume within `0.1 %`, zero header PII) in
  > **Autodesk Fusion** and **two operator-named CAM systems** (`DR-KRN-2`). 3DM release similarly requires the **Rhino 8**
  > open-and-measure receipt."
- **Ruling 68** (`docs/notes/rulings.md:807`):
  > "… (1) Apply docs/notes/solver-security-right-size.md: ADR-0012 D2 becomes the six short rules (code off via the
  > app-owned controlDict, a live Disallowing banner proves it, argv only, only cases the app wrote, own run directory
  > with a clean environment, resource caps); spec A8.5 backend-substrate row and A5.10 readiness take the verified facts
  > and the M1/M2/M4 rules; Run is enabled when the install smoke test shows Disallowing; only S-1 and S-2 serial stay,
  > as the pin-change check. (2) DR-SEC-1 A: setup detects an existing OpenFOAM install, hash-checks it against known
  > builds, and shows a plain 'unverified install' note the user may accept; otherwise it installs the pinned build. …"
- **Ruling 69** (`docs/notes/rulings.md:813`):
  > "Operator 2026-10-04 (AskUserQuestion): the guided solver setup mockup (docs/mockups/solver-setup.html) is approved.
  > DR-SETUP-1: Windows default = OpenFOAM in an app-owned WSL Linux (Ubuntu 24.04 image, OpenCFD openfoam2512 packages);
  > SU2 native under Other options; Docker rejected. DR-SETUP-2: the smoke test reads the final-time mean Courant number
  > (0.222158 on macOS) plus the output files, the Disallowing banner and the build id; the tolerance is set after the
  > Windows run (the spec's 'Cl on the cavity' cannot exist). DR-SETUP-3: the app neither adds nor removes the macOS
  > quarantine flag on its own download; it discloses 'not notarised' and relies on the pinned hash; confirm with probe
  > P-MAC-1. DR-SETUP-4: 'Set up a solver' is one verb that works with no AI key; RUN-01 and AI-11 amended to match.
  > DR-SETUP-5: the exact command is always under 'Technical details', shown before consent. DR-SETUP-6: a one-time
  > per-user entry reopens the app after a Windows restart. …"
- **Ruling 273** (`docs/notes/rulings.md:2037`):
  > "… (2) DR-KRN-2: option (a) approved. The EXP-02 STEP open-and-measure release fixture (SPIKE-CAD-02) is governed by
  > Autodesk Fusion, FreeCAD CAM (Path), and Vectric VCarve/Aspire, plus Rhino 8 for EXP-04 3DM; production STEP export
  > stays behind COPY-481 until docs/proof/cad-export-measure/cad-open-measure.json records both red-first and green
  > receipts. …"

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **W8-EXP** Area 7 STEP AP203 & Rhino 3DM Export (Design Slice + Core + CLI + Desktop Gate + `SPIKE-CAD-02` Harness) | `docs/design/export-step-3dm.{md,html} (new)`, `docs/proof/cad-export-measure/README.{md,html} (new)`, `src/CfdWorkbench.Core/{CfdWorkbench.Core.csproj,ICadStepExporter.cs (new),CadExportExtractor.cs (new),StepAp203Export.cs (new),StepVerifier.cs (new),Rhino3dmExport.cs (new — pure in-memory)}`, `src/CfdWorkbench.Analysis/Export/{ExportSession.cs,ExportCopy.cs,CadExportReleaseGate.cs (new),Rhino3dmExport.cs (only if disk I/O required by Rhino3dm)}`, `src/CfdWorkbench.Cli/{ExportVerb.cs,Program.cs}`, `src/CfdWorkbench.Desktop/{ExportDialog.axaml,ExportDialog.axaml.cs,packages.lock.json,Styles.axaml (non-theme tokens only — ZERO new theme Brush keys)}`, `THIRD-PARTY-NOTICES.md` (MIT `Rhino3dm 8.35.0`), `tools/spikes/cad-export-measure/{generate-fixture.py (new),verify-open-measure.py (new)}`, `DESIGN.md` (optional `COPY-750..765` proposed; zero edits to `COPY-481` or approved rows), `tests/CfdWorkbench.Core.Tests/{StepExportTests.cs (new),Rhino3dmExportTests.cs (new — pure in-memory),IdentityTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/{Rhino3dmExportTests.cs (if disk I/O needed),AnalysisChecks.cs (if Analysis tests added)}`, `tests/CfdWorkbench.Cli.Tests/ExportCliTests.cs`, `tests/CfdWorkbench.Desktop.Tests/{ExportCadDialogTests.cs (new),ExportTests.cs,WorkbenchTests.cs}` | S0 plan + S1 branch prune (Wave 8A) | T2 | 1 | 200 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + live mutants killed: (1) `/design-slice` doc `docs/design/export-step-3dm.{md,html}` covering Option B pure-C# STEP + MIT `Rhino3dm 8.35.0` `.3dm`, E7 surface list, data model, hard states, and `SPIKE-CAD-02` contract; (2) Pure zero-I/O Core (`STORE-SUBSET` compliant): `CadExportExtractor` implementing `KRN-SPK` Phase 1 note (exact channel knot mapping `t_i -> eta_i = x_channel(t_i)` on `Placement.Surface`, `≤ 10 µm` max closest-point deviation on all 5 benchmark wings), `ICadStepExporter` + `StepAp203Export` (`Whole`/`Half` × `ClosedShell`/`Untrimmed`, millimeter unscaled, fixed `1980-01-01T00:00:00` zero-PII header, always-on TE row), `StepVerifier` implementing `KRN-SPK` Phase 1 note (explicit boundary vs interior edge separation on `OPEN_SHELL`), and `Rhino3dmExport` using MIT `Rhino3dm 8.35.0` via pure in-memory `File3dm.ToByteArray()`/`FromByteArray(byte[])` (or placed in `Analysis/Export` if `Rhino3dm` requires disk I/O; `Wing Skin`, `Master Curves`, `Authored Stations` layers, mm unscaled, zero-PII, live `win-arm64` round-trip read-back), with `packages.lock.json` updated and MIT notice added in `THIRD-PARTY-NOTICES.md`; (3) `CadExportReleaseGate` reading `docs/proof/cad-export-measure/cad-open-measure.json` — strictly refusing production STEP/3DM in `ExportDialog` behind `COPY-481` when real Fusion + FreeCAD CAM + Vectric VCarve/Aspire + Rhino 8 receipts are absent/incomplete, and unlocking cleanly when a verified receipt is injected in tests (`ExportCadDialogTests`, wired into `DesktopChecks.Spawn`, `ScaleModes`, and `DesktopChecks.PrintScaleContext("export-cad")` in `WorkbenchTests.cs`); (4) CLI `export --format step|3dm` (`--step-variant closed|untrimmed`, `--scope whole|half`, atomic write via `ExportSession.WriteAtomicAsync`, zero-PII telemetry); (5) Cross-platform Python `tools/spikes/cad-export-measure/generate-fixture.py` + `verify-open-measure.py` (UTF-8 clean, self-test proving red-first planted-defect rejection & green acceptance for Fusion + FreeCAD CAM + VCarve/Aspire + Rhino 8) and `docs/proof/cad-export-measure/README.{md,html}` runbook; zero binary `.step`/`.3dm` files committed; all new test classes explicitly registered in `IdentityTests.cs` / `WorkbenchTests.cs`; `bash tools/run-tests.sh` `TESTS-GREEN` + all repo gates exit 0; Adversary pass (`w8-verifier`: Computational Geometry + Security + Test Architect + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W8-SLV-CORE** Area 5 Guided Solver Setup Backend (`T1` Pure Core + `M1–M8` Product Launcher + `T2/T5` I/O Adapters + `T4` AI-11 Validator + CLI) | `src/CfdWorkbench.Core/SolverSetup/{RouteCatalogue.cs,SolverSetupRecords.cs,StepSelector.cs,SetupCopyLint.cs,SetupReportBuilder.cs,SetupAssistantValidator.cs,SolverLauncherPolicy.cs} (new — strictly pure zero-I/O per STORE-SUBSET)`, `src/CfdWorkbench.Analysis/SolverSetup/{SetupFactStore.cs,SolverLauncher.cs,HostSurvey.cs,WindowsSetupAdapters.cs,MacSetupAdapters.cs,SolverSetupService.cs,SolverSetupProjection.cs} (new — all filesystem/process/HTTP/WSL/registry I/O)`, `src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/{SolverSetupVerb.cs (new),Program.cs}`, `docs/design/guided-solver-setup.md` (refresh frontmatter `status: approved (Ruling 69)` + record real Windows ARM64 survey receipt in §0), `tests/CfdWorkbench.Core.Tests/{SolverSetupSelectorTests.cs (new),SolverAssistantValidatorTests.cs (new),IdentityTests.cs}`, `tests/CfdWorkbench.Analysis.Tests/{SolverLauncherSecurityTests.cs (new),SolverSetupAdapterTests.cs (new),SolverSetupServiceTests.cs (new),AnalysisChecks.cs}`, `tests/CfdWorkbench.Cli.Tests/{SolverSetupCliTests.cs (new),CliTests.cs}` | `W8-EXP` joined on `main` (Wave 8B) | T2 | 1 | 220 calls · 380k · 2 h 30 min · ceiling 400k | Red-first tests + live mutants killed: (1) **Pure `T1` Setup Core (`src/CfdWorkbench.Core/SolverSetup/`, zero filesystem/env calls per `STORE-SUBSET`):** pinned Route catalogue (`mac-ofapp-2512`, `win-wsl-of2512`, `win-su2-850`) with manifest hash check (A8.5), immutable records (`HostSurvey`, `EnvironmentStep`, `InstallAcceptance`, `BackendCheck`), pure `StepSelector` (`allowedNext` + `railsNext`, resume = re-derivation closing any open attempt as `interrupted` + done-check before re-act, `R1`–`R5`, `SETUP-05`), one-step-at-a-time invariant, pure `SolverLauncherPolicy` (`allowSystemOperations 0` controlDict emitter, `Disallowing` banner parser, typed argv builder, clean env allowlist), `SetupCopyLint` (`SETUP-04`), zero-PII `SetupReportBuilder`, and **`T4` deterministic `AI-11` `SetupAssistantValidator`** (schema `explanation`/`environment-step` only, `stepId ∈ allowedNext`, citation resolution, shared `SetupCopyLint`, no-success-claim, no-weakening-verbs, `AI-03` numerals-in-context, key-free offline `AI-01`); (2) **Analysis I/O & `M1–M8` Product Launcher (`src/CfdWorkbench.Analysis/SolverSetup/`):** `SetupFactStore` JSONL using explicit `FileShare.Read | FileShare.Delete` (passes `check-reader-sharing.py`), `SolverLauncher` (`M1` controlDict write, `M2` live master-rank `Disallowing` gate, `M3` argv-only zero-shell, **`M4` CI emitter-lint test executing real `cases/tools/foam-dict-lint.py`** over a hostile foreign-design fixture, `M5` exclusive app-owned run dir with symlink refusal test, `M6` clean child env stripping `FOAM_CONTROLDICT`/`WM_*`, `M7` resource cap + Stop-to-kill escalation, `M8` install-identity hash check + unverified acceptance per `DR-SEC-1 A`); (3) **`T2/T5` Adapters:** real read-only Windows host survey executing live on this Windows 11 ARM64 machine (OS build, arch, free disk, reboot pending, virtualization/hypervisor, UTF-16/UTF-8 `wsl.exe` parser detecting `cfdw-openfoam2512-arm64` without mutating any distro, `< 5 s` budget), download+SHA-256 verification against local loopback `HttpListener` fixture (good, truncated, hash-mismatch deletes `.part`), `ROUTE-PARENT-MISSING` negative-path tests (Ruling 133), Windows `RunOnce` restart-resume abstraction (`DR-SETUP-6`, unit-tested via injected registry seam — never mutating real HKCU), and macOS fixture-tested adapters (`sw_vers`/`sysctl`, zero `xattr`/Homebrew); (4) CLI `solver-setup survey|status|report --json` parity; (5) All new test classes explicitly registered in `IdentityTests.cs`, `AnalysisChecks.cs`, and `CliTests.cs`; `bash tools/run-tests.sh` `TESTS-GREEN` + all repo gates exit 0; Adversary pass (`w8-verifier`: Security & Identity HARD VETO + Data & Persistence HARD VETO + Test Architect HARD VETO + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **W8-SLV-UI** Area 5 Guided Solver Setup Desktop UI (`T3` Dock `SolverSetupView` + StatusStrip `Solver` item + Desktop Tests) | `src/CfdWorkbench.Desktop/SolverSetup/{SolverSetupView.axaml (new),SolverSetupView.axaml.cs (new),SolverSetupCopy.cs (new)}`, `src/CfdWorkbench.Desktop/Shell/{StatusStrip.axaml,StatusStrip.axaml.cs,CommandTable.cs,ShellHost.cs}`, `src/CfdWorkbench.Desktop/{MainWindow.axaml.cs,Styles.axaml (non-theme tokens only — ZERO new theme Brush keys)}`, `src/CfdWorkbench.Desktop/Analysis/{AnalysisPanel.axaml,AnalysisPanel.axaml.cs}`, `DESIGN.md` (`COPY-766..805` proposed Guided Solver Setup copy rows; zero mutations to approved rows), `tests/CfdWorkbench.Desktop.Tests/{SolverSetupViewTests.cs (new),StatusStripTests.cs,ShellModelTests.cs,WorkbenchTests.cs}` | `W8-SLV-CORE` joined on `main` (Wave 8C) | T2 | 1 | 160 calls · 350k · 2 h · ceiling 400k | Red-first tests + live mutants killed: (1) **`T3` Desktop UI matching approved `docs/mockups/solver-setup.html` (Ruling 69):** mounts `SolverSetupView` as a non-blocking Dock document tab via `ShellHost.cs` (leaving `ModelArea.axaml*` untouched); wires `"Set up a solver"` in `CommandTable.DefaultRows` under existing `"View"` menu (`Modal: false`, updating `ShellModelTests.cs` gesture-less allowlist if gesture-less) + `AnalysisPanel` entry + `StatusStrip` `Solver` item showing derived `Solver: not set up · Setting up · Ready`; zero first-launch modal nagging; step card with plain-words summary, size/time/admin/restart metadata, collapsed `Technical details` disclosure showing exact command & pin before consent (`DR-SETUP-5`), `Other options` route switcher (`win-su2-850`), `Copy a report` button (zero-PII), offline key-free rails (`DR-SETUP-4`) + optional `Ask about this step` / `Explain this failure` slot honoring `AI-01`/`AI-11`, and all hard states (Survey, Step Ready, Existing Unverified Install, Failed with Cause + Next Step, Needs Restart, Ready); (2) **`SETUP-04` Copy Lint Test:** every `SolverSetupCopy` constant and `COPY-766..805` row passes `SetupCopyLint` (0 commands to type); (3) **Ring wiring & theme hygiene:** `--solver-setup` wired into `DesktopChecks.Spawn`, `ScaleModes`, and `DesktopChecks.PrintScaleContext("solver-setup")` in `WorkbenchTests.cs` (`LISTED-CHECK-IN-NO-RING`), `UI-CS-TOKEN` 0 findings, `xaml-token-lint` 0 findings, `check-event-subscribers` 0 findings, `design-lint --strict` 0 warnings, `bash tools/run-tests.sh` `TESTS-GREEN`; Adversary pass (`w8-verifier`: UX + Security + Test Architect + Simplifier) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `monitor` | `inherit` | `main` | inherit |
| Adversary verifier | `w8-verifier` | `inherit` | `main` | inherit |
| `W8-EXP` (Wave 8A) | `w8-cad-export` | `share` | `main` | inherit |
| `W8-SLV-CORE` (Wave 8B) | `w8-solver-core` | `share` | `main` | inherit |
| `W8-SLV-UI` (Wave 8C) | `w8-solver-ui` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** Zero open DRs at Wave 8 start (Rulings 68, 69, 70, 270, 273 settled all design/security/copy/CAM target
  questions). If an unexpected spec or copy ambiguity arises, Coordinator spawns `w8-owner` (`workspace: "inherit"`) to
  rule into `docs/notes/rulings.md` via `coord-decide.py rule next --request <id>`.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board &
  `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only` (backed by `pre-commit` floor +
  coordinator diff inspection), pack CLI `coord-mail.py` doorbells `unsupported`.
- **Deadline / Fallback:**
  - Each track has a 2–2.5 h wall budget and 400k context ceiling (warm `fork` at ~300–350k tokens if needed).
  - `W8-EXP` NuGet offline fallback: `Rhino3dm 8.35.0` is already cached in the local NuGet cache from Wave 6 `KRN-SPK`
    (verified offline-restorable); update `src/CfdWorkbench.Desktop/packages.lock.json` via `dotnet restore` without
    bypassing TLS.
  - `W8-SLV-CORE` WSL read-only rule: survey runs strictly read-only (`wsl.exe --status`, `wsl.exe --list --verbose`);
    NEVER mutate, start, or unregister `Ubuntu-24.04` or `cfdw-openfoam2512-arm64` during automated tests!
- **Termination condition:** Track ends when all exit evidence is independently verified by `w8-verifier` (`PASS` or
  `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py --no-push`, or when the 2-repair-cycle / budget cap
  fires.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` before dispatching | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-w8-coord`) |
| S1: Prune the 6 verified `ahead=0` merged remote branches on `origin` | Single remote-ref hygiene action (`git push origin --delete`); takes ~5 s, zero worker multiplier needed | Coordinator (`agy-w8-coord`) |
| S2: Serial wave order `W8-EXP` → `W8-SLV-CORE` → `W8-SLV-UI` | `W8-EXP` and `W8-SLV-CORE` share `CfdWorkbench.Core.csproj`, `CfdWorkbench.Cli/Program.cs`, `IdentityTests.cs`, and `tools/run-tests.sh` (real-display Desktop harness); `W8-SLV-UI` directly depends on `W8-SLV-CORE`'s service/projection/lint types and shares `DESIGN.md`/`Styles.axaml`/`WorkbenchTests.cs` (`GO5`) | Coordinator (`agy-w8-coord`) |
| S3: Joins run one at a time via `conductor-join.py --no-push` | Serial on `main`; runs full `tools/join-ring.sh` (`TESTS-GREEN`) + `xaml-token-lint` + `check-event-subscribers` + `run-verify-gates.py` per join | Coordinator (`agy-w8-coord`) |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W8-EXP` → `W8-SLV-CORE` | Clean `CfdWorkbench.Core.csproj` (with MIT `Rhino3dm 8.35.0`), `packages.lock.json`, `THIRD-PARTY-NOTICES.md`, `Program.cs` CLI dispatch, `IdentityTests.cs`, and `DESIGN.md` (`COPY-750..765` if used) | Hard wave boundary: `W8-EXP` joins onto `main` before `W8-SLV-CORE` branches |
| `W8-SLV-CORE` → `W8-SLV-UI` | Typed `SolverSetupService`, `SolverSetupProjection` (`SolverStatusSummary` for StatusStrip + step card view model), `SetupCopyLint` (shared with `SETUP-04` UI copy test), `SetupReportBuilder` (zero-PII Copy a report) | Hard wave boundary: `W8-SLV-CORE` joins onto `main` before `W8-SLV-UI` branches |
| `W8-EXP` → Operator (`SPIKE-CAD-02` future run) | Cross-platform `tools/spikes/cad-export-measure/generate-fixture.py` + `verify-open-measure.py` + `CadExportReleaseGate` reading `docs/proof/cad-export-measure/cad-open-measure.json` | Committed in `W8-EXP`; keeps `COPY-481` intact until external Fusion/FreeCAD/VCarve/Rhino 8 receipts are filled |

## Struck tracks

| track | why it was not worth its multiplier / why struck |
|---|---|
| `A5-RESUME` (`openfoam2512` apt + `petsc4Foam` `Allwmake`) | Live host probe (2026-11-23 07:40 AM) confirmed `dl.openfoam.com` (`curl rc=28`) and `sourceforge.net` (`curl rc=60`, untrusted Cisco Umbrella root `net.proxy-tls`). Cannot run on this network without bypassing TLS (forbidden). Ready to execute via `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` the moment the host joins an unfiltered network. |
| External commercial CAM/Rhino manual open-and-measure (`SPIKE-CAD-02` Fusion, FreeCAD CAM, Vectric VCarve/Aspire, Rhino 8) + `ADR-0013` acceptance | Live host probe (2026-11-23 07:40 AM) confirmed `Fusion=False`, `FreeCAD=False`, `Vectric=False`, `Rhino=False` on this machine. Cannot be executed autonomously without those applications; `W8-EXP` builds the full production engine + `SPIKE-CAD-02` fixture generator/verifier while keeping STEP/3DM UI honestly gated (`COPY-481`). |
| `ui-craft-gate.py` (`impeccable 0.1.27`) run | Live probe (2026-11-23 07:41 AM) exited `2`: `impeccable 0.1.27` ships no `windows-arm64` binary or checksum. Needs a macOS ARM64 or Windows x64 host. |
| Accessibility / screen-reader tracks (live NVDA speech, live macOS VoiceOver, `PropertiesFieldNudge` Narrator gate) | Explicitly excluded by operator directive of 2026-11-23 (*"ignore any accessibility features (like voice over)"*), and `NVDA=False` / Windows-only host. |
| Authenticode / SmartScreen & macOS codesign / Gatekeeper notarization | No code-signing certificates or macOS notarization host exist in this repository. |
| Running `W8-EXP` and `W8-SLV-CORE` concurrently | Both touch `CfdWorkbench.Core.csproj`, `IdentityTests.cs`, and `CfdWorkbench.Cli/Program.cs`, and both run `bash tools/run-tests.sh` (real-display Desktop harness; concurrent runs cause known exit-124 timeouts). Fails GO5 and GO6; width-1 serial execution across 3 right-sized `share` tracks gives 0 conflicts and 0 timeouts. |

## Cost justification (GO6 — 15× multiplier)

Honest Simplifier assessment: **width 1 across 3 isolated `share` tracks is the right shape.** Concurrent C#/.NET
tracks collide on `CfdWorkbench.Core.csproj`, `IdentityTests.cs`, `Program.cs`, `DESIGN.md`, and the real-display
Desktop harness, paying the 15× multiplier only to burn tokens on merge conflicts and timeouts. Splitting the work into
three crisp bounded-context milestones (`W8-EXP` Export → `W8-SLV-CORE` Solver Setup Backend/Security → `W8-SLV-UI`
Solver Setup UI) earns **context hygiene** (each track stays well under the 400k token ceiling without monolithic drift),
**domain-matched independent Adversary verification** (`w8-verifier` checks Computational Geometry on `W8-EXP`, Security
& Data Architect vetoes on `W8-SLV-CORE`, and UX/Token discipline on `W8-SLV-UI`), and **clean rollback boundaries** per
join.

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect hard veto + Simplifier soft veto + Tech Lead casting vote); fold C1–C7 | 1 subagent pass | Clears Test Architect hard veto before S0 commit |
| 2 | Render `docs/coordination/wave8-remaining-w5-w6-w7-closure.html`, run `docs-graph.py derive`, `check-docs.py`, `verify-ruling-citations.py`, `check-proof-pii.py`, append `prepare-for-coordination` audit entry, commit S0 on `main` | 1 commit | Clean primary required before any join |
| 3 | **S1:** Delete the 6 verified `ahead=0` merged remote branches on `origin` (`build/sdk-10.0.402`, `docs/laptop-handoff-2026-10-16`, `docs/rulings-237-238`, `inv-section-catalog-apply`, `investigate/drain-holder`, `win/old-pc-session-logs`) | 1 git command (~5 s) | Closes the Wave 5 stale-branch housekeeping item immediately |
| 4 | Start AGY swarm (`wave8-closure: W8-EXP -> W8-SLV-CORE -> W8-SLV-UI`), write `plan.json`, add `monitor` + `w8-verifier` (`inherit`), and dispatch **Wave 8A** `w8-cad-export` (`W8-EXP`, `share`) | width 1 + verifier | Delivers Area 7 STEP/3DM engine, CLI, gated Desktop UI, and `SPIKE-CAD-02` harness |
| 5 | Route `W8-EXP` to `w8-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w8-cad-export` | 1 serial join | Unblocks Wave 8B `W8-SLV-CORE` |
| 6 | Dispatch **Wave 8B** `w8-solver-core` (`W8-SLV-CORE`, `share`, branched from updated `main`) | width 1 | Delivers Area 5 Guided Solver Setup pure Core, Analysis `M1–M8` Product Launcher & I/O adapters, `AI-11` validator, and CLI |
| 7 | Route `W8-SLV-CORE` to `w8-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w8-solver-core` | 1 serial join | Unblocks Wave 8C `W8-SLV-UI` |
| 8 | Dispatch **Wave 8C** `w8-solver-ui` (`W8-SLV-UI`, `share`, branched from updated `main`) | width 1 | Delivers Area 5 Guided Solver Setup Dock UI + StatusStrip `Solver` item + `SETUP-04` copy lint |
| 9 | Route `W8-SLV-UI` to `w8-verifier` → repair if needed (cap 2) → join via `conductor-join.py --no-push` → `swarm kill w8-solver-ui w8-verifier monitor` | 1 serial join | Full Wave 8 convergence on `main` |
| 10 | Post `[done]` on swarm board, append Planned vs Actual to this plan, render `.html`, run `docs-graph.py derive` + `check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main`, and report full results to operator | 1 close commit | Clean Wave 8 closure |

## Gate record

| gate | reviewer | verdict | conditions folded |
|---|---|---|---|
| Independent Adversary Plan Review | `Wave 8 Adversary Plan Reviewer` (`44312930`; `Test Architect` hard veto · `Simplifier` soft veto · `Tech Lead` casting vote) | **PASS-WITH-CONDITIONS → CLEARED** | Folded all 7 verified conditions (`C1–C7`): (C1) added console-harness runner files `IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`, and `ShellModelTests.cs` to owned paths; (C2) added `src/CfdWorkbench.Desktop/packages.lock.json` and `THIRD-PARTY-NOTICES.md` to `W8-EXP`; (C3) split `W8-SLV-CORE` strictly along `STORE-SUBSET` (`src/CfdWorkbench.Core/SolverSetup/` strictly pure zero-I/O; all filesystem/process/HTTP/WSL/registry I/O and I/O tests in `CfdWorkbench.Analysis/SolverSetup/` and `CfdWorkbench.Analysis.Tests/`) and required `Rhino3dmExport` in Core to be pure in-memory `ToByteArray()`/`FromByteArray()` (else `Analysis/Export`); (C4) fixed oracle path to `cases/tools/foam-dict-lint.py` and replaced `generate-fixture.ps1` with cross-platform `generate-fixture.py`; (C5) corrected all GO14a scan guard roots/allowlists, added `tools/store_subset.py` (`STORE-SUBSET`), and locked `Styles.axaml` theme dictionaries (zero new theme `...Brush` keys per `verify-application-adapters.py`); (C6) specified Dock document hosting via `ShellHost.cs` + existing `"View"` menu (`Modal: false`) + `DesktopChecks.PrintScaleContext` for `--export-cad` and `--solver-setup`; (C7) added `…` ellipses on trimmed DC-189 Ruling 68/69/273 quotes. Tech Lead casting vote: APPROVE 3 serial width-1 `share` tracks + S1 branch prune. |

## Status

| | |
|---|---|
| **Completed** | Grounded layer state (`coord doctor` exit 0, 42/42 terminal requests, 3 merge drivers effective), verified Rulings 68/69/70 already cleared Guided Solver Setup (`M1–M8`, `DR-SEC-1 A`, `DR-F3-1 A`, `solver-setup.html`, `DR-SETUP-1..6`), verified live host probes, drafted & Adversary-cleared `docs/coordination/wave8-remaining-w5-w6-w7-closure.md` (`C1–C7` folded) |
| **Remaining** | S0 commit + S1 remote branch prune → serial swarm execution (`W8-EXP` → `W8-SLV-CORE` → `W8-SLV-UI`) → independent Adversary verification & `conductor-join.py --no-push` merges → Planned vs Actual closeout |
| **Best next action** | Render `.html`, run docs/PII/ruling/verify gates, commit S0 on `main`, prune the 6 merged remote branches (S1), and launch `monitor` + `w8-verifier` + Wave 8A `w8-cad-export` (`W8-EXP`) |
