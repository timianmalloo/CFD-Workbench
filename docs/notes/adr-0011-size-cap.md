---
id: note-adr-0011-size-cap
title: "ADR-0011 amendment note: the native document size cap is 32,000,000 bytes"
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [persistence, document-size, doc-size, adr-0011, ruling-258, ruling-263, measurement]
links:
  - { to: adr-0011-analysis-run-storage, rel: refines }
  - { to: adr-application-project-contract, rel: refines }
  - { to: architecture-application, rel: refines }
  - { to: rulings, rel: depends-on }
review-by: 2027-01-31
summary: >-
  Ruling 263 sets NativeProject.MaxBytes to 32,000,000 bytes (it was 8,000,000), because a document at 32 MB meets Ruling 258's
  unchanged budget as measured and a document at 64 MB does not. The format and its version do not change. An older build refuses a
  larger document with DOC-SIZE and never truncates or rewrites it. Lowering the cap later needs a migration note.
---

# ADR-0011 amendment: the document size cap is 32,000,000 bytes

**Decision.** `NativeProject.MaxBytes` is **32,000,000 bytes** (decimal), up from 8,000,000. ADR-0011 stays as accepted; this
note records the change beside it (DM13). Sources: Ruling 258 (the raise, conditional on a budget), Ruling 263 (the cap that the
measurements support), and the proof `docs/proof/size-cap-64mb/README.md` (the folder keeps its first name).

**Where 8,000,000 was set.** It was ruled in `docs/adr/0004-application-project-contract.md` (line 47, "must satisfy 8,000,000 bytes") and restated in `docs/architecture/application.md` (lines 110 and 205) and `docs/adr/0003-application-stack.md` (line 81, "8 MB M1 envelope"). Those records are accepted and stay as written (DM13); where they say 8 MB or 8,000,000, this note and Ruling 263 govern: the cap is 32,000,000.

**Why.** The operator asked on 2026-10-22 to "increase the VLM run budget so it works". At the measured 109,991 bytes per pinned
128-strip run, 8 MB holds about 72 such runs, which sits close to Ruling 251's trigger of 20 in normal use. 32 MB holds about 290,
by computation, not by measurement. Ruling 258's budget stands unchanged: at the cap, open within 3 s, save within 3 s, peak
working set within 1.5 GB (Desktop app, that document open, one save done).

## What was measured

Windows ARM64 laptop, Release build, NTFS, idle (no other suite running), medians of three runs, each in a fresh process.

| Size | Open | Save | Peak working set | Verdict |
|---|---|---|---|---|
| 64,000,000 bytes | 4,097 ms | 1,298 ms | 1,812,115,456 bytes | refused: open and peak are over budget |
| 32,000,000 bytes | 2,208 ms | 732 ms | 1,012,297,728 bytes | adopted: all three within budget |

Why 64 MB was refused: the open (4,097 ms against 3,000 ms) and the peak (1,812,115,456 bytes against 1,500,000,000) exceed the
budget. Load explains neither: idle lowered the open time by about 8 percent and left the peak unchanged. A raised budget would
defeat the budget, so the cap went down to the size that meets it.

## What does not change

- The document format and its version: the format string stays `cfdw-project-2`. No field is added, so a document keeps its bytes.
- The 4 kB line cap, `MaxDepth`, the 2,048-strip cap per run, and Ruling 251's trigger.
- `LayoutCodec` and `PreferenceStore` keep their own caps.
- One constant, read by every size guard: the session envelope, the save preflight, `NativeProject.Read`, the macOS store and the
  Windows store (including its read-back, which an earlier track found hard-coded). The user-facing "too large" sentence derives
  its number from the constant.

## Older builds

A build from before this change reads a document of up to 8,000,000 bytes. Given a larger one, it refuses with `DOC-SIZE`. It
does not truncate it, repair it or rewrite it, and the file on disk keeps its bytes. A document over 8 MB therefore stays
readable by this build and later builds only; this is the same refusal any too-large file gets.

## Lowering the cap later

A document saved above a lower cap would be refused on open. Any later reduction of `NativeProject.MaxBytes` therefore needs a
migration note first: which documents exist above the new limit, how the owner reaches them, and what the older refusal tells the
user. This note does not authorise a reduction.

## Next step (the operator's, not taken here)

Ruling 263 (c): optimise the open path and its memory, for example a streaming parse instead of holding the whole image and its
parse tree, so a larger cap could meet the same budget. Take it up only if a document needs more than 32 MB.
