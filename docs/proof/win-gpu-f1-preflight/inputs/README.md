---
id: proof-win-gpu-f1-inputs
title: "F1 preflight: repository-reviewed inputs"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, f1, inputs, keyring, provenance]
links:
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The inputs of the F1 preflight that live in the repository and are hash-bound in the contract: today the Ubuntu
  cloud-image keyring the Windows-side rootfs signature check reads (Ruling 240 (2)). Each file states where its bytes
  came from, measured, and what a reviewer still has to confirm.
---

# F1 preflight: repository-reviewed inputs

An input in this folder is bound three ways: its SHA-256 is a constant in `packet/attempt.py` (`INPUT_HASHES`), the
contract records its path under the execution worktree and the same hash, and the launcher hashes the file before any
WSL effect. The folder exists so that the bytes are reviewable in Git rather than only on one machine.

## ubuntu-cloudimage-keyring.gpg

| Fact | Value | Status |
|---|---|---|
| SHA-256 | `c2d40d925557dbe9a0745c12ca0dc98bed593ce9a6652f1b2faa1ff0858dbf2f` (2444 bytes) | Verified (`sha256sum`, 2026-10-19) |
| Origin | `/usr/share/keyrings/ubuntu-cloudimage-keyring.gpg` of the laptop's WSL Ubuntu-24.04, owned by the package `ubuntu-keyring` (`dpkg -S`) | Verified (2026-10-19) |
| Package | `ubuntu-keyring 2023.11.28.1`, archive file `pool/main/u/ubuntu-keyring/ubuntu-keyring_2023.11.28.1_all.deb`, SHA-256 `36de43b15853ccae0028e9a767613770c704833f82586f28eb262f0311adb8a8` (the local apt index for noble, `apt-cache show`) | Verified against the local index (2026-10-19); the index's own signature was verified by that distro's apt, not by this packet |
| Identities | `D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81` and `4A3CE3CD565D7EB5C810E2B97FF3F408476CF100`, no subkeys (`gpg --show-keys --with-colons` with Git's gpg on Windows and with the distro's gpg) | Verified (2026-10-19); both are in `attempt.SIGNERS['ubuntu_rootfs']` |
| Independent confirmation of the two fingerprints against Ubuntu's published cloud-image signing keys | not done | Flagged: a reviewer other than the author confirms them before the live attempt (node A) |

The signature fixture under `tests/fixtures/f1-rootfs-signature` is not an input: its key is a throwaway whose
fingerprint is in no allowlist, and the suite proves that a contract carrying the real signers refuses it.
