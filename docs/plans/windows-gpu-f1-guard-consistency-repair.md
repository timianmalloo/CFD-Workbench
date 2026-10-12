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
  R224's production regression returns stable state N when continuing event N+1
  has later UTC. Invalid gaps refuse; moving-state rereads retain bounded and
  inherited deadlines. Every recovery ack/intent reader is swept.
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
inherited 12-second ceiling. Under **Ruling 224**, a continuing event exactly one
sample ahead of a fresh running state, with strictly later UTC, is in flight:
the reader returns stable state N immediately, without waiting for N+1. It also
accepts an equal sample/timestamp pair. It confirms state remained unchanged
during the observation. Only healthy forward state motion during confirmation
causes a bounded reread. Changed binding, stopped/stale observations, child exit,
malformed complete rows, same-sample UTC mismatch, other gaps and regression
across reads refuse, as does adjacent skew with same/earlier UTC. Liveness,
freshness and budget are checked again before return. Reads finishing after a
deadline cannot pass. Motion retries enter the log as `guard-consistency`; normal
in-flight admission records completed/event samples and elapsed seconds as
`guard-in-flight`. Failures retain the classified receipt path. The ruling was
read at `origin/main:docs/notes/rulings.md`; its separate read-only live permission
was not exercised by this offline author task. The citation gate initially
refused because this branch ended at R223. The coordinator authorized importing
only `docs/notes/rulings.md` from accepted commit
`ed40faefef2771cfd33a723100f15b6b3f0047ea`: six appended lines, byte-identical
Git blob to that accepted source. This imports existing authority without
authoring or changing its terms; no live permission was exercised.

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

No `assume:` or `simplify:` marker occurs in F1 packet/recovery. Derive: preserve
the stable completed generation and distinguish its valid in-flight successor
from invalid disagreement. Prevent: `tests/test_f1_cleanup_recovery.py` uses the
actual reader and `append_event`/`write_atomic_json`, temporary files, an inert
process and deterministic clock. Ring: offline-fast; no build-ring addition.

## Complete ack/intent reader sweep in `recover.py`

**Verified by opening every match and call site:** recursive scan root is the
entire `recovery/recover.py`; tokens `ack`, `intent`, `.ack.json`, `.intent.json`,
`previous_ack_sha256`, and `assert_ack`; no allowlist. No host-ack JSON reader
exists in this source. Every guest pair interpretation is listed below.

| Reader/publisher or call site | Observed publication and disposition / executable control |
| --- | --- |
| `publish_intent` | Reads its own temporary intent and the previous owner digest before launching; atomically publishes the new intent, then copies its history. Existing owner bytes are a previous-generation exclusion hash, not a required equal paired generation. Missing previous owner intentionally records null. `test_Ack_StaleOrPartialOwner_Refuses` proves unchanged previous owner refuses after publication. |
| `assert_ack` | Reads the new intent plus owner digest/fields; requires owner presence, changed previous digest, complete PID=PGID=SID and affinity/nice. It does not demand equality between records. Before atomic `identity.sh` owner publication, refusal is required because ownership is unknown; new `test_Ack_IntentVisibleBeforeAtomicOwnerPublication_RefusesThenAccepts` plants that window, then atomically publishes valid owner bytes and proves acceptance/history. Existing partial/stale tests retain refusal. |
| `linux` → `assert_ack(label)` | Publishes intent before synchronous `command`, asserts owner only after child completion. `test_Linux_MissingEntryAck_RefusesAndRetainsIntent` exercises this actual call path. No added retry or relaxed ownership rule. |
| `start_guard` → `assert_ack('guard')` | Publishes intent before launch, waits boundedly for guard state, then requires owner. A visible state with absent owner remains unknown and refuses; guard startup failure does not authorize work. The actual `assert_ack` missing-owner fixture covers the pair-reader boundary; existing guard-expiry fixtures cover prelaunch refusal. |
| `stop_guard` → `assert_ack('guard')` | Performs the owner assertion after independent stop/check attempts. Assertion failure is retained with other finalization failures, not retried into success. `test_GuardStop_MultipleErrors_RetainsAllWithNoLaunch` proves disposition. |
| `host_intent` / `host_ack`, called by `command` and `start_guard` | Intent is atomically published before Popen; host ack is separately written after numeric PID validation. No reader joins these JSON records for equality. Pending intent is deliberate evidence, not guest absence. Existing `test_NativeChild_IntentPrecedesLaunchAndRealPidAcknowledged` and new `test_HostAck_NonnumericOrMissingPid_RefusesAndKeepsPendingIntent` prove ordering and refusal. |
| `prepare_public`, `bind_evidence`, manifest verification | Copy/hash retained records as evidence without interpreting live ack/intent equality. No TORN-PAIR-READ sibling. Evidence I/O failures still prevent complete binding. |

No ownership reader was modified. **Flagged residual:** `assert_ack` obtains
digest and fields in separate reads of the owner path; under concurrent owner
replacement those reads can span generations. The supported flow serializes an
intent before its child and uses atomic owner publication, but this sweep does
not certify arbitrary concurrent replacements. Owner `@pc-coordinator` retains
that separate race risk; guard in-flight admission is never applied to ownership.

# Red-first oracle

Before repair, the actual production reader failed this regression:

```text
py -3 -m unittest discover -s tests -p test_f1_cleanup_recovery.py -k EventPublishedBeforeState -v
Ran 1 test in 0.026s
ValueError: guard event/state disagree
FAILED (errors=1)
```

The fixture publishes pair 1 and appends event 2 with strictly later UTC using
the real writer. R224's reader returns stable sample 1 without sleeping; it does
not promote the in-flight event to a completed sample. The first candidate
`b63c1f04` waited for equality and returned 2, which did not conform to R224.
Cycle-1 red: return-N failed there with **2 != 1 / 0.055 s**; same/earlier UTC
counterexamples were incorrectly accepted (**two failing subcases / 0.106 s**).
The exact return-N fixture was also red against production bytes loaded with
`git show 5b1a9bb4:.../recover.py` into an isolated module: **exit 1 / 0.040 s**,
`guard event/state disagree`. Neither historical executor was invoked.
After repair, return-N and strict-UTC controls pass. Negative controls plant
persistent invalid skew, continuously moving state, other gaps/regression, UTC
disagreement, binding changes, stopped/stale/
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
| Complete recovery offline suite, cycle 1 | 50/50 PASS; 2.020 s, including healthy forward-motion stabilization |
| Complete packet Python fixture suite, cycle 1 | PASS; offline-fast, 0.469 s |
| Packet PowerShell self-tests, cycle 1 | PASS; Windows PowerShell 5.1 0.317 s, PowerShell 7 0.252 s |
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
initial implementation and review-requested repair cycle 1 of max 2; one
document-link correction, no gate weakened. Audit duration measures from the
cycle's marker after initial grounding.
No token/cost figure is invented. No PowerShell or Bash source changed; both
existing PowerShell suites were still run. No Bash/WSL launcher was invoked.
The application build/test ring and readiness were excluded by the explicit
no-build scope; this is an offline author candidate, not a join to main.

**Flagged:** Python control flow and real local publication functions are proved;
current WSL state, guest/host visibility, ownership, lifecycle effects and
protected terminal invariance are unobserved. File I/O cannot be interrupted by
this reader: budget checks refuse after return, not guarantee kernel-I/O latency.
Separate files retain an admission-to-next-operation window; bounded rereading
does not make a transactional snapshot. Invalid or stale skew refuses; fresh
adjacent/later/continue skew deliberately returns stable N under R224. No live action
ran. Independent review acceptance is recorded separately by the coordinator.
