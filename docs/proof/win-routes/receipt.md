---
id: proof-win-routes
title: "W-3 Windows solver routes: Ruling 133 completed manual smoke evidence"
type: proof-pack
status: done
owner: "@win-solver-routes"
tags: [windows, wsl, openfoam, su2, smoke, ruling-133]
links:
  - { to: coordination-pc-kickoff, rel: implements }
  - { to: coordination-windows-w0-w5-execution, rel: implements }
  - { to: design-guided-solver-setup, rel: relates-to }
  - { to: review-pr-4, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  Ruling 133 manual CPU qualification: pinned Ubuntu/OpenFOAM identity verified, cavity reaches t=0.5 under M1,
  Courant mean equals the Mac's printed value; fresh native SU2 LF smoke preserves CD/CL. Inputs match staged blobs.
  Product integration, unobserved OS prompts/error paths and the current application ring remain open.
---

# W-3 — Ruling 133 manual route qualification

**Goal:** finish the manual Windows solver-route smoke evidence. **Done when:** the tutorial hash matches its pin,
LF cavity inputs/YAML are frozen before launch, serial M1 cavity reaches t=0.5, fresh LF native SU2 emits CD/CL,
and YAML/manifest hashes equal repository blob bytes. **Not in scope:** W-4/W-5, GPU trials, product launcher/UI,
unobserved enable/restart/consent/error paths, audit/index/xmsg, publishing. **Tier:** T1. **Fan-out:** 1.
Budget: 60 minutes, new two-repair cap. Implementation worker/author seat; independent review is coordinator-owned.

**Verified:** cavity and native SU2 smoke pass. This is manual installation qualification, not physical validation
or product integration qualification. The heavy application ring is pending explicit coordinator release.
W-4 remains held until completed W-3 review. No GPU trial ran; the single W-4a GPU attempt remains untouched.

One tested source SHA: `defbe0a931e703068a4c06278a413c0cf6d7b6bc` (`r133/tested-sha.txt`, final lightweight gate).
Worktree `C:\Projects\CFD-Workbench-win-solver-routes-r133`, branch `win/solver-routes-r133`.
Fetch succeeded, merge reported Already up to date, xmsg unread messages were read/marked. Only owned case/proof files
and observed §0/§6 design rows changed; the design's route-step definitions are unchanged.

## Host and retained installation evidence

`r133/host-survey.txt` (2.172 s): Windows 11 Pro, 10.0.26300/build 26300.9457, AMD64; i9-12900H, 14 physical cores/
20 logical processors; RAM 34,009,374,720 bytes; C: free 1,852,276,015,104 bytes. HypervisorPresent=true while
VirtualizationFirmwareEnabled=false; both raw values are preserved. CBS/Windows Update restart keys absent;
PendingFileRenameOperations not recorded. WSL 2.7.14.0, kernel 6.18.33.2-2.

`r133/sdk-fresh.txt`: unmodified-shell `dotnet --version` cannot select 10.0.203; Program Files SDKs are 9.0.315 and
10.0.301. No SDK/PATH repair. This does not affect solver execution. No UAC/restart/SmartScreen prompt was observed.

| Guided step | Evidence / limits |
|---|---|
| survey.win | Fresh host and WSL outputs above |
| wsl.enable / os.restart | Existing WSL used; enable/UAC/restart/resume behavior unobserved |
| wsl.base.download / import | Prior owned logs retain observed Ubuntu 24.04.5, 388,975,696 bytes, SHA-256 `bb415d824822c4b878125729af451a5d18fb13d1cf5cbed9a7393ad64ac6039e`, successful private WSL2 import. Not repeated |
| of.repo | Prior fingerprint and signed-update logs retained: `DC93C096174122E256DA24063386DD74948D208F`; no signature bypass. Not repeated |
| of.install / identify | Fresh package state/hashes and help confirm runtime/common 2512.0-2, OpenFOAM-2512 `_bd2b6720-20260127`, label=32/scalar=64 |
| smoke.openfoam | Fresh M1 cavity below |
| su2.download / place | Prior observed v8.5.0 win64-omp zip, 28,043,921 bytes SHA-256 `4466fe21aedb5e0bad57afd45f829acbdec6ec79fe8c3f8954ddea06a4b4bc11`; nested zip extracted. Exe hash checked again before run: `3cb60646b31c08e468441be9f3497601960d4bb31349e6329982bcdeed599248`, Harrier |
| smoke.su2 | Fresh LF native smoke below |

Fresh cached package hashes (`r133/installed-package-hashes.txt`): runtime
`c59e65ffd99c9143fd7c2594dc9776e9d7190124965efdb667e28677d93f3fed`; common
`f438ecbfaff0248b9273502f3b43a6cb1b4f81c07224dbda811f1b48d989026f`.
No managed-device/offline/UAC-decline/SmartScreen/error-path or product resume claim is made.

## Cavity: frozen input to measured output

`r133/steps.jsonl` records mkdir → observed sha256sum at the real cache location → hash comparison → dpkg-deb -x.
Observed tutorial hash equals `4c87494c17a1381853af8a828e8df3a741d9cde5e06bb0212f3fc6940d889473`.
Signed `openfoam2512-tutorials=2512.0-2`, 34,667,466 bytes, extracted in 1.153 s, exit 0; not installed.

LF publisher inputs are under `r133/cavity-inputs/`; individual hashes are in `cases/win-smoke-cavity.yaml`.
`r133/frozen-before-launch.json` proves 14 disk files equal their staged Git blob bytes before launch.
M1 controlDict SHA-256 `f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854`.
Numerics unchanged: 20×20×1 cells, nu=0.01, deltaT=0.005, endTime=0.5.
Accepted run: `/root/CFDWorkbench/runs/20261007T220227Z-win-smoke-cavity`.

The manual adapter uses env -i, isolated HOME/.OpenFOAM/2512/controlDict, identical FOAM_CONTROLDICT bytes,
publisher activation, one OMP/OpenBLAS thread, nice=10, and GNU 60-second timeout per command. The outer runner also
caps the total solver-command sequence at 60 s. This is a manual adapter, not the unimplemented product launcher.

| Command | Exit / WSL wall s | GNU CPU user/system s | GNU wall s / CPU % / maximum RSS KiB |
|---|---|---|---|
| blockMesh | 0 / 0.304 | 0.02 / 0.02 | 0.07 / 63% / 36,528 |
| checkMesh | 0 / 0.272 | 0.02 / 0.00 | 0.02 / 89% / 37,856 |
| icoFoam | 0 / 0.331 | 0.07 / 0.01 | 0.09 / 89% / 35,156 |

Every master banner reads **Disallowing**. checkMesh: **Mesh OK**. icoFoam: **Time = 0.5**.
All ten U/p files at 0.1…0.5 have observed sha256sum readbacks (`r133/cavity-output-*.txt`).
Final Courant mean **0.222158**, max **0.852134**. Compared with Mac mean 0.222158: absolute/relative difference
0 at printed precision; within the proposed 1e-3 relative envelope. The envelope remains **Inferred**, not calibrated
by one equal observation. No physics-validation claim.

One activation repair: initial publisher setup under shell errexit failed before solver execution with
`pop_var_context: head of shell_variables not a function context` (`r133/cavity-blockMesh.txt`). Source setup with
errexit disabled; restore set -eu immediately; check resolved executable. Identity was read before launch and accepted
banners match the build. The failed run and first freeze snapshot are preserved. Fixture bytes/hashes unchanged;
only fresh-run metadata was re-frozen. Preparation first copied to a temporary docs-placeholder, then moved it into
the owned proof directory; move duration **Not recorded**, no leftover folder.

## Native SU2: LF provenance and measured result

Inputs were explicitly rewritten as LF, with actual hashes in the YAML and staged-blob equality before launch.
Fresh run `runs/20261007T220227Z-win-su2-smoke`: `SU2_CFD.exe -t 1 su2-smoke.cfg`, 60 s deadline.
Windows BELOW_NORMAL_PRIORITY_CLASS was requested and read back as **16384**.

Verified exit **0**, wall **0.490 s**; 2,048 correctly oriented quadrilateral cells/2,112 points;
INC_NAVIER_STOKES laminar cylinder; final iteration **61**, rms[P] **−10.02477646**, CD **2.885552317**,
CL **−2.157833374e−16**. Printed CD/CL equal the prior observation. CL near zero is a weak symmetric-case
discriminator; CD is retained. Independent physical reference and accepted tolerance remain **Not recorded**.

GetProcessTimes: **0.4375 CPU seconds**. GetProcessMemoryInfo: 45 samples, sampled maximum peak-working-set
counter **17,334,272 bytes**, not a guaranteed exit-lifetime maximum. API failures degrade to Not recorded.
Signatures checked against [GetProcessTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes)
and [GetProcessMemoryInfo](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-getprocessmemoryinfo).
History normalized to LF after run, numerical fields/rows retained in both history files.

**Correction:** the prior receipt's statement that committed SU2 blobs preserved CRLF was false, as PR #4 found.
This receipt supersedes that claim: current inputs/history are LF, YAML and manifest hashes cover actual blob bytes.
Old logs/ledger remain historical evidence; Git preserves the blocked receipt.

## Controls, validation and remaining gates

Class → sweep → derive → prevent: platform text writes can hash CRLF while Git commits LF. Sweep: SU2 mesh/config/
history, cavity inputs and both YAMLs. LF writing and pre-launch disk/staged-blob checks prevent the shape;
`verify-lf-inputs.py` accepts real frozen files and rejects planted CRLF/hash changes. Generator emits LF explicitly
and avoids duplicate history-output directives. Evidence-local manual control, <1 s; not installed product/CI gating.
Publisher activation/errexit repair is evidence-local too. Register/audit/index handoffs remain coordinator-owned.

Serial DAG: extraction → LF freeze → cavity → SU2 → readback → lightweight validation. One activation repair;
no second solver repair. Each numerical process used one thread, below aggregate 12-core capacity. Host aggregate
utilization, tokens and total agent time **Not recorded**; command UTC/exit/wall and process resources are measured.
Commands/outputs are immutable in `r133/steps.jsonl`; logs normalize LF/trailing whitespace for review.

Lightweight outputs: `r133/case-validation.txt`, `r133/docs-check.txt`, `r133/lf-control*.txt` and final blob check.
Both Windows cases pass; whole-repo case validation exit 1 (0.369 s) is the known shared spike03-s6-w4.yaml non-hash.
Docs check exit 1 (22.281 s): zero metadata problems/orphans; only two coordinator-owned index drifts, proof-win-routes
status and summary. Heavy `tools/run-tests.sh` explicitly held for W-1b capacity; not run.

Previous coordinator ring remains **Reported, not worker-verified**, not this re-entry's ring: build passed (0 errors,
2 Avalonia warnings); Core 13/13/11 failures; Desktop six SelfLaunch passes then exit 70 APP-UNHANDLED APP-CRASH
System.Exception; Analysis part 1 one catalog failure/part 2 green; CLI exit 127 DOC-UNSUPPORTED-PERSISTENCE;
8 cost failures; overall exit 1, 50 s. Current coordinator ring must name every failing test/first error, crash frame,
cost checks, tested SHA and DOTNET_ROOT per PR #4.

**Remaining:** coordinator/Owner review, official index/audit derivation, explicit ring release and single execution,
then delivery/review. W-4/W-5 held. Numerical route needs no operator repair; reruns require new timestamp directories.
