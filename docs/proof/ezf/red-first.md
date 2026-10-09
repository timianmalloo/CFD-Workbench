---
id: proof-ezf-red-first
title: EZF red-first record - Fixture.Reset drain and the folded settled-surface assertion
type: proof-pack
status: draft
owner: "@trk-ezf"
phase: implementation
tags: [ezf, red-first, desktop-tests]
links:
  - { to: investigation-ezf-zoompanfit, rel: relates-to }
review-by: 2026-11-08
summary: >-
  Planted-delay record for Ruling 172. The camera compare fails on the old Reset at 60 and 150 ms; the folded assertion fails at
  150 ms; with the Reset drain every delay from 0 to 800 ms passes.
---

# EZF red-first (Ruling 172)

Method: a temporary hook in the default surface compute (`WorkbenchController.cs:531`) slept `EZF_DELAY_MS` ms for basis
"accepted". It is reverted; the committed tree has no hook. Command, per delay (script `scratch/ezf/grid.sh`):

```
EZF_DELAY_MS=<d> CFD_TEST_ONLY=Elevation_TwistDomainClamp,Elevation_ZoomPanFit \
  dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --views
```

| Delay (ms) | A: old Reset, no assertion | B: old Reset + folded assertion | C: Reset drain + folded assertion |
|---|---|---|---|
| 0 | PASS | PASS | PASS |
| 60 | FAIL "⌘0 fits" | FAIL "⌘0 fits" | PASS |
| 150 | FAIL "⌘0 fits" | FAIL "The baseline fit was taken while a mesh was still pending" | PASS |
| 200 | (earlier run: FAIL "⌥→ pans 10 %") | not run | PASS |
| 800 | (earlier run: PASS, mesh lands after the check) | not run | PASS |

Reading: the folded assertion fires only when the mesh is still pending at the baseline (150 ms). At 60 ms the mesh lands
between `Reset`'s fit and the check start, so `SurfaceUpdating` is already false and only the camera compare catches the stale
baseline. The drain in `Reset` removes both. The assertion is therefore a guard for a `Reset` that stops draining, not a
replacement for the camera compare.
