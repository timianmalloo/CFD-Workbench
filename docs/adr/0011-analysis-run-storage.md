---
id: adr-0011-analysis-run-storage
title: "ADR-0011: analysis run storage — runs are append-only facts in the native project as cfdw-project-2, written only when a run exists"
type: adr
status: accepted
owner: "@timianmalloo"
phase: implementation — A3a PRE (Area 3 Analysis, Ruling 63)
tags: [analysis, persistence, native-format, run-key, retention, migration, adr, a3a, dr-ana-4]
links:
  - { to: design-area3-analysis, rel: implements }
  - { to: adr-application-project-contract, rel: refines }
  - { to: spec-amendments-1-7, rel: implements }
  - { to: rulings, rel: depends-on }
review-by: "none while accepted"
summary: >-
  Analysis runs are stored in the native project file as append-only rows under a new optional top-level
  `analysis` member. The format string is derived from the run count, so a project with no run is still written as
  `cfdw-project-1`, byte for byte as today; one run makes it `cfdw-project-2`. Store invariants are checked on read and
  in RecordRun; each run carries a content hash and its key is recomputed, never trusted. The first save to `-2` writes
  a `.v1.bak` first. Retention keeps every run reachable from a retained revision or the redo stack plus the latest
  20 others per tier, and leaves a tombstone for each pruned run.
review-suggested: []
---

# ADR-0011: analysis run storage

- **Status:** Accepted (Ruling 63, operator, 2026-10-03: DR-ANA-4 option (a) as recommended). Written 2026-10-04 by the
  A3a PRE track from `docs/design/area3-analysis.md` §3.3, §3.6, §15 (DR-ANA-4) and spec 1.7 AM-1.7-19.
- **Deciders:** the operator; the Data & Persistence Architect lens (hard veto on the migration); the design's data,
  test and CFD lenses (design §17).
- **Scope:** where and how an Analysis run is stored, what is checked, how an older build reacts, and what is pruned.
  The run key itself is defined once in design §3.4 (spec A3.1 Run-manifest row) and is cited here, not restated.

## Context

- An Analysis run must survive save and reopen and stay attributable to the exact inputs (ANA-07). A session-only
  store fails ANA-07; a sidecar file splits one document into two that can drift apart.
- The native project is `cfdw-project-1` today (`src/CfdWorkbench.Core/AuthoringSession.cs`: `Envelope`, `NativeProject`).
  Its reader refuses any other format string with `DOC-VERSION` (`AuthoringSession.cs`:1703), which the Desktop shows
  as COPY-130 ("A newer version of CFD Workbench may open it") without changing the file (`OpenOutcome.cs`:95).
- `NativeProject` lives in Core, and Core cannot reference `CfdWorkbench.Analysis`. So the stored row, its canonical
  form, the run key and the content hash live in Core (`src/CfdWorkbench.Core/RunRecord.cs`); Analysis computes and
  projects (design G-T8, P-3).
- The store publishes by write-to-temp, `fsync`, then `renameat` over the target (`src/CfdWorkbench.Persistence/ProjectStore.cs`
  :112–137, read 2026-10-04): the replace is atomic on macOS. This closes the design §3.6 `assume:` for macOS;
  Windows parity is not verified here.

## Decision

1. **Representation.** Runs are append-only facts in the native project, under one new optional top-level member
   `analysis: { runs: [...], polarSamples: [...], pruned: [...] }`. It is written only when at least one run exists
   (`WhenWritingNull`). The format string is derived by **one function** from the run count: no run → `cfdw-project-1`,
   byte-identical to today's writer; one or more → `cfdw-project-2`. Nothing else in the document changes.
2. **Grain (AM-1.7-19).** One run row is one evaluation attempt of one Surface revision (with its Profile revisions) at
   one Operating point by one method id + version under one settings hash, recorded when the method returns, with its
   outcome: Completed, or Failed (code, reason). Cancelled is telemetry, never a row. One strip row is spanwise lattice
   row j of one Completed run, identified by (runId, j). Every attribute is immutable; a change is a new row.
3. **Store invariants**, checked in `NativeProject.Check` on read and in `AuthoringSession.RecordRun` under the session
   lock (check and append in one step): `runId` unique; at most one Completed row per run key (Failed rows may repeat);
   strips contiguous 0…n−1 with n ≤ 2,048; polar grain key unique; every hash `[0-9a-f]{64}`; every number finite; each
   line ≤ 4,096 bytes; a document-size preflight (`DOC-SIZE`) before save.
4. **Integrity per run.** Each run carries `contentHash` = BLAKE3(JCS(the row without `contentHash`)). On read, a
   mismatch makes **that run** "Unavailable — run payload failed its check": never Current, never deleted. The stored
   `runKey` is recomputed from the manifest and never trusted; a stored key that disagrees is the same Unavailable.
5. **Expand–migrate–contract.** Expand: the reader accepts `-1` and `-2`. Migrate: none (a `-1` document has no runs;
   no backfill, DM16). Contract: none; `-1` stays readable and writable indefinitely.
6. **Rollback.** An older build opening a `-2` file fails closed with COPY-130 before it can write. The first save
   that turns a `-1` file into `-2` first writes `<name>.v1.bak` (flushed to disk), never overwrites an existing
   `.bak`, then publishes through the store's existing atomic save path. Today's reader opens the `.bak` and it
   round-trips byte-equal.
7. **Retention (at save).** Keep every run whose key is reachable from any retained accepted revision or from the redo
   stack; plus the latest 20 other runs per tier; plus the latest Failed row per key. Never prune a run that a
   Discrepancy record references (A3d). Prune the rest at save, list them in the save report, and leave a tombstone
   fact (runId, runKey, prunedAt) in `pruned`, so an Undo that reaches a pruned run reads "pruned", not "missing".
8. **Writer and readers (DM15).** One writer: the Analysis service through `AuthoringSession.RecordRun` (append only,
   not on the undo stack, marks the document dirty, refused after close with `DOC-CLOSED`). Readers: freshness (the
   recomputed key), `AnalysisProjection`, Compare (A3d) and CLI `inspect --runs`.
9. **Type-1 by decision (not in the key, not history).** Display units, the named attachment point, layer visibility,
   chart choice and the reconciliation tolerance. The tolerance is recorded in the manifest, outside the key.

## Consequences

- A file saved with no run is unchanged for every existing reader, tool and recount (`recount-application-contracts.py`
  stays exit 0 because the `-1` writer is byte-identical).
- A file with a run cannot be opened by a build older than A3a; the `.v1.bak` is the user's way back, and COPY-130 says
  so without touching the file.
- Document size grows with runs; the strip cap, retention and the `DOC-SIZE` preflight bound it
  (`Store_SizeAtStripCap_UnderDocLimit`, `Store_HundredThousandStrips_RefusedDocSize`).
- Freshness needs no stored flag: it is key equality against the current inputs (design §3.4).

## Alternatives rejected

- **(b) A sidecar file** beside the project: two files that can be moved, copied or restored apart, and a second
  durability path to prove.
- **(c) Session-only runs:** ANA-07 (reopen keeps the result and its provenance) is unmet.

## Verification (owned by the STO track; names from design §18.8)

`RunKey_PinnedVector_HexEqual`, `Project2_RoundTrip_ByteEqual`, `Project1_NoRun_ByteIdenticalToToday`,
`Project2_TodaysReader_FailsClosedCopy130` (with the old-build receipt `docs/proof/a3a-old-build/`),
`Backup_V1Bak_TodayReaderByteEqual`, `RecordRun_SameRunIdTwice_Refused`, `RecordRun_SameKeyTwice_OneCompletedRow`,
`RecordRun_StripGap_Refused`, `Tamper_EditedStripValue_RunUnavailable`, `Tamper_StoredKeySetToCurrent_RunUnavailable`,
`Retention_PruneThenUndo_TombstoneReadsPruned`, `Store_SizeAtStripCap_UnderDocLimit`,
`Store_HundredThousandStrips_RefusedDocSize`. Each has its mutant in design §18.8.
