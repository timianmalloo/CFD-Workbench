---
id: proof-c1-red-runs
title: C1 wing estimates and span red-first run
type: proof-pack
status: in-review
owner: "@track-c1"
phase: implementation
tags: [app-shell, wing-estimates, span, proof]
links:
  - {to: design-app-shell, rel: depends-on}
  - {to: adr-0006-driving-dimensions, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Red run of the fourteen C1 checks at 32e59ce, before EditReference accepted a dimension receipt.
  Receipt_Dimension_OldReaderRefusesDocReference failed with DOC-REFERENCE against the old validator.
review-suggested: []
---

# C1 wing estimates and span: red-first run

Track C1 (design `docs/design/app-shell.md` §3.8, §9, §14). Observed on branch `c1-wing-estimates`, macOS arm64, 2026-09-27.

The fourteen named checks were committed first (`32e59ce`). `WingEstimates.From`, `ApplyDimension` and `PatchSpan` throw. `EditReference` still ends in `_ => false`, so a receipt with `rail = "dimension"` is not a new field (`DOC-UNSUPPORTED-FIELD`); the old validator refuses it with `DOC-REFERENCE`.

## Red run, before the EditReference change

`tools/run-tests.sh` at `32e59ce`. Core exit 1. Wall 31 s. The C1 lines from `.tmp-tests/Core.log`:

```text
FAIL WingEstimates_Compute_EmitsEvent NotImplementedException: The method or operation is not implemented.
FAIL WingEstimates_ConstantChord_AreaArMeanMac NotImplementedException: The method or operation is not implemented.
FAIL WingEstimates_LinearTaper_MacDiffersFromMean NotImplementedException: The method or operation is not implemented.
FAIL WingEstimates_DraftBytes_DifferFromAccepted NotImplementedException: The method or operation is not implemented.
FAIL WingEstimates_Save_NoEstimateInFile NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Span15b_HalfSpanExact NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Span_ChordAt201EtaUnchanged NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Span_StationEtaUnchanged NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_Span_OneUndoStepUndoExact NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_SameOperationId_Memoized NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_SpanNotNumber_RefusedUnchanged NotImplementedException: The method or operation is not implemented.
FAIL ApplyDimension_SpanNonPositive_RefusedUnchanged NotImplementedException: The method or operation is not implemented.
FAIL Receipt_Dimension_OldReaderRefusesDocReference ContractError: DOC-REFERENCE
PASS Recovery_Dimension_Refused
RESULT failures=13
```

`Receipt_Dimension_OldReaderRefusesDocReference` is the old-validator observation: reopening a hand-built `rail = "dimension"`, `vertexId = "span"` receipt threw `DOC-REFERENCE`. `Recovery_Dimension_Refused` already passed, because a dimension recovery is not on the recovery allowlist.

Suite summary: Core exit 1 (237 PASS outside these failures), Cli exit 0, Desktop exit 0, wall 31 s, `tools/run-tests.sh` exit 1.
