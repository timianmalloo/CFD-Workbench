---
id: proof-a3a-old-build
title: "A3a old-build receipt: a cfdw-project-2 file opened by the build at the A3a base"
type: proof-pack
status: active
owner: "@track-a3a-sto"
phase: implementation
tags: [a3a, sto, analysis, native-format, rollback, old-build, copy-130]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: implements }
review-by: "2026-11-04"
summary: >-
  A cfdw-project-2 sample written by the STO writer, opened by the build at the A3a base (9709f72, before PRE): refused
  with DOC-UNSUPPORTED-FIELD, classified UnknownContent (COPY-130), and the file's SHA-256 is unchanged.
---

# A3a old-build receipt

Design `docs/design/area3-analysis.md` §3.6 (rollback), §18.2 STO row, P-9; ADR-0011 §6. Session `track-sto`,
2026-10-04. This is the "today's reader" half of `Project2_TodaysReader_FailsClosedCopy130`: the in-harness check is
the forward guard; this receipt is the older build itself.

## What ran

1. **The sample.** `one-run.cfdw.json`, beside this note, was written by the STO build (branch `feature/a3a-sto`):
   `docs/examples/foildsl/foil-basic.foil` opened, one Completed `vlm-strip` run of 3 strips recorded through
   `AuthoringSession.RecordRun`, then `SaveImage`. Its format is `cfdw-project-2`; it carries the `analysis` member.
2. **The old build.** A throwaway worktree (`git worktree add --detach`) at `9709f729305e5efbc021ca035d6fd55a5312fdc9`,
   the first parent of the PRE merge `e142b8c` — the A3a base, with no `RunRecord.cs` and a reader that accepts only
   `cfdw-project-1`. A program referencing that worktree's `CfdWorkbench.Desktop` project opened the sample the way
   `WorkbenchController` does: `ProjectStore.ReadAsync`, then `AuthoringSession.Reopen`, a refusal classified by
   `OpenFailure.Classify`. The worktree was removed clean afterwards (`git worktree remove`).

## Result

```
refused code=DOC-UNSUPPORTED-FIELD kind=UnknownContent
sha256 before 9edbe94b74396a7c97fa72096b15b9f1317a27f79b9e612139c2234e9f0fd929
sha256 after  9edbe94b74396a7c97fa72096b15b9f1317a27f79b9e612139c2234e9f0fd929
file unchanged
```

`UnknownContent` is COPY-130 ("It contains parts this version doesn't understand. The file hasn't been changed. A newer
version of CFD Workbench may open it."): the mapping is pinned at that base by `Copy_OpenFailures_MatchesDesignRows`
(`tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`:1569) and the COPY-130 row of `DESIGN.md`:474.

**Note on the code.** The old reader refuses on the unknown top-level `analysis` member (its `Exact` schema check runs
before the format check), so the code is `DOC-UNSUPPORTED-FIELD`, not `DOC-VERSION`. `DOC-VERSION` would classify as
`Newer` (COPY-103). The design's promise (COPY-130, nothing written) holds; ADR-0011's Context names the format check as
the refusing line, which is the later of the two.

**Not measured here:** Windows (the store is macOS arm64 only); a Desktop UI capture of the COPY-130 card.
