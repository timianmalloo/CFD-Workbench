---
id: plan-f1-rescope-r237
title: "F1 re-scope plan (Ruling 237): one preflight packet for Windows x64 and ARM64 hosts with an NVIDIA GPU"
type: plan
status: accepted
owner: "@pc-laptop-leader"
phase: preflight
tags: [f1, windows, wsl, arm64, x64, cuda, execution-graph, planning]
links:
  - { to: rulings, rel: depends-on }
  - { to: proof-win-gpu-f1-arm64-spike, rel: depends-on }
  - { to: plan-windows-gpu-f1-preflight-r217, rel: refines }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: relates-to }
  - { to: proof-win-gpu-f1-attempt2-offline-request, rel: relates-to }
  - { to: coordination-handoff-2026-10-16-laptop-setup, rel: relates-to }
review-by: "2026-11-02"
summary: >-
  The plan Ruling 237 (4) asks for before any packet code: what the re-scope changes in the F1 preflight packet so one
  packet runs on Windows x64 and Windows ARM64 with an NVIDIA GPU, the evidence each decision rests on (every Verified
  row names a committed receipt), the execution graph with its two-cycle repair cap and a named red-first test per
  node, the surfaces a change must reach, and the three decisions the reviewer and the operator settle before code
  starts. The spike did not refuse; two findings beyond the ruling's text are named. Ruling 240 (2026-10-19)
  ruled DR-1 as (b) and accepted the plan; the nodes run in order from C1.
---

# F1 re-scope plan (Ruling 237)

## 1. Goal state

**Goal.** Re-scope the F1 dependency-preflight packet (`docs/proof/win-gpu-f1-preflight/packet`) so one packet runs on a
Windows x64 (Intel or AMD) host and on a Windows ARM64 host, each with an NVIDIA GPU, as Ruling 237 (1) decides: the CPU
architecture is detected and recorded, never assumed; per-architecture artifacts are locked and hashed per architecture;
machine paths derive from the repository location, never a constant. Then re-plan F1 attempt 2 under it
(Ruling 237 (2)) with the Ruling 235 cycle-3 items carried in unchanged (Ruling 237 (3)).

**Done when.** (a) This plan is reviewed and accepted before code, and the decisions in §6 are ruled. (b) The packet
changes land on main with red-first tests on both PowerShell runtimes and in the Python ring, the fast gates green and
readiness green. (c) The attempt-2 selections are chosen again on the laptop, the packet's own prepare path emits the
committed contract pair, and the live authorization takes the next free ruling number (Rulings 235 and 236). (d) The
first live run on ARM64 is attempt 2 of the three Ruling 225 allows, under Ruling 217's limits, with a receipt that
records the OS architecture, CPU and GPU (Ruling 238 (1)).

**Not in scope.** A PETSc or petsc4Foam fetch or build, a case run, the benchmark, any change to the B2 proof, the
Windows x64 rerun (Ruling 238 (2): planned, not a gate), a Mac track (Ruling 236), the three 10-17 rulings' bodies.

**Tier** T2 (a change to a reviewed live-mutation packet; independent review is a floor). **Fan-out cap** 2 (the two
test runtimes; edits are serial, one author). **Repair cap** two cycles for the whole track (Ruling 237 (4)); a cap that
fires is a defect signal and returns the track to the operator.

## 2. Authority

- Ruling 237 (1)-(5), `docs/notes/rulings.md`: the decision quoted in §1; "(4) The re-scope is a new track with its own
  plan, reviewed before code, and its own two-cycle repair cap"; "(5) ... the plan starts with a read-only spike that
  establishes it. If the spike refuses, the track stops."
- Ruling 238 (1): "Every receipt records the OS architecture, CPU and GPU it ran on, so an ARM64 result is never read as
  an x64 result." (3): numbers measured on the old x64 PC are measured again.
- Ruling 235 (cycle-3 scope, carried by 237 (3)): the renewal stderr check compares the file's byte length to zero and
  refuses a BOM-only file; `expires_in_before >= 200` read before the CAS write on the first renewal event; the committed
  contract pair comes from the packet's own prepare path.
- Ruling 225: each live attempt keeps Ruling 217's limits exactly (fresh throwaway distro, simulate then download only,
  2 cores at nice 10, 60-minute wall cap, 8 GiB VHD cap, host free-space floor, completed-L3 guard, `cfdw-openfoam2512`
  untouched, delete and unregister with a receipt); attempt 1 is consumed; each attempt is hash-bound by its own ruling.
- Ruling 236: the PC session is the sole leader; the packet's `leader='mac'` is a label in the code, not a second
  machine; the PC commits the live authorization itself after a review it records.
- Ruling 189 shape and the Windows proof runbook (`.claude/knowledge/windows-proof-runbook.md`): launcher, ownership,
  hashing and no-raw-log rules for every proof action.

## 3. What the spikes established

Every row labelled Verified names the committed receipt that holds the observation; the receipts live in
`docs/proof/win-gpu-f1-preflight/arm64-spike/` (the folder of `proof-win-gpu-f1-arm64-spike`).

| Fact | Receipt | Label |
|---|---|---|
| CUDA in WSL works on the ARM64 laptop: driver CUDA 13.4, device cc 12.1; the arm64 cudart 13.2 round trip passes | `cuda-driver-probe.json`, `cuda-runtime-probe.json` | Verified |
| arm64 toolkit source is NVIDIA `ubuntu2404/sbsa` (toolkit 13.2.2-1 present); `wsl-ubuntu` is x86_64 only | `repository-evidence.json` | Verified |
| OpenFOAM 2512.0-2 and its four companions exist for arm64 on the noble suite | `repository-evidence.json`, `lock-resolution-arm64.json` | Verified |
| Ubuntu publishes a noble WSL rootfs per architecture: `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz`, SHA-256 `fecec1d9b7b750c12c109edb49c13c1006f4a2efabb9b8bf341f11c4c9f2ef11`, beside the amd64 one the packet pins (`8251e27f...`) | `sources-evidence.json` (the six `SHA256SUMS` lines and that file's own SHA-256; an unsigned read, the packet verifies the signature at run time; the arm64 bytes were not downloaded) | Verified (index), bytes unverified until C4 runs |
| arm64 Ubuntu packages come from `ports.ubuntu.com/ubuntu-ports`, not `archive.ubuntu.com/ubuntu` | `host-identity-evidence.pwsh.json` (`stock_distro_inside.apt_sources_uris`); the eight index URLs in `lock-resolution-arm64.json` | Verified |
| Of the 149 locked rows (135 B2 rows + 14 F1 extra roots), 142 resolve on arm64 at the locked version; 4 exist at other versions; 3 are absent (§4.2) | `lock-resolution-arm64.json` (`totals`), produced by `lock_resolution_probe.py` | Verified |
| The frozen amd64 lock has drifted on amd64 too: `linux-libc-dev` 6.8.0-146.146 is no longer in amd64 noble-updates main (147.147 is) | `sources-evidence.json` (`amd64_noble_updates_main`: index SHA-256, versions, `b2_locked_version_present: false`) | Verified |
| Host identifiers under **both** PowerShell runtimes (7.6.7 and 5.1): `PROCESSOR_ARCHITECTURE=ARM64`, `OSArchitecture=Arm64`, `ProcessArchitecture=Arm64`, CIM "ARM 64-bit Processor"; CPU `NVIDIA RTX Spark N1X (6144-core GPU, 20-core CPU)`; GPUs `NVIDIA RTX Spark N1X (6144-core Blackwell RTX GPU)` and `NVIDIA NPU`; inside WSL `dpkg --print-architecture=arm64`, `uname -m=aarch64` | `host-identity-evidence.pwsh.json`, `host-identity-evidence.ps51.json`, produced by `host_identity_probe.ps1` | Verified from an ARM64 parent; the same 5.1 binary started from an x64 parent reports x64 (C1 finding, register EMULATED-PROCESS-REPORTS-AN-EMULATED-OS); CIM's `ARM 64-bit Processor` holds in both |
| A fresh noble distro carries every tool the packet's Linux side calls (gpgv, gpg, dpkg, dpkg-query, apt-get, apt-cache, python3 3.12.3, sha256sum, setsid, taskset, findmnt, blockdev, xz, tar, curl, ps, awk) and both Ubuntu keyrings (`ubuntu-keyring` 2023.11.28.1; archive keyring SHA-256 `80a36b0a...`, cloud-image keyring `c2d40d92...`) | the two host-identity receipts (`stock_distro_inside`), via `wsl_tools_check.sh` in the stock Ubuntu-24.04 distro | Verified for the stock distro; the fresh target rootfs is the same release (Inferred until the first arm64 target creation) |
| Git for Windows on the ARM64 laptop ships `gpgv.exe` (GnuPG 2.4.9) at `<git root>\usr\bin`, found by walking three levels up from `git --exec-path` (`...\Git\clangarm64\libexec\git-core`) | the two host-identity receipts (`git`) | Verified on arm64; the x64 layout (`...\Git\mingw64\...`) Inferred |
| No `cfdw-openfoam2512` distro exists on the laptop; the only registration is `Ubuntu-24.04`, version 2, at `%USERPROFILE%\AppData\Local\wsl\<guid>` | the two host-identity receipts (`wsl_registrations`, `wsl_list_verbose`) | Verified |
| The repository drive on the laptop is `D:`, free bytes recorded (the old PC's free-space floor was a `C:` worksheet number) | the host-identity receipts (`repo_drive_free_bytes`) | Verified |
| nvcc targets cc 12.1; PETSc-CUDA builds on arm64 | not run (spike README §4) | Inferred, open |

## 4. Findings beyond the ruling's text

### 4.1 The protected distro does not exist on the laptop

The packet binds a **protected** distro by name (`cfdw-openfoam2512`), registry GUID (`{b7566a82-...}`) and a path under
`%USERPROFILE%` (`attempt.py` PROTECTED_KEY/PROTECTED_PATH; `Control.ps1` Assert-FreshRegistrations requires exactly one
such registration; `attempt.py` KEEPALIVE keeps it alive on CPU 0). It uses that distro for two things: it runs
`verify-rootfs.sh` there (`Invoke-Preflight.ps1`, gpgv with the cloud-image keyring that the `ubuntu-keyring` package
installs), and the **completed-L3 guard** watches that distro's finished OpenFOAM case so F1 cannot disturb B2's
result. Those are the old PC's B2 artifacts. On the laptop there is nothing to protect, and Assert-FreshRegistrations
would refuse before the first WSL effect. Ruling 237 does not name this; Ruling 225 lists the guard among the limits.
§6 DR-1.

### 4.2 The lock is not one artifact with two hash columns

Ruling 237 (1) says "per-architecture artifacts are locked and hashed per architecture". The probe shows the arm64 lock
has a **different row set**, not only different hashes:

| Row | amd64 lock | arm64 index |
|---|---|---|
| `cpp-13-x86-64-linux-gnu`, `gcc-13-x86-64-linux-gnu`, `g++-13-x86-64-linux-gnu` | native compiler internals, 13.3.0-6ubuntu2~24.04.1 | only as `...cross1` cross packages; the native rows are `*-13-aarch64-linux-gnu` at the same 13.3.0-6ubuntu2~24.04.1 |
| `libquadmath0` | present | absent (no quadmath on aarch64) |
| `cuda-nsight-13-2`, `cuda-opencl-13-2` | toolkit dependencies on x86_64 | absent from sbsa (the toolkit meta-package depends on fewer parts) |
| `linux-libc-dev` | 6.8.0-146.146 | 6.8.0-147.147 (and 6.8.0-146.146 is gone from amd64 noble-updates as well) |

So the per-architecture lock is **produced by the run on that architecture** from the pinned roots, and the B2 lock
leaves the packet's input set. The F1 packet already produces such a lock (`transaction.py` writes
`transaction-lock.json` from the simulate, print-uris and inspect stages, and reconciles it against the signed indices);
what changes is the input side (§5 C3). This is the smaller shape: an arm64 twin of the 135-row lock would need its own
metadata run on arm64 and would drift the same way. The drift row shows a second class: a pin on a suite that drops
superseded versions (`noble-updates`) is unresolvable later on **either** architecture; the register entry
LOCK-PINS-A-MOVING-SUITE records it with this plan as the control.

### 4.3 Every binding the re-scope changes, located

| Binding | Where | Re-scope | Ruling 217/225 limit |
|---|---|---|---|
| Target path `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` (done, C2) | `Invoke-Preflight.ps1` ($TargetPath, Assert-TargetPath), `Control.ps1` (Get-BoundTargetKey, Assert-FreshRegistrations), `SelfTest.ps1`, `contract.py` check_contract_fields, the attempt-1 contract, the attempt-2 request | derived only: `<repo parent>\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` from `$RepoRoot`; the contract records the absolute path it was prepared for, and the launcher checks equality with the derived value. No override parameter: both known hosts derive (the old PC's `C:\Projects` derives to today's constant), and an override would be a second definition of the path | kept |
| Drive `C:` | `Convert-LinuxPath` (`/mnt/c/` only), `Get-HostFree` (drive C), `preflight.sh` (`/mnt/c/*` arguments), the README's "absolute local paths on C:" | the drive letter of the target root and of each input; `/mnt/<letter>/`; the free-space floor measured on the target's drive | kept (drive follows the target) |
| Free-space floor and reserve numbers `19684091961` and `10737418240` (done, C2) | `Invoke-Preflight.ps1` $HostStartFloor/$HostReserve, `attempt.py` LIMITS | **changed:** the numbers are the old PC's `C:` worksheet; under Ruling 238 (3) they become contract fields (`host_start_floor_bytes`, `host_reserve_bytes`) from a laptop worksheet recorded in the attempt-2 proof; a contract without them refuses | changed, said here |
| Protected distro: exactly one `cfdw-openfoam2512` registration, its GUID and `%USERPROFILE%` path, the keepalive on CPU 0, `verify-rootfs.sh` run inside it (done, C4) | `Control.ps1` Assert-FreshRegistrations, `attempt.py` PROTECTED_KEY/PROTECTED_PATH/KEEPALIVE, `Invoke-Preflight.ps1` | **changed under DR-1 (b):** `protected` becomes a contract field with a `none` state; with `none` the registration check no longer requires a protected row (other registrations, such as the stock `Ubuntu-24.04`, are neither required nor refused, as today for any non-target, non-protected row), the guard records `not applicable`, and the rootfs signature is verified on the Windows side (C4). With a declared protected distro everything stays as today | changed, said here; DR-1 |
| Rootfs name and hash | `contract.py` ROOTFS, `Invoke-Preflight.ps1` $RootfsSha256, `verify-rootfs.sh` basename check, `attempt.py` INPUT_HASHES | a per-architecture table (name, SHA-256), one producer (§5 C1) | kept (per architecture) |
| apt sources `arch=amd64`, `archive.ubuntu.com`, `ubuntu2404/x86_64` | `preflight.sh` repositories stage; the InRelease-to-keyring map | per-architecture rows from the same table; the five-index count stays; `ports.ubuntu.com` joins the Ubuntu keyring map | kept (signed indices) |
| `make:amd64`, 135 `name=version` roots (done, C3) | `transaction.py` EXTRA_ROOTS, roots() | `make:<arch>`; roots are the pins (§4.2) | kept (simulate then download only) |
| `leader_epoch=8`, run id pattern, branch, session, run root for attempt 2 | `attempt.py` check_binding, `Control.ps1` Get-InvocationRunRoot | attempt-2 selections chosen again on the laptop (Ruling 237 (2)); the same binding shape | kept |
| Old-PC registry GUIDs (retired target keys) | `attempt.py`, `Control.ps1` | keep refusing them (harmless on any host) | kept |
| Receipts carry no architecture, CPU or GPU | `manifest.py`, the attempt-1 README | `host-identity.json` written before the first WSL effect and frozen with the evidence (§5 C1) | added (Ruling 238 (1)) |

Limits kept and unchanged, not listed above: fresh throwaway distro, 2 cores at nice 10, the 3600-second wall cap, the
8 GiB VHD cap, delete and unregister with a receipt, hash-bound inputs, the completed-L3 guard when a protected distro
is declared. Dropped: none.

## 5. Execution graph

| Node | Goal and inputs | Exit condition and oracle (red-first tests named) | Tier / capability | Depends on |
|---|---|---|---|---|
| P0 | Review this plan: an independent Adversary-mode review (Test Architect, Simplifier) recorded in the audit log and in §10; the operator rules DR-1..DR-3 | ACCEPT with no open hard veto; the rulings exist on main | T2 independent review | none |
| C1 | One module `architecture.py`: `detect()` (Windows side via `RuntimeInformation`/CIM through the launcher; Linux side via `dpkg --print-architecture`, `uname -m`, `nvidia-smi`) yields the canonical token `amd64` or `arm64` plus CPU and GPU names; the per-architecture table keyed by that token (rootfs name and SHA-256, Ubuntu archive host, NVIDIA repository path, native compiler package names, `make:<arch>`), read by `contract.py` and `transaction.py` and rendered into the sources list and the rootfs-verifier arguments; `host-identity.json` written before the first WSL effect and frozen by `manifest.py` | `test_f1_architecture.py`: `Detect_X64Fixture_Amd64`, `Detect_Arm64Fixture_Arm64`, `Detect_WindowsWslMismatch_Refuses`; `Table_Amd64Row_EqualsLiterals` carries its own literals (`8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea`, `ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz`, `archive.ubuntu.com/ubuntu`, `ubuntu2404/x86_64`, `make:amd64`), never the table; `Table_Arm64Row_EqualsLiterals` likewise from `sources-evidence.json`'s values; `Manifest_FreezeWithoutHostIdentity_Fails`; PowerShell `TargetCreate_WithoutHostIdentity_Refuses` (the launcher refuses `wsl --install` when the file is absent) | T0 deterministic | P0 |
| C2 | Derived paths: the target root from `$RepoRoot`'s parent; `Convert-LinuxPath` per drive letter; free-space floor on the target drive from the contract's `host_start_floor_bytes`/`host_reserve_bytes`; `preflight.sh` accepts `/mnt/<letter>/`; `Control.ps1` compares against the contract's recorded path | PowerShell fixtures on both runtimes: `TargetPath_RepoOnD_DerivesD`, `TargetPath_RepoOnC_DerivesOldConstant`, `TargetPath_ContractDiffersFromDerived_Refuses`, `LinuxPath_DriveD_MntD`, `HostFloor_ContractWithoutFloor_Refuses`; a grep gate `check-f1-host-bindings` finds no `C:\Projects`, `/mnt/c/`, `arch=amd64` or `x86_64` in packet code outside the allowlist (`C:\WINDOWS\System32\wsl.exe` in `attempt.py` KEEPALIVE and `Control.ps1`; the self-test fixtures in `transaction.py` and `packet_selftest.py`; the NVIDIA x86_64 row of the table) | T0 deterministic | P0 |
| C3 | Lock model: `transaction.py roots` takes the pinned roots (toolkit meta, OpenFOAM set, compilers, MPI, BLAS/LAPACK, `make:<arch>`); the `--b2-lock` input and the 135-row check leave; the run's own `transaction-lock.json` carries the `architecture` field (one file name) | `test_f1_transaction_roots.py`: `Roots_Arm64IndexFixture_Resolves` (the seven rows of §4.2 present as a fixture), `Roots_AbsentPin_Refuses`, `Roots_Amd64B2Fixture_ExpandsTo135Rows` (the pins expand over a fixture built from B2's recorded `apt-simulate.stdout.txt`, and the 135 names and versions are asserted against B2's committed `package-lock.json`, SHA-256 `2306d58e...`, so a pin that drops a package is caught) | T0 deterministic | C1 |
| C4 | Protected distro and rootfs verifier per DR-1: `protected` with a `none` state (guard binds when declared, records `not applicable` when none); the rootfs signature verified on the Windows side with the `gpgv.exe` found by walking up from `git --exec-path` (path and version recorded in `host-identity.json`) against a reviewed cloud-image keyring input `-UbuntuCloudimageKeyring`, hash-bound in the contract like the OpenFOAM and NVIDIA keyrings, so no distro is assumed | PowerShell on both runtimes: `Registrations_NoneDeclared_StockDistroOnly_Admitted`, `Registrations_ProtectedDeclaredButAbsent_Refuses`, `GuardState_None_RecordedInReceipt`, `Rootfs_UnapprovedSigner_RefusedOnWindows`, `Rootfs_GpgvAbsent_RefusesWithPath`; Python `Contract_CloudimageKeyringHashMismatch_Refuses` | T1 | P0 (DR-1) |
| C5 | Ruling 235 cycle-3 items, from the candidate on `win/gpu-f1-attempt2-contract` (46c0df8c): (i) its `expires_in_before` check (`Invoke-Preflight.ps1` lines 258-260 of that commit: the first event's `expires_in_before` is checked by `Assert-AttemptLeader` before the post-renewal sample) and its tests (`tests/f1_attempt2_evidence.ps1` lines 38-42: null, 199.999, a string, a boolean, NaN, infinity and a missing field all refuse) are carried; (ii) its stderr check at line 282 (`(Read-LiveText ...).Length -ne 0`, a decoded length, which accepts a BOM-only file) is replaced by `([IO.FileInfo]...).Length -ne 0`, and the sweep covers every other "must be empty" read in the packet; (iii) its `contract.py prepare` producer and `tests/test_f1_attempt2_prepare.py` are carried | red-first on both runtimes: `RenewStderr_BomOnly_Refused` (bytes EF BB BF; red on the candidate, which decodes it to length 0), `RenewStderr_WhitespaceOnly_Refused` (exists at line 44 of the candidate's suite), `RenewStderr_Empty_Accepted` (its fixture is a zero-byte file written with `WriteAllBytes`, never `WriteAllText('')`, whose default encoding may emit a preamble under 5.1); `FirstRenewEvent_ExpiresInBefore199_Refused` (carried); Python `Prepare_EmitsPair_HashesMatchCommitted` (carried, re-run on the laptop) | T1 | P0 |
| V | Verification: `packet_selftest.py`, `tests/test_f1_*.py`, the PowerShell `tests/f1_*.ps1` suites on 5.1 and 7, `check-docs`, `check-proof-pii`, the ring at the join | all green **and** the file counts are at least today's plus the new ones: Python `tests/test_f1_*.py` 6 today (+ `test_f1_architecture.py`, `test_f1_transaction_roots.py`, `test_f1_attempt2_prepare.py` = 9), PowerShell `tests/f1_*.ps1` 7 today (+ the C2 and C4 suites = 9); V refuses a smaller count | T0 deterministic, width 2 (Python and PowerShell) | C1..C5 |
| R | Independent source review (Adversary) and the audit entry; repair loop | ACCEPT; variant = open findings, floor 0, cap 2 cycles for the track | T2 independent review | V |
| A | Attempt-2 re-plan: worktree under `D:\Projects`, branch, session, run id, epoch chosen on the laptop; the laptop free-space worksheet; the prepare path emits the pair; the review is recorded; the live authorization ruling is committed with every `check_ruling` token; then the live attempt under Ruling 225 limits | the pair's hashes in the ruling; `INVOCATION-CONTRACT-PASS` before the first WSL effect; a receipt with host identity | T2 | R, DR-1..DR-3 |

**Shape decisions.** The first draft had eleven nodes (separate host-identity, source-table and receipt nodes, a
`-TargetRoot` parameter, a per-architecture lock file name, a run-time diff report against the B2 lock, and two
incidental edges). The Simplifier's review (§10) collapsed the three modules into C1 (one caller each, nothing lost: the
mismatch fixture and the regression test stay), deleted the parameter and the file name as second definitions of one
fact, deleted the report (no named reader; the regression fixture keeps its oracle), and deleted the incidental edges.
The Test Architect's review (§10) made every oracle carry its own failing input and named the tests above. C1, C2, C4
and C5 are independent in data but share two exclusive surfaces (`contract.py`, `Invoke-Preflight.ps1`), so edits stay
serial under one author; only V runs two branches. Gates P0 and R are immovable.

**Cost.** The previous packet cycles measured 23.1 s for the focused Python union and 18.4 s for the PowerShell suites
on the old PC (Verified there; Ruling 238 (3) says re-measure); the laptop's ring is 87-101 s (Verified, 2026-10-18);
token cost not recorded. **Degradation:** a red in V returns to the author (cycle 1); a second red stops the track
(cycle 2 is the cap); a DR answered "stop" ends the track with this plan as its record. **Re-plan checkpoints:** the
DR-1 ruling (ruled, Ruling 240); the launcher runtime on the ARM64 laptop is the native `pwsh` (C1 refuses an
emulated Windows PowerShell 5.1, so A's invocation names `pwsh`); the arm64 `simulate` stage's first real transaction (may add rows the probe did not
see, never a refusal of a pinned root that the index carries); the first `nvcc` print of the `sm_` list (Inferred
today); the Git layout for `gpgv.exe` (measured at C4 on the ARM64 laptop: `usr\bin\gpgv.exe` four levels above
`clangarm64\libexec\git-core`, found by walking up; the x64 layout stays Inferred until an x64 host runs the suite).

## 6. Decisions requested (before code)

- **DR-1, operator ruling.** The protected distro `cfdw-openfoam2512` is absent on the laptop (§4.1). Options:
  (a) rerun the B2 qualification on the laptop first so the protected distro, its GUID and the completed-L3 case exist
  on arm64, and F1 keeps the guard unchanged; (b) the protected binding becomes a contract field with a `none` state
  (the guard binds when one is declared and records "protected: none, guard: not applicable" when none is), and the
  rootfs signature is verified on the Windows side by the detected Git-for-Windows `gpgv.exe` against a reviewed
  cloud-image keyring input, so no distro is assumed (C4). **Recommendation: (b).** Ruling 217's "cfdw-openfoam2512
  untouched" holds vacuously; the guard protected B2's result, which this machine does not hold; (a) costs a full B2 run
  before any F1 work and still leaves x64 hosts without B2 unable to run the packet. (b) needs a ruling because Ruling
  225 lists the guard among the limits.
- **DR-2, reviewer confirmation.** The lock model of §4.2 (roots pinned, per-architecture lock produced by the run, B2
  lock out of the input set, its 135 rows kept as a regression fixture) satisfies Ruling 237 (1) "locked and hashed per
  architecture". The Simplifier confirmed it is the smaller shape (§10).
- **DR-3, reviewer acceptance of this plan** (Ruling 237 (4)), recorded in the audit log, with the hard vetoes cleared
  by someone other than the author.

**Ruled.** Ruling 240 (2026-10-19, operator: "rule DR-1 as (b), then start C1"): DR-1 is (b), as recommended; the
completed-L3 guard and the keepalive bind only when a protected distro is declared, and a `none` state records
"protected: none, guard: not applicable"; a B2 rerun on the laptop is not a precondition and stays available as its own
track. DR-2 and DR-3 are settled by the reviews in §10 (the Simplifier ACCEPT with conditions applied; the Test Architect
ACCEPT in cycle 2), so the plan's status is `accepted` and P0 is closed. Node C1 starts under the two-cycle cap.

## 7. Surface list (E7) and consistency

architecture module (C1: token, table, host identity) → contract fields (schema `cfdw-f1-r217-invocation/2`:
`architecture`, recorded `target_path`, per-architecture `inputs.rootfs`, `inputs.ubuntu_cloudimage_keyring`, roots
pins, `protected` with `none`, `host_start_floor_bytes`, `host_reserve_bytes`) → launcher asserts → Linux stage
arguments and apt sources → transaction and lock output → manifest and receipts (`host-identity.json`) → attempt-2
selections and the authorization tokens → packet README and the attempt-2 README. The schema bump is required: Ruling
237 (2) voids the `/1` contracts, and `check_contract_fields` equality-checks the literal path and the amd64 rootfs
hash, so `/1` code must refuse `/2` and the reverse. The canonical architecture token is the dpkg name (`amd64`,
`arm64`); the Windows name (`X64`, `Arm64`) is mapped once, in C1, and nowhere else. A test reads the token from every
surface of one prepared contract and asserts they agree (`Contract_ArchitectureToken_AgreesAcrossSurfaces`).

## 8. Residual risks, named

- `nvcc` and the `sm_` list for cc 12.1, and the PETSc-CUDA build on arm64, stay Inferred until the packet's later phases
  run (outside this preflight's scope, as on x64).
- The arm64 rootfs bytes and signature are unverified until C4's Windows-side gpgv runs on the downloaded file; the plan
  pins the index's hash, not observed bytes.
- Lock drift at attempt time on either architecture: the `simulate` stage asks for the pinned roots; a root the suite no
  longer carries refuses as today. The pins the publishers keep (toolkit 13.2.2-1, OpenFOAM 2512.0-2) are the roots.
- Index signatures: the probes read indices unsigned; the packet's repositories stage verifies every InRelease with gpgv
  and the contract's allowlists, and the ports host must be added to that map (C1) or the stage refuses with
  "unexpected apt index".
- The x64 path is regression-tested through the amd64 row's literal test (C1) and the PowerShell fixtures at `C:\`; it
  is not live-run until the Ruling 238 (2) rerun. The x64 Git-for-Windows layout for `gpgv.exe` is Inferred until that
  rerun; C4 refuses with the path named rather than guessing.

## 9. Ledger

Planned: the probes and this plan in one docs-only join. Actual: filled at the join (the audit entry records duration).
Planned-vs-actual for C1..A is recorded here, one line per node, when each node joins; the attempt-2 proof
carries the run itself.

- **C1 (2026-10-19).** Planned: one module, the table, `host-identity.json`, seven named tests, T0. Actual:
  `architecture.py` (the token from CIM's OS architecture checked against the process view, the table, `sources`,
  `host-identity` and `check`), `contract.py` schema `/2` with `architecture` and the per-architecture rootfs pin,
  `attempt.py` `input_hashes(token)`, `transaction.py` `roots --architecture` and `make:<arch>`, `manifest.py`
  requires the receipt, `preflight.sh` takes `--architecture` (22 arguments), writes `guest-identity.txt` in `probe`
  and renders the sources from the table, `verify-rootfs.sh` takes the token, the launcher records
  `host-observations.json`, writes the receipt before the first WSL effect, asserts it before `wsl --install` and
  checks the guest after `probe`; the stage-scope pin re-based (packet_selftest). Tests: `test_f1_architecture.py`
  15 cases (0.5 s), `f1_architecture.ps1` on 5.1 (2.9 s) and 7 (2.2 s); 130 Python tests green; both PowerShell
  runtimes green on the eight F1 suites and the packet self-test. Finding beyond the plan: register entry
  EMULATED-PROCESS-REPORTS-AN-EMULATED-OS (Windows PowerShell 5.1 on the ARM64 laptop runs as x64 from an x64
  parent; the token therefore comes from CIM and an emulated launcher is refused). Duration: the join's audit entry.
- **C2 (2026-10-19).** Planned: derived target path, `/mnt/<letter>/`, floors as contract fields, the grep gate, five named
  PowerShell tests. Actual: `Control.ps1` `Get-DerivedTargetPath` and `Get-ContractHostFloors`; the launcher derives
  `$TargetPath` from `$RepoRoot`, `Assert-TargetPath` requires the bound, derived and contract paths to agree,
  `Convert-LinuxPath` and `Get-HostFree` follow the drive letter, the floors are read from the contract after the
  precheck, `Assert-FreshRegistrations` takes the target path; `contract.py` `derived_target_path`, `is_target_path`,
  `check_target_path_derived` (run by `verify`), the `target-path` CLI, and the floor fields; `attempt.host_limits`;
  `preflight.sh` accepts `/mnt/<letter>/`; the gate `tools/check-f1-host-bindings.py` (17 allowlisted fixture and
  table lines, wired into check-docs). Tests: `tests/f1_derived_paths.ps1` (both runtimes; the Python and PowerShell
  derivations proved equal), `tests/test_f1_derived_paths.py` (5 cases); 135 Python tests green. Added at the operator's
  request: the ring runs every `tests/f1_*.ps1` on pwsh and on Windows PowerShell 5.1 started from pwsh
  (`tools/run-f1-powershell-suites.ps1`, 15685 ms for 18 runs on the laptop), failing `F1-PS-EMULATED` or
  `F1-PS-MODULES` before any suite. Finding beyond the plan: register entry PWSH-CHILD-INHERITS-CORE-MODULE-PATH (a 5.1
  child of a pwsh script loses `Get-FileHash` until it gets its own module path). Duration: the join's audit entry.
- **C3 (2026-10-19).** Planned: pinned roots, `--b2-lock` and the 135-row check leave, the lock carries `architecture`,
  three named tests. Actual: `transaction.py` `PINS` (the toolkit and OpenFOAM release roots, the tool names,
  `make:<arch>`), `pinned_roots`, `check_pins` (run by `lock` before any index read), the lock schema `/2` with
  `architecture` and `pins` and a per-row architecture check; `roots` takes `--architecture` only. Beyond the node's
  text, as section 4.2 says: the B2 lock left the input set (`contract.INPUT_KEYS` and `attempt.INPUT_HASHES` are
  six, `preflight.sh` takes `--b2-licences` in the slot `--b2-lock` held, the launcher passes `license-map.json`,
  the final precheck drops `b2_lock_sha256`; a contract still listing the lock refuses as `exact six reviewed
  inputs`). Tests: `tests/test_f1_transaction_roots.py` (the three named tests, plus the lock document carrying
  `architecture` and the input-set exit); 140 Python tests green; 9 PowerShell suites green on both native
  runtimes (16810 ms for 18 runs); the packet self-tests green; the stage-scope pin re-based. The
  LOCK-PINS-A-MOVING-SUITE control now names these tests. Duration: the join's audit entry.
- **C4 (2026-10-19).** Planned: `protected` with a `none` state, the rootfs signature verified on the Windows side by the
  `gpgv.exe` found from `git --exec-path` against a reviewed cloud-image keyring input, five PowerShell tests and one
  Python test. Actual: `contract.PROTECTED_STATES`; `Assert-FreshRegistrations` takes the state (`none` requires no
  protected row, other rows are neither required nor refused); `Assert-AttemptExclusivity` requires no keepalive under
  `none` and still refuses a foreign WSL actor; the launcher binds `$Protected` from the contract after the precheck,
  starts the guard, reads the registration, captures keepalive processes and requires the protected row only when one
  is declared; `Assert-Guard` returns `not applicable` under `none`; `New-ProtectedRecord` puts `protected` and `guard`
  into the final precheck and the cleanup receipt; `attempt.authorization` projects `not applicable` for the key, the
  path and the keepalive under `none` (same keys, so the committed attempt-1 projection still verifies). Signature:
  `Get-GpgvPath` walks up from `git --exec-path` (on the laptop `usr\gpgv.exe` is four levels above
  `clangarm64\libexec\git-core`), `Get-GpgvObservation` never throws and the receipt's `gpgv` block records path and
  version, `Invoke-RootfsSignature` runs `contract.py keyring` (with Git's `gpg.exe`), `gpgv --status-fd 1`,
  `contract.py signer` and the signed sums line against the contract pin and the measured file; `verify-rootfs.sh` and
  the `rootfs-name` CLI are deleted. The keyring is a seventh contract input (`ubuntu_cloudimage_keyring`, the
  committed copy `docs/proof/win-gpu-f1-preflight/inputs/ubuntu-cloudimage-keyring.gpg` from Ubuntu-24.04's
  ubuntu-keyring 2023.11.28.1, bound under the execution worktree for attempt 2 like the B2 licence map). Finding:
  the MSYS `gpgv` reads a drive-letter `--keyring` as a URL scheme (register entry MSYS-TOOL-READS-A-DRIVE-LETTER-AS-A-URL;
  `Convert-MsysPath`); register entry VERIFIER-RUNS-INSIDE-A-COINCIDENT-ARTIFACT records the class the node fixes. Tests:
  `tests/f1_protected_none.ps1` (the five named tests plus the exclusivity, MSYS-path and tampered-rootfs cases, over a
  committed fixture signed by a throwaway key, with the real `gpgv.exe` on both runtimes) and
  `tests/test_f1_protected_none.py` (4 cases); 144 Python tests green; 10 PowerShell suites green on both native
  runtimes (22834 ms for 20 runs); the packet self-tests green. Duration: the join's audit entry.
- **C5 (2026-10-19).** Planned: carry the Ruling 235 cycle-3 items from the candidate 46c0df8c: (i) `expires_in_before`
  and its tests, (ii) the stderr check by byte length with the sweep, (iii) the `prepare` producer and its tests.
  Actual: (i) `leader_supervisor.renew` returns `expires_in_before` read before the CAS write (the two lines keep their
  order); `Get-AttemptLeaderEvidence` refuses a first event without the field and runs `Assert-AttemptLeader` on it;
  the candidate's evidence cases (null, 199.999, a string, a boolean, NaN, infinity, missing) and its renewal cases
  (200.125 before, never 900 after) carried; the candidate's `ParentProcessId` mutation case in the controls suite
  carried. (ii) `([IO.FileInfo] ...).Length -ne 0`; the sweep over `Invoke-Preflight.ps1`, `Control.ps1`,
  `preflight.sh`, the Python modules and `recovery/` found that read to be the only must-be-empty read (the apt stderr
  files are recorded, never required empty); `RenewStderr_BomOnly_Refused` was red on main (`Read-LiveText` strips the
  BOM, `.Trim()` then saw ''), `RenewStderr_WhitespaceOnly_Refused`, `RenewStderr_Empty_Accepted` (zero bytes via
  `WriteAllBytes`). (iii) `contract.prepare` re-based on the `/2` contract: `--architecture`, `--protected`, the two
  host floors, the target path derived from the future worktree, the two repository inputs (B2 licence map, cloud-image
  keyring) projected under it, seven inputs; `tests/test_f1_attempt2_prepare.py` (8 cases, real Git). Not carried: the
  candidate's `docs/plans/f1-attempt2-contract.md` (the old PC's track; this plan supersedes it) and its contract
  literals (`/1`, the old target path). Register: F1-PRECONDITION-AFTER-MUTATION-A and
  F1-PREPARATION-EXECUTION-CONFLATION-A carried with their controls named on main; EMPTY-MEANS-ZERO-BYTES new.
  152 Python tests green (11 `tests/test_f1_*.py`, V's floor is 9); 10 PowerShell suites green on both native
  runtimes (22014 ms for 20 runs; V's floor is 9); the packet self-tests green. Duration: the join's audit entry.
- **V (2026-10-19).** Planned: the union green on both runtimes, the file counts at least the floors, width 2. Actual:
  `tools/run-f1-verification.py` runs the packet README's whole check list (authority scan, Python and PowerShell
  packet self-tests, the unit suites and the PowerShell runner concurrently, the three gates, `check-docs`), refuses
  a suite count below the floors before anything runs (`--self-test` plants a smaller count and a red step), and
  writes the receipt `docs/proof/win-gpu-f1-preflight/rescope/verification-v.json` (HEAD, `worktree_clean`, counts,
  per-step command, exit and seconds; no machine path). Counts: 11 Python suites (floor 9), 10 PowerShell suites
  (floor 9), 152 Python tests, 20 PowerShell runs. Two reds before green: the authority scan refused the C3
  self-test's quoted `"make"` literal (the scan ran in no ring; now a `check-docs` sub-check, register entry
  LISTED-CHECK-IN-NO-RING; the fixture names are not build tools), and the proof-PII gate refused the first
  receipt's scratch path (the runner scrubs `<repo>` and `<scratch>`). The receipt is the code-stage proof pack owed
  at R; its exact seconds are in the file. Duration: the join's audit entry.
- **R (2026-10-19).** Planned: an independent Adversary review, ACCEPT, floor 0 open findings, cap 2 cycles. Actual: two
  reviewers convened in parallel over main `9e047b28` with the V receipt (Test Architect; Security & Identity Architect),
  both ACCEPT WITH CONDITIONS, no veto (reports verbatim in `docs/reviews/f1-rescope-r237-node-r.md`; dispositions in
  §10). Cycle 1 applied every condition and three advisories: the pin oracle and every-pin absence; the structural halves
  on the AST with the launcher's order; the contract read once with `--expected-sha256` on verify; the protected identity
  refused under `none` (DR-4 requested, fallback: the code stands); the legacy no-attempt contract retired at admission;
  gpg with its own home; the host-bindings gate on any drive; the keyring provenance README. Two findings of the repair
  itself: Git's gpg reads a Windows `--homedir` relative to its MSYS root (the same class as MSYS-TOOL-READS-A-DRIVE-LETTER-AS-A-URL;
  the MSYS form is passed), and the admission suite's span anchor matched the first of two contract reads, which is
  why its cases passed before (the anchor is the single read now). Open advisories carried to A: test-count floors,
  message-matching `Refuses`, a hashed packet copy, the fingerprint confirmation. 152 Python tests green; 10
  PowerShell suites green on both native runtimes (24160 ms for 20 runs); the V receipt regenerated on the R
  commit. Cycle 2: both reviewers CONFIRMED every condition (mutation and probe evidence in the review doc §3) and
  jointly asked for a failing input on the `--expected-sha256` path, applied in the same cycle (Python and the
  admission stub); the cap was not reached. R is ACCEPT. Duration: the join's audit entry.
- **A (2026-10-20).** Planned: the attempt-2 re-plan on the laptop, the pair from `contract.py prepare`, the ruling with
  every `check_ruling` token, then one live attempt under Ruling 225. Actual: A1 the re-plan and the inputs receipt (every
  input at its pin; a first fetch that returned a directory index was caught by the hash); A2 the pair at source
  `a7f20025` (contract `2fb4815d…`, proposal `673f7667…`, epoch 3 of the `mac` designation kept live by a renewer because
  the pin's TTL is at most 300 s); A3 Ruling 242 (the accepted authorization `1a160eaf…` committed in place of the
  proposal, `check_ruling` green against the local register before the push; the cloud-image fingerprints confirmed by a
  reviewer other than the author). The live attempt (execution worktree created from `803fb83d`, native pwsh 7.6.7 arm64,
  session `pc-f1-attempt2`): `INVOCATION-CONTRACT-PASS` and `HOST-IDENTITY-PASS architecture=arm64`, then
  `F1-RENEW-START-FAILED` at second 6: `leader_supervisor.admit` projected five input names while `contract.verify`
  reads six since C4 (`AttributeError`), stage `not-started`, no WSL effect, target absent; cleanup also recorded
  `F1-EXCLUSIVITY-REFUSED: additional or changed WSL actor` (the harness's Git-for-Windows `bash.exe` matched by name)
  and the two consequent missing-evidence errors. Checkpoint `f1d3a22f` and final `32e6e384` joined main (`b1147fb1`).
  Attempt 2 of Ruling 225's three is consumed; one remains. Repair (this node, cycle 1 of the track's cap): one input
  list (`contract.ARGUMENT_INPUTS`) read by `verify` and `admit`, with a coverage test (red `['ubuntu_cloudimage_keyring']`
  then green); the exclusivity refusal names the actor (PowerShell assertion, red then green); register entries
  HAND-COPIED-PROJECTION-OF-AN-AUTHORITY-LIST, REFUSAL-NAMES-THE-CLASS-NOT-THE-ACTOR,
  NAME-MATCH-COUNTS-A-FOREIGN-TOOL-AS-AN-ACTOR. Open: DR-5 (`req-01M5CYY968W5KQBH0HE903NZT9`, how the scan treats Git's
  `bash.exe`); the next attempt needs its own pair at the repaired source and its own ruling. Duration: the join's audit
  entry.
- **A, Ruling 243 (2026-10-20).** Planned: DR-5 ruled, attempt 3 prepared. Actual: the operator ruled DR-5 as (a)
  and directed attempt 3, the last under Ruling 225's cap. Finding on the way: the packet bound the fresh-attempt scheme
  to the literal 2 in six places (`attempt.check_binding`, `contract.prepare`/`verify`, `leader_supervisor`,
  `Control.ps1 Get-InvocationRunRoot`, eleven launcher gates and three literal `2` arguments), so "prepare attempt 3"
  needed the scheme keyed once: `attempt.FRESH_ATTEMPTS = (2, 3)`, `contract.fresh_attempt`, `prepare --attempt`, names
  derived from the number, the launcher gates `-ge 2`. Red-first on both sides: attempt 3 admitted with derived names and
  refused with mixed ones; `--attempt` missing or foreign refuses without outputs; the supervisor's entry admits 2 and 3
  only; no literal `== 2` gate left in the packet's Python (a structural test); `Get-InvocationRunRoot` for attempt 3;
  the `bash.exe` path rule with five planted cases. One repair of the repair: a redundant attempt-number guard added to
  the launcher changed the reviewed admission refusal order (`f1_attempt_controls.ps1` showed it) and was dropped, since
  `Get-InvocationRunRoot` already refuses a foreign number. `docs/proof/win-gpu-f1-preflight/attempt-3/README.md` is the
  re-plan. Duration: the join's audit entry.
- **A, attempt 3 (2026-10-20, Ruling 244).** Planned: the last live attempt under Ruling 225. Actual: the pair at `5995f939`
  (epoch 4), Ruling 244 with every `check_ruling` token, the accepted authorization `f419edc7…` on origin/main, the
  execution worktree from `6b1bf813`, the launcher from native pwsh with no Bash-tool call in flight. It passed the
  contract, the host identity, the rootfs signature (`ROOTFS-SIGNED-PASS`), the leader activation and the first
  `wsl --list`, then refused `F1-OWN-LAUNCHER-IDENTITY` on the second `wsl --list` at second 10: the own-child identity
  equates a CIM creation time (microseconds) with `Process.StartTime` (100 ns ticks), unequal for nine processes in
  ten (a probe: 8 ticks apart). Stage `not-started`, no target, registrations unchanged. The cleanup then could not
  bind its checkpoint: the attempt-3 run root had no `-text` rule, Git normalized two captures, `manifest.py verify`
  refused, and the final commit was withheld; the launcher's checkpoint `0e24418f` and the captured bytes with the
  live receipts (`1d2b9695`, attributes added, bind green) joined main. Attempts consumed under Ruling 225: 1, 2
  and 3; **the cap is reached**, and the track's repair cap (two cycles) is spent, so the track stops here and the
  controls for TIMESTAMP-IDENTITY-ACROSS-PRECISIONS and RAW-CAPTURE-BLOB-MISMATCH recurrence 4 are proposed to the
  operator, not applied. Duration: the join's audit entry.
- **Rehearsal (2026-10-20, Ruling 245).** Planned: a new cap of three attempts (4, 5, 6), the repair loop restarted, a
  dry rehearsal of the launcher's Windows-side paths red-first, then the two controls. Actual: `tools/run-f1-dry-rehearsal.ps1`
  (own-child registration against real `cmd.exe` children, the run root's capture attributes, a freeze-commit-bind cycle
  in a no-checkout scratch worktree; receipt `cfdw-f1-dry-rehearsal/1`) ran red at the attempt-3 source (`own-child`
  0/5 and 1/5; `attributes` 0/3 for attempt-4; `bind` the exact `commit blob differs` refusal) and green at the repaired
  source for attempt-3; for attempt-4 it stays red on `attributes` and `bind` until `prepare --attempt 4` writes the
  rules, which is the control working. Controls: `Test-OwnLauncherCreation` (microsecond truncation), `Assert-CaptureAttributes`
  in the launcher after the precheck, `contract.py prepare` writing the run root's `-text` lines, `FRESH_ATTEMPTS`
  to 6; red-first on both runtimes and in Python. Harness defects on first contact, both recorded: a `$receipt` local
  shadowed the `-Receipt` parameter (CASE-INSENSITIVE-VARIABLE-SHADOWS-A-PARAMETER) and a full scratch checkout hit
  the repository's long paths (no checkout now). One placement repair: the attribute assertion first sat before the
  contract precheck and changed the admission span's refusal order (`f1_attempt_controls.ps1` showed it); it now
  follows the precheck. The rehearsal joined `tools/run-f1-verification.py` as the `dry-rehearsal` step against the
  highest `attempt-<n>` run root. 160 Python tests, 10 PowerShell suites on both runtimes; receipts in `docs/proof/win-gpu-f1-preflight/rehearsal/`. Next: attempt 4
  (pair at this source, rehearsal green for attempt-4 after prepare, its own ruling). Duration: the join's audit entry.

## 10. Gate record

| Reviewer (Adversary) | Verdict | Conditions and disposition |
|---|---|---|
| The Simplifier | ACCEPT with two conditions | (1) `-TargetRoot` was a second definition of the path: deleted, derive only (§4.3). (2) DR-1 (b) named the stock `Ubuntu-24.04` distro as the rootfs verifier, which assumed a distro: replaced by the detected Git-for-Windows `gpgv.exe` with a reviewed keyring input (C4; `gpgv.exe` 2.4.9 Verified present on the laptop). Also applied: C1+C2+C7 collapsed into C1; the run-time diff report, the per-architecture lock file name, the mermaid copy of the dependency column and two incidental edges deleted; the schema bump and `host-identity.json` kept as required by Rulings 237 (2) and 238 (1). |
| Test Architect, cycle 1 | VETO (two blockers) | (1) Five §3 facts were labelled Verified with no committed receipt: `sources_evidence_probe.py` → `sources-evidence.json` (rootfs checksums, amd64 index drift) and `host_identity_probe.ps1` + `wsl_tools_check.sh` → `host-identity-evidence.pwsh.json` and `.ps51.json` (host identifiers per runtime, registrations, stock-distro tools and keyrings, Git gpgv) now hold every observation, and §3 cites them per row. (2) C5 named no tests and cited an unlocated candidate check: the candidate's checks are cited by line (258-260, 282 of 46c0df8c; tests at 38-44) and five red-first tests are named. Also applied: C1's literal regression test (its own literals); the free-space floor numbers and the registration-set change stated in §4.3 with tests; C4's oracles named; V asserts file counts (6 Python and 7 PowerShell today, the "eight suites" figure corrected); the per-runtime measurement (both runtimes report Arm64, so the mismatch oracle holds under 5.1); `TargetCreate_WithoutHostIdentity_Refuses`; the grep-gate allowlist named. |
| Test Architect, cycle 2 | ACCEPT (two minor conditions, applied) | Both blockers cleared: every §3 value was found in its receipt; the candidate's lines were confirmed with `git show`, and every named test has a failing input. Conditions applied: C3's regression asserts the 135 names and versions against B2's committed `package-lock.json`, not the simulate output alone; C5's empty-stderr fixture is a zero-byte file written with `WriteAllBytes`. The code-stage proof pack is owed at R. |
| Test Architect, node R | ACCEPT WITH CONDITIONS (two conditions, four advisories; applied at R) | Full report: `docs/reviews/f1-rescope-r237-node-r.md` §1. F1 (condition): the pin set had no oracle outside the code; `tests/test_f1_transaction_roots.py` now carries the 15-pin literal `EXPECTED_PINS`, asserts `pinned_roots` equal to it for both tokens, and `Roots_AbsentPin_Refuses` loops the deletion over every pin. F2 (condition): the structural halves were raw-text searches a comment satisfies; `tests/f1_architecture.ps1` and `tests/f1_protected_none.ps1` now query the AST (top-level `CommandAst`, `HashtableAst` pairs, `BinaryExpressionAst`, `AssignmentStatementAst`, string constants) and assert the launcher order host identity &lt; rootfs signature &lt; guard start &lt; host identity again &lt; target creation. F3 (advisory): the V receipt is regenerated on the R commit. F4 (advisory, open): test-count floors when the file floors next move. F5 (advisory, open): `Refuses` helpers match the message. F6 (advisory, applied): `check-f1-host-bindings.py` catches a Projects root on any drive and any `/mnt/<letter>/`. |
| Security & Identity Architect, node R | ACCEPT WITH CONDITIONS (three conditions, three advisories; applied at R, one under DR-4) | Full report: `docs/reviews/f1-rescope-r237-node-r.md` §2. F1 (condition, applied): the launcher parsed the contract twice and hashed it after verify; it now reads the bytes once, hashes them, parses from them, and passes `--expected-sha256` to `contract.py verify`, which refuses other bytes (register entry READ-ONCE-VERIFY-ONCE). F2 (condition, applied under DR-4): under `protected: none` a registration carrying the protected name, GUID or path now refuses in `Assert-FreshRegistrations` and in the final precheck; other rows stay neither required nor refused; the operator's ruling on DR-4 amends or reverts it. F3 (condition, applied at the launcher): a contract without `attempt` is retired at admission (`a contract without attempt is retired`); the Python fixtures that still use the run-1 shape offline are not a live path; collapsing the `$freshAttempt` conditionals is a next step for node A. F4 (advisory, applied in part): `docs/proof/win-gpu-f1-preflight/inputs/README.md` records the keyring's package provenance measured from the local index; the fingerprint confirmation against Ubuntu's published keys stays Flagged for a reviewer before node A. F5 (advisory, open): run a hashed copy of the packet from `$Live`. F6 (advisory, applied): `verify_keyring` runs gpg with `--no-options --homedir <temp>` (MSYS form on Windows). |
| Both reviewers, node R cycle 2 | CONFIRMED (every condition met, Verified by mutation and probe at `26faf712`) | `docs/reviews/f1-rescope-r237-node-r.md` §3. Joint follow-up, applied in the same cycle: a failing input for the `--expected-sha256` path (`tests/test_f1_attempt_admission.py` `RealVerify_ExpectedSha256_BoundToParsedBytes`; the admission stub requires the forwarded hash on both runtimes). Open for node A: test-count floors (TA F4), message-matching `Refuses` (TA F5), a hashed packet copy run from `$Live` (SEC F5), the keyring fingerprint confirmation by a non-author reviewer (SEC F4). R is ACCEPT. |
| Security & Identity Architect, node A (Ruling 243 (1)) | ACCEPT WITH CONDITIONS (two conditions, applied in the node; one advisory adopted) | Full report: `docs/reviews/f1-rescope-r237-node-a.md`. F1 (condition, applied): the System32 path compare was not canonical (WMI reports the path as launched, `..` included); now `[IO.Path]::GetFullPath` after stripping the `\\?\` prefix, dotted and prefixed System32 paths planted (refuse) and a dotted Git path (passes). F2 (condition, applied): a `sed` pass had eaten the backslashes of the planted Git bash command line; restored verbatim. F3 (advisory, adopted): the scan is a hygiene control over the launcher own process population, not a boundary against a hostile peer; stated in the code and the register. Residual risk accepted with the reviewer rationale. |
