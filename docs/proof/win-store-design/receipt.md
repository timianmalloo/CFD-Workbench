---
id: proof-win-store-design
title: "B1 Windows store design author checkpoint"
type: proof-pack
status: in-review
owner: "@win-store-design-20261007"
phase: windows-w-2-design
tags: [windows, persistence, design, proof]
links:
  - { to: proof-win-store-design-candidate, rel: documents }
  - { to: coordination-windows-w0-w5-execution, rel: implements }
  - { to: design-windows-runtime, rel: relates-to }
review-by: 2026-11-07
summary: >-
  B1 author handback for the complete Windows store design candidate. The final
  design path is held pending affirmative Mac handoff; native qualification and
  independent Data, Security and Test approval are not asserted.
---

# B1 author checkpoint

Goal: prepare the source-cited Windows native project-store design for Mac/Fable review.
Done when: a complete candidate and exact approval request are committed under the
owned proof subtree, validation is recorded and the coordinator receives the handback.
Not in scope: implementation, final design-path write without Mac handoff, shared
audit/register/derived files, push or PR. Tier T1; fan-out cap 1; no subagents.

Seat: implementation worker / design author. Worktree
`C:\Projects\CFD-Workbench-win-store-design`; branch `win/windows-store-design`.
Grounding and working-tree validation base HEAD:
`70c9ba534ebc858cad48ba260bb30e2901affbd2`.
The final committed validation SHA is returned by the author to the coordinator;
the coordinator must retain that exact SHA in the delivery receipt and PR.

Observed host for documentation commands: Windows version `10.0.26300.0`
(`System.Environment.OSVersion.VersionString`), Python `3.13.14` (`py -3 --version`).
SDK/native compiler are Not assessed here: no build or native execution was needed
or authorized for this docs-only design checkpoint. W-0 is linked as historical host
evidence, not promoted into this author's runtime proof.

## Compiled scope and authority changes

The coordinator assigned B1 only, with owned paths
`docs/design/windows-native-store.md` and `docs/proof/win-store-design/**`, and excluded
all source/tests/tools/cases, coordination, audit/register and derived files.
Mac PR #1 conditions require a NEW Windows helper rather than `native/cfd_store.c`.
The author performed fetch/merge/xmsg reads before writing.

After grounding the coordinator relayed the Astra Owner's gate: research/proof
drafting PASS, final `docs/design/windows-native-store.md` write HOLD until the Mac
affirmatively answers the handoff. No final design file was written. The complete
content lives in [candidate.md](candidate.md), not in an unauthorized path.

The coordinator reports W-1 delivered through
[PR #3](https://github.com/timianmalloo/CFD-Workbench/pull/3), head `cb34dcee`, tested
evidence SHA `75335f8d`, pending Mac disposition. This author did not inspect/run the
W-1 evidence or copy it into B1. Its state is Reported, not Verified by this author.

The candidate specifies the separate helper `cfd_store_windows.c`, the managed
Windows adapter, native requests/sharing/identity/security/reparse policy, atomic
publication oracles and the unsatisfied namespace-durability gate. Its exact Mac/Fable
approval request is section 11. It preserves Ruling 121's existing optional units key
and ADR-0011's old-byte backup requirement.

## Optimize-graph execution record

The assigned delegation was the compiled input; the author used optimize-graph and
design-slice once. Plan: grounding source and Microsoft contracts → one integrated
candidate → documentation validation → one exact-path commit/handback. Independent
review is a coordinator decision edge after handback; it cannot be self-cleared.

Naive and actual author graphs each have four nodes; work and span are four node
units at width one. This is a structural count, not a duration model. No speed/token
saving is claimed. Batched independent reads reduced incidental command ordering;
edits, validation and commit remained serial. Native measurement is a separate B2
floor, not removed from the graph by a docs-only assignment.

Floors retained: prior contract reading, DDD/grain/history, end-to-end surfaces,
primary API sources, failure/security/privacy cases, exact helper/B2 seams,
truthful unverified durability and independent veto criteria. Loop variant is the
number of concrete author/documentation defects; one repair cycle allowed, no retry
to evade a native or ownership gate. Derivation drift is handed back, not repaired
outside the lease. Audit duration, token usage and native API cost are Not recorded;
the author does not model plausible numbers.

## Validation

`git fetch origin`: exit 0. `git merge --no-edit origin/main`: exit 0, already up to
date. `py -3 tools/xmsg.py unread --mark`: exit 0, five messages, including Ruling 121
and PR #1's helper/units conditions. `git diff --check`: exit 0.

Initial `py -3 tools/check-docs.py`: exit 1 solely for the candidate's missing derived
index entry. It reported no frontmatter problems, no orphans and actual spiral check
PASS. The printed synthetic `SPIRAL: 12 commits` failure belongs to the checker
self-test; the actual branch printed `spiral check: ok (33 commits, 11 product)`.
Other completed static/hook/lesson checks passed. There were 131 existing review
suggestions, explicitly non-failing. No source/tests/ring/native execution was run.

Final checkpoint raw docs validation output is retained in [check-docs.txt](check-docs.txt).
The coordinator must derive the index and rerun the branch docs gate before delivery;
new candidate and receipt entries are expected index drift under this lease. This
checkpoint does not claim a green full docs gate or readiness.

One patch call used a nonexistent exact context and was rejected before any write.
The correction used the actual file text; no product defect or data mutation occurred.
The author stayed within the one-cycle repair cap. General class: editing from an
unobserved context; preventive rule is open the actual context before patching.
The coordinator owns any needed shared defect-register/audit treatment; no such
files were edited here.

## Handoff and remaining work

Only candidate, author receipt and raw documentation check output are committed.
No source, tests, tools, cases, coordination, audit, lessons, index or final design
path changed. No push or PR was made.

Coordinator actions: obtain Mac final-path handoff, arrange independent Fable/Data/
Security/Test review, rule D-B1-DUR, narrow/authorize exact B2 shared seams, then pay
official docs derivation, audit/change entries and required rollups before PR delivery.
The final design and B2 remain gated. A native flush success cannot resolve the
shared product semantics, and an approval cannot substitute for native qualification.

| Status | Result |
|---|---|
| Completed | Full reviewable B1 candidate in the allowed proof subtree |
| Remaining | Final design-path handoff, coordinator derivation/audit, independent verdicts and B2/native/product proof |
| Best next action | Mac/Fable answer candidate section 11's exact approval request |
