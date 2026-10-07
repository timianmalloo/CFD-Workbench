---
id: proof-win-routes
title: "W-3 Windows solver routes: blocked cavity qualification"
type: proof-pack
status: blocked
owner: "@win-solver-routes"
tags: [windows, wsl, openfoam, su2, smoke]
links:
  - { to: coordination-pc-kickoff, rel: implements }
  - { to: coordination-windows-w0-w5-execution, rel: implements }
  - { to: design-guided-solver-setup, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  Ubuntu 24.04.5 and pinned OpenFOAM v2512 runtime installed; native SU2 v8.5.0 cylinder smoke passed.
  Cavity was not run: tutorial extraction failed at the two-repair cap. GPU inspection only, no trial.
---

# W-3 receipt — blocked

**Goal:** qualify the Windows guided solver routes. **Done when:** every route step has measured evidence, the
OpenFOAM cavity reaches t=0.5 with the required files/banner/scalar, and native SU2 emits finite CL history on a
2D incompressible mesh of at most 5,000 cells. **Not in scope:** W-4/W-5, product launcher implementation, GPU trials,
shared source/tests/tools, audit/register/index writes, publishing. **Tier:** T1. **Fan-out cap:** 1.

**Status: Blocked.** Ubuntu and both solver identities are Verified; SU2 smoke is Verified. OpenFOAM cavity is
**Not recorded / unexecuted**. This receipt is not acceptance of W-3 and does not release W-4 dependencies.

One tested source SHA: `70c9ba534ebc858cad48ba260bb30e2901affbd2` (`tested-sha.txt`). Authored evidence and
`cases/win-su2-smoke.yaml` are additions over that SHA. Worktree: `C:\Projects\CFD-Workbench-win-solver-routes`,
branch `win/solver-routes`. Fetch succeeded; merge reported Already up to date; xmsg unread messages were read/marked.
The coordinator owns delivery, audit append, defect-register handoff and docs-index derivation.

## Measurements and scope

Verified host observations: WSL reports Windows `10.0.26300.9457`; WSL `2.7.14.0`, kernel `6.18.33.2-2`.
CIM reports i9-12900H, 14 physical cores / 20 logical processors, 34,009,374,720 bytes RAM. Windows registry
ProductName reads Windows 10 Pro; this label alone is not an edition/version qualification.
Only one solver thread ran. Aggregate solver compute stayed below the 12-core cap. Host CPU utilization,
solver CPU time, peak RAM, free disk, pending restart flags and SDK 10.0.203 in the unmodified shell are
**Not recorded**. `dotnet-version.txt` records that the shell instead resolves Program Files .NET and cannot
load the repo-pinned SDK. No SDK or PATH repair was attempted in this track.

The command ledger is [steps.jsonl](steps.jsonl), with exact argv, UTC start, exit, elapsed time and output file.
Text logs are normalized to LF and stripped of trailing whitespace for review; input mesh/config and history bytes
are preserved through local .gitattributes. Durations are wall time measured by `time.perf_counter`, including Windows/WSL process startup. No operator
UAC, restart or interactive setup prompt occurred in the performed steps. Existing WSL enable/restart behavior
is not tested by importing a distro into already enabled WSL.

## OpenFOAM route steps (§3.2)

| Step | Observed result | Evidence / wall seconds |
|---|---|---|
| 0 survey.win | WSL initially had no distribution; host facts above | initial terminal observations; `wsl-version.txt`, `nvidia-host.txt`; survey total duration Not recorded |
| 1 wsl.enable | Existing WSL status exited 0, default version 2; enable command and UAC not executed | `wsl-version.txt`; enable duration Not recorded |
| 2 os.restart | Not executed; reboot flags not surveyed | Not recorded |
| 3 wsl.base.download | Ubuntu 24.04.5 image 388,975,696 bytes, expected and actual sha256 `bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e` | `ubuntu-download.txt`, `ubuntu-download-hash.txt`; 6.627 |
| 4 wsl.base.import | First import failed `Wsl/ERROR_PATH_NOT_FOUND`; destination directory creation followed by import succeeded; WSL lists cfdw-openfoam2512 version 2, root `/etc/os-release` says Ubuntu 24.04.5 | `ubuntu-import.txt`, `ubuntu-import-repair.txt`, `wsl-list.txt`, `distro-survey.txt`; successful import 8.604 |
| 5 of.repo | Publisher key fingerprint `DC93C096174122E256DA24063386DD74948D208F` matches; initial signed update failed because ASCII key was saved as .gpg. Dearmor corrected the key format; signed update passed | `repo-key-fingerprint.txt`, `key-dearmor.txt`, `openfoam.list`, `apt-update.txt`, `apt-update-repair.txt`; successful update 160.747 |
| 6 of.install | Runtime-only `openfoam2512` and `openfoam2512-common`, both 2512.0-2, installed with no-recommends. No dev package requested. Actual cached package hashes match signed index pins | `of-install-repair.txt`, `installed-state.txt`, `apt-package-index.txt`, `of-package-hashes.txt`; 218.417 |
| 7 of.identify | icoFoam help reports OpenFOAM-2512 build `_bd2b6720-20260127`, LSB label=32 scalar=64 | `of-identify.txt`; 0.419 |
| 8 smoke.openfoam | Not executed: fixture extraction failure below | No cavity YAML/run result committed; t=0.5, Courant mean, Disallowing banner and result hashes Not recorded |

Package actual/pin hashes:

- runtime: `c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed`
- common: `f438ecbfaff0248b9273502f3b43a6cb1b4f81c07224dbda811f1b48d989026f`
- icoFoam binary: `789678a0003411bd835b9a6458d50d70180033b791e1b82648ae50bf5f08fec9`

There was an overlapping apt-update lock failure during the initial update. The ledger includes the short lock
attempt and the later completed signature failure under the same output basename; the retained `apt-update.txt`
is the completed signature failure. The lock attempt's observed text was `Could not get lock /var/lib/apt/lists/lock.
It is held by process 286 (apt-get)` (exit 100). No lock was removed or signature check bypassed.

## Fixture failure and repair cap

The runtime route does not install its suggested tutorials. The coordinator explicitly authorized one signed
package acquisition/extraction for the required fixture. A raw source URL returned HTTP 502 first; it was not retried.
The signed repository supplied `openfoam2512-tutorials=2512.0-2`, 34,667,466 bytes, expected and observed SHA-256
`4c87494c17a1381853af8a828e8df3a741d9cde5e06bb0212f3fc6940d889473`. It was downloaded, not installed.
The Windows cache now holds the package in `%LOCALAPPDATA%\CFDWorkbench\qualification\`.

Failed exact argv (`fixture-extract.txt`, exit 2, 0.077 s):

```text
wsl.exe -d cfdw-openfoam2512 -u root -- dpkg-deb -x /mnt/c/Projects/CFD-Workbench-win-solver-routes/openfoam2512-tutorials_2512.0-2_all.deb /root/CFDWorkbench/fixture-cache
dpkg-deb (subprocess): failed to create directory: No such file or directory
dpkg-deb: error: tar subprocess returned error exit status 2
```

Two prior corrective cycles had completed (import directory and key dearmor). The coordinator's binding cap
instruction required stopping if extraction needed another correction. **No directory creation or extraction retry
followed this failure.** The running runtime install completed afterward and its installed state/hashes were read.

Class → sweep → derive → prevent handoff: both directory failures share **external command invoked before its
destination parent exists**. The import and extraction destination paths were the bounded sweep; both had this shape.
Required future control: create/check the known owned parent before either command, with a failing negative-path
check. This is a proposed control, not a claimed implemented gate; the register and product runner are coordinator/
Mac-owned and unchanged. The signing-key correction likewise requires checking armor and extension together before
apt use. No new product control is claimed by this manual qualification.

## SU2 route steps (§3.3)

| Step | Observed result | Evidence / wall seconds |
|---|---|---|
| 0 survey.win | Native Windows x64 route; host observations above | survey total duration Not recorded |
| 1 su2.download | v8.5.0 win64-omp release archive, 28,043,921 bytes; publisher digest and downloaded hash match `4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11` | `su2-download.txt`, `su2-download-hash.txt`; 1.098 |
| 2 su2.place | Outer zip contains nested win64-omp.zip; both extracted into local qualification cache; SU2_CFD.exe present. Actual exe SHA-256 `3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248`; help says v8.5.0 Harrier, -t OpenMP option | `su2-identify.txt`; extraction time Not recorded, identify 0.098 |
| 3 smoke.su2 | Exit 0; 2048 correctly oriented quadrilateral cells, 2112 points; INC_NAVIER_STOKES, laminar, one OpenMP thread; final history iteration 61, rms[P] -10.02477646, CL -2.157833374e-16, CD 2.885552317 | `su2-smoke.txt`, `su2-history.csv`, `su2-run-dir.txt`, `cases/win-su2-smoke.yaml`; 0.382 |

Fresh actual run: `runs/20261007T181811Z-win-su2-smoke`, under this Windows worktree. Original SU2 v8.5.0 publisher
configuration is cited in the YAML. Generated annular cylinder mesh replaces the oversized publisher mesh: 64 angular
segments × 32 radial layers, radius 0.5 to 20 m. Changes: ITER 100, MGLEVEL 0, explicit AERO_COEFF history and restart-only
files. Inputs are frozen in `su2-smoke.cfg`, `su2-cylinder.su2`; the generation script is retained. Fixture queries also
used TestCases commit `790c80ec5b543487b5f8ecf8bb0f0e4d2cc67f3f`; unused oversized downloads were removed.

**Reference:** this is the first Windows installation smoke observation, not an independent physical reference.
Tolerance and equivalence verdict are **Not recorded**. The YAML's required POSIX `nice: 10` is the policy value;
POSIX nice is inapplicable to the native process, and observed Windows process priority is Not recorded. CPU/RAM
telemetry was not captured. The YAML was written after the run, a provenance-order limitation; do not claim a pre-run
manifest control passed. A future smoke must freeze its manifest before launch.

## GPU inspection only

Verified: Windows and WSL both expose RTX 3080 Ti Laptop GPU, driver 596.47, 16,384 MiB VRAM. Installed OpenFOAM
contains `etc/config.sh/umpire` and `libfusedFiniteVolume.so`. The config describes experimental Umpire 2025.03.0,
used only by wmake. This proves files exist, **not** a working GPU runtime/offload route.
icoFoam full help contains no explicit GPU execution option. Direct ldd outside activation could not resolve its
OpenFOAM libraries; the attempted activated linkage command expanded empty variable paths and did not inspect the
library. Thus a supported offload path is **not established**, not proven absent. No GPU trial ran and the W-4a
single-attempt budget is untouched. GPU data cannot feed W-4a/L3/W-5 acceptance. Further capability inspection is a
future authorized step; no solver acceleration is claimed.

## Validation and planned versus actual

The plan was a serial DAG: pins → installations → fresh smoke → receipt/validation. Installation failure changes
the exit to a blocker receipt; all known failures remain visible. Numerical validation floor was not silently dropped.
90-minute budget; actual measured command durations are in the ledger; total agent runtime/tokens Not recorded.
Two corrective cycles completed; the third corrective need fired the cap. Peer author seat only; independent review
remains coordinator/Owner-owned. Surface list: publisher pins → local cache/install → binary identity → frozen fixture
and YAML → fresh run → logs/history → proof receipt. Product model/UI/launcher are unchanged and unqualified.

- `py -3 tools/check-docs.py`: exit 0, 21.051 s, before this new receipt; Documentation checks passed with 131 freshness
  findings. First receipt validation rejected `type: proof`; corrected to the repository's `proof-pack` metadata.
  The relation was corrected from unregistered `tests` to `relates-to`. Final result is separately recorded in the
  ledger: `delivery-docs-check.txt`, exit 1, 10.995 s, zero metadata problems, only `file not in index: proof-win-routes`.
  Index drift remains coordinator-owned.
- `py -3 cases/tools/validate-cases.py`: Windows SU2 case passes; repository-wide exit 1 due solely to existing
  `spike03-s6-w4.yaml` non-hash failed-mesh provenance. That shared case is unchanged.
- `git diff --check`: passed before staging. Staged raw history preserves the publisher's trailing-space header and
  CRLF, so the default whitespace check flags it. Courtesy verification with `core.whitespace=-blank-at-eol,cr-at-eol`
  preserves evidence bytes while checking other whitespace classes; its result is returned with the commit.
- Full Windows application ring: not run in the stopped solver track; shell cannot resolve repo SDK 10.0.203.
  Coordinator must perform the applicable join ring (cases path is not docs-only) with its qualified SDK environment.

## Exact operator re-entry sequence (unexecuted)

Only after a new authorization clears the repair stop, in PowerShell:

```powershell
wsl.exe -d cfdw-openfoam2512 -u root -- mkdir -p /root/CFDWorkbench/fixture-cache
wsl.exe -d cfdw-openfoam2512 -u root -- sha256sum /mnt/c/Users/malla/AppData/Local/CFDWorkbench/qualification/openfoam2512-tutorials_2512.0-2_all.deb
wsl.exe -d cfdw-openfoam2512 -u root -- dpkg-deb -x /mnt/c/Users/malla/AppData/Local/CFDWorkbench/qualification/openfoam2512-tutorials_2512.0-2_all.deb /root/CFDWorkbench/fixture-cache
```

Verify the package hash above, then resume W-3 as a new bounded track: freeze `cases/win-smoke-cavity.yaml` from the
extracted tutorial and measured Linux build before launching; run in a fresh Linux filesystem timestamp directory;
use the repo's pinned product controlDict in an isolated HOME, serial CPU, 60-second hard timeout; capture
blockMesh/checkMesh/icoFoam banners, exit codes, resource metrics, t=0.1…0.5 U/p files and final Courant mean.
Do not begin W-4 until the completed W-3 receipt is reviewed.

Authoritative routes: [Ubuntu noble images](https://releases.ubuntu.com/noble/),
[OpenCFD Linux installation](https://www.openfoam.com/download/openfoam-installation-on-linux),
[OpenCFD signed repository](https://dl.openfoam.com/repos/deb/dists/noble/main/binary-amd64/Packages),
[SU2 release v8.5.0](https://github.com/su2code/SU2/releases/tag/v8.5.0).
