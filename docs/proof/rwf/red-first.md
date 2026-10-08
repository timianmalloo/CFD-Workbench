---
id: proof-rwf-red-first
title: "Track RWF - red-first record for the record-write path (Ruling 157)"
type: doc
status: draft
owner: "@trk-rwf"
phase: build
tags: [determinism, record-path, ruling-157, red-first, crt-golden]
links:
  - {to: proof-caf-red-first, rel: relates-to}
review-by: 2027-04-01
summary: >-
  Red and green runs, measured drift per fitted-project family, and the item 3 exact-assert fixes for the record-write path (items 1 to 3 of Ruling 157).
---

# Red-first: record-write path (Ruling 157)

All runs on macOS arm64, .NET 10, branch `fix/rwf-record-path` stacked on `fix/caf-catalog-determinism` (b5955295). Label: **Verified** (observed on this Mac) unless stated. Cross-OS behaviour is **Inferred** until the PC ring prints the `DRIFT` lines. Line numbers are those of this base (the ruling's `:341` is `:327` here).

## Item 1. New-project determinism (commit 9d35ad27)

**Callers of `TipClusteredChannel` (Verified, grep):** `FoilSource.cs:541` and `:542`, both with `tipPower`, a `const double` of 3 in `NewDefault()`. No other caller. `power` is now `int`, `tipPower` is `const int`, and the knot is `1 - (1-f)*(1-f)*(1-f)` by a loop.

**Gate red** (`python3 tools/check-crt-transcendentals.py`, FILES widened, old source, exit 1):

```
FoilSource.cs:581: Math.Cos      FoilSource.cs:654: Math.Pow      FoilSource.cs:1010: Math.Cos
SectionReplace.cs:215: Math.Cos  SectionReplace.cs:327: Math.Cos
```

`FoilSource.cs:1010` is `BeyondProfileIdentity` (the 1e-6 profile oracle, a validation boolean). It is not named in the ruling. It sits in a listed file and the same `CosPi(i/(n-1))` removes it with no marker, so it was changed rather than given a `crt-allowed` escape. After the change: `check-crt-transcendentals: 4 file(s) clean`, ALLOWED_COUNT 0 for both new files.

**Golden.** `NewDefault_ControlPointDoubles_BitGolden` (fast ring, Core, under 0.1 s): 7 hex rows (5 channel curves and the 2 profile sides) of every knot and control-point double, `Fixtures/record-path/new-default-bits.txt`.

| Run | Code | Result |
|---|---|---|
| golden missing | old | FAIL `Expected ; actual leading 0000...` |
| golden recorded from the old code | old | PASS |
| same golden | new (CosPi, int power) | FAIL: the rows moved |
| golden re-recorded once from the new code | new | PASS (6 `NewDefault_*` checks PASS) |

What moved on this Mac: 8 of 36 values on each profile side (`naca-0012/upper`, `/lower`); the 5 channel rows did not move (here `Math.Pow(x, 3)` equals `x*x*x`). The Windows C runtime may have differed from the old code in either place; the new code does not depend on it.

## Item 2. Fitted project bytes (commit 7d193344)

**PointModel caller trace (Verified, reading).** `Planform.HandleTarget` (`PointModel.cs:84`) has one caller, `PropertiesView.Destination` (`PropertiesView.cs:696`, rails only). Its callers are `PropertiesPane.axaml.cs:1602` and `:1753`; `:1602` passes the result to `RunTypedGesture(controller, target, destination)`, a point-move gesture that writes the rail control point. So `:91-92` reaches project bytes.

**One definition.** `PlacementRule.SinCosDegrees(degrees) => double.SinCosPi(degrees / 180)`. `Binary64.SinCosDegrees` (CAF's, Placement) now calls it. Also routed: `ConstrainedFit.cs:400`, `SectionEdits.cs:415`, `PointModel.cs:91-92`. The placement bit golden (`placement-surface-bits`) did not move (full Core harness green). `Desktop/PropertiesView.cs:655` and `:1210` spell `double.SinCosPi(angleDegrees / 180)` themselves; not owned by this track, same expression (see residual.md).

**`DatImport.cs:331` `Math.Tan` stays.** 90 degrees is not refused: `Math.Tan(90 * RadiansPerDegree)` is about 1.6e16, which is finite, and the guard is `!double.IsFinite(slope)`. So the ruling's condition for changing it is not met.

**Family checks.** Each regenerates committed Mac-written output from a committed input and asserts every knot and control-point double within 1e-6 (profile coordinates are chord-normalised), then prints `DRIFT <family> max_chord=<v> limit=1e-06`.

| Check | Committed input | Committed Mac output | Sites reached |
|---|---|---|---|
| `RecordPath_DatRotation_...` | `turned-naca4412-3deg.dat` (catalog 4412 turned 3 degrees) replaced into `foil-basic.foil` | `dat-rotation.out.foil` | DatImport :524, :460 |
| `RecordPath_LambdaFit_...` | `fair-lambda.in.foil`, Fair at 1e-3 | `fair-lambda.out.foil` | ConstrainedFit :281 (76 bisection steps) |
| `RecordPath_TangentAngle_...` | `angle-tangent.in.foil` (an `angle` row on cv-4), Fair at 1e-3 | `angle-tangent.out.foil` | ConstrainedFit :400 via the helper (closes the never-run gap) |
| `RecordPath_HandlePolar_...` | `handle-polar.in.foil` + SetTangent angle 20; `foil-41-tangents.foil` + 7 polar targets | `handle-polar.out.foil`, `handle-target.tsv` | SectionEdits :415, PointModel :91-92 |

**Red.** Before the fixtures existed each check FAILed with `FileNotFoundException` (4 FAIL). With them, all PASS. Sensitivity (Verified): with the lambda midpoint of `ConstrainedFit.cs:281` raised by 0.1 percent in a scratch copy, `RecordPath_TangentAngle_...` FAILs with `DRIFT tangent-angle max_chord=8.2029619971196732E-06`. So the check is not vacuous at a perturbation 1000 times a ulp.

**Two defects of my own, caught and fixed before commit.** (1) The first row parser matched `] ids` with one space; the written files have two, so it found 0 curves and every family reported drift 0 vacuously. Fixed with `\]\s+ids\b` and `Equal(7, expected.Count)`. (2) At Fair tolerance 1e-4 the bisection ends at a lambda below 1e-12, where the output does not depend on lambda (0 drift even at +0.1 percent); the lambda family uses 1e-3, where +0.1 percent moves the output by 8.3e-7 and a ulp by 3.5e-16.

**Measured drift, +1 ulp model (Mac, `tools/crt-probe/`).** This is the model, not Windows. The Windows number is the PC ring's `DRIFT` line.

| Family | Perturbed | Calls hit | max drift (chord) |
|---|---|---|---|
| dat rotation | DatImport :524 (Atan2, Cos, Sin), :460 (Pow) | 6, 320 | 1.2906342661267445e-15 |
| lambda fit | ConstrainedFit :281 | 94 | 3.4694469519536142e-16 |
| tangent angle | `SinCosDegrees` Sin and Cos (model: the site is managed now) | 11, 11 | 3.677613769070831e-16 |
| handle polar, section | `SinCosDegrees` (model) | 11, 11 | 0 |
| handle polar, target | `SinCosDegrees` (model) | 11, 11 | 2.7755575615628914e-17 |

Mac baseline (old `Math.Cos/Sin` fixtures against the new helper): handle polar target 6.1232339957367663e-18 (at 180 degrees the old fixture holds `0.05 * Math.Sin(pi)`, about 6.1e-18, where the helper gives exactly 0); every other family 0. Hit counts are for the whole `RecordPath_` run.

## Item 3. Exact asserts (commit 81b9662f)

Probe sites found by bisection in a scratch copy: `SectionDisplay.cs:464` (the `Math.Pow(10, floor(log10 v))` of `RoundUp125`) and `Loads.cs:109` (cosine span edges, `Math.Cos`). RWS's Trefftz `:42` is not the cause (PASS).

| Perturbation (+1 ulp) | Before the change | After the change |
|---|---|---|
| SectionDisplay.cs:464 | FAIL `SectionForce_RunScale_...`: `1-2-5 up expected 2000; actual 2000.0000000000002` | PASS, both checks |
| Loads.cs:109 | FAIL `Loads_TotalDrag_...`: `Ncrit 4 wing drag expected 30.4; actual 30.400000000000002` | PASS, both checks |
| none | PASS | PASS |

`SectionForceTests.cs:194-196` now `Near(expected, actual, what, 1e-9 * expected)`; `PolarNumericsTests.cs:72` is a relative 1e-9 check inline (that file has no `Near`). `RoundUp125` is unchanged.

## Pre-join addendum (computational-geometry and Test Architect reviews)

**G1 correction.** Earlier text here and in residual.md treated `Math.Pow(x, 2)` as exact in practice (RWS: "Inferred exact"). That is false: a .NET 10 probe on this Mac found `Math.Pow(x,2) != x*x` in 2,647 of 2,000,000 cases, each 1 ulp, pow always the worse result. `DatImport.cs:460`, `SectionEdits.cs:402-403` and `PointModel.cs:193` now use `d*d` (matching `SectionEdits.cs:398`). The four family checks were re-run: drift unchanged (0 for dat rotation, lambda fit, tangent angle and handle-polar section; 6.1e-18 for the handle target). The +1 ulp drift table above, taken before G1, listed `:460` as a perturbed Pow site: that site is now managed; the 1.29e-15 figure stands as the dat-rotation measurement with `:524` and the old `:460`.

**T5.** The drift label is `max_abs` (absolute, in the units of the compared doubles: chord for profile rows, metres for planform rows and `HandleTarget`), not `max_chord`. Earlier tables in this file say `max_chord`; read them as `max_abs`.

**T3. A behaviour red for each of the five DRIFT lines (Verified, scratch copy, probe sites scaled; the committed code is not changed).** Each row is a perturbation that makes that check FAIL.

| DRIFT line | Perturbation | Result |
|---|---|---|
| dat-rotation | `DatImport.cs:524` angle, cos and sin x1.001 | FAIL: `Replace` returns no bytes (the frame fit is refused), `Expected True; actual False` |
| lambda-fit | `ConstrainedFit.cs:281` midpoint x1.1 | `DRIFT lambda-fit max_abs=7.03e-05`, FAIL (x10: 2.2e-03, FAIL). x1.001 gives 8.3e-7 and passes, which is the check's resolution |
| tangent-angle | `ConstrainedFit.cs:281` x1.1 | `max_abs=1.18e-04`, FAIL; also `SinCosDegrees` Sin x1.001: `3.69e-06`, FAIL |
| handle-polar-section | `SinCosDegrees` Sin only x1.001 | `max_abs=1.95e-05`, FAIL (scaling Sin and Cos together cancels in the slope and does not move it) |
| handle-polar-target | `SinCosDegrees` Sin and Cos x1.001 | `max_abs=4.83e-05`, FAIL |

**T4.** `WithinTolerance` asserts `Equal(7, rows)` and that every expected row has at least 8 values, so a row with no values cannot pass.

**T2.** `SectionForceTests` also checks `RoundUp125(100)` within 1e-9 relative.

**T7. Ring and cost of each new check** (Core harness, fast ring, run in `tools/run-tests.sh`; process wall measured alone, which includes about 0.25 s of runtime start-up that the ring pays once per part):

| Check | Ring | Process wall alone |
|---|---|---|
| `NewDefault_ControlPointDoubles_BitGolden` | fast | 0.29 s |
| `RecordPath_DatRotation_*` | fast | 0.36 s |
| `RecordPath_LambdaFit_*` | fast | 0.59 s |
| `RecordPath_TangentAngle_*` | fast | 0.58 s |
| `RecordPath_HandlePolar_*` | fast | 0.47 s |
| `RoundUp125(100)` (inside `SectionForce_RunScale_*`) | fast (Analysis) | negligible |

Each names what it protects: the family checks protect the 1e-6 identity tolerance of project bytes the user saves; the golden protects the New-project default.

## Gates

`python3 tools/check-crt-transcendentals.py --self-test`: 3 SELFTEST PASS. Plain run: `4 file(s) clean`.

`tools/run-tests.sh`, final run: exit 0, `Core 243 + 243 + 242 PASS`, `Desktop 699 PASS`, `Analysis 101 + 135 PASS`, `Cli 6 PASS`, `test costs: 0 failures, 0 COST-MISS`, wall 46 s against the 60 s budget. An earlier run at load 46 exited 1 on one Desktop check, `PlanCanvas_Escape_DismissTooltipThenClearSelection` (a tooltip timing check, unrelated to these files); it passed twice alone and in the final run. Repair cycles used: 1 of 2.
