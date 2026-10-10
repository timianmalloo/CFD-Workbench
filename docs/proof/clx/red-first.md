---
id: proof-clx-red-first
title: "CLX red-first receipt"
type: proof-pack
status: active
owner: "@trk-clx"
phase: implementation
tags: [export, cli, red-first, area-7]
links:
  - { to: design-export, rel: depends-on }
review-by: "2027-04-01"
summary: >-
  The red and green runs of the export CLI verb: ten Cli checks that failed while the verb was not wired and pass now,
  and the two exit-code branches no CLI input reaches.
---

# CLX red-first proof: the `export` CLI verb

Track `trk-clx`, branch `feat/clx-cli-export`. Ring: every join (checks run inside `tests/CfdWorkbench.Cli.Tests`).

## Red

The ten checks in `tests/CfdWorkbench.Cli.Tests/ExportCliTests.cs` were written before the verb was wired into
`Cli.RunAsync`. Run: `dotnet tests/CfdWorkbench.Cli.Tests/bin/Release/net10.0/CfdWorkbench.Cli.Tests.dll`, exit 1.
All ten FAIL; the old code answers every `export` command with the `inspect` usage line and exit 2. The lines are in
`docs/proof/clx/red.txt`. Each FAIL names the same cause (`exit 2, expected 0|3|4|5|6|8: Usage: cfd-workbench inspect ...`).

## Green

After `ExportVerb` was wired in: same command, exit 0, ten PASS lines (`docs/proof/clx/green.txt`, with per-check COST lines).

| Check | What it proves |
|---|---|
| `Cli_Export_DatEqualsFixtureAndPrintsSummary` | the `.dat` equals `tests/CfdWorkbench.Core.Tests/Fixtures/export/basic-foil-root-r1.dat` byte for byte; the summary has the revision, fidelity, trailing-edge row with "app default, no source", "Manufacturing: not assessed", limit, safety; no temp file left |
| `Cli_Export_StlPassesClosureCheck` | `StlExport.Check` on the written bytes: closed, no unpaired edges, no zero-area triangles; the Mesh and Size rows print |
| `Cli_Export_HalfNamesFileHalf` | `--scope half --out <folder>` writes `basic-foil-r1-half-mm.stl`; the whole wing writes `basic-foil-r1-mm.stl` |
| `Cli_Export_SymlinkTargetRefused` | exit 6, `EXPORT-TARGET-LINK`; the link and its target are untouched; no temp file |
| `Cli_Export_3mfRefusedToday` | exit 3, `EXPORT-FORMAT-UNAVAILABLE`, "not available yet"; nothing written |
| `Cli_Export_InvalidOptionPrintsUsage` | twelve bad commands (unknown or missing option, wrong format, STL option on `.dat`, free numeric tolerance, bad points, station out of range, repeated option) each exit 2 with `EXPORT-USAGE` and the usage text; nothing written; the general usage names `export` |
| `Cli_Export_TrailingEdgeBelowFloorAdvisesAndWrites` | an open 0.26 mm trailing edge: exit 0, the Advisory line, the file is written (Ruling 194 (5)) |
| `Cli_Export_GeometryNotAcceptedRefused` | `invalid-geometry.foil`: exit 4, `DSL-NOT-ASSESSED`, nothing written |
| `Cli_Export_ForcedExtensionNeverReplaces` | `--out x.txt` with `x.dat` present: exit 8, `x.dat` kept; with no `y.dat`, `y.txt` writes `y.dat` |
| `Cli_Export_UnwritablePathExitsIo` | a missing folder: exit 5 with the dialog's "Can't write the file" sentence |

## Round 2 (Rulings 203, TMF merged)

Main (TMF, `3515c44f`) was merged first. `ExportSession.cs` and `ExportCopy.cs` in `src/CfdWorkbench.Analysis/Export/` were
identical to main's old-path versions (`diff` printed nothing), so no TMF change was lost.

Red (`round2-red.txt`, before the CLI change; exit 1): `Cli_Export_3mfPassesPackageCheckAndHalfNamesFile` failed (exit 3,
`EXPORT-FORMAT-UNAVAILABLE`), `Cli_Export_StationTakesTheAppsNames` failed (`--station` needed a whole number), and
`Cli_Export_InvalidOptionPrintsUsage` failed (the unknown station printed no usage text and no names).
Green (`round2-green.txt`, exit 0): eleven Export checks PASS.

| Check | What it proves |
|---|---|
| `Cli_Export_3mfPassesPackageCheckAndHalfNamesFile` | the written package passes `ThreeMfExport.Check`; `--scope half` names `...-half...3mf` |
| `Cli_Export_StationTakesTheAppsNames` | every station of the example, by `StationNames.Choices`, writes the file the app's name gives; `TIP` works; the default is the root; `0`, `1`, `99`, `-1`, `middle` exit 2 and list `root` and `tip`; the numbered label resolves on a three-station set (`2` is the middle, `1` and `3` are not) |

The station naming rule is `CfdWorkbench.Core.StationNames`. The Desktop sites (`ElevationView`, `PointsView`, `View3d`,
`PropertiesView` heading) and `SectionEdits` now call it, and so does the CLI; the label text is unchanged.
The Example has only a Root and a Tip, so the "Station n" label is checked on a three-station list, not through a CLI run.

## Limits

- The `EXPORT-GEOMETRY-NOT-ACCEPTED` guard inside `ExportAsync` is not reached by any CLI input today: opening a foil refuses
  uncertified geometry first (exit 3 or 4 by its code). The guard keeps the session's own rule (H2) from turning into a
  wrong exit 5 if the opener ever admits one. Not tested through the CLI.
- `EXPORT-NOT-CLOSED` (exit 7) is not reached by any real input: the closure check passes on every wing the writer builds.
  The dialog's check drives it with an injected builder; the CLI has no such seam. Not tested.
