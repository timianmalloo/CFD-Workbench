---
id: proof-pvh-red-first
title: PVH red-first - bounded test pumps and a named in-flight check on a child TIMEOUT
type: proof-pack
status: draft
owner: "@trk-pvh"
tags: [pvh, desktop-tests, timeout, wait-unbounded-a]
links:
  - { to: proof-application-core, rel: relates-to }
review-by: 2026-11-12
summary: >-
  The properties-view part 2/2 hang did not reproduce in 104 runs; a pump on a never-completing task now throws a named TimeoutException at its deadline, and a child TIMEOUT line names the check it was running.
---
# PVH red-first: the intermittent `--properties-view --part=2/2` hang (2026-10-12)

Track `trk-pvh`, branch `fix/pvh-hang`, base 35dba343. Register: `WAIT-UNBOUNDED-A` in `docs/lessons/defect-classes.md`.

## Round 1: reproduction (Verified, Mac)

The hang did not reproduce. 0 hangs in 104 runs (limit 60 s per run):

| Loop | Runs | Result | Time per run |
|---|---|---|---|
| `--properties-view --part=2/2` alone, load about 9 | 30 | 30 pass | 18-21 s |
| the same, 3 concurrent loops plus 14 `yes` CPU hogs, load peak about 32 | 24 | 24 pass | 21-22 s |
| the whole Desktop harness (all 18 children in parallel), `CFD_TEST_CHILD_TIMEOUT_SECONDS=60` | 25 | 25 pass, 0 TIMEOUT | 43-57 s |

The check that runs right after `PropertiesView_TypedTwistBeyondDomain_ClampedEchoWarns` in part 2/2 is
`WingBlock_DuringThicknessDrag_MaxTcChangesBeforeRelease`, then `Copy_M12b2Outcomes_ExactStrings`.

## Round 2: defence in depth (two controls)

### 1. `Pump` never blocks forever

Defect: `Pump` looped for 8 s, then called `GetAwaiter().GetResult()` on a possibly incomplete task.

- Red (old body behind `DesktopChecks.PumpToCompletion`, no throw):
  `CFD_TEST_ONLY=Pump_NeverCompleting perl -e 'alarm 20; exec @ARGV' dotnet ... --properties-view` exit 142 (SIGALRM). The check hung.
- Green (the body throws `TimeoutException("<what> did not complete within <n> s")`): `PASS Pump_NeverCompletingTask_ThrowsNamedTimeoutAtDeadline`, 250.7 ms, exit 0.
  The same check proves a completed task returns and a faulted task rethrows its own exception.
- Sites moved to `DesktopChecks.PumpToCompletion`: `PropertiesViewTests.Pump`, `ShellWindowTests.Pump`, 7 inline loops in `ShellWindowTests`.

### 2. A child TIMEOUT names the in-flight check

- Red (the `RecordInFlight(name)` call disabled): `FAIL Timeout_SleepingChild_NamesItsInFlightCheck Exception: exit 124: FAIL TIMEOUT --inflight-probe after 3 s (CFD_TEST_CHILD_TIMEOUT_SECONDS); its process tree was killed; in-flight check: (unknown)`.
- Green: `PASS Timeout_SleepingChild_NamesItsInFlightCheck`, 3040.8 ms. A real child sleeps in `InFlightProbe_SleepsPastTheTimeout`; the parent's line ends `; in-flight check: InFlightProbe_SleepsPastTheTimeout`.
- `PASS TimeoutLine_NamesInFlightCheck_OrUnknownWithoutNote` (0.5 ms): the note, and `(unknown)` when the file is missing.
- Cost: one 20-byte file overwrite per check. A Python proxy measured 0.044 ms per overwrite (Inferred for .NET); the checks cost 1-1000 ms each. No output line is added.
