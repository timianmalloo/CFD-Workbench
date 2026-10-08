---
id: proof-caf-red-first
title: "Track CAF - red-first record for catalog generator naca4-closed/2"
type: doc
status: draft
owner: "@trk-caf"
phase: build
tags: [catalog, determinism, ruling-156, red-first]
links:
  - {to: review-cat-geometry, rel: implements}
review-by: 2027-04-01
summary: >-
  Red and green runs for the spacing bit golden and accuracy check, the generator id, and the CRT-transcendental gate (commit 1, catalog).
---

# Red-first: catalog commit (Ruling 156)

All runs on macOS arm64, .NET 10, base main `9e4154f5`. Label: **Verified** (observed). Cross-OS determinism: Inferred until the Windows ring.

## 1. The gate is red on main's Catalog.cs

Command: `python3 tools/check-crt-transcendentals.py` with the old `Catalog.cs` (exit 1):

```
Catalog.cs:165: Math.Atan
Catalog.cs:166: Math.Sin
Catalog.cs:185: Math.Atan2
Catalog.cs:189: Math.Cos
Catalog.cs:190: Math.Sin
Catalog.cs:197: Math.Cos
```

(each line prefixed `CRT-TRANSCENDENTAL src/CfdWorkbench.Core/`). Line 197 on main is the cosine spacing (`Math.Cos(Math.PI * index / 80.0)`, the Windows datum);
165/166 and 185/189/190 are the cambered-entry atan, sin, cos and atan2.

## 2. The checks are red on the old generator

The test file was changed first (generator id `naca4-closed/2`, new check `CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy`), with `CatalogGenerator.Spacing(i)` still the old
`0.5 * (1 - Math.Cos(Math.PI * i / 80.0))`. Command: `CFD_TEST_ONLY=Catalog tools/run-suite.sh dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll` (exit 1):

```
FAIL Catalog_GenEntries_RegenerateToRecordedHash InvalidOperationException: Expected naca4-closed/1; actual naca4-closed/2
PASS Catalog_HashMismatch_CatUnavailable
FAIL Catalog_Refusal_NamesItsCheck InvalidOperationException: Expected generated-bytes; actual generator-id
PASS Catalog_VendAndLink_NoCoordinates
PASS Catalog_Fairings_NeverListed
PASS CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations
PASS CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX
PASS Catalog_GenNeverThroughDatParse
FAIL CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy InvalidOperationException: Expected 4586779802925919240; actual 4586779802925919232
RESULT failures=3
```

(The first failure is the id assertion: the test pins `/2` while the generator still reports `/1`; the `Expected`/`Actual` labels in its message are swapped because the
old assertion is written `Equal(CatalogGenerator.Id, "...")`. The second is the same id in the planted rows.) The spacing check fails at the bit compare against `0.5 * (1 - CosPi)`. Separately measured on the old
formula: `0.5 * (1 - cos(pi * 40 / 80.0))` is `0.49999999999999994` (python3 `math.cos`, the same C-runtime call), not 0.5, so the midpoint assertion also fails on the old code.

## 3. The same checks are green on the new generator

After `Spacing` = `0.5 * (1 - double.CosPi(i / 80.0))`, sqrt-only sin and cos of the camber angle, the frame rotation from (mx, -my)/length, and the one re-record of the
three `.dat` files, the `catalog.tsv` hashes and the 4412 rotation and scale doubles (exit 0):

```
PASS Catalog_GenEntries_RegenerateToRecordedHash
PASS Catalog_HashMismatch_CatUnavailable
PASS Catalog_Refusal_NamesItsCheck
PASS Catalog_VendAndLink_NoCoordinates
PASS Catalog_Fairings_NeverListed
PASS CatalogGenerator_Naca0012_MatchesClosedFormAt81Stations
PASS CatalogGenerator_ClosedTe4412_ChordFrameLeAtMinimumX
PASS Catalog_GenNeverThroughDatParse
PASS CatalogGenerator_Spacing_CosPiBitGoldenAndAccuracy
RESULT failures=0
```

`python3 tools/check-crt-transcendentals.py` then prints `check-crt-transcendentals: 1 file(s) clean` (exit 0). The one allowed line is the `Math.Atan2` that fills the recorded
rotation column: no byte reads it, and `Catalog.Read` compares it within 1e-12 relative.

## Decisions recorded here

- **P2, rotation column.** Chosen: keep degrees in the `rotation` column and compare within 1e-12 relative (`|a - b| <= 1e-12 * max(|a|, |b|)`), not direction cosines.
  Why: the column, `CatalogEntry.FrameRotationDegrees` and the `G4` assertion in `CatalogTests` keep their meaning, with no schema change; the Atan2 feeds no byte (the frame
  rotation uses `mx/length` and `-my/length`). `LeShift` and `Scale` use only `+ - * / sqrt`, so their exact compare stays. The gate allows the one line with a
  `crt-allowed: <reason>` marker (a reason is required; the self-test plants the empty-reason case).
- **GEO-A.** `Naca0012Reference` now reads the `Catalog.Load()` bytes of `naca-0012`. No behaviour check was added: with a valid catalog the bytes equal the generator's, so no
  assertion can tell the two sources apart; the guard is the source line and the NeuralFoil tests staying green.
