---
id: review-test-ci-waste
title: Test and CI waste review — measured baseline, cuts and controls
type: proof-pack
status: in-review
owner: "@track-testci"
tags: [testing, ci, cost, rings, controls]
links:
  - {to: coordination-app-shell-build, rel: relates-to}
  - {to: design-app-shell, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-27
summary: Measured cost of the test harnesses, repo checks, verify gates and join on 2026-09-27; the waste found, what was cut and why, the ring split, the plan-ceremony patch list and the controls that stop the waste returning.
---

# Test and CI waste review

**Result first.** The local test run went from **67.4 s to about 30 s** (parallel suites, one Release
build), with the same 236 `PASS` names on every run. Two of the four red gates were a repo
tool's text I/O and are green. The other two are owned by the desktop-crash investigation; their
root causes are recorded here as evidence for that track. The join's heavy gates move to a
readiness ring, and the join gains the test run it never had. No test was deleted: none met the
removal rule (see the ledger).

Machine: macOS, 16 logical CPUs, .NET 10.0.203. Every number below is measured on this tree
(base `bdcbefb`) unless it says **Inferred**, which states its model.

## 1. Measured baseline (before)

| What | Measured | Notes |
|---|---:|---|
| `tools/run-tests.sh` wall, warm | **67.4 s** (66.8 / 67.6 / 67.7) | one Debug build, three suites in series |
| — build (warm, no change) | 2.8 s | |
| — Core suite (236 `PASS`) | 39.8 s | top 5 tests = 23.4 s (59 %) |
| — Cli suite | ~1 s | one scripted check |
| — Desktop suite (95 lines, no named checks) | 23.7 s | spawns `--section-flow`, `--section-tools` |
| `PASS` set across runs | identical (hash `136da3d40b29`) | 9 runs in total, 0 `FAIL` |
| `python3 tools/check-docs.py` | 2.6 s | |
| `recount-architecture-spike.py` | 14.6 s | builds a spike project; inputs are `tools/spikes/**` only |
| `recount-application-contracts.py` | 5.3 s | builds a spike project; inputs are `tools/spikes/**` only |
| `run-verify-gates.py` (12 gates) | **37.8 s, 4 red** | red gates fail early, so this is the *cheap* case |
| — `verify-application-core.py` | 19.5 s, red | fails right after its build |
| — `verify-application-adapters.py` | 17.2 s, red | fails at the Desktop suite |
| — 10 other gates | 1.2 s total | |
| Builds per join | **5 builds + 5 publishes** | core gate: build + publish (cold NuGet); adapters gate: build + 4 self-contained publishes (cold NuGet); 2 recount spike builds; `run-tests.sh` is not in the join at all |
| Test suites run per join | Core ×9, Cli ×1, Desktop ×2 (+ theme mode) | all inside the two app gates; see F2 (now Core ×2 full + 7 store-only runs) |

**Inferred — the green cost of the two app gates today.** Model: the step list in each gate × the
step times measured today (Core suite 39.8 s Debug, 26.8 s Release; build with a cold NuGet cache
19.4 s) and the per-step times in the last green receipts (2026-09-25: publishes 5–8.5 s each).
Core gate ≈ 19 + 3 × 40 + 5 + 6 × 27 ≈ **306 s**, which is past the runner's 300 s budget, so the
gate would go red on time alone once its crash is fixed. Adapters gate ≈ 17 + 1 + 24 + 2 + 1.3 +
4 × ~6 + 1 ≈ **70 s**. Gap that forced the model: running them now pops a crash report on the
operator's Mac (Coordinator instruction), so they were run once and not again.

Plan time boxes against measured work: S8 was boxed at **1 h**; its measured run was **254 s**
(audit `s8-dock-split-spike`, `duration_seconds`), 14× under the box.

## 2. Findings

| # | Location | Category | Evidence | Cost | Fix | Confidence |
|---|---|---|---|---|---|---|
| F1 | `tools/run-tests.sh` | serial suites, Debug | Core 40 s and Desktop 24 s ran one after the other; no data shared | 67 s per run, ×3 where the plan asks for three runs | run the three suites in parallel after one build; build Release | Verified (6 runs each way) |
| F2 | `verify-application-core.py` | redundant re-runs | the umask/probe variants return early only from `ProjectStoreTests.Run()`; `IdentityTests.Main` still runs all ~230 other tests, which do not read the umask. 9 full-suite runs | ≈ 240 s of ≈ 306 s (Inferred) | run only the store tests in the 8 variant runs | Verified (code) · cost Inferred · **Resolved (track COREGATE): 302 s → 87–90 s measured, see F2 resolution** |
| F3 | `docs/coordination/join.json` | wrong ring | every join runs both app gates (GUI launch, cold restore, 4 cross-RID publishes) but **no** `run-tests.sh` | ≈ 376 s per join when green (Inferred); crash pop-ups today | fast ring: `run-tests.sh` + light gates; app gates at readiness | Verified (config) |
| F4 | `join.json` `recount` | wrong ring | both recounts read only `tools/spikes/**`; a join that does not touch it re-proves unchanged input | 20 s per code join | move to the readiness ring | Verified |
| F5 | app gates | gate ignored | both were red on the joined tree `bdcbefb` and on `main`; joins went ahead | a red gate nobody acts on is noise | a cheap, green fast ring that can be enforced; slow ring at readiness | Verified |
| F6 | `verify-application-core.py` | red gate, root cause | `dotnet build` leaves `avalonia.buildservices` (Avalonia build telemetry) alive in the build's process group after exit; the gate refuses live descendants. The adapters gate sets `AVALONIA_TELEMETRY_OPTOUT=1`; the core gate does not | gate red | set `AVALONIA_TELEMETRY_OPTOUT=1` in the core gate (handed to the crash track) | Verified (probe: 1 survivor at +0, +0.5, +2, +5 s) |
| F7 | `verify-application-adapters.py` + Desktop harness | red gate, root cause | the gate runs `dotnet X.Desktop.Tests.dll`; the harness re-launches itself with `Environment.ProcessPath` + `--section-flow`, which under `dotnet X.dll` is the `dotnet` host, so it runs `dotnet --section-flow` ("command not found"), exit 1, unhandled exception, SIGABRT (-6). `run-tests.sh` uses the apphost, so it passes | gate red, crash report | launch the entry assembly when the host is `dotnet`, or run the apphost in the gate (handed to the crash track) | Verified (receipt stderr) |
| F8 | `tools/check-spiral.py` | red gates | text-mode `git` calls without `encoding=`, a write without `newline=`, no stdio guard | 2 gates red | fixed in `f086e23` | Verified (red before, green after) |
| F9 | plan and design | ceremony | see §5 | agent wall and tokens | patch list §5 | mixed, per row |
| F10 | `.github/workflows` | coverage gap (not waste) | no workflow builds or runs the C# suites; `docs-health` runs only docs checks | — | open item, not in scope | Verified |

Not waste (checked and kept): the `tools/*.mjs` browser oracles are in no automated ring (0 s per
join) and are cited as evidence by frozen mockup reviews; the Core tests are behavioural (store,
geometry, DSL, reopen) and their time is the product's geometry certification, not the harness.

## 3. Removal ledger

The rule: remove only a test that is redundant (name what covers it), trivial, ceremonial or
wrong. **No test met it.**

| Candidate | Category tested | Verdict | Why kept |
|---|---|---|---|
| `Rebuild_TenVertices_CertifiedOneUndoItem` (6.3 s) vs `Reopen_Rebuild_AcceptedRoundtripsUndoRedo` (4.6 s) | redundant? | keep both | the first is the BUDGET-DISPLAY control (runs under the real 1 s budget); the second is the EDIT-KIND-REOPEN control (save → reopen → Undo/Redo) |
| `Rebuild_TenVertices_SharesKnotsAbscissaAndPins` (4.6 s) | redundant? | keep | the only proof of the shared knot/abscissa/pin contract of the fit |
| `Reopen_RecoveryMidRebuild_ResumesSameDraftBytes` (4.6 s) | redundant? | keep | the only recovery-mid-construction case for Rebuild (EDIT-KIND-REOPEN) |
| `Canonical_Rfc8785_*` (24 vectors) | trivial? | keep | numeric oracle: published RFC 8785 vectors |
| Core suite ×8 extra runs in the core gate | redundant | **cut the re-runs, not the tests** (F2) | the store tests still run under every umask; the geometry tests run once |

## 4. Plan, as executed

| Step | Commit | Proof |
|---|---|---|
| `fix:` check-spiral text I/O: two gates green | `f086e23` | both gates red before, green after; `check-spiral --self-test` OK |
| `test:` `run-tests.sh`: one Release build, suites in parallel, timing, PASS counts, budget | `bd65a39` | 12 runs with an identical 236-name PASS set (3 serial Debug, 3 parallel Debug, 3 parallel Release, 3 under a 16-way CPU load); red-first below |
| `fix:` store test receipt race, found by the SRE review under load | `708003a` | mode assertion unchanged; see TEST-RECEIPT-RACE |
| `ci:` ring split, `tools/run-readiness.py`, TEST-RING lint, AGENTS.md rule | `e1248c7` | TEST-RING red on the old and a weakened `join.json`; `run-readiness --self-test` OK |
| `docs:` this review, three defect classes | this commit | — |

**BUDGET-DISPLAY mutation (Test Architect condition 1).** Display sampling was reverted to the
certificate's 1e-14, then restored. Release: `Reopen_InsertThenDelete_NeverThrows` is red
(`GEOMETRY-BUDGET`), but the named control `Rebuild_TenVertices_CertifiedOneUndoItem` stays green.
Debug: both are red. So the defect is still caught on every Release run, through a different test.
The named control catches it only in Debug. Both catches depend on the clock (condition 2, open).

**F2 resolution (track COREGATE, 2026-09-27).** Measured, not modeled. Before: 302 s, exit 0 (the
readiness ring kills at 300 s); build 11.5 s, 3 Debug suites 40.5 s each, publish 3.9 s, 6 Release
suites 27–28 s each. After: **87 s**, exit 0; build 11.2 s, Debug full 41.9 s, publish 3.9 s,
Release full 28.0 s, and each of the 7 selected runs 0.15–0.46 s. Almost all the suite time is the
geometry checks; the 32 store checks take under 0.5 s.

| Run | Property it proves | Checks that exercise it | Before | After |
|---|---|---|---|---|
| Debug, umask 0022 | the whole suite passes on the Debug build | all 236 | full | full (236) |
| Debug, umask 0000 and 0077 | the store forces 0600 and fails closed whatever the umask | `Store_*`, `NativePrimitive_*` (32; 6 `PERMISSION RECEIPT`s) | full | store subset (32) |
| Release publish, umask 0022 | the whole suite passes on the published layout (helper beside the DLL) | all 236 | full | full (236) |
| Published, umask 0000 and 0077 | as the Debug masks, on the published helper | the same 32 | full | store subset (32) |
| Owner-stripping, umask 0600 | an owner-stripping umask fails closed, no repair | `Store_OwnerStrippingUmask_FailsClosedWithoutRepair` | 1 + 204 others | that check |
| Helper missing / unloadable | persistence fails closed without the native helper | `Store_MissingOrUnloadableHelper_FailsClosed` | 1 + 204 others | that check |

Why the cut is safe: only `ProjectStoreTests.cs` reads `CFD_*`, creates files or loads
`libcfd_store`; `src/CfdWorkbench.Core` has no file, environment or native call, and the other
suites only read `docs/examples`. The 204 other checks gave the same PASS set in every variant run.
Per run, the after PASS sets equal the before store PASS sets (and the full sets are identical).

Controls. The Core harness takes `CFD_TEST_ONLY` (comma-separated check-name prefixes); only
matching checks run and print, and a prefix that selects nothing, or an empty selector, prints
`FAIL SELECTOR` and exits 1. The gate fails (`STORE-SUBSET`) when a check in `ProjectStoreTests.cs`
has a non-literal or non-store name; when any other Core test file or `src/CfdWorkbench.Core`
writes files, reads the environment or the temp path, or reaches Persistence or a native import
(only example-file reads are allowed), since such a check would run at one umask only; and when a
run does not pass exactly the 32 normal-run store checks named in the source (full and subset
runs alike). Receipts gain `suite` (`full` or the selector), because a
`tests-0000` label no longer means the full suite. Red-first: a native helper that `fchmod`s new
files to 0644 when the umask is 0077 left the full 0022 run green (236) and the 0000 subset green,
and the 0077 subset failed 20 checks; gate exit 1. A planted early `return` at umask 0000 made the harness exit 0
with 5 of 32 store checks; the gate failed `STORE-SUBSET` naming the 27 missing. Selector red:
`Nope_`, `Canonical_,Nope_`, `""` and `" , "` all exit 1. Final green run: 90 s. Test Architect
(adversary): PASS with conditions; three are fixed above, one is a seam: `tools/run-tests.sh` does
not unset `CFD_TEST_ONLY` (or the older `CFD_NATIVE_CAPABILITY_PROBE` / `CFD_OWNER_STRIPPING_*`),
so a selector exported in a shell would make it report a subset as green (open item).

**Handed to the crash track (not edited here, by boundary):** F2 (the core gate re-runs the whole
Core suite 9 times; only the store tests read the umask), F6 (the core gate lacks
`AVALONIA_TELEMETRY_OPTOUT=1`; SRE rates it Inferred until a run with the flag shows 0
survivors, and suggests one shared environment builder for both gates), and F7 (self-spawn through
the `dotnet` host; also, a failed child becomes an unhandled exception and SIGABRT instead of
exit 1). Both app gates are recorded as **owned by the crash investigation**.

## 5. Plan-ceremony patch list (for the Coordinator, after an Owner ruling)

The plan (`docs/coordination/app-shell-build.md`) and design (`docs/design/app-shell.md`) are frozen,
so these are proposals. The verdicts are the Test Architect's.

| # | Where | Today | Proposed | Verdict and reason |
|---|---|---|---|---|
| P1 | plan tracks table, lines 96–97, 126–127, 234–236 | S8 1 h, G0 2.5 h, C1 90 min, D3a/D4 3 × 70 min | box = 3× a measured prior from the same class of work. Without a prior, keep the box and record the measured time in the audit entry. | ACCEPT-WITH-CONDITIONS. A shorter box finds a silent exit sooner (HARNESS-SILENT-EXIT waited 80 min). One spike (S8: 254 s) is no prior for a T2 port. |
| P2 | plan lines 102, 104, 251; design §12 line 887 | "`run-tests.sh` three times with identical PASS sets" | keep three full runs, and add: the compared PASS set must be non-empty for every named suite. | The Desktop-only cut is **VETOED**: it saves ~18 s and drops the only repeat of the clock-dependent Core test under load. The runner change makes three runs ~90 s instead of ~200 s. The runner now enforces non-empty PASS for named suites. |
| P3 | design §12.5; plan D3a row | a recorded red run on every ported row | mutations may be batched, but every ported row must go red in at least one recorded mutation run | One-mutation-per-group is **VETOED**: ported tests reach private fields by reflection and can bind to nothing, so each row needs its own red. Batching is the saving. |
| P4 | design §12.2; plan line 211 | `check-named-tests.py`, 28-edge ledger | keep | ACCEPT. Both are mechanical and cheap. |
| P5 | plan join gates | app gates on every join | follow `join.json`: app gates and recounts at readiness, before merge to main | ACCEPT-WITH-CONDITIONS (applied): `xaml-token-lint` stays in the fast ring (the design's CD8 control on D3a/D4); readiness is checkable by receipt. |
| P6 | plan D3a and D4 | "`tools/verify-application-adapters.py` xaml-token-lint step clean" | "the join's `xaml-token-lint` check clean". The fast ring now runs it directly, so this no longer needs the GUI gate. | follows from P5 |

## 6. Controls

| Control | Class | Red-first proof | Where |
|---|---|---|---|
| C1 test budget: exit 3 TEST-BUDGET when a green run exceeds `CFD_TEST_BUDGET_SECONDS` (60 s) | TEST-COST | `CFD_TEST_BUDGET_SECONDS=5` → exit 3 | `tools/run-tests.sh` |
| C1b zero-PASS refusal: a named suite that exits 0 with no `PASS` line fails | TEST-COST / HARNESS-SILENT-EXIT | Cli marked as named (probe copy) → exit 1, "printed no PASS line" | `tools/run-tests.sh` |
| C3 TEST-RING lint, both directions | TEST-RING | the old `join.json` → 8 problems, exit 1; a weakened copy → 4 problems, exit 1 | `tools/check-docs.py` |
| C3b readiness receipt: `--check` refuses a HEAD with no green receipt | TEST-RING | self-test: a red command → red; a newer HEAD → stale, exit 1 | `tools/run-readiness.py` |
| C4 one always-loaded rule | TEST-RING, TEST-COST | — (prose, points at C1 and C3) | AGENTS.md project section |

**Dropped: C2**, a lint on time boxes and "N times" in plans. Test Architect VETO and Simplifier
DROP: it is a regex over prose; no recorded defect traces to a generous box; and S8's box was
exactly 60 min, so the lint could not have caught it. P1 goes to the plan template instead.

**Kept against the Simplifier's soft block, in writing:** `tools/run-readiness.py`. The Test Architect's
hard-veto condition requires readiness to be fail-closed and mechanical, and the SRE's rot finding
requires a receipt that names the SHA. One AGENTS.md line records neither. The C1 default is 60 s,
not the Simplifier's 45 s: 60 s catches the 67 s serial runner, and it leaves 1.8× headroom over the
34 s measured under a 16-way load, which answers the SRE's flake concern.

## 7. After (same measurements as §1)

| What | Before | After |
|---|---:|---:|
| `run-tests.sh` wall, warm | 67.4 s | **29.7–31.6 s** (9 runs); 32–34 s under a 16-way CPU load |
| Core / Desktop suites | 39.8 s / 23.7 s in series | 29 s / 26 s in parallel |
| Tests (named `PASS`) | 236 Core + 2 scripted suites | 236 + 2 (identical set, 0 removed) |
| Builds per local run | 1 (Debug) | 1 (Release) |
| Join fast ring | check-docs 2.6 s + recounts 20 s + gates 37.8 s (red) ≈ 60 s, **no test run** | **35.1 s, green**: check-docs 2.4 + run-tests 31.6 + xaml lint 0.2 + 10 gates 1.0 |
| Join when the app gates are green | ≈ 400 s (Inferred, §1) | 35 s per join; ≈ 400 s once, at readiness |
| Builds per join | 5 builds + 5 publishes | 1 build |
| Red gates | 4 | 0 in the fast ring; 2 in readiness, owned by the crash investigation |
| `run-verify-gates.py` | 12 gates, 4 red, 37.8 s | 10 run, 0 red, 1.0 s; 2 skipped by name |

**Test time budget, not measured here:** tokens and agent actions per join. The join's own audit
entry records `duration_seconds`; the next join's entry is the after-number for agent wall time.

## 8. Review (Adversary Mode, 2026-09-27)

| Lens | Verdict | Applied |
|---|---|---|
| Test Architect (hard veto) | BLOCK → conditions | Release mutation run (§4); PASS count and zero-PASS refusal; `xaml-token-lint` in the fast ring; fail-closed readiness receipt; TEST-RING both directions; C2 dropped; P2 and P3 vetoes recorded in §5. **Open:** a BUDGET-DISPLAY control that does not depend on the clock, which needs a `src/` change (owned by geometry). |
| Simplifier (soft) | BLOCK (soft) | status files dropped for `wait`; C2 dropped; C3 folded into check-docs; build-count lint dropped. The readiness script is kept, with the reason in §6. |
| SRE | PASS-WITH-CONDITIONS | loaded-run flake fixed (`708003a`) and 3 loaded runs recorded. **Open:** per-suite timeout (an unbounded `WaitForExit` in the Desktop harness); CPU-second history instead of wall time; readiness staleness warning; forced readiness on `tools/spikes/**` or `*.csproj` changes. |

Repair cycles used: 1 of 2.

## 9. Open items

- A clock-independent BUDGET-DISPLAY control (Test Architect condition 2). It needs a `src/` seam.
- No CI workflow builds or runs the C# suites (F10). The readiness ring is local and operator-run.
- The merge to main is refused by rule (AGENTS.md) and by `run-readiness.py --check`, but no hook
  calls `--check` yet. The pre-commit floor is the pack's (`coord-core.py precommit`).
- F2 is resolved (core gate 302 s → 87–90 s, above). Seam: `tools/run-tests.sh` should unset `CFD_TEST_ONLY` and the other `CFD_*` probe selectors. F6 and F7 are with the crash track. Until it lands, readiness is red, and it stays red, not skipped.
- The SRE's per-suite timeout and CPU-second budget.
- The `tools/*.mjs` mockup oracles run in no ring; they are evidence for frozen reviews, cost 0 s per join, and are kept.

## 10. Desktop suite profile (2026-09-30)

**Result first.** `tools/run-tests.sh` went from **51.3–52.1 s to 33.7–34.5 s** warm (32–35 s gate
`wall`), and to 37.4–37.9 s under a 16-way CPU load. The Desktop suite went from 50–51 s to 21–22 s.
The PASS set is byte-identical on all 9 runs (2 before, 5 after, 2 after under load): 591 sorted
`PASS` lines, sha256 prefix `29aa8f2c5ca3a621`. The Desktop PASS lines, in printed order, hash to
`7b02f8f91b73b01e` on every run. The 60 s budget is unchanged. Core (33 s) is now the critical path.

**Why.** At 51–53 s of a 60 s budget, M1.2b U1b (43 Plan-canvas tests) and U2 (25 pane tests) would
fire TEST-BUDGET. Raising the budget with no measured reason is TEST-COST, so the fix had to be real
speed.

### How the harness ran (read from `WorkbenchTests.cs`, not inferred)

With no arguments, the harness runs the in-process checks: `SelfLaunchTests`, the theme and
document checks, and `SectionCanvasTests`. Then it ran two children one at a time through
`SelfLaunch.RunChild` (`--section-flow`, `--section-tools`), then
`DesktopChecks.Spawn("--shell-model", "--controller-shell", "--shell-window", "--plan-canvas")`.
`Spawn` was a sequential loop: start the child, wait, print `SUITE <mode> exit N`. The children
inherited the parent's stdout. Each child is a separate process of the same assembly
(`SelfLaunch.StartInfo`).

### Profile (Release, warm, measured with a line timestamper and by running each mode alone)

| Stage | Before (sequential) | After (parallel, 4 slots) |
|---|---|---|
| In-process checks, incl. `SelfLaunchTests` 0.6–0.7 s | 6.5 s | 4.8 s |
| `--section-flow` child | 8.5–8.6 s | 9.1–9.3 s |
| `--section-tools` child | 11.3–11.4 s | 12.2–12.4 s |
| `--shell-model` child | 0.05–0.08 s | 0.1 s |
| `--controller-shell` child | 8.6–9.3 s | 9.3–11.1 s |
| `--shell-window` child | 15.6–16.0 s | 16.8–17.6 s |
| `--plan-canvas` child (empty; Avalonia setup only) | 0.2 s | 0.2 s |
| Children stage | 45.1 s (sum) | 16.5 s (≈ the longest child) |
| Desktop harness total | 51.6 s | 21.4 s |

Child start-up cost is 0.04 s (`--startup-failure-probe`, which throws at once) to 0.2 s (`--plan-canvas`,
which only sets up Avalonia). The cost is the suites themselves, not process start. The "after"
per-child times come from the new `SUITE-TIME <mode> <s> s` line, which prints on every run. Under
concurrency each child is 5–15 % slower (shared CPU), which the overlap more than repays.

### Independence (why concurrency is safe)

- **Files.** Every child scratch path is a GUID name under `Path.GetTempPath()`
  (`ControllerShellTests` 215 and 550, `ShellWindowTests.ScratchPath`). `run-tests.sh` points
  `TMPDIR` at `.tmp-tests/`. Repo reads (`RepoRootFromSource`, the example `.foil`) are read-only.
  `SectionFlowTests`, `SectionToolsTests` and `ShellModelTests` touch no files.
- **Ports, pipes, mutexes, settings.** None found (grep over all six child suites).
- **The one candidate shared resource: macOS window activation.** The window suites show real
  windows (`UsePlatformDetect`), and `ShellWindowTests` asserts Avalonia logical focus (`IsFocused`)
  about 30 times. If one process activating a window cleared focus in another, a focus check would
  flake. Probe: 4 `--shell-window` copies at once, 3 rounds. Result: 12 of 12 exit 0, each with 83
  PASS and one hash (`1211ddffd3c1d3e5`), 0 FAIL. The 7 parallel gate runs also ran
  `--shell-window` beside the window-showing `--section-flow` and `--section-tools`, with no
  difference. Confidence: **Verified** for these 19 observations. Absence of a rare race is
  **Inferred**; see the residual risk below.

### Change

- `DesktopChecks.Spawn` runs children at most `Math.Clamp(ProcessorCount / 2, 1, 4)` at a time
  (4 here; 16 CPUs). It starts them in mode order, buffers each child's stdout and stderr, and prints
  them in mode order after the child finishes. It keeps `SUITE <mode> exit N`, adds `SUITE-TIME`, and
  keeps the first-nonzero-exit rule and the `FAIL <mode> exited N` line.
- `--section-flow` and `--section-tools` moved into the same `Spawn` call. A failure there now prints
  `SUITE`/`FAIL` lines and returns the child's exit code, instead of throwing out of `RunChild`
  (exit 70). `SelfLaunch.RunChild` had no other caller and is deleted (seam touch in `SelfLaunch.cs`,
  a file no parallel track owns).
- `tools/run-tests.sh` is unchanged.

### Proof

| Run | Gate exit | Measured wall | PASS | Set hash | Desktop ordered hash |
|---|---|---|---|---|---|
| before 1 | 0 | 52.1 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| before 2 | 0 | 51.3 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after 1 | 0 | 34.5 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after 2 | 0 | 33.9 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after 3 | 0 | 33.8 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after 4 | 0 | 33.7 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after 5 | 0 | 33.8 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after, 16 × `yes` load 1 | 0 | 37.9 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |
| after, 16 × `yes` load 2 | 0 | 37.4 s | 591 | `29aa8f2c5ca3a621` | `7b02f8f91b73b01e` |

Set hash: sha256 of the sorted `^PASS ` lines of the Core, Cli and Desktop logs. Ordered hash: the
Desktop log's `^PASS ` lines in printed order.

**Red plant.** A temporary `DesktopChecks.Check("SPEED_Plant_Red", () => throw …)` in
`ShellModelTests.Run` (a middle child, so later children ran concurrently). `run-tests.sh` exited
**1** and printed `FAIL SPEED_Plant_Red InvalidOperationException: planted red`,
`FAIL --shell-model exited 1` and `FAILED: CfdWorkbench.Desktop.Tests (exit 1)`. The other children
still printed their `SUITE … exit 0` lines in mode order. The plant was removed with
`git checkout`, and it is not in any commit.

### Residual risk and next steps

- Cross-process window activation is not proven absent, only unobserved in 19 concurrent
  observations. If a focus check ever flakes only in the full run, first rerun `Spawn` with 1 slot.
- The Desktop children stage is now bounded by its longest child (`--shell-window`, 17 s). U1b's
  Plan-canvas tests land in `--plan-canvas`, which runs beside it, so they add wall time only past
  about 17 s of their own. U2's additions go to `ShellModelTests.cs` and `ShellWindowTests.cs`
  (m12b-points §14), so every second U2 adds to `--shell-window` adds a second to the Desktop wall.
  If `--shell-window` passes about 30 s, the cheapest next cut is to split it into two modes.
  `SUITE-TIME` shows when that happens.
- The next critical path is Core at 33 s, not Desktop.

## 11. Core suite back under budget (2026-10-02, track TESTCOST)

On `b222a72`, `tools/run-tests.sh` was green but over budget: Core took 59 s and the wall 62 s, so it
exited 3. Two causes were reported. Only one was real.

### The 5.00 s store checks were a measurement artifact (Verified)

The Coordinator's per-check timer ran the Core harness without `run-tests.sh`'s TMPDIR. Under the
inherited macOS TMPDIR (`/var/folders/…`, a symlink), the store refuses the path in
`ParentPath.Open` (`DOC-UNSUPPORTED-PERSISTENCE`). That happens before the `ClaimCreated` or
`Published` hook stage, so the hook never signals. The test thread's `claimed.Wait(5 s)` or
`reached.Wait(5 s)` then expires (`ProjectStoreTests.cs:126`, `:242`, `:264`), and the check **fails**
after 5.0 s. In that run, 21 store checks failed. The timer records PASS and FAIL lines alike, but
prints only the names. Under `run-tests.sh` these three checks take about 1 ms each. This was
measured alone, in a full sequential run, beside Cli and Desktop, with `DOTNET_PROCESSOR_COUNT=1`,
and in 24 concurrent runs (worst 0.76 s, which is the first selected check paying process start).
There is no store defect and no test defect. Every timed wait in the Core tests already asserts its
result. No change was made.

### Ring moves (TEST-RING)

| Check | Ring | Content | Before | After |
|---|---|---|---|---|
| `Placement_DisplayWithinCertifiedEnclosure_Fixtures` | fast | golden fixtures at chord samples 0, 5, 10; both two-hump (F3) fixtures at all 11 | 13.6 s | 7.7 s |
| `Placement_RandomFixtures_WithinCertifiedEnclosure` | fast | random fixture 0 (same seed and values) | 4.8 s | 1.2 s |
| `Placement_ProbeChord_EqualsWingEstimatesOnPl0Fixtures` | fast | every 10th η (101 values, bit-identical to a subset of the 1001) | 3.5 s | 0.34 s |
| `PlacementRule_CertificateGoldenMaster_PointAtBitsUnchanged` | fast | unchanged | 2.0 s | 1.8 s |
| `Readiness_Placement_DisplayWithinCertifiedEnclosure_AllSamples` | readiness | the old fast sweep: every sample on every fixture | — | 13.9 s |
| `Readiness_Placement_RandomFixtures_AllFour` | readiness | the old fast sweep: all four random fixtures | — | 5.0 s |
| `Readiness_Placement_ProbeChord_1001Samples` | readiness | the old fast sweep: 1001 η | — | 3.6 s |

The readiness sweeps print the same `max_outside_m` values as the old fast checks. No check was
deleted, and no assertion was weakened.

**Mutants against the new fast ring (both killed).** M1-F3: `Maximize` polishes only the best grid
sample (the pre-`0bd8de3` behaviour). It turns `…DisplayWithin…_Fixtures` and `…DisplayMaximum…`
red. M2: an interior chord sample (index 4 of 11, outside the golden fixtures' 0, 5, 10) drifts
10 nm on every station. It turns `…DisplayWithin…_Fixtures` red through the two-hump all-sample
sweep. Both plants were restored, and neither is in any commit.

**Readiness wiring.** Nothing passed `--readiness` before this change, so
`Readiness_Surface41x101_Under25Ms` and `Readiness_SixteenPointThreeAnchors_AssessUnderProofBudget`
had never run in a ring. The `readiness` array of `docs/coordination/join.json` now runs
`dotnet run -c Release --project tests/CfdWorkbench.Core.Tests/… -- --readiness`. `check-docs.py`'s
TEST-RING rule now fails a `join.json` whose readiness ring lacks that command; it was red first on the
unwired file.

| Measure (Release, same session, load average 8–14) | Before | After |
|---|---|---|
| Core harness alone, sequential | 57.5 s | 44.1 s |
| `run-tests.sh` Core / wall | 59 s / 62 s (exit 3, Coordinator) | 49 s / 51 s (exit 0) |
| Core PASS count | 437 | 437 |

### Residual risk

- A drift that occurs only at an interior chord sample, and only on a golden fixture other than the
  two-hump ones, passes the fast ring. Readiness catches it.
- A chord mismatch between `Placement.Frame` and `WingEstimates` that occurs only off the 0.01 η grid
  passes the fast ring. Readiness catches it.
- The two-hump fixtures cost about 6 s of the fast ring, and they are the next Core cut if the
  budget tightens again.
- Running the Core harness without `run-tests.sh`'s TMPDIR fails 21 store checks. Any ad hoc timer
  must set that TMPDIR, and it must report each check's status beside its time.

## 12. Split suites and a CPU-bounded slot count (2026-10-02, track budget-split)

**Result first.** `tools/run-tests.sh` went from **56–59 s to 43–44 s** on a quiet machine (16–17 s
headroom on the unchanged 60 s budget), and from 60 s to 45–46 s under 10 busy loops. The PASS multiset
is identical on every run, before and after: 949 sorted `PASS` lines (948 names;
`Properties_TipCloses_TipChordIsText` prints twice, before and after), sha256 prefix
`1c18fa1ff8845f68`, 0 `FAIL` lines. No check moved ring, and none was removed or weakened.

**Why.** At 56–59 s, four tracks about to add tests would fire TEST-BUDGET. Raising the budget with no
measured reason is TEST-COST.

### What limited the wall (measured on `e98ce73`, not inferred)

- `run-tests.sh` already ran Core, Cli and Desktop as three parallel processes.
- The Desktop harness ran 10 child modes through `DesktopChecks.Spawn`, at most
  `Math.Clamp(ProcessorCount / 2, 1, 4)` = **4** at a time, in a fixed order. The children summed to
  173.5 s of wall. The two longest ran alone in 34.1 s (`--shell-window`, 111 checks) and 31.7 s
  (`--plan-canvas`, 49 checks). So Desktop could not finish before about 5 s of in-process checks plus
  34 s, and 4 slots could not finish 173.5 s of children before about 43 s.
- Each child uses about 1.4 cores (alone: `--shell-window` real 34.1 s, user 47.5 s;
  `--properties-view` real 28.1 s, user 44.7 s). The whole gate used 308 CPU-s in 56 s of wall: an average
  of 5.5 busy cores out of 16. The limit was the scheduler, not the CPU.
- Core runs on one core: alone it took real 43.4 s, user 43.9 s (with the gate's TMPDIR; see §11).
  Once Desktop got faster, Core became the critical path (48 s).

| Suite (seconds, gate run) | Before (4 slots) | After (6 slots, quiet run 1) |
|---|---|---|
| `--shell-window` | 36.1–36.3 | part 1/2: 20.5 · part 2/2: 23.5 |
| `--plan-canvas` | 33.7–33.9 | part 1/2: 24.0 · part 2/2: 15.7 |
| `--properties-view` | 30.1–30.2 | 31.8 |
| `--views` | 23.5–23.7 | 25.7 |
| `--properties-cells` | 20.1 | 21.7 |
| `--controller-shell` | 9.5–9.7 | 10.3 |
| `--status-strip` | 7.4–7.7 | 7.7 |
| `--section-flow` / `--section-tools` / `--shell-model` | 6.2 / 6.0 / 0.4 | 6.5 / 6.2 / 0.4 |
| Desktop harness | 55 | 42 |
| Core harness | 46 | part 1/2: 18 · part 2/2: 32 |
| `run-tests.sh` wall | 56–59 | 43–44 |

### Change

- **Interleaved parts.** Both check helpers (`DesktopChecks.Check`, Core's `IdentityTests.Check`) take
  `--part=k/n` as a command-line argument, never as an environment variable an old shell could leak.
  A part runs the checks whose registration index `i` has `i % n == k - 1`. The rule is applied after the
  `CFD_TEST_ONLY` selector, so a prefix counts as matched in every part. A part prints
  `PARTITION k/n of N checks`. A part that runs no check fails (`FAIL PARTITION k/n ran no check`), and a
  malformed part fails with a named line (exit 1 in Core; the named startup-failure exit 70 in Desktop).
- **Proof that the parts cover the suite.** The parts run each check exactly once only if they are parts
  1..n of one n and every part enumerated the same registrations. `Spawn` (Desktop) and `run-tests.sh`
  (Core) check both from the `PARTITION` lines and fail the run otherwise. This catches a registration
  that depends on run-time state, which would otherwise drop a check silently.
- **Split suites.** `--shell-window` and `--plan-canvas` run as 2 parts each. Core runs as 2 parts
  (`run-tests.sh` jobs `Core 1/2`, `Core 2/2`; logs `Core.part1of2.log`, `Core.part2of2.log`).
- **Order.** `Spawn` starts the longest modes first. The log still prints in that same mode order.
- **Slots.** `Math.Clamp(ProcessorCount * 3 / 8, 1, modes.Length)` = 6 here (was capped at 4). See
  the load result below for why it is not `ProcessorCount / 2`.
- **Stale logs.** `run-tests.sh` now deletes `.tmp-tests/*.log` and `*.seconds` before it runs.
  `check-named-tests.py` reads every `.tmp-tests/*.log`, so a `Core.log` left from the old layout would
  have fed old `PASS` lines to it.

### Slot count under load (why 6, not 8)

| Desktop slots | Quiet wall | Wall under 10 × `yes` | Result under load |
|---|---|---|---|
| 4 (baseline code, `e98ce73`) | 56–59 s | 60 s | exit 0, multiset identical |
| 8 (`ProcessorCount / 2`) | 39–43 s | 46 s | **exit 1, twice**: 7 Core checks `GEOMETRY-BUDGET` |
| 6 (`ProcessorCount * 3 / 8`, shipped) | 43–45 s | 45–46 s | exit 0 on 3 runs, multiset identical |
| 4 (with the splits) | 55 s | 56 s | exit 0, multiset identical |

At 8 slots the gate peaks near 13 busy cores. Add 10 busy loops and the 16 logical CPUs are
oversubscribed. Then the product's cooperative proof budget (`ProofBudget`, 1 s wall clock,
`Geometry.cs`) runs out in 7 Core checks: `Rebuild_TenVertices_CertifiedOneUndoItem`,
`Thickness_UseSource_SharedExample_FitsRootTipAndUndoRestores`,
`Thickness_UseSource_ConflictingValueLock_RefusesApply`, `Profile_PatchUpper_LeavesLowerCurveBytes`,
`Profile_UpperCrossesLower_ReportsCross`, `Profile_SharedEdit_ScopeApplyUndoRedo`,
`Profile_IndependentEdit_ApplyUndoRedoKeepsOtherProfiles`. The same 7 failed in both runs. The baseline
passed under the same load, so 8 slots would turn a second concurrent gate on this machine into a
false red. At 6 slots the peak is about 10 cores, and it passed under load.

### Proof

| Run | Gate exit | Wall | PASS lines | Multiset vs baseline | FAIL lines |
|---|---|---|---|---|---|
| baseline 1 / 2 (`e98ce73`, quiet) | 0 / 0 | 59 / 56 s | 949 / 949 | — (the baseline) | 0 |
| baseline, 10 busy loops (`e98ce73`) | 0 | 60 s | 949 | identical | 0 |
| 6 slots, quiet 1 / 2 / 3 | 0 / 0 / 0 | 45 / 44 / 43 s | 949 | identical | 0 |
| 6 slots, 10 busy loops 1 / 2 / 3 | 0 / 0 / 0 | 46 / 46 / 45 s | 949 | identical | 0 |

Multiset: the sorted `^PASS ` lines of every `.tmp-tests/*.log`, compared with `diff` (the empty diff
is the evidence), sha256 prefix `1c18fa1ff8845f68` on every row. Every `PASS` line comes from the two
`Check` helpers or two literal Cli lines (grep over the three test projects), so no output path skips
the part rule.

**Red plants (one gate run, then restored byte-for-byte; none is in any commit).**
(1) `run-tests.sh` ran only `Core 1/2`. (2) `Spawn` dropped `--plan-canvas --part=2/2`. (3) A
`SPEED_Plant_Red` check threw inside the split `--shell-window` suite. The gate exited **1** and
printed `FAILED: Core parts are incomplete or enumerated different checks`,
`FAIL PARTITION --plan-canvas parts are incomplete or enumerated different checks: [--part=1/2 49 checks]`,
`FAIL SPEED_Plant_Red InvalidOperationException: planted red` and `FAIL --shell-window --part=1/2 exited 1`.
Direct probes: `--part=500/500` on Core and `--part=99/99` on `--shell-model` exit 1 with
`FAIL PARTITION … ran no check`; `--part=3/2` (Core) and `--part=0/2` (Desktop) exit nonzero.

### Residual risk and next steps

- **The product's 1 s proof budget has a thin margin.** A comment in `AuthoringSession.Sample` records a
  rebuild at about 970 ms against the 1 s `ProofBudget`. CPU starvation turns 7 checks red. So the
  fast ring's verdict depends on machine load, and a slower user machine may refuse these edits with
  `GEOMETRY-BUDGET`. That is a product question for the Core owner, not a gate setting. **Inferred**
  for user machines; **Verified** for this machine at 8 slots under 10 busy loops.
- Three or more gates at once on this machine (about 30 busy cores) were not measured. If a
  concurrent-run red shows `GEOMETRY-BUDGET`, rerun alone before you debug.
- The next Desktop pole is `--properties-view` (32–35 s, one process). It is the next suite to split,
  with one string in the `Spawn` list. Core part 2/2 (32–38 s) is unbalanced against part 1/2 (18–21 s),
  because its heaviest checks fall on odd indices. A third part or a different split is the next Core cut.
  `SUITE-TIME` and the per-part `==` lines show when either is needed.
- The part rule assumes each check is independent of the checks before it. That was true for all 949
  today (the multiset is identical). A future check that depends on an earlier one would go red in its
  part, which is loud, not silent.
