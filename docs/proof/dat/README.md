---
id: proof-dat
title: Export slice 1 (section .dat) exit evidence and the presentation choice
type: proof-pack
status: in-review
owner: "@track-dat"
phase: implementation
tags: [export, dat, proof, fidelity, te-floor]
links:
  - {to: design-export, rel: depends-on}
  - {to: proof-dat-red-first, rel: relates-to}
review-by: 2026-12-09
summary: >-
  What the .dat slice shows in its first dialog and why (one format row, no disabled STL or 3MF), the committed .dat fixture and its round trip through the
  existing Import .dat path, the dialog screenshot against the mockup's .dat ready state, and the measured test cost.
---

# Export slice 1, section .dat: evidence

Track `trk-dat`, base main `affddb89`, 2026-10-09. Authority: [docs/design/export.md](../../design/export.md), Rulings 193 to 196.

## The presentation choice (design 6.2)

The design draws a format list of Section (.dat), Wing (STL), Wing (3MF) and a disabled STEP row. This slice writes only the .dat, and the brief says not to show STL or 3MF as
disabled options. The choice, the smallest honest one:

- **The list stays, with one row: Section (.dat).** It is a listbox with one selected item, so the dialog's frame (list on the left, that format's options on the right, the fixed
  summary under them) is the final one and STL and 3MF are two more rows when their slices land, with no change to the layout, the tab order or the keys.
- **The STEP row stays**, disabled, carrying the spec string (COPY-481, "STEP export unavailable until the open-and-measure fixture exists"). It is not an STL or 3MF option, the spec
  requires the string, and it tells the user why a CAD format is missing. It is a disabled button with the reason as its help text.
- **Nothing says STL or 3MF** anywhere in the dialog (a check reads every text in the window for "STL", "3MF", "Wing", "Tolerance" and "Starboard").
- A format list of one row is not a choice, but a heading in its place would be a second layout to undo. The cost is one list row of height.

## The committed fixture and its round trip

[`basic-foil-root-r1.dat`](basic-foil-root-r1.dat) (the same bytes are `tests/CfdWorkbench.Core.Tests/Fixtures/export/basic-foil-root-r1.dat`, compared byte for byte by a check): the Example foil
(`src/CfdWorkbench.Desktop/Assets/example.foil`, closed trailing edge), station Root, At station, Selig, 101 points per surface (201 in the file), revision r1. 120.00 mm chord, station t/c 12.0 %,
trailing edge 0.0000 mm, deviation 0.00740 mm. First rows: `Basic foil | Root | r1`, `1 0`, `0.9997532801828658 0.0000262953793382983`. No exponent anywhere.

Re-imported through the existing Import .dat path (`DatImport.Parse`, then the `DatImport.Fit` a Replace runs), measured by `DatExport_ExampleFixture_RoundTripsThroughImportWithinDeviation`:

```
MEASURE dat_roundtrip_max_dy_chord=3.727E-006 deviation_chord=6.166E-005 fit_residual_chord=3.727E-006 vertices=14
```

The largest vertical difference between the refitted profile and the section the file holds, on 2001 chord stations, is 3.7e-6 chord (0.45 micrometre at 120 mm). The stated deviation is 6.17e-5 chord
(7.4 micrometre at 120 mm), so the round trip is within it, by a factor of 17. The fit accepted the file at 14 vertices. The check asserts the difference is at most the stated deviation plus the fit residual.

## The dialog against the mockup

[`dialog-dat-ready.png`](dialog-dat-ready.png) is the built dialog (rendered by `Export_Dialog_Renders_ReadyState_Screenshot` with `CFDW_EXPORT_SHOT` set), in the mockup's `.dat` ready state:
the Example foil with an open 0.26 mm trailing edge, Root, At station, Selig, 101 points. The mockup's state is [`docs/mockups/export/dat-ready-A-light.png`](../../mockups/export/dat-ready-A-light.png).

Same: the title, the format list and the STEP row with its reason, the shape, station, order and point-count options and their labels, the shape help (12.0 %), every summary row (revision, units, chord at Root,
points, trailing edge with its two lines, fidelity), the limit line, the first and last file rows (the real values `1 0.0010655364151166689`, `0.9997532801828658 0.0010892048507038603`, `0 0` and
`0.0002467198171342 -0.0024037402789592885` are the mockup's), the advisory band with its jump "Show at Root", the safety string, and the Cancel and Export… buttons with Export… as the accent default.

Different (Verified by looking at both images): the mockup draws the dialog over a dimmed app window with its menu bar; the built shot is the dialog window alone. The Export… button is drawn with the dialog's
own primary style (the app has no shared primary style for plain buttons). The revision reads r1 where the mockup's r12 is illustrative. Not checked: the dark theme and high contrast (the dialog uses only
token brushes; the operator ranked accessibility proof below function and look).

## Tests and their cost

`tools/run-tests.sh`, Release, exit 0 (the third run of the ring; the first found the one failure in red-first.md section 4, the second ran before the B9 peak search was made faster):

```
RING-LOCK waited 0 s
== CfdWorkbench.Core.Tests 1/3 28 s (28113 ms), 341 PASS
== CfdWorkbench.Core.Tests 2/3 30 s (30356 ms), 202 PASS
== CfdWorkbench.Core.Tests 3/3 30 s (29422 ms), 205 PASS
== CfdWorkbench.Desktop.Tests 40 s (40094 ms), 734 PASS
== CfdWorkbench.Analysis.Tests 1/2 5 s (4640 ms), 147 PASS
== CfdWorkbench.Analysis.Tests 2/2 5 s (4615 ms), 101 PASS
== CfdWorkbench.Cli.Tests 2 s (1864 ms), 6 PASS
test costs: 0 failures, 0 COST-MISS (load 12.89)
wall 45 s (45586 ms, net 43731 ms) (budget 60 s) cpu 467 s load 3.12 -> 12.89
```

Measured with `CFD_CORE_COST=1` and the `COST` lines: the 17 new Core checks cost 1.7 s together (the slowest, `B9f`, a blended eta, 0.9 s); the 20 Desktop checks 4.4 s (each opens the Example,
about 0.2 s); the 3 Analysis checks 0.2 s. The earlier peak search took 9.9 s for the Core checks (B9f alone 7.3 s) and was replaced by a batch scan and one evaluation per golden-section step. The 17 new Core
checks have no row in `core-costs.tsv` (a missing row costs balance, never coverage; the second ring printed `PARTITION-SKEW` 8.2 s, the third none).

## Not in this slice

STL, 3MF, the `cfdw export` verb (A9's CLI half), the large-mesh state, the preparing and writing states (a .dat is written in under a frame), and the default `CFD Workbench` start folder of the save panel
(`w2-save-picker` is not on this base; the panel starts in the project file's folder when there is one, else where the platform puts it). A symlink at the target is now refused (security review, red-first.md section 5); a symlinked folder the user chose is followed as chosen.

## Follow-ups accepted after the security review (no code now)

- Other path exceptions are not caught (`ExportSession.RunAsync` catches IOException and UnauthorizedAccessException only), and a locked Windows target shows the "no permission" copy.
- The slug has no length cap (the file name is not truncated; the name line is capped at 80).
- The temp file has the default mode (0644 on macOS), the rename drops extended attributes, ACLs and hard links of a replaced file, and a killed process can leave an orphan `.cfd-*.tmp`. All accepted.
- The refused-link and forced-extension refusals reuse the approved write-failure sentence with no cause (COPY-507); a dedicated cause sentence would need copy approval.
