---
id: investigation-cat-determinism
title: Cross-OS catalog generated-bytes mismatch (naca-0009 byte 727)
type: investigation
status: draft
owner: "@trk-cat"
tags: [catalog, determinism, cross-platform, investigation]
links:
  - {to: adr-application-stack, rel: relates-to}
review-by: 2026-10-25
summary: Byte 727 of generated naca-0009 is the X ordinate of upper-surface sample index 63. The only transcendental on the naca-0009 path is Math.Cos in the cosine spacing, which .NET forwards to the platform C runtime. A 1-ulp change of that one cosine reproduces the Windows datum exactly (first differing byte 727, same length). Options and their hash consequences are listed; a computational-geometry ruling comes before any fix.
---

# Cross-OS catalog generated-bytes mismatch

Goal: root cause of the Windows ring failure `entry naca-0009: lengths 6686 vs generated 6686, first differing byte 727` (Ruling 154). Done when questions 1-6 are answered with measurements. Not in scope: any fix. Tier T1. Base main 99116cea, branch `inv/cat-determinism`. Every claim is labelled **Verified** (observed on the Mac in this session) or **Inferred**.

Spike sources (not committed, per brief): `/Users/mallalieut/projects/cfd-workbench-continuation/scratch/cat/spike/Program.cs` (+ `tail.txt`) and `.../spike2/Program.cs`. Run with `dotnet run` in each folder.

## Answers

### 1. Where byte 727 sits (Verified)

Command: spike calls `CatalogGenerator.Naca4("0009")` and compares with `src/CfdWorkbench.Core/CatalogData/naca-0009.dat`.
Result: on the Mac, generated == recorded file (6686 bytes, `SequenceEqual` true), so the Mac matches the committed bytes and hash.

Byte 727 is on file line 18 (header is line 0; the line starts at byte 710), offset 17 in the line. That is the second-to-last digit of the X value of upper-surface point `index = 81 - 18 = 63`:

```
...0.0098032092659308939\n0.89265846544037253 0.010940528949303016\n0.88020298280001...
                                        ^ byte 727 (X field, digit 17 of 19)
```

So the difference is in the last two digits of a G17 X ordinate: an ulp-level difference, not a structural one. Lengths are equal because G17 prints the same digit count.

### 2. The computation (Verified by trace and replica)

`Catalog.cs` `CatalogGenerator.Shape` (lines 140-208). For `0009`: `m = 0`, so `slope = 0`, `theta = Math.Atan(0) = 0`, `Math.Sin(0) = 0`, `Math.Cos(0) = 1` exactly. The frame angle is `Math.Atan2(0, mx) = 0`, so `Cos(-0) = 1`, `Sin(-0) = 0`, and the leading edge shift `xs = 0` (spike print: `xs=0 length=1 angle=0 le.X=0`, matching the recorded row `0 0 1`). The `Math.Sqrt` calls are IEEE-exact and cannot differ. No `Math.Pow`, `Exp` or `Log` exists in `Catalog.cs`.

The one inexact call left on the 0009 path is the cosine spacing at `Catalog.cs:197`:

```csharp
double s = 0.5 * (1 - Math.Cos(Math.PI * index / 80.0));
```

`s` feeds `x = xs + (1 - xs) * s`, which for 0009 is the output X directly. A 1-ulp change in `cos` at index 63 moves X at index 63.

Call-site count in `Catalog.cs` (grep): Atan 165; Sin and Cos 166; Atan2 185; Cos and Sin 189; Sin and Cos 190; Cos 197. That is 4 distinct functions at 9 call sites (the leader's count of 7 matches neither the 9 sites nor the 4 functions; `Math.Sqrt` at 149 and 184 not counted). On the naca-0009 and naca-0012 path only line 197 is non-trivial. On naca-4412 (cambered) all of them are live.

Wider scope (Verified by grep): the same cosine spacing pattern is in `Placement.cs:209` and `:697`; `Math.SinCos` at `Placement.cs:117`. Roughly 110 `Math.{Sin,Cos,Tan,Atan,Pow,Exp,Log...}` sites exist across `src/` (Core, Analysis, Desktop). Only those that feed committed hashes or goldens matter for this class.

### 3. Why it can differ across OS (Verified from runtime source; determinism setting not found)

`dotnet/runtime` `src/coreclr/vm/floatdouble.cpp` (fetched with `gh api`):

```cpp
FCIMPL1_V(double, COMDouble::Cos, double x)
    FCALL_CONTRACT;
    return cos(x);
FCIMPLEND
```

and the file header: "Sin, Cos, and Tan on AMD64 Windows were previously implemented ... by calling x87 ... This is no longer the case and the CRT call is used on all platforms." So `Math.Cos` is the C runtime `cos`: Apple libm on macOS arm64, UCRT on Windows x64. These libraries are not required to return the same last bit. IEEE 754 requires correct rounding only for `+ - * / sqrt` and FMA, not for `cos`.

Measured on the Mac (spike2, exact BigInteger Taylor reference for the double argument): `Math.Cos(Math.PI * i / 80.0)` differs from the correctly rounded value at 6 of 81 indices (14, 28, 52, 55, 61, 62; by 1 ulp). At index 63 the Mac value is correctly rounded. Because the Windows value at index 63 differs by 1 ulp (Q4), the Windows CRT result there is not correctly rounded. So neither platform is "the right one" by construction, and the Mac is itself not correctly rounded elsewhere.

.NET 10 setting or API for determinism: **none found** (searched `Math`/`MathF` docs path through source; no switch controls the CRT dependency). I did not find a documented guarantee of cross-platform bit-identity for `Math.Cos`. **Inferred** that none exists; confirm before relying on it.

The `.NET` managed alternative: `double.CosPi(x)` (`Double.cs:1917`) is a managed polynomial ported from AMD aocl-libm-ose, with no `Math.Cos` call in its first 90 lines (grep found no `Math.` or `Cos(` there). Managed code with `+ - *` is deterministic across platforms (RyuJIT does not contract `a*b+c` into FMA unless written with `Math.FusedMultiplyAdd`; **Inferred**, not tested on Windows).

### 4. Sensitivity (Verified, measured)

Spike: a replica of `Shape` with an injectable cosine; first check replica == `CatalogGenerator.Naca4` == recorded (true). Then perturb `cos` at one index by one ulp (`Math.BitIncrement` / `BitDecrement`) and report the first differing byte:

```
index 61 +1/-1: 809      index 62 +1/-1: 768
index 63 +1/-1: 727  <-- the Windows datum
index 64 +1: 685, -1: no change     index 65 +1: no change, -1: 643
```

A 1-ulp change at index 63, in either direction, gives first differing byte **727**, equal length **6686**, and exactly two lines changed (the upper point and its lower mirror). This reproduces the datum bit for bit and is a measured mechanism, not just a plausible one.

What this does not show: the Windows CRT value itself (no Windows here). The datum says only that the first differing byte is 727. Because the upper surface is written from index 80 down to 0, bytes before 727 cover indices 80..64, so Windows agrees with the Mac at 80..64; it may also differ at indices 62 and below (later in the file), which the first-difference report hides. **Unknown**: the real number of differing indices on Windows. Recommend the PC dump all `Math.Cos(Math.PI * i / 80.0)` bits for i = 0..80 (a 15-line program) before any fix is chosen.

Also unknown: whether naca-0012 and naca-4412 also differ on Windows. `Catalog.Load` throws at the first failing entry (naca-0009 is first), so the ring could not report the others. Expect 0012 to fail by the same cosines; 4412 additionally exercises Atan, Sin, Cos and Atan2, and its `le`/`rotation`/`scale` columns are compared with exact `!=` (`Catalog.cs:101-103`).

### 5. The two class (f) failures

- `Replace_Preview_EmitsCatalogPreviewOutcome` (Family empty): **Verified by code**, same cause. `SectionReplace.CatalogFamilies` (`SectionReplace.cs:254-257`) calls `Catalog.Load()` and on `ContractError` returns null, so `Describe` yields no family. `Catalog.Load` throws the generated-bytes `ContractError` on Windows. The failure is a downstream symptom; no separate bug.
- `Placement_ProfileEvaluatorFold_SurfaceBitsUnchanged`: **Inferred**, probably the same class but a different site. It hashes `Placement.Surface` meshes of the `m12b2/*.foil` fixtures against a golden (`DisplayProfileTests.cs:202-213`). It does not touch the catalog. `Placement.cs:209` and `:697` use the same `Math.Cos(Math.PI * index / step)` spacing and `Placement.cs:117` uses `Math.SinCos`. I did not run a perturbation on it (it needs the fixture and the `Hash` function; not done within the time box). The failing evidence shows only the expected hash prefix, not which fixture differs. Treat as a second instance of one class, to be confirmed by the same ulp test.

### 6. Options

Recorded hashes now: `catalog.tsv` carries a SHA-256 per generated entry (naca-0009 `d0197517...`, naca-0012 `d7ed2309...`, naca-4412 `fab12bf3...`) plus `le`/`rotation`/`scale` doubles (4412: `-0.00030024789938986609`, `-0.17398455663928378`, `1.0003048597813819`); the three `.dat` files hold the bytes; and the `Placement` surface golden hashes. All were produced on the Mac.

**A. Deterministic implementations of cos/sin/atan.** Replace `Math.Cos` etc. with managed code (a fixed polynomial, or `double.CosPi(index / 80.0)` for the spacing, which also removes the `Math.PI *` rounding). Gives: bit-identical output on every OS by construction (+ - * / only), and fixes both the catalog and the placement site. Costs: the values change. Measured: `CosPi` vs `Math.Cos` changes 60 of the lines of naca-0009 and naca-0012 and 59 of naca-4412 (spike `[A]`), so **all three `.dat` files, all three SHA-256 values, the 4412 frame doubles, and the placement goldens must be regenerated and re-recorded**. It also needs a stated accuracy claim (error against the exact value) and a new generator id (`naca4-closed/2`) so the old id never labels new bytes. Risk: a hand-written atan/sin is more code to prove; for 0009/0012 only cos is needed (sin/atan are exactly 0 there); 4412 needs more.

**B. Quantize before serialising.** Print `G10`-`G12` instead of `G17`. Measured (spike `[B]`, 316 perturbations of 1 or 2 ulp per entry, all three entries): G17 changes the bytes in 208/316; G15 118-164; G14 28-70; G12 4-10; **G10 0/316**. So a short format hides ulp noise in these tests but is not a guarantee: any value that lies within an ulp or two of a rounding boundary flips, and the G12 residuals show boundary hits happen. Gives: smallest change, keeps CRT cos. Costs: the bytes (and every hash) change anyway because digits are dropped; it discards precision the profile geometry currently carries (about 5e-10 relative at G10, compared with the 8e-5 residual chord mentioned in tests, so likely harmless, but that is the geometry reviewer's judgement); it hides rather than removes the dependence, so a future platform with a 2-ulp error could still flip a digit; the exact `le`/`rotation`/`scale` comparisons would also need a tolerance or quantisation. Does not fix the placement golden unless that is quantised too.

**C. Per-OS recorded hashes.** Gives: no code change. Costs: the catalog stops being one artefact: the shipped `.dat` bytes would differ per OS, so a project saved on Mac and opened on Windows would reference different geometry for the same catalog id and the same recorded hash would not mean the same shape. That contradicts the purpose of a hash of record (the repository treats the generator output as the geometry authority), and it is the hidden second authority the geometry reviewer vetoes. Not honest unless the hashes are explicitly documented as platform-keyed, which I do not recommend.

**D. Better: fix the numbers once, not the arithmetic.** Ship the generated bytes as the authority (they already exist as `CatalogData/*.dat`, embedded; `Catalog.Load` already reads them) and make the generator a reproducibility check with a tolerance (for example max abs difference 1e-12 per ordinate) instead of byte equality. Gives: the shipped bytes and hashes stay as they are (no re-record), Windows loads the same bytes as the Mac, and the check still catches a real generator change. Costs: the generator no longer proves bit-exact regeneration, which is a weaker claim than `generated-bytes` states today; the tolerance must be justified; and `Placement` still needs its own treatment (option A or a tolerance on the golden).

### Recommendation

Option A for the cosine spacing (and sin/atan where the cambered entry needs them), with a new generator id and one deliberate re-record of the three hashes, the frame doubles and the placement goldens, because it removes the dependence at the cause and keeps byte equality as the check. If the re-record is judged too costly now, D is the cheapest honest stopgap: it changes no recorded hash. B is not recommended as the primary fix (it hides the cause), and C is not recommended.

## Open questions for the computational-geometry reviewer

1. Is replacing the cosine by `double.CosPi(index / 80.0)` an acceptable change to the spacing of record (it is also a slightly different, more accurate abscissa than `Math.PI * index / 80.0`)? What accuracy bound should be stated?
2. For naca-4412, are managed sin/atan/atan2 required, or can the generator use a closed form that needs only `+ - * / sqrt`? (Atan of slope and Sin/Cos of theta can be replaced by `slope / sqrt(1 + slope^2)` and `1 / sqrt(1 + slope^2)`, which are exact-operation only; the sine/cosine of the frame angle likewise from `(mx, my) / length`. **Inferred** and unmeasured: this would also change bytes.)
3. Is a tolerance (option D) acceptable for a catalog entry that other geometry derives from, or must regeneration be bit-exact?
4. Should the placement golden (`placement-surface-bits.txt`) be a bit golden at all, given it crosses the same CRT dependence?

## What was not done

- No Windows run: the actual Windows cosine bits at all 81 indices are unknown (one cheap PC job closes this).
- naca-0012 and naca-4412 were not observed failing on Windows (masked by the first throw).
- The surface-bits failure was not reproduced by perturbation.
- No fix; no change to any file outside this document.
