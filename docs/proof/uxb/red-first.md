---
id: proof-uxb-red-first
title: UXB red-first receipts and the sweep of per-span rows
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [uxb, units, sigma, analysis]
links:
  - { to: proof-sfv-captures, rel: refines }
review-by: 2026-12-31
summary: >-
  Red then green for the Imperial strip lift row and the Section sigma plate, the file:line sweep of every per-span row, and the
  sigma trace showing the band and the Section agree and why the review capture showed otherwise.
---

# UXB red-first

Run: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet <dll>` (Analysis harness; the Desktop checks add `--analysis` or `--readiness`).

## Item 1. Imperial lift per span

| Check | Old code | New code |
|---|---|---|
| `Units_Imperial_NoAnalysisRowKeepsNewtonsPerMetre` (Analysis, fast ring, 0.11 s) | `FAIL ... strip row Lift / span keeps N/m expected True; actual False` | `PASS` |
| `PropertiesPane_Analysis_Imperial_StripLiftReadsLbfPerFt` (Desktop `--analysis`, real ShellHost, station selected) | not run on old code (the Analysis check above is the red) | `PASS`; capture `imperial-lift-per-span.png` reads "Lift / span 3.97 lbf/ft" |

Fix: `AnalysisProjection.StripDetails` and `Layers` take `units`; the row converts with `Labels.ForcePerSpan` and names `Labels.ForcePerSpanUnit`; the
"Lift per strip" layer legend names the same unit. The value test compares the Imperial row with `Labels.ForcePerSpan(metric row)`.

### Sweep of every per-span or force row (Properties and bottom panel)

| Site | Was | Status |
|---|---|---|
| `src/CfdWorkbench.Analysis/AnalysisProjection.cs:338` strip "Lift / span" row (shown in Properties) | N/m always | fixed |
| `AnalysisProjection.cs:446` "Lift per strip" layer legend (Layers pane, 3D and Plan legends) | "N/m" always | fixed (the sample values stay N/m; they draw arrows and print no number) |
| `src/CfdWorkbench.Desktop/Analysis/LoadingChart.cs:65` Spanwise loading table twin, column header "L/span N/m" with `LoadingPoint.LiftPerSpan` values (`AnalysisProjection.cs:428`) | N/m always | NOT fixed: the file is not in the track's owned list, and the control is not told the units (`LoadingChart.Update(view.Loading)`). Needs a unit on `LoadingPoint` or a `Units` argument, and the conversion at `AnalysisProjection.cs:428` |
| `AnalysisProjection.cs:90, 134, 279-291, 478-479` Force and drag rows (lift, induced, wing drag) | already "lbf" or "N" by `units` | clean |
| `AnalysisProjection.cs:97-105` speed rows | already kn or m/s | clean |
| `SectionDisplay.cs:402-423` strip table (L', M', D') | already follows `Labels.*PerSpan` | clean |
| `SectionProfileView.cs:279-291` plates | already follows `Labels.*` | clean |
| `PropertiesView.cs:1359-1372` | copies `row.Unit` and `row.Value` verbatim; no unit text of its own | clean |

## Item 2. sigma across surfaces

Trace (observed, from the code):

- Band: `ConditionsBand.axaml.cs:144-149` calls `OperatingPoints.Derive(BuildOperatingPoint(), ...)` on the text boxes as they stand now. Depth empty gives
  `DerivedReason.DepthNotSet` (`OperatingPoint.cs:68-80`), shown as "σ Unavailable — depth not set" (COPY-45).
- Section table and plate: `SectionTier.Evaluate/Derive` (`SectionTier.cs:98-108`) take the depth from the stored run's `op.HRef` (`WorkbenchController.cs:370`:
  `SectionTier.Derive(run, source)`); with `HRef` null the depth is null and `Cavitation.Screen` returns `ANA-CAV-DEPTH-NOT-SET` with no sigma
  (`Cavitation.cs:41-43`). There is no default depth anywhere: the Section cannot show a sigma for a run with no depth.
- So the two surfaces read different inputs: the band reads the live fields, the Section reads the last run. The capture `docs/proof/sfv/captures/E-app.png`
  came from a harness that stored a run at depth 0.6 m and left the band's depth box empty. By hand, sigma at 0.6 m, 5.14 m/s, salt 15 degC is
  (101325 + 1025 x 9.81 x 0.6 - 1700) / 13553.5 = 7.80, the number on the plate. The Section was right for its run; the band was right for its empty box.
- The band is not wrong, and no hydrodynamics question arises. A real-app route to the same picture exists: Evaluate at a depth, then clear the depth box.
  The status chip stays "Current" because the controller's operating point changes only at Evaluate (`WorkbenchController.cs:395-401`). That is a
  stale-input signal in `ConditionsBand`/`WorkbenchController` (not owned here); reported, not changed.

What was wrong and is fixed: with depth unset the plate drew no sigma line at all, while the band and the table said COPY-45. The plate now says
"σ " + COPY-45 ("σ Unavailable — depth not set"), the same words as the band; no new copy.

| Check | Old code | New code |
|---|---|---|
| `Sigma_SameStateOnBandPlateAndTable` (Analysis, readiness ring, 0.3-0.5 s) | `FAIL ... the plate carries the same COPY-45 state, no number expected σ Unavailable — depth not set; actual ` (empty) | `PASS` |
| `SectionProfile_DepthNotSet_PlateReadsCopy45` (Desktop `--readiness`) | n/a (new draw check; the plate text is fed from the model) | `PASS`; capture `sigma-depth-not-set.png` |

The same Analysis check pins depth set: the band's sigma (`Derive`), the table cell and the plate agree at two decimals for the default foil at 0.5 m.
Note for the operator: the Section's sigma is at each station's local depth (`h_ref` minus the station's rise), the band's is at `h_ref`; they agree to two
decimals only while the rise is small. That is the design (Cavitation.cs local-depth reduction), not a defect.

`sigma-depth-not-set.png` is the real profile control fed a synthetic profile with the plate text the projection now produces (the projection itself is
asserted in the Analysis check); `imperial-lift-per-span.png` is the real shell with a real run.
