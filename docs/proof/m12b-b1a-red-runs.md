---
id: proof-m12b-b1a-red-runs
title: M1.2b B1a chord red-first run
type: proof-pack
status: in-review
owner: "@track-b1a"
phase: implementation
tags: [m12b, chords, length-expression, proof]
links:
  - {to: design-m12b-points, rel: depends-on}
  - {to: adr-0006-driving-dimensions, rel: depends-on}
  - {to: adr-0005-point-types, rel: depends-on}
  - {to: adr-0001-master-curve-degree, rel: depends-on}
review-by: 2026-10-30
summary: >-
  Red run of the 24 B1a checks at 6705872, before the chord fit and the dimension fingerprint.
  Reopen_RetrySameDimensionOperationId_ReturnsPriorId failed with DOC-OPERATION-CONFLICT against the as-built memo.
review-suggested: []
---

# M1.2b B1a chords: red-first run

Track B1a (design `docs/design/m12b-points.md` §3.4, §3.9, §9, §12.4). Observed on branch `m12b-b1a-core-chords`, macOS, 2026-09-30.

The 24 named checks were committed first (`6705872`). `ApplyChord`, `ChordDimension.Evaluate`, `ChordDimension.FitOrdinates` and `LengthExpression.ParseMeters` throw. The dimension memo payload and `EditReference` are unchanged. `"rule"` is not yet in `NativeProject.Read`'s optional list.

## Red run, before the fingerprint and the fit

`tools/run-tests.sh` at `6705872`. Core exit 1. Wall 48 s. `tools/run-tests.sh` exit 1. The B1a lines from `.tmp-tests/Core.log` (`RESULT failures=24`; no earlier check failed):

```text
FAIL ApplyChord_NewFoilRootX12_AcceptedBothNumbers NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_FitJustBelowLimit_Accepted NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_FitJustAboveLimit_AcceptedWithWarning NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_NewFoilRootX15_AcceptedWithFitWarning NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_RootChordShift_P0EqualsP1BitsZero NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_TipChord_NoShiftRootChordUnchanged NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_LocksOff_LinearRuleExact NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_OneRailLocked_ResidualReported NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Cad17Taper_ChordRuleWithinTolerance NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Refused_HistoryUnchanged NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_TipChordClosingTip_DslTarget NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_EdgesWouldCross_DslEdgesCross NotImplementedException: The method or operation is not implemented.
FAIL ChordRefit_SyntheticLinearRows_HeldExactly NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Receipt_CarriesRuleId NotImplementedException: The method or operation is not implemented.
FAIL BlendRule_LockStateToRuleId_Pinned NotImplementedException: The method or operation is not implemented.
FAIL ApplyChord_FitAboveLimit_ApplyEventCarriesFitAndWarning NotImplementedException: The method or operation is not implemented.
FAIL Reopen_ChordRows_UndoRedoRoundTrip NotImplementedException: The method or operation is not implemented.
FAIL Reopen_ForgedRuleValue_DocReference InvalidOperationException: Expected DOC-REFERENCE; actual DOC-UNSUPPORTED-FIELD
FAIL Reopen_ChordRowWithoutRule_DocReference InvalidOperationException: Expected refusal DOC-REFERENCE
FAIL Reopen_RetrySameDimensionOperationId_ReturnsPriorId ContractError: DOC-OPERATION-CONFLICT
FAIL LengthExpression_UnitsAndReferences_ResolvedToMetres NotImplementedException: The method or operation is not implemented.
FAIL LengthExpression_NonLength_DslUnit NotImplementedException: The method or operation is not implemented.
FAIL LengthExpression_RandomText_NeverThrowsUnexpected NotImplementedException: The method or operation is not implemented.
FAIL LengthExpression_SubMicrometreInput_RoundedToMicrometre NotImplementedException: The method or operation is not implemented.
RESULT failures=24
```

`Reopen_RetrySameDimensionOperationId_ReturnsPriorId` is the F-5 observation: after Save and Reopen, retrying the same Span operation id threw `DOC-OPERATION-CONFLICT` before any chord call. `Reopen_ForgedRuleValue_DocReference` threw `DOC-UNSUPPORTED-FIELD` because `"rule"` is not optional yet. `Reopen_ChordRowWithoutRule_DocReference` reopened, because a root-chord receipt with no rule is still accepted.

Suite summary: Core exit 1 (281 PASS outside these 24 failures), Cli exit 0, Desktop exit 0, wall 48 s, `tools/run-tests.sh` exit 1.
