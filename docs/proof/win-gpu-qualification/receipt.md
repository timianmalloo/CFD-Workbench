---
id: proof-win-gpu-qualification
title: "Windows OpenFOAM GPU qualification: read-only Ruling 191 inventory"
type: proof-pack
status: in-progress
owner: "@win-gpu-qualification"
phase: implementation
tags: [windows, wsl, openfoam, gpu, nvidia, ruling-191]
links:
  - { to: coordination-windows-w0-w5-execution, rel: implements }
  - { to: review-pr-28, rel: relates-to }
  - { to: plan-win-openfoam-gpu-qualification, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  Ruling 191 read-only inventory: Windows and WSL see the RTX 3080 Ti, but the installed OpenFOAM v2512 runtime has no
  demonstrated GPU execution route. Installation, build and trial authorization remain closed pending Fable review.
---

# Windows OpenFOAM GPU qualification — read-only inventory

## Result

**Verified:** Windows and the `cfdw-openfoam2512` WSL2 distro both expose one NVIDIA GeForce RTX 3080 Ti Laptop GPU,
driver 596.47, 16,384 MiB, compute capability 8.6. The WSL distro exposes the NVIDIA driver libraries
`libcuda.so.1` and `libcudadebugger.so.1`.

**Verified within the declared searches:** the WSL environment has no `nvcc`, `hipcc`, `rocminfo`, `rocm-smi`,
`cmake`, `gcc`, `g++`, `clang++` or `pkg-config` on `PATH`; `/usr/local/cuda` and `/opt/rocm` do not exist. The
installed `openfoam2512` and `openfoam2512-common` packages are version `2512.0-2`. OpenFOAM activates as
`v2512`, `linux64GccDPInt32Opt`. The activated `simpleFoam` SHA-256 is
`b7bb6321cac3586878a6c51c07f3139dba651ff1f3dbdb0dd406536b27846ba0`.

The OpenFOAM tree contains `libfusedFiniteVolume.so` plus PETSc and Umpire configuration files. The dynamic linkage
reported for the installed `simpleFoam` contains none of those libraries and no CUDA library. `ldconfig` reports the
WSL NVIDIA driver libraries and `libamdhip64.so.5`; it reports no PETSc, AmgX or Umpire library under the exact retained
search. `libamdhip64-5` version `5.7.1-3` is installed, but no HIP tool, ROCm root or AMD GPU was established. The broad
package substring search also returns `whiptail`; that is a `hip` substring false positive, not an accelerator package.

**Conclusion:** GPU visibility is established. GPU acceleration of this installed OpenFOAM binary is **not
established**. No GPU trial ran. The reviewed scripts contain no package install, driver/service mutation, solver launch
or L3-control command. They did create the four captured query processes; broader live-state noninterference and L3
impact were not measured. The active L3 unit, case, processes and files were excluded from every reviewed probe.

## Authority and scope

Ruling 191 authorizes this separate read-only inventory and plan. It withholds any install, driver or system change,
new solver build, and GPU execution until Fable reviews the plan. Ruling 192 keeps L3 C2 and final acceptance open. This
work feeds no L3, W-4a, W-5, GCI or accepted numerical result.

The capture ran from branch `win/gpu-qualification-plan`, base
`b58a84e9f33fa137dc2d1907ba35616f160a0f2f`. `capture.ps1` records literal argv, working directory, UTC bounds,
monotonic elapsed milliseconds, requested 15/30-second waits, timeout state, numeric child exits and output names. All four
children exited 0 without timing out. `inspect-wsl.sh` is an LF script invoked as a file; no nested `bash -lc` command
was used.

`wsl-version.stdout.txt` and `wsl-list.stdout.txt` preserve `wsl.exe`'s UTF-16 output. Other captured output is retained
as redirected process output. `capture.json` is PowerShell UTF-8 with a BOM. Exact child byte encoding was not
independently established and is stated there. Windows and Linux clocks were not aligned; elapsed time comes from the
Windows wrapper and is not a solver-performance measurement.

`capture.json` calls its wrapper hash `wrapper_sha256_before_execution`, but the implementation computes that hash
after the four children return. It is a post-probe hash of the wrapper bytes, not an independent pre-execution binding.
The execution-time hash of `inspect-wsl.sh` is Not recorded. The `openfoam-activation` section prints a hard-coded
`exit=0` after `source` continues; it is not a separately captured source-command exit. The wrapper's timed
`WaitForExit` bounds the initial wait, but its final `WaitForExit()` is unbounded. Therefore the requested timeout is
not a proven end-to-end deadline. The observed wrapper elapsed times and `timed_out: false` fields remain valid for
this completed run. Filtered `sh -c` sections use `|| true`; their section-level `exit=0` records wrapper completion,
not independent success of every command inside the pipeline.

## Search boundaries

Absence claims above are confined to these retained observations:

- `command -v` for the nine named tools;
- `test -e` for `/usr/local/cuda` and `/opt/rocm`;
- `dpkg-query` for the three exact packages and a package-name substring search for
  `cuda|rocm|hip|petsc|amgx|umpire`;
- `dpkg-query -L openfoam2512 openfoam2512-common`, plus a `find` rooted at
  `/usr/lib/openfoam/openfoam2512` with maximum depth 7 and the recorded accelerator filename tokens;
- `ldd` of the exact activated `simpleFoam` binary;
- `ldconfig -p` filtered to `libcuda|libamdhip|libpetsc|libamgx|libumpire`.

These searches do not prove that no compatible source tree, container, package repository or third-party integration
exists. The displayed `ldd` dependencies do not exclude plugins or libraries that could be loaded later at runtime.
The searches prove only that no usable GPU route was found in the installed runtime and bounded locations.

## L3 impact limitation

Ruling 191 requires L3 iteration-rate measurements before and during this work. This GPU track has no
authority to read the live L3 unit, process or files. PR #28 supplies one non-atomic progress point, not two samples on
one monotonic clock, so it cannot yield a rate. **Impact for this inventory is Not recorded.** The retained final-run
children used 4,016 ms of wrapper elapsed time in total and launched no compiler or solver. This sum excludes the
superseded attempts, review and packaging, and low duration does not prove no L3 impact.

Fable must explicitly rule whether this missing inventory-period measurement is acceptable. The plan also makes an
authorized, immutable L3-observer export a hard predecessor of any further source inspection, installation, build or
trial. Missing telemetry cannot be reported as no impact.

## Repair ledger

The following repair details are the operator/author's retained account; the superseded files no longer exist and the
final capture cannot independently verify them. The read-only capture used both allowed repair cycles:

1. The first run serialized numeric child exits as `null`, `set -u` caused OpenFOAM activation to stop early, and
   `uname -a` exposed a machine hostname. The wrapper now performs a final wait/refresh before reading `ExitCode`, the
   WSL script disables nounset only around vendor activation, and the kernel query uses `uname -srmo`. The superseded
   files were overwritten before staging because they contained the prohibited hostname.
2. The first repaired package regex missed the separately observed `libamdhip64-5`. The final regex searches the full
   declared token set and retains its `whiptail` false positive explicitly.

The final run has numeric exits, an empty WSL stderr, no hostname in the retained kernel line, and a complete OpenFOAM
linkage section. The repair cap is 2/2. No further capture rerun is authorized on this track.

## Disposition

The plan recommends evaluating the official OpenFOAM v2512 native fused/offload path first because it stays within the
pinned OpenFOAM release. It does not claim that the path can accelerate this case. PETSc/CUDA/AmgX remain alternatives
requiring an explicit integration contract and dependency decision. See
`docs/plans/windows-openfoam-gpu-qualification.md`.
