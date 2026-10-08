---
id: review-pr-9
title: "PR #9 (Windows PC) - W-4 NACA 0012 evidence and L3 preparation (Ruling 151), Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-4, openfoam, su2, naca0012]
links:
  - { to: review-pr-7, rel: relates-to }
  - { to: proof-win-naca, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  APPROVE WITH CONDITIONS (Ruling 151). The Windows G0 L6 OpenFOAM run reproduces the Mac G0 case from byte-identical
  LF inputs and meets A4; the window means differ from the Mac by Cl -1.604e-6 and Cd -9.7e-8, below the sum of the two
  runs' iterative half-bands. The one native SU2 run is kept as oracle NOT MET with no CFL3D number admitted; the GPU
  inspection ran zero trials; L3 is prepared per Ruling 79 and the 119-entry closing manifest equals the committed blobs.
---

# PR #9 — Fable owner review

PR #9 "W-4: record Windows NACA evidence and prepare L3" (origin/win/w4-evidence-fix, reviewed head 9b65d242) was
reviewed by the Fable owner on the Mac on 2026-10-08 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**, issued as
**Ruling 151**. Each finding below is Verified (observed by command at the reviewed head) unless marked Inferred.

## Findings

1. **Scope.** 117 files under `docs/proof/win-naca/`, three `cases/win-*.yaml`, `docs/coordination/xmsg.jsonl`. No
   `src/`, `tests/`, `tools/` or `schemas/` change.
2. **Ancestry.** 31d45cb8 (the LF generators), the tested source base fd96651e and the evidence head 714a9f7f are all
   ancestors of the reviewed head.
3. **Same inputs as the Mac.** `docs/proof/win-naca/l6-inputs/cfdw-manifest.json` hashes the same as main's
   `docs/proof/spike-04/receipts/20261004T175236Z-spike04r3-g0-l6/cfdw-manifest.json` (`0aacab97…23e4`).
4. **Cross-OS numbers (pr-7.md W-4 condition 2).** Mac reference (`convergence.txt:5-6`): Cl 1.051600898
   (U_I 7.925e-6), Cd 0.014812212 (U_I 3.314e-7). The Windows window 5578..7577, recomputed from the raw
   `coefficient.dat`: Cl 1.051599294 (half-band 1.942e-6), Cd 0.014812115 (8.962e-7), equal to `l6-convergence.txt`
   and `l6-comparison.json`. Difference Cl -1.604e-6 < 9.87e-6 and Cd -9.7e-8 < 1.23e-6 (each against the sum of the
   half-bands). Courant reads "Not recorded" (steady simpleFoam emits none). The Mac stopped at 6,545 iterations and
   Windows at 7,577, so the A4 windows differ; the receipt says so.
5. **GPU (condition 1).** One `fused-link-inspection` at 14:52:10Z, inside W-4a's window (first command 14:41:07Z). The
   fused library is present and the GPU is visible, but `libfiniteVolume` is unresolved outside activation. No install,
   no trial, and nothing fed into the comparison, L3 or W-5.
6. **SU2 oracle NOT MET is not admitted.** `su2-comparison.json:27-39` reads `declared_iterative_oracle: NOT MET`, and
   the SU2 mean and difference read "Not admitted". The only "Converged | Yes" is SU2's own exit table in a raw capture,
   and the receipt refuses it. "verified" appears only on exit codes and manifest counts, "equivalence" only negated.
7. **Manifest (the LF bytes).** All 119 entries equal `git show <head>:<path>` in SHA-256 and byte count. That includes
   the raw `su2-results/log.SU2_CFD` (`aab8d165…3543`, 3,045,192 bytes, 27,120 CRLF pairs preserved),
   `l6-inputs/constant/polyMesh/points`, `system/controlDict`, `su2-inputs/tmr-sa.cfg` and
   `cases/win-spike04r3-g2-l3.yaml`.
8. **L3 contract (condition 3, Ruling 79).** `cases/win-spike04r3-g2-l3.yaml` has the same numerics, mesh, physics, A4
   and clause 5 as `cases/spike04r3-g2-l3.yaml`. `wall_cap_s` is removed. `n_subdomains: 6`, `nice: 10` (14 physical
   cores, so ≤ cores − 2). `of-command-l3.sh` runs `mpirun -np 6` under `nice -n 10` with no `timeout`. `start-l3.sh`
   refuses to start without `CFDW_START_NOTE_REF` and runs detached. Schema: 43 cases, 0 errors.
9. **L3 as launched differs from the prepared contract.** Per `xmsg.jsonl`, attempt 2/2 was blocked when WSL killed the
   unit as `wsl.exe` closed. After the operator's "keep going", L3 re-entered at 17:04Z as systemd unit
   `cfdw-l3-20261008-r1`: six ranks at **nice 19** with a keepalive. The PR's "prepared, not launched" is true of these
   files, but what runs is not `start-l3.sh` at nice 10. Priority only, so numerically neutral (Inferred).
10. **W-5 (condition 4).** No cfMesh action here. `cartesianMesh -help` stays step 1.
11. **Privacy.** The PROOF-PII guard's own user-path and SID patterns, run over all 121 files: 0 hits. 5
    `%USERPROFILE%` placeholders.
12. **Signal.** The write-now stop worked with signal 30 on Linux (`a4-stop.txt`). The receipt corrects the earlier
    "SIGUSR1" label.

## Rulings (Ruling 151)

- (a) Condition 1 is met by the measurement-only inspection. Zero GPU trials stands; any GPU route stays a Mac decision
  request.
- (b) W-4b is oracle NOT MET. No SU2–CFL3D number may be cited anywhere. A rerun under a re-declared oracle (absolute RMS
  floors, since momentum residuals start near 1e-14 from freestream initialisation) needs a new decision request and a
  cfd-numerical-verification-expert review of the re-declaration.
- (c) The measured cross-OS difference is the W-4a result. Any future build-equivalence tolerance is set at the Mac,
  must exceed it, and is stated relative to both runs' iterative half-bands.

## Conditions

- **C1, pre-merge (Mac, at the join):** add `proof-win-naca` to the Docs Explorer index so check-docs is green.
- **C2, follow-up (W-4c done receipt, before any L3 triplet enters `gci.py`):** bind the actual launch: the unit name,
  the keepalive, the `nice` readback (19), six ranks and partial-run preservation. Reconcile the win L3 YAML's
  `decomposition.nice` with what ran.
- **C3, follow-up:** record four defect classes with controls in `docs/lessons/defect-classes.md`:
  - the platform signal integer (SIGUSR1 is 30 on one platform and 10 on another);
  - the WSL unit dying with the `wsl.exe` that launched it;
  - `.gitattributes` `*.log` versus `log.*`, which nearly normalised the raw bytes;
  - schema validation run only after the launch.
- **C4, follow-up:** the win L3 YAML's `generator` text cites the win case, not `cases/spike04r3-g2-l3.yaml`.

Nothing goes to the operator. The L3 re-entry was already operator-authorised ("keep going", xmsg 17:01Z).
