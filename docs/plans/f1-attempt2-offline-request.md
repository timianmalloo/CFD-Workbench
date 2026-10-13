---
id: plan-f1-attempt2-offline-request
title: "F1 attempt 2 offline request execution graph"
type: doc
status: proposed
owner: "@pc-coordinator"
phase: preflight
tags: [f1, offline, planning]
links:
  - { to: proof-win-gpu-f1-attempt2-offline-request, rel: relates-to }
review-by: "2026-10-20"
summary: "Bounded evidence-to-request graph; unknown Mac/live values stop activation."
---

# Goal and graph

Goal: prepare a reviewable attempt-2 decision request. Done when: committed and
pushed with exact source hashes, measured offline validation and explicit
unresolved authority/live fields. Not in scope: WSL, registry, source repair,
lease mutation, full ring or activation. Tier T1; fan-out cap one.

| Node | Capability | Inputs and goal | Exit and oracle | Tier |
|---|---|---|---|---|
| A | Reasoning | Current main, rulings, PR35/36 proof, packet contracts | Scope and all unresolved fields named | T1 |
| B | Deterministic mechanics | A; committed Git blobs | JSON hashes bind exact source; missing path fails | T0 |
| C | Reasoning | A+B | GitHub-ready request distinguishes baseline and live authority | T1 |
| D | Deterministic mechanics | C; existing focused suite/docs gate | Test counts/results, hashes and unchanged source verified | T0 |
| E | Independent review | C+D | Parent reviews request; author grants no live acceptance | T1 |

```mermaid
flowchart LR
  A[Ground and bound] --> B[Hash committed evidence]
  B --> C[Write request]
  C --> D[Validate and publish]
  D --> E[Independent parent review]
```

All edges carry data or decisions. The naive and final graphs have five nodes
and width one. Work and span are **Inferred** equal for this serial chain;
no duration estimate is asserted. Combining evidence reading and scope decision
avoids a speculative implementation stage. Independent review, exact-byte
binding, docs validation and audit are floors. The installed evidence has no
`kb-graph-and-loop-engineering` node, so no dangling dependency is added.

The checkpoint at A found immutable attempt-1 admission constants and unknown
Mac/live values. The delegated fallback selects request-only output. There is
no source repair loop. Validation correction variant: unresolved validation
findings, floor zero, exit all checks pass, maximum two cycles; cap firing is
a defect signal and stops publication. No third cycle is authorized.

Actual: two corrections. One snapshot-path correction after `git show` refused a nonexistent
checker path. The corrected path came from `rg --files`; no partial snapshot
was published. Existing fail-closed Git blob lookup is the control. Independent
review then corrected the distinct-new-epoch overclaim: Mac decides reuse
versus reclaim, and fresh samples are required. No further repair cycle is
authorized. PR37 synchronization fast-forwarded to ad1424b1; source and tests
were unchanged, and baseline/request hashes were regenerated. The class
recurrences are recorded in the lesson register. Measured validation costs are in
the companion proof JSON; audit start/append measures total run duration.
No elapsed or token figure is invented. Independent review remains with parent.
