---
id: proof-lsp-red-first
title: "Track LSP - one definition of the cosine spacing, red and green runs"
type: doc
status: draft
owner: "@trk-lsp"
phase: build
tags: [determinism, crt-golden, red-first, lattice]
links:
  - {to: proof-tfh2-red-first, rel: relates-to}
review-by: 2027-04-01
summary: >-
  Lattice span and chord stations and the strip-width fallback now share Settings' double.CosPi spacing; bit-equality checks and the CRT gate extended (macOS arm64, base 4dc824ef).
---

# LSP red-first proof (Ruling 214 item 3)

Host: macOS arm64, .NET 10, Release. Base main 4dc824ef. Track `trk-lsp`. Verified = observed on this host; the Windows claim is Inferred until the PC run below is green.

## What changed
- New `src/CfdWorkbench.Analysis/Spacing.cs`: `Spacing.Edge(index, n, spacing)` and `Spacing.Fraction(index, n, spacing)` (cosine on `double.CosPi`, uniform as before). `Settings.cs` lost its private `Edge`/`Fraction` and calls these.
- `VortexLattice.SpanStations` cosine: `mid + half * Spacing.Edge(i, n, "cosine")` (was `mid - half * Math.Cos(Math.PI * i / n)`; negation is exact, so the same value with CosPi). `VortexLattice.Fractions` cosine: `Spacing.Fraction`. `Loads.StripWidth` fallback: `Spacing.Edge`.
- Not changed: the uniform branches of `SpanStations` and `Fractions` (their own formulas are unchanged, so uniform bits do not move).
- One behaviour difference: `Loads.StripWidth`'s fallback treated any non-"cosine" spacing as uniform; it now throws `ArgumentOutOfRangeException` for an unknown spacing. `Settings` and the lattice already refuse such a value, so no stored run reaches it.
- `tools/check-crt-transcendentals.py`: FILES gains `VortexLattice.cs` (pin 7) and `Loads.cs` (pin 0). The seven flow-angle solver lines (177, 389, 397, 404, 427, 428, 429: `Math.Cos/Sin(alpha|twist)`, `Math.Atan2`) carry `crt-allowed: Ruling 214` and are compared under a tolerance, never a key. The pin is per line (the brief names 177, 389, 427; 428 and the three `Atan2` degree lines are the same class).

## Red on main (old code, new tests), then green
Old code, `CFD_TEST_ONLY=StationGolden`, exit 1:
- `StationGolden_LatticeSpanStations_EqualSettingsEtasBits` FAIL (`lattice span station 65/128`)
- `StationGolden_LatticeChordFractions_EqualSettingsChordXsBits` FAIL (`lattice chord fraction 2/4`)
- `StationGolden_LoadsStripWidthFallback_UsesCosPiEdges` FAIL (`strip width 1/4 bits expected 4598766861255926539; actual 4598766861255926538`, one ulp)
- the four earlier `StationGolden_*` checks PASS.

New code: all seven `StationGolden_*` PASS (exit 0).

CRT gate with the two files added and the old spacing lines, exit 1: `VortexLattice.cs:283,294` and `Loads.cs:109` flagged (plus the seven solver lines before they carried markers, and `0 allowed marker(s), the gate pins 7`). After: exit 0, "8 file(s) clean". `--self-test` exit 0.

## Goldens and moved asserts
Moved asserts: **none**. No exact assert changed to `Near`, no tolerance changed, no golden re-recorded. The full ring (F17 at 1e-12 relative, strip, load, cavitation and panel fixtures) ran green on the new code. No result moved by more than 1e-12 relative.

## Saved runs (item 5)
`RunRecord.Key` hashes the manifest of surface, profiles, water, op, method and `SettingsHash`; `SettingsHash` hashes `RunSettings` (stations are the section stations, built by `Settings`, untouched here). The lattice's span and chord stations are not an input to either. `RunRecord.Verify` checks `ContentHash` (over the stored row), `SettingsHash` and the recomputed key; it never re-runs the lattice. Nothing compares a stored strip against a recomputed lattice: `VortexLattice.SpanStations` has one product caller (`VortexLattice.cs:93`). So saved runs keep their key, `Verify` and Freshness. Stored strips carry their own `YLow`/`YHigh`; only older runs without them use the `StripWidth` fallback, which can differ by 1 ulp from before.

## Runs
- `tools/run-tests.sh`: exit 0, "all test harnesses passed", six `RESULT failures=0`.
- Desktop `--readiness`: exit 0, 96 PASS, 0 FAIL (Desktop source not changed).
- `run-verify-gates.py --skip verify-application-core.py verify-application-adapters.py verify-windows-store.py`: exit 0, "10 gate(s) passed". `tools/run-windows-store-gate.py`: exit 0, NOT ASSESSED.

## PC rerun line (claim gate, not a merge gate)
```
$env:CFD_TEST_ONLY='StationGolden'; dotnet run -c Release --project tests\CfdWorkbench.Analysis.Tests; Remove-Item Env:CFD_TEST_ONLY
```
Expect `PASS` for all seven `StationGolden_*` checks. The three new checks compare two uses of `double.CosPi` on the same host, so they hold on Windows if `StationGolden_CosPiBits_MatchFixture` does.
