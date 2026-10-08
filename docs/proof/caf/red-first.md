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

## 4. Placement commit (Ruling 156 P3)

Gate, with `Placement.cs` added to the file list and the code unchanged (exit 1):

```
Placement.cs:117: Math.SinCos
Placement.cs:209: Math.Cos
Placement.cs:697: Math.Cos
```

After the three sites move to `double.SinCosPi(degrees / 180)` and `double.CosPi(index / step)`, the gate prints `check-crt-transcendentals: 2 file(s) clean`. The Core harness (exit 1)
then shows the golden red, plus two test-side replicas of the same spacing:

```
FAIL Sections_PlaceEqualsSurfaceMidline_Bitwise InvalidOperationException: example.foil s0 i7 X bits 3fa0c5fe51a180f6 -> 3fa0c5fe51a180f5
FAIL ProfileView_Samples_CosineSpacedAtNose InvalidOperationException: Expected 4587018754652591008; actual 4587018754652591016
FAIL Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged InvalidOperationException: Expected blended-dihedral.foil 943866A4...  (5 hashes differ)
RESULT failures=3
```

The two replicas (`SectionsTests.Cosine`, `DisplayProfileTests.Cosine`) were moved to `double.CosPi`; the harness then showed `RESULT failures=1` (the golden only). Then
`placement-surface-bits.txt` was re-recorded once from the 5 `actual` hashes of that run, and `CFD_TEST_ONLY=Placement_ProfileEvaluatorFold` is green. The Analysis harness (`failures=0`)
and the Cli and Desktop default runs stayed green without edits. The surface list is `docs/proof/caf/surfaces.md`.

## 5. Gate bypasses (geometry re-check, condition C1)

Red: the eight bypass shapes run through the gate's `offences()` as committed in 06223cb8 (probe in scratch; exit 1, `flagged 0 of 8`):

```
MISSED double a = double.Cos(x);
MISSED double a = double.Atan2(y, x);
MISSED double a = double.Exp(x);
MISSED double a = double.AcosPi(x);
MISSED using static System.Math;
MISSED double a = Cos(x);
MISSED float a = MathF.Sin(x);
MISSED double a = Math.Cos(x) + "crt-allowed: Ruling 156 - x".Length;
```

Fix: `BANNED` is `Math|MathF|double` followed by the 30 names in the re-check list (CosPi, SinPi, SinCosPi, Sqrt stay legal); `using static System.Math|MathF|Double`
is flagged (a bare `Cos(x)` needs that line; a bare-name rule was tried and dropped, because it flagged the interface method `SinCos(...)` declared in Placement.cs);
the allow marker is read from the `//` comment only (string literals are blanked first) and must read `crt-allowed: Ruling <n> — <reason>`; `ALLOWED_COUNT` pins
Catalog.cs = 1 and Placement.cs = 0, so a new escape is a change to the gate. The Catalog.cs marker now cites Ruling 156 (comment only, no code change).

Green: `python3 tools/check-crt-transcendentals.py --self-test` prints three `SELFTEST PASS` lines (fifteen planted shapes: 11 flagged incl. all the bypasses above,
1 allowed, and comment, string and CosPi/SinCosPi/Sqrt clean; a missing file; a marker beyond the pinned count). The plain run prints `check-crt-transcendentals: 2 file(s) clean`.

## Decisions recorded here

- **P3, station angle.** `ReadStation` calls `Binary64.SinCosDegrees` (`double.SinCosPi(degrees / 180)`, the form the ruling names), not the interface `SinCos(Radians(degrees))`.
  The interface member `Binary64.SinCos(radians)` stays because `IPlacementScalar` requires it (`Geometry.cs`'s `RationalInterval` is the other implementer and is not in
  this track's paths); it now uses `SinCosPi(radians / Math.PI)`.

- **P2, rotation column.** Chosen: keep degrees in the `rotation` column and compare within 1e-12 relative (`|a - b| <= 1e-12 * max(|a|, |b|)`), not direction cosines.
  Why: the column, `CatalogEntry.FrameRotationDegrees` and the `G4` assertion in `CatalogTests` keep their meaning, with no schema change; the Atan2 feeds no byte (the frame
  rotation uses `mx/length` and `-my/length`). `LeShift` and `Scale` use only `+ - * / sqrt`, so their exact compare stays. The gate allows the one line with a
  `crt-allowed: <reason>` marker (a reason is required; the self-test plants the empty-reason case).
- **GEO-A.** `Naca0012Reference` now reads the `Catalog.Load()` bytes of `naca-0012`. No behaviour check was added: with a valid catalog the bytes equal the generator's, so no
  assertion can tell the two sources apart; the guard is the source line and the NeuralFoil tests staying green.
