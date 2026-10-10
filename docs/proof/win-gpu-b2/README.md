---
id: proof-win-gpu-b2
title: "Windows GPU B2 disposable CUDA proof"
type: proof-pack
status: blocked
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, wsl, cuda, gpu, ruling-200]
links:
  - { to: proof-win-l3-observer, rel: depends-on }
review-by: "2026-11-10"
summary: >-
  Ruling 200 proof for an exact CUDA 13.2 install and managed-memory toy in a fresh disposable Ubuntu 24.04 WSL distro.
---

# Windows GPU B2 disposable CUDA proof

B2 uses only the WSL distro named `cfdw-cuda132-b2`. It never installs into or changes `cfdw-openfoam2512`.
The first phase verified the NVIDIA repository signature and retained metadata, created a fresh Ubuntu 24.04 distro,
pins the CUDA 13.2 config packages against repository drift, updates signed apt indices, and captures an exact apt
simulation. It passed with CUDA root `13.2.2-1`, shared config packages pinned to `13.2.86-1`, 127 new packages,
eight upgrades, and no package matching the Linux-driver or CUDA 13.3/13.4 exclusions. No CUDA package is installed.

The exact dependency lock, downloaded package hashes, license map, payload measurement, capacity budget, and package
exclusions passed. The Astra owner accepted the phase-B installation boundary at commit `5d884ecc`. Installation may
now use only that accepted lock under the live Ruling 200 guard. The toy uses `cudaMallocManaged`, identifies the CUDA
device, checks every SAXPY output element, runs at `nice 10` on CPUs 0-1, and records first and warm kernel timings.

Phase B passed in session `pc-b2-r200-20261010-lock4`. The installable lock contains 135 exact package/version/
architecture records: 127 new packages, eight upgrades, zero removals, and 3,405,443,104 authenticated download bytes.
Every cached archive matches its locked SHA-256 and size. Streaming the package payloads measured 8,092,712,328 bytes.
The license map covers all 135 packages through an embedded notice, a uniquely resolved Debian shared-document notice,
or the versioned CUDA 13.2.2 EULA. The capacity gate requires 25,456,799,144 bytes and passed on both the WSL filesystem
and its Windows backing volume. A current `dpkg-query -W` check found 528 installed base packages and zero CUDA toolkit,
CUDA library, or Nsight package matches. `accepted-inputs.sha256` freezes the reviewed phase-B bytes for execution.

Rollback is `wsl.exe --unregister cfdw-cuda132-b2`, but only after the target's marker and exact distro identity are
verified. F1 stays closed; B2 can only produce its decision-request evidence.

## Execution status

The committed execution source at `c7569700` passed Astra review. Its non-installing preflight then passed the frozen
input hashes, current 528-package state, exact 135-package resimulation, archive verification, license map, and capacity
gate. `attempt-0-preflight/` retains the before-state and simulation evidence. The preflight left no owned process and
installed no CUDA package.

Execution attempt `pc-b2-r200-20261010-run1` stopped before ownership launch or package installation. Fresh observer row
17 reported `unit_active_state=inactive`, so the five-percent guard failed closed with `L3 unit is not active`. The
bound 17-row prefix is 5,336 bytes with SHA-256 `d09e44f0d519ba16b5a642f4c9e58e28a1a9bc5482431d18a8467f7352d01f4d`.
The retained read-only completion evidence shows the protected run reached `Time = 120000`, wrote `End`, finalized its
parallel run, and its unit reports `Result=success`, `ExecMainStatus=0`, `ActiveState=inactive`, `SubState=dead`.

B2 is blocked on a Mac-leader ruling for the guard after successful L3 completion. The operator authorized attempts
three, four, and five if needed, but did not replace the Mac leader's ruling authority. The PC session therefore staged
`capture-completed-install.ps1` and `completed_guard.py` as a concrete decision boundary without starting installation.
The proposed mode requires a new numbered ruling at invocation. It binds a canonical pre-snapshot containing the full
79,738,991-byte L3 log SHA-256 and successful terminal systemd state, samples the immutable state every five seconds
during B2, stops the owned B2 group on any change or stale sample, and requires an identical full post-snapshot. Its
read-only live preflight produced one `continue` event and stopped cleanly on request. CUDA remains uninstalled, the
disposable distro remains available, and F1 remains closed.

The pre-import preparation attempts stopped before CUDA installation: an invalid PowerShell exit-code read, a checksum
text-marker parser gap, and a NUL-removal overload all stopped before import; the first import was rolled back after the
freshness guard overmatched `libcups`/`libcurl`. Each class and its normal-path control is recorded in the defect-class
register. The accepted fresh import is session `pc-b2-r200-20261010e` in `plan-capture.json`. Phase-B preparation then
stopped twice before installation: the first exact lock mistook `cuda-driver-dev-13-2` for a driver installer, and the
second identified Debian shared-license symlinks before the capacity wrapper collided with PowerShell's `$Host` variable.
The exact-family guard, shared-notice resolver, and reserved-variable scan now cover those failure classes.
