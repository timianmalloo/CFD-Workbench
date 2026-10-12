---
id: plan-windows-gpu-f1-guard-consistency-repair
title: "R223 guard publication race: offline repair and proof"
type: doc
status: complete
owner: "@pc-f1-r217-guard-consistency-author"
phase: preflight
tags: [windows, recovery, concurrency, offline-proof]
links:
  - { to: note-windows-gpu-f1-cleanup-recovery, rel: relates-to }
  - { to: review-windows-gpu-f1-cleanup-recovery-cycle1, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-19"
summary: >-
  The production recovery reader reproduces event-before-state publication.
  Bounded rereads repair only an adjacent publication gap; offline negative
  controls preserve invalid-guard and deadline refusal.
---

# Exact scope and investigation

Goal: repair and prove the measured guard publication race offline. Done when the
production regression is red before repair, offline checks pass, and the source
and proof are committed and pushed. Not in scope: WSL, recovery, lifecycle
actions, cleanup, retry, download, install, build, CFD or new authority. Tier T1;
one assigned author, no delegated fan-out; independent review belongs to the
coordinator. The R223 live attempt is consumed. This document grants no authority.

**Verified, high confidence:** receipt commit
`11f56c79add5054c420c23911dbb2cf748c659c7` retains `recovery_success: false` and
the initiating `protected-invariance` ValueError hash
`5707392903dcd18955cf61a4dc93ba40f2b935f5384b7936952fe8180cb42386`, SHA-256 of
`guard event/state disagree`. No `l3-after.json` exists in that retained corpus.
Protected terminal invariance is unproved. Historical receipt and payload bytes
remain unchanged; this repair does not convert its failure into success.

**Verified, high confidence:** F1 `completed_guard.py:watch` durably appends
event N+1, then atomically replaces state N. Original `Recovery.guard_ok` reads
state and the last complete event once. During that writer window it observes
event N+1/state N and refuses. Reversing writer order would move the window,
not make two files atomic. The accepted writer and journal format are unchanged.

Surface list: event journal and atomic state → production guard admission →
guarded command checks and fresh post-unregister sample wait → classified error
and receipt. Domain schema, dispatch, authorization and UI are unchanged.

The reader attempts at most ten observations in 0.5 monotonic seconds, restricted
further by the absolute 600-second recovery deadline and the fresh-sample wait's
inherited 12-second ceiling. It retries only a complete continuing event exactly
one sample ahead of a fresh running state. Only a pair with matching sample and
timestamp passes. Changed binding, stopped/stale observations, child exit,
malformed complete rows, same-sample UTC mismatch, other gaps and regression
across reads refuse. Liveness, freshness and budget are checked again before
return. A read finishing after a deadline cannot pass. Retry attempts/elapsed
seconds enter the existing log as `guard-consistency`; failures retain the
existing classified receipt path.

# Class → sweep → derive → prevent

Class `TORN-PAIR-READ`: treating individually atomic files as one atomic publication causes false
refusal at a valid writer interleaving. Sweep: tracked recursive Python and
PowerShell in `docs/proof`, `tools`, `tests`; tokens `completed-guard.state`,
`completed-guard-events`, `last_sample_utc`, `completed_samples`, `append_event`,
`assume:` and `simplify:`; no production allowlist. Matches were inspected.

| Surface | Observed contract and disposition |
| --- | --- |
| F1 `Recovery.guard_ok` | Sole sampled pair-equality reader; repaired and tested through production. |
| F1 `Invoke-Preflight.ps1:Assert-Guard` | Independently validates state/event binding, continuation and state freshness, returning state count without pair equality. Exact false-refusal class absent. Cross-file consistency is not certified; owner `@pc-coordinator` retains that separate gap for future packet review. No packet change here. |
| B2 `capture-completed-install.ps1` | State freshness/binding and event stop detection without pair equality. No exact false-refusal sibling; pair consistency unclaimed. |
| B1 `capture.ps1`, B2 `capture-install.ps1` | Observer state/start-row admission and event detection; no completed pair-equality reader. |
| F1 and B2 `completed_guard.py:watch` | Both append event before atomic state replacement; order preserved. |
| Guard binders, manifest freezer, public-prefix publisher | Consume complete event prefix without joining live state. No exact sibling. |
| Ownership helpers | Exact argv/event-path ownership; no live state/event equality read. |

No `assume:` or `simplify:` marker occurs in F1 packet/recovery. Derive: handle
the two-file boundary explicitly while retaining every admission invariant and
deadline. Prevent: `tests/test_f1_cleanup_recovery.py` uses the actual reader and
actual `append_event`/`write_atomic_json`, temporary files, an inert process and
a deterministic clock. Ring: offline-fast, no application build-ring addition.

# Red-first oracle

Before repair, the actual production reader failed this regression:

```text
py -3 -m unittest discover -s tests -p test_f1_cleanup_recovery.py -k EventPublishedBeforeState -v
Ran 1 test in 0.026s
ValueError: guard event/state disagree
FAILED (errors=1)
```

The fixture publishes pair 1, appends event 2 with the real writer, and finishes
state 2 at the first mocked wait. The old reader raises before that completion.
The repair returns sample 2 after one bounded wait. The initial targeted green
run measured **15 tests / 0.757 s**. Negative controls plant persistent adjacent
skew, other gaps/regression, UTC disagreement, binding changes, stopped/stale/
future samples, malformed rows, invalid publication during retry, child exit,
absolute/caller deadline exhaustion and expiry during matching read. An actual
action fixture proves local-deadline propagation into the reader.

# Execution graph and bounded work

The optimized graph collapses investigation and sibling sweep into one grounding
node, and extends the existing harness instead of creating another probe program.
Every floor remains; finite checks have explicit pass/fail oracles.

| Node | Goal, inputs and exit | Tier / Capability | Dependency |
| --- | --- | --- | --- |
| G | Inspect source, receipt, sibling contracts; exit inspected boundaries | T1 / Reasoning | Assigned contract |
| R | Exercise production publication gap; exit observed red | T1 / Deterministic mechanics; floor | G, data |
| F | Repair gap and invalid/deadline controls; exit targeted green | T1 / Reasoning | R, decision |
| V | Complete offline suites/gates and evidence conservation; exit measured results | T1 / Deterministic mechanics; floor | F, data |
| C | Derive docs/audit, commit/push author candidate; exit remote commit | T1 / Deterministic mechanics; floor | V, decision |
| A | Coordinator independent review; exit commit-bound accept/veto outside author | T1 / Independent review; floor | C, data |

```mermaid
flowchart LR
  G --> R --> F
  F --> V
  V --> C
  C --> A
```

Before/after: six nodes, no agent fan-out. Inferred node work/span: six total,
six on the longest chain; no fabricated timing or agent speedup. Bounded
independent read-only checks may share one tool call. Remaining failed acceptance
clauses form the repair-loop variant, floor zero; cap two cycles, with cap firing
stopping the track and reporting to the coordinator. Re-plan only on an offline
failure or independent veto; no live retry or dropped gate is a degradation path.
Whole-run duration comes from the audit marker. Suite times below are observed.

The installed skill references `kb-graph-and-loop-engineering`, but that artifact
and its `docs/knowledge/graph-and-loop-engineering/` evidence directory are absent
in this checkout. The initial derive reported one dangling link; it was replaced
with the inspected packet dependency. Existing graph validation is the control
for this REVIEW-ENUM-CONTRACT-A recurrence; no missing source is claimed read.

# Verification ledger and residual risk

**Verified author checks:**

| Check | Observed result |
| --- | --- |
| Targeted guard controls | 15/15 PASS; 0.757 s |
| Complete recovery offline suite | 45/45 PASS; 1.384 s |
| Complete packet Python fixture suite | PASS; offline-fast, 0.614 s |
| Packet PowerShell self-tests | PASS; Windows PowerShell 5.1 0.334 s, PowerShell 7 0.254 s |
| Authority scanner | Planted-negative and packet scan PASS |
| Manifest, transaction, completed-guard self-tests | PASS |
| Safe verify bundle | 10/10 PASS; application-core, application-adapters and Windows-store build-backed gates explicitly excluded |
| `tools/check-docs.py` | PASS; graph/freshness/audit/lesson checks ran |
| Historical manifest verification at `11f56c79` | Git-binding PASS: checkpoint 76 blobs, failed-live 94, recovery-final 255 |
| Historical corpus diff at `11f56c79` | Empty for all three capture directories |
| Graph | Zero defects; 134 existing freshness suggestions. Strict `--gate fail` refused those suggestions; ordinary defect gate passed. No flag was cleared. |
| Proof privacy / whitespace | Closing results recorded by the audit entry |

Actual delivery: G/R/F/V/C author nodes complete; independent A is intentionally
after candidate publication so the coordinator can review committed bytes. One
implementation cycle, no second repair cycle; one document-link correction, no
gate weakened. Audit duration measures from the marker after initial grounding.
No token/cost figure is invented. No PowerShell or Bash source changed; both
existing PowerShell suites were still run. No Bash/WSL launcher was invoked.
The application build/test ring and readiness were excluded by the explicit
no-build scope; this is an offline author candidate, not a join to main.

**Flagged:** Python control flow and real local publication functions are proved;
current WSL state, guest/host visibility, ownership, lifecycle effects and
protected terminal invariance are unobserved. File I/O cannot be interrupted by
this reader: budget checks refuse after return, not guarantee kernel-I/O latency.
Separate files retain an admission-to-next-operation window; bounded rereading
does not make a transactional snapshot. Persistent skew refuses. No live action
ran. Independent review acceptance is recorded separately by the coordinator.
