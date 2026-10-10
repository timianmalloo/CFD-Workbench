---
id: proof-hyg-red-first
title: "HYG red-first receipt"
type: proof-pack
status: active
owner: "@trk-hyg"
phase: implementation
tags: [hyg, export, symlink, red-first]
links:
  - { to: proof-hyg-core-cost, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  The link-target check fails on the old code (the dialog's outcome has no code and records export.write EXPORT-WRITE-FAILED) and passes on the new, with the observed lines.
---

# HYG red-first receipt

Track `trk-hyg`, branch `fix/hyg-export-link`, base `945ee04e`, 2026-10-10, Release, macOS.

Check: `Export_LinkTargetHasOneCodeOnBothSurfaces` (`tests/CfdWorkbench.Cli.Tests/ExportCliTests.cs`). For a link to a file and a dangling
link it runs the CLI (`ExportVerb.RunAsync`) and the dialog's session (`ExportSession.RunAsync`, the same source) and requires exit 6, one event on each
surface equal to `export.validate / EXPORT-TARGET-LINK`, the dialog outcome code `EXPORT-TARGET-LINK`, and no file written or left.

Command: `dotnet run -c Release --project tests/CfdWorkbench.Cli.Tests`

Red, old shared writer (only the `Code` field of `ExportOutcome` added so the check compiles; behaviour unchanged):

    FAIL Export_LinkTargetHasOneCodeOnBothSurfaces Exception: file-link.dat: the dialog outcome code is

Green, after `ExportSession.RunAsync` refuses a link before any write and the CLI maps the outcome code to exit 6:

    PASS Cli_Export_SymlinkTargetRefused
    PASS Export_LinkTargetHasOneCodeOnBothSurfaces
    PASS Cli_Export_EmitsOneTelemetryEventPerOutcome_NoPathNoName

Decision: the kind is `export.validate`, before the write. A link is known before any byte moves, `EXPORT-WOULD-REPLACE` already records
this way, and the CLI's existing telemetry check expects it. The dialog keeps COPY-507 and the CLI keeps its line: DESIGN.md has no approved
link-specific message (COPY gap, reported). The CLI's own pre-check is deleted; the writer's `IsLink` guard stays as defence for direct callers.
A dangling link is now caught too (the old test `File.Exists` was false for it).
