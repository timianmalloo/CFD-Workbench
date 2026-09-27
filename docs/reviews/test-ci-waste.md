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
| Test suites run per join | Core ×9, Cli ×1, Desktop ×2 (+ theme mode) | all inside the two app gates; see F2 |

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
| F2 | `verify-application-core.py` | redundant re-runs | the umask/probe variants return early only from `ProjectStoreTests.Run()`; `IdentityTests.Main` still runs all ~230 other tests, which do not read the umask. 9 full-suite runs | ≈ 240 s of ≈ 306 s (Inferred) | run only the store tests in the 8 variant runs | Verified (code) · cost Inferred |
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
- F2, F6 and F7 are with the crash track. Until it lands, readiness is red, and it stays red, not skipped.
- The SRE's per-suite timeout and CPU-second budget.
- The `tools/*.mjs` mockup oracles run in no ring; they are evidence for frozen reviews, cost 0 s per join, and are kept.
