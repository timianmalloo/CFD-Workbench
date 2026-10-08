---
id: proof-rdh-readiness
title: "RDH: readiness headroom and the join's duplicate cost check"
type: proof-pack
status: active
owner: "@trk-rdh"
phase: implementation
tags: [readiness, test-cost, ring, concurrency]
links:
  - { to: plan-test-cost, rel: relates-to }
summary: "Item 1: join-ring.sh's second check-test-costs call was a weaker duplicate and is removed; the Ruling 89 guard now requires run-tests.sh to run it. Item 2: one Release build then the three --readiness suites as one concurrent group takes readiness from 201-203 s to 137-143 s with equal PASS counts; merging the python gates in as well (79 s) trips an advisory Desktop freeze miss and was not shipped."
review-by: 2026-11-07
---

# RDH: readiness headroom and the join's duplicate cost check

Measured 2026-10-07 on one quiet Mac, one worktree, `tools/dispatch-gate.py --running 0` (GO) before every run.
Raw receipts, stdout and group logs are in the track's scratchpad; every number below was read from them.

## Item 1: the duplicate cost check

Claim: `tools/join-ring.sh` ran `check-test-costs.py` after `tools/run-tests.sh`, and that second call is a weaker
duplicate of the call inside `run-tests.sh` (lines 153 and 158).

Rules enforced by `tools/check-test-costs.py` and by each call:

| Rule | Call in run-tests.sh (`--dir --jobs --load --load-source --host`) | Call in join-ring.sh (no flags) |
|---|---|---|
| C-6 missing `.ms`, missing `.log`, PASS without COST | fails | fails, on the same files: `--dir` default is `.tmp-tests` (run-tests.sh `scratch`) and the default jobs equal run-tests.sh `jobs` names (7 jobs) |
| C-2, C-3, C-4, C-5 | fail at a quiet end load, else COST-MISS | load `not-recorded`: always COST-MISS |
| TEST-BUDGET | second call at line 158 | not run |

So the brief's "can never fail" is not exact: the no-flag call can fail on C-6. But the first call checks the same
C-6 conditions on the same files, so the second call adds no rule. It was removed.

Guard (`tools/check-docs.py`, `join_ring_problems`): `join-ring.sh` must run `tools/run-tests.sh`, and
`tools/run-tests.sh` must run `tools/check-test-costs.py` in a non-comment line. The old guard only grepped the
wrapper for the cost script, so it passed a `run-tests.sh` copy with no cost call. The new guard is stronger: it
still requires the cost check, at the place that has the load.

Red first (`docs/proof/rdh/plant-check.py`, scratch-copy style: copies `join-ring.sh` and a `run-tests.sh` with and
without the cost call into a temp root and calls the guard):

- old guard: `red.txt`: clean copy `[]`, planted copy `[]`, exit 1 (the planted copy was not caught).
- new guard: `green.txt`: clean copy `[]`, planted copy `['tools/run-tests.sh does not run tools/check-test-costs.py']`, exit 0.
- `tools/join-ring.sh --self-test`: `join-ring-selftest.txt`, `SELFTEST 11/11 cases`, exit 0.

## Item 2: readiness headroom

### Why the suites ran serially (checked)

- `git log -S` on `docs/coordination/join.json`: the readiness key came with `e1248c72`; the Analysis step with
  `a2e6ea46`.
- `docs/plans/test-cost.md` section 8.1 (L4) gave two reasons: the `--readiness` steps hold wall-time frame budgets,
  and `dotnet run` builds into `src/`/`tests/` `bin`, which the adapters gate proves it did not change.
- Shared scratch: the suites' temp files are GUID-named under `Path.GetTempPath()` (grep of `tests/`), and the fast ring
  (`tools/run-tests.sh`) already runs the same suites concurrently with `--no-build`. Scratch is not the barrier.
  The build race and the wall-time budgets are.

### Shapes measured

- Current: python group, then three serial `dotnet run -c Release`.
- A (candidate, shipped): python group, `dotnet build CFDWorkbench.slnx -c Release`, then the three suites as one
  group with `dotnet run --no-build`. No `run-readiness.py` change was needed.
- B (exploratory): build first, then the python gates and the three suites all in one group.

### Runs (seconds; load is the 1-minute average at start -> end)

| Run | Total | Load | Core gate | Adapters gate | Build | Core suite | Desktop suite | Analysis suite | Result |
|---|---|---|---|---|---|---|---|---|---|
| cur1 | 203.3 | 2.51 -> 2.67 | 48.1 | 69.8 | in Core run | 42.5 | 67.1 | 22.3 | green |
| cur2 | 201.2 | 2.61 -> 2.80 | 50.5 | 73.8 | in Core run | 40.6 | 63.4 | 21.8 | green |
| cur3 | 201.3 | 2.80 -> 3.02 | 51.2 | 73.1 | in Core run | 40.9 | 64.0 | 21.6 | green |
| A1 | 142.6 | 2.84 -> 11.28 | 53.8 | 72.3 | 4.4 | 40.7 | 64.5 | 21.7 | green |
| A2 | 139.8 | 11.28 -> 5.27 | 52.4 | 72.9 | 1.5 | 40.7 | 64.0 | 21.5 | green |
| A3 | 137.2 | 5.27 -> 4.56 | 51.8 | 70.4 | 1.5 | 40.7 | 63.9 | 21.5 | green |
| B1 | 79.8 | 2.87 -> 9.59 | 54.8 | 74.2 | 4.4 | 45.7 | 69.4 | 24.5 | green |
| B2 | 78.4 | 8.91 -> 13.28 | - | - | - | - | - | - | green |
| B3 | 78.9 | 13.28 -> 10.76 | - | - | - | - | - | - | green |

Step seconds in a group run from the group start. A2, B2 and B3 started warm (the previous run's load); all three
gate calls printed GO. The "Core gate" column is `verify-application-core.py`.

### Coverage (PASS lines per suite, same in all 9 runs)

| Suite | PASS | FAIL | RESULT line | Advisory miss |
|---|---|---|---|---|
| Core `--readiness` | 15 | 0 | `RESULT failures=0` | `SectionStepApply` 58.9 ms vs 5 ms, every run of every shape (not new) |
| Desktop `--readiness` | 71 | 0 | none printed | none in current or A; `SectionReleaseFreeze` in B1, B2, B3 |
| Analysis `--readiness` | 37 | 0 | `RESULT failures=0` | none |
| Core gate (`verify-application-core.py`) | 1154 | 0 | 11 x `RESULT failures=0` | none |

Equal across current, A and B. No test failed in any run, and the Section Editor checks did not fail in A.

Desktop frame budgets (p95, ms; target 100 and 250): drag 16.3 / 16.4 / 17.1 (current), 15.5 / 15.1 / 17.7 (A),
19.7 / 17.8 / 20.1 (B); commit 67.5 / 68.8 / 69.6 (current), 70.5 / 70.3 / 70.6 (A), 68.3 / 65.7 / 80.3 (B).

### Decision

Ship A. Readiness falls from 201-203 s (mean 201.9) to 137-143 s (mean 139.9): about 62 s, 31 percent, with equal
coverage, no failure and no new advisory miss in 3 of 3 runs. Headroom against the 240 s budget grows from about 38 s
to about 100 s.

Do not ship B. It is 60 s faster still (79 s), but the Desktop `SectionReleaseFreeze` check ("UI thread blocked past
50 ms") printed a READINESS-MISS in 3 of 3 runs and Desktop took 69 s against 63-64 s. That is the
SECTION-EDITOR-LOAD-FLAKE shape: a wall-time check that reads the load. The miss is advisory today, so B stays green,
but it turns a clean wall-time signal into noise, and it was not retried away. The python gates stay in their own
group before the build. To revisit B, first make that check load-independent (a CPU-work measure, not wall time).

Changed: `docs/coordination/join.json` (readiness key only), the L4 note in `docs/plans/test-cost.md`. Unchanged:
`STEP_TIMEOUT`, the budget, the pre-push hook, the receipt (still bound to HEAD).

Observation, not fixed (outside the owned paths' intent): `run-readiness.py` does not clear
`.tmp-tests/readiness/` between runs, so a log from an earlier shape with a higher step index can remain and be read
by mistake. Read logs by the index of the current `join.json`.
