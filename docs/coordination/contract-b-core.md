---
id: coordination-contract-b-core
title: Proposed first production core author assignment
type: plan
status: accepted
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, core, implementation]
links:
  - {to: coordination-application-build, rel: depends-on}
  - {to: coordination-contract-b0, rel: depends-on}
  - {to: design-application-foundation, rel: depends-on}
  - {to: design-application-contracts, rel: depends-on}
  - {to: adr-application-project-contract, rel: depends-on}
  - {to: coordination-application-cancel-drill, rel: relates-to}
review-by: 2026-10-23
summary: Ruling 13 freezes one serial first-core implementation track and exactly 18 authored paths, subject to actual worker identity and cache preflight.
review-suggested:
  - { by: adr-application-project-contract, on: 2026-09-23, reason: "Owner Ruling 13 accepts the reviewed unshipped native-v1 contract for bounded serial core implementation; product proof gates remain open." }
  - { by: coordination-application-build, on: 2026-09-23, reason: "Active-seat dispatch control and observed serial core checkpoints added; review execution references." }
  - { by: design-application-contracts, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
  - { by: design-application-foundation, on: 2026-09-23, reason: "R17-19 reviewed evaluator v2 and native-store companion changed this dependency; review current contract claims" }
---

# B · first production core packet (G3 frozen, not dispatched)

**Admission gate:** serial B0's committed session/schema/identity/persistence
contract and executable fixtures passed bounded Owner Ruling 12 and independent
root review on joined commit `c13db27`. Owner Ruling 13 freezes this exact
18-path serial G3 contract. Ruling 11 conditionally selects one serial
built-in Codex core author under equal observed-only controls, but clears no
dispatch without actual worker preflight: the [same-harness cancellation drill](application-cancel-drill.md)
observed agent interruption and explicit owned-child cleanup, while exact
identity/path/cache checks remain. Ruling 13 authorizes implementation within
the frozen lease after those checks, not product acceptance. No further Grok
mode/probe loop is proposed.

| Assignment field | Provisional value; freeze at G3 |
|---|---|
| Session, branch and worktree | Not assigned. Create a fresh `feature/application-core` tree through `coord worktree new`; record exact session, branch, absolute path and base commit before the first author command. |
| Author profile | Requested `gpt-6-astra`; effective model `Not recorded` only under Ruling 11's first-core exception. The disposable same-harness drill is recorded; the production worker still needs its own exact launch/cwd/branch/base readback. |
| First command | `audit-log.py start --session <assigned> --skill implement` from assigned worktree; read full `implement` contract and B0 design. |
| Initial budget and checkpoints | 90 tool calls, ≤55 minutes, ≤100k context for first run. This is a checkpoint estimate, not a claim that the broad core will be done in that window. Checkpoints after red boundary fixtures, compiling core API, and store fault tests. At cap, report/replan without claiming completion. No author subagents. |
| Handback | Exact tracked/untracked path inventory, cwd/base/final HEAD, dependency and test receipts, proof pack, clean descendant commit. Root/Owner independent code and behavior review before join. |
| Lifecycle | Track child PID plus process start identity for each launched build/test; interruption stops dispatch, terminates only verified owned children and reads back absence. The drill showed `interrupt_agent` alone leaves a child running. Identity drift or unmanaged live processes stop for review. |

## Provisional exact ownership

These are the [foundation design](../design/application-foundation.md)'s path
proposals expanded into **18 frozen exact leases** under Ruling 13: its 16
source/build/test paths, one authored proof pack and one argument-free project
gate. They bind to the joined [session contract](../design/application-contracts.md),
[ADR 0004](../adr/0004-application-project-contract.md), public namespace and
fixture. The worker may not
write adjacent paths until the Coordinator publishes that freeze.

| Area | Candidate B-owned paths |
|---|---|
| Shared build pins | `global.json`, `CFDWorkbench.slnx` |
| UI-free core | `src/CfdWorkbench.Core/CfdWorkbench.Core.csproj`, `src/CfdWorkbench.Core/Contracts.cs`, `src/CfdWorkbench.Core/FoilSource.cs`, `src/CfdWorkbench.Core/Geometry.cs`, `src/CfdWorkbench.Core/Identity.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs` |
| Native persistence | `src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj`, `src/CfdWorkbench.Persistence/ProjectStore.cs` |
| Core proof | `tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj`, `tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs`, `tests/CfdWorkbench.Core.Tests/GeometryTests.cs`, `tests/CfdWorkbench.Core.Tests/IdentityTests.cs`, `tests/CfdWorkbench.Core.Tests/AuthoringSessionTests.cs`, `tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` |
| Durable evidence and gate | `docs/proof/application-core.md`, `tools/verify-application-core.py` (argument-free local build/test and named contract assertions, auto-discovered by the repository verification runner) |

No GUI, CLI adapter, markup, mockup, spec, architecture or B0 design authoring
belongs to B. The future C worker owns native Desktop and CLI adapter paths
after B exposes an actual compiled public core and consumer fixture; the
current sketch alone does not remove that decision edge. B and C remain serial
unless G3 proves stable independent interfaces and separate path ownership.
Official `docs/docs-index.js` and audit/change JSONL/render outputs are
generated/register handback exceptions, never extra authored path leases.
The focused native persistence capability reproducers belong inside the leased
`tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs` **before** corresponding
platform adapter implementation. Record exact SDK signatures and measured
behavior. No additional spike path is leased. Unknown or failed platform
primitives remain Unsupported; the worker cannot create an unleased
`tools/spikes/` file or infer Windows runtime safety from macOS tests.

## Required core result

Recognize the normative FoilDSL grammar and validate the **entire** source,
including features whose geometry is not admitted in M1. Those features receive
an explicit Unsupported/Not assessed outcome; the bounded B0 spike parser is
not production-language conformance. Use exact UTF-8 source snapshots and
diagnostics. `.foil` opening with missing IDs presents the
deterministic candidate diff and requires explicit acceptance; built-in Example
has an already authored accepted path. Parse/validate the whole source and
reject unsupported features rather than treating a partial read as valid.
Complete exact decimal/unit conversion, RFC 8785/BLAKE3 semantic identity and
distinct source SHA-256. The certified geometry evaluator admits only its
continuously proved subset, with bounded normalization/placement error;
uncertain or unsupported inputs stay Not assessed/Unsupported and cannot Apply.

Implement one session-owned independent leading/trailing rail numeric draft,
preview/Apply/Cancel, accepted source/Design/Surface revision distinctions,
cursor-fact Undo/Redo and durable operation-ID replay. A stale, retargeted or
forged certificate cannot Apply. Native-v1 save/reopen keeps immutable history
and separate recovery draft. Growth refusal keeps dirty state and last complete
file. Persistence must meet the approved new-file no-replace and cooperative
overwrite handle policy, with real filesystem fault/conflict tests; an
unsupported platform primitive reports `DOC-UNSUPPORTED-PERSISTENCE` rather
than claiming atomicity. Before writing native handle code, execute focused
SDK/system-call spikes for macOS and Windows path traversal, no-replace,
handle-relative identity and replace semantics; an absent target-platform
primitive leaves that adapter unsupported rather than guessed. The
[architecture's normal-path event contract](../architecture/application.md)
requires `language.parse`, `geometry.validate`, `geometry.preview`,
`document.apply`, `document.save` and `document.reopen` with operation ID,
source byte count, generation, evaluator, duration, status and stable code.
These events emit by default and make latency, volume and failure outcomes
measurable; missing measurements say Not recorded. The B0 retention contract is
a local 256-event session ring discarded at close, with no persistent MRU or
exporter. Source, path, names, vertex, raw exception and hash-correlation data
do not leak into telemetry. Core stays free of Avalonia/UI and network
dependencies.

The author provides red-before-green controls for normal, invalid, boundary
and fault paths; exact test names, runtime identities, source fingerprints,
build/test receipts and a clean descendant commit. `tools/verify-application-core.py`
must exercise its named behavior and remain a real nonempty gate on both target
platforms. Target-platform evidence is separated: a macOS local pass does not
establish Windows runtime behavior. The Coordinator recounts after join and
runs `tools/check-docs.py` plus the repository verification runner without a
muted exit. Owner/root independently review Data, Test, Geometry, Security and
API consistency before any C adapter launch or join. No B code is integrated
on a partial pass; a failed branch remains isolated with its receipt. This
track alone is an internal core increment, not a runnable M1 delivery or a CLI
proof.

Independent geometry and identity oracles must invert each authored channel's
abscissa before assigning spline basis weights at a physical station. For the
twist-collision review fixture, `eta=0.203125` is reached at first-span local
`u=1/2`, not `u=eta/0.5`; the CV2 weight is `3/8`. A reviewer computation that
uses the station as the parameter cannot clear a geometry or Surface-identity
veto even if its qualitative result happens to agree.

The selected application stack also needs a separate root-owned project-doc
reconciliation of the `AGENTS.md` preamble and specification's still-open
stack wording after Ruling 13 accepts ADRs 0003/0004 for the bounded direction.
B owns none of those files; this
documentation seam cannot be hidden in a production code review.
