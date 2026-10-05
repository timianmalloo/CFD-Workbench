---
id: plan-test-cost
title: Test time and cost — measured baseline and ranked levers (Ruling 67)
type: doc
status: proposed
owner: "@track-test-cost-study"
tags: [testing, ci, cost, rings, performance, agents]
links:
  - {to: rulings, rel: implements}
  - {to: review-test-ci-waste, rel: relates-to}
review-by: 2026-11-04
summary: Where the fast ring, the readiness ring and agent repair loops spend their time on 2026-10-04 (measured), and the levers that cut it, each with its saving, coverage risk and cost to build, in a recommended order. §8 records the levers shipped on 2026-10-04 (L1-L6 and the safety fixes) with their measured before/after: readiness 348.6 s to 101.6 s, the PASS multiset unchanged.
---

# Test time and cost: measured baseline and ranked levers

**Shipped 2026-10-04 (§8, measured):** readiness **348.6 s → 101.6 s** green on this branch under
load 16–75, with the fast-ring PASS multiset unchanged (1,162 lines, `d7ba74e68a688d06`). L1–L6 and
the safety fixes landed; the optional COST lines did not (§8.4).

**Result first (the study, before the levers).** The fast ring is near its floor. Ordering and two splits can save about 6 s of
its 53 s (Inferred). The money is in the **readiness ring** (310 s) and in **how often agents run
the rings**. Readiness spends **163 s of its 310 s** running two full suites a second and a third
time in Debug, which nothing else needs. Agents ran readiness 95 times and the fast ring about 359
times in the recorded Claude transcripts. A single Core check runs in 0.6 s and a single Desktop
check in 2.3 s; the full ring takes 53 s. Recommended order: the inner-loop rule (no code), then
remove the Debug re-runs (−81 s and −33 to −76 s per readiness run), then partition and overlap
what is left. Readiness should fall from 310 s to about 125 s serial (Inferred). No check moves
ring and no check is removed.

Two operational defects surfaced while measuring (Verified). They are reliability risks, not time
savings. **The gate scratch directories are never deleted: about 224 GB on this machine.**
**The fast ring has no timeout**, so a hung window check hangs a join forever. See §5.

Machine: macOS, 16 logical CPUs, .NET 10.0.203, worktree `design/test-cost-study` at `b504d85`.
The machine was **not quiet**: load average was 6.6–15 from other agents' worktrees. Every number
says which run it came from. Proposals only (Ruling 67); this track changed no product code and no
test.

## 1. Measured baseline

### 1.1 Fast ring (`tools/run-tests.sh`, every join)

| Run (2026-10-04) | Load avg | Build | Core 1/2 | Core 2/2 | Desktop | Cli | Wall | Exit |
|---|---|---|---|---|---|---|---|---|
| ring 1 | 6.6 → 11.3 | 4 s | 27 s | 34 s | **49 s** | 2 s | **53 s** | 0 |
| ring 2 | 15.2 | 1 s | 48 s | 54 s | **62 s** | 2 s | **63 s** | **3 (TEST-BUDGET)** |

- Both runs printed the same 1,135 `PASS` lines (sorted multiset sha256 prefix `33e5dc16c79f9f38`).
  Ring 2 was red **only on load**. The budget verdict depends on how many other agents are running
  (finding F-3).
- The join's other fast steps cost little: `check-docs.py` 4 s, the fast verify gates 2 s
  (10 gates, each ≤ 0.4 s). **The ring is about 88% of a join.**
- A warm incremental build costs 0–2 s: no-op 1 s, no-op `--no-restore` 0 s, a touched Core file
  1 s, a touched Desktop file 2 s, a touched Desktop test file 1 s. **Build is not a lever in the
  fast ring.**

### 1.2 Desktop harness, the fast ring's critical path

Run alone with a line timestamper (`docs/proof/test-cost/stamp.py`) on the Release build: 45.3 s.

- **Serial in-process prefix: 2.7 s** (self-launch, Example, NATIVE-STARTUP, 42 THEME-RESOURCE
  lines, shadow mutation, SectionCanvas). It is 6% of the harness. It is not a lever.
- **14 child modes on 8 slots** (`ProcessorCount / 2`). The child times summed to 256.7 s (solo
  run) and 271.7 s (ring 1).
- **Schedule replay** (`docs/proof/test-cost/slot-sim.py`): replaying Spawn's start-in-list-order
  rule with the measured child times predicts a 42.7 s makespan. Adding the 2.7 s prefix gives
  45.4 s; the measured harness took 45.3 s. **The model matches to 0.1 s**, so its what-ifs below
  are trustworthy as models (still labelled Inferred).
- Slot utilisation as listed: **75–76%**. The pole is `--properties-cells` (21.8 s, unsplit). It
  is listed *after* the shorter `--views` parts, so it starts at 21–22 s and finishes last.
- The lower bound at 8 slots is max(longest child, sum ÷ 8) = **32.1 s** (solo) or 34.0 s (ring 1).
  Below that you need fewer CPU-seconds, not better scheduling.

Per-check cost, from running two suites alone with the timestamper. Absolute values are inflated
by load (they ran beside a full Core run); the ranking holds.

| Suite | Checks | Shape |
|---|---|---|
| `--status-strip` | 21 | **2 checks hold 38%**: `SectionCommands_EveryRow_RunsOrNamesReason` 8.1 s, `StatusStrip_CrossingCleared_Copy124RenderedOnce` 5.9 s; then 5 checks at 2.4–3.1 s |
| `--properties-view` | 67 | Flat: 58 checks at 0.1–1 s (the per-window cost); 4 typed-section-point checks at 2.0–3.0 s |

### 1.3 Core harness

- Run alone: **586 checks in 45.9 s, one process, one core.** 493 checks take < 0.1 s each
  (8.5 s in total). **10 checks hold 50% of the time.** The largest are
  `Placement_DisplayWithinCertifiedEnclosure_Fixtures` 7.1 s and
  `ProfileEvaluator_BoundToCertificate_Within1e9Chord` 3.9 s.
- One check alone, with `CFD_TEST_ONLY`, finishes in **0.6 s** from launch.
- Part balance model (`docs/proof/test-cost/part-balance.py`): with 2 interleaved parts,
  19.7 / 26.5 s. That matches the 27 / 34 s ring ratio. **With 3 interleaved parts: 16.6 / 14.8 /
  15.1 s.** A cost-weighted split gains only 1 s over that, so the existing `--part` rule is
  enough.

### 1.4 Readiness ring (`tools/run-readiness.py`, before every main move)

From the green receipt for `872bf75` (the parent of this branch). Older receipts give the same
shape, with `run-verify-gates` at 203–261 s.

| Step | Seconds | Inside it (from the gates' own receipts, last 3 runs each) |
|---|---|---|
| `run-verify-gates.py` (all gates, **serial**) | **261.2** | `verify-application-core` 145–147 · `verify-application-adapters` 110–115 · 10 others ≈ 2 |
| `recount-architecture-spike.py` | 7.9 | |
| `recount-application-contracts.py` | 4.4 | |
| Core `--readiness` (`dotnet run`) | 24.4 | |
| Desktop `--readiness` (`dotnet run`) | 12.1 | |
| **Total** | **310.0** | |

**`verify-application-core` (146 s)**, from `receipts/process-*.json`, 3 runs:

| Step | Seconds |
|---|---|
| hermetic `dotnet build` (Debug, build servers off) | 12.5–12.8 |
| **full Core suite, Debug, umask 0022** | **81.9–82.1** |
| store subset at umask 0000 and 0077 | 0.6 + 0.7 |
| `dotnet publish` Core.Tests (Release) | 4.2–4.3 |
| **full Core suite, published Release, umask 0022** | **44.6–45.0** |
| store subset at 0000 / 0077 (published); owner-stripping; missing / unloadable helper | 0.6 + 0.7 + 0.2 + 0.2 + 0.2 |

**The umask re-runs are already cheap: 2.6 s in total.** The cost is the two **unpartitioned full
suites** (127 s, 87% of the gate).

**`verify-application-adapters` (110–115 s)**, derived from the step start stamps in
`receipts/verification.json` (the receipt records no durations; finding F-5):

| Step | Seconds |
|---|---|
| xaml-token-lint | 0.2 |
| hermetic `dotnet build` (Debug) | 11.6–14.6 |
| Cli tests | 1.2–1.4 |
| **full Desktop suite, Debug** | **80.0–81.9** |
| native startup smoke | 1.5–1.7 |
| 4 self-contained publishes (osx-arm64 and win-x64 × Desktop and Cli) + 2 packages | 15.9–16.0 |

The adapters gate reads only two kinds of line from that 81 s Desktop run. `THEME-RESOURCE` comes
from the 2.7 s in-process prefix. `THEME-ROW` comes from `--shell-window`
(`contrast_checks` and `applied_theme_checks` in `tools/verify-application-adapters.py`; the
`THEME-ROW` emitter is in `ShellWindowTests.cs`). The other 12 modes re-run what the fast ring ran
in Release.

### 1.5 Duplicate work before a main move (Verified from source and receipts)

| Suite | Fast ring (join) | Readiness | Times run per main move |
|---|---|---|---|
| Core, full | Release, 2 parts | Debug full (82 s) + published Release full (45 s) | **3** |
| Desktop, full | Release, 14 modes | Debug full (81 s) | **2** |
| xaml-token-lint | join `checks` | adapters gate | 2 (0.2 s; not worth a change) |

`grep` finds **0** lines of `Debug.Assert`, `#if DEBUG` or `[Conditional("DEBUG")]` in `src/` or
`tests/`. So the Debug runs exercise no code that the Release runs skip. What they add is the
Debug JIT. Optimisation differences in floating point are the residual risk (Inferred, small;
.NET does not contract to FMA on its own).

### 1.6 Agent cost: how often the rings run

`docs/proof/test-cost/ring-runs.py` counted Bash tool calls in this repo's Claude transcripts,
subagents included:

| Command | Runs | Transcripts | Median per transcript | p90 | Max |
|---|---|---|---|---|---|
| `tools/run-tests.sh` | 359 | 85 | 3 | 8 | 54 |
| `tools/run-readiness.py` (not `--check`) | 95 | 35 | 2 | 4 | 16 |
| narrowed run (`CFD_TEST_ONLY`, one mode, one project) | 348 | 42 | 5 | 17 | 23 |

Caveats. The count is a pattern match, so a `cat tools/run-tests.sh` also counts (an upper
bound). Copilot, Grok and agy sessions are **not recorded** here (a lower bound). Even so, at the
measured costs, readiness (95 × 310 s ≈ **8.2 h**) has cost agents more wall time than the fast
ring (359 × 53 s ≈ 5.3 h). The tokens an agent spends reading ring output are **not recorded**: the
ring prints about 10 lines when green, so they are probably small. The re-runs are the cost.

## 2. Levers

The saving is per run of the ring it affects. **Measured** means the removed step was timed (§1).
**Inferred** means a model on measured inputs (the slot replay, the part model, or overlap
arithmetic). Every lever is proven the same way: the same sorted `PASS` multiset hash before and
after, exit 0, and the wall time on a quiet machine *and* under load (`review-test-ci-waste` §12).

| # | Lever | Ring | Saving | Coverage risk | Cost to build |
|---|---|---|---|---|---|
| **L0** | **Inner-loop rule for agents** (§4): one check via `CFD_TEST_ONLY`, then one mode, then the full ring once at the end of a repair cycle | agent loop | **≈ 50 s per iteration** (53 s → 0.6 s for Core, 2.3 s for Desktop, measured) | None: the join still runs the full ring. Risk: an agent claims green from a subset. The join catches that | Prose in the delegation template, plus an optional `tools/run-check.sh <prefix>` that finds a check's mode (½ day) |
| **L1** | **`verify-application-core`: drop the Debug full run.** Keep the non-published store subset at 0022, 0000 and 0077 (3 × 0.6 s), and keep the published Release full run | readiness | **−80 s** (Measured: the 82 s step, minus 0.6 s for the added 0022 subset) | The full Core suite stops running under the Debug JIT anywhere. 0 Debug-conditional lines (Verified). The non-published shape still runs in full every join (Release) | Small: about 3 lines in `store_masks`. Test Architect sign-off on the Debug-JIT residual |
| **L2** | **`verify-application-adapters`: run only the theme emitters**, the in-process prefix and `--shell-window`, not all 14 modes. Better: a `--theme-rows` mode that prints the `THEME-ROW` table without the rest of the shell-window checks | readiness | **−33 s** (prefix + both shell-window parts in Debug ≈ 48 s, Inferred from the 1.8× Debug factor) **to −76 s** (with `--theme-rows`, Inferred ≈ 5 s) | The 12 other modes stop running in Debug; they still run in Release every join. Same Debug-JIT residual as L1 | Small for the first form (argv in the gate). Medium for `--theme-rows`: a test-side mode that must emit the same rows (proven by diffing them) |
| **L3** | **Partition the published full Core run** in `verify-application-core` into 3 concurrent `--part=k/3` processes, with the same PARTITION completeness check `run-tests.sh` uses | readiness | **−28 s** (Inferred: 45 s → max part 16.6 s, from the part model) | None, if the PARTITION check is carried over. Without it a check can drop out silently, as `review-test-ci-waste` §12 showed | Small–medium: parallel `run()` in the gate, and per-part receipts |
| **L4** | **Overlap the independent readiness steps**: the core gate, the adapters gate, Core `--readiness`, Desktop `--readiness` and the recounts share no scratch | readiness | **−110 s today** (Inferred: the 113 s adapters chain hides under the 146 s core chain). **About −40 to −60 s after L1–L3** (Inferred) | None in checks. **Load risk**: oversubscribed CPU once turned 7 Core checks red (`GEOMETRY-BUDGET`, `review-test-ci-waste` §12). The proof budget now counts bit-work, not wall time (comment in `DesktopChecks.Spawn`), so this is Inferred to be gone. Prove it under load before shipping | Medium: `run-readiness.py` runs one step at a time, and `run-verify-gates.py` is pack-managed. Prefer listing the two heavy gates as separate readiness steps and running independent steps concurrently in `run-readiness.py` |
| **L5** | **Desktop: start longest first, for real**, and split `--properties-cells` into 2 parts | fast ring | **−5.8 s** solo (42.7 → 36.9 s makespan), −3.9 s from the reorder alone (Inferred, from the replay calibrated to 0.1 s) | None. The PARTITION check already covers splits | Trivial: reorder one `Spawn` string list; add `--part` to one mode |
| **L6** | **Split `--status-strip` into 3 parts** (after L5) | fast ring | **−1.7 s** more (36.9 → 35.2 s, Inferred, optimistic: its 8.1 s and 5.9 s checks bound any part) | None | Trivial |
| **L7** | **Core into 3 parts** in `run-tests.sh` | fast ring | 0 s today. About −10 s on Core 2/2 (34 → about 21 s in the ring, Inferred). It counts **only after L5/L6** make Desktop ≤ 34 s | None (PARTITION check exists) | Trivial: one job entry. +1 core of load |
| **L8** | **Fast-ring receipt per tree** (HEAD, clean tree, configuration, multiset hash). The join skips the ring when the same tree already passed it in the same track | join | **−53 s per join whose tree already has a green receipt** (Measured ring cost; how often that happens is not recorded) | A lucky green on a flaky check is reused instead of re-rolled. The receipt must bind the tree hash and the inherited selectors (`CFD_TEST_ONLY`, `CFD_TEST_CONFIGURATION`) | Medium: mirror `run-readiness.py --check` |
| **L9** | **Machine-wide ring lock** (one fast ring per machine at a time; others queue and print the wait) | fast ring | Wall **53 → 63 s** and a red TEST-BUDGET under load 15 (Measured). The lock trades contention for queueing. Net saving not recorded | None | Small (`flock` on a file under the git common dir). Or record load and CPU-seconds instead (F-3) |

**Considered and not recommended** (each with its measurement):

| Lever | Why not |
|---|---|
| Cut the serial prefix | It is 2.7 s (6%) of Desktop. Overlapping it with the children saves ≤ 2.7 s, for a change to start-up order |
| More Desktop slots | 8 slots × about 1.4 cores + 2 Core parts ≈ 13 of 16 cores already. Under the measured fleet load (avg 15) more slots add contention, not speed. The 8-slot floor is 32 s; see L5/L6 for the reachable part |
| Headless window reuse | The suites deliberately use `UsePlatformDetect()`, which means native AppKit. A headless backend changes what is proven (native layout, input, theme loading). The per-window cost is 0.1–1 s a check. Not measurable without a code change: Inferred saving, high coverage risk. **Do not do this without a spike that shows the same failures are still caught** |
| Move the slow `--status-strip` checks to readiness | They are functional checks with no named reason to wait for readiness. L6 removes their wall cost without moving them |
| Suite-level test-impact selection at the join | Desktop and Cli depend on Core, so only docs- or tools-only diffs could skip anything. Mode-level selection inside Desktop needs a coverage map that does not exist. Use selection in the inner loop (L0) only; the join keeps the full ring |
| Build once and reuse it between the ring and readiness | Warm ring builds take 0–4 s. The gates' 12–15 s hermetic builds from an empty NuGet and artifacts root **are** the coverage (they prove a clean build). Saving ≤ 25 s, at the cost of that proof |

## 3. Recommended order

1. **L0 now.** No code. It has the largest per-iteration saving and needs no Test Architect ruling.
2. **L1, then L2.** −113 to −156 s of the 310 s readiness ring. Both need one Test Architect ruling
   on the same residual: the Debug JIT. Ask it once, for both.
3. **F-1 and F-2 (§5) with them.** The scratch leak grows by about 3.8 GB per readiness run (1.5 GB
   from the core gate, 2.3 GB from adapters). That is about 360 GB at the recorded 95 runs. Fix the
   hang before any change that makes runs cheaper and therefore more frequent.
4. **L3.**
5. **L4 last** among the readiness levers. Its saving shrinks after L1–L3, so re-measure first.
   Prove it at 0 and at 10 background busy loops with the same multiset.
6. **L5 + L6, then L7.** About −8 s on the fast ring's 53 s. The ring then sits about 6 s above its
   8-slot floor.
7. **L8 and L9** only after measuring how often a join re-runs a tree that already passed, and how
   often two rings overlap. Today neither number is recorded.

Expected result, all Inferred until each lever's proof run: readiness **310 → about 125 s
serial**, and about 60–70 s with L4. Fast ring **53 → about 45 s** quiet. A typical repair
iteration: **53 s → 1–3 s**.

## 4. What agents should run in their inner loop

Run the **smallest test set that can fail for your change**. First, the one check you are writing
or fixing:
`CFD_TEST_ONLY=<CheckNamePrefix> dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll`
(0.6 s). For Desktop, the same with
`CfdWorkbench.Desktop.Tests.dll --<mode>` (about 2 s; the mode is the `--…` switch in
`WorkbenchTests.cs` whose suite holds the check). Then the whole mode or Core part it lives in
(15–30 s). Build first with `dotnet build CFDWorkbench.slnx -c Release -v q` (0–2 s warm). A prefix
that matches nothing fails loudly, so a typo cannot pass. Run **`tools/run-tests.sh` once**, at
the end of the repair cycle, before you report. Do not run it after every edit. Run
`tools/run-readiness.py` **only for a main move**, never inside a repair loop. A subset green is
evidence about that subset only: never report it as the ring.

## 5. Operational findings (SRE, Adversary Mode)

| ID | Severity · confidence | Finding | Evidence | Smallest fix |
|---|---|---|---|---|
| F-1 | **Major** · Verified | The gate scratch directories are never deleted. That includes a private NuGet cache per run | `/tmp/cfd-application-core-*`: 62 dirs, 86 GB (1.6 GB each). `$TMPDIR/cfd-adapters-verify-*`: 59 dirs, 138 GB. The disk is 32% used now; when it fills, every build on the machine fails | Delete the scratch on green and keep it on red (the receipts are the debug story). Or keep the last N |
| F-2 | **Major** · Verified | The fast ring has **no timeout**. `run-tests.sh` `wait`s on each suite, and `DesktopChecks.Spawn` calls `WaitForExit()` with no limit. A hung native-window check hangs the join until the agent's own tool timeout, with no line that names the mode | Source of `run-tests.sh` and `WorkbenchTests.cs` `RunBuffered`. The gates have limits (core 180 s, adapters 600 s); the ring has none | A per-child limit (for example 120 s, about 4× the slowest measured child) that kills the child's process tree and prints `FAIL TIMEOUT <mode> after N s` |
| F-3 | **Major** · Verified | The TEST-BUDGET verdict measures fleet load, not regression. The same tree and multiset gave 53 s (exit 0) and 63 s (exit 3) | Ring 1 vs ring 2, load average 6.6–11 vs 15 | Print the load average and the child CPU-seconds on the `wall` line, so a budget red can be told from contention. Then decide on L9 |
| F-4 | Minor · Verified | There is no per-check cost on the normal path. This study needed an external timestamper | `check-cost.py` exists only because no `COST` line is emitted | The `Check` helpers print `COST <name> <ms>` on a separate line. A separate line keeps the `PASS` multiset unchanged |
| F-5 | Minor · Verified | The adapters receipt records each step's `startUtc` but no duration | `adapter-receipts.py` has to derive durations from gaps | Add `durationSeconds` to each step |
| F-6 | Minor · Verified | The readiness ring has no stated budget, so its growth from 203 s to 261 s in the gates went unflagged | Receipts across worktrees | State one after L1–L3 (for example 150 s serial) and fail like TEST-BUDGET |
| F-7 | Nit · Verified | `.tmp-tests/` collects thousands of temp dirs and `clr-debug-pipe` FIFOs per worktree | 8,312 dirs, 36 MB in one worktree | `run-tests.sh` clears directories older than the run |

## 6. How to reproduce

The scripts are in `docs/proof/test-cost/` (stdlib only):

- `stamp.py <cmd…>`: run a harness and prefix each line with seconds since launch.
- `check-cost.py <stamped>`: per-check cost, top-N and a histogram.
- `slot-sim.py <Desktop.log|stamped> [slots] [--mode=n …]`: replay Spawn's schedule as listed and
  longest-first, with optional what-if splits.
- `part-balance.py <stamped full Core run> [n…]`: the interleaved `--part` rule against a balanced split.
- `gate-receipts.py <core scratch…>`: `verify-application-core` step times.
- `adapter-receipts.py <adapters scratch…>`: `verify-application-adapters` step times.
- `ring-runs.py <claude projects dir>`: ring and readiness runs per transcript.

## 7. Residual risk

- **One load level per sample.** The machine was shared, with load average 6.6–15. The quiet-machine
  numbers in the brief (49–52 s) were not reproduced here. All savings are relative, from the same
  runs. Re-measure each lever before and after it ships.
- **The Debug-JIT residual** of L1 and L2 is Inferred small. It is not measured. The Test Architect
  rules on it.
- **L4's load risk** is Inferred to be gone (the bit-work proof budget). It is not measured. Prove it
  under load.
- **The agent-run counts** cover Claude transcripts only, and come from a pattern match, so they
  are approximate.

## 8. Shipped levers: measured results (2026-10-04, branch `perf/test-tooling`)

Operator approval 2026-10-04; Test Architect APPROVED-WITH-CONDITIONS on L1/L2 (C1, C2). Every number
below is from a run on this machine, with the load average (`uptime`) at start and end. The machine was
shared with three other tracks, so the load ran from 6 to 79 on 16 CPUs. **No check was deleted,
skipped or moved**: the fast-ring PASS multiset is the same before and after every change (1,162 lines,
1,161 distinct, sha256 prefix `d7ba74e68a688d06`; recomputed on base `5b49683`, since EDT added checks
after the study's 1,135).

### 8.1 Readiness ring

| Step | Before (base `5b49683`, load 19 → 6) | After (`a6dafd8`, load 74 → 24) |
|---|---|---|
| `verify-application-core` | 148.7 s, serial | 41.9 s, concurrent |
| `verify-application-adapters` | 152.6 s, serial | 62.1 s, concurrent |
| other verify gates + 2 recounts | 1.2 + 5.9 + 4.3 s, serial | 1.3 / 8.1 / 5.9 s, concurrent |
| Core `--readiness` | 24.5 s | 25.4 s (serial, alone) |
| Desktop `--readiness` | 11.4 s | 13.5 s (serial, alone) |
| **Total** | **348.6 s** | **101.6 s** (budget 240 s) |

Two earlier runs of the same ring after L4: 124.2 s (load 16 → 23) and 115.2 s (load 25 → 75). All three
were green, including every Core proof-budget and Desktop frame-budget check.

| Lever | Before | After | Proof |
|---|---|---|---|
| **L1** core gate: store subset (not the full suite) at 0022/0000/0077 in Debug | 148.6 s (step sum) | 73.8 s | Commit `f010563`. C1 below |
| **L2** adapters gate: `--theme-evidence` (the in-process prefix + `--shell-window` 2 parts) in Debug | 152.3 s; Desktop step 118.6 s | 60.5 s; Desktop step 24.4 s | Commit `cbedde9`. C2 below. Native startup smoke still runs the Debug Desktop executable |
| **L3** published full Core run as 3 concurrent `--part=k/3` | 73.8 s step sum; full step 45.5 s | gate wall 41 s; parts 16.2 / 17.1 / 17.2 s | Commit `19164fe`. The 3 parts pass the same 586 checks as the old full run (hash `4eb85f786d3c99b1`). PARTITION check shown red once with a duplicated part (586 PASS lines, 391 distinct) |
| **L4** gates + recounts in one concurrent group; `--readiness` runs serial and alone | 348.6 s | 101.6–124.2 s | Commits `4d91906`, `25c5666`. Green 3 of 3 runs at load 16–75. TEST-RING accepts a `--skip` only when each skipped gate is its own readiness step; red when the core gate step is dropped |

L4 keeps the two `--readiness` steps out of the group on purpose: they hold wall-time frame budgets, and
`dotnet run` builds into `src/`/`tests/` `bin`, which the adapters gate proves it did not change.

### 8.2 Fast ring (L5, L6)

| | Desktop | Wall | Load | Slowest child |
|---|---|---|---|---|
| Before (3 runs) | 52 / 59 / 56 s | 53 / 65 / 58 s | 10–25 | status-strip 1/2, 42.4–49.5 s |
| After L5/L6 (2 runs) | 53 / 48 s | 55 / 49 s | 62–79 | properties-view 1/2, 31.5–34.9 s |

Commit `a6dafd8`: longest first by measured SUITE-TIME, `--status-strip` in 3 parts, `--properties-cells`
in 2. **Not a clean A/B** (the load rose 3–6× between the runs): the pole fell by about 15 s and the
Desktop time held or fell under far more load. Child CPU-seconds were flat (549 → 542 / 525 s). The
quiet-machine saving is still Inferred (§2: −5.8 to −7.5 s).

### 8.3 Safety fixes

| Finding | Fix | Evidence |
|---|---|---|
| F-1 scratch never deleted | Both gates delete the build, package cache and publishes on green and keep `receipts/`; a red run keeps everything and prints `SCRATCH kept: <path>` | Green: −1.79 GB (core) and −2.74 GB (adapters) per run, 144–516 KB left. Red (the PARTITION red run): kept |
| F-2 no timeout | `DesktopChecks.RunBuffered` kills a child's process tree after 120 s (`CFD_TEST_CHILD_TIMEOUT_SECONDS`) and prints `FAIL TIMEOUT <mode> after N s`; readiness kills a step's process group after 1,200 s | Red at a 3 s limit: both `--shell-window` parts `exit 124`, no orphaned children. Readiness self-test kills a hung step |
| F-3 budget vs load | The `wall` line prints child CPU-seconds and load at start and end: `wall 58 s (budget 60 s) cpu 549 s load 16.46 -> 25.32` | Every ring above |
| F-5 no step durations | `durationSeconds` on every adapters receipt step | `receipts/verification.json` |
| F-6 no readiness budget | 240 s (about 2× the measured 102–124 s under load). Over it: `READINESS-BUDGET`, exit 3, and `--check` refuses the receipt; `CFD_READINESS_BUDGET_SECONDS` overrides | Self-test: an over-budget green ring exits 3 and fails `--check` |

**Test Architect conditions.**

- **C1.** `tools/check-debug-parity.py`, run by `tools/check-docs.py`, fails on `#if`/`#elif DEBUG`,
  `Debug.Assert`, `[Conditional("DEBUG")]` in `src/`/`tests/` C#, and a per-configuration `Optimize`,
  `CheckForOverflowUnderflow` or `DefineConstants` in any csproj/props/targets under `src/`, `tests/` or the
  root. It carries the stdio guard and an in-process self-test of each pattern. Red once on planted lines
  (`#if DEBUG` in `Identity.cs`, a Release-only `CheckForOverflowUnderflow` in the Core tests csproj, and a
  `Debug.Assert` through `check-docs.py`, exit 1); green on the tree.
- **C2.** The reduced Debug run emits the same 325 `THEME-*` lines as the full run, byte for byte: 42
  `THEME-RESOURCE`, 1 `THEME-SHADOW-MUTATION`, 1 `THEME-RESOURCE-CHECK`, 6 `THEME-FOCUS-RESOURCE`, 274
  `THEME-ROW`, 1 `THEME-SHELL-CHECK`. Run without the serial prefix (`--shell-window` alone), the gate's
  parser is red: `loaded-XAML theme resource evidence missing, duplicated, or stale`. No `--theme-rows`
  mode was added.

### 8.4 Not done, and why

- **Optional COST lines (F-4).** They belong in the `Check` helpers. This track owned only the Spawn list,
  the part plumbing and the per-child timeout in `DesktopChecks`, and not the Core harness. Open as a
  seam request to whichever track owns the harnesses.
- **The Core fast-ring parts have no timeout.** F-2 was fixed for the Desktop children (the native-window
  risk). A hung Core part still waits in `run-tests.sh` until the agent's tool timeout.
- **L0** needed nothing built (already in the briefs). **L7–L9** were not approved for this track.

## 9. Millisecond clocks and the cost checker (2026-10-05, track B1, branch `feature/ring-b1-rng`)

**Shipped.** `tools/run-tests.sh` writes `<name>.ms` per harness and `wall.ms` from one millisecond wall clock (C-1) and
prints them. `tools/check-test-costs.py` enforces C-2…C-6 (design `area3-analysis.md` §13.4) and has a `--self-test`
that plants each failing input of the table. `run-tests.sh` calls it after the wait loop, and `join.json` runs it again
after `run-tests.sh`. Ring: every join. Cost: under 0.1 s. The fast ring's `PASS` set is unchanged (1,442 lines, before
and after; `docs/proof/ring-b1/pass-before.txt`, `pass-after.txt`).

**OD-2 is not met, and the numbers are load-polluted.** Three runs after the change, with other agents' work on this
laptop (1-minute load 16 → 70, 16 logical CPUs). Desktop is the long pole in all three.

| Run | Desktop | wall | Core 1/2 | Analysis | C-3 (≤ 50 s) | C-4 (≤ 43 s) |
|---|---|---|---|---|---|---|
| 1 | 54.5 s | 56.8 s | 44.8 s | 4.9 s | red | red |
| 2 | 52.1 s | 53.5 s | 39.9 s | 4.0 s | red | red |
| 3 | 53.2 s | 54.4 s | 41.5 s | 4.1 s | red | red |

The idle baseline (`docs/proof/ring-oct05/baseline-2026-10-05.csv`) was wall 50–57 s, Desktop 50–51 s. Total CPU per
run is 560–600 s; over 16 cores that is a floor of 35–37 s for the whole ring, and the Desktop children (8 slots, about
1.4 cores each) plus two Core parts already fill the machine. The Desktop child times sum to about 400 s under load.
Reordering the slots cannot cut CPU-seconds, so **`WorkbenchTests.cs` was not touched** and no check moved or loosened.
Meeting OD-2 needs fewer CPU-seconds (Track B2) or the fallback (b) of OD-2 (restate C-3/C-4 as deltas, which needs a
ruling). Until then every `run-tests.sh` exits 1 with C-3 and C-4 named. Analysis at 4.0–4.9 s sits close to its
5 s limit (C-2); the slowest check is `F6_ObservedOrder` at 478 ms (limit 1,500 ms).

### 9.1 Ruling 84: OD-2 settled as deltas, load-gated (2026-10-05)

Ruling 84 (DR-RING-1, the Ruling 67 fallback (b) in Ruling 81's load-gated shape) replaces the §13.4 absolute C-3 and
C-4 limits with limits on the measured quiet base. C-3 measures `wall.ms - build.ms` (net ring time), base 52 s, limit
54,000 ms. C-4 measures `Desktop.ms`, base 51 s (quiet max), limit 53,000 ms. Both fail only when the 1-minute load at
ring end is at or below 24; above 24, or not recorded, the checker prints `COST-MISS <rule> <ms> load <value>` in the
ring log and does not fail. C-2, C-5 and C-6 stay strict, and the 60 s TEST-BUDGET stays the ceiling. `run-tests.sh`
writes `build.ms` and reads the end load before it calls the checker. The bases and threshold are constants in
`tools/check-test-costs.py` and change only from a new recorded 3-run quiet baseline. The standalone join entry has no
end load, so it reports C-3/C-4 as COST-MISS only; the enforcing call is the one inside `run-tests.sh`.

**When Track B2 lands, re-measure quiet. If Desktop <= 43 s and wall <= 50 s, revert to the §13.4 absolute limits and
remove the deltas.**
