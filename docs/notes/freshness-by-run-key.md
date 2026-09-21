---
id: decision-freshness-by-run-key
title: Freshness is derived by run-key equality, never written
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [analysis, provenance, data-model]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: kb-hydrofoil-workbench, rel: depends-on}
  - {to: review-spec-v02-critique, rel: relates-to}
review-by: 2027-03-19
summary: A run is Current when the BLAKE3 key over its canonical inputs, method id + version and settings hash equals the key recomputed from the current design; nothing ever writes a freshness flag.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-20, reason: "Specification v1 (build basis) written and gated 2026-09-20; supersedes revision 0.2 — re-read against the new contracts (identity oracle, run key, C2 state table)." }
---

# Freshness is derived by run-key equality, never written

**Decision.** An Analysis run is **Current** iff its run key — BLAKE3 over (Surface revision hash, referenced
Profile revision hashes, Water record, Operating point, method id + version, settings hash) — equals the key
recomputed from the current Design revision and settings; otherwise it is **Historical** (spec v1 A3.1 Run
manifest, A3.4, ANA-07). No flag is stored; Apply touches one aggregate and invalidation is a read.

**Why.** A written "stale" flag is a second definition of one quantity (DM7) and the classic source of the
"persuasive chart that outlived its shape" defect (A1). Deriving freshness from content hashes makes Undo to an
identical definition restore Currency for free, makes save-and-reopen inert, and makes a method-version or a
settings change (lattice density, TE floor) invalidate without anyone remembering to. The v1 gate found the key
defined twice with the settings hash in neither — the fix was one definition in one row, cited everywhere.

**Rejected.** Dirty flags on runs; timestamps ("newer than") — a clock is not a proof of identity; a dependency
graph maintained by hand.

**What it constrains.** The Run manifest field list (settings hash and reconciliation tolerance are members);
the CLI (GUI and CLI must print the same key — CLI-01); the boundary set (a settings change is a listed input);
the comparison view (a Historical run may be compared but is banded as such — UI-06).

**Confidence.** Verified as a design property (content-hash equality is decidable); the canonical form (A4.11,
RFC 8785) and the BLAKE3 binding are `/define-architecture` commitments to confirm on both platforms. Owner
lenses: Data & Persistence Architect · Test Architect.
