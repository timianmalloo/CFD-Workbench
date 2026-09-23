---
id: coordination-contract-b-core
title: Proposed first production core author assignment
type: plan
status: proposed
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, core, implementation]
links:
  - {to: coordination-application-build, rel: depends-on}
  - {to: coordination-contract-b0, rel: depends-on}
  - {to: design-application-foundation, rel: depends-on}
review-by: 2026-10-23
summary: Conditional exact-path production core packet to freeze only after B0 executable contract review and the Owner's G3 technical and harness-routing rulings.
---

# B · first production core packet (draft, not dispatched)

**Admission gate:** serial B0 must hand back a committed, independently reviewed
session/schema/identity/persistence contract and executable fixtures. Owner must
then issue the G3 technical freeze. Ruling 11 conditionally selects one serial
built-in Codex core author under equal observed-only controls, but clears no
dispatch: the harmless same-harness cancellation/quiescence drill and exact
identity/path checks remain. This packet is preparation, not authorization to
write production code. No further Grok mode/probe loop is proposed.

| Assignment field | Provisional value; freeze at G3 |
|---|---|
| Session, branch and worktree | Not assigned. Create a fresh `feature/application-core` tree through `coord worktree new`; record exact session, branch, absolute path and base commit before the first author command. |
| Author profile | Requested `gpt-6-astra`; effective model `Not recorded` only under Ruling 11's first-core exception. The candidate must pass actual same-harness cancellation and subprocess-quiescence observation. |
| First command | `audit-log.py start --session <assigned> --skill implement` from assigned worktree; read full `implement` contract and B0 design. |
| Initial budget and checkpoints | 90 tool calls, ≤55 minutes, ≤100k context for first run. This is a checkpoint estimate, not a claim that the broad core will be done in that window. Checkpoints after red boundary fixtures, compiling core API, and store fault tests. At cap, report/replan without claiming completion. No author subagents. |
| Handback | Exact tracked/untracked path inventory, cwd/base/final HEAD, dependency and test receipts, proof pack, clean descendant commit. Root/Owner independent code and behavior review before join. |

## Provisional exact ownership

These are the [foundation design](../design/application-foundation.md)'s path
proposals expanded into **18 candidate exact leases**: its 16 source/build/test
paths, one authored proof pack and one argument-free project gate. G3 freezes
names against the final B0 public namespace and fixture and links the joined
`design-application-contracts` and ADR 0004 artifacts. The worker may not
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
Native persistence spike source paths are not yet leased. G3 must either add
their exact source paths and review the ownership count, or identify an
executable in-proof reproducer wholly inside these leases; the worker cannot
create an unleased `tools/spikes/` file to satisfy this gate.

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
measurable; missing measurements say Not recorded. Source, path, names and
draft content do not leak into local-only telemetry. Core stays free of
Avalonia/UI and network dependencies. At G3, refine retention/minimization from
the joined B0 security/privacy design; this draft does not invent a log sink.

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

The selected application stack also needs a separate root-owned project-doc
reconciliation of the `AGENTS.md` preamble and specification's still-open
stack wording after the approved ADR joins. B owns none of those files; this
documentation seam cannot be hidden in a production code review.
