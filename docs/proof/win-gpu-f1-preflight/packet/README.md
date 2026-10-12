---
id: proof-win-gpu-f1-r217-packet
title: "Ruling 217 F1 dependency preflight execution packet"
type: proof-pack
status: proposed
owner: "@pc-f1-r217-packet"
phase: preflight
tags: [windows, wsl, gpu, openfoam, petsc, ruling-217]
links:
  - { to: plan-windows-gpu-f1-preflight-r217, rel: implements }
  - { to: coordination-windows-gpu-f1-preflight-r217, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  Reviewed-only, self-testing host and Linux packet for one bounded disposable Ruling 217 metadata/download preflight, with a committed checkpoint before cleanup.
---

# Ruling 217 preflight packet — frozen source, no live run here

This packet is for **one** reviewed live attempt in an isolated EXECUTE worktree. The PACKET author did not run it.
It permits a disposable dependency simulation and resolved `.deb` acquisition for licence/size inspection only.
`cfdw-openfoam2512` is read only for the completed-L3 guard and signed Ubuntu rootfs verification. The target is
exactly `cfdw-petsc-cuda132-f1` at `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`.
[Ruling 217](../../../notes/rulings.md) and the [execution plan](../../../plans/windows-gpu-f1-preflight-r217.md)
govern; independent Astra packet acceptance and a live epoch-3 authority check precede invocation.

## Inputs and invocation

Run from PowerShell in the **EXECUTE** worktree after packet acceptance, using reviewed absolute local paths on C:
without whitespace. Required inputs are the already existing B2 Ubuntu Noble WSL rootfs tarball, its matching Ubuntu
`SHA256SUMS` and `.gpg`, a binary OpenFOAM publisher keyring and its **independently reviewed 40-hex fingerprint**, a
binary NVIDIA repository keyring with the B2-pinned fingerprint, a unique run id and the executor's coordination
session. The caller must freeze the exact input paths, fingerprints and SHA-256 values in the execution contract;
an unreviewed OpenFOAM fingerprint blocks the live run. No input is silently fetched by the host wrapper.

```powershell
& .\docs\proof\win-gpu-f1-preflight\packet\Invoke-Preflight.ps1 `
  -Rootfs C:\reviewed\ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz `
  -UbuntuSums C:\reviewed\SHA256SUMS `
  -UbuntuSignature C:\reviewed\SHA256SUMS.gpg `
  -OpenFoamKeyring C:\reviewed\openfoam.gpg `
  -OpenFoamFingerprint <REVIEWED_40_HEX_FINGERPRINT> `
  -NvidiaKeyring C:\reviewed\cuda-archive-keyring.gpg `
  -RunId <REVIEWED_UNIQUE_ID> -AgentSession <EXECUTOR_SESSION>
```

The path examples are placeholders, not local files. The rootfs must be the B2 356,739,129-byte archive at SHA-256
`8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea`; `verify-rootfs.sh` checks its
Ubuntu detached signature with the protected distro's cloud-image keyring before target creation. NVIDIA's expected
key fingerprint is `EB693B3035CD5710E231E123A4B469963BF863CC`, established in the
[B2 source-verification receipt](../../win-gpu-b2/source-verify.stdout.txt). The OpenFOAM key fingerprint remains a
reviewed invocation input because its key provenance was not established by the design author. If its origin cannot
be authenticated independently, the packet review must VETO execution.

## Effects and evidence

The wrapper starts one host monotonic deadline **before the final precheck**. It checks exact distro absence,
protected presence, rootfs hash and signature, then starts the copied/adapted B2 `completed_guard.py` before the
first target-creation effect. The guard samples every 5 seconds through unregister. Its terminal projection includes
unit result/status, final iteration, marker counts and log byte/mtime/tail/full hashes. Missing, stale, partial or
changed observations stop new work. Guard events and state are frozen into both evidence commits; the final binding
has a contiguous complete-row prefix and a full before/after protected snapshot comparison.

Only `wsl.exe --install --from-file <verified local rootfs> --name cfdw-petsc-cuda132-f1 --location
C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1 --vhd-size 8GB --no-launch` creates the target. The next
Linux stage measures both root block-device and filesystem maxima and requires each ≤8,589,934,592 bytes. The host
also checks target physical VHD ≤8 GiB, C: free ≥19,684,091,961 bytes before creation and ≥10,737,418,240 bytes
throughout. **No network stage starts before both capacity checks pass.** The host records five-second capacity rows.
Target Linux stages are `probe`, `repositories`, `simulate`, `download`, `inspect`, and cleanup `delete-debs`.
Every Linux entry uses `taskset --cpu-list 0-1` (protected guard uses CPU 0), `nice -n 10`, and foreground
`setsid --wait`; it records effective `Cpus_allowed_list`, nice, PID, PGID, SID and cmdline. Stage leaders must have
PID=PGID=SID. Host WSL lifecycle and Git commands have affinity **N/A**. The owner-control helper signals only the
acknowledged target process group and verifies absence; uncertain identity blocks unregister.

The repositories stage uses exact Ubuntu Noble main/universe, updates and security indices, OpenFOAM Noble and
NVIDIA CUDA 13.2. `apt-get update` must authenticate them with explicit keyrings. It copies all five `InRelease`
files from an isolated target-only apt lists directory, verifies each via `gpgv`, records key fingerprints and hashes package indices, and freezes index bytes as a
compressed tar. The simulation pins all 135 B2 package versions and five OpenFOAM 2512.0-2 packages, and asks for
GCC/G++/gfortran 13, make, OpenMPI, BLAS/LAPACK and `pkg-config`. It excludes recommendations, requires zero removals, maps every
`apt-get --print-uris` row to one signed-index SHA-256/size row, and confirms an exact pinned re-simulation. Only
then does `apt-get download` fetch that resolved set into the disposable guest. `transaction.py` verifies every
downloaded archive against the authenticated index, streams package payloads without extracting them to disk,
records control `Installed-Size` and licence-notice hashes, and writes `transaction-lock.json`, `license-map.json`
and `capacity.json`. Any missing installed-size or licence basis blocks a complete result while retaining evidence.
The full F1 build/case space threshold remains outside this preflight and stays `Not recorded`.

All captures go to `docs/proof/win-gpu-f1-preflight/run-1/live/` in the EXECUTE worktree. Before deleting archives
or unregistering, `manifest.py` freezes a complete event prefix and raw/derived captures under `run-1/checkpoint/`;
the wrapper makes a real Git commit and verifies every manifest entry against that commit's blob. **If this binding
fails, it performs no archive deletion or unregister** and reports a blocked manual-cleanup handoff. Otherwise it
stops the exact owned group, removes only resolved `.deb` files inside the target, checks residual absence,
terminates and unregisters the exact target, independently checks registration/path absence, takes a post-unregister
guard sample and protected snapshot, stops/reaps the guard, then commits a separate `run-1/final/` manifest and
cleanup receipt. Partial creation treats registration and path independently; recursive path removal is allowed only
after checking the fixed absolute location and rejecting a reparse point. The initiating error is retained alongside
cleanup errors.

The whole host step, including final guard/absence verification, is capped at **3,600 seconds**. New stages stop at
45 minutes. A pre-cleanup Git checkpoint must finish by 50 minutes; the last 10 minutes are reserved for cleanup.
Every child/control wait is capped by `min(stage limit, remaining host deadline)`. Stage ceilings are creation 300 s,
guest probe 60 s, repository/index work 300 s, simulation 300 s, package download 1,200 s, inspection 600 s,
deb deletion 120 s, unregister 120 s and guard stop 20 s. A cap firing is a failed attempt, not a retry.

Package installation, PETSc/petsc4Foam source acquisition, compilation, an OpenFOAM case or solver run, global WSL
shutdown, and target resizing are prohibited. `authority_scan.py` plants each forbidden command class and proves
the two authorized apt simulation/URI forms and `apt-get download` remain allowed; it scans all executable packet
sources except its own fixture file. The `check-wsl-inline` gate forbids an inline multiline WSL program.

## Packet-only checks

These do **not** invoke the live wrapper:

```powershell
py -3 docs/proof/win-gpu-f1-preflight/packet/authority_scan.py
py -3 docs/proof/win-gpu-f1-preflight/packet/transaction.py self-test
py -3 docs/proof/win-gpu-f1-preflight/packet/manifest.py self-test
py -3 docs/proof/win-gpu-f1-preflight/packet/completed_guard.py self-test
py -3 tools/check-wsl-inline.py
py -3 tools/check-docs.py
```

The wrapper and helpers were authored from B2's
[`completed_guard.py`](../../win-gpu-b2/completed_guard.py),
[`capture-completed-install.ps1`](../../win-gpu-b2/capture-completed-install.ps1),
[`stop-owned.sh`](../../win-gpu-b2/stop-owned.sh),
[`residual-owned.sh`](../../win-gpu-b2/residual-owned.sh),
[`build-package-lock.py`](../../win-gpu-b2/build-package-lock.py) and
[`verify-debs.py`](../../win-gpu-b2/verify-debs.py), with a single 3,600-second host boundary and no install/test
stage. The prior proof is a source of mechanics, not permission to reuse its disposable identity or mutate its
evidence.
