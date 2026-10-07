---
id: review-pr-7
title: "PR #7 (Windows PC) - W-3 complete (Ruling 133), Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-3, openfoam, su2]
links:
  - { to: review-pr-4, rel: relates-to }
  - { to: design-guided-solver-setup, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE. W-3 is complete as manual Windows solver-route qualification and W-4 may start: the OpenFOAM cavity matches
  the Mac's Courant mean exactly, SU2 reproduces its drag from LF inputs, and all 160 manifest hashes equal the committed
  bytes.
---

# PR #7 — Fable owner review

PR #7 (win/solver-routes-r133, head 37532b12, numerical and ring tested SHA 614f9cce) was reviewed by the Fable owner on
the Mac on 2026-10-07 under Ruling 106. Verdict: **APPROVE**; W-3 is complete as manual Windows solver-route qualification
and **W-4 may start**. The Ruling 133 sequence was followed step by step and is Verified from `r133/steps.jsonl`: the
fixture parent was created first, the tutorial .deb's observed sha256sum equals the 4c87494c… pin, `dpkg-deb -x` exited 0,
the cavity inputs and YAML were frozen (14 files equal to staged blobs) before launch, and blockMesh/checkMesh/icoFoam
ran serially under `env -i`, an isolated HOME, the product M1 controlDict (f3debe8b…, equal to
`cases/tools/foam-bundle/controlDict`) and a 60 s timeout, reaching t = 0.5 with build `_bd2b6720-20260127`, `Disallowing`
banners and a Courant mean of 0.222158 equal to the Mac at printed precision; one activation repair stayed inside the new
cap. All nine cavity hashes, the LF SU2 mesh and config hashes and all 160 manifest entries equal the committed blobs,
and the SU2 re-run reproduces CD 2.885552317, which closes PR #4's three conditions. The guided-setup diff is the three
observed rows only and keeps the Mac's owned-parent amendments; the ring's 39 failures are set-equal to PR #6's; no
secrets.

## W-4 conditions

1. GPU: one measurement-only attempt inside W-4a's 60 min, no new dependency, feeding neither the W-4a equivalence
   comparison, L3 nor W-5; any admission is a Mac decision-request (PR #1 condition 3).
2. W-4a records the measured cross-OS Courant/Cl/Cd difference, so the proposed 1e-3 tolerance stops being Inferred.
3. L3 per Ruling 79: `cases/spike04r3-g2-l3.yaml` without the 10 h cap, `nice`, ≤ cores − 2, background, `note` at start
   and `done`/`blocked` at the end, YAML and receipt, then `gci.py` on the triplet.
4. W-5 per Ruling 102 step 1 first (`cartesianMesh -help`; absent → decision-request), beside L3 only if cores allow.

The Mac's first join attempt stopped on per-check cost limits at load 24.3 while another Mac build ran (the PR has no
code); the join's checks were re-run on a quiet machine (net 45.5 s, 0 failures) before the push.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/7#issuecomment-6048386322
