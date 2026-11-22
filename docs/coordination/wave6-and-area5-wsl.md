---
id: coordination-wave6-and-area5-wsl
title: "Coordination plan - wave 6 and Area 5 WSL build (AGY native swarm)"
type: plan
status: complete
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, windows, wsl, agy-swarm, wave6]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: review-ui-as-built-workbench, rel: implements }
  - { to: design-local-sweeps, rel: implements }
  - { to: design-setup-seeding, rel: implements }
  - { to: adr-0013-cad-export-kernel, rel: implements }
  - { to: proof-wsl-preflight, rel: refines }
  - { to: coordination-wave5-next-steps, rel: refines }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-20"
summary: >-
  Wave 6 + Area 5 WSL build executed on AGY native swarm at max width 3 with at most one C#/Desktop-test track active
  per wave: Wave A runs Area 5 WSL OpenFOAM 2512 + CUDA 13.2 + PETSc-CUDA/petsc4Foam build (A5-WSL) in parallel with
  Wave 6 UI P0 Blocker hardening (UI-P0, 15 Blockers) while w6-owner rules DR-SWP-1..6, DR-SET-1..5, and DR-KRN-1,3,4,6
  (immediately unlocking KRN-SPK Area 7 SPIKE-CAD-01 bake-off in Wave A); Wave B builds Area 4 Local Sweeps (SWP-BUILD)
  alongside ongoing A5-WSL; Wave C builds Area 1 Setup & Goal State (SET-BUILD).
---

# Coordination plan: wave 6 and Area 5 WSL build (AGY native swarm)

**Input.** Operator `/also` directive of 2026-11-21: push `main` (completed at `c2cca2f5`), then build and execute the
total coordination plan for **Wave 6 and Area 5 WSL build in parallel** using `/prepare-for-coordination` and
`/execute-with-coordination --swarm`.

**Goal state.**
- **Goal:** Execute Wave 6 (`UI-P0` Desktop UI blocker hardening, `KRN-SPK` Area 7 `SPIKE-CAD-01` bake-off, `SWP-BUILD`
  Area 4 Local Sweeps, and `SET-BUILD` Area 1 Setup & Goal State) in parallel with **Area 5 WSL Build (`A5-WSL`)**
  (dedicated arm64 WSL2 OpenFOAM 2512 + CUDA 13.2 + PETSc-CUDA + `petsc4Foam` build & qualification on the Blackwell
  RTX Spark N1X `sm_121` host) with zero file contention and zero Desktop harness timeouts.
- **Done when:** Every dispatched track returns observed exit evidence, clears independent Adversary review (`w6-verifier`:
  Test Architect hard veto + domain/UX/CG/Security/Simplifier lenses), converges onto `main` via `conductor-join.py`
  with `tools/run-tests.sh` (on code/tools merges, never concurrently with an active Desktop test run), `tools/check-docs.py`,
  and `run-verify-gates.py` 10/10 PASS, all share worktrees are cleaned, and Planned vs Actual is appended to this plan.
- **Not in scope:** Area 5 C# Guided Solver Setup (`SETUP-01..09`) product code (waits on `A5-WSL` measured Windows arm64
  build ID & smoke receipts per `docs/design/guided-solver-setup.md` §10); Area 7 production STEP/3DM code (waits on
  `KRN-SPK` + operator commercial CAM fixture `SPIKE-CAD-02`); non-blocker UI P1/P2 polish items; pushing post-Wave-6
  commits to `origin` without operator confirmation; mutating the host's existing `Ubuntu-24.04` WSL distro.
- **Tier:** T2 · **Fan-out cap:** 3 concurrent authors (at most **1** C#/Desktop-test author at a time to eliminate
  concurrent Desktop harness exit-124 timeouts observed in Wave 2).

**Seats.**
- **Coordinator:** Main AGY session (`agy-w6-coord`). Owns the plan, seams, join order, `conductor-join.py` merges, and
  ruling commits. Never authors track code.
- **Owner:** Independent swarm worker `w6-owner` (`workspace: "inherit"`, read-only on `src/`). Deliberates on
  `DR-SWP-1..6`, `DR-SET-1..5`, and `DR-KRN-1,3,4,6` (plus any mid-wave decision requests via
  `coord-decide.py request --to w6-owner`) and records numbered rulings (next sequential ruling numbers allocated by
  `coord-decide.py rule next`) in `docs/notes/rulings.md`; Coordinator commits rulings on `main` before dependent tracks
  dispatch.
- **Adversary Verifier:** Independent swarm worker `w6-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track's committed diff, red-first tests, mutants, contrast/a11y fixes, WSL receipts, and spec/ADR conformance before
  any join.
- **Stall Monitor:** Existing swarm worker `monitor` (`workspace: "inherit"`, `swarm_monitor` skill loaded).

**Leader ref.** Released at **epoch 5** and **not pinned** (carrying forward Wave 3 & Wave 5's verified deviation:
pinning broke local joins while released epoch 5 joined cleanly across Waves 2, 3, and 5).

## Layer state

Measured 2026-11-21 02:44Z–02:50Z by `python docs/ai-forward-pack/scripts/coord-core.py doctor`,
`python docs/ai-forward-pack/scripts/coord-core.py session list`, `python docs/ai-forward-pack/scripts/pack-doctor.py --json`,
`git worktree list`, and direct WSL/GPU host probes in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `c2cca2f5`, clean, synced with `origin/main`).

| check | result | meaning |
|---|---|---|
| registry | `ok - 7 pattern(s)` | `.agents/artifacts.yml` present; 3 managed + 4 repo patterns |
| merge driver | `effective - coord-regen, coord-register, defect-register declared and registered` | derived & register artifacts merge cleanly |
| leader | `released (epoch 5)` | not pinned (see Leader ref above) |
| heartbeat | `1 session(s) beating; newest beat 3493374 s ago; 0 stalled, 0 live, 0 blocked, 0 done` | nothing live in flight |
| sessions | `0 active session(s); 245 file(s) scanned` | clean start |
| requests | `ok - 37 request(s), 0 open, 37 terminal` | no unanswered decision requests |
| lease overlap | `none (0 live lease(s))` | clean start |
| worktrees | `1 (D:/Projects/CFD-Workbench c2cca2f5 [main])` | primary only |
| pack-doctor | `22 PASS · 2 WARN · 0 FAIL` | WARN 1: use `python` not `python3` on Windows; WARN 2: archival freshness (`check-docs.py` exit 0) |
| WSL / GPU host | `Ubuntu-24.04` (WSL2, Stopped) · `aarch64/arm64` · 20 cores · `NVIDIA RTX Spark N1X (6144-core Blackwell RTX GPU), compute_cap 12.1, driver 617.42, 64000 MiB` · `/` 955G free, `C:` 789G free, `D:` 3.7T free | `D:\reviewed` holds verified `openfoam-archive-keyring.gpg`, `cuda-archive-keyring.gpg`, `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz`, `SHA256SUMS`, `SHA256SUMS.gpg`; OpenFOAM & CUDA not yet installed |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifiers/owner/monitor) | each author gets an isolated linked working copy; `git config core.longpaths true` active |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token wakeup; `monitor` already attached |
| agy ownership hooks (`.agents/hooks.json`) | `observed-only` (`.agents/rules/agy-surface.md:39-40`) | `pre-commit` floor + coordinator diff inspection enforce path boundaries |
| pack CLI mail doorbells (`coord-mail.py`) | `unsupported` (unqualified on this host) | board `@mention` + `swarm_wait` is the sole enforced wakeup mechanism |

The coordination layer is installed **once, in the primary checkout** (`D:/Projects/CFD-Workbench`). Every share
workspace created by `swarm{action:"add", workspace:"share"}` **inherits** `.git/config` merge drivers and `.git/hooks`
from the primary checkout — workers confirm inherited state with `git config --get-regexp 'merge\..*\.driver'` (or
`coord doctor`) and **never** run `coord install` inside a share workspace.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | `coord-register` union merge | **no** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen`: `audit-log.py --root docs --project CFD-Workbench render` | **no** |
| `docs/docs-index.js` | derived | `coord-regen`: `docs-graph.py derive` | **no** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | `coord-register` union merge | **no** |
| `docs/lessons/defect-classes.md` | register | `defect-register` (`tools/merge-defect-register.py`) | **no** |
| `docs/notes/rulings.md` | authored, single writer | `w6-owner` deliberates; Coordinator commits on `main` before dependent dispatches/joins | serial (spine) |
| `DESIGN.md` copy rows | authored, serial range rule | High water `COPY-693` (proposed). `UI-P0` does **not** edit `DESIGN.md` (`Styles.axaml` holds new `CatalogList*Height` tokens); `SWP-BUILD` reserves `COPY-706..727`; `SET-BUILD` reserves `COPY-728..749` | serial (`SWP-BUILD` → `SET-BUILD`) |
| `src/CfdWorkbench.Core/AuthoringSession.cs`, `RunRecord.cs`, `src/CfdWorkbench.Analysis/OperatingPoint.cs`, `Labels.cs`, `src/CfdWorkbench.Cli/Program.cs`, `src/CfdWorkbench.Desktop/ModelArea.axaml(.cs)`, `WorkbenchController.cs`, `Shell/*` | authored, serial across waves | Wave A: `UI-P0` owns Desktop + `SectionDisplay.cs` only; Wave B: `SWP-BUILD` owns Core/Analysis/CLI/Desktop sweep wiring; Wave C: `SET-BUILD` owns Core/Analysis/CLI/Desktop setup wiring | serial (`UI-P0` → `SWP-BUILD` → `SET-BUILD`) |
| `tools/wsl-build-openfoam-cuda.sh`, `docs/proof/wsl-area5-build/**` | authored, exclusive | `A5-WSL` only (zero overlap with `src/`, `tests/`, or `tools/spikes/`) | owner (`A5-WSL`) |
| `tools/spikes/cad-export-spike/**`, `docs/proof/spike-cad-01/**` | authored, exclusive | `KRN-SPK` only (read-only against `src/CfdWorkbench.Core`; zero edits to `CFDWorkbench.slnx`, `src/`, or `tests/`) | owner (`KRN-SPK`) |
| all other track files | authored | strictly one owner per file (see Tracks) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- `src/` & `tests/`: strictly **one** C#/Desktop-test author active in each wave (`UI-P0` in Wave A, `SWP-BUILD` in
  Wave B, `SET-BUILD` in Wave C). `A5-WSL` and `KRN-SPK` never edit `CFDWorkbench.slnx`, `src/`, `tests/`, or `DESIGN.md`. Jointly satisfiable.
- `xaml-token-lint.py` & `check-event-subscribers.py` (`docs/coordination/join.json`): root `src/CfdWorkbench.Desktop/`;
  recursion `*.axaml` / `*.cs` excluding `bin/`/`obj/`; token set raw hex/brush/size literals in `.axaml` and event
  subscription balance; allowlist none. Only the active C# wave track writes under `src/CfdWorkbench.Desktop/`: jointly satisfiable.
- `PROOF-PII` & `verify-no-machine-paths.py` (`tools/check-docs.py`, `run-verify-gates.py`): root repo tracked **and**
  untracked non-ignored text files; recursion all non-binary files; token set Windows user-profile paths (`%USERPROFILE%`),
  machine SIDs, hostnames (`own-host`, including OpenFOAM `Host   : <host>` banners), and raw local checkout paths in
  machine-readable files; allowlist 1 existing allowlisted file in `tools/check-proof-pii.py`. `A5-WSL` and `KRN-SPK`
  scrub every hostname, OpenFOAM `Host` banner, and user path before writing `docs/proof/**` and commit zero binaries
  (`.tar.gz`/`.deb`/`.so`/`.o`/`.step`/`.3dm`/`.dll`/`.nupkg`): jointly satisfiable.
- `verify-portable-text-io.py` / `verify-subprocess-utf8.py` / `check-text-mode-hash.py` (`run-verify-gates.py`,
  `tools/check-docs.py`): root `tools/` (plus pack scripts); recursion `**/*.py` excluding `bin/`/`obj/`/`__pycache__/`;
  token set text-mode I/O without `encoding="utf-8"`/`newline="\n"` or stdio `.reconfigure`, subprocess calls without
  `encoding="utf-8"`, and text-mode SHA-256 hashing; allowlist 1 file in `check-text-mode-hash.py`. Any Python helper
  added by `A5-WSL` or `KRN-SPK` under `tools/` satisfies all three: jointly satisfiable.
- `tools/coordination/check-test-paths.py` & `tools/check-reader-sharing.py` (`tools/check-docs.py`): root
  `tests/CfdWorkbench.Core.Tests/` (`*.cs`, forbids relative `"docs/..."` reads — `SWP-BUILD` `T7` reading
  `docs/proof/swp-old-build/` MUST use `PlacementTests.RepoRoot()`) and `src/` (`**/*.cs`, requires `UserFile.OpenRead`
  instead of bare `File.ReadAll*` for user documents); allowlist none. Only one C# track writes `src/`/`tests/` per wave: jointly satisfiable.
- `docs-graph.py validate` & `verify-ruling-citations.py`: root `docs/`; recursion all `.md` files; token set unique V2
  frontmatter `id`s and `Ruling \d+` / `R-\d+` citations; allowlist none. Tracks create strictly distinct unused artifact
  IDs (`proof-wsl-area5-build`, `proof-spike-cad-01`, `proof-swp-old-build`, `review-local-sweeps-native`,
  `review-setup-seeding-native`): jointly satisfiable.

**Quoted ADR / Ruling trigger lines (DC-189 — exact citations, never paraphrased).**
- **ADR-0010 §Decision (1, 4, 5)** (`docs/adr/0010-one-placement-rule.md:63-88`):
  > "1. One rule, one site. `src/CfdWorkbench.Core/Placement.cs` holds `PlacementRule`… 4. Displays receive geometry, never
  > compute it… 5. Binding controls: `Placement_DisplayWithinCertifiedEnclosure_Fixtures` (display points at most 1 nm
  > outside the certified `PointAt` enclosure, on fixtures that include a 2 m chord and a C⁰ thickness peak, plus a
  > property set)…"
- **ADR-0011 §Decision (1, 3, 7)** (`docs/adr/0011-analysis-run-storage.md:53-85`):
  > "The format string is derived by one function from the run count: no run → `cfdw-project-1`, byte-identical to today's
  > writer; one or more → `cfdw-project-2`… a document-size preflight (`DOC-SIZE`) before save… Never prune a run that a
  > Discrepancy record references (A3d)."
- **ADR-0013 §Decision (3)** (`docs/adr/0013-cad-export-kernel.md:59-61`):
  > "Before building production OCCT packaging, `SPIKE-CAD-01` (`docs/architecture/cad-export-kernel.md` §5.1) will verify
  > `win-arm64` / `win-x64` / `osx-arm64` ABI/packaging for `rhino3dm`/`openNURBS` and OCCT **and** benchmark **Option B
  > (a pure-C# tensor-product B-spline skinner + direct ISO 10303-21 STEP AP203 writer)** on all 4 export wings from
  > `docs/design/export.md` §3.3 plus the ADR-0010 twisted/blended wing."
- **Ruling 263** (`docs/notes/rulings.md:1977`):
  > "`NativeProject.MaxBytes` becomes 32,000,000 bytes, and its budget stands unchanged (open within 3 s, save within 3 s,
  > peak working set within 1.5 GB at the cap)."
- **Ruling 269** (`docs/notes/rulings.md:2013`):
  > "PlacementRule and both IPlacementScalar instantiations stay untouched; ADR-0010's 1 nm certified-enclosure bound and
  > bit-identical equal-spacing golden masters (XPA-I3) remain binding."

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **A5-WSL** Area 5 WSL OpenFOAM 2512 + CUDA 13.2 + PETSc-CUDA + `petsc4Foam` build | `tools/wsl-build-openfoam-cuda.sh` (new), `docs/proof/wsl-area5-build/README.md` (new), `docs/proof/wsl-area5-build/*.json` (manifests/receipts; zero binaries/tarballs) | — (runs across Wave A → Wave B; join held to a wave boundary when no C# worker runs `Desktop.Tests`) | T2 | 1 | 120 calls · 300k · 2 h 30 min · ceiling 400k | (1) Pre/post `wsl.exe --list --verbose` proves host `Ubuntu-24.04` remains `Stopped` and untouched (never invoke `wsl -d Ubuntu-24.04`, clone, or mount it); abort if `cfdw-openfoam2512-arm64` or `D:\wsl\cfdw-openfoam2512-arm64` already exists; verify `D:\reviewed\ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz` against both `SHA256SUMS` and `gpgv` `SHA256SUMS.gpg`, then import dedicated WSL2 distro `cfdw-openfoam2512-arm64` at `D:\wsl\cfdw-openfoam2512-arm64`; (2) run `tools/wsl-preflight.sh --keyrings /mnt/d/reviewed --out <receipt.json>` inside `cfdw-openfoam2512-arm64` → exit 0; (3) install signed reviewed packages (`openfoam2512*=2512.0-2`, `cuda-toolkit-13-2=13.2.2-1`, GCC-13/OpenMPI/BLAS/LAPACK; zero removals; zero Linux GPU driver); (4) OpenFOAM v2512 arm64 CPU `icoFoam/cavity` smoke passes to `t = 0.5` with M1 `Disallowing` banner, exact arm64 build id recorded, Courant mean `0.222158` verified; (5) check `nvcc --list-gpu-arch` (verify Blackwell `sm_121` vs `120` PTX) and PETSc 3.26.0 `./configure --help`, verify `petsc-3.26.0.tar.gz` SHA-256 (`f5230023e6e22ee607802a13c82bc25f3d81e71654ad386a5b9bdff17fed93df`, 16,933,940 B) and `petsc4Foam` `git rev-parse HEAD == cb1badc4978306e7001a4654dcdf53f284a92365`, build PETSc `--with-cuda=1` (float64, 32-bit indices, shared libs, system OpenMPI/BLAS/LAPACK, zero `--download-*`) + `petsc4Foam` `Allwmake` and verify `foamHasLibrary -verbose petscFoam` (or record exact measured upstream blocker honestly without faking pass); (6) real CPU vs PETSc-CUDA pressure-solve trial receipt (`-ksp_view`, CUDA kernel evidence, wall times; OpenFOAM `Host` banners, hostnames, and user paths scrubbed); zero binaries committed; `python tools/check-docs.py` & `tools/check-proof-pii.py` green; Adversary pass (`w6-verifier`: SRE + Security + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **UI-P0** Wave 6 UI P0 Blocker hardening | `src/CfdWorkbench.Desktop/**` (files cited by `F-01..F-17`, `F-30`, `F-31`), `src/CfdWorkbench.Analysis/SectionDisplay.cs` (`F-30` Ncrit fix only), `tests/CfdWorkbench.Desktop.Tests/**`, `tests/CfdWorkbench.Analysis.Tests/**` (Ncrit regression test) — does **not** edit `DESIGN.md` | — (Wave A sole C#/Desktop track) | T2 | 1 | 180 calls · 380k · 2 h 30 min · ceiling 400k | Every one of the **15 Blockers** (`F-01..F-14`, `F-16`) + coupled visual/copy Majors (`F-15`, `F-17`, `F-30`, `F-31`) from `docs/reviews/ui-as-built-workbench.md` proved **red first**, then green, with mutants killed: `Save As…` always opens picker; dirty guard fires on New/Example/Open/Recent; Command Palette routes all commands & fixes Windows Row 0; `Compare` reachable from both tab and menu (`IsPaneCommand`/`IsShellCommand` + `PanelTabs.SelectionChanged`); `Styles.axaml` hover/HC contrast ≥ 4.5:1 on `StartView`, `CatalogDialog` disabled rows, and `PointsPane` (`pts-cell.ro` incl.); `SectionChartView` uses `PrimaryBrush`/`WarningBrush` and fixed Y-ticks; `LoadingChart` table scrolls; dead `SectionEditor` & `LegacyEditor` deleted & `ModePlate`/`ModeSourceChip` stacked; `CatalogDialog` bounded height (`Styles.axaml` tokens); `F6` cycles all live docks + station keyboard walk; OS High Contrast & OS Reduced Motion wired + dialog theme propagation + static Export progress under reduced motion; `FindAlphaDialog` + AT `LiveSetting`/`LabeledBy`/names/`SectionCanvas` peers/`PointsPane` `Speech.Unit` fixed; `SectionDisplay.cs` Ncrit & `LAB-01` `"certified"` fixed (`COPY-584`/`585` untouched); `bash tools/run-tests.sh` green, `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, `--theme-matrix` passing on expanded controls; Adversary UX & Accessibility + Test Architect pass *(Note: `F-23` 200% modal scaling and live NVDA/VoiceOver traces remain open predicates of `ui-as-built-workbench.md` §1's product accessibility veto after `UI-P0`)* | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **KRN-SPK** Area 7 `SPIKE-CAD-01` empirical bake-off & `win-arm64` ABI probe | `tools/spikes/cad-export-spike/**` (new), `docs/proof/spike-cad-01/README.md` (new), `docs/proof/spike-cad-01/*.json` (receipts; zero `.step`/`.3dm`/`.dll`/`.nupkg` committed; never touches `CFDWorkbench.slnx`, `src/`, or `tests/`) | `W6-0` ruling on `DR-KRN-1,3,4,6` committed on `main` (CO-S0; dispatches in Wave A right after ~5 min `W6-0`; join held to wave boundary when no C# worker runs `Desktop.Tests`) | T1 | 1 | 90 calls · 250k · 1 h 30 min · ceiling 400k | Empirical execution of `ADR-0013` `SPIKE-CAD-01`: (1) real `win-arm64` NuGet/native build & load probe for `Rhino3dm` / `openNURBS` and OCCT availability (exact versions, RID assets, load exit/error observed); (2) pure-C# Option B tensor-product B-spline skinner + ISO 10303-21 STEP AP203 writer consuming `CfdWorkbench.Core` read-only over all 5 benchmark wings (`docs/design/export.md` §3.3 + ADR-0010 twisted/blended wing), reporting exact measured A-vs-B closest-point deviation (µm vs `10 µm` floor), topology/manifold sanity checks, zero-PII header verification, and build/run times; (3) **red-first negative control**: planted twist omission (> 10 µm) and planted PII header string MUST turn the spike verifier red; (4) if no local OCCT/FreeCAD STEP reader is installed on host, record `BRepCheck_Analyzer` read-back explicitly as `Flagged / deferred to SPIKE-CAD-02`; (5) data-backed Option A vs Option B recommendation in `docs/proof/spike-cad-01/README.md`; any Python helper satisfies `verify-portable-text-io.py`/`verify-subprocess-utf8.py`; `python tools/check-docs.py` & `tools/check-proof-pii.py` green; Adversary pass (`w6-verifier`: Computational Geometry + Security + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **SWP-BUILD** Area 4 Local-Tier Sweeps & Performance Curves (`SWP-1..5`) | `src/CfdWorkbench.Core/SweepRecord.cs` (new), `RunRecord.cs`, `AuthoringSession.cs`, `src/CfdWorkbench.Analysis/{SweepSchedule,SweepService,SweepProjection}.cs` (new), `AnalysisService.cs`, `Compare.cs`, `OperatingPoint.cs` (if `DR-SWP-4`/`DR-SET-2` selects `op.Load`), `Labels.cs`, `src/CfdWorkbench.Cli/SweepVerb.cs` (new), `Program.cs`, `src/CfdWorkbench.Desktop/Sweeps/**` (new), `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/StatusStrip.axaml(.cs)`, `Shell/CommandTable.cs`, `Shell/ShellHost.cs`, `tests/CfdWorkbench.{Core,Analysis,Cli,Desktop}.Tests/Sweep*Tests.cs`, `docs/proof/swp-old-build/`, `docs/reviews/local-sweeps-native.md`, `DESIGN.md` (`COPY-706..727`) | `UI-P0` joined; `W6-0` ruling on `DR-SWP-1..6` committed on `main` (Wave B sole C#/Desktop track) | T2 | 1 | 220 calls/seg (400 total) · 900k · 3 h · ceiling 400k/seg (sequential green commits: `SWP-1` Core → `SWP-2` Analysis → `SWP-3` CLI → warm `fork` at ~300k tokens → `SWP-4` Desktop + `SWP-5` Copy/Proof) | All 18 red-first tests (`T1`–`T18` in `docs/design/local-sweeps.md` §8) observed red → green with named mutants killed: Core `SweepRecord` + `GoalStateSweepSnapshot` seam (`AuthoringSession.CurrentGoalSweepSnapshot()` returning `null` until Wave C) + generalized `Format(runs, setup, experiments)` admitting `["analysis","setup","experiments"]` + `.v1.bak` + retention/Archive pinning + old-build `COPY-130` proof (`T7` uses `PlacementTests.RepoRoot()`, zero relative `"docs/..."` reads); Analysis `SweepSchedule`, dedicated `SweepService` (120-case sequential, idempotent `runKey`, ≤ 250 ms Cancel, failed-case gap with `runId == null`, `DOC-SIZE` stop), `SweepProjection` (`CL`, `CD Ncrit 2–4`, `L/D`, `Cm` frame origin, non-bridged gaps, Historical by run key); CLI `sweep preview`/`sweep run`/`inspect --sweeps` bitwise `runKey` parity; Desktop `Sweeps` navbar segment (`Experiment setup` · `Run (local)` · `Results`, zero Run button in setup, linked selection, table twins, `LAB-01` 0 hits); `bash tools/run-tests.sh` green, `check-docs.py` green, Adversary pass (Hydrodynamicist + Data & Persistence + UX & Accessibility + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |
| **SET-BUILD** Area 1 Setup & Goal State (`SET-1..4`) | `src/CfdWorkbench.Core/{SetupRecords,DisciplinePresets,RecipeSeeder,ClassRules,SanityBounds}.cs` (new), `src/CfdWorkbench.Core/CatalogData/SanityBounds.json` (new, auto-embedded by `CfdWorkbench.Core.csproj:10`), `src/CfdWorkbench.Core/RunRecord.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs`, `src/CfdWorkbench.Analysis/GoalFeasibility.cs` (new), `OperatingPoint.cs`, `Labels.cs`, `src/CfdWorkbench.Cli/SeedVerb.cs` (new), `Program.cs`, `src/CfdWorkbench.Desktop/Setup/**` (new), `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/StatusStrip.axaml(.cs)`, `Shell/CommandTable.cs`, `Shell/ShellHost.cs`, `tests/CfdWorkbench.{Core,Analysis,Cli,Desktop}.Tests/Setup*Tests.cs`, `docs/reviews/setup-seeding-native.md`, `DESIGN.md` (`COPY-728..749`) | `SWP-BUILD` joined; `W6-0` ruling on `DR-SET-1..5` committed on `main` (Wave C sole C#/Desktop track) | T2 | 1 | 220 calls/seg (400 total) · 900k · 3 h · ceiling 400k/seg (sequential green commits: `SET-1` Core → `SET-2` Analysis → `SET-3` CLI → warm `fork` at ~300k tokens → `SET-4` Desktop + Copy/Proof) | All 15 red-first tests (`T1`, `T2`, `T2b`, `T3`–`T14` in `docs/design/setup-seeding.md` §7) observed red → green with named mutants killed: Core `SetupRecords` (`Envelope.setup`, BLAKE3(JCS) immutable `SetupBriefVersion`/`GoalStateVersion`/`DrcExclusion`, `-1` byte-identity intact) + wires `AuthoringSession.CurrentGoalSweepSnapshot()` to live Goal states without touching `Desktop/Sweeps/**`, exact 2D deterministic soft-target `RecipeSeeder` (`κ(λ) = (3+λ)/4`, TipChord minimum, FoilDSL 4.1 one-way emission), all 9 discipline presets + GEN fallback + `SETUP-FLAGGED-UNACK`, GWA/iQFOiL/Formula Kite/open-class DRC + `CatalogData/SanityBounds.json`; Analysis `GoalFeasibility` matching `GOAL-02` 7-point + `0.3 m` exact oracle + `OperatingPoint.Load` alignment; CLI `seed` + `inspect --json` hash parity; Desktop `Setup` view with all 6 hard states + no-key `AI-02` slot (`COPY-53`) + `LAB-01` 0 hits; `bash tools/run-tests.sh` green, `check-docs.py` green, Adversary pass (Marine-CAD UX + Hydrodynamicist + Data & Persistence + Test Architect) | AGY swarm (`workspace: "share"`): isolation `enforced`, board/wait `enforced`, path hooks `observed-only`, pack mail doorbells `unsupported` |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `monitor` | `inherit` | `main` | inherit (existing) |
| Owner seat | `w6-owner` | `inherit` | `main` | inherit |
| Adversary verifier | `w6-verifier` | `inherit` | `main` | inherit |
| `A5-WSL` (Wave A→B) | `w6-wsl-build` | `share` | `main` | inherit |
| `UI-P0` (Wave A) | `w6-ui-hardening` | `share` | `main` | inherit |
| `KRN-SPK` (Wave A, post-`W6-0`) | `w6-cad-spike` | `share` | `main` | inherit |
| `SWP-BUILD` (Wave B) | `w6-sweeps-build` | `share` | `main` | inherit |
| `SET-BUILD` (Wave C) | `w6-setup-build` | `share` | `main` | inherit |

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** `w6-owner` rules into `docs/notes/rulings.md` via `coord-decide.py rule next --request <id>` under the
  standing autonomy precedent (Rulings 259–266, 269; operator may revisit any ruling/copy). Coordinator commits rulings
  on `main` before dependent tracks dispatch or join.
- **Harness capabilities (verified on this host):** workspace isolation `enforced` (`share` per author), board &
  `swarm_wait` `enforced`, ownership hooks `.agents/hooks.json` `observed-only` (backed by `pre-commit` floor +
  coordinator diff inspection), pack CLI `coord-mail.py` doorbells `unsupported`.
- **Deadline / Fallback:**
  - `A5-WSL`: 2.5 h wall / fallback: if PETSc-CUDA or `petsc4Foam` hits a verified upstream Blackwell `sm_121` / OpenFOAM
    2512 arm64 build defect after 1 bounded repair, commit the verified OpenFOAM v2512 arm64 CPU smoke receipt + exact
    PETSc/CUDA failure receipt (`result: BLOCKED` with full scrubbed logs/hashes) without faking a pass; keep dedicated
    distro cleanly stopped for inspection.
  - `UI-P0`: 2.5 h wall (warm `fork` if context approaches 300k–350k tokens) / fallback: land P0-A + P0-B + P0-C if P0-D
    hits budget, recording exact remaining P0-D items for follow-up (never landing half-wired dead code).
  - `KRN-SPK`: 1.5 h wall / fallback: commit verified `win-arm64` ABI probe + completed Option B wings with any failing
    wing explicitly documented as evidence for Option A.
  - `SWP-BUILD` / `SET-BUILD`: 3 h wall each with sequential green per-node commits (`SWP-1`→`SWP-2`→`SWP-3`, warm `fork`
    at ~300k tokens → `SWP-4`+`SWP-5`; `SET-1`→`SET-2`→`SET-3`, warm `fork` at ~300k tokens → `SET-4`) / fallback: clean
    Core+Analysis+CLI checkpoint joinable only under an explicit Owner narrowing ruling; never half-wired Desktop UI.
- **Termination condition:** Track ends when all exit evidence is verified first-hand by `w6-verifier` (`PASS` or
  `PASS-WITH-CONDITIONS` resolved) and joined via `conductor-join.py`, or when the 2-repair-cycle / budget cap fires
  (escalated to Coordinator, never ignored).

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, audit entry) on `main` before dispatching or joining | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-w6-coord`) |
| S1 (`W6-0`): `w6-owner` deliberates and rules `DR-SWP-1..6`, `DR-SET-1..5`, and `DR-KRN-1,3,4,6` into `docs/notes/rulings.md` (next sequential ruling numbers allocated by `coord-decide.py rule next`); Coordinator commits on `main` | `KRN-SPK` requires `DR-KRN-1,3,4,6` ruled before dispatch (CO-S0); `SWP-BUILD` and `SET-BUILD` share the `Envelope` format (`DR-SWP-1` / `DR-SET-1`) and `OperatingPoint.Load` (`DR-SWP-4` / `DR-SET-2`) decision edges (`GO5(b)`) | `w6-owner` rules → Coordinator commits |
| S2: `UI-P0` joins to `main` before `SWP-BUILD` dispatches | Both edit `src/CfdWorkbench.Desktop/` (`ModelArea.axaml`, `WorkbenchController.cs`, `Shell/StatusStrip`, `Shell/CommandTable`, `Shell/ShellHost`) and run `Desktop.Tests` (`GO5` shared-file & harness exclusivity) | Coordinator |
| S3: `SWP-BUILD` joins to `main` before `SET-BUILD` dispatches | Both edit `AuthoringSession.cs`, `RunRecord.cs`, `OperatingPoint.cs`, `Labels.cs`, `Cli/Program.cs`, `Desktop/`, and `DESIGN.md` (`GO5` shared-file & harness exclusivity) | Coordinator |
| S4: Joins run one at a time via `conductor-join.py` **and never while any C# worker is executing `Desktop.Tests`** | `tools/join-ring.sh:15` runs `tools/run-tests.sh` (including the real-display Desktop harness) on any merge touching `src/`, `tests/`, **or `tools/`** (`KRN-SPK` and `A5-WSL` touch `tools/`). Hold `tools/` joins to wave boundaries (between `UI-P0`, `SWP-BUILD`, and `SET-BUILD`) so two Desktop harnesses never run concurrently | Coordinator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `W6-0` -> `KRN-SPK`, `SWP-BUILD`, `SET-BUILD` | Rulings on `DR-KRN-1,3,4,6`, `DR-SWP-1..6`, and `DR-SET-1..5` (including unified `Envelope` `["analysis","setup","experiments"]` on `cfdw-project-2` and `OperatingPoint.Load` contract) | `w6-owner` rules at Wave A start; Coordinator commits before dispatching `KRN-SPK`, `SWP-BUILD`, and `SET-BUILD` |
| `UI-P0` -> `SWP-BUILD` | Clean `Styles.axaml`, `ShellHost`, `CommandTable`, `NativeMenuBuilder`, `ModelArea`, `AnalysisPanel`, and `StatusStrip` baseline (15 Blockers cleared) | Hard wave boundary: `UI-P0` joins before `SWP-BUILD` branches from updated `main` |
| `SWP-BUILD` -> `SET-BUILD` | Generalized `RunRecord.Format` / `NativeProject.Read` (`["analysis","setup","experiments"]`), `GoalStateSweepSnapshot` defined in `CfdWorkbench.Core` with `AuthoringSession.CurrentGoalSweepSnapshot()` returning `null` until Wave C, and `DESIGN.md` `COPY-706..727` landed | Hard wave boundary: `SWP-BUILD` joins before `SET-BUILD` branches; `SET-BUILD` wires live Goal states into `AuthoringSession.CurrentGoalSweepSnapshot()` purely in `Core` without editing `Desktop/Sweeps/**` |
| `A5-WSL` -> future Area 5 `T1/T5` | Measured Windows arm64 OpenFOAM v2512 build ID, CPU smoke Courant mean, and PETSc-CUDA qualification receipt | Committed in `docs/proof/wsl-area5-build/README.md` (zero file coupling with Wave 6 C# tracks) |
| `KRN-SPK` -> future Area 7 build | Measured `win-arm64` ABI availability for `Rhino3dm`/OCCT + Option B pure-C# STEP deviation vs `10 µm` floor | Committed in `docs/proof/spike-cad-01/README.md` (zero file coupling with Wave 6 C# tracks) |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Running `SWP-BUILD` and `SET-BUILD` in parallel | Both author `AuthoringSession.cs`, `RunRecord.cs`, `OperatingPoint.cs`, `Labels.cs`, `Cli/Program.cs`, Desktop shell/model files, and `DESIGN.md`, AND both run the Desktop pixel/focus harness (known exit-124 timeout under concurrent runs). Fails GO5; pipelining `UI-P0 → SWP-BUILD → SET-BUILD` eliminates all conflicts and timeouts. |
| Area 5 C# Guided Solver Setup build (`SETUP-01..09`) | `docs/design/guided-solver-setup.md` §10 explicitly requires real Windows evidence (build ID, Courant mean, WSL behavior) before pinning `T5` adapters. `A5-WSL` captures that real arm64 WSL evidence first without guessing pins. |
| Area 7 production STEP/3DM build (`EXP-02`, `EXP-04`) | Blocked on `SPIKE-CAD-01` (`KRN-SPK`) empirical bake-off + `SPIKE-CAD-02` commercial Fusion/CAM open-and-measure fixture (`ADR-0013`). |
| Splitting `UI-P0` into 4 micro-tracks (`P0-A..P0-D`) | All 4 sub-packages share `Styles.axaml`, `ShellHost.cs`, `MainWindow.axaml.cs`, `ModelArea.axaml`, and `Desktop.Tests`. One coherent track avoids the 15× multiplier and merge churn. |
| Wave 6 UI P1/P2 non-blocker polish (Majors F-18..F-29, Minors F-32..F-46) | Deferring non-blocker polish keeps `UI-P0` bounded inside one context window so `SWP-BUILD` and `SET-BUILD` aren't delayed; `F-15`, `F-17`, `F-30`, and `F-31` are pulled into `UI-P0` because they directly share files with Blockers `F-04`, `F-06`, `F-08`, `F-16`. |

## Cost justification (GO6 — 15× multiplier)

Parallelism multiplies token spend; every concurrent lane clears GO6 on a concrete non-speed basis:
- **`A5-WSL` alongside `UI-P0` + `KRN-SPK` (Wave A, width 3):** **long-running machine time + isolation** (`A5-WSL` runs
  3.24 GiB apt install + PETSc/CUDA C++ compilation + OpenFOAM solves inside an isolated WSL2 distro with zero repo code
  overlap); **genuine independence + context hygiene** (`KRN-SPK` is a standalone spike under `tools/spikes/` with zero
  edits to `src/` or `tests/`; `UI-P0` is the sole C#/Desktop-test track).
- **`SWP-BUILD` (Wave B) and `SET-BUILD` (Wave C) pipelined serially after `UI-P0`:** **isolation & context hygiene**
  (each feature spans Core + Analysis + CLI + Desktop + 15–18 red-first tests, so each earns its own fresh share workspace
  and 400k context budget without colliding on shared C# files or Desktop harness locks, while `A5-WSL` overlaps freely).
- **`w6-owner` & `w6-verifier` (`workspace: "inherit"`):** **governance separation** (no author clears its own hard veto
  or rules on its own decision request).

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect hard veto + Simplifier + Tech Lead); fold C1–C8 | 1 subagent pass | Clears Test Architect hard veto before S0 commit |
| 2 | Render `docs/coordination/wave6-and-area5-wsl.html`, run `docs-graph.py derive`, `check-docs.py`, `verify-ruling-citations.py`, `check-proof-pii.py`, commit plan + audit entry on `main` | 1 commit | S0: clean primary checkout required before any join |
| 3 | Update swarm title (`wave6-and-area5-wsl: A5-WSL + UI-P0 + KRN-SPK + SWP + SET`), write Swarm Overview `plan.json`, add `w6-owner` and `w6-verifier` (`inherit`), and launch `W6-0` (`DR-SWP-1..6`, `DR-SET-1..5`, `DR-KRN-1,3,4,6`) in parallel with **Wave A** unblocked tracks `w6-wsl-build` (`A5-WSL`) and `w6-ui-hardening` (`UI-P0`) | width 2 + governance | `A5-WSL` and `UI-P0` have zero open DRs; `W6-0` resolves `DR-KRN-1,3,4,6` in ~5 min |
| 4 | Commit `W6-0` rulings (next sequential ruling numbers allocated by `coord-decide.py rule next`) on `main` as soon as `w6-owner` posts them (when primary is clean), then immediately add `w6-cad-spike` (`KRN-SPK`) | 1 commit + width 3 | Strictly honors CO-S0 before `KRN-SPK` dispatch and unblocks Wave B/C |
| 5 | Route `UI-P0` and `KRN-SPK` completions to `w6-verifier` → repair if needed (cap 2) → join **at the Wave A boundary** (after `UI-P0` finishes its test runs so `tools/join-ring.sh` never races `Desktop.Tests`: join `UI-P0`, then `KRN-SPK`, and `A5-WSL` too if ready) via `conductor-join.py` (`--no-push`) → `swarm kill` landed workers | 2–3 serial joins | Eliminates concurrent Desktop harness risk (C3) and cleanly unblocks Wave B `SWP-BUILD` |
| 6 | Dispatch **Wave B**: `w6-sweeps-build` (`SWP-BUILD`, branched from updated `main` carrying `W6-0` + `UI-P0` + `KRN-SPK`) | width 1 (+ `A5-WSL` if still running) | Sole C#/Desktop track; zero merge conflicts |
| 7 | Route `SWP-BUILD` to `w6-verifier` → repair if needed → join at the Wave B boundary (`SWP-BUILD`, plus `A5-WSL` if completed while `SWP-BUILD` ran) via `conductor-join.py` (`--no-push`) → `swarm kill` landed workers | 1–2 serial joins | Unblocks Wave C `SET-BUILD` with zero display-harness contention |
| 8 | Dispatch **Wave C**: `w6-setup-build` (`SET-BUILD`, branched from updated `main` carrying `SWP-BUILD`) | width 1 | Sole C#/Desktop track; builds cleanly on generalized `Envelope` & `GoalStateSweepSnapshot` seam |
| 9 | Route `SET-BUILD` (and `A5-WSL` if still finishing) to `w6-verifier` → repair if needed → join via `conductor-join.py` (`--no-push`) → `swarm kill` landed workers | 1–2 serial joins | Full convergence on `main` |
| 10 | Post `[done]` on swarm board, append Planned vs Actual to this plan, run `docs-graph.py derive` + `check-docs.py` + `run-verify-gates.py`, append closing `execute-with-coordination` audit entry, commit on `main`, and report full results to operator | 1 close commit | Clean Wave 6 + Area 5 WSL closure |

## Gate record

`GATE prepare-for-coordination · 2026-11-21 · Orchestrator (author, Peer) · Independent Adversary Wave 6 Plan Reviewer (read-only research subagent 10f52232, Test Architect hard veto + Simplifier + Tech Lead lenses) · verdict PASS-WITH-CONDITIONS, CLEARS-THE-VETO yes · all eight conditions (C1–C8) folded verbatim before S0 commit: C1 removed undefined future ruling number citation; C2 replaced placeholder user path and clarified PROOF-PII scope; C3 added join-time Desktop-harness serialization rule for tools/ merges (A5-WSL and KRN-SPK); C4 added mandatory --out on wsl-preflight.sh, pre/post Ubuntu-24.04 checks, SHA256SUMS + gpgv, pinned PETSc SHA-256, nvcc/configure checks, and OpenFOAM Host banner scrubbing; C5 gated KRN-SPK dispatch on W6-0 DR-KRN-1,3,4,6, added binary/slnx/Python-gate rules and planted negative control; C6 clarified UI-P0 does not edit DESIGN.md and noted remaining product a11y veto predicates (F-23 + live AT traces); C7 added SanityBounds.json/RunRecord.cs/OperatingPoint.cs owned paths, defined GoalStateSweepSnapshot Core seam, and required sequential per-node green commits with ~300k warm fork for SWP-BUILD/SET-BUILD; C8 added remaining GO14a scan guards and exact harness capability vocabulary. Simplifier: CLEARED. Tech Lead casting vote: APPROVE 5 tracks (Wave A width 3, Wave B width 1 + ongoing A5-WSL, Wave C width 1).`

## Planned vs actual (record by `agy-w6-coord`, 2026-11-21; final at SET-BUILD join `20344c64`)

**Outcome.** All five planned tracks (`UI-P0`, `KRN-SPK`, `A5-WSL`, `SWP-BUILD`, `SET-BUILD`) plus Serial Spine `W6-0`
(**Rulings 270, 271, 272**) completed, cleared independent Adversary review (`w6-verifier`) on **first pass with zero
repair cycles**, and merged cleanly onto `main` via `conductor-join.py` (`--no-push`). Integrated suite on `main` at
`20344c64`: **`TESTS-GREEN 373 s`** (`CfdWorkbench.Core.Tests` 973 passed / 7 skipped / 0 failed;
`CfdWorkbench.Analysis.Tests` 326 / 1 / 0; `CfdWorkbench.Desktop.Tests` 936 / 1 / 0; `CfdWorkbench.Cli.Tests` 38 / 1 / 0;
Python unittest 10 / 0), `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings (38 events, 4 allowed),
`tools/check-docs.py` exit 0, and **10/10 `run-verify-gates.py` PASS** (`verify-ruling-citations.py` 272/272 OK,
`verify-no-machine-paths.py` clean). Zero model failovers; zero merge conflicts; zero Desktop harness timeouts; all
share workspaces cleanly torn down (`git worktree list` = primary only).

### 1. Per track

| track | swarm worker & model | planned budget | wall span (UTC) | repair cycles (cap 2) | Adversary verdict & result | join commit |
|---|---|---|---|---|---|---|
| **W6-0** (Spine S1) | `w6-owner` (`inherit`) | 30 calls · 15 min | 03:03Z–03:08Z (5 min) | 0 | **Rulings 270, 271, 272** (`DR-KRN-1,3,4,6`, `DR-SWP-1..6`, `DR-SET-1..5`; staged `Exact` allowlist; coordinated `OperatingPoint.Load`; `DR-KRN-2` kept open for operator) | `c308984f` |
| **KRN-SPK** (Wave A) | `w6-cad-spike` (`share`) | 90 calls · 250k · 1.5 h | 03:09Z–03:27Z (18 min) | 0 | `PASS` (`#53`): `Rhino3dm 8.35.0` MIT `win-arm64`/`win-x64`/`osx-arm64` native ABI verified; pure-C# Option B STEP AP203 **≤ 1.6139 µm** max deviation across 5 wings (6.2× below 10 µm floor), 20/20 deterministic STEP exports, 3/3 red-first negative controls RED; `BRepCheck_Analyzer` honestly `Flagged / deferred to SPIKE-CAD-02` | `35ac59a1` |
| **A5-WSL** (Wave A) | `w6-wsl-build` (`share`) | 120 calls · 300k · 2.5 h | 03:03Z–03:54Z (51 min) | 0 | `PASS` (`#57`, scoped to Option B ruling `#50`): dedicated `cfdw-openfoam2512-arm64` distro at `D:\wsl\cfdw-openfoam2512-arm64` (`Ubuntu-24.04` untouched `Stopped 2`); CUDA 13.2 + GCC-13/OpenMPI/BLAS/LAPACK installed (0 removals, 0 Linux GPU driver); PETSc 3.26.0 `--with-cuda=1 --with-cuda-arch=121` Blackwell **4/4 `make check` PASS** + 1M-DOF CG+Jacobi GPU smoke **2.98 s GPU vs 37.27 s CPU** (100% GPU flops, exact norm/iter parity); `petsc4Foam` (`cb1badc4...`) staged; verified `net.proxy-tls` receipt (`dl.openfoam.com -> sourceforge.net` intercepted by Cisco Umbrella, zero TLS bypass); idempotent `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` ready for unfiltered network | `90b4af4f` |
| **UI-P0** (Wave A) | `w6-ui-hardening` (`share`) | 180 calls · 380k · 2.5 h | 03:03Z–04:05Z (62 min) | 0 | `PASS` (`#59`): all **15 Blockers** (`F-01..F-14`, `F-16`) + `F-15`, `F-17`, `F-30`, `F-31` fixed red-first; 17 new Desktop tests + 1 Analysis Ncrit test; `--theme-matrix` 302 frozen + 36 extended rows PASS (Light ≥ 5.34:1, Dark ≥ 6.27:1, HC ≥ 17.62:1); dead `SectionEditor`/`LegacyEditor` deleted; `TESTS-GREEN 371 s` | `e2c1a802` |
| **SWP-BUILD** (Wave B) | `w6-sweeps-build` (`share`) | 400 calls · 900k · 3 h | 04:33Z–05:48Z (75 min) | 0 | `PASS` (`#70`, 5 sequential commits `SWP-1..5`): Core `SweepRecord` + `GoalStateSweepSnapshot` seam + retention/Archive + `swp-old-build` proof; Analysis `SweepSchedule`/`SweepService`/`SweepProjection`/`Cm`/`OperatingPoint.Load`; CLI `sweep preview`/`run`/`inspect --sweeps` bitwise `runKey` parity; Desktop `Sweeps` workspace (`Experiment setup`/`Run (local)`/`Results`, 0 Run button in setup, non-bridged gaps, linked table twins, `LAB-01` 0 hits); `DESIGN.md` `COPY-706..727`; `T1`–`T18` PASS; `TESTS-GREEN 379 s` | `53b6b985` |
| **SET-BUILD** (Wave C) | `w6-setup-build` (`share`) | 400 calls · 900k · 3 h | 06:11Z–07:06Z (55 min) | 0 | `PASS` (`#77`, 4 sequential commits `SET-1..4`): Core `SetupRecords`/`DisciplinePresets`/exact 2D `RecipeSeeder`/`ClassRules`/`SanityBounds` (BLAKE3 pinned) + live `CurrentGoalSweepSnapshot()` Core seam (0 edits to `Desktop/Sweeps/**`); Analysis `GoalFeasibility` (`GOAL-02` 7-pt + `0.3 m` exact); CLI `seed` + `inspect --json` parity; Desktop `Setup` workspace (`Setup \| CAD \| Analysis \| Sweeps` navbar, all 6 hard states, `COPY-53` no-key `AI-02`, `LAB-01` 0 hits); `DESIGN.md` `COPY-728..749`; `T1`, `T2`, `T2b`, `T3`–`T14` PASS; `TESTS-GREEN 373 s` | `20344c64` |

*(Per-worker tool-call and token counts are Not recorded because AGY swarm status does not expose worker counters; wall
spans are Measured from board timestamps and audit entries.)*

### 2. Seams, scope rulings & findings

- **Network blocker handled cleanly (`A5-WSL`, `#49` → `#50`):** `dl.openfoam.com` 302-redirects to `sourceforge.net`,
  which the host network's Cisco Umbrella filter intercepted with an untrusted TLS root (`net.proxy-tls`). Rather than
  bypassing TLS or halting all progress, Option B completed CUDA 13.2 + GCC-13/OpenMPI/BLAS/LAPACK + PETSc 3.26.0
  `--with-cuda=1 --with-cuda-arch=121` Blackwell qualification + staged `petsc4Foam` + captured real Windows
  `net.proxy-tls` evidence (closing a `Flagged` row in `docs/design/guided-solver-setup.md` §8) + shipped
  `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` to finish OpenFOAM v2512 + `petsc4Foam` `Allwmake` on an
  unfiltered network.
- **Core seam (`SWP-BUILD` → `SET-BUILD`):** `GoalStateSweepSnapshot` + `AuthoringSession.CurrentGoalSweepSnapshot()`
  worked with zero friction — `SET-BUILD` wired live Goal states into Area 4 Sweeps purely inside `Core` without editing
  a single file under `src/CfdWorkbench.Desktop/Sweeps/**`, verified by `SetupViewTests` confirming non-null
  `Sweeps.GoalSnapshot` after Seed.
- **Which parallelism justifications paid:**
  - **Wave A (width 3: `A5-WSL` ∥ `UI-P0` ∥ post-`W6-0` `KRN-SPK`):** Paid heavily — all 3 finished inside ~62 min
    wall time with 0 file collisions, 0 Desktop harness timeouts, and 3/3 first-try Adversary `PASS` verdicts. Holding
    `KRN-SPK` and `A5-WSL` joins to the Wave A boundary (C3/S4) prevented `tools/join-ring.sh` from racing `UI-P0`'s
    `Desktop.Tests`.
  - **Wave B (`SWP-BUILD`) → Wave C (`SET-BUILD`) serial pipeline:** Paid cleanly — zero merge conflicts on
    `AuthoringSession.cs`, `RunRecord.cs`, `Labels.cs`, `Program.cs`, `ModelArea.axaml`, or `DESIGN.md`, and both large
    full-stack tracks completed inside single contexts with sequential per-node green commits (C7).
  - **Stall monitor (`monitor`):** Caught two quiet waits (`#65`/`#66` at `SWP-5` closeout and `#69` verifier handoff)
    and recovered both automatically with zero wasted work.
- **Non-blocking carry-forward polish notes logged by `w6-verifier`:**
  1. `PerformanceCurveChart.cs:31-35` (Sweeps) and `PlanformPreviewControl.cs:13-23` (Setup): add
     `ActualThemeVariantChanged += (_, _) => InvalidateVisual()` for instant mid-view live theme repaint (P1 UI polish).
  2. `KRN-SPK` Phase 1 production notes: map channel knot parameters `t_i -> eta_i = x_channel(t_i)` in production
     `CadExportExtractor`, and separate boundary vs interior edges on `OPEN_SHELL` in `StepVerifier`.
  3. Remaining product accessibility veto predicates from `docs/reviews/ui-as-built-workbench.md` §1: `F-23` (200% modal
     dialog scaling) and live Windows NVDA / macOS VoiceOver traces.

## Status

| | |
|---|---|
| **Completed** | All 5 tracks (`UI-P0`, `KRN-SPK`, `A5-WSL` Option B, `SWP-BUILD`, `SET-BUILD`) and Serial Spine `W6-0` (Rulings 270–272) cleared independent Adversary review on first pass and landed on `main` (`20344c64`); integrated suite `TESTS-GREEN 373 s` (2,283 passed / 0 failed), `xaml-token-lint` clean, `check-event-subscribers` 0 findings, `check-docs.py` exit 0, `10/10` verify gates PASS; all share worktrees torn down |
| **Remaining** | (1) On an unfiltered network where `sourceforge.net` has valid TLS, run `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` inside `cfdw-openfoam2512-arm64` to finish OpenFOAM v2512 + `petsc4Foam` `Allwmake` + M1 cavity/GPU pressure trial; (2) Operator review of Rulings 270–272, `DR-KRN-2` (two CAM systems for `SPIKE-CAD-02`), and proposed copy `COPY-686..749`; (3) Wave 7 candidates: UI P1 polish (incl. `F-23` 200% modals & live NVDA/VoiceOver traces), Area 5 C# Guided Solver Setup (`T1/T5`), and Area 7 STEP/3DM Phase 1 (`SPIKE-CAD-02`); (4) Push `main` to `origin/main` when operator confirms |
| **Best next action** | Review Wave 6 results on `main`, decide whether to push `main` to `origin`, and run `tools/wsl-build-openfoam-cuda.sh --mode openfoam-resume` off the Umbrella-filtered network to complete the final OpenFOAM + `petsc4Foam` step |
