---
id: proof-tfh-red-first
title: "Track TFH - red and green runs of the settings-hash diagnostic"
type: doc
status: draft
owner: "@trk-tfh"
phase: build
tags: [determinism, settings-hash, red-first]
links:
  - {to: proof-tfh-proposal, rel: relates-to}
review-by: 2027-04-01
summary: >-
  The split golden checks pass on the Mac and print DIAG lines when a golden is forced to differ (macOS arm64, base c74a78fc).
---

# Red-first: the diagnostic (test-only)

macOS arm64, .NET 10, base main `c74a78fc`. **Verified** (observed here). No Windows result is claimed.

## Red: a golden that differs prints the diagnostic

I changed the first character of `NoStationsHash` (and, in a second run, of `DefaultHash`) in a scratch edit, rebuilt, and ran
`CFD_TEST_ONLY=TeFloor_B10_SettingsHash_Golden_NoStations tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`:

```
DIAG nostations runtime=osx-arm64 arch=Arm64 hash=9b402c74...e58a
DIAG nostations jcs-without-stations={"chordSpacing":"cosine","envelope":"vlm-envelope/1","nChord":4,...,"wakeSpans":20}
DIAG nostations etas none
DIAG nostations xs none
DIAG nostations jcs-sha256=f75f5b9f...184b
FAIL TeFloor_B10_SettingsHash_Golden_NoStations InvalidOperationException: nostations expected 0b402c74...; actual 9b402c74...
RESULT failures=1
```

The `DefaultHash` run printed 145 `DIAG` lines (70 stations with bits). Station `etas[54]` read `3fe3affa292050b9`, the Mac libm value one ulp from the correctly rounded `...b8`. The scratch edits were reverted.

## Green: the committed goldens

`CFD_TEST_ONLY=TeFloor_B10 tools/run-suite.sh dotnet <dll>`: `PASS` for `_Golden_Default`, `_Golden_Small`, `_Golden_NoStations`, `_DiagnosticTextIsTheHashedText`, `TeFloor_B10_Numerics_DefaultRecordFieldsUnchanged`, `TeFloor_B10_Label_OneNamedConstant_AppDefaultNoSource`; `RESULT failures=0`.
