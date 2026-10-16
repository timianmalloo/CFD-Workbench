---
id: coordination-handoff-2026-10-16-laptop-setup
title: "Handoff 2026-10-16: laptop setup state before the D: reformat"
type: proof-pack
status: draft
owner: "@operator"
tags: [coordination, handoff, windows, laptop, arm64, sdk, f1]
links:
  - {to: coordination-handoff-2026-10-13-pc-sole-leader, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2026-10-30
summary: >-
  Where the laptop session stopped on 2026-10-16: Rulings 237-239, the .NET SDK 10.0.402 branch, the ring evidence on
  Windows ARM64, the open decisions, and the steps to resume after the operator reformats D: from exFAT to NTFS.
---

# Handoff 2026-10-16: laptop setup state before the D: reformat

**Read this after** `handoff-2026-10-16-new-laptop.md` (the old-PC move) and `handoff-2026-10-13-pc-sole-leader.md`
(Ruling 236). The laptop is the PC sole leader.

## 1. What is on origin

`main` has not moved (`80935463`). Readiness on the laptop is not GREEN yet, so nothing below is joined. The branches
form one stack; join them in this order.

| Order | Branch | Holds |
|---|---|---|
| 1 | `win/old-pc-session-logs` | The old PC's 58 session logs and the new-laptop handoff (from the old PC). |
| 2 | `docs/rulings-237-238` | Rulings 237 (F1 re-scope) and 238 (Windows ARM64 + NVIDIA evidence). Docs only. |
| 3 | `build/sdk-10.0.402` | Ruling 239 and the SDK change: `global.json` 10.0.402 with `rollForward: latestPatch`, the Windows workflow and runner tools at exactly 10.0.402, the ADR 0003 amendment, the setup docs. On top of 2. |
| 4 | `docs/laptop-handoff-2026-10-16` | This file and the laptop session's coordination logs. On top of 3. |
| — | `win/gpu-f1-attempt2-contract` | The old PC's unreviewed F1 cycle-2 candidate. Not to be joined as is (section 4). |

Branches 2–4 are based on `main` at `80935463`, as is branch 1, so 1 and 2 both touch `docs/docs-index.js`; expect a
derived-file merge there.

## 2. The laptop

| Fact | Value |
|---|---|
| OS / CPU | Windows 11, **ARM64** (NVIDIA RTX Spark N1X: 20-core CPU, 6144-core Blackwell GPU, 64 GB) |
| GPU driver | 617.42, `nvidia-smi` works on Windows |
| WSL | Installed, **no distribution** |
| .NET | SDK 10.0.402 only (system) |
| Python / PowerShell | Python 3.14 (ARM64, `py -3`), PowerShell 7.6 and Windows PowerShell 5.1 |
| uv | Installed with winget. A fresh shell needs a restart before `uv` is on PATH. `check-docs.py` fails without it (`validate-cases`). |
| D: | **exFAT** today, being reformatted to NTFS by the operator. C: is NTFS. |

exFAT has no ownership, ACLs or symlinks. On it, Git reports "dubious ownership" for every repo (worked around with a
global `safe.directory '*'`, which can be removed after the reformat), and the test ring fails 92 checks: symlink tests
hit "Incorrect function", and the native store refuses the volume with `DOC-UNSUPPORTED-PERSISTENCE` far beyond the
manifest's expected list. **Run rings and readiness only from an NTFS checkout.**

## 3. Ring evidence on the laptop (NTFS checkout on C:)

`bash tools/run-tests.sh`, each row one full run. "Same 3" is the set below.

| Run | SDK | DRAIN failures | Other unexpected | Wall |
|---|---|---|---|---|
| Baseline, unchanged `main` | 10.0.203 (task-local install) | 0 | same 3 | 100 s |
| SDK branch, run 1 | 10.0.402 | 2 | same 3 | 90 s |
| SDK branch, run 2 | 10.0.402 | 1 | same 3 | 85 s |
| SDK branch, `MSBUILDDISABLENODEREUSE=1` after `dotnet build-server shutdown` | 10.0.402 | **0** | same 3 | 82 s |

- **The SDK change moves no test outcome** (Verified: the pass/fail sets are identical across SDKs apart from DRAIN).
- **DRAIN** ("its output did not reach end of file within 10 s of the child ending", exit 125) is **Inferred** to be a
  reused MSBuild node holding a test child's pipe: MSBuild worker nodes with `/nodeReuse:true` stay alive after the
  ring, and the one run with node reuse off had none. One confirming run only.
- **The same 3**, present with either SDK:
  1. `WindowsNative_Environment_RealNtfsX64` expects X64 and sees Arm64. Ruling 238 says record the architecture
     instead of asserting x64.
  2. `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` and
  3. `PropertiesPane_Density_UnitOnValueBaseline` pass here but `tests/expected-failures.windows.json` lists them
     (Ruling 186, old-PC display). The ring asks for the entries to be deleted.
- Everything else that fails is a listed expected failure (the Windows fail-closed store).
- **Budget:** 82–100 s against the 60 s ring budget; Desktop alone 62 s. Open item, not yet decided.
- `tools/check-windows-runner.py --self-test` fails on the laptop with "CreateProcess suspended", on `main` too. It is not
  part of the ring, readiness or check-docs. Open finding.

## 4. Decisions made this session

| Ruling | Decision |
|---|---|
| 237 | The F1 packet is re-scoped to run on Windows x64 (Intel/AMD) and ARM64 with an NVIDIA GPU: architecture detected and recorded, per-architecture artifact locks, no hard-coded machine paths. Attempt 2 is re-planned; its old selections are void. Ruling 235's cycle-3 items carry over. A read-only spike first establishes whether CUDA-in-WSL and the PETSc-CUDA/OpenFOAM stack run on Windows ARM64. New track, own plan, own two-cycle cap. |
| 238 | Windows + NVIDIA evidence may come from the ARM64 laptop. Every receipt records OS architecture, CPU and GPU. An Intel/NVIDIA rerun is a later follow-up, not a gate. Old-PC numbers are measured again. |
| 239 | .NET SDK 10.0.402 with `rollForward: latestPatch` (the ADR 0003 maintenance review). |

**F1 candidate review** (`win/gpu-f1-attempt2-contract`, commit `46c0df8c`). Its own plan calls it cycle 2. Its 17
Python tests and 4 PowerShell suites (PowerShell 7 and 5.1) pass on the laptop. Against the carried-over items:
the activation floor (`expires_in_before >= 200`) is done; the empty-stderr check is **not byte-exact** (it compares the
decoded string length, and a BOM-only file `EF BB BF` decodes to length 0 and is accepted, observed); the contract pair
is not generated or committed, and the test fixtures still request Ruling 235.

## 5. Resume after the reformat

1. Clone to the NTFS D: and fetch every branch:

   ```powershell
   git clone https://github.com/timianmalloo/CFD-Workbench.git D:\projects\CFD-Workbench
   cd D:\projects\CFD-Workbench
   git fetch --tags origin
   # then set git user.name and user.email in the clone to match the earlier commits (no global identity on the laptop)
   ```

2. Check: `dotnet --version` in the repo prints 10.0.402 only on the SDK branch (on `main` it still asks for
   10.0.203); `uv --version`; `py -3 docs/ai-forward-pack/scripts/pack-doctor.py`; `py -3 tools/check-proof-pii.py`.
3. Waiting on the operator: may the SDK branch also carry (a) `MSBUILDDISABLENODEREUSE=1` in `tools/run-tests.sh`,
   (b) the architecture-recording form of `WindowsNative_Environment_RealNtfsX64`, and (c) deleting the two Density
   manifest entries? With those the ring should show no unexpected result; the time budget stays open.
4. Then join in the order of section 1 with `conductor-join.py` (branches 1, 2 and 4 are docs-only, 3 runs the ring),
   run `py -3 tools/run-readiness.py` to GREEN, push `main`, `bash tools/verify-push.sh`, and delete the joined
   branches on origin.
5. Then the Ruling 237 track: the read-only ARM64 CUDA-in-WSL spike, before any packet change.

Other open items from the 2026-10-13 handoff (ring classifier and exit 126+, the TMX 150 % diagnostic, the
Properties-view hang, the stale Windows store evidence) are unchanged.
