---
id: proof-tmi-red-first
title: "TMI red-first receipt"
type: proof-pack
status: active
owner: "@trk-tmi"
phase: implementation
tags: [windows, test-hygiene, proof, ruling-176]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for making Telemetry_MarkerInjection_AbsentEverywhere portable to Windows: the backslash path case becomes a created subfolder on Windows, proven on the Mac through a pure helper.
---

# TMI red-first (Ruling 176 item 3)

Intent of `Telemetry_MarkerInjection_AbsentEverywhere`: a marker in a file name, and in a second path shape that carries a backslash,
must reach neither telemetry nor stderr. The second shape is one file name with a backslash on macOS (legal there). On Windows a
backslash is a separator, so the old `"win\\" + marker` needed a `win` folder that was never created (`DirectoryNotFoundException`
at `File.Copy`, PR #18 run 1, Desktop.log:283). The Windows case is now a separator case in a created `win` subfolder.

New check: `Telemetry_MarkerPaths_SeparatorShapeCreatesFolder` calls `CopyMarkerFiles(..., backslashIsSeparator: true)` on the Mac
(pure helper; the flag selects a path shape that is valid on every OS, so no foreign-OS branch runs).

Command: `CFD_TEST_ONLY=Telemetry_Marker tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll`

- Red (helper with the old behaviour, no folder created):
  `FAIL Telemetry_MarkerPaths_SeparatorShapeCreatesFolder InvalidOperationException: Separator shape did not create the win folder and copy into it: .../M.foil | .../win\M.foil`
- Green (after the fix):
  `PASS Telemetry_MarkerInjection_AbsentEverywhere` and `PASS Telemetry_MarkerPaths_SeparatorShapeCreatesFolder`

Not provable on the Mac: that the whole marker check passes on Windows. The next PC ring must show
`PASS Telemetry_MarkerInjection_AbsentEverywhere` in Desktop.log.
