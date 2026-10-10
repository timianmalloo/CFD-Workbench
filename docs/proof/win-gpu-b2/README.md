---
id: proof-win-gpu-b2
title: "Windows GPU B2 disposable CUDA proof"
type: proof-pack
status: in-progress
owner: "@pc-coordinator"
phase: implementation
tags: [proof, windows, wsl, cuda, gpu, ruling-200]
links:
  - { to: proof-win-gpu-b1, rel: depends-on }
  - { to: proof-win-l3-observer, rel: observes }
review-by: "2026-11-10"
summary: >-
  Ruling 200 proof for an exact CUDA 13.2 install and managed-memory toy in a fresh disposable Ubuntu 24.04 WSL distro.
---

# Windows GPU B2 disposable CUDA proof

B2 uses only the WSL distro named `cfdw-cuda132-b2`. It never installs into or changes `cfdw-openfoam2512`.
The first phase verifies the NVIDIA repository signature and retained metadata, creates a fresh Ubuntu 24.04 distro,
pins the CUDA 13.2 config packages against repository drift, updates signed apt indices, and captures an exact apt
simulation. No CUDA package is installed in that phase.

The install gate remains closed until the exact dependency lock, downloaded package hashes, applicable license map,
measured payload size, free-space budget, Linux-driver exclusion, and CUDA 13.3/13.4 exclusion all pass and the Astra
owner accepts them. The later toy must use `cudaMallocManaged`, identify the selected CUDA device, validate a numeric
SAXPY oracle, run at `nice 10` with at most CPUs 0-1, and keep the Ruling 200 L3 observer and five-percent guard live.

Rollback is `wsl.exe --unregister cfdw-cuda132-b2`, but only after the target's marker and exact distro identity are
verified. F1 stays closed; B2 can only produce its decision-request evidence.
