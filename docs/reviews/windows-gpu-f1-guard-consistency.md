---
id: review-windows-gpu-f1-guard-consistency
title: "Independent R224 guard-reader review"
type: doc
status: accepted
owner: "@pc-f1-r217-guard-consistency-review"
phase: preflight
tags: [windows, recovery, concurrency, independent-review]
links:
  - { to: plan-windows-gpu-f1-guard-consistency-repair, rel: depends-on }
  - { to: proof-win-gpu-f1-r217-packet, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: "2026-10-19"
summary: >-
  Independent ACCEPT of 5c1b727f for the R224 offline guard-reader contract.
  The actual regression is red on the old reader and all 50 recovery tests pass.
  Historical failure evidence and authority limits remain intact.
---

# Verdict and exact scope

**ACCEPT**, Verified with high confidence, exact author candidate
`5c1b727fe4448020701b044247d417a3d61c647f` for offline source integration under
Ruling 224. The reviewer used a separate worktree and changed no candidate
source, tests, authorization or captured evidence. This review grants no live
permission and is not a main-branch join or readiness result.

Goal: independently accept or veto the guard publication repair. Done when the
production boundary, negative controls, sibling disposition and evidence
conservation are verified and this review is committed. Not in scope: WSL, live
verification, cleanup, retry, downloads, installation, builds or CFD. Tier T1;
one reviewer, no sub-agent fan-out. The review graph was source/authority
inspection → independent controls and conservation → verdict, audit and derive.
One author repair cycle followed the initial R224 contract findings. No review
gate was weakened. Elapsed audit duration includes the wait for that amendment.

Bound source blobs:

| Surface | Git blob at the accepted candidate |
| --- | --- |
| `recovery/recover.py` | `24fce4a8670174a29aa9176e07f55cb0eddbe8f0` |
| `tests/test_f1_cleanup_recovery.py` | `512fae3be1f589614b5d800d9141ab540cde688e` |
| `docs/notes/rulings.md` | `f5d4d2c61818a5dae3d179b18d1bc6a5a8b4ba49` |

# Contract and adversarial findings

**Verified:** the accepted writer appends/fsyncs event N+1 before atomically
replacing state N. The old reader demands immediate equality. The private-error
digest in historical receipt `11f56c79add5054c420c23911dbb2cf748c659c7` equals
SHA-256 of `guard event/state disagree`. The exact intermediate live reads were
not captured; the publication interleaving is demonstrated by the offline test.

The initial candidate `b63c1f04249c2258433e0210b7405ba033a3058d` waited for
equality and returned N+1; its retry classification also omitted strictly later
UTC. That differs from R224. Those findings blocked acceptance. The coordinator
required literal R224 semantics and the author supplied the accepted amendment.
The review's initial equal-pair-only proposal is superseded by that ruling.

**Verified:** `guard_sample_time` and `guard_ok` at lines 427 and 437 now accept
an equal sample/timestamp pair, or exactly N+1 with strictly later UTC and a
`continue` verdict, while returning only stable completed state N. Both records
retain the session and expected-snapshot binding checks and freshness limits.
State is reread before admission; observed stopped/stale/rebound state refuses.
Healthy forward state motion triggers another observation, capped at ten and
0.5 monotonic seconds. The absolute 600-second recovery budget remains enforced;
the post-unregister fresh-sample wait passes its 12-second deadline into the
reader. Child liveness, freshness and budget are checked before return.

N+2, reverse/invalid gaps, same-sample UTC disagreement, same/earlier adjacent
UTC, malformed complete events, changed binding, stopped state, stop verdict,
staleness and exhausted budgets refuse in the exercised controls. Continually
moving state cannot cause an unbounded read loop. A fresh valid adjacent gap is
deliberately accepted under R224: persistence is bounded by state freshness,
not by a cross-call 0.5-second skew timer. Invalid persistent gaps refuse.

# Independent verification

All results below were observed by the reviewer. The packet, helpers, historical
corpora and gate implementations were unchanged between the two candidates;
their initial independent results apply to the amended candidate by an empty
Git diff. Recovery tests, safe gates and the full docs gate were rerun after
the amendment.

| Check | Observed result |
| --- | --- |
| Actual return-N fixture against old `5b1a9bb4` source loaded into an isolated module | Expected RED: `guard event/state disagree`, 1 test, 0.034 s; no executor invoked |
| Complete amended recovery suite | 50/50 PASS, 2.476 s; includes production publication, UTC, persistent-invalid, moving-state, deadline and ack controls |
| Complete packet Python fixture suite | PASS, offline-fast, 0.543 s |
| Packet PowerShell self-tests | PASS, Windows PowerShell 5.1 0.345 s; PowerShell 7 0.284 s |
| Authority scanner | Planted-negative and packet scan PASS |
| Manifest, transaction and completed-guard self-tests | PASS |
| Safe verify bundle on amended candidate | 10/10 PASS; application-core, application-adapters and Windows-store build gates explicitly skipped |
| `py -3 tools/check-docs.py` on amended candidate | PASS; 17 capture manifests match committed blobs; audit records readable |
| Graph | Zero defects; 134 pre-existing review suggestions retained |
| Proof privacy | PASS, zero prohibited Windows user paths or machine SIDs; one existing allowlist entry |
| Historical manifest verification at `11f56c79` | PASS: checkpoint 76 blobs, failed-live 94 blobs, recovery-final 255 blobs |
| Historical capture/authorization conservation | Empty diff from `11f56c79` for `run-1` and `recovery-1` |
| R224 text conservation | Empty diff for `docs/notes/rulings.md` against accepted `ed40faefef2771cfd33a723100f15b6b3f0047ea` |

Ring: existing offline-fast recovery and packet checks, with measured costs
above. No new test or gate was added by this review. Application build/test ring
and readiness remain obligations of an authorized join, outside this no-build
offline review. Final review-only changes receive graph, audit, privacy,
whitespace and ruling-citation checks after publication.

# Class and sibling disposition

**Verified:** the exact `TORN-PAIR-READ` register entry contains class → sweep →
derive → prevent, production-boundary red/green evidence, test ring/cost, the
initial contract correction and assigned residual risks.

The independent sweep inspected the F1/B2 append-before-state writers; F1
PowerShell `Assert-Guard`; B2 completed-install state/event checks; B1/B2 observer
readers; and prefix binders. F1 recovery is the pair-equality false-refusal site.
The other readers do not establish atomic pair consistency, and this review
does not certify them. The coordinator owns that separate packet gap.

Every recovery ack/intent interpretation is now explicitly accounted for:
`publish_intent` records a previous-owner exclusion hash before launch;
`assert_ack` rejects absent, stale and partial ownership after command completion
or guard-state readiness; `linux`, `start_guard` and `stop_guard` retain those
checks; host intent/ack records have no equality-joining reader. `identity.sh`
publishes ownership atomically before executing the guard. Evidence copying and
binding do not interpret live pair equality. New ack-window and invalid-host-PID
controls plus existing stale/partial/finalization fixtures passed. Ownership
never inherits the guard's in-flight exception.

# Evidence and authority limits

The original receipt remains `cleanup_proved: false` and
`recovery_success: false`; its initiating error and absent historical
`l3-after.json` remain intact. Source acceptance does not rewrite that outcome.
R223's single cleanup attempt remains consumed. R224's separate, one-shot,
120-second read-only verification has exact conditions and forbids lifecycle
mutation and preflight stages. This review exercises none of that permission.
Any subsequent receipt is a separate record, not a replacement of `11f56c79`.

**Flagged residuals:** host/guest visibility and live WSL behavior were not
measured here. Blocking filesystem I/O can finish after a deadline; checks refuse
late admission rather than interrupt kernel I/O. Separate records still have an
admission-to-operation window. Arbitrary concurrent owner replacement can split
the digest and fields reads in `assert_ack`; the coordinator owns that separate
risk. No live safety claim or new execution authority follows from this ACCEPT.
