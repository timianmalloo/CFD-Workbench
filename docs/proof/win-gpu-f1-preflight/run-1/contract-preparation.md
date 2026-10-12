---
id: proof-win-gpu-f1-r217-contract-preparation
title: "Ruling 217 run 1 environment contract preparation"
type: proof-pack
status: proposed
owner: "@pc-f1-r217-execute-r220"
phase: preflight
tags: [windows, wsl, gpu, ruling-217, ruling-220, contract, trust]
links:
  - { to: review-windows-gpu-f1-preflight-packet, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-b2, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  Proposed run-1 invocation contract binds the Astra-accepted packet, retained signed inputs, exact signer sets,
  execution branch/session, and live Mac epoch 6; independent review still precedes any WSL effect.
review-suggested:
  - { by: proof-win-gpu-f1-r217-packet, on: 2026-10-12, reason: "Offline live-defect repair changes packet bytes; prior acceptance and invocation contract do not accept these new bytes." }
---

# Ruling 217 run 1 environment contract preparation

## Disposition

**Prepared; independently unreviewed. Do not execute yet.** Astra accepted all 15 packet files at joined commit
`88ae5819a038` and verified they are byte-identical to repair commit
`d5bb68fe7db5e511ac46708e14564bb6a738db37`. The invocation contract names that repair commit as the accepted packet
source. Mac leadership was reclaimed without changing its holder and is live at epoch 6. A separate Astra review of
this contract, inputs, signer sets, paths, branch, session, and epoch is still mandatory before the wrapper may run.

No WSL command, distro mutation, apt command, package download, installation, source acquisition, build, or CFD case
ran during this preparation. The official OpenFOAM public key was the only network retrieval.

## Contract identity

| Field | Bound value |
|---|---|
| Contract schema | `cfdw-f1-r217-invocation/1` |
| Contract path | `docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json` |
| Contract SHA-256 before commit | `f109d4f253f436d8b178e40ea5af497316faf36b0c4398c8a598abc52748336c` |
| Accepted packet commit | `d5bb68fe7db5e511ac46708e14564bb6a738db37` |
| Packet files | 15 exact relative paths and SHA-256 values |
| Execution branch | `win/gpu-f1-r217-execute-r220` |
| Execution session | `pc-f1-r217-execute-r220` |
| Leader | `mac`, epoch 6 |
| Target | `cfdw-petsc-cuda132-f1` |
| Protected distro | `cfdw-openfoam2512` |
| Target path | `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` |

## Exact inputs

The five external inputs are isolated under
`C:\Projects\CFD-Workbench-wsl\f1-r217-inputs-r220-epoch6`. The two repository inputs resolve inside the execution
worktree. The contract records these exact absolute paths and hashes.

| Input | Bytes | SHA-256 | Provenance |
|---|---:|---|---|
| Ubuntu Noble WSL rootfs | 356,739,129 | `8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea` | Retained B2 official cloud-image download; matches signed `SHA256SUMS`. |
| Ubuntu `SHA256SUMS` | 642 | `337176fdeda3dbbf7bd36146894c03670d5ed8cffc2bd804f39d4c4ace0d424f` | Retained B2 official cloud-image metadata. |
| Ubuntu `SHA256SUMS.gpg` | 833 | `c0deb277ccda24b8fcc2d36bf9d9530c0aed7138f30375dc45b6539a6aa6799b` | Retained B2 official detached signature. |
| OpenFOAM binary keyring | 1,157 | `6d17d37b82fc840b8a5c767545703df7264cfa3e30f9cf74556ebb124bda1a93` | Dearmored from `https://dl.openfoam.com/pubkey.gpg`, retrieved 2026-10-12; armored bytes SHA-256 `e6ddd89ed33131a4fc63460cb131ae7416a2a0062b590f4237121598626edf86`. |
| NVIDIA binary keyring | 1,134 | `25100d6f2eccaee7d6719a70e3c0c6145064aa9edc2c32148f693ac4c524a376` | Retained B2 extraction from `cuda-keyring_1.1-1_all.deb`; prior B2 source-chain verification passed. |
| B2 package lock | repository file | `2306d58e2fb0d728195ee2691cb7faab7c651e04bb70746f242310834d61e76c` | Accepted B2 135-row lock. |
| B2 licence map | repository file | `470de11258ff4e78a05a38cb3d1c75090387c79b2cc3b06fc3534e9eee5da4d6` | Accepted B2 135-row licence map. |

## Signer evidence

Git Bash `gpgv` verified the copied Ubuntu signature and sums against the cloud-image keyring extracted directly from
the exact retained rootfs. The observed valid signer was
`D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81`, and the sums named the same rootfs SHA-256 measured above.

The contract allowlists every full fingerprint in the exact rootfs keyrings so the later keyring-subset check does
not silently omit an installed key:

| Kind | Allowed full fingerprints |
|---|---|
| Ubuntu cloud image | `D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81`; `4A3CE3CD565D7EB5C810E2B97FF3F408476CF100` |
| Ubuntu archive | `790BC7277767219C42C86F933B4FE6ACC0B21F32`; `843938DF228D22F7B3742BC0D94AA3F0EFE21092`; `F6ECB3762474EDA9D21B7022871920D1991BC93C` |
| OpenFOAM | `DC93C096174122E256DA24063386DD74948D208F` |
| NVIDIA | `EB693B3035CD5710E231E123A4B469963BF863CC` |

The extracted cloud-image keyring is 2,444 bytes at SHA-256
`c2d40d925557dbe9a0745c12ca0dc98bed593ce9a6652f1b2faa1ff0858dbf2f`; the archive keyring is 3,607 bytes at
SHA-256 `80a36b0a6de2f69f49d2df75ef473ccde121e9e190b9ea01d20a4f63778d5c31`.

## Review and execution gates

The independent reviewer must recompute the contract hash and every named file hash, enumerate all packet files at the
accepted commit, inspect each keyring fingerprint, repeat the detached-signature verification, and confirm the live
Mac leader remains epoch 6. A reviewer may reject any extra key, path mismatch, stale epoch, source-byte mismatch, or
dirty execution tree.

After review is joined, the execution worktree must be clean. `contract.py verify` must pass there with the exact
branch, session, and seven input paths before the first WSL effect. Any epoch expiry or renewal to a later epoch makes
this contract stale and requires a newly reviewed contract; it is never edited in place during execution.

