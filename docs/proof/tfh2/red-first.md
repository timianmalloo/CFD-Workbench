---
id: proof-tfh2-red-first
title: "Track TFH2 - CosPi stations and panel grid, red and green runs"
type: doc
status: draft
owner: "@trk-tfh2"
phase: build
tags: [determinism, settings-hash, red-first, crt-golden]
links:
  - {to: proof-tfh-proposal, rel: relates-to}
review-by: 2027-04-01
summary: >-
  Settings stations and the panel grid move to double.CosPi; DefaultHash re-recorded, station bit golden added, CRT gate extended (macOS arm64, base 4a110476).
---

# TFH2 red-first proof (Ruling 214, Ruling 215)

Host: macOS arm64, .NET 10, Release. Base main 4a110476. Track `trk-tfh2`. Verified = observed on this host; the Windows claim is Inferred until the PC run below is green.

## What changed
- `Settings.cs` `Edge` and `Fraction`: `double.CosPi(index / (double)n)`, same algebra (`-CosPi`, `0.5 * (1 - CosPi)`).
- `PanelMethod.cs` `SampleSections`: `(1 - double.CosPi(i / (double)half)) / 2`.
- Not touched: `VortexLattice.cs`, `Loads.cs`.
- `tools/check-crt-transcendentals.py`: FILES gains `Settings.cs` (pin 0) and `PanelMethod.cs` (pin 5, see Deviation).
- New: `Fixtures/stations/station-cospi.tsv` (445 rows, sets n = 4, 8, 100, 128, 200), `StationGoldenTests.cs` (4 checks), `Freshness_OldStationRule_HistoricalOnce_VerifyPasses_CurrentAfterRerun`.

## Re-recorded goldens (old and new)
| Golden | Old | New |
|---|---|---|
| `DefaultHash` | `9713863597250cd245c4d70560abee8840c002f6ff33501127ba331ac8efad62` | `54bbba427a9fd039446415d85976f6abc4fe5a5f8bb4da22746464d44a9bd831` |
| `SmallHash` | `aa39f244f4235616ded920b58a1599a768765de16f74ba76e225cbe9c81543d7` | unchanged (on this host the small config's stations are identical under both rules) |
| `NoStationsHash` | `9b402c74273eea9dcd0c3ca47cdcced2ec6764536434e462ba837150281fe58a` | unchanged (shown: `TeFloor_B10_SettingsHash_Golden_NoStations` PASS on old and new code) |

No other golden re-recorded. No exact assert on strip or Cp values broke, so no assert became `Near` and no tolerance changed. The full ring ran F17 (1e-12 relative), panel, cavitation and strip tests green.

## Red on the old code (src stashed, tests new), then green
Old code, `CFD_TEST_ONLY=TeFloor_B10` -> exit 1: `FAIL TeFloor_B10_SettingsHash_Golden_Default ... expected 54bbba42...; actual 9713863597...` (Small, NoStations, others PASS).
Old code, `CFD_TEST_ONLY=StationGolden` -> exit 1:
- `StationGolden_CosPiBits_MatchFixture` PASS (the fixture pins CosPi itself)
- `StationGolden_SpanEtas_AreMinusCosPiBits` FAIL (`eta edge 65/128`)
- `StationGolden_ChordXs_AreHalfOneMinusCosPiBits` FAIL (`chord edge 2/4`)
- `StationGolden_PanelGrid_IsOneMinusCosPiOverTwoBits` FAIL (`panel x 4/8`, expected 4602678819172646912, actual 4602678819172646911: one ulp)

New code: all four `StationGolden_*` PASS; all six `TeFloor_B10_*` PASS (`DefaultHash` new value).

CRT gate, `python3 tools/check-crt-transcendentals.py`: red with `Settings.cs` and `PanelMethod.cs` added and the old code (exit 1, 8 findings: Settings.cs:79,86; PanelMethod.cs:44,87,140,166,183,185); green after (exit 0, "6 file(s) clean"). `--self-test` exit 0.

Stale once (Ruling 215), `Freshness_OldStationRule_HistoricalOnce_VerifyPasses_CurrentAfterRerun`: a run saved with stations one ulp off the new rule has `RunRecord.Verify` true, reads `Historical` with what-changed `settings` (not Unavailable), the re-run reads `Current`, the old run stays `Historical`, `Verify` on it stays true. This check also passes on the old code. It characterizes the key and Freshness behaviour that the change relies on, and is not a red-first check. The old rule is modelled as one ulp on one station (`Math.BitIncrement`), because the Mac `Math.Cos` and `CosPi` rows differ only at some points and a model keeps the check host-independent. No dual read, no new copy.

## Deviation to review
The brief and Ruling 214 pin `PanelMethod.cs` at `ALLOWED_COUNT` 0. That file also holds five solver lines with `Math.Pow/Cos/Sin/Atan2/Log` (lines 87, 140, 166, 183, 185) that reach Cp and forces, compared with a tolerance, never a recorded key. Pin 0 is not satisfiable without moving them. They carry `crt-allowed: Ruling 214` markers and the pin is 5. A new unmarked call, or a sixth marker, fails the gate. The grid line (44) has no marker. Alternative if the leader wants 0: move the grid into its own file and list that file.

## Runs
- `tools/run-tests.sh`: exit 0, "all test harnesses passed", wall 51 s (budget 60 s); Core 222 PASS, Desktop 799 PASS, Analysis 44 + 69 + 143 PASS, Cli 19 PASS, `failures=0`.
- Desktop `--readiness`: exit 0, 96 PASS lines, no FAIL (Desktop source not changed).

## PC rerun line (after the build, claim gate, not a merge gate; Ruling 214 (5))
Two prefixes, because `CFD_TEST_ONLY` takes one prefix. Run from the repo root on Windows, in the same Release build:

```
$env:CFD_TEST_ONLY='StationGolden'; dotnet run -c Release --project tests\CfdWorkbench.Analysis.Tests; $env:CFD_TEST_ONLY='TeFloor_B10'; dotnet run -c Release --project tests\CfdWorkbench.Analysis.Tests; Remove-Item Env:CFD_TEST_ONLY
```

Expect `PASS` for the 4 `StationGolden_*` checks and the 6 `TeFloor_B10_*` checks (`_Golden_Default`, `_Golden_Small`, `_Golden_NoStations`, `_DiagnosticTextIsTheHashedText`, `_Numerics_DefaultRecordFieldsUnchanged`, `_Label_OneNamedConstant_AppDefaultNoSource`). On a fail, `DIAG` lines print the station bits. If `StationGolden_CosPiBits_MatchFixture` fails on the PC, copy the cosine kernel into Core (Ruling 156 (2)).
