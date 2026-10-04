---
id: proof-a3a-sto-red-first
title: "A3a STO red-first receipt"
type: proof-pack
status: active
owner: "@track-a3a-sto"
phase: implementation
tags: [a3a, sto, analysis, persistence, run-key, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: implements }
  - { to: proof-a3a-old-build, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  Each STO check of design §18.8 observed red against its named mutant (15 red lines over 14 checks), including the
  planted mutant of the STO exit: the content hash not checked on read turns both tamper checks red.
---

# A3a STO red-first receipt

Design `docs/design/area3-analysis.md` §18.2 (STO row), §18.8; ADR-0011. Session `track-sto`, branch
`feature/a3a-sto`, 2026-10-04. Each mutant was planted in the committed tree at `549a693`, the owning project rebuilt,
the check selected with `CFD_TEST_ONLY`, and the file restored (`git status` clean after the run). The pinned
run-key check was also red before its values were pinned (placeholder hex), at `1d9feb3`'s parent.

| Check | Ring | Mutant | Red line |
|---|---|---|---|
| `RunKey_PinnedVector_HexEqual` | A | two JCS members swapped (`speed` ↔ `alphaDeg` values) | `run key expected cfad1f85…5808; actual 9d043855…5963` |
| `Project2_RoundTrip_ByteEqual` | A | strips written in reverse j order | `ContractError: DOC-RUN-STRIPS` |
| `Project1_NoRun_ByteIdenticalToToday` | A | format string hard-coded `-2` | `format expected cfdw-project-1; actual cfdw-project-2` |
| `Project2_TodaysReader_FailsClosedCopy130` | A + receipt | the reader accepts any `cfdw-project-*` | `refusal code expected DOC-VERSION; actual DOC-SCHEMA` |
| `RecordRun_SameRunIdTwice_Refused` | A | the `runId` uniqueness check removed | `expected refusal DOC-RUN-ID; none` |
| `RecordRun_SameKeyTwice_OneCompletedRow` | A | the per-key Completed check removed | `expected refusal DOC-RUN-KEY; none` |
| `RecordRun_StripGap_Refused` | A | the contiguity check removed | `expected refusal DOC-RUN-STRIPS; none` |
| `Tamper_EditedStripValue_RunUnavailable` | A | **planted:** the content hash not checked on read | `edited run expected PayloadFailedCheck; actual Intact` |
| `Tamper_StoredKeySetToCurrent_RunUnavailable` | A | **planted:** the content hash not checked on read | `forged key expected PayloadFailedCheck; actual Intact` |
| `Tamper_StoredKeySetToCurrent_RunUnavailable` | A | the stored key trusted (key not recomputed) | `forged key expected PayloadFailedCheck; actual Intact` |
| `Retention_PruneThenUndo_TombstoneReadsPruned` | A | prune without a tombstone | `Sequence contains no elements` (no tombstone) |
| `RevisionLabel_TwistEdit_OrdinalsAndRail` | A | the rail read from the first accepted row | `leading edit expected … Rail = leading; actual … Rail = ` |
| `Backup_V1Bak_TodayReaderByteEqual` | C0 store | an existing `.bak` overwritten (rename in place of link-no-replace) | `Expected True; actual False` |
| `Store_SizeAtStripCap_UnderDocLimit` | C0 store | the strip cap raised to 4,096 | `Expected refusal DOC-RUN-STRIPS` |
| `Store_HundredThousandStrips_RefusedDocSize` | R (Core) | the `DOC-SIZE` preflight removed | `Expected refusal DOC-SIZE` |

**Measured sizes.** One run at the 2,048-strip cap: 1,870,097 bytes of the 8,000,000-byte document limit (23 %).
10⁵ strips: 90,168,220 bytes, refused `DOC-SIZE` by the preflight, the reader and the store read.

**Measured costs (COST lines, ms, Debug, this machine).** RunKey 31 · RoundTrip 186 · NoRun 134 · ForwardGuard 107 ·
SameRunId 55 · SameKey 64 · StripGap 50 · TamperStrip 76 · TamperKey 82 · Retention 452 · RevisionLabel 191. The design
estimated < 5 ms to < 50 ms (Inferred); the measured floor is a session Open with geometry certification (about 50 ms)
per check, and each edit, Undo and reopen certifies again. Retention (4 edits, 2 Undos, a reopen) is under the 0.5 s
per-check rule with little headroom.
