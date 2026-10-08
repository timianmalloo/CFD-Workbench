---
id: proof-tcv-red-first
title: TCV red-first receipts for Ruling 142
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [tcv, red-first, ruling-142]
links:
  - { to: proof-tcv-governing, rel: refines }
review-by: 2026-12-31
summary: >-
  Each Ruling 142 behaviour has a check that failed on the old code and passes on the new, and two mutants that make the checks fail again.
---

# TCV red-first

Ruling 142 (quoted, `docs/notes/rulings.md`): "(1) A tip-provisional strip (StripLoad.TipProvisionalReason) never decides the wing cavitation verdict:
governing-station selection (the 200-panel pass, the 400-panel candidate set and Cavitation.SelectWing) uses judged stations only, and the station count in COPY-304
counts judged stations only. [...] (2) When tip-provisional stations are excluded, the wing cavitation line appends "; N Not judged — tip strip" [...]. If every
station is tip-provisional, the wing line reads Not judged — tip strip. (3) For a selected tip-provisional station, the station-table cavitation word, the profile
cavitation line and the estimator -Cp_min row show Not judged — tip strip, with no sigma number and no -Cp_min number on that station."

Ring: the two synthetic tier checks run every push (Analysis harness group `TipCavitation`, about 0.15 s). The three checks that need the shared lattice run
and the example-wing check run in the readiness ring (about 3.3 s together); the full ring's C-2 gate (5 s per Analysis part) rejected them in the fast ring
(parts 5.2 s and 6.6 s, load 9.2), so they moved. Command (add `--readiness` for the readiness checks):
`CFD_TEST_ONLY=TipCavitation_ tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`
(In `red-run-1.txt` and the mutant logs all checks ran in the one fast group, before the move.)

## Red, on the old selection code (`red-run-1.txt`)

The new members existed as inert properties (always false / 0), so the checks compiled and failed on behaviour. All four failed:

| Check | Item | Old-code failure |
|---|---|---|
| `TipCavitation_PlantedTipStation_NeverGovernsTheWing` | 1 | "the tip-provisional station is not the governing station expected 0.5; actual 1" (its control line, no flag, governs at eta 1, passed first) |
| `TipCavitation_EveryStationTip_WingLineIsNotJudged` | 1 | "no judged station, no verdict expected Unavailable; actual Clear" |
| `TipCavitation_Exclusion_WingLineStatesCountAndJudgedStations` | 2 | "eta 1 reads the tip strip expected 1; actual 0" |
| `TipCavitation_TipStationDisplay_NoSigmaAndNoCpMinNumber` | 3 | "estimator -Cp_min row expected Not judged — tip strip; actual 5.61" |

## Green (`green.txt`)

After the fix, five checks, `RESULT failures=0` (the fifth, `TipCavitation_ProjectionRowsAndBand_FollowTheRule`, covers the Analysis panel rows and the V_crit
band row of item 4).

## Mutants, planted after the fix was committed (`mut1.txt`, `mut2.txt`), then reverted

| Mutant | Result |
|---|---|
| `Judged(int)` always true in `SectionTier.Evaluate` | 2 checks fail (planted station governs; the count and the governing eta) |
| the `Cavitation` row in `AnalysisProjection.SectionRows` loses its suffix | 1 check fails ("Cavitation row: Clear — ... expected True") |
