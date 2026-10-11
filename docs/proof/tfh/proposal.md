---
id: proof-tfh-proposal
title: "Track TFH - why the run settings hash differs between Windows and macOS, and the options"
type: doc
status: draft
owner: "@trk-tfh"
phase: design
tags: [determinism, settings-hash, run-key, crt-golden]
links:
  - {to: proof-tfh-red-first, rel: relates-to}
review-by: 2027-04-01
summary: >-
  Diagnosis (Mac-side, Verified where observed), a PC diagnostic, three fix options with run-key consequences, and a recommendation (c: managed CosPi stations, one stale-once).
---

# Track TFH, phase 1: the settings hash differs between Windows and macOS

Labels: **Verified** = I observed it on the Mac in this session or it is in the PC's raw log. **Inferred** = a model or a claim about Windows I did not observe. Nothing under `src/` was changed.

## 1. Result first

- **Verified (PC log):** `TeFloor_B10_SettingsHash_GoldenUnchanged` fails on Windows x64 at its first `Equal`: expected `9713863597...ad62`, actual `752ebf3bb5...d865`.
- **Verified (Mac):** every hashed field except the section stations is a constant or authored value. The stations are the only field that passes through a C-runtime transcendental (`Math.Cos`, `Settings.cs:79`, `:86`).
- **Verified (Mac):** .NET's `Math.Cos` on this Mac returns the platform libm value, and that value is not the correctly rounded one at one of the 65 default span arguments (index 91 of 128: libm `bfe3affa292050b9`, correctly rounded `...b8`, error 0.579 ulp). Station `etas[54]` of the default settings carries exactly that libm value (`3fe3affa292050b9`, read from the new diagnostic).
- **Verified (repo, Ruling 163):** on the PC, `Math.Cos` differs from the Mac at 5 of 81 test indices, and `double.CosPi` is bit-identical across the two hosts. This is the same class as `CRT-GOLDEN` (Ruling 154, 156, 157).
- **Inferred:** the Windows hash differs because at least one of the 70 default stations differs in its last bit. The PC log stops at the first `Equal`, so the `SmallHash` and `NoStationsHash` goldens were never reached on Windows. The new diagnostic (item 2) answers this on the next PC run.
- **The gap that let it through:** `tools/check-crt-transcendentals.py` scans an explicit file list (`FILES`) and `src/CfdWorkbench.Analysis/Settings.cs` is not on it. The recorded hash is a platform-keyed authority that the gate did not cover.

## 2. How the hash is built (Verified, read from source)

`RunRecord.SettingsHash(settings)` (`src/CfdWorkbench.Core/RunRecord.cs:214`): serialise the `RunSettings` record with `System.Text.Json` (camelCase), rebuild it as an object tree (`Canonical`, `:336`), write RFC 8785-style JCS (`Jcs.Write`, `Identity.cs:61-`), hash with BLAKE3.

- Doubles are written by `Jcs.Number`: `ToString("R", InvariantCulture)`, lowercased, re-laid out to the ECMAScript form. `R` is the shortest round-trip form in .NET Core 3.0 and later, and the culture is fixed. So the text is a pure function of the 64 bits. A one-bit difference in a double changes the text.
- BLAKE3 is specified bit-for-bit; I found no platform input in it.

| Hashed field | Origin | Platform math |
|---|---|---|
| `nSpanPerHalf`, `nChord`, `wakeSpans`, `ncrit` | authored / constant | none |
| `spanSpacing`, `chordSpacing`, `wakeDirection`, `envelope`, `surfaceState` | authored / constant | none |
| `singularityCutoff` (1e-8), `teFloorMm` (0.3) | constant | none (literals) |
| `polar` | authored (null on default) | none |
| `sectionEtas` | **derived** (`Settings.SpanEtas`) | `Math.Cos(Math.PI * i / n)`, plus `0.5 * (a + b)` and a clamp (exact IEEE ops) |
| `sectionXs` | **derived** (`Settings.ChordXs`) | `Math.Cos(Math.PI * k / n)`, plus `+ - *` (exact IEEE ops) |

`WithStations` keeps stations a caller set, and then they are authored. The default and the product method (`ProductWingMethod.cs:35`) use the derived ones.

## 3. What the Mac can and cannot say about the other two goldens

On the Mac all three goldens pass and the three canonical texts are stable (new check `TeFloor_B10_SettingsHash_DiagnosticTextIsTheHashedText`). The `NoStationsHash` text holds no transcendental and no derived double: it is `{"chordSpacing":"cosine","envelope":"vlm-envelope/1","nChord":4,"nSpanPerHalf":64,"ncrit":[2,4],"polar":null,"singularityCutoff":1e-8,"spanSpacing":"cosine","surfaceState":"clean","teFloorMm":0.3,"wakeDirection":"+x","wakeSpans":20}`. If the PC reports a match on `NoStations`, the stations are the cause. `SmallHash` (n = 8, 1 chord) is a weaker probe: with exact-pi arguments 3 of its 5 span edges would differ from the Mac values, but whether the Windows libm differs there is unknown (Inferred).

## 4. Diagnostic for the PC's next run (done, test-only)

`tests/CfdWorkbench.Analysis.Tests/TeFloorLabelTests.cs`: the one check is split into `TeFloor_B10_SettingsHash_Golden_Default`, `_Golden_Small`, `_Golden_NoStations`, plus `_DiagnosticTextIsTheHashedText`. A golden that fails prints `DIAG` lines: the canonical JCS text without stations, every station as `index bits(hex) JCS-number`, and the SHA-256 of the full text. No path or host name appears (the runtime id is `osx-arm64` style). The old name `TeFloor_B10_SettingsHash_GoldenUnchanged` is retired; it was never in `tests/expected-failures.windows.json` (the PC log prints `UNEXPECTED ... not in the manifest`), so that manifest needs no edit. The PC's WRX gate that compares check names to the manifest should see four new names, all expected to pass or fail on their own merit.

Rerun on the PC (Windows form is Inferred; the harness reads `CFD_TEST_ONLY`):

```
$env:CFD_TEST_ONLY='TeFloor_B10'; dotnet run -c Release --project tests\CfdWorkbench.Analysis.Tests
```

The `DIAG` lines print only when a golden fails. For the Mac side of the diff, the leader runs the same check on the Mac with one golden constant edited (a scratch edit, as in `red-first.md`); the Mac reference lines are committed as `docs/proof/tfh/mac-default-diag.txt` (default settings, 70 stations). The first differing `bits` column names the stations and the ulp gap.

## 5. Options

All three leave the numerical solver untouched. They differ in what changes for saved runs. Two facts first (Verified, `RunRecord.cs:239-247`, `Freshness.cs:46-50`):

- A stored run holds its own derived stations inside its `Settings`, and its stored `SettingsHash`. `Verify` recomputes the hash from the stored settings and compares. Stored stations are doubles read back exactly, so a run saved on one host verifies on the other. **Integrity is not the problem; freshness is.** The current key is built from this host's freshly derived stations, so a run saved on the other host reads as Historical (out of date).
- Therefore **changing the `SettingsHash` function itself** (options a, b) makes every existing saved run fail `Verify` (stored hash differs from the recomputed one) and show as Unavailable, not merely out of date, unless `Verify` also accepts the legacy form. Changing only how stations are computed (option c) leaves `Verify` intact.

Stored runs do hold the derived values: the stations in `Settings`, and strips (`YLow`, `YHigh`) from `VortexLattice.SpanStations`, which also calls `Math.Cos`. The stored numbers therefore differ by platform too, in the last bits. They are not in the key.

### (a) Hash the generating inputs, not the derived doubles

Hash `{spacing, counts}` and omit stations when they equal the rule's output (compare within the 1e-12 that `Unique` already uses); hash the values only when authored. Default settings then hash to the `NoStationsHash` value, `9b402c74...`, which already passes on the Mac and does not depend on libm.

- Run key on macOS: changes, because the function changes. Needs a dual-read `Verify` (accept the legacy hash) so saved runs stay Intact. With the dual read, `RecomputedKey` uses the new function on the stored settings, so a Mac-saved run reads **Current**, and so does a Windows-saved run: no stale at all. Without the dual read: every saved run Unavailable.
- Stored values: unchanged, still platform-specific in the last bits.
- Numerical result: none.
- Cost: a "derived vs authored" rule in the hash, a dual-read path to retire later (expand, migrate, contract), a persisted-format decision for the data lens.
- Tests: golden for the new default hash (platform-free); the legacy hash of a saved fixture still verifies; stations differing by 1 ulp hash equal; stations differing by more than 1e-12 hash differently; a Mac-written project with a 1-ulp-shifted station reads Current.

### (b) Canonicalise derived doubles before hashing

Round each station to a fixed number of digits (for example 12 significant) in the hash only.

- Run key on macOS: changes (function changes); same dual-read need as (a).
- Not structurally safe (Inferred model): a value within about 1e-16 relative of a rounding boundary still splits across hosts. With about 70 stations and 12 digits the chance is roughly 1 in 70 for the default grid; it is deterministic, so a golden on both hosts either passes or not, but it is a hidden hazard for any other grid. It also makes the hash blind to real tiny differences.
- Numerical result: none. Tests: as (a), plus a near-boundary case. Not recommended.

### (c) Compute stations without the platform math

Replace `Math.Cos(Math.PI * i / n)` by `double.CosPi(i / (double)n)` in `Settings.Edge` and `Settings.Fraction`, the project's existing remedy (Ruling 156 P1, Ruling 157, Ruling 163: managed code, bit-identical across hosts at the 81 tested indices). `i / n` is exact for `n` a power of two; the argument is exact for any `n` as far as a double allows.

- Run key on macOS: changes **once**. The stations move by 0 to 1 ulp at 39 of the 65 default span edges and 2 of the 5 chord abscissae (Verified by exact-arithmetic comparison against the libm values, `scratch/tfh/cospi.py`; the libm-equals-Mac-.NET step is Verified only at index 91). The settings hash function is unchanged, so every saved run stays Intact (`Verify` passes) and reads Historical (out of date) until it is re-run. **Operator-visible:** after the update, analysis results saved earlier show as out of date once; re-running makes them current. There is no migration that keeps them Current (the keys really differ). A Windows-saved run goes the same way.
- After the change both hosts hash the default settings alike, if `CosPi` is bit-identical at these arguments (Inferred beyond Ruling 163's `i/80`): that needs a bit golden of the stations at `/128`, `/4` and `/8`, which the PC ring confirms.
- Numerical result: stations move at the 1e-16 level, so strip positions and results move at that level. Existing lattice and strip fixtures must be checked for bit goldens (Inferred: they use tolerances; measure in phase 2).
- Coherence: `VortexLattice.SpanStations`, `VortexLattice.Fractions` and `Loads.cs:109` call `Math.Cos` for the same spacing. After (c) the key-side stations and the lattice-side stations differ by an ulp. Lookups I found are nearest-match (`MinBy`), not equality (Inferred: no equality lookup exists; confirm in phase 2). The numerical lens should rule whether to move all three to one helper; that would shift the strip and load numbers by 1e-16 and is a separate step.
- Tests: red-first golden on the three settings (the new split checks, re-recorded), a stations bit golden like `spacing-cospi-80.tsv`, extend `FILES` in `tools/check-crt-transcendentals.py` to `Settings.cs` with `ALLOWED_COUNT` 0 (the control for the class), the PC ring.

## 6. Recommendation

**(c)**, in this order: (1) the PC rerun above to confirm the stations are the cause (cheap, no code); (2) `CosPi` in `Settings.cs`, re-record the two station goldens, add the stations bit golden, add `Settings.cs` to the CRT gate; (3) the operator accepts one stale-once. Reasons: it is the project's own ruled remedy for this class (156/157/163), it fixes the cause rather than hiding it, it does not touch the hash function, the persisted format or `Verify`, and the cost is one visible re-run.

Why not (a) first: it also removes the dependence, and it would keep runs Current, but it changes the persisted hash contract and needs a dual-read that must later be retired; that is a bigger step than the problem needs while there is a single user. It stays available if the operator prefers "no stale at all" and accepts the format work. If (a) is chosen later, (c) is still worth doing for the stored values.

## 7. Operator questions

1. **User-visible:** after the fix, analysis results saved before the update show as out of date once and need a re-run. Acceptable? (Recommendation: yes.)
2. Should the lattice and load code (`VortexLattice.cs:283,294`, `Loads.cs:109`) move to the same managed cosine in a later step, so there is one definition of the spacing?

## 8. Sweep: other keys and hashes in `src/` that serialise a computed double

| Site | What is hashed | Computed doubles | Verdict |
|---|---|---|---|
| `RunRecord.SettingsHash` (`:214`) | settings | derived stations | **At risk** (this finding) |
| `RunRecord.Key` (`:187`) | surface/profile hashes, water, op, method, settings hash | water `Rho/Nu/Pv`: `Blend` uses `+ - *` on table values (`WaterTable.cs:72-75`); op fields authored | Safe given settings hash; water arithmetic is exact IEEE |
| `RunRecord.ContentHash` (`:218`) | the whole stored row | strips, loads, results: lattice `Math.Cos/Sin/Atan2` | Safe for integrity (hashes the stored bytes, not a recompute); not comparable across hosts, by design |
| `FoilSource` surface and profile identity (`FoilSource.cs:28,55`) | the authored `Semantic` tree | authored control points; New-project default uses `CosPi` (Ruling 157) | Safe (already controlled, on the gate list) |
| `RunPolarResolver.FromPlaced` (`RunPolarResolver.cs:37`) | section `x`, `upper`, `lower` from `PanelMethod.SampleSections` | `PanelMethod.cs:44` `Math.Cos(Math.PI * i / half)` | **At risk (Inferred)**: this hash becomes `NeuralFoilSection.ProfileHash`, the polar sample's profile key. Not measured on the PC. Phase 2 should add the same diagnostic for it |
| `NeuralFoilPolarSource` water hash (`:107`) | water record | exact-IEEE blend | Safe |
| `WaterTable` BLAKE3 (`:33,41`) | committed CSV bytes | none | Safe |
| `Catalog`, `Placement`, `SectionReplace`, `DatImport`, `SectionLibrary` (`Jcs.Quote`) | strings, bytes | `CosPi` or none | Safe (the `crt-allowed` Atan2 in `Catalog.cs:195` is compared within a tolerance, Ruling 156) |

The Verified `FILES` gap in the CRT gate is the control to close for the class; `RunPolarResolver`/`PanelMethod` is the next candidate for it once measured.
