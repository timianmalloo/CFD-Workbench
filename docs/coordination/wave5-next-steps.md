---
id: coordination-wave5-next-steps
title: "Coordination plan - wave 5: cleanup, compatible fit, sweeps and setup designs, and export kernel scoping"
type: plan
status: complete
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, windows, agy-swarm]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: design-compatible-fit, rel: implements }
  - { to: coordination-wave3-next-steps, rel: refines }
  - { to: adr-0010-one-placement-rule, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-12-19"
summary: >-
  Wave 5 executes every unblocked next-step suggestion on AGY native swarm at width cap 3: Wave A runs cleanup/headroom
  (CLN) in parallel with Area 4 Local Sweeps design (SWP) and Area 1 Setup design (SET); after CLN joins and Owner rules
  DR-DF-7/DR-DF-9, Wave B runs the full Compatible Fit build (CF, cfdw-cv/3) in parallel with Area 7 CAD export kernel
  architecture scoping (KRN). Area 5 solver build/WSL install and UI builds for unapproved mockups are explicitly struck.
---

# Coordination plan: wave 5 (AGY native swarm)

**Input.** Operator request of 2026-11-19: build an optimal coordination plan covering all suggested next steps
(cleanup & headroom, Compatible Fit `cfdw-cv/3`, Area 4 Local Sweeps, Area 1 Setup, Area 5 OpenFOAM/Guided Setup,
Area 7 STEP/3DM export) and execute it with `/execute-with-coordination --swarm` on Antigravity (AGY).

**Goal state.**
- **Goal:** Land the Wave 3/4 cleanup & headroom fixes (`CLN`), land the complete Compatible Fit (`cfdw-cv/3`, Ruling 73)
  implementation (`CF`), and produce reviewable `/design-slice` designs + interactive HTML mockups for Area 4 Local
  Sweeps (`SWP`), Area 1 Setup (`SET`), and an architecture decision brief for Area 7 STEP/3DM export kernel (`KRN`).
- **Done when:** Every dispatched track returns observed exit evidence, clears independent Adversary review (Test
  Architect + domain/UX/Simplifier lenses), converges onto `main` via `conductor-join.py` with `check-docs.py`,
  `run-tests.sh` (on code merges), and `run-verify-gates.py` green, and operator-gated items (Area 5 and Wave A mockups)
  are handed back cleanly as a concise decision packet.
- **Not in scope:** Area 5 Guided Solver Setup code build or host WSL OpenFOAM/PETSc-CUDA compilation (blocked on
  operator review of `docs/mockups/solver-setup.html`, `DR-SETUP-1..6`, and `DR-F3-1`); code builds for Area 4 Sweeps,
  Area 1 Setup, or Area 7 STEP/3DM (blocked on mockup/ADR approval); bulk `review-by` churn on archival docs; deleting
  remote git branches; pushing to `origin` without operator instruction.
- **Tier:** T2 · **Fan-out cap:** 3 concurrent authors (at most **1** code/Desktop-test author at a time to eliminate
  Wave 2's concurrent Desktop harness exit-124 timeouts).

**Seats.**
- **Coordinator:** Main AGY session (`agy-coord-w5`). Owns the plan, seams, join order, `conductor-join.py` merges, and
  ruling commits. Never authors track code.
- **Owner:** Independent swarm worker `w5-owner` (`workspace: "inherit"`, read-only on `src/`). Deliberates on every
  `coord decide request --to w5-owner` (starting with `CF-0`: `DR-DF-7` and `DR-DF-9`) and records numbered rulings into
  `docs/notes/rulings.md` (next heading: **Ruling 269**) via `coord-decide.py rule next`; Coordinator commits the
  ruling on `main` before any join. A requester never rules on its own request.
- **Adversary Verifier:** Independent swarm worker `w5-verifier` (`workspace: "inherit"`, read-only). Reviews every
  track's committed diff, red-first proofs, mutants, and spec/ADR conformance in Adversary Mode before any join.
- **Stall Monitor:** Dedicated swarm worker `monitor` (`workspace: "inherit"`, loads built-in `swarm_monitor` skill).

**Leader ref.** Released at **epoch 5** and **not pinned**: the Ruling 247 session and Waves 2–3 verified that pinning
broke joins while joins read the released epoch cleanly (`docs/coordination/wave3-next-steps.md:38-40`). Deviation from
S3 row 1, recorded here.

## Layer state

Measured 2026-11-19 22:45Z by `python docs/ai-forward-pack/scripts/coord-core.py doctor`,
`python docs/ai-forward-pack/scripts/coord-core.py session list`, `python docs/ai-forward-pack/scripts/pack-doctor.py`,
and `python tools/check-docs.py` in the primary checkout (`D:/Projects/CFD-Workbench`, HEAD `9b194656`, clean).

| check | result | meaning |
|---|---|---|
| registry | ok - 7 pattern(s) | `.agents/artifacts.yml` present; managed block (3) plus 4 repo entries |
| merge driver | effective - coord-regen, coord-register, defect-register declared and registered | derived and register files merge without human conflict resolution |
| leader | released (epoch 5) | not pinned (see Leader ref above) |
| heartbeat | 1 session(s) beating; newest beat 3306058 s ago; 0 stalled, 0 live, 0 blocked, 0 done | nothing live in flight |
| sessions | 0 active session(s); 243 file(s) scanned | clean start |
| requests | ok - 36 request(s), 0 open, 36 terminal | no unanswered decision request |
| lease overlap | none (0 live lease(s)) | clean start |
| worktrees | 1 (`D:/Projects/CFD-Workbench 9b194656 [main]`) | primary only |
| pack-doctor | 22 PASS · 0 FAIL · 2 WARN | WARN 1: Windows `python3` alias → use `python`; WARN 2: archival docs freshness (`check-docs.py` exit 0) |
| agy swarm workspace isolation | `enforced` (`workspace: "share"` per author; `"inherit"` for read-only verifiers/monitor) | each author gets an isolated linked working copy; never two authors on `"inherit"` |
| agy swarm board & `swarm_wait` | `enforced` inside the swarm session | zero-token reactive wakeup; `swarm_monitor` skill present for stall detection |
| agy ownership hooks (`.agents/hooks.json`) | `observed-only` (`.agents/rules/agy-surface.md:39-40`) | `pre-commit` floor (`coord-core.py precommit`) + coordinator diff inspection enforce path boundaries |

The coordination layer is installed **once, in the primary checkout** (`D:/Projects/CFD-Workbench`). Every share
workspace created by `swarm{action:"add", workspace:"share"}` **inherits** `.git/config` merge drivers and `.git/hooks`
from the primary checkout — workers run `coord doctor` (or verify `.git/hooks/pre-commit` and `git config --get-regexp
'merge\..*\.driver'`) to read the inherited state back, and **never** run `coord install` inside a share workspace.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl` | register | `coord-register` union merge | **no** |
| `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen`: `audit-log.py --root docs --project CFD-Workbench render` | **no** |
| `docs/docs-index.js` | derived | `coord-regen`: `docs-graph.py derive` | **no** |
| `.agents/log/*.jsonl`, `.agents/requests.jsonl` | register | `coord-register` union merge | **no** |
| `docs/lessons/defect-classes.md` | register | `defect-register` (`tools/merge-defect-register.py`) | **no** |
| `docs/notes/rulings.md` | authored, single writer | `w5-owner` deliberates; Coordinator commits on `main` before any join | serial (spine) |
| `DESIGN.md` copy rows | authored, range rule | High water today `COPY-680`. `CLN` reserves `COPY-681..685` (only if needed); `CF` reserves `COPY-686..705` (proposed; `COPY-C1..C7`). `CLN` joins before `CF` starts | serial (`CLN` → `CF`) |
| `src/CfdWorkbench.Analysis/Labels.cs` | authored, single owner per wave | Only `CF` (Wave B) appends constants if needed; `CLN` does not edit `Labels.cs` | single owner per wave |
| `src/CfdWorkbench.Core/AuthoringSession.cs`, `src/CfdWorkbench.Desktop/PropertiesView.cs`, `src/CfdWorkbench.Desktop/WorkbenchController.cs` | authored, serial across waves | `CLN` owns in Wave A (`CompareFields` caching; `PropertiesView.SourceName` and controller source-name callers); `CF` owns in Wave B (`Evaluator.IdFor`, `StationEdits`, `PatchSectionStep`, `ImportPatch`, `BlendMatch` chip/strip) after `CLN` joins | serial (`CLN` → `CF`) |
| all other track files | authored | strictly one owner per file (see Tracks) | owner |

**Joint satisfiability of shared surfaces (GO14a).**
- `DESIGN.md` & `Labels.cs`: existing one-definition scans (`Copy_*_MatchesDesignRows` and Labels-to-DESIGN row scans in
  Desktop/Analysis harnesses; root: repo `DESIGN.md` §7 copy table & `src/CfdWorkbench.Analysis/Labels.cs`; recursion:
  none; token set: `COPY-\d+`; allowlist: none). Because `CLN` joins before `CF` starts and each track uses disjoint ID
  ranges (`681..685` vs `686..705`), no concurrent writer exists and every existing row remains intact: jointly
  satisfiable.
- `Compatible_AuthoredMode_SingleCaller` & `Placement_SingleEntry_NoAuthoredBlendReader` (`CF` tests,
  `docs/design/compatible-fit.md` §8): root `src/CfdWorkbench.Core/` (and `src/` for authored-mode call scan);
  recursion: `*.cs` under `src/` excluding `bin/` and `obj/`; token set: authored-mode internal symbol and every public
  member of `Placement` reading profile ordinates (`Surface`, `Sections`, `Frame`, `StationEta`, `ChordGrid`,
  `OwnProfile`, `ProfileDifferenceMaximum`, `SameRecord`, `SameAbscissa`) plus `StationEdits.Blend`; allowlist:
  `Compatible.cs` residual only (for authored mode) and explicit documented authored-record predicates (`SameRecord`,
  `SameAbscissa`). No other Wave B track edits `src/`: jointly satisfiable.
- `Evaluator_IdFor_V3OnlyWhenSpacingDiffers` literal scan (`CF`): root `src/`; recursion: `*.cs` excluding `bin/`/`obj/`;
  token set: literal `"cfdw-cv/`; allowlist: `Evaluator` helper only. Disjoint from Wave B's docs-only `KRN`: jointly
  satisfiable.
- `docs-graph.py validate` & `check-docs.py` (all tracks): root `docs/`; recursion: all V2-frontmatter `.md` files;
  token set: unique frontmatter `id` values and valid link targets; allowlist: none. Tracks create strictly distinct
  artifact IDs (`proof-copy-615-post-0b277815`, `design-local-sweeps`, `mockup-local-sweeps`, `spec-amendment-local-sweeps`,
  `design-setup-seeding`, `mockup-setup-seeding`, `spec-amendment-setup-seeding`, `architecture-cad-export-kernel`,
  `adr-0013-cad-export-kernel`): jointly satisfiable.

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
- **Ruling 252** (`docs/notes/rulings.md:1911`):
  > "`Placement.cs` stays read-only" *(lifted for `CF` only by `CF-0` CG Adversary clearance + Owner ruling on `DR-DF-7` before `CF` dispatches).*
- **Ruling 256** (`docs/notes/rulings.md:1935`):
  > "This is the only exception to Ruling 252's read-only clause."
- **Ruling 263** (`docs/notes/rulings.md:1977`):
  > "`NativeProject.MaxBytes` becomes 32,000,000 bytes, and its budget stands unchanged (open within 3 s, save within 3 s,
  > peak working set within 1.5 GB at the cap)."
- **Ruling 267** (`docs/notes/rulings.md:2001`):
  > "the COPY-615 headline for a stored run from a build at or after 0b277815 whose serialized run record predates fields
  > added later (read with defaults) is Inferred, not Verified; the fixture that closes it must be produced by such a
  > build's own writer, committed with its receipt and pinned hash, and opened by a test with the plan's fail condition
  > (any run shown without its COPY-615 reason or with a wrong one)."

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **CLN** cleanup & headroom | `src/CfdWorkbench.Core/Provenance.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs` (`CompareFields` encode cache + invalidation only), `src/CfdWorkbench.Desktop/PropertiesView.cs` (section source-name caller only), `src/CfdWorkbench.Desktop/WorkbenchController.cs` (source-name caller only if needed), `tests/CfdWorkbench.Core.Tests/ProvenanceTests.cs`, `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs`, `tests/CfdWorkbench.Desktop.Tests/SourceTextTests.cs`, `docs/proof/copy-615-post-0b277815/` (new genuine post-`0b277815` fixture + receipt), `README.md`, `DESIGN.md` (`COPY-681..685` only if needed) | — | T1 | 1 | 80 calls · 250k · 1 h 30 min · ceiling 400k | Every change observed **red first**, then green, with mutants killed: (1) `.dat` (`dat:sha256:...`) and My-sections origins render designer-readable names across Browser, Properties title, and Section chip without breaking `gen:`/`vend:` lookups or lazy catalog reading (`ProvenanceTests`, `SourceTextTests`); (2) `AuthoringSession.CompareFields` avoids re-encoding an unchanged envelope on repeated compare reads while returning exact `SaveImage().Length` (verified by mutant returning stale size on mutation → red, and `CompareFieldsEncodeCost` measured well under 100 ms on warm reads); (3) Ruling 267 genuine fixture emitted by a real post-`0b277815` build (built in a scratch detached worktree, SHA-256 pinned in `docs/proof/copy-615-post-0b277815/README.md`) opened in `CompareTests.cs` asserting COPY-615 fail condition (fail = any run shown without its COPY-615 reason or with a wrong one); (4) `README.md` accurately reflects `main` (Waves 1–4 landed, Windows platform of record, pack rev 120, `CFDWorkbench.slnx`); `bash tools/run-tests.sh` green; `python tools/check-docs.py` green | AGY native swarm (`workspace: "share"`, enforced isolation; `observed-only` hooks; commit floor + verifier gate) |
| **SWP** Area 4 local sweeps design | `docs/design/local-sweeps.md`, `docs/design/local-sweeps.html`, `docs/mockups/local-sweeps.html`, `docs/mockups/local-sweeps.md`, `docs/specs/amendments/spec-local-sweeps.md` | — | T2 | 1 | 65 calls · 220k · 1 h 30 min · ceiling 400k | Full `/design-slice` for Area 4 Local-Tier Sweeps & Performance Curves (`XS-01`, `XS-03`, `ANA-16`, `RES-04` local over `local · in-process` VLM+strip): DM1–DM18 data model (Experiment / Case schedule / Sweep attempt grain, append-only facts, run-key linkage, retention impact against ADR-0011 32 MB cap), E7 surface list, hard states (empty, invalid sample blocked, running/cancel, failed sample gap never interpolated, Historical sweep, depth-unset labels, dark/light & reduced-motion), self-contained dependency-free interactive HTML mockup (`local-sweeps.html`) with review harness, proposed copy rows (not editing `DESIGN.md`), explicit red-first test plan & mutants, and numbered `DR-SWP-*` requests; passes `check-docs.py` and Adversary review (Hydrodynamicist + UX & Accessibility + Data & Persistence + Test Architect) | AGY native swarm (`workspace: "share"`, docs-only) |
| **SET** Area 1 setup & goal-state design | `docs/design/setup-seeding.md`, `docs/design/setup-seeding.html`, `docs/mockups/setup-seeding.html`, `docs/mockups/setup-seeding.md`, `docs/specs/amendments/spec-setup-seeding.md` | — | T2 | 1 | 65 calls · 220k · 1 h 30 min · ceiling 400k | Full `/design-slice` for Area 1 Setup & Goal State (`SET-01`, `SET-03`, `SET-04`, `GEO-01`, `GEO-04`, `CAT-03`, `CAT-04`, offline parameter/preset seeding with AI-02 language slot disabled/ready-for-key): DM1–DM18 data model (Setup brief + Goal state append-only versions, soft-target weighted solver, discipline & class-rule presets, Flagged-field acknowledgement, re-seed history rule without rewriting revisions), E7 surface list, hard states (conflicting soft targets with weights, Flagged preset acknowledgement, GWA/iQFOiL class-rule DRC, re-seed on edited doc, no-key language box, dark/light & reduced-motion), self-contained interactive HTML mockup (`setup-seeding.html`) with review harness, proposed copy rows (not editing `DESIGN.md`), red-first test plan & mutants, and `DR-SET-*` requests; passes `check-docs.py` and Adversary review (Marine-CAD UX + Hydrodynamicist + Data & Persistence + Test Architect) | AGY native swarm (`workspace: "share"`, docs-only) |
| **CF** Compatible Fit (`cfdw-cv/3`) | `src/CfdWorkbench.Core/Compatible.cs` (new), `src/CfdWorkbench.Core/Geometry.cs` (`AssessWithin` entry), `src/CfdWorkbench.Core/Placement.cs` (one preparation entry + internal authored mode), `src/CfdWorkbench.Core/AuthoringSession.cs` (`Evaluator.IdFor` 20 sites, `StationEdits` R3, `PatchSectionStep`, `ImportPatch`), `src/CfdWorkbench.Core/FoilSource.cs` (1 literal), `src/CfdWorkbench.Core/SectionEdits.cs`, `src/CfdWorkbench.Core/Contracts.cs`, `src/CfdWorkbench.Analysis/Labels.cs` (CF region), `src/CfdWorkbench.Desktop/Shell/StatusStrip.axaml.cs`, `src/CfdWorkbench.Desktop/PropertiesView.cs`, `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `src/CfdWorkbench.Desktop/RebuildPopover.axaml.cs`, `src/CfdWorkbench.Cli/Program.cs` (verify `evaluator` output), `tests/CfdWorkbench.Core.Tests/CompatibleTests.cs` (new), `tests/CfdWorkbench.Core.Tests/IdentityTests.cs` (suite registration + `--readiness` test 22), `tests/CfdWorkbench.Core.Tests/SectionEditTests.cs` (update Ruling 71 stopgap assertion), `tests/CfdWorkbench.Core.Tests/PlacementTests.cs`, `tests/CfdWorkbench.Core.Tests/SectionDraftTests.cs`, `tests/CfdWorkbench.Core.Tests/ReopenSectionDraftTests.cs`, `tests/CfdWorkbench.Core.Tests/StationTests.cs`, `tests/CfdWorkbench.Core.Tests/SectionEditsTests.cs`, `tests/CfdWorkbench.Desktop.Tests/SectionEditorTests.cs`, `tests/CfdWorkbench.Cli.Tests/CliTests.cs` (pin `inspect --json` evaluator id), `docs/adr/0010-one-placement-rule.md` (Amendment 2), `docs/adr/0005-point-types-in-the-b-spline-record.md` (Amendment 1), `docs/proof/xpa-old-build/`, `DESIGN.md` (`COPY-686..705` proposed) *(Boundary: `SectionReplace.cs` stays untouched; `SectionReplaceTests.cs` and `ControllerSectionTests.cs` `CAT-SPACING` pins remain intact)* | `CLN` joined; `CF-0` (`w5-verifier` CG clearance on `compatible-fit.md` §3–§6 & §12 Q1–Q2 → `Ruling 269`) committed on `main` | T2 | 1 (internal serial stages CF-1..CF-6; split via warm fork at 350k tokens if needed) | 220 calls/segment (440 total) · 1.2M total · 4 h · ceiling 400k per context | All `docs/design/compatible-fit.md` §8 & `cross-profile-abscissa.md` §10 tests observed **red first**, then green, with named mutants killed: `Compatible_AuthoredMode_SingleCaller` (C1), `Placement_SingleEntry_NoAuthoredBlendReader` (C2), `Remove_CopyChanges_MeasuresTouchedIntervals` (C3), `Promote_SharedSpacingInMixedDoc_KeepsLSpacing` (C4), `Compatible_SurfaceResidual_SameMeasureAsStations`, `Evaluator_IdFor_V3OnlyWhenSpacingDiffers` (zero stray `"cfdw-cv/` literals), `Evaluator_OldRowsNeverRewritten`, `Evaluator_V2RowWithMixedSpacing_Refused`, `Evaluator_RemoveBackToOneSpacing_V2Again`, `Promote_R3_OnCopies_RegimeMeasured`, equal-spacing golden masters bit-identical, UI `COPY-C1..C7` rendered (`SectionEditorTests`), CLI `inspect --json` evaluator pinned (`CliTests`), ADR-0010 Amd 2 & ADR-0005 Amd 1 updated, old-build `DOC-VERSION` receipt recorded, `bash tools/run-tests.sh` green, `python tools/check-docs.py` green, Adversary pass (Computational Geometry + Data & Persistence + Test Architect) | AGY native swarm (`workspace: "share"`, enforced isolation) |
| **KRN** Area 7 export kernel architecture | `docs/architecture/cad-export-kernel.md`, `docs/architecture/cad-export-kernel.html`, `docs/adr/0013-cad-export-kernel.md` (proposed) | — (scheduled in Wave B beside `CF`) | T2 | 1 | 55 calls · 180k · 1 h · ceiling 400k | Evidence-based `/define-architecture` brief & proposed ADR-0013 selecting candidate NURBS/STEP AP203/3DM export paths compatible with COMMIT-02 (MIT/BSD/Apache linked or LGPL/GPL process-isolated), Windows ARM64 + x64 + macOS ARM64 ABI, building on `docs/notes/kernel-spike-occt-loft.md` and `docs/design/export.md` (no code changes): evaluates OCCT process-isolated worker vs `rhino3dm` (MIT) vs pure-C# STEP writer, defines the mandatory Spike Protocol contract and two-CAM + Fusion open-and-measure acceptance fixture (`EXP-02`), includes Threat Model / License Register entries and numbered `DR-KRN-*` decisions; passes `check-docs.py` and Adversary review (Enterprise Architect + Computational Geometry + Security) | AGY native swarm (`workspace: "share"`, docs-only) |

### Swarm topology (AGY)

| track | swarm name | workspace | channel | model |
|---|---|---|---|---|
| Stall monitor | `monitor` | `inherit` | `main` | inherit |
| Owner seat | `w5-owner` | `inherit` | `main` | inherit |
| Adversary verifier | `w5-verifier` | `inherit` | `main` | inherit |
| `CLN` (Wave A) | `w5-cleanup` | `share` | `main` | inherit |
| `SWP` (Wave A) | `w5-sweeps-design` | `share` | `main` | inherit |
| `SET` (Wave A) | `w5-setup-design` | `share` | `main` | inherit |
| `CF` (Wave B) | `w5-compatible-fit` | `share` | `main` | inherit |
| `KRN` (Wave B) | `w5-kernel-arch` | `share` | `main` | inherit |

*(5 tracks total — single `main` channel keeps coordination flat and visible without extra channel overhead; all workers inherit the user's selected model unless quota failover triggers `swarm{action:"model"}`.)*

### Track contracts (Owner, deadlines, fallbacks, termination)
- **Who rules:** `w5-owner` rules into `docs/notes/rulings.md` via `coord-decide.py rule next --request <id>` (Coordinator commits the ruling on `main` before any join).
- **Doorbells:** AGY native swarm board + `swarm_wait` (`enforced` inside this swarm session); pack CLI `coord-mail.py` doorbells `not recorded`.
- **Deadline / Fallback:**
  - `CLN`: 90 min wall / fallback: land verified completed items (e.g., `Provenance` + `CompareFields` + `README`) only if a third item blocks on an unbuildable historical commit, with an Owner ruling recording the narrowed scope.
  - `SWP`, `SET`, `KRN`: 90 min wall / fallback: emit completed design + open `DR-*` list; never ship an unreviewed mockup as approved.
  - `CF`: 4 h wall (up to 2 context segments via warm `fork` if context approaches 350k tokens) / fallback: if `CF-1..CF-6` cannot complete cleanly inside cap, keep branch unmerged with exact node checkpoint — **never** land `CF-1` alone without callers (TL1 / HYG-A).
- **Termination condition:** Track ends when all listed exit evidence is observed by `w5-verifier` with PASS (or PASS-WITH-CONDITIONS resolved) and joined via `conductor-join.py`, or when the 2-repair-cycle / budget cap fires (reported as a defect signal to Coordinator, never ignored).

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| S0: Commit this plan (`.md`, `.html`, `docs-index.js`, logged prompt) on `main` before dispatching or joining | `conductor-join.py` refuses a dirty primary (`COORD-JOIN-DIRTY-PRIMARY`), and every worker brief cites this plan | Coordinator (`agy-coord-w5`) |
| S1 (`CF-0`): `w5-verifier` (Computational Geometry lens) reviews and clears `docs/design/compatible-fit.md` §3–§6 & §12 Q1–Q2, **then** `w5-owner` rules `DR-DF-7` (lift Ruling 252/256 read-only clause on `Placement.cs` for single-entry compatible preparation with C1–C2 tests) and `DR-DF-9` (refuse `cfdw-cv/2` row needing CF with `DOC-VERSION`) → **Ruling 269** committed on `main` | `docs/design/compatible-fit.md:215-216` requires both CG Adversary clearance on §3–§6 and `DR-DF-7` ruled before any CF build node dispatches (`GO5(b)` decision edge) | `w5-verifier` (CG lens) clears → `w5-owner` rules → Coordinator commits |
| S2: `CLN` joins to `main` before `CF` dispatches | Both touch `AuthoringSession.cs`, `PropertiesView.cs`, and `WorkbenchController.cs` (`GO5` shared-file edge) | Coordinator |
| S3: Joins run one at a time via `conductor-join.py`, with at most **1** code/Desktop-test track active | Wave 2 verified Desktop harness exit-124 timeouts under concurrent test suites; `conductor-join.py` is serial on `main` | Coordinator |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| `CLN` -> `CF` | `AuthoringSession.cs` (`CompareFields` caching), `PropertiesView.cs` (`SourceName`), and `WorkbenchController.cs` land on `main` before `CF` forks its share workspace | Hard wave boundary: `CLN` joins in Wave A, `CF` starts in Wave B |
| `CF-0` -> `CF` | `w5-verifier` CG clearance on `compatible-fit.md` §3–§6 & §12 Q1–Q2 followed by Ruling 269 authorizing `DR-DF-7` (a) and `DR-DF-9` (a) | `w5-verifier` reviews first in Wave A → `w5-owner` rules → Coordinator commits Ruling 269 on `main` before Wave B |
| `SWP` <-> `SET` | Goal state operating-point list (Area 1 `SET` writes Goal state; Area 4 `SWP` reads Goal state operating points as an optional sweep source per `XS-01` / A5.9) | Both cite canonical `docs/specs/cfd-workbench-v1.md` A3.1 `Goal state` grain; if a field name needs aligning, post `@w5-setup-design seam: ...` on the board |
| any track -> `w5-owner` | Unexpected spec ambiguity, copy supersession, or scope/cap exception | `coord decide request --to w5-owner` + `@w5-owner decision-request: ...` |

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Area 5 · Guided Solver Setup build (`SETUP-01..09`) | Already designed + mocked up (`docs/design/guided-solver-setup.md`, `docs/mockups/solver-setup.html`), explicitly gated on operator approval of the mockup and `DR-SETUP-1..6`. Building before approval violates the repo's UI gate; writing another doc would be pure waste. Surfaced in the close decision packet instead. |
| Area 5 · Host WSL OpenFOAM / PETSc-CUDA build & benchmark | Preflight already PASSED (`docs/proof/wsl-preflight/README.md`). Next step modifies host WSL state (3.24 GiB install + multi-hour compilation) and requires operator build ruling + `DR-F3-1` (determinant floor). Struck from autonomous swarm; surfaced in decision packet. |
| Area 4 Sweeps code build & Area 1 Setup code build | Blocked on operator visual review of the mockups `SWP` and `SET` produce in Wave A (standing repo rule). |
| Area 7 STEP/3DM export code build | Blocked on unselected CAD/NURBS kernel (`0003-application-stack.md`) and two-CAM open-and-measure fixture (`EXP-02`). `KRN` produces the architecture decision brief first. |
| Splitting `CLN` into 4 micro-tracks (Provenance, CompareFields, COPY-615, README) | Fails GO6 (15× multiplier) and shares `AuthoringSession.cs` / `CompareTests.cs`. One coherent T1 track is faster, cheaper, and conflict-free. |
| Splitting `CF` into 6 concurrent worktrees (`CF-1..CF-6`) | `CF-1..CF-6` have a strict serial dependency chain (`CF-1 → CF-2 → CF-3/4/5`) and share `AuthoringSession.cs` / `Placement.cs`; `CF-1` alone is rejected dead code (TL1 / HYG-A). One share workspace progressing sequentially through CF-1..CF-6 (with a warm fork only if approaching the 400k context ceiling) avoids dead code and merge churn. |
| Bulk `review-by` refresh on 442 archival freshness findings | Archival proof/plan documents; `check-docs.py` gates freshness as `warn`, not failure. Churning 100+ historical files adds zero product value. |

## Cost justification (GO6 — 15× multiplier)

Parallelism multiplies token cost (~10–15× orchestrator-worker shape). Each retained track clears the bar on a specific
non-speed justification:
- **`CLN` + `SWP` + `SET` (Wave A, width 3):** **genuine independence** (100% disjoint authored file families: Core/Desktop
  code vs Area 4 docs/mockup vs Area 1 docs/mockup) + **context hygiene** (each `/design-slice` + interactive HTML mockup
  holds full spec/UX/UI context without polluting the C# cleanup context) + **zero Desktop test contention** (only `CLN`
  runs `dotnet test`).
- **`CF` + `KRN` (Wave B, width 2):** **isolation & context hygiene** (`CF` is a large 6-node geometry-core change that
  must stay isolated in its own share workspace away from `main`; `KRN` is a pure docs architecture study with zero code
  or test overlap).
- **`w5-owner` & `w5-verifier` (`workspace: "inherit"`):** **governance separation** (the author never clears its own hard
  veto or rules on its own decision request).

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Independent Adversary review of this plan (Test Architect + Simplifier + Tech Lead); fold any blockers | 1 subagent pass | Clears Test Architect hard veto before committing or dispatching |
| 2 | Render `docs/coordination/wave5-next-steps.html`, run `docs-graph.py derive`, `check-docs.py`, commit plan + logged prompt on `main` | 1 commit | S0: clean primary checkout required before any join |
| 3 | Initialize AGY swarm (`title`, add `monitor`, `w5-owner`, `w5-verifier`) and dispatch **Wave A** at width 3: `w5-cleanup` (`CLN`), `w5-sweeps-design` (`SWP`), `w5-setup-design` (`SET`); in parallel, `w5-verifier` runs the `CF-0` Computational Geometry Adversary review of `compatible-fit.md` §3–§6 & §12 Q1–Q2, then hands off to `w5-owner` for `DR-DF-7` & `DR-DF-9` | width 3 + governance | Maximum genuine independence; `CF-0` CG clearance + ruling complete during Wave A so Wave B starts with zero wait |
| 4 | Commit **Ruling 269** (`CF-0`) on `main` once `w5-verifier` clears CG and `w5-owner` rules (only when primary is clean and no join is mid-flight) | 1 commit | Unblocks `CF` (`Placement.cs` read-only lift) |
| 5 | As each Wave A track reports `[done]`, route to `w5-verifier` for Adversary review → repair if needed → join one at a time via `conductor-join.py` (`CLN` first, then `SWP`, `SET`), then `swarm kill` landed workers | 3 serial joins | `CLN` join unlocks Wave B `CF` |
| 6 | Dispatch **Wave B** at width 2: `w5-compatible-fit` (`CF`, branched from updated `main` carrying `CLN` + Ruling 269) and `w5-kernel-arch` (`KRN`; per Simplifier advisory, drop/defer `KRN` first if Wave B budget tightens) | width 2 | `CF` runs as sole code/test track; `KRN` runs docs-only beside it |
| 7 | Route Wave B completions to `w5-verifier` → repair if needed → join `KRN` and `CF` via `conductor-join.py` → `swarm kill` landed workers | 2 serial joins | Full convergence on `main` |
| 8 | Post `[done]` on swarm board, append Planned vs Actual to this plan, run `docs-graph.py derive`, append closing `execute-with-coordination` audit entry, and deliver final report + Operator Decision Packet (`SWP`/`SET` mockups, `KRN` ADR-0013, Area 5 `DR-SETUP-1..6`/`DR-F3-1`/WSL build, and 5 old remote branches) | 1 close commit | Completes Wave 5 cleanly |

## Gate record

`GATE prepare-for-coordination · 2026-11-19 · Orchestrator (author, Peer) · Independent Adversary Plan Reviewer (read-only research subagent 1682a3bb, Test Architect + Simplifier + Tech Lead lenses) · verdict PASS-WITH-CONDITIONS, CLEARS-THE-VETO yes · four conditions folded verbatim before S0 commit: C1 added six missing CF test files (IdentityTests.cs, SectionEditTests.cs, PlacementTests.cs, SectionDraftTests.cs, ReopenSectionDraftTests.cs, CliTests.cs) and stated SectionReplace.cs stays untouched; C2 added w5-verifier Computational Geometry Adversary clearance of compatible-fit.md §3–§6 & §12 Q1–Q2 ahead of w5-owner Ruling 269 in CF-0; C3 added DC-189 verbatim quote of Ruling 256; C4 added WorkbenchController.cs to Artifact classes and Seams. Simplifier: no strike required (advisory: defer KRN first if Wave B budget tightens). Tech Lead casting vote: 5 tracks across 2 waves (Wave A width 3, Wave B width 2; at most 1 code/Desktop-test track active per wave).`

## Planned vs actual (record by `agy-coord-w5`, 2026-11-19; final at CF join `ee1942a7`)

**Outcome.** All five planned tracks (`SET`, `SWP`, `CLN`, `KRN`, `CF`) and Serial Spine `CF-0` (CG Adversary clearance + **Ruling 269**) completed, cleared independent Adversary review (`w5-verifier`), and merged cleanly onto `main` via `conductor-join.py` (`--no-push`). Integrated ring on `main` at `ee1942a7`: **`TESTS-GREEN 357 s`** (Core 952 passed / 7 skipped / 0 failed; Analysis 313 / 1 / 0; Desktop 914 / 1 / 0; Cli 35 / 1 / 0; Python unittest 10 ran), `check-docs.py` exit 0 (`631` artifacts, `0` problems, `0` PII), `xaml-token-lint.py` clean, `check-event-subscribers.py` 0 findings, and **10/10 `run-verify-gates.py` PASS** (`verify-ruling-citations.py` 269/269). Zero model failovers; zero merge conflicts; all share workspaces cleanly removed after join (`git worktree list` = primary only).

### 1. Per track

| track | swarm worker & model | planned budget | wall span (UTC) | repair cycles (cap 2) | Adversary verdict & result | join commit |
|---|---|---|---|---|---|---|
| **CF-0** (Spine S1) | `w5-verifier` (CG) → `w5-owner` (`Gemini 4 Neon (High)`) | 40 + 25 calls | 22:58Z–23:03Z (5 min) | 0 | CG `PASS-WITH-CONDITIONS` (`#7`, adding BC-1..BC-5) → **Ruling 269** (`req-01M7V0S8KD6MMJGRFYV4AD33BJ`, DR-DF-7(a) + DR-DF-9(a)) | `8b79fd63` |
| **SET** (Wave A) | `w5-setup-design` (`share`) | 65 calls · 220k · 1 h 30 | 22:59Z–23:25Z (26 min) | 1 (#13 → #14: exact 4-CV cubic `κ(λ) = (3+λ)/4`, complete stationary-point proof + 64-step bisection, `NativeProject.Read` note) | `PASS` (`#15`): `setup-seeding.{md,html}`, `setup-seeding.html` mockup (6 hard states, 34/34 hex tokens, 16/16 WCAG AA, 0 `LAB-01` hits), `spec-setup-seeding.md` (`AM-SET-S1..S4`) | joined before `4a3c67d5` (`al-01M7V24SV3HR9AVY8BWWZ4R4AY`) |
| **SWP** (Wave A) | `w5-sweeps-design` (`share`) | 65 calls · 220k · 1 h 30 | 22:59Z–23:34Z (35 min) | 1 (#20 → #21: `fixedVlmBasis` shallow `h/c < 5` `"deep water"` guard + `SET`/`SWP` `Envelope` composition) | `PASS` (`#22`): `local-sweeps.{md,html}`, `local-sweeps.html` mockup (7 hard states, 32/32 hex tokens, 33/33 WCAG AA, non-bridged gaps, 0 `LAB-01` hits), `spec-local-sweeps.md` (`AM-SWP-1..3`) | `1bcedbf7` |
| **CLN** (Wave A) | `w5-cleanup` (`share`) | 80 calls · 250k · 1 h 30 | 22:59Z–23:35Z (36 min) | 0 | `PASS` (`#23`): designer-readable `.dat`/My-sections provenance across 6 surfaces; `CompareFields` cache dropping 32 MB median **58.8 ms → 3.4 ms** (17.3× speedup); genuine post-`0b277815` COPY-615 fixture (`docs/proof/copy-615-post-0b277815/`, Ruling 267); `README.md` refreshed; `TESTS-GREEN 259 s` | `df566d56` |
| **KRN** (Wave B) | `w5-kernel-arch` (`share`) | 55 calls · 180k · 1 h | 23:42Z–23:53Z (11 min) | 0 | `PASS-WITH-CONDITIONS` (`#28`, non-blocking Phase 0/1 CF-alignment & spike gate continuity): `cad-export-kernel.{md,html}`, proposed `ADR-0013` (Option A hybrid OCCT process-isolated + `rhino3dm` baseline with Option B pure-C# STEP bake-off in `SPIKE-CAD-01`, `EXP-02` `SPIKE-CAD-02` fixture, `DR-KRN-1..6`) | `67dc9410` |
| **CF** (Wave B) | `w5-compatible-fit` (`share`) | 440 calls · 1.2M · 4 h | 23:42Z–01:10Z (88 min) | 1 (#35 → #38: `PrepMemo`/`MeasureMemo` profile-tangent key fixing XPA-I6 stale cache + removed 4 uncalled/duplicated `Labels.cs` helpers & dead `mid6` locals) | `PASS` (`#39`, CG Return diff gate cleared): `Compatible.cs` + single `PrepareCompatible` entry in `Placement.cs` & `Geometry.AssessWithin` (enclosure `1.21e-17 m ≤ 1 nm`), `Evaluator.IdFor` 21-site sweep & `DR-DF-9(a)` `DOC-VERSION`, R3 Promote/Match/Remove (C3/C4), XPAU UI (`COPY-686..693` proposed), ADR-0010 Amd 2, ADR-0005 Amd 1, genuine pre-CF `DOC-VERSION` receipt (`docs/proof/xpa-old-build/`); `TESTS-GREEN 357 s` | `ee1942a7` |

*(Per-worker tool-call and token totals are Not recorded because AGY swarm status does not expose worker call/token counters; wall spans are Measured from board timestamps.)*

### 2. Seams, boundary corrections & findings

- **Peer-to-peer seam (`SWP` ↔ `SET`, `#9` → `#11`):** Resolved directly on the board without Coordinator bottleneck — aligned `GoalStateVersion` `(goalStateId, contentHash)`, `GoalOperatingPoint`, `WaterRecord`, `speedBand`/`depthBand`, and `OperatingPoint.Load` (`DR-SET-2` / `DR-SWP-4`).
- **Boundary correction (`CF`, `#33`):** Extended `CF` ownership to 5 wiring/test files (`SectionEditorView.axaml.cs`, `Shell/ShellHost.cs`, `BlendTests.cs`, `ConstructionTests.cs`, `ProjectStoreTests.cs`) while no other author was active — zero contention, all 5 verified in `#34`/`#39`.
- **Which parallelism justifications paid:**
  - **Wave A (width 3, 1 code + 2 docs):** Paid in full — 0 file collisions, 0 Desktop test timeouts, and `CF-0` (CG clearance + Ruling 269) completed inside Wave A so Wave B started immediately on `CLN`'s join.
  - **Wave B (width 2, 1 code + 1 docs):** Paid cleanly — `KRN` landed in 11 min with zero impact on `CF`'s 88-min geometry build, and `CF` finished all 6 stages inside one context without needing a warm fork.
  - **Independent `w5-verifier` + `monitor`:** High ROI — `w5-verifier` caught real mathematical/hydrodynamic/cache defects in `SET` (4-CV cubic `κ` + stationary-point proof), `SWP` (shallow `h/c < 5` `"deep water"` leak), and `CF` (`PrepMemo`/`MeasureMemo` profile-tangent stale-cache bug + HYG-A dead helpers), while `monitor` broke 2 quiet stalls (`#30`, `#32`, `#36`) automatically.
- **Environment & pack findings resolved during execution:**
  1. **Windows `core.longpaths`:** Initial `swarm add workspace:"share"` failed on 260-char paths under `docs/proof/ring-windows/...`; setting `git config core.longpaths true` in the primary repo resolved it permanently for all share workspaces.
  2. **`verify-ruling-citations.py` rev-120 short-form ruling false positive:** Pack rev 120 flagged two pre-existing short-form NACA report citations (`496` and `567`) in `07-low-order-hydrodynamics.md` and `state-of-the-art.md` (already spelled `NACA TR-496`/`TR-567` elsewhere in `state-of-the-art.md`); harmonized at `4a3c67d5`, restoring 10/10 `run-verify-gates.py` PASS.
  3. **`ui-craft-gate.py` on Windows ARM64:** Still `Not recorded` on this host (`impeccable` 0.1.27 lacks a `windows-arm64` binary); run on macOS ARM64 or Windows x64 before Area 1/4 UI build sign-off.

## Status

| | |
|---|---|
| **Completed** | All 5 Wave 5 tracks (`SET`, `SWP`, `CLN`, `KRN`, `CF`) and Serial Spine `CF-0` (Ruling 269) verified in Adversary Mode and landed on `main` (`ee1942a7`); `TESTS-GREEN 357 s`, `check-docs.py` exit 0, `10/10` verify gates green; all share workspaces torn down |
| **Remaining** | Operator review of Wave 5 deliverables & Decision Packet: `COPY-686..693` (CF proposed copy); `setup-seeding.html` + `DR-SET-1..5`; `local-sweeps.html` + `DR-SWP-1..6`; `ADR-0013` + `DR-KRN-1..6` (`SPIKE-CAD-01/02`); Area 5 (`solver-setup.html`, `DR-SETUP-1..6`, `DR-F3-1`, WSL OpenFOAM/PETSc-CUDA build authorization); optional push to `origin/main` and cleanup of 5 old remote branches |
| **Best next action** | Review the Wave 5 mockups & Operator Decision Packet, decide whether to push `main` to `origin`, and pick Wave 6 (Area 4 Local Sweeps build, Area 1 Setup build, `SPIKE-CAD-01/02`, or Area 5 WSL build) |
