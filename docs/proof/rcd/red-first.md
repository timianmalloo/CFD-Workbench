---
id: proof-rcd-red-first
title: RCD readiness runs check-docs, red-first run
type: proof-pack
status: in-review
owner: "@track-rcd"
phase: implementation
tags: [readiness, check-docs, proof]
links:
  - {to: defect-classes, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Readiness stayed GREEN with a mismatched capture manifest on main 034f37fd; with check-docs as its first step it is RED.
review-suggested: []
---

# RCD red-first: readiness runs check-docs (POST-JOIN-EDIT-UNGATED)

Scenario: a scratch worktree of main 034f37fd where `docs/proof/ring-windows/capture-manifest.json` declares
`"bytes": 694` for a file that is 693 bytes (committed, tree clean). `python3 tools/check-capture-manifests.py` exits 1:
`bytes declared 694, committed 693`.

The harness ran `run_ring` from `tools/run-readiness.py` over the real readiness list, restricted to its cheap `python3`
steps (the dotnet build, tests and spike recounts do not bear on this scenario).

| Run | Readiness list | Result |
| --- | --- | --- |
| Red (old join.json) | check-neuralfoil-weights, check-notices | `GREEN`, exit 0, 0.8 s. This is the defect. |
| Green (new join.json) | check-docs, check-neuralfoil-weights, check-notices | `RED`, exit 1. `FAIL 10.4 s python3 tools/check-docs.py` with the CAPTURE-MANIFEST line. |

Cost: `tools/check-docs.py` takes about 10 to 11 s. It is a foreground entry, not in a concurrent group: it reads the
whole docs tree and the recount steps rewrite docs files. Full-ring total is quoted in the Return.
