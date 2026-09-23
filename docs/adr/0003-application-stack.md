---
id: adr-application-stack
title: Native modular monolith and source-snapshot persistence for M1
type: adr
status: proposed
owner: "@cfd-owner-20260923"
phase: architecture
tags: [adr, native, stack, persistence]
links:
  - {to: architecture-application, rel: documents}
  - {to: adr-foildsl-authority, rel: refines}
  - {to: spec-foildsl, rel: implements}
  - {to: proof-application-spikes, rel: depends-on}
review-by: 2026-12-23
summary: >-
  Proposes C#/.NET with Avalonia for the offline native milestone after actual candidate spikes.
  Retains lossless source snapshots and append-only project facts without a database or editable AST shadow;
  Owner approval and named cross-platform/numerical/persistence gates remain required.
---

# ADR 0003: native offline composition

**Proposed; Owner ruling pending.** This is a proposal, not selection through mockup inheritance.

## Decision requested

1. Select a native modular monolith with C#/.NET 10 and Avalonia 11.3.14 for the first offline editor,
   with one UI-free deterministic core consumed by native GUI and CLI. Pin .NET SDK 10.0.203 for the
   observed spike; release support/security updates require explicit maintenance review. Do not float packages.
2. Retain ADR-0002 source snapshot history in a versioned `.cfdw.json` envelope, no database, no second writable
   AST. Store exact source bytes, immutable accepted-revision references and append-only cursor facts; store
   invalid recovery separately. Parsed geometry is a derived projection.
3. Admit only the conservative certificate subset in architecture §5 for M1. Valid-but-unproved definitions
   return Not assessed/Unsupported and cannot Apply; no weakening to sampled validation.
4. Treat native files as cooperative single-writer documents, with hash conflict detection and explicit
   Save As for external conflicts. Make any guarantee against arbitrary uncooperative writers a separate
   required OS-specific contract before shipping overwrite. Do not claim hash-check-then-replace is CAS.

These are load-bearing decisions submitted together because serialization, GUI state and accepted geometry
share one invariant. The Owner may split/amend them. No simulation backend or export geometry kernel is selected.

## Evidence before recommendation

SDK 10.0.203 and osx-arm64 runtime observed. Avalonia.Desktop/Fluent 11.3.14 and Blake3 2.2.1 restored and
compiled with zero warnings. The self-contained macOS app launched and rendered its custom viewport.
Native AX exposed the numeric field and Open button; a real native file sheet opened and Cancel retained
the input/focus. Root independently repeated input/Tab/Return/Escape. The viewport's generic AX role is
**unknown**, so a custom semantic peer and station/value child controls are a product gate, not a passed
accessibility requirement. See [evidence and sources](../proof/application-spikes.md).

The stack recommendation is strongest on one-language identity/transaction ownership and actual native
window/file integration. It is weakest on the as-yet-unproven production evaluator, native accessibility
breadth and Windows live behavior. Package availability and compiler success do not establish these.

## Alternatives

WebView + IPC could reuse HTML skills, but adds another precision/serialization/AX boundary and its actual
contract was not exercised. Native + local worker buys failure isolation but adds transport/lifecycle before
a measured need. A database buys indexes beyond the bounded document but introduces schema/locking and a
portable-project export path; neither is needed for M1. Independently persisted AST is rejected outright.

The package choice is deliberately bounded to a spiked stable 11.x API rather than asserting compatibility
with the newer 12.x search result. Upgrading is a new version-sensitive spike, not a hidden incidental change.
Blake3.NET's BSD-2-Clause package is a candidate native hashing adapter; Python libraries are development
oracles only. Avalonia and Fluent are MIT. Resolved transitive notices, SBOM and native library provenance
must be pinned before distribution; the spike package manifest is not a complete license audit.

## Consequences and reversibility

Desktop UI stays outside the core; a future UI replacement retains language, project and CLI contracts.
Source-preserving parser and JCS need exact tests; generic JSON or `.ToString("R")` is insufficient.
Source snapshot growth is bounded by the 8 MB M1 envelope; Save overflow retains the dirty session and last
complete saved file. Save As of the same oversized envelope cannot resolve the cap. No history is pruned.
Explicit shape-only source export, if implemented and clearly labelled, or a future expanded format are
recovery options. The serial core design gate must decide pre-Apply growth refusal versus a dirty Save
failure. Future format expansion preserves original files and has rollback tests.
No branch is allowed to change the same aggregate through another authority. Local metadata telemetry has
no egress. Rendering and validation have separate statuses and budgets.

## Owner seams and gate

- Amend AGENTS.md's stack-unselected line only after selection; simulation backend remains unselected.
- Reconcile product A4.12/A8.4/A8.5 older proposed kernel/library wording with the selected M1 scope without
  silently deleting future manufacturing/numerical obligations. This author does not own those files.
- Rule external-writer semantics, envelope line-cap interpretation and conservative certificate admission.
- Approve public contract readiness or require serial B→C; a compiling type sketch is not a completed core.
- Owner Ruling 5 extends the author's lease to the two security/privacy rollup files. The author writes
  their dispositions from the design tables; Coordinator only regenerates derived surfaces at join.

Independent Data, Geometry, Security, Test and UX review must record its own disposition. Author verdict:
**proposal ready for review; implementation gate not self-cleared**.
