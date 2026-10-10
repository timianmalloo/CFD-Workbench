---
id: receipt-win-gpu-b1-r199
title: "Windows GPU B1 inventory receipt"
type: proof-pack
status: complete
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, gpu, cuda, blocker, ruling-199, ruling-200]
links:
  - { to: proof-win-gpu-b1-r199, rel: relates-to }
  - { to: proof-win-l3-observer, rel: depends-on }
  - { to: review-pr-30, rel: implements }
review-by: "2026-11-10"
summary: >-
  Attempt 3 completed the bounded B1 inventory; B2 remains blocked on an installable CUDA 13.2 recipe.
---

# Windows GPU B1 inventory receipt

**Verdict: B1 COMPLETE; B2 BLOCKED.** Attempt 3 captured the accepted machine/toolchain inventory. The Astra owner
accepted the evidence and requires no further B1 capture. B2 remains blocked until the CUDA 13.2 inventory becomes an
installable pinned recipe. F1 remains closed.

## Authority and scope

Ruling 199 authorized one read-only inventory under one CPU, nice 10, a live observer, and a ten-minute workload
ceiling. Ruling 200 authorized B2 only after B1. Ruling 201 authorized the measured launcher selection and attempt 3;
the operator later authorized attempts 3 through 5 if needed. No CUDA install, apt update, `wmake`, OpenFOAM build,
solver launch, L3 mutation, disposable distro creation, or B2 action occurred.

The original execution source was committed as `c3635420`. The attempt-1 UTC repair was committed as `a75e51c7`.
Commit `c4e32730` retained the append-only observer's intermediate attempt-2 state. The Ruling 201 source and archived
attempt-2 evidence were committed as `7f83acd5`; the actual-candidate precheck repair as `f6630a79`; and the accepted
precheck plus LF controls as `4cc1afd9`. Attempt 3 recorded the execution-source hashes in `capture.json`, including
capture `9aef0919…13c0`, probe `97d4db60…0786`, residual check `09653b70…ff2`, and precheck artifact
`f845ed99…3c1d`.

## Attempts

| Attempt | Fresh baseline | Workload | Result |
|---|---:|---:|---|
| `pc-b1-r199-20261010a` | 658,661 ms; rows 9–10 | Not started | PowerShell JSON conversion changed the typed UTC instant into local wall text. The −25,201-second false age failed closed before probe launch. Evidence is retained in `attempt-1-host-date-parse/`. |
| `pc-b1-r199-20261010b` | 659,187 ms; rows 11–12 | 354 ms launcher lifetime | The `setsid` launcher exited 0 before ownership acknowledgement. The ownership check was not reached. Probe stdout/stderr are empty and `.source-cache` was not created. These facts do not prove Linux-child absence. |
| `pc-b1-r199-20261010c` | 659,053 ms; rows 14–15 | 8,131 ms wrapper workload window | Foreground ownership was acknowledged; the probe exited 0 after the inventory. Row 16 completed post-workload evidence. |

Attempt 2 is preserved under `attempt-2-setsid-fork/`; its later exact cmdline check found no residual process. Attempt
3's prelaunch all-attempt cmdline check found zero candidates. The probe then reported PID/PGID/SID
`44542/44542/44542`, CPU 0, nice 10; `stop-owned.sh --check` independently acknowledged that identity. The 8,131 ms
number is the wrapper workload window, not isolated compute or download time.

## Machine and toolchain result

The accepted probe recorded:

- Ubuntu `24.04.5 LTS`, x86-64.
- NVIDIA GeForce RTX 3080 Ti Laptop GPU; Windows driver `596.47`; driver-reported CUDA ceiling `13.2`; compute
  capability `8.6`.
- Linux root free bytes: `1,022,443,405,312`; Windows C free bytes: `1,842,515,783,680`.
- OpenFOAM activation succeeded at `/usr/lib/openfoam/openfoam2512`; installed `openfoam2512` and
  `openfoam2512-common` are `2512.0-2`.
- `wmake` was absent. The probed `IOstreams.H`, `fvCFD.H`, and `modules/external-solver/README.md` paths were absent.
  `openfoam2512-dev` was not installed and its configured candidate was `2512.0-2`.
- Configured apt policy emitted no CUDA package candidate. The probe did not add a repository or update apt metadata.

The retained official artifacts are NVIDIA Ubuntu 24.04 `InRelease` (1,578 bytes,
`096a0acc…fe7`), `Packages.gz` (1,911,967 bytes, `acffd961…552`), PETSc `3.26.0` source (16,933,940 bytes,
`f5230023…df`), PETSc install documentation (100,173 bytes, `65e7cdb5…c8`), and the CUDA EULA (103,131 bytes,
`dae48849…a06`). They remain in the ignored local `.source-cache`; their hashes and sizes are bound by probe stdout,
while their bytes are not committed. The PETSc documentation contains the `--with-cuda` configure contract.

## Package inventories

| Root | Version | Packages | Download bytes | Installable lock |
|---|---:|---:|---:|---:|
| `cuda-toolkit-13-2` | `13.2.2-1` | 64 | 3,279,856,044 | no |
| `cuda-toolkit-13-4` | `13.4.2-1` | 65 | 3,491,944,780 | no |

Both inventories retain exact NVIDIA package versions, filenames, download sizes, SHA-256 values, and dependency text.
Both declare `installable_lock: false`. Their four blockers are: repository signature retained but unverified;
package-level license mapping incomplete; dependencies outside the NVIDIA repository unresolved and unpinned; and at
least one installed size not recorded. CUDA 13.4 is also unqualified by the measured 13.2 driver ceiling and is not an
approved install target. CUDA 13.2 is the only B2 candidate.

## Live observer result

Rows 14–15 formed the fresh baseline. Row 16 completed after the short inventory workload:

- UTC rate `0.7250755287` iterations/s; baseline change **−1.317521%**.
- `/proc/uptime` rate `0.7993738238` iterations/s; baseline change **−0.414938%**.
- UTC/monotonic interval ratio `1.1024697320`.
- Guard verdict **continue**; the five-percent threshold did not fire.

The coarse 600-second interval contains the 8.1-second inventory and does not isolate its performance effect.
`prefix-binding.json` binds the exact first 16 complete rows: 5,020 bytes, SHA-256
`b34dff967b289e383df34eca8526a34ba9923db46a68aba225c8df4a4be52ca7`. The retained snapshot equaled the live
store prefix when bound.

## Next gate

Before B2 can install anything, its committed recipe must verify the NVIDIA repository signature, map the applicable
license, pin external Ubuntu dependencies, and complete the installed-size capacity budget. The B2 distro must still be
a fresh disposable Ubuntu 24.04 instance, never `cfdw-openfoam2512`; rollback remains `wsl --unregister`; the live L3
observer and two-core/nice-10 limits remain mandatory. Missing OpenFOAM development files block later F1, not the
standalone B2 toy test. F1 remains closed.
