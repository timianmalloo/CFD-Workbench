---
id: proof-win-gpu-f1-attempt2-offline-request
title: "F1 attempt 2: the laptop re-plan (Ruling 237 node A)"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [windows, arm64, offline, f1, attempt-2, decision-request]
links:
  - { to: rulings, rel: depends-on }
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: proof-win-gpu-f1-rescope-verification, rel: depends-on }
  - { to: review-f1-rescope-r237-node-r, rel: depends-on }
  - { to: proof-win-gpu-f1-inputs, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Node A of the F1 re-scope plan: the attempt-2 selections chosen on the Windows ARM64 laptop, the reviewed inputs
  fetched and measured against the packet's pins (inputs-receipt.json), the free-space worksheet, and the sequence that
  produces the contract pair with contract.py prepare and asks the operator for the live authorization ruling. The old
  PC's request-only preparation (offline-decision-request.json, validation.json, admission-proof.md) is history.
---

# F1 attempt 2: the laptop re-plan (Ruling 237 node A)

**Where this stands.** The packet was re-scoped for x64 and ARM64 hosts (plan `docs/plans/f1-rescope-r237.md`, nodes
C1 to C5), verified (node V, `../rescope/verification-v.json`) and independently reviewed (node R,
`docs/reviews/f1-rescope-r237-node-r.md`, ACCEPT with every condition applied; Ruling 241 settled DR-4). Node A
re-plans attempt 2 on this laptop under Ruling 237 (2): new selections, the inputs fetched here, the pair produced by
the packet's own producer, then the live authorization ruling, then one live attempt under the Ruling 225 limits.

**History.** `offline-decision-request.json`, `validation.json` and `admission-proof.md` are the old PC's request-only
preparation (2026-10-13; observed source `ad1424b1`). Their identities (`C:\Projects` target path, the protected
distro `cfdw-openfoam2512`, epoch and ruling unresolved) are superseded by this document; they are kept as the record
of what that machine proposed. Ruling 235's items were carried at node C5.

## 1. Selections (Ruling 237 (2): chosen again on the laptop)

| Field | Value | Fixed by |
|---|---|---|
| `attempt` | 2 | Ruling 225 (attempts 2 and 3 remain) |
| `architecture` | `arm64` | the host (C1 detects it from CIM; the contract must agree) |
| `execution_branch` | `win/gpu-f1-attempt2` | `attempt.check_binding` |
| `session` | `pc-f1-attempt2` | `attempt.check_binding` |
| `run_id` | `f1-attempt2-20261020` | pattern `f1-attempt2-YYYYMMDD`, the day the pair is prepared (UTC) |
| `run_root` | `docs/proof/win-gpu-f1-preflight/attempt-2` | `attempt.check_binding` |
| `execution_worktree` | `D:\Projects\CFD-Workbench-f1-attempt2` | chosen here; absolute, no whitespace, uncreated at prepare time, created from the accepted commit after the ruling |
| `target_path` | `D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1` | derived from the execution worktree (C2; never a parameter) |
| `protected` | `none` | the laptop carries no `cfdw-openfoam2512` (Ruling 240 (1), Ruling 241: a registration carrying that identity would refuse) |
| `leader` / `leader_epoch` | `mac` (the PC-local designation label, Ruling 236) / the epoch `coord leader pin mac` returns at preparation time | the lease is pinned with a TTL that covers preparation, the ruling and the run (the supervisor renews it every 100 s during the run); epochs 1 and 2 were consumed by a pin-release probe on 2026-10-20 |
| `host_start_floor_bytes` / `host_reserve_bytes` | 19,669,226,335 / 10,737,418,240 | section 3 |
| `accepted_packet_commit` | the `main` commit the pair is prepared from (recorded in the contract and named in the ruling) | `contract.py prepare --source-commit` |
| `requested_ruling` | the next free ruling number at the time of the request | Ruling 236 |

## 2. Inputs (fetched 2026-10-20, measured against the pins: `inputs-receipt.json`)

| Input | Where | Bytes | SHA-256 matches the pin |
|---|---|---:|---|
| arm64 rootfs `ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz` | `D:\reviewed` | 341,873,503 | yes (`fecec1d9…`, the C1 table row; also the line in the signed `SHA256SUMS`) |
| `SHA256SUMS` | `D:\reviewed` | 642 | yes (`337176fd…`, byte-identical to the B2-era file) |
| `SHA256SUMS.gpg` | `D:\reviewed` | 833 | yes (`c0deb277…`) |
| OpenFOAM keyring (dearmored from `dl.openfoam.com/pubkey.gpg`) | `D:\reviewed` | 1,157 | yes (`6d17d37b…`; the armored bytes hash as attempt 1 recorded) |
| NVIDIA keyring (from the sbsa repository's `cuda-keyring_1.1-1_all.deb`) | `D:\reviewed` | 1,134 | yes (`25100d6f…`; byte-identical to the x86_64 package's keyring that B2 extracted) |
| Ubuntu cloud-image keyring | `<execution worktree>/docs/proof/win-gpu-f1-preflight/inputs/` | 2,444 | yes (`c2d40d92…`, provenance in `../inputs/README.md`) |
| B2 licence map | `<execution worktree>/docs/proof/win-gpu-b2/` | repository file | yes (`470de112…`) |

The first three were fetched with `curl.exe -sS -L` from the cloud-images `current` pointer; a first attempt with
`Invoke-WebRequest` had written the directory index page for all three (2,563 bytes, one hash), which the hash
comparison caught before anything else read them. The two binary keyrings were derived inside WSL Ubuntu-24.04 the way
attempt 1 recorded (`gpg --dearmor`; `dpkg-deb --fsys-tarfile`). Every hash equals its pin, so no pin changes at A.

## 3. Free-space worksheet (Ruling 238 (3): contract fields, never constants)

| Term | Bytes | Source |
|---|---:|---|
| `host_reserve_bytes` | 10,737,418,240 | Ruling 225's reserve, unchanged |
| VHD cap | 8,589,934,592 | Ruling 225 |
| rootfs bytes | 341,873,503 | the arm64 rootfs, measured |
| `host_start_floor_bytes` = reserve + VHD cap + rootfs | **19,669,226,335** | the attempt-1 worksheet's shape (10,737,418,240 + 8,589,934,592 + 356,739,129 = 19,684,091,961 there) |
| `D:` free, measured 2026-10-20 | 3,998,438,477,824 | `(Get-PSDrive D).Free` |

The launcher measures the target drive's free space itself before every stage (C2); these numbers only bind the floor
it refuses below.

## 4. Sequence

1. **A1 (this document).** Ruling 241, this re-plan and the inputs receipt join `main` (docs only).
2. **Leader.** `AGENT_SESSION=mac coord leader pin mac --ttl <seconds>`; `coord leader who` gives the epoch the contract
   carries.
3. **A2, the pair.** In a fresh worktree at `main` (clean, HEAD = the source commit): `py -3 contract.py prepare
   --source-commit <HEAD> --execution-worktree D:\Projects\CFD-Workbench-f1-attempt2 --leader-epoch <epoch>
   --requested-ruling <next> --architecture arm64 --protected none --host-start-floor-bytes 19669226335
   --host-reserve-bytes 10737418240 --branch win/gpu-f1-attempt2 --session pc-f1-attempt2 --run-id
   f1-attempt2-20261020` plus the seven input paths. It writes `invocation-contract.json` and the proposed
   `attempt-authorization.json` here and prints both hashes; they join `main` (docs only).
4. **The ruling.** A decision request to the operator carrying every token `attempt.check_ruling` requires: `ACCEPT ONE
   PREFLIGHT ATTEMPT`, the contract path and SHA-256, the source commit, the run id, session and branch, `epoch <n>`,
   the authorization file's SHA-256 and its path. The operator's ruling takes the next free number and is pushed to
   `origin/main` (the launcher reads it from there).
5. **A3, the live attempt.** The execution worktree is created from the accepted commit on `win/gpu-f1-attempt2`; the
   launcher runs from native `pwsh` as `pc-f1-attempt2` with the inputs above; `INVOCATION-CONTRACT-PASS` precedes the
   first WSL effect; the receipt carries the host identity; the evidence commits land on the branch and join.

## 5. Carried from node R (open advisories), and what A does with them

- Fingerprint confirmation of the cloud-image keyring against Ubuntu's published keys by a reviewer other than the
  author: **required before step 5** (recorded in `../inputs/README.md` when done).
- Test-count floors in the verification runner; message-matching `Refuses` helpers; a hashed copy of the packet run
  from the live folder; collapsing the `$freshAttempt` conditionals now that a contract without `attempt` is refused:
  accepted residual risks for this attempt, listed for the packet's next repair cycle.

## 6. Residual risks (named, from plan section 8)

The first arm64 `simulate` may add rows the probe did not see (never a refusal of a pinned root the index carries);
the `nvcc` `sm_` list and the x64 `gpgv.exe` layout stay Inferred; lock drift at attempt time refuses by name. A red
in the live attempt is a failed attempt with an immutable receipt, never a retry inside the same authorization.

## 7. Outcome (2026-10-20): attempt 2 refused before any WSL effect

Ruling 242 accepted the pair; the execution worktree was created from `803fb83d` and the launcher ran from native pwsh
7.6.7 (arm64) as `pc-f1-attempt2`. `INVOCATION-CONTRACT-PASS` and `HOST-IDENTITY-PASS architecture=arm64 cpu=1 gpu=2`
were recorded, then the leader supervisor failed at start (`F1-RENEW-START-FAILED`, elapsed 5.6 s, stage
`not-started`): `leader_supervisor.admit` projected five input names into the namespace `contract.verify` reads, and
`verify` has read a sixth (`ubuntu_cloudimage_keyring`) since node C4. No target was created and nothing in WSL
changed (`target_absent: Not applicable`, registrations unchanged). The cleanup's final identity added
`F1-EXCLUSIVITY-REFUSED: additional or changed WSL actor` (the agent harness's Git-for-Windows `bash.exe`, matched
by name; DR-5) and the two consequent missing-evidence errors. The receipts are `checkpoint/` (commit `f1d3a22f`)
and `final/` (commit `32e6e384`); `final/cleanup-receipt.json` is the classified receipt Ruling 242 names.

Attempt 2 of Ruling 225's three attempts is consumed. The repair is on main (plan ledger, node A); the next attempt is
attempt 3, the last under the cap, and takes its own pair at the repaired source and its own ruling.
