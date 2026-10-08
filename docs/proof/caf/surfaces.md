---
id: proof-caf-surfaces
title: "Track CAF - E7 surface list for the Placement change (Ruling 156 P3)"
type: doc
status: done
owner: "@trk-caf"
phase: build
tags: [placement, determinism, ruling-156, surface-list]
links:
  - {to: review-cat-geometry, rel: implements}
review-by: 2027-04-01
summary: >-
  Every committed hash or golden that Placement output reaches, found by search. One bit golden moves (placement-surface-bits.txt); no persisted
  non-test artifact carries Placement output, so no decision request is raised.
---

# Which committed hashes and goldens does Placement output reach?

Change: `Placement.cs` no longer calls the C runtime. The display spacing at `ProfileEvaluator.Samples` (`:215`) and `ChordSamples` (`:703`) is
`(1 - double.CosPi(index / step)) / 2`; the station angle at `ReadStation` (`:386`) is `Binary64.SinCosDegrees`, which is `double.SinCosPi(degrees / 180)`.
The interface form `Binary64.SinCos(radians)` (not called on the double path today) uses `SinCosPi(radians / Math.PI)`. Labels: **Verified** unless marked.

## Found by search (commands run in the worktree)

| Search | Result |
|---|---|
| `grep` for `Placement.Surface/Sections`, `ProfileEvaluator.Samples`, `ChordSamples` in `src/` | Consumers: `Analysis` (`MethodRecord`, `SectionDisplay`, `AnalysisService`, `PanelMethod`, `FindAlpha`), `Desktop` (`WorkbenchController` display mesh and frames), `Core` (`SectionModel.DisplayCrossing`, `SectionModel.Measure`). All compute at run time. |
| `git ls-files` for `.cfdw`, `.foil`, json, txt, tsv under `tests/`, `cases/`, `docs/` | Goldens: the six rows below. |
| `git grep` for the five old `placement-surface-bits` hashes | Only `docs/proof/win-routes/**` and `docs/proof/win-smoke-reverify/**` (Windows ring logs and receipts that quote the old expected hash). They are historical run records, not goldens that any test reads. |
| Run of Core, Analysis, Cli and the Desktop default harness after the change, before re-recording | Exactly one golden failed: `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged`. Two test-side replicas of the spacing failed and were updated (see below). |

## The list

| Golden or hash | Reached? | Action |
|---|---|---|
| `tests/CfdWorkbench.Core.Tests/Fixtures/m12c/display/placement-surface-bits.txt` (5 SHA-256 values over `Placement.Surface` meshes) | **Yes, moves.** | Re-recorded once, in this commit. |
| `DisplayProfileTests.cs` `Cosine(index, count)` and `SectionsTests.cs` `Cosine(count)` (test-side replicas of the spacing, bit-compared with `Equal(Bits(...))`) | **Yes, moved.** `ProfileView_Samples_CosineSpacedAtNose`, `Sections_PlaceEqualsSurfaceMidline_Bitwise` failed. | The replicas now use `double.CosPi`, the same formula as the code. `PlacementTests.cs:505` has the old formula too and still passes (tolerance compare); left alone. |
| `tests/CfdWorkbench.Core.Tests/Fixtures/m12b2/*` placement certificate golden (JSON, `PlacementTests.CertificateGolden`) | Reached, unchanged. Compares the binary64 evaluation with a rational enclosure; the change is about 1e-16, far inside the enclosure. | None. Green. |
| `tests/CfdWorkbench.Core.Tests/Fixtures/m12b2/placement-operation-trace.golden.txt` | Not reached: it records the symbolic `Trace` scalar (`rad(...)`, `sin(...)`), not `Binary64`. | None. Green. |
| `tests/CfdWorkbench.Analysis.Tests/Fixtures/a3a/f17-example.txt` (VLM strip vector through `Placement.Sections`) | Reached through the twist angle of `foil-basic.foil`; the test reads the header and compares the vector within its tolerance. | None. Green. |
| `tests/CfdWorkbench.Analysis.Tests/Fixtures/neuralfoil/cases.tsv` | Not reached by Placement (NeuralFoil reads the catalog bytes; see the catalog commit). | None. Green. |
| `docs/proof/a3a-old-build/one-run.cfdw.json` (a persisted run record from an old build) | Not reached. Decoded: the strips are synthetic (`gamma` 0.31 on every strip, `tableHash` `aaaa...`), written by `RecordRun` in a test program; `surfaceHash` is a hash of the surface definition, not of Placement output. Its only use is the old-reader refusal check. | None. |

## Run freshness (geometry re-check, condition C2)

The run key carries `Placement.PlacementRuleVersion` (`src/CfdWorkbench.Analysis/Freshness.cs:27`, compared at `:67`), and this change leaves it unchanged. Runs stored
before the change therefore still show as fresh, although a re-run differs by about 1e-16 relative. Accepted: the version names the rule, not the bits, and
Mac and Windows runs already differed by this much. Verified by reading `Freshness.cs`; the 1e-16 figure is **Inferred** from the angle and spacing arithmetic, not measured on a run record.

## Persisted non-test artifacts: none found

No shipped file, stored project fixture or recorded receipt carries Placement output that a test or the product compares with live Placement output.
Result: **no stop and no decision request**. Two limits, both labelled: (1) the search is over tracked files in this worktree; a user's own saved project
that holds an analysis run recorded before this change keeps the numbers it was computed with (a run record stores its results; the run key hashes settings
and inputs, not results; **Inferred** from `RunRecord.cs`, not exercised), and a re-run gives results that differ at about 1e-16 relative. (2) Cross-OS
determinism of Placement output is **Inferred until the Windows ring**.
