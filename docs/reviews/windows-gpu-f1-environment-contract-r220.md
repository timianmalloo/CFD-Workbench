---
id: review-windows-gpu-f1-environment-contract-r220
title: "Astra environment-contract review - Ruling 217 after Ruling 220"
type: proof-pack
status: complete
owner: "@pc-f1-r217-contract-astra-r220"
phase: preflight
tags: [windows, wsl, gpu, ruling-217, ruling-220, trust, owner-review]
links:
  - { to: proof-win-gpu-f1-r217-contract-preparation, rel: relates-to }
  - { to: review-windows-gpu-f1-preflight-packet, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-19"
summary: >-
  ACCEPT the exact committed epoch-6 environment contract: all seven input hashes,
  fifteen packet files, exact signer sets and detached Ubuntu signature independently
  match. Live execution still requires unchanged bytes, a clean tree and live epoch 6.
---

# Independent verdict

**ACCEPT** the environment contract committed at
`754b23e0f9b43fd158ccd15f3cba1734a39c9d48` on
`win/gpu-f1-r217-execute-r220`.

The exact contract is
`docs/proof/win-gpu-f1-preflight/run-1/invocation-contract.json`, **4,420 bytes**,
SHA-256 **`f109d4f253f436d8b178e40ea5af497316faf36b0c4398c8a598abc52748336c`**.
Working bytes equal the named committed blob. This acceptance applies only to
those bytes and the observed input set, branch/session and live Mac epoch 6.

**Verified; confidence high:** all checks below were independently run on the
Windows host. No WSL command, wrapper top-level, distro operation, apt/package
download, installation, source acquisition, build or case ran. The reviewer used
the assigned isolated review worktree and authored only this review plus required
derived index/audit files. The reviewed contract and packet were not changed.

Scope follows the packet acceptance in
[the Ruling 220 review](windows-gpu-f1-preflight-packet.md), and Rulings 217 and 220
at `docs/notes/rulings.md:1699-1703,1717-1721`. The review graph was source/input
identity → independent key/signature verification → real host contract verifier →
review and documentation gates → audit/commit/push. Tier T1; fan-out zero.

## Identity and source binding

| Check | Observed result |
|---|---|
| Immutable target | Execution HEAD and contract blob both match `754b23e0f9b43fd158ccd15f3cba1734a39c9d48`; execution worktree was clean. |
| Accepted packet | All **15** relative paths exactly equal the accepted commit's file set. Every working file is byte-identical to `d5bb68fe7db5e511ac46708e14564bb6a738db37` and matches its contract SHA-256. No extra/missing packet file. |
| Execution identity | Branch `win/gpu-f1-r217-execute-r220`; session `pc-f1-r217-execute-r220`. The coordination session listing maps that session to `CFD-Workbench-win-gpu-f1-r217-execute-r220`. |
| Distro identities | Target `cfdw-petsc-cuda132-f1`; protected `cfdw-openfoam2512`; target path `C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1`. Exact equality with the accepted packet constants. No runtime distro-state claim was made. |
| Leader | `coord leader who` reported holder `mac`, epoch **6**, state **live**. The actual host `contract.py verify` also accepted this live authority. It must be rechecked at dispatch/invocation. |
| Runtime contract verifier | The accepted `packet/contract.py verify` was invoked from the review worktree against the real execution repo, committed contract and exact seven inputs. It printed **`INVOCATION-CONTRACT-PASS`**. This verifier performs host file/Git/coordination reads and no WSL effects. |

The contract's source and input fields occupy lines 1-58; exact signer sets occupy
lines 59-74. The packet requirements are recorded in `packet/README.md:87-132`,
with enforcement in `packet/contract.py:40-75,120-176`.

## Independently recomputed input hashes

The first five paths are under the exact directory
`C:\Projects\CFD-Workbench-wsl\f1-r217-inputs-r220-epoch6`. The two B2 paths are
under the named execution worktree, not the review worktree. Every named path was
opened and hashed; all seven values match the committed contract.

| Input | Bytes | Observed SHA-256 |
|---|---:|---|
| `ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz` | 356,739,129 | `8251e27ffff381a4af5f41dcb94d867de3e0d9774a9241908ab34555d99315ea` |
| `ubuntu-noble-SHA256SUMS` | 642 | `337176fdeda3dbbf7bd36146894c03670d5ed8cffc2bd804f39d4c4ace0d424f` |
| `ubuntu-noble-SHA256SUMS.gpg` | 833 | `c0deb277ccda24b8fcc2d36bf9d9530c0aed7138f30375dc45b6539a6aa6799b` |
| `openfoam-archive-keyring.gpg` | 1,157 | `6d17d37b82fc840b8a5c767545703df7264cfa3e30f9cf74556ebb124bda1a93` |
| `nvidia-cuda-archive-keyring.gpg` | 1,134 | `25100d6f2eccaee7d6719a70e3c0c6145064aa9edc2c32148f693ac4c524a376` |
| `docs/proof/win-gpu-b2/package-lock.json` | 87,899 | `2306d58e2fb0d728195ee2691cb7faab7c651e04bb70746f242310834d61e76c` |
| `docs/proof/win-gpu-b2/license-map.json` | 68,160 | `470de11258ff4e78a05a38cb3d1c75090387c79b2cc3b06fc3534e9eee5da4d6` |

## Keyring and signature evidence

Python's tar reader located the two exact regular-file members under
`usr/share/keyrings/` in the pinned rootfs and read their bytes without importing
or unpacking a distro. Those bytes equal the preparation's extracted keyring files:

| Rootfs member | Bytes | SHA-256 |
|---|---:|---|
| `ubuntu-cloudimage-keyring.gpg` | 2,444 | `c2d40d925557dbe9a0745c12ca0dc98bed593ce9a6652f1b2faa1ff0858dbf2f` |
| `ubuntu-archive-keyring.gpg` | 3,607 | `80a36b0a6de2f69f49d2df75ef473ccde121e9e190b9ea01d20a4f63778d5c31` |

Installed Git for Windows `gpg.exe --show-keys --with-colons --fingerprint`
enumerated these exact sets. Set equality and duplicate checks passed: **no extra,
missing or duplicate fingerprint** relative to the contract.

| Kind | Exact observed fingerprints |
|---|---|
| Ubuntu cloud image | `D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81`; `4A3CE3CD565D7EB5C810E2B97FF3F408476CF100` |
| Ubuntu archive | `790BC7277767219C42C86F933B4FE6ACC0B21F32`; `843938DF228D22F7B3742BC0D94AA3F0EFE21092`; `F6ECB3762474EDA9D21B7022871920D1991BC93C` |
| OpenFOAM | `DC93C096174122E256DA24063386DD74948D208F` |
| NVIDIA | `EB693B3035CD5710E231E123A4B469963BF863CC` |

Host `gpgv.exe`, given the exact cloud-image keyring, detached signature and sums,
exited **0** and emitted:

```text
GOODSIG 1A5D6C4C7DB87C81 UEC Image Automatic Signing Key <cdimage@ubuntu.com>
VALIDSIG D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81
```

The valid signature timestamp is **1713934142**, reported as **2024-04-24** in the
status stream. Exactly one sums row names the rootfs filename; its signed hash
equals the independently measured rootfs hash above. The full status was checked
for an allowed valid signer, not merely a zero exit status.

Trust remains anchored in the previously accepted B2 rootfs hash and source chain
(`docs/proof/win-gpu-b2/source-verify.stdout.txt`), rather than treating a key found
inside an arbitrary archive as its own authentication. The OpenFOAM identity also
matches retained publisher evidence in
`docs/proof/win-routes/repo-key-fingerprint.txt`; NVIDIA matches the retained B2
source-chain fingerprint. This review performed no new network trust bootstrap.

Method detail: Git's GnuPG tools were run with MSYS `/c/...` paths and an isolated
temporary homedir. An initial Windows-drive-form GnuPG invocation exited 2 before
verification; the corrected path form produced the results above. Temporary
metadata was cleaned; no input or keyring was modified.

## Acceptance limits and handoff

This contract closes the packet review's environment-input and signer-set seam for
the exact observed epoch-6 bytes. It does **not** certify runtime WSL state, VHD or
host capacity, the protected distro's live keyring/projection, a dependency
transaction, archive licences/sizes, or teardown. Those remain the accepted
packet's mandatory runtime gates and the later result review.

Before launch, the executor must retain a clean tree and these unchanged bytes,
join this review through the coordinator, and rerun the host contract verifier.
Mac epoch **6 must still be live**. Expiry or a later epoch makes this contract
stale and requires a newly reviewed binding; no in-place rewrite is covered.
The packet's guest keyring/signature checks must still pass before target creation.

Ruling 217 then permits only its single bounded dependency simulation and resolved
`.deb` inspection preflight, followed by manifest-bound evidence and cleanup.
Installation, PETSc/petsc4Foam source acquisition, build, solver/case execution and
benchmarking remain closed. Documentation/graph checks and publication results are
recorded in the closing audit and handoff.
