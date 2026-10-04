---
id: proof-a3a-pre-red-first
title: "A3a PRE red-first receipt"
type: proof-pack
status: active
owner: "@track-a3a-pre"
phase: implementation
tags: [a3a, pre, analysis, architecture, placement, red-first]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: relates-to }
review-by: "2026-11-04"
summary: >-
  The red runs of the A3a PRE track: the architecture check turned red by a planted ProfileAt call and by a planted
  edit verb, the PlacedSection.Assignment check red before the station-index fix, and the checker self-test case red
  on a mutant that drops the new section flags.
---

# A3a PRE red-first receipt

Design: `docs/design/area3-analysis.md` §18.2 (PRE row), §18.8. Session `a3a-pre`, branch `feature/a3a-pre`, 2026-10-04.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| `Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt` | `session.ProfileAt(0)` planted in `AnalysisService.EvaluateAsync` | `FAIL Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt InvalidOperationException: Core edit verbs or ProfileAt referenced by CfdWorkbench.Analysis: AuthoringSession.ProfileAt; expected 0; actual 1` | mutant removed, `PASS` |
| same | `session.BeginSectionDraft("d", 0)` planted in the same place (the `Begin*` prefix rule) | `… referenced by CfdWorkbench.Analysis: AuthoringSession.BeginSectionDraft; expected 0; actual 1` | mutant removed, `PASS` |
| `Placement_Surface_AssignmentIsTheStationIndex` (Core) | the shipped code: `assignment = stationProfiles[index]` | `FAIL Placement_Surface_AssignmentIsTheStationIndex InvalidOperationException: Expected 0:0 1:1; actual 0:0 1:0` | `assignment = index`, `PASS` (commit `1b42ee6`) |
| `check-named-tests.py --self-test` case "--track-section/--named-sections route through main()" | `main()` stops passing the flags to `check()` | `SELFTEST FAIL --track-section/--named-sections route through main(): flagged exit 1 'FAILED: design line 3: … which the track table (## 14.) does not define` · `SELFTEST 8/9 cases` | the real script, `SELFTEST 9/9 cases` |

The architecture check reads the member-reference table of the built `CfdWorkbench.Analysis.dll` (System.Reflection.Metadata);
it reads no repository file. It bans `ProfileAt`, `Cancel`, `Undo`, `Redo` and the `Begin*`, `Update*`, `Apply*` prefixes on
members of `CfdWorkbench.Core` types only, so `CancellationTokenSource.Cancel` stays allowed for latest-wins cancel.

The surface-bit pin (`Fixtures/m12c/display/placement-surface-bits.txt`) hashes `PlacedSection.Assignment`. It moved for
exactly the three fixtures whose root and tip share one profile (`c0-peak`, `chord-2m`, `example`); the two whose stations
use distinct profiles (`blended-dihedral`, `blended-peaks`, where station index equals profile index) kept their hashes.
So only the integer changed, never a coordinate.
