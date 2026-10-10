---
id: proof-win-l3-observer
title: "Windows L3 append-only observer"
type: proof-pack
status: in-progress
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, openfoam, l3, observer, ruling-197]
links:
  - { to: proof-win-r151-l3, rel: follows }
  - { to: review-pr-29, rel: governed-by }
review-by: "2026-11-10"
summary: >-
  Ruling 197 read-only observer for the active Windows L3 run, sampled every 600 seconds into an append-only JSONL file.
---

# Windows L3 append-only observer

`observe.sh` is the PC coordinator's observer from Ruling 197. It reads only UTC and monotonic clocks, `/proc/loadavg`,
the systemd unit's `ActiveState`, a bounded 262,144-byte tail of `log.simpleFoam`, and the log's size and modification
time. It does not signal a process, alter priority or affinity, change the run, or invoke a solver.

One `samples.jsonl` row is one readback of run `20261008T145043Z-win-spike04r3-g2-l3` at one UTC. `monotonic_s` is the
first `/proc/uptime` value. `log_mtime` is Unix epoch seconds. Numeric fields use the JSON string `Not recorded` when a
read does not produce a valid number. `observer_sha256` binds every row to the exact script bytes that emitted it.

The bounded foreground invocation is:

```text
wsl.exe -d cfdw-openfoam2512 -u root --exec /bin/bash /mnt/c/Projects/CFD-Workbench-win-gpu-g1-r197/docs/proof/win-l3-observer/observe.sh --count 3
```

The first sample is immediate. Later samples occur every 600 seconds. No scheduler entry is created. Consecutive-row
rate is `(iteration[n] - iteration[n-1]) / (monotonic_s[n] - monotonic_s[n-1])`. Before, during and after values are
medians aligned to the GPU step's captured UTC bounds. Missing, reset, mismatched-run or nonpositive-time intervals are
invalid rather than zero-impact observations.
