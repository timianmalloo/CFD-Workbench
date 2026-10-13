---
id: plan-windows-gpu-f1-r230-activation-review
title: "Exact R230 activation review plan"
type: doc
status: complete
owner: "@pc-f1-r230-activation-review"
tags: [review, cleanup, activation, offline]
links:
  - { to: review-windows-gpu-f1-r230-activation, rel: relates-to }
  - { to: note-windows-gpu-f1-r230-cleanup-request, rel: depends-on }
review-by: "2026-10-20"
summary: "Bounded activation rehash and real read-only precheck, then publication."
---

Goal: accept or veto exact fd4f738a activation. Done when exact findings and
verdict are pushed. Tier T1; no delegation, WSL, registry, lifecycle, cleanup,
GitHub message, mail, source repair or activation mutation. The existing
optimize-graph sequence is reused at activation scope.

| Node | Input / goal | Exit oracle | Capability | Tier | Dependency |
| --- | --- | --- | --- | --- | --- |
| A | Exact head, ruling and artifact bytes | Four hashes, ancestry and same-commit sample/auth verified | Independent review | T1 | none |
| B | A; production precheck | Read-only allowlist; exact execution branch clean/pushed; no output writes | Deterministic mechanics | T0 | A data |
| C | A/B; audit and review publication | Exact verdict pushed; clean reviewer tree | Independent review | T1 | B decision |

Three nodes, serial dependency span three, no fan-out or loop. No broad suite
is repeated: the source bytes equal the independently accepted conformance
source, and this task concerns activation only. The subprocess allowlist is an
immovable floor. Actual: three nodes completed, no rework, 28 read-only commands
in production precheck, 8.295 seconds measured probe wall time. Audit measures
full review duration; token usage is unavailable.
