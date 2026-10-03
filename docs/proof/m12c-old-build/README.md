---
id: proof-m12c-old-build
title: M1.2c old-build characterization of section files
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation
tags: [m12c, sdr, foildsl, characterization, section, downgrade]
links:
  - {to: design-m12c-section-editor, rel: documents}
  - {to: adr-0007-edit-transactions, rel: depends-on}
  - {to: adr-0005-point-types, rel: depends-on}
review-by: 2026-12-30
summary: >-
  What the 4b9bc35 CLI and app controller do with M1.2c section files (cases a–e of design §3.3): every
  file is refused or read-only, no edit or Save succeeds, and every file's bytes are unchanged.
---

# Old-build characterization (M1.2c, cases a–e)

**Result.** The `4b9bc35` build never changes an M1.2c file. It refuses every native project, and opens the two
bare `.foil` cases read-only. No edit begins and no Save succeeds. The SHA-256 of every file is the same
before and after. Three outcomes differ from the design's Inferred expectations (marked ≠ below).

## How it ran

- `generate/` writes the fixtures with this branch's build (M1.2c SDR). Projects c1 and d1 are saved by the
  real section draft. A profile `tangents` row cannot be written before SPT, so the sources with a row are
  assembled as text. Their design hash is the hash of the same source without the row, because a tangent row
  is outside identity (ADR-0005 §4; `Identity_TangentsRows_DefinitionHashUnchanged`). **Inferred:** the bytes
  SPT will write have this shape.
- `probe/` was built against a throwaway worktree at `4b9bc35` (`git worktree add --detach`), then removed
  clean. For each fixture, on a scratch copy, it ran:
  1. that build's CLI (`inspect <path> --json`);
  2. its app controller: `WorkbenchController.OpenAsync`, one keyboard edit (`BeginGesture` on leading
     `cv-2`, then `Nudge`), and `SaveAsync` to the same path.
- The raw lines are in `receipt.jsonl`. The app path is the controller the window calls, run headless. The
  rendered window was not driven.

## Results

| Case | Fixture | Expected (§3.3) | Observed at `4b9bc35` (CLI exit · code; app) | Edit · Save | Bytes |
|---|---|---|---|---|---|
| (a) per-surface bases, no rows | `a-per-surface.foil` | Unsupported, read-only | 3 · `DSL-GEOMETRY` "Independent profile x mappings are not assessed."; app "Refused. Original source retained read-only." | no point · `DOC-TYPE` | unchanged |
| (b) profile `tangents` row | `b-profile-row.foil` | `DSL-LOCK` at parse | 2 · `DSL-LOCK` "A tangent row names an interior anchor."; app read-only | no point · `DOC-TYPE` | unchanged |
| (b) control: same shape, no row | `b0-profile-anchor-no-row.foil` | — | 0 · certified; app opens | edit began · `DOC-TYPE` (a `.foil` is never saved over) | unchanged |
| (c1) `"section"` receipt, no profile row | `c1-section-receipt.cfdw.json` | `DOC-REFERENCE` | 2 · `DOC-REFERENCE`; app refuses | no point · `DOC-EMPTY` | unchanged |
| (c2) `"section"` receipt, source with a row | `c2-section-receipt-row.cfdw.json` | `DOC-SCHEMA` (Inferred) | ≠ 2 · `DSL-LOCK`; app refuses | no point · `DOC-EMPTY` | unchanged |
| (d) `"section"` recovery | `d1-section-recovery.cfdw.json` | `DOC-REFERENCE` | 2 · `DOC-REFERENCE`; app refuses | no point · `DOC-EMPTY` | unchanged |
| (d) control: recovery on a row-free anchored history | `d0-section-recovery-no-row.cfdw.json` | `DOC-REFERENCE` | 2 · `DOC-REFERENCE`; app refuses | no point · `DOC-EMPTY` | unchanged |
| (d) recovery with a row in the history | `d2-section-recovery-row.cfdw.json` | `DOC-SCHEMA`, as (c2) | ≠ 2 · `DSL-LOCK`; app refuses | no point · `DOC-EMPTY` | unchanged |
| (e) per-surface source in the open row, no rows | `e1-open-per-surface.cfdw.json` | Unsupported, read-only | ≠ 4 · `DSL-NOT-ASSESSED`: refused, not read-only | no point · `DOC-EMPTY` | unchanged |
| (e) the same with a row | `e2-open-per-surface-row.cfdw.json` | `DOC-SCHEMA` (Inferred) | ≠ 2 · `DSL-LOCK`; app refuses | no point · `DOC-EMPTY` | unchanged |

## What the differences mean

- **c2, d2, e2 read `DSL-LOCK`, not `DOC-SCHEMA`.** `NativeProject.Check` parses every retained source through
  `SessionSource.Parse`, which throws the parse diagnostic's own code. The old parser refuses the row as an
  interior-anchor violation. The refusal and the unchanged bytes are as the design expects. Only the code
  differs.
- **e1 is refused, not read-only.** A native project's current revision must be admitted at reopen. The old
  build refuses an Unsupported revision there (`DSL-NOT-ASSESSED`). The read-only path exists only for a bare
  `.foil` (case a). The user sees an open failure, and the file is unchanged.
- Deviation **D-6** holds. An old build shows "a tangent row names an interior anchor", "not assessed" or a
  reference refusal instead of "saved by a newer version", and it never changes the file.
