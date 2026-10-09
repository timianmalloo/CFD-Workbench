---
id: proof-ring-windows-calibration-attempt-1
title: "Ruling 168 Windows baseline calibration attempt 1 (excluded)"
type: proof-pack
status: incomplete
owner: "@win-r166-ring-baseline"
tags: [windows, ruling-168, ruling-166, ruling-156, test-ring, evidence]
links:
  - {to: proof-r163-windows-ring, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: "2026-11-09"
summary: >-
  One complete Windows ring on 1146ec8e ran with six-CPU affinity while L3 used six WSL ranks. It exceeded Ruling 168's
  300 s ceiling and used the default host name, so it is preserved as evidence only and contributes no baseline row.
---

# Ruling 168 Windows baseline calibration attempt 1

**Goal:** preserve the first complete Windows ring measurement requested by Ruling 166.
**Done when:** the full ring output, suite logs, expected-failure classifications, `DRIFT` lines and measured runtime context are
  retained and independently hash-verifiable.
**Not in scope:** treating this attempt as a baseline row, changing product/tests/tools, or starting another ring before the
  host override arrives.
**Tier:** T1. **Fan-out:** 0.

## Result and disposition

This was a complete ring result, not a launcher or capture failure. `tools/run-tests.sh` completed its Release build and all
seven harness jobs, emitted its final cost check, and exited **1** because of unexpected Windows test failures. The harness
failure is evidence and was not repaired. The ring itself reported **345,588 ms** (wall **345 s**, net **319,072 ms**) and
load **11.81 -> 12.39**. Its end load met the repository's <=24 load gate, but the run exceeded Ruling 168's 300 s outer
ceiling. It also ran before the Mac added `CFD_RING_HOST`. Ruling 174 redacts the captured machine hostname as `pc-win`
in the COST-MISS output; the measured values are unchanged. Therefore this attempt
is **excluded from calibration** and no `baseline.csv` row is written.

Ruling 168 allows L3 to run during calibration, so concurrency does not itself disqualify the measurement. The coordinator
reported L3 active at six WSL ranks; continuous process residency was not independently sampled. The capture process, Git
Bash and ring descendants used affinity mask `0x3F`.
The SDK was .NET **10.0.203** from `%USERPROFILE%\.dotnet`. Windows total-CPU samples were 35.2%, 35.1%, 32.8% before the
ring and 33.5%, 33.8%, 36.8% after it. The capture envelope was 348,517 ms; its start/end load readings were 11.97 and 5.95.
The ring's own load readings above are the values relevant to its baseline format. `run-1/measurement.txt` records both
measurements and their scope.

The run completed from `2026-10-09T00:05:08.7624322Z` to `2026-10-09T00:11:01.0860140Z`, on tested HEAD
`1146ec8e609aa0e0b2d78aae9e4771e10c98275b`. Its end load was below 24. The completed wall time exceeded 300 s, so the
outer-ceiling control was not applied to this initial attempt. The next calibration attempt must use the Mac-provided
non-personal `CFD_RING_HOST` override and enforce the 300 s ceiling. If a future run is killed at that ceiling, report the
defect signal and do not retry it in this track.

## Harness classification

The exact stdout from `tools/check-expected-failures.py --log <suite> --host windows` for each copied suite log is preserved
under `run-1/classifications/`:

| Suite | `EXPECTED-FAIL` | `UNEXPECTED` |
|---|---:|---:|
| Analysis.part1of2 | 0 | 0 |
| Analysis.part2of2 | 0 | 1 |
| Cli | 2 | 0 |
| Core.part1of3 | 8 | 0 |
| Core.part2of3 | 11 | 0 |
| Core.part3of3 | 9 | 1 |
| Desktop | 2 | 39 |

The terminal unexpected failures were `Section_WingRun_PanelValuesAtEveryStation` in Analysis part 2,
`Library_UserFileHeldReader_SurvivesReplaceByRename` in Core part 3, and 39 Desktop failures. The full Desktop names and
all failure details remain in `classifications/Desktop.txt` and `tmp-tests/Desktop.log`.

The cost checker reported **0 failures, 26 COST-MISS** at ring end load 12.39. These were host-advisory because the host
did not yet have a calibrated baseline. All `COST-MISS` lines are retained in `console.stdout.txt`.

## Cross-OS drift output

All five `DRIFT` lines are retained in `run-1/drift-lines.txt` and the corresponding Core logs:

```text
DRIFT lambda-fit max_abs=0 limit=1e-06
DRIFT tangent-angle max_abs=0 limit=1e-06
DRIFT dat-rotation max_abs=0 limit=1e-06
DRIFT handle-polar-section max_abs=0 limit=1e-06
DRIFT handle-polar-target max_abs=6.1232339957367663E-18 limit=1e-06
```

These are the observed Windows values from this run. Their presence does not make this run a calibrated baseline.

## Capture and launch controls

`run-1/console.stdout.txt`, `console.stderr.txt`, `tmp-tests/`, per-suite classifier output, measurements and drift lines are
preserved separately. `capture-manifest.json` binds their committed Git blob byte counts and SHA-256 values;
`verify-captures.py` checks them after commit.

Before the ring, one attempted tool invocation passed the descriptive intent text as a PowerShell command and failed before
creating a process. A capture-script syntax typo was also fixed before the ring started. The control is to launch only the
saved capture script and verify its PID, affinity, and first captured output before reporting a ring active. This process
check passed for this run; the failed pre-launch command had no test effects. One repair cycle was used (1/2).

No product, source, test, `tools/`, project, build, case, or solver files were changed. No second ring has been started.
