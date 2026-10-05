---
id: proof-a3b-red-first
title: A3b section numerics red-first and proof pack
type: proof-pack
status: in-review
owner: "@track-d1"
phase: implementation
tags: [analysis, section, panel-method, cavitation, proof]
links:
  - {to: design-area3-analysis, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-11-05
summary: >-
  A3b red-first receipt and numerical proof for the linear-vorticity section panel,
  the panel-based section estimator and cavitation screen. Includes the corrected
  conformal-map oracle, measured interior convergence and Cp_min error, red-first
  repair receipts, test-ring costs, and integration limits.
review-suggested: []
---

# A3b section numerics — red-first and proof pack

Track `trk-d1`, branch `feature/a3b-numerics`, macOS arm64, 2026-10-05. Design authority:
`docs/design/area3-analysis.md` §5.1, §13.3/§13.5, and DR-ANA-2 option (a) at §15.
The 100/200/400-panel oracle and other named tests are in the fast ring; the
800-panel cusp case is in readiness (`AnalysisChecks.cs`).

## Red-first receipt

The named tests were committed at `0322d70` before the implementation. With the
three untracked implementation files temporarily moved aside and restored afterward,
`CFD_TEST_ONLY=PanelCp_KarmanTrefftz_100_200_400 dotnet run -c Release --project
tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj` exited 1:
`PanelCpTests.cs(41,20): error CS0246: SectionPoint could not be found`. This is
the observed baseline at `0322d70`: the test assembly cannot compile without the
A3b implementation. All nine A3b tests below reached green at `2164a26` and
remained green after the angle-conversion repair at `8775bcb`.

| Test | Failing input and planted mutant it detects | Red commit | Green commit |
|---|---|---|---|
| `PanelCp_KarmanTrefftz_100_200_400` (superseded) | Joukowski cusp at 4° and 100/200/400 panels; dropping the trailing-edge panel changes the lower count | `0322d70`; mutant failed `lower panel count expected 50; actual 49` | `2164a26`, then superseded by the repair below |
| `Section_ParabolicCamber_ZeroLiftMinus4p584Deg` (superseded) | 4% parabolic camber against Glauert α_L0 and Cm_c/4 | `0322d70` | `2164a26`, then superseded by the panel single-source repair below |
| `Section_ThinSymmetric_PanelClApproaches2PiSlope` | t/c 0.002 at 0° and 3°; a missing lift response or a second Cl definition fails | `0322d70` | `2164a26` |
| `Section_Ittc1957_TurbulentBound` | Re = 10⁶ and t/c = 0.12; omitted thickness factor or added α-dependent profile drag fails | `0322d70` | `2164a26` |
| `Cavitation_ScreenString_NamesStationCount` | 200 stations; omitting N failed COPY-48 equality | `0322d70`; mutant failed with `Cp_min resolution: stations` | `2164a26` |
| `Cavitation_Margin15Percent_Applied` | σ = 1.10(−Cp_min); ignoring margin failed `expected InsideMargin; actual Clear` | `0322d70`; mutant observed red | `2164a26` |
| `Cavitation_NegCpMinNonPositive_Undefined` | Cp_min = 0 and +0.1; an unconditional division yields a spurious value | `0322d70` | `2164a26` |
| `Cavitation_PvOrDepthMissing_Unavailable` | missing vapour pressure and missing depth; defaulting either to zero yields a spurious value | `0322d70` | `2164a26` |
| `Cavitation_GoverningStation_AtAlphaEffAndLocalDepth` | outer η 0.75 has α_eff 8° and depth 0.25 m while α_geo = 0° and h_ref = 0.5 m; α_geo mutant selected η 0, h_ref mutant failed local-depth equality | `0322d70`; both mutants observed red | `2164a26` |

The four specified planted mutant classes were applied one at a time to the owned
source, run with `CFD_TEST_ONLY=<name> dotnet run -c Release --project
tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj`, and reverted
before the green run. Each mutant exited 1 with its named `FAIL` line. The angle and
depth substitutions were tested separately.

## Numerical oracle and observed values

The first red run was partly an **oracle defect**: the old inverse used the
circle's leading ζ instead of the mapped leading w. Correcting that transform
removed one false error, but the full-surface RMS at 100/200/400 was
`0.112672 / 0.0851623 / 0.017199`. The 400-panel dip was accidental: the
cusped trailing-edge panel grew worse with refinement, from Cp `0.886` at 100
to `−1.941107` at 800, while its analytic value is about `0.147`. At 800 it
became the raw minimum and could wrongly control cavitation. The old monotonic
RMS test did not prove convergence.

The replacement oracle evaluates analytic Joukowski complex velocity at the
circle-angle midpoint of each panel and measures **interior RMS**, omitting
exactly the first and last three panels. Production `CpMin` likewise omits
the three panels nearest the TE on each side; for contours below eight panels
it omits only enough to leave two interior panels. All Cp samples remain in the
upper/lower arrays. This is a screen contract, not a claim that the cusp's
pointwise Cp is repaired. The independent 200,000-angle analytic reference has
`Cp_min = −1.713602662`.

| Panels | Interior RMS Cp | Observed order to next mesh | Panel Cp_min | Absolute Cp_min error | Test ceiling |
|---:|---:|---:|---:|---:|---:|
| 100 | 0.0325004 | 1.087 | −1.597071 | 0.116532 (6.80%) | 0.13 |
| 200 | 0.0152981 | 1.043 | −1.653954 | 0.0596486 (3.48%) | 0.07 |
| 400 | 0.00742289 | 1.021 to 800, readiness | −1.685973 | 0.0276297 (1.61%) | 0.04 |
| 800 | 0.00365849 | — | −1.699673 | 0.0139298 (0.813%) | 0.025, readiness |

Observed `p = log₂(E_N/E_2N)` is 1.087 and 1.043 over the three fast-ring
meshes; the 400→800 cross-ring measurement is 1.021. The test requires each
fast-ring pair to have `p ≥ 0.9`. First order is consistent with piecewise
straight geometry, linearly varying sheet strength and midpoint collocation;
this discretisation does not promise second-order interior Cp. The 800-panel
readiness check observes TE Cp `−1.941107` but reported `CpMin = −1.699673`,
equal to the interior minimum and within 0.025 of the analytic reference.

The current product `Settings.Default` has 13 chord positions (`SectionXs`),
which form **24 section panels** if passed to `PanelMethod`. A 24-panel KT
contour at 4° gives Cp_min `−1.257621`: absolute error `0.455982`, or a
**26.6% under-read** of the true suction peak. This is the same panel count,
not the same foil or spacing as a product section. It exceeds the nominal 15%
screen margin by 11.6 percentage points; applying that 15% to this under-read
would give only `0.844 ×` the true suction threshold. Section Cp/cavitation are
not yet wired into the production service. Integration must use a demonstrated
resolution or preserve an Unavailable screen at the default count; this proof
does not certify a 24-panel cavitation decision.

The panel is the sole source of section Cl, Cm_c/4 and α_L0. At 4% parabolic
camber and 10% thickness, α_L0 is `−4.14220529°`, found by solving panel Cl = 0;
panel Cm_c/4 at 0° is `−0.124559429`. The old Glauert α_L0 (`−4.583662361°`)
left panel Cl `−0.0531005`, so it was not the panel's zero lift. Glauert now
appears only in tests: the thin 0.2%-thick cambered section gives panel α_L0
`−4.62713572°` versus Glauert `−4.583662361°`, and Cm_c/4 `−0.125426269`
versus `−π·0.04`. A thin symmetric panel gives `dCl/dα = 6.26649016 rad⁻¹`,
0.266% from 2π. At Re = 10⁶, t/c = 0.12, the ITTC turbulent bound is
`0.01174164`, independent of α.

## Ring cost and acceptance

Command: `CFD_TEST_ONLY=PanelCp_,Section_,Cavitation_ dotnet run -c Release
--project tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj`.
After the repair, this named subset printed `RESULT failures=0`; the separate
800-panel readiness subset also printed `PASS`.

| Check | Observed COST (ms) | Design §13.5 estimate |
|---|---:|---:|
| KT interior and Cp_min, 100/200/400 | 32.244 | 200 |
| 800-panel cusp exclusion, readiness | 111.030 | readiness; 0.2 s observed ceiling |
| Panel camber zero lift | 16.244 | not separately estimated |
| Thin symmetric and cambered oracles | 17.854 / 15.020 | not separately estimated |
| ITTC turbulent bound | 4.371 | not separately estimated |
| Governing local ratio | 2.362 | 100 |

`COST` is the Analysis harness's per-check elapsed time in this warm process, not
a cross-platform performance claim. The built implementation is pure C# in
process. No external solver, polar or persistence path was added.

Repair-cycle red-first evidence: the new 800-panel test failed on the old
minimum (`reported −1.9411068` versus interior `−1.6996729`). The section
zero-lift test failed on the old Glauert estimate (`panel Cl = −0.0531005` at
its reported α_L0). The local-ratio test failed with a shallower, weaker
station: old winner `η 0.75`, required winner `η 0`. The split-reason test
failed on the combined "surface piercing or water invalid" copy. Each then
passed after its corresponding change. The nonzero-angle rotation test passed
on the original sign and fixes that convention as a regression check.

## Full-ring repair and exit receipt

The first `tools/run-tests.sh` run built successfully and the Analysis harness
reported 118 PASS, but Core's `PlacementRule_RadiansConstant_SingleSiteInSource`
failed on three newly written angle-conversion expressions. This source invariant
requires the repository's shared conversion helpers. The repair at `8775bcb`
uses `VortexLattice.ToRadians` and `.ToDegrees`; the exact Core check then
printed `PASS PlacementRule_RadiansConstant_SingleSiteInSource` (exit 0), and
the A3b subset printed all nine PASS again. This was one repair cycle.

The second `tools/run-tests.sh` run exited 0: Core partitions 334 and 333 PASS,
Desktop 661 PASS, Analysis 118 PASS, Cli 5 PASS, `wall 56 s (budget 60 s)`, and
`all test harnesses passed`. The two Avalonia AVLN3001 warnings were present in
the build but did not fail it. The first failed run and the green repair run are
both stated here because the first exposed a real source-contract omission.

That receipt precedes this reviewer repair. This repair's single full
`tools/run-tests.sh` attempt built and printed 334/333 Core, 661 Desktop,
123 Analysis and 5 Cli PASS, but exited 1 on cost gates. Under observed host
load `99.90 → 211.58`, Analysis took `15,744 ms` (C-2 limit 5,000 ms); its
two 800-panel tests then cost `1,405 / 873 ms` (C-5 limit 500 ms), and three
existing Freshness checks also crossed C-5. C-3/C-4 emitted `COST-MISS` under
Ruling 84's load rule. The 800-panel case was then moved to readiness and the
fast oracle's 200,000-point reference was replaced by the measured literal;
warm focused costs became `32.244 ms` fast and `111.030 ms` readiness. The
full ring was run once as requested and **has not been rerun** after this move;
the repair does not claim a green full-ring receipt. The strong host contention
also means those warm focused costs do not establish C-2 under comparable load.

## Claims, boundaries, and handoff

| Claim | Writer and compute reader | Evidence and confidence | Residual |
|---|---|---|---|
| Section Cp, Cl, Cm_c/4 and α_L0 share the panel | `PanelMethod.Solve` → `SectionEstimate` | KT interior order and Cp_min bounds, zero-lift root and thin-section oracle; Verified in Analysis harness | No production service or rendered Section tab consumes it yet; 24-panel default is too coarse for this KT screen example |
| Drag is the ITTC turbulent bound | `SectionEstimator.Estimate` → `SectionEstimate.CdTurbulentBound` | exact ITTC check; Verified | No transition or lift-dependent drag is modeled |
| Wing screen uses α_eff, local depth and minimum local σ/(−Cp_min) | `Cavitation.ScreenWing` / `.Screen` → `CavitationResult` | exact-copy, threshold, two-station ratio, depth-rotation and status tests; Verified | Wing service must pass stored strip α_eff and accepted geometry; product surface not wired |

The design's §5.1 row calls Cl the thin-airfoil expression, while DR-ANA-2(a)
later makes the panel the section Cl source. The same single-source rule now
covers Cm_c/4 and α_L0; the old Glauert production integrals are gone. The
coordinator owns the design-row replacement. Ruling 86 at
`chore/ruling-86:docs/notes/rulings.md` (`93c65f9`, operator, 2026-10-05)
governs the wing screen by
minimum local σ/(−Cp_min), not by the largest raw suction peak. That branch was
not on `origin/main` at this repair's rebase; the implementation follows its
operator-approved text.

The current `Labels.SectionCp` and the `AnalysisProjection` Section (2D) row
still say Cp is unavailable. Those files belong to Track C this round. The
coordinator should replace the obsolete unavailable copy and wire the estimator
and cavitation result into the service/projection after C joins, with a rendered
surface and cross-surface consistency check. Until then, this proof covers the
numerics API and Analysis harness, not the user-facing A3b flow.

Defect classes for the coordinator's register: (1) a transformed-coordinate
oracle that does not round-trip to the source contour; the test now uses the
circle-angle midpoint directly and checks interior order on three meshes;
(2) a singular boundary sample controls a safety screen despite interior
convergence; the 800-panel readiness assertion checks the TE exclusion;
(3) a wing reduction uses a numerator peak without each station's denominator;
the two-depth test requires the minimum local ratio. This track does not own
`docs/lessons/defect-classes.md`.
