---
id: plan-f1-attempt2-contract
title: "F1 attempt 2 source freeze and offline contract preparation"
type: plan
status: active
owner: "@pc-coordinator"
tags: [windows, offline, f1, execution-graph]
links:
  - { to: proof-win-gpu-f1-attempt2-admission, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-20"
summary: >-
  Final C1 repair followed by a separate source commit and deterministic requested
  contract pair for an uncreated future execution worktree.
---

# Goal and boundaries

Goal: finish the second and final offline repair cycle and prepare the contract
selected by Mac in [PR39 comment 6154714050](https://github.com/timianmalloo/CFD-Workbench/pull/39#issuecomment-6154714050).
Done when: C1 and preparation tests pass, independent review accepts the source,
the source is committed before generation, the packet's own producer emits a
reviewed requested-form pair, and the candidate is audited and pushed.
Tier T2, one independent review boundary, two total repair cycles for the track;
this work is cycle 2. No WSL, registry, lease mutation, lifecycle, preflight,
full ring, OpenFOAM run, or future execution-tree creation is authorized.

The author tree is `C:\Projects\CFD-Workbench-win-gpu-f1-attempt2-contract`,
branch `win/gpu-f1-attempt2-contract`, session `pc-f1-attempt2-contract`, at base
`51b1cc65900c5e2ba87f4461b0d3a4938fdf7a73`. Parent had already created the
assigned fresh tree; its exact branch/base and clean state were checked.

# Execution graph

| Node | Goal / inputs | Exit condition and oracle | Tier / capability | Dependency |
|---|---|---|---|---|
| G | Read exact Mac conditions and joined source | Source paths/constraints observed | T2 Reasoning | None |
| R | C1 emitter and receipt red fixtures; preparation red fixtures | Old source fails missing pre-write evidence and absent producer | T1 Deterministic mechanics | G, decision |
| S | Minimal source update, preserving live verify | Strict pre-write TTL; offline proposed-only producer; no live checks | T2 Reasoning | R, data |
| V | Focused Python + both PowerShell runtimes + docs gates | C1, conservation, source/blob, input/path, malformed/dirty/existing-output refusals | T1 Deterministic mechanics | S, data |
| A | Independent source review, audit, source commit | Exact source accepted and committed | T2 Independent review | V, decision |
| P | Read-only leader measurement and packet preparation | Live mac epoch >=200 at binding; two deterministic requested outputs, future tree absent | T1 Deterministic mechanics | A, data |
| J | Artifact review, docs/audit, final commit/push | Exact source + both file hashes reported | T2 Independent review | P, data |

```mermaid
flowchart LR
  G --> R --> S --> V --> A --> P --> J
```

These seven nodes are the naive and optimized graph; no safety/data edge can be
removed. Python and PowerShell checks inside V are independent and run with a
tool-call width cap of two. Source editing, Git operations and output generation
are serial. Reviewer failure stops the final cycle rather than starting a third
repair. The generation variant is two absent files to two verified proposed
files, with one invocation and no automatic retry. A failed prerequisite yields
zero new outputs and a report. No fallback weakens a gate or creates the future
tree. The producer uses exclusive output creation and removes only its own
partial pair after a filesystem failure.

Cost baseline, **Verified** from the prior accepted source: focused Python union
23.110 seconds; eight PowerShell suites on each runtime 18.361 seconds. These
are measurements of a previous snapshot, not predictions. Work/span for this
turn are not yet measured. Within V, the ideal span is at least the larger
Python/PowerShell cost; docs gates and independent review retain their own
latency. No model-token count is available. Completion and rigor take precedence
over parallel speed. The installed docs graph has no graph-engineering knowledge
node; this plan uses the loaded execution-graph standard and prior measured proof.

# Model, surfaces and decisions

One renewal fact carries the pre-write TTL from the observed ref record. This
non-additive measurement cannot be inferred from a post-renewal sample. One
preparation produces exactly one invocation value and one proposed authorization
value, identified by source commit and raw SHA-256. They are immutable after
commit; Mac creates accepted authorization separately with the live ruling.

Surface list: renewal return value -> first event -> receipt validator ->
PowerShell 5.1/7 fixtures; source Git blobs -> packet/external/input hashes ->
future B2 path projection -> proposed authorization -> committed bytes. Live
verify remains unchanged. Existing parent PID15528 binding is retained and
explicitly tested. Empty stderr means zero characters, including no whitespace.

Preparation is a new explicit producer mode under `contract.py prepare`, selected
by `--source-commit` with the future worktree, epoch and requested ruling. The old
validation-only prepare mode remains. The producer requires clean author HEAD
equal to the source commit; exact packet set and byte equality with source blobs;
fixed seven input hashes; only B2 repo-relative paths projected onto the future
root; in-memory schema/binding/authorization checks; and two absent exact output
paths. It never calls the live verifier or a leader operation. The future tree
must remain absent. A separate read-only leader observation selects the epoch.

Mac selected the future tree `C:\Projects\cfd-workbench-win-gpu-f1-r225-attempt2`,
branch `win/gpu-f1-attempt2`, session `pc-f1-attempt2`, run
`f1-attempt2-20261013`, root `docs/proof/win-gpu-f1-preflight/attempt-2`, and
requested ruling 235. These are requested identities, not a live ruling.

# Evidence and ledger

C1 emitter red: missing `expires_in_before` raised KeyError; 12-test renewal
suite failed one test in 0.366 seconds. Receipt red: both PowerShell runtimes
accepted missing/invalid pre-write TTL. Green after the source repair: 12 renewal
tests passed in 0.252 seconds and both receipt suites passed. Startup ordering
was not changed. Preparation red: five tests failed before the producer existed;
green: five real-Git tests passed in 5.192 seconds. All tests are offline-fast,
with live boundaries mocked. Final counts/review/source/hash bindings belong in
the companion contract preparation proof after they are observed.
