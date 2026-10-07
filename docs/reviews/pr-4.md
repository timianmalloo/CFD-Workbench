---
id: review-pr-4
title: "PR #4 (Windows PC) - blocked W-3 solver-route checkpoint, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-3, openfoam, su2]
links:
  - { to: coordination-pc-kickoff, rel: depends-on }
  - { to: design-guided-solver-setup, rel: relates-to }
  - { to: review-pr-3, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS as a blocked W-3 checkpoint, not W-3 acceptance. Ubuntu, OpenFOAM v2512 and SU2 v8.5.0
  installed and hash-verified; SU2 smoke passed; the cavity was blocked by a missing parent directory. Re-entry is
  Ruling 133; evidence conditions on the re-entry PR.
---

# PR #4 — Fable owner review

PR #4 (win/solver-routes, head b0c7a05c, tested SHA 70c9ba53) was reviewed by the Fable owner on the Mac on 2026-10-07
under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**, as a blocked W-3 checkpoint and not W-3 acceptance. The receipt
`docs/proof/win-routes/receipt.md` is honest and observed: 45 ledger rows in `steps.jsonl` each carry argv, exit and wall
time; the Ubuntu 24.04.5 image (bb415d82…) and SU2 v8.5.0 archive (4466fe21…) hashes are computed and compared in
`qualify.py`; the OpenFOAM 2512.0-2 runtime and common package hashes match the signed index and icoFoam identifies as
build `_bd2b6720-20260127`; the native SU2 smoke (2,048-quad laminar cylinder, Re 10) exited 0, converged at iteration 61,
CD 2.8856 (CL ≈ 0 by symmetry, so CD is the discriminating scalar). `check-docs` and `validate-cases` were reproduced on
the Mac; the tested SHA is a main ancestor; no secrets, home paths only.

## Conditions on the re-entry PR

1. The frozen-input hashes in `cases/win-su2-smoke.yaml:14,59` and `manifest.json` are of CRLF on-disk bytes, while the
   committed `.su2`/`.cfg`/`.csv` blobs are LF; re-write with LF, re-run the SU2 smoke, and make the hashes equal the
   committed bytes (the receipt's "preserves CRLF" line is false for the committed blobs).
2. Record an observed `sha256sum` of the tutorial `.deb` (4c87494c…), not only apt's verification.
3. The coordinator-reported Windows ring (Core 13/13/11, Desktop exit 70, Analysis 1, CLI exit 127, 8 cost) stays as
   Reported evidence; a later receipt names every failing test with its first error, the Desktop crash frame, the cost
   checks, the ring SHA and `DOTNET_ROOT`.

## Re-entry and classification

Ruling 133: a fresh 60-minute track with its own cap (mkdir parent → sha256sum → dpkg-deb -x → freeze
`cases/win-smoke-cavity.yaml` with LF inputs → cavity to t = 0.5 under M1 with a 60 s timeout → update the observed
Inferred rows). The missing parent directory is a guided-setup route defect (§3.2 step 4, §6) of the class "external
command invoked before its owned destination parent exists"; the Mac records it and amends the design.

## Mac-side defects found

- `cases/spike03-s6-w4.yaml:27` puts a prose string in `geometry.source.sha256`, violating `schemas/cfd-case.schema.json:28`.
- `validate-cases.py` is run by no gate, so main went red on case validation unnoticed.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/4#issuecomment-6044446558
