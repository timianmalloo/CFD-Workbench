---
id: receipt-win-gpu-b1-r199
title: "Windows GPU B1 blocked receipt"
type: proof-pack
status: blocked
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, gpu, cuda, blocker, ruling-199, ruling-200]
links:
  - { to: proof-win-gpu-b1-r199, rel: relates-to }
  - { to: proof-win-l3-observer, rel: depends-on }
  - { to: review-pr-30, rel: implements }
review-by: "2026-11-10"
summary: >-
  Two B1 attempts stopped before verified probe ownership; the live observer completed without a five-percent slowdown.
---

# Windows GPU B1 blocked receipt

**Verdict: BLOCKED.** B1 did not capture an accepted machine/toolchain inventory. B2 and F1 remain closed. The track
stopped at the two-live-attempt repair cap.

## Authority and scope

Ruling 199 authorized one read-only inventory under one CPU, nice 10, a live observer, and a ten-minute workload
ceiling. Ruling 200 authorized B2 only after B1. No CUDA install, apt update, `wmake`, OpenFOAM build, solver launch,
L3 mutation, disposable distro creation, or B2 action occurred in this track.

The execution source was committed as `c3635420`. The attempt-1 UTC repair and retained evidence were committed as
`a75e51c7`. Commit `c4e32730` retained the append-only observer's then-current attempt-2 state after a long gate's shell
tail continued beyond the harness return; the final evidence below supersedes that intermediate state without rewriting
it.

## Attempts

| Attempt | Fresh baseline | Workload | Result |
|---|---:|---:|---|
| `pc-b1-r199-20261010a` | 658,661 ms; rows 9–10 | Not started | PowerShell JSON conversion changed the typed UTC instant into local wall text. The −25,201-second false age failed closed before probe launch. Evidence is retained in `attempt-1-host-date-parse/`. |
| `pc-b1-r199-20261010b` | 659,187 ms; rows 11–12 | 354 ms launcher lifetime | The `setsid` launcher exited 0 before ownership acknowledgement. The ownership check was not reached. Probe stdout/stderr are empty and `.source-cache` was not created. These facts do not prove Linux-child absence. |

Attempt 2 recorded script hashes before its first child. `capture.json` is the canonical attempt-2 wrapper record. The
probe result is not accepted because ownership and termination were not established.

## Live observer result

The attempt-2 observer completed all three requested samples and stopped with exit 0. Rows 11–12 formed the fresh
baseline. Row 13 produced:

- UTC rate: 0.7203647416 iterations/s; change from baseline: **+0.303951 %**.
- `/proc/uptime` rate: 0.7894342388 iterations/s; change from baseline: **+0.004996 %**.
- UTC/monotonic interval ratio: **1.0958812851**.
- Guard verdict: **continue**; the five-percent threshold did not fire.

This is observer and guard evidence only. It is not a B1 workload-impact measurement because verified B1 ownership was
never established.

`prefix-binding.json` binds the exact first 13 complete rows: 4,078 bytes, SHA-256
`8eee025939a9fcb1d3472c64d137ca8210855cfdf63c15aef0f60d655d744d6a`. The retained
`samples-prefix.snapshot.jsonl` equaled the live store prefix when bound.

## Missing B1 outputs and B2 stops

The following remain **Not recorded**: driver CUDA ceiling, Linux and Windows capacity decision, installed `wmake` and
v2512 development headers, configured CUDA apt candidates, retained and verified NVIDIA metadata, PETSc CUDA contract,
and complete package versions/sizes/hashes/licenses/dependencies. The prepared resolver was never accepted as executed;
it would produce a non-installable inventory until all blockers were resolved.

B2 cannot start because B1 is incomplete, package pins are absent, capacity and driver compatibility are unmeasured,
and Linux-child absence for attempt 2 is unproven. F1 remains closed by Ruling 200.

## Residual and smallest repair

Verified: the Windows launcher exited 0 before ownership acknowledgement, no ownership check ran, and no cancellation or
termination was recorded. Flagged: a Linux child may have survived; artifact absence cannot prove otherwise. Inferred:
util-linux `setsid` forked because the `setsid` process itself was already a process-group leader and the parent returned immediately.
That matches the observation but was not measured against the installed binary.

The smallest next repair is to add `setsid --wait` to the executable and recorded argv, request cancellation on every
pre-acknowledgement failure even when the launcher has exited, and establish attempt-specific residual-process evidence
before another launch. A third live attempt requires operator authorization beyond this receipt.

## Authorized continuation

Ruling 201 authorized a cap-free launcher teardown precheck and attempt 3. The operator then explicitly authorized
attempts 3, 4, and 5 if needed. The attempt-2 artifacts are preserved under `attempt-2-setsid-fork/`. This blocked
receipt remains the historical attempt-2 record until the continuation writes its measured result; B2 and F1 remain
closed.
