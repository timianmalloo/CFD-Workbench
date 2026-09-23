---
id: review-application-contracts
title: Independent review of the application session contracts
type: proof-pack
status: in-review
owner: "@cfd-application-20260923"
tags: [application, contracts, independent-review, persistence, identity]
links:
  - {to: coordination-contract-b0, rel: documents}
  - {to: architecture-application, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: depends-on}
review-by: 2026-10-23
summary: Independent review of the serial B0 contract fixture, its durable identity and session boundaries, with explicit limits on what fixture evidence establishes.
review-suggested:
  - { by: architecture-application, on: 2026-09-23, reason: "Owner Ruling 13 accepts conditional native M1 architecture and serial core implementation; product proof gates remain open." }
---

# Independent application contract review

Root review session: `cfd-application-20260923`, 23 September 2026. The lead is
independent of B0 author `cfd-contracts-author-20260923` and technical Owner
`cfd-owner-20260923`. Data & Persistence, Test Architect, Computational Geometry,
Security, UX and Simplifier are review lenses, not separately claimed model runs.

**Disposition: PASS for bounded B0 contract completion.** Reviewed source commit
`cb73079e041668221618b8dfc08753ff00b1d9f6`. This clears root's independent
session/schema/identity review, not the Owner's G3 decision, harness qualification,
production implementation or an application milestone. The fixture's allowlisted
certificate authority is deliberately not a geometry evaluator.

## Review basis and boundaries

The [B0 packet](../coordination/contract-b0.md) defines the eight possible authored
paths, session operations, native schema, decimal identity, persistence model and
required handback. FoilDSL sections 3, 7 and 8 govern numeric semantics, validation
and canonical identity. CAD-11 and UX-23 govern draft ownership during inspection.
Architecture Rulings 8 and 9 retain a serial contract gate before production.

Review crosses source bytes → parsed definition → validation certificate → owned
session and revision facts → envelope → reopen → adapter snapshot. A native file
hash proves byte integrity; it does not prove geometry admission. An externally
returned snapshot must not provide a second writer into session-owned state.

## Findings

Initial findings came from direct source inspection before the author's first
reported clean build. Root did not execute or fingerprint that early candidate.
The final dispositions below are **Verified within the named fixture scope**:
root inspected the repairs, rebuilt the committed source independently and ran
the final regressions. Root also independently executed five constructed mutants.
Those mutants are not preserved historical source or historical pre-fix execution.

| ID | Finding and consequence | Required evidence | Disposition |
|---|---|---|---|
| BC-01 | Decimal conversion bounded the written exponent before handling zero or compensating significand digits. | Normalize with bounded scanning; preserve exact units, ties-to-even, zero/subnormals and distinct resource refusal. | Resolved: compensated exponent, zero-huge-exponent, subnormal, overflow and deadline cases pass; Ruling 9 records policy. |
| BC-02 | Outward records exposed session-owned mutable chunk arrays. | Mutation of returned nested arrays cannot alter source/recovery/history; save requests also own their bytes. | Resolved: `RecoveryAndEnvelope_CallerMutation_PreservesOwnedBytes` and `SaveRequest_AsyncBoundary_CapturesDefensiveImage` pass. Copy-removal mutant fails. |
| BC-03 | Open/Reopen lost persisted operation retry bindings. | Same-payload retry returns its original result without cursor movement; changed payload/action refuses. | Resolved: durable Open/Apply/Undo replay and changed draft/target/action cases pass. Reconstruction-removal mutant fails. Ruling 10 explicitly limits no-op deduplication to the session. |
| BC-04 | Open/Reopen admission could be bypassed or use an unrelated certificate. | Exact source/evaluator/identity binding before adoption. | Resolved: unknown and unrelated authority refusals pass; removing the binding guard fails. Undo/Redo target revalidation is also present. |
| BC-05 | A legal long ID can exceed the native encoded line limit in a receipt/recovery row. | Writer and reader use the same bound; refusal precedes state mutation. | Resolved: escaped 4096-scalar ID Apply/recovery refusals preserve history/draft; the 7930-byte positive envelope round-trips with every line within limit. |
| BC-06 | Disk formatting could make a just-opened document dirty. | Separate exact disk conflict hash from normalized session-image identity. | Resolved: differently formatted valid JSON opens clean; late acknowledgement leaves a newer revision dirty. |
| BC-07 · Owner finding | First-cursor Apply could pass replay and then dereference a missing receipt. | Require initial Open and refuse malformed input before adoption. | Resolved: three native/reopen/no-adoption cases pass; initial-Open guard-removal mutant fails. |

Owner also required the decimal converter's direct entry point to use ASCII
digits. Its original `\d` regular expression accepted Unicode digit categories
outside FoilDSL before a lower-level integer conversion failed. The lexical
refusal passes; the Unicode-digit mutant fails with the expected FormatException.
Owner also found truncated `half_span` input could exhaust token access; guarded
Peek/Take now produce stable refusal and leave the session empty. At the persistence seam, root required
`SaveRequest` to own a defensive byte snapshot and derive create-only behavior
from an absent expected disk hash, avoiding two competing mode fields.

The target-retarget concern from the earlier architecture sketch is also in the
B0 packet: the session owns its draft target. The competing-begin and external
retarget/mutation cases pass. This establishes the session boundary, not the
future native selection/focus behavior.

## Evidence and unresolved gates

Root copied the source into an isolated temporary directory, built it with .NET
10.0.203 (zero warnings/errors) and independently ran the Python oracle against
that new binary on macOS ARM64: **89 C# checks, 42 Python checks and 2505
deterministic cross-runtime vectors passed**. The oracle measured 0.259 seconds;
that is fixture execution time, not an application performance claim. Root read
the complete design, ADR 0004, proof, security/privacy refinements and focal code.

| Source | Independently built SHA-256 |
|---|---|
| `ApplicationContracts.csproj` | `4a67878b7b5d5a9512f836a4ccd38d6c55697f1b438db532013ac318ba2f2f62` |
| `Program.cs` | `95ec719b8e17fac4f7e33e32d4f8d7e910063fcec52f5cabf9b26937709e5b4f` |
| `application-session-contract-vectors.py` | `595556833579d94beff37ec2d1df19bcd1aba25ce8f4769249b6cc367d427942` |

The raw local receipt is `/tmp/cfd-contract-review.SIdno9/final-receipt.json`,
SHA-256 `796be000f955f8a9308ef4d1ed18cf7cad2bf353e29188ddfd6464a1383c3e48`.
Root separately built and executed all five constructed mutants. Each compiled
successfully, then failed at the expected runtime boundary: initial-Open replay,
ASCII digits, recovery copy, durable retry reconstruction and authority binding.
The raw local mutant receipt is `/tmp/cfd-contract-mutants.VEQJ5C/mutation-results.json`,
SHA-256 `cbbdf33b86adf7c12010d5cc423b0115a5c1c2b2e01b1082c31c2cc286c5976c`.
Temporary raw receipts are local evidence; these recorded results and the author's
committed proof contain the durable disposition and reproducible source basis.

Root inspected the 12 committed paths: eight authored artifacts and four official
metadata outputs, matching B0 ownership. The author reported an advisory commit
hook because AGENT_SESSION was not exported; there is no retrospective enforcement
claim. Coordinator verifies committed path decisions and records COORD-ENV recurrence.

Data & Persistence, Test, Geometry, Security and Simplifier lenses accept this
bounded seam. No additional datastore, transport or generic source-edit abstraction
is justified. The receipt and defensive copies preserve existing invariants; their
removal fails named tests. UI/Accessibility acceptance remains a product gate.

The fixture does **not** establish full FoilDSL parsing, continuous geometry
certification, native handle/race safety, telemetry privacy, rendered UI behavior,
or Windows runtime compatibility. Its Python persistence models are distinct from
the two live local no-replace primitive cases. Production must replace the fixture
authority with verified geometry, implement full grammar/diagnostics, spike each
native store capability, and exercise the actual GUI/CLI save/reopen workflow.
The first core track remains subject to Owner G3 and Ruling 11's separate observed
cancellation, worktree identity, ownership and model-evidence conditions.

The audit timing marker for this review artifact began at `2026-09-23T14:42:23Z`,
after preliminary read-only review. Its measured duration therefore covers the
artifact/final-review phase, not all earlier review work or the overall task.
