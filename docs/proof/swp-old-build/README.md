---
id: proof-swp-old-build
title: "SWP old-build receipt: a cfdw-project-2 file carrying experiments opened by the pre-SWP build"
type: proof-pack
status: active
owner: "@timianmalloo"
phase: implementation
tags: [sweeps, swp, analysis, native-format, rollback, old-build, copy-130]
links:
  - { to: design-local-sweeps, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: implements }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-22"
summary: >-
  A cfdw-project-2 sample carrying a 12-case queued local sweep under the additive top-level experiments member
  (zero analysis runs yet), opened by the pre-SWP build at 90b4af4f: refused fail-closed with
  DOC-UNSUPPORTED-FIELD (UnknownContent -> COPY-130) and the file's SHA-256 is unchanged.
---

# SWP old-build receipt

Design `docs/design/local-sweeps.md` §1.5, §8 (`T7`), and Ruling 271 (`DR-SWP-1`). Session `w6-sweeps-build`,
2026-11-22. This receipt proves the pre-SWP reader half of `SweepStore_OldBuildReader_FailsClosedCopy130`: an older
build refuses a `cfdw-project-2` document carrying the additive top-level `experiments` member before writing.

## What ran

1. **The sample.** `one-sweep.cfdw.json`, beside this note (`9,819` bytes), was written by the `SWP-1` build:
   `docs/examples/foildsl/foil-basic.foil` opened, one 12-case (`3 × 4`) Cartesian VLM+strip sweep queued via
   `AuthoringSession.RecordExperiment` (with zero `AnalysisRun` rows yet), then `SaveImage`. Its format is
   `cfdw-project-2`; it carries `experiments` and omits `analysis`.
2. **The pre-SWP build.** `src/CfdWorkbench.Core` and `src/CfdWorkbench.Persistence` were extracted cleanly via
   `git archive 90b4af4f` (the `main` parent of `SWP-BUILD`, before `SweepRecord.cs`, where `NativeProject.Read`
   enforces `Exact(root, [...], ["analysis"])`). A standalone probe referencing that archived build opened
   `one-sweep.cfdw.json` via `ProjectStore.ReadAsync` and `AuthoringSession.Reopen` and hashed the file before
   and after the call.

## Result

```
refused code=DOC-UNSUPPORTED-FIELD
sha256 before 6e2763b6f107e31bfcb616ae28cd1e4d7083d6692f91ec7e3e4b91726bf0288d
sha256 after  6e2763b6f107e31bfcb616ae28cd1e4d7083d6692f91ec7e3e4b91726bf0288d
file unchanged
```

`DOC-UNSUPPORTED-FIELD` maps to `OpenFailureKind.UnknownContent` in `src/CfdWorkbench.Desktop/OpenOutcome.cs:141-142`,
which renders `COPY-130` ("It contains parts this version doesn't understand. The file hasn't been changed. A newer
version of CFD Workbench may open it.").

In the `SWP-1` build, `SweepStore_OldBuildReader_FailsClosedCopy130` (`tests/CfdWorkbench.Core.Tests/SweepStoreTests.cs`)
and `Store_OldBuildFixtures_TodaysReaderOutcomesPinned` (`tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs`) verify
both that `one-sweep.cfdw.json` opens cleanly as `cfdw-project-2` and that unknown top-level fields (including Wave C's
unparsed `setup` member) remain refused fail-closed with `DOC-UNSUPPORTED-FIELD`.
