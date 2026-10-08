---
id: investigation-rws-record-path
title: C-runtime math on the record-write path (Ruling 156 (3))
type: investigation
status: draft
owner: "@trk-rws"
tags: [determinism, cross-platform, crt-golden, record-path, investigation]
links:
  - {to: adr-application-stack, rel: relates-to}
review-by: 2026-10-25
summary: Of the six record-write sites, none reaches a committed hash or golden (each is green under a +1 ulp perturbation, five of six verified to execute under the ring; ConstrainedFit.cs:400 never runs). Five of the six write bytes into the user's project, so the same input can write different project bytes per OS. The only committed goldens hit by C-runtime math in src/ are the Catalog generator and Placement display spacing, which fix/caf-catalog-determinism already addresses. Options per role are listed as recommendations for review.
---

# C-runtime math on the record-write path

Goal: measure which of the six sites in Ruling 156 (3) feed a committed hash, and sweep `src/` for the rest. Done when questions 1-4 are answered with measurements. Not in scope: any fix. Tier T1. Base main 437a9967, branch `inv/rws-record-path`. Labels: **Verified** (observed on this Mac, 2026-10-08) or **Inferred** (reason given).

Scratch (not committed): `/Users/mallalieut/projects/cfd-workbench-continuation/scratch/rws/` (`perturb.py`, `run1.sh`, `copy/src/CfdWorkbench.Core/CrtProbe.cs`, `out-*.txt`, `hits-*.tsv`).

## Method (Verified)

1. A scratch copy of the worktree (no `.git`). `perturb.py` wraps every `Math.<transcendental>(...)` on a named line in `CrtProbe.Up(tag, value)`, which returns `double.BitIncrement(value)` (+1 ulp), except when the value is exactly 0, 1 or -1 (a real libm returns those exactly at the cosine end points; a first attempt without that guard moved `x` to -1.1e-16 and hung the Core harness, which was a probe artifact, not a finding).
2. `tools/run-tests.sh` (the full ring, 7 jobs) after each perturbation. Baseline: exit 0, 49 s, 241+241+241 Core, 700 Desktop, 101+135 Analysis, 6 Cli PASS.
3. `CrtProbe` counts calls per tag and writes them at process exit, so a green result can be told from "the site never ran".
4. Canaries, to prove the probe can turn the ring red: `Placement.cs:209,:697` and `Catalog.cs:197` (see Q2).

Limits: +1 ulp only (one direction), Mac only, no Windows run. `Math.SinCos` and `double.Atan2Pi` are not wrapped by the probe (Placement.cs:117 and ElevationView.cs:1293 are classified by reading). `Math.Pow(x, 2)` is counted as a call, but is treated as Inferred-exact in practice, not measured.

## Answers

### 1. The six sites, re-checked on main (Verified by reading)

The ruling's lines have drifted: `SectionReplace.cs:327` is now `:341` (`:215` is a second cosine in the same file). The rest match.

| Site | Call, on what | What it feeds | Sink |
|---|---|---|---|
| SectionReplace.cs:341 `Cosine()` | `Math.Cos(PI*i/(SourceSamples-1))`: x of 201 samples taken on a catalog/record source curve (`Shape.From`, `ReplaceSource.Record` only) | `DatProfile` samples -> `DatImport.FitToBasis` (SectionReplace.cs:119) -> `Candidate` -> `FoilSource.WriteSurfaces` (SectionReplace.cs:152) | **project file write** (the replaced section's control points) |
| DatImport.cs:386 `EuclideanResidual` | `Math.Cos(PI*i/200)`: 201 parameters at which the fit is compared with the source | the residual `Math.Max` -> `current.Residual <= limit` and `own.Residual < best` (SectionReplace.cs:54-67), the reported residual (SectionReplace.cs:88), refusal text | **validation boolean + display**; it also selects which spacing is `chosen`, so it can change the written bytes only when a residual sits within ~1e-16 of the limit (Inferred) |
| DatImport.cs:524 `ParseInChordFrame` | `Atan2(my,mx)`, then `Cos(angle)`, `Sin(angle)` of it: rotation of every coordinate of a user .dat (and record) into the chord frame | `framed` points -> `DatProfile` -> `SourceCurve` and `FitToBasis` -> written; also `RotationDegrees` in the report | **project file write** (and display) |
| ConstrainedFit.cs:281 `FitSide` | `Exp((Log(lo)+Log(hi))/2)`: geometric midpoint of the lambda bisection | `Consider(mid)` -> `best` ordinates -> `Build`/`ReplaceOrdinates` -> `FairResult` curve | **project file write** (Fair/Rebuild result) |
| ConstrainedFit.cs:400 `AddTangentRows` | `Cos`, `Sin` of a user tangent angle | extra equality rows of the constrained fit -> fitted ordinates | **project file write** (only for fits with an `angle` tangent row) |
| FoilSource.cs:654 `TipClusteredChannel` | `Pow(1-fraction, power)` with `tipPower = 3`: interior knots of the default rails and channels | `NewDefault()` -> the bytes of every new project | **project file write** (the first document of a New project) |

### 2. Committed hashes or goldens reached (Verified by perturbation)

Each row is one +1 ulp run of the full ring in the scratch copy.

| Perturbed | Calls wrapped | Hits in ring | Ring result |
|---|---|---|---|
| SectionReplace.cs:341 | 1 | 402 | green (exit 0) |
| DatImport.cs:386 | 1 | 129,645 (all-six run) | green |
| DatImport.cs:524 | 3 (Atan2, Cos, Sin) | 363 | green |
| ConstrainedFit.cs:281 | 1 | 735 | green |
| ConstrainedFit.cs:400 | 2 | **0** | green, but the site never ran: **not measured** |
| FoilSource.cs:654 | 1 | 642 | green |
| all six at once | 9 | as above | green |
| canary Placement.cs:209,:697 | 2 | 2,073,406 | **red**: `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged` (blended-dihedral.foil hash), `Sections_PlaceEqualsSurfaceMidline_Bitwise`, `ProfileView_Samples_CosineSpacedAtNose` |
| canary Catalog.cs:197 | 1 | 8,829 | **red**: `Catalog_GenEntries_RegenerateToRecordedHash` and 6 more Core checks, ~25 Desktop checks (`a catalog file failed its check`, naca-0009: 6686 vs 6678 bytes) |

Reading: the probe works, so a green result is a measurement. **No test, fixture or recorded hash pins the output of any of the five executed record-write sites to the bit.** Why (Verified by grep): the tests that run them assert tolerances, structure or self-consistency. The one committed `NewDefault` look-alike, `tests/CfdWorkbench.Core.Tests/Fixtures/planform-verbs/new-default-10.foil`, is read as an input by PointCommandTests/PointVerbTests; no test compares `NewDefault()` bytes with it. The hashes in tests are `Identity.Sha256` of bytes the test itself just produced.

Evidence is thin for some sites: `DatImport.cs:331` has 2 hits, `PointModel.cs:91/92` 8, `SectionEdits.cs:402/403` 8, `:415` 16 (Q3). A green on a site with a handful of calls says "no golden on that one path", not "no golden anywhere".

CAT (cat-geometry finding 8) is confirmed from the other side: the Replace chain writes bytes that differ by OS within tolerance, and nothing in the committed tests notices.

### 3. Sweep of `src/` (Verified: grep; classes by reading, perturbation as stated)

Pattern: `(Math|MathF|double).(Sin|Cos|Tan|Asin|Acos|Atan|Atan2|SinCos|Sinh|Cosh|Tanh|Exp|Log|Log2|Log10|Pow|Cbrt|AtanPi|Atan2Pi)`; no `using static System.Math`, no `MathF` in `src/`. `tools/check-crt-transcendentals.py` is **not on main**: it exists only on branch `fix/caf-catalog-determinism` (b5955295) and lists `Catalog.cs` and `Placement.cs`. 111 call lines in 42 files: Core 36, Analysis 54, Desktop 21, Persistence 0, Cli 0.

Whole-project perturbation runs (+1 ulp on every line of the project at once):

| Project | Lines | Result |
|---|---|---|
| Core (36) | all | red only in the Catalog generator and Placement checks listed above; everything else green. 2 lines never ran (ConstrainedFit.cs:400, Placement.cs:117 not wrapped) |
| Analysis (54) | all | green except two exact-equality asserts (below); FindAlpha.cs:32 never ran |
| Desktop (21) | all | green; 7 lines never ran (ElevationView 1293, PlanCanvas 567, PropertiesView 1203, SectionProfileView 234/268/331/342) |

Counts by class (line level):

| Class | Lines | Where |
|---|---|---|
| (a) feeds a committed hash or golden | **9** | listed below |
| (b) feeds bytes written to a user's project | **13** (+ 0 in Analysis/Desktop) | listed below |
| (c) display or validation only | **89** (Core 14, Analysis 54, Desktop 21) | not listed |

**(a) in full (9):**

- Catalog.cs:165, :166 (offset by thickness normal), :185 (`Atan2`), :189, :190 (rotation), :197 (cosine spacing): the generator behind the shipped NACA `.dat` bytes; `Catalog_GenEntries_RegenerateToRecordedHash` and 6 more Core and ~25 Desktop checks go red (Verified). Fixed on `fix/caf-catalog-determinism` (50499dcc) per Ruling 156.
- Placement.cs:209, :697 (cosine spacing; red in 3 checks, Verified) and :117 (`Math.SinCos` for the twist angle; not wrapped, Inferred from the gate's own comment "the twist angle behind placement-surface-bits"). Fixed on the same branch (06223cb8).

**(b) in full (13):**

- The six sites of Q1 minus DatImport.cs:386: SectionReplace.cs:341, DatImport.cs:524, ConstrainedFit.cs:281, :400, FoilSource.cs:654 (5 lines).
- DatImport.cs:331 `Math.Tan(angle*RadiansPerDegree)`: slope of an `angle` tangent row in the fit constraint matrix -> fitted control points (Verified by reading; 2 hits).
- DatImport.cs:460 `Pow(.,2)` inside `SourceCurve` chord-length parameter: only `Pow(x,2)`, Inferred exact on all three C runtimes (not measured) -> fit samples.
- FoilSource.cs:581 `Cos` in `FitNaca`: sample positions for the default section ordinates -> `NewDefault()` bytes.
- SectionEdits.cs:402, :403 (`Pow(.,2)` of handle length), :415 (`Cos`, `Sin` of an `angle` tangent): handle moves written by the edit (Verified by reading).
- PointModel.cs:91, :92 `Cos`, `Sin` of a typed angle: `HandleTarget` -> the handle position the verb then writes (Inferred: the caller was not traced to the write).

**Test-side exact equality (a CRT-GOLDEN shape, Analysis, not a golden file):** `SectionForce_RunScale_LiftFixedByLargestStripDragByRule` expects `RoundUp125(1733) == 2000.0` exactly; with all Analysis sites +1 ulp it gets `2000.0000000000002` (`Math.Pow(10, Math.Floor(Math.Log10(v)))`, SectionDisplay.cs:464, Inferred as the cause; not bisected). `Loads_TotalDrag_InducedPlusProfileOrNamesMissing` expects `30.4` exactly and gets `30.400000000000002` (site not bisected). A real C runtime that returns a different last bit for `Pow(10, 3)` would turn these red on that OS. The display value itself is class (c).

Analysis writes no file: `grep` finds no `File.Write*` in `src/CfdWorkbench.Analysis`, and the CLI prints JSON to stdout. So all 54 Analysis lines are (c) (Inferred from that grep).

### 4. Options and what each costs (recommendation for review, not a decision)

Options: **A** CAF's approach (managed `CosPi`/`SinCosPi`, `sqrt`-only, no `Pow`); **B** tolerance on the golden; **C** accept OS-dependent project bytes with a stated tolerance.

| Role | Sites | Recommendation | Cost |
|---|---|---|---|
| (a) golden-keyed generator | Catalog 6, Placement 3 | **A**, already ruled and built on `fix/caf-catalog-determinism`; merge it. B would void the bit golden | one re-record per generator (done on the branch) |
| (b1) fixed-grid helper whose inputs are constants | SectionReplace.cs:341, FoilSource.cs:581 (and the display-only cosines in (c)) | **A** is cheap: the same `CosPi(i/n)` CAF already wrote, exact at the end points and quarter points; these are the same expression as the Placement spacing | no golden to re-record; the written bytes change once on every OS (no fixture pins them) |
| (b2) `NewDefault` | FoilSource.cs:654 | **A**: `tipPower` is a constant 3, so `1 - (1-f)*(1-f)*(1-f)` removes `Pow` exactly; with :581 gives byte-identical New projects on both OS | one-time change of the default bytes; zero tests pin them (Q2) |
| (b3) data-dependent fit (user .dat rotation, lambda bisection, tangent angle, handle polar entry) | DatImport.cs:331, :460, :524; ConstrainedFit.cs:281, :400; SectionEdits.cs:402-415; PointModel.cs:91, :92 | **C**: the input is arbitrary user data, so a transcendental cannot be removed (A applies only to the exact angles via `SinCosPi(deg/180)`: it makes 0, 90, 180 degrees exact, which is a quality gain on tangent rows, not a determinism fix). The written record is the authority and reopens identically on either OS. State the tolerance (the Replace residual limit and the 1e-12 reproduction floor are already stated) and add one ring check that perturbs the site by 1 ulp and asserts the outputs stay inside it. The check names what it protects (the class) and should cost about a second (Inferred, not built) | one small test per family; no re-record |
| (b4) acceptance threshold | DatImport.cs:386 | **C**, no change: it decides a boolean at a limit of micrometres; the change would need a residual within 1e-16 of the limit | none |
| (c) display or validation | 89 lines | leave. Do not widen the CAF gate's `FILES` to them: the gate says "list a file when its output reaches a committed hash" and a marker on 89 lines is noise | none |

Two follow-ups that need an owner, not done here: (1) `ConstrainedFit.cs:400` and `Placement.cs:117` never run in the ring, so the angle-tangent Fair path has no test at all; (2) the two exact-equality Analysis asserts should compare with a tolerance or the display scale should avoid `Pow(10, n)`.

## What this does not show

- Windows behaviour: no run there; the +1 ulp model reproduced CAT's byte 727 only for the catalog (CAT's finding), not here.
- That a real libm differs at these sites; only that if it did by 1 ulp, no committed golden would notice (five sites), or no test would run (one site).
- Any claim about `Pow(x, 2)` exactness (Inferred).
