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
  the estimator's Glauert and ITTC reads, and the cavitation screen. Includes the
  conformal-map Cp oracle, measured panel convergence, planted mutants, ring costs,
  and the integration seams left to the coordinator.
review-suggested: []
---

# A3b section numerics — red-first and proof pack

Track `trk-d1`, branch `feature/a3b-numerics`, macOS arm64, 2026-10-05. Design authority:
`docs/design/area3-analysis.md` §5.1, §13.3/§13.5, and DR-ANA-2 option (a) at §15.
The test ring is ring 0 (`tests/CfdWorkbench.Analysis.Tests/AnalysisChecks.cs`).

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
| `PanelCp_KarmanTrefftz_100_200_400` | Joukowski cusp at 4° and 100/200/400 panels; dropping the trailing-edge panel changes the lower count | `0322d70`; mutant failed `lower panel count expected 50; actual 49` | `2164a26` |
| `Section_ParabolicCamber_ZeroLiftMinus4p584Deg` | 4% parabolic camber gives α_L0 = −4.583662361° and Cm_c/4 = −π·0.04; a zero camber integral fails | `0322d70` | `2164a26` |
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

`PanelCp_KarmanTrefftz_100_200_400` uses the exponent-2 Kármán–Trefftz family
(Joukowski cusp). The independent oracle evaluates the analytic complex velocity
on the mapped foil, at the panel midpoints. Its first implementation used the
circle's leading ζ in place of the mapped leading w when unnormalizing x. This
oracle error produced nonconvergent RMS values `0.3583 / 0.3630 / 0.3614` and
was corrected before acceptance. The corrected oracle gives RMS Cp errors:

| Panels | RMS Cp error | Test ceiling |
|---:|---:|---:|
| 100 | 0.112672 | 0.20 |
| 200 | 0.0851623 | 0.11 |
| 400 | 0.017199 | 0.06 |

All three errors decrease, and the 400-panel error is 15.3% of the 100-panel error.
The upper and lower sample counts are both N/2. The first midpoint next to the
cusped trailing edge remains the hardest sample; the RMS and monotonic checks are
the acceptance evidence, not a claim of pointwise accuracy at that cusp.

The t/c 0.002 symmetric section at 200 panels produced
`dCl/dα = 6.26649016 rad⁻¹` from the panel's 0° and 3° evaluations. The
relative difference from 2π is 0.266%; the test now permits at most 1%.
The 4% parabolic camber's analytic α_L0 is −4.583662361°; the estimator returns
its Glauert moment, while `SectionEstimate.Cl` reads `Panel.Cl` only.
At Re = 10⁶ and t/c = 0.12, the ITTC-1957 expression gives Cd bound 0.01174164,
independent of α in this method.

## Ring cost and acceptance

Command: `CFD_TEST_ONLY=PanelCp_,Section_,Cavitation_ dotnet run -c Release
--project tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj`.
Observed `RESULT failures=0` and all nine A3b names printed `PASS`.

| Check | Observed COST (ms) | Design §13.5 estimate |
|---|---:|---:|
| Panel Cp KT, all three counts | 25.554 | 200 |
| Parabolic camber | 4.131 | not separately estimated |
| Thin symmetric panel lift | 10.484 | not separately estimated |
| ITTC turbulent bound | 1.198 | not separately estimated |
| Screen string | 0.342 | < 5 |
| 15% margin | 0.230 | < 1 |
| Nonpositive −Cp_min | 0.107 | < 1 |
| Missing p_v or depth | 0.082 | < 1 |
| Governing station | 2.587 | 100 |

`COST` is the Analysis harness's per-check elapsed time in this warm process, not
a cross-platform performance claim. The built implementation is pure C# in
process. No external solver, polar or persistence path was added.

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

`python3 docs/ai-forward-pack/scripts/docs-graph.py derive` indexed this proof
as one of 270 artifacts. `python3 tools/check-docs.py` exited 0 with
`Documentation checks passed`; its 131 review suggestions are existing
non-failing graph flags.

## Claims, boundaries, and handoff

| Claim | Writer and compute reader | Evidence and confidence | Residual |
|---|---|---|---|
| Section Cp and Cl share the panel solution | `PanelMethod.Solve` → `SectionEstimate.Panel` / `.Cl` | conformal-map Cp oracle, count and lift checks; Verified in Analysis harness | No production service or rendered Section tab consumes it yet |
| Camber α_L0 and Cm_c/4 use `CamberSlope`; Cd is the turbulent bound | `SectionEstimator.Estimate` → `SectionEstimate` | analytic parabolic-camber and exact ITTC checks; Verified | Quadrature and method envelope on arbitrary authored profiles require later integration tests |
| Wing screen uses α_eff and local depth, names N, and fails without inputs | `Cavitation.ScreenWing` / `.Screen` → `CavitationResult` | exact-copy, threshold, state and governing-station tests plus mutants; Verified | Wing service must pass its stored strip α_eff and accepted station geometry |

The design's §5.1 row calls Cl the thin-airfoil expression, while DR-ANA-2(a)
later says the estimator's section Cl comes from the panel as one definition.
This implementation follows the explicit ruling: thin-airfoil integrals supply
α_L0 and Cm_c/4, and the sole reported section Cl is panel Cl. The coordinator
should confirm this reading at the join.

The current `Labels.SectionCp` and the `AnalysisProjection` Section (2D) row
still say Cp is unavailable. Those files belong to Track C this round. The
coordinator should replace the obsolete unavailable copy and wire the estimator
and cavitation result into the service/projection after C joins, with a rendered
surface and cross-surface consistency check. Until then, this proof covers the
numerics API and Analysis harness, not the user-facing A3b flow.

The mapped-leading-coordinate oracle defect is a **class**: an analytic oracle
can use the source-plane coordinate after a transform, producing a false solver
finding. Sweep: inspect every mapping from normalized foil coordinates to
complex-plane coordinates in oracle fixtures. Prevent: derive the mapping from
the same forward transform in the fixture and require convergence at three
resolutions. The class entry in `docs/lessons/defect-classes.md` needs the
coordinator because this track does not own that register.
