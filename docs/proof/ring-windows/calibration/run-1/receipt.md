---
id: proof-ring-windows-calibration-run-1
title: "Ruling 168 Windows calibration run 1 (incomplete)"
type: proof-pack
status: incomplete
owner: "@win-r166-ring-baseline"
tags: [windows, ruling-168, ruling-166, test-ring, evidence]
links:
  - {to: proof-ring-windows-calibration-attempt-1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: "2026-11-09"
summary: >-
  The first host-keyed Windows calibration attempt was terminated at the 300 s deadline while Desktop remained at STAGE
  spawn. It is incomplete evidence only. The requested held-reader PASS appeared in Core.part3of3 rather than Core.part2of3,
  and Core.part3of3 also recorded NativeFailure Win32 32 for WindowsNative_Replace_HeldReaderKeepsOldImage.
---

# Ruling 168 Windows calibration run 1

**Goal:** execute calibration run 1 with the Mac-provided host key and six-CPU affinity.
**Done when:** preserve the run's exact output and per-suite logs, classification, partition, DRIFT, timing, load, affinity, and
  timeout evidence; retain the capture identity after commit.
**Not in scope:** a baseline row, test or product repair, or another ring after this mismatch.
**Tier:** T1. **Fan-out:** 0.

## Result and disposition

This is an incomplete ring, not a baseline measurement. The measurement file's UTC envelope started at
**2026-10-09T00:26:19.2459358Z** and ended at **2026-10-09T00:31:25.2064630Z**, a span of **305,960.528 ms**. This envelope
includes setup and the pre-run samples, and ends at process-tree termination before post-run samples and evidence copying.
Against a 300,000 ms total outer ceiling,
it exceeded by **5,960.528 ms**; the run does not meet the ceiling.

The separate monotonic `wall_ms=302154` clock started immediately before `Start-Process` and stopped after the timeout
termination/wait branch. That grain is **2,154 ms over 300,000 ms**. `WaitForExit(300000)` began after setup, so this value
does not represent a 300-second absolute deadline from capture entry. Desktop remained at `STAGE spawn`; the exact
`taskkill` output is retained in `timeout-taskkill.txt`. No final run-tests wall, budget, or host-cost result was emitted.
The partial run does not contribute a baseline row, and `docs/proof/ring-windows/baseline.csv` remains absent.

The run used tested HEAD `a248b264e8f9a911cbfd22322b74f737ba203f84`, `CFD_RING_HOST=pc-win`, and .NET SDK **10.0.203** from
`%USERPROFILE%\.dotnet`. The retained `process-affinity.txt` records Git Bash PID 20296, conhost PID 28100, and bash PID 19364
at affinity `0x3F`; bash.exe PID 1148 is recorded as unreadable. This file does not retain samples for the dotnet or test
worker processes. A later live process-tree query reported dotnet, Core test and Desktop test descendants at `0x3F`; that
observation was not retained with a timestamp and is **Reported, not Verified**. L3 was reported by the coordinator as active
on six WSL ranks; continuous residency was not independently sampled. The capture measured load **11.74** at start and
**17.70** after termination. Windows CPU samples were **37.3%, 32.5%, 34.4%** before and **59.9%, 46.9%, 41.8%** after the ring.

## Ruling 168 test mismatch

The required `PASS Library_UserFileHeldReader_SurvivesReplaceByRename` is in `Core.part3of3.log`, not `Core.part2of3.log`.
`Core.part3of3.log` also records `FAIL WindowsNative_Replace_HeldReaderKeepsOldImage NativeFailure: NTSTATUS=0xc0000043;
IO_STATUS=0x00000000; Win32=32`. The WindowsNative failure is classified as an expected historical probe by the manifest;
the wrong partition for the required PASS remains a Ruling 168 mismatch and is a decision request. No repair or retry was made.

The completed Core partitions reported:

| Suite | PASS | Failures classified as expected | Unexpected |
|---|---:|---:|---:|
| Core.part1of3 | 228 | 8 | 0 |
| Core.part2of3 | 225 | 11 | 0 |
| Core.part3of3 | 226 | 9 | 0 |

Exact output from `tools/check-expected-failures.py --log <suite> --host windows` is retained under `classifications/`.
The other classifier summaries are Analysis.part1of2 0 expected/0 unexpected, Analysis.part2of2 0/1
(`Section_WingRun_PanelValuesAtEveryStation`), Cli 2/0, and Desktop 2/0. Desktop classification is only a snapshot of its
partial log: the process was killed before Desktop completed, so 2/0 is not a completed-suite result.

## Cross-OS drift

All five observed `DRIFT` lines are retained in `drift-lines.txt`:

```text
DRIFT lambda-fit max_abs=0 limit=1e-06
DRIFT tangent-angle max_abs=0 limit=1e-06
DRIFT dat-rotation max_abs=0 limit=1e-06
DRIFT handle-polar-section max_abs=0 limit=1e-06
DRIFT handle-polar-target max_abs=0 limit=1e-06
```

## Capture scope and timeout control

`console.stdout.txt`, `console.stderr.txt`, `tmp-tests/`, `classifications/`, `drift-lines.txt`, `process-affinity.txt`,
`timeout-taskkill.txt` and `measurement.txt` retain the run output. `tmp-tests/` is an exact copy of the shared scratch
directory after termination. It contains **33 files older than this run**, including prior fixture data and compiler cache
files; `run-tests.sh` clears `*.log`, `*.seconds` and `*.ms` at launch, but does not clear every auxiliary scratch file.
The current run's seven suite logs and completed job timings are fresh; older auxiliary files are not treated as run output.

The **2,154 ms stopwatch overage** and **5,960.528 ms UTC-envelope overage** exposed `CAPTURE-DEADLINE-DRIFT`: a relative
wait began after setup and final process-tree termination was unbounded. The capture helper now starts one monotonic deadline
before setup, passes only the remaining time to the harness wait, requests process-tree termination at expiry, and does not
request a post-deadline process wait. Its deterministic self-test verifies remaining-time arithmetic before, at, and after
the deadline. `verify-capture-deadline.ps1` proves the self-test rejects a hard-coded `WaitForExit(300000)` mutation and a
disabled total-envelope check; its red/green output is retained in `calibration/deadline-self-test.txt`. The helper was
corrected after this run; captured run-1 measurements remain unchanged. The total UTC envelope above remains the ceiling
result for this run. No ring was rerun.

## Coordination decision request

The PC-to-Mac decision request is recorded on coordination branch `win/coord-w0-w5`, commit
`d9fb43ba04fcd83facd44d57c3df5aff62306ec7`, in `docs/coordination/xmsg.jsonl` as message
`20261009T003204-pc-562384600` (ref: `Ruling 168; calibration run 1`). Its text reports the timeout, the PASS partitioning
into Core.part3of3 rather than Core.part2of3, and asks Mac to rule on the mismatch and next calibration action. This reference
is branch evidence only; the commit is not claimed to be on main. No Mac response to that request is included here.

`capture-manifest.json` binds each committed evidence blob by SHA-256 and byte count. `verify-captures.py --self-test` and
`tools/check-proof-pii.py` are the final integrity and privacy checks for this receipt.
