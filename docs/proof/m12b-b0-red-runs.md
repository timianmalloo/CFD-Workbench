---
id: proof-m12b-b0-red-runs
title: M1.2b B0 red run of the named checks
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation
tags: [m12b, foildsl, red-first, b0]
links:
  - {to: design-m12b-points, rel: documents}
  - {to: proof-m12b-old-build, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
review-by: 2026-12-30
summary: >-
  The 29 B0 checks at commit 9ab4603, run by tools/run-tests.sh before the
  FoilDSL 4.1 grammar change. Exit 1. Twenty-six checks fail; three already
  match the unchanged reader.
---

# B0 red run

Commit `9ab46031f49a1f36d917e6026cbd5d5c23d9f4b4` names the checks and leaves
`ReadDocument`, `ReadCurve` and the version gate unchanged. `EnsureHeader41`
returns the same array. `Planform.View` throws. The old-build receipt
`e441355` is an ancestor.

Command, from the worktree root, with `AGENT_SESSION=track-b0` and `AGENT_WI=B0`:

```text
tools/run-tests.sh
```

`run_tests_exit=1`. Wall 48 s, inside the 60 s budget. Core printed 284 PASS
lines and then `RESULT failures=25`. CLI printed `FAIL Cli_InspectJson_PointsRolesAndKinds`
and exited 134. Desktop stayed green.

## Failures (old reader)

```text
FAIL Parse_Foil41WithTangents_RowsParsed InvalidOperationException: Expected True; actual False
FAIL Parse_TangentRowOnControlPoint_DslLock InvalidOperationException: Expected DSL-LOCK; actual DSL-SYNTAX
FAIL Parse_AngleKindOnChannel_DslLock InvalidOperationException: Expected DSL-LOCK; actual DSL-SYNTAX
FAIL Parse_SixteenChannelPointsUnder41_Parsed InvalidOperationException: Expected True; actual False
FAIL Parse_SeventeenChannelPointsUnder41_DslCurve InvalidOperationException: Expected DSL-CURVE; actual DSL-VERSION
FAIL Parse_Foil42UnknownBlock_DslVersion InvalidOperationException: Expected DSL-VERSION; actual DSL-SYNTAX
FAIL Parse_Foil41RoundTrip_RandomRowsStable InvalidOperationException: Expected True; actual False
FAIL Identity_TangentsRows_DefinitionHashUnchanged InvalidOperationException: Expected True; actual False
FAIL Identity_Header41RewriteSameGeometry_DefinitionHashUnchanged InvalidOperationException: Expected True; actual False
FAIL EnsureHeader41_FirstRow_HeaderRewritten InvalidOperationException: Expected True; actual False
FAIL Assess_SmoothRowSatisfied_Certified InvalidOperationException: Expected Certified; actual NotAssessed
FAIL Assess_SmoothRowOffByHalfDegree_Invalid InvalidOperationException: Expected Invalid; actual NotAssessed
FAIL Assess_SymmetricRowNotMidpoint_Invalid InvalidOperationException: Expected Invalid; actual NotAssessed
FAIL Assess_SixteenPointThreeAnchors_WorkCountBounded InvalidOperationException: Expected Certified; actual NotAssessed
FAIL PointModel_DefaultExample_RolesEndsHandlesControls NotImplementedException: The method or operation is not implemented.
FAIL PointModel_AnchorAtMultiplicityThree_HandlesAssigned NotImplementedException: The method or operation is not implemented.
FAIL PointModel_RootMirror_TeRootAftOnlyLeRootFixed NotImplementedException: The method or operation is not implemented.
FAIL PointModel_MultiplicityTwoKnot_ControlPointsAndC1Marker NotImplementedException: The method or operation is not implemented.
FAIL PlanformView_Sampling_NeverUsesProofBudget NotImplementedException: The method or operation is not implemented.
FAIL PlanformView_SplineBasisVsBernstein_Within1e12 NullReferenceException: Object reference not set to an instance of an object.
FAIL Planform_Probe_ChordAndRailsAtEta NotImplementedException: The method or operation is not implemented.
FAIL Planform_HandleTarget_AngleFromSpanAxis NotImplementedException: The method or operation is not implemented.
FAIL Comb_Anchor_TwoOneSidedTeeth NotImplementedException: The method or operation is not implemented.
FAIL Comb_CornerAnchor_BreakReported NotImplementedException: The method or operation is not implemented.
FAIL Comb_SixteenPointRail_FairnessFixture NotImplementedException: The method or operation is not implemented.
FAIL Cli_InspectJson_PointsRolesAndKinds
```

`Parse_Foil42UnknownBlock_DslVersion` is red: the unchanged reader reports
`DSL-SYNTAX` for a future version that contains an unknown block.

## Already matching

`Parse_TangentsUnder40_DslSyntax` passes (`DSL-SYNTAX`).
`Parse_ElevenChannelPointsUnder40_DslCurve` passes (`DSL-CURVE`).
`EnsureHeader41_Already41_Unchanged` passes because the stub returns the same array.
