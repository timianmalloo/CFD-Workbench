---
id: adr-application-project-contract
title: Native-v1 immutable receipts and bounded admission
type: adr
status: accepted
owner: "@cfd-owner-20260923"
phase: design
tags: [adr, persistence, contracts, identity]
links:
  - {to: adr-application-stack, rel: refines}
  - {to: design-application-contracts, rel: documents}
  - {to: proof-application-contracts, rel: tested-by}
review-by: 2026-12-23
summary: >-
  Records Owner-approved unshipped native-v1 policy for durable rail edit receipts, bounded immutable history,
  exact numeric resource admission and fail-closed platform persistence. Independent executable-contract
  acceptance remains separate from these design-policy rulings.
review-suggested:
  - { by: design-application-contracts, on: 2026-09-23, reason: "Serial contract completion adds durable edit receipts, bounded writer-reader admission and explicit typed session/store seams." }
  - { by: adr-application-stack, on: 2026-09-23, reason: "ADR 0003 accepted under Owner Ruling 13; reconcile decision references while retaining unverified product and platform gates." }
---

# ADR 0004: native project contract completion

**Accepted as an unshipped contract, 2026-09-23.** Rulings 9 and 10 establish the design policy;
Ruling 12 accepts the bounded executable contract gate following independent review.
[Ruling 13](../notes/rulings.md#ruling-13--g3-serial-first-core-implementation-freeze) authorizes
serial core implementation after worker preflight. A supported native overwrite adapter,
production geometry and application acceptance still require their own observed proof.

Native-v1 retains exact UTF-8 source snapshots, immutable Design/accepted facts and append-only cursor facts.
Add required `accepted.edit`: null only initial Open; every numeric rail Apply stores draft ID, generation,
rail and vertex ID. Combined with parent/source/design/evaluator/Surface references, this reconstructs its
durable retry binding. Same-Surface/source-only rail results still retain the receipt. Generic source editing
requires a future discriminated receipt contract and is not admitted here. This is the smallest addition that
makes target-sensitive operation identity durable; an in-memory registry alone fails after reopen.

Persisted Open/Apply/Undo/Redo IDs never expire or get reused for another payload/action. Root Undo and empty
Redo append nothing; their session-local retry IDs expire on reopen. No-op lifetime is explicit, not an
exactly-once promise for an event that was never persisted. Replay derives active and redo state; no second
active pointer or editable parsed geometry representation exists.

The exact prospective serialized image must satisfy 8,000,000 bytes, 4096-byte native lines and every source's
1,048,576 decoded-byte cap **before** mutation. Full immutable history is not silently pruned. Refusal retains
draft/dirty/facts/file; Save As is not a size escape. Recovery replacement follows the same admission rule.
Long legal FoilDSL IDs may be unrepresentable in this bounded native receipt; refuse transaction/recovery with
`DOC-SIZE`, preserving work, rather than writing a file the reader rejects.

Decimal work is bounded before BigInteger/powers: 4096-byte tokens and normalized nonzero decimal magnitude
[-400,400], compensated spellings allowed, all-zero huge exponents shortcut to zero. Exact unit scaling then
one nearest/ties-even binary64 rounding includes subnormal/underflow cases. Resource refusal is unsupported
admission, not invalid language. The alternative of capping raw exponent was rejected because compensated
finite spellings and zero are valid under the language's exact-number contract.

Missing curve IDs are deterministically inserted in a candidate before the final diff and explicit acceptance.
Original bytes are retained unmodified at the import boundary. No source rewrite happens after acceptance.

New-file publication requires atomic no-replace, preserving a competing creator. Cooperative overwrite
requires verified handle-relative no-follow, selected-directory identity and platform conflict/fault evidence.
Without those capabilities the adapter returns `DOC-UNSUPPORTED-PERSISTENCE`; no unsafe fallback exists.
Hash-check-then-replace is not CAS against arbitrary noncooperating writers. Power-loss durability is a distinct
reported outcome and remains unproven. OS-user ACL/cooperative-writer posture is accepted; same-user malicious
process exposure remains explicit.

No released format is migrated. Reversibility: amend this unshipped contract before implementation; future
format changes require versioned forward/backward readers, preserved originals and tested rollback.
