---
id: proof-copy-615-post-0b277815
title: "Ruling 267 receipt: genuine post-0b277815 stored run compared under COPY-615"
type: proof-pack
status: active
owner: "@timianmalloo"
phase: implementation
tags: [a3d, compare, copy-615, old-build, ruling-267, analysis]
links:
  - { to: rulings, rel: implements }
  - { to: adr-0011-analysis-run-storage, rel: depends-on }
  - { to: proof-a3a-old-build, rel: relates-to }
review-by: "2027-03-19"
summary: >-
  A genuine cfdw-project-2 document written by commit 0b2778153b182e85d0da9dd53517396ccce29964
  (modern axes and ReferenceQuantities, predating Provisional/YLow/YHigh/FlagCode/Discrepancies),
  reopened by the current build and compared against a modern run with different reference
  quantities to verify the exact COPY-615 headline and dimensional deltas in both orientations.
---

# Ruling 267 receipt: post-`0b277815` COPY-615 fixture

Closes the open gap recorded in **Ruling 267** (`docs/notes/rulings.md`):

> the COPY-615 headline for a stored run from a build at or after 0b277815 whose serialized run
> record predates fields added later (read with defaults) is Inferred, not Verified; the fixture
> that closes it must be produced by such a build's own writer, committed with its receipt and
> pinned hash, and opened by a test with the plan's fail condition (any run shown without its
> COPY-615 reason or with a wrong one).

## Producer build and commands

1. **Historical commit:** `0b2778153b182e85d0da9dd53517396ccce29964` (`feat(analysis): couple strips, loads, water, and the product wing method`).
   - Introduces `ReferenceQuantities` with `axes = "body; wind for lift and drag"` and `momentDatum = "frame origin"`.
   - Predates `StripLoad.Provisional`, `StripLoad.ProvisionalReason`, `StripLoad.YLow`, `StripLoad.YHigh`, `StripValue.FlagCode`, and `AnalysisRecords.Discrepancies`.
2. **Isolated checkout and capture:**
   - Checked out in a scratch detached worktree outside the repository (`<temp>/cfdw-r267-gen/wt`):
     `git worktree add --detach <temp>/cfdw-r267-gen/wt 0b2778153b182e85d0da9dd53517396ccce29964`
   - Built a throwaway console driver referencing the detached worktree's unmodified `CfdWorkbench.Core.csproj` and `CfdWorkbench.Analysis.csproj` (`dotnet run -c Release`).
   - Opened `docs/examples/foildsl/foil-basic.foil` via `AuthoringSession.Open(..., acceptIdInsertion: true)`, ran `AnalysisService(session, new ProductWingMethod()).EvaluateAsync(new OperatingPoint(10.0, 101325.0, null, "free surface", 2.0, null), WaterTable.At(15.0, 35.0), Tier.VlmStrip, new Scope.Wing(), default)` (128 strips, `Completed`), and captured `session.SaveImage()`.
   - Removed the scratch worktree and directory cleanly (`git worktree remove --force`).

## Pinned hashes and schema check

- **Raw writer output on Windows (CRLF, predating `3d58a915` LF pin):**
  - Bytes: `138224`
  - SHA-256: `b3b2270797d9054f805fab768b4dd89e2b470028435267d8118d1b070ed4f385`
- **Committed fixture (`docs/proof/copy-615-post-0b277815/one-run.cfdw.json`, LF normalized per `.gitattributes` `eol=lf`):**
  - Bytes: `134627`
  - SHA-256: `676a7c632d7066ac8f6875e6a5b438bd8433184de25a76215acbda8c6b1791a8`
- **Schema facts verified on the committed bytes:**
  - Contains `"axes": "body; wind for lift and drag"`.
  - Contains none of `"provisional"`, `"yLow"`, `"yHigh"`, `"flagCode"`, `"discrepancies"` (all read with defaults by the current reader while preserving `RunIntegrity.Intact`).

## Verification test and fail condition

`Compare_Post0b277815StoredRun_Copy615DimensionalDeltas` in `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs`:

1. Pins the committed SHA-256 (`676a7c632d7066ac8f6875e6a5b438bd8433184de25a76215acbda8c6b1791a8`) and verifies the absent later fields.
2. Reopens `one-run.cfdw.json` with `AuthoringSession.Reopen`, confirms `RunIntegrity.Intact`, edits span (`900 mm` → `1000 mm`) so `S_ref` and `b_ref` differ while operating point, water, moment datum, and axes stay identical, and computes a modern `vlm-strip` run.
3. Builds `CompareProjection` in **both orientations** (`old -> modern` and `modern -> old`) and asserts `CompareState.ReferenceDiffers`, exact `COPY-615` (`Labels.DimensionalHeadline`), exact `Labels.DeltaBlockedReferenceDimensional("S_ref, b_ref")`, exact dimensional `L` and `D_i` deltas, blocked `CL`/`CDi`/`e`, zero stored discrepancy records, and unchanged fixture bytes. Any run shown without its COPY-615 reason or with a wrong one fails the test.
