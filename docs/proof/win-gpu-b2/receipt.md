---
id: receipt-win-gpu-b2-r209
title: "Windows GPU B2 CUDA 13.2 execution receipt"
type: proof-pack
status: complete
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, wsl, cuda, gpu, ruling-200, ruling-209]
links:
  - { to: proof-win-gpu-b2, rel: relates-to }
  - { to: proof-win-l3-observer, rel: depends-on }
review-by: "2026-11-10"
summary: >-
  The first live Ruling 209 attempt installed the pinned CUDA 13.2 recipe and passed a manifest-bound managed-memory SAXPY proof, then the disposable distro was unregistered.
---

# Windows GPU B2 CUDA 13.2 execution receipt

**Verdict: Verified execution success and rollback complete.** Session `pc-b2-r209-20261010-run4` is the first live
B2 attempt under Ruling 209. It installed only the accepted 135-package CUDA 13.2 lock in disposable distro
`cfdw-cuda132-b2`, ran the owned GPU workload, and exited with no residual owned process. The protected
`cfdw-openfoam2512` terminal projection remained byte-for-byte unchanged through 29 five-second samples and the final
snapshot. `capture-manifest.json` binds the committed receipt, sources, accepted inputs, and captured outputs.

## Authority, attempts, and bounds

Rulings 200 and 209 authorize this execution. The Astra owner accepted the phase-B inputs at `5d884ecc`. Ruling 209
allows up to five live attempts and substitutes the completed-run guard for the earlier live-throughput guard.

| Run | Target operation | Live B2 attempt | Result |
|---|---:|---:|---|
| `pc-b2-r200-20261010-run1` | None | No | The earlier live observer rejected the already completed L3 unit before B2 ownership launch. |
| `pc-b2-r209-20261010-run2` | None | No | Windows PowerShell 5.1 rejected an unsupported `Contains` overload before the first WSL command. |
| `pc-b2-r209-20261010-run3` | Protected snapshot only | No | The wrapper stopped before observer or target launch when a redirected process exit code was blank. |
| `pc-b2-r209-20261010-run4` | Install and GPU test | Yes, attempt 1 of 5 | Passed in 143,744 ms workload time and 144,591 ms through cleanup. |

**Verified:** the run used `taskset --cpu-list 0-1` and `nice -n 10`; its foreground process was PID/PGID/SID
`410/410/410`. The ownership check and both residual checks exited 0. The wrapper and workload exited 0.

## Pinned installation

**Verified:** the before-state contained 528 packages. The frozen recipe contained 135 exact package/version/
architecture records: 127 new packages, eight upgrades, zero removals, and 3,405,443,104 authenticated download bytes.
The payload budget was 8,092,712,328 bytes with 25,456,799,144 required free bytes. The after-state contained 655
packages; all 135 locked records changed to their pinned state, and the forbidden-package set was empty. `nvcc`
reported CUDA 13.2, V13.2.86, at `/usr/local/cuda-13.2/bin/nvcc`.

## Protected L3 guard

**Verified:** the pre- and post-run canonical snapshots both have SHA-256
`42e1d0bed05aa071a70fba8445474a85fa43a1746a394adc4e46a433a090aab9`. They bind the completed unit state
`success/0/0`, inactive/dead, final iteration 120000, one `End` marker, one `Finalising` marker, the 79,738,991-byte
log, its modification time, tail SHA-256, and full SHA-256. The guard recorded 29 `continue` samples at a nominal
five-second cadence (measured monotonic gaps 5.031–5.057 seconds) with zero excluded trailing bytes. Its 23,169-byte
event projection has SHA-256
`ed7ea1bad151c5260671fd148a8b61f347824180b62b721cbda258646a28c95c`; it stopped with exit 0 and confirmed process
termination.

## GPU result

**Verified:** CUDA selected one `NVIDIA GeForce RTX 3080 Ti Laptop GPU`, compute capability 8.6, driver/runtime
version 13020, with managed memory reported available and concurrent managed access reported unavailable. The program
allocated 134,217,732 bytes for 16,777,216 float32 elements, completed six device iterations, wrote device marker 6,
and compared every output element with a finite CPU oracle. It recorded zero mismatches, maximum absolute error
`1.907348633e-06`, maximum relative error `2.67954013e-07`, and tolerance `0.0001`. The first kernel took
84.88857269 ms; the five warm kernels averaged 82.66300964 ms.

**Inferred:** this result supports using this laptop's NVIDIA GPU for later approved compute experiments. It does not
measure end-to-end application acceleration.

**Flagged:** the device reports `concurrent_managed_access=0`; future workload design must account for that constraint.
This toy proves CUDA managed allocation and device execution. It does not prove OpenFOAM or PETSc GPU offload, broad
memory-transfer performance, or F1. F1 remains closed and requires a leader decision request.

## Evidence and rollback gate

The run evidence is retained under `attempt-4-success/`. `capture.json` records the executed argv, source hashes,
timings, guard result, ownership result, and SAXPY result. `saxpy.sha256.txt` binds the source, binary, result, and
device marker. `capture-manifest.json` binds these files to their committed Git blobs.

The proof checkpoint was committed and pushed at `80eb82a6` before rollback. The first rollback wrapper invocation
failed closed before unregister because the Linux-only snapshot collector was launched in host Python. The second
invocation ran that collector inside `cfdw-openfoam2512` and passed every gate: exact disposable marker, Ubuntu 24.04,
recorded install path, zero residual process, both distro identities, and unchanged protected snapshot. The exact
command `wsl.exe --unregister cfdw-cuda132-b2` exited 0. The closeout proves the target name, registry entry, and install
directory absent; `cfdw-openfoam2512` remains present, with matching pre/post SHA-256
`42e1d0bed05aa071a70fba8445474a85fa43a1746a394adc4e46a433a090aab9`.

## Integrated Windows ring

**Verified:** after merging WLK from `origin/main` at `eea2c7f1`, the full Windows ring exited 1 in 147.246 seconds.
It reported 37 named product failures: Core 11, Desktop 25, Analysis 1, and CLI 0. Three Desktop child modes timed out
at 120 seconds. Source-order localization names their first uncompleted checks, and isolated reruns show that all three
terminate promptly outside the contended harness. The Analysis golden mismatch expected
`9713863597250cd245c4d70560abee8840c002f6ff33501127ba331ac8efad62` and observed
`752ebf3bb52860261175c7f04c38c2df04488e09b437f84be4ef31a8479bd865`. The exact inventory and limitations are in
`windows-ring-wlk-rerun-summary.txt`; the raw and isolated outputs are manifest-bound beside it.

## Ruling 213 redaction

**Verified:** the two retained Windows ring stdout captures originally included the local hostname in repeated stack
trace paths. Ruling 213 requires source redaction before join. Every case-insensitive occurrence in
`windows-ring.stdout.txt` and `windows-ring-wlk-rerun.stdout.txt` is now the literal placeholder `<host>`. The B2
manifest binds the redacted Git blobs, and the proof-PII gate was rerun with the local hostname supplied through the
uncommitted `CFD_PII_HOSTNAMES` environment variable.
