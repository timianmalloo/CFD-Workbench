---
id: note-discrepancy-records
title: Discrepancy records in the analysis member of cfdw-project-2
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [analysis, persistence, compare, discrepancy-record, adr-0011]
links:
  - {to: adr-0011-analysis-run-storage, rel: refines}
  - {to: design-a3d-compare, rel: relates-to}
  - {to: spec-amendments-1-7-7, rel: relates-to}
  - {to: rulings, rel: depends-on}
review-by: 2027-01-31
summary: The A3d Compare slice adds an optional discrepancies member to the analysis object of cfdw-project-2, one record per unordered pair of Completed runs in canonical orientation; it refines ADR-0011 (expand only, older readers refuse the file, referenced runs are never pruned) without editing it.
---

# Discrepancy records in the `analysis` member

**Decision.** The `analysis` object of a `cfdw-project-2` document (ADR-0011) gains one optional member,
`discrepancies`: an array of Discrepancy records, written only when at least one exists, so a document without one
keeps its bytes and every hash. ADR-0011 stays as accepted; this note records the addition beside it (DM13). Sources:
Ruling 250 (Q1 to Q5), Ruling 251 (DR-A3D-6), the Data & Persistence confirmation of cycle 2, and the design
[docs/design/a3d-compare.md](../design/a3d-compare.md) §1.

**Grain and key.** One Discrepancy record is exactly one comparison of one unordered pair of distinct Completed
Analysis runs, stored as a first (base) and a second (compared) run in canonical orientation — lower tier first, else
earlier accepted revision first, else earlier recorded run first — under one compare-rule version. It holds one line
per compared quantity (CL, CDi, e in rule `cfdw.compare` 1.0.0). Key = (baseRunId, comparedRunId, rule id, rule
version) after orientation; re-comparing either order is a read. A blocked pair stores no record. ρ and the dimensional
loads are derived, never stored ([spec note 1.7.7](../specs/amendments/spec-1.7.7.md)). Orientation and key are
defined once, in Core.

**Store invariants** (checked by the reader and on record, same codes): id UUID and unique (`DOC-DISCREPANCY-ID`);
key unique (`DOC-DISCREPANCY-KEY`); base ≠ compared and canonical orientation (`DOC-SCHEMA`); both ids name a Completed
run present in `runs`, never a tombstone (`DOC-REFERENCE`); content hash hex64 (`DOC-INTEGRITY`); numbers finite,
line codes from the closed set, lines in rule order (`DOC-SCHEMA`). A record that fails its hash, does not reproduce
from its runs, or names an unknown rule version stays in the document and reads Unavailable.

**Pinning.** Retention never prunes or tombstones a run a record references, and records are never pruned; there is no
unpin path. Ruling 251 settles DR-A3D-6 as accept and measure: no cap, record pruning refused as a contract change, an explicit Record action adopted only by the operator after the trigger fires; every save emits the pinned-run count and the document byte size;
the revisit trigger is the first `pinnedRuns` reading above 20 or the first `DOC-SIZE` refusal in a document that
holds records.

**Migration.** Expand only: the reader accepts the member or its absence; no backfill (no record existed before A3d);
no contract step. The format string stays `cfdw-project-2`.

**Older readers.** A build before A3d reads the `analysis` object with an exact member list
(`AuthoringSession.cs` line 2254 at `176c9540`) and refuses any other member with `DOC-UNSUPPORTED-FIELD`, so it refuses
a document with records rather than dropping them on save.
