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

Ring: readiness (about 3.5 s for all five checks). They are not in the every-push ring. Measured 2026-10-08 under load 9 to 12: the baseline
Analysis parts (main at 357155e8, `tools/run-tests.sh`) take 4914 ms and 4849 ms against the 5000 ms C-2 limit; this group in the fast ring (first as five checks,
then as two) pushed the parts to 5.2/6.6 s and then 5.1/5.2 s, so C-2 failed. The baseline also fails C-3 (50733 ms) at that load, so the ring has
no headroom now. Command (add `--readiness`):
`CFD_TEST_ONLY=TipCavitation_ tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll --readiness`
(`red-run-1.txt` and the mutant logs ran in a fast-ring group, before the move; the checks are the same.)

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

## Ruling 144 follow-up

Ruling 144 (quoted): "(1) Geometric unavailability at any station, tip included (ANA-CAV-SURFACE-PIERCING, ANA-CAV-DEPTH-NOT-SET), makes the wing cavitation line Unavailable, as under Ruling 86 before the Ruling 142 fix. Ruling 142 excludes only the verdict that rests on a tip strip's alpha_eff, never a geometric unavailability. A test pins it: a surface-piercing tip station makes the wing line Unavailable. (2) The wing cavitation suffix names its noun: "; N stations Not judged — tip strip" (new DESIGN.md row citing this ruling). The run verdict line (AnalysisProjection.cs:409) keeps its strip count. Also, under Ruling 142 (3): on a tip station the colour bar ends and the Cp chart axis extent are rounded outward to a fixed step, so neither prints Cp_min."

| Item | Evidence |
|---|---|
| (1) red, committed before the fix (f130e95e) | `red-run-3-piercing.txt`: `TipCavitation_PiercingTipStation_WingLineIsUnavailable_Ruling144` fails with "geometric unavailability at the tip makes the wing line Unavailable expected Unavailable; actual Clear". The fixture asserts first: the tip rises 1.34 mm above the inner station (alpha -3), a depth between them pierces only the tip, and the judged station reads Clear. Cause: `SectionTier.cs` checked only judged screens for Unavailable. |
| (1) green | The same check passes: wing line Unavailable with reason ANA-CAV-SURFACE-PIERCING, `TipNotJudgedCount` still 1. The all-tip override now applies only when the wing screen is not already Unavailable. |
| (2) | DESIGN.md row COPY-410 (`check-copy-ids`: 412 rows). `Labels.TipNotJudgedSuffix` reads "; N stations Not judged — tip strip". The Section line, the Analysis panel Cavitation row and the V_crit note use it; the run sentence (COPY-252) is untouched. Three test assertions updated to "; 1 stations ...". |
| (3) | `ChartPlot.ExtentStep` (0.5 on a tip station) and `ChartPlot.RoundOut`; `SectionChartView` rounds the Y extent outward with no pad; `SectionProfileView` rounds the colour-bar range outward. Checks: Analysis `TipCavitation_TipStationDisplay...` (step set on tip, null on interior, -0.46 to -0.5, 0.99 to 1.0) and Desktop `SectionProfile_TipStation...` (tip bar 1.0, interior 0.99). No new capture was taken for this step. |
| (4) | `AnalysisChecks.cs` tuple pair split onto two lines. |

All new checks are in the readiness ring.
