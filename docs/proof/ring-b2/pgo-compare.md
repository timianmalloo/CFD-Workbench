---
id: proof-ring-b2-pgo-compare
title: "Ring B2: Analysis readiness with TieredPGO on and off"
type: proof-pack
status: active
owner: "@trk-b2"
phase: implementation
tags: [ring, test-cost, tiered-pgo]
links:
  - { to: proof-ring-b2-profile, rel: relates-to }
summary: "One Analysis --readiness pass with DOTNET_TieredPGO=1 and one with 0 print identical PASS names and identical MEASURE values."
review-by: "2026-11-05"
---

# TieredPGO on versus off (test-architect condition on the B2 join)

`tools/run-tests.sh` exports `DOTNET_TieredPGO=0`. This pass shows the setting does not change a numerical result: the Analysis harness,
`--readiness`, Release, run once with `DOTNET_TieredPGO=1` and once with `0`. Both exit 0 with `RESULT failures=0`. `cmp` on the extracted
lines: PASS lines identical (10), MEASURE lines identical (11), every digit.

## PASS lines (both runs)

```
PASS Readiness_Camber4_N128Convergence
PASS Readiness_Washin1_N128Convergence
PASS Readiness_Camber4_N256Point
PASS Readiness_Washin1_N256Solves
PASS Readiness_EllipticQuarterChord_SweepZeroFine
PASS F2_EllipticAR8_RichardsonClInRecordedBand
PASS F5_InducedDrag_TrefftzWithin1PercentOfNearField
PASS Retention_PruneThenUndo_TombstoneReadsPruned
PASS Freshness_ProfileEdit_Historical
PASS PanelCp_Cusp800_TeExcludedFromCpMin
```

## MEASURE lines (both runs)

```
MEASURE flat n=64 tipAi=1.9338212116470839 CL=0.40114222545272266 kappa1=1103.3688595591279 residualInf=6.8278716014447127E-15
MEASURE camber n=64 tipAi=4.9576172097241731 CL=0.77842297648594505 kappa1=1104.0159763340032 residualInf=1.9650947535865271E-14
MEASURE camber n=128 tipAi=4.9770613103764658 CL=0.77694162070117179 kappa1=4395.5830073307579 residualInf=5.7537308251198738E-14
MEASURE washin n=64 tipAi=2.365956994691333 CL=0.43704989172738007 kappa1=1103.257371643844 residualInf=7.3691053259494765E-15
MEASURE flat n=64 tipAi=1.9338212116470839 CL=0.40114222545272266 kappa1=1103.3688595591279 residualInf=6.8278716014447127E-15
MEASURE washin n=128 tipAi=2.3713799418184354 CL=0.43615647743445113 kappa1=4392.5580939251668 residualInf=4.0634162701280729E-14
MEASURE camber n=256 tipAi=4.9861533951699917 CL=0.77619596371981925 kappa1=17541.785068285251 residualInf=4.70762318016682E-13
MEASURE washin n=256 tipAi=2.3739521341199317 CL=0.43570714998171328 kappa1=17529.704204314083 residualInf=2.4880097981849758E-13
MEASURE straight-quarter-chord n=128 maxSweep=0
MEASURE straight-quarter-chord n=256 maxSweep=0
MEASURE F6 CL=0.42061592925327917/0.41906892443595917/0.4183318986301362 e=1.0188185139225332/1.008614926943703/1.0035578493401365 pCL=1.0696906502356247 pE=1.0127005850954285
```
